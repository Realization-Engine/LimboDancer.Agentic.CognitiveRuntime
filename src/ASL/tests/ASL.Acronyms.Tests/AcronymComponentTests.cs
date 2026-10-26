using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Acronyms;
using LimboDancer.Domains.Asl.MapStudio.Components.Layout;
using LimboDancer.Domains.Asl.MapStudio.Components.Pages;
using LimboDancer.Domains.Asl.MapStudio.Services.Acronyms;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ASL.Acronyms;

public sealed class AcronymComponentTests : IDisposable
{
    private readonly BunitContext context = new();
    public AcronymComponentTests()
    {
        var catalog = new AcronymCatalog(NullLogger<AcronymCatalog>.Instance);
        context.Services.AddSingleton(catalog);
        context.Services.AddSingleton(new AcronymSearchService(catalog));
    }
    public void Dispose() => context.Dispose();

    [Fact]
    public void CompleteDefinitionAndEvidenceRenderWithoutChangingCase()
    {
        var cut = context.Render<AcronymSearchPanel>(p => p.Add(c => c.State, new(Query: "dr", Entry: "main:dr")));
        Assert.Equal("dr", cut.Find(".acronym-entry h3").TextContent);
        Assert.Contains("Die roll, using one die.", cut.Find(".acronym-entry").TextContent, StringComparison.Ordinal);
        Assert.Contains("Search hits are leads", cut.Markup, StringComparison.Ordinal);
        Assert.Equal("search", cut.Find("input[type=search]").GetAttribute("type"));
        Assert.NotEmpty(cut.FindAll("label"));
    }

    [Fact]
    public void SelectionAndClearEmitStateAndPanelsAreIndependent()
    {
        AcronymSearchState? emitted = null;
        var first = context.Render<AcronymSearchPanel>(p => p.Add(c => c.State, new(Query: "DR"))
            .Add(c => c.StateChanged, value => emitted = value));
        var second = context.Render<AcronymSearchPanel>(p => p.Add(c => c.State, new(Query: "MC")));
        Assert.NotEqual(first.Find("input").Id, second.Find("input").Id);
        first.Find(".acronym-result").Click();
        Assert.Equal("main:DR", emitted!.Entry);
        Assert.Equal("MC", second.Find("input").GetAttribute("value"));
        first.Find("button.link").Click();
        Assert.Equal(new AcronymSearchState(), emitted);
    }

    [Fact]
    public void UnknownAndFilteredSelectionsAreExplained()
    {
        var cut = context.Render<AcronymSearchPanel>(p => p.Add(c => c.State, new(Query: "no-such-entry", Entry: "missing")));
        Assert.Contains("No entries match", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Entry not found", cut.Markup, StringComparison.Ordinal);
        cut.Render(p => p.Add(c => c.State, new(Query: "no-such-entry", Entry: "main:DR")));
        Assert.Contains("outside the current search filters", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PageAcceptsMalformedParametersAndNavigationHasReferenceDestination()
    {
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/acronyms?page=bad&case=bad&entry=main%3Adr");
        var cut = context.Render<AcronymSearch>();
        Assert.Equal("dr", cut.Find(".acronym-entry h3").TextContent);
        Assert.Equal("reference", StudioNavigation.GroupOf("acronyms?entry=main:dr")?.Id);
        var nav = context.Render<StudioNavigation>();
        Assert.Contains(nav.FindAll("a"), a => a.TextContent == "Acronym Search");
    }

    [Fact]
    public async Task HostUpdateCancelsPendingTyping()
    {
        var clock = new ManualClock();
        AcronymSearchState? emitted = null;
        var cut = context.Render<AcronymSearchPanel>(p => p.Add(c => c.Clock, clock)
            .Add(c => c.StateChanged, value => emitted = value));
        var typing = cut.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "DR" });
        await clock.Ready.Task;
        cut.Render(p => p.Add(c => c.State, new(Query: "MC")));
        clock.Fire();
        await typing;
        Assert.Null(emitted);
        Assert.Equal("MC", cut.Find("input[type=search]").GetAttribute("value"));
    }

    private sealed class ManualClock : TimeProvider
    {
        public TaskCompletionSource Ready { get; } = new();
        private TimerCallback? callback;
        private object? state;
        public override ITimer CreateTimer(TimerCallback action, object? value, TimeSpan dueTime, TimeSpan period)
        {
            callback = action;
            state = value;
            Ready.SetResult();
            return new ManualTimer();
        }
        public void Fire() => callback?.Invoke(state);
        private sealed class ManualTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;
            public void Dispose()
            {
            }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
