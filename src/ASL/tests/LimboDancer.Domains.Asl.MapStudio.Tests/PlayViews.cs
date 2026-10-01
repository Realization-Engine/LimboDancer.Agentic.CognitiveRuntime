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
}
