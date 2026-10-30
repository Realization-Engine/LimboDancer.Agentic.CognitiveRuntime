namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Pushing a Gun (C10.3 and C10.31 p. 181, and the C10.3 chart; C10.2, C10.25, C10.26 p. 180; E3.61 p. 230; E3.733, E3.7332 p. 231; B24.8 p. 143; the
/// C10.3 claim's design, rulings RC10.1 to RC10.12): who may push a Gun into a hex, the Manhandling DRM, the result of the Final DR and what follows it,
/// the Labor a failure leaves, and whether the Gun may be hooked up after its push. Ruling R8.6's limit of pushes to Open Ground or grain stays in
/// <see cref="ScenarioA1MovementCalculator.EntryCost"/> and <see cref="ScenarioA1EntryCalculator.PushedExit"/> (RC10.8); this calculator decides C10.3's
/// own bars, and its DRM takes the hex's TEM, so it is right for any terrain.
/// </summary>
public static class ScenarioA1GunPushCalculator
{
    /// <summary>The cheapest doubled entry in half MF (RC10.4): Open Ground at 1 MF, doubled.</summary>
    private const int ClearFloorHalfMf = 4;

    /// <summary>The cheapest doubled entry in half MF in Mud (E3.64) or Deep Snow (E3.733): Open Ground at 1½ MF, doubled.</summary>
    private const int WeatherFloorHalfMf = 6;

    private static readonly PushOutcome Entered = new(true, true, true, false, true);
    private static readonly PushOutcome EnteredAndStopped = new(true, false, false, false, true);
    private static readonly PushOutcome Stayed = new(false, false, false, true, true);

    /// <summary>
    /// C10.3 (p. 181; C10.2, C10.25, C10.26 p. 180; RC10.1, RC10.5): null when the Gun may be pushed into the hex; otherwise the first bar, in this order:
    /// NM or RFNM; neither QSU nor limbered; unhooked this Player Turn; Bypass; Assault Movement; a Minimum Move; carrying PP; beneath Wire or Panji;
    /// into a deep stream or Bamboo but via TB; a push already ended this MPh (<see cref="PushOnRefusal"/>); and an Infantry that has pushed this Player
    /// Turn without the MF left to expend in the hex, or with its MF not known.
    /// </summary>
    public static string? PushRefusal(GunPushFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(facts.PushersHalfMfLeft);
        if (facts.Nm || facts.Rfnm)
        {
            return "play.move-push: a Gun with NM or RFNM is never pushed (C10.3, C10.25, C10.26)";
        }

        if (!facts.Qsu && !facts.Limbered)
        {
            return "play.move-push: a Gun that is not QSU is pushed only limbered (C10.3, C10.2)";
        }

        if (facts.UnhookedThisPlayerTurn)
        {
            return "play.move-push: a Gun is not pushed in the Player Turn it was unhooked (C10.3)";
        }

        var how = facts.Bypass ? "a Gun is never pushed using Bypass"
            : facts.Assault ? "pushing a Gun prevents Assault Movement"
            : facts.MinimumMove ? "a Gun is not pushed as a Minimum Move"
            : facts.CarryingPp ? "a unit carrying PP does not push a Gun"
            : facts.BeneathWireOrPanji ? "a Gun is not pushed beneath a Wire or Panji counter"
            : (facts.IntoDeepStream || facts.IntoBamboo) && !facts.ViaTb ? "a Gun is not pushed into a deep stream or Bamboo but via a TB"
            : null;
        if (how is not null)
        {
            return $"play.move-push: {how} (C10.3)";
        }

        if (PushOnRefusal(facts.LastResultThisMph) is { } ended)
        {
            return ended;
        }

        if (facts.PushersHalfMfLeft.Count == 0 || facts.PushersHalfMfLeft.Any(left => left is null))
        {
            return "play.move-push: the MF left of every Infantry that has pushed this Player Turn must be known (C10.3)";
        }

        return facts.PushersHalfMfLeft.Any(left => left < facts.EntryHalfMf)
            ? "play.move-push: every Infantry that has pushed this Player Turn needs the MF left to expend in the hex (C10.3)"
            : null;
    }

    /// <summary>
    /// C10.3 (p. 181; RC10.5): null when the Gun may be pushed on: no push yet this MPh, or the last one entered below the M#. After a result at the M#
    /// ("cannot be moved any farther during that MPh") or above it, no push this MPh, on the map or at its edge.
    /// </summary>
    public static string? PushOnRefusal(ManhandlingResult? lastResultThisMph) => lastResultThisMph switch
    {
        ManhandlingResult.EnterStop => "play.move-push: the last Manhandling DR equalled the M#, so the Gun and its pushers move no farther this MPh (C10.3)",
        ManhandlingResult.Stay => "play.move-push: the last Manhandling DR was above the M#, so the Gun and its pushers move no farther this MPh (C10.3)",
        _ => null,
    };

    /// <summary>
    /// C10.3's road term (p. 181; RC10.3): -2 for crossing a road hexside, whatever SMOKE is in the hex or which rate A4.132's mover chose, except into or
    /// from mud (unless the road is paved) or deep snow (unless it is plowed). Ground Snow keeps it.
    /// </summary>
    public static bool RoadDrmApplies(bool roadHexsideCrossed, bool mud, bool deepSnow, bool paved, bool plowed) =>
        roadHexsideCrossed && !(mud && !paved) && !(deepSnow && !plowed);

    /// <summary>C10.3 (p. 181; E3.61 p. 230; E3.7332 p. 231; RC10.2): +3 for pushing during Mud or Deep Snow, a paved or plowed road hex included.</summary>
    public static int WeatherDrm(bool mud, bool deepSnow) => mud || deepSnow ? 3 : 0;

    /// <summary>
    /// C10.3 (p. 181; RC10.6): -1 per additional pushing crew or HS, -2 per additional squad, -1 if the only pushing MMC is a squad, at most -4. A
    /// negative count reads as none.
    /// </summary>
    public static int ExtraPushersDrm(int extraCrewsOrHalfSquads, int extraSquads, bool onlyPusherIsSquad) =>
        -Math.Min(4, Math.Clamp(extraCrewsOrHalfSquads, 0, 4) + (2 * Math.Clamp(extraSquads, 0, 4)) + (onlyPusherIsSquad ? 1 : 0));

    /// <summary>B24.8 (p. 143; C10.3): the Labor DRM, -1 or -2; a status above 2 reads as 2, one below 0 as none.</summary>
    public static int LaborDrm(int labor) => -Math.Clamp(labor, 0, 2);

    /// <summary>
    /// The Manhandling DRM (C10.3 p. 181 and its chart; RC10.2, RC10.3, RC10.6, RC10.10), cumulative: +X the positive TEM of the hexside crossed and of the
    /// hex entered; +Y the MF expended for the hex; +3 in Mud or Deep Snow; -2 across a road hexside (<see cref="RoadDrmApplies"/>); the extra pushers;
    /// -2 a Low Ammo counter; the Labor status. The C10.3 EX: a 37L AT into a stone building from Open Ground, +3 TEM and +4 MF, is +7.
    /// </summary>
    public static int ManhandlingDrm(ManhandlingDrmFacts facts) =>
        facts.MfSpent + Math.Max(0, facts.HexTem) + Math.Max(0, facts.HexsideTem) + WeatherDrm(facts.Mud, facts.DeepSnow)
        - (RoadDrmApplies(facts.RoadHexsideCrossed, facts.Mud, facts.DeepSnow, facts.PavedRoad, facts.PlowedRoad) ? 2 : 0)
        + ExtraPushersDrm(facts.ExtraCrewsOrHalfSquads, facts.ExtraSquads, facts.OnlyPusherIsSquad) - (facts.LowAmmoPlaced ? 2 : 0) + LaborDrm(facts.Labor);

    /// <summary>C10.3 (p. 181): a pushed Gun's entry costs double its normal MF, here in half MF.</summary>
    public static int PushHalfMf(int halfMf) => halfMf * 2;

    /// <summary>C10.3 (p. 181): the result of a Final Manhandling DR against the M#, as <see cref="ScenarioA1ResultTables.Manhandling"/> gives it.</summary>
    public static ManhandlingResult Result(int finalDr, int mNumber) => ScenarioA1ResultTables.Manhandling(finalDr, mNumber) switch
    {
        ScenarioA1ResultTables.ManhandlingEnter => ManhandlingResult.Enter,
        ScenarioA1ResultTables.ManhandlingEnterStop => ManhandlingResult.EnterStop,
        _ => ManhandlingResult.Stay,
    };

    /// <summary>A result as the <c>manhandling-rolled</c> record names it (<c>enter</c>, <c>enter-stop</c>, <c>stay</c>), or null for none or another word.</summary>
    public static ManhandlingResult? ResultOf(string? recorded) => recorded switch
    {
        ScenarioA1ResultTables.ManhandlingEnter => ManhandlingResult.Enter,
        ScenarioA1ResultTables.ManhandlingEnterStop => ManhandlingResult.EnterStop,
        ScenarioA1ResultTables.ManhandlingStay => ManhandlingResult.Stay,
        _ => null,
    };

    /// <summary>
    /// What follows a result (C10.3 p. 181; C10.31): below the M# the Gun enters and may be pushed on or hooked up; at it the Gun and its pushers enter
    /// and move no farther; above it neither moves and the pushers gain Labor. In all three the Gun and its pushers become TI once their pushing ends.
    /// </summary>
    public static PushOutcome Follows(ManhandlingResult result) => result switch
    {
        ManhandlingResult.Enter => Entered,
        ManhandlingResult.EnterStop => EnteredAndStopped,
        _ => Stayed,
    };

    /// <summary>
    /// B24.8 (p. 143; C10.3; RC10.9): the pushers' Labor status after a result: a failure (above the M#) raises it by one to at most 2; an entry achieves
    /// the task and clears it. A status below 0 reads as none.
    /// </summary>
    public static int LaborAfter(int labor, ManhandlingResult result) => result == ManhandlingResult.Stay ? Math.Min(2, Math.Max(0, labor) + 1) : 0;

    /// <summary>
    /// C10.31 (p. 181; E3.64 p. 230; E3.733 p. 231; RC10.4): the least MF, in half MF, a pusher needs left to attempt another Manhandling DR: the cheapest
    /// doubled entry, Open Ground at 1 MF (2 MF, 4 half MF) in Clear or Ground Snow; at 1½ MF (3 MF) in Mud or Deep Snow; at 1 MF there when a paved
    /// (Mud) or plowed (Deep Snow) road hexside leaves the Gun's hex.
    /// </summary>
    public static int PushFloorHalfMf(bool mud, bool deepSnow, bool roadExit) => (mud || deepSnow) && !roadExit ? WeatherFloorHalfMf : ClearFloorHalfMf;

    /// <summary>
    /// C10.31 (p. 181; C10.11 p. 180; RC10.4, RC10.7): null when the Gun may be hooked up: it was not pushed this MPh (C10.31 says nothing then), or its
    /// last push entered below the M#, every Infantry that has pushed this Player Turn has at least <see cref="PushFloorHalfMf"/> left (an unknown MF
    /// refuses), and the vehicle has not exceeded the MP it needs to hook up. C10.11's own push sentence is that row's, not this one's.
    /// </summary>
    public static string? HookAfterPushRefusal(HookAfterPushFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(facts.PushersHalfMfLeft);
        if (!facts.PushedThisMph)
        {
            return null;
        }

        if (facts.LastResult != ManhandlingResult.Enter)
        {
            return "play.hook-gun: a Gun is hooked up after its push only when its last Manhandling DR was below the M#, so its pushers could push it on (C10.31, C10.3)";
        }

        var floor = PushFloorHalfMf(facts.Mud, facts.DeepSnow, facts.RoadExit);
        if (facts.PushersHalfMfLeft.Count == 0 || facts.PushersHalfMfLeft.Any(left => left is null || left < floor))
        {
            return "play.hook-gun: a Gun is hooked up after its push only when every Infantry that has pushed this Player Turn could still attempt another "
                + "Manhandling DR, with the MF left for the cheapest push (C10.31, C10.3)";
        }

        return facts.VehicleHasHookUpMp ? null
            : "play.hook-gun: the vehicle has exceeded the MP it may expend before hooking up the Gun (C10.31, C10.11)";
    }
}
