using System.Text;
using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Campaign.PilotHost;

public sealed record SituationHandoff(string Version, string SourceHash, string CampaignId, string WorldIdentity,
    string Catalog, string Board, string Name, string BoardVersion, string Features, string GameRecord,
    SituationRosterSnapshot Roster, SituationDeploymentDraft Setup, string? CampaignContext = null);

/// <summary>Full Goodwood initial setup, validated by the existing ASL planner. Does not publish or advance play.</summary>
public static class SituationHandoffs
{
    public const string Version = "campaign-goodwood-handoff-1";
    public static string ContextJson(SituationHandoff handoff) => JsonSerializer.Serialize(new
    {
        handoff.Version, handoff.SourceHash, handoff.CampaignId, handoff.WorldIdentity,
        handoff.Catalog, handoff.Roster, handoff.Setup, handoff.CampaignContext
    });

    private static readonly Guid Tenant = Guid.Parse("bb17e94d-c54b-4260-a193-26ad215eebc9");

    public static async Task<SituationHandoff> Prepare(PilotInputs inputs, SituationMap map,
        SituationDeploymentDraft draft, string campaignId, string? campaignContext = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(campaignId) || campaignId.Length > 100)
            throw new InvalidDataException("A campaign identity is required for the complete Situation handoff.");
        var accepted = new SituationDeployment(inputs, map).Review(draft, cancellationToken);
        if (!accepted.Ready) throw new InvalidDataException("Review all Situation placement issues before ASL handoff.");
        var roster = SituationRoster.Create(inputs);
        var board = SituationAslBoard.Assemble(inputs, map, cancellationToken);
        var grid = FeatureCompiler.Compile(board.Model, inputs.Catalog).Grid;
        var facts = VaslCompatibleHexFactDerivation.Derive(grid, inputs.Catalog, HexsideAnnotations.None);
        var validation = BoardValidator.Validate(board.Model, facts, inputs.Catalog);
        if (validation.HasErrors) throw new InvalidDataException("Full Goodwood ASL board failed terrain validation.");
        cancellationToken.ThrowIfCancellationRequested();
        var vocabulary = UnitVocabulary.Asl();
        var catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, vocabulary)?.Catalog
            ?? throw new InvalidDataException("ASL catalog is unavailable.");
        var hash = BoardPackage.Hash(CanonicalJson.Serialize(new[] { Version, campaignId, accepted.WorldIdentity,
            JsonSerializer.Serialize(accepted), board.Version, catalog.Identity.ToString(), JsonSerializer.Serialize(roster), campaignContext ?? string.Empty }));
        var planner = new GamePlanner(new PilotHandoffs.PreparationStore(), new InMemoryBoardCatalog([
            new BoardHandle(board.Board, board.Version, BoardReadStatus.AuthoredValid, "Complete Goodwood terrain", facts)]),
            vocabulary, [catalog], new PilotHandoffs.PreparationClock());
        var positions = accepted.Positions.ToDictionary(p => p.UnitId, StringComparer.Ordinal);
        object Position(PilotUnit unit)
        {
            var position = positions[unit.Id];
            var hex = board.Model.Geometry.NameOf(new HexIndex(position.Column - SituationAslBoard.OriginColumn,
                position.Row - SituationAslBoard.OriginRow));
            var at = board.Board.Value + ":" + hex + ":0";
            return unit.Kind is "gun" or "vehicle" ? new { at, facing = unit.Side == "Allied" ? "east" : "west" } : (object)new { at };
        }
        var placements = roster.Ground.SelectMany(f => f.Units).Select(unit => new
        {
            id = PilotHandoffs.AslId(unit.Id), kind = catalog.Definition(unit.Definition!.Definition)!.Kind,
            definition = unit.Definition.Definition, side = unit.Side.ToLowerInvariant(), position = Position(unit),
            holding = unit.Holding is { } link ? new { holder = PilotHandoffs.AslId(link.Holder), role = "manned" } : null,
            conditions = new Dictionary<string, bool> { [Conditions.Broken] = false, [Conditions.Berserk] = false,
                [Conditions.Captured] = false, [Conditions.Melee] = false, ["asl:ti"] = false,
                [Conditions.Disrupted] = false, [Conditions.Concealed] = false, [Conditions.Hidden] = false }
        }).ToArray();
        var arguments = JsonSerializer.SerializeToElement(new
        {
            gameId = "campaign-" + hash[..24], attemptId = "setup-" + hash[..24], expectedRevision = 0,
            start = new { label = "Operation Goodwood campaign setup", catalog = catalog.Identity.ToString(),
                boards = new[] { board.Board.Value }, firstSide = "allied",
                sides = new[] { new { id = "allied", nationality = "british" }, new { id = "german", nationality = "german" } } },
            placements
        });
        var plan = await planner.PlanAsync(GameActions.Setup, arguments, Tenant, "campaign-defaults", cancellationToken);
        if (!plan.CanCommit) throw new InvalidDataException("ASL setup validation: " + string.Join("; ", plan.Reasons));
        var history = planner.Replay(plan.Events);
        if (history.HasErrors || history.Current is not { } state || state.Units.Count + state.Equipment.Count != placements.Length)
            throw new InvalidDataException("Full ASL setup replay failed: " + string.Join("; ", history.Diagnostics.Select(d => d.Message)));
        cancellationToken.ThrowIfCancellationRequested();
        return new(Version, hash, campaignId, accepted.WorldIdentity, catalog.Identity.ToString(), board.Board.Value,
            board.Name, board.Version, Encoding.UTF8.GetString(board.FeaturesBytes()),
            GameEventWriter.Write(plan.Scope, new GameRecord(plan.Label, false, plan.Events)), roster, accepted, campaignContext);
    }
}
