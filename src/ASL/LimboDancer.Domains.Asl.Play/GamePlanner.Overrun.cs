using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// A vehicle's OVR and the PAATC (backlog pass 11, rulings R11.11 to R11.13): the OVR declared with an entry, or in the vehicle's Location after the
/// A12.41 choice there, resolved by the Fire package once the DEFENDER's window on it closes, and followed by the DEFENDER's Reaction Fire window
/// (D7.1, D7.2); the reveal or combined PAATC of concealed Personnel a vehicle enters (A12.41); and the PAATC of Infantry attacking or advancing on an
/// AFV (A11.6, D7.21).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>D7.1 (ruling R11.11): an OVR costs a quarter of the vehicle's printed MP allotment (FRU), in half MP.</summary>
    private static int OverrunHalfMp(UnitInstance vehicle) => (((VehicleDefinition(vehicle)?.MovementPoints ?? 0) + 3) / 4) * 2;

    /// <summary>Whether a Location holds an enemy unit the vehicle's side can see (A12.41): a unit neither concealed nor hidden.</summary>
    private static bool KnownTargets(GameState state, UnitInstance vehicle, BoardLocation at) =>
        state.At(at).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != vehicle.Side && !Is(unit, Conditions.Captured)
            && unit.Kind != UnitKinds.Dummy && VisibleTo(unit, vehicle.Side));

    /// <summary>
    /// Why a vehicle may not OVR a Location (D7.1, D7.12 to D7.14; ruling R11.11), or null: not in Reverse or VBM, not after its own Bounding First Fire
    /// (an earlier OVR aside), with an enemy unit there to attack, none of them in Melee and no enemy vehicle there in Motion.
    /// </summary>
    private static string? OverrunBar(GameState state, IReadOnlyList<GameEvent> existing, UnitInstance vehicle, BoardLocation at, bool reverse, bool bypass)
    {
        if (reverse)
        {
            return "play.move-vehicle-ovr: no OVR is made in Reverse (D7.13)";
        }

        if (bypass)
        {
            return "play.move-vehicle-ovr: no OVR is made from VBM (D7.13; ruling R11.11)";
        }

        var overrunsThisPhase = ThisPhase(existing).Select(item => item.Payload).OfType<OverrunResolved>().Where(item => item.Vehicle == vehicle.Id).ToArray();
        if (Is(vehicle, Conditions.BoundingFire) && overrunsThisPhase.Length == 0)
        {
            return $"play.move-vehicle-ovr: {vehicle.Id} is marked Bounding Fire from its own fire, so it makes no OVR (D7.1, D7.13)";
        }

        var enemies = state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != vehicle.Side && !Is(unit, Conditions.Captured)).ToArray();
        if (enemies.Length == 0)
        {
            return $"play.move-vehicle-ovr: {at} holds no enemy unit to OVR (D7.1)";
        }

        if (enemies.All(unit => IsAfv(unit) && !LiveFire.CrewExposed(unit)))
        {
            return $"play.move-vehicle-ovr: an AFV is not OVR, and {at} holds nothing else it could attack (D7.12)";
        }

        if (enemies.Any(unit => Is(unit, Conditions.Melee)))
        {
            return $"play.move-vehicle-ovr: an OVR of units held in Melee is not reviewed (ruling R11.11)";
        }

        if (enemies.Any(unit => LiveFire.IsVehicle(unit) && Is(unit, Conditions.Motion)))
        {
            return "play.move-vehicle-ovr: an OVR of a Location holding an enemy vehicle in Motion, with its +2 against the vehicle's PRC, is not built (D7.12)";
        }

        return null;
    }

    /// <summary>
    /// D7.1, A12.41 (ruling R11.12): the OVR declared in the vehicle's own Location, after its entry revealed the concealed units there or they passed their
    /// PAATC, as its next expenditure there.
    /// </summary>
    private GamePlan PlanDeclareOverrun(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, UnitInstance vehicle, BoardLocation at, bool moving, int step)
    {
        var steps = StepsThisPhase(existing, vehicle.Id);
        if (!moving || steps.LastOrDefault() is not { Kind: VehicleStepped.Enter, Overrunning: false } last || last.At != at || last.Straddling is not null
            || !ThisPhase(existing).Select(item => item.Payload).OfType<ChoiceMade>().Any(made => made.Key == PaatcKey(vehicle.Id, at)))
        {
            return Refused(scope, label, expected, $"play.move-vehicle-ovr: {vehicle.Id} declares an OVR in its own Location only right after entering one that held no Known enemy unit and revealed or tested its concealed units (A12.41, D7.1)");
        }

        if (OverrunBar(state, existing, vehicle, at, last.Reverse, false) is { } bar)
        {
            return Refused(scope, label, expected, bar);
        }

        var (spent, allotment) = HalfMp(vehicle);
        var cost = OverrunHalfMp(vehicle);
        if (spent + cost > allotment)
        {
            return Refused(scope, label, expected, $"play.move-vehicle-mp: {vehicle.Id} has {Mp(Math.Max(0, allotment - spent))} MP left, and an OVR costs {Mp(cost)} (D7.1)");
        }

        GameEvent[] declared = [Event(scope, attemptId, 1, expected, "vehicle-step", new VehicleStepped(vehicle.Id, VehicleStepped.Overrun, at, null, cost, step) { Overrunning = true }, null, null)];
        if (OverrunDeclarationBar(existing, declared, vehicle.Id, at) is { } unresolvable)
        {
            return Refused(scope, label, expected, unresolvable);
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, declared,
            [$"play.move-vehicle-ovr: {vehicle.Id} declares an OVR of {at} for {Mp(cost)} MP; the DEFENDER may fire first, then the OVR is resolved (A12.41, D7.1)"]);
    }

    /// <summary>
    /// The OVR's resolution (D7.1 to D7.17; ruling R11.11), once the DEFENDER's window on its declaration closes: the Fire package attacks every enemy
    /// unit in the Location with the vehicle's OVR FP, then the DEFENDER's Reaction Fire window opens (D7.2).
    /// </summary>
    private GamePlan PlanOverrun(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (state.Movement is not { Vehicle: true, Overrun: { } at } movement || movement.Movers is not [{ } id])
        {
            return Refused(scope, label, expected, "play.overrun: no OVR is declared (D7.1)");
        }

        if (Text(arguments, "vehicleId", out var named) && named != id)
        {
            return Refused(scope, label, expected, $"play.overrun: the declared OVR is {id}'s");
        }

        if (movement.WindowOpen)
        {
            return Refused(scope, label, expected, "play.overrun: the DEFENDER may still fire at the OVR's MP expenditure; the OVR is resolved when he passes (D7.1)");
        }

        var (attack, refusal, terrain, wall, smoke, targetSide) = OverrunAttack(state, movement, id, at);
        if (attack is null)
        {
            return Refused(scope, label, expected, refusal!);
        }

        var facts = attack;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            AddFireEvents(scope, attemptId, expected, actor, state, facts, targetSide, null, events, draw);
            AddOverrunResolved(scope, attemptId, expected, state, facts, events);
            return events;
        }

        var fp = ScenarioA1FireCalculator.OverrunWeapons(facts.Overrun!, FireReference.Value.Definitions[facts.Overrun!.DefinitionId!]);
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.overrun: {id} OVRs {at} ({terrain}{(wall is null ? string.Empty : $", {wall.Terrain} crossed")}, SMOKE {smoke}) with its OVR FP"
                + (fp.Count == 0 ? string.Empty : $" and {string.Join(", ", fp.Select(item => item.Weapon))}") + " (D7.11)"])
        {
            Roll = new PlannedRoll("overrun", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// The facts of a declared OVR (D7.1, D7.15; ruling R11.11), read from the state after its declaration, and the Fire package's pre-check of them;
    /// the declaration is refused on the same reasons as the resolution would be, so a declared OVR always resolves (table-player finding).
    /// </summary>
    private (FireAttack? Attack, string[]? Refusal, string Terrain, FireHexsideTem? Wall, int Smoke, string TargetSide) OverrunAttack(GameState state, MovementState movement,
        string id, BoardLocation at)
    {
        if (LiveFire.OverrunFromState(state, id) is not ({ } attack, null) || state.Unit(id) is not { } vehicle)
        {
            return (null, ["play.overrun: the OVR cannot be read from the game state"], string.Empty, null, 0, string.Empty);
        }

        if (ReadLocation(state, at) is not { } read || TerrainKey(read) is not { } terrain)
        {
            return (null, [$"play.overrun: {at}'s terrain is not one the Fire package reviews"], string.Empty, null, 0, string.Empty);
        }

        // A24.2 (ruling R9.6): SMOKE in the Location hinders an attack within it, +1 more than traced into it; D7.15: a wall or hedge TEM only across the
        // hexside the vehicle entered by.
        var sources = SmokeSources(state).Count(place => place.Board == at.Board && place.Hex == at.Hex);
        var smoke = sources == 0 ? 0 : Math.Min(3, 2 * sources) + 1;
        FireHexsideTem? wall = null;
        if (movement.EnteredFrom is { } from && at.Level == 0 && SideToward(state, at, from) is { } side && WallOn(HexsideAt(state, at, side)) is ("wall" or "hedge") and var kind)
        {
            wall = new FireHexsideTem(kind, ScenarioA1FireReference.HexsideTem[kind]);
        }

        var targetSide = state.Sides.First(item => item.Id != vehicle.Side).Id;
        attack = HeatOfBattleFacts(state, attack with
        {
            TargetTerrain = terrain == "grain" && state.ScenarioMonth is not (>= 6 and <= 9) ? "open-ground" : terrain,
            Los = new FireLos(false, smoke, true, false),
            HexsideTem = wall,
            AfvCover = CoverAt(state, at, targetSide),
        });
        var precheck = ScenarioA1FireCalculator.Precheck(attack, FireReference.Value);
        return precheck.Count != 0
            ? (null, RefusalReasons.Refusal("play.overrun-refused", "Fire", "OVR", precheck), terrain, wall, smoke, targetSide)
            : (attack, null, terrain, wall, smoke, targetSide);
    }

    /// <summary>The refusal of an OVR declared by the events given, read from the state after them, or null when the Fire package would resolve it.</summary>
    private string[]? OverrunDeclarationBar(IReadOnlyList<GameEvent> existing, IReadOnlyList<GameEvent> declared, string id, BoardLocation at) =>
        Replay([.. existing, .. declared]).Current is not { Movement: { Vehicle: true } movement } after ? ["play.overrun: the OVR cannot be read from the game state"]
            : OverrunAttack(after, movement, id, at).Refusal;

    /// <summary>
    /// The OVR is done once its fire record is made with no option left to answer (D7.1, D7.2): at once, or after the owner's answer resumes the attack
    /// (table-player finding). A surrender after it waits for its captor as usual.
    /// </summary>
    private void AddOverrunResolved(GameScope scope, string attemptId, long expected, GameState state, FireAttack facts, List<GameEvent> events)
    {
        if (facts.Overrun is { VehicleId: { } id } && state.Movement is { Vehicle: true, Overrun: { } at }
            && events.FirstOrDefault(item => item.Payload is FireResolved) is { } fire && !events.Any(item => item.Payload is ChoicePending))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "overrun-resolved", new OverrunResolved(id, at, fire.EventId), null, null, [fire.EventId]));
        }
    }

    /// <summary>The key of the A12.41 choice of a vehicle's entry (ruling R11.12).</summary>
    private static string PaatcKey(string vehicle, BoardLocation at) => $"paatc:{vehicle}:{at}";

    /// <summary>Whether a unit is exempt from a PAATC (A11.6): a SMC, a Fanatic unit, or a berserk one.</summary>
    private bool PaatcExempt(UnitInstance unit) =>
        vocabulary.IsA(unit.Kind, "asl:leader") || vocabulary.IsA(unit.Kind, "asl:hero") || Is(unit, Conditions.Fanatic) || Is(unit, Conditions.Berserk);

    /// <summary>
    /// The concealed or hidden enemy Personnel, and Dummies, a vehicle's entry subjects to the A12.41 choice (ruling R11.12): none exempt from a PAATC, and
    /// none when the vehicle's crew cannot see (Stunned, Shocked, or Abandoned; A12.41's "unbroken" vehicle).
    /// </summary>
    private IReadOnlyList<string> PaatcSubjects(GameState state, UnitInstance vehicle, BoardLocation at)
    {
        if (!Watching(vehicle))
        {
            return [];
        }

        return [.. state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != vehicle.Side && !LiveFire.IsVehicle(unit)
                && !Is(unit, Conditions.Captured) && (unit.Kind == UnitKinds.Dummy || Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden)) && !PaatcExempt(unit))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal).Select(unit => unit.Id)];
    }

    /// <summary>The pending A12.41 choice (ruling R11.12): the concealed units' owner reveals them or takes one combined PAATC.</summary>
    private static ChoicePending PaatcChoice(GameState state, UnitInstance vehicle, BoardLocation at, IReadOnlyList<string> units)
    {
        var resume = new JsonObject
        {
            ["record"] = "paatc",
            ["vehicle"] = vehicle.Id,
            ["location"] = at.ToString(),
            ["units"] = new JsonArray([.. units.Select(id => (JsonNode)id)]),
        };
        var owner = state.Unit(units[0])!.Side;
        return new ChoicePending(PaatcKey(vehicle.Id, at), ChoicePending.Paatc, owner, [ChoicePending.Reveal, ChoicePending.Check], JsonSerializer.SerializeToElement(resume));
    }

    /// <summary>
    /// A PAATC's Morale Level and DRM (A11.6, A12.41; rulings R11.12, R11.17): the lowest current Morale Level among the units (a Dummy's 7), +1 for a 1PAATC
    /// when any of them is Inexperienced, and the best leadership of an unpinned Good Order leader of their side in <paramref name="at"/>.
    /// </summary>
    private (int Morale, int Drm, IReadOnlyList<string> Causes) PaatcFacts(GameState state, IReadOnlyList<UnitInstance> units, BoardLocation at)
    {
        var morale = units.Select(unit => unit.Kind == UnitKinds.Dummy ? 7
            : unit.Definition is { } reference && FireReference.Value.Definitions.GetValueOrDefault(reference.Definition) is { Morale: { } printed } definition
                ? printed + (Is(unit, Conditions.Fanatic) ? 1 : 0) - (definition.IsLeader && Is(unit, Conditions.Wounded) ? 1 : 0)
                : 7).DefaultIfEmpty(7).Min();
        var drm = 0;
        var causes = new List<string>();
        if (units.Any(unit => unit.Kind != UnitKinds.Dummy && Experience.Inexperienced(state, unit, catalogs, vocabulary) == ConditionState.True))
        {
            drm += 1;
            causes.Add("inexperienced-1paatc");
        }

        var side = units[0].Side;
        var leader = state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side && vocabulary.IsA(unit.Kind, "asl:leader")
                && !Is(unit, Conditions.Broken) && !Is(unit, Conditions.Pinned) && !Is(unit, Conditions.Captured)
                && unit.Definition is { } reference && FireReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.Leadership is not null)
            .Select(unit => (unit.Id, Value: FireReference.Value.Definitions[unit.Definition!.Definition].Leadership!.Value + (Is(unit, Conditions.Wounded) ? 1 : 0)))
            .OrderBy(item => item.Value).FirstOrDefault();
        if (leader.Id is not null && leader.Value != 0)
        {
            drm += leader.Value;
            causes.Add("leadership:" + leader.Id);
        }

        return (morale, drm, causes);
    }

    /// <summary>
    /// The answer to the A12.41 choice (ruling R11.12): a reveal places every subject unit in view (a Dummy is removed); a combined PAATC keeps them
    /// concealed when passed, and pins and reveals them when failed.
    /// </summary>
    private GamePlan PlanPaatcAnswer(GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor,
        GameState state, PendingChoice pending, GameEvent made, string option)
    {
        var resume = pending.Resume;
        var vehicle = resume.GetProperty("vehicle").GetString()!;
        var at = BoardLocation.Parse(resume.GetProperty("location").GetString()!);
        UnitInstance[] units = [.. resume.GetProperty("units").EnumerateArray().Select(item => state.Unit(item.GetString()!)).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active)];
        void Reveal(List<GameEvent> events, bool pin)
        {
            foreach (var unit in units)
            {
                if (unit.Kind == UnitKinds.Dummy)
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(unit.Id), null, null, [made.EventId]));
                    continue;
                }

                var conditions = new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False, [Conditions.Hidden] = ConditionState.False };
                if (pin)
                {
                    conditions[Conditions.Pinned] = ConditionState.True;
                }

                events.Add(Event(scope, attemptId, events.Count + 1, expected, "concealment-lost", new ConditionsChanged(unit.Id, conditions), null, null, [made.EventId]));
            }
        }

        if (option == ChoicePending.Reveal || units.Length == 0)
        {
            var events = new List<GameEvent> { made };
            Reveal(events, false);
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [$"play.paatc: the concealed units in {at} are revealed to {vehicle} (A12.41)"]);
        }

        var (morale, drm, causes) = PaatcFacts(state, units, at);
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent> { made };
            var dice = draw(new RollRequest(2, 6));
            var rollId = $"{attemptId}-roll-1";
            events.Add(Event(scope, attemptId, 2, expected, "dice-rolled", new DiceRolled(rollId, "paatc", 2, 6, dice.Values, DiceRolled.SystemSource, actor), null, null));
            var passed = dice.Values[0] + dice.Values[1] + drm <= morale;
            events.Add(Event(scope, attemptId, 3, expected, "paatc-taken", new PaatcTaken([.. units.Select(unit => unit.Id)], vehicle, rollId, morale, drm, passed), null, null,
                [EventId(attemptId, 2)]));
            if (!passed)
            {
                Reveal(events, true);
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.paatc: the concealed units in {at} take one combined PAATC against Morale Level {morale}{(drm == 0 ? string.Empty : $", DRM {drm:+0;-0} ({string.Join(", ", causes)})")}: passed, they stay concealed; failed, they are pinned and revealed (A12.41)"])
        {
            Roll = new PlannedRoll("paatc", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// A PAATC of Infantry against an AFV (A11.6, D7.21; ruling R11.17), taken before they advance into its Location or make CC Reaction Fire at it: the
    /// dice and the record, then a pin on failure. Units that passed one against the AFV this phase, and exempt units, take none.
    /// </summary>
    private void AddPaatc(GameScope scope, string attemptId, long expected, string actor, GameState state, IReadOnlyList<UnitInstance> units, UnitInstance afv,
        BoardLocation leaderAt, List<GameEvent> events, Func<RollRequest, RollResult> draw, out bool passed)
    {
        var (morale, drm, _) = PaatcFacts(state, units, leaderAt);
        var dice = draw(new RollRequest(2, 6));
        var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled", new DiceRolled(rollId, "paatc", 2, 6, dice.Values, DiceRolled.SystemSource, actor), null, null));
        passed = dice.Values[0] + dice.Values[1] + drm <= morale;
        var record = EventId(attemptId, events.Count);
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "paatc-taken", new PaatcTaken([.. units.Select(unit => unit.Id)], afv.Id, rollId, morale, drm, passed), null, null,
            [record]));
        if (!passed)
        {
            foreach (var unit in units)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                    new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [Conditions.Pinned] = ConditionState.True }), null, null, [EventId(attemptId, events.Count)]));
            }
        }
    }

    /// <summary>Whether a MMC must pass a PAATC to advance on or make CC Reaction Fire at an AFV (A11.6, D7.21): a manned, unconcealed AFV, not exempt, not passed this phase.</summary>
    private bool NeedsPaatc(GameState state, UnitInstance unit, UnitInstance vehicle) =>
        IsAfv(vehicle) && !Is(vehicle, Conditions.Abandoned) && !Is(vehicle, Conditions.Concealed) && !Is(vehicle, Conditions.Hidden) && !PaatcExempt(unit)
        && !state.PaatcPassed.Contains(unit.Id + "|" + vehicle.Id, StringComparer.Ordinal);
}
