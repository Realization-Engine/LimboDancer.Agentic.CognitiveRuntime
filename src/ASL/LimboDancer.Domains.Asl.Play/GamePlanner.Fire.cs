using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Reads LOS for a fire attack. The planner's own reader builds the LOS map from the boards' LOS data; a host or a test
/// may supply another, such as one over a map it has already built.
/// </summary>
public interface IFireLosReader
{
    /// <summary>The LOS result from one Location to another on the game's map, or null when it cannot be read.</summary>
    public LosResult? Read(GameState state, BoardLocation from, BoardLocation target);

    /// <summary>
    /// The LOS to a hexside Location's auxiliary vertex (the second end of the hexside, for a Snap Shot, A8.15); by default the reader's LOS to the
    /// hexside Location itself.
    /// </summary>
    public LosResult? ReadAuxiliary(GameState state, BoardLocation from, BoardLocation hexside) => Read(state, from, hexside);
}

/// <summary>
/// A proposed fire attack (Fire in Live Play, part 5): the facts read from the game and the map, and the reasons the firing
/// side may see. When the target Location holds a unit the firing side cannot see, or nothing it can see, a refusal tells
/// that side only that the Fire package does not decide the attack, since the package's reasons can name the unit or its
/// printed values; the target side and the adjudicator see the package's reasons.
/// </summary>
public sealed record FireProposal(string FiringSide, string TargetSide, FireAttack Attack, IReadOnlyList<string> FiringSideReasons)
{
    public const string Undisclosed =
        "play.fire-refused: the Fire package does not decide every outcome of an attack on this Location, for reasons about units the firing side cannot see";

    /// <summary>The reasons a perspective may see, given the reasons of the plan or of its result.</summary>
    public IReadOnlyList<string> ReasonsFor(IReadOnlyList<string> reasons, Perspective perspective)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        ArgumentNullException.ThrowIfNull(perspective);
        return perspective.IsAdjudicator || perspective.Name != FiringSide || FiringSideReasons.Count == 0 ? reasons : FiringSideReasons;
    }
}

/// <summary>
/// Fire in live play (unit steps 18 to 23): PFPh, AFPh, DFPh, and MPh attacks by a fire group in one Location or across
/// ADJACENT Locations, with MGs, resolved by the reviewed Fire package. Every fact comes from the game and the map read;
/// the attack is refused before any roll unless every outcome the dice can reach is decided
/// (<see cref="ScenarioA1FireCalculator.Precheck"/>), and its rolls are drawn one at a time as the package asks for them.
/// </summary>
public sealed partial class GamePlanner
{
    private static readonly Lazy<ScenarioA1FireReference> FireReference = new(() => new ScenarioA1FirePackage().Reference);

    // The VASL terrain names the Fire package's TEM admits (Terrain Chart p. 698; B1.1, B12, B13, B14, B15, B23). A road hex is
    // Open Ground apart from its road (B1.11, p. 113).
    private static readonly IReadOnlyDictionary<string, string> FireTerrain = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Open Ground"] = "open-ground",
        ["Paved Road"] = "open-ground",
        ["Dirt Road"] = "open-ground",
        ["Brush"] = "brush",
        ["Woods"] = "woods",
        ["Orchard"] = "orchard",
        ["Grain"] = "grain",

