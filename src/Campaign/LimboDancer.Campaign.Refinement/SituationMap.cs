using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Grid;

namespace LimboDancer.Campaign.Refinement;

public sealed record SituationTerrainTile(BoardPackage Package, TerrainGrid Grid, HexFactSet Facts);

/// <summary>Demand-loaded accepted terrain with a bounded LRU cache. Never regenerates a saved map.</summary>
public sealed class SituationMap
{
    private readonly PilotInputs inputs;
    private readonly string path;
    private readonly SituationManifest manifest;
    private readonly Dictionary<string, SituationPackage> packages;
    private readonly Dictionary<string, SituationTerrainTile> cache = new(StringComparer.Ordinal);
    private readonly LinkedList<string> order = new();
    private readonly object gate = new();
    private readonly int capacity;
    public SituationManifest Manifest => manifest with { Packages = [.. manifest.Packages] };
    public int CachedTiles { get { lock (gate) return cache.Count; } }

    public SituationMap(PilotInputs source, string manifestPath, int cacheCapacity = 4, CancellationToken cancellationToken = default)
    {
        if (cacheCapacity is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(cacheCapacity));
        capacity = cacheCapacity;
        path = Path.GetFullPath(manifestPath);
        manifest = SituationPublisher.ReadManifest(source, path, cancellationToken);
        inputs = source.ForVersion(manifest.GeneratorVersion);
        packages = manifest.Packages.ToDictionary(p => p.Tile, StringComparer.Ordinal);
    }

    public SituationTerrainTile Tile(string key, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!packages.TryGetValue(key, out var descriptor)) throw new InvalidDataException("Unknown Situation tile.");
            if (!cache.TryGetValue(key, out var result))
            {
                result = Load(inputs, path, descriptor, cancellationToken);
                if (cache.Count == capacity)
                {
                    cache.Remove(order.First!.Value);
                    order.RemoveFirst();
                }
                cache.Add(key, result);
            }
            order.Remove(key);
            order.AddLast(key);
            return result;
        }
    }

    internal static SituationTerrainTile Load(PilotInputs inputs, string manifestPath, SituationPackage package,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!, "packages", package.PackageVersion);
        var loaded = new BoardPackageStore(Path.Combine(directory, "boards")).Load(BoardRef.Parse(package.Board));
        if (loaded.Package?.Version != package.PackageVersion ||
            BoardPackage.Hash(File.ReadAllBytes(Path.Combine(directory, "refinement.json"))) != package.RefinementSha256 ||
            !File.ReadAllBytes(Path.Combine(directory, "SharedBoardMetadata.xml")).AsSpan().SequenceEqual(inputs.CatalogBytes))
            throw new InvalidDataException("Stored Situation terrain is corrupt or missing.");
        var grid = FeatureCompiler.Compile(loaded.Package.Model, inputs.Catalog).Grid;
        if (PilotGenerator.GridHash(grid) != package.GridSha256)
            throw new InvalidDataException("Stored Situation grid differs from the accepted map.");
        cancellationToken.ThrowIfCancellationRequested();
        var facts = VaslCompatibleHexFactDerivation.Derive(grid, inputs.Catalog, HexsideAnnotations.None);
        cancellationToken.ThrowIfCancellationRequested();
        return new(loaded.Package, grid, facts);
    }
}
