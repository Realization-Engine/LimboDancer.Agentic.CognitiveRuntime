using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Icons;
using LimboDancer.Domains.Asl.MapStudio.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>The Icon Lab's plan: briefs, decisions, batches, the prompt they make, and the Coverage, Prompts, and Batches tabs.</summary>
public sealed class IconPlanTests(StudioFactory factory) : IClassFixture<StudioFactory>, IDisposable
{
    private static readonly IconPromptStyle Style = new("set of 4 flat vector icons, 2x2 grid.", "Flat ink.", "--style raw --ar 1:1", "a single flat vector icon.");

    private readonly BunitContext context = new();
    private readonly DirectoryInfo scratch = Directory.CreateTempSubdirectory("icon-plan-");
    private PhysicalFileProvider? provider;

    public void Dispose()
    {
        context.Dispose();
        provider?.Dispose();
        scratch.Delete(true);
    }

    private static IconPlan Plan(params IconBrief[] briefs) => IconPlan.Empty with { Style = Style, Briefs = briefs };

    private static IconBrief Brief(string key, string brief) => new(key, key, brief);

    [Fact]
    public void APromptIsTheOpeningTheNumberedBriefsTheStyleTheSrefAndTheParameters()
    {
        var plan = Plan(Brief("a", "a bar, step back."), Brief("b", "a triangle, play"));
        Assert.Equal("set of 4 flat vector icons, 2x2 grid. Icons: 1. a bar, step back; 2. a triangle, play. Flat ink. --style raw --ar 1:1",
            plan.Prompt([plan.Brief("a")!, plan.Brief("b")!], string.Empty));
        Assert.EndsWith("Flat ink. --sref https://example.test/s.png --style raw --ar 1:1", plan.Prompt([plan.Brief("a")!], " https://example.test/s.png "), StringComparison.Ordinal);

        var batch = new IconBatch("2026-10-17-1", "Two", ["a", "gone"], string.Empty, "2026-10-17");
        Assert.Null(plan.WithBatch(batch).Prompt(batch));
    }

    [Fact]
    public void AOneIconPromptOpensWithTheSingleOpeningAndNamesItsOneBriefUnnumbered()
    {
        var plan = Plan(Brief("proposal", "a clipboard with a checkmark box and a pencil, a proposed order awaiting confirmation."));
        Assert.Equal("a single flat vector icon. Icon: a clipboard with a checkmark box and a pencil, a proposed order awaiting confirmation. Flat ink. --style raw --ar 1:1",
            plan.Prompt([plan.Brief("proposal")!], string.Empty, oneIcon: true));

        var batch = new IconBatch("2026-10-17-1", "Proposal", ["proposal"], string.Empty, "2026-10-17", OneIcon: true);
        Assert.StartsWith("a single flat vector icon. Icon: a clipboard", plan.WithBatch(batch).Prompt(batch), StringComparison.Ordinal);
        Assert.True(IconPlan.Read(plan.WithBatch(batch).Write()).Batches.Single().OneIcon);
    }

    [Fact]
    public void KeysSlugsAndBatchIdsFollowTheirRules()
    {
        Assert.Equal("step-back", IconPlan.Slug("Step back"));
        Assert.Equal("propose-end-the-phase", IconPlan.Slug("Propose: end the phase!"));
        Assert.True(IconPlan.IsKey("zoom-in"));
        Assert.False(IconPlan.IsKey("Zoom in"));
        Assert.False(IconPlan.IsKey("zoom--in"));

        var component = new ComponentCoverage("Play", "Components/Pages/Play.razor", [], [], []);
        Assert.Equal("Play#play-map-fit", IconPlan.ControlKey(component, new ControlCoverage("button", "Fit", false, "play-map-fit", null, IconCoverage.Needed)));
        Assert.Equal("Play:Stop picking", IconPlan.ControlKey(component, new ControlCoverage("button", "Stop picking", false, null, null, IconCoverage.Needed)));
        Assert.Null(IconPlan.ControlKey(component, new ControlCoverage("button", "@label", true, null, null, IconCoverage.Needed)));

        var day = new DateOnly(2026, 10, 17);
        var plan = Plan().WithBatch(new IconBatch("2026-10-17-1", "One", [], string.Empty, "2026-10-17"));
        Assert.Equal("2026-10-17-2", plan.NextBatchId(day));
    }

