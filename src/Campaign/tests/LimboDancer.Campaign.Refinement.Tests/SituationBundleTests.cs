using System.Text.Json;
using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class SituationBundleTests
{
    [Fact]
    public void PortableTerrainRestoresExactAcceptedMapAndRejectsChangedOrIncompleteSaves()
    {
        var inputs = new PilotInputs(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
        var root = Path.Combine(Path.GetTempPath(), "situation-portable-" + Guid.NewGuid().ToString("N"));
        try
        {
            var source = SituationPublisher.Publish(inputs, "00112233445566778899aabbccddeeff", Path.Combine(root, "source"));
            var bundle = SituationBundles.Export(inputs, source);
            var json = JsonSerializer.Serialize(bundle);
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(json) < 50_000_000);
            var saved = JsonSerializer.Deserialize<SituationBundle>(json)!;
            var target = Path.Combine(root, "restored");
            var path = SituationBundles.Restore(inputs, saved, target);
            Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(path));
            Assert.Equal(json, JsonSerializer.Serialize(SituationBundles.Export(inputs, path)));
            Assert.Equal(path, SituationBundles.Restore(inputs, saved, target));
            var rejected = Path.Combine(root, "rejected");
            Assert.Throws<InvalidDataException>(() => SituationBundles.Restore(inputs, saved with { Version = "future" }, rejected));
            Assert.Throws<InvalidDataException>(() => SituationBundles.Restore(inputs, saved with { Catalog = "changed" }, rejected));
            Assert.Throws<InvalidDataException>(() => SituationBundles.Restore(inputs, saved with { Tiles = saved.Tiles[..^1] }, rejected));
            var altered = saved.Tiles.ToArray();
            altered[0] = altered[0] with { Refinement = altered[0].Refinement + " " };
            Assert.Throws<InvalidDataException>(() => SituationBundles.Restore(inputs, saved with { Tiles = altered }, rejected));
            altered = saved.Tiles.ToArray();
            altered[0] = altered[0] with { Name = "Changed board" };
            Assert.Throws<InvalidDataException>(() => SituationBundles.Restore(inputs, saved with { Tiles = altered }, rejected));
            altered = saved.Tiles.ToArray(); altered[0] = altered[1];
            Assert.Throws<InvalidDataException>(() => SituationBundles.Restore(inputs, saved with { Tiles = altered }, rejected));
            Assert.Throws<OperationCanceledException>(() => SituationBundles.Restore(inputs, saved, rejected, new CancellationToken(true)));
            Assert.Empty(Directory.EnumerateFileSystemEntries(rejected));
            Assert.Equal(json, JsonSerializer.Serialize(saved));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
