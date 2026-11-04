using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1BuildingFireTests
{
    private static readonly BuildingFireFacts Stone = new(BuildingFireKind.Stone, BuildingFireLocation.Ordinary,
        BuildingFireProfile.Standard, BuildingFireAttack.Direct, false);

    [Theory]
    [InlineData(BuildingFireKind.Stone, BuildingFireAttack.Direct, false, 3)]
    [InlineData(BuildingFireKind.Wood, BuildingFireAttack.Indirect, false, 2)]
    [InlineData(BuildingFireKind.Stone, BuildingFireAttack.Direct, true, 0)]
    [InlineData(BuildingFireKind.Wood, BuildingFireAttack.Indirect, true, 0)]
    [InlineData(BuildingFireKind.Stone, BuildingFireAttack.Residual, true, 3)]
    [InlineData(BuildingFireKind.Wood, BuildingFireAttack.Residual, true, 2)]
    public void OrdinaryBuildingsReturnOnlyTheirMaterialContribution(BuildingFireKind kind, BuildingFireAttack attack, bool bypass, int expected)
    {
        // B23.31 p. 136; A9.222 p. 63: Fire Lane uses the Residual classification too.
        var facts = Stone with
        {
            Building = kind,
            Attack = attack,
            Bypassing = bypass
        };
        AssertTem(expected, facts);
        AssertTem(expected, facts with
        {
            Firer = FactoryFirerPlace.Inside
        });
        AssertTem(expected, facts with
        {
            Firer = FactoryFirerPlace.Outside
        });
    }

    [Fact]
    public void UnknownFactsBlockOnlyMaterialBuildingBranches()
    {
        AssertTem(3, Stone with
        {
            Attack = null,
            Profile = null
        });
        AssertTem(3, Stone with
        {
            Attack = BuildingFireAttack.Residual,
            Bypassing = null
        });
        AssertTem(0, Stone with
        {
            Bypassing = true,
            Profile = null
        });
        AssertUnknown(Stone with
        {
            Bypassing = null
        });
        AssertUnknown(Stone with
        {
            Bypassing = null,
            Attack = null
        });
        AssertUnknown(Stone with
        {
            Bypassing = true,
            Attack = null
        });
        AssertUnknown(Stone with
        {
            Bypassing = true,
            Attack = BuildingFireAttack.Residual,
            Profile = null
        });
        var deluxe = ScenarioA1BuildingFireCalculator.BuildingTem(Stone with
        {
            Bypassing = true,
            Attack = BuildingFireAttack.Residual,
            Profile = BuildingFireProfile.Deluxe,
        });
        Assert.Null(deluxe.Tem);
        Assert.Equal(("J2.23", 602), (deluxe.Rule, deluxe.Page));
    }

    [Theory]
    [InlineData(FactoryFirerPlace.Inside, true, null, 1)]
    [InlineData(FactoryFirerPlace.Inside, false, null, 3)]
    [InlineData(FactoryFirerPlace.Rubble, null, null, 1)]
    [InlineData(FactoryFirerPlace.Rooftop, null, null, 1)]
    [InlineData(FactoryFirerPlace.Outside, null, true, 1)]
    [InlineData(FactoryFirerPlace.Outside, null, false, 3)]
    public void FactoryDirectFireDelegatesWithoutRequiringHindrance(FactoryFirerPlace firer, bool? trace, bool? rubble, int expected)
    {
        var facts = Stone with
        {
            Building = BuildingFireKind.StoneFactory,
            Firer = firer,
            TracedWithinDepiction = trace,
            OverAdjacentRubbleEnteringAcrossDepiction = rubble
        };
        AssertTem(expected, facts);
        AssertTem(expected == 3 ? 2 : expected, facts with
        {
            Building = BuildingFireKind.WoodFactory
        });
        AssertTem(0, facts with
        {
            Bypassing = true,
            Firer = null,
            TracedWithinDepiction = null
        });
    }

    [Fact]
    public void FactoryResidualNormalizesEntryBeforeCheckingDeluxeAndContradictions()
    {
        var facts = Stone with
        {
            Building = BuildingFireKind.StoneFactory,
            Attack = BuildingFireAttack.Residual,
            Bypassing = null
        };
        AssertTem(1, facts with
        {
            ResidualEntry = FactoryResidualEntry.BuildingHexside
        });
        AssertTem(3, facts with
        {
            ResidualEntry = FactoryResidualEntry.NonBuildingHexside
        });
        AssertTem(3, facts with
        {
            ResidualEntry = FactoryResidualEntry.Bypass
        });
        AssertTem(3, facts with
        {
            Bypassing = true
        });
        AssertTem(1, facts with
        {
            Profile = BuildingFireProfile.Deluxe,
            ResidualEntry = FactoryResidualEntry.BuildingHexside
        });
        AssertUnknown(facts);
        AssertUnknown(facts with
        {
            Profile = BuildingFireProfile.Deluxe,
            ResidualEntry = FactoryResidualEntry.Bypass
        });
        AssertUnknown(facts with
        {
            Bypassing = false,
            ResidualEntry = FactoryResidualEntry.Bypass
        });
        AssertUnknown(facts with
        {
            Bypassing = true,
            ResidualEntry = FactoryResidualEntry.BuildingHexside
        });
    }

    [Fact]
    public void UnsupportedAndMissingBuildingFactsNeverBecomeZeroTem()
    {
        var absent = ScenarioA1BuildingFireCalculator.BuildingTem(Stone with
        {
            Building = BuildingFireKind.None
        });
        Assert.Equal(BuildingFireDisposition.NotApplicable, absent.Disposition);
        Assert.Null(absent.Tem);
        AssertUnknown(Stone with
        {
            Building = null
        });
        AssertUnknown(Stone with
        {
            Location = BuildingFireLocation.UnsupportedSpecial
        });
        AssertUnknown(Stone with
        {
            Building = (BuildingFireKind)999
        });
        var factory = Stone with
        {
            Building = BuildingFireKind.StoneFactory,
            Attack = BuildingFireAttack.Indirect
        };
        AssertUnknown(factory);
        AssertUnknown(factory with
        {
            Roofless = true
        });
        AssertTem(3, factory with
        {
            Roofless = false
        });
        AssertUnknown(factory with
        {
            Attack = null
        });
    }

    [Fact]
    public void BuildingContractRequiresPublicContextAndMaterialPrivateAnchors()
    {
        var contract = ScenarioA1RuleContracts.Find("b23.31-building-fire-tem")!;
        var facts = JsonNode.Parse("""
            {"buildingKind":"stone","locationKind":"ordinary","profile":"standard","attackKind":"direct","roofless":false,
             "targetId":"target","bypassing":false}
            """)!.AsObject();
        var answer = ScenarioA1RuleContracts.Evaluate(contract, facts);
        Assert.Equal("building-tem", answer.Outcome);
        Assert.Contains(answer.Reasons, reason => reason.Text.Contains("contribution: 3", StringComparison.Ordinal));
        foreach (var field in contract.Facts.Where(field => !field.MayBeUnknown))
        {
            var missing = facts.DeepClone().AsObject();
            missing.Remove(field.Name);
            Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, missing).Outcome);
        }

        facts["targetId"] = null;
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["attackKind"] = "residual";
        Assert.Equal("building-tem", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["attackKind"] = "indirect-residual";
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        Assert.Empty(contract.Events);
        Assert.All(contract.Facts.Where(field => field.Disclosure == ContractDisclosure.Owner), field =>
        {
            var anchor = Assert.Single(contract.Facts, candidate => candidate.Name == field.Concerns);
            Assert.Equal(ContractFactType.Unit, anchor.Type);
            Assert.Equal(anchor.Name, anchor.Concerns);
        });
    }

    [Fact]
    public void LegacyBypassLaneStillIncludesResidualWhileNewTemRetainsBuilding()
    {
        Assert.True(ScenarioA1FireMapRules.UsesBypassLane(false, ScenarioA1FireCalculator.ResidualFire, "mph", true));
        Assert.False(ScenarioA1FireMapRules.UsesBypassLane(false, ScenarioA1FireCalculator.ResidualFire, "dfph", true));
        AssertTem(3, Stone with
        {
            Bypassing = true,
            Attack = BuildingFireAttack.Residual
        });
    }

    private static void AssertTem(int expected, BuildingFireFacts facts)
    {
        var result = ScenarioA1BuildingFireCalculator.BuildingTem(facts);
        Assert.Equal(BuildingFireDisposition.Evaluated, result.Disposition);
        Assert.Equal(expected, result.Tem);
    }

    private static void AssertUnknown(BuildingFireFacts facts)
    {
        var result = ScenarioA1BuildingFireCalculator.BuildingTem(facts);
        Assert.Equal(BuildingFireDisposition.NotEvaluated, result.Disposition);
        Assert.Null(result.Tem);
    }
}
