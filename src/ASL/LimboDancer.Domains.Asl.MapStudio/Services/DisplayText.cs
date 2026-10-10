using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Vasl;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// How the Studio words values that the game and the boards keep as identifiers (pass 29, the UX analysis of 2026-10-02): a side as its counters
/// name it, a yes-or-no condition, a unit kind without its vocabulary prefix, a board's status, and a Location. The identifiers themselves are
/// unchanged; only what the page shows is worded.
/// </summary>
public static class DisplayText
{
    /// <summary>A side as its counters name it: "german" reads "German".</summary>
    public static string Side(string? side) =>
        string.IsNullOrEmpty(side) ? string.Empty : char.ToUpper(side[0], CultureInfo.InvariantCulture) + side[1..];

    /// <summary>A side with its article, "A German" or "An American", to begin a sentence about one of its counters.</summary>
    public static string ASide(string? side) => Side(side) is { Length: > 0 } name ? ("AEIOU".Contains(name[0], StringComparison.Ordinal) ? "An " : "A ") + name : "A";

    /// <summary>A side in a sentence: "the German side", or "the adjudicator".</summary>
    public static string ViewName(string? name) =>
        name is null ? string.Empty : name == Perspective.AdjudicatorName ? "the adjudicator" : $"the {Side(name)} side";

    /// <summary>A condition that is true, false, or not known, as Yes, No, Unknown, or "Does not apply".</summary>
    public static string YesNo(ConditionState state) => state switch
    {
        ConditionState.True => "Yes",
        ConditionState.False => "No",
        ConditionState.Inapplicable => "Does not apply",
        _ => "Unknown",
    };

    /// <summary>One condition: its name alone when it holds, or its name and state ("broken unknown").</summary>
    public static string Condition(string key, ConditionState state) =>
        state == ConditionState.True ? Kind(key) : $"{Kind(key)} {Conditions.Name(state)}";

    /// <summary>The conditions an object holds or may hold, leaving out those known to be false; "none" when it holds none.</summary>
    public static string ConditionsOf(IGameObject item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var shown = item.Conditions.Where(pair => pair.Value is not ConditionState.False).Select(pair => Condition(pair.Key, pair.Value)).ToArray();
        return shown.Length == 0 ? "none" : string.Join(", ", shown);
    }

    /// <summary>A fidelity batch outcome; Ingested reads as "Not verified", the name the board's own status uses.</summary>
    public static string Outcome(BatchOutcome outcome) => outcome switch
    {
        BatchOutcome.Verified => "Verified",
        BatchOutcome.Ingested => "Not verified",
        BatchOutcome.Failed => "Failed",
        BatchOutcome.OutOfScope => "Out of scope",
        _ => outcome.ToString(),
    };

    /// <summary>F1, whether VASL's terrain data is read exactly: decoded, encoded again, and compared byte for byte.</summary>
    public static string TerrainData(F1Status? status) => status switch
    {
        null => "Not checked",
        F1Status.Pass => "Pass",
        F1Status.FramingOnly => "Pass, framing differs",
        F1Status.Fail => "Fail",
        _ => status.Value.ToString(),
    };

    /// <summary>F2, whether the hex facts match VASL's own answers for the board.</summary>
    public static string HexFacts(string status) => status switch
    {
        BatchF2.Pass => "Pass",
        BatchF2.Fail => "Fail",
        BatchF2.NoFixture => "No VASL answers to compare",
        _ => status,
    };

    /// <summary>A named fidelity check in words; F3's two checks test the Styled drawing.</summary>
    public static string Check(string name) => name switch
    {
        "exact-outlines" => "Terrain outlines",
        "elevation-outlines" => "Elevation outlines",
        "f3-hexfacts" => "Styled drawing: hex facts",
        "f3-pixels" => "Styled drawing: pixels",
        "hexfacts-svg" => "Hex facts drawing",
        _ when name.EndsWith("-svg", StringComparison.Ordinal) => $"{Side(name[..^4])} drawing",
        _ => name,
    };

    /// <summary>A unit or entity kind without its vocabulary prefix: "asl:half-squad" reads "half-squad".</summary>
    public static string Kind(string? kind) => (kind ?? string.Empty).Replace("asl:", string.Empty, StringComparison.Ordinal);

    /// <summary>A board's status in words.</summary>
    public static string BoardStatus(BoardStatus status) => status switch
    {
        MapStudio.Services.BoardStatus.Verified => "Verified",
        MapStudio.Services.BoardStatus.Ingested => "Not verified",
        MapStudio.Services.BoardStatus.Authored => "Authored, with errors",
        MapStudio.Services.BoardStatus.AuthoredValid => "Authored, valid",
        _ => status.ToString(),
    };

