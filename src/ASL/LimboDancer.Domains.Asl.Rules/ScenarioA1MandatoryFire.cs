namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// One unit of the DEFENDER in the Location a moving stack has just entered (A8.312). <paramref name="HasInherentFp"/> is the catalog's;
/// <paramref name="UsableWeapons"/> counts the MG it possesses that may fire; <paramref name="Marked"/> is a First or Final Fire counter;
/// <paramref name="AttackedThisEntry"/> is whether it has fired at that Location since the step.
/// </summary>
public sealed record MandatoryDefenderFacts(string Id, bool Infantry, bool Smc, bool HasInherentFp, int UsableWeapons, bool Unarmed, bool Broken, bool Melee, bool Captured,
    bool Marked, bool AttackedThisEntry);

/// <summary>
/// The attack the DEFENDER owes in its own Location (A8.312, p. 61; pass 40, task 40.5): "An armed, unbroken Infantry DEFENDER not in Melee must
/// ... immediately attack any Infantry/Cavalry MMC unit that enters its Location during the MPh whether it uses Defensive First Fire, Subsequent
/// First Fire, or FPF", all of it "combined into one Mandatory FG". The game fires nothing on its own: while the attack is owed the DEFENDER's
/// pass is refused, and its fire is the Mandatory FG's.
/// </summary>
public static class ScenarioA1MandatoryFire
{
    /// <summary>
    /// The units that still owe the attack, in the order given. None outside the MPh, with the DEFENDER's window closed, on a step that did not
    /// change the stack's Location, or when no Infantry or Cavalry MMC of the stack stands there. "[EXC: A SMC with a MG/IFE already marked with a
    /// First Fire counter may not use Subsequent First Fire or FPF because it cannot use Sustained Fire; 9.3]". A unit the fire rules give no attack
    /// now is not held to one: <paramref name="mayAttack"/> is asked last, and only of a unit that would otherwise owe the attack.
    /// </summary>
    public static IReadOnlyList<string> Owed(bool mph, bool windowOpen, bool enteredThisStep, bool mmcEntered, IReadOnlyList<MandatoryDefenderFacts> defenders,
        Func<string, bool> mayAttack)
    {
        ArgumentNullException.ThrowIfNull(defenders);
        ArgumentNullException.ThrowIfNull(mayAttack);
        if (!mph || !windowOpen || !enteredThisStep || !mmcEntered)
        {
            return [];
        }

        return [.. defenders.Where(unit => unit.Infantry && !unit.Unarmed && !unit.Broken && !unit.Melee && !unit.Captured && (unit.HasInherentFp || unit.UsableWeapons > 0)
            && !(unit.Smc && unit.Marked && unit.UsableWeapons > 0) && !unit.AttackedThisEntry && mayAttack(unit.Id)).Select(unit => unit.Id)];
    }

    /// <summary>
    /// How many of its usable MG a unit fires in the plainest attack it owes: every one its size operates when it is already marked, since
    /// Subsequent First Fire and FPF fire "all usable MG/IFE possessed by that unit" (A8.3, A8.31); one when it has no FP of its own; else none.
    /// </summary>
    public static int WeaponsFired(bool squad, bool hasInherentFp, bool marked, int usableWeapons) =>
        marked ? Math.Min(usableWeapons, squad ? 2 : 1) : hasInherentFp ? 0 : Math.Min(usableWeapons, 1);

    /// <summary>Why the DEFENDER may not pass while the attack is owed; null when nothing is owed.</summary>
    public static string? PassBar(IReadOnlyList<string> owed, bool groupPossible, string location)
    {
        ArgumentNullException.ThrowIfNull(owed);
        return owed.Count == 0 ? null
            : groupPossible ? $"play.pass-mandatory: {string.Join(", ", owed)} must first attack the units that entered {location}, as one Mandatory FG (A8.312)"
            : $"play.pass-mandatory: {string.Join(", ", owed)} owe an attack on the units that entered {location}; fire those of them that form one group, and the others are released (A8.312; ruling R40.17)";
    }

