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

    private static readonly IReadOnlySet<string> InexperiencedClasses = Rules.ScenarioA1Definitions.InexperiencedClasses;

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

        // The facts (pass 32.a): the kind, the current definition's class and whether the vocabulary knows it, the Location, and the same-side leaders there.
        var @class = unit.Definition is { } reference
            ? catalogs.FirstOrDefault(catalog => catalog.Identity == reference.Catalog)?.Definition(reference.Definition)?.Class
            : null;
        var known = @class is not null && vocabulary.TryGetAttribute(ClassAttribute, out var attribute) && attribute.Member(@class) is not null;
        var location = state.Location(unit.Id);
        Rules.RuleState[] leaders = location is null ? [] : [.. state.At(location.Location).OfType<UnitInstance>()
            .Where(item => item.Id != unit.Id && item.Side == unit.Side && vocabulary.IsA(item.Kind, "asl:leader"))
            .Select(leader => GameState.RuleStateOf(GameState.Condition(leader, Conditions.Broken)))];
        return GameState.ConditionStateOf(Rules.ScenarioA1Experience.Inexperienced(vocabulary.IsA(unit.Kind, "asl:mmc"), @class, known, location is not null, leaders));
    }

    /// <summary>
    /// The MF allotment of a Good Order MMC moving on its own: four, three if Inexperienced (A4.11, p. 48; A19.31, p. 86).
    /// Null when Inexperienced status is unknown or the unit is not an MMC. The leader's bonus for a stack moving with
    /// it, and conveyances, are not modelled.
    /// </summary>
    public static int? MfAllowance(GameState state, UnitInstance unit, IReadOnlyList<UnitCatalog> catalogs, UnitVocabulary vocabulary) =>
        Rules.ScenarioA1Experience.MfAllowance(GameState.RuleStateOf(Inexperienced(state, unit, catalogs, vocabulary)));

    /// <summary>
    /// The MF allotment of a Good Order unit moving hex by hex (unit step 22): a MMC's as <see cref="MfAllowance"/> gives,
    /// a SMC's six, three if wounded (A4.11, p. 48; A17.2, p. 85), and a berserk unit's eight (A15.431, p. 84; unit step 30). A SMC is wounded only when its state says so, as
    /// the Fire and Rally facts read it; a wound is always recorded by the event that inflicts it. The building entry of
    /// steps 7 to 11 keeps the MMC allotment its reviewed cases assume.
    /// </summary>
    public static int? MoveAllowance(GameState state, UnitInstance unit, IReadOnlyList<UnitCatalog> catalogs, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(vocabulary);
        return Rules.ScenarioA1Experience.MoveAllowance(GameState.Condition(unit, Conditions.Berserk) == ConditionState.True,
            GameState.Condition(unit, Conditions.Wounded) == ConditionState.True, vocabulary.IsA(unit.Kind, "asl:smc"), () => MfAllowance(state, unit, catalogs, vocabulary));
    }
}
