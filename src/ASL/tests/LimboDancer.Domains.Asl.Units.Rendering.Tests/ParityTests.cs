using System.Xml.Linq;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering.Styles;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>
/// Information parity (ASL-UNIT-075; section 12): for every row of the section 4 tables, a document with the fact and
/// one without render differently under both ASL sheets, and the difference is where the row says. A row the sheet
/// shows only in the detail panel is checked against the detail panel.
/// </summary>
public sealed class ParityTests
{
    private const string Squad = """
        { "id": "u", "kind": "asl:squad", "side": "german",
          "faces": { "front": { "firepower": 4, "range": 6, "morale": 7, "identity": "A", "class": "1st-line" }, "broken": { "broken-morale": 7, "bpv": 10 } } }
        """;

    private const string Leader = """
        { "id": "u", "kind": "asl:leader", "side": "german", "faces": { "front": { "leadership": -1, "morale": 8 }, "broken": { "broken-morale": 8 } } }
        """;

    private const string Hero = """
        { "id": "u", "kind": "asl:hero", "side": "american", "faces": { "front": { "firepower": 1, "range": 4, "morale": 9 }, "wounded": { "firepower": 1, "range": 3, "morale": 8 } } }
        """;

    private const string Mg = """
        { "id": "u", "kind": "asl:mg", "side": "german",
          "faces": { "front": { "size": "light", "firepower": 3, "range": 6, "breakdown": 12, "rate-of-fire": 1, "portage": 1 }, "malfunctioned": { "repair": 6 } } }
        """;

    private const string Radio = """
        { "id": "u", "kind": "asl:radio", "side": "russian",
          "faces": { "front": { "contact": [6, 7, 8], "breakdown": 11, "portage": 1 }, "reverse": { "contact-dates": ["to 6/42", "7/42 on"] } } }
        """;

    private const string Gun = """
        { "id": "u", "kind": "asl:gun", "side": "german", "facing": "north-east",
          "faces": { "front": { "designation": "PaK 40", "gun-type": "at", "caliber": 75, "caliber-suffix": "l", "rate-of-fire": 2, "ife": 3, "range-minimum": 2, "range-maximum": 40, "special-ammo": ["H6"], "manhandling": 8, "breakdown": 11 }, "malfunctioned": { "repair": 1, "removal": 6 }, "limbered": { "manhandling": 8 } } }
        """;

    private const string LightMortar = """
        { "id": "u", "kind": "asl:light-mortar", "side": "british", "faces": { "front": { "caliber": 50, "range-minimum": 2, "range-maximum": 13, "breakdown": 12, "portage": 2 } } }
        """;

    private const string Latw = """
        { "id": "u", "kind": "asl:latw", "side": "german", "faces": { "front": { "latw-type": "psk", "caliber": 88, "range-maximum": 4, "breakdown": 10, "portage": 2 } } }
        """;

