using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Units.Documents;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// <c>vehicle-wrecked</c>: a vehicle destroyed by an attack becomes a wreck in its Location (D10.1; ruling R6.5), burning when the attack
/// burned it (B25.14); the Blaze of a burning wreck is its own entity.
/// </summary>
public sealed record VehicleWrecked(string Id, bool Burning) : EventPayload;

/// <summary>
/// <c>rally-attempted</c>: a Rally attempt resolved by the Rally package (unit step 19). It records the unit, the rallying
/// leader or none for Self-Rally, the rolls by purpose, the declared facts, and the package's resolution; the effects
/// follow as ordinary condition, elimination, and lineage events. Replay hands it to an <see cref="IRallyRecordVerifier"/>.
/// </summary>
public sealed record RallyAttempted(
    string Unit,
    string? Leader,
    IReadOnlyDictionary<string, string> Rolls,
    JsonElement Facts,
    JsonElement Resolution) : EventPayload;

/// <summary>Recomputes a Rally record from its facts and rolls; supplied to replay by the project that owns the package.</summary>
public interface IRallyRecordVerifier
{
    /// <summary>Null when the record reproduces and its facts agree with the state it is made in; otherwise why not.</summary>
    public string? Verify(GameState state, RallyAttempted rally, IReadOnlyDictionary<string, DiceRolled> rolls);
}

/// <summary>
/// <c>residual-fp-placed</c>: the Residual FP a Defensive First Fire, Subsequent First Fire, or FPF record leaves in its
/// target Location (A8.2, p. 60). It names the record, whose resolution must give the same value; a larger counter
/// replaces a smaller one (A8.21), and all are removed at the end of the MPh.
/// </summary>
public sealed record ResidualFirePlaced(string Fire, BoardLocation Location, int Fp) : EventPayload;

/// <summary>Residual FP in a Location this MPh, and the fire record that left it.</summary>
public sealed record ResidualFire(BoardLocation Location, int Fp, string Fire);

/// <summary>
/// <c>repair-attempted</c>: a Repair attempt in the RPh (A9.72, p. 65): the unit, the malfunctioned SW it possesses, the
/// recorded dr, the SW's Repair Number, and the result. Replay recomputes it: a dr at most the Repair Number repairs, a 6
/// eliminates, and anything else changes nothing.
/// </summary>
public sealed record RepairAttempted(string Unit, string Equipment, string Roll, int RepairNumber, string Result) : EventPayload
{
    /// <summary>Current D3.7 weapon and participant validation. Null preserves the recorded legacy repair reading.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? VehicleRepairVersion
    {
        get; init;
    }

    /// <summary>The actual Hero Rider taking the AAMG repair; null selects the inherent crew.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Hero
    {
        get; init;
    }

    public const string Repaired = "repaired";
    public const string NoChange = "no-change";
    public const string Eliminated = "eliminated";

    /// <summary>
    /// Which weapon of a vehicle the attempt is for (D3.7, p. 201; pass 50): <c>ma</c> for a Gun MA, <c>bmg</c>, or <c>cmg</c>, with the vehicle as
    /// unit and equipment. Null for a SW and for a vehicle's MA AAMG, which is every record made before pass 50.
    /// </summary>
    public string? Weapon
    {
        get; init;
    }
}

/// <summary>
/// <c>movement-step</c>: a moving stack spends MF to enter an adjacent Location in its MPh (A4.1, A4.13; unit step 22).
/// <paramref name="HalfMf"/> is the cost in half MF, so grain's 1½ (B15.4) is exact. The step opens the DEFENDER's window
/// to fire at the stack (A8.1); the ATTACKER may not spend more MF until it closes.
/// </summary>
public sealed record MovementStepped(IReadOnlyList<string> Movers, BoardLocation To, int HalfMf, bool Assault, int Step) : EventPayload
{
    /// <summary>
    /// For a berserk stack's charge (A15.43, A15.431): the Location of the Known enemy unit it charges, which it keeps charging
    /// when that unit leaves its LOS; null for other moves.
    /// </summary>
    public BoardLocation? Charge
    {
        get; init;
    }

    /// <summary>Whether the movers Double Time with this step (A4.5, ruling R5.1): each becomes CX.</summary>
    public bool DoubleTime
    {
        get; init;
    }

    /// <summary>
    /// The map edge the movers leave by (A2.6; pass 21, ruling R21.5), from their Location, as if entering the mirror-image hex beyond it; null for a
    /// step on the map.
    /// </summary>
    public string? Exit
    {
        get; init;
    }

    /// <summary>The Gun its crew pushes along with this step (C10.3; ruling R8.6); null when none. Any other Gun a mover mans is abandoned.</summary>
    public string? PushedGun
    {
        get; init;
    }

    /// <summary>
    /// A SMOKE grenade placement attempt made with this step's MF in the movers' Location (A24.1; ruling R9.5): the squad, the Location named, the
    /// dr's roll and value, and the squad's Smoke Placement Exponent; null for a move.
    /// </summary>
    public SmokeAttempt? Smoke
    {
        get; init;
    }

    /// <summary>
    /// A DC Placed with this step's MF in an ADJACENT Location (A23.3; backlog pass 15, ruling R15.2): the unit, the DC, the Location, whether the unit
    /// was CX, and whether every unit there was concealed; null for a move.
    /// </summary>
    public DcPlacement? DcPlacement
    {
        get; init;
    }

    /// <summary>
    /// Whether the step crossed a road hexside at the road rate into a hex with no SMOKE, burning wreck, or rubble (B3.4, A4.132; ruling R10.8): a
    /// unit whose every step this MPh was one gets one more MF.
    /// </summary>
    public bool Road
    {
        get; init;
    }

    /// <summary>
    /// Whether the movers entered <see cref="To"/> across a road hexside paying the road's cost, into a hex that has other terrain (A4.132; pass 40):
    /// they are on the road, not in that terrain, while the DEFENDER First Fires at them there.
    /// </summary>
    public bool RoadRate
    {
        get; init;
    }

    /// <summary>
    /// The Location a Dash declared with this step leads to (A4.63; pass 40): the step is into the road Location, and the stack's only further step
    /// is into that Location, where its MPh ends. Null for other steps.
    /// </summary>
    public BoardLocation? Dash
    {
        get; init;
    }

