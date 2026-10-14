using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Campaign.PilotHost;

public sealed record HandoffUnit(string Id, string Label, string? AslId, string Status, string Reason);
public sealed record PilotHandoff(string Version, string SourceHash, string Catalog, string GameRecord, HandoffUnit[] Units);

// A prepared setup artifact, not a write to Map Studio's live game store or an advance into combat.
public static class PilotHandoffs
{
    public const string Version = "campaign-asl-handoff-2";
    private static readonly Guid Tenant = Guid.Parse("bb17e94d-c54b-4260-a193-26ad215eebc9");
    public static async Task<PilotHandoff> Prepare(PilotDeployment d, DeploymentContext context, PilotSetup setup)
    {
        d.Validate(context, setup);
        if (!setup.Complete) throw new InvalidDataException("Complete campaign setup before preparing the ASL handoff.");
        var vocabulary = UnitVocabulary.Asl();
        var catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, vocabulary)?.Catalog
            ?? throw new InvalidDataException("ASL catalog is unavailable.");
        var hash = BoardPackage.Hash(CanonicalJson.Serialize(new object[] { Version, context.CampaignId, JsonSerializer.Serialize(setup), d.Bundle.PackageVersion, catalog.Identity.ToString() }));
        var boards = new InMemoryBoardCatalog([new BoardHandle(d.Package.Board, d.Package.Version,
            BoardReadStatus.AuthoredValid, "Validated campaign-generated terrain", d.Facts)]);
        var planner = new GamePlanner(new PreparationStore(), boards, vocabulary, [catalog], new PreparationClock());
        var rows = d.Units.Select(u => new HandoffUnit(u.Id, u.Label, u.Definition is null ? null : AslId(u.Id), u.Definition is null ? "Needs data" : "Ready",
            u.Definition is null ? "Weapon definition unresolved; retained in campaign setup, omitted from ASL setup."
                : u.CatalogStatus + (u.Holding is { } link ? " Operated by " + link.Holder + "." : "") )).ToArray();
        object Position(PilotUnit unit)
        {
            var at = d.Package.Board.Value + ":" + setup.Placements.Single(p => p.UnitId == unit.Id).Hex + ":0";
            return unit.Kind == "vehicle" ? new { at, facing = "west" } : (object)new { at };
        }
        var placements = d.Units.Where(u => u.Definition is not null).Select(u => new
        {
            id = AslId(u.Id), kind = catalog.Definition(u.Definition!.Definition)!.Kind, definition = u.Definition!.Definition,
            side = u.Side.ToLowerInvariant(), position = Position(u),
            holding = u.Holding is { } link ? new { holder = AslId(link.Holder), role = "manned" } : null,
            conditions = new Dictionary<string, bool> { [Conditions.Broken] = false, [Conditions.Berserk] = false,
                [Conditions.Captured] = false, [Conditions.Melee] = false, ["asl:ti"] = false,
                [Conditions.Disrupted] = false, [Conditions.Concealed] = false, [Conditions.Hidden] = false }
        }).ToArray();
        var arguments = JsonSerializer.SerializeToElement(new
        {
            gameId = "campaign-" + hash[..24], attemptId = "setup-" + hash[..24], expectedRevision = 0,
            start = new { label = "Campaign setup handoff", catalog = catalog.Identity.ToString(), boards = new[] { d.Package.Board.Value },
                firstSide = "allied", sides = new[] { new { id = "allied", nationality = "british" }, new { id = "german", nationality = "german" } } },
            placements
        });
        var plan = await planner.PlanAsync(GameActions.Setup, arguments, Tenant, "campaign-defaults");
        if (!plan.CanCommit) throw new InvalidDataException("ASL setup validation: " + string.Join("; ", plan.Reasons));
        var history = planner.Replay(plan.Events);
        if (history.Current is not { } state || state.Units.Count + state.Equipment.Count != placements.Length)
            throw new InvalidDataException("ASL setup replay failed: " + string.Join("; ", history.Diagnostics.Select(x => x.Message)));
        var record = GameEventWriter.Write(plan.Scope, new GameRecord(plan.Label, false, plan.Events));
        return new(Version, hash, catalog.Identity.ToString(), record, rows);
    }

    public static string AslId(string campaignId) => campaignId.Replace("/", "--", StringComparison.Ordinal);

    public static async Task Validate(PilotDeployment d, DeploymentContext context, PilotSetup setup, PilotHandoff saved)
    {
        var expected = await Prepare(d, context, setup);
        if (saved.Version != Version || saved.SourceHash != expected.SourceHash || saved.Catalog != expected.Catalog ||
            saved.GameRecord != expected.GameRecord || saved.Units is null || !saved.Units.SequenceEqual(expected.Units))
            throw new InvalidDataException("Saved ASL handoff differs from this deployment or catalog. Prepare it again; campaign terrain and deployment are retained.");
    }

    private sealed class PreparationClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch;
    }
    private sealed class PreparationStore : IGameStore
    {
        public GameRecord? Read(GameScope scope) => null;
        public IReadOnlyList<GameScope> List(Guid tenant) => [];
        public AppendResult Append(GameScope scope, string label, long expectedRevision, IReadOnlyList<GameEvent> events, Func<IReadOnlyList<GameEvent>, GameHistory> replay) => throw new NotSupportedException();
        public AppendResult AppendRolled(GameScope scope, string label, long expectedRevision, string firstEventId, PlannedRoll roll, DiceRoller roller, Func<IReadOnlyList<GameEvent>, GameHistory> replay) => throw new NotSupportedException();
    }
}
