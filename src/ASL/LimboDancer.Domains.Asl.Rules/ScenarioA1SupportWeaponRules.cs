namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rule decisions of squads and support weapons outside fire (S7; rulings R13.4 to R13.6, R31d.1): Deployment and Recombining (A1.31, A1.32),
/// dismantling (A9.8), Recovery (A4.44), and a broken unit's rout load (A4.42, A10.4). The caller reads the state and the map and writes the events.
/// </summary>
public static class ScenarioA1SupportWeaponRules
{
    /// <summary>A9.8 (ruling R13.6): a MG is dismantled or assembled in its side's PFPh or DFPh.</summary>
    public static bool DismantlePhase(string? phase, bool phasing) => (phase == "pfph" && phasing) || (phase == "dfph" && !phasing);

    /// <summary>A4.431: a SW passes between units in the RPh, or in the ATTACKER's APh before either advances.</summary>
    public static bool TransferPhase(string? phase, bool phasing) => phase == "rph" || (phase == "aph" && phasing);

    /// <summary>
    /// A4.43 (ruling R13.5): an unbroken unit drops a SW in its own MPh during its move, its own APh before it advances, or at the start of the CCPh.
    /// A broken unit's drop before it routs (A10.4) is the rout's own choice of what it keeps.
    /// </summary>
    public static bool DropPhase(string? phase, bool phasing) => phase == "ccph" || (phase is "mph" or "aph" && phasing);

    /// <summary>A4.44 (ruling R13.5): a SW is Recovered in the RPh, as the unit's sole action, or during the unit's own move in its MPh.</summary>
    public static bool RecoverPhase(string? phase, bool phasing) => phase == "rph" || (phase == "mph" && phasing);

    /// <summary>A1.31, A10.7: the directing leader's modifier, one worse when wounded (A17.3).</summary>
    public static int LeaderDrm(int? leadership, bool wounded) => (leadership ?? 0) + (wounded ? 1 : 0);

    /// <summary>
    /// A1.31, A1.32 (p. 45; pass 40): "Guards ... [and] unarmed units (20.5) ... do not require leader direction to Deploy", nor "leader presence to
    /// Recombine". The Guards are the Guards of prisoners (ruling R31.4). Pass 70 (ruling R70.28): A1.32 names "Carrier HS" too, and D6.82 (p. 206) lets
    /// them "recombine after Dismounting" with no TC and no leader (<see cref="ScenarioA1CarrierRules.CarrierHalfSquad"/>). Finns and U.S.M.C. 7-6-8s are
    /// not in the catalog.
    /// </summary>
    public static bool NeedsNoLeader(bool guard, bool unarmed, bool carrierHalfSquad = false) => guard || unarmed || carrierHalfSquad;

    /// <summary>
    /// A20.5 (p. 88; pass 40): "An unbroken Guard squad can Deploy into HS automatically at any time, regardless of nationality or leader
    /// requirements": in any phase, with no NTC.
    /// </summary>
    public static bool GuardDeploysAtWill(bool squad, bool guard, bool broken) => squad && guard && !broken;

    /// <summary>
    /// A1.32 (p. 45; pass 40): HS Recombine "at the start of any RPh", so not once a unit of their side has attempted a Rally, a Repair, a
    /// Deployment, a Recovery, or a Transfer in it. Null when they may.
    /// </summary>
    public static string? RecombineStartBar(bool sideHasActed) => sideHasActed
        ? "play.recombine-start: HS Recombine at the start of the RPh, before any other RPh action of their side (A1.32)" : null;

    /// <summary>A1.32: the squad is Fanatic only when both HS are.</summary>
    public static bool RecombinedFanatic(bool one, bool two) => one && two;

    /// <summary>A4.44 (p. 50; pass 40): "a Recovery attempt is allowed only by an unpinned, Good Order non-Bypassing unit".</summary>
    public static string? RecoveryBypassBar(string unitId, bool bypassing) =>
        bypassing ? $"play.recover-bypass: {unitId} is in Bypass, and a Bypassing unit Recovers nothing (A4.44)" : null;

    /// <summary>A4.44: a Recovery in the MPh costs one MF, refused when it would pass the unit's allotment.</summary>
    public static bool RecoveryMfShort(int allotment, int mfSpent, bool halfMfSpent) => (2 * mfSpent) + (halfMfSpent ? 1 : 0) + 2 > 2 * allotment;

    /// <summary>A4.44, E1.56 (ruling R16.7): +1 CX, +1 at night.</summary>
    public static int RecoveryDrm(bool cx, bool night) => (cx ? 1 : 0) + (night ? 1 : 0);

    /// <summary>A4.42, A4.51: a broken unit's IPC: a SMC's 1, none when wounded; otherwise 3.</summary>
    public static int RoutIpc(bool smc, bool wounded) => smc ? (wounded ? 0 : 1) : 3;

    /// <summary>
    /// A10.4 (ruling R31d.1): the best loads, as index sets in mask order, of the SW with the most PP not over the IPC.
    /// </summary>
    public static IReadOnlyList<int[]> BestLoads(IReadOnlyList<int> pp, int ipc)
    {
        ArgumentNullException.ThrowIfNull(pp);
        var loads = Enumerable.Range(0, 1 << pp.Count)
            .Select(mask => Enumerable.Range(0, pp.Count).Where(index => (mask & (1 << index)) != 0).ToArray())
            .Where(load => load.Sum(index => pp[index]) <= ipc).ToArray();
        var best = loads.Max(load => load.Sum(index => pp[index]));
        return [.. loads.Where(load => load.Sum(index => pp[index]) == best)];
    }
}
