using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1CapturedEquipmentUseTests
{
    [Theory]
    [InlineData("finnish", "russian", "lmg", null, "1939-12", "captured-use")]
    [InlineData("finnish", "russian", "lmg", null, "1940-01", "exempt")]
    [InlineData("finnish", "russian", "hmg50", null, "not-applicable", "captured-use")]
    [InlineData("finnish", "russian", "mmg", null, "not-applicable", "exempt")]
    [InlineData("russian", "finnish", "hmg", "r", "not-applicable", "exempt")]
    [InlineData("russian", "finnish", "hmg", "none", "not-applicable", "captured-use")]
    [InlineData("russian", "finnish", "hmg", null, "not-applicable", "not-evaluated")]
    [InlineData("russian", "finnish", "hmg", "not-applicable", "not-applicable", "not-evaluated")]
    [InlineData("finnish", "russian", "lmg", null, "not-applicable", "not-evaluated")]
    [InlineData("finnish", "russian", "lmg", null, "1940-13", "not-evaluated")]
    public void MachineGunExemptionsAreDirectionalAndDateSpecific(string user, string weapon, string kind, string? mark, string date, string outcome)
    {
        var result = ScenarioA1CapturedEquipmentUseCalculator.Evaluate(new(user, true, null, weapon, kind, mark, date, true));
        Assert.Equal(outcome, result.Outcome);
        Assert.True(result.PhysicallyCaptured);
        Assert.Equal(result.CapturedUsePenalties, result.NightCapturedMgPenalty);
        Assert.Null(result.UseRefusal);
    }

    [Theory]
    [InlineData("1944-06", "elite", "german", "refused-use")]
    [InlineData("1944-07", "elite", "german", "exempt")]
    [InlineData("1945-12", "first-line", "german", "exempt")]
    [InlineData("1946-01", "elite", "german", "refused-use")]
    [InlineData("1944-07", "second-line", "german", "captured-use")]
    [InlineData("1944-07", "green", "finnish", "captured-use")]
    [InlineData("1944-07", "conscript", "finnish", "captured-use")]
    [InlineData("1944-07", "conscript", null, "captured-use")]
    [InlineData("1944-07", "elite", "finnish", "exempt")]
    [InlineData("1944-07", null, "german", "not-evaluated")]
    [InlineData("1944-07", null, "russian", "captured-use")]
    [InlineData("not-applicable", "elite", "german", "not-evaluated")]
    public void FinnishPskDatesAndPrintedClassApplyEvenToFinnishColor(string date, string? printed, string? weapon, string outcome)
    {
        var result = ScenarioA1CapturedEquipmentUseCalculator.Evaluate(new("finnish", true, printed, weapon, "psk", null, date, false));
        Assert.Equal(outcome, result.Outcome);
        Assert.False(result.PhysicallyCaptured);
        Assert.Equal(outcome == "refused-use", result.UseRefusal is not null);
        if (result.Outcome != "not-evaluated")
        {
            Assert.False(result.NightCapturedMgPenalty);
        }
    }

    [Theory]
    [InlineData("german", null, null, "mmg", "not-applicable")]
    [InlineData("russian", null, null, "psk", "not-applicable")]
    [InlineData("finnish", null, "german", "lmg", "not-applicable")]
    [InlineData("american", null, null, null, "not-applicable")]
    [InlineData(null, false, null, null, "not-applicable")]
    [InlineData("finnish", null, "russian", "mmg", "not-evaluated")]
    [InlineData("german", true, "finnish", "psk", "exempt")]
    public void OnlyMaterialUnknownsBlockAnAnswer(string? user, bool? personnel, string? weapon, string? kind, string outcome)
    {
        Assert.Equal(outcome, ScenarioA1CapturedEquipmentUseCalculator.Evaluate(new(user, personnel, null, weapon, kind, null, "not-applicable", null)).Outcome);
    }

    [Fact]
    public void LegacyCapturedByKeepsNullAndDoesNotAcquireAnExemption()
    {
        Assert.True(ScenarioA1FireEligibility.CapturedBy("russian", "finnish"));
        Assert.Null(ScenarioA1FireEligibility.CapturedBy("finnish", "finnish"));
        Assert.Null(ScenarioA1FireEligibility.CapturedBy(null, "finnish"));
        Assert.Null(ScenarioA1FireEligibility.CapturedBy("russian", null));
    }

    [Fact]
    public void RequiredPublicDateAndConditionalSentinelsAreValidatedByTheRegistry()
    {
        var contract = ScenarioA1RuleContracts.Find("a25.75-equipment-use")!;
        var facts = JsonSerializer.SerializeToNode(new
        {
            subjectId = "f",
            userNationality = "german",
            weaponNationality = "finnish",
            weaponKind = "psk",
            personnel = true
        })!.AsObject();
        Assert.Equal("not-evaluated", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["scenarioDate"] = "not-applicable";
        Assert.Equal("exempt", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["userNationality"] = "finnish";
        Assert.Equal("not-evaluated", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["scenarioDate"] = "1944-13";
        Assert.Equal("not-evaluated", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        facts["scenarioDate"] = "1944-07";
        facts["printedClass"] = "elite";
        Assert.Equal("exempt", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
    }

    [Fact]
    public void OwnerFactsUseTheirSubjectAndPrisonerContractsKeepPublicDisclosure()
    {
        foreach (var id in new[] { "a25.61-italian-deployment", "a25.63-capture-modifier", "a25.71-organization", "a25.75-equipment-use" })
        {
            var contract = ScenarioA1RuleContracts.Find(id)!;
            Assert.Equal("subjectId", contract.Facts.Single(fact => fact.Name == "subjectId").Concerns);
            Assert.All(contract.Facts.Where(fact => fact.Disclosure == ContractDisclosure.Owner), fact => Assert.Equal("subjectId", fact.Concerns));
        }

        foreach (var id in new[] { "a20.55-prisoner-escape", "a25.63-prisoner-escape" })
        {
            var contract = ScenarioA1RuleContracts.Find(id)!;
            Assert.All(contract.Facts, fact => Assert.Equal(ContractDisclosure.All, fact.Disclosure));
            var facts = JsonNode.Parse("""{"prisonerId":"p","nationality":"eritrean","captorNationality":"german","infantry":true,"guardBroken":true,"meleeInLocation":false} """)!.AsObject();
            Assert.Equal(id == "a20.55-prisoner-escape" ? "not-evaluated" : "refused-nationality", ScenarioA1RuleContracts.Evaluate(contract, facts).Outcome);
        }
    }
}
