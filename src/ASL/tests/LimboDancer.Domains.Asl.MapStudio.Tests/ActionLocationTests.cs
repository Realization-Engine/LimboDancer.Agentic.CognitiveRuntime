using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class ActionLocationTests : IDisposable
{
    private readonly BunitContext context = new();
    public void Dispose() => context.Dispose();

    [Fact]
    public void InvalidEditBlocksOldValueUntilExplicitReplacement()
    {
        var pending = false;
        var value = "bd01:A1:0";
        var picking = new LocationPicking { Boards = () => ["bd01"] };
        var editor = context.Render<ActionLocationChoice>(p => p.AddCascadingValue("Picking", picking)
            .Add(x => x.Id, "target").Add(x => x.Value, value).Add(x => x.Choices, [value, "bd01:B2:0"])
            .Add(x => x.OnEditingChanged, v => pending = v).Add(x => x.OnSuggested, v => value = v));
        editor.Find("#target-entry").Input("not a location");
        Assert.True(pending);
        editor.Find("#target-entry").Change("not a location");
        Assert.True(pending);
        Assert.Equal("bd01:A1:0", value);
        editor.Find("#target").Change("bd01:B2:0");
        Assert.False(pending);
        Assert.Equal("bd01:B2:0", value);
        Assert.DoesNotContain("not a location", editor.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void HiddenFieldCancelsOnlyItsOwnPicker()
    {
        var picking = new LocationPicking { Armed = "target", Boards = () => ["bd01"] };
        picking.Arm = (id, label) => { picking.Armed = id; picking.ArmedLabel = label; };
        var editor = context.Render<LocationField>(p => p.AddCascadingValue("Picking", picking)
            .Add(x => x.Id, "target").Add(x => x.Label, "Target"));
        editor.Render(p => p.Add(x => x.Visible, false));
        Assert.Null(picking.Armed);
        picking.Armed = "route";
        editor.Render();
        Assert.Equal("route", picking.Armed);
    }

    [Fact]
    public void RestrictedPickWaitsForAnExplicitLevelAndExcludesDisabledChoices()
    {
        var value = "bd01:A1:0";
        var picking = new LocationPicking { Boards = () => ["bd01"] };
        picking.Arm = (id, label) => { picking.Armed = id; picking.ArmedLabel = label; };
        var editor = context.Render<ActionLocationChoice>(p => p.AddCascadingValue("Picking", picking)
            .AddCascadingValue("PickedLocation", "bd01:C3:0")
            .Add(x => x.Id, "destination").Add(x => x.AllowFreeEntry, false).Add(x => x.Value, value)
            .Add(x => x.Choices, [value, "bd01:C3:0", "bd01:C3:1", "bd01:C3:2"])
            .Add(x => x.Unavailable, new HashSet<string> { "bd01:C3:2" })
            .Add(x => x.OnSuggested, v => value = v));
        Assert.Empty(editor.FindAll("input"));
        editor.Find(".pick-on-map").Click();
        Assert.Empty(value);
        picking.Picked = "bd01:C3:0";
        picking.Serial++;
        editor.Render(p => p.Add(x => x.Value, value));
        Assert.Empty(value);
        Assert.Equal(2, editor.FindAll(".use-picked").Count);
        editor.Find(".use-picked[data-location='bd01:C3:1']").Click();
        Assert.Equal("bd01:C3:1", value);
        Assert.Null(picking.Armed);
    }

    [Fact]
    public void ResetWithUnchangedValueDropsUnfinishedText()
    {
        var editor = context.Render<ActionLocationChoice>(p => p.Add(x => x.Id, "target").Add(x => x.Value, "bd01:A1:0"));
        editor.Find("#target-entry").Input("unfinished");
        editor.Render(p => p.Add(x => x.ResetToken, 1));
        Assert.DoesNotContain("unfinished", editor.Markup, StringComparison.Ordinal);
    }
}
