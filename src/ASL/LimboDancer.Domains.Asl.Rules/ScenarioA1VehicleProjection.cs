namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The projector's vehicle decisions (pass 32.e): the vehicle step's, the vehicle check's, the OVR's, the PAATC's, the Passengers', and the
/// wreck's. The projector reads the state and rewrites it; these take plain values and decide.
/// </summary>
public static class ScenarioA1VehicleProjection
{
    /// <summary>D2.1 (UNIT-STATE-034): a vehicle step is made in the MPh; the projector asks before it reads the vehicle.</summary>
    public static bool StepPhase(string? phase) => phase == "mph";

    /// <summary>
    /// D2.1 (UNIT-STATE-034): a vehicle step moves a vehicle of the phasing side that has not ended its move; only a Load spends no MP.
    /// </summary>
    public static bool StepAllowed(bool vehicle, bool phasing, bool movementEnded, int halfMp, bool load) =>
        vehicle && phasing && !movementEnded && halfMp >= 0 && (halfMp != 0 || load);

    /// <summary>D5.341: a Recall stops the AFV like a Stun for the rest of that Player Turn; once its counter shows Recall; +1 it must move.</summary>
    public static bool Recalling(bool recalled, bool stunRecovery) => recalled && !stunRecovery;

    /// <summary>D2.11: a turn is one hexspine either way.</summary>
    public static bool OneHexspine(int turned, int facing) => Math.Abs((turned - facing + 6) % 6) is 1 or 5;

    /// <summary>D2.1: the vehicle's MP after an expenditure, counted in halves.</summary>
    public static (int MfSpent, bool HalfMfSpent) Spend(int mfSpent, bool halfMfSpent, int halfMp)
    {
        var halves = (mfSpent * 2) + (halfMfSpent ? 1 : 0) + halfMp;
        return (halves / 2, halves % 2 == 1);
    }

    /// <summary>D8.3: a Bog Removal is decided by its colored dr, the first die; every other vehicle check by its DR.</summary>
    public static int CheckFinal(bool bogRemoval, int first, int second, int drm) => (bogRemoval ? first : first + second) + drm;

    /// <summary>
    /// D8.21, D2.5, D2.51, D8.3: a Bog or an Immobilization, or a Bog Removal that does not free the vehicle, ends its move once the DEFENDER passes.
    /// </summary>
    public static bool CheckStops(bool boggedOrImmobilized, bool bogRemoval, bool freed) => boggedOrImmobilized || (bogRemoval && !freed);

    /// <summary>D7.1: an OVR is resolved by its vehicle's fire record after the DEFENDER's window on its declaration closes.</summary>
    public static bool OverrunResolvable(bool vehicleMoveClosed, bool declaredHere, Func<bool> mover, Func<bool> fireRecorded) =>
        vehicleMoveClosed && declaredHere && mover() && fireRecorded();

    /// <summary>A11.6: a PAATC passes when its Final DR does not exceed the Morale.</summary>
    public static bool PaatcPassed(int first, int second, int drm, int morale) => first + second + drm <= morale;

    /// <summary>A2.6, D6.1: a Passenger or Rider leaves with its vehicle when it exits, and is lost with it otherwise.</summary>
    public static bool PassengerExits(bool containerExited) => containerExited;

    /// <summary>D10.1: only an active vehicle on the map becomes a wreck.</summary>
    public static bool WreckAllowed(bool vehicle, Func<bool> onMap) => vehicle && onMap();
}
