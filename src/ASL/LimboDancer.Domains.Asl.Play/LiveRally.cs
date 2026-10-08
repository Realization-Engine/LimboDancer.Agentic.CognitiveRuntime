using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The facts of a live Rally attempt that the game state decides (unit step 19): the phase and side, the unit and the
/// rallying leader, the leaders in the Location, whether it is the side's first MMC Rally attempt of its own Player Turn,
/// and the attempts already made. The map facts (the Location's terrain, and whether a Good Order enemy within 16 hexes
/// has LOS to it) are added by the planner.
/// </summary>
public static class LiveRally
{
    /// <summary>The state's part of the attempt, or the reason it cannot be read.</summary>
    public static (RallyAttempt? Attempt, string? Reason) FromState(GameState state, string unitId, string? leaderId, string? terrain,
        bool? enemyGoodOrderInLosWithin16, bool? knownEnemyInLos = null, IReadOnlyList<string>? captors = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unitId);
        if (state.Catalog.Catalog != LiveFire.Catalog || state.Catalog.Version != LiveFire.CatalogVersion)
        {
            return (null, $"play.rally-catalog: the Rally package reads {LiveFire.Catalog}@{LiveFire.CatalogVersion}, and this game uses {state.Catalog.Catalog}@{state.Catalog.Version}");
        }

        if (state.Unit(unitId) is not { Status: InstanceStatus.Active, Definition: not null } unit || state.Location(unit.Id)?.Location is not { } at)
        {
            return (null, $"play.rally-unit: '{unitId}' is not an active unit from the catalog on the map");
        }

        UnitInstance? leader = null;
        if (leaderId is not null)
        {
            if (state.Unit(leaderId) is not { Status: InstanceStatus.Active, Definition: not null } found)
            {
                return (null, $"play.rally-leader: '{leaderId}' is not an active unit from the catalog");
            }

            leader = found;
        }

        // A10.6, A10.63, A10.71: the other friendly leaders in the unit's Location, Good Order or broken.
        var leaders = state.At(at).OfType<UnitInstance>()
            .Where(item => ScenarioA1RallyRules.OtherFriendlyLeader(item.Status == InstanceStatus.Active, item.Id == unit.Id, item.Side == unit.Side, item.Kind == "asl:leader")).ToArray();

        // A15.41: the other friendly units in the Location, whom a leader who goes berserk tries to take with him.
        RallyCompanion[] companions = [.. state.At(at).OfType<UnitInstance>()
            .Where(item => ScenarioA1RallyRules.BerserkCompanion(item.Status == InstanceStatus.Active, item.Id == unit.Id, item.Side == unit.Side, item.Definition is not null,
                Is(item, Conditions.Captured)))
            .OrderBy(item => item.Id, StringComparer.Ordinal)
            .Select(item => new RallyCompanion(item.Id, item.Definition!.Definition, Is(item, Conditions.Broken), Is(item, Conditions.Wounded), Is(item, Conditions.Fanatic),
                Is(item, Conditions.Heroic), Is(item, Conditions.Berserk)))];
        // The state's facts cross to Rules, which decides what the attempt declares (pass 32.f); the record keeps its shape.
        return (ScenarioA1RallyRules.Attempt(new RallyAttemptStateFacts(
            state.Phase,
            unit.Side == state.PhasingSide,
            new RallyUnitStateFacts(unit.Id, unit.Definition.Definition, at.ToString(), Is(unit, Conditions.Broken), Is(unit, Conditions.Disrupted),
                Is(unit, Conditions.Wounded), Is(unit, Conditions.DesperationMorale), Is(unit, Conditions.Concealed),
                state.RallyAttemptsThisPlayerTurn.Contains(unit.Id), state.RepairsThisPhase.Contains(unit.Id), state.RallyPhaseActions.Contains(unit.Id),
                Is(unit, Conditions.Fanatic), LiveFire.GreenInexperienced(state, unit)),
            leader is null ? null : new RallyLeaderStateFacts(leader.Id, leader.Definition!.Definition, state.Location(leader.Id)?.Location.ToString(),
                Is(leader, Conditions.Broken), Is(leader, Conditions.Wounded), Is(leader, Conditions.Concealed)),
            terrain,
            [.. leaders.Select(item => Is(item, Conditions.Broken))],
            state.FirstMmcRallyTaken.Contains(unit.Side),
            enemyGoodOrderInLosWithin16,
            knownEnemyInLos,
            captors,
            companions,
            state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal),
            Commissar(state, unit)?.Id,
            GamePlanner.ExtremeWinterReduction(state, unit.Side) is not null)), null);
    }

    /// <summary>
    /// The unpinned, unbroken, not berserk Commissar of a unit's side in its Location, other than the unit (A25.221, A25.222; backlog pass 15, ruling R15.6);
    /// null when there is none.
    /// </summary>
    public static UnitInstance? Commissar(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        return state.Location(unit.Id) is not { } at ? null : state.At(at.Location).OfType<UnitInstance>().FirstOrDefault(item => ScenarioA1RallyRules.Commissar(
            item.Status == InstanceStatus.Active, item.Id == unit.Id, item.Side == unit.Side, item.Definition is { } reference && ScenarioA1FireReference.IsCommissar(reference.Definition),
            Is(item, Conditions.Broken), Is(item, Conditions.Pinned), Is(item, Conditions.Berserk), Is(item, Conditions.Captured)));
    }

    /// <summary>The rolls of a record, rebuilt from its roll ids and the recorded dice.</summary>
    public static RallyRolls? Rolls(RallyAttempted rally, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(rally);
        ArgumentNullException.ThrowIfNull(rolls);
        IReadOnlyList<int>? dice = null;
        IReadOnlyList<int>? heat = null;
        int? severity = null;
        int? creation = null;
        Dictionary<string, IReadOnlyList<int>>? berserk = null;
        foreach (var (key, id) in rally.Rolls)
        {
            if (!rolls.TryGetValue(id, out var roll) || roll.Sides != 6)
            {
                return null;
            }

            switch (key)
            {
                case "rally" when roll.Count == 2:
                    dice = roll.Values;
                    break;
                case "woundSeverity" when roll.Count == 1:
                    severity = roll.Values[0];
                    break;
                case "heatOfBattle" when roll.Count == 2:
                    heat = roll.Values;
                    break;
                case "leaderCreation" when roll.Count == 1:
                    creation = roll.Values[0];
                    break;
                case var check when check.StartsWith("berserkCheck:", StringComparison.Ordinal) && roll.Count == 2:
                    (berserk ??= new(StringComparer.Ordinal))[check["berserkCheck:".Length..]] = roll.Values;
                    break;
                default:
                    return null;
            }
        }

        return new RallyRolls(dice, severity)
        {
            HeatOfBattle = heat,
            LeaderCreation = creation,
            BerserkChecks = berserk,
        };
    }

    private static bool Is(UnitInstance unit, string condition) => GameState.Condition(unit, condition) == ConditionState.True;
}

