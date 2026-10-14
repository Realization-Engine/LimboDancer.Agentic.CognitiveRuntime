using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Campaign.PilotHost;
using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class RecoveryTests
{
    private const string Seed = "00112233445566778899aabbccddeeff";
    private static readonly PilotInputs Inputs = new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
    private static readonly Lazy<PilotResult> Result = new(() => new PilotGenerator(Inputs).Generate(Seed));

    [Fact]
    public void InterruptedStagingAndFailedRenameCanRetryWithoutChangingAcceptedPackage()
    {
        var root = Path.Combine(Path.GetTempPath(), "pilot-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            // An interrupted process can leave an incomplete staging folder. It is never accepted.
            var orphan = Path.Combine(root, ".stage-interrupted"); Directory.CreateDirectory(orphan);
            File.WriteAllText(Path.Combine(orphan, "refinement.json"), "{partial");
            var destination = Path.Combine(root, Result.Value.Package.Version);
            File.WriteAllText(destination, "simulate publication rename failure");
            Assert.Throws<IOException>(() => PackagePublisher.Publish(Result.Value, Inputs, root));
            Assert.False(Directory.Exists(destination));
            Assert.Equal([orphan], Directory.GetDirectories(root));
            File.Delete(destination);
            Assert.Equal(destination, PackagePublisher.Publish(Result.Value, Inputs, root));
            var accepted = File.ReadAllBytes(Path.Combine(destination, "refinement.json"));
            Assert.Equal(destination, PackagePublisher.Publish(Result.Value, Inputs, root));
            Assert.Equal(accepted, File.ReadAllBytes(Path.Combine(destination, "refinement.json")));
            Assert.Equal("{partial", File.ReadAllText(Path.Combine(orphan, "refinement.json")));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task CancelledJobCanRetrySameSeedAndPublishOnlyCompleteOutput()
    {
        var root = Path.Combine(Path.GetTempPath(), "pilot-cancel-" + Guid.NewGuid().ToString("N"));
        using var service = new PilotSessions(Inputs, root);
        try
        {
            service.Start(Seed); service.Cancel(Seed);
            Assert.Equal("cancelled", await Wait(service, Seed));
            Assert.False(Directory.Exists(root) && Directory.GetDirectories(root).Length > 0);
            service.Start(Seed);
            Assert.Equal("ready", await Wait(service, Seed));
            Assert.Single(Directory.GetDirectories(root));
            Assert.Equal(Result.Value.Package.Version, Path.GetFileName(Directory.GetDirectories(root)[0]));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("CompilerVersion")]
    [InlineData("DerivationVersion")]
    [InlineData("GeneratorVersion")]
    [InlineData("SerializationVersion")]
    public async Task RejectedImportDoesNotReplaceExistingSession(string field)
    {
        var pilot = new PilotDeployment(Inputs, Result.Value, Seed);
        var context = DeploymentTests.SourceContext(); var setup = pilot.AutoDeploy(context, pilot.Create(context));
        using var service = new PilotSessions(Inputs, Path.GetTempPath());
        var opened = JsonSerializer.SerializeToElement(await service.Open(new(Seed, pilot.Bundle, context, setup)));
        var manifest = JsonNode.Parse(pilot.Bundle.Manifest)!; manifest[field] = "unsupported-future-version";
        await Assert.ThrowsAsync<InvalidDataException>(() => service.Open(new(Seed, pilot.Bundle with { Manifest = manifest.ToJsonString() }, context, setup)));
        await Assert.ThrowsAnyAsync<JsonException>(() => service.Open(new(Seed, pilot.Bundle with { Features = "{broken" }, context, setup)));
        var retained = service.Edit(new(opened.GetProperty("session").GetString()!, setup, "complete", null, null));
        Assert.True(retained.Complete); Assert.Equal(setup.Placements, retained.Placements);
    }

    private static async Task<string> Wait(PilotSessions service, string seed)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (true)
        {
            var state = JsonSerializer.SerializeToElement(service.Status(seed)).GetProperty("status").GetString()!;
            if (state != "generating") return state;
            await Task.Delay(20, timeout.Token);
        }
    }
}
