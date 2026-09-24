namespace LimboDancer.Domains.Asl.Maps.Composition;

/// <summary>The kinds of LOS scenario-specific rule in VASL's <c>SharedBoardMetadata.xml</c> (<c>LOSSSRule</c> types).</summary>
public enum LosSsRuleKind
{
    /// <summary>A valid SSR that does not change LOS data (<c>ignore</c>).</summary>
    Ignore,

    /// <summary>Handled by code in <c>VASLBoard</c> (<c>customCode</c>).</summary>
    CustomCode,

    /// <summary>Every cell of one terrain becomes another (<c>terrainMap</c>).</summary>
    TerrainMap,

    /// <summary>Every cell at one elevation moves to another (<c>elevationMap</c>).</summary>
    ElevationMap,

    /// <summary>Cells of a terrain take an elevation and become open ground (<c>terrainToElevationMap</c>).</summary>
    TerrainToElevationMap,

    /// <summary>Cells at an elevation drop to level 0 and take a terrain unless they are LOS obstacles (<c>elevationToTerrainMap</c>).</summary>
    ElevationToTerrainMap,

    /// <summary>Cells of a terrain at one elevation become open ground (<c>terrainToSelectElevationMap</c>).</summary>
    TerrainToSelectElevationMap,
}

/// <summary>One LOS scenario-specific rule: its name, kind, and from and to values as written.</summary>
public sealed record LosSsRule(string Name, LosSsRuleKind Kind, string FromValue, string ToValue);

/// <summary>
/// VASL's LOS scenario-specific rules by name (<c>AbstractMetadata.LOSSSRules</c>). As in VASL's map, a later rule with
/// the same name replaces the earlier one.
/// </summary>
public sealed class LosSsRuleSet
{
    private readonly Dictionary<string, LosSsRule> byName = new(StringComparer.Ordinal);
    private readonly List<string> order = [];

    public LosSsRuleSet(IEnumerable<LosSsRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        foreach (var rule in rules)
        {
            if (!byName.ContainsKey(rule.Name))
            {
                order.Add(rule.Name);
            }

            byName[rule.Name] = rule;
        }
    }

    public static LosSsRuleSet Empty { get; } = new([]);

    /// <summary>The rules in document order of their first occurrence.</summary>
    public IReadOnlyList<LosSsRule> Rules => order.Select(name => byName[name]).ToArray();

    public int Count => order.Count;

    public bool TryGet(string name, out LosSsRule rule) => byName.TryGetValue(name, out rule!);

    public LosSsRule this[string name] => byName[name];
}
