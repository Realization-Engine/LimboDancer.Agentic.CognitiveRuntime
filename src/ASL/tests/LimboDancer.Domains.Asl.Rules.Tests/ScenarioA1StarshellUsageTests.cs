using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The E1.8 claim's Starshell members of <see cref="ScenarioA1Starshells"/> (E1.91, E1.92, E1.921 p. 226; E1.923 p. 227): who fires one, the Usage dr a
/// CE Armor Leader needs, the first Starshell's three events, where a Starshell is fired from and what it lights, the old members that forward to them, and
/// the contract <c>e1.921-starshell-usage</c>.
/// </summary>
public sealed class ScenarioA1StarshellUsageTests
{
    private const string FirstRefusal = "play.starshell-first";

    private static readonly bool[] Bools = [false, true];

    private static readonly StarshellFirer[] Firers = Enum.GetValues<StarshellFirer>();

    // FirerBar's body before the claim.
    private static string? OldFirerBar(string unitId, bool leader, bool afv, bool mmc, bool buttonedUp, bool stunned, bool shocked, bool goodOrder, bool pinned, bool ti, bool captured) =>
        (!leader && !afv && !mmc) || (afv && (buttonedUp || stunned || shocked)) || (!afv && !goodOrder) || pinned || ti || captured
            ? $"play.starshell-firer: {unitId} is not a Good Order, unpinned, not TI leader, CE AFV, or MMC (E1.92, E1.921)"
            : null;

    private static string? Refusal(StarshellFirer firer, bool goodOrder = true, bool stunned = false, bool shocked = false, bool pinned = false, bool ti = false,
        bool captured = false, bool aerial = false, bool inPillbox = false, bool? interior = false) =>
        ScenarioA1Starshells.FirerRefusal("g-leader", firer, stunned, shocked, goodOrder, pinned, ti, captured, aerial, inPillbox, interior);

    [Fact]
    public void TheFirerIsReadAfvFirst()
    {
        Assert.Equal(StarshellFirer.Leader, ScenarioA1Starshells.FirerOf(leader: true, afv: false, mmc: false, buttonedUp: false, armorLeader: false));
        Assert.Equal(StarshellFirer.Mmc, ScenarioA1Starshells.FirerOf(leader: false, afv: false, mmc: true, buttonedUp: false, armorLeader: false));
        Assert.Equal(StarshellFirer.CeAfv, ScenarioA1Starshells.FirerOf(leader: false, afv: true, mmc: false, buttonedUp: false, armorLeader: false));
        Assert.Equal(StarshellFirer.CeAfvWithArmorLeader, ScenarioA1Starshells.FirerOf(leader: false, afv: true, mmc: false, buttonedUp: false, armorLeader: true));
        // A BU AFV with an Armor Leader fires none (E1.92: "one friendly CE AFV").
        Assert.Equal(StarshellFirer.Other, ScenarioA1Starshells.FirerOf(leader: false, afv: true, mmc: false, buttonedUp: true, armorLeader: true));
        Assert.Equal(StarshellFirer.Other, ScenarioA1Starshells.FirerOf(leader: true, afv: true, mmc: false, buttonedUp: true, armorLeader: false));
        Assert.Equal(StarshellFirer.Other, ScenarioA1Starshells.FirerOf(leader: false, afv: false, mmc: false, buttonedUp: false, armorLeader: false));
    }

    [Fact]
    public void ALeaderAndACeArmorLeaderNeedFourAndTheOthersTwo()
    {
        Assert.Equal(4, ScenarioA1Starshells.UsageNeed(StarshellFirer.Leader));
        Assert.Equal(4, ScenarioA1Starshells.UsageNeed(StarshellFirer.CeAfvWithArmorLeader));
        Assert.Equal(2, ScenarioA1Starshells.UsageNeed(StarshellFirer.CeAfv));
        Assert.Equal(2, ScenarioA1Starshells.UsageNeed(StarshellFirer.Mmc));
        Assert.Equal(4, ScenarioA1Starshells.UsageNeed(true));
        Assert.Equal(2, ScenarioA1Starshells.UsageNeed(false));
        Assert.All(Firers, firer => Assert.Equal(firer is StarshellFirer.Leader or StarshellFirer.CeAfvWithArmorLeader, ScenarioA1Starshells.FiresAsLeader(firer)));
    }

