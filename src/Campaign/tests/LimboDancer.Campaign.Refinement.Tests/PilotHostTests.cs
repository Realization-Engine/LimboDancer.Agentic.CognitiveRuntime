using System.Text.Json;
using LimboDancer.Campaign.PilotHost;
using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class PilotHostTests
{
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
            Assert.Throws<InvalidDataException>(() => fresh.Status(seed)); // No generation job was created by import.
            await Assert.ThrowsAsync<InvalidDataException>(() => fresh.Open(new(seed, bundle, context with { CampaignId = "different" }, setup)));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
