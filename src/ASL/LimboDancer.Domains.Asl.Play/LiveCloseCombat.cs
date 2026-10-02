using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The facts of a live CC round or Ambush that the game state decides (unit step 29): the CCPh, the Location's units with
/// their conditions, which of them advanced there this APh (A11.4), which are held in Melee, the SW they possess, and the
/// Location's CC so far this CCPh. The Location's terrain is the planner's map read; the declared attacks and the SMC
/// stacking are the players' choices.
/// </summary>
public static class LiveCloseCombat
{
    /// <summary>The units of a CC Location as the package reads them, or the reason they cannot be read.</summary>
    public static (IReadOnlyList<CloseCombatUnit>? Units, string? Reason) Units(GameState state, BoardLocation location,
        IReadOnlyDictionary<string, string>? stacking, IReadOnlyDictionary<string, string>? withdrawals = null, IReadOnlyDictionary<string, string>? infiltrations = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(location);
        if (state.Catalog.Catalog != LiveFire.Catalog || state.Catalog.Version != LiveFire.CatalogVersion)
        {
            return (null, $"play.cc-catalog: the Close Combat package reads {LiveFire.Catalog}@{LiveFire.CatalogVersion}, and this game uses {state.Catalog.Catalog}@{state.Catalog.Version}");
        }

        // A11.19 (ruling R14.2): Dummies are removed before any attack is declared, so they are never CC units.
        UnitInstance[] units = [.. state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Kind != UnitKinds.Dummy)
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];
        if (units.Any(unit => unit.Definition is null))
        {
            return (null, "play.cc-units: the Location holds a unit outside the catalog (A11.19)");
        }

