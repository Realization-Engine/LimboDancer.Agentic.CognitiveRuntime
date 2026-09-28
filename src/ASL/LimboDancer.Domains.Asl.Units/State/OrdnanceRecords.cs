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

/// <summary>
/// The Turret Covered Arc of a turreted AFV once its MA fired outside its VCA (D3.12; ruling R7.10): the TCA keeps its direction while the
/// hull moves or turns. A vehicle with no record has its TCA along its VCA.
/// </summary>
public sealed record TurretFacing(string Vehicle, UnitFacing Facing);

/// <summary>A Special Ammunition a Gun or an AFV has run out of for the rest of the scenario (C8.9; ruling R7.6).</summary>
public sealed record DepletedAmmunition(string Gun, string Ammunition);

/// <summary>
/// <c>shock-recovery-rolled</c>: a Shocked AFV's or an Unconfirmed Kill's dr in the RPh after it was placed (C7.42; ruling R7.8): the
/// vehicle, the recorded dr, and the result. Replay recomputes it: a Shock is removed on 1 or 2 and becomes an Unconfirmed Kill on 3 to 6;
/// an Unconfirmed Kill is removed on 1 to 3 and wrecks the AFV on 4 to 6.
/// </summary>
public sealed record ShockRecoveryRolled(string Vehicle, string Roll, string Result) : EventPayload
{
    public const string Recovered = "recovered";
    public const string UnconfirmedKill = "unconfirmed-kill";
    public const string Wrecked = "wrecked";

    /// <summary>The result of a dr for a Shocked AFV, or for an Unconfirmed Kill.</summary>
    public static string For(bool unconfirmedKill, int dr) => unconfirmedKill ? dr <= 3 ? Recovered : Wrecked : dr <= 2 ? Recovered : UnconfirmedKill;
}

/// <summary>A Gun's shots this fire phase and whether its last one kept its Multiple ROF (C2.24); cleared at every phase change.</summary>
public sealed record OrdnanceShotRecord(string Gun, int Shots, bool RateOfFireKept);

/// <summary>
/// The Location a Gun has acquired and its Acquisition DRM, -1 or -2 (C6.5, C6.51), and the Known units it is on (ruling R5.13): the counter
/// follows them while they are in the Gun's LOS. With no units it stays on its Location.
/// </summary>
public sealed record GunAcquisition(string Gun, BoardLocation Location, int Level)
{
    public IReadOnlyList<string> Units { get; init; } = [];

    public bool Equals(GunAcquisition? other) =>
        other is not null && Gun == other.Gun && Location == other.Location && Level == other.Level && Units.SequenceEqual(other.Units, StringComparer.Ordinal);

    public override int GetHashCode() => HashCode.Combine(Gun, Location, Level, Units.Count);
}
