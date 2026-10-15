using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Campaign.PilotHost;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class SituationSessionTests
{
    [Fact]
    public async Task WholeSituationJobsPersistReopenAndTransferWithoutRelocatingCounters()
    {
        var inputs = new PilotInputs(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
        var root = Path.Combine(Path.GetTempPath(), "situation-session-" + Guid.NewGuid().ToString("N"));
        var firstRoot = Path.Combine(root, "first");
        const string seed = "00112233445566778899aabbccddeeff";
        var open = inputs.Regions.Where(r => r.Kind == "open").OrderBy(r => r.Key, StringComparer.Ordinal).ToArray();
        var formations = SituationRoster.Create(inputs).Ground.Select((f, i) => new FormationSelection(f.Id, open[i].Key)).ToArray();
        var request = new SituationStartRequest("campaign-test", seed, formations, "{\"supply\":\"accepted\"}");
        try
        {
            string id;
            SituationSave original;
            await using (var service = new SituationSessions(inputs, firstRoot))
            {
                Assert.Throws<InvalidDataException>(() => service.Read("../outside"));
                id = await Ready(service, service.Start(request));
                original = service.Read(id);
                Assert.True(original.Setup.Ready, string.Join("; ", original.Setup.Review.Select(r => r.Reason)));
                Assert.Equal(624, original.Setup.Positions.Length);
                Assert.Equal(8, original.Roster.AirSupport.Aircraft.Length);
                Assert.Equal(request.CampaignContext, original.CampaignContext);
            }
            await using var restarted = new SituationSessions(inputs, firstRoot);
            Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(restarted.Read(id)));
            Assert.Equal(id, await Ready(restarted, restarted.Start(request with { Saved = id })));
            Assert.Throws<InvalidDataException>(() => restarted.Start(request with { Saved = id, CampaignId = "other" }));
            var changed = formations.ToArray(); changed[0] = changed[0] with { Anchor = open[200].Key };
            var reviewId = await Ready(restarted, restarted.Start(request with { Saved = id, Formations = changed }));
            var reviewed = restarted.Read(reviewId);
            Assert.False(reviewed.Setup.Ready);
            Assert.Equal(original.Setup.Positions, reviewed.Setup.Positions);
            Assert.Equal(reviewId, await Ready(restarted, restarted.PlaceRemaining(reviewId)));
            Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(restarted.Read(id)));
            var portable = restarted.Export(id);
            await using var destination = new SituationSessions(inputs, Path.Combine(root, "second"));
            Assert.Throws<InvalidDataException>(() => destination.Import(portable with { Save = portable.Save with { Version = "future" } }));
            // Explicitly fill a missing crew/weapon group, without touching other positions or the old save.
            var missingIds = new HashSet<string> { "de-81mm-01/mortar-1", "de-81mm-01/crew-2", "de-81mm-01/mortar-2" };
            var incomplete = portable with { Save = portable.Save with { Setup = portable.Save.Setup with {
                Positions = portable.Save.Setup.Positions.Where(p => !missingIds.Contains(p.UnitId)).ToArray() } } };
            var gapId = await Ready(destination, destination.Import(incomplete));
            var gap = destination.Read(gapId);
            Assert.False(gap.Setup.Ready);
            Assert.Equal(621, gap.Setup.Positions.Length);
            var filledId = await Ready(destination, destination.PlaceRemaining(gapId));
            var filled = destination.Read(filledId);
            Assert.True(filled.Setup.Ready);
            Assert.Equal(624, filled.Setup.Positions.Length);
            Assert.Equal(gap.TerrainManifest, filled.TerrainManifest);
            Assert.Equal(JsonSerializer.Serialize(gap.Roster), JsonSerializer.Serialize(filled.Roster));
            Assert.Equal(gap.CampaignContext, filled.CampaignContext);
            Assert.All(gap.Setup.Positions, p => Assert.Contains(p, filled.Setup.Positions));
            Assert.Equal(621, destination.Read(gapId).Setup.Positions.Length);
            Assert.Equal(filledId, await Ready(destination, destination.PlaceRemaining(filledId)));
            var restoredId = await Ready(destination, destination.Import(portable));
            var restored = destination.Read(restoredId);
            Assert.Equal(JsonSerializer.Serialize(original.Setup), JsonSerializer.Serialize(restored.Setup));
            Assert.Equal(JsonSerializer.Serialize(original.Roster), JsonSerializer.Serialize(restored.Roster));
            Assert.Equal(original.CampaignContext, restored.CampaignContext);
            var prepared = await Ready(restarted, restarted.Prepare(id));
            var handoff = restarted.TransferHandoff(id, prepared);
            Assert.Throws<InvalidDataException>(() => restarted.TransferHandoff(reviewId, prepared));
            var withProgress = portable with { Progress = new("goodwood-asl-progress-1", handoff, handoff.GameRecord) };
            Assert.Throws<InvalidDataException>(() => destination.Import(withProgress));
            Assert.Throws<InvalidDataException>(() => destination.Import(withProgress with { Progress = withProgress.Progress with { Version = "future" } }, (_, _, _) => Task.FromResult("unused")));
            var invoked = 0;
            var result = await Ready(destination, destination.Import(withProgress, (accepted, record, token) =>
            {
                token.ThrowIfCancellationRequested();
                Assert.Equal(JsonSerializer.Serialize(handoff), JsonSerializer.Serialize(accepted));
                Assert.Equal(handoff.GameRecord, record);
                invoked++;
                return Task.FromResult("http://127.0.0.1:5178/games/play?game=campaign-" + accepted.SourceHash[..24]);
            }));
            using var metadata = JsonDocument.Parse(result);
            Assert.Equal(1, invoked);
            Assert.Equal(restoredId, metadata.RootElement.GetProperty("save").GetString());
            Assert.Equal(prepared, metadata.RootElement.GetProperty("prepared").GetString());
            Assert.Equal(JsonSerializer.Serialize(handoff), JsonSerializer.Serialize(destination.ReadHandoff(prepared)));
            var mismatch = withProgress with { Progress = withProgress.Progress with { Handoff = handoff with { CampaignId = "wrong" } } };
            var rejected = await Finished(destination, destination.Import(mismatch, (_, _, _) => throw new InvalidOperationException("Must reject before publication")));
            Assert.Equal("failed", rejected.Status);
            Assert.Contains("different terrain or deployment", rejected.Error);
            Assert.Equal(JsonSerializer.Serialize(restored), JsonSerializer.Serialize(destination.Read(restoredId)));
            var saveCount = Directory.GetFiles(Path.Combine(root, "second", "saves"), "*.json").Length;
            var cancelled = destination.Start(request with { CampaignId = "cancelled", Seed = "ffeeddccbbaa99887766554433221100" });
            destination.Cancel(cancelled);
            var terminal = await Finished(destination, cancelled);
            Assert.Equal("cancelled", terminal.Status);
            Assert.Equal(saveCount, Directory.GetFiles(Path.Combine(root, "second", "saves"), "*.json").Length);
            var path = Path.Combine(firstRoot, "saves", id + ".json");
            File.AppendAllText(path, " ");
            Assert.Throws<InvalidDataException>(() => restarted.Read(id));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static async Task<string> Ready(SituationSessions service, string job)
    {
        var status = await Finished(service, job);
        Assert.True(status.Status == "ready", status.Error ?? status.Status);
        return status.Result!;
    }

    private static async Task<SituationJobStatus> Finished(SituationSessions service, string job)
    {
        var deadline = DateTime.UtcNow.AddMinutes(10);
        while (DateTime.UtcNow < deadline)
        {
            var status = service.Status(job);
            if (status.Status != "working") return status;
            await Task.Delay(100);
        }
        throw new TimeoutException("Situation job exceeded the acceptance timeout.");
    }
}
