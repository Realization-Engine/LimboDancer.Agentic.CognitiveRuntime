using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// Cross-checks the Scenario A1 catalog against the existing Scenario A1 snapshots (ASL-MAP-081, decision D4), which
/// stay unchanged. Each snapshot fact that names a kind of unit is bound, by <c>nameof</c>, to the catalog slot that
/// stands for it; renaming or removing the fact breaks the build, and a catalog whose slot or definitions disagree with
/// the fact fails here. The facts' conditions (Good Order, unpinned, concealed) are state, not definitions, and are
/// checked when the state model reads them (Unit Requirements, section 13, steps 4 and 5).
/// </summary>
public sealed class ScenarioA1CatalogCrossCheckTests
{
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();

    private static readonly (string Fact, string Slot, string Kind)[] KindFacts =
    [
        (nameof(ScenarioA1BoardSnapshot) + "." + nameof(ScenarioA1BoardSnapshot.IsGoodOrderInfantrySquad), "a1-moving-squad", "asl:squad"),
        (nameof(ScenarioA1AdditionalBoardState) + "." + nameof(ScenarioA1AdditionalBoardState.FriendlySquads), "a1-moving-squad", "asl:squad"),
        (nameof(ScenarioA1AdditionalBoardState) + "." + nameof(ScenarioA1AdditionalBoardState.IncomingSquads), "a1-moving-squad", "asl:squad"),
        (nameof(ScenarioA1AdditionalBoardState) + "." + nameof(ScenarioA1AdditionalBoardState.FriendlyUnmannedCrewsOrHalfSquads), "a1-friendly-half-squad", "asl:half-squad"),
        (nameof(ScenarioA1AdditionalBoardState) + "." + nameof(ScenarioA1AdditionalBoardState.IsGoodOrderInfantryMmc), "a1-moving-mmc", "asl:mmc"),
        (nameof(ScenarioA1ConcealedSmcOverrunSnapshot) + "." + nameof(ScenarioA1ConcealedSmcOverrunSnapshot.IsGoodOrderUnconcealedNonDummyInfantryMmc), "a1-moving-mmc", "asl:mmc"),
        (nameof(ScenarioA1SecondDefenderSnapshot) + "." + nameof(ScenarioA1SecondDefenderSnapshot.IsGoodOrderUnconcealedNonDummyInfantryMmc), "a1-moving-mmc", "asl:mmc"),
        (nameof(ScenarioA1AdditionalBoardState) + "." + nameof(ScenarioA1AdditionalBoardState.IsGoodOrderUnpinnedInfantry), "a1-moving-infantry", "asl:personnel"),
        (nameof(ScenarioA1AdditionalBoardState) + "." + nameof(ScenarioA1AdditionalBoardState.IsGoodOrderUnpinnedInfantryWithoutTiOrCc), "a1-moving-infantry", "asl:personnel"),
        (nameof(ScenarioA1PostRevealSnapshot) + "." + nameof(ScenarioA1PostRevealSnapshot.IsOrdinaryInfantry), "a1-moving-infantry", "asl:personnel"),
        (nameof(ScenarioA1BoardOccupancy) + "." + nameof(ScenarioA1BoardOccupancy.KnownUnconcealedEnemyMmc), "a1-defending-mmc", "asl:mmc"),
        (nameof(ScenarioA1BoardOccupancy) + "." + nameof(ScenarioA1BoardOccupancy.ExactlyOneKnownUnconcealedEnemyMmc), "a1-defending-mmc", "asl:mmc"),
        (nameof(ScenarioA1BoardOccupancy) + "." + nameof(ScenarioA1BoardOccupancy.KnownUnpinnedGoodOrderArmedEnemySquad), "a1-defending-squad", "asl:squad"),
        (nameof(ScenarioA1BoardOccupancy) + "." + nameof(ScenarioA1BoardOccupancy.ExactlyOneKnownEnemySmc), "a1-defending-smc", "asl:smc"),
        (nameof(ScenarioA1AdditionalBoardState) + "." + nameof(ScenarioA1AdditionalBoardState.IsSmcOutsideAfv), "a1-defending-smc", "asl:smc"),
        (nameof(ScenarioA1RevealedOccupant) + "." + nameof(ScenarioA1RevealedOccupant.EnemySmc), "a1-defending-smc", "asl:smc"),
        (nameof(ScenarioA1SecondDefenderState) + "." + nameof(ScenarioA1SecondDefenderState.RevealedSmc), "a1-second-defender-smc", "asl:smc"),
        (nameof(ScenarioA1SecondDefenderState) + "." + nameof(ScenarioA1SecondDefenderState.RevealedMmc), "a1-second-defender-mmc", "asl:mmc"),
        (nameof(ScenarioA1AdditionalDefenderType) + "." + nameof(ScenarioA1AdditionalDefenderType.Smc), "a1-second-defender-smc", "asl:smc"),
        (nameof(ScenarioA1AdditionalDefenderType) + "." + nameof(ScenarioA1AdditionalDefenderType.Mmc), "a1-second-defender-mmc", "asl:mmc"),
    ];

