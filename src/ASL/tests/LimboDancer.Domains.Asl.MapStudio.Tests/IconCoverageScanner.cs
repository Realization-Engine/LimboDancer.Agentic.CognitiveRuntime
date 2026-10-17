using System.Net;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Reads the Studio's Razor sources for the Icon Lab's coverage: each component's buttons and tabs, whether each draws an icon, and the
/// pages each component appears on. It reads the source as text, so a label worked out at run time shows as its source expression.
/// <list type="bullet">
/// <item>A button is a <c>&lt;button&gt;</c> element; it is a tab when it has <c>role="tab"</c>. It draws an icon when it holds an
/// <c>IconLabel</c>, and keeps its words by design when it has <c>data-icon="none"</c>. Its label is its words when they are literal,
/// else its literal aria-label, else its words as source, else its literal title; an id worked out at run time is left out.</item>
/// <item>A tab of <c>InspectorTabs</c> is a <c>Tab</c> record: <c>new InspectorTabs.Tab("id", "Title", ...)</c>, <c>ExtraTab(new("id",
/// "Title", ...))</c>, or <c>new("id", "Title", ...)</c> in a list declared as <c>IReadOnlyList&lt;InspectorTabs.Tab&gt;</c>. Its icon is
/// its <c>Icon:</c> argument; <c>Icon: "none"</c> keeps its words by design.</item>
/// </list>
/// </summary>
public static partial class IconCoverageScanner
{
    /// <summary>Files whose buttons are not the Studio's controls: the Icon Lab's samples, and the template that draws every Tab record.</summary>
    private static readonly HashSet<string> Skipped = new(StringComparer.Ordinal) { "IconGallery", "InspectorTabs" };

    private const int LongestLabel = 80;

    /// <summary>The inspector whose tab strip other components add tabs to with <c>BoardInspector.ExtraTab</c>.</summary>
    private const string Inspector = "BoardInspector";

    private const string InspectorStrip = "BoardInspector tabs";

    /// <summary>The coverage of every .razor file under a project folder, paths relative to it with forward slashes.</summary>
    public static IconCoverage Scan(string projectRoot)
    {
        var files = Directory.EnumerateFiles(Path.Combine(projectRoot, "Components"), "*.razor", SearchOption.AllDirectories)
            .Select(path => (Name: Path.GetFileNameWithoutExtension(path), Path: Path.GetRelativePath(projectRoot, path).Replace('\\', '/'),
                Text: WithoutComments(File.ReadAllText(path).ReplaceLineEndings("\n"))))
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToList();
        var names = files.Select(file => file.Name).ToHashSet(StringComparer.Ordinal);

        // Who places whom, and the routes of each page.
        var users = files.ToDictionary(file => file.Name, _ => new SortedSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        foreach (var file in files)
        {
            foreach (Match tag in ComponentTag().Matches(file.Text))
            {
                var used = tag.Groups["name"].Value;
                if (used != file.Name && names.Contains(used))
                {
                    users[used].Add(file.Name);
                }
            }
        }

        var routes = files.ToDictionary(file => file.Name, file => PageRoute().Matches(file.Text).Select(match => match.Groups["route"].Value).ToList(),
            StringComparer.Ordinal);

        var components = new List<ComponentCoverage>();
        foreach (var file in files.Where(file => !Skipped.Contains(file.Name)))
        {
            var controls = Controls(file.Name, file.Text);
            if (controls.Count > 0)
            {
                components.Add(new ComponentCoverage(file.Name, file.Path, PagesOf(file.Name, users, routes), [.. users[file.Name]], controls));
            }
        }

        return new IconCoverage(components);
    }

    private static List<string> PagesOf(string name, Dictionary<string, SortedSet<string>> users, Dictionary<string, List<string>> routes)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>([name]);
        var pages = new SortedSet<string>(StringComparer.Ordinal);
        while (pending.TryPop(out var next))
        {
            if (!seen.Add(next))
            {
                continue;
            }

            pages.UnionWith(routes[next]);
            foreach (var user in users[next])
            {
                pending.Push(user);
            }
        }

        return [.. pages];
    }

