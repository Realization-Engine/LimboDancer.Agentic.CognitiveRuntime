using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Documents;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// <c>ordnance-fired</c>: a Gun's HE shot at a Location on the Infantry Target Type (C3.3, C3.32; unit step 24), resolved by the
/// Ordnance package: the Gun and its crew, the target Location, the Gun's new facing when the shot turned it (C3.21), whether
/// it kept its Multiple ROF (C2.24), the Acquisition it holds after the shot and where (C6.5), the rolls by key, the facts, and
/// the resolution. The hit's effects follow as ordinary events. Replay hands it to an <see cref="IOrdnanceRecordVerifier"/>.
/// </summary>
public sealed record OrdnanceFired(string Gun, string Crew, BoardLocation Target, UnitFacing? Facing, bool RateOfFireKept, int Acquisition,
    BoardLocation? Acquired, IReadOnlyDictionary<string, string> Rolls, JsonElement Facts, JsonElement Resolution) : EventPayload;

/// <summary>Recomputes ordnance records from their facts and rolls; supplied to replay by the project that owns the package.</summary>
public interface IOrdnanceRecordVerifier
{
    /// <summary>Null when the record reproduces and its facts agree with the state; otherwise why not.</summary>
    public string? Verify(GameState state, OrdnanceFired fired, IReadOnlyDictionary<string, DiceRolled> rolls);
}

/// <summary>A Gun's shots this fire phase and whether its last one kept its Multiple ROF (C2.24); cleared at every phase change.</summary>
public sealed record OrdnanceShotRecord(string Gun, int Shots, bool RateOfFireKept);

/// <summary>The Location a Gun has acquired and its Acquisition DRM, -1 or -2 (C6.5, C6.51).</summary>
public sealed record GunAcquisition(string Gun, BoardLocation Location, int Level);