    /// <summary>The synthetic catalog, and the published one once its transcription is reviewed.</summary>
    public static TheoryData<string> Catalogs()
    {
        var names = new TheoryData<string>();
        foreach (var name in UnitCatalogs.Names)
        {
            names.Add(name);
        }

        return names;
    }

    private static UnitCatalog Catalog(string name)
    {
        var result = UnitCatalogs.Read(name, Vocabulary)!;
        Assert.Empty(result.Diagnostics);
        return result.Catalog!;
    }

    [Fact]
    public void TheSyntheticCatalogIsAmongTheCheckedCatalogs() =>
        Assert.Contains(UnitCatalogs.ScenarioA1Synthetic, UnitCatalogs.Names);

    [Theory]
    [MemberData(nameof(Catalogs))]
    public void EverySnapshotKindFactHasASlotOfThatKind(string name)
    {
        var catalog = Catalog(name);
        foreach (var (fact, slotId, kind) in KindFacts)
        {
            var slot = catalog.Slots.SingleOrDefault(slot => slot.Id == slotId);
            Assert.True(slot is not null, $"{fact}: {name} has no slot {slotId}.");
            Assert.Equal(kind, slot.Kind);
            var filling = catalog.Filling(slotId);
            Assert.True(filling.Count > 0, $"{fact}: no definition in {name} fills {slotId}.");
            Assert.All(filling, definition => Assert.True(Vocabulary.IsA(definition.Kind, kind), $"{fact}: {definition.Id} is a {definition.Kind}, not a {kind}."));
        }
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public void EverySlotStandsForASnapshotFact(string name)
    {
        var bound = KindFacts.Select(fact => fact.Slot).ToHashSet(StringComparer.Ordinal);
        Assert.All(Catalog(name).Slots, slot => Assert.Contains(slot.Id, bound));
    }

    /// <summary>
    /// The reviewed stacking case counts squads and half-squads (A5.1, p. 52; A5.5, p. 53). Counted from the catalog
    /// kinds of a stack of two squads and one half-squad, with one squad entering, the facts are the ones the existing
    /// <see cref="ScenarioA1StackingCost"/> reviews, and their Unit Size Numbers are those of A1.6 (p. 45).
    /// </summary>
    [Theory]
    [MemberData(nameof(Catalogs))]
    public void CatalogKindsReproduceTheReviewedStackingFacts(string name)
    {
        var catalog = Catalog(name);
        var squad = catalog.Filling("a1-moving-squad")[0];
        var halfSquad = catalog.Filling("a1-friendly-half-squad")[0];
        UnitDefinition[] stack = [squad, squad, halfSquad];
        var facts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["friendlySquads"] = Count(stack, "asl:squad"),
            ["friendlyUnmannedCrewsOrHalfSquads"] = (stack.Count(unit => Vocabulary.IsA(unit.Kind, "asl:half-squad") || Vocabulary.IsA(unit.Kind, "asl:crew"))).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["friendlySmc"] = Count(stack, "asl:smc"),
            ["incomingSquads"] = Count([squad], "asl:squad"),
        };

        Assert.True(ScenarioA1StackingCost.IsReviewedThreeMfEntry(facts));
        Assert.Equal(3, Vocabulary.SizeClass(squad.Kind));
        Assert.Equal(2, Vocabulary.SizeClass(halfSquad.Kind));
        Assert.All(catalog.Filling("a1-defending-smc"), smc => Assert.Equal(1, Vocabulary.SizeClass(smc.Kind)));
    }

    private static string Count(IEnumerable<UnitDefinition> stack, string kind) =>
        stack.Count(unit => Vocabulary.IsA(unit.Kind, kind)).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
