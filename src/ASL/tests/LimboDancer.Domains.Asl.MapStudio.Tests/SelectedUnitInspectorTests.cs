using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.MapStudio.Components.Board;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class SelectedUnitInspectorTests
{
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly BoardLocation Location = BoardLocation.Parse("ab-synthetic:B1:0");
    private static readonly UnitInstance Crew = new("crew-1", "asl:crew", null, "allied",
        new MapPosition(Location), new Dictionary<string, ConditionState>(), InstanceStatus.Active, []);

    [Theory]
    [InlineData(HoldingRole.Manned, "Operates", "Manned by")]
    [InlineData(HoldingRole.Possessed, "Carries", "Carried by")]
    [InlineData(HoldingRole.Towed, "Tows", "Towed by")]
    public void ShowsBothDirectionsOfVisibleEquipmentRelationship(HoldingRole role, string action, string holderLabel)
    {
        using var context = new BunitContext();
        var view = View([Crew], [Equipment(role)]);
        var crew = Render(context, "crew-1", "crew", view);
        Assert.Contains(action, crew.Find("#unit-game-facts").TextContent, StringComparison.Ordinal);
        Assert.Contains("Test gun (gun-1)", crew.Find("#unit-game-facts").TextContent, StringComparison.Ordinal);
        var gun = Render(context, "gun-1", "gun", view);
        Assert.Contains(holderLabel, gun.Find("#unit-game-facts").TextContent, StringComparison.Ordinal);
        Assert.Contains("Test crew (crew-1)", gun.Find("#unit-game-facts").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void HiddenHolderIsNotNamedEvenIfNameResolverKnowsIt()
    {
        using var context = new BunitContext();
        var gun = Render(context, "gun-1", "gun", View([], [Equipment(HoldingRole.Manned)]));
        Assert.Contains("a unit not in view", gun.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Test crew", gun.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("crew-1", gun.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleasedOrInactiveEquipmentIsNotShownAsOperated()
    {
        using var context = new BunitContext();
        var item = Equipment(HoldingRole.Manned);
        foreach (var equipment in new[] { item with { Holding = null }, item with { Status = InstanceStatus.Eliminated } })
        {
            var crew = Render(context, "crew-1", "crew", View([Crew], [equipment]));
            Assert.DoesNotContain("Operates", crew.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Test gun", crew.Markup, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CampaignOriginIsOnlyResolvedForAVisibleCounter()
    {
        using var context = new BunitContext();
        var visible = Render(context, "crew-1", "crew", View([Crew], []));
        Assert.Contains("de-mortar-01", visible.Find("#unit-campaign-origin").TextContent, StringComparison.Ordinal);
        Assert.Contains("A/D-5", visible.Find("#unit-campaign-origin").TextContent, StringComparison.Ordinal);
        Assert.Contains("Manufactured profile", visible.Markup, StringComparison.Ordinal);
        var hidden = Render(context, "crew-1", "crew", View([], []));
        Assert.Empty(hidden.FindAll("#unit-campaign-origin"));
        Assert.DoesNotContain("de-mortar-01", hidden.Markup, StringComparison.Ordinal);
    }

    // The user, 2026-10-21: the units in a hex by side, the ATTACKER's first, each side's leaders at the top of its group.
    [Fact]
    public void TheHexsUnitsAreListedBySideTheAttackersFirstLeadersOnTop()
    {
        using var context = new BunitContext();
        PlacedUnit Placed(string id, string kind, string side) => new(Assert.Single(UnitDocumentReader.Read($$"""
            {"vocabulary":["asl@1.0.0"],"id":"{{id}}","kind":"asl:{{kind}}","side":"{{side}}","faces": { "front": {} } }
            """, Vocabulary).Documents), Location, new HexIndex(1, 0), true, id);
        var list = context.Render<HexUnitStackList>(parameters => parameters
            .Add(component => component.Hex, "B1")
            .Add(component => component.Members, [Placed("g1", "squad", "german"), Placed("r1", "squad", "russian"), Placed("g9", "leader", "german")])
            .Add(component => component.Attacker, "russian"));
        Assert.Equal(["Russian (ATTACKER) (1)", "German (DEFENDER) (2)"], list.FindAll("h4.unit-list-side").Select(item => item.TextContent));
        Assert.Equal(["r1", "g9", "g1"], list.FindAll("#hex-units li > button").Select(item => item.TextContent));
    }

    private static EquipmentInstance Equipment(HoldingRole role) => new("gun-1", "asl:gun", "allied",
        new MapPosition(Location), new Holding("crew-1", role), new Dictionary<string, ConditionState>(), InstanceStatus.Active);

    private static GameView View(IReadOnlyList<UnitInstance> units, IReadOnlyList<EquipmentInstance> equipment) => new(
        Perspective.Side("allied"), new StateStamp(new GameScope(Guid.Empty, "inspection"), 1, "v1"), true,
        [], new MapInPlay("ab-synthetic", "v1", []), 1, "rally", "allied", units, [], equipment, [], [],
        new Dictionary<string, MapPosition>());

    private static IRenderedComponent<SelectedUnitInspector> Render(BunitContext context, string id, string kind, GameView view)
    {
        var document = Assert.Single(UnitDocumentReader.Read($$"""
            {"vocabulary":["asl@1.0.0"],"id":"{{id}}","kind":"asl:{{kind}}","side":"british",
             "faces": { "front": {} } }
            """, Vocabulary).Documents);
        var placed = new PlacedUnit(document, Location, new HexIndex(1, 0), true, "Selected counter");
        return context.Render<SelectedUnitInspector>(parameters => parameters
            .Add(component => component.Unit, placed)
            .Add(component => component.Vocabulary, Vocabulary)
            .Add(component => component.View, view)
            .Add(component => component.NameOf, id => id == "crew-1" ? "Test crew" : "Test gun")
            .Add(component => component.OriginOf, _ => new CampaignCounterOrigin("de-mortar-01", "A/D-5", "catalog@1 / mortar", "Manufactured profile")));
    }
}
