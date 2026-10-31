using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// A10.531 Open Ground (p. 67), as <see cref="ScenarioA1OpenGroundCalculator"/> decides it, the old members that forward to it or keep their answers,
/// and the contracts <c>a10.531-open-ground</c> and <c>a12.14-advance-concealment</c> (the A10.531 claim's design, rulings RA10.1 to RA10.9).
/// </summary>
public sealed class ScenarioA1OpenGroundCalculatorTests
{
    [Fact]
    public void DashKeepsUnknownViewsSeparateFromNoBenefit()
    {
        Assert.Null(ScenarioA1Dash.QualifiesAgainstViews([(null, false)]));
        Assert.False(ScenarioA1Dash.QualifiesAgainstViews([(null, true)]));
        Assert.True(ScenarioA1Dash.QualifiesAgainstViews([(null, false), (false, false)]));
        Assert.False(ScenarioA1Dash.QualifiesAgainstViews([(true, false), (false, true)]));
    }

    [Fact]
    public void EvadeDestinationsRequireSeasonOnlyWhenItChangesTheChoice()
    {
        var uncertain = ScenarioA1InfantryOverrun.EvadeDestinationsBySeason([("grain", true, true, false, null), ("woods", true, true, false, false)]);
        Assert.NotNull(uncertain.Refusal);
        var alone = ScenarioA1InfantryOverrun.EvadeDestinationsBySeason([("grain", true, true, false, null)]);
        Assert.Null(alone.Refusal);
        Assert.Equal(["grain"], alone.Destinations);
        var october = ScenarioA1InfantryOverrun.EvadeDestinationsBySeason([("grain", true, true, false, true), ("woods", true, true, false, false)]);
        Assert.Equal(["woods"], october.Destinations);

        // A4.151: Enterable is Accessible, and grain is Accessible in every month, so with no month and nothing else open it is the destination.
        var besideEnemy = ScenarioA1InfantryOverrun.EvadeDestinationsBySeason([("grain", true, true, false, null), ("woods", true, true, true, false)]);
        Assert.Null(besideEnemy.Refusal);
        Assert.Equal(["grain"], besideEnemy.Destinations);
        var known = ScenarioA1InfantryOverrun.EvadeDestinationsBySeason([("open", true, true, false, true), ("woods", true, true, false, false)]);
        Assert.Null(known.Refusal);
        Assert.Equal(["woods"], known.Destinations);
        Assert.Equal(["grain", "woods"], ScenarioA1InfantryOverrun.EvadeDestinationsBySeason([("grain", true, true, false, false), ("woods", true, true, false, false)]).Destinations);
    }

    private static readonly string?[] TerrainKeys = ["open-ground", "grain", "brush", "woods", "orchard", "marsh", "wooden-rubble", "stone-rubble", "unknown", null];

    private static readonly int?[] MonthsAndNone = [null, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13];

    private static readonly bool[] Both = [false, true];

    private static readonly int?[] MapHindrances = [null, 0, 1, 2];

    private static readonly int[] Counts = [0, 1, 2];

    private static readonly OpenGroundLocationFacts Open = new("open-ground", 7, false, false);

    private static OpenGroundViewFacts Clear(RoutCoverFacts? cover = null, int hindrance = 0, string? asSeen = null) => new(true, hindrance, cover ?? RoutCoverFacts.None, asSeen);

    // B1.1, B1.11, B15.6, B12.3, B14.3, B16.3: only Open Ground (a road reads so) and out-of-season grain are Open Ground terrain.
    [Theory]
    [InlineData("open-ground", null, true)]
    [InlineData("open-ground", 7, true)]
    [InlineData("grain", 5, true)]
    [InlineData("grain", 6, false)]
    [InlineData("grain", 9, false)]
    [InlineData("grain", 10, true)]
    [InlineData("grain", null, null)]
    [InlineData("orchard", 1, false)]
    [InlineData("brush", 10, false)]
    [InlineData("marsh", 10, false)]
    [InlineData("woods", 10, false)]
    [InlineData(null, 10, false)]
    public void TerrainIsOpenGroundByItsKeyAndGrainBySeason(string? terrain, int? month, bool? expected) =>
        Assert.Equal(expected, ScenarioA1OpenGroundCalculator.OpenGroundTerrain(terrain, month));