    [Fact]
    public void FirerRefusalKeepsTheOldChecksThenAerialPillboxAndInterior()
    {
        var old = OldFirerBar("g-leader", true, false, false, false, false, false, false, false, false, false)!;
        Assert.Equal(old, Refusal(StarshellFirer.Leader, goodOrder: false));
        Assert.Equal(old, Refusal(StarshellFirer.Leader, pinned: true));
        Assert.Equal(old, Refusal(StarshellFirer.Leader, ti: true));
        Assert.Equal(old, Refusal(StarshellFirer.Leader, captured: true));
        Assert.Equal(old, Refusal(StarshellFirer.CeAfv, stunned: true));
        Assert.Equal(old, Refusal(StarshellFirer.CeAfvWithArmorLeader, shocked: true));
        Assert.Equal(old, Refusal(StarshellFirer.Other));
        // The old check comes first: a broken leader in a pillbox gets the old text.
        Assert.Equal(old, Refusal(StarshellFirer.Leader, goodOrder: false, inPillbox: true));
        Assert.Contains("Aerial or in a pillbox", Refusal(StarshellFirer.Leader, aerial: true), StringComparison.Ordinal);
        Assert.Contains("Aerial or in a pillbox", Refusal(StarshellFirer.Mmc, inPillbox: true), StringComparison.Ordinal);
        Assert.Contains("Interior Building Hex", Refusal(StarshellFirer.Leader, interior: true), StringComparison.Ordinal);
        Assert.Contains("Interior Building Hex", Refusal(StarshellFirer.Leader, interior: null), StringComparison.Ordinal);
        Assert.Null(Refusal(StarshellFirer.Leader));
        Assert.Null(Refusal(StarshellFirer.CeAfvWithArmorLeader, goodOrder: false));
    }

    [Fact]
    public void FirerBarKeepsItsOldAnswerOnEveryCombinationOfItsFlags()
    {
        for (var bits = 0; bits < 1 << 10; bits++)
        {
            bool Bit(int index) => (bits & (1 << index)) != 0;
            Assert.Equal(OldFirerBar("u", Bit(0), Bit(1), Bit(2), Bit(3), Bit(4), Bit(5), Bit(6), Bit(7), Bit(8), Bit(9)),
                ScenarioA1Starshells.FirerBar("u", Bit(0), Bit(1), Bit(2), Bit(3), Bit(4), Bit(5), Bit(6), Bit(7), Bit(8), Bit(9)));
        }
    }

    [Fact]
    public void EachOfTheThreeEventsOpensTheFirstStarshell()
    {
        Assert.StartsWith(FirstRefusal, ScenarioA1Starshells.FirstStarshellRefusal(false, false, false, false, false), StringComparison.Ordinal);
        Assert.Null(ScenarioA1Starshells.FirstStarshellRefusal(true, false, false, false, false));
        Assert.Null(ScenarioA1Starshells.FirstStarshellRefusal(false, true, false, false, false));
        Assert.Null(ScenarioA1Starshells.FirstStarshellRefusal(false, false, false, true, false));
        Assert.Null(ScenarioA1Starshells.FirstStarshellRefusal(false, false, false, false, true));
        // Event 1 holds only while the friendly force has no motorized vehicle on board, and an unknown motion does not open it.
        Assert.StartsWith(FirstRefusal, ScenarioA1Starshells.FirstStarshellRefusal(false, false, true, true, false), StringComparison.Ordinal);
        Assert.StartsWith(FirstRefusal, ScenarioA1Starshells.FirstStarshellRefusal(false, false, false, null, false), StringComparison.Ordinal);
    }

