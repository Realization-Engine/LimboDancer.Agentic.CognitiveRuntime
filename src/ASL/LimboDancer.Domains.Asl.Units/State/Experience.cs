using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// Inexperienced Personnel and the MF allowance it sets (Occupied and Concealed Entry Design, section 5), derived and
/// never stored (ASL-UNIT-023). Green and Conscript MMC are Inexperienced (A19.2, p. 86); a Green MMC stacked with an
/// unbroken leader is exempt, a Conscript never is (A19.3, p. 86). A Replaced unit (A19.13, p. 86) takes its new
/// definition's class through lineage, so the class is always the current definition's.
/// </summary>
public static class Experience
{
    public const string ClassAttribute = "asl:class";

    private static readonly HashSet<string> InexperiencedClasses = new(StringComparer.Ordinal) { "green", "conscript" };

    /// <summary>
    /// Whether an MMC is Inexperienced: unknown when its definition, its class, or a stacked leader's condition cannot
    /// establish it; inapplicable to units that are not MMC.
    /// </summary>
    public static ConditionState Inexperienced(GameState state, UnitInstance unit, IReadOnlyList<UnitCatalog> catalogs, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(catalogs);
        ArgumentNullException.ThrowIfNull(vocabulary);
        if (!vocabulary.IsA(unit.Kind, "asl:mmc"))
        {
            return ConditionState.Inapplicable;
        }

        var @class = unit.Definition is { } reference
            ? catalogs.FirstOrDefault(catalog => catalog.Identity == reference.Catalog)?.Definition(reference.Definition)?.Class
            : null;
        if (@class is null || !vocabulary.TryGetAttribute(ClassAttribute, out var attribute) || attribute.Member(@class) is null)
        {
            return ConditionState.Unknown;
        }

        if (!InexperiencedClasses.Contains(@class))
        {
            return ConditionState.False;
        }

        if (@class == "conscript")
        {
            return ConditionState.True;
        }

        // A19.3: a Green MMC stacked with an unbroken leader is exempt.
        if (state.Location(unit.Id) is not { } location)
        {
            return ConditionState.True;
        }

        var leaders = state.At(location.Location).OfType<UnitInstance>()
            .Where(item => item.Id != unit.Id && item.Side == unit.Side && vocabulary.IsA(item.Kind, "asl:leader"))
            .Select(leader => GameState.Condition(leader, Conditions.Broken))
            .ToArray();
        return leaders.Contains(ConditionState.False) ? ConditionState.False
            : leaders.Contains(ConditionState.Unknown) ? ConditionState.Unknown
            : ConditionState.True;
    }

    /// <summary>
    /// The MF allotment of a Good Order MMC moving on its own: four, three if Inexperienced (A4.11, p. 48; A19.31, p. 86).
    /// Null when Inexperienced status is unknown or the unit is not an MMC. The leader's bonus for a stack moving with
    /// it, and conveyances, are not modelled.
    /// </summary>
    public static int? MfAllowance(GameState state, UnitInstance unit, IReadOnlyList<UnitCatalog> catalogs, UnitVocabulary vocabulary) =>
        Inexperienced(state, unit, catalogs, vocabulary) switch
        {
            ConditionState.True => 3,
            ConditionState.False => 4,
            _ => null,
        };
}