    [Fact]
    public void AClearViewOfOpenGroundIsOpenGroundForEveryPurpose()
    {
        foreach (var purpose in Enum.GetValues<OpenGroundPurpose>())
        {
            Assert.True(ScenarioA1OpenGroundCalculator.OpenGroundTo(Open, Clear(), purpose));
        }
    }

    // A10.531 EX and A4.6: SMOKE in the Location, a protected entry, or no LOS make it not Open Ground to the unit, for every purpose.
    [Fact]
    public void SmokeAProtectedEntryOrNoLosIsNeverOpenGround()
    {
        foreach (var purpose in Enum.GetValues<OpenGroundPurpose>())
        {
            Assert.False(ScenarioA1OpenGroundCalculator.OpenGroundTo(Open with
            {
                Smoke = true
            }, Clear(), purpose));
            Assert.False(ScenarioA1OpenGroundCalculator.OpenGroundTo(Open with
            {
                ProtectedEntry = true
            }, Clear(), purpose));
            Assert.False(ScenarioA1OpenGroundCalculator.OpenGroundTo(Open, Clear() with
            {
                LosClear = false
            }, purpose));
            Assert.False(ScenarioA1OpenGroundCalculator.OpenGroundTo(Open, Clear(new RoutCoverFacts(null, 0, false, false, true)), purpose));

            // Grain with no month is unknown, unless something else already says no.
            Assert.Null(ScenarioA1OpenGroundCalculator.OpenGroundTo(new("grain", null, false, false), Clear(), purpose));
            Assert.False(ScenarioA1OpenGroundCalculator.OpenGroundTo(new("grain", null, true, false), Clear(), purpose));
            Assert.False(ScenarioA1OpenGroundCalculator.OpenGroundTo(new("grain", null, false, false), Clear() with
            {
                LosClear = false
            }, purpose));
        }
    }

    // RA10.2: a Hindrance, a hexside TEM, or Height Advantage along the LOS cancels FFMO for the rout, Dash, and Interdiction, but does not keep a "?".
    [Theory]
    [InlineData(OpenGroundPurpose.Rout, false)]
    [InlineData(OpenGroundPurpose.Dash, false)]
    [InlineData(OpenGroundPurpose.Interdiction, false)]
    [InlineData(OpenGroundPurpose.Concealment, true)]
    public void CoverAlongTheLosCountsForEveryPurposeButConcealment(OpenGroundPurpose purpose, bool open)
    {
        Assert.Equal(open, ScenarioA1OpenGroundCalculator.OpenGroundTo(Open, Clear(hindrance: 1), purpose));
        Assert.Equal(open, ScenarioA1OpenGroundCalculator.OpenGroundTo(Open, Clear(new RoutCoverFacts(1, 0, false, false, false)), purpose));
        Assert.Equal(open, ScenarioA1OpenGroundCalculator.OpenGroundTo(Open, Clear(new RoutCoverFacts(0, 1, false, false, false)), purpose));
        Assert.Equal(open, ScenarioA1OpenGroundCalculator.OpenGroundTo(Open, Clear(new RoutCoverFacts(null, 0, true, false, false)), purpose));
        Assert.Equal(open, ScenarioA1OpenGroundCalculator.OpenGroundTo(Open, Clear(new RoutCoverFacts(null, 0, false, true, false)), purpose));

        // The rout's attributed count of the map Hindrance stands for the LOS's own.
        Assert.True(ScenarioA1OpenGroundCalculator.OpenGroundTo(Open, Clear(new RoutCoverFacts(0, 0, false, false, false), hindrance: 2), purpose));
    }

    // A4.132, B13.31, B14.6: a road-rate mover in a hex of other terrain is in Open Ground to a unit whose LOS reaches the road without crossing it.
    [Fact]
    public void ARoadRateMoverIsSeenInTheTerrainItsViewerSees()
    {
        var woodsRoad = new OpenGroundLocationFacts("woods", 7, false, false);
        foreach (var purpose in Enum.GetValues<OpenGroundPurpose>())
        {
            Assert.False(ScenarioA1OpenGroundCalculator.OpenGroundTo(woodsRoad, Clear(), purpose));
            Assert.True(ScenarioA1OpenGroundCalculator.OpenGroundTo(woodsRoad, Clear(asSeen: "open-ground"), purpose));
            Assert.False(ScenarioA1OpenGroundCalculator.OpenGroundTo(woodsRoad with
            {
                Smoke = true
            }, Clear(asSeen: "open-ground"), purpose));
        }

        Assert.True(ScenarioA1OpenGroundCalculator.OpenGroundTo(new("orchard", 7, false, false), Clear(asSeen: "open-ground"), OpenGroundPurpose.Concealment));
    }

