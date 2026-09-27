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
    public const string CatalogVersion = "1.3.0";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Reads recorded facts strictly: an unknown member refuses the record.</summary>
    public static readonly JsonSerializerOptions StrictJson = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>
    /// The state's part of the attack, or the reason it cannot be read (units steps 18 to 23). The firers may span
    /// Locations; the first firer's Location is the group's. The targets are every active unit in the target Location, in
    /// ordinal id order, Dummies included; in the MPh they are the moving stack only (A8.1); the Location may hold none
    /// (ruling R21.1). <paramref name="weapons"/> names the MGs each firer uses, and <paramref name="withoutInherent"/> the
    /// firers whose MG fires again alone on its Multiple ROF (A9.2).
    /// </summary>
    public static (FireAttack? Attack, string? Reason) FromState(GameState state, IReadOnlyList<string> firerIds, IReadOnlyList<string> directorIds,
        BoardLocation target, IReadOnlyDictionary<string, IReadOnlyList<string>>? weapons = null, IReadOnlyCollection<string>? withoutInherent = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(firerIds);
        ArgumentNullException.ThrowIfNull(directorIds);
        if (firerIds.Count == 0 || firerIds.Distinct(StringComparer.Ordinal).Count() != firerIds.Count)
        {
            return (null, "play.fire-firers: name each firer once");
        }

        if (state.Catalog.Catalog != Catalog || state.Catalog.Version != CatalogVersion)
        {
            return (null, $"play.fire-catalog: the Fire package reads {Catalog}@{CatalogVersion}, and this game uses {state.Catalog.Catalog}@{state.Catalog.Version}");
        }

        var firers = new List<(UnitInstance Unit, BoardLocation At)>();
        foreach (var id in firerIds)
        {
            if (state.Unit(id) is not { Status: InstanceStatus.Active, Definition: not null } unit || state.Location(unit.Id)?.Location is not { } at)
            {
                return (null, $"play.fire-firers: '{id}' is not an active unit from the catalog on the map");
            }

            firers.Add((unit, at));
        }

        var locations = firers.Select(item => item.At).Distinct().ToArray();
        var directors = new List<(UnitInstance Unit, BoardLocation At)>();
        foreach (var id in directorIds)
        {
            if (state.Unit(id) is not { Status: InstanceStatus.Active, Definition: not null } leader || state.Location(leader.Id)?.Location is not { } at
                || !locations.Contains(at))
            {
                return (null, $"play.fire-director: '{id}' is not an active unit in a Location of the fire group");
            }

            directors.Add((leader, at));
        }

        var side = firers[0].Unit.Side;
        var kind = (string?)null;
        if (state.Phase == "mph")
        {
            // A8.1, A8.3, A8.31: the firers' markers decide the kind of Defensive fire.
            var marks = firers.Select(item => Is(item.Unit, Conditions.FinalFire) ? 2 : Is(item.Unit, Conditions.FirstFire) ? 1 : 0).Distinct().ToArray();
            if (marks.Length != 1 && firers.All(item => withoutInherent?.Contains(item.Unit.Id) != true))
            {
                return (null, "play.fire-kind: a group mixes firers marked for different kinds of Defensive fire (A8.3, A8.31)");
            }

            kind = marks.Max() switch
            {
                0 => ScenarioA1FireCalculator.FirstFire,
                1 => withoutInherent is { Count: > 0 } ? ScenarioA1FireCalculator.FirstFire : ScenarioA1FireCalculator.SubsequentFirstFire,
                _ => ScenarioA1FireCalculator.FinalProtectiveFire,
            };
        }

        var movers = state.Phase == "mph" && state.Movement is { } movement && movement.Location == target ? movement.Movers : null;
        UnitInstance[] targets = [.. state.At(target).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active && (movers is null || movers.Contains(unit.Id)))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];
        if (targets.Any(unit => unit.Definition is null && unit.Kind != UnitKinds.Dummy))
        {
            return (null, "play.fire-target: the target Location holds a unit outside the catalog");
        }

        var phase = state.Phase switch
        {
            "pfph" => "PFPh",
            "dfph" => "DFPh",
            "afph" => "AFPh",
            "mph" => "MPh",
            _ => state.Phase,
        };
        var targetSide = targets.FirstOrDefault()?.Side ?? state.Sides.FirstOrDefault(item => item.Id != side)?.Id;
        var multi = locations.Length > 1;

        FireWeapon Weapon(string id) => state.Find(id) is EquipmentInstance equipment
            ? new FireWeapon(equipment.Id, equipment.Definition?.Definition, Is(equipment, Conditions.Malfunctioned), Fired(equipment),
                Is(equipment, Conditions.FirstFire))
            : new FireWeapon(id, null, null, null, null);

        var firerFacts = new List<FireFirer>();
        foreach (var (unit, at) in firers)
        {
            IReadOnlyList<FireWeapon>? used = null;
            if (weapons?.TryGetValue(unit.Id, out var named) == true && named.Count > 0)
            {
                // A7.35: a SW fires only when possessed by its unit.
                if (named.Any(id => state.Find(id) is not EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } holding }
                    || holding.Holder != unit.Id))
                {
                    return (null, $"play.fire-weapon: every weapon '{unit.Id}' fires must be one it possesses (A7.35)");
                }

                used = [.. named.Select(Weapon)];
            }

            firerFacts.Add(new FireFirer(unit.Id, unit.Definition!.Definition, at.ToString(), Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned),
                Is(unit, Conditions.Concealed), Fired(unit), used is not null)
            {
                FirstFireMarked = Is(unit, Conditions.FirstFire) ? true : null,
                FinalFireMarked = state.Phase == "mph" && Is(unit, Conditions.FinalFire) ? true : null,
                Weapons = used,
                UsesInherentFp = withoutInherent?.Contains(unit.Id) == true ? false : null,
                Fanatic = Is(unit, Conditions.Fanatic) ? true : null,
                Wounded = Is(unit, Conditions.Wounded) ? true : null,
            });
        }

        FireDirector Director((UnitInstance Unit, BoardLocation At) item) =>
            new(item.Unit.Id, item.Unit.Definition!.Definition, item.At.ToString(), Is(item.Unit, Conditions.Broken), Is(item.Unit, Conditions.Pinned),
                Is(item.Unit, Conditions.Concealed), Fired(item.Unit) || Is(item.Unit, Conditions.FirstFire), Is(item.Unit, Conditions.Wounded));

        return (new FireAttack(
            phase,
            side == state.PhasingSide ? "phasing" : "non-phasing",
            true,
            firers[0].At.ToString(),
            target.ToString(),
            firerFacts,
            directors.Count == 0 ? null : Director(directors[0]),
            null,
            null,
            null,
            state.ScenarioMonth,
            null,
            [.. targets.Select(unit => Target(unit, target))],
            targetSide is null ? null : state.Side(targetSide)?.Elr,
            null)
        {
            FireKind = kind,
            TargetMovement = kind is null ? null : new FireMovement(state.Movement?.Assault ?? false),
            FiringSideElr = kind == ScenarioA1FireCalculator.FinalProtectiveFire ? state.Side(side)?.Elr : null,
            OtherDirectors = directors.Count > 1 ? [.. directors.Skip(1).Select(Director)] : null,
        }, null);
    }

    /// <summary>
    /// The state's part of a Residual FP attack on the moving stack as it enters a Location (A8.2, A8.22): no firers, the
    /// counter's FP, and the stack as the targets.
    /// </summary>
    public static (FireAttack? Attack, string? Reason) ResidualFromState(GameState state, BoardLocation target, int fp)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Movement is not { } movement || movement.Location != target)
        {
            return (null, "play.fire-residual: Residual FP attacks the moving stack in its Location (A8.2)");
        }

        UnitInstance[] targets = [.. state.At(target).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active && movement.Movers.Contains(unit.Id)).OrderBy(unit => unit.Id, StringComparer.Ordinal)];
        var targetSide = state.PhasingSide;
        return (new FireAttack("MPh", "non-phasing", null, null, target.ToString(), null, null, null, null, null, state.ScenarioMonth, null,
            [.. targets.Select(unit => Target(unit, target))],
            state.Side(targetSide)?.Elr, null)
        {
            FireKind = ScenarioA1FireCalculator.ResidualFire,
            TargetMovement = new FireMovement(movement.Assault),
            ResidualFp = fp,
        }, null);
    }

    /// <summary>The rolls of a record, rebuilt from its roll ids and the recorded dice, in the calculator's shape.</summary>
    public static FireRolls? Rolls(FireResolved fire, FireAttack attack, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(fire);
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(rolls);
        IReadOnlyList<int>? ift = null;
        Dictionary<string, int>? selection = null;
        Dictionary<string, int>? weaponSelection = null;
        Dictionary<string, int>? firerSelection = null;
        Dictionary<string, IReadOnlyList<int>>? checks = null;
        Dictionary<string, IReadOnlyList<int>>? leaderLoss = null;
        Dictionary<string, int>? wounds = null;
        Dictionary<string, IReadOnlyList<int>>? heat = null;

        // A selection roll names the units it selects among, one die each, in order.
        static bool Select(ref Dictionary<string, int>? into, string ids, DiceRolled roll)
        {
            var names = ids.Split(',');
            if (roll.Count != names.Length)
            {
                return false;
            }

            into ??= new(StringComparer.Ordinal);
            foreach (var (id, index) in names.Select((id, index) => (id, index)))
            {
                into[id] = roll.Values[index];
            }

            return true;
        }
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
                case "randomSelection" when unit.Length == 0 && roll.Count == attack.Targets!.Count:
                    // A record made before the steps 19 to 23 revision draws one die for every target.
                    selection = attack.Targets.Select((target, index) => (target.UnitId!, roll.Values[index]))
                        .ToDictionary(pair => pair.Item1, pair => pair.Item2, StringComparer.Ordinal);
                    break;
                case "randomSelection" when unit.Length > 0:
                    if (!Select(ref selection, unit, roll))
                    {
                        return null;
                    }

                    break;
                case "weaponSelection":
                    if (!Select(ref weaponSelection, unit, roll))
                    {
                        return null;
                    }

                    break;
                case "firerSelection":
                    if (!Select(ref firerSelection, unit, roll))
                    {
                        return null;
                    }

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
                case "heatOfBattle" when roll.Count == 2:
                    (heat ??= new(StringComparer.Ordinal))[unit] = roll.Values;
                    break;
                default:
                    return null;
            }
        }

        return new FireRolls(ift, selection, checks, leaderLoss, wounds)
        {
            WeaponSelection = weaponSelection,
            FirerSelection = firerSelection,
            HeatOfBattle = heat,
        };
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    /// <summary>A unit in the target Location, with the Fanatic (A10.8) and heroic (A15.21) states the Fire package reads.</summary>
    private static FireTarget Target(UnitInstance unit, BoardLocation at) =>
        new(unit.Id, unit.Definition?.Definition, at.ToString(), Is(unit, Conditions.Broken),
            Is(unit, Conditions.Pinned), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), unit.Kind == UnitKinds.Dummy,
            Is(unit, Conditions.Wounded), Is(unit, Conditions.Disrupted))
        {
            Fanatic = Is(unit, Conditions.Fanatic) ? true : null,
            Heroic = Is(unit, Conditions.Heroic) ? true : null,
        };

    // A7.1: a unit fires in one fire phase per Player Turn; A7.531: a directing leader is marked too.
    private static bool Fired(IGameObject item) => Is(item, Conditions.PrepFire) || Is(item, Conditions.FinalFire);
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

        FireAttack? expected;
        string? reason;
        if (recorded.FireKind == ScenarioA1FireCalculator.ResidualFire)
        {
            (expected, reason) = LiveFire.ResidualFromState(state, target, recorded.ResidualFp ?? 0);
        }
        else
        {
            // The recorded choices (directors, weapons, a Multiple ROF shot) select what the state is read for; the state's
            // facts are then compared whole.
            var directors = (recorded.Director is null ? [] : new[] { recorded.Director.UnitId! })
                .Concat(recorded.OtherDirectors?.Select(item => item.UnitId!) ?? []).ToArray();
            var weapons = recorded.Firers?.Where(item => item.Weapons is { Count: > 0 })
                .ToDictionary(item => item.UnitId!, item => (IReadOnlyList<string>)[.. item.Weapons!.Select(weapon => weapon.EquipmentId!)], StringComparer.Ordinal);
            var alone = recorded.Firers?.Where(item => item.UsesInherentFp == false).Select(item => item.UnitId!).ToArray();
            (expected, reason) = LiveFire.FromState(state, fire.Firers, directors, target, weapons, alone);
        }

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
            Firers = recorded.Firers is null || expected.Firers is null ? expected.Firers
                : [.. expected.Firers.Zip(recorded.Firers, (fact, record) => fact with { Range = record.Range, SameLevel = record.SameLevel, Los = record.Los })],
            FirerLocationsAdjacent = recorded.FirerLocationsAdjacent,
            WithinSubsequentFirstFireRange = recorded.WithinSubsequentFirstFireRange,
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
