using LimboDancer.Domains.Asl.MapStudio.Components.Library;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The Board library's search and filters for one browser session (plan section 12.1), so they are as the user left them on return
/// from a viewer. Ephemeral UI state only (section 15): nothing here is game state.
/// </summary>
public sealed class LibraryViewState
{
    public LibraryFilterBar.Filter Filter { get; set; } = LibraryFilterBar.Filter.Default;
}
