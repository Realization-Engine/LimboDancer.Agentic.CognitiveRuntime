using System.Runtime.CompilerServices;
using Bunit;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 28c (R11): the Play map is drawn on B06 BoardViewport, whose script bUnit cannot run. These helpers set the script up and read the layers the
/// map panel sent it, so a test still checks what the view's map shows.
/// </summary>
internal static class PlayMaps
{
    private static readonly ConditionalWeakTable<BunitContext, BunitJSModuleInterop> Viewports = [];

    /// <summary>Sets up the viewport's script, and lets the page's other scripts (the workspace's focus) run as no-ops.</summary>
    public static void UseViewport(this BunitContext context)
    {
        // Pass 29: a proposal draws its busy state before the gate is asked, so its result arrives a render later; under a full run that can take
        // longer than the default second.
        BunitContext.DefaultWaitTimeout = TimeSpan.FromSeconds(15);
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = context.JSInterop.SetupModule("./js/boardViewport.js");
        var viewport = module.SetupModule(invocation => invocation.Identifier == "create");
        viewport.Mode = JSRuntimeMode.Loose;
        Viewports.AddOrUpdate(context, viewport);
    }

    /// <summary>The last text a command sent to the map (setUnits, setLos, setMarks, highlight); empty when it was never sent or was cleared.</summary>
    public static string MapLayer(this BunitContext context, string command) =>
        Viewports.TryGetValue(context, out var viewport) && viewport.Invocations.LastOrDefault(invocation => invocation.Identifier == command) is { } last
            ? (last.Arguments is { Count: > 0 } arguments ? arguments[0] as string : null) ?? string.Empty
            : string.Empty;

    /// <summary>Every layer the map shows now, the units first, as one text a test may search.</summary>
    public static string MapText(this BunitContext context) =>
        string.Concat(context.MapLayer("setUnits"), context.MapLayer("setLos"), context.MapLayer("setMarks"));

    /// <summary>The first element of the map's layers that matches a selector, such as "g[data-unit-id='g1']"; null when none does.</summary>
    public static AngleSharp.Dom.IElement? MapQuery(this BunitContext context, string selector) =>
        new AngleSharp.Html.Parser.HtmlParser().ParseDocument("<body>" + context.MapText() + "</body>").QuerySelector(selector);

    /// <summary>The map's source board, as the panel names it.</summary>
    public static string? MapSource<T>(this IRenderedComponent<T> page)
        where T : Microsoft.AspNetCore.Components.IComponent => page.Find("#play-panel-map").GetAttribute("data-source");
}
