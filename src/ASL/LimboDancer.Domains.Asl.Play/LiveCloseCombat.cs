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
    /// <summary>
    /// The units of a CC Location as the package reads them, or the reason they cannot be read; with <paramref name="guns"/>, the Guns each mans, which a
    /// round's A11.13 loss reads (pass 70, ruling R70.27) and an Ambush or a vehicle's CC does not.
    /// </summary>
    public static (IReadOnlyList<CloseCombatUnit>? Units, string? Reason) Units(GameState state, BoardLocation location,
        IReadOnlyDictionary<string, string>? stacking, IReadOnlyDictionary<string, string>? withdrawals = null, IReadOnlyDictionary<string, string>? infiltrations = null,
        bool guns = false, bool riders = false)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(location);
        if (ScenarioA1CloseCombatFactBuilders.CatalogBar(state.Catalog.Catalog, state.Catalog.Version, LiveFire.Catalog, LiveFire.CatalogVersion) is { } catalogBar)
        {
            return (null, catalogBar);
        }

        // A11.19 (ruling R14.2): Dummies are removed before any attack is declared, so they are never CC units. Pass 70 (ruling R70.30): nor is a vehicle,
        // whose CC is the vehicle package's; a Location holding one had no Infantry round or Ambush before the pass. With <paramref name="riders"/>, the
        // Riders of its vehicles, attacked apart in Infantry against Infantry CC (A11.611; ruling R70.31).
        var here = state.At(location).OfType<UnitInstance>().ToArray();
        UnitInstance[] units = [.. here.Where(unit => ScenarioA1CloseCombatFactBuilders.IsCcUnit(unit.Status == InstanceStatus.Active, unit.Kind == UnitKinds.Dummy) && !LiveFire.IsVehicle(unit))
            .Concat(riders ? here.Where(LiveFire.IsVehicle).SelectMany(vehicle => state.Passengers(vehicle.Id)).Where(unit => unit.Position is ContainedPosition { Role: ContainmentRole.Rider })
                : [])
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];
        if (units.Any(unit => unit.Definition is null))
        {
            return (null, ScenarioA1CloseCombatFactBuilders.OutsideCatalogText());
        }

        // Rulings R14.3, R14.5, R14.8: TI, Unarmed units and prisoners' Guards, and the declared Infiltrations; Rules builds each unit (pass 32.g). Pass 70
        // (ruling R70.27): the Guns each unit mans, for A11.13's loss; (ruling R70.31) a Rider marked as one.
        return ([.. units.Select(unit => Marked(unit, ScenarioA1CloseCombatFactBuilders.Unit(unit.Id, unit.Definition!.Definition, unit.Side, Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned),
            Is(unit, Conditions.Wounded), Is(unit, Conditions.Disrupted), Is(unit, Conditions.Berserk), Is(unit, Conditions.Fanatic), Is(unit, Conditions.Heroic),
            Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), Is(unit, Conditions.Captured), state.Advances.Any(item => item.Unit == unit.Id && item.To == location),
            Is(unit, Conditions.Melee), stacking?.GetValueOrDefault(unit.Id), withdrawals?.GetValueOrDefault(unit.Id), Is(unit, Conditions.Cx), Is(unit, "asl:ti"),
            Is(unit, Conditions.Unarmed), unit.Custodian, infiltrations?.GetValueOrDefault(unit.Id),
            [.. state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id)
                .Select(item => item.Id).Order(StringComparer.Ordinal)], LiveStacking.Manning(state, unit.Id),
            !guns ? null : [.. state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Kind == "asl:gun" && item.Holding is { Role: HoldingRole.Manned } manned
                && manned.Holder == unit.Id).Select(item => item.Id).Order(StringComparer.Ordinal)])))], null);

        static CloseCombatUnit Marked(UnitInstance unit, CloseCombatUnit built) => unit.Position is ContainedPosition { Role: ContainmentRole.Rider } ? built with
        {
            Rider = true
        } : built;
    }

    /// <summary>The Ambush facts of a Location, before the drs; <paramref name="terrain"/> is the planner's read.</summary>
    public static (AmbushFacts? Facts, string? Reason) AmbushFromState(GameState state, BoardLocation location, string? terrain)
    {
        ArgumentNullException.ThrowIfNull(state);
        var (units, reason) = Units(state, location, null);

        // A11.4 (ruling R14.2): a hidden unit placed there as the CCPh began makes an Ambush possible. A11.4's table (pass 70, ruling R70.30): each side's
        // vehicles there that are not Abandoned or captured, and which of them are AFV that are BU or Stunned, for the side's drm.
        return units is null ? (null, reason) : (ScenarioA1CloseCombatFactBuilders.Ambush(state.Phase, location.ToString(), terrain, state.PhasingSide, units,
            units.Any(unit => state.HiddenPlaced.Contains(unit.UnitId!, StringComparer.Ordinal))) with
        {
            Vehicles = ScenarioA1CloseCombatFactBuilders.AmbushVehicles([.. state.At(location).OfType<UnitInstance>()
                .Where(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit) && !Is(unit, Conditions.Abandoned) && !Is(unit, Conditions.Captured))
                .Select(unit => (unit.Side, LiveFire.UnarmoredOf(unit.Definition?.Definition) != true, LiveFire.CrewExposed(unit), Is(unit, Conditions.Stunned)))]),
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
        // A11.31, A11.611 (pass 70, rulings R70.30, R70.31): a sequential round in a Location holding a vehicle reads its Riders too.
        var sequential = requested == CloseCombatFacts.Sequential;
        var (units, reason) = Units(state, location, stacking, withdrawals, infiltrations, guns: true, riders: sequential);
        if (units is null)
        {
            return (null, reason);
        }

        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);

        // A4.152 (ruling R27.3): the CC of an Infantry OVR follows its entry at once, with no Ambush (A11.4 is for units that advance into CC).
        if (overrun)
        {
            return (ScenarioA1CloseCombatFactBuilders.Overrun(location.ToString(), terrain, state.PhasingSide, units, attacks), null);
        }

        if (ScenarioA1CloseCombatFactBuilders.AmbushFirstBar(entry is not null, ScenarioA1CloseCombatCalculator.AmbushPossible(terrain, units, units.Any(unit => state.HiddenPlaced.Contains(unit.UnitId!, StringComparer.Ordinal))),
            location.ToString()) is { } ambushFirst)
        {
            return (null, ambushFirst);
        }

        // A11.33, A11.34 (ruling R14.6): the prisoners' escape round, when asked for, comes before any other; the rounds after it follow as before. J2.31
        // (ruling R14.1): the Location's first round other than the prisoners' declares Hand-to-Hand for the CCPh.
        var (round, rounds) = ScenarioA1CloseCombatFactBuilders.NextRound(requested, entry?.Rounds ?? [], entry?.Ambusher, entry?.Ambusher is { } ambusher && units.Any(unit => unit.Side == ambusher && unit.Captured != true));
        var hand = ScenarioA1CloseCombatFactBuilders.HandToHand(rounds, entry?.HandToHand == true, round, handToHand);

        // A11.12's EXC (ruling R70.30): a sequential round's units attacked before are those Infantry attacked; a vehicle's attacks are counted apart.
        return (ScenarioA1CloseCombatFactBuilders.Round(state.Phase, location.ToString(), terrain, state.PhasingSide, round, entry?.Ambusher, units,
            (sequential ? entry?.InfantryAttacked : entry?.Attacked) ?? [], entry?.Attacking ?? [], attacks, hand), null);
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
            return (null, ScenarioA1CloseCombatFactBuilders.VehicleNotActiveText(vehicleId, location.ToString()));
        }

        var (units, reason) = Units(state, location, null);
        if (units is null)
        {
            return (null, reason);
        }

        var moving = ScenarioA1CloseCombatFactBuilders.VehicleMoving(reaction, state.Movement is { Vehicle: true, Started: true, Stopped: false },
            state.Movement?.Members.Contains(vehicle.Id, StringComparer.Ordinal) == true);
        var facts = ScenarioA1CloseCombatFactBuilders.VehicleAttack(state.Phase, location.ToString(),
            ScenarioA1CloseCombatFactBuilders.Vehicle(vehicle.Id, definition.Definition, vehicle.Side, LiveFire.CrewExposed(vehicle), Is(vehicle, Conditions.Motion), moving,
                Is(vehicle, Conditions.Immobilized), Is(vehicle, Conditions.Bogged), Is(vehicle, Conditions.Stunned), Is(vehicle, Conditions.Recalled),
                Is(vehicle, Conditions.Shocked), Is(vehicle, Conditions.UnconfirmedKill), Is(vehicle, Conditions.Abandoned),
                Is(vehicle, Conditions.Malfunctioned), Is(vehicle, Conditions.Disabled), Is(vehicle, Conditions.BmgMalfunctioned), Is(vehicle, Conditions.CmgMalfunctioned),
                Is(vehicle, Conditions.StunRecovery),
                // D3.44 (pass 55, ruling R55.16): the Armor Leader modifies the vehicle's own CC attack; Rules gives none to an attack on it.
                ScenarioA1ArmorLeaders.CloseCombatLeadership(LiveFire.ArmorLeadership(vehicle), byVehicle)),
            [.. units.Where(unit => state.Unit(unit.UnitId!) is { } found && !LiveFire.IsVehicle(found))], attackers, defenders, byVehicle, reaction,
            ScenarioA1CloseCombatFactBuilders.FireMarked(reaction, [.. attackers.Where(id => state.Unit(id) is { } unit && (Is(unit, Conditions.FirstFire) || Is(unit, Conditions.FinalFire)))]));

        // A21.2 (pass 65, ruling R65.22): an attack on an Abandoned AFV planned since the pass carries its unmanned state; Rules reads it for an AFV only.
        // A11.4, A11.51 (pass 70, ruling R70.30): the side that ambushed there this CCPh, which no Location holding a vehicle had before the pass. A11.62
        // (ruling R70.31): the vehicle's own attack takes the FP of the Passengers who join it, as Rules reads their part.
        var joining = byVehicle ? JoiningPassengers(vehicle, state.Passengers(vehicle.Id)) : [];
        var passengers = joining.Count == 0 ? null : PassengerUnits(state, joining);
        return ((Is(vehicle, Conditions.Abandoned) && !byVehicle ? facts with
        {
            Unmanned = true
        } : facts) with
        {
            Ambusher = state.CloseCombats.FirstOrDefault(item => item.Location == location)?.Ambusher,
            Passengers = passengers,
        }, null);
    }

    /// <summary>
    /// The PRC aboard a vehicle who join its own CC attack on Infantry (A11.62; pass 70, ruling R70.31), as Rules reads each one's part: a Passenger of a
    /// halftrack while it is CE, of an unarmored vehicle always; a Rider attacks apart, and a broken or captured unit never.
    /// </summary>
    public static IReadOnlyList<string> JoiningPassengers(UnitInstance vehicle, IEnumerable<UnitInstance> aboard)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(aboard);
        var unarmored = LiveFire.UnarmoredOf(vehicle.Definition?.Definition) == true;
        var exposed = ScenarioA1PassengerActionRules.PassengersExposed(unarmored, LiveFire.CrewExposed(vehicle));
        var mobile = !Is(vehicle, Conditions.Immobilized) && !Is(vehicle, Conditions.Bogged);
        return [.. aboard.Where(passenger => passenger.Status == InstanceStatus.Active && !Is(passenger, Conditions.Broken) && !Is(passenger, Conditions.Captured)
                && ScenarioA1PassengerActionRules.CloseCombat(passenger.Position is ContainedPosition { Role: ContainmentRole.Rider } ? FirePrc.Rider : FirePrc.Passenger, !unarmored, exposed,
                    mobile).Part == ScenarioA1PassengerActionRules.Attacks)
            .Select(passenger => passenger.Id).Order(StringComparer.Ordinal)];
    }

    /// <summary>The Passengers who join a vehicle's attack (A11.62; ruling R70.31), as the package reads a CC unit.</summary>
    private static IReadOnlyList<CloseCombatUnit>? PassengerUnits(GameState state, IReadOnlyList<string> ids) =>
        ids.Select(state.Unit).OfType<UnitInstance>().Where(unit => unit.Definition is not null).ToArray() is { Length: > 0 } found
            ? [.. found.Select(unit => ScenarioA1CloseCombatFactBuilders.Unit(unit.Id, unit.Definition!.Definition, unit.Side, Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned),
                Is(unit, Conditions.Wounded), Is(unit, Conditions.Disrupted), Is(unit, Conditions.Berserk), Is(unit, Conditions.Fanatic), Is(unit, Conditions.Heroic),
                Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), Is(unit, Conditions.Captured), false, Is(unit, Conditions.Melee), null, null, Is(unit, Conditions.Cx),
                Is(unit, "asl:ti"), Is(unit, Conditions.Unarmed), unit.Custodian, null, []))]
            : null;

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

            // A21.2 (pass 65, ruling R65.22): the capture declaration and the unmanned fact are the record's; a record made before the pass has neither.
            Capture = recorded.Capture,
            Unmanned = recorded.Unmanned,

            // A11.62 (pass 70, ruling R70.31): a record made before the pass names no Passengers joining, and its attack is replayed with the vehicle's
            // armament alone; a record since must name the state's.
            Passengers = recorded.Passengers is null ? null : expected.Passengers,
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

        // Pass 40: whether a crew mans its Gun (A5.5) is taken as recorded, so a record made before the pass, which lacks the fact, replays.
        var manning = recorded.Units.Where(unit => unit.UnitId is not null).ToDictionary(unit => unit.UnitId!, unit => unit.Manning, StringComparer.Ordinal);

        // Pass 70 (ruling R70.27): a record made before the pass lists no Guns, so its Gun loss is not rolled and the state's Guns are not asked of it; a
        // record since the pass must list the state's.
        var recordsGuns = recorded.Units.Any(unit => unit.Guns is not null);
        expected = expected with
        {
            Units = expected.Units is null ? null : [.. expected.Units.Select(unit => unit with
            {
                Manning = unit.UnitId is { } id ? manning.GetValueOrDefault(id) : null,
                Guns = recordsGuns ? unit.Guns : null,
            })]
        };
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
