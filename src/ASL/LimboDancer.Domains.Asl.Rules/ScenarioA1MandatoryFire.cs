namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// One unit of the DEFENDER in the Location a moving stack has just entered (A8.312). <paramref name="HasInherentFp"/> is the catalog's;
/// <paramref name="UsableWeapons"/> counts the MG it possesses that are not malfunctioned; <paramref name="Marked"/> is a First or Final Fire counter;
/// <paramref name="AttackedThisEntry"/> is whether it has fired at that Location since the step; <paramref name="MayAttack"/> is whether an attack by
/// it alone at that Location is one the fire rules allow now.
/// </summary>
public sealed record MandatoryDefenderFacts(string Id, bool Infantry, bool Smc, bool HasInherentFp, int UsableWeapons, bool Unarmed, bool Broken, bool Melee, bool Captured,
    bool Marked, bool AttackedThisEntry, bool MayAttack);

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
    /// now is not held to one.
    /// </summary>
    public static IReadOnlyList<string> Owed(bool mph, bool windowOpen, bool enteredThisStep, bool mmcEntered, IReadOnlyList<MandatoryDefenderFacts> defenders)
    {
        ArgumentNullException.ThrowIfNull(defenders);
        if (!mph || !windowOpen || !enteredThisStep || !mmcEntered)
        {
            return [];
        }

        return [.. defenders.Where(unit => unit.Infantry && !unit.Unarmed && !unit.Broken && !unit.Melee && !unit.Captured && (unit.HasInherentFp || unit.UsableWeapons > 0)
            && !(unit.Smc && unit.Marked && unit.UsableWeapons > 0) && !unit.AttackedThisEntry && unit.MayAttack).Select(unit => unit.Id)];
    }

    /// <summary>Why the DEFENDER may not pass while the attack is owed; null when nothing is owed.</summary>
    public static string? PassBar(IReadOnlyList<string> owed, string location)
    {
        ArgumentNullException.ThrowIfNull(owed);
        return owed.Count == 0 ? null
            : $"play.pass-mandatory: {string.Join(", ", owed)} must first attack the units that entered {location}, as one Mandatory FG (A8.312)";
    }

    /// <summary>
    /// Why an attack by the DEFENDER is refused while the Mandatory FG's is owed; null when it is that attack. It is at the Location entered and
    /// by every unit that owes it, when the fire rules let them fire as one group (<paramref name="groupPossible"/>); when they do not (a unit with no
    /// counter beside one marked First Fire), by at least one of them and by no one else.
    /// </summary>
    public static string? FireBar(IReadOnlyList<string> owed, bool groupPossible, IReadOnlyList<string> firers, string target, string location)
    {
        ArgumentNullException.ThrowIfNull(owed);
        ArgumentNullException.ThrowIfNull(firers);
        if (owed.Count == 0)
        {
            return null;
        }

        if (!string.Equals(target, location, StringComparison.Ordinal))
        {
            return $"play.fire-mandatory: {string.Join(", ", owed)} must first attack the units that entered {location} (A8.312)";
        }

        var missing = owed.Where(id => !firers.Contains(id, StringComparer.Ordinal)).ToArray();
        return groupPossible && missing.Length > 0
            ? $"play.fire-mandatory: the attack on the units that entered {location} is one Mandatory FG, which {string.Join(", ", missing)} must join (A8.312, A7.55)"
            : !groupPossible && (missing.Length == owed.Count || firers.Any(id => !owed.Contains(id, StringComparer.Ordinal)))
                ? $"play.fire-mandatory: one of {string.Join(", ", owed)} must first attack the units that entered {location}, alone or with others that owe the attack (A8.312)"
                : null;
    }

    /// <summary>The page's note for the DEFENDER; the units fire each alone when their counters keep them out of one group.</summary>
    public static string Note(IReadOnlyList<string> owed, bool groupPossible, string place)
    {
        ArgumentNullException.ThrowIfNull(owed);
        return groupPossible
            ? $"Enemy Infantry has entered {place}: {string.Join(", ", owed)} must attack it now as one Mandatory FG, before a pass (A8.312)."
            : $"Enemy Infantry has entered {place}: one of {string.Join(", ", owed)} must attack it now, before a pass; their fire counters differ, and a Location fires once at a MF expenditure (A8.312, A7.55).";
    }

    /// <summary>
    /// Whether a crew that fired its Gun this Player Turn still adds its inherent FP (A8.311, p. 61): "Infantry manning ordnance cannot fire it during
    /// FPF ... but must add their inherent FP to the FPF attack". Outside FPF it has none (A7.352; ruling R8.4).
    /// </summary>
    public static bool CrewAddsInherentFp(bool gunFired, bool finalProtectiveFire) => !gunFired || finalProtectiveFire;
}
