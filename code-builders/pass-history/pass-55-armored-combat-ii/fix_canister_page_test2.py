"""The Canister page test: the squad stands in any attacked hex of an aim the small board allows (review fixes of pass 55)."""
import io

PATH = r"E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/PlayPageOrdnanceTests.cs"
text = io.open(PATH, encoding="utf-8", newline="").read()
start = text.index("    /// <summary>\n    /// A hex, a facing, and an aim of the small test board")
end = text.index("    [Fact]\n    public void AGunThatListsCanisterAimsItAtAVertexOfItsCoveredArc()")
helper = '''    /// <summary>
    /// A hex, a facing, and an aim of the small test board from which a Canister shot attacks a hex in Open Ground at the firing hex's level,
    /// with no hexside terrain, where the target squad stands: a hex of the aim's cluster, or the adjacent hex on its LOS. C8.41 (the referee's
    /// review of pass 55): the adjacent hex is attacked with a cluster that is, so a hex of the cluster is at the Gun's level and in its LOS, as
    /// the board's own LOS reads it. No hex of the aim is seen through a Hindrance, which the Fire package would not attribute.
    /// </summary>
    private (string Gun, string Facing, int Aim, string Squad, bool Adjacent) CanisterSpot()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        var los = context.Services.GetRequiredService<StudioLos>();
        bool Open(HexName? hex) => hex is { } name && handle.HexFacts(name) is { Center.Terrain.Name: "Open Ground" } facts
            && facts.Hexsides.All(item => item.HexsideTerrain is null && !item.Cliff);

        // Every order of so many steps across the left and the right hexside of the Covered Arc.
        static IEnumerable<int[]> Orders(int lefts, int rights, int left, int right) =>
            lefts == 0 && rights == 0 ? [[]]
            : (lefts > 0 ? Orders(lefts - 1, rights, left, right).Select(rest => (int[])[left, .. rest]) : [])
                .Concat(rights > 0 ? Orders(lefts, rights - 1, left, right).Select(rest => (int[])[right, .. rest]) : []);
        foreach (var index in handle.Geometry.Hexes())
        {
            var gun = handle.Geometry.NameOf(index);
            var level = handle.HexFacts(gun)!.BaseLevel;
            if (!Open(gun))
            {
                continue;
            }

            LimboDancer.Domains.Asl.Maps.Los.LosResult? Read(HexName far) => los.Check(FakeBoardProvider.Board, $"{Board}:{gun}:0", $"{Board}:{far}:0").Result;
            bool Attacked(HexName hex) => handle.HexFacts(hex)!.BaseLevel == level && Read(hex) is { IsBlocked: false, Hindrance: 0 };
            bool Passed(HexName hex) => handle.HexFacts(hex)!.BaseLevel != level || Read(hex) is { IsBlocked: true };
            foreach (var facing in Enum.GetValues<LimboDancer.Domains.Asl.Units.Documents.UnitFacing>())
            {
                var (left, right) = LimboDancer.Domains.Asl.Rules.ScenarioA1CanisterCalculator.ArcSides((int)facing);
                HexName? At(LimboDancer.Domains.Asl.Rules.CanisterOffset offset) => Orders(offset.Left, offset.Right, left, right)
                    .Select(order => order.Aggregate((HexName?)gun, (at, side) => at is { } hex ? handle.Neighbor(hex, (HexsideDirection)side) : null))
                    .FirstOrDefault(hex => hex is not null);
                foreach (var aim in LimboDancer.Domains.Asl.Rules.ScenarioA1CanisterCalculator.Aims.Where(item => item.Adjacent.Count == 1))
                {
                    HexName[] cluster = [.. aim.Cluster.Select(At).OfType<HexName>()];
                    HexName[] adjacent = [.. aim.Adjacent.Select(At).OfType<HexName>()];
                    if (!cluster.Any(Attacked) || !cluster.Concat(adjacent).All(hex => Attacked(hex) || Passed(hex)))
                    {
                        continue;
                    }

                    if (cluster.Where(hex => Attacked(hex) && Open(hex)).Select(hex => (HexName?)hex).FirstOrDefault() is { } inCluster)
                    {
                        return ($"{Board}:{gun}:0", LimboDancer.Domains.Asl.Units.Documents.UnitFacings.Name(facing), aim.Aim, $"{Board}:{inCluster}:0", false);
                    }

                    if (adjacent.Where(hex => Attacked(hex) && Open(hex)).Select(hex => (HexName?)hex).FirstOrDefault() is { } beside)
                    {
                        return ($"{Board}:{gun}:0", LimboDancer.Domains.Asl.Units.Documents.UnitFacings.Name(facing), aim.Aim, $"{Board}:{beside}:0", true);
                    }
                }
            }
        }

        throw new InvalidOperationException("The test board has no open spot for a Canister aim.");
    }

'''
text = text[:start] + helper + text[end:]
swaps = [
    ('        Place(page, "g1", "attacker-squad", spot.Adjacent);', '        Place(page, "g1", "attacker-squad", spot.Squad);'),
    ('        Assert.EndsWith($"and {DisplayText.Place(1, spot.Adjacent)} at half FP: 1 unit in view", aim.TextContent, StringComparison.Ordinal);',
     '        Assert.Contains(DisplayText.Place(1, spot.Squad), aim.TextContent, StringComparison.Ordinal);\n'
     '        Assert.EndsWith(": 1 unit in view", aim.TextContent, StringComparison.Ordinal);\n'
     '        if (spot.Adjacent)\n        {\n'
     '            Assert.Contains($"and {DisplayText.Place(1, spot.Squad)} at half FP: ", aim.TextContent, StringComparison.Ordinal);\n        }'),
    ('        // A 2 and a 4: below the Depletion Number, the ROF kept; the squad in the adjacent hex passes whatever check the 6 FP attack gives it.',
     '        // A 2 and a 4: below the Depletion Number, the ROF kept; the squad passes whatever check the attack on its hex gives it.'),
    ('        var near = Assert.Single(fires, fire => fire.TargetLocation == spot.Adjacent);\n'
     '        Assert.Equal((fired.Rolls["canister"], 1, 6m), (near.Rolls["attack"], near.Facts.GetProperty("canister").GetProperty("adjacentHexes").GetInt32(),\n'
     '            near.Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetDecimal()));',
     '        var near = Assert.Single(fires, fire => fire.TargetLocation == spot.Squad);\n'
     '        Assert.Equal((fired.Rolls["canister"], spot.Adjacent ? 6m : 12m), (near.Rolls["attack"], near.Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetDecimal()));\n'
     '        Assert.Equal(spot.Adjacent, near.Facts.GetProperty("canister").TryGetProperty("adjacentHexes", out _));\n'
     '\n'
     '        // C8.41 (the referee\'s review): a Location of the cluster is attacked whenever the adjacent hex is.\n'
     '        Assert.Contains(fires, fire => !fire.Facts.GetProperty("canister").TryGetProperty("adjacentHexes", out _));'),
]
for old, new in swaps:
    count = text.count(old)
    print(count, old[:70].replace("\n", " "))
    if count != 1:
        raise SystemExit("not exactly one: " + old[:70])
    text = text.replace(old, new)
io.open(PATH, "w", encoding="utf-8", newline="").write(text)
