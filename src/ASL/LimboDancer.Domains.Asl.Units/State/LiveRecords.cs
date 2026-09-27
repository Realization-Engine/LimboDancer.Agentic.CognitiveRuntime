using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;

namespace LimboDancer.Domains.Asl.Units.State;

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
public sealed record MovementStepped(IReadOnlyList<string> Movers, BoardLocation To, int HalfMf, bool Assault, int Step) : EventPayload;

/// <summary><c>movement-window-closed</c>: the DEFENDER passes on the stack's latest MF expenditure (A8.11).</summary>
public sealed record MovementWindowClosed(int Step) : EventPayload;

/// <summary><c>movement-ended</c>: the ATTACKER declares the end of the stack's move (A8.11): its MPh ends.</summary>
public sealed record MovementEnded(IReadOnlyList<string> Movers) : EventPayload;

/// <summary>
/// The stack moving now: its units, its Location, the half MF spent in that Location, the step count, whether it declared
/// Assault Movement (A4.61), and whether the DEFENDER's window on the latest step is open.
/// </summary>
public sealed record MovementState(IReadOnlyList<string> Movers, BoardLocation Location, int HalfMfInLocation, int Step, bool Assault, bool WindowOpen);