    /// <summary>The laden MMC the stack's leader lends his IPC to for this move, when the mover named it (A4.42; pass 40, ruling R40.11); null otherwise.</summary>
    public string? IpcTo
    {
        get; init;
    }

    /// <summary>
    /// Whether the step enters a Location of enemy units in the MPh as an Infantry OVR onto a lone Known SMC, or onto Unarmed units to recapture
    /// them (A4.15, A20.54; pass 40): its CC follows at once when the DEFENDER's window closes.
    /// </summary>
    public bool InfantryOverrun
    {
        get; init;
    }

    /// <summary>
    /// Whether the step is a Minimum Move (A4.134; ruling R10.9): the movers' only hex this MPh; when the DEFENDER's window closes their move ends and
    /// the unbroken ones become pinned and CX.
    /// </summary>
    public bool MinimumMove
    {
        get; init;
    }

    /// <summary>
    /// The Location the stack attempted when a concealed enemy unit there was revealed and forced it back (A12.15; ruling R10.11): the stack stays in
    /// <see cref="To"/>, its Location, with the MF spent there, and its move ends when the DEFENDER's window closes. Null for other steps.
    /// </summary>
    public BoardLocation? Attempted
    {
        get; init;
    }

    /// <summary>
    /// The hexsides of the entered woods or building hex the stack moves along in Bypass (A4.3; ruling R10.7), in order; null when it does not
    /// Bypass. The stack must then leave from the last one's far vertex or occupy the obstacle.
    /// </summary>
    public IReadOnlyList<HexsideDirection>? Bypass
    {
        get; init;
    }

    /// <summary>
    /// The wounded men carried with this step, each by a MMC of the stack at five PP (A17.2; pass 71, ruling R71.16): a man named spends no MF with the step.
    /// Null when none is, as in every step recorded before the pass.
    /// </summary>
    public IReadOnlyList<CarriedMan>? Carried
    {
        get; init;
    }
}

/// <summary>A wounded man carried by a MMC of his moving stack (A17.2; pass 71, ruling R71.16).</summary>
public sealed record CarriedMan(string Man, string Carrier);

/// <summary>
/// A SMOKE grenade placement attempt (A24.1; ruling R9.5): a dr at most the exponent places a SMOKE counter in the Location named; a dr of 6 ends
/// the squad's MPh.
/// </summary>
/// <summary>A DC Placement (A23.3; ruling R15.2).</summary>
public sealed record DcPlacement(string Unit, string Charge, BoardLocation Target, bool Cx, bool TargetsConcealed)
{
    /// <summary>Owner at placement, persisted by B5. Null in legacy records; never inferred from a later custodian.</summary>
    public string? OwnerSide
    {
        get; init;
    }

    /// <summary>
    /// The Target Facings of each enemy AFV in the target Location through the hexside the DC was Placed across (A23.5; pass 60, the second referee):
    /// they stand "even if the AFV changes its CA" before the detonation. Null in a record made before the pass, whose detonation reads them then.
    /// </summary>
    public IReadOnlyList<DcTargetFacing>? Facings
    {
        get; init;
    }
}

/// <summary>An AFV's hull and turret Target Facings toward a Placed DC (A23.5; pass 60); the turret's null for a vehicle with none.</summary>
public sealed record DcTargetFacing(string Vehicle, string Hull, string? Turret);

public sealed record SmokeAttempt(string Unit, BoardLocation Target, string Roll, int Dr, int Exponent)
{
    /// <summary>Whether the squad is CX, which adds one to the dr (A4.51; table player, pass 9).</summary>
    public bool Cx
    {
        get; init;
    }

    public bool Placed => Rules.ScenarioA1ResultTables.SmokePlaced(Dr, Cx, Exponent);
}

/// <summary>The leader directing a SW's To Hit DR this phase (A7.53).</summary>
public sealed record SupportWeaponDirector(string Gun, string Leader);

/// <summary>A light mortar's Spotter (C9.3; ruling R9.4).</summary>
public sealed record MortarSpotter(string Gun, string Spotter);

/// <summary>Observed C9.3 assignment loss. A later rally cannot erase it or establish a replacement designation.</summary>
public sealed record MortarSpotterLoss(string Gun, string Spotter, string EventId, long Revision,
    Rules.SpotterChange Change, Rules.TurnPhaseOccurrence? Occurrence)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public int PlayerTurnOrdinal
    {
        get; init;
    }
}

/// <summary>Replay's contiguous command batch and the calculator's steps for the current phase.</summary>
public sealed record MortarSpotterOpening(string? Command, int Sequence, bool PhaseChangeBatch,
    IReadOnlyList<string> EventTypes, IReadOnlyList<Rules.MovementOpeningStep> Steps);

/// <summary>State facts shared by the command and projection; all decisions remain in Rules.</summary>
public static class MortarSpotterFacts
{
    /// <summary>Reads assignment history once for a candidate-list refresh. The caller retains it only for this exact immutable state.</summary>
    public static MortarSpotterAssignmentRead ReadAssignments(GameState state)
    {
        var origins = new Dictionary<string, MortarSpotterDesignation>(StringComparer.Ordinal);
        foreach (var origin in state.MortarSpotterDesignations)
        {
            origins[origin.Gun] = origin;
        }
        var losses = new Dictionary<(string Gun, string Spotter), MortarSpotterLoss>();
        foreach (var loss in state.MortarSpotterLosses)
        {
            if (loss.Revision > (origins.GetValueOrDefault(loss.Gun)?.Revision ?? 0))
            {
                losses[(loss.Gun, loss.Spotter)] = loss;
            }
        }
        var assignments = new Dictionary<string, MortarSpotter>(StringComparer.Ordinal);
        foreach (var assignment in state.MortarSpotters)
        {
            assignments[assignment.Gun] = assignment;
        }
        var locations = assignments.Values.Where(item => !losses.ContainsKey((item.Gun, item.Spotter)))
            .GroupBy(item => item.Spotter).ToDictionary(group => group.Key,
                group => (IReadOnlyList<(string Gun, string? Location)>)group.Select(item => (item.Gun, state.Location(item.Gun)?.Location.ToString())).ToArray(),
                StringComparer.Ordinal);
        return new(state, origins, losses, assignments, locations);
    }