    private static List<ControlCoverage> Controls(string name, string text)
    {
        var found = new List<(int At, ControlCoverage Control)>();
        foreach (Match button in ButtonStart().Matches(text))
        {
            var attrsStart = button.Index + button.Length;
            var tagEnd = OpenTagEnd(text, attrsStart);
            var close = text.IndexOf("</button>", tagEnd, StringComparison.Ordinal);
            if (tagEnd < text.Length && close >= 0)
            {
                found.Add((button.Index, ButtonControl(text[attrsStart..tagEnd], text[(tagEnd + 1)..close], ContainerStrip(name, text, button.Index))));
            }
        }

        var tabIds = new HashSet<string>(StringComparer.Ordinal);
        void AddTab(int at, Match record, string strip, string? rendersIn = null)
        {
            if (tabIds.Add(record.Groups["id"].Value))
            {
                found.Add((at, TabControl(record, strip, rendersIn)));
            }
        }

        // Tabs in a declared list first, so each knows its list; a list in BoardInspector is the inspector's own strip.
        foreach (Match list in TabList().Matches(text))
        {
            var strip = name == Inspector ? InspectorStrip : $"{name} {list.Groups["list"].Value}";
            var body = list.Groups["body"];
            foreach (var record in ShortTabRecord().Matches(body.Value).Concat(TabRecord().Matches(body.Value)).OrderBy(record => record.Index))
            {
                AddTab(body.Index + record.Index, record, strip);
            }
        }

        // A tab handed to BoardInspector joins the inspector's own strip there.
        foreach (Match record in ExtraTabRecord().Matches(text))
        {
            AddTab(record.Index, record, InspectorStrip, Inspector);
        }

        foreach (Match record in TabRecord().Matches(text))
        {
            AddTab(record.Index, record, $"{name} tabs");
        }

        return [.. found.OrderBy(item => item.At).Select(item => item.Control)];
    }

