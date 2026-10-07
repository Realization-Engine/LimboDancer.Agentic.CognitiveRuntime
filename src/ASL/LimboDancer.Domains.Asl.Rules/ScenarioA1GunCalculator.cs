namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rule decisions of a Gun's handling (S7; rulings R8.5, R8.6, R8.9, R26.1, R26.2): a CA change without fire (C3.22, A12.141), and a vehicle
/// hooking up or unhooking a Gun (C10.1, C10.11, C10.12, C10.13). The caller reads the state and the map and writes the events.
/// </summary>
public static class ScenarioA1GunCalculator
{
    /// <summary>C3.22: the Gun's side's fire phase: the PFPh or AFPh for the phasing side, the DFPh for the other.</summary>
    public static bool FriendlyFirePhase(string? phase, bool crewPhasing) => phase is "pfph" or "afph" ? crewPhasing : phase == "dfph" && !crewPhasing;

    /// <summary>
    /// C3.22 (ruling R8.9): a Gun changes its CA while its Good Order, unpinned, non-TI crew could still fire it, to a new hexspine. A Gun that fired
    /// this phase may on a kept Multiple ROF; otherwise not when it carries a fire or malfunction marker, read lazily.
    /// </summary>
    public static bool TurnGunRefused(bool friendly, bool crewBroken, bool crewPinned, bool crewTi, bool gunTi, bool firedBefore, bool rateOfFireKept,
        Func<bool> marked, bool sameFacing) =>
        !friendly || crewBroken || crewPinned || crewTi || gunTi || (firedBefore ? !rateOfFireKept : marked()) || sameFacing;

    /// <summary>C3.22: in the PFPh neither the Gun nor its crew moves that Player Turn.</summary>
    public static string TurnGunNote(string? phase) => phase == "pfph" ? "; it and its crew do not move this Player Turn" : string.Empty;

    /// <summary>C10.11: a hook-up costs half the vehicle's MP, rounded up.</summary>
    public static int HookCost(int mp) => (mp + 1) / 2;

    /// <summary>C10.11: the MP a vehicle has spent, a half MP counted as one.</summary>
    public static int MpSpent(int mfSpent, bool halfMfSpent) => mfSpent + (halfMfSpent ? 1 : 0);

    /// <summary>C10.1: a vehicle tows a Gun whose M# its T# does not exceed.</summary>
    public static bool Towable(int towing, int manhandling) => towing <= manhandling;

    /// <summary>C10.13 (ruling R26.2): the Gun's ammunition takes four PP of the vehicle's capacity, eight at 100mm or more.</summary>
    public static int AmmunitionPp(int? caliber) => caliber >= 100 ? 8 : 4;

    /// <summary>C10.13, D6.1: the Passengers overload the room the ammunition leaves; an empty vehicle never does.</summary>
    public static bool Overloaded(int riders, int load, int room) => riders > 0 && load > room;
}