    public static Rules.SpotterDesignationFacts? Designation(GameState state, string gunId, string spotterId,
        Vocabulary.UnitVocabulary vocabulary, bool mortar, int? range, MortarSpotterAssignmentRead? assignments = null)
    {
        if (state.Equipment.FirstOrDefault(item => item.Id == gunId) is not { Holding: { } holding } gun
            || state.Unit(holding.Holder) is not { Side: { } owner } holder || state.Unit(spotterId) is not { } unit)
        {
            return null;
        }
        bool Is(string condition) => GameState.Condition(unit, condition) == ConditionState.True;
        var phaseFirer = state.PhaseFirers.FirstOrDefault(item => item.Unit == unit.Id);
        var fired = Is(Conditions.PrepFire) || Is(Conditions.FinalFire) || Is(Conditions.FirstFire);
        var read = assignments is not null && ReferenceEquals(assignments.State, state) ? assignments : ReadAssignments(state);
        var assignment = read.Assignments.GetValueOrDefault(gunId);
        var origin = read.Origins.GetValueOrDefault(gunId);
        var loss = assignment is null ? null : read.Losses.GetValueOrDefault((gunId, assignment.Spotter));
        var lossAt = loss is { Occurrence: { } at, PlayerTurnOrdinal: > 0 }
            ? new Rules.TurnRulePosition(at.GameTurn, loss.PlayerTurnOrdinal, at.Owner, at.Phase, Rules.TurnCheckpoint.DuringPhase) : null;
        var timing = new Rules.SpotterTimingFacts(owner,
            new(state.Turn, state.PlayerTurnOrdinal, state.PhasingSide, state.Phase,
                Rules.ScenarioA1SpotterCalculator.OpeningCheckpoint(state.SpotterOpening.Steps)),
            origin is not null || assignment is not null, loss?.Change ?? Rules.SpotterChange.None, lossAt);
        var candidate = new Rules.SpotterCandidateFacts(unit.Status == InstanceStatus.Active && unit.Position is MapPosition,
            unit.Side == owner, Rules.ScenarioA1SpotterCalculator.Personnel(unit.Kind),
            GameState.GoodOrder(unit, vocabulary) == ConditionState.True, Is(Conditions.Captured), Is(Conditions.Melee), unit.Id == holder.Id);
        var gunAt = state.Location(gun.Id)?.Location;
        var otherLocations = read.Locations.GetValueOrDefault(unit.Id)?.Where(item => item.Gun != gun.Id).Select(item => item.Location).ToArray() ?? [];
        return new(timing, mortar && gun.Status == InstanceStatus.Active && holder.Status == InstanceStatus.Active && holder.Position is MapPosition,
            candidate, range, Rules.ScenarioA1SpotterCalculator.SharesLocation(gunAt?.ToString(), otherLocations), state.Night,
            GameState.Condition(gun, "asl:ti") == ConditionState.True, unit.Kind == "asl:squad", fired,
            state.SpottedThisPhase.Any(item => item.Spotter == unit.Id),
            new(unit.Id, unit.Kind == "asl:squad", Is(Conditions.FirstFire), Is(Conditions.PrepFire) || Is(Conditions.FinalFire),
                phaseFirer is not null, phaseFirer?.Weapon, state.SupportWeaponUses.Any(item => item.Unit == unit.Id)),
            state.Choice is not null || state.OpenAttempts.Count > 0 || state.PendingSurrenders.Count > 0 || state.Movement is not null,
            Is("asl:ti"));
    }

    public static Rules.SpotterReplacementFacts Replacement(GameState state, Rules.SpotterDesignationFacts facts) =>
        new(facts.Timing, state.SpotterOpening.Steps, facts.Mortar, facts.Candidate, facts.HexRange, facts.SharesLocation, facts.Night, facts.PendingDecision);
}

/// <summary>Immutable-state assignment index shared by all candidates of one read. No viewer-specific verdicts are cached.</summary>
public sealed record MortarSpotterAssignmentRead(GameState State,
    IReadOnlyDictionary<string, MortarSpotterDesignation> Origins,
    IReadOnlyDictionary<(string Gun, string Spotter), MortarSpotterLoss> Losses,
    IReadOnlyDictionary<string, MortarSpotter> Assignments,
    IReadOnlyDictionary<string, IReadOnlyList<(string Gun, string? Location)>> Locations);

/// <summary>The C9.3 Good Order half-squad that inherits an observed assignment, with journal provenance.</summary>
public sealed record MortarSpotterSuccessor(string Gun, string Predecessor, string Successor, string EventId, long Revision);

/// <summary>Explicit original designation with authoritative journal provenance, distinct from observing a shot.</summary>
public sealed record MortarSpotterDesignation(string Gun, string Spotter, string Owner, string EventId, long Revision,
    Rules.TurnPhaseOccurrence Occurrence);

/// <summary>Observed C9.31 firing locations and targets, retained for the entire Player Turn.</summary>
public sealed record MortarSpotterUse(string Gun, string Spotter, BoardLocation? Location, BoardLocation Target);

/// <summary>A squad's one SW use this phase (A7.351): the SW, a Spotter's mortar, or <c>panzerfaust</c>.</summary>
public sealed record SupportWeaponUse(string Unit, string Weapon);

/// <summary>
/// <c>vehicle-step</c>: one MP expenditure of a moving vehicle in its MPh (D2.1, unit step 25): <c>start</c> (D2.12), <c>turn</c> one
/// hexspine to <paramref name="Facing"/> (D2.11), <c>enter</c> the ADJACENT Location <paramref name="At"/> in its VCA (D2.11),
/// <c>stop</c> (D2.13), <c>remain</c> to spend the MP left at the end of its move in its hex (D2.1, ruling R5.15), or <c>exit</c> the
/// playing area from its edge hex <paramref name="At"/> (A2.6, D5.341); <paramref name="HalfMp"/> is its cost in half MP, so the ½ MP road
/// rate is exact (D2.16). Like a movement step it opens the DEFENDER's window (A8.1), except an exit, after which the vehicle is gone.
/// </summary>
public sealed record VehicleStepped(string Vehicle, string Kind, BoardLocation At, UnitFacing? Facing, int HalfMp, int Step) : EventPayload
{
    public const string Start = "start";
    public const string Turn = "turn";
    public const string Enter = "enter";
    public const string Stop = "stop";
    public const string Remain = "remain";
    public const string Exit = "exit";

    /// <summary>An OVR declared in the vehicle's own Location after the A12.41 choice there (D7.1; ruling R11.12): a quarter of its allotment.</summary>
    public const string Overrun = "overrun";

