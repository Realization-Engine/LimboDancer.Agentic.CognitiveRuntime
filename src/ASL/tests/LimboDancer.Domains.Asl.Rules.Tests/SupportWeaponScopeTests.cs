using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class SupportWeaponScopeTests
{
    [Fact]
    public void ConfigurationSharesInherentFireCapacityAndRejectsUnsupportedCarriage()
    {
        var facts = new SupportWeaponConfigurationFacts("pfph", true, true, true, false, true, true, true, false, 1, 2, true);
        Assert.Equal(SupportWeaponReadinessStatus.Allowed, ScenarioA1SupportWeaponConfigurationRules.Evaluate(facts).Status);
        Assert.Equal(SupportWeaponReadinessStatus.Refused, ScenarioA1SupportWeaponConfigurationRules.Evaluate(facts with
        {
            InherentFireUsed = true
        }).Status);
        Assert.Equal(SupportWeaponReadinessStatus.Refused, ScenarioA1SupportWeaponConfigurationRules.Evaluate(facts with
        {
            WeaponUsed = true
        }).Status);
        Assert.Equal(SupportWeaponReadinessStatus.NotEvaluated, ScenarioA1SupportWeaponConfigurationRules.Evaluate(facts with
        {
            Aboard = true
        }).Status);
        Assert.Equal(SupportWeaponReadinessStatus.NotEvaluated, ScenarioA1SupportWeaponConfigurationRules.Evaluate(facts with
        {
            GoodOrder = null
        }).Status);
    }

    [Fact]
    public void RepairAllowsSecondWeaponButNotAThirdOrARepeatedWeapon()
    {
        var facts = new SupportWeaponRepairFacts("rph", true, true, true, true, true, false, 3, false, false, 1, 2, false);
        Assert.Equal(SupportWeaponReadinessStatus.Allowed, ScenarioA1SupportWeaponRepairRules.Evaluate(facts).Status);
        Assert.Equal(SupportWeaponReadinessStatus.Refused, ScenarioA1SupportWeaponRepairRules.Evaluate(facts with
        {
            AttemptsThisPhase = 2
        }).Status);
        Assert.Equal(SupportWeaponReadinessStatus.Refused, ScenarioA1SupportWeaponRepairRules.Evaluate(facts with
        {
            AttemptedThisPlayerTurn = true
        }).Status);
        Assert.Equal(SupportWeaponReadinessStatus.NotEvaluated, ScenarioA1SupportWeaponRepairRules.Evaluate(facts with
        {
            Captured = null
        }).Status);
    }

    [Fact]
    public void WithdrawalNeedsKnownRetainedLoadWithinCapacity()
    {
        Assert.Equal(SupportWeaponReadinessStatus.Refused, ScenarioA1WithdrawalEquipmentRules.Load(false, false, false, [5, 1]).Readiness.Status);
        Assert.Equal(SupportWeaponReadinessStatus.Allowed, ScenarioA1WithdrawalEquipmentRules.Load(false, false, false, [1]).Readiness.Status);
        Assert.Equal(SupportWeaponReadinessStatus.NotEvaluated, ScenarioA1WithdrawalEquipmentRules.Load(false, false, false, null).Readiness.Status);
    }

    [Theory]
    [InlineData("ccph")]
    [InlineData("rtph")]
    public void StandaloneDropCannotBypassTheEnclosingWorkflow(string phase)
    {
        Assert.NotNull(ScenarioA1SupportWeaponPossessionRules.DropRefusal(phase, true, true, true, true, false, false));
    }

    [Fact]
    public void BrokenHolderExceptionRequiresAllRelationshipFacts()
    {
        Assert.True(ScenarioA1SupportWeaponPossessionRules.FromBrokenHolder(true, true, true, true));
        Assert.False(ScenarioA1SupportWeaponPossessionRules.FromBrokenHolder(false, true, true, true));
        Assert.False(ScenarioA1SupportWeaponPossessionRules.FromBrokenHolder(true, false, true, true));
        Assert.False(ScenarioA1SupportWeaponPossessionRules.FromBrokenHolder(true, true, true, false));
        Assert.Equal(0, ScenarioA1SupportWeaponPossessionRules.RecoveryMf("mph", true));
        Assert.Equal(1, ScenarioA1SupportWeaponPossessionRules.RecoveryMf("mph", false));
    }

    [Fact]
    public void ConfigurationUsePersistsIntoDefensiveFireAndBlocksReuse()
    {
        Assert.True(ScenarioA1SupportWeaponUseRules.SameUseWindow("mph", "dfph"));
        Assert.False(ScenarioA1SupportWeaponUseRules.SameUseWindow("pfph", "dfph"));
        Assert.NotNull(ScenarioA1SupportWeaponUseRules.FireAfterConfiguration("dfph", "asl:squad", ["mg"], ["mg"], false, ["mg"], false, false));
        Assert.Null(ScenarioA1SupportWeaponUseRules.FireAfterConfiguration("dfph", "asl:squad", ["mg"], ["mg"], false, ["lmg"], false, false));
        Assert.NotNull(ScenarioA1SupportWeaponUseRules.FireAfterConfiguration("dfph", "asl:squad", ["mg"], ["mg"], true, ["lmg"], false, false));
    }
}
