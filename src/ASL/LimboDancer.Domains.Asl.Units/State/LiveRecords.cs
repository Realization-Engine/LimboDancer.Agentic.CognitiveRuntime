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
    public const string Repaired = "repaired";
    public const string NoChange = "no-change";
    public const string Eliminated = "eliminated";
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
}

/// <summary>
/// A SMOKE grenade placement attempt (A24.1; ruling R9.5): a dr at most the exponent places a SMOKE counter in the Location named; a dr of 6 ends
/// the squad's MPh.
/// </summary>
/// <summary>A DC Placement (A23.3; ruling R15.2).</summary>
public sealed record DcPlacement(string Unit, string Charge, BoardLocation Target, bool Cx, bool TargetsConcealed);

public sealed record SmokeAttempt(string Unit, BoardLocation Target, string Roll, int Dr, int Exponent)
{
    /// <summary>Whether the squad is CX, which adds one to the dr (A4.51; table player, pass 9).</summary>
    public bool Cx
    {
        get; init;
    }

    public bool Placed => Dr + (Cx ? 1 : 0) <= Exponent;
}

/// <summary>The leader directing a SW's To Hit DR this phase (A7.53).</summary>
public sealed record SupportWeaponDirector(string Gun, string Leader);

/// <summary>A light mortar's Spotter (C9.3; ruling R9.4).</summary>
public sealed record MortarSpotter(string Gun, string Spotter);

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

    /// <summary>For a load or an unload (D6.4, D6.5; ruling R26.2): the Infantry boarding or the Passengers disembarking.</summary>
    public IReadOnlyList<string>? Units
    {
        get; init;
    }

    /// <summary>For an unload (D6.5; ruling R26.2): the MF each disembarking unit has spent once beneath the vehicle.</summary>
    public int UnitMf
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

    public const string Passed = "passed";
    public const string Bogged = "bogged";
    public const string Freed = "freed";
    public const string Mired = "mired";
    public const string Immobilized = "immobilized";

    /// <summary>The MP an ESB DR sought (D2.5); zero for other checks.</summary>
    public int Mp
    {
        get; init;
    }

    /// <summary>The result a check's Final DR or dr gives (D2.5, D2.51, D8.21, D8.3).</summary>
    public static string For(string check, int final) => check switch
    {
        Bog => final >= 12 ? Bogged : Passed,
        Esb => final >= 12 ? Immobilized : Passed,
        Mechanical => final >= 12 ? Immobilized : Passed,
        BogRemoval => final <= 4 ? Freed : final == 5 ? Mired : Immobilized,
        _ => Passed,
    };
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
/// <c>rout-stepped</c>: one Location of a broken unit's rout in the RtPh (A10.5; backlog pass 13, ruling R13.3): the Location entered, its cost in
/// half MF, and whether it is Low Crawl (A10.52), which uses all the unit's MF.
/// </summary>
public sealed record RoutStepped(string Unit, BoardLocation To, int HalfMf, bool LowCrawl) : EventPayload;

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

    /// <summary>The result of an Interdiction NMC (A10.53, A10.31): an Original 12 eliminates, a Final DR above the Morale Level reduces, equal pins.</summary>
    public static string For(int original, int final, int morale) =>
        original == 12 ? Eliminated : final > morale ? Reduced : final == morale ? Pinned : Passed;
}

/// <summary>
/// <c>deployment-attempted</c>: a squad's attempt to Deploy in its RPh (A1.31; ruling R13.4): the directing leader (none for Guards), the NTC roll,
/// the squad's Morale Level, the leadership DRM, and whether it passed; a passed attempt's lineage follows.
/// </summary>
public sealed record DeploymentAttempted(string Squad, string? Leader, string Roll, int Morale, int Drm, bool Passed) : EventPayload;

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
}
