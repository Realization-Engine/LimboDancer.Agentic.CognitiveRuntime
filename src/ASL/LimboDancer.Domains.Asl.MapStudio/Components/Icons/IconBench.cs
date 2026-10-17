using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio.Components.Icons;

/// <summary>
/// The batch on the Prompts bench, kept by the Icon Lab page so it outlives a switch of tabs: the saved batch it edits, if any, its
/// name, its four slots in Midjourney's 2x2 order, and its style reference.
/// </summary>
public sealed class IconBench
{
    public string? BatchId
    {
        get; set;
    }

    public string Name { get; set; } = string.Empty;

    public string?[] Slots { get; } = new string?[IconPlan.Slots];

    public string Sref { get; set; } = string.Empty;

    /// <summary>One icon, four tries: the batch prompts the first slot's brief alone.</summary>
    public bool OneIcon
    {
        get; set;
    }

    /// <summary>The slots this batch prompts: every filled one, or the first filled one for one icon.</summary>
    public IEnumerable<string> Filled => OneIcon ? Slots.OfType<string>().Take(1) : Slots.OfType<string>();

    /// <summary>How many briefs the prompt needs: four, or one.</summary>
    public int Needed => OneIcon ? 1 : IconPlan.Slots;

    public bool Holds(string key) => Filled.Contains(key);

    /// <summary>Puts an icon in the first empty slot; false when the batch is full (four slots, or one for one icon).</summary>
    public bool Add(string key)
    {
        var free = Array.IndexOf(Slots, null);
        if (free < 0 || Holds(key) || Filled.Count() >= Needed)
        {
            return false;
        }

        Slots[free] = key;
        return true;
    }

    /// <summary>
    /// Switches between a set of four and one icon; going to one icon keeps the first filled slot's brief in the first slot and empties
    /// the rest.
    /// </summary>
    public void SetOneIcon(bool oneIcon)
    {
        if (oneIcon && !OneIcon)
        {
            var first = Slots.OfType<string>().FirstOrDefault();
            Array.Fill(Slots, null);
            Slots[0] = first;
        }

        OneIcon = oneIcon;
    }

    public void Clear()
    {
        BatchId = null;
        Name = string.Empty;
        Sref = string.Empty;
        OneIcon = false;
        Array.Fill(Slots, null);
    }

    public void Load(IconBatch batch)
    {
        Clear();
        BatchId = batch.Id;
        Name = batch.Name;
        Sref = batch.Sref;
        OneIcon = batch.OneIcon;
        for (var slot = 0; slot < Math.Min(IconPlan.Slots, batch.Icons.Count); slot++)
        {
            Slots[slot] = batch.Icons[slot];
        }
    }
}
