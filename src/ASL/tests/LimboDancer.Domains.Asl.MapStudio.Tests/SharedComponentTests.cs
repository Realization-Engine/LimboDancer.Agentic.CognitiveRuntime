using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Cards;
using LimboDancer.Domains.Asl.MapStudio.Components.Layout;
using LimboDancer.Domains.Asl.MapStudio.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>The shell and the shared components of pass 22b (plan sections 10 and 16.3, and K02 to K04 of section 16.16).</summary>
public sealed class SharedComponentTests : IDisposable
{
    private readonly BunitContext context = new();

    public void Dispose() => context.Dispose();

    [Theory]
    [InlineData("", "boards")]
    [InlineData("boards/bd01", "boards")]
    [InlineData("author/new", "boards")]
    [InlineData("maps", "maps")]
    [InlineData("units/cards/edit?card=gambit", "scenarios")]
    [InlineData("units/scenarios", "scenarios")]
    [InlineData("games/play?game=village", "play")]
    [InlineData("units/games", "play")]
    [InlineData("fidelity", "verify")]
    public void EveryRouteBelongsToItsGroup(string route, string group) => Assert.Equal(group, StudioNavigation.GroupOf(route)?.Id);

    [Fact]
    public void TheNavigationMarksTheCurrentGroupAndHidesWhenAsked()
    {
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("units/cards/edit");
        var nav = context.Render<StudioNavigation>();
        Assert.Equal("scenarios", nav.Find(".nav-group.current").GetAttribute("data-group"));
        Assert.Equal(StudioNavigation.Groups.Sum(group => group.Items.Count), nav.FindAll(".studio-nav a").Count);
        Assert.False(nav.Find("#studio-nav").HasAttribute("hidden"));

        nav.Render(parameters => parameters.Add(component => component.Hidden, true));
        Assert.True(nav.Find("#studio-nav").HasAttribute("hidden"));
    }

