using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>Where an ingested board's data came from (VASL Board Ingestion Design, section 8.2).</summary>
public sealed record BoardProvenance(
    string? VaslCommit,
    VaslBoardSourceKind SourceKind,
    SourceFileProvenance LosData,
    SourceFileProvenance Metadata,
    SourceFileProvenance? Archive,
    SourceFileProvenance SharedBoardMetadata,
    string ImporterVersion);

/// <summary>A metadata building-type override compared with the terrain the grid gives the hex center.</summary>
public sealed record BuildingOverrideCheck(HexName Hex, string Expected, string Actual, CenterTerrainSource Source)
{
    public bool Consistent => Expected == Actual;
}

/// <summary>A decoded VASL board. Hex facts are derived from it separately (ASL-MAP-012).</summary>
public sealed record IngestedBoard(
    BoardRef Board,
    BoardGeometry Geometry,
    TerrainGrid Grid,
    BoardMetadata Metadata,
    BoardProvenance Provenance,
    F1Result F1,
    IReadOnlyList<BuildingOverrideCheck> BuildingOverrides)
{
    /// <summary>The source identity: the VASL board and the Git blob id of its LOSData.</summary>
    public string SourceIdentity => $"vasl:{Board.Value}@{Provenance.LosData.ContentBlob}";
}

public sealed record BoardImportResult(IngestedBoard? Board, IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool Succeeded => Board is not null;

    /// <summary>True when the board was recognized but is outside version 1 scope (<c>VASL-SCOPE-001</c>).</summary>
    public bool OutOfScope => Diagnostics.Any(diagnostic => diagnostic.Code == "VASL-SCOPE-001");
}

/// <summary>Whether a board is within version 1 scope, decided from its metadata alone.</summary>
public enum BoardScope
{
    /// <summary>A standard geomorphic board that version 1 ingests.</summary>
    InScope,

    /// <summary>Recognized and declined by design (<c>VASL-SCOPE-001</c>).</summary>
    OutOfScope,

    /// <summary>The source is missing or its metadata cannot be read, so scope is unknown.</summary>
    Unreadable,
}

/// <summary>The scope decision for one board, with the metadata it was made from.</summary>
public sealed record BoardScopeResult(BoardScope Scope, BoardMetadata? Metadata, byte[]? MetadataBytes, IReadOnlyList<MapDiagnostic> Diagnostics)
{
    /// <summary>The reason a board is out of scope or unreadable, or null when it is in scope.</summary>
    public string? Reason => Scope == BoardScope.InScope
        ? null
        : Diagnostics.LastOrDefault(diagnostic => diagnostic.Code == "VASL-SCOPE-001" || diagnostic.Severity == MapDiagnosticSeverity.Error)?.Message;
}

/// <summary>
/// Ingests one VASL board (VASL Board Ingestion Design, sections 1 to 8): version 1 scope check, metadata,
/// LOSData decoding, catalog coverage, building-override consistency, provenance, and the F1 check.
/// </summary>
public static class VaslBoardImporter
{
    public const string ImporterVersion = "1.1.0";

    /// <summary>The standard geomorphic hex size, which VASL uses when a board's metadata gives none.</summary>
    public const double StandardHexWidth = 1800.0 / 32.0;

    public const double StandardHexHeight = 645.0 / 10.0;

    // ASLMap.buildVASLMap sends boards with these names to board-specific HASL code paths.
    private static readonly HashSet<string> HaslBoards = new(StringComparer.Ordinal) { "RBv3", "RO", "DaE", "SG", "HT", "VotG", "SaPF", "FB" };

    // Map.createtheHexGrid lays out hexes only for these A1 center heights; ASLMap passes half the hex height.
    private static readonly double[] LaidOutA1CenterY = [32.25, 32.235, -612.75, 97.1];

    // The A1CenterX of b boards (1b to 26b and similar), which VASL names Q1 onward.
    private const double BBoardA1CenterX = -901;

    // The A1CenterY of the lower boards of BFP double-width pairs, which VASL names A11 onward.
    private const double DoubleWidthA1CenterY = -612.75;

    /// <summary>
    /// Decides version 1 scope from the board's metadata only, without reading LOSData. The board library uses this
    /// to list boards cheaply; <see cref="Import"/> makes the same decision first.
    /// </summary>
    public static BoardScopeResult CheckScope(VaslBoardSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.Exists)
        {
            return new BoardScopeResult(BoardScope.Unreadable, null, null, [Error("VASL-SRC-003", $"{source.RepositoryPath} does not exist.")]);
        }

        var metadataBytes = source.ReadEntry(VaslBoardSource.MetadataEntry);
        if (metadataBytes is null)
        {
            return new BoardScopeResult(BoardScope.OutOfScope, null, null,
                [Scope($"{source.RepositoryPath} has no BoardMetadata.xml (a legacy V5 board).")]);
        }

        BoardMetadataResult metadataResult;
        using (var stream = new MemoryStream(metadataBytes))
        {
            metadataResult = BoardMetadataParser.Parse(stream);
        }

