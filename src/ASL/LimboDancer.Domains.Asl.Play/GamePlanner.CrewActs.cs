using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The crew's own acts (pass 65, task 65.6; ruling R65.20): a voluntary Abandonment in the crew's own MPh (D5.41, D5.43), with D5.411's self-destruction
/// and D6.631's MG Removal, and an Infantry unit's entry into an Abandoned vehicle to crew it (D5.42, D5.341), with the Armor Leader and the MG it brings
/// back (D3.43, D6.631). Rules decides each bar; the plans read the state, the index, and the records, and write the events.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The unit that crews a vehicle from inside after a re-crewing (D5.42; ruling R65.20), from the state's index; null for an inherent crew.</summary>
    public UnitInstance? CrewInsideOf(GameState state, string vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        return MemoOf(state).Objects(state).CrewOf(vehicle);
    }

    /// <summary>
    /// The units that might enter an Abandoned vehicle to crew it (D5.42): its side's Personnel on foot in its Location that have not moved this MPh, read
    /// from the state's index; for the Play page, which proposes and shows the plan's refusal for the rest.
    /// </summary>
    public IReadOnlyList<UnitInstance> RecrewCandidates(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        var index = MemoOf(state).Objects(state);
        return vehicle.Status == InstanceStatus.Active && Is(vehicle, Conditions.Abandoned) && vehicle.Position is MapPosition && index.Location(vehicle.Id) is { } at
            ? [.. index.At(at.Location).OfType<UnitInstance>().Where(unit => unit.Side == vehicle.Side && !LiveFire.IsVehicle(unit) && vocabulary.IsA(unit.Kind, "asl:personnel")
                && unit.MfSpent == 0 && !unit.HalfMfSpent && !unit.MovementEnded)]
            : [];
    }

    /// <summary>
    /// D5.311 (ruling R65.31): why a vehicle whose crew is broken inside it neither moves nor fires, or null. One scan of the state's units, made only when a
    /// vehicle's own move or fire is planned, never in a loop; static, since the fire packages that ask it (LiveFire, LiveOrdnance) hold no planner's index.
    /// </summary>
    internal static string? BrokenCrewBar(GameState state, UnitInstance vehicle) =>
        ScenarioA1CrewActsRules.BrokenCrewBar(vehicle.Id, state.Units.FirstOrDefault(unit => unit.Status == InstanceStatus.Active
            && unit.Position is ContainedPosition { Role: ContainmentRole.Crew } inside && inside.Container == vehicle.Id) is { } crew && Is(crew, Conditions.Broken) ? crew.Id : null);

    /// <summary>Whether a vehicle is armed, so manned by a crew that may leave it (D5.1): a MA or any MG, as the Victory count reads it.</summary>
    private static bool Armed(FireDefinition? definition) =>
        ScenarioA1VictoryCalculator.VehicleArmed(definition?.MaType is not null, definition?.MainArmament is not null, definition?.AntiAircraftMg is not null,
            definition?.BowMg is not null, definition?.CoaxialMg is not null);

    /// <summary>
    /// Whether the phasing side's crew may Abandon a vehicle now, by the state alone (D5.41): for the Play page, which proposes and shows the plan's
    /// refusal for what the records decide.
    /// </summary>
    public static bool MayAbandon(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        return ScenarioA1CrewActsRules.AbandonBar(vehicle.Id, state.Phase == "mph" && vehicle.Side == state.PhasingSide, vehicle.Position is MapPosition,
            Armed(VehicleDefinition(vehicle)), Is(vehicle, Conditions.Abandoned), state.Movement is not null, VehicleMovedThisPlayerTurn(state, vehicle),
            VehicleFiredThisPlayerTurn(vehicle), Is(vehicle, Conditions.Shocked) || Is(vehicle, Conditions.UnconfirmedKill), Is(vehicle, Conditions.Stunned)) is null;
    }

    /// <summary>Whether a vehicle's crew could Remove its MG as it Abandons it (D6.631), its own inherent crew assumed; for the Play page.</summary>
    public static bool MgRemovable(UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        var definition = VehicleDefinition(vehicle);
        return ScenarioA1CrewActsRules.MgRemoval(vehicle.Id, definition?.VehicleClass, definition?.Unarmored == true, definition?.Nationality, definition?.Nationality, false, false,
            definition?.MainArmament, definition?.AntiAircraftMg, definition?.BowMg, Is(vehicle, Conditions.Disabled), Is(vehicle, Conditions.BmgDisabled)).Bar is null;
    }

    /// <summary>
    /// A voluntary Abandonment (D5.41, D5.43, D5.411, D6.631, D5.8, D3.43; ruling R65.20). The crew, the vehicle's inherent crew placed as its counter or a
    /// unit that crews it from inside, is beneath the vehicle with its MF spent; the vehicle is Abandoned; the DEFENDER may fire at the crew with FFNAM. The
    /// events: the crew counter, the vehicle's weapons malfunctioned or a MG's mount Disabled, the Removed MG's counter, <c>vehicle-abandoned</c>, and a wreck.
    /// </summary>
    private GamePlan PlanAbandon(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "vehicleId", out var id) || state.Unit(id) is not { Status: InstanceStatus.Active } vehicle || !LiveFire.IsVehicle(vehicle))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: an Abandonment names a vehicle in play");
        }

        var definition = VehicleDefinition(vehicle);
        var index = MemoOf(state).Objects(state);
        var at = vehicle.Position is MapPosition ? index.Location(id)?.Location : null;
        var moved = VehicleMovedThisPlayerTurn(state, vehicle);
        var fired = VehicleFiredThisPlayerTurn(vehicle);
        if (ScenarioA1CrewActsRules.AbandonBar(id, state.Phase == "mph" && vehicle.Side == state.PhasingSide, at is not null, Armed(definition), Is(vehicle, Conditions.Abandoned),
            state.Movement is not null, moved, fired, Is(vehicle, Conditions.Shocked) || Is(vehicle, Conditions.UnconfirmedKill), Is(vehicle, Conditions.Stunned)) is { } bar)
        {
            return Refused(scope, label, expected, bar);
        }

        // D5.42 (ruling R65.20): a unit that re-crewed the vehicle leaves as itself; one that re-crewed as a Temporary crew (A21.22) is no Inherent crew,
        // which the vehicle's state holds (the boundary review).
        var inside = index.CrewOf(id);
        var temporary = inside is not null && vehicle.TemporaryCrew is not null;
        string? destroy = Text(arguments, "destroy", out var choice) ? choice : null;
        if (ScenarioA1CrewActsRules.SelfDestructionBar(id, destroy, !temporary, index.Aboard(id).Count > 0) is { } destroyBar)
        {
            return Refused(scope, label, expected, destroyBar);
        }

        InstanceCreated? created = null;
        if (inside is null)
        {
            // D3.43, D5.34: the inherent crew takes the Armor Leader, dormant, and the Stun +1 with it.
            created = LeavingCrew(state, vehicle, attemptId, Is(vehicle, Conditions.StunRecovery), vehicle.ArmorLeader);
            if (created is null)
            {
                return Refused(scope, label, expected, $"play.abandon: the catalog has no vehicle crew of {id}'s nationality (D5.1)");
            }
        }

        var crewId = inside?.Id ?? created!.Instance.Id;
        var events = new List<(string Type, EventPayload Payload)>();
        if (created is not null)
        {
            events.Add(("instance-created", created));
        }

        var vehicleChanges = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
        if (destroy == ScenarioA1CrewActsRules.Malfunction)
        {
            foreach (var (name, value) in ConditionChanges(ScenarioA1CrewActsRules.SelfDestructionConditions(definition?.MaType is not null || definition?.MainArmament is not null,
                definition?.BowMg is not null, definition?.CoaxialMg is not null)))
            {
                vehicleChanges[name] = value;
            }
        }

        // D6.631: the MG Removed becomes the crew's LMG, malfunctioned if its mount was, and its mount is Disabled.
        string? removed = null;
        string? mount = null;
        if (arguments.TryGetProperty("removeMg", out var removeMg) && removeMg.ValueKind == JsonValueKind.True)
        {
            var crewNationality = inside?.Definition is { } own ? FireReference.Value.Definitions.GetValueOrDefault(own.Definition)?.Nationality : definition?.Nationality;
            var removal = ScenarioA1CrewActsRules.MgRemoval(id, definition?.VehicleClass, definition?.Unarmored == true, definition?.Nationality, crewNationality,
                inside is not null && vocabulary.IsA(inside.Kind, "asl:smc"), false, definition?.MainArmament, definition?.AntiAircraftMg, definition?.BowMg,
                Is(vehicle, Conditions.Disabled), Is(vehicle, Conditions.BmgDisabled));
            if (removal.Bar is { } removalBar)
            {
                return Refused(scope, label, expected, removalBar);
            }

            if (ScenarioA1CrewActsRules.RemovedMgDefinition(FireReference.Value.Definitions.Values, crewNationality, removal.Fp) is not { } lmg)
            {
                return Refused(scope, label, expected, $"play.abandon: the catalog has no light MG of {crewNationality} for {id}'s MG to become (D6.631)");
            }

            (removed, mount) = ($"{attemptId}-{id}-mg", removal.Mount);
            vehicleChanges[ConditionName(ScenarioA1CrewActsRules.RemovedCondition(mount!))] = ConditionState.True;
            var swConditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal)
            {
                [Conditions.Malfunctioned] = Is(vehicle, mount == ScenarioA1CrewActsRules.BowMg ? Conditions.BmgMalfunctioned : Conditions.Malfunctioned)
                    && destroy != ScenarioA1CrewActsRules.Malfunction ? ConditionState.True : ConditionState.False,
            };
            events.Add(("instance-created", new InstanceCreated(new NewInstance(removed, "asl:mg", lmg, vehicle.Side, null, new Holding(crewId, HoldingRole.Possessed), swConditions))));
        }

        if (vehicleChanges.Count > 0)
        {
            events.Insert(created is null ? 0 : 1, ("conditions-changed", new ConditionsChanged(id, vehicleChanges)));
        }

        var counter = ScenarioA1CrewActsRules.CrewFireCounter(fired, moved, Is(vehicle, Conditions.Motion), true);
        events.Add(("vehicle-abandoned", new VehicleAbandoned(id, at!, crewId)
        {
            Destroyed = destroy,
            Removed = removed,
            Mount = mount,
            FireCounter = ScenarioA1CrewActsRules.FireCounterName(counter),
        }));
        if (destroy == ScenarioA1CrewActsRules.Wreck)
        {
            events.Add(("vehicle-wrecked", new VehicleWrecked(id, false)));
        }

        var text = $"play.abandon: {crewId} Abandons {id} in {at}, spending all its MF to be placed beneath it; {id} is Abandoned and may not move or fire (D5.41)"
            + (inside is not null ? $"; {crewId} leaves as itself, as it entered (D5.42)" : string.Empty)
            + (vehicle.ArmorLeader is not null ? "; its Armor Leader goes with the crew, to return if it re-crews (D3.43)" : string.Empty)
            + (destroy == ScenarioA1CrewActsRules.Wreck ? $"; the crew leaves {id} a non-burning wreck, with no Crew Survival roll (D5.411)"
                : destroy == ScenarioA1CrewActsRules.Malfunction ? $"; the crew malfunctions {id}'s MA and every MG as it leaves (D5.411)" : string.Empty)
            + (removed is not null ? $"; the crew Removes its MG as its LMG {removed}, and the mount is Disabled until a crew returns it (D6.631)" : string.Empty)
            + (counter is not null ? $"; the crew is marked {(counter == UnitCondition.PrepFire ? "Prep" : "Final")} Fire, since {id} was a moving target (D5.8)" : string.Empty)
            + "; the DEFENDER may fire at the crew with FFNAM (D5.43)";
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [.. events.Select((item, number) => Event(scope, attemptId, number + 1, expected, item.Type, item.Payload, null, null))], [text]);
    }

    /// <summary>
    /// A re-crewing (D5.42, D5.43, D5.341, A21.22, D3.43, D6.631; ruling R65.20): an Infantry unit enters its side's Abandoned vehicle from its Location to
    /// become its crew, keeping its id; one other than a vehicle or infantry crew is a Temporary crew. It brings back the Armor Leader its crew took, and
    /// the MG Removed from the vehicle when it holds that SW. One event, <c>vehicle-recrewed</c>.
    /// </summary>
    private GamePlan PlanRecrew(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "vehicleId", out var vehicleId) || state.Unit(unitId) is not { Status: InstanceStatus.Active } unit
            || state.Unit(vehicleId) is not { Status: InstanceStatus.Active } vehicle || !LiveFire.IsVehicle(vehicle))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a re-crewing names a unit in play and a vehicle in play");
        }

        if (state.Movement is not null)
        {
            return Refused(scope, label, expected, $"play.recrew: a unit is moving; {unitId} enters {vehicleId} once that move ends (A4.2)");
        }

        var index = MemoOf(state).Objects(state);
        var at = vehicle.Position is MapPosition ? index.Location(vehicleId)?.Location : null;

        // D5.42 (the boundary review): Rules knows the vehicle's own crew by the id it gave the counter, and when its crew left it by the state's list.
        var ownCrew = ScenarioA1CrewActsRules.IsOwnCrew(unitId, vehicleId);
        if (ScenarioA1CrewActsRules.RecrewBar(new RecrewFacts(unitId, vehicleId, state.Phase == "mph" && unit.Side == state.PhasingSide,
            at is not null && Is(vehicle, Conditions.Abandoned), unit.Side == vehicle.Side,
            vocabulary.IsA(unit.Kind, "asl:personnel") && !LiveFire.IsVehicle(unit) && unit.Position is MapPosition,
            at is not null && index.Location(unitId)?.Location == at, unit.MfSpent > 0 || unit.HalfMfSpent || unit.MovementEnded,
            ScenarioA1CrewActsRules.RecrewReady(Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned), Is(unit, Conditions.Berserk), Is(unit, Conditions.Melee),
                Is(unit, Conditions.Captured), Is(unit, "asl:ti")),
            state.AbandonedThisPlayerTurn.Contains(vehicleId, StringComparer.Ordinal), ownCrew, Is(vehicle, Conditions.Recalled))) is { } bar)
        {
            return Refused(scope, label, expected, bar);
        }

        // D6.631: the MG Removed from this vehicle goes back with a crew that holds it; the last Removal is read walking back from the end (the boundary
        // review), and only when a mount is Disabled.
        VehicleAbandoned? removal = null;
        var disabled = Is(vehicle, Conditions.Disabled) || Is(vehicle, Conditions.BmgDisabled);
        for (var back = existing.Count - 1; disabled && back >= 0 && removal is null; back--)
        {
            removal = existing[back].Payload is VehicleAbandoned { Removed: not null } left && left.Vehicle == vehicleId ? left : null;
        }

        var returned = removal is not null && state.Find(removal.Removed!) is EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } holding }
            && holding.Holder == unitId && Is(vehicle, ConditionName(ScenarioA1CrewActsRules.RemovedCondition(removal.Mount ?? ScenarioA1CrewActsRules.MainArmament)))
            ? removal : null;
        // A21.22 (pass 65, ruling R65.22): Rules reads a captured vehicle from its capture's record (the side it was taken from, the referee's review), and
        // gives how the unit mans it.
        var captured = ScenarioA1CapturedVehicleRules.Captured(vehicle.CapturedFrom, vehicle.Side);
        var manning = ScenarioA1CapturedVehicleRules.Manning(unit.Kind, vocabulary.IsA(unit.Kind, "asl:smc"), ownCrew, captured);
        if (ScenarioA1CapturedVehicleRules.SquadBar(unitId, vehicleId, unit.Kind, manning) is { } squadBar)
        {
            return Refused(scope, label, expected, squadBar);
        }

        var temporary = manning is not null;
        var text = $"play.recrew: {unitId} enters {vehicleId} in {at}, spending all its MF, and crews it"
            + (manning is not null ? " as " + ScenarioA1CapturedVehicleRules.ManningText(manning, captured) : string.Empty)
            + $"; {vehicleId} is no longer Abandoned and may move and fire from its next phase (D5.42)"
            + (unit.ArmorLeader is not null ? $"; the Armor Leader {unitId} took with it is back in {vehicleId} (D3.43)" : string.Empty)
            + (Is(unit, Conditions.StunRecovery) ? $"; {vehicleId} keeps its crew's Stun +1 (D5.34)" : string.Empty)
            + (returned is not null ? $"; {returned.Removed} goes back into {vehicleId}, and its mount is no longer Disabled (D6.631)" : string.Empty);
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "vehicle-recrewed", new VehicleRecrewed(vehicleId, at!, unitId)
            {
                Temporary = temporary,
                Returned = returned?.Removed,
                Mount = returned?.Mount,
                Manning = manning,
                Captured = manning is not null && captured,
            }, null, null)], [text]);
    }
}