    [Fact]
    public void TheStoreSavesEachChangeToTheFileAndReadsItBack()
    {
        var store = new IconPlanStore(Path.Combine(scratch.FullName, IconPlan.File));
        Assert.Same(IconPlan.Empty, store.Load());

        store.Update(plan => plan with { Style = Style });
        store.Update(plan => plan.WithBrief(Brief("step-back", "a bar and an arrowhead")).WithAssignment("ReplayTransport#replay-step-back", "step-back"));
        store.Update(plan => plan.WithAssignment("Play#propose-advance", IconLibrary.TextByDesign).WithReference("step-back", " top row 3rd "));
        var saved = store.Update(plan => plan.WithBatch(new IconBatch("2026-10-17-1", "Replay", ["step-back"], string.Empty, "2026-10-17")));

        var read = store.Load();
        Assert.Equal(saved.Write(), read.Write());
        Assert.Equal("step-back", read.IconOf("ReplayTransport#replay-step-back"));
        Assert.Equal(IconLibrary.TextByDesign, read.IconOf("Play#propose-advance"));
        Assert.Equal("top row 3rd", read.Brief("step-back")!.Reference);
        Assert.True(read.InBatch("step-back"));

        Assert.Null(store.Update(plan => plan.WithAssignment("Play#propose-advance", null)).IconOf("Play#propose-advance"));
        Assert.False(File.Exists(store.FilePath + ".tmp"));
    }