    /// <summary>Infantry in the vehicle's Location board it as Passengers (D6.4; ruling R26.2): <see cref="Units"/> name them.</summary>
    public const string Load = "load";

    /// <summary>Passengers disembark beneath the Stopped vehicle (D6.5; ruling R26.2): <see cref="Units"/> name them.</summary>
    public const string Unload = "unload";

    /// <summary>A Delay MP (D2.17, p. 196; pass 50): one MP spent without moving, by a vehicle that is stopped or has not yet started.</summary>
    public const string Delay = "delay";

    /// <summary>
    /// The TCA the turret is turned to with this MP expenditure (D3.12, p. 199; pass 50): "the TCA may also change freely with each MP expended
    /// during the MPh"; null when it does not change.
    /// </summary>
    public UnitFacing? Turret
    {
        get; init;
    }

    /// <summary>For an entry from off board (A2.52, D2.4; ruling R26.1): the map edge the vehicle crosses into <see cref="At"/>, its VCA in <see cref="Facing"/>.</summary>
    public string? Entry
    {
        get; init;
    }

    /// <summary>For an exit (A2.6; ruling R26.4): the map edge the vehicle leaves by.</summary>
    public string? Edge
    {
        get; init;
    }

    /// <summary>
    /// For a load or an unload (D6.4, D6.5; ruling R26.2): the Infantry boarding or the Passengers disembarking. For an OVR's declaration (D7.11; pass 65,
    /// ruling R65.13): the Passengers adding their FP to it; null on every OVR declared before the pass and by a vehicle carrying none.
    /// </summary>
    public IReadOnlyList<string>? Units
    {
        get; init;
    }

    /// <summary>For an unload (D6.5; ruling R26.2): the MF each disembarking unit has spent once beneath the vehicle.</summary>
    public int UnitMf
    {
        get; init;
    }

    /// <summary>
    /// For an unload (D6.5; pass 65, ruling R65.15): the disembarking units move on before any other unit once their vehicle's move ends, since "no
    /// unit other than the conveying vehicle and any other disembarking PRC of that vehicle may move in the interim". False on every unload recorded
    /// before the pass, after which any unit moved next.
    /// </summary>
    public bool Interim
    {
        get; init;
    }

    /// <summary>
    /// For a load (D6.2; pass 65, ruling R65.16): the units board as Riders, on the outside of the AFV. False on every load recorded before the pass,
    /// which boarded Passengers.
    /// </summary>
    public bool Riders
    {
        get; init;
    }

    /// <summary>A Start declaring Reverse movement, and an entry made in Reverse (D2.2, D2.23; ruling R11.1).</summary>
    public bool Reverse
    {
        get; init;
    }

    /// <summary>
    /// For an entry or a VCA change in Bypass (D2.3; ruling R11.2): the ground-level Location across the hexside the vehicle straddles afterwards;
    /// null for an entry to the hex center.
    /// </summary>
    public BoardLocation? Straddling
    {
        get; init;
    }

    /// <summary>Whether the entry declares an OVR of the Location entered, its cost included (D7.1; ruling R11.11).</summary>
    public bool Overrunning
    {
        get; init;
    }

    /// <summary>Whether the entry is a Minimum Move, after which the vehicle ends its MPh in Motion (D2.15; ruling R11.5).</summary>
    public bool MinimumMove
    {
        get; init;
    }

    /// <summary>Whether the Start MP is a Bog Removal attempt, whose cost is its dr product (D8.3; ruling R11.10).</summary>
    public bool BogRemoval
    {
        get; init;
    }

    /// <summary>Whether the entry took the vehicle's whole printed allotment, after which it only Stops or ends in Motion (D2.7, B13.41; ruling R11.5).</summary>
    public bool All
    {
        get; init;
    }
}

/// <summary>
/// <c>rider-bailed-out</c>: a Rider Bails Out of its AFV (D6.21, D6.23, D6.24; pass 65, ruling R65.17): <paramref name="Unit"/> leaves
/// <paramref name="Vehicle"/> for <paramref name="At"/> (the last Location before terrain Riders may not ride through, or the AFV's own) for its
/// <paramref name="Cause"/> (<c>terrain</c>, <c>turret</c>, <c>pin</c>, or <c>break</c>), and takes a NMC: its roll, the Morale Level it is taken
/// against, and its <paramref name="Result"/> (<c>passed</c>, <c>pinned</c>, <c>broken</c>, <c>reduced</c>, or <c>eliminated</c>), whose effects the
/// records after it write. It is beneath the AFV on foot, its move ended, with a Prep or Final Fire counter if unbroken; each SW it carries is kept,
/// malfunctioned, or eliminated as <see cref="Weapons"/> say.
/// </summary>
public sealed record RiderBailedOut(string Unit, string Vehicle, BoardLocation At, string Cause, string Roll, int Morale, string Result) : EventPayload
{
    /// <summary>Each SW the unit carries, with its dr's roll (null when the unit breaks or is eliminated, which eliminates it) and its fate (D6.24).</summary>
    public IReadOnlyList<RiderWeapon>? Weapons
    {
        get; init;
    }

    /// <summary>
    /// The NMC's DRM: a Rider leader's modifier (A10.7 EXC; pass 70, ruling R70.9), 0 with none. Written by every Bail Out planned since the pass, whose NMC the
    /// Morale Check member resolves; null on every older record, checked as before.
    /// </summary>
    public int? Drm
    {
        get; init;
    }

    /// <summary>The ELR the NMC was taken against (A19.13; pass 70, ruling R70.9), written with <see cref="Drm"/>; null with it, or for a unit with no ELR.</summary>
    public int? Elr
    {
        get; init;
    }
}

/// <summary>A SW a Bailing Out Rider carries (D6.24): its dr's roll, null when the carrier breaks, and its fate: <c>kept</c>, <c>malfunctioned</c>, or <c>eliminated</c>.</summary>
public sealed record RiderWeapon(string Weapon, string? Roll, string Fate);