        return ([.. units.Select(unit => new CloseCombatUnit(unit.Id, unit.Definition!.Definition, unit.Side, Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned),
            Is(unit, Conditions.Wounded), Is(unit, Conditions.Disrupted), Is(unit, Conditions.Berserk), Is(unit, Conditions.Fanatic), Is(unit, Conditions.Heroic),
            Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden), Is(unit, Conditions.Captured),
            state.Advances.Any(item => item.Unit == unit.Id && item.To == location), Is(unit, Conditions.Melee))
        {
            StackedWith = stacking?.GetValueOrDefault(unit.Id),
            WithdrawingTo = withdrawals?.GetValueOrDefault(unit.Id),
            Cx = Is(unit, Conditions.Cx) ? true : null,

            // Rulings R14.3, R14.5, R14.8: TI, Unarmed units and prisoners' Guards, and the declared Infiltrations.
            Ti = Is(unit, "asl:ti") ? true : null,
            Unarmed = Is(unit, Conditions.Unarmed) ? true : null,
            GuardId = Is(unit, Conditions.Captured) ? unit.Custodian : null,
            InfiltrateTo = infiltrations?.GetValueOrDefault(unit.Id),
            Weapons = state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id)
                .Select(item => item.Id).Order(StringComparer.Ordinal).ToArray() is { Length: > 0 } weapons ? weapons : null,
        })], null);
    }

    /// <summary>The Ambush facts of a Location, before the drs; <paramref name="terrain"/> is the planner's read.</summary>
    public static (AmbushFacts? Facts, string? Reason) AmbushFromState(GameState state, BoardLocation location, string? terrain)
    {
        ArgumentNullException.ThrowIfNull(state);
        var (units, reason) = Units(state, location, null);

        // A11.4 (ruling R14.2): a hidden unit placed there as the CCPh began makes an Ambush possible.
        return units is null ? (null, reason) : (new AmbushFacts(state.Phase == "ccph" ? "CCPh" : state.Phase, location.ToString(), terrain, state.PhasingSide, units, null)
        {
            HiddenPlaced = units.Any(unit => state.HiddenPlaced.Contains(unit.UnitId!, StringComparer.Ordinal)) ? true : null,
        }, null);
    }

    /// <summary>
    /// The facts of the next CC round in a Location, before the rolls: its round follows from the Location's CC so far (A11.32),
    /// and a Location where an Ambush can occur must have its Ambush drs first (A11.4). After an Ambush the ambusher's attacks are
    /// sequential (A11.3): each is resolved before the next is declared, until the ambushed side's round is <paramref name="requested"/>.
    /// </summary>
    public static (CloseCombatFacts? Facts, string? Reason) FromState(GameState state, BoardLocation location, string? terrain,
        IReadOnlyList<CloseCombatDeclaration> attacks, IReadOnlyDictionary<string, string>? stacking, IReadOnlyDictionary<string, string>? withdrawals = null,
        string? requested = null, IReadOnlyDictionary<string, string>? infiltrations = null, bool handToHand = false, bool overrun = false)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(attacks);
        var (units, reason) = Units(state, location, stacking, withdrawals, infiltrations);
        if (units is null)
        {
            return (null, reason);
        }

        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);

        // A4.152 (ruling R27.3): the CC of an Infantry OVR follows its entry at once, with no Ambush (A11.4 is for units that advance into CC).
        if (overrun)
        {
            return (new CloseCombatFacts("MPh", location.ToString(), terrain, state.PhasingSide, CloseCombatFacts.Simultaneous, null, units, [], [], attacks, null)
            {
                InfantryOverrun = true,
            }, null);
        }

        if (entry is null && ScenarioA1CloseCombatCalculator.AmbushPossible(terrain, units, units.Any(unit => state.HiddenPlaced.Contains(unit.UnitId!, StringComparer.Ordinal))))
        {
            return (null, $"play.cc-ambush-first: Infantry advanced into CC in {location}, so the Ambush drs come first (A11.4)");
        }

        // A11.33, A11.34 (ruling R14.6): the prisoners' escape round, when asked for, comes before any other; the rounds after it follow as before.
        var rounds = entry?.Rounds.Where(item => item != CloseCombatFacts.PrisonersRound).Count() ?? 0;
        var ambushers = entry?.Ambusher is { } ambusher && units.Any(unit => unit.Side == ambusher && unit.Captured != true);
        var round = requested == CloseCombatFacts.PrisonersRound && (entry is null || entry.Rounds.Count == 0) ? CloseCombatFacts.PrisonersRound
            : entry?.Ambusher is null ? CloseCombatFacts.Simultaneous
            : ambushers && (rounds == 0 || requested == CloseCombatFacts.AmbusherRound) ? CloseCombatFacts.AmbusherRound
            : CloseCombatFacts.AmbushedRound;

        // J2.31 (ruling R14.1): the Location's first round other than the prisoners' declares Hand-to-Hand for the CCPh.
        var hand = rounds > 0 ? entry!.HandToHand : round != CloseCombatFacts.PrisonersRound && handToHand;
        return (new CloseCombatFacts(state.Phase == "ccph" ? "CCPh" : state.Phase, location.ToString(), terrain, state.PhasingSide, round, entry?.Ambusher, units,
            entry?.Attacked ?? [], entry?.Attacking ?? [], attacks, null)
        {
            HandToHand = hand ? true : null,
        }, null);
    }

    /// <summary>The rolls of a CC record, rebuilt from its roll ids and the recorded dice, in the package's shape.</summary>
    public static CloseCombatRolls? Rolls(CloseCombatResolved combat, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(rolls);
        var attacks = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal);
        var selection = new Dictionary<string, int>(StringComparer.Ordinal);
        var wounds = new Dictionary<string, int>(StringComparer.Ordinal);
        var creation = new Dictionary<string, int>(StringComparer.Ordinal);
        var weapons = new Dictionary<string, int>(StringComparer.Ordinal);
        var escapes = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal);
        var stacks = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (key, id) in combat.Rolls)
        {
            if (!rolls.TryGetValue(id, out var roll) || roll.Sides != 6)
            {
                return null;
            }

            var split = key.IndexOf(':', StringComparison.Ordinal);
            var (kind, rest) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
            switch (kind)
            {
                case "attack" when roll.Count == 2:
                    attacks[rest] = roll.Values;
                    break;
                case "escapeNtc" when roll.Count == 2:
                    escapes[rest] = roll.Values;
                    break;
                case "leaderStack":
                    var stackAt = rest.IndexOf(':', StringComparison.Ordinal);
                    var stackIds = stackAt < 0 ? [] : rest[(stackAt + 1)..].Split(',');
                    if (stackAt < 0 || roll.Count != stackIds.Length)
                    {
                        return null;
                    }

                    foreach (var (unit, index) in stackIds.Select((unit, index) => (unit, index)))
                    {
                        stacks[rest[..stackAt] + ":" + unit] = roll.Values[index];
                    }

                    break;
                case "randomSelection":
                    // "randomSelection:<attack>:<id>,<id>": one die per unit, in order.
                    var at = rest.IndexOf(':', StringComparison.Ordinal);
                    var ids = at < 0 ? [] : rest[(at + 1)..].Split(',');
                    if (at < 0 || roll.Count != ids.Length)
                    {
                        return null;
                    }

                    foreach (var (unit, index) in ids.Select((unit, index) => (unit, index)))
                    {
                        selection[rest[..at] + ":" + unit] = roll.Values[index];
                    }

                    break;
                case "woundSeverity" when roll.Count == 1:
                    wounds[rest] = roll.Values[0];
                    break;
                case "leaderCreation" when roll.Count == 1:
                    creation[rest] = roll.Values[0];
                    break;
                case "weaponLoss" when roll.Count == 1:
                    weapons[rest] = roll.Values[0];
                    break;
                default:
                    return null;
            }
        }

        return new CloseCombatRolls(attacks.Count > 0 ? attacks : null)
        {
            RandomSelection = selection.Count > 0 ? selection : null,
            WoundSeverity = wounds.Count > 0 ? wounds : null,
            LeaderCreation = creation.Count > 0 ? creation : null,
            WeaponLoss = weapons.Count > 0 ? weapons : null,
            EscapeNtc = escapes.Count > 0 ? escapes : null,
            LeaderStack = stacks.Count > 0 ? stacks : null,
        };
    }

    /// <summary>
    /// The facts of one CC attack with a vehicle (rulings R11.13 to R11.15), before the rolls: the Infantry of the Location, the vehicle's states, the
    /// attackers or defenders, and whether it is CC Reaction Fire, whose attackers marked First or Final Fire are named (D7.213). A vehicle moving in
    /// the MPh is Non-Stopped (A11.51).
    /// </summary>
    public static (VehicleCloseCombatFacts? Facts, string? Reason) VehicleFromState(GameState state, BoardLocation location, string vehicleId,
        IReadOnlyList<string> attackers, IReadOnlyList<string> defenders, bool byVehicle, bool reaction)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Unit(vehicleId) is not { Status: InstanceStatus.Active } vehicle || !LiveFire.IsVehicle(vehicle) || state.Location(vehicleId)?.Location != location
            || vehicle.Definition is not { } definition)
        {
            return (null, $"play.cc-vehicle: {vehicleId} is not an active vehicle in {location}");
        }

        var (units, reason) = Units(state, location, null);
        if (units is null)
        {
            return (null, reason);
        }

        var moving = reaction && state.Movement is { Vehicle: true, Started: true, Stopped: false } movement && movement.Members.Contains(vehicle.Id, StringComparer.Ordinal);
        var facts = new VehicleCloseCombatFacts(state.Phase == "ccph" ? "CCPh" : state.Phase == "mph" ? "MPh" : state.Phase, location.ToString(),
            new VehicleCloseCombatVehicle(vehicle.Id, definition.Definition, vehicle.Side, LiveFire.CrewExposed(vehicle), Is(vehicle, Conditions.Motion) || moving,
                Is(vehicle, Conditions.Immobilized) || Is(vehicle, Conditions.Bogged), Is(vehicle, Conditions.Stunned) || Is(vehicle, Conditions.Recalled),
                Is(vehicle, Conditions.Shocked) || Is(vehicle, Conditions.UnconfirmedKill), Is(vehicle, Conditions.Abandoned),
                Is(vehicle, Conditions.Malfunctioned) || Is(vehicle, Conditions.Disabled), Is(vehicle, Conditions.BmgMalfunctioned), Is(vehicle, Conditions.CmgMalfunctioned)),
            [.. units.Where(unit => state.Unit(unit.UnitId!) is { } found && !LiveFire.IsVehicle(found))],
            byVehicle ? [] : attackers, byVehicle ? defenders : [], byVehicle, reaction, null)
        {
            FireMarked = reaction && attackers.Where(id => state.Unit(id) is { } unit && (Is(unit, Conditions.FirstFire) || Is(unit, Conditions.FinalFire))).ToArray() is { Length: > 0 } marked
                ? marked : null,
        };
        return (facts, null);
    }

    /// <summary>The rolls of a CC record with a vehicle, rebuilt from its roll ids and the recorded dice.</summary>
    public static VehicleCloseCombatRolls? VehicleRolls(VehicleCloseCombatResolved combat, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(rolls);
        IReadOnlyList<int>? attack = null;
        int? unlikely = null;
        var selection = new Dictionary<string, int>(StringComparer.Ordinal);
        var wounds = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (key, id) in combat.Rolls)
        {
            if (!rolls.TryGetValue(id, out var roll) || roll.Sides != 6)
            {
                return null;
            }

            var split = key.IndexOf(':', StringComparison.Ordinal);
            var (kind, rest) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
            switch (kind)
            {
                case "attack" when roll.Count == 2:
                    attack = roll.Values;
                    break;
                case "unlikelyKill" when roll.Count == 1:
                    unlikely = roll.Values[0];
                    break;
                case "randomSelection":
                    var ids = rest.Split(',');
                    if (roll.Count != ids.Length)
                    {
                        return null;
                    }

                    foreach (var (unit, index) in ids.Select((unit, index) => (unit, index)))
                    {
                        selection[unit] = roll.Values[index];
                    }

                    break;
                case "woundSeverity" when roll.Count == 1:
                    wounds[rest] = roll.Values[0];
                    break;
                default:
                    return null;
            }
        }

        return new VehicleCloseCombatRolls(attack)
        {
            UnlikelyKill = unlikely,
            RandomSelection = selection.Count > 0 ? selection : null,
            WoundSeverity = wounds.Count > 0 ? wounds : null,
        };
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;
}

