using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Features;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class SituationMapTests
{
    [Fact]
    public void LazyOpenLoadsOnlyRequestedTilesAndRechecksEvictedContent()
    {
        var inputs = new PilotInputs(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")))
            .ForVersion(PilotInputs.SituationGeneratorVersion);
        const string seed = "00112233445566778899aabbccddeeff";
        var root = Path.Combine(Path.GetTempPath(), "situation-lazy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tiles = SituationCoverage.Plan(inputs);
            // Only two packages exist. The complete descriptor set proves metadata validation does not load them all.
            var descriptors = tiles.Select(tile => new SituationPackage(tile.Key, tile.Crop,
                PilotIdentity.Reference(inputs, seed, tile.Crop), new string('a', 64), new string('b', 64),
                new string('c', 64), tile.Cells.Count)).ToArray();
            for (var i = 0; i < 2; i++)
            {
                var result = new PilotGenerator(inputs).Generate(seed, tiles[i].Crop);
                PackagePublisher.Publish(result, inputs, Path.Combine(root, "packages"));
                descriptors[i] = descriptors[i] with { PackageVersion = result.Package.Version,
                    GridSha256 = PilotGenerator.GridHash(result.Grid), RefinementSha256 = BoardPackage.Hash(result.Manifest) };
            }
            var manifest = new SituationManifest(SituationCoverage.Version, inputs.Version, seed,
                PilotIdentity.World(inputs, seed), descriptors);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest);
            var path = Path.Combine(root, "situation-" + BoardPackage.Hash(bytes) + ".json");
            File.WriteAllBytes(path, bytes);
            var map = new SituationMap(inputs, path, 1);
            Assert.Equal(0, map.CachedTiles);
            var first = map.Tile(tiles[0].Key);
            Assert.Same(first, map.Tile(tiles[0].Key));
            Assert.Equal(descriptors[0].GridSha256, PilotGenerator.GridHash(first.Grid));
            Assert.NotEmpty(first.Facts.Hexes);
            Assert.Equal(1, map.CachedTiles);
            Assert.Throws<OperationCanceledException>(() => map.Tile(tiles[1].Key, new CancellationToken(true)));
            Assert.Same(first, map.Tile(tiles[0].Key));
            _ = map.Tile(tiles[1].Key);
            Assert.Equal(1, map.CachedTiles);
            // Mutating an exported manifest does not alter the live map's trusted descriptor set.
            map.Manifest.Packages[0] = descriptors[1];
            var refinement = Path.Combine(root, "packages", descriptors[0].PackageVersion, "refinement.json");
            File.AppendAllText(refinement, " ");
            Assert.Throws<InvalidDataException>(() => map.Tile(tiles[0].Key));
            Assert.Throws<InvalidDataException>(() => map.Tile(tiles[2].Key));
            Assert.Throws<InvalidDataException>(() => map.Tile("../outside"));
            Assert.Equal(1, map.CachedTiles);
            Assert.Throws<InvalidDataException>(() => SituationPublisher.Verify(inputs, path));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
