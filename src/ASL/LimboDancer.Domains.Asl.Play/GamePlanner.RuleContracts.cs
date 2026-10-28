using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The fact sources of the wiring contracts (the Rules-first plan, section 6.6; the proving slice, ruling RPS.5): the facts Play reads from one state
/// for a contract, through the reads its commands use, so the Rule Workbench and play see the same facts. A fact Play has no source for is left out,
/// and the workbench asks for it by hand. Nothing here decides a rule.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// The facts gathered for a contract from a state, given the Units and Locations picked on the map (by the contract's fact names). The rally gathers
    /// every fact, as its command does (<see cref="LiveRally.FromState"/>); the advance's concealment reads night, the terrain, and the LOS of a Good Order
    /// enemy within 16 hexes; E10 eligibility reads only the participants' sides and whether they are active vehicles.
    /// </summary>
    public IReadOnlyDictionary<string, JsonNode?> ContractFacts(string contractId, GameState state, IReadOnlyDictionary<string, string> picks)
    {
        ArgumentNullException.ThrowIfNull(contractId);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(picks);
        return contractId switch
        {
            "a10.6-rally" => RallyContractFacts(state, picks),
            "a12.14-advance-concealment" => AdvanceConcealmentFacts(state, picks),
            "e10.1-eligibility" => AmmoEligibilityFacts(state, picks),
            _ => new Dictionary<string, JsonNode?>(),
        };
    }

    private Dictionary<string, JsonNode?> RallyContractFacts(GameState state, IReadOnlyDictionary<string, string> picks)
    {
        var facts = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        if (!picks.TryGetValue("unitId", out var unitId) || state.Unit(unitId) is not { } unit || state.Location(unit.Id)?.Location is not { } at
            || ReadLocation(state, at) is not { } read)
        {
            return facts;
        }

        var leaderId = picks.TryGetValue("leaderId", out var named) ? named : null;
        var concealed = ScenarioA1RallyRules.ConcealedAttempt(GameState.Condition(unit, Conditions.Concealed) == ConditionState.True,
            leaderId is not null && state.Unit(leaderId) is { } leaderUnit && GameState.Condition(leaderUnit, Conditions.Concealed) == ConditionState.True);
        var heat = ScenarioA1RallyRules.ReachesHeatOfBattle(leaderId);
        var (attempt, _) = LiveRally.FromState(state, unitId, leaderId, TerrainKey(read) ?? read.Level.Terrain?.Name ?? "unknown",
            concealed ? EnemyGoodOrderInLosWithin16(state, unit.Side, at) : null, heat ? KnownEnemyInLos(state, unit.Side, at) : null, heat ? Captors(state, unit) : null);
        if (attempt is null)
        {
            return facts;
        }

        facts["phase"] = attempt.Phase;
        facts["rallyingSide"] = attempt.RallyingSide;
        facts["locationId"] = attempt.LocationId;
        facts["terrain"] = attempt.Terrain;
        facts["unitId"] = attempt.Unit?.UnitId;
        facts["unitDefinition"] = attempt.Unit?.DefinitionId;
        facts["unitBroken"] = attempt.Unit?.Broken;
        facts["unitDisrupted"] = attempt.Unit?.Disrupted;
        facts["unitWounded"] = attempt.Unit?.Wounded;
        facts["unitDesperationMorale"] = attempt.Unit?.DesperationMorale;
        facts["unitConcealed"] = attempt.Unit?.Concealed;
        facts["unitAttemptedThisPlayerTurn"] = attempt.Unit?.AttemptedThisPlayerTurn;
        facts["unitOtherActionThisPhase"] = attempt.Unit?.OtherActionThisPhase;
        facts["unitFanatic"] = attempt.Unit?.Fanatic;
        facts["leaderId"] = attempt.Leader?.UnitId;
        facts["leaderDefinition"] = attempt.Leader?.DefinitionId;
        facts["leaderBroken"] = attempt.Leader?.Broken;
        facts["leaderWounded"] = attempt.Leader?.Wounded;
        facts["leaderConcealed"] = attempt.Leader?.Concealed;
        facts["goodOrderLeaderInLocation"] = attempt.GoodOrderLeaderInLocation;
        facts["brokenLeaderInLocation"] = attempt.BrokenLeaderInLocation;
        facts["firstMmcRallyOfOwnPlayerTurn"] = attempt.FirstMmcRallyOfOwnPlayerTurn;
        facts["enemyGoodOrderInLosWithin16"] = attempt.EnemyGoodOrderInLosWithin16;
        facts["knownEnemyInLos"] = attempt.KnownEnemyInLos;
        facts["captors"] = attempt.Captors is null ? null : attempt.Captors.Count == 0 ? "none" : string.Join(",", attempt.Captors);
        return facts;
    }

    private Dictionary<string, JsonNode?> AdvanceConcealmentFacts(GameState state, IReadOnlyDictionary<string, string> picks)
    {
        var facts = new Dictionary<string, JsonNode?>(StringComparer.Ordinal) { ["night"] = state.Night };
        if (picks.TryGetValue("advancerId", out var advancerId) && state.Unit(advancerId) is { } advancer)
        {
            facts["advancerId"] = advancer.Id;
            if (picks.TryGetValue("to", out var toText) && BoardLocation.TryParse(toText, out var to) && to is not null)
            {
                facts["to"] = to.ToString();
                facts["terrain"] = ReadLocation(state, to) is { } read ? TerrainKey(read) : null;
                facts["enemyGoodOrderInLosWithin16"] = EnemyGoodOrderInLosWithin16(state, advancer.Side, to);
            }
        }

        return facts;
    }

    private static Dictionary<string, JsonNode?> AmmoEligibilityFacts(GameState state, IReadOnlyDictionary<string, string> picks)
    {
        var facts = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var role in new[] { "supply", "recipient" })
        {
            if (picks.TryGetValue(role + "Id", out var id) && state.Unit(id) is { } unit)
            {
                facts[role + "Id"] = unit.Id;
                facts[role + "Owner"] = unit.Side;
                facts[role + "Active"] = unit.Status == InstanceStatus.Active;
                if (role == "recipient")
                {
                    facts["recipientIsVehicle"] = LiveFire.IsVehicle(unit);
                }
            }
        }

        return facts;
    }
}