    [Fact]
    public void TheCommittedPlanBriefsEveryDrawnIconAndItsBatchesNameOnlyBriefs()
    {
        var root = factory.Services.GetRequiredService<IWebHostEnvironment>().WebRootPath;
        var plan = new IconPlanStore(Path.Combine(root, IconLibrary.Folder, IconPlan.File)).Load();
        var icons = factory.Services.GetRequiredService<IconLibrary>();

        Assert.All(icons.Icons, icon => Assert.NotNull(plan.Brief(icon.Key)));
        Assert.All(plan.Batches, batch => Assert.All(batch.Icons, key => Assert.NotNull(plan.Brief(key))));
        Assert.All(plan.Batches, batch => Assert.NotNull(plan.Prompt(batch)));
        Assert.StartsWith("set of 4 flat vector icons, 2×2 grid", plan.Style.Opening, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("coverage", "id=\"icon-lab-coverage\"")]
    [InlineData("prompts", "id=\"icon-bench-copy\"")]
    [InlineData("batches", "data-batch=\"2026-10-17-1\"")]
    public async Task EachTabOpensFromItsAddress(string tab, string marker)
    {
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync(new Uri($"/lab/icons?tab={tab}", UriKind.Relative));
        Assert.Contains(marker, html, StringComparison.Ordinal);
        Assert.Contains($"id=\"icon-lab-tab-{tab}\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-selected=\"true\"", html[html.IndexOf($"id=\"icon-lab-tab-{tab}\"", StringComparison.Ordinal)..], StringComparison.Ordinal);
    }

    [Fact]
    public void CoverageGivesAnOpenControlAnIconItsWordsOrABriefAndKeepsCheckedOnesWordsAtOnce()
    {
        var coverage = new IconCoverage([new ComponentCoverage("ReplayTransport", "Components/Replay/ReplayTransport.razor", ["/games/replay"], ["Replay"],
        [
            new ControlCoverage("button", "Step back", false, "replay-step-back", null, IconCoverage.Needed),
            new ControlCoverage("button", "Play", false, "replay-play", null, IconCoverage.Needed),
            new ControlCoverage("button", "Step forward", false, "replay-step-forward", null, IconCoverage.Needed),
        ])]);
        var assigned = new List<(string Control, string? Icon)>();
        (IconBrief Brief, string Control)? briefed = null;
        var view = context.Render<IconCoverageView>(parameters => parameters
            .Add(component => component.Coverage, coverage)
            .Add(component => component.Icons, EmptyLibrary())
            .Add(component => component.Plan, Plan())
            .Add(component => component.OnAssign, (IReadOnlyList<(string Control, string? Icon)> decisions) => assigned.AddRange(decisions))
            .Add(component => component.OnBrief, ((IconBrief Brief, string Control) made) => briefed = made));

        Assert.Contains("3 still open", view.Find("#icon-lab-coverage-summary").TextContent, StringComparison.Ordinal);

        // A new icon: the brief starts from the control's label.
        Row(view, "ReplayTransport#replay-step-back").QuerySelectorAll("button").First(button => button.TextContent == "New icon").Click();
        Assert.Equal("step-back", view.Find("#coverage-brief-key").GetAttribute("value"));
        view.Find("#coverage-brief-brief").Input("a bar and a single arrowhead pointing left, step back");
        view.FindAll("#coverage-brief button").First(button => button.TextContent == "Save brief").Click();
        Assert.Equal(("step-back", "ReplayTransport#replay-step-back"), (briefed!.Value.Brief.Key, briefed.Value.Control));

        // Words for the two checked at once.
        Row(view, "ReplayTransport#replay-play").QuerySelector("input[type=checkbox]")!.Change(true);
        Row(view, "ReplayTransport#replay-step-forward").QuerySelector("input[type=checkbox]")!.Change(true);
        view.FindAll("button").First(button => button.TextContent.StartsWith("Keep words for the 2 checked", StringComparison.Ordinal)).Click();
        Assert.Equal([("ReplayTransport#replay-play", IconLibrary.TextByDesign), ("ReplayTransport#replay-step-forward", IconLibrary.TextByDesign)], assigned);
    }

    [Fact]
    public void TheBenchFillsItsSlotsFromOneComponentMakesThePromptAndSavesTheBatch()
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var plan = Plan(Brief("step-back", "a bar and an arrowhead left"), Brief("play", "a triangle right"), Brief("step-forward", "an arrowhead and a bar right"),
                Brief("to-end", "two arrowheads and a bar right"), Brief("lonely", "a lone thing"))
            .WithAssignment("ReplayTransport#replay-step-back", "step-back").WithAssignment("ReplayTransport#replay-play", "play")
            .WithAssignment("ReplayTransport#replay-step-forward", "step-forward").WithAssignment("ReplayTransport#replay-to-end", "to-end");
        IconBatch? saved = null;
        var bench = new IconBench();
        var view = context.Render<IconPromptBench>(parameters => parameters
            .Add(component => component.Plan, plan)
            .Add(component => component.Icons, EmptyLibrary())
            .Add(component => component.Bench, bench)
            .Add(component => component.OnSaveBatch, (IconBatch batch) => saved = batch));

        view.Find("#icon-bench-copy").Click();
        Assert.Contains("Fill all four slots first", view.Markup, StringComparison.Ordinal);

        view.Find("#icon-bench-fill").Click();
        Assert.Equal(["step-back", "play", "step-forward", "to-end"], bench.Filled);
        Assert.Equal("ReplayTransport", bench.Name);
        Assert.Contains("Icons: 1. a bar and an arrowhead left; 2. a triangle right; 3. an arrowhead and a bar right; 4. two arrowheads and a bar right.",
            view.Find("#icon-bench-prompt").TextContent, StringComparison.Ordinal);

        view.Find("#icon-bench-save").Click();
        Assert.Equal(["step-back", "play", "step-forward", "to-end"], saved!.Icons);
        Assert.Equal("ReplayTransport", saved.Name);
        Assert.Equal(saved.Id, bench.BatchId);
    }

