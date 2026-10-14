using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Maps.Vasl;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Uses an explicitly pinned catalog for authored boards; VASL ingestion keeps its own catalog.</summary>
public sealed class AuthoredCatalogSource(StudioOptions options, VaslBoardProvider vasl) : ICatalogSource
{
    private readonly Lazy<(TerrainCatalog Catalog, string Hash)?> pinned = new(() =>
    {
        if (string.IsNullOrWhiteSpace(options.AuthoredCatalogPath))
        {
            return null;
        }
        var bytes = File.ReadAllBytes(options.AuthoredCatalogPath);
        using var stream = new MemoryStream(bytes);
        var catalog = SharedBoardMetadataParser.Parse(stream).Catalog
            ?? throw new InvalidDataException("The configured authored terrain catalog is invalid.");
        return (catalog, GitBlob.Sha(bytes));
    });

    public (TerrainCatalog Catalog, string Hash)? Catalog() => pinned.Value ?? vasl.Catalog();
}
