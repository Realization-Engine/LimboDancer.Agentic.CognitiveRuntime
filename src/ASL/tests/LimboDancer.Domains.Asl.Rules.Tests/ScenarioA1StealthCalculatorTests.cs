using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The A11.17 claim (rows A11.17, A11.18, A25.64; the design's section 4): <see cref="ScenarioA1StealthCalculator"/> on every case of A11.17 and A11.18
/// (p. 73), A19.2 (p. 86), A25.7 and A25.64 (p. 97), and G1.6 (p. 280); the Ambush's forward, equal to its old answer on every input; the three members
/// that keep their answers, unchanged with <c>Stealth</c> null and taking it when set; and the contract <c>a11.17-stealth</c>.
/// </summary>
public sealed class ScenarioA1StealthCalculatorTests
{
    [Theory]
    [InlineData("stealthy")]
    [InlineData("stealthy:")]
    [InlineData("lax:german:")]
    [InlineData("lax:german:group:extra")]
    public void MalformedStealthSsrsAreRefusedAtSetup(string token)
    {
        Assert.StartsWith("play.stealth-rule:", ScenarioA1StealthCalculator.SsrRulesBar([token]), StringComparison.Ordinal);
    }

    [Fact]
    public void AStealthOrLaxSsrMayNameAnObGroupAndTheFoldReadsItsSideAndGroup()
    {
        // A11.17 and A11.18 (p. 73): "Good Order troops", "certain units"; ruling RA11S.11, the Fanatic token's form (ruling R70.15).
        Assert.Null(ScenarioA1StealthCalculator.SsrRulesBar(["lax:german:reserve", "stealthy:russian:scouts"], ["german", "russian"]));
        Assert.NotNull(ScenarioA1StealthCalculator.SsrRulesBar(["lax:missing:reserve"], ["german", "russian"]));
        Assert.Equal(("german", (string?)"reserve", true), ScenarioA1StealthCalculator.SsrScope("lax:german:reserve"));
        Assert.Equal(("german", (string?)null, false), ScenarioA1StealthCalculator.SsrScope("stealthy:german"));
        // The side-wide reader keeps its answer: a group token is not side-wide.
        Assert.Null(ScenarioA1StealthCalculator.SsrToken("lax:german:reserve"));

        string[] rules = ["lax:german:reserve", "stealthy:german", "hip:german:2", "fanatic:russian", "stealthy:russian:scouts"];
        Assert.Equal((true, true), ScenarioA1StealthCalculator.SsrFlags(rules, "german", "reserve"));
        Assert.Equal((true, false), ScenarioA1StealthCalculator.SsrFlags(rules, "german", "assault"));
        Assert.Equal((true, false), ScenarioA1StealthCalculator.SsrFlags(rules, "german", null));
        Assert.Equal((true, false), ScenarioA1StealthCalculator.SsrFlags(rules, "russian", "scouts"));
        Assert.Equal((false, false), ScenarioA1StealthCalculator.SsrFlags(rules, "russian", "line"));
        Assert.Equal((false, false), ScenarioA1StealthCalculator.SsrFlags([], "german", null));
    }

    [Fact]
    public void TheRulesBarIgnoresOtherTokensAndNeedsNoSides()
    {
        Assert.Null(ScenarioA1StealthCalculator.SsrRulesBar(["hip:german:2", "stealthy:german", "lax:russian:line", "laxity", "fanatic:russian"]));
    }

    [Theory]
    [InlineData("none", StealthDesignation.None)]
    [InlineData("anzac", StealthDesignation.Anzac)]
    [InlineData("gurkha", StealthDesignation.Gurkha)]
    [InlineData("commando", StealthDesignation.Commando)]
    [InlineData("partisan", StealthDesignation.Partisan)]
    [InlineData("raider", StealthDesignation.Raider)]
    public void EveryDesignationTokenReadsItsDesignation(string token, StealthDesignation expected)
    {
        Assert.Equal(expected, ScenarioA1StealthCalculator.Designation(token));
        Assert.Null(ScenarioA1StealthCalculator.Designation(token.ToUpperInvariant() + "x"));
    }

    [Fact]
    public void TheContractsDesignationChoicesAreTheCalculatorsTokens()
    {
        var choices = ScenarioA1RuleContracts.All.Single(contract => contract.Id == "a11.17-stealth").Facts.Single(fact => fact.Name == "designation").Choices;
        Assert.Equal(ScenarioA1StealthCalculator.DesignationTokens, choices);
        Assert.Equal(Enum.GetValues<StealthDesignation>().Length, ScenarioA1StealthCalculator.DesignationTokens.Count);
    }

    [Fact]
    public void StealthSsrsNameAnExplicitScenarioSideAndLaxStillWins()
    {
        Assert.Null(ScenarioA1StealthCalculator.SsrRulesBar(["stealthy:german", "lax:german"], ["german", "russian"]));
        Assert.NotNull(ScenarioA1StealthCalculator.SsrRulesBar(["stealthy:missing"], ["german", "russian"]));
        var stealth = ScenarioA1StealthCalculator.SsrToken("stealthy:german")!.Value;
        var lax = ScenarioA1StealthCalculator.SsrToken("lax:german")!.Value;
        Assert.Equal("german", stealth.Side);
        Assert.False(stealth.Lax);
        Assert.True(lax.Lax);
        Assert.Equal(StealthStatus.Lax, ScenarioA1StealthCalculator.Status(Unit("asl:squad", @class: "1st-line", ssrStealthy: true, ssrLax: true), StealthPurpose.Ambush));
    }

    [Fact]
    public void ACardDesignationIsExplicitAndUnknownIsNotOrdinary()
    {
        Assert.Equal(StealthDesignation.None, ScenarioA1StealthCalculator.Designation(null));
        Assert.Equal(StealthDesignation.Anzac, ScenarioA1StealthCalculator.Designation("anzac"));
        Assert.Null(ScenarioA1StealthCalculator.Designation("New Zealanders"));
        Assert.Equal(StealthStatus.Stealthy, ScenarioA1StealthCalculator.Status(Unit("asl:squad", "british", "1st-line",
            designation: ScenarioA1StealthCalculator.Designation("anzac")!.Value), StealthPurpose.Search));
    }

    private static readonly ScenarioA1CloseCombatReference Reference = new ScenarioA1CloseCombatPackage().Reference;

    private static readonly FireModifier[] StealthyOnly = [ScenarioA1StealthCalculator.AmbushStealthy];

    private static readonly FireModifier[] LaxOnly = [ScenarioA1StealthCalculator.AmbushLax];

    private static readonly FireModifier[] LaxThenStealthy = [ScenarioA1StealthCalculator.AmbushLax, ScenarioA1StealthCalculator.AmbushStealthy];

    private static readonly string[] HeroExampleNames = ["stealthy", "vehicle", "bu-or-stunned-afv"];

    private static readonly string[] LaxHeroNotes = ["-1 Stealthy", "+1 Lax"];

    private static readonly string[] LaxNotes = ["+1 Lax"];

    private static StealthFacts Unit(string kind, string? nationality = "german", string? @class = null, bool heroic = false, bool? inexperienced = false,
        bool goodOrder = true, bool berserk = false, StealthDesignation designation = StealthDesignation.None, bool ssrStealthy = false, bool ssrLax = false,
        bool banzai = false) =>
        new(kind, heroic, nationality, @class, designation, ssrStealthy, ssrLax, inexperienced, berserk, goodOrder, banzai);

    private static StealthStatus? Status(StealthFacts unit, StealthPurpose purpose = StealthPurpose.Concealment) => ScenarioA1StealthCalculator.Status(unit, purpose);

    [Fact]
    public void HeroesAndHeroicLeadersAreStealthyInGoodOrder()
    {
        // A15.24 (p. 83): "A hero is always considered Stealthy"; A15.21: a heroic leader as a hero; A11.17: "Such Good Order troops".
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:hero")));
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:leader", heroic: true)));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:leader")));
        // A wounded hero is still in Good Order (A.7); a berserk one is not, and is Lax in CC alone (A15.432).
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:hero", goodOrder: true)));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:hero", goodOrder: false, berserk: true)));
        Assert.Equal(StealthStatus.Lax, Status(Unit("asl:hero", goodOrder: false, berserk: true), StealthPurpose.Ambush));
    }

    [Fact]
    public void EliteAndFirstLineFinnsAreStealthyAndNoOtherFinn()
    {
        // A11.17, A25.7 (p. 97): "Stealthy if Elite/1st-Line"; A19.2 (p. 86): Green and Conscript never, "even a Green squad accompanied by a leader".
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:squad", "finnish", "elite")));
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:half-squad", "finnish", "1st-line")));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:squad", "finnish", "2nd-line")));
        Assert.Equal(StealthStatus.Lax, Status(Unit("asl:squad", "finnish", "green", inexperienced: true)));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:squad", "finnish", "green", inexperienced: false)));
        Assert.Equal(StealthStatus.Lax, Status(Unit("asl:squad", "finnish", "conscript", inexperienced: null)));
        // A1.123 (p. 44): an Infantry crew is Elite, a vehicle crew is not (ruling RA11S.6); a Finnish leader has no class (ruling RA11S.2).
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:crew", "finnish", "elite")));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:vehicle-crew", "finnish")));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:leader", "finnish")));
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:hero", "finnish")));
        // Ruling RA11S.5: a broken Finnish elite squad is not in Good Order, so not Stealthy.
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:squad", "finnish", "elite", goodOrder: false)));
    }

    [Fact]
    public void DesignatedForcesAndSsrAreStealthyUnlessGreen()
    {
        // A11.17; A25.44 (ANZAC unless Green), A25.43 and H1.24 (Gurkhas, Commandos), G17.111 (Raiders), A25.24 (Partisans), p. 95 to 97 and 319.
        foreach (var designation in new[] { StealthDesignation.Anzac, StealthDesignation.Gurkha, StealthDesignation.Commando, StealthDesignation.Raider })
        {
            Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:squad", "british", "1st-line", designation: designation)));
            Assert.Equal(StealthStatus.Normal, Status(Unit("asl:squad", "british", "green", designation: designation)));
            Assert.Equal(StealthStatus.Lax, Status(Unit("asl:squad", "british", "green", inexperienced: true, designation: designation)));
        }

        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:squad", "russian", "2nd-line", designation: StealthDesignation.Partisan)));
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:leader", "russian", designation: StealthDesignation.Partisan)));
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:leader", "british", designation: StealthDesignation.Anzac)));
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:leader", "british", designation: StealthDesignation.Gurkha)));
        Assert.Null(Status(Unit("asl:squad", "british", null, designation: StealthDesignation.Anzac)));
        // An SSR's Stealth (A11.17), and an SSR's Lax on Stealthy Finns: "Stealthy unless Lax", Lax wins (A11.18).
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:squad", "german", "1st-line", ssrStealthy: true)));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:squad", "german", "green", inexperienced: false, ssrStealthy: true)));
        Assert.Equal(StealthStatus.Lax, Status(Unit("asl:squad", "german", "conscript", inexperienced: true, ssrStealthy: true)));
        Assert.Equal(StealthStatus.Lax, Status(Unit("asl:squad", "finnish", "elite", ssrLax: true)));
        Assert.Equal(1, ScenarioA1StealthCalculator.Drm(Status(Unit("asl:squad", "finnish", "elite", ssrLax: true))!.Value));
    }

    [Theory]
    [InlineData(StealthDesignation.Commando)]
    [InlineData(StealthDesignation.Raider)]
    public void CommandosAndRaidersAreSquadsAndHalfSquadsNotTheirLeadersOrCrews(StealthDesignation designation)
    {
        // H1.24 (p. 329): "A squad designated as a Commando"; G17.111 (p. 319): "Raider MMC are Stealthy" of the Raider squads/HS (ruling RA11S.2).
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:squad", "american", "elite", designation: designation)));
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:half-squad", "american", "elite", designation: designation)));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:leader", "american", designation: designation)));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:crew", "american", "elite", designation: designation)));
        // A hero and a heroic leader stay Stealthy by A15.24 and A15.21, whatever the designation.
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:hero", "american", designation: designation)));
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:leader", "american", heroic: true, designation: designation)));
    }

    [Fact]
    public void NonEliteItaliansAreLaxAndTheJapaneseByG16()
    {
        // A25.64 (p. 97; ruling RA11S.9): non-elite Italian MMC are Lax; an elite one, a leader, a hero (no class), an Infantry crew (Elite) are not.
        Assert.Equal(StealthStatus.Lax, Status(Unit("asl:squad", "italian", "1st-line")));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:squad", "italian", "elite")));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:leader", "italian")));
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:hero", "italian")));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:crew", "italian", "elite")));
        Assert.Equal(StealthStatus.Lax, Status(Unit("asl:vehicle-crew", "italian")));
        Assert.Null(Status(Unit("asl:squad", "italian", null)));
        // G1.6 (p. 280; ruling RA11S.3): Elite and 1st Line Japanese Infantry, crews included, Stealthy; Conscripts and a Banzai Charge Lax.
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:squad", "japanese", "elite")));
        Assert.Equal(StealthStatus.Stealthy, Status(Unit("asl:crew", "japanese", "elite")));
        Assert.Equal(StealthStatus.Lax, Status(Unit("asl:squad", "japanese", "conscript", inexperienced: null)));
        Assert.Equal(StealthStatus.Lax, Status(Unit("asl:squad", "japanese", "elite", banzai: true)));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:squad", "japanese", "2nd-line")));
    }

    [Fact]
    public void AnUnknownFactNeverMakesAUnitStealthy()
    {
        // A Green MMC whose Inexperience (A19.3) is unknown, a Finnish squad whose class is unknown: no status. A vehicle is not Personnel.
        Assert.Null(Status(Unit("asl:squad", "german", "green", inexperienced: null)));
        Assert.Null(Status(Unit("asl:squad", "finnish", null)));
        Assert.Null(Status(Unit("asl:squad", "german", null, inexperienced: null)));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:squad", "german", null)));
        Assert.Equal(StealthStatus.Normal, Status(Unit("asl:vehicle", "finnish", inexperienced: true)));
        Assert.Null(ScenarioA1StealthCalculator.ConcealmentDrm(Unit("asl:squad", "finnish", null)));
        Assert.Equal(-1, ScenarioA1StealthCalculator.ConcealmentDrm(Unit("asl:squad", "finnish", "elite")));
    }

    [Fact]
    public void InexperienceIsTheRulesPredicatesAndUnarmedAlways()
    {
        // Ruling RA11S.10 (A19.2, A19.3): ScenarioA1Experience's answer, and always when Unarmed.
        Assert.True(ScenarioA1StealthCalculator.Inexperienced(RuleState.True, false));
        Assert.False(ScenarioA1StealthCalculator.Inexperienced(RuleState.False, false));
        Assert.False(ScenarioA1StealthCalculator.Inexperienced(RuleState.Inapplicable, false));
        Assert.Null(ScenarioA1StealthCalculator.Inexperienced(RuleState.Unknown, false));
        Assert.True(ScenarioA1StealthCalculator.Inexperienced(RuleState.Unknown, true));
        Assert.True(ScenarioA1StealthCalculator.Inexperienced(RuleState.Inapplicable, true));
    }

    [Fact]
    public void TheAmbushCountsEachCauseOncePerSideAndTheOtherDrsPerUnit()
    {
        // A11.4 (p. 73; rulings R14.13, R29.9): once per side, "even if only a portion ... is qualified"; Stealthy and Lax are separate causes.
        StealthFacts[] two = [Unit("asl:hero"), Unit("asl:squad", "finnish", "elite")];
        Assert.Equal(StealthyOnly, ScenarioA1StealthCalculator.AmbushDrm(two)!);
        Assert.Equal(LaxThenStealthy,
            ScenarioA1StealthCalculator.AmbushDrm([Unit("asl:hero"), Unit("asl:squad", "italian", "1st-line")])!);
        Assert.Empty(ScenarioA1StealthCalculator.AmbushDrm([Unit("asl:squad", "german", "1st-line")])!);
        Assert.Null(ScenarioA1StealthCalculator.AmbushDrm([Unit("asl:hero"), Unit("asl:squad", "german", "green", inexperienced: null)]));
        // A12.152, A12.154 (p. 79): -1 per Stealthy unit, +1 per Lax unit.
        Assert.Equal(2, ScenarioA1StealthCalculator.PerUnitStealthy(two, StealthPurpose.Search));
        Assert.Equal(0, ScenarioA1StealthCalculator.PerUnitLax(two, StealthPurpose.Search));
        Assert.Equal(1, ScenarioA1StealthCalculator.PerUnitLax([Unit("asl:squad", "italian", "1st-line"), Unit("asl:hero")], StealthPurpose.SearchCasualty));
        Assert.Null(ScenarioA1StealthCalculator.PerUnitStealthy([Unit("asl:squad", "finnish", null)], StealthPurpose.Search));
        // A berserk unit is Lax only in CC (A15.432).
        Assert.Equal(0, ScenarioA1StealthCalculator.PerUnitLax([Unit("asl:squad", goodOrder: false, berserk: true, @class: "1st-line")], StealthPurpose.Search));
    }

    private static CloseCombatUnit Cc(string id, string definition, string side) =>
        new(id, definition, side, false, false, false, false, false, false, false, false, false, false, false);

    private static AmbushFacts Ambush(string terrain, params CloseCombatUnit[] units) =>
        new("CCPh", "bd01:G5:0", terrain, "german", units, new Dictionary<string, int>(StringComparer.Ordinal) { ["german"] = 4, ["russian"] = 3 });

    private static readonly JsonSerializerOptions Json = new();

    private static string Text(AmbushResolution resolution) => JsonSerializer.Serialize(resolution, Json);

    [Fact]
    public void ThePartisanExampleOfA114()
    {
        // A11.4's EX (p. 73): a concealed Partisan squad with a CX 8-0 leader advances: -2 (-3 for the concealed and Stealthy squad, +1 for the CX
        // leader). The squad is a designated German squad here, since no Partisan counter exists; the old reading has no designation.
        var units = new[]
        {
            Cc("p1", "attacker-squad", "german") with { Concealed = true, Advanced = true },
            Cc("pl", "attacker-leader-8-0", "german") with { Concealed = true, Advanced = true, Cx = true },
            Cc("r1", "defender-squad", "russian"),
        };
        var context = new StealthContext(StealthReading.Calculator)
        {
            Designations = new Dictionary<string, StealthDesignation>(StringComparer.Ordinal) { ["p1"] = StealthDesignation.Partisan },
        };
        var now = ScenarioA1CloseCombatCalculator.ResolveAmbush(Ambush("open-ground", units), Reference, context);
        var german = now.Sides.Single(side => side.Side == "german");
        Assert.Equal(-2, german.Drm.Sum(item => item.Value));
        Assert.Contains(ScenarioA1StealthCalculator.AmbushStealthy, german.Drm);
        var before = ScenarioA1CloseCombatCalculator.ResolveAmbush(Ambush("open-ground", units), Reference);
        Assert.Equal(-1, before.Sides.Single(side => side.Side == "german").Drm.Sum(item => item.Value));
    }

    [Fact]
    public void TheHeroExampleOfPage74()
    {
        // The CC example (p. 74), hex H3: a German 5-4-8 and Hero advance into a building with their BU PzV: +2 (+2 vehicle, +1 BU, -1 Stealthy [Hero]).
        var facts = Ambush("stone-building", Cc("g1", "attacker-squad", "german") with
        {
            Advanced = true
        }, Cc("gh", "attacker-hero", "german") with
        {
            Advanced = true
        },
            Cc("r1", "defender-squad", "russian")) with
        {
            Vehicles = [new AmbushVehicleFacts("german", 1)],
        };
        foreach (var resolution in new[] { ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, Reference),
            ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, Reference, new StealthContext(StealthReading.Calculator)) })
        {
            var german = resolution.Sides.Single(side => side.Side == "german");
            Assert.Equal(2, german.Drm.Sum(item => item.Value));
            Assert.Equal(HeroExampleNames, german.Drm.Select(item => item.Name));
            Assert.Empty(resolution.Sides.Single(side => side.Side == "russian").Drm);
        }
    }

    [Fact]
    public void FinnsAndItaliansChangeOnlyUnderTheCalculatorsReading()
    {
        // Ruling RA11S.8: the old reading leaves a Finnish elite squad without -1 and an Italian squad without +1; the calculator's gives both.
        var facts = Ambush("woods", Cc("f1", "finnish-elite-squad", "german") with
        {
            Advanced = true
        }, Cc("i1", "italian-squad", "russian"));
        var before = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, Reference);
        Assert.All(before.Sides, side => Assert.Empty(side.Drm));
        var now = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, Reference, new StealthContext(StealthReading.Calculator));
        Assert.Equal(StealthyOnly, now.Sides.Single(side => side.Side == "german").Drm);
        Assert.Equal(LaxOnly, now.Sides.Single(side => side.Side == "russian").Drm);
        // An SSR's Lax on the Finns' side cancels their Stealth (A11.17: "unless Lax").
        var lax = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, Reference, new StealthContext(StealthReading.Calculator) { SsrLaxSides = ["german"] });
        Assert.Equal(LaxOnly, lax.Sides.Single(side => side.Side == "german").Drm);
        Assert.Equal(Text(before), Text(ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, Reference, new StealthContext(StealthReading.HeroOnly))));
    }

    private static readonly string[] GridDefinitions =
    [
        "attacker-hero", "attacker-leader-8-1", "attacker-squad", "attacker-conscript-squad", "attacker-2nd-line-squad", "finnish-green-squad",
        "finnish-elite-squad", "attacker-crew", "german-vehicle-crew", "italian-squad", "italian-conscript-squad", "attacker-tank",
    ];

    private static IEnumerable<CloseCombatUnit> Grid(string id)
    {
        foreach (var definition in GridDefinitions)
        {
            foreach (var flags in Enumerable.Range(0, 64))
            {
                foreach (var inexperienced in new bool?[] { null, true, false })
                {
                    yield return Cc(id, definition, "german") with
                    {
                        Broken = (flags & 1) != 0,
                        Berserk = (flags & 2) != 0,
                        Captured = (flags & 4) != 0,
                        Heroic = (flags & 8) != 0,
                        Unarmed = (flags & 16) != 0,
                        Wounded = (flags & 32) != 0,
                        Advanced = true,
                        Inexperienced = inexperienced,
                    };
                }
            }
        }
    }

    // A copy of the Ambush's Lax and Stealthy lines before the claim.
    private static bool OldLax(IEnumerable<CloseCombatUnit> force) => force.Any(unit => unit.Berserk == true) || force.Any(unit =>
        Reference.Definitions[unit.DefinitionId!] is { } definition && (definition.IsMmc || ScenarioA1Definitions.IsCrew(definition.Kind))
        && (definition.Class == "conscript" || unit.Unarmed == true || (definition.Class == "green" && unit.Inexperienced == true)));

    private static bool OldStealthy(IEnumerable<CloseCombatUnit> force) => force.Any(unit =>
        (Reference.Definitions[unit.DefinitionId!].IsHero || (Reference.Definitions[unit.DefinitionId!].IsLeader && unit.Heroic == true))
        && unit.Broken != true && unit.Berserk != true && unit.Captured != true);

    private static void AssertForwardEqual(CloseCombatUnit[] german)
    {
        var facts = Ambush("woods", [.. german, Cc("r1", "defender-squad", "russian")]);
        var forwarded = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, Reference);
        Assert.Equal(Text(forwarded), Text(ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, Reference, null)));
        Assert.Equal(Text(forwarded), Text(ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, Reference, new StealthContext(StealthReading.HeroOnly))));
        if (forwarded.Disposition != CloseCombatResolution.Resolved)
        {
            return;
        }

        var force = german.Where(unit => unit.Captured != true).ToArray();
        var names = forwarded.Sides.SingleOrDefault(side => side.Side == "german")?.Drm.Select(item => item.Name).ToList() ?? [];
        Assert.Equal(force.Length != 0 && OldLax(force), names.Contains("lax"));
        Assert.Equal(force.Length != 0 && OldStealthy(force), names.Contains("stealthy"));
        // The order of the lines: broken, pinned, berserk, cx, lax, concealed, stealthy, leadership.
        string[] order = ["broken", "pinned", "berserk", "cx", "lax", "concealed", "stealthy"];
        var known = names.Where(name => order.Contains(name)).ToList();
        Assert.Equal(known.OrderBy(name => Array.IndexOf(order, name)), known);
        Assert.All(forwarded.Sides.SelectMany(side => side.Drm).Where(item => item.Name is "lax" or "stealthy"),
            item => Assert.True(item == ScenarioA1StealthCalculator.AmbushLax || item == ScenarioA1StealthCalculator.AmbushStealthy));
    }

    [Fact]
    public void TheAmbushForwardEqualsItsOldAnswerOnEveryUnit()
    {
        foreach (var unit in Grid("g1"))
        {
            AssertForwardEqual([unit]);
        }
    }

    [Fact]
    public void TheAmbushForwardEqualsItsOldAnswerOnPairs()
    {
        var picks = Grid("g1").Where((_, index) => index % 37 == 0).ToArray();
        var others = Grid("g2").Where((_, index) => index % 53 == 0).ToArray();
        foreach (var first in picks)
        {
            foreach (var second in others)
            {
                AssertForwardEqual([first, second]);
            }
        }
    }

    // The concealment gain's candidate and its dr's map (A12.122): one enemy in range 10 without a LOS, in Open Ground, so the dr is needed.
    private sealed class GainMap : IConcealmentFactReader
    {
        public int? Range(int from, int target) => 10;

        public bool LosOpen(int from, int target) => false;

        public int? Nvr(int unit) => null;

        public bool Illuminated(int location) => false;
    }

    private static ConcealmentCandidateFacts Candidate(string id, string kind, int leadership = 0) =>
        new(id, kind, true, true, false, true, false, true, kind == "asl:squad", kind == "asl:half-squad", 0, false, false, false, false, false, false,
            () => false, () => false, () => "open-ground", () => false, () => leadership);

    private static int? GainDrm(ConcealmentCandidateFacts unit) => ScenarioA1FireFollowUps.ConcealmentGains([unit],
        [new WatchingEnemyFacts(0, true, new SightSeerFacts(true, "russian", false, false, false, true, true, false, false, 1))], false, 7, new GainMap())
        .Single().Drm;

    [Fact]
    public void UnknownStealthRefusesOnlyAConcealmentDrThatIsNeeded()
    {
        var unit = Candidate("u", "asl:squad");
        WatchingEnemyFacts[] watchers = [new(0, true, new SightSeerFacts(true, "russian", false, false, false, true, true, false, false, 1))];
        var day = ScenarioA1FireFollowUps.ConcealmentGainsWithStealth([unit], watchers, false, 7, new GainMap());
        Assert.Null(day.Gains);
        Assert.StartsWith("play.stealth-facts:", day.Reason, StringComparison.Ordinal);
        var night = ScenarioA1FireFollowUps.ConcealmentGainsWithStealth([unit], watchers, true, 7, new GainMap());
        Assert.Null(night.Reason);
        Assert.Null(Assert.Single(night.Gains!).Drm);
        var ineligible = ScenarioA1FireFollowUps.ConcealmentGainsWithStealth([unit with { Broken = true }], watchers, false, 7, new GainMap());
        Assert.Null(ineligible.Reason);
        Assert.Empty(ineligible.Gains!);
        Assert.Single(ScenarioA1FireFollowUps.ConcealmentGains([unit], watchers, false, 7, new GainMap()));
        var known = ScenarioA1FireFollowUps.ConcealmentGainsWithStealth([unit with { Stealth = StealthStatus.Lax }], watchers, false, 7, new GainMap());
        Assert.Null(known.Reason);
        Assert.Equal(GainDrm(unit with
        {
            Stealth = StealthStatus.Lax
        }), Assert.Single(known.Gains!).Drm);
    }

    [Fact]
    public void UnknownContributingStealthStatusIsNotALegacyFallback()
    {
        Assert.NotNull(ScenarioA1StealthCalculator.FactsRefusal([StealthStatus.Normal, null]));
        Assert.Null(ScenarioA1StealthCalculator.FactsRefusal([]));
        Assert.Null(ScenarioA1StealthCalculator.FactsRefusal([StealthStatus.Normal, StealthStatus.Lax]));
    }

    [Fact]
    public void TheHeroExampleOfA12122TakesTheStatus()
    {
        // A12.122's EX (p. 77): "6 [dr] +1 [US#] -1 [Leadership] -1 [Stealthy] = 5".
        var hero = Candidate("gh", "asl:hero", leadership: -1) with
        {
            Stealth = Status(Unit("asl:hero"))
        };
        Assert.Equal(-1, GainDrm(hero));
        Assert.Equal(5, 6 + GainDrm(hero));
        Assert.Equal(-1, GainDrm(Candidate("gh", "asl:hero", leadership: -1)));
    }

    [Fact]
    public void TheConcealmentCandidateKeepsItsAnswerUntilItsStealthIsSet()
    {
        // Ruling RA11S.7: null reads the old Lax and Stealthy, on every input; set, the status alone (a Lax hero is +1, not 0).
        foreach (var kind in new[] { "asl:squad", "asl:hero", "asl:leader", "asl:half-squad" })
        {
            foreach (var heroic in new[] { false, true })
            {
                foreach (var inexperienced in new[] { false, true })
                {
                    var unit = Candidate("u", kind) with
                    {
                        Heroic = heroic,
                        Inexperienced = () => inexperienced
                    };
                    // The dr's other terms, read with a Normal status.
                    var rest = GainDrm(unit with
                    {
                        Stealth = StealthStatus.Normal
                    })!.Value;
                    var old = rest + (inexperienced ? 1 : 0) - (kind == "asl:hero" || (kind == "asl:leader" && heroic) ? 1 : 0);
                    Assert.Equal(old, GainDrm(unit));
                    foreach (var status in Enum.GetValues<StealthStatus>())
                    {
                        Assert.Equal(rest + ScenarioA1StealthCalculator.Drm(status), GainDrm(unit with
                        {
                            Stealth = status
                        }));
                    }
                }
            }
        }
    }

    private static SearcherFacts Searcher(bool hero, bool leader, bool heroic, bool lax) =>
        new("s", true, true, true, true, !hero && !leader, leader, false, false, false, false, false, hero, heroic, hero || leader ? 0 : 1, leader ? 0 : null, false, lax, false);

    [Fact]
    public void TheSearchAndCasualtyDrsKeepTheirAnswersUntilStealthIsSet()
    {
        // Ruling RA11S.7: with Stealth null, the old counts (a copy below); set, the status: a Lax hero gives +1 and no -1.
        foreach (var flags in Enumerable.Range(0, 16))
        {
            var (hero, leader, heroic, lax) = ((flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0, (flags & 8) != 0);
            var searcher = Searcher(hero, leader, heroic, lax);
            var defender = new MopUpDefenderFacts(hero, leader, heroic, hero || leader ? 0 : 1, leader ? 0 : null, false, lax);
            var stealthy = hero || (leader && heroic) ? 1 : 0;
            var expected = (lax ? 1 : 0) - stealthy;
            Assert.Equal(expected, ScenarioA1Searching.Drm([searcher]).Drm);
            Assert.Equal(expected, ScenarioA1MoppingUp.CasualtyDrm([defender]).Drm);
            foreach (var status in Enum.GetValues<StealthStatus>())
            {
                Assert.Equal(ScenarioA1StealthCalculator.Drm(status), ScenarioA1Searching.Drm([searcher with { Stealth = status }]).Drm);
                Assert.Equal(ScenarioA1StealthCalculator.Drm(status), ScenarioA1MoppingUp.CasualtyDrm([defender with { Stealth = status }]).Drm);
            }
        }

        var laxHero = Searcher(true, false, false, true);
        Assert.Equal(LaxHeroNotes, ScenarioA1Searching.Drm([laxHero]).Notes);
        Assert.Equal(LaxNotes, ScenarioA1Searching.Drm([laxHero with { Stealth = StealthStatus.Lax }]).Notes);
        StealthFacts[] finns = [Unit("asl:squad", "finnish", "elite"), Unit("asl:squad", "finnish", "1st-line")];
        var statuses = finns.Select(unit => ScenarioA1StealthCalculator.Status(unit, StealthPurpose.Search)!.Value).ToArray();
        Assert.Equal(-2, ScenarioA1Searching.Drm([Searcher(false, false, false, false) with { Stealth = statuses[0] },
            Searcher(false, false, false, false) with { Stealth = statuses[1] }]).Drm + 1);
    }

    private static readonly string[] OwnedPurposes = ["concealment", "search", "search-casualty"];

    private const string Owned = "a11.17-stealth";

    private const string AmbushContract = "a11.17-stealth-ambush";

    private static string Evaluate(JsonObject facts, string contract = Owned) =>
        ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find(contract)!, facts).Outcome;

    private static JsonObject Facts(string kind, string nationality, string @class, string? purpose = "concealment", bool? inexperienced = false,
        string designation = "none", bool ssrLax = false, bool berserk = false, bool goodOrder = true)
    {
        var facts = new JsonObject
        {
            ["unitId"] = "u1",
            ["goodOrder"] = goodOrder,
            ["berserk"] = berserk,
            ["banzaiOrHumanWave"] = false,
            ["ssrStealthy"] = false,
            ["ssrLax"] = ssrLax,
            ["kind"] = kind,
            ["heroic"] = false,
            ["nationality"] = nationality,
            ["class"] = @class,
            ["designation"] = designation,
            ["inexperienced"] = inexperienced,
        };
        if (purpose is not null)
        {
            facts["purpose"] = purpose;
        }

        return facts;
    }

    private static JsonObject AmbushFactsOf(string kind, string nationality, string @class, bool? inexperienced = false, bool ssrLax = false, bool berserk = false,
        bool goodOrder = true) => Facts(kind, nationality, @class, null, inexperienced, ssrLax: ssrLax, berserk: berserk, goodOrder: goodOrder);

    [Fact]
    public void TheContractGivesEachOutcome()
    {
        Assert.Equal("stealthy", Evaluate(Facts("asl:squad", "finnish", "elite")));
        Assert.Equal("stealthy", Evaluate(Facts("asl:hero", "russian", "none", "concealment")));
        Assert.Equal("stealthy", Evaluate(Facts("asl:squad", "british", "1st-line", "search", designation: "anzac")));
        Assert.Equal("lax", Evaluate(Facts("asl:squad", "finnish", "elite", ssrLax: true)));
        Assert.Equal("lax", Evaluate(Facts("asl:squad", "italian", "1st-line", "search-casualty")));
        Assert.Equal("normal", Evaluate(Facts("asl:squad", "finnish", "2nd-line")));
        Assert.Equal("normal", Evaluate(Facts("asl:squad", "finnish", "green")));
        // A berserk unit is Lax only in CC (A15.432): not for the Searching dr.
        Assert.Equal("normal", Evaluate(Facts("asl:hero", "german", "none", "search", berserk: true, goodOrder: false)));
        // A Green MMC whose Inexperience is unknown: not evaluated.
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate(Facts("asl:squad", "finnish", "green", inexperienced: null)));
        // Each dr is asked by one contract: the Ambush is not a purpose of this one.
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate(Facts("asl:squad", "finnish", "elite", "ambush")));
    }

    [Fact]
    public void TheAmbushContractGivesEachOutcomeAsTheAmbushDrmDoes()
    {
        // A11.4, A11.17, A11.18 (p. 73): the side's drm from this unit, as ScenarioA1StealthCalculator.AmbushDrm gives it.
        Assert.Equal("stealthy", Evaluate(AmbushFactsOf("asl:squad", "finnish", "elite"), AmbushContract));
        Assert.Equal("stealthy", Evaluate(AmbushFactsOf("asl:hero", "german", "none"), AmbushContract));
        Assert.Equal("lax", Evaluate(AmbushFactsOf("asl:squad", "finnish", "elite", ssrLax: true), AmbushContract));
        Assert.Equal("lax", Evaluate(AmbushFactsOf("asl:hero", "german", "none", berserk: true, goodOrder: false), AmbushContract));
        Assert.Equal("lax", Evaluate(AmbushFactsOf("asl:squad", "italian", "1st-line"), AmbushContract));
        Assert.Equal("normal", Evaluate(AmbushFactsOf("asl:squad", "finnish", "2nd-line"), AmbushContract));
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate(AmbushFactsOf("asl:squad", "finnish", "green", inexperienced: null), AmbushContract));
        // It has no purpose fact.
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate(Facts("asl:squad", "finnish", "elite", "concealment"), AmbushContract));
    }

    [Fact]
    public void TheEnemyOfAConcealedUnitCannotEvaluateItOutsideTheAmbush()
    {
        // The other side asks about a "?" stack: the unit and what it is are its owner's, so the facts it may give leave the verdict not evaluated.
        var facts = Facts("asl:squad", "finnish", "elite");
        foreach (var hidden in new[] { "unitId", "kind", "nationality", "class", "designation", "heroic" })
        {
            var partial = facts.DeepClone().AsObject();
            partial.Remove(hidden);
            Assert.Equal(ContractVerdict.NotEvaluated, Evaluate(partial));
        }

        string[] unitFacts = ["kind", "heroic", "nationality", "class", "designation", "inexperienced"];
        var owned = ScenarioA1RuleContracts.Find(Owned)!;
        Assert.All(owned.Facts.Where(fact => unitFacts.Contains(fact.Name) || fact.Name == "unitId"),
            fact => Assert.Equal((ContractDisclosure.Owner, "unitId"), (fact.Disclosure, fact.Concerns)));
        Assert.All(owned.Facts.Where(fact => fact.Name is "purpose" or "goodOrder" or "berserk" or "ssrStealthy" or "ssrLax"),
            fact => Assert.Equal(ContractDisclosure.All, fact.Disclosure));
        Assert.Equal(OwnedPurposes, owned.Facts.Single(fact => fact.Name == "purpose").Choices!);

        // A11.19 (p. 73) and the p. 74 example: in CC every unit reveals its Strength Factor before the Ambush dr, so both sides know what it is.
        var ambush = ScenarioA1RuleContracts.Find(AmbushContract)!;
        Assert.All(ambush.Facts, fact => Assert.Equal((ContractDisclosure.All, (string?)null), (fact.Disclosure, fact.Concerns)));
        Assert.DoesNotContain(ambush.Facts, fact => fact.Name == "purpose");
    }
}
