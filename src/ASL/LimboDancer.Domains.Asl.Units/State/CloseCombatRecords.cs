using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// <c>advanced</c>: units of the phasing side advance one Location in the APh (A3.7, A4.7; unit step 29), together from
/// one Location. A unit advances once per APh; a Guard's prisoners go with it (A20.53).
/// </summary>
public sealed record AdvanceMoved(IReadOnlyList<string> Units, BoardLocation To) : EventPayload
{
    /// <summary>The map edge the units leave by from <see cref="To"/>, their own Location (A2.6; ruling R25.5); null for an advance on the map.</summary>
    public string? Exit
    {
        get; init;
    }
}

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

    /// <summary>The prisoners' escape round, before any other in the Location (A11.33, A20.55; ruling R14.6).</summary>
    public const string PrisonersRound = "prisoners";
}

/// <summary>
/// <c>vehicle-close-combat-resolved</c>: one CC attack with a vehicle (A11.5, A11.62, D7.21; backlog pass 11, rulings R11.13 to R11.16): Infantry
/// attacking the vehicle, or the vehicle attacking Infantry, in the CCPh, or CC Reaction Fire in the MPh; the rolls by key, the facts, and the
/// resolution. In the CCPh it names the side that attacks next (A11.31) and whether the Location's CC is over, as the planner read them. Replay
/// hands it to an <see cref="ICloseCombatRecordVerifier"/>.
/// </summary>
public sealed record VehicleCloseCombatResolved(BoardLocation Location, string Vehicle, IReadOnlyList<string> Attackers, IReadOnlyList<string> Defenders, bool ByVehicle,
    bool Reaction, IReadOnlyDictionary<string, string> Rolls, JsonElement Facts, JsonElement Resolution) : EventPayload
{
    public string? Next
    {
        get; init;
    }

    public bool Closed
    {
        get; init;
    }
}

/// <summary>
/// <c>vehicle-close-combat-passed</c>: a side passes its attack in a CC Location holding a vehicle (A11.31; ruling R11.16), and makes no more there this
/// CCPh; the side that attacks next and whether the Location's CC is over, as the planner read them.
/// </summary>
public sealed record VehicleCloseCombatPassed(BoardLocation Location, string Side, string? Next, bool Closed) : EventPayload;

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

    /// <summary>Null when the record of a CC attack with a vehicle reproduces and its facts agree with the state; otherwise why not (ruling R11.14).</summary>
    public string? VerifyVehicle(GameState state, VehicleCloseCombatResolved combat, IReadOnlyDictionary<string, DiceRolled> rolls) =>
        "A CC record with a vehicle needs a verifier that reads it.";
}

/// <summary>
/// The CC of one Location this CCPh (A11.12): whether its Ambush drs were rolled and who ambushed, the rounds resolved,
/// and the units that have attacked or been attacked. It is closed once its last round is resolved.
/// </summary>
public sealed record CloseCombatLocation(BoardLocation Location, bool AmbushRolled, string? Ambusher, IReadOnlyList<string> Rounds, bool Closed)
{
    public IReadOnlyList<string> Attacking { get; init; } = [];

    public IReadOnlyList<string> Attacked { get; init; } = [];

    /// <summary>The side whose attack is next in a Location holding a vehicle (A11.31; ruling R11.16); null in other Locations.</summary>
    public string? Next
    {
        get; init;
    }

    /// <summary>The sides that passed in a Location holding a vehicle, and attack no more there this CCPh (ruling R11.16).</summary>
    public IReadOnlyList<string> Passed { get; init; } = [];

    /// <summary>Whether the Location's CC is Hand-to-Hand this CCPh, as its first round declared (J2.31; ruling R14.1).</summary>
    public bool HandToHand
    {
        get; init;
    }
}

/// <summary>A unit's advance this Player Turn and the Location it entered, which decides whether an Ambush can occur (A11.4).</summary>
public sealed record AdvanceRecord(string Unit, BoardLocation To);

/// <summary>A surrender awaiting its captor's choice (A15.5, A20.21).</summary>
public sealed record PendingSurrender(string Unit, IReadOnlyList<string> Captors, string Event);
