using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
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
}

/// <summary>
/// A SMOKE grenade placement attempt (A24.1; ruling R9.5): a dr at most the exponent places a SMOKE counter in the Location named; a dr of 6 ends
/// the squad's MPh.
/// </summary>
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
}

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
}
