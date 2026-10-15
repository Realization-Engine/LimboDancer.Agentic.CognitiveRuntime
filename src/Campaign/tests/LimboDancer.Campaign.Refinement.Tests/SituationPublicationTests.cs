using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Features;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class SituationPublicationTests
{
    private static readonly PilotInputs Inputs = new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
    private const string Seed = "00112233445566778899aabbccddeeff";

    [Fact]
    public void CancellationAfterOnePackageNeverPublishesACompleteSet()
    {
        var root = Path.Combine(Path.GetTempPath(), "situation-cancel-" + Guid.NewGuid().ToString("N"));
        using var cancellation = new CancellationTokenSource();
        try
        {
            Assert.Throws<OperationCanceledException>(() => SituationPublisher.Publish(Inputs, Seed, root,
                (done, total) => { Assert.Equal(1, done); Assert.True(total > 1); cancellation.Cancel(); }, cancellation.Token));
            Assert.Empty(Directory.GetFiles(root, "situation-*.json"));
            Assert.Single(Directory.GetDirectories(Path.Combine(root, "packages")));
            Assert.Empty(Directory.GetFiles(root, "*.stage-*"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void IncompleteAndIncompatibleSetsFailEvenWithValidContentHash()
    {
        var root = Path.Combine(Path.GetTempPath(), "situation-invalid-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var inputs = Inputs.ForVersion(PilotInputs.SituationGeneratorVersion);
            var manifest = new SituationManifest(SituationCoverage.Version, inputs.Version, Seed,
                PilotIdentity.World(inputs, Seed), []);
            string Save(SituationManifest value)
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
                var path = Path.Combine(root, "situation-" + BoardPackage.Hash(bytes) + ".json");
                File.WriteAllBytes(path, bytes);
                return path;
            }
            Assert.Contains("Incomplete", Assert.Throws<InvalidDataException>(() => SituationPublisher.Verify(Inputs, Save(manifest))).Message);
            Assert.Contains("Unsupported", Assert.Throws<InvalidDataException>(() => SituationPublisher.Verify(Inputs,
                Save(manifest with { GeneratorVersion = "future" }))).Message);
            var validName = Save(manifest);
            File.AppendAllText(validName, " ");
            Assert.Contains("identity", Assert.Throws<InvalidDataException>(() => SituationPublisher.Verify(Inputs, validName)).Message);
        }
        finally { Directory.Delete(root, true); }
    }
}