    /// <summary>Each row: without the fact, with it, the face to show (or null), and where the difference is under classic and digital.</summary>
    public static TheoryData<string, string, string, string?, string, string> Rows() => new()
    {
        // 4.1 Personnel.
        { "Personnel kind", Squad, Squad.Replace("asl:squad", "asl:half-squad", StringComparison.Ordinal), null, "slot:fig", "slot:glyph" },
        { "Firepower", Squad.Replace("\"firepower\": 4, ", "", StringComparison.Ordinal), Squad, null, "slot:fp", "slot:fp" },
        { "Smoke Placement Exponent", Squad, Squad.Replace("} } }", "} }, \"unit\": { \"smoke-exponent\": 1 } }", StringComparison.Ordinal), null, "slot:fp", "badge" },
        { "Assault Fire", Squad, WithTrait(Squad, "front", "asl:assault-fire"), null, "slot:fp", "slot:fp" },
        { "Normal Range", Squad.Replace("\"range\": 6, ", "", StringComparison.Ordinal), Squad, null, "slot:range", "slot:range" },
        { "Spraying Fire", Squad, WithTrait(Squad, "front", "asl:spraying-fire"), null, "slot:range", "slot:range" },
        { "Morale", Squad.Replace("\"morale\": 7, ", "", StringComparison.Ordinal), Squad, null, "slot:morale", "slot:morale" },
        { "ELR of 5", Squad, WithTrait(Squad, "front", "asl:elr-5"), null, "slot:morale", "slot:morale" },
        { "Identity", Squad.Replace("\"identity\": \"A\", ", "", StringComparison.Ordinal), Squad, null, "slot:ident", "slot:ident" },
        { "Crew numeral", Squad.Replace("asl:squad", "asl:crew", StringComparison.Ordinal).Replace("\"identity\": \"A\", ", "", StringComparison.Ordinal),
            Squad.Replace("asl:squad", "asl:crew", StringComparison.Ordinal).Replace("\"A\"", "\"3\"", StringComparison.Ordinal), null, "slot:ident", "slot:ident" },
        { "Class", Squad.Replace(", \"class\": \"1st-line\"", "", StringComparison.Ordinal), Squad, null, "slot:class", "slot:class" },
        { "Class variant", Squad, Squad.Replace("\"class\": \"1st-line\"", "\"class\": \"1st-line\", \"class-variant\": \"square\"", StringComparison.Ordinal), null,
            "slot:class", "slot:class" },
        { "Broken side", Squad, WithStates(Squad, "asl:broken"), null, "face", "face" },
        { "Broken Morale Level", Squad.Replace("\"broken-morale\": 7, ", "", StringComparison.Ordinal), Squad, "broken", "slot:bmorale", "slot:bmorale" },
        { "Can Self-Rally", Squad, WithTrait(Squad, "broken", "asl:self-rally"), "broken", "slot:bmorale", "slot:bmorale" },
        { "Basic Point Value", Squad.Replace(", \"bpv\": 10", "", StringComparison.Ordinal), Squad, "broken", "slot:bpv", "details" },
        { "Special status", Squad, WithTrait(Squad, "front", "asl:sapper"), null, "details", "badge" },
        { "Unit Size Number", Squad, Squad.Replace("} } }", "} }, \"unit\": { \"unit-size\": 2 } }", StringComparison.Ordinal), null, "details", "details" },
        { "Leadership DRM", Leader.Replace("\"leadership\": -1, ", "", StringComparison.Ordinal), Leader, null, "slot:drm", "slot:drm" },
        { "Leader morale", Leader.Replace(", \"morale\": 8", "", StringComparison.Ordinal), Leader, null, "slot:morale", "slot:morale" },
        { "Hero wounded side", Hero, WithStates(Hero, "asl:wounded"), null, "face", "face" },

        // 4.2 Support weapons.
        { "MG Firepower", Mg.Replace("\"firepower\": 3, ", "", StringComparison.Ordinal), Mg, null, "slot:fp", "slot:fp" },
        { "MG Normal Range", Mg.Replace("\"range\": 6, ", "", StringComparison.Ordinal), Mg, null, "slot:range", "slot:range" },
        { "MG size", Mg, Mg.Replace("\"light\"", "\"heavy\"", StringComparison.Ordinal), null, "slot:label", "slot:label" },
        { "Multiple ROF", Mg.Replace("\"rate-of-fire\": 1, ", "", StringComparison.Ordinal), Mg, null, "slot:rof", "badge" },
        { "Breakdown Number", Mg.Replace("\"breakdown\": 12, ", "", StringComparison.Ordinal), Mg, null, "slot:bd", "slot:bd" },
        { "X# breakdown", Mg, WithTrait(Mg, "front", "asl:breakdown-removes"), null, "slot:bd", "slot:bd" },
        { "Portage cost", Mg.Replace(", \"portage\": 1", "", StringComparison.Ordinal), Mg, null, "slot:pp", "slot:pp" },
        { "Malfunctioned side", Mg, WithStates(Mg, "asl:malfunctioned"), null, "face", "face" },
        { "Repair Number", Mg.Replace("\"repair\": 6", "", StringComparison.Ordinal), Mg, "malfunctioned", "slot:repair", "slot:repair" },
        { "Flamethrower", Mg, Mg.Replace("asl:mg", "asl:ft", StringComparison.Ordinal).Replace("\"size\": \"light\", ", "", StringComparison.Ordinal), null, "slot:label", "slot:label" },
        { "Demolition Charge", Mg, Mg.Replace("asl:mg", "asl:dc", StringComparison.Ordinal).Replace("\"size\": \"light\", ", "", StringComparison.Ordinal), null, "slot:label", "slot:label" },
        { "Radio contact", Radio.Replace("\"contact\": [6, 7, 8], ", "", StringComparison.Ordinal), Radio, null, "slot:contact", "slot:contact" },
        { "Field phone", Radio, WithTrait(Radio, "front", "asl:field-phone"), null, "slot:label", "slot:label" },
        { "Contact dates", Radio.Replace("\"contact-dates\": [\"to 6/42\", \"7/42 on\"]", "", StringComparison.Ordinal), Radio, "reverse", "slot:dates", "slot:dates" },
        { "LATW", Mg, """{ "id": "u", "kind": "asl:latw", "side": "german", "faces": { "front": { "latw-type": "baz", "breakdown": 10, "portage": 2 } } }""",
            null, "slot:label", "slot:label" },
        { "Light mortar", Mg, """{ "id": "u", "kind": "asl:light-mortar", "side": "german", "faces": { "front": { "breakdown": 12, "portage": 2 } } }""",
            null, "slot:label", "slot:label" },

        // 4.3 States.
        { "Pinned", Squad, WithStates(Squad, "asl:pinned"), null, "badge", "badge" },
        { "Counter Exhaustion", Squad, WithStates(Squad, "asl:cx"), null, "badge", "badge" },
        { "Desperation Morale", Squad, WithStates(Squad, "asl:dm"), null, "badge", "badge" },
        { "Temporarily Immobilized", Squad, WithStates(Squad, "asl:ti"), null, "badge", "badge" },
        { "Berserk", Squad, WithStates(Squad, "asl:berserk"), null, "face", "face" },
        { "Fanatic", Squad, WithStates(Squad, "asl:fanatic"), null, "face", "face" },
        { "Wounded SMC", Leader, WithStates(Leader, "asl:wounded"), null, "badge", "badge" },
        { "Disrupted", Squad, WithStates(Squad, "asl:disrupted"), null, "badge", "badge" },
        { "Concealed, owner's view", Squad, WithStates(Squad, "asl:concealed"), null, "face", "face" },

        // 4.4 Guns and ordnance values.
        { "Gun Caliber Size", Gun.Replace("\"caliber\": 75, ", "", StringComparison.Ordinal), Gun, null, "slot:cal", "slot:cal" },
        { "Cannot fire AP", Gun, WithTrait(Gun, "front", "asl:no-ap"), null, "slot:cal", "slot:cal" },
        { "Cannot fire HE", Gun, WithTrait(Gun, "front", "asl:no-he"), null, "slot:cal", "slot:cal" },
        { "Caliber suffix", Gun.Replace("\"caliber-suffix\": \"l\", ", "", StringComparison.Ordinal), Gun, null, "slot:cal", "slot:cal" },
        { "Gun type", Gun, Gun.Replace("\"gun-type\": \"at\"", "\"gun-type\": \"inf\"", StringComparison.Ordinal), null, "slot:type", "slot:type" },
        { "Gun designation", Gun.Replace("\"designation\": \"PaK 40\", ", "", StringComparison.Ordinal), Gun, null, "details", "details" },
        { "Facing and Covered Arc", Gun.Replace(" \"facing\": \"north-east\",", "", StringComparison.Ordinal), Gun, null, "slot:glyph", "direction" },
        { "Gun ROF", Gun.Replace("\"rate-of-fire\": 2, ", "", StringComparison.Ordinal), Gun, null, "slot:rof", "badge" },
        { "Range limit", Gun.Replace("\"range-minimum\": 2, \"range-maximum\": 40, ", "", StringComparison.Ordinal), Gun, null, "slot:range", "slot:range" },
        { "Minimum range", Gun.Replace("\"range-minimum\": 2, ", "", StringComparison.Ordinal), Gun, null, "slot:range", "slot:range" },
        { "Special ammunition", Gun.Replace("\"special-ammo\": [\"H6\"], ", "", StringComparison.Ordinal), Gun, null, "slot:ammo", "slot:ammo" },
        { "Manhandling Number", Gun.Replace("\"manhandling\": 8, ", "", StringComparison.Ordinal), Gun, null, "slot:mnum", "slot:mnum" },
        { "Small Target", Gun, Gun.Replace("\"manhandling\": 8, ", "\"manhandling\": 8, \"target-size\": \"small\", ", StringComparison.Ordinal), null, "slot:mnum", "slot:mnum" },
        { "Large Target", Gun, Gun.Replace("\"manhandling\": 8, ", "\"manhandling\": 8, \"target-size\": \"large\", ", StringComparison.Ordinal), null, "slot:mnum", "slot:mnum" },
        { "Gun breakdown", Gun.Replace(", \"breakdown\": 11", "", StringComparison.Ordinal), Gun, null, "slot:bd", "slot:bd" },
        { "IFE", Gun.Replace("\"ife\": 3, ", "", StringComparison.Ordinal), Gun, null, "slot:ife", "badge" },
        { "360 Mount", Gun, WithTrait(Gun, "front", "asl:mount-360"), null, "slot:glyph", "badge" },
        { "Quick Set-Up", Gun, WithTrait(Gun, "front", "asl:qsu"), null, "slot:mv", "badge" },
        { "No Movement", Gun, WithTrait(Gun, "front", "asl:nm"), null, "slot:mv", "badge" },
        { "Restricted Fire, No Movement", Gun, WithTrait(Gun, "front", "asl:rfnm"), null, "slot:mv", "badge" },
        { "Gun malfunctioned side", Gun, WithStates(Gun, "asl:malfunctioned"), null, "face", "face" },
        { "Gun Repair Number", Gun.Replace("\"repair\": 1, ", "", StringComparison.Ordinal), Gun, "malfunctioned", "slot:repair", "slot:repair" },
        { "Gun Removal Number", Gun.Replace(", \"removal\": 6", "", StringComparison.Ordinal), Gun, "malfunctioned", "slot:removal", "slot:removal" },
        { "Limbered side", Gun, WithStates(Gun, "asl:limbered"), null, "face", "face" },
        { "Limbered Fire values", Gun, Gun.Replace("\"limbered\": { \"manhandling\": 8 }", "\"limbered\": { \"caliber\": 75, \"manhandling\": 8 }", StringComparison.Ordinal),
            "limbered", "slot:cal", "slot:cal" },
        { "Light mortar Caliber Size", LightMortar.Replace("\"caliber\": 50, ", "", StringComparison.Ordinal), LightMortar, null, "slot:cal", "slot:cal" },
        { "Light mortar range limit", LightMortar.Replace("\"range-minimum\": 2, \"range-maximum\": 13, ", "", StringComparison.Ordinal), LightMortar, null, "slot:range", "slot:range" },
        { "LATW Caliber Size", Latw.Replace("\"caliber\": 88, ", "", StringComparison.Ordinal), Latw, null, "slot:cal", "slot:cal" },
        { "LATW range limit", Latw.Replace("\"range-maximum\": 4, ", "", StringComparison.Ordinal), Latw, null, "slot:range", "slot:range" },
    };

