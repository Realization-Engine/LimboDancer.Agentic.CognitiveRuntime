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

    public IEnumerable<string> Filled => Slots.OfType<string>();

    public bool Holds(string key) => Slots.Contains(key);

    /// <summary>Puts an icon in the first empty slot; false when every slot is full.</summary>
    public bool Add(string key)
    {
        var free = Array.IndexOf(Slots, null);
        if (free < 0 || Holds(key))
        {
            return false;
        }

        Slots[free] = key;
        return true;
    }

    public void Clear()
    {
        BatchId = null;
        Name = string.Empty;
        Sref = string.Empty;
        Array.Fill(Slots, null);
    }

    public void Load(IconBatch batch)
    {
        Clear();
        BatchId = batch.Id;
        Name = batch.Name;
        Sref = batch.Sref;
        for (var slot = 0; slot < Math.Min(IconPlan.Slots, batch.Icons.Count); slot++)
        {
            Slots[slot] = batch.Icons[slot];
        }
    }
}