    /// <summary>
    /// A Location as a player reads it: "F6 on board 01", "F6 on board 01, cellar", or "F6 on board 01, level 1"; the identifier as it was when it
    /// is not a Location.
    /// </summary>
    public static string Location(string location) =>
        BoardLocation.TryParse(location, out var at) ? Location(at) : location;

    public static string Location(BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        var board = location.Board.Value is ['b', 'd', .. var number] && number.Length > 0 ? $"board {number}" : location.Board.Value;
        var level = location.Level switch
        {
            0 => string.Empty,
            -1 => ", cellar",
            var other => $", level {other}",
        };
        return $"[{location.Hex}] on {board}{level}";
    }

    /// <summary>
    /// A reason as a player reads it (pass 31c, design D11; play test P-18): its text without the code at its head, and the code apart, for the
    /// element's <c>data-code</c> and title. "play.fire: the range is 3" reads "the range is 3"; a reason that is only a code reads as its last
    /// word in plain words. The text is not capitalized here, since it may begin with an id a view's names still have to read.
    /// </summary>
    public static (string? Code, string Text) Reason(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        // The third play test: the game's own refusal reads "UNIT-STATE-030 revision 525 (advance-7d5a-1): The CC ..."; a player reads the
        // sentence, and a side is shown no revision in a game still played (design D15).
        var stated = System.Text.RegularExpressions.Regex.Match(reason, @"^([A-Z]+(?:-[A-Z]+)*-\d+) revision \d+ \([^)]*\):\s+(.*)$", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (stated.Success)
        {
            return (stated.Groups[1].Value, stated.Groups[2].Value);
        }

        // The second play test: a phase's advance says "Turn 1, pfph, russian phasing"; a player reads the phase that begins.
        var phase = System.Text.RegularExpressions.Regex.Match(reason, @"^(?:[a-z.]+:\s+)?[Tt]urn (\d+), ([a-z]+), (\w+) phasing$");
        if (phase.Success)
        {
            return ("play.advance", $"The {Side(phase.Groups[3].Value)} {Components.Games.GameText.PhaseLabel(phase.Groups[2].Value)} of Turn {phase.Groups[1].Value} begins");
        }

        var match = System.Text.RegularExpressions.Regex.Match(reason, @"^([a-z][a-z0-9]*(?:\.[a-z0-9-]+)+(?::[\w-]+)?)(?::\s+(.*))?$", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (!match.Success)
        {
            return (null, reason);
        }

        var code = match.Groups[1].Value;

        // Pass 31d (design D11): a reason that is a fact of the reviewed entry case reads as the fact in words, not as its name in the code.
        var fact = System.Text.RegularExpressions.Regex.Match(reason, @"^fact\.(unknown|outside-reviewed-case):(\w+)$");
        if (fact.Success)
        {
            // The table player, pass 31d: the fact named is the one that failed, so the sentence says that it does not hold.
            return (code, $"{(fact.Groups[1].Value == "unknown" ? "the game cannot say whether this holds:" : "this does not hold:")} {Fact(fact.Groups[2].Value)}");
        }

        if (match.Groups[2].Success && match.Groups[2].Value.Length > 0)
        {
            return (code, match.Groups[2].Value);
        }

        return (code, code[(code.LastIndexOf('.') + 1)..].Replace('-', ' ').Replace(":", ", ", StringComparison.Ordinal));
    }

    private static readonly Dictionary<string, string> Facts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["isKnownGoodOrderInfantrySquad"] = "the mover is a Known Good Order Infantry squad",
        ["isAttackerMovementPhase"] = "it is the ATTACKER's Movement Phase",
        ["canMoveThisPhase"] = "the unit may move this phase",
        ["isAdjacentGroundLevelOrdinaryBuilding"] = "the Location entered is an ADJACENT ordinary building at ground level",
        ["hasNoRoadBypassElevationOrAdditionalTerrain"] = "no road, Bypass, elevation, or other terrain enters into the move",
        ["hasEnoughMovementFactors"] = "the unit has the MF for the entry",
        ["hasNoSpecialRuleOrOtherModifier"] = "no special rule or other modifier applies",
    };

    /// <summary>A fact of the reviewed entry case in words (pass 31d, design D11); a name with no words is split at its capitals.</summary>
    public static string Fact(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Facts.TryGetValue(name, out var words) ? words : System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ").ToLowerInvariant();
    }

    /// <summary>
    /// A planner's sentence with its last tokens in a player's words (pass 31d, design D11): a terrain key ("stone-building", "open-ground") and a
    /// phase's short form after "in the" ("in the PFPh"). The planner's sentences are not changed; a refusal's audit line reads as it did.
    /// </summary>
    public static string Planner(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var said = text.Replace("stone-building", "stone building", StringComparison.Ordinal).Replace("wooden-building", "wooden building", StringComparison.Ordinal)
            .Replace("stone-rubble", "stone rubble", StringComparison.Ordinal).Replace("wooden-rubble", "wooden rubble", StringComparison.Ordinal)
            .Replace("open-ground", "Open Ground", StringComparison.Ordinal);
        return System.Text.RegularExpressions.Regex.Replace(said, @"\bin the (RPh|PFPh|MPh|DFPh|AFPh|RtPh|APh|CCPh)\b",
            match => "in the " + Components.Games.GameText.PhaseLabel(match.Groups[1].Value.ToLowerInvariant()));
    }

    /// <summary>
    /// A text's hexes in brackets where it names them outside a Location's identifier (the user, 2026-10-04): a board's hex such as
    /// "bd01:W4" reads "[W4]" on a map of one board and "[W4] on board 01" otherwise, and "building X3" reads "building [X3]". The Victory
    /// account and the game's result are written this way.
    /// </summary>
    public static string Hexes(string text, int boards)
    {
        ArgumentNullException.ThrowIfNull(text);
        var said = System.Text.RegularExpressions.Regex.Replace(text, @"\b(bd\w+?|ab-[a-z0-9-]+):([A-Z]{1,2}\d{1,2})\b(?!:-?\d)",
            match => boards == 1 ? $"[{match.Groups[2].Value}]"
                : $"[{match.Groups[2].Value}] on {(match.Groups[1].Value.StartsWith("bd", StringComparison.Ordinal) ? "board " + match.Groups[1].Value[2..] : match.Groups[1].Value)}");
        return System.Text.RegularExpressions.Regex.Replace(said, @"\b([Bb]uilding) ([A-Z]{1,2}\d{1,2})\b", "$1 [$2]");
    }

    /// <summary>A text with its first letter a capital, once its identifiers have been put in words.</summary>
    public static string Sentence(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Length > 0 && char.IsLower(text[0]) ? char.ToUpperInvariant(text[0]) + text[1..] : text;
    }

    /// <summary>An action's registered name in words: "asl.game.end-phase" reads "End phase", "asl.game.throw-dc" "Throw DC".</summary>
    public static string Action(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var last = name[(name.LastIndexOf('.') + 1)..];
        return last switch
        {
            "" => name,
            "pass-fire" => "Decline First Fire",
            _ => Sentence(Plain(last)),
        };
    }

    private static readonly Dictionary<string, string> Abbreviations = new(StringComparer.Ordinal)
    {
        ["dc"] = "DC",
        ["afv"] = "AFV",
        ["mg"] = "MG",
        ["ce"] = "CE",
        ["ovr"] = "OVR",
        ["afph"] = "AFPh",
        ["pf"] = "PF",
        ["vs"] = "against",
        ["ti"] = "TI",
        ["cx"] = "CX",
        ["sw"] = "SW",
        ["ft"] = "FT",
        ["los"] = "LOS",
        ["tem"] = "TEM",
        ["cc"] = "CC",
        ["hs"] = "HS",
        ["smc"] = "SMC",
        ["mmc"] = "MMC",
    };

    /// <summary>A recorded name with no words of its own: its hyphens as spaces, and the game's abbreviations in capitals ("thrown dc afph" reads "thrown DC AFPh").</summary>
    private static string Plain(string name) => string.Join(' ', name.Split('-').Select(word => Abbreviations.TryGetValue(word, out var known) ? known : word));

    private static readonly Dictionary<string, string> ModifierWords = new(StringComparer.Ordinal)
    {
        ["ffnam"] = "FFNAM",
        ["ffmo"] = "FFMO",
        ["area-fire"] = "Area Fire",
        ["area-fire-concealed-target"] = "Area Fire, concealed target",
        ["area-fire-gunflash"] = "Area Fire at a Gunflash",
        ["smc-area-fire"] = "Area Fire by a SMC",
        ["point-blank-fire"] = "Point Blank Fire",
        ["triple-point-blank-fire"] = "Triple Point Blank Fire",
        ["tpbf"] = "Triple Point Blank Fire",
        ["long-range-fire"] = "Long Range Fire",
        ["advancing-fire"] = "Advancing Fire",
        ["assault-fire"] = "Assault Fire",
        ["spraying-fire"] = "Spraying Fire",
        ["bounding-fire"] = "Bounding Fire",
        ["motion-fire"] = "Motion Fire",
        ["snap-shot"] = "Snap Shot",
        ["los-hindrance"] = "LOS Hindrance",
        ["lv-hindrance"] = "LV Hindrance",
        ["hazardous-movement"] = "Hazardous Movement",
        ["height-advantage"] = "Height Advantage",
        ["desperation-morale"] = "DM",
        ["cx"] = "CX",
        ["vs-cx"] = "against a CX unit",
        ["vs-concealed"] = "against a concealed unit",
        ["vs-broken"] = "against a broken unit",
        ["vs-ti"] = "against a TI unit",
        ["vs-ambush"] = "Ambushed",
        ["vs-withdrawing"] = "against a withdrawing unit",
        ["pinned-firer"] = "pinned firer",
        ["hs-or-crew"] = "HS or crew",
        ["smc"] = "SMC",
        ["nkvd"] = "NKVD",
        ["self-rally"] = "Self-Rally",
        ["ift-mc"] = "IFT MC",
        ["crew-exposed"] = "CE",
        ["stun-recovery"] = "Stun +1",
        ["odds-below-1-1"] = "odds below 1-1",
    };

    private static readonly Dictionary<string, string> RollWords = new(StringComparer.Ordinal)
    {
        ["fire-ift"] = "IFT DR",
        ["fire-check"] = "MC or TC DR",
        ["fire-heat-of-battle"] = "Heat of Battle DR",
        ["fire-leader-loss"] = "Leader Loss DR",
        ["fire-random-selection"] = "Random Selection dr",
        ["random-selection"] = "Random Selection dr",
        ["deployment"] = "Deployment NTC DR",
        ["shock-recovery"] = "Shock recovery dr",
        ["starshell-usage"] = "Starshell Usage dr",
        ["cc-attack"] = "CC DR",
        ["cc-leader-creation"] = "Leader Creation dr",
        ["cc-ambush"] = "Ambush dr",
        ["rally"] = "Rally DR",
        ["first-move"] = "first move",
        ["sniper"] = "Sniper dr",
        ["wind-change"] = "Wind Change DR",
    };

    /// <summary>A roll's recorded purpose in words: "fire-ift" reads "IFT DR"; one with no words of its own reads with spaces for its hyphens.</summary>
    public static string RollPurpose(string purpose)
    {
        ArgumentNullException.ThrowIfNull(purpose);
        return RollWords.TryGetValue(purpose, out var words) ? words : purpose.Replace('-', ' ');
    }

    private static readonly Dictionary<string, string> ModifierHeads = new(StringComparer.Ordinal)
    {
        ["leadership"] = "leadership, {0}",
        ["reversed-leadership"] = "leadership reversed, {0}",
        ["berserk-leader"] = "berserk leader {0}",
        ["heroic"] = "heroic, {0}",
        ["hero-mg"] = "hero's MG, {0}",
        ["created-leader"] = "the leader created",
        ["smc-combining"] = "SMC combining, {0}",
        ["tem"] = "{1} TEM",
        ["critical-hit-tem"] = "{1} TEM, reversed by the Critical Hit",
        ["terrain"] = "{1}",
        ["nationality"] = "{2}",
        ["emplacement"] = "emplacement, {1}",
        ["gunshield"] = "gunshield, {0}",
        ["afv-cover"] = "AFV cover, {0}",
        ["escort"] = "escort, {0}",
        ["guarding"] = "guarding {0}",
        ["encircled"] = "Encircled, {0}",
        ["inexperienced"] = "Inexperienced, {0}",
        ["pinned"] = "pinned, {0}",
        ["cx"] = "CX, {0}",
        ["ti"] = "TI, {0}",
        ["stun-recovery"] = "Stun +1, {0}",
    };

    /// <summary>
    /// A modifier's recorded name in words (pass 31c, design D11): "tem:stone-building" reads "stone building TEM", "ffnam" reads "First Fire, no
    /// Assault Movement", and "leadership:g-leader-9-2-1" reads "leadership, g-leader-9-2-1", whose unit a view's names then put in words. A name
    /// with no words of its own reads with spaces for its hyphens; a To Hit case reads "Case A".
    /// </summary>
    public static string Modifier(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (ModifierWords.TryGetValue(name, out var words))
        {
            return words;
        }

        var colon = name.IndexOf(':', StringComparison.Ordinal);
        var (head, rest) = colon < 0 ? (name, string.Empty) : (name[..colon], name[(colon + 1)..]);
        if (colon >= 0 && ModifierHeads.TryGetValue(head, out var pattern))
        {
            return string.Format(CultureInfo.InvariantCulture, pattern, rest, rest.Replace('-', ' '), Side(rest));
        }

        var spoken = head.StartsWith("case-", StringComparison.Ordinal) ? "Case " + head["case-".Length..].ToUpperInvariant()
            : ModifierWords.TryGetValue(head, out var known) ? known : Plain(head);
        return rest.Length == 0 ? spoken : $"{spoken}, {(rest.Contains(':', StringComparison.Ordinal) ? Modifier(rest) : rest)}";
    }

    /// <summary>
    /// The one way a game's Location is written for a player (pass 31c, design D12): "[G4], level 1" on a map of one board, and
    /// "[G4] on board 01, level 1" on a map of several; ground level is the hex alone, and level -1 the cellar. A hex stands in brackets
    /// wherever it is written (the user, 2026-10-04), which tells it from a unit's tag: "4-6-7 squad G4 in [G4]".
    /// </summary>
    public static string Place(int boards, BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (boards != 1)
        {
            return Location(location);
        }

        return location.Level switch
        {
            0 => $"[{location.Hex}]",
            -1 => $"[{location.Hex}], cellar",
            var level => string.Create(CultureInfo.InvariantCulture, $"[{location.Hex}], level {level}"),
        };
    }

    /// <summary>A Location's identifier in a game's words; the text as it was when it is not a Location.</summary>
    public static string Place(int boards, string location) =>
        BoardLocation.TryParse(location, out var at) ? Place(boards, at) : location;

    /// <summary>
    /// A Location as a player types it, read into its identifier (pass 31c, design D12): the identifier itself ("bd01:G4:1"), the short "G4" and
    /// "G4:1", and the words ("G4, level 1", "G4 on board 01, cellar"). A form without its board is read on the map's one board; on a map of
    /// several, or when the text is none of these, it is returned as typed, for the gate to refuse in its own words.
    /// </summary>
    public static string ReadPlace(string text, IReadOnlyList<string> boards)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(boards);
        // A hex is written in brackets, "[G4]" (the user, 2026-10-04), and is read with or without them.
        var typed = text.Trim().Replace("[", string.Empty, StringComparison.Ordinal).Replace("]", string.Empty, StringComparison.Ordinal);
        if (typed.Length == 0 || BoardLocation.TryParse(typed, out _))
        {
            return typed;
        }

        // The identifier typed in lower case ("bd01:g4:1").
        var whole = System.Text.RegularExpressions.Regex.Match(typed, @"^(bd\w+):([A-Za-z]{1,2}\d{1,2}):(-?\d)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (whole.Success && $"{whole.Groups[1].Value.ToLowerInvariant()}:{whole.Groups[2].Value.ToUpperInvariant()}:{whole.Groups[3].Value}" is var canonical && BoardLocation.TryParse(canonical, out _))
        {
            return canonical;
        }

        // "G4", "G4:1", "G4 1", "G4 level 1", "G4, level 1", "G4 cellar", each with "on board 01" after the hex.
        var match = System.Text.RegularExpressions.Regex.Match(typed,
            @"^([A-Za-z]{1,2}\d{1,2})(?:\s+on\s+board\s+(\w+))?(?:\s*(?::|,?\s*level\s|\s)\s*(-?\d)|\s*,?\s*(cellar))?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return typed;
        }

        var board = match.Groups[2].Success ? boards.FirstOrDefault(id => id == "bd" + match.Groups[2].Value || id == match.Groups[2].Value) ?? "bd" + match.Groups[2].Value
            : boards.Count == 1 ? boards[0] : null;
        var level = match.Groups[4].Success ? "-1" : match.Groups[3].Success ? match.Groups[3].Value : "0";
        var read = $"{board}:{match.Groups[1].Value.ToUpperInvariant()}:{level}";
        return board is not null && BoardLocation.TryParse(read, out _) ? read : typed;
    }

    /// <summary>A Location of the game a state belongs to.</summary>
    public static string Place(GameState state, BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(state);
        return Place(state.Map.Boards.Count, location);
    }
}
