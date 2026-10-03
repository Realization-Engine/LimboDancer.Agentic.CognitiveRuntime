using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Tools.NamePools;

internal static partial class Program
{
    private static readonly string[] SecondFields = ["familyNames", "patronymics", "communityNames"];

    private static int Main(string[] args)
    {
        try
        {
            Require(args.Length == 1, "Supply the name-pool directory.");
            Validate(Path.GetFullPath(args[0]));
            return 0;
        }
        catch (Exception error) when (error is InvalidDataException or IOException or JsonException or KeyNotFoundException or InvalidOperationException or FormatException or ArgumentException)
        {
            Console.Error.WriteLine($"Name-pool validation failed: {error.Message}");
            return 1;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }

    private static string Text(JsonElement value, string key) => value.GetProperty(key).GetString() ?? throw new InvalidDataException($"Missing string: {key}");
    private static JsonElement[] Items(JsonElement value, string key) => value.TryGetProperty(key, out var items) ? [.. items.EnumerateArray()] : [];
    private static string[] Strings(JsonElement value, string key) => [.. Items(value, key).Select(item => item.GetString() ?? throw new InvalidDataException($"Null string in {key}"))];
    private static string Normalize(string value) => value.Normalize(NormalizationForm.FormC).ToUpperInvariant();

    private static JsonElement Read(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        CheckProperties(document.RootElement);
        return document.RootElement.Clone();
    }

    private static void CheckProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                Require(keys.Add(property.Name), $"Duplicate JSON property: {property.Name}");
                CheckProperties(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                CheckProperties(item);
            }
        }
    }

    private static Dictionary<string, JsonElement> Index(JsonElement[] items, string label)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var id = Text(item, "id");
            Require(Identifier().IsMatch(id), $"{label}: invalid id {id}");
            Require(result.TryAdd(id, item), $"{label}: duplicate id {id}");
        }
        return result;
    }

    private static void Version(JsonElement value, string label) => Require(SupportedVersion().IsMatch(Text(value, "version")), $"{label}: unsupported version; expected 1.x.y");

    private static void Validate(string root)
    {
        var manifest = Read(Path.Combine(root, "manifest.json"));
        Require(Text(manifest, "format") == "asl-name-manifest", "Unexpected manifest format");
        Version(manifest, "manifest");
        var sources = Index(Items(manifest, "sources"), "sources");
        var registered = Index(Items(manifest, "pools"), "pools");
        var contexts = Index(Items(manifest, "nationalityContexts"), "contexts");
        var forces = Index(Items(manifest, "forceAffiliations"), "forces");
        var groups = Index(Items(manifest, "rulesGroups"), "groups");
        Require(registered.Count > 0, "No pools registered");
        var files = registered.Values.Select(item => Text(item, "file")).ToHashSet(StringComparer.Ordinal);
        Require(files.Count == registered.Count, "A pool file is registered twice");
        Require(files.SetEquals(Directory.EnumerateFiles(root, "*.names.json").Select(path => Path.GetFileName(path))), "Pool files differ from manifest");
        var totalGiven = 0;
        var totalFamily = 0;
        var totalOther = 0;
        var shortfalls = new List<string>();
        foreach (var (key, registration) in registered)
        {
            var filename = Text(registration, "file");
            Require(filename == key + ".names.json", $"{key}: unsafe or inconsistent filename");
            var path = Path.Combine(root, filename);
            var pool = Read(path);
            Require(Text(pool, "id") == key && Text(pool, "format") == "asl-name-pool", $"{key}: inconsistent id or format");
            Version(pool, key);
            Require(Text(pool, "version") == Text(registration, "version"), $"{key}: version mismatch");
            Require(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))) == Text(registration, "sha256"), $"{key}: SHA-256 mismatch; update manifest after review");
            Require(Text(pool, "reviewStatus") == Text(registration, "reviewStatus"), $"{key}: review state mismatch");
            var sourceIds = Strings(pool.GetProperty("provenance"), "sourceIds");
            Require(sourceIds.Length > 0 && sourceIds.All(sources.ContainsKey), $"{key}: unknown source");
            var years = pool.GetProperty("intendedScenarioYears");
            Require(years.GetProperty("from").GetInt32() <= years.GetProperty("to").GetInt32(), $"{key}: reversed dates");
            var subgroups = Index(Items(pool, "subgroups"), key + " subgroups").Keys.ToHashSet(StringComparer.Ordinal);
            Require((subgroups.Count > 0) == pool.GetProperty("requiresSubgroup").GetBoolean(), $"{key}: subgroup requirement mismatch");
            var componentIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in new[] { "givenNames", "familyNames", "patronymics", "communityNames" })
            {
                var values = new HashSet<string>(StringComparer.Ordinal);
                foreach (var entry in Items(pool, field))
                {
                    var id = Text(entry, "id");
                    Require(Identifier().IsMatch(id) && componentIds.Add(id), $"{key}: bad or duplicate component id");
                    var name = Text(entry, "text");
                    Require(name.Length > 0 && name == name.Trim() && name.IsNormalized(NormalizationForm.FormC), $"{key}: blank, untrimmed or non-NFC name");
                    Require(!name.EnumerateRunes().Any(rune => Rune.GetUnicodeCategory(rune) is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate or UnicodeCategory.PrivateUse or UnicodeCategory.OtherNotAssigned), $"{key}: control or unsupported character");
                    Require(values.Add(Normalize(name)), $"{key}: duplicate {field} text: {name}");
                    Require(entry.GetProperty("weight").TryGetInt32(out var weight) && weight > 0, $"{key}: invalid weight");
                    var tags = Strings(entry, "subgroupIds");
                    Require(tags.Distinct(StringComparer.Ordinal).Count() == tags.Length && tags.All(subgroups.Contains), $"{key}: invalid subgroup tag");
                }
            }
            var given = Items(pool, "givenNames");
            var family = Items(pool, "familyNames");
            Require(given.Length > 0, $"{key}: empty given-name list");
            var convention = pool.GetProperty("convention");
            var role = Text(convention, "secondComponentRole");
            var secondField = role switch
            {
                "family" => "familyNames",
                "father-given" => "patronymics",
                "community" => "communityNames",
                _ => throw new InvalidDataException($"{key}: unknown second component role")
            };
            var second = Items(pool, secondField);
            Require(second.Length > 0, $"{key}: missing second components");
            Require(SecondFields.Where(field => field != secondField).All(field => Items(pool, field).Length == 0), $"{key}: conflicting second component collections");
            var expected = Text(convention, "id") switch
            {
                "given-family" => (Role: "family", Order: new[] { "given", "family" }),
                "family-given" => (Role: "family", Order: new[] { "family", "given" }),
                "given-father" => (Role: "father-given", Order: new[] { "given", "father-given" }),
                "given-community" => (Role: "community", Order: new[] { "given", "community" }),
                _ => throw new InvalidDataException($"{key}: unsupported convention")
            };
            Require(role == expected.Role && Strings(convention, "componentOrder").SequenceEqual(expected.Order), $"{key}: component order/role mismatch");
            var actual = new Dictionary<string, int>(StringComparer.Ordinal) { ["givenNames"] = given.Length, ["familyNames"] = family.Length, ["secondComponent"] = second.Length };
            CheckCounts(pool.GetProperty("actualCounts"), actual, key);
            CheckCounts(registration.GetProperty("actualCounts"), actual, key);
            var target = pool.GetProperty("targetCounts");
            var reached = given.Length >= target.GetProperty("givenNames").GetInt32() && family.Length >= target.GetProperty("familyNames").GetInt32() && second.Length >= (target.TryGetProperty("secondComponent", out var secondTarget) ? secondTarget : target.GetProperty("familyNames")).GetInt32();
            var completeness = reached ? "target-met" : "smaller-curated-pool";
            Require(Text(pool, "completeness") == completeness && Text(registration, "completeness") == completeness, $"{key}: false completion claim");
            if (!reached)
            {
                shortfalls.Add(key);
            }
            var capacities = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var subgroup in subgroups.Count > 0 ? subgroups.ToArray() : new[] { "all" })
            {
                var firsts = given.Where(entry => !entry.TryGetProperty("subgroupIds", out _) || Strings(entry, "subgroupIds").Contains(subgroup, StringComparer.Ordinal)).ToArray();
                var lasts = second.Where(entry => subgroups.Count == 0 || Strings(entry, "subgroupIds").Contains(subgroup, StringComparer.Ordinal)).ToArray();
                Require(firsts.Length > 0 && lasts.Length > 0, $"{key}: empty subgroup {subgroup}");
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var first in firsts)
                {
                    foreach (var last in lasts)
                    {
                        var a = Text(first, "text");
                        var b = Text(last, "text");
                        if (Normalize(a) != Normalize(b))
                        {
                            names.Add(Normalize(string.Join(Text(convention, "separator"), expected.Order[0] == "given" ? new[] { a, b } : new[] { b, a })));
                        }
                    }
                }
                capacities[subgroup] = names.Count;
            }
            CheckCounts(pool.GetProperty("availableCombinations"), capacities, key);
            Require(subgroups.Count == 0 || second.All(entry => Strings(entry, "subgroupIds").Length > 0), $"{key}: unclassified second component");
            totalGiven += given.Length;
            totalFamily += family.Length;
            totalOther += role == "family" ? 0 : second.Length;
        }
        var usedPools = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (key, context) in contexts)
        {
            var choices = Strings(context, "poolIds");
            Require(choices.Length > 0 && choices.Distinct(StringComparer.Ordinal).Count() == choices.Length && choices.All(registered.ContainsKey), $"{key}: invalid pool choices");
            usedPools.UnionWith(choices);
            var defaultPool = context.GetProperty("defaultPoolId").GetString();
            Require(defaultPool is null || choices.Contains(defaultPool, StringComparer.Ordinal), $"{key}: default not in choices");
            Require((Text(context, "selection") == "explicit-pool") == (defaultPool is null), $"{key}: ambiguous default");
            Require(choices.Length == 1 || defaultPool is null, $"{key}: multi-pool context silently defaults");
        }
        Require(usedPools.SetEquals(registered.Keys), "Unreachable pools");
        foreach (var (key, force) in forces)
        {
            Require(contexts.TryGetValue(Text(force, "nationalityContextId"), out var context), $"{key}: missing context");
            Require(Strings(context, "rulesNationalityContexts").Contains(Text(force, "rulesNationalityContext"), StringComparer.Ordinal), $"{key}: incompatible rules context");
        }
        foreach (var (key, group) in groups)
        {
            Require(Strings(group, "nationalityContextIds").All(contexts.ContainsKey), $"{key}: missing group context");
        }
        var policy = manifest.GetProperty("poolSelectionPolicy");
        Require(!policy.GetProperty("allowSilentFallback").GetBoolean(), "Silent fallback must remain disabled");
        Require(Text(policy, "supportWeapons") == "no-personnel-assignment", "Support weapons must remain unnamed");
        Require(Text(policy, "authoredNames") == "preserve", "Authored names must be preserved");
        Console.WriteLine($"Validated {registered.Count} pools, {contexts.Count} nationality contexts, {forces.Count} force affiliations.");
        Console.WriteLine($"Components: {totalGiven} given names, {totalFamily} family names, {totalOther} other second components.");
        Console.WriteLine($"Smaller pools: {string.Join(", ", shortfalls)}. Historical review is not established by validation.");
    }

    private static void CheckCounts(JsonElement declared, Dictionary<string, int> actual, string label)
    {
        Require(declared.EnumerateObject().Count() == actual.Count && actual.All(pair => declared.TryGetProperty(pair.Key, out var count) && count.GetInt32() == pair.Value), $"{label}: count or capacity mismatch");
    }

    [GeneratedRegex(@"\A[a-z][a-z0-9-]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex Identifier();

    // Major identifies the format contract; minor/patch identify compatible data revisions.
    [GeneratedRegex(@"\A1\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\z", RegexOptions.CultureInvariant)]
    private static partial Regex SupportedVersion();
}
