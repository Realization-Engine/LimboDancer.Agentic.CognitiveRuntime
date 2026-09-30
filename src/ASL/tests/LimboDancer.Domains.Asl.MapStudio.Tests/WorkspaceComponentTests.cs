using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Board;
using LimboDancer.Domains.Asl.MapStudio.Components.Layout;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using Microsoft.AspNetCore.Components.Web;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>Pass 22d (plan sections 15.5, 16.6, and 17.1): the board viewport's lifecycle, the inspector tabs, and the editor's header.</summary>
public sealed class WorkspaceComponentTests : IDisposable
{
    private readonly BunitContext context = new();

    public void Dispose() => context.Dispose();

    [Fact]
    public async Task TheViewportMountsOnceCallsBackAndDisposesWhatItMade()
    {
        var module = context.JSInterop.SetupModule("./js/boardViewport.js");
        var viewportScript = module.SetupModule(invocation => invocation.Identifier == "create");
        viewportScript.Mode = JSRuntimeMode.Loose;
        var ready = 0;
        var clicks = new List<(double X, double Y)>();
        var units = new List<string>();
        var keys = new List<string>();
        var viewport = context.Render<BoardViewport>(parameters => parameters
            .Add(component => component.OnReady, () => ready++)
            .Add(component => component.OnClick, (x, y) => { clicks.Add((x, y)); return Task.CompletedTask; })
            .Add(component => component.OnUnitSelected, id => { units.Add(id); return Task.CompletedTask; })
            .Add(component => component.OnKey, key => { keys.Add(key); return Task.CompletedTask; }));

        // Mounting: the module is imported and one viewport is made for the host, then the page is told once.
        viewport.WaitForAssertion(() => Assert.True(viewport.Instance.IsReady));
        Assert.Equal(1, ready);
        Assert.Single(module.Invocations, invocation => invocation.Identifier == "create");
        viewport.Render();
        Assert.Single(module.Invocations, invocation => invocation.Identifier == "create");

        // Callbacks: the script calls the component, which calls the page's delegates; a callback left unset does nothing.
        await viewport.InvokeAsync(() => viewport.Instance.OnBoardClick(3.5, 7));
        await viewport.InvokeAsync(() => viewport.Instance.OnUnitClick("g1"));
        await viewport.InvokeAsync(() => viewport.Instance.OnBoardKey("Escape"));
        await viewport.InvokeAsync(() => viewport.Instance.OnBoardHover(1, 1));
        Assert.Equal([(3.5, 7.0)], clicks);
        Assert.Equal(["g1"], units);
        Assert.Equal(["Escape"], keys);

        // Commands go to the viewport the component made.
        await viewport.InvokeAsync(() => viewport.Instance.Highlight("0,0 1,0 1,1").AsTask());
        await viewport.InvokeAsync(() => viewport.Instance.Reset().AsTask());
        viewportScript.VerifyInvoke("highlight");
        viewportScript.VerifyInvoke("reset");

        // Disposal: the viewport is disposed, which removes its listeners, and later commands do nothing.
        await viewport.InvokeAsync(() => viewport.Instance.DisposeAsync().AsTask());
        viewportScript.VerifyInvoke("dispose");
        Assert.False(viewport.Instance.IsReady);
        await viewport.InvokeAsync(() => viewport.Instance.Reset().AsTask());
        Assert.Single(viewportScript.Invocations, invocation => invocation.Identifier == "reset");
    }

    [Fact]
    public void TheTabsMoveByKeyboardAndOnlyTheSelectedTabIsInTheTabOrder()
    {
        var selected = "selection";
        var tabs = context.Render<InspectorTabs>(parameters => parameters
            .Add(component => component.Tabs, [new InspectorTabs.Tab("selection", "Selection"), new("los", "LOS"), new("evidence", "Evidence", 2)])
            .Add(component => component.Selected, selected)
            .Add(component => component.SelectedChanged, value => selected = value)
            .AddChildContent("<p id=\"panel-body\">body</p>"));

        Assert.Equal("true", tabs.Find("#inspector-tab-selection").GetAttribute("aria-selected"));
        Assert.Equal("-1", tabs.Find("#inspector-tab-los").GetAttribute("tabindex"));
        Assert.Contains("Evidence (2)", tabs.Find("#inspector-tab-evidence").TextContent, StringComparison.Ordinal);
        Assert.Equal("inspector-tab-selection", tabs.Find("[role=tabpanel]").GetAttribute("aria-labelledby"));

        tabs.Find("#inspector-tab-selection").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("los", selected);
        tabs.Find("#inspector-tab-selection").KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.Equal("evidence", selected);
        tabs.Find("#inspector-tab-selection").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal("evidence", selected);
    }

    [Fact]
    public void AnLosLocationThatIsNotReadIsNotCalledUnanswered()
    {
        var panel = context.Render<LosPanel>(parameters => parameters
            .Add(component => component.Draft, new LosPanel.LosDraft("bd01:Z99:0", "bd01:E4:0", false))
            .Add(component => component.Result, new LosCheck("bd01:Z99:0", "bd01:E4:0", null, "not a location", null, null)));
        Assert.Equal("not read", panel.Find("#los-result .status-badge").TextContent);
        Assert.True(panel.Find("#los-source-selected").HasAttribute("disabled"));
    }

    [Fact]
    public void TheEditorHeaderSaysWhetherThereAreUnsavedChanges()
    {
        var saved = context.Render<BoardEditorHeader>(parameters => parameters
            .Add(component => component.Name, "Hill 621").Add(component => component.Board, BoardRef.Parse("ab-hill"))
            .Add(component => component.Version, new string('a', 40)).Add(component => component.Status, BoardStatus.AuthoredValid));
        Assert.Contains("saved", saved.Find("#editor-header .status-badge").TextContent, StringComparison.Ordinal);
        Assert.True(saved.Find("#editor-save").HasAttribute("disabled"));
        Assert.True(saved.Find("#editor-undo").HasAttribute("disabled"));

        var draft = context.Render<BoardEditorHeader>(parameters => parameters
            .Add(component => component.Name, "Hill 621").Add(component => component.Board, BoardRef.Parse("ab-hill"))
            .Add(component => component.Version, new string('a', 40)).Add(component => component.Dirty, true).Add(component => component.IsDraft, true)
            .Add(component => component.UndoDescription, "add woods"));
        Assert.Contains("unsaved changes", draft.Find("#editor-header .status-badge").TextContent, StringComparison.Ordinal);
        Assert.False(draft.Find("#editor-save").HasAttribute("disabled"));
        Assert.Equal("Save draft", draft.Find("#editor-save").TextContent);
        Assert.Equal("add woods", draft.Find("#editor-undo").GetAttribute("title"));
    }
}
