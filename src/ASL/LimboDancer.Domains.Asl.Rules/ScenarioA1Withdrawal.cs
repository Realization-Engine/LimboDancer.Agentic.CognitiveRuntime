namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A counter in the Location a unit withdraws into, as the withdrawal reads it (A11.21; pass 70, ruling R70.26): its id, side, whether it is a prisoner,
/// concealed, hidden, a Dummy, and whether it is equipment (a Gun set up hidden with its crew).
/// </summary>
public sealed record WithdrawalLocationFacts(string Id, string? Side, bool Captured, bool Concealed, bool Hidden, bool Dummy, bool Equipment);

/// <summary>
/// What a withdrawal into a Location meets there (A11.21, p. 73, and its example, p. 74; ruling R70.26): whether the withdrawing units are eliminated; the
/// concealed enemy units among which a Random Selection decides who loses "?"; the hidden units and Guns placed on board unconcealed; and the Dummies removed
/// from a Location of Dummies only.
/// </summary>
public sealed record WithdrawalEntryVerdict(bool Eliminated, IReadOnlyList<string> Selection, IReadOnlyList<string> Placed, IReadOnlyList<string> DummiesRemoved)
{
    /// <summary>Whether the withdrawal changes anything there.</summary>
    public bool Any => Eliminated || DummiesRemoved.Count > 0;

    /// <summary>Nothing there but friends, prisoners, or no one.</summary>
    public static WithdrawalEntryVerdict None { get; } = new(false, [], [], []);
}

/// <summary>
/// Withdrawal completed (pass 70, task 70.6; ruling R70.26): A11.21's destination barred only by a Known enemy unit (<see
/// cref="ScenarioA1CloseCombatRules.WithdrawalDestination"/>), what a withdrawal into a concealed or hidden enemy's Location meets, and the SW a unit above
/// its IPC drops to withdraw. Play reads the Locations and the portage and writes the events; the reveals are public, as every reveal is.
/// </summary>
public static class ScenarioA1Withdrawal
{
    /// <summary>The purpose of the Random Selection dr among the concealed enemy units of the Location withdrawn into.</summary>
    public const string SelectionPurpose = "random-selection";

    /// <summary>
    /// A11.21 (p. 73): "If a unit withdraws into a concealed enemy's (not Dummy) Location it is eliminated automatically but an enemy unit(s) in that
    /// Location (determined by Random Selection) must forfeit its concealment." The p. 74 example withdraws into a HIP AT Gun and crew: "The Gun and crew
    /// are placed on board unconcealed (11.21) and the 2-4-7 is eliminated", and of a Dummy stack "it would be removed now". So: a real concealed or a hidden
    /// enemy unit there eliminates the withdrawers, the Random Selection is among the real concealed units, and every hidden unit and every Gun under a
    /// "?" or hidden is placed unconcealed (a Gun is no unit to select, and its "?" is its crew's); a Location of enemy Dummies only loses them and is
    /// entered. Dummies beside real units are not named by the rule and stay.
    /// </summary>
    public static WithdrawalEntryVerdict Entry(IReadOnlyList<WithdrawalLocationFacts> there, string withdrawingSide)
    {
        ArgumentNullException.ThrowIfNull(there);
        WithdrawalLocationFacts[] enemies = [.. there.Where(item => item.Side != withdrawingSide && !item.Captured)];
        string[] concealed = [.. enemies.Where(item => !item.Equipment && !item.Dummy && !item.Hidden && item.Concealed).Select(item => item.Id)];
        string[] hidden = [.. enemies.Where(item => !item.Dummy && (item.Hidden || (item.Equipment && item.Concealed))).Select(item => item.Id)];
        if (concealed.Length > 0 || enemies.Any(item => !item.Equipment && !item.Dummy && item.Hidden))
        {
            return new WithdrawalEntryVerdict(true, concealed, hidden, []);
        }

        string[] dummies = [.. enemies.Where(item => item.Dummy && !item.Equipment).Select(item => item.Id)];
        return dummies.Length > 0 ? new WithdrawalEntryVerdict(false, [], [], dummies) : WithdrawalEntryVerdict.None;
    }

    /// <summary>Whether the Random Selection needs a dr: more than one concealed unit to choose among.</summary>
    public static bool SelectionRolled(int candidates) => candidates > 1;

    /// <summary>
    /// The concealed units that lose "?" (A11.21; A.9's Random Selection): the one there, or among several the highest dr, ties all, as the other Random
    /// Selections of the engine read it. <paramref name="drs"/> gives one dr per candidate, in order; null when none was rolled.
    /// </summary>
    public static IReadOnlyList<string> Revealed(IReadOnlyList<string> candidates, IReadOnlyList<int>? drs)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return !SelectionRolled(candidates.Count) ? candidates
            : drs is { } rolled && rolled.Count == candidates.Count ? ScenarioA1MoppingUp.Struck(candidates, rolled)
            : throw new ArgumentException("A Random Selection among several units needs one dr for each.", nameof(drs));
    }

    /// <summary>
    /// A11.21, A4.43 (ruling R70.26): whether the SW named to drop are the unit's own: each one dropped is possessed by a unit that withdraws (or
    /// infiltrates). Null when they are; else the first that is not, for the refusal.
    /// </summary>
    public static string? ForeignDrop(IReadOnlyList<string> dropped, Func<string, bool> heldByWithdrawer)
    {
        ArgumentNullException.ThrowIfNull(dropped);
        ArgumentNullException.ThrowIfNull(heldByWithdrawer);
        return dropped.FirstOrDefault(weapon => !heldByWithdrawer(weapon));
    }

    /// <summary>
    /// A11.21 (ruling R70.26): the plan's words for a withdrawal that drops SW: they stay unpossessed in the Location it leaves, and are dropped only when
    /// it withdraws.
    /// </summary>
    public static string DropsText(IReadOnlyList<string> dropped)
    {
        ArgumentNullException.ThrowIfNull(dropped);
        return $"play.cc-withdrawal-drop: {string.Join(", ", dropped)} {(dropped.Count == 1 ? "is" : "are")} dropped, unpossessed in the Location, if {(dropped.Count == 1 ? "its unit withdraws" : "their units withdraw")} (A11.21, A4.43; ruling R70.26)";
    }
}