    /// <summary>
    /// The strip a button sits in: the innermost toolbar, tab list, or group still open where the button starts, named by the component
    /// and the container's id, accessible name, or other class; the component's name alone when there is none.
    /// </summary>
    private static string ContainerStrip(string name, string text, int at)
    {
        var open = new Stack<Match>();
        foreach (Match tag in DivTag().Matches(text[..at]))
        {
            if (tag.Value.StartsWith("</", StringComparison.Ordinal))
            {
                open.TryPop(out _);
            }
            else
            {
                open.Push(tag);
            }
        }

        foreach (var div in open)
        {
            var attrs = div.Value;
            var classes = (Attribute(attrs, "class") ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var role = Attribute(attrs, "role");
            if (classes.Contains("toolbar") || role is "tablist" or "group")
            {
                var label = Literal(Attribute(attrs, "id")) ?? Literal(Attribute(attrs, "aria-label"))
                    ?? classes.FirstOrDefault(other => other != "toolbar" && !other.Contains('@', StringComparison.Ordinal)) ?? "toolbar";
                return $"{name} {label}";
            }
        }

        return name;
    }
    /// <summary>
    /// Where an opening tag's '>' is: outside an attribute's quotes, where a quote inside an attribute's <c>@(...)</c> (a lambda's string
    /// argument, a conditional's two strings) neither opens nor closes the attribute.
    /// </summary>
    private static int OpenTagEnd(string text, int from)
    {
        var quoted = false;
        var depth = 0;
        for (var at = from; at < text.Length; at++)
        {
            switch (text[at])
            {
                case '"' when depth == 0:
                    quoted = !quoted;
                    break;
                case '(' when quoted:
                    depth++;
                    break;
                case ')' when quoted && depth > 0:
                    depth--;
                    break;
                case '>' when !quoted:
                    return at;
            }
        }

        return text.Length;
    }

    private static ControlCoverage ButtonControl(string attrs, string inner, string strip)
    {
        var kind = Attribute(attrs, "role") == "tab" ? "tab" : "button";
        var id = Literal(Attribute(attrs, "id"));
        if (IconLabelTag().Match(inner) is { Success: true } iconLabel)
        {
            var icon = Attribute(iconLabel.Value, "Icon");
            var label = Attribute(iconLabel.Value, "Label") ?? string.Empty;
            return new ControlCoverage(kind, Shorten(label), IsDynamic(label), id, icon, IconCoverage.HasIcon, strip);
        }

        var inside = Words(inner);
        // A title is often an explanation, not a name, so it labels only a button with no words of its own.
        var words = Literal(inside) ?? Literal(Attribute(attrs, "aria-label")) ?? (inside.Length > 0 ? inside : Literal(Attribute(attrs, "title")) ?? inside);
        var status = Attribute(attrs, "data-icon") == IconLibrary.TextByDesign ? IconCoverage.Text : IconCoverage.Needed;
        return new ControlCoverage(kind, Shorten(words), IsDynamic(words), id, null, status, strip);
    }

    private static ControlCoverage TabControl(Match record, string strip, string? rendersIn)
    {
        var icon = TabIcon().Match(record.Groups["rest"].Value) is { Success: true } match ? match.Groups["icon"].Value : null;
        var status = icon is null ? IconCoverage.Needed : icon == IconLibrary.TextByDesign ? IconCoverage.Text : IconCoverage.HasIcon;
        return new ControlCoverage("tab", record.Groups["title"].Value, false, record.Groups["id"].Value, status == IconCoverage.HasIcon ? icon : null, status,
            strip, rendersIn);
    }

    private static string? Attribute(string tag, string name) =>
        Regex.Match(tag, $@"(?<![\w-]){Regex.Escape(name)}=""(?<value>[^""]*)""") is { Success: true } match ? match.Groups["value"].Value : null;

    private static string? Literal(string? value) => value is null || value.Contains('@', StringComparison.Ordinal) || value.Length == 0 ? null : value;

    private static string Words(string inner) => WebUtility.HtmlDecode(Whitespace().Replace(Tag().Replace(inner, " "), " ").Trim());

    private static bool IsDynamic(string label) => label.Contains('@', StringComparison.Ordinal) || label.Length == 0;

    private static string Shorten(string label) => label.Length <= LongestLabel ? label : label[..(LongestLabel - 3)] + "...";

    private static string WithoutComments(string text) => RazorComment().Replace(text, string.Empty);

    [GeneratedRegex(@"@\*.*?\*@", RegexOptions.Singleline)]
    private static partial Regex RazorComment();

    [GeneratedRegex(@"<button\b")]
    private static partial Regex ButtonStart();

    [GeneratedRegex(@"</?div\b(?:[^>""]|""[^""]*"")*>")]
    private static partial Regex DivTag();

    [GeneratedRegex(@"<IconLabel\b[^>]*>")]
    private static partial Regex IconLabelTag();

    [GeneratedRegex(@"<(?<name>[A-Z]\w*)[\s/>]")]
    private static partial Regex ComponentTag();

    [GeneratedRegex(@"^@page\s+""(?<route>[^""]+)""", RegexOptions.Multiline)]
    private static partial Regex PageRoute();

    [GeneratedRegex(@"InspectorTabs\.Tab\(\s*""(?<id>[^""]+)""\s*,\s*""(?<title>[^""]+)""(?<rest>[^)]*)\)")]
    private static partial Regex TabRecord();

    [GeneratedRegex(@"ExtraTab\(\s*new\s*\(\s*""(?<id>[^""]+)""\s*,\s*""(?<title>[^""]+)""(?<rest>[^)]*)\)")]
    private static partial Regex ExtraTabRecord();

    [GeneratedRegex(@"IReadOnlyList<InspectorTabs\.Tab>\s+(?<list>\w+)\s*=>?(?<body>[^;]*);")]
    private static partial Regex TabList();

    [GeneratedRegex(@"new\s*\(\s*""(?<id>[^""]+)""\s*,\s*""(?<title>[^""]+)""(?<rest>[^)]*)\)")]
    private static partial Regex ShortTabRecord();

    [GeneratedRegex(@"Icon:\s*""(?<icon>[^""]+)""")]
    private static partial Regex TabIcon();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
