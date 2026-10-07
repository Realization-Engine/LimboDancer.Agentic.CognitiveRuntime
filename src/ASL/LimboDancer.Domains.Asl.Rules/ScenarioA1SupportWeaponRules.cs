namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rule decisions of squads and support weapons outside fire (S7; rulings R13.4 to R13.6, R31d.1): Deployment and Recombining (A1.31, A1.32),
/// dismantling (A9.8), Recovery (A4.44), and a broken unit's rout load (A4.42, A10.4). The caller reads the state and the map and writes the events.
/// </summary>
public static class ScenarioA1SupportWeaponRules
{
    /// <summary>A9.8 (ruling R13.6): a MG is dismantled or assembled in its side's PFPh or DFPh.</summary>
    public static bool DismantlePhase(string? phase, bool phasing) => (phase == "pfph" && phasing) || (phase == "dfph" && !phasing);

    /// <summary>A1.31, A10.7: the directing leader's modifier, one worse when wounded (A17.3).</summary>
    public static int LeaderDrm(int? leadership, bool wounded) => (leadership ?? 0) + (wounded ? 1 : 0);

    /// <summary>A1.32: the squad is Fanatic only when both HS are.</summary>
    public static bool RecombinedFanatic(bool one, bool two) => one && two;

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