/// <summary>
/// Replays CC and Ambush records through the Close Combat package (unit step 29): the recorded facts must agree with the state
/// the record is made in (the terrain is the planner's, taken as recorded; the attacks and the stacking are the players'), and
/// the package must reproduce the recorded resolution.
/// </summary>
public sealed class CloseCombatRecordVerifier(ScenarioA1CloseCombatReference reference) : ICloseCombatRecordVerifier
{
    private static readonly Lazy<CloseCombatRecordVerifier> Instance = new(() => new CloseCombatRecordVerifier(new ScenarioA1CloseCombatPackage().Reference));

    public static CloseCombatRecordVerifier Shared => Instance.Value;

    public string? VerifyAmbush(GameState state, AmbushRolled ambush, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(ambush);
        ArgumentNullException.ThrowIfNull(rolls);
        AmbushFacts? recorded;
        try
        {
            recorded = ambush.Facts.Deserialize<AmbushFacts>(LiveFire.StrictJson);
        }
        catch (JsonException exception)
        {
            return "The Ambush record's facts cannot be read: " + exception.Message;
        }

        if (recorded is null || recorded.Rolls is not null)
        {
            return "The Ambush record's facts are incomplete.";
        }

        var (expected, reason) = LiveCloseCombat.AmbushFromState(state, ambush.Location, recorded.Terrain);
        if (expected is null)
        {
            return reason;
        }

        // E1.77 (ruling R16.7): whether the Location is Illuminated is the planner's reading of the map; by day there is no dark night.
        expected = expected with
        {
            DarkNight = state.Night ? recorded.DarkNight : null
        };
        if (JsonSerializer.Serialize(expected, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json))
        {
            return "The Ambush record's facts do not match the game state.";
        }

        var drs = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (side, id) in ambush.Rolls)
        {
            if (!rolls.TryGetValue(id, out var roll) || roll.Count != 1 || roll.Sides != 6)
            {
                return "The Ambush record names a roll of the wrong shape.";
            }

            drs[side] = roll.Values[0];
        }