    [Fact]
    public void TheOldFirstBarKeepsItsAnswerAndAgreesInVerdictOnAQualifyingGunflash()
    {
        foreach (var used in Bools)
        {
            foreach (var seen in Bools)
            {
                foreach (var gunflash in Bools)
                {
                    Assert.Equal(ScenarioA1Starshells.FirstBar(used, () => seen, () => gunflash) is null,
                        ScenarioA1Starshells.FirstStarshellRefusal(used, seen, false, false, gunflash) is null);
                }
            }
        }
    }

    [Fact]
    public void ACeArmorLeaderIsExemptFromTheTimingRestrictionAsALeaderIs()
    {
        Assert.Null(ScenarioA1Starshells.TimingBar(true, "t1", "t2", ScenarioA1Starshells.FiresAsLeader(StarshellFirer.CeAfvWithArmorLeader), "dfph", true));
        Assert.NotNull(ScenarioA1Starshells.TimingBar(true, "t1", "t2", ScenarioA1Starshells.FiresAsLeader(StarshellFirer.CeAfv), "dfph", true));
    }

    [Fact]
    public void AStarshellLightsThreeHexesAndNoInteriorOrSubterraneanLocation()
    {
        Assert.True(ScenarioA1Starshells.StarshellIlluminates(3, false));
        Assert.True(ScenarioA1Starshells.StarshellIlluminates(0, false));
        Assert.False(ScenarioA1Starshells.StarshellIlluminates(4, false));
        Assert.False(ScenarioA1Starshells.StarshellIlluminates(3, true));
        Assert.False(ScenarioA1Starshells.StarshellIlluminates(3, null));
        Assert.False(ScenarioA1Starshells.StarshellIlluminates(null, false));
        // The old member keeps lighting every Location within three hexes.
        Assert.True(ScenarioA1NightAndWeather.Illuminated(true, [3], []));
    }

    private static JsonObject Facts(string firer = "leader", bool firedBefore = false, bool enemyInLos = false, bool friendlyMotorized = false,
        bool? enemyMoved = false, bool gunflash = true) => new()
        {
            ["night"] = true,
            ["phase"] = "PFPh",
            ["firerSidePhasing"] = true,
            ["attemptedFromHex"] = false,
            ["firerId"] = "g-leader",
            ["firer"] = firer,
            ["goodOrder"] = true,
            ["pinned"] = false,
            ["ti"] = false,
            ["captured"] = false,
            ["stunned"] = false,
            ["shocked"] = false,
            ["inPillbox"] = false,
            ["aerial"] = false,
            ["interiorOrSubterranean"] = false,
            ["firerHasEnemyInLos"] = enemyInLos,
            ["friendlyMotorizedOnboard"] = friendlyMotorized,
            ["enemyMotorizedMovedWithin16"] = enemyMoved,
            ["starshellFiredBefore"] = firedBefore,
            ["firstStarshellTurnPast"] = false,
            ["startOfPhaseBeforeActs"] = true,
            ["qualifyingGunflashOccurred"] = gunflash,
        };

