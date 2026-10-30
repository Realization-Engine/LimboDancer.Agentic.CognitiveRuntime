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
    public IReadOnlyDictionary<string, JsonNode?> ContractFacts(string contractId, GameState state, IReadOnlyDictionary<string, string> picks, Perspective viewer)
    {
        ArgumentNullException.ThrowIfNull(contractId);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(picks);
        ArgumentNullException.ThrowIfNull(viewer);
        var view = ViewOf(state, viewer, []);
        return contractId switch
        {
            "a10.6-rally" => RallyContractFacts(state, picks, view),
            "a12.14-advance-concealment" => AdvanceConcealmentFacts(state, picks, view),
            "e10.1-eligibility" => AmmoEligibilityFacts(picks, view),
            "c9.3-spotter-replacement" => SpotterReplacementContractFacts(state, picks, view),
            "c9-ordnance-participation" => OrdnanceParticipationContractFacts(state, picks, view),
            "c9-mortar-shot" => MortarShotContractFacts(picks, view),
            _ => new Dictionary<string, JsonNode?>(),
        };
    }

    private static Dictionary<string, JsonNode?> OrdnanceParticipationContractFacts(GameState state, IReadOnlyDictionary<string, string> picks, GameView view)
    {
        var facts = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        var primary = picks.TryGetValue("firerId", out var firerId) ? view.Units.FirstOrDefault(unit => unit.Id == firerId) : null;
        var second = picks.TryGetValue("secondId", out var secondId) ? view.Units.FirstOrDefault(unit => unit.Id == secondId) : null;
        var primaryKnown = primary is not null && (view.Perspective.IsAdjudicator || primary.Side == view.Perspective.Name);
        var secondKnown = second is not null && (view.Perspective.IsAdjudicator || second.Side == view.Perspective.Name);
        if (primaryKnown)
        {
            facts["firerId"] = primary!.Id;
            facts["primaryKind"] = primary.Kind;
        }
        if (secondKnown)
        {
            facts["secondId"] = second!.Id;
            facts["secondKind"] = second.Kind;
        }
        if (!primaryKnown || !secondKnown)
        {
            return facts;
        }
        // A Unit pick does not choose among several held weapons. Ambiguous weapon history remains unknown.
        var weapons = view.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == primary!.Id
            && ScenarioA1OrdnanceParticipationCalculator.WeaponRefusal(item.Kind) is null).Take(2).ToArray();
        if (weapons.Length != 1)
        {
            return facts;
        }
        var read = LiveOrdnance.ParticipantFacts(state, weapons[0].Id, primary!, second!.Id);
        facts["active"] = read.ActiveWithDefinition;
        facts["sameSide"] = read.SameSide;
        facts["sameLocation"] = read.AtFirer;
        facts["broken"] = read.Crew?.Broken;
        facts["berserk"] = read.Crew?.Berserk;
        facts["pinned"] = read.Crew?.Pinned;
        facts["firedOther"] = read.Crew?.FiredInherentFp;
        facts["melee"] = read.Melee;
        facts["captured"] = read.Captured;
        facts["ti"] = read.Ti;
        facts["usedOther"] = read.UsedOtherWeapon;
        facts["directed"] = read.DirectedFire;
        facts["spotted"] = read.Spotted;
        return facts;
    }

    private static Dictionary<string, JsonNode?> MortarShotContractFacts(IReadOnlyDictionary<string, string> picks, GameView view)
    {
        var facts = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        if (!picks.TryGetValue("firerId", out var firerId) || view.Units.FirstOrDefault(unit => unit.Id == firerId) is not { } primary
            || (!view.Perspective.IsAdjudicator && primary.Side != view.Perspective.Name))
        {
            return facts;
        }
        facts["firerId"] = primary.Id;
        facts["hero"] = primary.Kind == "asl:hero" || Is(primary, Conditions.Heroic);
        if (!ScenarioA1OrdnanceParticipationCalculator.Smc(primary.Kind))
        {
            facts["loneSmc"] = false;
        }
        var mortars = view.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == primary.Id
            && LiveOrdnance.IsMortar(item)).Take(2).ToArray();
        if (mortars.Length == 1 && mortars[0].Definition is { } definition && OrdnanceReference.Value.Guns.GetValueOrDefault(definition.Definition) is { } mortar)
        {
            facts["weapon"] = mortar.GunType;
            facts["caliber"] = mortar.Caliber;
            facts["minimumRange"] = mortar.RangeMinimum;
        }
        // This contract selects no target or second firer. Those proposed-shot facts are still entered explicitly.
        return facts;
    }

    private Dictionary<string, JsonNode?> RallyContractFacts(GameState state, IReadOnlyDictionary<string, string> picks, GameView view)
    {
        var facts = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        if (!picks.TryGetValue("unitId", out var unitId) || view.Units.FirstOrDefault(item => item.Id == unitId) is not { } unit
            || !view.Locations.TryGetValue(unit.Id, out var position) || position.Location is not { } at
            || ReadLocation(state, at) is not { } read)
        {
            return facts;
        }

        var owner = view.Perspective.IsAdjudicator || view.Perspective.Name == unit.Side;
        var leader = picks.TryGetValue("leaderId", out var named) ? view.Units.FirstOrDefault(item => item.Id == named) : null;
        var leaderId = leader?.Id;
        var concealed = ScenarioA1RallyRules.ConcealedAttempt(Is(unit, Conditions.Concealed), leader is not null && Is(leader, Conditions.Concealed));
        var heat = ScenarioA1RallyRules.ReachesHeatOfBattle(leaderId);
        var seen = !owner || concealed ? ContractEnemySight(state, view, unit.Side, at) : null;
        // Unknown contents are not an absent leader or captor. These optional sources stay unknown when inspection withholds their inputs.
        var enemyContentsKnown = view.Perspective.IsAdjudicator || !view.Sealed.Any(stack => stack.Side != unit.Side);
        var knownEnemy = heat && enemyContentsKnown ? KnownEnemyInLos(state, unit.Side, at) : null;
        var captors = heat && enemyContentsKnown ? Captors(state, unit) : null;
        var terrain = TerrainKey(read) ?? read.Level.Terrain?.Name ?? "unknown";
        // An opponent never invokes the whole-state rally read, which scans the rallying side's concealed leaders and companions.
        var attempt = owner ? LiveRally.FromState(state, unitId, leaderId, terrain, seen, knownEnemy, captors).Attempt
            : unit.Definition is null ? null : ScenarioA1RallyRules.Attempt(new RallyAttemptStateFacts(state.Phase, unit.Side == state.PhasingSide,
                new RallyUnitStateFacts(unit.Id, unit.Definition.Definition, at.ToString(), Is(unit, Conditions.Broken), Is(unit, Conditions.Disrupted),
                    Is(unit, Conditions.Wounded), Is(unit, Conditions.DesperationMorale), Is(unit, Conditions.Concealed),
                    state.RallyAttemptsThisPlayerTurn.Contains(unit.Id), state.RepairsThisPhase.Contains(unit.Id), state.RallyPhaseActions.Contains(unit.Id), false, false),
                null, terrain, [], state.FirstMmcRallyTaken.Contains(unit.Side), seen, knownEnemy, captors, [], false, null, false));
        if (attempt is null)
        {
            return facts;
        }

        facts["phase"] = attempt.Phase;
        facts["rallyingSide"] = attempt.RallyingSide;
        facts["locationId"] = attempt.LocationId;
        facts["terrain"] = attempt.Terrain;
        facts["unitId"] = attempt.Unit?.UnitId;
        if (owner)
        {
            facts["unitDefinition"] = attempt.Unit?.DefinitionId;
            facts["unitFanatic"] = attempt.Unit?.Fanatic;
            facts["goodOrderLeaderInLocation"] = attempt.GoodOrderLeaderInLocation;
            facts["brokenLeaderInLocation"] = attempt.BrokenLeaderInLocation;
        }
        facts["unitBroken"] = attempt.Unit?.Broken;
        facts["unitDisrupted"] = attempt.Unit?.Disrupted;
        facts["unitWounded"] = attempt.Unit?.Wounded;
        facts["unitDesperationMorale"] = attempt.Unit?.DesperationMorale;
        facts["unitConcealed"] = attempt.Unit?.Concealed;
        facts["unitAttemptedThisPlayerTurn"] = attempt.Unit?.AttemptedThisPlayerTurn;
        facts["unitOtherActionThisPhase"] = attempt.Unit?.OtherActionThisPhase;
        facts["leaderId"] = leaderId;
        if (leader is not null && (view.Perspective.IsAdjudicator || leader.Side == view.Perspective.Name))
        {
            facts["leaderDefinition"] = leader.Definition?.Definition;
        }
        if (owner)
        {
            facts["leaderBroken"] = attempt.Leader?.Broken;
            facts["leaderWounded"] = attempt.Leader?.Wounded;
            facts["leaderConcealed"] = attempt.Leader?.Concealed;
        }
        facts["firstMmcRallyOfOwnPlayerTurn"] = attempt.FirstMmcRallyOfOwnPlayerTurn;
        if (view.Perspective.IsAdjudicator || view.Perspective.Name != unit.Side)
        {
            facts["enemyGoodOrderInLosWithin16"] = attempt.EnemyGoodOrderInLosWithin16;
        }
        facts["knownEnemyInLos"] = attempt.KnownEnemyInLos;
        facts["captors"] = attempt.Captors is null ? null : attempt.Captors.Count == 0 ? "none" : string.Join(",", attempt.Captors);
        return facts;
    }

    private Dictionary<string, JsonNode?> AdvanceConcealmentFacts(GameState state, IReadOnlyDictionary<string, string> picks, GameView view)
    {
        var facts = new Dictionary<string, JsonNode?>(StringComparer.Ordinal) { ["night"] = state.Night };
        if (picks.TryGetValue("to", out var toText) && BoardLocation.TryParse(toText, out var to) && to is not null)
        {
            facts["to"] = to.ToString();
            facts["terrain"] = ReadLocation(state, to) is { } read ? TerrainKey(read) : null;
            if (picks.TryGetValue("advancerId", out var advancerId))
            {
                var advancer = view.Units.FirstOrDefault(unit => unit.Id == advancerId);
                var side = advancer?.Side ?? view.Sealed.FirstOrDefault(stack => stack.PlacementId == advancerId)?.Side;
                if (advancer is not null && (view.Perspective.IsAdjudicator || side == view.Perspective.Name))
                {
                    facts["advancerId"] = advancer.Id;
                }
                if (side is not null && (view.Perspective.IsAdjudicator || side != view.Perspective.Name))
                {
                    facts["enemyGoodOrderInLosWithin16"] = ContractEnemySight(state, view, side, to);
                }
            }
        }

        return facts;
    }

    private static Dictionary<string, JsonNode?> AmmoEligibilityFacts(IReadOnlyDictionary<string, string> picks, GameView view)
    {
        var facts = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var role in new[] { "supply", "recipient" })
        {
            if (picks.TryGetValue(role + "Id", out var id) && view.Units.FirstOrDefault(item => item.Id == id) is { } unit)
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

    private Dictionary<string, JsonNode?> SpotterReplacementContractFacts(GameState state, IReadOnlyDictionary<string, string> picks, GameView view)
    {
        var facts = new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
        {
            ["phase"] = ScenarioA1RuleContracts.Find("c9.3-spotter-replacement")?.Facts.FirstOrDefault(fact => fact.Name == "phase")?.Choices
                ?.FirstOrDefault(phase => string.Equals(phase, state.Phase, StringComparison.OrdinalIgnoreCase)) ?? state.Phase,
            ["night"] = state.Night,
        };
        var holder = picks.TryGetValue("holderId", out var holderId) ? view.Units.FirstOrDefault(unit => unit.Id == holderId) : null;
        var candidate = picks.TryGetValue("spotterId", out var spotterId) ? view.Units.FirstOrDefault(unit => unit.Id == spotterId) : null;
        if (holder is not null)
        {
            facts["holderId"] = holder.Id;
            facts["ownerPhasing"] = holder.Side == state.PhasingSide;
        }
        if (candidate is not null)
        {
            facts["spotterId"] = candidate.Id;
            if (view.Perspective.IsAdjudicator || candidate.Side == view.Perspective.Name)
            {
                facts["spotterActive"] = candidate.Status == InstanceStatus.Active && view.Locations.ContainsKey(candidate.Id);
                facts["spotterSameSide"] = holder is null ? null : candidate.Side == holder.Side;
                facts["spotterPersonnel"] = ScenarioA1SpotterCalculator.Personnel(candidate.Kind);
                facts["spotterGoodOrder"] = GameState.GoodOrder(candidate, vocabulary) == ConditionState.True;
                facts["spotterCaptured"] = Is(candidate, Conditions.Captured);
                facts["spotterMelee"] = Is(candidate, Conditions.Melee);
                facts["spotterIsMortarCrew"] = holder is null ? null : candidate.Id == holder.Id;
            }
        }
        if (holder is null || (!view.Perspective.IsAdjudicator && holder.Side != view.Perspective.Name))
        {
            return facts;
        }
        var candidateKnown = candidate is not null && (view.Perspective.IsAdjudicator || candidate.Side == view.Perspective.Name);
        // The contract picks a holder, not equipment. More than one mortar leaves the weapon ambiguous instead of choosing one silently.
        var mortars = view.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == holder.Id
            && ScenarioA1MortarCalculator.IsMortar(item.Kind, item.Definition is { } definition
                ? OrdnanceReference.Value.Guns.GetValueOrDefault(definition.Definition)?.GunType : null)).Take(2).ToArray();
        // With no authorized candidate, use the known holder only to read the mortar's history. No candidate-dependent fact from that read is emitted.
        if (mortars.Length != 1 || ReadSpotterReplacement(state, mortars[0].Id, candidateKnown ? candidate!.Id : holder.Id) is not { } read)
        {
            return facts;
        }
        facts["mortar"] = read.Mortar;
        facts["hasDesignation"] = read.Timing.HasDesignation;
        facts["lossCause"] = read.Timing.Change switch
        {
            SpotterChange.Eliminated => "eliminated",
            SpotterChange.Broken => "broken",
            SpotterChange.Captured => "captured",
            SpotterChange.SquadReduced => "reduced",
            SpotterChange.None => "none",
            _ => "unknown",
        };
        facts["reducedSurvivorGoodOrder"] = read.Timing.GoodOrderReductionSurvivor;
        facts["lossBeforeThisPhase"] = read.Timing.LossAt is { } lost
            ? lost.GameTurn != read.Timing.Now.GameTurn || lost.PlayerTurn != read.Timing.Now.PlayerTurn || lost.Phase != read.Timing.Now.Phase
            : null;
        facts["openingClosed"] = !ScenarioA1SpotterCalculator.OpeningOpen(read.StepsTaken);
        if (candidateKnown)
        {
            facts["hexRange"] = read.HexRange;
            facts["sharesLocation"] = read.SharesLocation;
            facts["spotterActive"] = read.Candidate.Active;
            facts["spotterSameSide"] = read.Candidate.SameSide;
            facts["spotterPersonnel"] = read.Candidate.Personnel;
            facts["spotterGoodOrder"] = read.Candidate.GoodOrder;
            facts["spotterCaptured"] = read.Candidate.Captured;
            facts["spotterMelee"] = read.Candidate.Melee;
            facts["spotterIsMortarCrew"] = read.Candidate.MortarCrew;
        }
        facts["pendingDecision"] = read.PendingDecision;
        return facts;
    }

    /// <summary>The opponent's LOS fact comes only from that viewer's own seers; the owner does not ask it at all (PS1).</summary>
    private bool? ContractEnemySight(GameState state, GameView view, string watchedSide, BoardLocation at)
    {
        if (!view.Perspective.IsAdjudicator && view.Perspective.Name == watchedSide)
        {
            return null;
        }
        if (view.Perspective.IsAdjudicator)
        {
            return EnemyGoodOrderInLosWithin16(state, watchedSide, at);
        }
        var locations = new List<BoardLocation>();
        var indexes = new Dictionary<BoardLocation, int>();
        var seers = new List<SightSeerFacts>();
        var index = MemoOf(state).Objects(state);
        foreach (var unit in view.Units.Where(unit => unit.Side == view.Perspective.Name))
        {
            int? position = null;
            if (view.Locations.TryGetValue(unit.Id, out var map))
            {
                if (!indexes.TryGetValue(map.Location, out var found))
                {
                    found = locations.Count;
                    indexes.Add(map.Location, found);
                    locations.Add(map.Location);
                }
                position = found;
            }
            seers.Add(SeerFactsOf(index, unit, position));
        }
        var table = new SightTable([.. seers], [.. locations], indexes);
        return ScenarioA1Sight.SeenWithin(ScenarioA1Sight.NearestSeer(ScenarioA1Sight.SeerLocations(table.Units, watchedSide, SightPurpose.Loss),
            table.IndexOf(at), new SightLos(this, state, MemoOf(state), table, at), SightLosReading.Clear), 16);
    }
}
