using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

/// <summary>Board ingestion against a local VASL checkout; skipped without <c>AslMaps__VaslRoot</c>.</summary>
public sealed class LocalVaslBoardTests
{
    // Pinned evidence for board 01 (VASL Board 01 Terrain Evidence; Ingestion Design section 8.2).
    private const string Board01LosDataBlob = "8d77d26222b7bb21d8c1fdda6ba05b447f63c317";
    private const string Board01MetadataBlob = "e91b0d99a7a788812444753cae245841875a8de6";

    [VaslFact]
    public void Board01DecodesAndPassesF1()
    {
        var board = Import("01");

        Assert.Equal("bd01", board.Board.Value);
        Assert.Equal(F1Status.Pass, board.F1.Status);
        Assert.Equal(BoardGeometry.StandardGeomorphic.GridWidth, board.Grid.Geometry.GridWidth);
        Assert.Equal(34, board.Grid.Stairways.ToArray().Count(stairway => stairway));
        Assert.All(board.Grid.Elevations.ToArray(), elevation => Assert.Equal(0, elevation));
        Assert.Equal(
            ["Open Ground", "Stone Building", "Stone Building, 1 Level", "Stone Building, 2 Level", "Wooden Building",
                "Wooden Building, 1 Level", "Woods", "Dirt Road", "Paved Road"],
            board.Grid.DistinctCodes().Select(code => Catalog()[code].Name));
    }

    [VaslFact]
    public void Board01ProvenanceMatchesThePinnedEvidence()
    {
        var provenance = Import("01").Provenance;

        // LOSData is binary, so the bytes read are the committed blob.
        Assert.Equal(Board01LosDataBlob, provenance.LosData.ContentBlob);
        Assert.Equal(Board01LosDataBlob, provenance.LosData.IndexBlob);

        // BoardMetadata.xml may be checked out with CRLF; the index still records the committed blob.
        Assert.Equal(Board01MetadataBlob, provenance.Metadata.IndexBlob);
        Assert.Equal("boards/src/bd01/BoardMetadata.xml", provenance.Metadata.RepositoryPath);
        Assert.Matches("^[0-9a-f]{40}$", provenance.VaslCommit ?? string.Empty);
        Assert.NotNull(provenance.SharedBoardMetadata.IndexBlob);
        Assert.Equal(VaslBoardImporter.ImporterVersion, provenance.ImporterVersion);
        Assert.Equal("vasl:bd01@" + Board01LosDataBlob, Import("01").SourceIdentity);
    }

    [VaslFact]
    public void All63Board01BuildingOverridesAgreeWithTheGrid()
    {
        var board = Import("01");
        Assert.Equal(63, board.BuildingOverrides.Count);
        Assert.All(board.BuildingOverrides, check => Assert.True(check.Consistent, $"{check.Hex}: {check.Expected} vs {check.Actual}"));

        var e4 = board.BuildingOverrides.Single(check => check.Hex.ToString() == "E4");
        Assert.Equal("Stone Building, 2 Level", e4.Actual);
        Assert.Equal(CenterTerrainSource.CenterSample, e4.Source);
        Assert.Equal(43, board.BuildingOverrides.Count(check => check.Expected == "Stone Building, 2 Level"));
        Assert.Equal(12, board.BuildingOverrides.Count(check => check.Expected == "Stone Building, 1 Level"));
        Assert.Equal(8, board.BuildingOverrides.Count(check => check.Expected == "Wooden Building, 1 Level"));
    }

    [VaslFact]
    public void Board01ArchiveAndSourceDirectoryAgree()
    {
        var vasl = Vasl();
        var directory = VaslBoardSource.SourceDirectory(vasl, "01");
        var archive = VaslBoardSource.Archive(vasl, "01");
        Assert.Empty(VaslBoardComparison.Compare(directory, archive));

        var fromArchive = VaslBoardImporter.Import(vasl, archive, Catalog());
        var archiveBoard = Assert.IsType<IngestedBoard>(fromArchive.Board);
        Assert.Equal(F1Status.Pass, archiveBoard.F1.Status);
        Assert.True(archiveBoard.Grid.Codes.SequenceEqual(Import("01").Grid.Codes));
        Assert.Equal(Board01LosDataBlob, archiveBoard.Provenance.LosData.ContentBlob);
        Assert.Equal("boards/bdFiles/bd01!LOSData", archiveBoard.Provenance.LosData.RepositoryPath);
        Assert.NotNull(archiveBoard.Provenance.Archive);
    }

