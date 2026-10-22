using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;

// Scratch LOS survey for the Tractor Works' 295th group setup plans. Not product code.
var options = new StudioOptions { VaslRoot = "E:/Archive/GitHub/dlandi/vasl", OracleFixtures = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle" };
var provider = new VaslBoardProvider(options);
// The board: bd01 unless LOS_BOARD names another (the "three" mode tries several).
var boardName = Environment.GetEnvironmentVariable("LOS_BOARD") is { Length: > 0 } named ? named : "bd01";
var load = provider.Load(BoardRef.Parse(boardName));
var board = load.Board ?? throw new InvalidOperationException(string.Join("; ", load.Diagnostics.Select(d => d.Message)));
Console.WriteLine($"# board {board.Title}, status {board.Status}");
var map = LosMap.ForGrid(board.Ref, board.Render.Grid, board.Facts, board.Catalog, board.Status is BoardStatus.Verified);

List<string> Levels(string hex)
{
    var found = new List<string>();
    for (var level = 0; level <= 3; level++)
    {
        var at = BoardLocation.Parse($"{boardName}:{hex}:{level}");
        try
        {
            LosCalculator.Check(map, at, at);
            found.Add($"{hex}:{level}");
        }
        catch (ArgumentException)
        {
        }
    }

    // The probe also accepts the plane above a building's top level; drop it.
    if (found.Count > 1)
    {
        found.RemoveAt(found.Count - 1);
    }

    return found;
}

string[] russian = ["P7", "P8", "P5", "Q6", "P3", "Q3", "Q4", "R1", "S1"];
var germanAreas = new Dictionary<string, string[]>
{
    ["Stahler U3"] = ["T2", "U2", "U3", "V2"],
    ["Stahler T4"] = ["S5", "T4"],
    ["Stahler R7"] = ["R7"],
    ["Stahler T7"] = ["T7", "U8"],
    ["AA4"] = ["AA4", "AA5", "BB4"],
    ["CC3"] = ["CC3", "DD3"],
    ["Y8"] = ["AA7", "Y7", "Y8", "Z6", "Z7"],
    ["CC7"] = ["CC7", "DD7"],
};
string[] works = ["W4", "W5", "X2", "X3", "X4", "X5", "Y3", "Y4", "Y5"];

string Read(string from, string to)
{
    var result = LosCalculator.Check(map, BoardLocation.Parse($"{boardName}:{from}"), BoardLocation.Parse($"{boardName}:{to}"));
    return result.Status == LosStatus.Clear ? $"r{result.Range}" + (result.Hindrance > 0 ? $"h{result.Hindrance}" : string.Empty)
        : result.Status == LosStatus.Blocked ? "-" : result.IsBlocked == true ? "-?" : $"r{result.Range}?";
}

var mode = args.Length > 0 ? args[0] : "setup";
var russianLocations = russian.SelectMany(Levels).ToArray();
var germanLocations = germanAreas.SelectMany(area => area.Value.SelectMany(Levels).Select(l => (Area: area.Key, Location: l))).ToArray();
if (mode == "setup")
{
    Console.WriteLine("## 295th Locations: German setup Locations in LOS (clear), and Works hexes in LOS");
    foreach (var from in russianLocations)
    {
        var seen = germanLocations.Select(g => (g, los: Read(from, g.Location))).Where(x => !x.los.StartsWith('-')).Select(x => $"{x.g.Location}({x.los})").ToArray();
        var worksSeen = works.Select(w => (w, los: Read(from, w + ":0"))).Where(x => !x.los.StartsWith('-')).Select(x => $"{x.w}({x.los})").ToArray();
        Console.WriteLine($"{from}: sees {seen.Length} German: {string.Join(' ', seen)} | Works: {string.Join(' ', worksSeen)}");
    }
}
else if (mode == "lanes")
{
    // Exposure of each ground hex in the area between the 295th and the Works to the German setup Locations.
    var columns = "OPQRSTUVWXY".Select(c => c.ToString()).ToArray();
    Console.WriteLine("## ground hexes: number of German setup Locations with clear LOS, and which areas");
    foreach (var column in columns)
    {
        for (var row = 0; row <= 10; row++)
        {
            var hex = column + row;
            if (Levels(hex).Count == 0)
            {
                continue;
            }

            var seen = germanLocations.Where(g => !Read(g.Location, hex + ":0").StartsWith('-')).ToArray();
            Console.WriteLine($"{hex}: {seen.Length} [{string.Join(", ", seen.GroupBy(s => s.Area).Select(a => $"{a.Key}:{a.Count()}"))}]");
        }
    }
}
else if (mode == "levels")
{
    foreach (var hex in russian.Concat(germanAreas.Values.SelectMany(v => v)).Concat(works))
    {
        Console.WriteLine($"{hex}: {string.Join(' ', Levels(hex))}");
    }
}
else if (mode == "pair")
{
    Console.WriteLine($"{args[1]} -> {args[2]}: {Read(args[1], args[2])}");
}
else if (mode == "routes")
{
    // A measurement layout read from a file: "russian <hex>" lines and "route <name> <hex> <hex> ..." lines. For each step of each route: whether
    // it is ADJACENT to the step before, its terrain, and which Russians have clear LOS to it (all at level 0).
    var russiansAt = new List<string>();
    var routes = new List<(string Name, string[] Hexes)>();
    foreach (var raw in File.ReadAllLines(args[1]))
    {
        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && parts[0] == "russian")
        {
            russiansAt.AddRange(parts.Skip(1));
        }
        else if (parts.Length >= 3 && parts[0] == "route")
        {
            routes.Add((parts[1], parts[2..]));
        }
    }

    string TerrainOf(string hex) => board.Facts[board.Facts.Geometry.IndexOf(HexName.Parse(hex))].Center.Terrain?.Name ?? "?";
    foreach (var (name, hexes) in routes)
    {
        Console.WriteLine($"{name}:");
        for (var i = 0; i < hexes.Length; i++)
        {
            var adjacent = i == 0 ? "start" : LosCalculator.Check(map, BoardLocation.Parse($"{boardName}:{hexes[i - 1]}:0"), BoardLocation.Parse($"{boardName}:{hexes[i]}:0")).Range == 1 ? "adj" : "NOT ADJACENT";
            var seenBy = russiansAt.Where(r => Read(r + ":0", hexes[i] + ":0") is var los && los.StartsWith('r') && !los.Contains('?')).ToArray();
            Console.WriteLine($"  {hexes[i],-4} {adjacent,-12} {TerrainOf(hexes[i]),-28} seen by {(seenBy.Length == 0 ? "none" : string.Join(' ', seenBy))}");
        }
    }
}
else if (mode == "terrain")
{
    // Each hex's center terrain, by column, as a short code: O open ground, B building, W woods, R road, and the first letters otherwise.
    var geometry = board.Facts.Geometry;
    var rows = new SortedDictionary<int, SortedDictionary<string, string>>();
    foreach (var index in geometry.Hexes())
    {
        var name = geometry.NameOf(index).ToString();
        var column = new string(name.TakeWhile(char.IsLetter).ToArray());
        var row = int.Parse(new string(name.SkipWhile(char.IsLetter).ToArray()));
        var facts = board.Facts[index];
        var terrain = facts.Center.Terrain?.Name ?? "?";
        var code = terrain switch
        {
            "Open Ground" => "O",
            "Woods" => "W",
            "Paved Road" or "Dirt Road" => "R",
            _ when terrain.Contains("Building", StringComparison.Ordinal) => "B",
            _ => terrain[..Math.Min(2, terrain.Length)],
        };
        if (!rows.TryGetValue(row, out var line))
        {
            rows[row] = line = new SortedDictionary<string, string>(Comparer<string>.Create((a, b) => a.Length != b.Length ? a.Length - b.Length : string.CompareOrdinal(a, b)));
        }

        line[column] = code + (facts.BaseLevel != 0 ? facts.BaseLevel.ToString() : string.Empty);
    }

    var columnsAll = rows.Values.SelectMany(line => line.Keys).Distinct().OrderBy(c => c.Length).ThenBy(c => c, StringComparer.Ordinal).ToArray();
    Console.WriteLine("row " + string.Join(' ', columnsAll.Select(c => c.PadLeft(2))));
    foreach (var (row, line) in rows)
    {
        Console.WriteLine($"{row,3} " + string.Join(' ', columnsAll.Select(c => (line.TryGetValue(c, out var code) ? code : ".").PadLeft(2))));
    }

    Console.WriteLine("# codes: " + string.Join(", ", board.Facts.Geometry.Hexes().Select(index => board.Facts[index].Center.Terrain?.Name ?? "?").Distinct()));
}
else if (mode == "three")
{
    // The Play Streamlining Plan's measurement game (section 1): three German and three Russian squads on Open Ground, every German in clear
    // LOS of every Russian, and each German with three Open Ground hexes ahead of it toward the Russians, each in clear LOS of every Russian.
    var geometry = board.Facts.Geometry;
    var open = new HashSet<string>(StringComparer.Ordinal);
    foreach (var index in geometry.Hexes())
    {
        var facts = board.Facts[index];
        if (facts.Center.Terrain?.Name == "Open Ground" && facts.BaseLevel == 0)
        {
            open.Add(geometry.NameOf(index).ToString());
        }
    }

    Console.WriteLine($"# {open.Count} Open Ground hexes at level 0, e.g. {string.Join(' ', open.Take(12))}");
    if (open.Count > 1)
    {
        var sample = open.Take(2).ToArray();
        Console.WriteLine($"# sample LOS {sample[0]} -> {sample[1]}: {Read(sample[0] + ":0", sample[1] + ":0")}");
    }
    static (string Column, int Row) Split(string hex) => (new string(hex.TakeWhile(char.IsLetter).ToArray()), int.Parse(new string(hex.SkipWhile(char.IsLetter).ToArray())));
    var columns = open.Select(hex => Split(hex).Column).Distinct().OrderBy(c => c.Length).ThenBy(c => c, StringComparer.Ordinal).ToList();
    bool Clear(string a, string b) => Read(a + ":0", b + ":0").StartsWith('r') && !Read(a + ":0", b + ":0").Contains('?');
    var found = 0;
    var stage = new int[3];
    for (var ci = 0; ci + 2 < columns.Count && found < 5; ci++)
    {
        string[] cols = [columns[ci], columns[ci + 1], columns[ci + 2]];
        for (var row = 1; row <= 10 && found < 5; row++)
        {
            for (var gap = 4; gap <= 8 && found < 5; gap++)
            {
                foreach (var direction in new[] { -1, 1 })
                {
                    var germans = cols.Select(c => c + row).ToArray();
                    var russians = cols.Select(c => c + (row + direction * gap)).ToArray();
                    if (!germans.Concat(russians).All(open.Contains))
                    {
                        continue;
                    }

                    var paths = cols.Select(c => Enumerable.Range(1, 3).Select(step => c + (row + direction * step)).ToArray()).ToArray();
                    stage[0]++;
                    if (stage[0] <= 3)
                    {
                        Console.WriteLine($"# candidate {string.Join(' ', germans)} vs {string.Join(' ', russians)}: "
                            + string.Join(' ', germans.SelectMany(g => russians.Select(r => $"{g}>{r}={Read(g + ":0", r + ":0")}"))));
                    }

                    if (!germans.All(g => russians.All(r => Clear(g, r))))
                    {
                        continue;
                    }

                    // The straight paths are reported, not required: the moves are plotted by hand, on Open Ground hexes in LOS of the Russians.
                    stage[1]++;
                    var seen = paths.SelectMany(path => path).Count(hex => open.Contains(hex) && russians.All(r => Clear(hex, r)));
                    if (seen >= 6)
                    {
                        stage[2]++;
                        found++;
                        Console.WriteLine($"germans {string.Join(' ', germans)} | russians {string.Join(' ', russians)} | straight-path hexes open and seen by all {seen} of 9");
                    }
                }
            }
        }
    }

    Console.WriteLine($"# {found} layouts; candidates with all six hexes open {stage[0]}, in mutual LOS {stage[1]}, with six or more path hexes open and seen {stage[2]}");

    // Clusters: three Russians within two hexes of each other, and three Germans within two hexes of each other, every German in clear LOS of
    // every Russian at 3 to 7 hexes; the hexes the Germans could move through are read by hand in the Studio.
    int? Range(string a, string b) => LosCalculator.Check(map, BoardLocation.Parse($"{boardName}:{a}:0"), BoardLocation.Parse($"{boardName}:{b}:0")).Range;
    var list = open.OrderBy(hex => hex, StringComparer.Ordinal).ToArray();
    var clusters = 0;
    foreach (var r1 in list)
    {
        var near = list.Where(x => x != r1 && Range(r1, x) is <= 2).ToArray();
        for (var i = 0; i < near.Length && clusters < 6; i++)
        {
            for (var j = i + 1; j < near.Length && clusters < 6; j++)
            {
                string[] russians = [r1, near[i], near[j]];
                var pool = list.Where(g => !russians.Contains(g) && russians.All(r => Range(g, r) is >= 3 and <= 7 && Clear(g, r))).ToArray();
                var g1 = pool.FirstOrDefault(g => pool.Count(o => o != g && Range(g, o) is <= 2) >= 2);
                if (g1 is null)
                {
                    continue;
                }

                var mates = pool.Where(o => o != g1 && Range(g1, o) is <= 2).Take(2).ToArray();
                clusters++;
                Console.WriteLine($"cluster russians {string.Join(' ', russians)} | germans {g1} {string.Join(' ', mates)} | {pool.Length} open hexes see all three Russians: {string.Join(' ', pool)}");
                break;
            }

            if (clusters >= 6)
            {
                break;
            }
        }

        if (clusters >= 6)
        {
            break;
        }
    }

    Console.WriteLine($"# {clusters} clusters");
}