        if (metadataResult.Metadata is not { } metadata)
        {
            return new BoardScopeResult(BoardScope.Unreadable, null, metadataBytes, metadataResult.Diagnostics);
        }

        var problem = ScopeProblem(metadata) ?? (source.HasEntry(VaslBoardSource.LosDataEntry) ? null : "no LOSData.");
        return problem is not null
            ? new BoardScopeResult(BoardScope.OutOfScope, metadata, metadataBytes, [.. metadataResult.Diagnostics, Scope($"bd{source.BoardName}: {problem}")])
            : new BoardScopeResult(BoardScope.InScope, metadata, metadataBytes, metadataResult.Diagnostics);
    }

    public static BoardImportResult Import(VaslSource vasl, VaslBoardSource source, TerrainCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(vasl);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(catalog);
        var scope = CheckScope(source);
        var diagnostics = new List<MapDiagnostic>(scope.Diagnostics);
        if (scope.Scope != BoardScope.InScope)
        {
            return new BoardImportResult(null, diagnostics);
        }

        var metadata = scope.Metadata!;
        var metadataBytes = scope.MetadataBytes!;
        var losDataBytes = source.ReadEntry(VaslBoardSource.LosDataEntry);
        if (losDataBytes is null)
        {
            return Fail(diagnostics, Scope($"bd{source.BoardName} has no LOSData."));
        }

        LosDataDecodeResult decoded;
        using (var stream = new MemoryStream(losDataBytes))
        {
            decoded = LosDataCodec.Decode(stream, header => GeometryFor(metadata, header));
        }

        diagnostics.AddRange(decoded.Diagnostics);
        if (decoded.Grid is not { } grid)
        {
            return new BoardImportResult(null, diagnostics);
        }

        var missingCodes = grid.DistinctCodes().Where(code => !catalog.TryGet(code, out _)).ToArray();
        if (missingCodes.Length > 0)
        {
            return Fail(diagnostics, Error("VASL-CAT-002",
                $"bd{source.BoardName} uses terrain codes absent from the catalog: {string.Join(", ", missingCodes)}."));
        }

        var geometry = grid.Geometry;
        ReportUnknownHexes(metadata, geometry, diagnostics);
        var overrideChecks = CheckBuildingOverrides(metadata, grid, catalog, diagnostics);
        var provenance = new BoardProvenance(
            vasl.Git?.HeadCommit,
            source.Kind,
            source.Provenance(VaslBoardSource.LosDataEntry, losDataBytes),
            source.Provenance(VaslBoardSource.MetadataEntry, metadataBytes),
            source.ArchiveProvenance(),
            vasl.SharedBoardMetadataProvenance(),
            ImporterVersion);
        var f1 = LosDataFidelity.CheckF1(decoded.DecompressedStream!, grid);
        var board = new IngestedBoard(BoardRef.ForVaslBoard(source.BoardName), geometry, grid, metadata, provenance, f1, overrideChecks);
        return new BoardImportResult(board, diagnostics);
    }

    /// <summary>
    /// Why a board is outside the supported scope, or null when VASL lays it out as a geomorphic board (VASL Board
    /// Ingestion Design, sections 1 and 11.1): its own hex size, the A1 center dot at (0, half a hex height), odd
    /// columns one hex longer, and names starting at A1, at Q1 for b boards, or at A11 for the lower double-width boards.
    /// This covers 33 by 10 boards, a/b half boards, and the BFP double-width and Deluxe boards. HASL maps, which VASL
    /// builds with board-specific code, and boards whose metadata places A1 where VASL's layout does not are out of scope.
    /// </summary>
    public static string? ScopeProblem(BoardMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (HaslBoards.Contains(metadata.Name))
        {
            return $"{metadata.Name} is a HASL map that VASL builds with board-specific code.";
        }

        if (metadata.Width < 2 || metadata.Height < 1)
        {
            return $"{metadata.Width} by {metadata.Height} hexes is not a board size VASL lays out.";
        }

        if (Attribute(metadata, "altHexGrain") is { } grain && (!JdomBoolean.TryParse(grain, out var alternate) || alternate))
        {
            return "alternate hex grain.";
        }

        if (!TryGeometry(metadata, "hexWidth", StandardHexWidth, out var hexWidth) || hexWidth <= 0)
        {
            return $"custom geometry hexWidth=\"{Attribute(metadata, "hexWidth")}\".";
        }

        if (!TryGeometry(metadata, "hexHeight", StandardHexHeight, out var hexHeight) || hexHeight <= 0)
        {
            return $"custom geometry hexHeight=\"{Attribute(metadata, "hexHeight")}\".";
        }

        if (!LaidOutA1CenterY.Contains(hexHeight / 2.0))
        {
            return $"custom geometry hexHeight=\"{Attribute(metadata, "hexHeight")}\": VASL lays out no hexes for an A1 center at {hexHeight / 2.0}.";
        }

        if (!TryGeometry(metadata, "A1CenterX", 0.0, out var a1CenterX) || (a1CenterX != 0.0 && a1CenterX != BBoardA1CenterX))
        {
            return $"custom geometry A1CenterX=\"{Attribute(metadata, "A1CenterX")}\".";
        }

        // VASL's layout puts A1 at half the hex height whatever the metadata says, so a stated value must agree with it.
        if (!TryGeometry(metadata, "A1CenterY", hexHeight / 2.0, out var a1CenterY)
            || (Math.Abs(a1CenterY - (hexHeight / 2.0)) > 0.5 && a1CenterY != DoubleWidthA1CenterY))
        {
            return $"custom geometry A1CenterY=\"{Attribute(metadata, "A1CenterY")}\".";
        }

        return null;
    }

    /// <summary>
    /// The layout VASL's runtime gives an in-scope board with this LOSData header. The grid size comes from the header;
    /// it must reach every hex center dot, or the geometry is given the size the hexes imply and decoding reports the
    /// difference (<c>VASL-LOS-005</c>).
    /// </summary>
    public static BoardGeometry GeometryFor(BoardMetadata metadata, LosDataHeader header)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        TryGeometry(metadata, "hexWidth", StandardHexWidth, out var hexWidth);
        TryGeometry(metadata, "hexHeight", StandardHexHeight, out var hexHeight);
        TryGeometry(metadata, "A1CenterX", 0.0, out var a1CenterX);
        TryGeometry(metadata, "A1CenterY", hexHeight / 2.0, out var a1CenterY);
        var impliedWidth = (int)Math.Ceiling((metadata.Width - 1) * hexWidth);
        var impliedHeight = (int)Math.Ceiling(metadata.Height * hexHeight);
        var reachesCenters = header.GridWidth >= impliedWidth - (hexWidth / 2.0) && header.GridWidth <= impliedWidth + hexWidth
            && header.GridHeight >= impliedHeight - (hexHeight / 2.0) && header.GridHeight <= impliedHeight + hexHeight;
        return BoardGeometry.Vasl(metadata.Width, metadata.Height, hexWidth, hexHeight,
            reachesCenters ? header.GridWidth : impliedWidth,
            reachesCenters ? header.GridHeight : impliedHeight,
            a1CenterX == BBoardA1CenterX ? 16 : 0,
            a1CenterY == DoubleWidthA1CenterY ? 10 : 0);
    }

    private static string? Attribute(BoardMetadata metadata, string name) =>
        metadata.GeometryAttributes.TryGetValue(name, out var value) ? value : null;

    // BoardArchive getters: a missing attribute takes the standard value.
    private static bool TryGeometry(BoardMetadata metadata, string name, double standard, out double value)
    {
        value = standard;
        return Attribute(metadata, name) is not { } text
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static void ReportUnknownHexes(BoardMetadata metadata, BoardGeometry geometry, List<MapDiagnostic> diagnostics)
    {
        var referenced = metadata.BuildingTypes.Select(item => ("buildingType", item.Hex))
            .Concat(metadata.Slopes.Select(item => ("slope", item.Hex)))
            .Concat(metadata.RailroadEmbankments.Select(item => ("rrembankment", item.Hex)))
            .Concat(metadata.PartialOrchards.Select(item => ("partialorchard", item.Hex)));
        foreach (var (element, hex) in referenced)
        {
            if (!geometry.TryGetIndex(hex, out _))
            {
                diagnostics.Add(new MapDiagnostic("VASL-META-002", MapDiagnosticSeverity.Warning,
                    $"<{element}> names hex {hex}, which is not on the board; VASL ignores it."));
            }
        }
    }

    // Overrides are written into LOSData when VASL creates it, so the grid must already agree (Ingestion Design 6.4).
    private static List<BuildingOverrideCheck> CheckBuildingOverrides(BoardMetadata metadata, TerrainGrid grid, TerrainCatalog catalog,
        List<MapDiagnostic> diagnostics)
    {
        var checks = new List<BuildingOverrideCheck>();
        foreach (var buildingType in metadata.BuildingTypes)
        {
            if (!grid.Geometry.TryGetIndex(buildingType.Hex, out var hex))
            {
                continue;
            }

            var sample = CenterTerrainSampler.Sample(grid, catalog, hex);
            var check = new BuildingOverrideCheck(buildingType.Hex, buildingType.BuildingTypeName, sample.Terrain.Name, sample.Source);
            checks.Add(check);
            if (!check.Consistent)
            {
                diagnostics.Add(new MapDiagnostic("VASL-META-004", MapDiagnosticSeverity.Warning,
                    $"Override for {check.Hex} names '{check.Expected}' but the grid gives '{check.Actual}' ({check.Source})."));
            }
        }

        return checks;
    }

    private static MapDiagnostic Scope(string message) => new("VASL-SCOPE-001", MapDiagnosticSeverity.Info, message);

    private static MapDiagnostic Error(string code, string message) => new(code, MapDiagnosticSeverity.Error, message);

    private static BoardImportResult Fail(List<MapDiagnostic> diagnostics, MapDiagnostic diagnostic)
    {
        diagnostics.Add(diagnostic);
        return new BoardImportResult(null, diagnostics);
    }
}