    [Fact]
    public void TheShellCollapsesWideAndClosesTheDrawerOnEscapeAndOnARouteChange()
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var shell = context.Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, (RenderFragment)(builder => builder.AddContent(0, "page"))));

        // Wide: the sidebar shows, the toggle collapses it, and a route change leaves it as it is.
        Assert.Equal("true", shell.Find(".nav-toggle").GetAttribute("aria-expanded"));
        shell.Find(".nav-toggle").Click();
        Assert.True(shell.Find("#studio-nav").HasAttribute("hidden"));
        navigation.NavigateTo("maps");
        Assert.True(shell.Find("#studio-nav").HasAttribute("hidden"));
        shell.Find(".nav-toggle").Click();
        shell.Find(".studio-shell").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(shell.Find("#studio-nav").HasAttribute("hidden"));

        // Narrow: a drawer, closed until opened; Escape anywhere in the shell closes it, and so does a route change.
        shell.InvokeAsync(() => shell.Instance.SetNarrow(true));
        Assert.Contains("narrow", shell.Find(".studio-shell").ClassName, StringComparison.Ordinal);
        Assert.True(shell.Find("#studio-nav").HasAttribute("hidden"));
        shell.Find(".nav-toggle").Click();
        Assert.Equal("true", shell.Find(".nav-toggle").GetAttribute("aria-expanded"));
        shell.Find(".studio-shell").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(shell.Find("#studio-nav").HasAttribute("hidden"));
        shell.Find(".nav-toggle").Click();
        navigation.NavigateTo("settings");
        shell.WaitForAssertion(() => Assert.True(shell.Find("#studio-nav").HasAttribute("hidden")));
    }

    [Fact]
    public void ThePageHeaderShowsItsTitleDescriptionAndActions()
    {
        var header = context.Render<PageHeader>(parameters => parameters
            .Add(component => component.Title, "Maps")
            .AddChildContent("<p>Maps place boards.</p>")
            .Add(component => component.Actions, "<button>New map</button>"));
        Assert.Equal("Maps", header.Find("h2.page").TextContent);
        Assert.Equal("Maps place boards.", header.Find(".page-description").TextContent);
        Assert.Equal("New map", header.Find(".page-actions button").TextContent);
        Assert.Empty(context.Render<PageHeader>(parameters => parameters.Add(component => component.Title, "Settings")).FindAll(".page-description"));
    }

    [Fact]
    public void AFindingListShowsSeverityInWordsAndAnExplicitEmptyState()
    {
        var list = context.Render<FindingList>(parameters => parameters
            .Add(component => component.Id, "found")
            .Add(component => component.Items, [new FindingList.Finding("card.elr: out of range"), new FindingList.Finding("check the date", FindingList.Severity.Warning)]));
        Assert.Equal(["Error", "Warning"], list.FindAll("#found .status-badge").Select(badge => badge.TextContent).ToArray());
        Assert.Contains("fail", list.Find("#found .status-badge").ClassList);

        var empty = context.Render<FindingList>(parameters => parameters
            .Add(component => component.Items, [])
            .Add(component => component.EmptyId, "clean")
            .Add(component => component.Empty, "No findings."));
        Assert.Equal("No findings.", empty.Find("#clean").TextContent);
        Assert.Empty(empty.FindAll("ul"));
    }

    [Fact]
    public void OperationFeedbackIsSilentWithoutAMessageAndAlertsOnFailure()
    {
        var silent = context.Render<OperationFeedback>(parameters => parameters.Add(component => component.Id, "op"));
        Assert.Equal("status", silent.Find("#op").GetAttribute("role"));
        Assert.Empty(silent.Find("#op").TextContent.Trim());
        var busy = context.Render<OperationFeedback>(parameters => parameters.Add(component => component.Busy, true).Add(component => component.Id, "op"));
        Assert.Contains("Working", busy.Find("#op.operation-feedback-region .busy").TextContent, StringComparison.Ordinal);
        var failed = context.Render<OperationFeedback>(parameters => parameters
            .Add(component => component.Id, "op")
            .Add(component => component.Message, "Not saved.")
            .Add(component => component.Outcome, OperationFeedback.OperationOutcome.Failed)
            .Add(component => component.Details, ["disk full"]));
        Assert.Equal("alert", failed.Find("#op .operation-feedback.failed").GetAttribute("role"));
        Assert.Contains("disk full", failed.Find("#op .finding-list").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void AFieldGroupLabelsItsControlAndNamesItsHelp()
    {
        var field = context.Render<FieldGroup>(parameters => parameters
            .Add(component => component.Label, "Game id")
            .Add(component => component.For, "new-id")
            .Add(component => component.Help, "Lower-case letters and digits.")
            .AddChildContent("<input id=\"new-id\" />"));
        Assert.Equal("new-id", field.Find("label").GetAttribute("for"));
        Assert.Equal("new-id-help", field.Find(".field-help").Id);
        Assert.Empty(field.FindAll(".field-error"));
    }

    [Fact]
    public void TheSourceNoticeNamesWhatIsMissingAndLinksToSettings()
    {
        var notice = context.Render<SourceRequiredNotice>(parameters => parameters.Add(component => component.Missing, "No VASL checkout is configured."));
        Assert.Equal("No VASL checkout is configured.", notice.Find("strong").TextContent);
        Assert.Equal("settings", notice.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void ThePerspectivePickerReportsTheChosenView()
    {
        string? chosen = null;
        var picker = context.Render<PerspectivePicker>(parameters => parameters
            .Add(component => component.Id, "view")
            .Add(component => component.Choices, [new PerspectivePicker.Choice("adjudicator", "Adjudicator"), new PerspectivePicker.Choice("german", "German")])
            .Add(component => component.Value, "adjudicator")
            .Add(component => component.ValueChanged, value => chosen = value));
        picker.Find("#view").Change("german");
        Assert.Equal("german", chosen);
    }

    [Fact]
    public void TheRevisionNavigatorStepsWithinItsBounds()
    {
        var revisions = new List<int>();
        var navigator = context.Render<RevisionNavigator>(parameters => parameters
            .Add(component => component.Min, 0).Add(component => component.Max, 5).Add(component => component.Current, 0)
            .Add(component => component.RevisionChanged, revision => revisions.Add(revision)));
        Assert.True(navigator.Find("#revision-previous").HasAttribute("disabled"));
        navigator.Find("#revision-next").Click();
        navigator.Find("#revision").Change("9");
        Assert.Equal([1, 5], revisions);
    }

    [Fact]
    public void TheCardPickerMarksTheUsersCardsTheSameEverywhere()
    {
        string? chosen = "gambit";
        var picker = context.Render<CardPicker>(parameters => parameters
            .Add(component => component.Id, "cards")
            .Add(component => component.Cards, [new CardPicker.Choice("gambit", "Gambit", false), new CardPicker.Choice("village", "Village Fight", true)])
            .Add(component => component.EmptyLabel, "choose a card")
            .Add(component => component.ValueChanged, value => chosen = value));
        Assert.Equal(["choose a card", "Gambit", "Village Fight (yours)"], picker.FindAll("option").Select(option => option.TextContent).ToArray());
        picker.Find("#cards").Change(string.Empty);
        Assert.Null(chosen);
    }

    [Fact]
    public void TheBalanceChoiceAsksForThePlayersOnlyWhenADrDecides()
    {
        var balance = context.Render<BalanceChoice>(parameters => parameters.Add(component => component.Sides, ["german", "russian"]).Add(component => component.Value, "agree:german"));
        Assert.Equal(5, balance.FindAll("#new-balance option").Count);
        Assert.Empty(balance.FindAll("#new-player-one"));
        balance.Render(parameters => parameters.Add(component => component.Value, "wish:russian"));
        Assert.NotNull(balance.Find("#new-player-two"));
    }
}