    [Theory]
    [MemberData(nameof(Rows))]
    public void EveryPrintedFactIsReadableUnderBothSheets(string row, string without, string with, string? face, string classic, string digital)
    {
        foreach (var (sheet, expected) in new[] { (UnitStyles.Classic, classic), (UnitStyles.Digital, digital) })
        {
            var differences = Differences(sheet, RenderingTestData.Document(without), RenderingTestData.Document(with), face);
            Assert.True(differences.Contains(expected), $"{row} under {sheet}: expected a difference in {expected}, found {string.Join(", ", differences)}.");
        }
    }

    [Theory]
    [InlineData(UnitStyles.Classic)]
    [InlineData(UnitStyles.Digital)]
    public void AConcealedPlaceholderIsASideColoredQuestionMark(string sheet)
    {
        var renderer = RenderingTestData.Renderer(sheet);
        var placeholder = RenderingTestData.Catalog.Value["example-concealed"];
        var svg = RenderingTestData.Render(renderer, placeholder);
        Assert.Equal("?", CascadeTests.Slots(svg)["q"]);
        Assert.Equal(renderer.Palette.For("german").Fill, svg.Descendants().Single(e => e.Attribute("data-face") is not null).Attribute("fill")!.Value);
    }

    /// <summary>The regions whose markup differs: each slot, the badges, the face shape, attachments, and the detail panel.</summary>
    private static HashSet<string> Differences(string sheet, UnitDocument without, UnitDocument with, string? face)
    {
        var renderer = RenderingTestData.Renderer(sheet);
        var before = Regions(RenderingTestData.Render(renderer, without, DetailTier.Near, face), without, face);
        var after = Regions(RenderingTestData.Render(renderer, with, DetailTier.Near, face), with, face);
        return [.. before.Keys.Union(after.Keys).Where(key => before.GetValueOrDefault(key) != after.GetValueOrDefault(key))];
    }

