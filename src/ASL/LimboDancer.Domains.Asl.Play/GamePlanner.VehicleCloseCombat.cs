using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// CC with vehicles (backlog pass 11, rulings R11.13 to R11.16): a DEFENDER unit's CC Reaction Fire at a vehicle in its Location in the MPh, after any
/// PAATC (D7.21, A11.6); and in the CCPh, the sequential CC of a Location holding a vehicle (A11.31): Infantry attack the vehicle (A11.5), the vehicle
/// attacks Infantry (A11.62), a side passes, and an unarmed vehicle alone with enemy Infantry is captured (A11.52).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>Whether a vehicle has a CC attack (A11.62): a CMG not malfunctioned, or an AAMG while CE, with its crew able to fight.</summary>
    private static bool VehicleCcArmament(UnitInstance vehicle) =>
        VehicleDefinition(vehicle) is { } definition && !Is(vehicle, Conditions.Stunned) && !Is(vehicle, Conditions.Shocked) && !Is(vehicle, Conditions.UnconfirmedKill)
        && !Is(vehicle, Conditions.Abandoned) && !Is(vehicle, Conditions.Recalled) && !Is(vehicle, Conditions.Captured)
        && ((definition.CoaxialMg is not null && !Is(vehicle, Conditions.CmgMalfunctioned))
            || (definition.AntiAircraftMg is not null && LiveFire.CrewExposed(vehicle) && !(definition.MainArmament == "aamg" && (Is(vehicle, Conditions.Malfunctioned) || Is(vehicle, Conditions.Disabled)))));

    /// <summary>Whether an Infantry unit may make a CC attack in the CCPh (A11.16, A20.5): active, unbroken, and not a prisoner.</summary>
    private bool CcAttacker(UnitInstance unit) =>
        unit.Status == InstanceStatus.Active && !LiveFire.IsVehicle(unit) && vocabulary.IsA(unit.Kind, "asl:personnel") && !Is(unit, Conditions.Broken)
        && !Is(unit, Conditions.Captured) && unit.Kind != UnitKinds.Dummy;

    /// <summary>
    /// The sides with an attack left in a CC Location holding a vehicle (ruling R11.16): Infantry that have not attacked and face an enemy vehicle, and
    /// vehicles with CC armament that have not attacked and face enemy Infantry; a side that passed has none.
    /// </summary>
    private HashSet<string> SidesWithAttacks(GameState state, BoardLocation location, IReadOnlyCollection<string> attacked, IReadOnlyCollection<string> passed)
    {
        var here = state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active).ToArray();
        var sides = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in here.Where(unit => !attacked.Contains(unit.Id) && !passed.Contains(unit.Side)))
        {
            var enemies = here.Where(other => other.Side != unit.Side && !Is(other, Conditions.Captured)).ToArray();
            if (LiveFire.IsVehicle(unit) ? VehicleCcArmament(unit) && enemies.Any(other => !LiveFire.IsVehicle(other) && CcAttacker(other) || (!LiveFire.IsVehicle(other) && vocabulary.IsA(other.Kind, "asl:personnel")))
                : CcAttacker(unit) && enemies.Any(LiveFire.IsVehicle))
            {
                sides.Add(unit.Side);
            }
        }

        return sides;
    }

    /// <summary>
    /// The side that attacks first in a CC Location holding a vehicle (A11.31): the non-vehicular side, or with vehicles on both sides the ATTACKER.
    /// </summary>
    private static string FirstCcSide(GameState state, BoardLocation location)
    {
        var sides = state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured)).ToArray();
        var vehicular = sides.Where(LiveFire.IsVehicle).Select(unit => unit.Side).Distinct(StringComparer.Ordinal).ToArray();
        return vehicular.Length == 1 ? sides.Select(unit => unit.Side).FirstOrDefault(side => side != vehicular[0]) ?? state.PhasingSide! : state.PhasingSide!;
    }

    /// <summary>
    /// After an attack or a pass (A11.31; ruling R11.16): the side to attack next, the other side while it has attacks left, else the same side, and
    /// whether the Location's CC is over.
    /// </summary>
    private (string? Next, bool Closed) NextCcSide(GameState after, BoardLocation location, string moved, IReadOnlyCollection<string> attacked, IReadOnlyCollection<string> passed)
    {
        var left = SidesWithAttacks(after, location, attacked, passed);
        var other = left.FirstOrDefault(side => side != moved);
        return other is not null ? (other, false) : left.Contains(moved) ? (moved, false) : (null, true);
    }

    /// <summary>
    /// A berserk unit (of <paramref name="side"/>, or of either side) that still owes an attack on a Known enemy vehicle in a Location's sequential CC
    /// this CCPh (A15.43, A11.31; ruling R27.2): it may attack, has not, and the Location holds no Gun's crew, whose CC is not built. Null when none does.
    /// </summary>
    private UnitInstance? BerserkOwingVehicleAttack(GameState state, BoardLocation location, string? side)
    {
        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        if (state.Phase != "ccph" || entry is { Closed: true } || !VehicleCloseCombatTurn(state, location).Open)
        {
            return null;
        }

        var here = state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active).ToArray();
        return here.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew")) ? null
            : here.Where(unit => (side is null || unit.Side == side) && Is(unit, Conditions.Berserk) && CcAttacker(unit) && entry?.Attacking.Contains(unit.Id) != true
                && here.Any(other => other.Side != unit.Side && LiveFire.IsVehicle(other) && KnownEnemy(other) && !Is(other, Conditions.Captured)))
                .OrderBy(unit => unit.Id, StringComparer.Ordinal).FirstOrDefault();
    }

    /// <summary>
    /// The Location of a berserk Infantry OVR onto a lone SMC whose CC is still to be resolved this MPh (A4.152, A15.432; ruling R27.3): the charging
    /// stack, with a berserk MMC (A15.432, A4.15: a SMC alone does not OVR; referee and table player, pass 27), has entered the Location of the charge,
    /// the DEFENDER's window on that entry has closed, the only enemy unit there is a Known SMC not held in Melee, and the Close Combat package
    /// accepts the CC (otherwise the units are simply left together for the CCPh, so the MPh never stalls; table player, pass 27). Null when there
    /// is none.
    /// </summary>
    public BoardLocation? BerserkOverrunPending(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Phase != "mph" || state.Movement is not { WindowOpen: false, Charge: { } charged } movement || movement.Location != charged
            || state.CloseCombats.Any(item => item.Location == charged))
        {
            return null;
        }

        var here = state.At(charged).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured)).ToArray();
        var enemies = here.Where(unit => unit.Side != state.PhasingSide).ToArray();
        if (!here.Any(unit => unit.Side == state.PhasingSide && movement.Members.Contains(unit.Id, StringComparer.Ordinal) && Is(unit, Conditions.Berserk)
            && vocabulary.IsA(unit.Kind, "asl:mmc")) || enemies is not [{ } smc] || !vocabulary.IsA(smc.Kind, "asl:smc") || !KnownEnemy(smc) || Is(smc, Conditions.Melee))
        {
            return null;
        }

        var terrain = ReadLocation(state, charged) is { } read ? TerrainKey(read) : null;
        var (facts, _) = LiveCloseCombat.FromState(state, charged, terrain, OverrunAttacks(state, charged, smc), null, overrun: true);
        return facts is not null && ScenarioA1CloseCombatCalculator.Precheck(facts, CloseCombatReference.Value) is { Count: 0 } ? charged : null;
    }

    /// <summary>
    /// The attacks of a berserk OVR's CC (A4.152, A15.432; ruling R27.3): every berserk unit of the phasing side there attacks the SMC, and the SMC attacks
    /// them back when it may attack in CC (not broken or captured), which never costs it anything.
    /// </summary>
    public IReadOnlyList<CloseCombatDeclaration> OverrunAttacks(GameState state, BoardLocation location, UnitInstance smc)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(smc);
        string[] berserk = [.. state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide
            && Is(unit, Conditions.Berserk)).Select(unit => unit.Id).Order(StringComparer.Ordinal)];
        return CcAttacker(smc) ? [new CloseCombatDeclaration(berserk, [smc.Id]), new CloseCombatDeclaration([smc.Id], berserk)] : [new CloseCombatDeclaration(berserk, [smc.Id])];
    }

    /// <summary>The sides and units a CC Location holding a vehicle waits for, for the Play page (ruling R11.16).</summary>
    public (string? Next, bool Open) VehicleCloseCombatTurn(GameState state, BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(state);
        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        if (state.Phase != "ccph" || entry is { Closed: true } || !state.At(location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)))
        {
            return (null, false);
        }

        var left = SidesWithAttacks(state, location, entry?.Attacking ?? [], entry?.Passed ?? []);
        var next = entry?.Next ?? FirstCcSide(state, location);
        return left.Count == 0 ? (null, false) : (left.Contains(next) ? next : left.First(), true);
    }

    private GamePlan PlanVehicleCloseCombat(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "location", out var locationText) || !BoardLocation.TryParse(locationText, out var location))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: CC names its Location");
        }

        var attackers = Strings(arguments, "attackers").ToArray();
        var defenders = Strings(arguments, "defenders").ToArray();
        var vehicleId = Text(arguments, "vehicleId", out var named) ? named : null;
        var pass = Flag(arguments, "pass");
        return state.Phase switch
        {
            "mph" => PlanReactionFire(scope, existing, attemptId, expected, label, actor, state, location, attackers, vehicleId),
            "ccph" => PlanSequentialCloseCombat(scope, existing, attemptId, expected, label, actor, state, location, attackers, defenders, vehicleId, pass),
            _ => Refused(scope, label, expected, "play.cc-vehicle-phase: CC with a vehicle is made in the CCPh, or as CC Reaction Fire in the MPh (A11.5, D7.21)"),
        };
    }

    /// <summary>
    /// CC Reaction Fire (D7.21, D7.213; ruling R11.13): in the DEFENDER's window after the moving vehicle's MP expenditure in their Location, one
    /// unbroken, unpinned, armed DEFENDER unit not in Melee, alone or with a SMC, attacks it, after a PAATC against a manned AFV unless exempt or passed
    /// this phase; units marked Final Fire (FPF CC Reaction Fire) are not built.
    /// </summary>
    private GamePlan PlanReactionFire(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, BoardLocation location, string[] attackers, string? vehicleId)
    {
        if (state.Movement is not { WindowOpen: true, Vehicle: true, Members: [{ } moving] } window || state.Unit(moving) is not { } vehicle
            || state.Location(moving)?.Location != location || (vehicleId is not null && vehicleId != moving))
        {
            return Refused(scope, label, expected, "play.reaction-fire: CC Reaction Fire attacks the moving vehicle in the attackers' Location while the DEFENDER's window on its MP expenditure is open (D7.2, D7.21)");
        }

        // D7.1, D7.2 (referee, pass 11): Reaction Fire at an OVRing vehicle comes after the OVR is resolved.
        if (window.Overrun is not null)
        {
            return Refused(scope, label, expected, "play.reaction-fire: the OVR is resolved first; CC Reaction Fire follows it (D7.1, D7.2)");
        }

        var units = attackers.Select(state.Unit).ToArray();
        if (units.Length is < 1 or > 2 || units.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side == vehicle.Side || state.Location(unit.Id)?.Location != location
            || LiveFire.IsVehicle(unit) || Is(unit, Conditions.Broken) || Is(unit, Conditions.Pinned) || Is(unit, Conditions.Captured) || Is(unit, Conditions.Melee)))
        {
            return Refused(scope, label, expected, $"play.reaction-fire: the attackers are one or two unbroken, unpinned, armed DEFENDER units in {location}, not in Melee (D7.21)");
        }

        if (units.FirstOrDefault(unit => Is(unit!, Conditions.FinalFire)) is { } finalFire)
        {
            return Refused(scope, label, expected, $"play.reaction-fire: {finalFire.Id} is marked Final Fire; FPF CC Reaction Fire is not built (D7.212; ruling R11.13)");
        }

        return PlanVehicleAttack(scope, existing, attemptId, expected, label, actor, state, location, vehicle, [.. units.Select(unit => unit!)], true);
    }

    /// <summary>
    /// A11.52 (ruling R11.16): at the start of the CCPh an unarmed vehicle, not in Motion, with no Personnel of its side in its Location but enemy
    /// Infantry there, is captured: it passes out of play for its side as an Abandoned vehicle, since the use of captured vehicles is not built.
    /// </summary>
    private IEnumerable<UnitInstance> CapturedVehicles(GameState state) => state.Units.Where(vehicle => vehicle.Status == InstanceStatus.Active && LiveFire.IsVehicle(vehicle)
        && !Is(vehicle, Conditions.Captured) && !Is(vehicle, Conditions.Motion)
        && VehicleDefinition(vehicle) is { MainArmament: null, AntiAircraftMg: null, CoaxialMg: null, BowMg: null }
        && state.Location(vehicle.Id)?.Location is { } at
        && !state.At(at).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side == vehicle.Side && !LiveFire.IsVehicle(unit) && !Is(unit, Conditions.Captured))
        && state.At(at).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != vehicle.Side && CcAttacker(unit)))
        .OrderBy(vehicle => vehicle.Id, StringComparer.Ordinal);

    /// <summary>
    /// The CCPh in a Location holding a vehicle (A11.31; rulings R11.14 to R11.16): the side named attacks once, Infantry against an enemy vehicle or a
    /// vehicle against enemy Infantry, or passes. Infantry against Infantry there is not built.
    /// </summary>
    private GamePlan PlanSequentialCloseCombat(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, BoardLocation location, string[] attackers, string[] defenders, string? vehicleId, bool pass)
    {
        var (next, open) = VehicleCloseCombatTurn(state, location);
        if (!open || next is null)
        {
            return Refused(scope, label, expected, $"play.cc-vehicle: {location} holds no vehicle CC left this CCPh (A11.31)");
        }

        if (state.CloseCombats.Any(item => !item.Closed && item.Location != location))
        {
            return Refused(scope, label, expected, "play.cc-vehicle: another Location's CC is open; one Location at a time (A11.12)");
        }

        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        var attacked = entry?.Attacking ?? [];
        var passed = entry?.Passed ?? [];
        if (pass)
        {
            // A15.43 (ruling R27.2): a side with a berserk unit facing a Known enemy vehicle there does not pass; the berserk unit attacks.
            if (BerserkOwingVehicleAttack(state, location, next) is { } owing)
            {
                return Refused(scope, label, expected, $"play.cc-vehicle-berserk: {owing.Id} is berserk and attacks the enemy vehicle in {location}; its side does not pass (A15.43)");
            }

            var (following, closed) = NextCcSide(state, location, next, attacked, [.. passed, next]);
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
                [Event(scope, attemptId, 1, expected, "vehicle-close-combat-passed", new VehicleCloseCombatPassed(location, next, following, closed), null, null)],
                [$"play.cc-vehicle: the {next} side passes in {location} and makes no more attacks there (A11.31)"]);
        }

        if (vehicleId is null || state.Unit(vehicleId) is not { Status: InstanceStatus.Active } vehicle || !LiveFire.IsVehicle(vehicle) || state.Location(vehicle.Id)?.Location != location)
        {
            return Refused(scope, label, expected, "play.cc-vehicle: a CC attack here names the vehicle attacking or attacked; CC between Infantry in a Location holding a vehicle is not built (ruling R11.16)");
        }

        if (defenders.Length > 0)
        {
            // A11.62 (ruling R11.15): the vehicle attacks enemy Infantry in its Location.
            if (vehicle.Side != next || attacked.Contains(vehicle.Id))
            {
                return Refused(scope, label, expected, $"play.cc-vehicle-order: the {next} side attacks next, and each unit attacks once (A11.31)");
            }

            return PlanVehicleAttacksInfantry(scope, existing, attemptId, expected, label, actor, state, location, vehicle, defenders, attacked, passed);
        }

        var units = attackers.Select(state.Unit).ToArray();
        if (units.Length is < 1 or > 2 || units.Any(unit => unit is not { } found || !CcAttacker(found) || found.Side != next || found.Side == vehicle.Side
            || state.Location(found.Id)?.Location != location || attacked.Contains(found.Id)))
        {
            return Refused(scope, label, expected, $"play.cc-vehicle-order: the {next} side attacks next, with one or two of its unbroken Infantry units in {location} that have not attacked (A11.31, A11.5)");
        }

        return PlanVehicleAttack(scope, existing, attemptId, expected, label, actor, state, location, vehicle, [.. units.Select(unit => unit!)], false);
    }

    /// <summary>The Infantry of a CC attack on a vehicle, with each unit's Inexperience as A19.2 reads it (ruling R11.14).</summary>
    private VehicleCloseCombatFacts WithInexperience(GameState state, VehicleCloseCombatFacts facts) => facts with
    {
        Units = [.. facts.Units!.Select(unit => (state.Unit(unit.UnitId!) is { } found ? Experience.Inexperienced(state, found, catalogs, vocabulary) : ConditionState.Unknown) switch
        {
            ConditionState.True => unit with { Inexperienced = true },
            ConditionState.False => unit with { Inexperienced = false },
            _ => unit,
        })],
    };

    /// <summary>
    /// Infantry attack a vehicle (A11.5; rulings R11.13, R11.14): in CC Reaction Fire a MMC takes its PAATC first against a manned AFV (a failure pins
    /// it and ends the attack); the package resolves the attack; a destroyed vehicle leaves a wreck (A11.611: its crew does not survive), an
    /// immobilized one loses Motion; the attackers of CC Reaction Fire are marked First Fire (Final Fire when already First Fire) and, while the vehicle
    /// survives, with a CC counter.
    /// </summary>
    private GamePlan PlanVehicleAttack(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, BoardLocation location, UnitInstance vehicle, IReadOnlyList<UnitInstance> units, bool reaction)
    {
        var (facts, reason) = LiveCloseCombat.VehicleFromState(state, location, vehicle.Id, [.. units.Select(unit => unit.Id)], [], false, reaction);
        if (facts is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        facts = WithInexperience(state, facts);
        var reference = CloseCombatReference.Value;
        var precheck = ScenarioA1VehicleCloseCombat.Precheck(facts, reference);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, RefusalReasons.Refusal("play.cc-vehicle-refused", "Close Combat", "attack", precheck));
        }

        var testing = reaction ? units.Where(unit => NeedsPaatc(state, unit, vehicle)).ToArray() : [];
        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        var package = ScenarioA1CloseCombatPackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            if (testing.Length > 0)
            {
                AddPaatc(scope, attemptId, expected, actor, state, testing, vehicle, location, events, draw, out var passed);
                if (!passed)
                {
                    return events;
                }
            }

            var rollIds = new Dictionary<string, string>(StringComparer.Ordinal);
            var rolls = new VehicleCloseCombatRolls(null);
            VehicleCloseCombatResolution resolution;
            while ((resolution = ScenarioA1VehicleCloseCombat.Resolve(facts with
            {
                Rolls = rolls
            }, reference)).Disposition != CloseCombatResolution.Resolved)
            {
                if (resolution.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.cc-vehicle.roll-missing:", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The Close Combat package left an attack on a vehicle it had accepted undecided: " + string.Join("; ", resolution.Reasons));
                }

                var key = missing["asl.a1.cc-vehicle.roll-missing:".Length..];
                var split = key.IndexOf(':', StringComparison.Ordinal);
                var (kind, rest) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
                var count = kind == "attack" ? 2 : 1;
                var drawn = draw(new RollRequest(count, 6));
                var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
                rollIds[key] = rollId;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(rollId, "cc-vehicle-" + kind, count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
                rolls = kind switch
                {
                    "attack" => rolls with { Attack = drawn.Values },
                    "unlikelyKill" => rolls with { UnlikelyKill = drawn.Values[0] },
                    _ => rolls with { WoundSeverity = With(rolls.WoundSeverity, rest, drawn.Values[0]) },
                };
            }

            var destroyed = resolution.VehicleResult is VehicleCloseCombatResolution.Eliminated or VehicleCloseCombatResolution.BurningWreck;
            var (following, closed) = reaction ? (null, false) : NextAfter(state, location, vehicle, units, destroyed, entry);
            var recordId = EventId(attemptId, events.Count + 1);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "vehicle-close-combat-resolved",
                new VehicleCloseCombatResolved(location, vehicle.Id, [.. units.Select(unit => unit.Id)], [], false, reaction, rollIds,
                    JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json))
                {
                    Next = following,
                    Closed = closed,
                }, package, null));
            if (destroyed)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "vehicle-wrecked", new VehicleWrecked(vehicle.Id, resolution.VehicleResult == VehicleCloseCombatResolution.BurningWreck),
                    package, null, [recordId]));
            }
            else if (resolution.VehicleResult == VehicleCloseCombatResolution.Immobilized)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(vehicle.Id,
                    new Dictionary<string, ConditionState> { [Conditions.Immobilized] = ConditionState.True, [Conditions.Motion] = ConditionState.False }), package, null, [recordId]));
            }

            foreach (var (type, payload) in CloseCombatEffects(state, location, new CloseCombatResolution(CloseCombatResolution.Resolved, [], [], resolution.Effects, [], []), attemptId, []))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, [recordId]));
            }

            // D7.21: the attackers of CC Reaction Fire carry a First Fire counter (Final Fire when already marked First Fire), and a CC counter while the
            // vehicle survives.
            if (reaction)
            {
                foreach (var unit in units.Where(unit => !resolution.Effects.Any(effect => effect.UnitId == unit.Id && (effect.Eliminated || effect.FinalDefinitionId != effect.DefinitionId))))
                {
                    var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
                    if (Is(unit, Conditions.FirstFire))
                    {
                        conditions[Conditions.FinalFire] = ConditionState.True;
                        conditions[Conditions.FirstFire] = ConditionState.False;
                    }
                    else
                    {
                        conditions[Conditions.FirstFire] = ConditionState.True;
                    }

                    if (!destroyed)
                    {
                        conditions[Conditions.CcReaction] = ConditionState.True;
                    }

                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.Id, conditions), package, null, [recordId]));
                }
            }

            return events;
        }

        var paatc = testing.Length > 0 ? $"; {string.Join(", ", testing.Select(unit => unit.Id))} first pass{(testing.Length == 1 ? "es" : string.Empty)} a PAATC (A11.6)" : string.Empty;
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.cc-vehicle: {string.Join(" and ", units.Select(unit => unit.Id))} attack{(units.Count == 1 ? "s" : string.Empty)} {vehicle.Id} in CC{(reaction ? " as CC Reaction Fire (D7.21)" : " (A11.5)")}{paatc}"])
        {
            Roll = new PlannedRoll("cc-vehicle", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>The next side after an attack on a vehicle, reading a destroyed vehicle as gone (ruling R11.16).</summary>
    private (string? Next, bool Closed) NextAfter(GameState state, BoardLocation location, UnitInstance vehicle, IReadOnlyList<UnitInstance> attackers, bool destroyed,
        CloseCombatLocation? entry)
    {
        var after = destroyed ? state with
        {
            Units = [.. state.Units.Select(unit => unit.Id == vehicle.Id ? unit with { Status = InstanceStatus.Wrecked } : unit)]
        } : state;
        return NextCcSide(after, location, attackers[0].Side, [.. entry?.Attacking ?? [], .. attackers.Select(unit => unit.Id)], entry?.Passed ?? []);
    }

    /// <summary>
    /// A vehicle's CC attack on Infantry (A11.62; ruling R11.15): the package resolves it on the CCT; its effects follow as Infantry CC effects.
    /// </summary>
    private GamePlan PlanVehicleAttacksInfantry(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, BoardLocation location, UnitInstance vehicle, string[] defenders, IReadOnlyList<string> attacked, IReadOnlyList<string> passed)
    {
        var (facts, reason) = LiveCloseCombat.VehicleFromState(state, location, vehicle.Id, [], defenders, true, false);
        if (facts is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        facts = WithInexperience(state, facts);
        var reference = CloseCombatReference.Value;
        var precheck = ScenarioA1VehicleCloseCombat.Precheck(facts, reference);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, RefusalReasons.Refusal("play.cc-vehicle-refused", "Close Combat", "attack", precheck));
        }

        var package = ScenarioA1CloseCombatPackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            var rollIds = new Dictionary<string, string>(StringComparer.Ordinal);
            var rolls = new VehicleCloseCombatRolls(null);
            VehicleCloseCombatResolution resolution;
            while ((resolution = ScenarioA1VehicleCloseCombat.Resolve(facts with
            {
                Rolls = rolls
            }, reference)).Disposition != CloseCombatResolution.Resolved)
            {
                if (resolution.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.cc-vehicle.roll-missing:", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The Close Combat package left a vehicle's attack it had accepted undecided: " + string.Join("; ", resolution.Reasons));
                }

                var key = missing["asl.a1.cc-vehicle.roll-missing:".Length..];
                var split = key.IndexOf(':', StringComparison.Ordinal);
                var (kind, rest) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
                var selected = kind == "randomSelection" ? rest.Split(',') : [];
                var count = kind switch
                {
                    "attack" => 2,
                    "randomSelection" => selected.Length,
                    _ => 1,
                };
                var drawn = draw(new RollRequest(count, 6));
                var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
                rollIds[key] = rollId;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(rollId, "cc-vehicle-" + kind, count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
                rolls = kind switch
                {
                    "attack" => rolls with { Attack = drawn.Values },
                    "randomSelection" => rolls with
                    {
                        RandomSelection = selected.Select((id, index) => (id, index)).Aggregate(rolls.RandomSelection, (map, pair) => With(map, pair.id, drawn.Values[pair.index])),
                    },
                    _ => rolls with { WoundSeverity = With(rolls.WoundSeverity, rest, drawn.Values[0]) },
                };
            }

            var after = state with
            {
                Units = [.. state.Units.Select(unit => resolution.Effects.FirstOrDefault(effect => effect.UnitId == unit.Id) is { Eliminated: true } ? unit with { Status = InstanceStatus.Eliminated } : unit)],
            };
            var (following, closed) = NextCcSide(after, location, vehicle.Side, [.. attacked, vehicle.Id], passed);
            var recordId = EventId(attemptId, events.Count + 1);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "vehicle-close-combat-resolved",
                new VehicleCloseCombatResolved(location, vehicle.Id, [vehicle.Id], defenders, true, false, rollIds,
                    JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json))
                {
                    Next = following,
                    Closed = closed,
                }, package, null));
            foreach (var (type, payload) in CloseCombatEffects(state, location, new CloseCombatResolution(CloseCombatResolution.Resolved, [], [], resolution.Effects, [], []), attemptId, []))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, [recordId]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.cc-vehicle: {vehicle.Id} attacks {string.Join(", ", defenders)} in CC with its CC armament (A11.62)"])
        {
            Roll = new PlannedRoll("cc-vehicle", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }
}
