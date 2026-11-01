using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class SupportWeaponScopeTests
{
    [Fact]
    public void FinalFireRetainsHalvedInherentFpAfterEarlierWeaponUse()
    {
        // A7.353 (PDF p. 56), A8.4 (p. 61): earlier First Fire does not consume the restored allowance; current full SW capability does.
        var restored = ScenarioA1SupportWeaponUseRules.ConfigurationFireUse("dfph", "asl:squad", ["A", "B"], ["B"], false, [], true, false,
            firstFireMarked: true, usedThisPhase: ["B"]);
        Assert.Null(restored.Reason);
        Assert.True(restored.RestoresInherent);
        var weaponMarked = ScenarioA1SupportWeaponUseRules.ConfigurationFireUse("dfph", "asl:squad", ["A", "B"], ["B"], false, [], true, false,
            usedThisPhase: ["B"], weaponFirstFireMarked: true);
        Assert.Null(weaponMarked.Reason);
        Assert.True(weaponMarked.RestoresInherent);
        var fullCapability = ScenarioA1SupportWeaponUseRules.ConfigurationFireUse("dfph", "asl:squad", ["A", "B"], ["B"], false, ["A"], true, false,
            firstFireMarked: true, usedThisPhase: ["B"]);
        Assert.NotNull(fullCapability.Reason);
        Assert.False(fullCapability.RestoresInherent);
        Assert.NotNull(ScenarioA1SupportWeaponUseRules.ConfigurationFireUse("dfph", "asl:squad", ["A", "B"], ["B"], false, ["B"], true, false,
            firstFireMarked: true, usedThisPhase: ["B"]).Reason);
        Assert.NotNull(ScenarioA1SupportWeaponUseRules.FireAfterConfiguration("dfph", "asl:squad", ["A", "B"], ["B"], false, [], true, false));
    }

    [Theory]
    [InlineData("asl:squad", 1, true)]
    [InlineData("asl:squad", 2, false)]
    [InlineData("asl:half-squad", 1, false)]
    [InlineData("asl:hero", 1, false)]
    public void LegacyWeaponOnlyFireDoesNotInventAnInherentUse(string kind, int weapons, bool expected)
    {
        // A7.351/.352 (PDF p. 56) and R15.11's existing hero reading: reuse the fire calculator when the optional field is absent.
        Assert.Equal(expected, ScenarioA1SupportWeaponUseRules.UsesInherentFp(null, kind, kind == "asl:hero", weapons));
    }

    private static SupportWeaponRecoveryFacts RecoveryFacts() => new("mph", true, true, true, false, true, false, false,
        true, true, false, false, false, false, false, false, false, false, false, 2, true, false, false, "open");

    private static SupportWeaponTransferFacts TransferFacts() => new("aph", "G1", "G2", "LMG", true, true, true,
        false, false, true, true, true, null, null, false, true, false, false, false, true, false);

    [Fact]
    public void RecoverySeparatesUnknownFactsFromMfAndRepeatRefusals()
    {
        // A4.44 (PDF p. 50): one MF, once per weapon per phase, and no Bypass recovery.
        var facts = RecoveryFacts();
        Assert.Equal(SupportWeaponReadinessStatus.Allowed, ScenarioA1SupportWeaponPossessionRules.RecoveryRefusal(facts).Status);
        foreach (var unknown in new[] { facts with { Terrain = null }, facts with { Terrain = "jungle" }, facts with { ConditionsKnown = false },
            facts with { HalfMfRemaining = null }, facts with { KnownWirePresent = true }, facts with { Aboard = true } })
        {
            Assert.Equal(SupportWeaponReadinessStatus.NotEvaluated, ScenarioA1SupportWeaponPossessionRules.RecoveryRefusal(unknown).Status);
        }
        foreach (var refused in new[] { facts with { HalfMfRemaining = 1 }, facts with { Attempted = true }, facts with { Bypassing = true },
            facts with { ArmedKnownEnemy = true }, facts with { ReactionPending = true } })
        {
            Assert.Equal(SupportWeaponReadinessStatus.Refused, ScenarioA1SupportWeaponPossessionRules.RecoveryRefusal(refused).Status);
        }
    }

    [Fact]
    public void TransferSeparatesUnknownFactsFromPhaseAndParticipantRefusals()
    {
        // A4.431/.44 (PDF p. 50): GO, unpinned, same Location; not after advance or in the recovery phase.
        var facts = TransferFacts();
        Assert.Equal(SupportWeaponReadinessStatus.Allowed, ScenarioA1SupportWeaponPossessionRules.TransferRefusal(facts).Status);
        foreach (var unknown in new[] { facts with { GiverGoodOrder = null }, facts with { RecipientGoodOrder = null },
            facts with { KnownWirePresent = true }, facts with { GiverVehicle = "truck" }, facts with { Phase = null } })
        {
            Assert.Equal(SupportWeaponReadinessStatus.NotEvaluated, ScenarioA1SupportWeaponPossessionRules.TransferRefusal(unknown).Status);
        }
        foreach (var refused in new[] { facts with { AdvanceStarted = true }, facts with { RecoveredThisPhase = true },
            facts with { RecipientPinned = true }, facts with { SameLocation = false }, facts with { Phase = "pfph" } })
        {
            Assert.Equal(SupportWeaponReadinessStatus.Refused, ScenarioA1SupportWeaponPossessionRules.TransferRefusal(refused).Status);
        }
    }

    [Fact]
    public void ConfigurationDerivesCapacityAndPermittedTypeFromDefinitions()
    {
        // A7.351/.352 (PDF p. 56), A9.8 (p. 65): the caller cannot supply an invented capacity or permission flag.
        var facts = new SupportWeaponConfigurationFacts("pfph", true, true, true, false, true, true, "asl:mg", false, 1,
            "asl:squad", "german", "medium", 3);
        Assert.Equal(SupportWeaponReadinessStatus.Allowed, ScenarioA1SupportWeaponConfigurationRules.Evaluate(facts).Status);
        Assert.Equal(SupportWeaponReadinessStatus.Refused, ScenarioA1SupportWeaponConfigurationRules.Evaluate(facts with
        {
            UnitKind = "asl:half-squad"
        }).Status);
        Assert.Equal(SupportWeaponReadinessStatus.Refused, ScenarioA1SupportWeaponConfigurationRules.Evaluate(facts with
        {
            WeaponSize = "light"
        }).Status);
        Assert.Equal(SupportWeaponReadinessStatus.NotEvaluated, ScenarioA1SupportWeaponConfigurationRules.Evaluate(facts with
        {
            UnitKind = null
        }).Status);
    }

    [Fact]
    public void AdvanceDropClosesWhenTheHolderAdvances()
    {
        // A4.43 (PDF p. 50), established R13.5 APh window retained by PR 96 review.
        Assert.Null(ScenarioA1SupportWeaponPossessionRules.DropRefusal("aph", true, true, true, true, false, false, false));
        Assert.NotNull(ScenarioA1SupportWeaponPossessionRules.DropRefusal("aph", true, true, true, true, false, false, true));
    }

    [Fact]
    public void RecoveryInPlaceKeepsGunShotsAndAddsOnlyItsMf()
    {
        // A4.44 (PDF p. 50), A8.1 (p. 59), C6.17 (p. 173).
        var paid = ScenarioA1SupportWeaponPossessionRules.RecoveryMovement("mph", false, true, 2, 3, 1);
        Assert.True(paid.OpensFireWindow);
        Assert.False(paid.ClearsShotsHere);
        Assert.Equal(4, paid.HalfMfInLocation);
        Assert.Equal(4, paid.Step);
        var newMover = ScenarioA1SupportWeaponPossessionRules.RecoveryMovement("mph", false, false, 2, 3, 1);
        Assert.True(newMover.ClearsShotsHere);
        Assert.Equal(2, newMover.HalfMfInLocation);
        var free = ScenarioA1SupportWeaponPossessionRules.RecoveryMovement("mph", true, true, 2, 3, 1);
        Assert.False(free.OpensFireWindow);
        Assert.False(free.ClearsShotsHere);
        Assert.Equal(2, free.HalfMfInLocation);
        Assert.Equal(3, free.Step);
    }

    [Fact]
    public void RepairKeepsTheKnownDefinitionWithoutNationalityReading()
    {
        // A9.72 (PDF p. 65): preserve the existing repair reading; an absent nationality is distinct from an unknown definition.
        var captured = ScenarioA1SupportWeaponRepairRules.CapturedForRepair(true, null, "german");
        Assert.False(captured);
        Assert.False(ScenarioA1SupportWeaponRepairRules.CapturedForRepair(true, "german", null));
        Assert.Null(ScenarioA1SupportWeaponRepairRules.CapturedForRepair(false, null, "german"));
        var facts = new SupportWeaponRepairFacts("rph", true, true, true, true, true, captured, 3, false, false, 0, "asl:squad", false);
        Assert.Equal(SupportWeaponReadinessStatus.Allowed, ScenarioA1SupportWeaponRepairRules.Evaluate(facts).Status);
    }

    [Fact]
    public void ConfigurationSharesInherentFireCapacityAndRejectsUnsupportedCarriage()
    {
        var facts = new SupportWeaponConfigurationFacts("pfph", true, true, true, false, true, true, "asl:mg", false, 1, "asl:squad", "german", "medium", 3);
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
        var facts = new SupportWeaponRepairFacts("rph", true, true, true, true, true, false, 3, false, false, 1, "asl:squad", false);
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
        // RSW.1, user-approved amendment to R13.5 on 2026-10-31; A4.43 (PDF p. 50), A11.21 (p. 73).
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
