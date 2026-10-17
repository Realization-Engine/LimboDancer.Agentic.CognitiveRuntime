namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// Where a planned icon will be seen: the control (its key, its component, and the coverage row), and the controls beside it in the same
/// strip. A strip's neighbours are its own members (those written in the component it renders in) and the other members its control's
/// component adds; so Proposal, which Play hands to BoardInspector, sits beside Selection, LOS, Range, and Evidence, not beside the tabs
/// Replay adds on another page.
/// </summary>
public sealed record IconPlacement(string Control, ComponentCoverage Component, ControlCoverage Item, IReadOnlyList<ControlCoverage> Neighbours)
{
    /// <summary>The component the control renders in: the one that writes it, or the one it is handed to.</summary>
    public string Home => Item.RendersIn ?? Component.Name;

    /// <summary>The placement of a control's key in the coverage, or null when no control has that key.</summary>
    public static IconPlacement? Find(IconCoverage coverage, string control)
    {
        foreach (var component in coverage.Components)
        {
            foreach (var item in component.Controls)
            {
                if (IconPlan.ControlKey(component, item) == control)
                {
                    return new IconPlacement(control, component, item, Beside(coverage, component, item));
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The Midjourney results that drew this strip's icons: each batch holding a neighbour's icon, with the references chosen for those
    /// icons; attach one as the style so the new icon matches them.
    /// </summary>
    public IReadOnlyList<(IconBatch Batch, IReadOnlyList<string> References)> StyleSources(IconPlan plan)
    {
        var drawn = Neighbours.Where(neighbour => neighbour.Status == IconCoverage.HasIcon && neighbour.Icon is not null).Select(neighbour => neighbour.Icon!).ToList();
        return plan.Batches
            .Select(batch => (Batch: batch, References: (IReadOnlyList<string>)batch.Icons.Where(drawn.Contains)
                .Select(key => plan.Brief(key)?.Reference ?? string.Empty).Where(reference => reference.Length > 0).Distinct(StringComparer.Ordinal).ToList()))
            .Where(source => source.References.Count > 0)
            .ToList();
    }

    private static List<ControlCoverage> Beside(IconCoverage coverage, ComponentCoverage component, ControlCoverage item)
    {
        if (item.Strip is not { } strip)
        {
            return [];
        }

        var home = item.RendersIn ?? component.Name;
        return
        [
            .. coverage.Components
                .Where(other => other.Name == home || other.Name == component.Name)
                .SelectMany(other => other.Controls)
                .Where(other => other.Strip == strip && !ReferenceEquals(other, item)),
        ];
    }
}
