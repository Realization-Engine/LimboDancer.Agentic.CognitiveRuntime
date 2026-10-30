using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The A20.54 claim, the captor's CVP for a prisoner (A26.222, p. 100; A20.1, p. 86; A20.54, A20.55, p. 88; rulings RA20.3 to RA20.5, RA20.12):
/// <see cref="ScenarioA1PrisonerCalculator.PrisonerCvp"/>, the Victory calculator's forward equal to its old arms while
/// <see cref="VictoryStandingUnitFacts.EliminatedBy"/> is null, its reading when set, and the CVP contract.
/// </summary>
public sealed class ScenarioA1PrisonerCvpTests
{
    [Fact]
    public void ACapturedSquadCountsNormalInPlayAndDoubleAtTheEnd()
    {
        Assert.Equal(2, ScenarioA1PrisonerCalculator.PrisonerCvp(2, PrisonerCvpStatus.Held, false));
        Assert.Equal(4, ScenarioA1PrisonerCalculator.PrisonerCvp(2, PrisonerCvpStatus.Held, true));
        Assert.Equal(2, ScenarioA1PrisonerCalculator.PrisonerCvp(2, PrisonerCvpStatus.ExitedUnderGuard, false));
        Assert.Equal(4, ScenarioA1PrisonerCalculator.PrisonerCvp(2, PrisonerCvpStatus.ExitedUnderGuard, true));
        Assert.Equal(4, ScenarioA1PrisonerCalculator.PrisonerCvp(2, PrisonerCvpStatus.EliminatedByOwnSide, false));
        Assert.Equal(4, ScenarioA1PrisonerCalculator.PrisonerCvp(2, PrisonerCvpStatus.EliminatedByOwnSide, true));
        Assert.Equal(2, ScenarioA1PrisonerCalculator.PrisonerCvp(2, PrisonerCvpStatus.EliminatedOtherwise, true));
        Assert.Equal(0, ScenarioA1PrisonerCalculator.PrisonerCvp(2, PrisonerCvpStatus.Escaped, true));
    }

    [Fact]
    public void OnlyACapturedUnitItsOwnSideEliminatesCountsDouble()
    {
        Assert.Equal(PrisonerCvpStatus.EliminatedByOwnSide, ScenarioA1PrisonerCalculator.EliminatedStatus(true, "russian", "russian"));
        Assert.Equal(PrisonerCvpStatus.EliminatedOtherwise, ScenarioA1PrisonerCalculator.EliminatedStatus(true, "russian", "german"));
        Assert.Equal(PrisonerCvpStatus.EliminatedOtherwise, ScenarioA1PrisonerCalculator.EliminatedStatus(true, "russian", null));
        // RA20.3: an Unarmed unit that is no prisoner counts normal, whoever eliminated it.
        Assert.Equal(PrisonerCvpStatus.EliminatedOtherwise, ScenarioA1PrisonerCalculator.EliminatedStatus(false, "russian", "russian"));
    }

    private sealed class Reader(IReadOnlyList<VictoryStandingUnitFacts> standing) : IVictoryStateReader
    {
        public int Count => 2;

        public bool SetupClosed(int state) => state > 0;

        public IEnumerable<(string Gun, string HolderSide)> GunHolders(int state) => [];

        public IEnumerable<(string Building, string Side, IReadOnlyList<string> Locations)> NewlySecured(int state) => [];

        public IReadOnlyList<VictoryUnitFacts> Units(int state) => [];

        public IReadOnlyList<VictoryStandingUnitFacts> Standing(int state) => standing;

        public IReadOnlyList<VictoryGunFacts> Guns(int state) => [];

        public IReadOnlyList<VictoryExitFacts> Exits(int state) => [];
    }

    private static readonly VictoryCardFacts Card = new(["russian", "german"], [], "german", _ => null, [], 1, "bd01", _ => null, null);

    private static int GermanCvp(VictoryStandingUnitFacts unit, bool ended) =>
        ScenarioA1VictoryCalculator.Evaluate(Card, new Reader([unit]), ended, null, new VictoryFoldFacts(null, null, 0, 0, null))!.Sides.Single(side => side.Side == "german").Cvp;

    private static VictoryStandingUnitFacts Russian(VictoryStatus status, bool captured, bool vehicle = false, VictoryExitFacts? exit = null, string? eliminatedBy = null) =>
        new("r", "russian", false, status, captured, vehicle, () => 2, exit)
        {
            EliminatedBy = eliminatedBy,
        };

