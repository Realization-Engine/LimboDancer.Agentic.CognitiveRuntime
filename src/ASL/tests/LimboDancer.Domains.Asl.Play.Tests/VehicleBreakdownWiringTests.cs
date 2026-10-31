using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed class VehicleBreakdownWiringTests
{
    private static readonly Lazy<UnitDefinition> Tank = new(() =>
        UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitVocabulary.Asl())!.Catalog!.Definition("attacker-tank")!);

    [Fact]
    public void InherentB12IsNotMistakenForAPrintedNumber()
    {
        var definition = Tank.Value;
        Assert.Equal(12, definition.Printed("front", "asl:breakdown")!.Value!.Number);
        var facts = LiveFire.PrintedVehicleBreakdown(definition);
        Assert.NotNull(facts);
        Assert.Null(facts.PrintedB);
        Assert.Null(facts.PrintedX);
        Assert.False(facts.Circled);
        Assert.False(facts.NoteExtends);
    }

    [Theory]
    [InlineData("asl:printed-breakdown")]
    [InlineData("asl:printed-elimination")]
    [InlineData("asl:breakdown-circled")]
    [InlineData("asl:breakdown-note-extends")]
    public void EveryPrintedFactMustBeKnown(string name)
    {
        var definition = Tank.Value;
        Assert.Null(LiveFire.PrintedVehicleBreakdown(definition with
        {
            Values = [.. definition.Values.Where(value => value.Name != name)],
        }));
        Assert.Null(LiveFire.PrintedVehicleBreakdown(definition with
        {
            Values = [.. definition.Values.Select(value => value.Name == name ? value with
            {
                State = PrintedState.NotInSource,
            } : value)],
        }));
    }
}