/// <summary>
/// Replays a Rally record through the Rally package (unit step 19): the recorded facts must agree with the state the record
/// is made in, and the package must reproduce the recorded resolution. The map facts are the planner's, taken as recorded.
/// </summary>
public sealed class RallyRecordVerifier(ScenarioA1RallyReference reference) : IRallyRecordVerifier
{
    private static readonly Lazy<RallyRecordVerifier> Instance = new(() => new RallyRecordVerifier(new ScenarioA1RallyPackage().Reference));

    public static RallyRecordVerifier Shared => Instance.Value;

    public string? Verify(GameState state, RallyAttempted rally, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(rally);
        ArgumentNullException.ThrowIfNull(rolls);
        RallyAttempt? recorded;
        try
        {
            recorded = rally.Facts.Deserialize<RallyAttempt>(LiveFire.StrictJson);
        }
        catch (JsonException exception)
        {
            return "The Rally record's facts cannot be read: " + exception.Message;
        }

        if (recorded is null || recorded.Rolls is not null)
        {
            return "The Rally record's facts are incomplete.";
        }

        var (expected, reason) = LiveRally.FromState(state, rally.Unit, rally.Leader, recorded.Terrain, recorded.EnemyGoodOrderInLosWithin16, recorded.KnownEnemyInLos,
            recorded.Captors);
        if (expected is null)
        {
            return reason;
        }

        // A record made before unit step 30 names no companions, and is compared without them.
        if (recorded.Companions is null)
        {
            expected = expected with
            {
                Companions = null
            };
        }

        // The owner's answers are declared, and the projector checks them against the choices made (ruling R5.8).
        expected = expected with
        {
            Choices = recorded.Choices
        };

        if (JsonSerializer.Serialize(expected, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json))
        {
            return "The Rally record's facts do not match the game state.";
        }

        if (LiveRally.Rolls(rally, rolls) is not { } dice)
        {
            return "The Rally record names a roll of the wrong shape.";
        }

        var resolution = ScenarioA1RallyCalculator.Resolve(recorded with
        {
            Rolls = dice
        }, reference);
        return resolution.Disposition != RallyResolution.Resolved
            ? "The Rally record's facts and rolls do not resolve: " + string.Join("; ", resolution.Reasons)
            : !JsonElement.DeepEquals(JsonSerializer.SerializeToElement(resolution, LiveFire.Json), rally.Resolution)
                ? "The Rally record's resolution differs from what its facts and rolls give."
                : null;
    }
}