    private static ContractVerdict Evaluate(JsonObject facts) => ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find("e1.921-starshell-usage")!, facts);

    [Fact]
    public void TheContractGivesEachOutcome()
    {
        var leader = Evaluate(Facts());
        Assert.Equal("usage-4", leader.Outcome);
        Assert.Equal(4, Assert.Single(leader.Rolls).Target);
        Assert.Equal("usage-4", Evaluate(Facts("ce-afv-armor-leader")).Outcome);
        Assert.Equal("usage-2", Evaluate(Facts("ce-afv")).Outcome);
        Assert.Equal("usage-2", Evaluate(Facts("mmc", gunflash: false, enemyMoved: true)).Outcome);
        Assert.Equal("refused", Evaluate(Facts("other")).Outcome);
        Assert.Equal("refused", Evaluate(Facts("mmc", gunflash: false)).Outcome);
        Assert.Equal("refused", Evaluate(Facts("mmc", gunflash: false, enemyMoved: true, friendlyMotorized: true)).Outcome);

        // After the first Starshell's Player Turn, a MMC fires one only at the start of the phase; a leader at any time.
        var late = Facts("mmc", firedBefore: true);
        late["firstStarshellTurnPast"] = true;
        late["startOfPhaseBeforeActs"] = false;
        Assert.Equal("refused", Evaluate(late).Outcome);
        late["firer"] = "ce-afv-armor-leader";
        Assert.Equal("usage-4", Evaluate(late).Outcome);
    }

    [Fact]
    public void WithinTheFirstStarshellsPlayerTurnAMmcFiresOneAfterOthersHaveActed()
    {
        // E1.921: the timing restriction starts only after the Player Turn of the first Starshell.
        var within = Facts("mmc", firedBefore: true);
        within["firstStarshellTurnPast"] = false;
        within["startOfPhaseBeforeActs"] = false;
        Assert.Equal("usage-2", Evaluate(within).Outcome);
    }

    [Fact]
    public void TheContractRefusesByDayOutOfPhaseAndASecondAttemptFromTheHex()
    {
        var day = Facts();
        day["night"] = false;
        var refused = Evaluate(day);
        Assert.Equal("refused", refused.Outcome);
        Assert.Equal("E1.9", Assert.Single(refused.Reasons).Rule);
        Assert.StartsWith("play.starshell-day", refused.Reasons[0].Text, StringComparison.Ordinal);

        // E1.92: the PFPh by the phasing side; the MPh (Defensive First Fire) or the DFPh by the other.
        var dfph = Facts();
        dfph["phase"] = "DFPh";
        Assert.Equal("refused", Evaluate(dfph).Outcome);
        Assert.Equal("E1.92", Assert.Single(Evaluate(dfph).Reasons).Rule);
        dfph["firerSidePhasing"] = false;
        Assert.Equal("usage-4", Evaluate(dfph).Outcome);
        var mph = Facts("mmc");
        mph["phase"] = "MPh";
        mph["firerSidePhasing"] = false;
        Assert.Equal("usage-2", Evaluate(mph).Outcome);
        var pfph = Facts();
        pfph["firerSidePhasing"] = false;
        Assert.Equal("refused", Evaluate(pfph).Outcome);

        // After the first Starshell's Player Turn, a MMC in the DFPh is refused; a leader is not.
        var late = Facts("mmc", firedBefore: true);
        late["phase"] = "DFPh";
        late["firerSidePhasing"] = false;
        late["firstStarshellTurnPast"] = true;
        Assert.StartsWith("play.starshell-timing", Assert.Single(Evaluate(late).Reasons).Text, StringComparison.Ordinal);
        late["firer"] = "leader";
        Assert.Equal("usage-4", Evaluate(late).Outcome);

        // E1.92: one attempt per hex per phase.
        var again = Facts();
        again["attemptedFromHex"] = true;
        var once = Evaluate(again);
        Assert.Equal("refused", once.Outcome);
        Assert.Equal("E1.92", Assert.Single(once.Reasons).Rule);
        Assert.StartsWith("play.starshell-once", once.Reasons[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownFactLeavesTheQuestionOpenUnlessAKnownFactRefuses()
    {
        var interior = Facts();
        interior["interiorOrSubterranean"] = null;
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate(interior).Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate(Facts("mmc", gunflash: false, enemyMoved: null)).Outcome);
        // A qualifying Gunflash opens it whatever the vehicle did.
        Assert.Equal("usage-2", Evaluate(Facts("mmc", enemyMoved: null)).Outcome);
        // A known refusal stands.
        var pinned = Facts("other");
        pinned["interiorOrSubterranean"] = null;
        Assert.Equal("refused", Evaluate(pinned).Outcome);
    }

    [Fact]
    public void TheOpponentKnowsNoneOfTheFirersOwnFacts()
    {
        var russian = new JsonObject
        {
            ["night"] = true,
            ["phase"] = "PFPh",
            ["firerSidePhasing"] = true,
            ["attemptedFromHex"] = false,
            ["starshellFiredBefore"] = false,
            ["firstStarshellTurnPast"] = false,
            ["startOfPhaseBeforeActs"] = true,
            ["qualifyingGunflashOccurred"] = true,
        };
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate(russian).Outcome);
    }
}