        // Backlog pass 10 (ruling R10.1): marsh (B16) and rubble (B24).
        ["Marsh"] = "marsh",
        ["Wooden Rubble"] = "wooden-rubble",
        ["Stone Rubble"] = "stone-rubble",
    };

    // The IFT results that are at least a NMC (A10.62).
    private static readonly string[] AtLeastNmc =
        ["NMC", "1MC", "2MC", "3MC", "4MC", "K/1", "K/2", "K/3", "K/4", "1KIA", "2KIA", "3KIA", "4KIA", "5KIA", "6KIA", "7KIA"];

    /// <summary>
    /// Why a unit may not fire or direct fire at all, or null: berserk fire is not reviewed (A15.432), a unit held in Melee fires
    /// only in CC (A11.15), a prisoner does not fire (A20.5), and a Guard's fire is not reviewed (A20.52).
    /// </summary>
    public static string? FireBar(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        return Is(unit, Conditions.Berserk) ? "is berserk, and berserk fire is not reviewed (A15.432)"
            : Is(unit, Conditions.Melee) ? "is held in Melee and fires only in CC (A11.15)"
            : Is(unit, Conditions.Captured) ? "is a prisoner and does not fire (A20.5)"
            : state.Units.Any(prisoner => prisoner.Status == InstanceStatus.Active && prisoner.Custodian == unit.Id) ? "guards prisoners, and a Guard's fire is not reviewed (A20.52)"
            : null;
    }

    private GamePlan PlanFire(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!arguments.TryGetProperty("firers", out var firerList) || firerList.ValueKind != JsonValueKind.Array
            || firerList.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String)
            || !Text(arguments, "target", out var targetText) || !BoardLocation.TryParse(targetText, out var target))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: fire names its firers and a target Location");
        }

        var directors = new List<string>();
        if (Text(arguments, "director", out var named))
        {
            directors.Add(named);
        }

        directors.AddRange(Strings(arguments, "directors").Where(id => !directors.Contains(id)));
        var weapons = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        if (arguments.TryGetProperty("weapons", out var weaponMap) && weaponMap.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in weaponMap.EnumerateObject())
            {
                weapons[entry.Name] = entry.Value.ValueKind == JsonValueKind.Array
                    ? [.. entry.Value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)]
                    : [];
            }
        }

        var alone = Strings(arguments, "withoutInherent").ToArray();
        string[] firerIds = [.. firerList.EnumerateArray().Select(item => item.GetString()!)];

        // D3.3 (ruling R6.9): the moving vehicle's Bounding First Fire comes once the DEFENDER has passed on its MP expenditure.
        var bounding = state.Phase == "mph" && firerIds.Length == 1 && LiveFire.MayBoundingFire(state, firerIds[0]);

        // A8.1, A8.11: in the MPh, Defensive fire answers the moving stack's MF expenditure, in its Location.
        if (state.Phase == "mph" && !bounding && (state.Movement is not { WindowOpen: true } window || window.Location != target))
        {
            return Refused(scope, label, expected, "play.fire-window: Defensive First Fire attacks the moving stack in its Location, while the DEFENDER's window on its MF expenditure is open (A8.1, A8.11)");
        }

        var (attack, reason) = LiveFire.FromState(state, firerIds, directors, target, weapons.Count > 0 ? weapons : null, alone.Length > 0 ? alone : null);
        if (attack is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        // D2.4: a vehicle under a Motion counter may not Prep Fire.
        if (attack.VehicleFire is { InMotion: true } inMotion && state.Phase == "pfph")
        {
            return Refused(scope, label, expected, $"play.fire-vehicle-motion: {inMotion.VehicleId} is in Motion and may not Prep Fire (D2.4)");
        }

        // A15.432: berserk fire (TPBF in the AFPh, and the DFPh) is not reviewed; A11.15: units held in Melee fire only in CC; A20.52:
        // a Guard's fire is not reviewed; A20.54, A11.15: fire at a Location holding prisoners or units in Melee is not reviewed.
        if (firerIds.Concat(directors).Select(state.Unit).OfType<UnitInstance>().Select(unit => (unit, Cause: FireBar(state, unit))).FirstOrDefault(item => item.Cause is not null)
            is ({ } barred, { } cause))
        {
            return Refused(scope, label, expected, $"play.fire-barred: {barred.Id} {cause}");
        }

        if (state.At(target).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && (Is(unit, Conditions.Melee) || Is(unit, Conditions.Captured))))
        {
            return Refused(scope, label, expected, "play.fire-melee: fire at a Location holding units in Melee or prisoners is not reviewed (A11.15, A20.54)");
        }

        // A8.3, A8.31: Subsequent First Fire and FPF use every usable MG the firer possesses.
        if (attack.FireKind is ScenarioA1FireCalculator.SubsequentFirstFire or ScenarioA1FireCalculator.FinalProtectiveFire
            && attack.Firers!.Any(item => !(item.Weapons?.Select(weapon => weapon.EquipmentId!).Order(StringComparer.Ordinal).ToArray() ?? [])
                .SequenceEqual(Possessed(state, item.UnitId!))))
        {
            return Refused(scope, label, expected, "play.fire-weapons: Subsequent First Fire and FPF use every MG the firer possesses (A8.3, A8.31)");
        }

        var firingSide = state.Unit(attack.VehicleFire?.VehicleId ?? attack.Firers![0].UnitId!)!.Side;
        var targetSide = attack.Targets!.Count > 0 ? state.Unit(attack.Targets[0].UnitId!)!.Side
            : attack.Vehicles is [{ } vehicleTarget, ..] ? state.Unit(vehicleTarget.VehicleId!)!.Side
            : state.Sides.First(item => item.Id != firingSide).Id;

        // The firing side sees nothing, or not everything, at the target: its refusals say only that the attack is undecided. A concealed
        // vehicle is unseen too (ruling R6.7).
        var seen = attack.Targets.Count(item => VisibleTo(state.Unit(item.UnitId!)!, firingSide));
        var unseen = seen < attack.Targets.Count || (seen == 0 && attack.Vehicles is not { Count: > 0 }) || (attack.Vehicles ?? []).Any(item => item.Concealed == true);
        FireProposal Proposal(FireAttack facts, bool undecided = false) =>
            new(firingSide, targetSide, facts, undecided && unseen ? [FireProposal.Undisclosed] : []);

        // A7.55: the units of a Location that fire at a target in a phase (in the MPh, at one MF expenditure) form one fire
        // group, so it fires once; a MG firing again alone on its Multiple ROF is not a new group.
        var step = state.Phase == "mph" && !bounding ? state.Movement!.Step : (int?)null;
        var fromLocations = attack.Firers!.Select(item => item.LocationId!).Distinct(StringComparer.Ordinal).ToArray();
        if (alone.Length == 0 && state.FiresThisPhase.Any(record => !(attack.VehicleFire is not null && record.Vehicle)
            && (fromLocations.Contains(record.FirerLocation) || record.FirerLocation == attack.FirerLocationId)
            && record.TargetLocation == attack.TargetLocationId && record.Step == step))
        {
            return Refused(scope, label, expected,
                $"play.fire-group: a Location of the group has already fired at {attack.TargetLocationId}{(step is null ? " this phase" : " at this MF expenditure")}, and its units fire as one fire group (A7.55, p. 57)")
                with
            {
                Fire = Proposal(attack)
            };
        }

        // A8.3, A9.2: a unit or MG fires at a moving stack in a Location no more often than the MF the stack spent entering it
        // (FRD, at least once); each step enters a new Location, so the attacks there answer the current step.
        if (state.Movement is { } moving)
        {
            var limit = Math.Max(1, moving.HalfMfInLocation / 2);
            var here = existing.Select(item => item.Payload).OfType<FireResolved>().Where(record => record.MovementStep == moving.Step).ToArray();
            if (attack.Firers!.Any(firer => here.Count(record => record.Firers.Contains(firer.UnitId!)) >= limit))
            {
                return Refused(scope, label, expected,
                    $"play.fire-mf-limit: a firer attacks a moving stack in a Location no more often than the MF it spent there, {limit} here (A8.3, A9.2)")
                    with
                {
                    Fire = Proposal(attack)
                };
            }
        }

        // A8.15 (ruling R10.13): a Snap Shot at the hexside the moving stack crossed.
        if (arguments.TryGetProperty("snapShot", out var snap) && snap.ValueKind == JsonValueKind.True)
        {
            attack = attack with
            {
                SnapShot = true
            };
        }

        var (map, mapReason) = FireMapFacts(state, attack, target, existing);
        if (map is null)
        {
            return Refused(scope, label, expected, mapReason!) with
            {
                Fire = Proposal(attack)
            };
        }

        attack = HeatOfBattleFacts(state, map);

        // C11 (ruling R8.3): a Gun's crew alone in the target Location takes its gunshield, facing every firer's Location, or its Emplacement.
        if (attack.VehicleFire is null && attack.FireKind != ScenarioA1FireCalculator.ResidualFire && attack.Firers is { Count: > 0 } firing
            && GunAt(state, target, state.Unit(firing[0].UnitId!)!.Side, [.. firing.Select(item => BoardLocation.Parse(item.LocationId!)).Distinct()]) is { } gunTarget)
        {
            attack = attack with
            {
                GunTarget = gunTarget
            };
        }

        var precheck = ScenarioA1FireCalculator.Precheck(attack, FireReference.Value);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, RefusalReasons.Refusal("play.fire-refused", "Fire", "attack", precheck)) with
            {
                Fire = Proposal(attack, undecided: true)
            };
        }

        var facts = attack;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            AddFireEvents(scope, attemptId, expected, actor, state, facts, targetSide, step, events, draw);
            return events;
        }

        var directing = new[] { facts.Director?.UnitId }.Concat(facts.OtherDirectors?.Select(item => item.UnitId) ?? []).OfType<string>().ToArray();
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.fire: {string.Join(", ", facts.VehicleFire is { } byVehicle ? [byVehicle.VehicleId] : facts.Firers!.Select(item => item.UnitId))} fire at {facts.TargetLocationId} in the {facts.Phase}"
                + (facts.FireKind is { } kind ? $" ({kind})" : string.Empty)
                + (directing.Length > 0 ? $", directed by {string.Join(", ", directing)}" : string.Empty)
                + $"; range {facts.Range}, {facts.TargetTerrain}, Hindrance {facts.Los!.HindranceDrm}"])
        {
            Roll = new PlannedRoll("fire", Build),
            FirstEventId = EventId(attemptId, 1),
            Fire = Proposal(facts),
        };
    }

    /// <summary>
    /// The map reads Heat of Battle needs before any roll (unit step 30): for each target, and each FPF firer, whether a Known enemy
    /// unit is in its LOS (A15.44) and the ADJACENT units it may surrender to (A15.5).
    /// </summary>
    private FireAttack HeatOfBattleFacts(GameState state, FireAttack attack)
    {
        // A12.14: a concealed firer or director loses "?" by this attack when every firer is within 16 hexes and a target is Good Order,
        // as the Fire package decides it; one that does is Known when a target's Heat of Battle result is read (A15.44, A15.5).
        var firers = attack.Firers ?? [];
        string[] revealed = firers.Count > 0 && firers.All(item => (item.Range ?? attack.Range) <= 16)
            && attack.Targets!.Any(item => item.Broken == false && item.Dummy != true)
            ? [.. firers.Select(item => item.UnitId).Concat(new[] { attack.Director?.UnitId }).Concat((attack.OtherDirectors ?? []).Select(item => item.UnitId))
                .OfType<string>().Where(id => state.Unit(id) is { } unit && Is(unit, Conditions.Concealed))]
            : [];
        FireTarget Read(FireTarget target)
        {
            if (target.Dummy == true || state.Unit(target.UnitId!) is not { } unit || state.Location(unit.Id) is not { } at)
            {
                return target;
            }

            return target with
            {
                KnownEnemyInLos = revealed.Length > 0 ? true : KnownEnemyInLos(state, unit.Side, at.Location),
                Captors = Captors(state, unit, revealed),
            };
        }

        return attack with
        {
            Targets = [.. attack.Targets!.Select(Read)],
            Firers = attack.FireKind != ScenarioA1FireCalculator.FinalProtectiveFire ? attack.Firers
                : [.. attack.Firers!.Select(firer => state.Unit(firer.UnitId!) is { } unit && state.Location(unit.Id) is { } at
                    ? firer with { KnownEnemyInLos = KnownEnemyInLos(state, unit.Side, at.Location), Captors = Captors(state, unit) }
                    : firer)],
        };
    }

    private static IEnumerable<string> Strings(JsonElement arguments, string name) =>
        arguments.TryGetProperty(name, out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)
            : [];

    /// <summary>The usable MGs and ATR a unit possesses, in id order (table player, pass 9b: a mortar or PSK takes no part in its fire groups).</summary>
    private static string[] Possessed(GameState state, string unitId) =>
        [.. state.Equipment.Where(equipment => equipment.Status == InstanceStatus.Active && equipment.Holding is { Role: HoldingRole.Possessed } holding
                && holding.Holder == unitId && GameState.Condition(equipment, Conditions.Malfunctioned) != ConditionState.True
                && (equipment.Kind == "asl:mg" || (equipment.Definition is { } weapon && LiveOrdnance.LatwType(weapon.Definition) == "atr")))
            .Select(equipment => equipment.Id).Order(StringComparer.Ordinal)];

    /// <summary>
    /// Draws the rolls the package asks for, one at a time, and adds the attack's events: the dice, the fire record (withheld
    /// from the firing side when it would identify unseen targets the attack leaves unaffected), its public report, the
    /// effects on the targets, the FPF firers' NMC, the MGs' malfunctions and markers, the fire markers, and the Residual FP
    /// it leaves.
    /// </summary>
    private void AddFireEvents(GameScope scope, string attemptId, long expected, string actor, GameState state, FireAttack facts, string targetSide,
        int? step, List<GameEvent> events, Func<RollRequest, RollResult> draw, ResumedRolls? resumed = null)
    {
        var reference = FireReference.Value;
        var package = ScenarioA1FirePackage.Identity.ToString();

        // Ruling R5.8: the owners' options are asked for as the attack reaches them; a resumed attack starts from the rolls it drew.
        facts = facts with
        {
            Choices = facts.Choices ?? new Dictionary<string, string>(StringComparer.Ordinal)
        };
        var rollIds = new Dictionary<string, string>(resumed?.RollIds ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        var rolls = new FireRolls(null, null, null, null, null);
        foreach (var (key, values) in resumed?.Values ?? [])
        {
            rolls = ApplyFireRoll(rolls, key, values);
        }

        FireResolution resolution;
        while (true)
        {
            resolution = ScenarioA1FireCalculator.Resolve(facts with
            {
                Rolls = rolls
            }, reference);
            if (resolution.Disposition == FireResolution.Resolved)
            {
                break;
            }

            // An option the attack reaches stops it until its owner answers (ruling R5.8).
            if (resolution.Reasons is [{ } option] && option.StartsWith("asl.a1.fire.choice-missing:", StringComparison.Ordinal))
            {
                var choiceKey = option["asl.a1.fire.choice-missing:".Length..];
                var resume = new JsonObject
                {
                    ["record"] = "fire",
                    ["facts"] = JsonNode.Parse(JsonSerializer.Serialize(facts, LiveFire.Json)),
                    ["rolls"] = RollNode(rollIds),
                    ["targetSide"] = targetSide,
                };
                if (step is { } movementStep)
                {
                    resume["step"] = movementStep;
                }

                events.Add(Event(scope, attemptId, events.Count + 1, expected, "choice-pending", Pending(state, choiceKey, resume), package, null));
                return;
            }

            // The pre-check leaves only missing rolls, asked for one at a time.
            if (resolution.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Fire package left an attack it had accepted undecided: " + string.Join("; ", resolution.Reasons));
            }

            var key = missing["asl.a1.fire.roll-missing:".Length..];
            var split = key.IndexOf(':', StringComparison.Ordinal);
            var (kind, unit) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);

            // A Random Selection names the units it selects among, one die each (A.9, A8.31, A9.71).
            var selected = kind is "randomSelection" or "weaponSelection" or "firerSelection" ? unit.Split(',') : [];
            var (count, purpose) = kind switch
            {
                "attack" => (2, "fire-ift"),
                "randomSelection" => (selected.Length, "fire-random-selection"),
                "weaponSelection" => (selected.Length, "fire-weapon-selection"),
                "firerSelection" => (selected.Length, "fire-firer-selection"),
                "checks" => (2, "fire-check"),
                "leaderLoss" => (2, "fire-leader-loss"),
                "heatOfBattle" => (2, "fire-heat-of-battle"),
                "berserkCheck" => (2, "fire-berserk-check"),
                "crewCheck" => (2, "fire-crew-check"),
                "unlikelyKill" => (1, "fire-unlikely-kill"),
                _ => (1, "fire-wound-severity"),
            };
            var drawn = draw(new RollRequest(count, 6));
            var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
            rollIds[key] = rollId;
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                new DiceRolled(rollId, purpose, count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
            rolls = ApplyFireRoll(rolls, key, drawn.Values);
        }

        // A12.13: concealed, hidden, and Dummy targets have their own column when known targets share the Location. A
        // result that leaves unseen targets, or nothing, unaffected is not identified to the firing side (A12.14; R21.1).
        var arithmetic = resolution.Arithmetic!;
        var hiddenResult = arithmetic.Concealed?.Result ?? arithmetic.Result;
        var hidesIdentity = hiddenResult == "none"
            && (facts.Targets!.Count == 0 || facts.Targets.Any(item => item.Concealed == true || item.Hidden == true || item.Dummy == true));
        var fireId = EventId(attemptId, events.Count + 1);
        var firerLocation = facts.FirerLocationId ?? facts.TargetLocationId!;
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "fire-resolved",
            new FireResolved(facts.VehicleFire is { } byVehicle ? [byVehicle.VehicleId!] : facts.Overrun is { } overrunning ? [overrunning.VehicleId!]
                    : [.. (facts.Firers ?? []).Select(item => item.UnitId!)],
                facts.Director?.UnitId, firerLocation, facts.TargetLocationId!, rollIds,
                JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json))
            {
                MovementStep = step,
            }, package, hidesIdentity ? [targetSide] : null));
        if (hidesIdentity)
        {
            // The firing side still learns the arithmetic, which names no target unit.
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "fire-reported",
                new FireReported(fireId, firerLocation, facts.TargetLocationId!, JsonSerializer.SerializeToElement(arithmetic, LiveFire.Json)), package, null,
                [fireId]));
        }

        // A10.62: a broken unit attacked by FP that could inflict at least a NMC, allowing for Cowering, is under DM.
        var desperate = CouldCauseNmc(facts, arithmetic);
        foreach (var effect in resolution.Effects)
        {
            var target = facts.Targets!.First(item => item.UnitId == effect.UnitId);
            var attackedBroken = target.Broken == true && desperate(target.Concealed == true || target.Hidden == true || target.Dummy == true);
            foreach (var payload in EffectEvents(state, effect, attemptId, attackedBroken))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, payload.Type, payload.Payload, package, null, [fireId]));
            }
        }

        // A7.308, A7.309, D.8B: the Vehicle line's result and the crew's (ruling R25.5, R25.6).
        foreach (var vehicle in resolution.VehicleEffects ?? [])
        {
            foreach (var payload in VehicleEffectEvents(state, vehicle, facts))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, payload.Type, payload.Payload, package, null, [fireId]));
            }

            // D5.341, D5.41 (ruling R5.18): a Recalled AFV on its way off the map that is immobilized is Abandoned by its crew.
            if (vehicle.Result == FireVehicleEffect.Immobilized && state.Unit(vehicle.VehicleId) is { } stuck && MustLeave(stuck))
            {
                foreach (var (type, abandon) in AbandonEvents(stuck, attemptId))
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, type, abandon, package, null, [fireId]));
                }
            }
        }

        foreach (var effect in resolution.FirerEffects ?? [])
        {
            foreach (var payload in EffectEvents(state, effect, attemptId, false))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, payload.Type, payload.Payload, package, null, [fireId]));
            }
        }

        // A15.41: the companions a berserk leader took with him.
        foreach (var effect in resolution.CompanionEffects ?? [])
        {
            if (EffectEvent(state, effect, attemptId, false) is { } companion)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, companion.Type, companion.Payload, package, null, [fireId]));
            }
        }

        // The MGs: a malfunction (A9.7), and the fire counter of a MG that lost its Multiple ROF (A9.2).
        foreach (var weapon in resolution.WeaponEffects ?? [])
        {
            var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
            if (weapon.Malfunctioned)
            {
                conditions[Conditions.Malfunctioned] = ConditionState.True;
            }

            if (weapon.FireCounter is { } counter)
            {
                conditions[Marker(counter)] = ConditionState.True;
                if (counter == "final-fire" && state.Find(weapon.EquipmentId) is { } equipment && GameState.Condition(equipment, Conditions.FirstFire) == ConditionState.True)
                {
                    conditions[Conditions.FirstFire] = ConditionState.False;
                }
            }

            if (conditions.Count > 0)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(weapon.EquipmentId, conditions),
                    package, null, [fireId]));
            }
        }

        // D7.17 (ruling R11.11): an OVR's Original 12 malfunctions a weapon that added FP, or immobilizes a vehicle with none; a wreck keeps no weapons.
        if (resolution.OverrunEffect is { } overrunEffect && state.Unit(overrunEffect.VehicleId) is { Status: InstanceStatus.Active })
        {
            var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
            foreach (var weapon in overrunEffect.MalfunctionedWeapons)
            {
                conditions[weapon switch
                {
                    FireOverrunEffect.BowMg => Conditions.BmgMalfunctioned,
                    FireOverrunEffect.CoaxialMg => Conditions.CmgMalfunctioned,
                    _ => Conditions.Malfunctioned,
                }] = ConditionState.True;
            }

            if (overrunEffect.Immobilized)
            {
                conditions[Conditions.Immobilized] = ConditionState.True;
                conditions[Conditions.Motion] = ConditionState.False;
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(overrunEffect.VehicleId, conditions), package, null,
                [fireId]));
        }

        // The fire markers (A3.2, A3.4, A3.5, A8.1, A8.3, A8.4); a MG firing again alone on its Multiple ROF leaves its unit as it was.
        var alone = (facts.Firers ?? []).Where(item => item.UsesInherentFp == false).Select(item => item.UnitId!).ToHashSet(StringComparer.Ordinal);
        foreach (var id in resolution.FireCounterUnitIds.Where(id => !alone.Contains(id) && state.Unit(id) is not { Status: not InstanceStatus.Active }))
        {
            var conditions = new Dictionary<string, ConditionState> { [Marker(resolution.FireCounter!)] = ConditionState.True };
            if (resolution.FireCounter == "final-fire" && state.Unit(id) is { } marked && GameState.Condition(marked, Conditions.FirstFire) == ConditionState.True)
            {
                conditions[Conditions.FirstFire] = ConditionState.False;
            }

            // A12.2 (ruling R6.7): a concealed vehicle that fires loses its "?".
            if (resolution.FirerConcealmentLost.Contains(id) || (facts.VehicleFire?.VehicleId == id && state.Unit(id) is { } firing
                && (Is(firing, Conditions.Concealed) || Is(firing, Conditions.Hidden))))
            {
                conditions[Conditions.Concealed] = ConditionState.False;
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(id, conditions), package, null, [fireId]));
        }

        // A8.2, A8.21: Residual FP, unless a counter at least as large is already there.
        if (arithmetic.ResidualFp is { } residual && BoardLocation.TryParse(facts.TargetLocationId!, out var location)
            && !state.ResidualFire.Any(item => item.Location == location && item.Fp >= residual))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "residual-fp-placed", new ResidualFirePlaced(fireId, location, residual), package, null,
                [fireId]));
        }

        // A15.5: a unit that surrendered to ADJACENT captors waits for the captor's choice, last, since nothing else happens until then.
        foreach (var effect in resolution.Effects.Concat(resolution.FirerEffects ?? []))
        {
            if (!effect.Eliminated && (effect.SecondHeatOfBattle ?? effect.HeatOfBattle) is { Result: HeatOfBattleOutcome.Surrender, Captors.Count: > 0 } surrender)
            {
                var id = effect.FinalDefinitionId != effect.DefinitionId ? $"{attemptId}-{effect.UnitId}" : effect.UnitId;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "surrender-pending", new SurrenderPending(id, surrender.Captors!), package, null, [fireId]));
            }
        }
    }

    /// <summary>
    /// The events that record a vehicle's effect (rulings R25.5, R25.6, R6.5, R6.7): a destroyed vehicle becomes a wreck, with a Blaze when it
    /// burns (D10.1, B25.14); an immobilized one loses Motion (D.7); a Stunned crew buttons up and the vehicle Stops (D5.34); a Recalled crew
    /// is treated as Stunned but marked Recalled alone (D5.341); a pinned crew is pinned (A7.82). A concealed vehicle given a Vehicle line
    /// result, or whose crew took at least a PTC, loses its "?" to the firer in its LOS (A12.2); Residual FP has no firer.
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> VehicleEffectEvents(GameState state, FireVehicleEffect effect, FireAttack facts)
    {
        var vehicle = state.Unit(effect.VehicleId);
        if (effect.Result is FireVehicleEffect.Eliminated or FireVehicleEffect.BurningWreck)
        {
            var burning = effect.Result == FireVehicleEffect.BurningWreck;
            yield return ("vehicle-wrecked", new VehicleWrecked(effect.VehicleId, burning));
            if (burning && vehicle is not null && state.Location(vehicle.Id) is { } at)
            {
                yield return ("instance-created", new InstanceCreated(new NewInstance(BlazeId(vehicle.Id), "asl:fire", null, null, new MapPosition(at.Location), null,
                    new Dictionary<string, ConditionState>())));
            }

            yield break;
        }

        if (VehicleConditions(effect) is { } changed)
        {
            if (vehicle is not null && (Is(vehicle, Conditions.Concealed) || Is(vehicle, Conditions.Hidden)) && facts.FireKind != ScenarioA1FireCalculator.ResidualFire
                && (effect.Result != FireVehicleEffect.None || effect.CrewCheck is not null || effect.CrewResult == FireVehicleEffect.Recalled))
            {
                changed[Conditions.Concealed] = ConditionState.False;
                changed[Conditions.Hidden] = ConditionState.False;
            }

            yield return ("conditions-changed", new ConditionsChanged(effect.VehicleId, changed));
        }
        else if (vehicle is not null && (Is(vehicle, Conditions.Concealed) || Is(vehicle, Conditions.Hidden)) && facts.FireKind != ScenarioA1FireCalculator.ResidualFire
            && effect.CrewCheck is not null)
        {
            yield return ("conditions-changed", new ConditionsChanged(effect.VehicleId,
                new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False, [Conditions.Hidden] = ConditionState.False }));
        }
    }

    private static Dictionary<string, ConditionState>? VehicleConditions(FireVehicleEffect effect)
    {
        var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
        if (effect.Result == FireVehicleEffect.Immobilized)
        {
            conditions[Conditions.Immobilized] = ConditionState.True;
            conditions[Conditions.Motion] = ConditionState.False;
        }

        switch (effect.CrewResult)
        {
            case FireVehicleEffect.Stunned:
                conditions[Conditions.Stunned] = ConditionState.True;
                conditions[Conditions.ButtonedUp] = ConditionState.True;
                conditions[Conditions.Motion] = ConditionState.False;
                break;
            case FireVehicleEffect.Recalled:
                // A Recall is a Stun that removes the vehicle at the end of the Player Turn (D5.341); every check reads either.
                conditions[Conditions.Recalled] = ConditionState.True;
                conditions[Conditions.Stunned] = ConditionState.False;
                conditions[Conditions.StunRecovery] = ConditionState.False;
                conditions[Conditions.ButtonedUp] = ConditionState.True;
                conditions[Conditions.Motion] = ConditionState.False;
                break;
            case FireVehicleEffect.Pinned:
                conditions[Conditions.Pinned] = ConditionState.True;
                break;
        }

        return conditions.Count == 0 ? null : conditions;
    }

    private static string Marker(string counter) => counter switch
    {
        "prep-fire" => Conditions.PrepFire,
        "first-fire" => Conditions.FirstFire,
        "bounding-fire" => Conditions.BoundingFire,
        _ => Conditions.FinalFire,
    };

    /// <summary>
    /// Whether the attack could inflict at least a NMC on a target of a group (A10.62): over every DR, with the Cowering a
    /// doubles DR brings when no leader directs, on the group's column and with the attack's DRM.
    /// </summary>
    private static Func<bool, bool> CouldCauseNmc(FireAttack facts, FireArithmetic arithmetic)
    {
        var reference = FireReference.Value;
        var drm = (int)arithmetic.Drm.Sum(item => item.Value);
        var directed = facts.Director is not null;
        // Residual FP and an ordnance hit are never subject to Cowering (A8.224, C.2).
        var residual = facts.FireKind == ScenarioA1FireCalculator.ResidualFire || facts.OrdnanceHit is not null;
        bool Could(int? columnFp)
        {
            var column = columnFp is { } fp ? Array.IndexOf(ScenarioA1FireReference.ColumnFp, fp) : -1;
            if (column < 0)
            {
                return false;
            }

            for (var first = 1; first <= 6; first++)
            {
                for (var second = 1; second <= 6; second++)
                {
                    var shifted = column - (!residual && !directed && first == second ? 1 : 0);
                    if (shifted >= 0 && AtLeastNmc.Contains(reference.Result(first + second + drm, shifted)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        var known = Could(arithmetic.UnshiftedColumnFp);
        var concealed = arithmetic.Concealed is { } second ? Could(second.UnshiftedColumnFp) : known;
        return vsConcealed => vsConcealed ? concealed : known;
    }

    private static Dictionary<string, T> Add<T>(IReadOnlyDictionary<string, T>? existing, string id, T value)
    {
        var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new Dictionary<string, T>(StringComparer.Ordinal);
        next[id] = value;
        return next;
    }

    /// <summary>
    /// The event that records one unit's effect: an elimination, a Reduction or Replacement, or new conditions. A unit that
    /// breaks, or that is attacked while broken by enough FP, is under DM (A10.62).
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> EffectEvents(GameState state, FireUnitEffect effect, string attemptId,
        bool attackedWhileBroken)
    {
        if (EffectEvent(state, effect, attemptId, attackedWhileBroken) is { } own)
        {
            yield return own;
        }

        // A15.21: a hero created by Heat of Battle, in the unit's Location, sharing its fire and movement status (ruling R5.11); a hero created
        // from a Fanatic unit is Fanatic (A10.8). A second Heat of Battle DR may create a second hero (ruling R5.10).
        if (state.Unit(effect.UnitId) is { } creator)
        {
            var creatorId = effect.FinalDefinitionId != effect.DefinitionId && !effect.Eliminated ? $"{attemptId}-{effect.UnitId}" : effect.UnitId;
            var alive = !effect.Eliminated;
            if (effect.HeatOfBattle?.HeroDefinitionId is { } hero)
            {
                yield return ("instance-created", new InstanceCreated(HeroOf(creator, hero, attemptId)) { Creator = alive ? creatorId : null });
            }

            if (effect.SecondHeatOfBattle?.HeroDefinitionId is { } second)
            {
                yield return ("instance-created", new InstanceCreated(HeroOf(creator, second, attemptId, "hero-2")) { Creator = alive ? creatorId : null });
            }
        }
    }

    /// <summary>A hero a unit creates (A15.21): unbroken and known, with the unit's fire markers and Fanaticism.</summary>
    private static NewInstance HeroOf(UnitInstance creator, string hero, string attemptId, string suffix = "hero")
    {
        var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal)
        {
            [Conditions.Broken] = ConditionState.False,
            [Conditions.Pinned] = ConditionState.False,
            [Conditions.Wounded] = ConditionState.False,
            [Conditions.Concealed] = ConditionState.False,
            [Conditions.Hidden] = ConditionState.False,
        };
        foreach (var marker in new[] { Conditions.PrepFire, Conditions.FirstFire, Conditions.FinalFire, Conditions.Fanatic, Conditions.Cx })
        {
            if (GameState.Condition(creator, marker) == ConditionState.True)
            {
                conditions[marker] = ConditionState.True;
            }
        }

        return new NewInstance($"{attemptId}-{creator.Id}-{suffix}", "asl:hero", hero, creator.Side, creator.Position, null, conditions);
    }

    private static (string Type, EventPayload Payload)? EffectEvent(GameState state, FireUnitEffect effect, string attemptId, bool attackedWhileBroken)
    {
        var unit = state.Unit(effect.UnitId)!;
        if (effect.Eliminated)
        {
            return ("instance-eliminated", new InstanceEliminated(unit.Id));
        }

        var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
        void Set(string name, bool value)
        {
            if ((GameState.Condition(unit, name) == ConditionState.True) != value)
            {
                conditions[name] = value ? ConditionState.True : ConditionState.False;
            }
        }

        Set(Conditions.Broken, effect.Broken);
        Set(Conditions.Pinned, effect.Pinned);
        Set(Conditions.Wounded, effect.Wounded);
        Set(Conditions.Disrupted, effect.Disrupted);
        if (effect.Broken && (GameState.Condition(unit, Conditions.Broken) != ConditionState.True || attackedWhileBroken))
        {
            Set(Conditions.DesperationMorale, true);
        }

        if (effect.ConcealmentLost)
        {
            conditions[Conditions.Concealed] = ConditionState.False;
            conditions[Conditions.Hidden] = ConditionState.False;
        }

        // A10.8, A15.3: Fanaticism, once gained, lasts; A15.21: a heroic leader.
        if (effect.Fanatic == true)
        {
            Set(Conditions.Fanatic, true);
        }

        // A15.4, A15.42: a unit that goes berserk is rallied and no longer under DM.
        if (effect.Berserk == true)
        {
            Set(Conditions.Berserk, true);
            Set(Conditions.DesperationMorale, false);
        }

        if (effect.Heroic == true)
        {
            Set(Conditions.Heroic, true);
        }

        // A15.3: a Battle Hardened unit is unbroken, so no longer under DM; A15.21: nor is a leader made heroic.
        if (effect.HeatOfBattle?.Hardening == true || effect.HeatOfBattle?.Heroic == true)
        {
            Set(Conditions.DesperationMorale, false);
        }

        if (effect.FinalDefinitionId != effect.DefinitionId)
        {
            // A7.302: Casualty Reduction makes a HS of the same broken status; A19.13: Replacement by a lesser unit; A15.3:
            // Battle Hardening by an unbroken, unpinned unit of the next higher quality.
            var reference = FireReference.Value.Definitions[effect.FinalDefinitionId];
            var reduced = unit.Kind == "asl:squad" && reference.Kind == "asl:half-squad";
            var produced = unit.Conditions.Where(item => item.Key != Conditions.Concealed && item.Key != Conditions.Hidden)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            foreach (var (name, value) in conditions)
            {
                produced[name] = value;
            }

            // A12.14: a unit that passed its MC and was Battle Hardened keeps "?" unless the attack cost it; any other
            // Reduction or Replacement loses it.
            var hardened = effect.HeatOfBattle?.HardenedDefinitionId == effect.FinalDefinitionId;
            produced[Conditions.Concealed] = hardened && !effect.ConcealmentLost ? GameState.Condition(unit, Conditions.Concealed) : ConditionState.False;
            produced[Conditions.Hidden] = ConditionState.False;
            return ("lineage", new LineageRecorded(reduced ? LineageAction.Reduced : LineageAction.Replaced, [unit.Id],
                [new NewInstance($"{attemptId}-{unit.Id}", reference.Kind, reference.Id, unit.Side, unit.Position, null, produced)]));
        }

        return conditions.Count == 0 ? null : ("conditions-changed", new ConditionsChanged(unit.Id, conditions));
    }

    /// <summary>
    /// The map's facts of an attack: for each firer's Location, its range, level, and LOS and its attributed Hindrance; the
    /// target's terrain; for a group across Locations, whether each Location is ADJACENT to another (A7.5); and for
    /// Subsequent First Fire, whether no target is farther than the closest Known enemy unit (A8.3). Backlog pass 10 adds the
    /// levels between firers and target, the wall or hedge TEM, Height Advantage, a Bypassing target's terrain, Snap Shots,
    /// TPBF, and Hazardous Movement (rulings R10.4 to R10.8, R10.13, R10.14).
    /// </summary>
    private (FireAttack? Facts, string? Reason) FireMapFacts(GameState state, FireAttack attack, BoardLocation target, IReadOnlyList<GameEvent>? history = null)
    {
        if (ReadLocation(state, target) is not { } targetRead)
        {
            return (null, "play.fire-map: the target Location cannot be read");
        }

        if (TerrainKey(targetRead) is not { } terrain)
        {
            var name = (targetRead.Level.Terrain ?? targetRead.Hex.Center.Terrain)?.Name;
            return (null, $"play.fire-terrain: the target's terrain ({name ?? "unknown"}) has no TEM in the Fire package");
        }

        // Rulings R10.5, R10.6: walls and hedges are reviewed for Infantry fire; ordnance, a vehicle's fire, and Residual FP keep refusing them.
        // Table player, pass 10: Residual FP crosses no hexside, so it takes no wall TEM and needs no refusal.
        var direct = attack.FireKind != ScenarioA1FireCalculator.ResidualFire && attack.OrdnanceHit is null && attack.VehicleFire is null;
        if (!direct && attack.FireKind != ScenarioA1FireCalculator.ResidualFire && targetRead.Hex.Hexsides.Any(side => side.HexsideTerrain is not null || side.Cliff))
        {
            return (null, "play.fire-terrain: hexside terrain at the target is not reviewed for ordnance or a vehicle's fire (ruling R10.5)");
        }

        // B15.6 (ruling R5.19): grain is Open Ground outside June to September, where FFMO applies to a unit moving in it; with no scenario
        // month, fire at a moving unit in grain is not decided.
        if (terrain == "grain")
        {
            if (state.ScenarioMonth is not { } month)
            {
                if (state.Phase == "mph")
                {
                    return (null, "play.fire-grain: grain is Open Ground outside its season, which the game does not name, so FFMO there is not decided (B15.6)");
                }
            }
            else if (month is < 6 or > 9)
            {
                terrain = "open-ground";
            }
        }

        // A4.3, A4.34, B23.31 (ruling R10.7): a stack in Bypass is in the other terrain of the hexsides it moves along, not in the obstacle.
        var lane = (direct || attack.FireKind == ScenarioA1FireCalculator.ResidualFire) && state.Phase == "mph" && state.Movement is { Bypass: { Count: > 0 } bypassed } bypassing
            && bypassing.Location == target ? bypassed : null;
        if (lane is not null)
        {
            string?[] laneTerrain = [.. lane.Select(side => HexsideAt(state, target, side))
                .Select(side => side?.Terrain?.Name is { } name ? FireTerrain.GetValueOrDefault(name) ?? (side.Terrain.IsRoad ? "open-ground" : null) : null)];
            if (laneTerrain.Length == 0 || laneTerrain.Any(key => key is null))
            {
                return (null, "play.fire-bypass: the terrain the Bypassing stack moves through is not reviewed (ruling R10.7)");
            }

            terrain = laneTerrain.OrderByDescending(key => ScenarioA1FireReference.Tem[key!]).First()!;

            // Table player, pass 10: whether units in the obstacle and a stack Bypassing it share a Location for TPBF is not built, nor a Snap Shot at
            // a Bypass step.
            if (direct && ((attack.Firers ?? []).Any(item => item.LocationId == target.ToString()) || attack.SnapShot == true))
            {
                return (null, "play.fire-bypass: fire from within the Bypassed hex, and a Snap Shot at a Bypass step, are not built (A4.34, A8.15; ruling R10.7)");
            }

            // A4.34 (referee, pass 10): a wall or hedge of the hex applies when the LOS crosses it, which the center LOS cannot tell.
            if (targetRead.Hex.Hexsides.Any(side => WallOn(side) is not null))
            {
                return (null, "play.fire-bypass: fire at a Bypassing stack in a hex with a wall or hedge needs the vertex LOS, which is not built (A4.34; ruling R10.7)");
            }
        }

        // D9.3, D10.3 (ruling R6.1): the wreck or AFV whose +1 TEM the target Location's Infantry may claim.
        var infantrySide = attack.Targets!.Select(item => state.Unit(item.UnitId!)?.Side).FirstOrDefault(side => side is not null);
        attack = attack with
        {
            AfvCover = CoverAt(state, target, infantrySide)
        };
        if (attack.FireKind == ScenarioA1FireCalculator.ResidualFire)
        {
            // A8.2 (rulings R6.6, R9.6): Residual FP has no LOS Hindrance, but the SMOKE of its Location applies: a burning wreck's and each grenade
            // counter's +2, at most +3, never the outgoing +1 (B25.2, A24.8).
            var smoke = Math.Min(3, 2 * SmokeSources(state).Count(place => place == target));
            return (attack with
            {
                TargetTerrain = terrain,
                Los = smoke > 0 ? new FireLos(false, smoke, true, false) : null,
            }, null);
        }

        var firingSide = attack.VehicleFire is { } vehicleFire ? state.Unit(vehicleFire.VehicleId!)?.Side : state.Unit(attack.Firers![0].UnitId!)?.Side;
        var targetHeight = targetRead.Hex.BaseLevel + target.Level;

        // A8.15 (ruling R10.13): a Snap Shot is traced to both ends of the hexside the stack crossed into the target hex.
        BoardLocation? snapHexside = null;
        BoardLocation? left = null;
        if (attack.SnapShot == true)
        {
            if (!direct || state.Phase != "mph" || state.Movement is not { From: { } came } snapped || snapped.Location != target || SideToward(state, target, came) is not { } crossedSide)
            {
                return (null, "play.fire-snap-shot: a Snap Shot is Infantry Defensive First Fire at the hexside the moving stack just crossed (A8.15)");
            }

            // A8.15, B9.42 (referee, pass 10): a wall, hedge, SMOKE, or rubble of either hex can modify a Snap Shot; that is not built.
            if (new[] { target, came }.Any(hex => ReadLocation(state, hex) is not { } hexRead || hexRead.Hex.Hexsides.Any(side => WallOn(side) is not null)
                || IsRubbleTerrain(TerrainKey(hexRead)) || HasSmoke(state, hex)))
            {
                return (null, "play.fire-snap-shot: a Snap Shot at a hexside of a hex with a wall, hedge, SMOKE, or rubble is not built (A8.15; ruling R10.13)");
            }

            snapHexside = new BoardLocation(target.Board, target.Hex, 0, crossedSide);
            left = came;
        }

        var perLocation = new Dictionary<string, (int Range, bool SameLevel, FireLos Los, int Height)>(StringComparer.Ordinal);
        foreach (var location in (attack.Firers ?? []).Select(item => item.LocationId!).Append(attack.FirerLocationId!).Distinct(StringComparer.Ordinal))
        {
            var from = BoardLocation.Parse(location);
            if (ReadLocation(state, from) is not { } firerRead)
            {
                return (null, "play.fire-map: a firer's Location cannot be read");
            }

            var height = firerRead.Hex.BaseLevel + from.Level;
            if (direct)
            {
                // B16.32: fire from a marsh hex is limited and Area Fire, which is not built.
                if (TerrainKey(firerRead) == "marsh")
                {
                    return (null, "play.fire-marsh: fire from a marsh hex is limited to some weapons and resolved as Area Fire, which is not built (B16.32; ruling R10.1)");
                }

                // A7.212 (ruling R10.14): a unit whose Location holds a Known enemy unit fires at nothing else.
                if (from != target && state.At(from).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != firingSide && KnownEnemy(unit)
                    && !(LiveFire.IsVehicle(unit) && !HasVehicleMg(unit) && unit.Definition is { } vehicleDefinition
                        && FireReference.Value.Definitions.GetValueOrDefault(vehicleDefinition.Definition)?.Unarmored == true)))
                {
                    return (null, $"play.fire-target-limit: {from} holds a Known enemy unit, so its units fire only at their own Location (A7.212)");
                }
            }

            // A7.21 (ruling R10.14): TPBF at the enemy units of the firer's own Location.
            if (from == target)
            {
                if (!direct || attack.SnapShot == true)
                {
                    return (null, "play.fire-own-location: only Infantry fire as TPBF at units in their own Location (A7.21)");
                }

                // D7.22 (table-player finding, pass 11): in the MPh, fire at a moving vehicle in the firer's own Location is Non-CC Reaction Fire, made
                // only after its OVR there; CC Reaction Fire is the vehicle CC action (D7.21).
                if (state.Phase == "mph" && state.Movement is { Vehicle: true, Reaction: false } moving
                    && moving.Movers.Any(id => state.Location(id)?.Location == from))
                {
                    return (null, "play.fire-reaction: a DEFENDER unit fires at a moving vehicle in its own Location only as Reaction Fire after the vehicle's OVR there (D7.22); CC Reaction Fire is the vehicle CC action (D7.21)");
                }

                perLocation[location] = (0, true, new FireLos(false, 0, true, false), height);
                continue;
            }

            LosResult? los;
            int range;
            if (snapHexside is { } hexside)
            {
                var (first, second) = LosToHexside(state, from, hexside);
                if (first is not { Status: LosStatus.Clear or LosStatus.Blocked } || second is not { Status: LosStatus.Clear or LosStatus.Blocked })
                {
                    return (null, "play.fire-los: the LOS read to the crossed hexside gives no definitive answer");
                }

                if (first.IsBlocked == true || second.IsBlocked == true)
                {
                    return (null, "play.fire-snap-shot: the firer has no LOS to the whole hexside crossed (A8.15)");
                }

                los = first.Hindrances.Sum(item => item.Value) >= second.Hindrances.Sum(item => item.Value) ? first : second;
                range = Math.Min(HexDistance(state, from, target) ?? int.MaxValue, HexDistance(state, from, left!) ?? int.MaxValue);
                if (range == int.MaxValue || HexDistance(state, from, target) == 0)
                {
                    return (null, "play.fire-snap-shot: a Snap Shot is not taken at a unit entering the firer's hex, and its range must be read (A8.15)");
                }
            }
            else
            {
                los = Los(state, from, target);
                if (los is null)
                {
                    return (null, "play.fire-los: the board has no LOS data to read");
                }

                if (los.Status is not (LosStatus.Clear or LosStatus.Blocked))
                {
                    return (null, $"play.fire-los: the LOS read gives no definitive answer ({los.Status}: {los.Reason})");
                }

                range = los.Range;
            }

            // A4.34 (ruling R10.7): LOS to a Bypassing stack's hex center must cross a hexside it Bypasses; the vertex LOS is not built.
            if (lane is not null && (LosEntrySides(state, target, from) is not { } entering || !entering.Any(lane.Contains)))
            {
                return (null, "play.fire-bypass-los: the LOS to the Bypassing stack does not cross a hexside it Bypasses; LOS to its vertices is not built (A4.34; ruling R10.7)");
            }

            // A6.7: the largest Hindrance at each range counts; brush always, grain June to September (B15.2), marsh at the same level (B16.2); and an
            // AFV or wreck where the map has none at that range, and a burning wreck's smoke (D9.4, B25.2; rulings R6.2, R6.3).
            var inSeason = state.ScenarioMonth is >= 6 and <= 9;
            var sameLevel = height == targetHeight;
            var attributed = los.Hindrances.All(entry => entry.Terrains.Count > 0 && entry.Terrains.All(item => item is "Brush" or "Grain" or "Marsh"));
            var mapRanges = los.Hindrances.Where(entry => entry.Terrains.Contains("Brush") || (sameLevel && entry.Terrains.Contains("Marsh")) || (inSeason && entry.Terrains.Contains("Grain")))
                .Select(entry => entry.Range).ToHashSet();
            var grain = los.Hindrances.Any(entry => entry.Terrains.Contains("Grain"));
            var (vehicleDrm, vehicleReason) = VehicleHindrance(state, from, target, los, sameLevel, mapRanges);
            if (vehicleReason is not null)
            {
                return (null, vehicleReason);
            }

            perLocation[location] = (range, sameLevel, new FireLos(los.IsBlocked == true, mapRanges.Count + vehicleDrm, attributed, grain), height);
        }

        var firstLocation = perLocation[attack.FirerLocationId!];
        var facts = attack with
        {
            Range = firstLocation.Range,
            SameLevel = firstLocation.SameLevel,
            TargetLevelAbove = firstLocation.SameLevel ? null : targetHeight - firstLocation.Height,
            Los = firstLocation.Los,
            TargetTerrain = terrain,
        };
        if (perLocation.Count > 1)
        {
            // A7.5: every Location of the group ADJACENT to another of them.
            var locations = perLocation.Keys.Select(BoardLocation.Parse).ToArray();
            facts = facts with
            {
                Firers = [.. attack.Firers!.Select(firer => firer with
                {
                    Range = perLocation[firer.LocationId!].Range,
                    SameLevel = perLocation[firer.LocationId!].SameLevel,
                    TargetLevelAbove = perLocation[firer.LocationId!].SameLevel ? null : targetHeight - perLocation[firer.LocationId!].Height,
                    Los = perLocation[firer.LocationId!].Los,
                })],
                FirerLocationsAdjacent = locations.All(one => locations.Any(two => two != one && IsAdjacent(state, one, two))),
            };
        }

        if (direct)
        {
            var sources = perLocation.Where(pair => pair.Value.Range > 0).Select(pair => (From: BoardLocation.Parse(pair.Key), pair.Value.Range, pair.Value.Height)).ToArray();

            // B9.3 to B9.41 (rulings R10.5, R10.6): the wall or hedge TEM; a Snap Shot, TPBF, and a Bypassing stack take none.
            if (attack.SnapShot != true && lane is null && sources.Length == perLocation.Count && sources.Length > 0)
            {
                var (wall, wallReason) = HexsideTemAt(state, target, [.. sources.Select(item => (item.From, item.Range))], firingSide!, history);
                if (wallReason is not null)
                {
                    return (null, wallReason);
                }

                facts = facts with
                {
                    HexsideTem = wall
                };
            }

            // B10.31 (ruling R10.4): Height Advantage over every firer; A4.62 (ruling R10.8): a crew pushing its Gun is under Hazardous Movement.
            facts = facts with
            {
                HeightAdvantage = sources.Length == perLocation.Count
                    && HeightAdvantageAt(state, target, targetRead, [.. sources.Select(item => (item.From, item.Height))], attack.SnapShot == true) ? true : null,

                // A4.62 (referee, pass 10): only the pushing crew is under Hazardous Movement.
                HazardousMovement = state.Phase == "mph" && state.Movement is { PushedGun: { } pushedGun } pushing && pushing.Location == target
                    && state.Find(pushedGun) is EquipmentInstance { Holding: { } manning } && attack.Targets!.Count > 0
                    && attack.Targets.All(item => item.UnitId == manning.Holder) ? true : null,
            };
        }

        if (attack.FireKind == ScenarioA1FireCalculator.SubsequentFirstFire)
        {
            // A8.3: no farther than the closest armed, Known enemy unit, from each firer's Location.
            var side = state.Unit(attack.Firers![0].UnitId!)!.Side;
            var enemies = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && unit.Kind != UnitKinds.Dummy
                    && VisibleTo(unit, side) && GameState.Condition(unit, Conditions.Captured) != ConditionState.True
                    && (!LiveFire.IsVehicle(unit) || HasVehicleMg(unit)))
                .Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>().Distinct().ToArray();
            facts = facts with
            {
                WithinSubsequentFirstFireRange = perLocation.All(pair => enemies
                    .Select(enemy => pair.Value.Range == 0 ? 0 : Los(state, BoardLocation.Parse(pair.Key), enemy)?.Range ?? int.MaxValue).DefaultIfEmpty(int.MaxValue).Min() >= pair.Value.Range),
            };
        }

        return (facts, null);
    }
}
