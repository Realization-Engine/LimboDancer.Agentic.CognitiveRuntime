using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>A <c>buildingType</c> override: the building type VASL wrote into LOSData for a hex.</summary>
public sealed record BuildingTypeOverride(HexName Hex, string BuildingTypeName);

/// <summary>Hexsides of one hex flagged by a slope, railroad embankment, or partial orchard element.</summary>
public sealed record HexsideFlags(HexName Hex, IReadOnlyList<HexsideDirection> Sides);

/// <summary>
/// The parsed <c>BoardMetadata.xml</c> of one VASL board (VASL Board Ingestion Design, section 6.1).
/// Sections outside version 1 scope are kept as raw XML in <see cref="DeferredElements"/>.
/// </summary>
public sealed record BoardMetadata
{
    public required string Name
    {
        get; init;
    }

    public required string Version
    {
        get; init;
    }

    public required string VersionDate
    {
        get; init;
    }

    public required string Author
    {
        get; init;
    }

    public required string BoardImageFileName
    {
        get; init;
    }

    public required bool HasHills
    {
        get; init;
    }

    public required int Width
    {
        get; init;
    }

    public required int Height
    {
        get; init;
    }

    /// <summary>Custom geometry attributes as written (A1CenterX, A1CenterY, hexWidth, hexHeight, altHexGrain, snapScale, HexGridConfig).</summary>
    public required IReadOnlyDictionary<string, string> GeometryAttributes
    {
        get; init;
    }

    /// <summary>Building-type overrides in document order; a later duplicate replaces an earlier one, as in VASL.</summary>
    public required IReadOnlyList<BuildingTypeOverride> BuildingTypes
    {
        get; init;
    }

    public required IReadOnlyList<HexsideFlags> Slopes
    {
        get; init;
    }

    public required IReadOnlyList<HexsideFlags> RailroadEmbankments
    {
        get; init;
    }

    public required IReadOnlyList<HexsideFlags> PartialOrchards
    {
        get; init;
    }

    /// <summary>Top-level elements deferred beyond version 1 (colors, colorSSRules, overlaySSRules), as raw XML.</summary>
    public required IReadOnlyDictionary<string, string> DeferredElements
    {
        get; init;
    }
}

public sealed record BoardMetadataResult(BoardMetadata? Metadata, IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool Succeeded => Metadata is not null;
}
