using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// <c>advanced</c>: units of the phasing side advance one Location in the APh (A3.7, A4.7; unit step 29), together from
/// one Location. A unit advances once per APh; a Guard's prisoners go with it (A20.53).
/// </summary>
public sealed record AdvanceMoved(IReadOnlyList<string> Units, BoardLocation To) : EventPayload;

/// <summary>
/// <c>ambush-rolled</c>: the Ambush drs of a CC Location (A11.4), resolved by the Close Combat package: the rolls by side,
/// the side that ambushes (null for none), the facts, and the resolution. Replay hands it to an
/// <see cref="ICloseCombatRecordVerifier"/>.
/// </summary>
public sealed record AmbushRolled(BoardLocation Location, IReadOnlyDictionary<string, string> Rolls, string? Ambusher, JsonElement Facts,
    JsonElement Resolution) : EventPayload;

/// <summary>
/// <c>close-combat-resolved</c>: one round of CC in a Location (A11.12; unit step 29): the round, the units that attack and
/// those attacked, the rolls by key, the facts, and the resolution; its effects follow as ordinary events. Replay hands it
/// to an <see cref="ICloseCombatRecordVerifier"/>.
/// </summary>
public sealed record CloseCombatResolved(BoardLocation Location, string Round, IReadOnlyList<string> Attackers, IReadOnlyList<string> Defenders,
    IReadOnlyDictionary<string, string> Rolls, JsonElement Facts, JsonElement Resolution) : EventPayload
{
    public const string Simultaneous = "simultaneous";
    public const string AmbusherRound = "ambusher";
    public const string AmbushedRound = "ambushed";
}

/// <summary>
/// <c>surrender-pending</c>: a unit surrenders after a Heat of Battle DR of 12 to one of these ADJACENT Known Good Order armed
/// enemy Infantry units, the captor's choice (A15.5, A20.21). Until the captor's side chooses, the game waits.
/// </summary>
public sealed record SurrenderPending(string Unit, IReadOnlyList<string> Captors) : EventPayload;

/// <summary>Recomputes CC and Ambush records from their facts and rolls; supplied to replay by the project that owns the package.</summary>
public interface ICloseCombatRecordVerifier
{
    /// <summary>Null when the Ambush record reproduces and its facts agree with the state; otherwise why not.</summary>
    public string? VerifyAmbush(GameState state, AmbushRolled ambush, IReadOnlyDictionary<string, DiceRolled> rolls);

    /// <summary>Null when the CC record reproduces and its facts agree with the state; otherwise why not.</summary>
    public string? Verify(GameState state, CloseCombatResolved combat, IReadOnlyDictionary<string, DiceRolled> rolls);
}

/// <summary>
/// The CC of one Location this CCPh (A11.12): whether its Ambush drs were rolled and who ambushed, the rounds resolved,
/// and the units that have attacked or been attacked. It is closed once its last round is resolved.
/// </summary>
public sealed record CloseCombatLocation(BoardLocation Location, bool AmbushRolled, string? Ambusher, IReadOnlyList<string> Rounds, bool Closed)
{
    public IReadOnlyList<string> Attacking { get; init; } = [];

    public IReadOnlyList<string> Attacked { get; init; } = [];
}

/// <summary>A unit's advance this Player Turn and the Location it entered, which decides whether an Ambush can occur (A11.4).</summary>
public sealed record AdvanceRecord(string Unit, BoardLocation To);

/// <summary>A surrender awaiting its captor's choice (A15.5, A20.21).</summary>
public sealed record PendingSurrender(string Unit, IReadOnlyList<string> Captors, string Event);
