namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The facts of one counter a side's view decides on (pass 32.a, slice S11, from Units' <c>GameView.Of</c>): whether it is the enemy's, hidden,
/// concealed, a Dummy, in an OB group setting up out of sight, a unit, the top counter of its stack, or equipment lying beneath an enemy stack.
/// </summary>
public sealed record CounterFacts(bool Enemy, bool Hidden, bool Concealed, bool Dummy, bool OutOfSight, bool IsUnit, bool IsTop, bool EquipmentBeneathStack);

/// <summary>How a side's view shows a counter it does not withhold.</summary>
public enum CounterPresence
{
    /// <summary>The counter itself.</summary>
    Shown,

    /// <summary>A "?" at its Location: a concealed enemy unit (A12.11).</summary>
    Sealed,

    /// <summary>A counted but uninspected counter beneath the top of an enemy stack before play (A2.9; ruling R23.3).</summary>
    SealedUninspected,
}

/// <summary>
/// What a side may know of the counters on the map (A12.11, A12.3, A12.12, A2.9, D10.1; rulings R23.1, R23.3, R6.5). The view leaves out what the
/// side cannot know: an enemy's hidden instances and everything inside or held by them; an enemy's concealed units become sealed presences, and what
/// they hold is absent. Before play starts, an enemy stack not under "?" shows its top counter and counts the rest, and a side setting up now is out
/// of sight entirely. Units reads the state and applies each verdict.
/// </summary>
public static class ScenarioA1Visibility
{
    /// <summary>A wreck stays on the map for every perspective (D10.1; ruling R6.5): the instances a view may show are the active ones and the wrecks.</summary>
    public static bool OnMapForEveryone(bool active, bool wrecked) => active || wrecked;

    /// <summary>A Dummy is a "?" with nothing beneath it (A12.11), so it is always concealed to the enemy, whatever its condition says (referee, pass 23).</summary>
    public static bool ConcealedToEnemy(bool concealed, bool dummy) => concealed || dummy;

    /// <summary>
    /// Ruling R23.3 (A2.9): before play starts, an enemy stack not under "?" shows only its top counter: the first counter of the stack that is not
    /// concealed to the enemy, or none.
    /// </summary>
    public static string? TopCounter(IEnumerable<(string Id, bool ConcealedToEnemy)> stack)
    {
        ArgumentNullException.ThrowIfNull(stack);
        return stack.Where(counter => !counter.ConcealedToEnemy).Select(counter => counter.Id).FirstOrDefault();
    }

    /// <summary>
    /// Whether an instance is withheld from the view: it, or anything it is inside or held by, is an enemy's hidden or concealed instance, belongs to
    /// a side setting up out of sight (ruling R23.3), or lies beneath the top of an enemy stack before play (A2.9). <paramref name="chain"/> is the
    /// instance first, then each holder or container outward. A concealed instance, or one beneath a top, is not withheld as the instance itself
    /// when <paramref name="sealAllowed"/>: it becomes a sealed presence instead.
    /// </summary>
    public static bool Withheld(IReadOnlyList<CounterFacts> chain, bool beforePlay, bool sealAllowed)
    {
        ArgumentNullException.ThrowIfNull(chain);
        for (var index = 0; index < chain.Count; index++)
        {
            var current = chain[index];
            var self = index == 0;
            if (current.Enemy && (current.Hidden || (ConcealedToEnemy(current.Concealed, current.Dummy) && !(sealAllowed && self))
                || current.OutOfSight
                || (beforePlay && current.IsUnit && !current.IsTop && !(sealAllowed && self))
                || (beforePlay && current.EquipmentBeneathStack)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>How a unit the view does not withhold is shown: a concealed enemy unit as a sealed presence, one beneath a stack's top before play as an uninspected one, else itself.</summary>
    public static CounterPresence UnitPresence(CounterFacts unit, bool beforePlay)
    {
        ArgumentNullException.ThrowIfNull(unit);
        var concealed = unit.Enemy && ConcealedToEnemy(unit.Concealed, unit.Dummy);
        return concealed ? CounterPresence.Sealed
            : beforePlay && unit.Enemy && !unit.IsTop ? CounterPresence.SealedUninspected
            : CounterPresence.Shown;
    }

    /// <summary>
    /// Referee, pass 23: a setup event names what it creates, so a side reads only the creations of its own instances, of the instances it may see now,
    /// and of instances no longer in play.
    /// </summary>
    public static bool CreationShown(bool ownSide, bool shownNow, bool stillInPlay) => ownSide || shownNow || !stillInPlay;
}
