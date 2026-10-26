using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Scrounging (D10.5 to D10.52, p. 211; pass 65, task 65.9, ruling R65.23): an Infantry MMC in its own RPh salvages the SW of a non-burning wreck or an
/// Abandoned vehicle in its Location. Rules decides the bar, what may be taken, and how many; the plan reads the state and the index, draws the dr and the
/// Random Selection, and writes the SW taken and <c>vehicle-scrounged</c>.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// Whether a vehicle may be scrounged now, by itself alone (D10.5): a non-burning wreck or an Abandoned vehicle on the map, not Scrounged, and not marked
    /// Disabled (either mount's counter, D6.631); for the Play page, which shows the plan's refusal for the rest.
    /// </summary>
    public static bool MayBeScrounged(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        return LiveFire.IsVehicle(vehicle) && vehicle.Position is MapPosition
            && (vehicle.Status == InstanceStatus.Wrecked ? !IsBurning(state, vehicle) : vehicle.Status == InstanceStatus.Active && Is(vehicle, Conditions.Abandoned))
            && !Is(vehicle, Conditions.Scrounged) && !Is(vehicle, Conditions.Disabled) && !Is(vehicle, Conditions.BmgDisabled);
    }

    /// <summary>
    /// The units that might scrounge a vehicle (D10.5): the phasing side's MMC on foot in its Location, read from the state's index; for the Play page, which
    /// proposes and shows the plan's refusal for those that may not.
    /// </summary>
    public IReadOnlyList<UnitInstance> ScroungeCandidates(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        var index = MemoOf(state).Objects(state);
        return index.Location(vehicle.Id) is { } at
            ? [.. index.At(at.Location).OfType<UnitInstance>().Where(unit => unit.Side == state.PhasingSide && ScenarioA1Definitions.MmcKinds.Contains(unit.Kind))]
            : [];
    }

    /// <summary>The mounts of a vehicle a Scrounging may salvage (D10.51), from its catalog definition and its conditions.</summary>
    private static IReadOnlyList<string> ScroungeableMounts(UnitInstance vehicle)
    {
        var definition = VehicleDefinition(vehicle);
        return ScenarioA1WreckRules.Scroungeable(definition?.MainArmament, definition?.AntiAircraftMg, definition?.BowMg, definition?.FixedBmg == true,
            Is(vehicle, Conditions.Malfunctioned), Is(vehicle, Conditions.BmgMalfunctioned), Is(vehicle, Conditions.BmgDisabled));
    }

    /// <summary>
    /// A Scrounging (D10.5 to D10.52; ruling R65.23). The dr, a Random Selection among the mounts when the dr is lower than their number, the SW taken as
    /// counters the unit possesses, TI (a MG as the nationality's LMG, a mortar as its light mortar, each in its dm form where it has one), and
    /// <c>vehicle-scrounged</c>, which marks the vehicle Scrounged and its scroungeable mounts Disabled whatever the dr.
    /// </summary>
    private GamePlan PlanScrounge(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "vehicleId", out var vehicleId) || state.Unit(unitId) is not { Status: InstanceStatus.Active } unit
            || state.Unit(vehicleId) is not { Status: InstanceStatus.Active or InstanceStatus.Wrecked } vehicle || !LiveFire.IsVehicle(vehicle))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Scrounging names a unit in play and a vehicle or wreck on the map");
        }

        var index = MemoOf(state).Objects(state);
        var at = vehicle.Position is MapPosition ? index.Location(vehicleId)?.Location : null;
        var wreck = vehicle.Status == InstanceStatus.Wrecked;
        if (ScenarioA1WreckRules.ScroungeBar(new ScroungeFacts(unitId, vehicleId, state.Phase == "rph" && unit.Side == state.PhasingSide,
            ScenarioA1Definitions.MmcKinds.Contains(unit.Kind), !Is(unit, Conditions.Broken) && !Is(unit, Conditions.Captured), Is(unit, Conditions.Berserk),
            Is(unit, Conditions.Melee), Is(unit, "asl:ti"), at is not null && unit.Position is MapPosition && index.Location(unitId)?.Location == at, wreck,
            wreck && IsBurning(state, vehicle), Is(vehicle, Conditions.Abandoned), Is(vehicle, Conditions.Scrounged),
            Is(vehicle, Conditions.Disabled) || Is(vehicle, Conditions.BmgDisabled))) is { } bar)
        {
            return Refused(scope, label, expected, bar);
        }

        // D10.5, D10.52: what is taken is of the vehicle's nationality, a U.S. LMG a British one; the catalog must hold the counter for each mount.
        var definition = VehicleDefinition(vehicle);
        var mounts = ScroungeableMounts(vehicle);
        var nationality = ScenarioA1WreckRules.ScroungedNationality(definition?.Nationality);
        var counters = mounts.ToDictionary(mount => mount, mount => ScenarioA1WreckRules.ScroungedDefinition(FireReference.Value.Definitions.Values, nationality, mount,
            definition?.MainArmament), StringComparer.Ordinal);
        if (counters.FirstOrDefault(item => item.Value is null) is { Key: { } missing })
        {
            return Refused(scope, label, expected, $"play.scrounge: the catalog has no SW of {nationality} for {vehicleId}'s {ScenarioA1WreckRules.MountText(missing)} to become (D10.52)");
        }

        // A12.141 (p. 78; ruling R70.17): Scrounging is an activity, and costs a "?" in the LOS of a Good Order enemy within 16 hexes.
        var lost = ActionLosesConcealment(state, [unit]);
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var dr = draw(new RollRequest(1, 6)).Values[0];
            var rollId = $"{attemptId}-roll-1";
            var events = new List<GameEvent>
            {
                Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, ScenarioA1WreckRules.Purpose, 1, 6, [dr], DiceRolled.SystemSource, actor), null, null),
            };

            // A.9: a Random Selection, one die a mount, only when the dr finds more SW than it may take.
            string? selectionId = null;
            IReadOnlyList<int>? selection = null;
            if (ScenarioA1WreckRules.SelectionNeeded(dr, mounts.Count))
            {
                selection = draw(new RollRequest(mounts.Count, 6)).Values;
                selectionId = $"{attemptId}-roll-2";
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(selectionId, ScenarioA1WreckRules.Purpose, mounts.Count, 6, selection, DiceRolled.SystemSource, actor), null, null));
            }

            var taken = new List<ScroungedWeapon>();
            foreach (var mount in ScenarioA1WreckRules.Taken(mounts, dr, selection) ?? [])
            {
                var counter = counters[mount]!;
                var sw = FireReference.Value.Definitions[counter];
                var id = $"{attemptId}-{vehicleId}-{mount}";
                var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal)
                {
                    [Conditions.Malfunctioned] = ConditionState.False,
                    ["asl:ti"] = ConditionState.True,
                };
                if (ScenarioA1WreckRules.ScroungedDismantled(sw.Kind, sw.Nationality, sw.Size, null))
                {
                    conditions[Conditions.Dismantled] = ConditionState.True;
                }

                events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-created",
                    new InstanceCreated(new NewInstance(id, sw.Kind, counter, unit.Side, null, new Holding(unitId, HoldingRole.Possessed), conditions)), null, null));
                taken.Add(new ScroungedWeapon(id, mount));
            }

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "vehicle-scrounged", new VehicleScrounged(vehicleId, at!, unitId, rollId, dr)
            {
                Selection = selectionId,
                Taken = taken.Count > 0 ? taken : null,
                Mounts = mounts.Count > 0 ? mounts : null,
            }, null, null, [EventId(attemptId, 1)]));
            events.AddRange(RevealEvents(scope, attemptId, expected, events.Count + 1, lost));
            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [ScenarioA1WreckRules.ScroungeText(unitId, vehicleId, at!.ToString(), mounts), .. ConcealmentLossNote(lost)])
        {
            Roll = new PlannedRoll(ScenarioA1WreckRules.Purpose, Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }
}