    [Fact]
    public void TheBenchInOneIconModeHoldsOneBriefNamesTheBatchAfterItAndSavesASingleBatch()
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var plan = Plan(Brief("proposal", "a clipboard with a checkmark box") with
        {
            Label = "Proposal"
        }, Brief("other", "something else") with
        {
            Label = "Other"
        })
            .WithAssignment("Play#proposal", "proposal");
        IconBatch? saved = null;
        var bench = new IconBench();
        bench.Add("other");
        var view = context.Render<IconPromptBench>(parameters => parameters
            .Add(component => component.Plan, plan)
            .Add(component => component.Icons, EmptyLibrary())
            .Add(component => component.Bench, bench)
            .Add(component => component.OnSaveBatch, (IconBatch batch) => saved = batch));

        // Switching to one icon keeps the first brief, shows one slot with no fill from a component, and names the batch after the icon.
        view.Find("#icon-bench-single").Change(true);
        Assert.True(bench.OneIcon);
        Assert.Equal(["other"], bench.Filled);
        Assert.Single(view.FindAll(".icon-bench-slot"));
        Assert.Empty(view.FindAll("#icon-bench-fill"));
        Assert.Equal("Other", bench.Name);
        Assert.StartsWith("a single flat vector icon. Icon: something else.", view.Find("#icon-bench-prompt").TextContent, StringComparison.Ordinal);

        // The one slot is full until its brief is removed; the next brief takes it, and the name follows.
        Assert.False(bench.Add("proposal"));
        view.FindAll(".icon-bench-slot button").First(button => button.TextContent == "Remove").Click();
        view.FindAll(".icon-bench-queue li").First(item => item.TextContent.Contains("proposal", StringComparison.Ordinal)).QuerySelector("button")!.Click();
        Assert.Equal("Proposal", bench.Name);

