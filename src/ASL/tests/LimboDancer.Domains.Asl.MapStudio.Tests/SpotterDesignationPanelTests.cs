using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class SpotterDesignationPanelTests : IDisposable
{
    private readonly BunitContext context = new();

    public void Dispose() => context.Dispose();

    [Fact]
    public void AReplacementNeedsPermissionAndItsChoicesKeepHexGroupingAndCallbacks()
    {
        var gun = string.Empty;
        var spotter = string.Empty;
        var proposals = 0;
        UnitPlace? Place(string id) => id switch
        {
            "m1" or "s1" => new("[G4]", "[G4]", (1, "G", 4), false),
            "l1" => new("[G4]", "[G4]", (1, "G", 4), true),
            "s2" => new("[H4]", "[H4]", (1, "H", 4), false),
            _ => null,
        };
        var panel = context.Render<SpotterDesignationPanel>(parameters => parameters
            .Add(item => item.Guns, [new("m1", "mortar m1")])
            .Add(item => item.Candidates, [new("s2", "squad s2"), new("s1", "squad s1"), new("l1", "leader l1")])
            .Add(item => item.Gun, "m1")
            .Add(item => item.Spotter, "s1")
            .Add(item => item.Replacement, true)
            .Add(item => item.Ready, false)
            .Add(item => item.Note, "The assignment history is not known.")
            .Add(item => item.OnGun, value => gun = value)
            .Add(item => item.OnSpotter, value => spotter = value)
            .Add(item => item.OnPropose, () => proposals++)
            .AddCascadingValue("PlaceOfUnit", (Func<string, UnitPlace?>)Place));

        Assert.Contains("replace Spotter", panel.Find("#propose-spotter-designation").TextContent, StringComparison.Ordinal);
        Assert.True(panel.Find("#propose-spotter-designation").HasAttribute("disabled"));
        Assert.Contains("history is not known", panel.Find("#spotter-designation-note").TextContent, StringComparison.Ordinal);
        Assert.StartsWith("[G4]:", panel.Find("#spotter-designation-gun option[value='m1']").TextContent, StringComparison.Ordinal);
        Assert.Equal(["[G4]", "[H4]"], panel.FindAll("#spotter-designation-unit optgroup").Select(group => group.GetAttribute("label")));
        Assert.Equal(["l1", "s1", "s2"], panel.FindAll("#spotter-designation-unit optgroup option").Select(option => option.GetAttribute("value")));
        Assert.All(panel.FindAll("#spotter-designation-unit optgroup option"), option => Assert.StartsWith("[", option.TextContent, StringComparison.Ordinal));

        panel.Find("#spotter-designation-gun").Change("m1");
        panel.Find("#spotter-designation-unit").Change("l1");
        Assert.Equal("m1", gun);
        Assert.Equal("l1", spotter);
        Assert.Equal(0, proposals);

        panel.Render(parameters => parameters.Add(item => item.Ready, true));
        Assert.False(panel.Find("#propose-spotter-designation").HasAttribute("disabled"));
        panel.Find("#propose-spotter-designation").Click();
        Assert.Equal(1, proposals);

        panel.Render(parameters => parameters.Add(item => item.Replacement, false).Add(item => item.Ready, false).Add(item => item.Note, "A Spotter cannot be designated now."));
        Assert.Contains("designate Spotter", panel.Find("#propose-spotter-designation").TextContent, StringComparison.Ordinal);
        Assert.True(panel.Find("#propose-spotter-designation").HasAttribute("disabled"));
    }
}
