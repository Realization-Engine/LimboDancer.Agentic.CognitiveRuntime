using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed class BoardFootprintSetupTests
{
    private static readonly string[] Boards = ["bd01"];

    [Fact]
    public async Task ExistingSetupValidationRefusesExteriorGuardWithoutChangingTerrain()
    {
        var source = Board01Fixture.Handle();
        var restricted = new BoardHandle(source.Ref, source.Version, source.Status, source.Provenance, source.Facts,
            source.VaslSource, [HexName.Parse("E4")])
        {
            Los = source.Los,
        };
        var vocabulary = UnitVocabulary.Asl();
        var catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, vocabulary)!.Catalog!;
        var root = Path.Combine(Path.GetTempPath(), "asl-footprint-" + Guid.NewGuid().ToString("N"));
        try
        {
            var planner = new GamePlanner(new FileGameStore(root), new InMemoryBoardCatalog([restricted]), vocabulary, [catalog]);
            var tenant = Guid.Parse("733a2f93-29f7-4c1a-80f5-b60d70f8c6f2");
            JsonElement Arguments(string hex) => JsonSerializer.SerializeToElement(new
            {
                gameId = "footprint",
                attemptId = "setup",
                expectedRevision = 0,
                start = new
                {
                    label = "Footprint test",
                    catalog = catalog.Identity.ToString(),
                    boards = Boards,
                    firstSide = "german",
                    sides = new[]
                    {
                        new { id = "german", nationality = "german" },
                        new { id = "russian", nationality = "russian" },
                    },
                },
                placements = new[]
                {
                    new
                    {
                        id = "squad",
                        kind = "asl:squad",
                        definition = "attacker-squad",
                        side = "german",
                        position = new { at = "bd01:" + hex + ":0" },
                        conditions = new Dictionary<string, bool>(),
                    },
                },
            });
            var accepted = await planner.PlanAsync(GameActions.Setup, Arguments("E4"), tenant, "test");
            Assert.True(accepted.CanCommit, string.Join("; ", accepted.Reasons));
            var refused = await planner.PlanAsync(GameActions.Setup, Arguments("F1"), tenant, "test");
            Assert.False(refused.CanCommit);
            Assert.Contains(refused.Reasons, reason => reason.Contains("F1", StringComparison.Ordinal));
            Assert.NotNull(source.Resolve(BoardLocation.Parse("bd01:F1:0")).Read);
            Assert.Same(source.Facts, restricted.Facts);
            Assert.False(planner.Replay(accepted.Events).HasErrors);
            var unrestricted = new GamePlanner(new FileGameStore(root), new InMemoryBoardCatalog([source]), vocabulary, [catalog]);
            var legacy = await unrestricted.PlanAsync(GameActions.Setup, Arguments("F1"), tenant, "test");
            Assert.True(legacy.CanCommit, string.Join("; ", legacy.Reasons));
            Assert.True(planner.Replay(legacy.Events).HasErrors);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