        view.Find("#icon-bench-save").Click();
        Assert.Equal(["proposal"], saved!.Icons);
        Assert.True(saved.OneIcon);
        Assert.Equal("Proposal", saved.Name);
    }

    [Fact]
    public void ASavedBatchIsARecordRenamingKeepsItButNewSlotsOrAModeSaveANewBatch()
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var tabs = new IconBatch("2026-10-17-2", "Inspector tabs", ["selection", "los", "range", "evidence"], string.Empty, "2026-10-17");
        var plan = Plan(Brief("selection", "a hexagon and a pointer"), Brief("los", "an eye"), Brief("range", "a row of hexes"), Brief("evidence", "a book"))
            .WithBatch(tabs);
        var saves = new List<IconBatch>();
        var bench = new IconBench();
        bench.Load(tabs);
        var view = context.Render<IconPromptBench>(parameters => parameters
            .Add(component => component.Plan, plan)
            .Add(component => component.Icons, EmptyLibrary())
            .Add(component => component.Bench, bench)
            .Add(component => component.OnSaveBatch, (IconBatch batch) => saves.Add(batch)));
        Assert.Equal("Batch 2026-10-17-2", view.Find("#icon-bench-batch-title").TextContent);
        Assert.Equal("Save the name", view.Find("#icon-bench-save").TextContent);

        // A new name alone renames the record.
        view.Find("#icon-bench-name").Input("Inspector tab strip");
        view.Find("#icon-bench-save").Click();
        Assert.Equal(("2026-10-17-2", "Inspector tab strip"), (saves[^1].Id, saves[^1].Name));

        // One icon changes what is sent: the title says so, and saving makes a new batch named after the icon, leaving 2026-10-17-2 alone.
        view.Find("#icon-bench-single").Change(true);
        Assert.Equal("New batch, from 2026-10-17-2", view.Find("#icon-bench-batch-title").TextContent);
        Assert.Contains("2026-10-17-2 stays as it was sent", view.Find("#icon-bench-record").TextContent, StringComparison.Ordinal);
        view.Find("#icon-bench-save").Click();
        Assert.NotEqual("2026-10-17-2", saves[^1].Id);
        Assert.Equal(("selection", true), (saves[^1].Icons.Single(), saves[^1].OneIcon));
        Assert.Equal(saves[^1].Id, bench.BatchId);
        Assert.Contains("2026-10-17-2 stays as it was", view.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ASlotShowsWhereItsIconGoesTheIconsBesideItAndTheResultToMatch()
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var coverage = new IconCoverage(
        [
            new ComponentCoverage("BoardInspector", "Components/Board/BoardInspector.razor", ["/games/play", "/games/replay"], ["BoardWorkspace"],
            [
                new ControlCoverage("tab", "Selection", false, "selection", "selection", IconCoverage.HasIcon, "BoardInspector tabs"),
                new ControlCoverage("tab", "LOS", false, "los", "los", IconCoverage.HasIcon, "BoardInspector tabs"),
            ]),
            new ComponentCoverage("Play", "Components/Pages/Play.razor", ["/games/play"], [],
                [new ControlCoverage("tab", "Proposal", false, "proposal", null, IconCoverage.Needed, "BoardInspector tabs", "BoardInspector")]),
            new ComponentCoverage("Replay", "Components/Pages/Replay.razor", ["/games/replay"], [],
                [new ControlCoverage("tab", "Step", false, "step", null, IconCoverage.Needed, "BoardInspector tabs", "BoardInspector")]),
        ]);
        var plan = Plan(Brief("proposal", "a clipboard"), Brief("selection", "a hexagon") with
        {
            Reference = "Set Two, top row 3rd"
        }, Brief("los", "an eye") with
        {
            Reference = "Set Two, bottom row 3rd"
        })
            .WithAssignment("Play#proposal", "proposal")
            .WithBatch(new IconBatch("2026-10-17-2", "Inspector tabs", ["selection", "los"], string.Empty, "2026-10-17"));

        var placement = IconPlacement.Find(coverage, "Play#proposal")!;
        Assert.Equal("BoardInspector", placement.Home);
        Assert.Equal(["Selection", "LOS"], placement.Neighbours.Select(neighbour => neighbour.Label));
        var source = Assert.Single(placement.StyleSources(plan));
        Assert.Equal("2026-10-17-2", source.Batch.Id);
        Assert.Equal(["Set Two, top row 3rd", "Set Two, bottom row 3rd"], source.References);

        var bench = new IconBench { OneIcon = true };
        bench.Add("proposal");
        var view = context.Render<IconPromptBench>(parameters => parameters
            .Add(component => component.Plan, plan)
            .Add(component => component.Icons, EmptyLibrary())
            .Add(component => component.Coverage, coverage)
            .Add(component => component.Bench, bench));
        var where = view.Find(".icon-bench-slot .icon-bench-where").TextContent;
        Assert.Contains("Play#proposal", where, StringComparison.Ordinal);
        Assert.Contains("shown in BoardInspector", where, StringComparison.Ordinal);
        Assert.Contains("Selection", where, StringComparison.Ordinal);
        Assert.DoesNotContain("Step", where, StringComparison.Ordinal);
        Assert.Contains("Inspector tabs (2026-10-17-2): Set Two, top row 3rd; Set Two, bottom row 3rd", view.Find(".icon-bench-match").TextContent, StringComparison.Ordinal);
    }

    /// <summary>A library with no icons and no coverage.</summary>
    private IconLibrary EmptyLibrary() => new(provider ??= new PhysicalFileProvider(scratch.FullName));

    private static AngleSharp.Dom.IElement Row(IRenderedComponent<IconCoverageView> view, string control) => view.Find($"tr[data-control='{control}']");
}