    /// <summary>
    /// Why an attack by the DEFENDER is refused while the Mandatory FG's is owed; null when it is that attack. It is at the Location entered and
    /// by every unit that owes it, when the fire rules let them fire as one group (<paramref name="groupPossible"/>). When they do not (a unit with
    /// no counter beside one marked First Fire; ruling R40.17), it is by units that owe it and no one else, and by every such unit that could
    /// have joined those named (<paramref name="couldJoin"/>).
    /// </summary>
    public static string? FireBar(IReadOnlyList<string> owed, bool groupPossible, IReadOnlyList<string> firers, string target, string location, IReadOnlyList<string> couldJoin)
    {
        ArgumentNullException.ThrowIfNull(owed);
        ArgumentNullException.ThrowIfNull(firers);
        ArgumentNullException.ThrowIfNull(couldJoin);
        if (owed.Count == 0)
        {
            return null;
        }

        if (!string.Equals(target, location, StringComparison.Ordinal))
        {
            return $"play.fire-mandatory: {string.Join(", ", owed)} must first attack the units that entered {location} (A8.312)";
        }

        var missing = owed.Where(id => !firers.Contains(id, StringComparer.Ordinal)).ToArray();
        if (groupPossible)
        {
            return missing.Length > 0
                ? $"play.fire-mandatory: the attack on the units that entered {location} is one Mandatory FG, which {string.Join(", ", missing)} must join (A8.312, A7.55)"
                : null;
        }

        if (missing.Length == owed.Count || firers.Any(id => !owed.Contains(id, StringComparer.Ordinal)))
        {
            return $"play.fire-mandatory: the units that owe the attack on those that entered {location} ({string.Join(", ", owed)}) make it first, with no one else (A8.312; ruling R40.17)";
        }

        return couldJoin.Count > 0
            ? $"play.fire-mandatory: {string.Join(", ", couldJoin)} can fire in one group with the units named, and must join them (A8.312, A7.55; ruling R40.17)"
            : null;
    }

    /// <summary>
    /// Why another act of the DEFENDER waits while the attack is owed (A8.312, p. 61; pass 45, ruling R45.2): the DEFENDER "must ... then immediately
    /// attack", so a Gun's shot, a thrown DC, and a Starshell come after it.
    /// </summary>
    public static string OtherActBar(IReadOnlyList<string> owed, string location, string act)
    {
        ArgumentNullException.ThrowIfNull(owed);
        return $"play.act-mandatory: {string.Join(", ", owed)} must first attack the units that entered {location}; the {act} waits for that attack (A8.312)";
    }

    /// <summary>The page's note for the DEFENDER; where the fire rules form no one group of the units, it says so and cites the ruling.</summary>
    public static string Note(IReadOnlyList<string> owed, bool groupPossible, string place)
    {
        ArgumentNullException.ThrowIfNull(owed);
        return groupPossible
            ? $"Enemy Infantry has entered {place}: {string.Join(", ", owed)} must attack it now as one Mandatory FG, before a pass (A8.312)."
            : $"Enemy Infantry has entered {place}. By A8.312 {string.Join(", ", owed)} owe one Mandatory FG; the game forms no FG of units whose fire counters differ (ruling R40.17), "
                + "so fire those of them that form one group, and the others are released.";
    }

    /// <summary>The hand-over prompt's sentence for the DEFENDER while the attack is owed.</summary>
    public static string HandOverText(string place) => $"Your units in {place} must attack the units that entered it before you pass (A8.312).";

    /// <summary>
    /// Whether a crew that fired its Gun this Player Turn still adds its inherent FP (A8.311, p. 61): "Infantry manning ordnance cannot fire it during
    /// FPF ... but must add their inherent FP to the FPF attack"; and A7.353 (p. 56) keeps it in Subsequent First Fire and in Final Fire against an
    /// adjacent unit (<paramref name="areaFireAgain"/>). In any other fire it has none (A7.352; ruling R8.4).
    /// </summary>
    public static bool CrewAddsInherentFp(bool gunFired, bool areaFireAgain) => !gunFired || areaFireAgain;
}
