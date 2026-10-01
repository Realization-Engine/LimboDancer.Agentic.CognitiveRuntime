using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// The backlog pass 17 scenario cards (rulings R17.1 to R17.11): the card format, its reader and validation, the two cards adapted from legacy
/// cards to the registered rulebook, and the counters they add to catalog 1.11.0 (with The Tractor Works, pass 17b, and 1.12.0). Boards 01, 02, and 04 from their Hex Fact oracle fixtures.
/// </summary>
public sealed class BacklogPass17Tests
{
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Cards = ["gambit", "guards-counterattack", "tractor-works"];
    private static readonly string[] ExitHexes = ["I1", "Q1", "Y1"];
    private static readonly string[] StoneHexes = ["F5", "K5", "I7", "M7", "M9", "N4", "J2", "M2", "F3"];

    private static ScenarioCard Card(string name)
    {
        var read = ScenarioCards.Read(name, Catalog)!;
        Assert.True(read.IsValid, string.Join("; ", read.Diagnostics));
        return read.Card!;
    }

    private static JsonObject Json(string name) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "ASL", "units", "scenarios", name + ".scenario-card.json")))!.AsObject();

    private static IReadOnlyList<string> Diagnostics(JsonObject card) => ScenarioCards.Parse("changed", card.ToJsonString(), Catalog).Diagnostics;

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "docs", "ASL", "SourceRegistry")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root is not above the test output.");
    }

    private static Dictionary<string, JsonElement> Board(string board)
    {
        using var file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Oracle", board + ".hexfacts.json.gz"));
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var document = JsonDocument.Parse(gzip);
        return document.RootElement.GetProperty("hexes").EnumerateArray().ToDictionary(hex => hex.GetProperty("hex").GetString()!, hex => hex.Clone());
    }

    private static string Terrain(JsonElement hex) => hex.GetProperty("center").GetProperty("terrain").GetString()!;

    private static readonly string[] Columns = [.. Enumerable.Range(0, 26).Select(index => ((char)('A' + index)).ToString()), "AA", "BB", "CC", "DD", "EE", "FF", "GG"];

    private static (int Column, int Number) Split(string hex)
    {
        var letters = new string([.. hex.TakeWhile(char.IsLetter)]);
        return (Array.IndexOf(Columns, letters), int.Parse(hex[letters.Length..], System.Globalization.CultureInfo.InvariantCulture));
    }

    // The neighbor across a hexside of a geomorphic board (side 0 north, clockwise); odd columns (B, D, ...) sit half a hex lower.
    private static string? Neighbor(string hex, int side)
    {
        var (column, number) = Split(hex);
        var odd = column % 2 == 1;
        (int Column, int Number) offset = side switch
        {
            0 => (0, -1),
            1 => odd ? (1, 0) : (1, -1),
            2 => odd ? (1, 1) : (1, 0),
            3 => (0, 1),
            4 => odd ? (-1, 1) : (-1, 0),
            _ => odd ? (-1, 0) : (-1, -1),
        };
        var next = column + offset.Column;
        return next >= 0 && next < Columns.Length ? $"{Columns[next]}{number + offset.Number}" : null;
    }

    /// <summary>The hexes of the building a hex is in: joined across hexsides whose terrain is the building's.</summary>
    private static HashSet<string> Building(Dictionary<string, JsonElement> board, string start)
    {
        var found = new HashSet<string>(StringComparer.Ordinal) { start };
        var open = new Stack<string>([start]);
        while (open.TryPop(out var hex))
        {
            foreach (var side in board[hex].GetProperty("hexsides").EnumerateArray()
                .Where(side => side.GetProperty("terrain").GetString()?.Contains("Building", StringComparison.Ordinal) == true))
            {
                if (Neighbor(hex, side.GetProperty("side").GetInt32()) is { } next && board.TryGetValue(next, out var other)
                    && Terrain(other).Contains("Building", StringComparison.Ordinal) && found.Add(next))
                {
                    open.Push(next);
                }
            }
        }

        return found;
    }

    [Fact]
    public void BothCardsAreEmbeddedAndValid()
    {
        // Pass 26 (ruling R26.7): the manufactured Armor Test is embedded beside the ported cards; it adapts no legacy card.
        Assert.Equal(["armor-test", .. Cards], ScenarioCards.Names);
        foreach (var name in Cards)
        {
            var card = Card(name);
            Assert.Equal("asl-scenario-a1@1.13.0", card.Catalog);
            Assert.Contains("registered rulebook", card.Source.Basis, StringComparison.Ordinal);
            Assert.Contains("paraphrased", card.Source.Adaptation[0], StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheGuardsCounterattackPresentsWhatTheGameKnows()
    {
        var card = Card("guards-counterattack");
        Assert.Equal((6, 10, 1942), (card.Date.Day, card.Date.Month, card.Date.Year));
        Assert.Equal(("german", "russian", 5, false), (card.Turns.SetsUpFirst, card.Turns.MovesFirst, card.Turns.Count, card.Turns.HalfTurn));
        Assert.Null(card.ScenarioDefender);
        var german = card.Sides[0];
        var russian = card.Sides[1];
        Assert.Equal((6, 4, "bottom"), (german.San, german.Groups[0].Elr, german.FriendlyEdge.Edge));
        Assert.Equal((6, "top"), (russian.San, russian.FriendlyEdge.Edge));
        Assert.All(russian.Groups, group => Assert.Equal(3, group.Elr));
        Assert.Equal(13, german.Groups[0].Units.Where(unit => unit.Definition == "attacker-squad").Sum(unit => unit.Count));
        Assert.Equal(12, russian.Groups[1].Units.Single(unit => unit.Definition == "defender-guards-squad").Count);
        Assert.Equal("control", card.VictoryConditions.Kind);
        Assert.Empty(card.Tokens);
    }

    [Fact]
    public void TheIntegrityTotalsAreTheCatalogBpvOfTheStartingMmc()
    {
        // A16.1: 13 German 4-6-7 at BPV 10; 9 Russian 4-4-7 at 7 and 12 Guards 6-2-8 at 12. The legacy card printed the same [130] and [207].
        var card = Card("guards-counterattack");
        Assert.Equal(130, ScenarioCards.IntegrityBpv(card, card.Sides[0], Catalog));
        Assert.Equal(207, ScenarioCards.IntegrityBpv(card, card.Sides[1], Catalog));
        Assert.Equal((130, 207), (card.Sides[0].IntegrityBpv, card.Sides[1].IntegrityBpv));
        var changed = Json("guards-counterattack");
        changed["sides"]![0]!["integrityBpv"] = 131;
        Assert.Contains(Diagnostics(changed), item => item.StartsWith("card.integrity:", StringComparison.Ordinal) && item.Contains("130", StringComparison.Ordinal));
    }

    [Fact]
    public void GambitPrintsNoIntegrityTotalBecauseTheGermansStartWithEightSquads()
    {
        var card = Card("gambit");
        Assert.Equal(8.0, ScenarioCards.SquadEquivalents(card, card.Sides[1], Catalog));
        Assert.All(card.Sides, side => Assert.Null(side.IntegrityBpv));
        var changed = Json("gambit");
        changed["sides"]![0]!["integrityBpv"] = 156;
        Assert.Contains(Diagnostics(changed), item => item.Contains("fewer than ten squad-equivalents", StringComparison.Ordinal));
    }

    [Fact]
    public void GambitPresentsItsEntryExitAndBoards()
    {
        var card = Card("gambit");
        Assert.Equal((21, 5, 1941), (card.Date.Day, card.Date.Month, card.Date.Year));
        Assert.Equal(("british", "german", 8), (card.Turns.SetsUpFirst, card.Turns.MovesFirst, card.Turns.Count));
        Assert.Equal([("bd04", 0, 0, false), ("bd02", 0, 1, true)], card.Boards.Select(board => (board.Board, board.Column, board.Row, board.Reversed)));
        var british = card.Sides[0];
        Assert.Equal(("top", "entry"), (british.FriendlyEdge.Edge, british.FriendlyEdge.Basis));
        Assert.Contains(british.Groups[0].Areas, area => area is { Kind: "entry", Turn: 1, Edge: "top" });
        Assert.Contains(british.Groups[0].Areas, area => area is { Kind: "hex-numbers", Board: "bd04", From: 5, To: 7 });
        Assert.Equal(("bottom", 4, 4), (card.Sides[1].FriendlyEdge.Edge, card.Sides[1].San, card.Sides[1].Groups[0].Elr));
        Assert.Equal(2, british.Groups[0].Units.Single(unit => unit.Definition == "british-leader-8-0").Count);
        Assert.Contains("A25.44", string.Join(" ", card.Source.Adaptation), StringComparison.Ordinal);
        Assert.Equal("exit", card.VictoryConditions.Kind);

        // The exit hexes lie on board 2, which is turned 180 degrees so that its hexrow 1 is the south edge.
        var board2 = Board("bd02");
        Assert.All(ExitHexes, hex => Assert.Contains("Road", Terrain(board2[hex]), StringComparison.Ordinal));
    }

    [Fact]
    public void TheGuardsCounterattackSetupBuildingsAreOnBoard1AndItsVictoryConditionsCountOnlyStone()
    {
        var board = Board("bd01");
        var card = Card("guards-counterattack");
        var areas = card.Sides.SelectMany(side => side.Groups).SelectMany(group => group.Areas).ToArray();
        var hexes = areas.Select(area => area.Id).ToArray();
        Assert.Equal(10, hexes.Length);
        Assert.All(areas, area => Assert.Equal(Building(board, area.Id).Order(StringComparer.Ordinal), area.Hexes!.Order(StringComparer.Ordinal)));
        Assert.Equal("Wooden Building", Terrain(board["N2"]));
        Assert.All(StoneHexes, hex => Assert.StartsWith("Stone Building", Terrain(board[hex]), StringComparison.Ordinal));
        Assert.Contains("N2 is wooden", card.VictoryConditions.Text, StringComparison.Ordinal);

        // Every setup building is its own building: no two sides share one.
        var buildings = hexes.Select(hex => Building(board, hex)).ToArray();
        for (var first = 0; first < buildings.Length; first++)
        {
            for (var second = first + 1; second < buildings.Length; second++)
            {
                Assert.False(buildings[first].Overlaps(buildings[second]), $"{hexes[first]} and {hexes[second]} are one building");
            }
        }
    }

    [Fact]
    public void TheGuardsCounterattackEdgesFollowFromItsSetup()
    {
        // A20.53 (ruling R17.7): in every hex column the German setup buildings lie south of the Russian ones, with no enemy between either and its edge.
        var board = Board("bd01");
        var card = Card("guards-counterattack");
        HashSet<string> Hexes(int side) => [.. card.Sides[side].Groups.SelectMany(group => group.Areas).SelectMany(area => area.Hexes!)];
        var german = Hexes(0).Select(Split).ToArray();
        var russian = Hexes(1).Select(Split).ToArray();
        Assert.All(german, hex => Assert.DoesNotContain(russian, other => other.Column == hex.Column && other.Number > hex.Number));
        Assert.All(russian, hex => Assert.DoesNotContain(german, other => other.Column == hex.Column && other.Number < hex.Number));
        Assert.Contains(german, hex => hex.Number == 4);
        Assert.Contains(russian, hex => hex.Number == 5);
    }

    [Fact]
    public void TheCardsCountersComeFromTheCatalogAndTheManufacturedOnesAreLabeled()
    {
        // The pass 17 HMG, LMG, and ATR, and the MGs manufactured at unit step 23 (sheet MFG, R0.3).
        string[] mfg = ["attacker-hmg", "british-lmg", "british-atr", "attacker-lmg", "attacker-mmg", "defender-mmg", "defender-hmg", "defender-lmg", "attacker-ft",
            "attacker-dc"];
        foreach (var name in Cards)
        {
            foreach (var unit in Card(name).Sides.SelectMany(side => side.Groups).SelectMany(group => group.Units))
            {
                var definition = Catalog.Definition(unit.Definition)!;
                Assert.Equal(mfg.Contains(unit.Definition), ScenarioCards.Manufactured(definition));
            }
        }

        var squad = Catalog.Definition("attacker-elite-squad-5-4-8")!;
        Assert.Equal(("NCC", 5, 4, 8, 13), (squad.Counter.Sheet, Number(squad, "front", "asl:firepower"), Number(squad, "front", "asl:range"),
            Number(squad, "front", "asl:morale"), Number(squad, "broken", "asl:bpv")));
        var half = Catalog.Definition("attacker-elite-half-squad-2-3-8")!;
        Assert.Equal((2, 3, 8, 5), (Number(half, "front", "asl:firepower"), Number(half, "front", "asl:range"), Number(half, "front", "asl:morale"),
            Number(half, "broken", "asl:bpv")));
        var hmg = Catalog.Definition("attacker-hmg")!;
        Assert.Equal((7, 16, 3), (Number(hmg, "front", "asl:firepower"), Number(hmg, "front", "asl:range"), Number(hmg, "front", "asl:rate-of-fire")));
        var mortar = Catalog.Definition("british-light-mortar")!;
        Assert.Equal(("OLG", 51, 2, 11, 2), (mortar.Counter.Sheet, Number(mortar, "front", "asl:caliber"), Number(mortar, "front", "asl:range-minimum"),
            Number(mortar, "front", "asl:range-maximum"), Number(mortar, "front", "asl:rate-of-fire")));

        // The legacy Russian 9-0 is the 9-0 Commissar of A25.22 (ruling R17.9): the registered leader table has no plain Russian 9-0.
        Assert.Null(Catalog.Definition("defender-leader-9-0"));
        Assert.Contains(Card("guards-counterattack").Sides[1].Groups[0].Units, unit => unit is { Definition: "defender-commissar-9-0", Area: "N4" });
    }

    private static int? Number(UnitDefinition definition, string face, string name) => definition.Printed(face, name)?.Value?.Number;

    [Fact]
    public void TheTractorWorksPresentsItsSequentialSetupDummiesAndDieRoll()
    {
        // Pass 17b (rulings R17.12, R17.13): the 308th sets up first in X3, the Germans second, the 295th last; a die roll decides the first move.
        var card = Card("tractor-works");
        Assert.Equal(("russian", 8), (card.Turns.SetsUpFirst, card.Turns.Count));
        Assert.Null(card.Turns.MovesFirst);
        Assert.Contains("die roll", card.Turns.MovesFirstNote, StringComparison.Ordinal);
        var russian = card.Sides[0];
        var german = card.Sides[1];
        Assert.Equal([1, 3], russian.Groups.Select(group => group.SetupOrder!.Value));
        Assert.All(german.Groups, group => Assert.Equal(2, group.SetupOrder));
        Assert.Equal((18, 12), (russian.Groups[0].Dummies!.Value, german.Groups[1].Dummies!.Value));
        Assert.Equal((266, 226), (ScenarioCards.IntegrityBpv(card, russian, Catalog), ScenarioCards.IntegrityBpv(card, german, Catalog)));
        Assert.Equal((266, 226), (russian.IntegrityBpv!.Value, german.IntegrityBpv!.Value));
        Assert.Equal(("top", "setup", "bottom", "setup"), (russian.FriendlyEdge.Edge, russian.FriendlyEdge.Basis, german.FriendlyEdge.Edge, german.FriendlyEdge.Basis));
        Assert.All(card.SpecialRules, rule => Assert.Equal("not-enforced", rule.Status));
        Assert.Equal(["A26.1", "A26.11", "A26.13"], card.VictoryConditions.Rules);
    }

    [Fact]
    public void TheTractorWorksSetupBuildingsAreWholeBuildingsOfBoard1()
    {
        var board = Board("bd01");
        var card = Card("tractor-works");
        var areas = card.Sides.SelectMany(side => side.Groups).SelectMany(group => group.Areas).ToArray();
        Assert.All(areas, area => Assert.Equal(Building(board, area.Id).Order(StringComparer.Ordinal), area.Hexes!.Order(StringComparer.Ordinal)));
        Assert.Equal(9, areas.Single(area => area.Id == "X3").Hexes!.Count);
        Assert.All(areas, area => Assert.StartsWith("Stone Building", Terrain(board[area.Id]), StringComparison.Ordinal));

        // A20.53 (ruling R17.13; referee, pass 17b): in every hex column the Russian setup hexes lie north of the German ones, so Russian north and
        // German south; a west or east edge would have enemy between (the 308th in X3 has Germans west and east of it in its hex rows).
        var russian = card.Sides[0].Groups.SelectMany(group => group.Areas).SelectMany(area => area.Hexes!).Select(Split).ToArray();
        var germans = card.Sides[1].Groups.SelectMany(group => group.Areas).SelectMany(area => area.Hexes!).Select(Split).ToArray();
        Assert.All(russian, hex => Assert.DoesNotContain(germans, other => other.Column == hex.Column && other.Number < hex.Number));
        var x3 = areas.Single(area => area.Id == "X3").Hexes!.Select(Split).ToArray();
        Assert.Contains(germans, hex => x3.Any(other => other.Number == hex.Number && other.Column > hex.Column));
        Assert.Contains(germans, hex => x3.Any(other => other.Number == hex.Number && other.Column < hex.Column));
    }

    [Fact]
    public void TheTractorWorksCountersComeFromTheChartAndMfg()
    {
        var squad = Catalog.Definition("attacker-elite-squad-8-3-8")!;
        Assert.Equal(("NCC", 8, 3, 8, 16), (squad.Counter.Sheet, Number(squad, "front", "asl:firepower"), Number(squad, "front", "asl:range"),
            Number(squad, "front", "asl:morale"), Number(squad, "broken", "asl:bpv")));
        Assert.Equal(3, Number(squad, "front", "asl:smoke-exponent"));
        var half = Catalog.Definition("attacker-elite-half-squad-3-3-8")!;
        Assert.Equal((3, 3, 8, 6), (Number(half, "front", "asl:firepower"), Number(half, "front", "asl:range"), Number(half, "front", "asl:morale"),
            Number(half, "broken", "asl:bpv")));
        var hmg = Catalog.Definition("defender-hmg")!;
        Assert.True(ScenarioCards.Manufactured(hmg));
        Assert.Equal((6, 12, 3, 12), (Number(hmg, "front", "asl:firepower"), Number(hmg, "front", "asl:range"), Number(hmg, "front", "asl:rate-of-fire"),
            Number(hmg, "front", "asl:breakdown")));
    }

    [Fact]
    public void ASequentialSetupDieRollAndDummiesAreChecked()
    {
        var gap = Json("tractor-works");
        gap["sides"]![0]!["groups"]![1]!["setupOrder"] = 4;
        Assert.Contains(Diagnostics(gap), item => item.Contains("numbers the groups from 1", StringComparison.Ordinal));
        var wrongFirst = Json("tractor-works");
        wrongFirst["turns"]!["setsUpFirst"] = "german";
        Assert.Contains(Diagnostics(wrongFirst), item => item.Contains("beginning with the side that sets up first", StringComparison.Ordinal));
        var silent = Json("tractor-works");
        silent["turns"]!.AsObject().Remove("movesFirstNote");
        Assert.Contains(Diagnostics(silent), item => item.StartsWith("card.turns:", StringComparison.Ordinal));
        var negative = Json("tractor-works");
        negative["sides"]![1]!["groups"]![1]!["dummies"] = -1;
        Assert.Contains(Diagnostics(negative), item => item.Contains("\"?\"", StringComparison.Ordinal));
    }

    [Fact]
    public void AScenarioDefenderMustFaceASideThatEntersWhollyFromOffboard()
    {
        var changed = Json("guards-counterattack");
        changed["scenarioDefender"] = "german";
        Assert.Contains(Diagnostics(changed), item => item.StartsWith("card.defender:", StringComparison.Ordinal));

        var entering = Json("gambit");
        entering["scenarioDefender"] = "german";
        var british = entering["sides"]![0]!["groups"]![0]!.AsObject();
        british["areas"] = new JsonArray(new JsonObject { ["id"] = "entry", ["kind"] = "entry", ["turn"] = 1, ["edge"] = "top" });
        Assert.DoesNotContain(Diagnostics(entering), item => item.StartsWith("card.defender:", StringComparison.Ordinal));

        // The Defender itself sets up wholly or partly on board (referee, pass 17).
        entering["scenarioDefender"] = "british";
        Assert.Contains(Diagnostics(entering), item => item.Contains("sets up wholly or partly on board", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("night:3", true)]
    [InlineData("weather:rain", true)]
    [InlineData("weather:fog", false)]
    [InlineData("weather:extreme-winter", false)]
    public void TheSsrTokensAreCheckedAsANewGameChecksThem(string token, bool accepted)
    {
        var changed = Json("gambit");
        changed["specialRules"]![0]!["status"] = "token";
        changed["specialRules"]![0]!["tokens"] = new JsonArray(token);
        Assert.Equal(accepted, !Diagnostics(changed).Any(item => item.StartsWith("card.ssr:", StringComparison.Ordinal)));
    }

    [Fact]
    public void AnSsrSaysWhetherTheGameReadsIt()
    {
        var tokenless = Json("gambit");
        tokenless["specialRules"]![0]!["status"] = "token";
        Assert.Contains(Diagnostics(tokenless), item => item.Contains("has tokens exactly when", StringComparison.Ordinal));
        var silent = Json("gambit");
        silent["specialRules"]![1]!.AsObject().Remove("note");
        Assert.Contains(Diagnostics(silent), item => item.Contains("is not enforced and says why", StringComparison.Ordinal));
    }

    [Fact]
    public void ACardCitesOnlyComparedRulesAndRulings()
    {
        var changed = Json("guards-counterattack");
        changed["victoryConditions"]!["rules"] = new JsonArray("A26.1", "A99.9");
        Assert.Contains(Diagnostics(changed), item => item.Contains("'A99.9'", StringComparison.Ordinal));

        // The citable fragments are exactly the subjects of the pass 17 PDF comparison.
        using var comparison = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "ASL", "SourceRegistry", "asl-scenario-a1.pass17-pdf-comparison.json")));
        Assert.Equal(ScenarioCards.CitableRules, comparison.RootElement.GetProperty("subjects").EnumerateArray().Select(subject => subject.GetProperty("ruleId").GetString()));
    }

    [Fact]
    public void ACounterMustBeInTheCatalogAndOfItsSide()
    {
        var unknown = Json("gambit");
        unknown["sides"]![1]!["groups"]![0]!["units"]![0]!["definition"] = "attacker-panther";
        Assert.Contains(Diagnostics(unknown), item => item.Contains("'attacker-panther' is not in the catalog", StringComparison.Ordinal));
        var wrong = Json("gambit");
        wrong["sides"]![1]!["groups"]![0]!["units"]![0]!["definition"] = "british-elite-squad";
        Assert.Contains(Diagnostics(wrong), item => item.Contains("is not german", StringComparison.Ordinal));
    }

    [Fact]
    public void TheNumbersStayInsideTheirRules()
    {
        var changed = Json("gambit");
        changed["sides"]![0]!["san"] = 8;
        changed["sides"]![1]!["groups"]![0]!["elr"] = 6;
        changed["sides"]![0]!["friendlyEdge"]!["edge"] = "north";
        var found = Diagnostics(changed);
        Assert.Contains(found, item => item.StartsWith("card.san:", StringComparison.Ordinal));
        Assert.Contains(found, item => item.StartsWith("card.elr:", StringComparison.Ordinal));
        Assert.Contains(found, item => item.StartsWith("card.edge:", StringComparison.Ordinal));
    }

    [Fact]
    public void AnUnknownFieldOrAWrongCatalogIsRefused()
    {
        var typo = Json("gambit");
        typo["victoryCondition"] = "x";
        Assert.Contains(Diagnostics(typo), item => item.StartsWith("card.json:", StringComparison.Ordinal));
        var old = Json("gambit");
        old["catalog"] = "asl-scenario-a1@1.10.0";
        Assert.Contains(Diagnostics(old), item => item.StartsWith("card.catalog:", StringComparison.Ordinal));
    }

    [Fact]
    public void ASetupAreaMustBeOnTheCardsBoards()
    {
        var anchor = Json("guards-counterattack");
        anchor["sides"]![0]!["groups"]![0]!["areas"]![0]!["hexes"] = new JsonArray("F6", "G6");
        Assert.Contains(Diagnostics(anchor), item => item.StartsWith("card.setup:", StringComparison.Ordinal));
        var changed = Json("gambit");
        changed["sides"]![1]!["groups"]![0]!["areas"]![0]!["board"] = "bd05";
        Assert.Contains(Diagnostics(changed), item => item.StartsWith("card.setup:", StringComparison.Ordinal));
        var late = Json("gambit");
        late["sides"]![0]!["groups"]![0]!["areas"]![1]!["turn"] = 9;
        Assert.Contains(Diagnostics(late), item => item.StartsWith("card.setup:", StringComparison.Ordinal));
    }
}
