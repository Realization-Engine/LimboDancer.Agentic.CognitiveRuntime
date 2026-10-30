using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The E1.8 claim's Gunflash calculator, <see cref="ScenarioA1GunflashCalculator"/> (E1.8, E1.82, E1.89, E1.91 p. 226; ASOP 5.33B p. 680): which acts mark
/// a Gunflash, which place a fire counter at night, how long a mark lasts, which mark opens the first Starshell, the old members that forward to it, and the
/// contract <c>e1.8-gunflash</c>.
/// </summary>
public sealed class ScenarioA1GunflashCalculatorTests
{
    private static readonly string[] PhaseKeys = ["rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph", "", "other"];

    private static readonly bool[] Bools = [false, true];

    private static readonly GunflashCause[] Rule89 =
        [GunflashCause.DismantleOrAssemble, GunflashCause.Spotting, GunflashCause.RadioOrPhone, GunflashCause.AmmoVehicleBenefit];

    // The old answers, copied from the members before the claim.
    private static readonly UnitCondition[] OldMarkers =
        [UnitCondition.PrepFire, UnitCondition.FirstFire, UnitCondition.FinalFire, UnitCondition.BoundingFire, UnitCondition.IntensiveFire, UnitCondition.Melee];

    private static IReadOnlyList<UnitCondition> OldClearedMarkers(string phase, bool night) => phase switch
    {
        "dfph" when night => [UnitCondition.IntensiveFire],
        "dfph" => [UnitCondition.FinalFire, UnitCondition.FirstFire, UnitCondition.IntensiveFire],
        "afph" when night => [UnitCondition.PrepFire, UnitCondition.BoundingFire, UnitCondition.IntensiveFire, UnitCondition.FinalFire, UnitCondition.FirstFire],
        "afph" => [UnitCondition.PrepFire, UnitCondition.BoundingFire, UnitCondition.IntensiveFire],
        "ccph" => [UnitCondition.Pinned, UnitCondition.Ti],
        "mph" => [UnitCondition.CcReaction],
        "rph" => [UnitCondition.DesperationMorale],
        _ => [],
    };

    [Fact]
    public void AnAttackAMeleeAndADeclarationMarkAGunflashAndNoneOfE189sActsOrAStarshellDoes()
    {
        Assert.True(ScenarioA1GunflashCalculator.PlacesGunflash(GunflashCause.Attack));
        Assert.True(ScenarioA1GunflashCalculator.PlacesGunflash(GunflashCause.Melee));
        // RE1.4: E1.8 lists the Opportunity Fire counter, so the declared unit's Location is a Gunflash for sight.
        Assert.True(ScenarioA1GunflashCalculator.PlacesGunflash(GunflashCause.OpportunityFireDeclared));
        Assert.All(Rule89, cause => Assert.False(ScenarioA1GunflashCalculator.PlacesGunflash(cause)));
        Assert.False(ScenarioA1GunflashCalculator.PlacesGunflash(GunflashCause.Starshell));
    }

    [Fact]
    public void E189sActsPlaceNoFireCounterAtNightButStillCountAsUse()
    {
        Assert.All(Rule89, cause =>
        {
            Assert.False(ScenarioA1GunflashCalculator.PlacesFireCounter(cause, night: true));
            Assert.True(ScenarioA1GunflashCalculator.PlacesFireCounter(cause, night: false));
            Assert.True(ScenarioA1GunflashCalculator.CountsAsUse(cause));
        });
        Assert.True(ScenarioA1GunflashCalculator.PlacesFireCounter(GunflashCause.Attack, night: true));
        Assert.True(ScenarioA1GunflashCalculator.PlacesFireCounter(GunflashCause.Attack, night: false));
        Assert.True(ScenarioA1GunflashCalculator.PlacesFireCounter(GunflashCause.OpportunityFireDeclared, night: true));
        Assert.False(ScenarioA1GunflashCalculator.PlacesFireCounter(GunflashCause.Melee, night: true));
        Assert.False(ScenarioA1GunflashCalculator.PlacesFireCounter(GunflashCause.Starshell, night: true));
        Assert.False(ScenarioA1GunflashCalculator.CountsAsUse(GunflashCause.Attack));
        Assert.False(ScenarioA1GunflashCalculator.CountsAsUse(GunflashCause.Starshell));
    }

    [Fact]
    public void AMarkBelongsToItsLocationWhoeverIsThereNow()
    {
        // RE1.2: the marks carry no unit, so a firer that moved away or was eliminated leaves its Location marked.
        GunflashMark[] marks = [new("bd04:H5:0", "german", GunflashCause.Attack, true)];
        Assert.True(ScenarioA1GunflashCalculator.Marked("bd04:H5:0", marks, meleeInLocation: false));
        Assert.False(ScenarioA1GunflashCalculator.Marked("bd04:H6:0", marks, meleeInLocation: false));
        Assert.False(ScenarioA1GunflashCalculator.Marked("bd04:H5:0", [], meleeInLocation: false));
        Assert.True(ScenarioA1GunflashCalculator.Marked("bd04:H5:0", [], meleeInLocation: true));
        Assert.False(ScenarioA1GunflashCalculator.Marked("bd04:H5:0", [new("bd04:H5:0", "german", GunflashCause.DismantleOrAssemble, false)], meleeInLocation: false));
    }

