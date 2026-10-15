using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Features;

namespace LimboDancer.Campaign.Refinement;

public sealed record SituationTileBundle(string Tile, string Name, string Features, string Refinement);
public sealed record SituationBundle(string Version, string Manifest, string Catalog, SituationTileBundle[] Tiles);

/// <summary>Portable accepted terrain. Restore validates saved bytes and never calls the generator.</summary>
public static class SituationBundles
{
    public const string Version = "goodwood-situation-bundle-1";

    public static SituationBundle Export(PilotInputs inputs, string manifestPath, CancellationToken cancellationToken = default)
    {
        var map = new SituationMap(inputs, manifestPath);
        var tiles = new List<SituationTileBundle>();
        foreach (var descriptor in map.Manifest.Packages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tile = map.Tile(descriptor.Tile, cancellationToken);
            var refinement = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!,
                "packages", descriptor.PackageVersion, "refinement.json"));
            if (BoardPackage.Hash(refinement) != descriptor.RefinementSha256)
                throw new InvalidDataException("Situation refinement changed during export.");
            tiles.Add(new(descriptor.Tile, tile.Package.Name, Encoding.UTF8.GetString(tile.Package.FeaturesBytes()),
                Encoding.UTF8.GetString(refinement)));
        }
        return new(Version, File.ReadAllText(manifestPath, Encoding.UTF8), Encoding.UTF8.GetString(inputs.CatalogBytes), tiles.ToArray());
    }

    public static string Restore(PilotInputs inputs, SituationBundle bundle, string outputRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        cancellationToken.ThrowIfCancellationRequested();
        if (bundle.Version != Version || bundle.Manifest is not { Length: > 0 and <= 1_000_000 } ||
            bundle.Catalog is not { Length: > 0 and <= 1_000_000 } || bundle.Tiles is not { Length: 84 } ||
            !Encoding.UTF8.GetBytes(bundle.Catalog).AsSpan().SequenceEqual(inputs.CatalogBytes))
            throw new InvalidDataException("Unsupported or incomplete Situation terrain save.");
        if (bundle.Tiles.Any(t => t is null || t.Tile is null || t.Name is not { Length: > 0 and <= 1000 } ||
            t.Features is not { Length: > 0 and <= 2_000_000 } || t.Refinement is not { Length: > 0 and <= 2_000_000 }) ||
            bundle.Tiles.Sum(t => (long)t.Features.Length + t.Refinement.Length) > 50_000_000)
            throw new InvalidDataException("Invalid or oversized Situation terrain entries.");
        var manifestBytes = Encoding.UTF8.GetBytes(bundle.Manifest);
        var name = "situation-" + BoardPackage.Hash(manifestBytes) + ".json";
        var identity = BoardPackage.Hash(JsonSerializer.SerializeToUtf8Bytes(bundle));
        var root = Path.GetFullPath(outputRoot);
        Directory.CreateDirectory(root);
        var destination = Path.Combine(root, identity);
        var stage = Path.Combine(root, ".restore-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(stage);
            var path = Path.Combine(stage, name);
            File.WriteAllBytes(path, manifestBytes);
            // Validate every descriptor before any of its names are used as paths.
            var manifest = SituationPublisher.ReadManifest(inputs, path, cancellationToken);
            if (bundle.Tiles.Select(t => t.Tile).Distinct(StringComparer.Ordinal).Count() != manifest.Packages.Length)
                throw new InvalidDataException("Duplicate Situation terrain entries.");
            var entries = bundle.Tiles.ToDictionary(t => t.Tile, StringComparer.Ordinal);
            foreach (var descriptor in manifest.Packages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!entries.TryGetValue(descriptor.Tile, out var entry))
                    throw new InvalidDataException("Missing Situation terrain entry.");
                var refinement = Encoding.UTF8.GetBytes(entry.Refinement);
                var package = new BoardPackage(BoardRef.Parse(descriptor.Board), entry.Name,
                    FeatureModelJson.Deserialize(Encoding.UTF8.GetBytes(entry.Features)));
                if (package.Version != descriptor.PackageVersion || BoardPackage.Hash(refinement) != descriptor.RefinementSha256)
                    throw new InvalidDataException("Saved Situation terrain differs from its accepted identity.");
                var directory = Path.Combine(stage, "packages", descriptor.PackageVersion);
                Directory.CreateDirectory(directory);
                new BoardPackageStore(Path.Combine(directory, "boards")).Save(package);
                File.WriteAllBytes(Path.Combine(directory, "refinement.json"), refinement);
                File.WriteAllBytes(Path.Combine(directory, "SharedBoardMetadata.xml"), inputs.CatalogBytes);
            }
            _ = SituationPublisher.Verify(inputs, path, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            try { Directory.Move(stage, destination); }
            catch (IOException) when (Directory.Exists(destination))
            {
                // A retry must verify the already published copy, not silently replace it.
                _ = SituationPublisher.Verify(inputs, Path.Combine(destination, name), cancellationToken);
            }
            return Path.Combine(destination, name);
        }
        finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
    }
}