        var resolution = ScenarioA1CloseCombatCalculator.ResolveAmbush(recorded with
        {
            Rolls = drs
        }, reference);
        return resolution.Disposition != CloseCombatResolution.Resolved
            ? "The Ambush record's facts and rolls do not resolve: " + string.Join("; ", resolution.Reasons)
            : resolution.Ambusher != ambush.Ambusher || !JsonElement.DeepEquals(JsonSerializer.SerializeToElement(resolution, LiveFire.Json), ambush.Resolution)
                ? "The Ambush record's resolution differs from what its facts and rolls give."
                : null;
    }

    /// <summary>
    /// A CC attack with a vehicle (rulings R11.13 to R11.15): its facts must be the state's, each unit's Inexperience taken as recorded (the planner's
    /// read of A19.2), and the package must reproduce its resolution.
    /// </summary>
    public string? VerifyVehicle(GameState state, VehicleCloseCombatResolved combat, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(rolls);
        VehicleCloseCombatFacts? recorded;
        try
        {
            recorded = combat.Facts.Deserialize<VehicleCloseCombatFacts>(LiveFire.StrictJson);
        }
        catch (JsonException exception)
        {
            return "The CC record's facts cannot be read: " + exception.Message;
        }

        if (recorded?.Units is null || recorded.Rolls is not null)
        {
            return "The CC record's facts are incomplete.";
        }

        var (expected, reason) = LiveCloseCombat.VehicleFromState(state, combat.Location, combat.Vehicle, combat.Attackers, combat.Defenders, combat.ByVehicle, combat.Reaction);
        if (expected is null)
        {
            return reason;
        }

        var inexperienced = recorded.Units.ToDictionary(unit => unit.UnitId!, unit => unit.Inexperienced, StringComparer.Ordinal);
        expected = expected with
        {
            Units = [.. expected.Units!.Select(unit => unit with { Inexperienced = inexperienced.GetValueOrDefault(unit.UnitId!) })],
        };
        if (JsonSerializer.Serialize(expected, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json))
        {
            return "The CC record's facts do not match the game state.";
        }

        if (LiveCloseCombat.VehicleRolls(combat, rolls) is not { } dice)
        {
            return "The CC record names a roll of the wrong shape.";
        }

        var resolution = ScenarioA1VehicleCloseCombat.Resolve(recorded with
        {
            Rolls = dice
        }, reference);
        return resolution.Disposition != CloseCombatResolution.Resolved
            ? "The CC record's facts and rolls do not resolve: " + string.Join("; ", resolution.Reasons)
            : !JsonElement.DeepEquals(JsonSerializer.SerializeToElement(resolution, LiveFire.Json), combat.Resolution)
                ? "The CC record's resolution differs from what its facts and rolls give."
                : null;
    }

    public string? Verify(GameState state, CloseCombatResolved combat, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(rolls);
        CloseCombatFacts? recorded;
        try
        {
            recorded = combat.Facts.Deserialize<CloseCombatFacts>(LiveFire.StrictJson);
        }
        catch (JsonException exception)
        {
            return "The CC record's facts cannot be read: " + exception.Message;
        }

        if (recorded?.Units is null || recorded.Attacks is null || recorded.Rolls is not null)
        {
            return "The CC record's facts are incomplete.";
        }

        // The stacking and the withdrawals are the players' declarations, taken as recorded.
        var stacking = recorded.Units.Where(unit => unit.StackedWith is not null).ToDictionary(unit => unit.UnitId!, unit => unit.StackedWith!, StringComparer.Ordinal);
        var withdrawals = recorded.Units.Where(unit => unit.WithdrawingTo is not null).ToDictionary(unit => unit.UnitId!, unit => unit.WithdrawingTo!, StringComparer.Ordinal);
        var infiltrations = recorded.Units.Where(unit => unit.InfiltrateTo is not null).ToDictionary(unit => unit.UnitId!, unit => unit.InfiltrateTo!, StringComparer.Ordinal);
        var (expected, reason) = LiveCloseCombat.FromState(state, combat.Location, recorded.Terrain, recorded.Attacks, stacking, withdrawals, recorded.Round, infiltrations,
            recorded.HandToHand == true, recorded.InfantryOverrun == true);
        if (expected is null)
        {
            return reason;
        }

        if (JsonSerializer.Serialize(expected, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json))
        {
            return "The CC record's facts do not match the game state.";
        }

        if (combat.Round != recorded.Round
            || !combat.Attackers.Order(StringComparer.Ordinal).SequenceEqual(recorded.Attacks.SelectMany(item => item.Attackers ?? []).Order(StringComparer.Ordinal))
            || !combat.Defenders.Order(StringComparer.Ordinal).SequenceEqual(recorded.Attacks.SelectMany(item => item.Defenders ?? []).Order(StringComparer.Ordinal)))
        {
            return "The CC record's round, attackers, or defenders differ from its facts.";
        }

        if (LiveCloseCombat.Rolls(combat, rolls) is not { } dice)
        {
            return "The CC record names a roll of the wrong shape.";
        }

        var resolution = ScenarioA1CloseCombatCalculator.Resolve(recorded with
        {
            Rolls = dice
        }, reference);
        return resolution.Disposition != CloseCombatResolution.Resolved
            ? "The CC record's facts and rolls do not resolve: " + string.Join("; ", resolution.Reasons)
            : !JsonElement.DeepEquals(JsonSerializer.SerializeToElement(resolution, LiveFire.Json), combat.Resolution)
                ? "The CC record's resolution differs from what its facts and rolls give."
                : null;
    }
}