    [VaslFact]
    public void MalformedAndNonStandardBoardsAreReportedNotIngested()
    {
        var vasl = Vasl();
        var malformed = VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, "79"), Catalog());
        Assert.Null(malformed.Board);
        Assert.Contains(malformed.Diagnostics, diagnostic => diagnostic.Code == "VASL-META-000");

        var missing = VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, "NoSuchBoard"), Catalog());
        Assert.Contains(missing.Diagnostics, diagnostic => diagnostic.Code == "VASL-SRC-003");
    }

    [VaslFact]
    public void EveryIngestibleBoardPassesF1AndEveryRefusalIsExplained()
    {
        var vasl = Vasl();
        var catalog = Catalog();
        var ingested = 0;
        foreach (var name in vasl.BoardNames())
        {
            var result = VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, name), catalog);
            if (result.Board is { } board)
            {
                ingested++;
                Assert.True(board.F1.Passed, $"bd{name}: {board.F1.Detail}");
            }
            else
            {
                Assert.True(
                    result.OutOfScope || result.Diagnostics.Any(diagnostic => diagnostic.Severity == MapDiagnosticSeverity.Error),
                    $"bd{name} was neither ingested nor explained.");
            }
        }

        // The pinned checkout ingests 235 boards in VASL's geomorphic layout; allow for a newer checkout.
        Assert.True(ingested >= 230, $"Only {ingested} boards were ingested.");
    }

    [VaslFact]
    public void ScopeIsDecidedFromMetadataAndAgreesWithImport()
    {
        var vasl = Vasl();
        var catalog = Catalog();
        Assert.Equal(BoardScope.InScope, Scope(vasl, "01").Scope);
        Assert.Equal(BoardScope.Unreadable, Scope(vasl, "79").Scope);
        Assert.Equal(BoardScope.InScope, Scope(vasl, "1a").Scope);
        Assert.Equal(BoardScope.InScope, Scope(vasl, "BFPDW1b").Scope);
        var hasl = Scope(vasl, "RO");
        Assert.Equal(BoardScope.OutOfScope, hasl.Scope);
        Assert.Contains("HASL map", hasl.Reason, StringComparison.Ordinal);

        // Every board the scope check declines, import declines the same way; every in-scope board either imports or fails with an error.
        var inScope = 0;
        foreach (var name in vasl.BoardNames())
        {
            var scope = Scope(vasl, name);
            var import = VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, name), catalog);
            if (scope.Scope == BoardScope.OutOfScope)
            {
                Assert.True(import.OutOfScope, $"bd{name} is out of scope but import did not say so.");
            }
            else if (scope.Scope == BoardScope.InScope)
            {
                inScope++;
                Assert.False(import.OutOfScope, $"bd{name} is in scope but import declined it.");
            }
        }

        // The pinned checkout has 235 in-scope boards in VASL's geomorphic layout; bd79 is unreadable.
        Assert.True(inScope >= 230, $"Only {inScope} boards are in scope.");
    }

    private static BoardScopeResult Scope(VaslSource vasl, string boardName) =>
        VaslBoardImporter.CheckScope(VaslBoardSource.SourceDirectory(vasl, boardName));

    private static IngestedBoard Import(string boardName)
    {
        var vasl = Vasl();
        var result = VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, boardName), Catalog());
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == MapDiagnosticSeverity.Error);
        return Assert.IsType<IngestedBoard>(result.Board);
    }

    private static VaslSource Vasl() => Assert.IsType<VaslSource>(VaslSource.FromEnvironment());

    private static TerrainCatalog Catalog() => Assert.IsType<TerrainCatalog>(Vasl().ReadTerrainCatalog().Catalog);
}
