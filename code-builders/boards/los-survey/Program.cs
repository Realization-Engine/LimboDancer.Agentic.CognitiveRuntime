using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;

// Scratch LOS survey for the Tractor Works' 295th group setup plans. Not product code.
var options = new StudioOptions { VaslRoot = "E:/Archive/GitHub/dlandi/vasl", OracleFixtures = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle" };
var provider = new VaslBoardProvider(options);
var load = provider.Load(BoardRef.Parse("bd01"));
var board = load.Board ?? throw new InvalidOperationException(string.Join("; ", load.Diagnostics.Select(d => d.Message)));
Console.WriteLine($"# board {board.Title}, status {board.Status}");
var map = LosMap.ForGrid(board.Ref, board.Render.Grid, board.Facts, board.Catalog, board.Status is BoardStatus.Verified);

List<string> Levels(string hex)
{
    var found = new List<string>();
    for (var level = 0; level <= 3; level++)
    {
        var at = BoardLocation.Parse($"bd01:{hex}:{level}");
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
    var result = LosCalculator.Check(map, BoardLocation.Parse("bd01:" + from), BoardLocation.Parse("bd01:" + to));
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
