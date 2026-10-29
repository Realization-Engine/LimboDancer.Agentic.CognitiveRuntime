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
    BoardLocation? Acquired, IReadOnlyDictionary<string, string> Rolls, JsonElement Facts, JsonElement Resolution) : EventPayload
{
    /// <summary>Version 1 uses current assignment occurrences and applies Rules' spotting marks. Absent preserves legacy replay.</summary>
    public int? SpotterWiringVersion
    {
        get; init;
    }
}

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

/// <summary>
/// A weapon of a vehicle that fired this Player Turn (D3.5, D3.51, pp. 200 and 201; pass 50): the vehicle, the weapon (<c>ma</c>, <c>bmg</c>, or
/// <c>cmg</c>), the fire phase (Defensive First Fire and Final Fire are one, A.15), the hexspines its CA changed for the shot, and whether that
/// change was doubled by woods, a building, or rubble (C5.11). Kept until the Player Turn ends.
/// </summary>
public sealed record VehicleWeaponShot(string Vehicle, string Weapon, string FirePhase, int Turn, bool Doubled)
{
    /// <summary>The movement step a shot of the phasing side's MPh followed (C5.3; pass 50): its MA fires again only after another MP expenditure. Null outside it.</summary>
    public int? Step
    {
        get; init;
    }
}

/// <summary>
/// A Gun Duel under way (C2.2401, p. 167; pass 55, ruling R55.19): the moving vehicle that answered <c>duel</c>, the DEFENDER's Gun or vehicle
/// whose declared shot it answers, the movement step the duel belongs to, and whether the vehicle's first shot is recorded. The declared shot's
/// record, or the record that it is void, ends it.
/// </summary>
public sealed record GunDuelState(string Vehicle, string Firer, int Step, bool Fired);

/// <summary>
/// <c>gun-duel-voided</c>: the declared shot of <paramref name="Firer"/> is not made after the first shot of a Gun Duel by
/// <paramref name="Vehicle"/> (C2.2401; pass 55, ruling R55.19): that shot "voids the opponent's return shot by eliminating, breaking, stunning
/// or shocking it". <paramref name="Cause"/> names which (<c>eliminated</c>, <c>broken</c>, <c>stunned</c>, <c>shocked</c>, <c>gun</c> for a Gun
/// destroyed or malfunctioned, or <c>other</c>: a shot the first shot left none of those causes for, which the checks of every shot refused;
/// <see cref="Reason"/> then says why).
/// </summary>
public sealed record GunDuelVoided(string Firer, string Vehicle, string Cause) : EventPayload
{
    /// <summary>Why the declared shot is not fired when its cause is <c>other</c>, in the refusal's words; null otherwise, and in a record made before the review of pass 55.</summary>
    public string? Reason
    {
        get; init;
    }
}

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
    public static string For(bool unconfirmedKill, int dr) => Rules.ScenarioA1ResultTables.ShockRecovery(unconfirmedKill, dr);
}

/// <summary>
/// <c>bore-sighted</c>: at setup, the Scenario Defender's Gun Bore Sights one Location outside its hex, in its LOS, within 16 hexes (C6.41,
/// C6.42; ruling R8.8): its original crew firing it from its setup Location adds Case M's -2 there (C6.43).
/// </summary>
public sealed record BoreSighted(string Gun, BoardLocation Location, string Crew, BoardLocation SetupLocation) : EventPayload;

/// <summary>
/// <c>gun-turned</c>: a Gun changes its CA without firing in a friendly fire phase (C3.22; ruling R8.9): it fires no more that phase, and a
/// change in the PFPh keeps the Gun and its crew from moving that Player Turn.
/// </summary>
public sealed record GunTurned(string Gun, UnitFacing Facing) : EventPayload;

/// <summary>
/// <c>manhandling-rolled</c>: a crew's Manhandling DR to push its Gun into an adjacent hex (C10.3; ruling R8.6): the Gun, the recorded roll,
/// the DRM, the Gun's M#, and the result: <c>enter</c> below the M#, <c>enter-stop</c> at it, <c>stay</c> above it. Replay recomputes it.
/// </summary>
public sealed record ManhandlingRolled(string Gun, string Roll, int Drm, int Manhandling, string Result) : EventPayload
{
    public const string Enter = "enter";
    public const string EnterStop = "enter-stop";
    public const string Stay = "stay";

    /// <summary>The result of a Final Manhandling DR against the M# (C10.3).</summary>
    public static string For(int final, int manhandling) => Rules.ScenarioA1ResultTables.Manhandling(final, manhandling);
}

/// <summary>
/// <c>gun-hooked</c>: a Stopped truck or halftrack hooks up a Gun in its hex, or unhooks it there, spending half its MP (FRU) with the Gun's crew
/// on foot in the hex (C10.11, C10.12; ruling R8.6). Hooked, the Gun is towed and its crew is on foot; unhooked, the crew mans it again with
/// the facing given.
/// </summary>
public sealed record GunHooked(string Vehicle, string Gun, string Crew, bool Hooked, int Mp, UnitFacing? Facing) : EventPayload
{
    /// <summary>Whether the crew boards the towing vehicle as a Passenger as it hooks up the Gun, at no extra MP (C10.11; ruling R26.2).</summary>
    public bool Boards
    {
        get; init;
    }
}

/// <summary>A Gun's shots this fire phase and whether its last one kept its Multiple ROF (C2.24); cleared at every phase change.</summary>
public sealed record OrdnanceShotRecord(string Gun, int Shots, bool RateOfFireKept)
{
    /// <summary>The moving vehicle's MP in its Location its Defensive First Fire shots have claimed (C6.17 and its EX); zero otherwise.</summary>
    public int Mp
    {
        get; init;
    }
}

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
