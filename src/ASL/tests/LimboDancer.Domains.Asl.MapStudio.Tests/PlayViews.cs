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
}