    private static Dictionary<string, string> Regions(XElement svg, UnitDocument document, string? face)
    {
        var regions = new Dictionary<string, string>(StringComparer.Ordinal);
        var own = svg.Descendants().Where(element => element.Ancestors().All(ancestor => ancestor.Attribute("data-attached-id") is null)).ToArray();
        foreach (var slot in own.Where(element => element.Attribute("data-slot") is not null))
        {
            regions["slot:" + slot.Attribute("data-slot")!.Value] = slot.ToString();
        }

        regions["badge"] = string.Concat(own.Where(element => element.Attribute("data-badge") is not null).Select(element => element.ToString()));
        regions["face"] = string.Concat(own.Where(element => element.Attribute("data-face") is not null).Select(element => element.ToString()));
        regions["direction"] = string.Concat(own.Where(element => element.Attribute("data-facing") is not null || element.Attribute("data-covered-arc") is not null)
            .Select(element => element.ToString()));
        regions["details"] = string.Join("\n", UnitLabels.Details(document, RenderingTestData.Vocabulary.Value, face));
        return regions;
    }

    private static string WithTrait(string json, string face, string trait)
    {
        var marker = $"\"{face}\": {{ ";
        var start = json.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = json.IndexOf('}', start);
        return json[..end].TrimEnd() + $", \"traits\": [\"{trait}\"] " + json[end..];
    }

    private static string WithStates(string json, string state) =>
        json.TrimEnd()[..^1] + $", \"states\": [\"{state}\"] }}";
}
