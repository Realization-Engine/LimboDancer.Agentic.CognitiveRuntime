using Bunit;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Choosing a view on the Play page (pass 23, ruling R23.2): a new view waits behind the hand-over screen until its viewer confirms being at the screen.
/// </summary>
internal static class PlayViews
{
    public static void ViewAs(this IRenderedComponent<PlayPage> page, string view)
    {
        page.Find("#play-perspective").Change(view);
        if (page.FindAll("#play-handover-confirm") is [var confirm])
        {
            confirm.Click();
        }
    }

    /// <summary>A record's own text, without the link to its step on the Replay page that follows it (pass 31b).</summary>
    public static string RecordText(this AngleSharp.Dom.IElement record) =>
        string.Concat(record.ChildNodes.Where(node => node is not AngleSharp.Dom.IElement { ClassName: "record-replay" }).Select(node => node.TextContent)).Trim();

    /// <summary>Opens a game from the picker; a game opened waits behind the hand-over (pass 28c), which its first viewer confirms here.</summary>
    public static void OpenGame(this IRenderedComponent<PlayPage> page, string game)
    {
        page.Find("#play-game").Change(game);
        if (page.FindAll("#play-handover-confirm") is [var confirm])
        {
            confirm.Click();
        }
    }

    /// <summary>The DEFENDER passes from its own view (pass 28c; A8.1, A8.11); the screen then returns to the view shown before.</summary>
    public static void PassAs(this IRenderedComponent<PlayPage> page, string defender, Action<IRenderedComponent<PlayPage>, string> commit)
    {
        var shown = page.Find("#play-perspective").GetAttribute("value")!;
        page.ViewAs(defender);
        commit(page, "#propose-pass");
        page.ViewAs(shown);
    }

    /// <summary>
    /// Ends the phase (pass 31, ruling R31.6): the page offers the phase end to the side that ends it, so a test that plays from one view ends a
    /// phase another side ends from that side's view, and the screen then returns to the view shown before.
    /// </summary>
    public static void EndPhase(this IRenderedComponent<PlayPage> page, Action<IRenderedComponent<PlayPage>, string> commit)
    {
        if (page.FindAll("#propose-advance").Count > 0)
        {
            commit(page, "#propose-advance");
            return;
        }

        var shown = page.Find("#play-perspective").GetAttribute("value")!;
        foreach (var view in page.FindAll("#play-perspective option").Select(option => option.GetAttribute("value")!).Where(value => value != shown).ToArray())
        {
            page.ViewAs(view);
            if (page.FindAll("#propose-advance").Count > 0)
            {
                commit(page, "#propose-advance");
                page.ViewAs(shown);
                return;
            }
        }

        page.ViewAs(shown);
        throw new InvalidOperationException("No view may end the phase.");
    }
}