    [Fact]
    public void MarksEndOnlyWithTheAfph()
    {
        Assert.All(PhaseKeys, phase => Assert.Equal(phase == "afph", ScenarioA1GunflashCalculator.MarksEnd(phase)));
    }

    [Fact]
    public void FireCountersStayAtNightThroughEveryPhaseButTheAfph()
    {
        Assert.True(ScenarioA1GunflashCalculator.FireCountersStayAtNight("dfph", night: true));
        Assert.False(ScenarioA1GunflashCalculator.FireCountersStayAtNight("afph", night: true));
        Assert.All(PhaseKeys, phase => Assert.False(ScenarioA1GunflashCalculator.FireCountersStayAtNight(phase, night: false)));
    }

    [Fact]
    public void ClearedMarkersKeepsItsOldAnswerForEveryPhaseDayAndNight()
    {
        foreach (var phase in PhaseKeys)
        {
            foreach (var night in Bools)
            {
                Assert.Equal(OldClearedMarkers(phase, night), ScenarioA1SequenceProjection.ClearedMarkers(phase, night));
            }
        }
    }

    [Fact]
    public void OnlyAnAttackVsAnEnemyUnitOpensTheFirstStarshell()
    {
        Assert.True(ScenarioA1GunflashCalculator.OpensFirstStarshell(new GunflashMark("bd04:H5:0", "german", GunflashCause.Attack, true)));
        Assert.False(ScenarioA1GunflashCalculator.OpensFirstStarshell(new GunflashMark("bd04:H5:0", "german", GunflashCause.Attack, false)));
        Assert.True(ScenarioA1GunflashCalculator.OpensFirstStarshell(GunflashCause.Melee, true));
        Assert.False(ScenarioA1GunflashCalculator.OpensFirstStarshell(GunflashCause.OpportunityFireDeclared, true));
        Assert.False(ScenarioA1GunflashCalculator.OpensFirstStarshell(GunflashCause.DismantleOrAssemble, true));
        Assert.False(ScenarioA1GunflashCalculator.OpensFirstStarshell(GunflashCause.Starshell, true));
    }

    [Fact]
    public void TheMarkersAreTheOldGunflashMarkers()
    {
        Assert.Equal(OldMarkers, ScenarioA1GunflashCalculator.Markers);
        Assert.Equal(OldMarkers, ScenarioA1NightAndWeather.GunflashMarkers);
        Assert.All(Enum.GetValues<UnitCondition>(), marker => Assert.Equal(OldMarkers.Contains(marker), ScenarioA1GunflashCalculator.MarkerIsGunflash(marker)));
    }

    [Fact]
    public void TheOldGunflashReadKeepsItsAnswerOnPresentOccupants()
    {
        // The old member reads the present occupants' counters; Marked replaces it (RE1.13).
        Assert.True(ScenarioA1NightAndWeather.Gunflash([condition => condition == UnitCondition.PrepFire]));
        Assert.False(ScenarioA1NightAndWeather.Gunflash([]));
    }

    private static string Outcome(string cause, bool night = true, bool vsEnemyUnit = true, bool firerStillInLocation = true) =>
        ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find("e1.8-gunflash")!, new JsonObject
        {
            ["cause"] = cause,
            ["night"] = night,
            ["vsEnemyUnit"] = vsEnemyUnit,
            ["firerStillInLocation"] = firerStillInLocation,
        }).Outcome;

    [Fact]
    public void TheContractGivesEachOutcome()
    {
        Assert.Equal("gunflash-opens", Outcome("attack"));
        Assert.Equal("gunflash-opens", Outcome("attack", firerStillInLocation: false));
        Assert.Equal("gunflash-opens", Outcome("melee"));
        Assert.Equal("gunflash", Outcome("attack", vsEnemyUnit: false));
        Assert.Equal("gunflash", Outcome("opportunity-fire-declared"));
        Assert.Equal("no-gunflash-no-counter", Outcome("dismantle-or-assemble"));
        Assert.Equal("no-gunflash-no-counter", Outcome("spotting"));
        Assert.Equal("no-gunflash-no-counter", Outcome("radio-or-phone"));
        Assert.Equal("no-gunflash-no-counter", Outcome("ammo-vehicle-benefit"));
        Assert.Equal("no-gunflash", Outcome("starshell"));
        Assert.Equal(ContractVerdict.NotEvaluated, Outcome("attack", night: false));
        Assert.Equal(ContractVerdict.NotEvaluated, Outcome("no-such-cause"));
    }
}