/// <summary>
/// <c>vehicle-abandoned</c>: a crew Abandons its vehicle voluntarily in its own MPh (D5.41, D5.43; pass 65, ruling R65.20): <paramref name="Crew"/> is
/// beneath <paramref name="Vehicle"/> in <paramref name="At"/>, its MF spent, and the vehicle is marked Abandoned and stopped. A crew counter placed by the
/// <c>instance-created</c> before this record is already there; a unit that crewed the vehicle from inside leaves it as itself, taking the vehicle's Armor
/// Leader (D3.43) and Stun +1 (D5.34). The DEFENDER may fire at the crew with FFNAM, as at an unloader (D5.43). The records before this one write what the
/// choices did to the vehicle (malfunctioned or Disabled weapons, the Removed MG's counter) and the one after it a wreck.
/// </summary>
public sealed record VehicleAbandoned(string Vehicle, BoardLocation At, string Crew) : EventPayload
{
    /// <summary>D5.411: the self-destruction the crew chose, <c>wreck</c> or <c>malfunction</c>; null for none.</summary>
    public string? Destroyed
    {
        get; init;
    }

    /// <summary>D6.631: the SW counter the crew took a Removed MG away as; null when it Removed none.</summary>
    public string? Removed
    {
        get; init;
    }

    /// <summary>D6.631: the mount the MG was Removed from, <c>ma</c> or <c>bmg</c>; null when none was.</summary>
    public string? Mount
    {
        get; init;
    }

    /// <summary>D5.8: the fire counter the crew takes, <c>prep</c> or <c>final</c>, when the vehicle fired or was a moving target; null for none.</summary>
    public string? FireCounter
    {
        get; init;
    }
}

/// <summary>
/// <c>vehicle-recrewed</c>: an Infantry unit of the vehicle's side enters an Abandoned vehicle in its Location to become its crew, spending all its MF (D5.42;
/// pass 65, ruling R65.20). It is inside the vehicle in the crew's role, keeping its id; the vehicle is no longer Abandoned, and takes back the Armor Leader
/// (D3.43) and the Stun +1 (D5.34) the unit carries. <see cref="Temporary"/> marks a Temporary crew (A21.22), whose penalties are increment 2i's; a SW
/// Removed from the vehicle (D6.631) that the unit brings back is returned, and the mount's Disabled counter removed.
/// </summary>
public sealed record VehicleRecrewed(string Vehicle, BoardLocation At, string Unit) : EventPayload
{
    /// <summary>A21.22: the unit is a friendly MMC other than a vehicle or infantry crew, crewing as a Temporary crew.</summary>
    public bool Temporary
    {
        get; init;
    }

    /// <summary>
    /// A21.22 (pass 65, ruling R65.22): how a Temporary crew mans the vehicle, Rules' name (<c>vehicle-crew</c>, <c>infantry-crew</c>, <c>half-squad</c>,
    /// <c>smc</c>), which gives its penalties; null for a qualified crew and for every record made before increment 2i, which has none.
    /// </summary>
    public string? Manning
    {
        get; init;
    }

    /// <summary>A21.22: the vehicle is captured, of an enemy nationality to the crew, so its weapons are captured weapons (A21.11, A21.12).</summary>
    public bool Captured
    {
        get; init;
    }

    /// <summary>D6.631: the SW the unit returns to the vehicle; null when it returns none.</summary>
    public string? Returned
    {
        get; init;
    }

    /// <summary>D6.631: the mount the returned MG goes back to, <c>ma</c> or <c>bmg</c>.</summary>
    public string? Mount
    {
        get; init;
    }
}

/// <summary>
/// <c>vehicle-captured</c>: a vehicle in the CCPh passes to <paramref name="Side"/>, the captor (A11.52, A21.2, A21.21; pass 65, ruling R65.22): an Abandoned
/// AFV at the end of the CCPh with no Personnel of its side there (<c>automatic</c>), or by a capture attempt in CC (<c>cc</c>), stays Abandoned until a crew
/// enters it; an unarmed vehicle alone with the captor's Infantry (<c>unarmed</c>) runs at once with a Temporary Driver, a SMC captor alone being removed
/// into it as its <see cref="Driver"/>. Captures recorded before the pass are <c>conditions-changed</c> records (ruling R11.16), which stay as they were.
/// </summary>
public sealed record VehicleCaptured(string Vehicle, BoardLocation At, string Side, string Cause) : EventPayload
{
    public const string Automatic = Rules.ScenarioA1CapturedVehicleRules.Automatic;
    public const string CloseCombat = Rules.ScenarioA1CapturedVehicleRules.CloseCombat;
    public const string Unarmed = Rules.ScenarioA1CapturedVehicleRules.Unarmed;

    /// <summary>A11.52 (pass 70, ruling R70.34): an armed unarmored vehicle captured with all its Passengers.</summary>
    public const string Passengers = Rules.ScenarioA1CapturedVehicleRules.Passengers;

    /// <summary>A21.21: the SMC captor removed into an unarmed vehicle as its Temporary Driver; null for a MMC captor's driver, who costs nothing.</summary>
    public string? Driver
    {
        get; init;
    }
}

/// <summary>
/// <c>vehicle-scrounged</c>: an Infantry MMC scrounges a non-burning wreck or an Abandoned vehicle in its Location in its own RPh (D10.5 to D10.52; pass 65,
/// ruling R65.23). <paramref name="Roll"/> is the dr's roll and <paramref name="Dr"/> its value: 1 to 3 salvages that many SW, by the Random Selection of
/// <see cref="Selection"/> when more exist. The SW taken are created, possessed by the unit, by the <c>instance-created</c> records before this one. The vehicle
/// is marked Scrounged, and the mounts it may no longer fire Disabled, whatever the dr; the unit and its SW taken are TI for the rest of the Player Turn.
/// </summary>
public sealed record VehicleScrounged(string Vehicle, BoardLocation At, string Unit, string Roll, int Dr) : EventPayload
{
    /// <summary>A.9: the Random Selection's roll, one die a scroungeable mount, when more could be taken than the dr; null otherwise.</summary>
    public string? Selection
    {
        get; init;
    }

    /// <summary>D10.51, D10.52: each SW salvaged, by its mount (<c>bmg</c>, <c>ma</c>, or <c>aamg</c>); null when none was.</summary>
    public IReadOnlyList<ScroungedWeapon>? Taken
    {
        get; init;
    }

    /// <summary>D10.5: the scroungeable mounts, every one Disabled now; null when the vehicle had none.</summary>
    public IReadOnlyList<string>? Mounts
    {
        get; init;
    }
}

/// <summary>A SW salvaged by Scrounging (D10.52): the counter it became and the mount it came from.</summary>
public sealed record ScroungedWeapon(string Weapon, string Mount);

