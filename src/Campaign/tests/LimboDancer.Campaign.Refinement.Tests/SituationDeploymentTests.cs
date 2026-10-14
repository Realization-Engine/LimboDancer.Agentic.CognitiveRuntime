using System.Text.Json;
using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class SituationDeploymentTests
{
    [Fact]
    public void CompleteGroundRosterAssemblesDeterministicallyAndRetainsPositionsOnReview()
    {
        var inputs = new PilotInputs(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
        const string seed = "00112233445566778899aabbccddeeff";
        var root = Path.Combine(Path.GetTempPath(), "situation-deployment-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = SituationPublisher.Publish(inputs, seed, root);
            var map = new SituationMap(inputs, path);
            var service = new SituationDeployment(inputs, map);
            var roster = SituationRoster.Create(inputs);
            // A full, dispersed formation deployment, with unique open parent hexes for both sides.
            var open = inputs.Regions.Where(r => r.Kind == "open").OrderBy(r => r.Key, StringComparer.Ordinal).ToArray();
            var formations = roster.Ground.Select((f, i) => new FormationSelection(f.Id, open[i].Key)).ToArray();
            var draft = service.Assemble(formations);
            Assert.True(draft.Ready, string.Join("; ", draft.Review.Select(r => r.UnitId + ": " + r.Reason)));
            Assert.Equal(624, draft.Positions.Length);
            Assert.Equal(624, draft.Positions.Select(p => p.UnitId).Distinct().Count());
            var addresses = new SituationAddresses(inputs, seed);
            Assert.All(draft.Positions, p => Assert.Equal(formations.Single(f => f.UnitId ==
                roster.Ground.Single(f => f.Units.Any(u => u.Id == p.UnitId)).Id).Anchor, addresses.At(p.Column, p.Row).Parent));
            var json = JsonSerializer.Serialize(draft);
            var reloaded = JsonSerializer.Deserialize<SituationDeploymentDraft>(json)!;
            Assert.Equal(json, JsonSerializer.Serialize(service.Review(reloaded)));
            Assert.Equal(json, JsonSerializer.Serialize(service.Assemble(formations.Reverse())));
            Assert.Equal(json, JsonSerializer.Serialize(service.Assemble(formations, reloaded.Positions)));
            foreach (var weapon in roster.Ground.SelectMany(f => f.Units).Where(u => u.Holding is not null))
            {
                var p = draft.Positions.Single(p => p.UnitId == weapon.Id);
                var crew = draft.Positions.Single(p => p.UnitId == weapon.Holding!.Holder);
                Assert.Equal((crew.Column, crew.Row), (p.Column, p.Row));
            }
            // An anchor change produces review, never an automatic move of existing squads.
            var changed = formations.ToArray();
            changed[0] = changed[0] with { Anchor = open[200].Key };
            var review = service.Assemble(changed, reloaded.Positions);
            Assert.False(review.Ready);
            Assert.Equal(draft.Positions, review.Positions);
            Assert.Contains(review.Review, r => r.Reason.Contains("parent formation", StringComparison.Ordinal));
            Assert.Throws<InvalidDataException>(() => service.Review(draft with { Policy = "future" }));
            Assert.Throws<InvalidDataException>(() => service.Assemble(formations, [draft.Positions[0], draft.Positions[0]]));
            Assert.Throws<OperationCanceledException>(() => service.Assemble(formations, cancellationToken: new CancellationToken(true)));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
