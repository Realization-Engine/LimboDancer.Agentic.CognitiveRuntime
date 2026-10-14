"""Pass 40 task 40.9, third part: a butted half-hex at setup (A2.3, p. 45). Exact anchors, asserted once each."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new, count in edits:
        assert text.count(old) == count, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("LimboDancer.Domains.Asl.Rules/ScenarioA1SetupCalculator.cs", [
    ("public sealed record SetupLocationFacts(string Text, string Board, string Hex, int RowNumber)\n{\n",
     "public sealed record SetupLocationFacts(string Text, string Board, string Hex, int RowNumber)\n{\n"
     "    /// <summary>\n"
     "    /// For a half-hex butted against another board's (A2.3, p. 45; pass 40): the same hex as that other board names it. The Location's own name is\n"
     "    /// the owning board's, the one placed later (ruling R40.23). Null for a full hex and for a half-hex on the map's edge.\n"
     "    /// </summary>\n"
     "    public SetupLocationFacts? OtherHalf\n"
     "    {\n"
     "        get; init;\n"
     "    }\n\n", 1),
    ("    private static bool In(SetupCardFacts card, SetupAreaFacts area, SetupLocationFacts at) =>\n",
     "    /// <summary>\n"
     "    /// Whether a counter may set up at a Location by its group's areas (A2.3, p. 45; pass 40): \"Units allowed to set up/enter anywhere on a given\n"
     "    /// board may set up/enter on that board's half-hexes only if the half-hex is either part of another board on which it is also allowed to set\n"
     "    /// up/enter or is not butted against another half-hex\". So a butted half-hex is in the areas under both boards' names, or it is not.\n"
     "    /// </summary>\n"
     "    public static bool InAreas(SetupCardFacts card, IReadOnlyList<SetupAreaFacts> areas, SetupLocationFacts at)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(card);\n"
     "        ArgumentNullException.ThrowIfNull(areas);\n"
     "        ArgumentNullException.ThrowIfNull(at);\n"
     "        return areas.Any(area => area.Kind != \"entry\" && In(card, area, at))\n"
     "            && (at.OtherHalf is not { } other || areas.Any(area => area.Kind != \"entry\" && In(card, area, other)));\n"
     "    }\n\n"
     "    private static bool In(SetupCardFacts card, SetupAreaFacts area, SetupLocationFacts at) =>\n", 1),
    ("                    if (!group.Areas.Any(area => area.Kind != \"entry\" && In(card, area, counter.At!)))",
     "                    if (!InAreas(card, group.Areas, counter.At!))", 1),
    ("            if (!counter.OffBoard && !group.Areas.Any(area => area.Kind != \"entry\" && In(card, area, counter.At!)))",
     "            if (!counter.OffBoard && !InAreas(card, group.Areas, counter.At!))", 1),
])
apply("LimboDancer.Domains.Asl.Play/ScenarioSetup.cs", [
    ("    public static SetupReport Check(ScenarioCard card, IReadOnlyList<SetupCounter> counters, Func<BoardLocation, string?> terrain, Func<string?, bool> enterable,\n"
     "        Func<string, string?> halfSquadOf, int? month, string? balance = null)\n",
     "    public static SetupReport Check(ScenarioCard card, IReadOnlyList<SetupCounter> counters, Func<BoardLocation, string?> terrain, Func<string?, bool> enterable,\n"
     "        Func<string, string?> halfSquadOf, int? month, string? balance = null, Func<BoardLocation, BoardLocation?>? otherHalf = null)\n", 1),
    ("                counter.At is { } at ? new SetupLocationFacts(at.ToString(), at.Board.Value, at.Hex.ToString(), at.Hex.RowNumber) : null, counter.Concealed, counter.Hidden, counter.Dummy,",
     "                counter.At is { } at ? LocationFacts(at, otherHalf) : null, counter.Concealed, counter.Hidden, counter.Dummy,", 1),
    ("    /// <summary>Whether a Location lies in a setup area of the card (R17.8, R19.3): a building's hexes, or a board's hex numbers.</summary>\n",
     "    /// <summary>A Location as the setup's facts, with the other board's name for a butted half-hex (A2.3; pass 40).</summary>\n"
     "    private static SetupLocationFacts LocationFacts(BoardLocation at, Func<BoardLocation, BoardLocation?>? otherHalf) =>\n"
     "        new(at.ToString(), at.Board.Value, at.Hex.ToString(), at.Hex.RowNumber)\n"
     "        {\n"
     "            OtherHalf = otherHalf?.Invoke(at) is { } other ? new SetupLocationFacts(other.ToString(), other.Board.Value, other.Hex.ToString(), other.Hex.RowNumber) : null,\n"
     "        };\n\n"
     "    /// <summary>Whether a Location lies in a setup area of the card (R17.8, R19.3): a building's hexes, or a board's hex numbers.</summary>\n", 1),
])
apply("LimboDancer.Domains.Asl.Play/GamePlanner.CardSetup.cs", [
    ("            state.Scenario?.Balance);\n    }\n",
     "            state.Scenario?.Balance, at => OtherHalf(state, at));\n    }\n\n"
     "    /// <summary>\n"
     "    /// The same hex as the other board names it, for a half-hex butted against another board's on a placed map (A2.3, p. 45; pass 40); null for a\n"
     "    /// full hex, a half-hex on the map's edge, and a map that is not placed.\n"
     "    /// </summary>\n"
     "    internal BoardLocation? OtherHalf(GameState state, BoardLocation at)\n"
     "    {\n"
     "        if (!state.Map.IsPlaced || Composed(state)?.Layout is not { } layout || layout.Locate(at.Board, at.Hex) is not { } index)\n"
     "        {\n"
     "            return null;\n"
     "        }\n\n"
     "        var other = layout.Names(index).Where(name => name != (at.Board, at.Hex)).Select(name => ((BoardRef Board, HexName Hex)?)name).FirstOrDefault();\n"
     "        return other is { } half ? new BoardLocation(half.Board, half.Hex, at.Level) : null;\n"
     "    }\n", 1),
])
print("ok")