/// <summary>
/// <c>vehicle-check-rolled</c>: a vehicle's DR or dr outside combat (rulings R11.3, R11.4, R11.9, R11.10): a Bog Check (<c>bog</c>), a Bog Removal
/// (<c>bog-removal</c>), an ESB DR (<c>esb</c>, for <see cref="Mp"/> extra MP), or a Mechanical Reliability DR (<c>mechanical</c>), with its roll,
/// its total DRM, and its result: <c>passed</c>, <c>bogged</c>, <c>freed</c>, <c>mired</c>, or <c>immobilized</c>.
/// </summary>
public sealed record VehicleCheckRolled(string Vehicle, string Check, string Roll, int Drm, string Result) : EventPayload
{
    public const string Bog = "bog";
    public const string BogRemoval = "bog-removal";
    public const string Esb = "esb";
    public const string Mechanical = "mechanical";

    /// <summary>A21.22 (pass 65, ruling R65.22): a Temporary crew's dr before each start, the colored dr of its DR as a Bog Removal's.</summary>
    public const string TemporaryStart = Rules.ScenarioA1ResultTables.TemporaryStartCheck;

    public const string Passed = "passed";
    public const string Bogged = "bogged";
    public const string Freed = "freed";
    public const string Mired = "mired";
    public const string Immobilized = "immobilized";

    /// <summary>A21.22: a start dr of 5, no movement this phase.</summary>
    public const string NoMove = Rules.ScenarioA1ResultTables.CheckNoMove;

    /// <summary>The MP an ESB DR sought (D2.5); zero for other checks.</summary>
    public int Mp
    {
        get; init;
    }

    /// <summary>
    /// The part of <see cref="Drm"/> that is the vehicle's Armor Leader's modifier on the colored dr of a Bog Removal (D8.3, D3.44; pass 55,
    /// ruling R55.16), or an Inexperienced Crew's +1 (D3.45); null for none, which is every record made before pass 55.
    /// </summary>
    public int? ArmorLeadership
    {
        get; init;
    }

    /// <summary>The result a check's Final DR or dr gives (D2.5, D2.51, D8.21, D8.3).</summary>
    public static string For(string check, int final) => Rules.ScenarioA1ResultTables.VehicleCheck(check, final);
}

/// <summary>
/// <c>motion-attempted</c>: a DEFENDER's vehicle attempts Motion status in the enemy MPh (D2.401, p. 198; pass 55, rulings R55.11 to R55.13): its
/// dr's roll, the half MF or MP the moving unit <see cref="Mover"/> has spent in its LOS in this MPh, and the result: <c>passed</c> (it gains
/// Motion, with the VCA in <see cref="Facing"/> and the TCA in <see cref="Turret"/> when it changes them), <c>failed</c>, or, after a passed dr,
/// <c>immobilized</c> by its Mechanical Reliability DR (D2.51) or <c>bogged</c> by the Bog Check of its VCA change (D2.11, D8.2), whose
/// <c>vehicle-check-rolled</c> records come before this one. The vehicle attempts no more in this MPh.
/// </summary>
public sealed record MotionAttempted(string Vehicle, string Roll, int HalfPoints, string Result) : EventPayload
{
    public const string Passed = Rules.ScenarioA1MotionAttempts.Passed;
    public const string Failed = Rules.ScenarioA1MotionAttempts.Failed;
    public const string Immobilized = Rules.ScenarioA1MotionAttempts.Immobilized;
    public const string Bogged = Rules.ScenarioA1MotionAttempts.Bogged;

    /// <summary>The moving unit whose MF or MP in the vehicle's LOS the dr is made against.</summary>
    public string? Mover
    {
        get; init;
    }

    /// <summary>The VCA the vehicle takes with a passed attempt ("allowed to freely change its VCA/TCA"); null when it keeps its VCA.</summary>
    public UnitFacing? Facing
    {
        get; init;
    }

    /// <summary>The TCA the vehicle takes with a passed attempt; null when its turret is not turned apart from the hull.</summary>
    public UnitFacing? Turret
    {
        get; init;
    }
}

/// <summary>
/// <c>overrun-resolved</c>: a vehicle's OVR of its Location was resolved by the fire record named (D7.1; ruling R11.11); the DEFENDER's Reaction Fire
/// window follows (D7.2).
/// </summary>
public sealed record OverrunResolved(string Vehicle, BoardLocation At, string Fire) : EventPayload;

/// <summary>
/// <c>paatc-taken</c>: a PAATC (A11.6, A12.41; rulings R11.12, R11.13, R11.17): the units that took it against the vehicle, the roll, the Morale Level
/// used, the DRM, and whether it passed; a failure pins them (and reveals concealed ones), as the events after it record.
/// </summary>
public sealed record PaatcTaken(IReadOnlyList<string> Units, string Vehicle, string Roll, int Morale, int Drm, bool Passed) : EventPayload;

/// <summary>
/// <c>infantry-overrun-ntc</c>: the NTC of an Infantry OVR (A4.15, p. 49; pass 40): the stack it is taken for, the unit that takes it (the stack's
/// leader, or the one MMC), the Location the stack would enter, the roll, the Morale Level, the DRM (the TEM there), and the result.
/// </summary>
public sealed record InfantryOverrunNtc(IReadOnlyList<string> Units, string Taker, BoardLocation Target, string Roll, int Morale, int Drm, bool Passed) : EventPayload;

/// <summary>A NTC of an Infantry OVR taken in this phase, as the state keeps it: the stack, the Location, and the result (A4.15; pass 40).</summary>
public sealed record InfantryOverrunNtcTaken(IReadOnlyList<string> Units, string Target, bool Passed, string? Taker = null);

/// <summary>
/// <c>rout-stepped</c>: one Location of a broken unit's rout in the RtPh (A10.5; backlog pass 13, ruling R13.3): the Location entered, its cost in
/// half MF, and whether it is Low Crawl (A10.52), which uses all the unit's MF. A step repulsed by a concealed unit (A10.533; pass 35, task 35.4)
/// names in <see cref="Attempted"/> the Location it tried to enter, and its Location is the one the unit stays in, where its rout ends.
/// </summary>
public sealed record RoutStepped(string Unit, BoardLocation To, int HalfMf, bool LowCrawl) : EventPayload
{
    /// <summary>The Location a repulsed step tried to enter (A10.533), or null for a step that was made.</summary>
    public BoardLocation? Attempted
    {
        get; init;
    }

