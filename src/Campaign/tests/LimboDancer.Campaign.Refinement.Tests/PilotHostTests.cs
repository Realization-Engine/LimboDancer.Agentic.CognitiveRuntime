using System.Text.Json;
using LimboDancer.Campaign.PilotHost;
using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class PilotHostTests
{
    [Fact]
    public async Task SelectedFormationJobAndAcceptedImportStaySeparateFromLegacyPilot()
    {
        var root = Path.Combine(Path.GetTempPath(), "selected-formation-test-" + Guid.NewGuid().ToString("N"));
        var inputs = new PilotInputs(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
        const string seed = "00112233445566778899aabbccddeeff";
        var selection = new FormationSelection("uk-rifle-02", "A/X-6");
        var context = new DeploymentContext("selected-test", "placement-snapshot", ["A/X-6"], [], selection);
        try
        {
            using var service = new PilotSessions(inputs, root);
            var job = JsonSerializer.SerializeToElement(service.Start(seed, selection)).GetProperty("id").GetString()!;
            Assert.NotEqual(seed, job);
            Assert.Equal(job, JsonSerializer.SerializeToElement(service.Start(seed, selection)).GetProperty("id").GetString());
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (true)
            {
                var status = JsonSerializer.SerializeToElement(service.Status(job));
                Assert.NotEqual("failed", status.GetProperty("status").GetString());
                if (status.GetProperty("status").GetString() == "ready") break;
                await Task.Delay(50, timeout.Token);
            }
            Assert.Throws<InvalidDataException>(() => service.Status(seed));
            var opened = JsonSerializer.SerializeToElement(await service.Open(new(seed, null, context, null)));
            Assert.Equal(4, opened.GetProperty("roster").GetArrayLength());
            var setup = opened.GetProperty("setup").Deserialize<PilotSetup>()!;
            setup = service.Edit(new(opened.GetProperty("session").GetString()!, setup, "deploy", null, null));
            var bundle = opened.GetProperty("bundle").Deserialize<PilotBundle>()!;
            using var fresh = new PilotSessions(inputs, root);
            var imported = JsonSerializer.SerializeToElement(await fresh.Open(new(seed, bundle, context, setup)));
            Assert.Equal(opened.GetProperty("svg").GetString(), imported.GetProperty("svg").GetString());
            Assert.Equal(4, imported.GetProperty("setup").GetProperty("Placements").GetArrayLength());
            Assert.Throws<InvalidDataException>(() => fresh.Status(job));
            await Assert.ThrowsAsync<InvalidDataException>(() => fresh.Open(new(seed, bundle,
                context with { Formation = selection with { Anchor = "A/W-6" } }, setup)));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Fact]
    public async Task GenerateOpenEditAndImportIntoFreshServiceRetainsAcceptedPackage()
    {
        var root = Path.Combine(Path.GetTempPath(), "campaign-deployment-test-" + Guid.NewGuid().ToString("N"));
        var inputs = new PilotInputs(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
        const string seed = "00112233445566778899aabbccddeeff";
        try
        {
            using var service = new PilotSessions(inputs, root);
            service.Start(seed); service.Start(seed);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (true)
            {
                var status = JsonSerializer.SerializeToElement(service.Status(seed)).GetProperty("status").GetString();
                Assert.NotEqual("failed", status);
                if (status == "ready") break;
                await Task.Delay(50, timeout.Token);
            }
            var context = DeploymentTests.SourceContext();
            var opened = JsonSerializer.SerializeToElement(await service.Open(new(seed, null, context, null)));
            var session = opened.GetProperty("session").GetString()!;
            var setup = opened.GetProperty("setup").Deserialize<PilotSetup>()!;
            var bundle = opened.GetProperty("bundle").Deserialize<PilotBundle>()!;
            setup = service.Edit(new(session, setup, "deploy", null, null));
            setup = service.Edit(new(session, setup, "complete", null, null));
            Assert.True(setup.Complete); Assert.Equal(8, setup.Placements.Length);
            using var fresh = new PilotSessions(inputs, root);
            var resumed = JsonSerializer.SerializeToElement(await fresh.Open(new(seed, bundle, context, setup)));
            Assert.Equal(opened.GetProperty("svg").GetString(), resumed.GetProperty("svg").GetString());
            Assert.Equal(JsonSerializer.Serialize(setup), JsonSerializer.Serialize(resumed.GetProperty("setup").Deserialize<PilotSetup>()));
            await Assert.ThrowsAsync<InvalidDataException>(() => fresh.Open(new(seed, bundle,
                context with { FormationSignature = "changed" }, setup, context)));
            Assert.Throws<InvalidDataException>(() => fresh.Status(seed)); // No generation job was created by import.
            await Assert.ThrowsAsync<InvalidDataException>(() => fresh.Open(new(seed, bundle, context with { CampaignId = "different" }, setup)));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
