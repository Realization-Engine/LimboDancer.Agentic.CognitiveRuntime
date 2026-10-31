using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// B23.74 to B23.743, the Factory (pp. 139 and 140), as <see cref="ScenarioA1FactoryCalculator"/> decides it, the members that forward to it or read its
/// facts, and the contracts <c>b23.741-factory-tem</c> and <c>b23.742-factory-movement</c> (the B23.74 claim, rulings RB23F.1 to RB23F.14).
/// </summary>
public sealed class ScenarioA1FactoryCalculatorTests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;

    private static readonly string?[] SideNames = [null, "Breach", "Rowhouse Wall, 1 Level", "Stone Building, 2 Level"];

    private static readonly FactoryTemFacts Inside = new(true, FactoryFirerPlace.Inside, true, null, false, false, false, null);

    private static readonly FactoryStepFacts Within = new(true, true, true, false, true, false, false, false);

    private static readonly FactoryStepFacts FromOutside = new(false, true, false, false, false, false, false, false);

    [Fact]
    public void AFactoryExistsBySsrTokenOrPrintedTerrainAndItsHeightFollowsItsStairwell()
    {
        Assert.Equal("X3", ScenarioA1FactoryCalculator.FactoryToken("factory:X3"));
        Assert.Null(ScenarioA1FactoryCalculator.FactoryToken("factory:"));
        Assert.Null(ScenarioA1FactoryCalculator.FactoryToken("factory:X3:Y4"));
        Assert.Null(ScenarioA1FactoryCalculator.FactoryToken("factory:X 3"));
        Assert.Null(ScenarioA1FactoryCalculator.FactoryToken("fanatic:X3"));
        Assert.True(ScenarioA1FactoryCalculator.IsFactory(true, false));
        Assert.True(ScenarioA1FactoryCalculator.IsFactory(false, true));
        Assert.False(ScenarioA1FactoryCalculator.IsFactory(false, false));
        Assert.Equal(5, ScenarioA1FactoryCalculator.ObstacleHalfLevels(true));
        Assert.Equal(3, ScenarioA1FactoryCalculator.ObstacleHalfLevels(false));
    }

    [Fact]
    public void OnlyGroundLevelIsOccupiedAndTheRooftopOnlyBySsr()
    {
        Assert.True(ScenarioA1FactoryCalculator.OccupiableLevel(0, false, false));
        Assert.Null(ScenarioA1FactoryCalculator.OccupationRefusal(0, false, false));
        Assert.Contains("no upper level floors", ScenarioA1FactoryCalculator.OccupationRefusal(1, false, true)!, StringComparison.Ordinal);
        Assert.Contains("no cellar", ScenarioA1FactoryCalculator.OccupationRefusal(-1, false, true)!, StringComparison.Ordinal);
        Assert.Contains("B23.8", ScenarioA1FactoryCalculator.OccupationRefusal(3, true, false)!, StringComparison.Ordinal);
        Assert.True(ScenarioA1FactoryCalculator.OccupiableLevel(3, true, true));
        Assert.Equal(0, ScenarioA1FactoryCalculator.NonRooftopLevelsAbove());
        Assert.Contains("B23.87", ScenarioA1FactoryCalculator.LevelChangeRefusal("[X3]"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheTemFollowsTheRulesOrderAndTheFiveExampleAttacks()
    {
        // B23.741's example: E7 attacks D8 at +3, its LOS not traced entirely within the depiction; D6 attacks D9 at +1 through two Factory hexes.
        Assert.Equal(new FactoryTemVerdict(3, false, ScenarioA1FactoryCalculator.Tem(Inside with
        {
            TracedWithinDepiction = false
        }).Reason),
            ScenarioA1FactoryCalculator.Tem(Inside with
            {
                TracedWithinDepiction = false
            }));
        Assert.Equal((1, true), Pair(ScenarioA1FactoryCalculator.Tem(Inside)));
        Assert.Equal(2, ScenarioA1FactoryCalculator.Hindrance(2, FactoryFirerPlace.Inside));

        // B23.743: the firer in the rubble hex C6 attacks at +1 though the LOF leaves the depiction, with the Hindrance of the Factory hexes it traces.
        Assert.Equal((1, true), Pair(ScenarioA1FactoryCalculator.Tem(Inside with
        {
            Firer = FactoryFirerPlace.Rubble,
            TracedWithinDepiction = false
        })));
        Assert.Equal(1, ScenarioA1FactoryCalculator.Hindrance(1, FactoryFirerPlace.Rubble));

        // The outside firer over the adjacent rubble, entering across the depiction: +1; into B6, not crossing it: +3.
        var outside = Inside with
        {
            Firer = FactoryFirerPlace.Outside,
            TracedWithinDepiction = null
        };
        Assert.Equal((1, true), Pair(ScenarioA1FactoryCalculator.Tem(outside with
        {
            OverAdjacentRubbleEnteringAcrossDepiction = true
        })));
        Assert.Equal((3, false), Pair(ScenarioA1FactoryCalculator.Tem(outside with
        {
            OverAdjacentRubbleEnteringAcrossDepiction = false
        })));
        Assert.Equal((null, false), Pair(ScenarioA1FactoryCalculator.Tem(outside)));
        Assert.Equal(0, ScenarioA1FactoryCalculator.Hindrance(3, FactoryFirerPlace.Outside));

        // The rooftop firer: +1 and no Factory Hindrance.
        Assert.Equal((1, true), Pair(ScenarioA1FactoryCalculator.Tem(Inside with
        {
            Firer = FactoryFirerPlace.Rooftop,
            TracedWithinDepiction = null
        })));
        Assert.Equal(0, ScenarioA1FactoryCalculator.Hindrance(2, FactoryFirerPlace.Rooftop));
        Assert.Equal(0, ScenarioA1FactoryCalculator.Hindrance(0, FactoryFirerPlace.Inside));

        // Unknown is not evaluated; a wooden Factory is +2.
        Assert.Equal((null, false), Pair(ScenarioA1FactoryCalculator.Tem(Inside with
        {
            TracedWithinDepiction = null
        })));
        Assert.Equal((null, false), Pair(ScenarioA1FactoryCalculator.Tem(Inside with
        {
            Firer = null
        })));
        Assert.Equal((2, false), Pair(ScenarioA1FactoryCalculator.Tem(Inside with
        {
            Stone = false,
            TracedWithinDepiction = false
        })));
    }

    [Fact]
    public void IndirectFireAndResidualFpTakeTheBuildingsTemByTheirOwnFacts()
    {
        // Indirect Fire: the building's TEM, whatever the firer's place; Roofless (O5.45) is not evaluated.
        var indirect = Inside with
        {
            IndirectFire = true
        };
        Assert.Equal((3, false), Pair(ScenarioA1FactoryCalculator.Tem(indirect)));
        Assert.Equal((2, false), Pair(ScenarioA1FactoryCalculator.Tem(indirect with
        {
            Stone = false
        })));
        Assert.Equal((null, false), Pair(ScenarioA1FactoryCalculator.Tem(indirect with
        {
            Roofless = true
        })));

        // Residual FP: the building's TEM across a non-building hexside or in Bypass, else the Factory's +1; an unknown entry is not evaluated.
        var residual = new FactoryTemFacts(true, null, null, null, false, false, true, FactoryResidualEntry.BuildingHexside);
        Assert.Equal((1, true), Pair(ScenarioA1FactoryCalculator.Tem(residual)));
        Assert.Equal((3, false), Pair(ScenarioA1FactoryCalculator.Tem(residual with
        {
            ResidualEntry = FactoryResidualEntry.NonBuildingHexside
        })));
        Assert.Equal((3, false), Pair(ScenarioA1FactoryCalculator.Tem(residual with
        {
            ResidualEntry = FactoryResidualEntry.Bypass
        })));
        Assert.Equal((null, false), Pair(ScenarioA1FactoryCalculator.Tem(residual with
        {
            ResidualEntry = null
        })));
    }

    [Fact]
    public void InfantryPayOneMfWithinTheFactoryAndOpenGroundThroughAnEntranceFromOutside()
    {
        Assert.Equal(2, ScenarioA1FactoryCalculator.InfantryHalfMf(Within));
        Assert.Equal(2, ScenarioA1FactoryCalculator.InfantryHalfMf(Within with
        {
            FromFactory = false,
            FromRubbleOfFactory = true
        }));
        Assert.Null(ScenarioA1FactoryCalculator.InfantryHalfMf(Within with
        {
            AcrossFactoryHexside = false
        }));
        Assert.Null(ScenarioA1FactoryCalculator.InfantryHalfMf(FromOutside));
        Assert.Equal(2, ScenarioA1FactoryCalculator.InfantryHalfMf(FromOutside with
        {
            VehicularEntrance = true
        }));
        Assert.Null(ScenarioA1FactoryCalculator.InfantryHalfMf(FromOutside with
        {
            VehicularEntrance = true,
            EntranceRoofless = true
        }));

        // From inside, the Entrance gives nothing: leaving the Factory is the normal entry of the hex beyond.
        Assert.Null(ScenarioA1FactoryCalculator.InfantryHalfMf(new FactoryStepFacts(true, false, false, false, false, true, false, true)));
        Assert.True(ScenarioA1FactoryCalculator.TracedWithinFactory(true, true));
        Assert.False(ScenarioA1FactoryCalculator.TracedWithinFactory(true, false));
        Assert.False(ScenarioA1FactoryCalculator.TracedWithinFactory(false, true));
    }

    [Fact]
    public void AFullyTrackedAfvMovesWithinAtAQuarterOfItsAllotmentAndOthersOnlyThroughAnEntrance()
    {
        FactoryVehicleStepVerdict Step(FactoryStepFacts step, string type = "fully-tracked", int mp = 16, bool afv = true, bool bypass = false) =>
            ScenarioA1FactoryCalculator.VehicleStep(new FactoryVehicleStepFacts(type, afv, mp, true, step, bypass));

        // One-fourth, FRU, with a Bog Check at +1: 13, 16, and 17 MP give 4, 4, and 5 MP.
        Assert.Equal(new FactoryVehicleStepVerdict(8, true, 1, false, null), Step(Within, mp: 13));
        Assert.Equal(8, Step(Within, mp: 16).HalfMp);
        Assert.Equal(10, Step(Within, mp: 17).HalfMp);
        Assert.Contains("only a fully-tracked AFV", Step(Within, "half-tracked").Refusal!, StringComparison.Ordinal);
        Assert.Contains("only a fully-tracked AFV", Step(Within, afv: false).Refusal!, StringComparison.Ordinal);
        Assert.Equal(new FactoryVehicleStepVerdict(null, false, null, false, null), Step(Within, "half-tracked", bypass: true));

        // The Vehicular-Sized Entrance, friendly-controlled: Open Ground cost, in and out, for any vehicle.
        var entrance = FromOutside with
        {
            VehicularEntrance = true,
            EntranceFriendlyControlled = true
        };
        Assert.Equal(new FactoryVehicleStepVerdict(2, false, null, false, null), Step(entrance));
        Assert.Equal(8, Step(entrance, "truck", afv: false).HalfMp);
        var exit = new FactoryStepFacts(true, false, false, false, false, true, false, true);
        Assert.Equal(2, Step(exit, "half-tracked").HalfMp);

        // Enemy-controlled: entering is not the Factory's case (B23.41); leaving costs a fully-tracked AFV half its allotment, Bog +4 stone, and a rubble check.
        Assert.Equal(new FactoryVehicleStepVerdict(null, false, null, false, null), Step(entrance with
        {
            EntranceFriendlyControlled = false
        }));
        Assert.Equal(new FactoryVehicleStepVerdict(16, true, 4, true, null), Step(exit with
        {
            EntranceFriendlyControlled = false
        }));
        Assert.Equal(3, ScenarioA1FactoryCalculator.VehicleStep(new FactoryVehicleStepFacts("fully-tracked", true, 16, false, exit with
        {
            VehicularEntrance = false
        }, false)).BogDrm);
        Assert.Contains("leaves a Factory only", Step(exit with
        {
            VehicularEntrance = false
        }, "truck", afv: false).Refusal!, StringComparison.Ordinal);
        Assert.Contains("not reviewed", Step(entrance, "wheeled").Refusal!, StringComparison.Ordinal);

        Assert.True(ScenarioA1FactoryCalculator.VcaChangeBog(true));
        Assert.False(ScenarioA1FactoryCalculator.VcaChangeBog(false));
        Assert.Equal(("factory", 1), ScenarioA1FactoryCalculator.BogDrm());
        Assert.Equal(3, ScenarioA1FactoryCalculator.SniperTem(true));
        Assert.Equal(2, ScenarioA1FactoryCalculator.SniperTem(false));
    }

    [Fact]
    public void BuildingGoesOnForwardsItsFactoryClauseWithTheSameAnswerOnEveryInput()
    {
        static bool Old(bool hexsideBuilding, bool nextBuilding, string? sideTerrain, bool sideOutsideFactoryWall, string? hexsideTerrain, bool hexsideOutsideFactoryWall)
        {
            static bool Divides(string? name, bool outsideFactoryWall) => outsideFactoryWall || name == "Breach" || name?.StartsWith("Rowhouse Wall", StringComparison.Ordinal) == true;
            return hexsideBuilding && nextBuilding && !Divides(sideTerrain, sideOutsideFactoryWall) && !Divides(hexsideTerrain, hexsideOutsideFactoryWall);
        }

        bool[] flags = [false, true];
        var count = 0;
        foreach (var a in flags)
        {
            foreach (var b in flags)
            {
                foreach (var c in flags)
                {
                    foreach (var d in flags)
                    {
                        foreach (var side in SideNames)
                        {
                            foreach (var hexside in SideNames)
                            {
                                Assert.Equal(Old(a, b, side, c, hexside, d), ScenarioA1RoutCalculator.BuildingGoesOn(a, b, side, c, hexside, d));
                                count++;
                            }
                        }
                    }
                }
            }
        }

        Assert.Equal(256, count);
        Assert.True(ScenarioA1FactoryCalculator.DividedByFactoryWall(false, true));
        Assert.False(ScenarioA1FactoryCalculator.DividedByFactoryWall(false, false));
    }

    [Fact]
    public void LocationLosForwardsWithNoFactoryHindranceAndItsOverloadCountsAndAttributesTheFactory()
    {
        static (string? Refusal, FireLos? Los) Old(LosReadFacts los, int? month, bool sameLevel, Func<HashSet<int>, (int Drm, string? Reason)> vehicleHindrance)
        {
            var inSeason = month is >= 6 and <= 9;
            var attributed = los.Hindrances.All(entry => entry.Terrains.Count > 0 && entry.Terrains.All(item => item is "Brush" or "Grain" or "Marsh"));
            var mapRanges = los.Hindrances.Where(entry => entry.Terrains.Contains("Brush") || (sameLevel && entry.Terrains.Contains("Marsh")) || (inSeason && entry.Terrains.Contains("Grain")))
                .Select(entry => entry.Range).ToHashSet();
            var grain = los.Hindrances.Any(entry => entry.Terrains.Contains("Grain"));
            var (vehicleDrm, vehicleReason) = vehicleHindrance(mapRanges);
            var total = mapRanges.Count + vehicleDrm;
            return vehicleReason is not null ? (vehicleReason, null) : (null, new FireLos(los.Blocked || ScenarioA1FireMapRules.HindranceBlocks(total), total, attributed, grain));
        }

        LosHindranceFacts[][] hindrances =
        [
            [],
            [new(1, 1, ["Brush"])],
            [new(1, 1, ["Grain"]), new(2, 1, ["Marsh"])],
            [new(1, 1, ["Stone Factory, 2.5 Level"])],
            [new(1, 1, ["Brush"]), new(2, 1, ["Stone Factory, 2.5 Level"]), new(3, 1, ["Orchard"])],
            [new(1, 1, [])],
        ];
        Func<HashSet<int>, (int, string?)>[] vehicles = [_ => (0, null), ranges => (ranges.Count + 2, null), _ => (0, "a wreck read refuses")];
        foreach (var entries in hindrances)
        {
            foreach (var blocked in new[] { false, true })
            {
                var los = new LosReadFacts(true, "Clear", "", blocked, 4, entries);
                foreach (var month in new int?[] { null, 3, 7 })
                {
                    foreach (var sameLevel in new[] { false, true })
                    {
                        foreach (var vehicle in vehicles)
                        {
                            Assert.Equal(Old(los, month, sameLevel, vehicle), ScenarioA1FireMapRules.LocationLos(los, month, sameLevel, vehicle));
                        }
                    }
                }
            }
        }

        // B23.741: two Factory hexes traced give +2, attributed; B.10's +6 test sees the sum.
        var factory = new LosReadFacts(true, "Clear", "", false, 3, [new(1, 1, ["Stone Factory, 2.5 Level"]), new(2, 1, ["Stone Factory, 2.5 Level"])]);
        Assert.Equal(new FireLos(false, 0, false, false), ScenarioA1FireMapRules.LocationLos(factory, 7, true, _ => (0, null)).Los);
        Assert.Equal(new FireLos(false, 2, true, false), ScenarioA1FireMapRules.LocationLos(factory, 7, true, _ => (0, null), 2).Los);
        Assert.True(ScenarioA1FireMapRules.LocationLos(factory, 7, true, _ => (4, null), 2).Los!.Blocked);
    }

    private static StepLocationFacts Building(bool stairway = false) => new("stone-building", "Stone Building, 2 Level", 0, stairway);

    private static InfantryStepFacts Step(int fromLevel = 0, int toLevel = 0, bool sameHex = false, StepLocationFacts? from = null, StepLocationFacts? to = null,
        CrossedHexsideFacts? crossed = null) =>
        new(false, false, sameHex, fromLevel, toLevel, "[X4]", "[Y4]", "[X4]", from ?? Building(), to ?? Building(), true,
            crossed ?? new CrossedHexsideFacts(false, false, null, false, "Stone Building, 2 Level"), false, 7);

    private static (int HalfMf, bool RoadRate) Dry(string terrain, bool road, int rise) => (0, road);

    [Fact]
    public void TheInfantryStepOverloadWithNoFactoryIsTheOldMemberAndWithOneGivesOneMfAndRefusesUpperLevels()
    {
        InfantryStepFacts[] steps =
        [
            Step(),
            Step(sameHex: true, toLevel: 1, from: Building(true), to: Building(true)),
            Step(sameHex: true, toLevel: 1),
            Step(fromLevel: 1, toLevel: 1),
            Step(to: new StepLocationFacts("open-ground", "Open Ground", 0, false), crossed: new CrossedHexsideFacts(false, false, null, false, "Open Ground")),
            Step(to: new StepLocationFacts(null, "Rooftop", 0, false)),
            Step() with { Crossed = null },
        ];
        foreach (var step in steps)
        {
            Assert.Equal(ScenarioA1TerrainCosts.InfantryStep(step, () => 0, Dry), ScenarioA1TerrainCosts.InfantryStep(step, null, () => 0, Dry));
        }

        // B23.742: 1 MF between Factory hexes, where the old member charges 2; SMOKE adds its cost.
        Assert.Equal(4, ScenarioA1TerrainCosts.InfantryStep(Step(), () => 0, Dry).Entry!.HalfMf);
        Assert.Equal(2, ScenarioA1TerrainCosts.InfantryStep(Step(), Within, () => 0, Dry).Entry!.HalfMf);
        Assert.Equal(4, ScenarioA1TerrainCosts.InfantryStep(Step(), Within, () => 2, Dry).Entry!.HalfMf);

        // Entering from outside: the building's normal entry; through an Entrance, Open Ground.
        var fromOpen = Step(from: new StepLocationFacts("open-ground", "Open Ground", 0, false), crossed: new CrossedHexsideFacts(false, false, null, false, "Open Ground"));
        Assert.Equal(4, ScenarioA1TerrainCosts.InfantryStep(fromOpen, FromOutside, () => 0, Dry).Entry!.HalfMf);
        Assert.Equal(2, ScenarioA1TerrainCosts.InfantryStep(fromOpen, FromOutside with
        {
            VehicularEntrance = true
        }, () => 0, Dry).Entry!.HalfMf);

        // No stairwell climb, upper level, or cellar in a Factory, where the old member allows them.
        var climb = Step(sameHex: true, toLevel: 1, from: Building(true), to: Building(true));
        Assert.NotNull(ScenarioA1TerrainCosts.InfantryStep(climb, () => 0, Dry).Entry);
        Assert.Contains("B23.74", ScenarioA1TerrainCosts.InfantryStep(climb, Within, () => 0, Dry).Reason!, StringComparison.Ordinal);
        Assert.Contains("no upper level floors", ScenarioA1TerrainCosts.InfantryStep(Step(fromLevel: 1, toLevel: 1), Within, () => 0, Dry).Reason!, StringComparison.Ordinal);
        Assert.Contains("different base levels", ScenarioA1TerrainCosts.InfantryStep(Step(to: Building() with
        {
            BaseLevel = 1
        }), Within, () => 0, Dry).Reason!, StringComparison.Ordinal);
    }

    private static FireAttack Attack(int? factoryTem, string terrain = "stone-building") =>
        new("PFPh", "phasing", true, "bd01:X4:0", "bd01:Y4:0", [new FireFirer("ge-1", "defender-squad", "bd01:X4:0", false, false, false, false, false)], null, 1, true,
            new FireLos(false, 0, true, false), null, terrain, [new FireTarget("ru-1", "attacker-squad", "bd01:Y4:0", false, false, false, false, false, false, false)], 3,
            new FireRolls([6, 5], null, null, null))
        {
            FactoryTem = factoryTem
        };

    [Fact]
    public void TheFireCalculatorReadsTheFactoryTemInPlaceOfTheTerrainKeysOnlyWhenItIsSet()
    {
        var before = ScenarioA1FireCalculator.Resolve(Attack(null), Reference).Arithmetic!.Drm;
        Assert.Contains(before, item => item.Name == "tem:stone-building" && item.Value == 3m && item.Rule == "A7.6");
        var factory = ScenarioA1FireCalculator.Resolve(Attack(1), Reference).Arithmetic!.Drm;
        Assert.Contains(factory, item => item.Name == "tem:factory" && item.Value == 1m && item.Rule == "B23.741");
        Assert.DoesNotContain(factory, item => item.Name == "tem:stone-building");
        Assert.Equal(before.Where(item => !item.Name.StartsWith("tem:", StringComparison.Ordinal)), factory.Where(item => !item.Name.StartsWith("tem:", StringComparison.Ordinal)));

        // An AFV's +1 cover (D9.3) is claimed only where the TEM is not positive: a Factory's +1 is positive, as the building's +3 is.
        Assert.Null(ScenarioA1FireCalculator.Cover(Attack(1, "open-ground") with
        {
            AfvCover = "ge-tank"
        }));
        Assert.Equal("ge-tank", ScenarioA1FireCalculator.Cover(Attack(null, "open-ground") with
        {
            AfvCover = "ge-tank"
        }));
        Assert.Null(ScenarioA1FireCalculator.Cover(Attack(null) with
        {
            AfvCover = "ge-tank"
        }));
    }

    private static IReadOnlyList<(string Name, decimal Value, string Rule)> Drm(FireAttack attack) =>
        [.. ScenarioA1FireCalculator.Resolve(attack, Reference).Arithmetic!.Drm.Select(item => (item.Name, item.Value, item.Rule))];

    private static FireAttack OrdnanceHit(int? factoryTem, bool area, bool critical) => Attack(factoryTem) with
    {
        Firers = [],
        OrdnanceHit = new FireOrdnanceHit("ge-gun", 2, critical) { Area = area ? true : null },
    };

    [Fact]
    public void TheFireCalculatorsAreaAndCriticalHitBranchesReadTheFactoryTem()
    {
        // C3.331 (the review of the B23.74 build, findings 3 and 4): an Area hit takes the Factory's +1 on its Effects DR, named for the Factory; null is
        // the old stone building's +3, to the name.
        Assert.Contains(("tem:stone-building", 3m, "C3.331"), Drm(OrdnanceHit(null, area: true, critical: false)));
        var area = Drm(OrdnanceHit(1, area: true, critical: false));
        Assert.Contains(("tem:factory", 1m, "C3.331"), area);
        Assert.DoesNotContain(area, item => item.Name == "tem:stone-building");

        // C3.71: a Critical Hit reverses the Factory's +1 to -1, not the building's +3 to -3.
        Assert.Contains(("critical-hit-tem:stone-building", -3m, "C3.71"), Drm(OrdnanceHit(null, area: false, critical: true)));
        var critical = Drm(OrdnanceHit(1, area: false, critical: true));
        Assert.Contains(("critical-hit-tem:factory", -1m, "C3.71"), critical);
        Assert.DoesNotContain(critical, item => item.Name == "critical-hit-tem:stone-building");
    }

    [Fact]
    public void TheEmplacementAndWallComparisonsReadTheFactoryTem()
    {
        // C11.2: an Emplaced Gun's crew takes the Emplacement's +2 instead of a lower TEM: the Factory's +1 is lower, the building's +3 is not.
        FireAttack Crew(FireAttack attack) => attack with
        {
            GunTarget = new FireGunTarget("ru-gun", "defender-at-gun", "ru-1", true, false),
        };
        Assert.Contains(("tem:stone-building", 3m, "C3.331"), Drm(Crew(OrdnanceHit(null, area: true, critical: false))));
        var area = Drm(Crew(OrdnanceHit(1, area: true, critical: false)));
        Assert.Contains(("emplacement:ru-gun", 2m, "C11.2"), area);
        Assert.DoesNotContain(area, item => item.Name.StartsWith("tem:", StringComparison.Ordinal));
        Assert.Contains(("tem:stone-building", 3m, "A7.6"), Drm(Crew(Attack(null))));
        var small = Drm(Crew(Attack(1)));
        Assert.Contains(("emplacement:ru-gun", 2m, "C11.2"), small);
        Assert.DoesNotContain(small, item => item.Name.StartsWith("tem:", StringComparison.Ordinal));

        // B9.3: a wall's +2 replaces a lower in-hex TEM: the Factory's +1, not the building's +3.
        FireAttack Walled(int? factoryTem) => Attack(factoryTem) with
        {
            HexsideTem = new FireHexsideTem("wall", 2),
        };
        Assert.Contains(("tem:stone-building", 3m, "A7.6"), Drm(Walled(null)));
        var wall = Drm(Walled(1));
        Assert.Contains(("wall", 2m, "B9.3"), wall);
        Assert.DoesNotContain(wall, item => item.Name.StartsWith("tem:", StringComparison.Ordinal));
    }

    [Fact]
    public void AFactoryStepBetweenBaseLevelsIsRefusedByTheCalculator()
    {
        // B23.742 (the review of the B23.74 build, finding 7; ruling RB23F.15): the refusal the terrain costs' overload returns is the calculator's.
        Assert.Null(ScenarioA1FactoryCalculator.BaseLevelRefusal(0, 0));
        Assert.Equal("play.move-factory: a Factory step between hexes of different base levels is not reviewed (B23.742)",
            ScenarioA1FactoryCalculator.BaseLevelRefusal(0, 1));
    }

    private static RuleContract TemContract => ScenarioA1RuleContracts.Find("b23.741-factory-tem")!;

    private static RuleContract MovementContract => ScenarioA1RuleContracts.Find("b23.742-factory-movement")!;

    private static JsonObject Fire() => new()
    {
        ["material"] = "stone",
        ["roofless"] = false,
        ["indirectFire"] = false,
        ["residual"] = false,
        ["targetId"] = "r-squad",
        ["firerId"] = "g-squad",
        ["firerPlace"] = "inside",
        ["tracedWithinDepiction"] = true,
        ["factoryHexesTraced"] = 1,
    };

    private static JsonObject With(JsonObject facts, string name, JsonNode? value)
    {
        var copy = (JsonObject)facts.DeepClone();
        if (value is null)
        {
            copy.Remove(name);
        }
        else
        {
            copy[name] = value;
        }

        return copy;
    }

    [Fact]
    public void TheTemContractGivesTheFactoryOrBuildingTemAndHidesTheFirersFacts()
    {
        var within = ScenarioA1RuleContracts.Evaluate(TemContract, Fire());
        Assert.Equal("factory-tem", within.Outcome);
        Assert.Contains(within.Reasons, reason => reason.Text.Contains("+1 Factory Hindrance", StringComparison.Ordinal));
        Assert.Contains(ScenarioA1RuleContracts.Evaluate(TemContract, With(Fire(), "factoryHexesTraced", 0)).Reasons,
            reason => reason.Text.Contains("no Factory Hindrance", StringComparison.Ordinal));
        Assert.Equal("building-tem", ScenarioA1RuleContracts.Evaluate(TemContract, With(Fire(), "tracedWithinDepiction", false)).Outcome);
        Assert.Equal("factory-tem", ScenarioA1RuleContracts.Evaluate(TemContract, With(With(Fire(), "firerPlace", "rooftop"), "factoryHexesTraced", null)).Outcome);
        Assert.Equal("building-tem", ScenarioA1RuleContracts.Evaluate(TemContract, With(With(Fire(), "firerPlace", "outside"), "overAdjacentRubbleEnteringAcrossDepiction", false)).Outcome);

        // The opponent knows neither where the firer is nor its LOS: not evaluated; nor the Factory hexes it traces.
        var opponent = With(With(With(Fire(), "firerPlace", null), "tracedWithinDepiction", null), "factoryHexesTraced", null);
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(TemContract, opponent).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(TemContract, With(Fire(), "factoryHexesTraced", null)).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(TemContract, With(Fire(), "firerId", null)).Outcome);

        // Indirect Fire needs no firer's facts; Residual FP reads the mover's entry, its owner's.
        var mortar = With(With(With(opponent, "firerId", null), "indirectFire", true), "material", "wooden");
        Assert.Equal("building-tem", ScenarioA1RuleContracts.Evaluate(TemContract, mortar).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(TemContract, With(mortar, "roofless", true)).Outcome);
        var residual = With(With(opponent, "firerId", null), "residual", true);
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(TemContract, residual).Outcome);
        Assert.Equal("factory-tem", ScenarioA1RuleContracts.Evaluate(TemContract, With(residual, "residualEntry", "building-hexside")).Outcome);
        Assert.Equal("building-tem", ScenarioA1RuleContracts.Evaluate(TemContract, With(residual, "residualEntry", "non-building-hexside")).Outcome);
    }

    [Fact]
    public void EachContractsUnitIdIsItsOwnersSoTheOtherSideNamesOnlyAQuestionMarkStack()
    {
        // The review of the B23.74 build, finding 2: each id is Owner, concerning itself (AGENTS, a contract's owner facts).
        foreach (var (contract, name) in new[] { (TemContract, "targetId"), (TemContract, "firerId"), (MovementContract, "moverId") })
        {
            var fact = contract.Facts.Single(item => item.Name == name);
            Assert.Equal(ContractDisclosure.Owner, fact.Disclosure);
            Assert.Equal(name, fact.Concerns);
        }

        // The firer's side, which sees the target only as a "?" stack, still gets the verdict; the target's side, which sees only the firer's "?" stack,
        // is not told the firer's case; the mover's opponent learns nothing.
        Assert.Equal("factory-tem", ScenarioA1RuleContracts.Evaluate(TemContract, With(Fire(), "targetId", null)).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(TemContract, With(Fire(), "firerId", null)).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(MovementContract, With(Move(), "moverId", null)).Outcome);
    }

    private static JsonObject Move() => new()
    {
        ["moverId"] = "g-squad",
        ["vehicle"] = false,
        ["bypass"] = false,
        ["material"] = "stone",
        ["from"] = "factory",
        ["to"] = "factory",
        ["acrossFactoryHexside"] = true,
        ["vehicularEntrance"] = false,
        ["entranceRoofless"] = false,
        ["entranceFriendlyControlled"] = false,
        ["toLevel"] = 0,
    };

    [Fact]
    public void TheMovementContractPricesAStepInAFactoryAndRefusesItsUpperLevels()
    {
        var within = ScenarioA1RuleContracts.Evaluate(MovementContract, Move());
        Assert.Equal("factory-cost", within.Outcome);
        Assert.Contains(within.Reasons, reason => reason.Text == "1 MF");
        Assert.Equal("normal-cost", ScenarioA1RuleContracts.Evaluate(MovementContract, With(Move(), "from", "outside")).Outcome);
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(MovementContract, With(Move(), "toLevel", 1)).Outcome);
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(MovementContract, With(Move(), "toLevel", -1)).Outcome);

        var tank = With(With(With(With(Move(), "vehicle", true), "afv", true), "movementType", "fully-tracked"), "mpAllotment", 17);
        var verdict = ScenarioA1RuleContracts.Evaluate(MovementContract, tank);
        Assert.Equal("factory-cost", verdict.Outcome);
        Assert.Contains(verdict.Reasons, reason => reason.Text == "5 MP, with a Bog Check at +1");
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(MovementContract, With(tank, "movementType", "half-tracked")).Outcome);
        Assert.Equal("normal-cost", ScenarioA1RuleContracts.Evaluate(MovementContract, With(tank, "bypass", true)).Outcome);

        // The opponent knows neither whether the mover is a vehicle nor its MP.
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(MovementContract, With(Move(), "vehicle", null)).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(MovementContract, With(tank, "mpAllotment", null)).Outcome);
    }

    private static (int? Tem, bool FactoryTem) Pair(FactoryTemVerdict verdict) => (verdict.Tem, verdict.FactoryTem);
}