    /// <summary>
    /// The broken unit an unbroken leader routs with by Voluntary Rout (A10.711; pass 70, ruling R70.12), whose step just before is his; null for a
    /// broken unit's own step, as every older record has it.
    /// </summary>
    public string? With
    {
        get; init;
    }
}

/// <summary>
/// <c>rout-interdicted</c>: the NMC of Interdiction as a routing unit enters Open Ground (A10.53; ruling R13.3): the roll, the unit's Morale Level, the
/// DRM, and its result: <c>passed</c>, <c>pinned</c>, <c>reduced</c>, or <c>eliminated</c>, which the events after it apply.
/// </summary>
public sealed record RoutInterdicted(string Unit, BoardLocation At, string Roll, int Morale, int Drm, string Result) : EventPayload
{
    public const string Passed = "passed";
    public const string Pinned = "pinned";
    public const string Reduced = "reduced";
    public const string Eliminated = "eliminated";

    /// <summary>The unit that Interdicts (A10.532); null in a record that does not name it.</summary>
    public string? Interdictor
    {
        get; init;
    }

    /// <summary>
    /// The leader who routs with the unit by Voluntary Rout and whose leadership is the <see cref="Drm"/> (A10.711; pass 70, ruling R70.12); null with no
    /// such leader, as every older record has it.
    /// </summary>
    public string? Leader
    {
        get; init;
    }

    /// <summary>The result of an Interdiction NMC (A10.53, A10.31): an Original 12 eliminates, a Final DR above the Morale Level reduces, equal pins.</summary>
    public static string For(int original, int final, int morale) => Rules.ScenarioA1ResultTables.RoutInterdiction(original, final, morale);
}

/// <summary>
/// <c>voluntary-break</c>: units that break voluntarily at the start of the RtPh so as to rout (A10.41; pass 70, ruling R70.11). The events after it
/// break them, under DM (A10.62), and clear a pin (A7.83); a leader's break may call for a LLTC (A10.2).
/// </summary>
public sealed record VoluntaryBreak(IReadOnlyList<string> Units) : EventPayload;

/// <summary>
/// <c>units-searched</c>: members of the moving stack Search as it ends its move (A12.152; pass 70, ruling R70.24): the units, their Location, the Search dr's
/// roll and drm, how many Accessible hexes other than its own the stack has, and the hexes searched, its own first, each at ground level (every Location of a
/// hex above ground is searched). The searchers spend one more MF there and become TI for the rest of the Player Turn; the DEFENDER's window opens on that MF
/// (A8.1), and their move ends when it closes. The reveals and any Casualty dr (A12.154) follow as their own records. A new event: no older record holds one.
/// </summary>
public sealed record UnitsSearched(IReadOnlyList<string> Units, BoardLocation Location, string Roll, int Drm, int Accessible, IReadOnlyList<BoardLocation> Searched) : EventPayload
{
    /// <summary>
    /// The drm of the DEFENDER's Casualty dr that follows, from his concealed and hidden units as they were at the Search dr (A12.154; pass 70's fixes), for
    /// the record line of the views that may know it; null when no Casualty dr is made, and on a record made before the fixes. No rule reads it at replay.
    /// </summary>
    public int? CasualtyDrm
    {
        get; init;
    }
}

/// <summary>
/// <c>deployment-attempted</c>: a squad's attempt to Deploy in its RPh (A1.31; ruling R13.4): the directing leader (none for Guards), the NTC roll,
/// the squad's Morale Level, the leadership DRM, and whether it passed; a passed attempt's lineage follows.
/// </summary>
public sealed record DeploymentAttempted(string Squad, string? Leader, string Roll, int Morale, int Drm, bool Passed) : EventPayload
{
    /// <summary>Choice bound before the NTC, including an unsuccessful attempt.</summary>
    public string? SpotterHalf
    {
        get; init;
    }
}

/// <summary>
/// <c>rph-action-taken</c>: units whose RPh action is spent by a Recombination or a Transfer (A1.32, A4.431; rulings R13.4, R13.5), with the action.
/// </summary>
public sealed record RallyPhaseActionTaken(IReadOnlyList<string> Units, string Action) : EventPayload;

/// <summary>
/// <c>recovery-attempted</c>: a unit's attempt to Recover a SW (A4.44; ruling R13.5): the dr, its DRM, and whether it succeeded; in the MPh it costs one
/// MF. A successful Recovery's transfer of the SW follows.
/// </summary>
public sealed record RecoveryAttempted(string Unit, string Weapon, string Roll, int Drm, bool Recovered) : EventPayload;

/// <summary>
/// <c>opportunity-fire-declared</c>: Infantry marked with a Bounding Fire counter in their PFPh as Opportunity Firers (A7.25; backlog pass 12, ruling
/// R12.1): they neither fire in the PFPh nor move in the MPh, and fire in the AFPh without its halving.
/// </summary>
public sealed record OpportunityFireDeclared(IReadOnlyList<string> Units) : EventPayload;

/// <summary>
/// <c>encirclement-placed</c>: a Location Encircled by the fire record named (A7.7; ruling R12.11): the side's non-berserk, non-heroic Personnel there, and
/// every Melee unit there, have their Morale Level one lower against attacks on it, +1 on their own fire, and double MF for the first Location they enter.
/// </summary>
public sealed record EncirclementPlaced(BoardLocation Location, string Side, string Fire) : EventPayload;

/// <summary>An Encircled Location and the side whose units there are Encircled (A7.7; ruling R12.11).</summary>
public sealed record Encirclement(BoardLocation Location, string Side);

/// <summary>
/// <c>fire-lane-placed</c>: a Fire Lane declared with a MG's Defensive First Fire (A9.22; ruling R12.7): its MG, the manning Infantry, and each
/// Location of the lane with its Fire Lane Residual FP and the Hindrance DRM of the LOS from the MG.
/// </summary>
public sealed record FireLanePlaced(string Fire, string Weapon, string Operator, IReadOnlyList<FireLaneEntry> Entries) : EventPayload;

/// <summary>One Location of a Fire Lane (A9.22, A9.222).</summary>
public sealed record FireLaneEntry(BoardLocation Location, int Fp, int HindranceDrm);

