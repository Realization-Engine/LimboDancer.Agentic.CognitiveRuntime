using System.Text.Json;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// <c>fire-resolved</c>: a fire attack resolved by the Fire package (Fire in Live Play, unit step 18). It records the
/// firers, the directing leader, the two Locations, the declared facts, the rolls it used by purpose, and the package's
/// resolution; the effects on units follow as ordinary condition, elimination, and lineage events. The Units project
/// does not interpret the facts or the resolution: replay hands them to an <see cref="IFireRecordVerifier"/>, which
/// recomputes the resolution and refuses a record that disagrees.
/// </summary>
public sealed record FireResolved(
    IReadOnlyList<string> Firers,
    string? Director,
    string FirerLocation,
    string TargetLocation,
    IReadOnlyDictionary<string, string> Rolls,
    JsonElement Facts,
    JsonElement Resolution) : EventPayload
{
    /// <summary>The movement step a Defensive fire record answers (A8.1, unit step 22); null in the fire phases.</summary>
    public int? MovementStep
    {
        get; init;
    }
}

/// <summary>
/// <c>fire-reported</c>: the public part of a fire record that only the target side may see, because the attack left a
/// concealed target concealed (A12.14, p. 77). It names the record and repeats its Locations and its arithmetic (the
/// firers' FP, the column, the DRM, the dice, the final DR, and the IFT result), which name no target unit, so the firing
/// side learns what it would know at the table. Replay refuses a report that disagrees with its record.
/// </summary>
public sealed record FireReported(string Fire, string FirerLocation, string TargetLocation, JsonElement Arithmetic) : EventPayload;

/// <summary>An attack made in the current phase, for the mandatory fire group rule (A7.55, p. 57).</summary>
public sealed record FireRecord(string EventId, string FirerLocation, string TargetLocation)
{
    /// <summary>The movement step the attack answered; A7.55 binds a Location's units within one MF expenditure (A8.1).</summary>
    public int? Step
    {
        get; init;
    }

    /// <summary>Whether a vehicle's MG made the attack (D3.4): it never joins its Location's fire group (ruling R25.7).</summary>
    public bool Vehicle
    {
        get; init;
    }
}

/// <summary>Recomputes a fire record from its facts and rolls; supplied to replay by the project that owns the package.</summary>
public interface IFireRecordVerifier
{
    /// <summary>
    /// Null when the record reproduces from its facts and recorded rolls and its facts agree with the state it is made
    /// in; otherwise the reason it does not.
    /// </summary>
    public string? Verify(GameState state, FireResolved fire, IReadOnlyDictionary<string, DiceRolled> rolls);
}
