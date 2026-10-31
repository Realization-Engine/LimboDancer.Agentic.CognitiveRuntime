using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The minigames in the rulebook's order (the user, 2026-10-31), for the lists a reader scans: by each bundle's first rule, its section number read as a
/// number and the digits after the point one by one, as the rulebook numbers them, so A7.7 comes before A10.531, and A10.531 before A10.6; the
/// registry itself keeps its ordinal order.
/// </summary>
public static partial class MinigameOrder
{
    private static readonly Lazy<IReadOnlyList<MinigameManifest>> Ordered = new(() =>
        [.. Minigames.All.OrderBy(bundle => Key(bundle.Rules.Count > 0 ? bundle.Rules[0] : bundle.Title), StringComparer.Ordinal).ThenBy(bundle => bundle.Id, StringComparer.Ordinal)]);

    /// <summary>Every minigame, in rule order.</summary>
    public static IReadOnlyList<MinigameManifest> All => Ordered.Value;

    /// <summary>A rule's sort key: the section number after the chapter letter padded to six, the rest as written, so "A7.7" sorts before "A10.531"
    /// and "A10.531" before "A10.6".</summary>
    public static string Key(string rule) => Section().Replace(rule, match => match.Groups[1].Value + match.Groups[2].Value.PadLeft(6, '0'), 1);

    [GeneratedRegex("^([A-Za-z]*)([0-9]+)")]
    private static partial Regex Section();
}
