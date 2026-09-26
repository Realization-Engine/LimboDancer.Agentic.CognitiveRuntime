using System.Text.Json;
using System.Text.Json.Serialization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The facts of a live fire attack that the game state decides (Fire in Live Play, unit step 18): the phase and side,
/// the fire group and its director, the target Location's units, the target side's ELR, and the month. The map facts
/// (range, levels, LOS, terrain) are added by the planner from the map read.
/// </summary>
public static class LiveFire
{
    public const string Catalog = "asl-scenario-a1";
    public const string CatalogVersion = "1.1.0";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Reads recorded facts strictly: an unknown member refuses the record.</summary>
    public static readonly JsonSerializerOptions StrictJson = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>
    /// The state's part of the attack, or the reason it cannot be read. The firers must share one Location; the targets
    /// are every active unit in the target Location, in ordinal id order.
    /// </summary>
    public static (FireAttack? Attack, string? Reason) FromState(GameState state, IReadOnlyList<string> firerIds, string? directorId,
        BoardLocation target)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(firerIds);
        if (firerIds.Count == 0 || firerIds.Distinct(StringComparer.Ordinal).Count() != firerIds.Count)
        {
            return (null, "play.fire-firers: name each firer once");
        }

        if (state.Catalog.Catalog != Catalog || state.Catalog.Version != CatalogVersion)
        {
            return (null, $"play.fire-catalog: the Fire package reads {Catalog}@{CatalogVersion}, and this game uses {state.Catalog.Catalog}@{state.Catalog.Version}");
        }

        var firers = new List<UnitInstance>();
        foreach (var id in firerIds)
        {
            if (state.Unit(id) is not { Status: InstanceStatus.Active } unit || unit.Definition is null)
            {
                return (null, $"play.fire-firers: '{id}' is not an active unit from the catalog");
            }

            firers.Add(unit);
        }

        if (firers.Select(unit => state.Location(unit.Id)?.Location).Distinct().ToArray() is not [{ } from])
        {
            return (null, "play.fire-firers: the fire group must be in one Location on the map (A7.5)");
        }

        UnitInstance? director = null;
        if (directorId is not null)
        {
            if (state.Unit(directorId) is not { Status: InstanceStatus.Active, Definition: not null } leader || state.Location(leader.Id)?.Location != from)
            {
                return (null, $"play.fire-director: '{directorId}' is not an active unit in the firers' Location");
            }

            director = leader;
        }