/// <summary>A Fire Lane in place (A9.22, A9.223; ruling R12.7).</summary>
public sealed record FireLane(string Fire, string Weapon, string Operator, IReadOnlyList<FireLaneEntry> Entries);

/// <summary><c>movement-window-closed</c>: the DEFENDER passes on the stack's latest MF expenditure (A8.11).</summary>
public sealed record MovementWindowClosed(int Step) : EventPayload;

/// <summary><c>movement-ended</c>: the ATTACKER declares the end of the stack's move (A8.11): its MPh ends.</summary>
public sealed record MovementEnded(IReadOnlyList<string> Movers) : EventPayload;

/// <summary>
/// The stack moving now: the units that moved in its latest step (the DEFENDER's targets), their Location, the half MF
/// spent in that Location, the step count, whether it declared Assault Movement (A4.61), and whether the DEFENDER's window
/// on the latest step is open. <see cref="Members"/> are the stack's units that may still move: they may move on together
/// or apart, and no other unit moves until the ATTACKER ends them all (A4.2).
/// </summary>
public sealed record MovementState(IReadOnlyList<string> Movers, BoardLocation Location, int HalfMfInLocation, int Step, bool Assault, bool WindowOpen)
{
    public IReadOnlyList<string> Members { get; init; } = Movers;

    /// <summary>The Location a berserk stack charges (A15.431), from its latest step; null for other moves.</summary>
    public BoardLocation? Charge
    {
        get; init;
    }

    /// <summary>Whether the mover is a vehicle (D2.1): its steps are its MP expenditures.</summary>
    public bool Vehicle
    {
        get; init;
    }

    /// <summary>A moving vehicle's state: started (D2.12, or began its MPh in Motion, D2.4) and not stopped since, or stopped (D2.13).</summary>
    public bool Started
    {
        get; init;
    }

    public bool Stopped
    {
        get; init;
    }

    /// <summary>The members whose move ends when the DEFENDER's window on this step closes, such as a squad whose SMOKE dr was a 6 (A24.1).</summary>
    public IReadOnlyList<string> EndingMembers { get; init; } = [];

    /// <summary>Whether the vehicle spent its MP left in its final hex (ruling R5.15): its move ends when the DEFENDER's window closes.</summary>
    public bool Ending
    {
        get; init;
    }

    /// <summary>Whether the latest step was a Minimum Move (A4.134; ruling R10.9).</summary>
    public bool MinimumMove
    {
        get; init;
    }

    /// <summary>Whether the latest step entered the Location at the road rate, into a hex that has other terrain (A4.132; pass 40).</summary>
    public bool RoadRate
    {
        get; init;
    }

    /// <summary>The Location the stack Dashes to from the road Location it is in (A4.63; pass 40); null when it is not Dashing.</summary>
    public BoardLocation? Dash
    {
        get; init;
    }

    /// <summary>The laden MMC the leader's IPC went to with an earlier step of this move, when the mover named it (A4.42; pass 40).</summary>
    public string? IpcTo
    {
        get; init;
    }

    /// <summary>
    /// The wounded men carried with this move's steps and their carriers (A17.2; pass 71, ruling R71.16), kept for the move: a carrier's five PP are not
    /// recouped (A4.4), and a man set down moves no farther. Null when none was carried.
    /// </summary>
    public IReadOnlyList<CarriedMan>? Carried
    {
        get; init;
    }

    /// <summary>Whether a step of this move has changed the stack's Location: Assault Movement allows one such step (A4.61; pass 40).</summary>
    public bool ChangedLocation
    {
        get; init;
    }

    /// <summary>The Location the stack's latest step entered as an Infantry OVR or to recapture Unarmed units (A4.15, A20.54; pass 40); null otherwise.</summary>
    public BoardLocation? InfantryOverrun
    {
        get; init;
    }

    /// <summary>The Location the latest step left (null when it stayed): for Snap Shots and Height Advantage (A8.15, B10.31; rulings R10.4, R10.13).</summary>
    public BoardLocation? From
    {
        get; init;
    }

    /// <summary>The Gun its crew pushed with the latest step: Hazardous Movement (A4.62; ruling R10.8); null when none.</summary>
    public string? PushedGun
    {
        get; init;
    }

    /// <summary>The hexsides the stack moves along in Bypass in its Location (A4.3; ruling R10.7); null when it is not in Bypass.</summary>
    public IReadOnlyList<HexsideDirection>? Bypass
    {
        get; init;
    }

    /// <summary>Whether a moving vehicle Started in Reverse (D2.23; ruling R11.1).</summary>
    public bool Reverse
    {
        get; init;
    }

    /// <summary>The Location of an OVR declared and not yet resolved (D7.1; ruling R11.11): the vehicle spends nothing more until it is.</summary>
    public BoardLocation? Overrun
    {
        get; init;
    }

    /// <summary>Whether the open window is the Reaction Fire window after an OVR's resolution (D7.2; ruling R11.13).</summary>
    public bool Reaction
    {
        get; init;
    }

    /// <summary>The hex a moving vehicle left to enter its present one (D7.15; ruling R11.11): an OVR takes the wall TEM of the hexside it crossed.</summary>
    public BoardLocation? EnteredFrom
    {
        get; init;
    }

    /// <summary>
    /// The units that boarded or disembarked with a vehicle's latest MP expenditure (D6.4, D6.5, D5.43; pass 65, ruling R65.14): Infantry targets of the
    /// DEFENDER's First Fire with it; null after any other step.
    /// </summary>
    public IReadOnlyList<string>? Embarking
    {
        get; init;
    }

    /// <summary>Whether <see cref="Embarking"/> disembarked (D6.5) rather than boarded (D6.4).</summary>
    public bool Unloading
    {
        get; init;
    }

    /// <summary>
    /// The units a moving vehicle disembarked this move by an unload recorded since pass 65 (D6.5; ruling R65.15): they move on before any other unit
    /// once its move ends; null when none did.
    /// </summary>
    public IReadOnlyList<string>? Disembarked
    {
        get; init;
    }

    /// <summary>
    /// The Riders that Bailed Out with the moving vehicle's latest MP expenditure or under the DEFENDER's fire at it (D6.24; pass 65, ruling R65.17):
    /// for Defensive First Fire they spent all their MF, at least one, and take FFNAM as unloaders do; null when none did.
    /// </summary>
    public IReadOnlyList<string>? BailedOut
    {
        get; init;
    }
}