    private static VictoryExitFacts UnderGuard() => new("r", false, false, "german", true, "bottom", _ => false, ("russian", () => 2), null);

    [Fact]
    public void TheForwardEqualsTheOldArmsWhileEliminatedByIsNull()
    {
        foreach (var ended in new[] { false, true })
        {
            foreach (var captured in new[] { false, true })
            {
                foreach (var vehicle in new[] { false, true })
                {
                    // The Victory calculator's old arms, copied: eliminated or wrecked its VP; captured active or taken off under guard, double at the end.
                    var doubled = 2 * (ended ? 2 : 1);
                    Assert.Equal(2, GermanCvp(Russian(VictoryStatus.Eliminated, captured, vehicle), ended));
                    Assert.Equal(2, GermanCvp(Russian(VictoryStatus.Wrecked, captured, vehicle), ended));
                    Assert.Equal(doubled, GermanCvp(Russian(VictoryStatus.Exited, captured, vehicle, UnderGuard()), ended));
                    Assert.Equal(captured ? doubled : 0, GermanCvp(Russian(VictoryStatus.Active, captured, vehicle), ended));
                }
            }
        }
    }

    [Fact]
    public void APrisonerItsOwnSideEliminatesScoresTheCaptorDoubleAtOnce()
    {
        Assert.Equal(4, GermanCvp(Russian(VictoryStatus.Eliminated, true, eliminatedBy: "russian"), false));
        Assert.Equal(4, GermanCvp(Russian(VictoryStatus.Eliminated, true, eliminatedBy: "russian"), true));
        // Eliminated by its captor (Massacre, or its Guard in an escape attempt): normal.
        Assert.Equal(2, GermanCvp(Russian(VictoryStatus.Eliminated, true, eliminatedBy: "german"), false));
        // RA20.3: an Unarmed unit no longer captured counts normal.
        Assert.Equal(2, GermanCvp(Russian(VictoryStatus.Eliminated, false, eliminatedBy: "russian"), false));
        // Escaped (active, no longer captured): nothing; recaptured, held again.
        Assert.Equal(0, GermanCvp(Russian(VictoryStatus.Active, false), true));
        Assert.Equal(4, GermanCvp(Russian(VictoryStatus.Active, true), true));
        // A captured vehicle its own side eliminates keeps the old arm: its normal VP.
        Assert.Equal(2, GermanCvp(Russian(VictoryStatus.Eliminated, true, vehicle: true, eliminatedBy: "russian"), false));
        Assert.Equal(2, GermanCvp(Russian(VictoryStatus.Eliminated, true, vehicle: true, eliminatedBy: "russian"), true));
    }

    [Theory]
    [InlineData(2, "held", false, "normal")]
    [InlineData(2, "held", true, "double")]
    [InlineData(2, "exited-under-guard", true, "double")]
    [InlineData(1, "eliminated-by-own-side", false, "double")]
    [InlineData(1, "eliminated-otherwise", true, "normal")]
    [InlineData(2, "escaped", true, "none")]
    public void TheCvpContractGivesTheVerdict(int vp, string status, bool ended, string outcome)
    {
        var facts = $$"""{"vp":{{vp}},"status":"{{status}}","ended":{{(ended ? "true" : "false")}}}""";
        Assert.Contains($"\"outcome\":\"{outcome}\"", ScenarioA1RuleContracts.Evaluate("a26.222-prisoner-cvp", facts), StringComparison.Ordinal);
    }

    [Fact]
    public void TheCvpContractIsNotEvaluatedWithoutAWholeVp()
    {
        Assert.Contains("\"outcome\":\"not-evaluated\"", ScenarioA1RuleContracts.Evaluate("a26.222-prisoner-cvp", """{"vp":1.5,"status":"held","ended":false}"""), StringComparison.Ordinal);
        Assert.Contains("\"outcome\":\"not-evaluated\"", ScenarioA1RuleContracts.Evaluate("a26.222-prisoner-cvp", """{"status":"held","ended":false}"""), StringComparison.Ordinal);
        // A standing the contract does not read is never counted as escaped.
        Assert.DoesNotContain("\"outcome\":\"none\"", ScenarioA1RuleContracts.Evaluate("a26.222-prisoner-cvp", """{"vp":1,"status":"eliminated-by-captor","ended":false}"""),
            StringComparison.Ordinal);
    }
}
