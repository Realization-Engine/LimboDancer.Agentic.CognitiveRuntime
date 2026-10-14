using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Features;

namespace LimboDancer.Campaign.Refinement;

public sealed record SituationPackage(string Tile, Crop Crop, string Board, string PackageVersion, string GridSha256, string RefinementSha256, int OwnedCells);
public sealed record SituationManifest(string CoverageVersion, string GeneratorVersion, string Seed, string WorldIdentity, SituationPackage[] Packages);

/// <summary>Packages are immutable; the complete-set manifest is published only after every package verifies.</summary>
public static class SituationPublisher
{
    public static string Publish(PilotInputs source, string seed, string outputRoot,
        Action<int, int>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        SeedRandom.ValidateSeed(seed);
        cancellationToken.ThrowIfCancellationRequested();
        var inputs = source.ForVersion(PilotInputs.SituationGeneratorVersion);
        var tiles = SituationCoverage.Plan(inputs, cancellationToken);
        var root = Path.GetFullPath(outputRoot);
        var packages = new List<SituationPackage>();
        foreach (var tile in tiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new PilotGenerator(inputs).Generate(seed, tile.Crop, cancellationToken);
            PackagePublisher.Publish(result, inputs, Path.Combine(root, "packages"), cancellationToken);
            packages.Add(new(tile.Key, tile.Crop, result.Package.Board.Value, result.Package.Version,
                PilotGenerator.GridHash(result.Grid), BoardPackage.Hash(result.Manifest), tile.Cells.Count));
            progress?.Invoke(packages.Count, tiles.Count);
        }
        var manifest = new SituationManifest(SituationCoverage.Version, inputs.Version, seed,
            PilotIdentity.World(inputs, seed), packages.ToArray());
        var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest);
        var path = Path.Combine(root, "situation-" + BoardPackage.Hash(bytes) + ".json");
        var stage = path + ".stage-" + Guid.NewGuid().ToString("N");
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            File.WriteAllBytes(stage, bytes);
            cancellationToken.ThrowIfCancellationRequested();
            try { File.Move(stage, path); }
            catch (IOException) when (File.Exists(path))
            {
                if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes))
                    throw new InvalidDataException("Existing Situation manifest differs from its identity.");
            }
            return path;
        }
        finally { if (File.Exists(stage)) File.Delete(stage); }
    }

    // Reopen accepted packages without rerunning procedural generation. Recompile to verify their terrain.
    public static SituationManifest Verify(PilotInputs source, string manifestPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (new FileInfo(manifestPath).Length > 1_000_000) throw new InvalidDataException("Oversized Situation manifest.");
        var bytes = File.ReadAllBytes(manifestPath);
        if (bytes.Length > 1_000_000 || Path.GetFileName(manifestPath) != "situation-" + BoardPackage.Hash(bytes) + ".json")
            throw new InvalidDataException("Invalid Situation manifest identity.");
        var manifest = JsonSerializer.Deserialize<SituationManifest>(bytes)
            ?? throw new InvalidDataException("Missing Situation manifest.");
        if (manifest.CoverageVersion != SituationCoverage.Version || manifest.GeneratorVersion != PilotInputs.SituationGeneratorVersion)
            throw new InvalidDataException("Unsupported Situation coverage or generator version.");
        var inputs = source.ForVersion(manifest.GeneratorVersion);
        if (manifest.WorldIdentity != PilotIdentity.World(inputs, manifest.Seed))
            throw new InvalidDataException("Situation world identity differs from its pinned inputs.");
        var tiles = SituationCoverage.Plan(inputs, cancellationToken);
        if (manifest.Packages is null || tiles.Count != manifest.Packages.Length)
            throw new InvalidDataException("Incomplete Situation package coverage.");
        var root = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        for (var i = 0; i < tiles.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tile = tiles[i]; var package = manifest.Packages[i];
            if (package is null || package.Tile != tile.Key || package.Crop != tile.Crop || package.OwnedCells != tile.Cells.Count ||
                package.Board != PilotIdentity.Reference(inputs, manifest.Seed, tile.Crop) ||
                !IsHash(package.PackageVersion) || !IsHash(package.GridSha256) || !IsHash(package.RefinementSha256))
                throw new InvalidDataException("Invalid Situation tile identity or ownership.");
            var directory = Path.Combine(root, "packages", package.PackageVersion);
            var loaded = new BoardPackageStore(Path.Combine(directory, "boards")).Load(BoardRef.Parse(package.Board));
            if (loaded.Package?.Version != package.PackageVersion ||
                BoardPackage.Hash(File.ReadAllBytes(Path.Combine(directory, "refinement.json"))) != package.RefinementSha256 ||
                !File.ReadAllBytes(Path.Combine(directory, "SharedBoardMetadata.xml")).AsSpan().SequenceEqual(inputs.CatalogBytes))
                throw new InvalidDataException("Stored Situation terrain is corrupt or missing.");
            var grid = FeatureCompiler.Compile(loaded.Package.Model, inputs.Catalog).Grid;
            if (PilotGenerator.GridHash(grid) != package.GridSha256)
                throw new InvalidDataException("Stored Situation grid differs from the accepted map.");
        }
        return manifest;
    }

    private static bool IsHash(string? value) => value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
