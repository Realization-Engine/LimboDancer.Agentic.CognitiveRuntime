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
    JsonElement Resolution) : EventPayload;

/// <summary>An attack made in the current phase, for the mandatory fire group rule (A7.55, p. 57).</summary>
public sealed record FireRecord(string EventId, string FirerLocation, string TargetLocation);

/// <summary>Recomputes a fire record from its facts and rolls; supplied to replay by the project that owns the package.</summary>
public interface IFireRecordVerifier
{
    /// <summary>
    /// Null when the record reproduces from its facts and recorded rolls and its facts agree with the state it is made
    /// in; otherwise the reason it does not.
    /// </summary>
    public string? Verify(GameState state, FireResolved fire, IReadOnlyDictionary<string, DiceRolled> rolls);
}