    [Fact]
    public void AnyOneEnemyUnitMakesItOpenGroundAndAnUnknownLeavesNoneOpen()
    {
        Assert.True(ScenarioA1OpenGroundCalculator.OpenGroundToAny([false, null, true]));
        Assert.Null(ScenarioA1OpenGroundCalculator.OpenGroundToAny([false, null]));
        Assert.False(ScenarioA1OpenGroundCalculator.OpenGroundToAny([false, false]));
        Assert.False(ScenarioA1OpenGroundCalculator.OpenGroundToAny([]));
    }

    [Fact]
    public void BadFactsAreRefused()
    {
        Assert.Throws<ArgumentNullException>(() => ScenarioA1OpenGroundCalculator.NoFfmoCover(null!, 0));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1OpenGroundCalculator.OpenGroundTo(null!, Clear(), OpenGroundPurpose.Rout));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1OpenGroundCalculator.OpenGroundTo(Open, null!, OpenGroundPurpose.Rout));
        Assert.Throws<ArgumentOutOfRangeException>(() => ScenarioA1OpenGroundCalculator.OpenGroundTo(Open, Clear(), (OpenGroundPurpose)9));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1OpenGroundCalculator.OpenGroundToAny(null!));
    }

    // RA10.9: the rout's OpenGround forwards, equal to its old body on every terrain key, month, and SMOKE.
    [Fact]
    public void TheRoutsOpenGroundForwardsWithTheSameAnswer()
    {
        static bool Old(RoutLocationFacts? read, int? scenarioMonth) =>
            read is { } location && !location.Smoke
                && (location.TerrainKey == "open-ground" || (location.TerrainKey == "grain" && scenarioMonth is < 6 or > 9));

        Assert.Equal(Old(null, 7), ScenarioA1RoutCalculator.OpenGround(null, 7));
        foreach (var terrain in TerrainKeys)
        {
            foreach (var month in MonthsAndNone)
            {
                foreach (var smoke in Both)
                {
                    var read = new RoutLocationFacts(terrain, smoke);
                    Assert.Equal(Old(read, month), ScenarioA1RoutCalculator.OpenGround(read, month));
                }
            }
        }
    }

    // RA10.9: fire's grain season forwards, equal on every terrain key, month, and phase.
    [Fact]
    public void FiresGrainTerrainForwardsWithTheSameAnswer()
    {
        static (string? Refusal, string Terrain) Old(string terrain, int? month, string? phase)
        {
            if (terrain == "grain")
            {
                if (month is not { } known)
                {
                    if (phase == "mph")
                    {
                        return ("play.fire-grain: grain is Open Ground outside its season, which the game does not name, so FFMO there is not decided (B15.6)", terrain);
                    }
                }
                else if (known is < 6 or > 9)
                {
                    return (null, "open-ground");
                }
            }

            return (null, terrain);
        }

        foreach (var terrain in TerrainKeys.OfType<string>())
        {
            foreach (var month in MonthsAndNone)
            {
                foreach (var phase in new[] { "mph", "dfph", null })
                {
                    Assert.Equal(Old(terrain, month, phase), ScenarioA1FireMapRules.GrainTerrain(terrain, month, phase));
                }
            }
        }
    }

    // RA10.9: Dash's OpenGroundTo and the rout's CouldApplyFfmo forward their cover test, equal over a grid of cover facts.
    [Fact]
    public void DashAndTheRoutsCoverTestForwardWithTheSameAnswer()
    {
        static bool Old(bool openGroundTerrain, bool losClear, int losHindrance, RoutCoverFacts cover) =>
            openGroundTerrain && losClear && (cover.MapHindrance ?? losHindrance) + cover.OtherHindrance == 0 && !cover.HexsideTem && !cover.HeightAdvantage && !cover.InHexCover;

        foreach (var cover in Covers())
        {
            foreach (var hindrance in Counts)
            {
                foreach (var open in Both)
                {
                    foreach (var clear in Both)
                    {
                        Assert.Equal(Old(open, clear, hindrance, cover), ScenarioA1Dash.OpenGroundTo(open, clear, hindrance, cover));
                    }
                }

                foreach (var range in Counts)
                {
                    foreach (var clear in Both)
                    {
                        var reader = new Reader(new RoutLosFacts(clear, hindrance, 1), cover);
                        Assert.Equal(clear && range >= 1 && Old(true, true, hindrance, cover), ScenarioA1RoutCalculator.CouldApplyFfmo(reader, 1, 2, range));
                    }
                }
            }
        }

        Assert.Throws<ArgumentNullException>(() => ScenarioA1Dash.OpenGroundTo(false, false, 0, null!));
    }

    private static IEnumerable<RoutCoverFacts> Covers()
    {
        foreach (var map in MapHindrances)
        {
            foreach (var other in Counts)
            {
                foreach (var tem in Both)
                {
                    foreach (var height in Both)
                    {
                        foreach (var inHex in Both)
                        {
                            yield return new RoutCoverFacts(map, other, tem, height, inHex);
                        }
                    }
                }
            }
        }
    }

    // RA10.5: the overloads give the old members' answers when A10.531's fact equals the old terrain test, and read each fact only where the old did.
    [Fact]
    public void TheConcealmentOverloadsAnswerAsTheOldMembersOnTheOldReading()
    {
        MoverConcealmentFacts[] movers = [new("a", false, true), new("b", true, false), new("c", false, false)];
        foreach (var terrain in new[] { "open-ground", "grain", "woods" })
        {
            foreach (var forcedBack in Both)
            {
                foreach (var assault in Both)
                {
                    foreach (var night in Both)
                    {
                        foreach (var sees in Both)
                        {
                            foreach (var lit in Both)
                            {
                                var oldReads = new List<string>();
                                var newReads = new List<string>();
                                var old = ScenarioA1MovementCalculator.Unmasked(movers, forcedBack, assault, night, terrain, () => Read(oldReads, "sees", sees), () => Read(oldReads, "lit", lit));
                                var updated = ScenarioA1MovementCalculator.Unmasked(movers, forcedBack, assault, night,
                                    () => Read(newReads, "open", terrain == "open-ground" && sees), () => Read(newReads, "sees", sees), () => Read(newReads, "lit", lit));
                                Assert.Equal(old.Select(unit => unit.Id), updated.Select(unit => unit.Id));
                                Assert.Equal(oldReads, newReads.Where(read => read != "open"));
                            }
                        }

                        var losReads = 0;
                        Assert.Equal(ScenarioA1AdvanceCalculator.LosesConcealment(night, terrain, () => assault),
                            ScenarioA1AdvanceCalculator.LosesConcealment(night, () => ++losReads > 0 && terrain == "open-ground" && assault));
                        Assert.Equal(night ? 0 : 1, losReads);
                    }
                }
            }
        }
    }

    private static bool Read(List<string> reads, string name, bool value)
    {
        reads.Add(name);
        return value;
    }

    // RA10.5: the overloads take A10.531's answer: out-of-season grain loses the "?", SMOKE in Open Ground keeps it.
    [Fact]
    public void TheOverloadsReadA10531sAnswer()
    {
        var grain = ScenarioA1OpenGroundCalculator.OpenGroundTo(new("grain", 10, false, false), Clear(), OpenGroundPurpose.Concealment);
        var smoke = ScenarioA1OpenGroundCalculator.OpenGroundTo(Open with
        {
            Smoke = true
        }, Clear(), OpenGroundPurpose.Concealment);
        Assert.True(ScenarioA1AdvanceCalculator.LosesConcealment(false, () => grain == true));
        Assert.False(ScenarioA1AdvanceCalculator.LosesConcealment(false, () => smoke == true));
        Assert.False(ScenarioA1AdvanceCalculator.LosesConcealment(false, "grain", () => true));
        Assert.True(ScenarioA1AdvanceCalculator.LosesConcealment(false, "open-ground", () => true));
        MoverConcealmentFacts[] movers = [new("a", false, true)];
        Assert.Single(ScenarioA1MovementCalculator.Unmasked(movers, false, true, false, () => grain == true, () => true, () => false));
        Assert.Empty(ScenarioA1MovementCalculator.Unmasked(movers, false, true, false, () => smoke == true, () => true, () => false));
    }

    private static JsonObject Facts(string purpose, string terrain, int? month = 7) => new()
    {
        ["purpose"] = purpose,
        ["at"] = "bd04:G5:0",
        ["terrain"] = terrain,
        ["month"] = month,
        ["smokeInLocation"] = false,
        ["protectedEntry"] = false,
        ["inLocationCover"] = false,
        ["roadRate"] = false,
    };

    private static JsonObject Seen(JsonObject facts, bool losClear = true, int losHindrance = 0, bool hexsideTem = false, bool heightAdvantage = false)
    {
        facts["viewerId"] = "r-squad";
        facts["losClear"] = losClear;
        facts["losHindrance"] = losHindrance;
        facts["hexsideTem"] = hexsideTem;
        facts["heightAdvantage"] = heightAdvantage;
        facts["seesAlongRoad"] = false;
        return facts;
    }

    private static ContractVerdict OpenGround(JsonObject facts) => ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find("a10.531-open-ground")!, facts);

    [Fact]
    public void TheOpenGroundContractAnswersEachCase()
    {
        Assert.Equal("open-ground", OpenGround(Seen(Facts("rout", "open-ground"))).Outcome);
        Assert.Equal("open-ground", OpenGround(Seen(Facts("interdiction", "grain", 10))).Outcome);
        Assert.Equal("not-open-ground", OpenGround(Seen(Facts("dash", "grain", 7))).Outcome);
        Assert.Equal("not-open-ground", OpenGround(Seen(Facts("rout", "open-ground"), hexsideTem: true)).Outcome);
        Assert.Equal("not-open-ground", OpenGround(Seen(Facts("rout", "open-ground"), losHindrance: 1)).Outcome);
        Assert.Equal("not-open-ground", OpenGround(Seen(Facts("rout", "open-ground"), heightAdvantage: true)).Outcome);
        Assert.Equal("not-open-ground", OpenGround(Seen(Facts("rout", "open-ground"), losClear: false)).Outcome);
        Assert.Equal("open-ground", OpenGround(Seen(Facts("concealment", "open-ground"), losHindrance: 1, hexsideTem: true, heightAdvantage: true)).Outcome);

        var smoke = Seen(Facts("concealment", "open-ground"));
        smoke["smokeInLocation"] = true;
        Assert.Contains("SMOKE", Assert.Single(OpenGround(smoke).Reasons).Text, StringComparison.Ordinal);
        Assert.Equal("not-open-ground", OpenGround(smoke).Outcome);

        var road = Seen(Facts("concealment", "woods"));
        road["roadRate"] = true;
        road["seesAlongRoad"] = true;
        Assert.Equal("open-ground", OpenGround(road).Outcome);
        road["seesAlongRoad"] = false;
        Assert.Equal("not-open-ground", OpenGround(road).Outcome);
    }

    // Hidden information: the viewer's side alone knows its LOS; the public facts settle "not Open Ground" alone where they can.
    [Fact]
    public void ThePublicFactsSettleWhatTheyCanAndTheRestIsNotEvaluated()
    {
        Assert.Equal(ContractVerdict.NotEvaluated, OpenGround(Facts("rout", "open-ground")).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, OpenGround(Seen(Facts("rout", "grain", null))).Outcome);
        Assert.Contains("month", OpenGround(Seen(Facts("rout", "grain", null))).Reasons[0].Text, StringComparison.Ordinal);
        Assert.Equal("not-open-ground", OpenGround(Facts("rout", "woods")).Outcome);
        Assert.Equal("not-open-ground", OpenGround(Facts("dash", "orchard", null)).Outcome);
        Assert.Equal("not-open-ground", OpenGround(Facts("rout", "grain", 7)).Outcome);

        var wreck = Facts("interdiction", "open-ground");
        wreck["inLocationCover"] = true;
        Assert.Equal("not-open-ground", OpenGround(wreck).Outcome);
        var entrenched = Facts("rout", "open-ground");
        entrenched["protectedEntry"] = true;
        Assert.Equal("not-open-ground", OpenGround(entrenched).Outcome);

        // A road-rate mover in woods: the viewer's view of the road decides, so woods alone no longer settles it.
        var road = Facts("concealment", "woods");
        road["roadRate"] = true;
        Assert.Equal(ContractVerdict.NotEvaluated, OpenGround(road).Outcome);

        // For concealment only the LOS is the viewer's to give; the rest of its view does not matter.
        var seenOnly = Facts("concealment", "open-ground");
        seenOnly["losClear"] = true;
        Assert.Equal("open-ground", OpenGround(seenOnly).Outcome);

        var badMonth = Facts("rout", "grain", 13);
        Assert.Equal(ContractVerdict.NotEvaluated, OpenGround(badMonth).Outcome);
    }

    private static JsonObject Advance(bool night, string terrain, int? month = 7, bool? smoke = false, bool? cover = false, bool? seen = null)
    {
        var facts = new JsonObject
        {
            ["advancerId"] = "g-squad",
            ["to"] = "bd04:H5:0",
            ["night"] = night,
            ["terrain"] = terrain,
            ["month"] = month,
            ["smokeInLocation"] = smoke,
            ["inLocationCover"] = cover,
        };
        if (seen is { } value)
        {
            facts["enemyGoodOrderInLosWithin16"] = value;
        }

        return facts;
    }

    private static string Concealment(JsonObject facts) => ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find("a12.14-advance-concealment")!, facts).Outcome;

    // RA10.6: the a12.14 contract asks the new overload with A10.531's reading for concealment.
    [Fact]
    public void TheAdvanceConcealmentContractReadsA10531()
    {
        Assert.Equal("loses-concealment", Concealment(Advance(false, "open-ground", seen: true)));
        Assert.Equal("loses-concealment", Concealment(Advance(false, "grain", 10, seen: true)));
        Assert.Equal("keeps-concealment", Concealment(Advance(false, "grain", 7, seen: true)));
        Assert.Equal("keeps-concealment", Concealment(Advance(false, "open-ground", smoke: true, seen: true)));
        Assert.Equal("keeps-concealment", Concealment(Advance(false, "open-ground", cover: true, seen: true)));
        Assert.Equal("keeps-concealment", Concealment(Advance(false, "open-ground", seen: false)));
        Assert.Equal("keeps-concealment", Concealment(Advance(true, "grain", null, seen: true)));

        // The owner settles a woods advance, SMOKE, or in-season grain alone; Open Ground waits on the opponent's LOS, grain with no month on the month.
        Assert.Equal("keeps-concealment", Concealment(Advance(false, "woods")));
        Assert.Equal("keeps-concealment", Concealment(Advance(false, "open-ground", smoke: true)));
        Assert.Equal(ContractVerdict.NotEvaluated, Concealment(Advance(false, "open-ground")));
        Assert.Equal(ContractVerdict.NotEvaluated, Concealment(Advance(false, "grain", null, seen: true)));

        // W-A10.531: missing smoke or cover facts cannot grant a concealment loss.
        Assert.Equal(ContractVerdict.NotEvaluated, Concealment(Advance(false, "open-ground", null, null, null, true)));
        Assert.Equal("keeps-concealment", Concealment(Advance(false, "open-ground", null, null, null, false)));
        Assert.Equal(ContractVerdict.NotEvaluated, Concealment(Advance(false, "open-ground", 13, seen: true)));

        // RA10.7 as refined: grain with no month is not-evaluated unless the seer flag settles it; with no Good Order enemy in LOS (A12.14 Case B)
        // the season cannot matter, so the "?" stays.
        Assert.Equal("keeps-concealment", Concealment(Advance(false, "grain", null, seen: false)));

        // A known SMOKE settles "not Open Ground" alone, with cover absent; a known absence of SMOKE leaves the absent cover fact deciding.
        Assert.Equal("keeps-concealment", Concealment(Advance(false, "open-ground", smoke: true, cover: null, seen: true)));
        Assert.Equal(ContractVerdict.NotEvaluated, Concealment(Advance(false, "open-ground", smoke: false, cover: null, seen: true)));
    }

    private sealed class Reader(RoutLosFacts los, RoutCoverFacts cover) : IRoutFactReader
    {
        public string Name(int location) => location.ToString(System.Globalization.CultureInfo.InvariantCulture);

        public IEnumerable<int> Neighbors(int location) => [];

        public bool Playable(int location) => true;

        public RoutLocationFacts? Location(int location) => null;

        public RoutLosFacts? Los(int fromLocation, int toLocation) => los;

        public RoutCoverFacts Cover(int enemyLocation, int location, int? steppedFrom = null) => cover;

        public int? Distance(int one, int two) => 1;

        public bool Adjacent(int one, int two) => true;

        public (InfantryEntry? Entry, string? Reason) Entry(int fromLocation, int toLocation) => (null, null);
    }
}
