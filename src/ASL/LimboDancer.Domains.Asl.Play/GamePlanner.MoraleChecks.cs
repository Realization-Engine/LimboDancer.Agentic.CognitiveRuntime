using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The morale checks made outside an attack (pass 70, task 70.1; rulings R70.3 to R70.9): the Heat of Battle an Interdiction NMC's Original 2 calls for, the
/// LLMC and LLTC after a leader lost outside fire, and the Bail Out NMC. The Morale Check member decides each; this file reads the state and the map, hands
/// the facts over, draws the dice, and writes the record and its effects through the Fire planner's own effect events.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// A unit as a subject or other unit of a morale check (rulings R70.3, R70.7, R70.8, R70.17): its Fire target facts with Unarmed, the vehicle it rides,
    /// whether it is Encircled, whether a Good Order enemy ground unit within 16 hexes sees it when concealed, and the reads its Heat of Battle needs.
    /// </summary>
    private FireTarget MoraleTarget(GameState state, UnitInstance unit, BoardLocation at)
    {
        var concealed = Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden);
        return LiveFire.Target(state, unit, at) with
        {
            Unarmed = Is(unit, Conditions.Unarmed) ? true : null,
            Aboard = AboardFacts(state, unit),
            Encircled = Encircled(state, unit) ? true : null,
            SeenByGoodOrderEnemy = concealed ? EnemyGoodOrderInLosWithin16(state, unit.Side, at) : null,
            KnownEnemyInLos = KnownEnemyInLos(state, unit.Side, at),
            Captors = Captors(state, unit),
        };
    }

    /// <summary>
    /// The vehicle a Passenger or Rider rides (D6.651; ruling R70.8), read from the state's index: its role, whether it has moved this phase (C.8), and for a
    /// Passenger whether it is CE in an armored halftrack that has not; null for a unit on foot.
    /// </summary>
    private FireAboard? AboardFacts(GameState state, UnitInstance unit)
    {
        if (unit.Position is not ContainedPosition { Role: ContainmentRole.Passenger or ContainmentRole.Rider } contained
            || MemoOf(state).Objects(state).VehicleOf(unit.Id) is not { } vehicleId || state.Unit(vehicleId) is not { } vehicle)
        {
            return null;
        }

        var rider = contained.Role == ContainmentRole.Rider;
        var moved = VehicleMoved(state, vehicle, false);
        var ceInHalftrack = !rider && !moved && LiveFire.VehicleClassOf(vehicle.Definition?.Definition) == "halftrack"
            && VehicleDefinition(vehicle) is { Unarmored: not true } definition && PassengersExposed(vehicle, definition);
        return new FireAboard(vehicleId, rider ? FirePrc.Rider : FirePrc.Passenger, moved, ceInHalftrack ? true : null);
    }

    /// <summary>
    /// Draws the rolls the Morale Check member asks for, one at a time, each as a <c>dice-rolled</c> event, and returns its resolution and the rolls by key
    /// (ruling R70.3). A check that reaches an option is resolved with every option taken (the facts carry no choices).
    /// </summary>
    private (FireResolution Resolution, Dictionary<string, string> RollIds, FireRolls Rolls) ResolveMoraleCheck(GameScope scope, string attemptId, long expected, string actor,
        List<GameEvent> events, Func<RollRequest, RollResult> draw, MoraleCheckFacts facts, IReadOnlyDictionary<string, string>? preset, IReadOnlyList<string>? causes)
    {
        var package = ScenarioA1FirePackage.Identity.ToString();
        var rollIds = new Dictionary<string, string>(preset ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        var rolls = facts.Rolls ?? new FireRolls(null, null, null, null, null);
        while (true)
        {
            var resolution = ScenarioA1MoraleCheck.Resolve(facts with
            {
                Rolls = rolls
            }, FireReference.Value);
            var next = ScenarioA1FireFollowUps.NextFireStep(resolution);
            if (next.Resolved)
            {
                return (resolution, rollIds, rolls);
            }

            if (next.RollKey is not { } key)
            {
                throw new InvalidOperationException("The Morale Check member left a check undecided: " + (next.Undecided ?? next.ChoiceKey));
            }

            var drawn = draw(new RollRequest(next.Count, 6));
            var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
            rollIds[key] = rollId;
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                new DiceRolled(rollId, next.Purpose!, next.Count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null, causes));
            rolls = ApplyFireRoll(rolls, key, drawn.Values);
        }
    }

    /// <summary>
    /// A morale check made outside an attack, resolved and written (ruling R70.4): its dice, the <c>morale-check-resolved</c> record, its effects as a fire
    /// record's are written, and a pending surrender last. Nothing is written when the check holds no check at all. A check of a concealed unit that keeps its
    /// "?" is its side's own business: the record, its dice, and its effects are withheld from the other side (A12.14). A concealed or hidden unit that took
    /// no check is left out of the record, or, when the check would resolve otherwise without it, the record and its dice are withheld (A12.14, A12.3; the
    /// referee's finding at the reviews). The resolution, or null when nothing was written.
    /// </summary>
    private FireResolution? AddMoraleCheck(GameScope scope, string attemptId, long expected, string actor, List<GameEvent> events, Func<RollRequest, RollResult> draw,
        GameState state, MoraleCheckFacts facts, IReadOnlyDictionary<string, string>? preset, string side, BoardLocation at, string cause, IReadOnlyList<string>? causes)
    {
        var firstDie = events.Count;
        var (resolution, rollIds, rolls) = ResolveMoraleCheck(scope, attemptId, expected, actor, events, draw, facts, preset, causes);
        if (!ScenarioA1MoraleCheck.AnyCheck(resolution))
        {
            return null;
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        var withheld = false;
        if (ScenarioA1MoraleCheck.Unnamed(resolution, facts.Others ?? []) is { Count: > 0 } unnamed)
        {
            var reduced = facts with
            {
                Others = [.. facts.Others!.Where(unit => !unnamed.Contains(unit.UnitId!))]
            };
            var without = ScenarioA1MoraleCheck.Resolve(reduced with
            {
                Rolls = rolls
            }, FireReference.Value);
            var expectedWithout = resolution with
            {
                Effects = [.. resolution.Effects.Where(effect => !unnamed.Contains(effect.UnitId))]
            };
            if (JsonSerializer.Serialize(without, LiveFire.Json) == JsonSerializer.Serialize(expectedWithout, LiveFire.Json))
            {
                (facts, resolution) = (reduced, without);
            }
            else
            {
                withheld = true;
            }
        }

        FireTarget[] named = [.. facts.Subjects ?? [], .. facts.Others ?? []];
        var visibility = withheld || ScenarioA1MoraleCheck.Withheld(resolution, named) ? new[] { side } : null;

        // A12.14 (the referee's finding at the reviews): the check's own dice are as public as its record, since their number counts the units it checked.
        if (visibility is not null)
        {
            for (var index = firstDie; index < events.Count; index++)
            {
                events[index] = events[index] with
                {
                    Visibility = visibility
                };
            }
        }

        var recordId = EventId(attemptId, events.Count + 1);
        // Pass 72 (ruling R72.19): the record's stamp, so the verifier checks every unit's SSR ELR against the state.
        var recorded = facts with
        {
            Rolls = null,
            UnitSubstitution = true,
        };
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "morale-check-resolved", new MoraleCheckResolved([.. named.Select(unit => unit.UnitId!)], at.ToString(), cause,
            rollIds, JsonSerializer.SerializeToElement(recorded, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json)),
            package, visibility, causes));
        foreach (var effect in resolution.Effects)
        {
            // What a check did to a unit that keeps its "?" is its side's own; what it did to any other unit both sides see.
            var alone = resolution with
            {
                Effects = [effect]
            };
            var own = visibility is not null && ScenarioA1MoraleCheck.Withheld(alone, named) ? visibility : null;
            foreach (var payload in EffectEvents(state, effect, attemptId, false))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, payload.Type, payload.Payload, package, own, [recordId]));
            }
        }

        // A15.41: the companions a berserk leader took with him.
        foreach (var effect in resolution.CompanionEffects ?? [])
        {
            if (EffectEvent(state, effect, attemptId, false) is { } companion)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, companion.Type, companion.Payload, package, visibility, [recordId]));
            }
        }

        // A15.5: a unit that surrendered to ADJACENT captors waits for the captor's choice, last.
        foreach (var (id, captors) in ScenarioA1FireFollowUps.SurrenderPendings(resolution, attemptId))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "surrender-pending", new SurrenderPending(id, captors), package, null, [recordId]));
        }

        return resolution;
    }

    /// <summary>
    /// The LLMC and LLTC after leaders lost outside fire (A10.2, p. 65: "For each leader eliminated (whether by breaking when already broken, a mortal wound,
    /// or any other cause)"; pass 70, ruling R70.3): for each Location a lost leader stood in, one morale check of the other friendly units there, as
    /// <paramref name="after"/> holds them, with the leaders as <paramref name="before"/> held them. A unit not of the catalog, a vehicle, a Dummy, and a
    /// prisoner take none. <paramref name="lost"/> names each leader and whether he was eliminated (otherwise broken while unbroken).
    /// </summary>
    private void AddLeaderLossChecks(GameScope scope, string attemptId, long expected, string actor, List<GameEvent> events, Func<RollRequest, RollResult> draw,
        GameState before, GameState after, IReadOnlyList<(string Id, bool Eliminated)> lost, string cause, IReadOnlyList<string>? causes)
    {
        foreach (var (facts, side, at) in LeaderLossFacts(before, after, lost))
        {
            AddMoraleCheck(scope, attemptId, expected, actor, events, draw, after, facts, null, side, at, cause, causes);
        }
    }

    /// <summary>Whether a leader lost outside fire calls on any unit for a LLMC or LLTC (A10.2), read before any roll: whether a plan must roll for it.</summary>
    private bool LeaderLossDue(GameState before, GameState after, IReadOnlyList<(string Id, bool Eliminated)> lost) =>
        LeaderLossFacts(before, after, lost).Any(item => ScenarioA1MoraleCheck.Resolve(item.Facts with
        {
            Rolls = new FireRolls(null, null, null, null, null)
        }, FireReference.Value).Disposition != FireResolution.Resolved);

    /// <summary>The facts of the morale checks <see cref="AddLeaderLossChecks"/> makes, one for each Location a lost leader stood in, with its side.</summary>
    private List<(MoraleCheckFacts Facts, string Side, BoardLocation At)> LeaderLossFacts(GameState before, GameState after, IReadOnlyList<(string Id, bool Eliminated)> lost)
    {
        var made = new List<(MoraleCheckFacts, string, BoardLocation)>();
        var beforeIndex = MemoOf(before).Objects(before);
        var afterIndex = MemoOf(after).Objects(after);
        var leaders = lost.Select(item => (item.Eliminated, Unit: before.Unit(item.Id), At: beforeIndex.Location(item.Id)?.Location))
            .Where(item => item.Unit is { Status: InstanceStatus.Active } unit && DefinitionOf(unit)?.IsLeader == true && item.At is not null).ToArray();
        foreach (var group in leaders.GroupBy(item => item.At!))
        {
            var at = group.Key;
            if (group.First().Unit!.Side is not { } side)
            {
                continue;
            }

            // The units of the Location from the state's index, those on foot and those aboard its vehicles (PRC, and a unit that crews one, D5.42); Rules
            // picks who takes the check (the Rules boundary review), none for a side with no ELR declared (A19.13).
            var gone = group.Select(item => item.Unit!.Id).ToHashSet(StringComparer.Ordinal);
            UnitInstance[] present = [.. afterIndex.At(at).OfType<UnitInstance>().SelectMany(unit => LiveFire.IsVehicle(unit)
                ? [unit, .. afterIndex.Aboard(unit.Id), .. afterIndex.CrewOf(unit.Id) is { } crew ? new[] { crew } : []] : new[] { unit })];
            var picked = ScenarioA1MoraleCheck.LeaderLossSubjects([.. present.Select(unit => new LeaderLossUnitFacts(unit.Id, unit.Status == InstanceStatus.Active,
                unit.Side == side, gone.Contains(unit.Id), LiveFire.IsVehicle(unit), unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Captured), DefinitionOf(unit) is not null))],
                after.Side(side)?.Elr).ToHashSet(StringComparer.Ordinal);
            UnitInstance[] here = [.. present.Where(unit => picked.Contains(unit.Id))];
            if (here.Length == 0)
            {
                continue;
            }

            var facts = new MoraleCheckFacts(MoraleCheckFacts.None, [], after.Side(side)?.Elr, null)
            {
                Others = [.. here.Select(unit => MoraleTarget(after, unit, at))],
                LostLeaders = Leaders(group.Where(item => item.Eliminated)),
                BrokenLeaders = Leaders(group.Where(item => !item.Eliminated)),
                NoQuarter = after.NoQuarter.Contains(side, StringComparer.Ordinal) ? true : null,
                InMelee = here.Any(unit => Is(unit, Conditions.Melee)) || group.Any(item => Is(item.Unit!, Conditions.Melee)) ? true : null,
            };
            made.Add((facts, side, at));

            FireTarget[]? Leaders(IEnumerable<(bool Eliminated, UnitInstance? Unit, BoardLocation? At)> items) =>
                items.Select(item => LiveFire.Target(before, item.Unit!, at) with { Aboard = AboardFacts(before, item.Unit!) }).ToArray() is { Length: > 0 } list ? list : null;
        }

        return made;
    }
}