        var side = firers[0].Side;
        UnitInstance[] targets = [.. state.At(target).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active)
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];
        if (targets.Length == 0 || targets.Any(unit => unit.Definition is null))
        {
            return (null, "play.fire-target: the target Location holds no unit from the catalog");
        }

        var phase = state.Phase switch
        {
            "pfph" => "PFPh",
            "dfph" => "DFPh",
            _ => state.Phase,
        };
        var targetSide = targets[0].Side;
        return (new FireAttack(
            phase,
            side == state.PhasingSide ? "phasing" : "non-phasing",
            true,
            from.ToString(),
            target.ToString(),
            [.. firers.Select(unit => new FireFirer(unit.Id, unit.Definition!.Definition, from.ToString(), Is(unit, Conditions.Broken),
                Is(unit, Conditions.Pinned), Is(unit, Conditions.Concealed), Fired(unit), false))],
            director is null ? null : new FireDirector(director.Id, director.Definition!.Definition, from.ToString(), Is(director, Conditions.Broken),
                Is(director, Conditions.Pinned), Is(director, Conditions.Concealed), Fired(director), Is(director, Conditions.Wounded)),
            null,
            null,
            null,
            state.ScenarioMonth,
            null,
            [.. targets.Select(unit => new FireTarget(unit.Id, unit.Definition!.Definition, target.ToString(), Is(unit, Conditions.Broken),
                Is(unit, Conditions.Pinned), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), false, Is(unit, Conditions.Wounded),
                Is(unit, Conditions.Disrupted)))],
            state.Side(targetSide)?.Elr,
            null), null);
    }

    /// <summary>The rolls of a record, rebuilt from its roll ids and the recorded dice, in the calculator's shape.</summary>
    public static FireRolls? Rolls(FireResolved fire, FireAttack attack, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(fire);
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(rolls);
        IReadOnlyList<int>? ift = null;
        Dictionary<string, int>? selection = null;
        Dictionary<string, IReadOnlyList<int>>? checks = null;
        Dictionary<string, IReadOnlyList<int>>? leaderLoss = null;
        Dictionary<string, int>? wounds = null;
        foreach (var (key, id) in fire.Rolls)
        {
            if (!rolls.TryGetValue(id, out var roll) || roll.Sides != 6)
            {
                return null;
            }

            var split = key.IndexOf(':', StringComparison.Ordinal);
            var (kind, unit) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
            switch (kind)
            {
                case "attack" when roll.Count == 2:
                    ift = roll.Values;
                    break;
                case "randomSelection" when roll.Count == attack.Targets!.Count:
                    selection = attack.Targets.Select((target, index) => (target.UnitId!, roll.Values[index]))
                        .ToDictionary(pair => pair.Item1, pair => pair.Item2, StringComparer.Ordinal);
                    break;
                case "checks" when roll.Count == 2:
                    (checks ??= new(StringComparer.Ordinal))[unit] = roll.Values;
                    break;
                case "leaderLoss" when roll.Count == 2:
                    (leaderLoss ??= new(StringComparer.Ordinal))[unit] = roll.Values;
                    break;
                case "woundSeverity" when roll.Count == 1:
                    (wounds ??= new(StringComparer.Ordinal))[unit] = roll.Values[0];
                    break;
                default:
                    return null;
            }
        }

        return new FireRolls(ift, selection, checks, leaderLoss, wounds);
    }

    private static bool Is(UnitInstance unit, string condition) => GameState.Condition(unit, condition) == ConditionState.True;

    // A7.1: a unit fires in one fire phase per Player Turn; A7.531: a directing leader is marked too.
    private static bool Fired(UnitInstance unit) => Is(unit, Conditions.PrepFire) || Is(unit, Conditions.FinalFire);
}

/// <summary>
/// Replays a fire record through the Fire package (Fire in Live Play): the recorded facts must agree with the state the
/// record is made in, and the package must reproduce the recorded resolution from those facts and the recorded dice.
/// The map facts are the planner's, recorded with the attack.
/// </summary>
public sealed class FireRecordVerifier(ScenarioA1FireReference reference) : IFireRecordVerifier
{
    private static readonly Lazy<FireRecordVerifier> Instance = new(() => new FireRecordVerifier(new ScenarioA1FirePackage().Reference));

    public static FireRecordVerifier Shared => Instance.Value;

    public string? Verify(GameState state, FireResolved fire, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(fire);
        ArgumentNullException.ThrowIfNull(rolls);
        FireAttack? recorded;
        try
        {
            recorded = fire.Facts.Deserialize<FireAttack>(LiveFire.StrictJson);
        }
        catch (JsonException exception)
        {
            return "The fire record's facts cannot be read: " + exception.Message;
        }

        if (recorded is null || recorded.Rolls is not null || !BoardLocation.TryParse(fire.TargetLocation, out var target))
        {
            return "The fire record's facts are incomplete.";
        }

        var (expected, reason) = LiveFire.FromState(state, fire.Firers, fire.Director, target);
        if (expected is null)
        {
            return reason;
        }

        // The state's facts must be the recorded ones; the map facts are taken as recorded.
        var merged = expected with
        {
            Range = recorded.Range,
            SameLevel = recorded.SameLevel,
            Los = recorded.Los,
            TargetTerrain = recorded.TargetTerrain,
        };
        if (JsonSerializer.Serialize(merged, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json))
        {
            return "The fire record's facts do not match the game state.";
        }

        if (LiveFire.Rolls(fire, recorded, rolls) is not { } dice)
        {
            return "The fire record names a roll of the wrong shape.";
        }

        var resolution = ScenarioA1FireCalculator.Resolve(recorded with
        {
            Rolls = dice
        }, reference);
        return resolution.Disposition != FireResolution.Resolved
            ? "The fire record's facts and rolls do not resolve: " + string.Join("; ", resolution.Reasons)
            : !JsonElement.DeepEquals(JsonSerializer.SerializeToElement(resolution, LiveFire.Json), fire.Resolution)
                ? "The fire record's resolution differs from what its facts and rolls give."
                : null;
    }
}
