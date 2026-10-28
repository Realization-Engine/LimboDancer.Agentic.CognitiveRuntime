using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The registry test of the wiring contracts (the Rules-first plan, section 5; the proving slice, ruling RPS.13): every contract has a unique id and rules
/// that are rows of the rule inventory, every fact type round-trips through the JSON entry point, and facts that are missing, unknown, or of the wrong
/// type give "not-evaluated", never an exception.
/// </summary>
public sealed class ScenarioA1RuleContractTests
{
    private static readonly Lazy<string> Inventory = new(() =>
        File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "ASL", "docs", "ASL Rule Inventory A to E.md")));

    private static string RepositoryRoot()
    {
        var configured = Environment.GetEnvironmentVariable("ASL_REPO");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "ASL", "LimboDancer.Domains.Asl.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The repository root was not found.");
    }

    private static JsonNode? Sample(ContractFact fact) => fact.Type switch
    {
        ContractFactType.Flag => false,
        ContractFactType.Number => 75,
        ContractFactType.Choice => fact.Choices![0],
        ContractFactType.Hex => "bd04:H5:0",
        ContractFactType.Unit => "u-" + fact.Name,
        _ => "open-ground",
    };

    private static JsonObject Facts(RuleContract contract, string? leaveOut = null)
    {
        var facts = new JsonObject();
        foreach (var fact in contract.Facts.Where(fact => fact.Name != leaveOut))
        {
            facts[fact.Name] = Sample(fact);
        }

        return facts;
    }

    private static string Outcome(string verdictJson) => JsonDocument.Parse(verdictJson).RootElement.GetProperty("outcome").GetString()!;

    [Fact]
    public void EveryContractHasAUniqueIdAndRulesThatAreRowsOfTheInventory()
    {
        Assert.Equal(ScenarioA1RuleContracts.All.Count, ScenarioA1RuleContracts.All.Select(contract => contract.Id).Distinct().Count());
        foreach (var contract in ScenarioA1RuleContracts.All)
        {
            Assert.NotEmpty(contract.Rules);
            Assert.Contains(ContractVerdict.NotEvaluated, contract.Outcomes);
            Assert.Equal(contract.Facts.Count, contract.Facts.Select(fact => fact.Name).Distinct().Count());
            foreach (var rule in contract.Rules)
            {
                Assert.True(Inventory.Value.Contains($"\n| {rule.Rule} |", StringComparison.Ordinal), $"{contract.Id}: {rule.Rule} is not a row of the inventory");
            }

            // An owner's or an opponent's fact names the Unit fact whose side decides who may know it.
            foreach (var fact in contract.Facts.Where(fact => fact.Disclosure is ContractDisclosure.Owner or ContractDisclosure.Opponent))
            {
                Assert.Contains(contract.Facts, other => other.Name == fact.Concerns && other.Type == ContractFactType.Unit);
            }
        }
    }

    [Fact]
    public void EveryFactTypeRoundTripsThroughTheJsonEntryPoint()
    {
        foreach (var contract in ScenarioA1RuleContracts.All)
        {
            var verdict = ScenarioA1RuleContracts.Evaluate(contract.Id, Facts(contract).ToJsonString());
            Assert.Contains(Outcome(verdict), contract.Outcomes);
        }
    }

    [Fact]
    public void AMissingAnUnknownOrAMistypedFactGivesNotEvaluated()
    {
        foreach (var contract in ScenarioA1RuleContracts.All)
        {
            var required = contract.Facts.First(fact => !fact.MayBeUnknown);
            Assert.Equal(ContractVerdict.NotEvaluated, Outcome(ScenarioA1RuleContracts.Evaluate(contract.Id, Facts(contract, required.Name).ToJsonString())));

            var unknown = Facts(contract);
            unknown["no-such-fact"] = true;
            Assert.Equal(ContractVerdict.NotEvaluated, Outcome(ScenarioA1RuleContracts.Evaluate(contract.Id, unknown.ToJsonString())));

            var mistyped = Facts(contract);
            mistyped[required.Name] = required.Type == ContractFactType.Flag ? "yes" : true;
            Assert.Equal(ContractVerdict.NotEvaluated, Outcome(ScenarioA1RuleContracts.Evaluate(contract.Id, mistyped.ToJsonString())));
            Assert.Equal(ContractVerdict.NotEvaluated, Outcome(ScenarioA1RuleContracts.Evaluate(contract.Id, "[1, 2]")));
        }
    }

    [Fact]
    public void TheRallyContractGivesTheRallyDrsTargetAndRollsNothing()
    {
        var facts = new JsonObject
        {
            ["phase"] = "RPh",
            ["rallyingSide"] = "phasing",
            ["locationId"] = "bd01:E4:0",
            ["terrain"] = "woods",
            ["unitId"] = "ru-1",
            ["unitDefinition"] = "defender-squad",
            ["unitBroken"] = true,
            ["unitDisrupted"] = false,
            ["unitWounded"] = false,
            ["unitDesperationMorale"] = false,
            ["unitConcealed"] = false,
            ["unitAttemptedThisPlayerTurn"] = false,
            ["unitOtherActionThisPhase"] = false,
            ["leaderId"] = "ru-leader",
            ["leaderDefinition"] = "defender-leader",
            ["leaderBroken"] = false,
            ["leaderWounded"] = false,
            ["leaderConcealed"] = false,
            ["goodOrderLeaderInLocation"] = true,
            ["brokenLeaderInLocation"] = false,
            ["firstMmcRallyOfOwnPlayerTurn"] = false,
            ["knownEnemyInLos"] = false,
            ["captors"] = "none",
        };
        var verdict = ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find("a10.6-rally")!, facts);
        Assert.Equal("may-attempt", verdict.Outcome);
        var roll = Assert.Single(verdict.Rolls);
        Assert.Equal("rally", roll.Kind);
        Assert.NotNull(roll.Target);

        // A10.61 (p. 68): the woods' -1 lowers the Final DR, so the same unit in Open Ground needs a Rally DR one lower.
        facts["terrain"] = "open-ground";
        var open = ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find("a10.6-rally")!, facts);
        Assert.Equal(roll.Target - 1, Assert.Single(open.Rolls).Target);
    }

    [Fact]
    public void TheConcealmentContractReadsTheEnemyLosOnlyWhenNightAndTerrainLeaveItOpen()
    {
        var contract = ScenarioA1RuleContracts.Find("a12.14-advance-concealment")!;
        JsonObject Position(bool night, string terrain) => new()
        {
            ["advancerId"] = "g-squad",
            ["to"] = "bd04:H5:0",
            ["night"] = night,
            ["terrain"] = terrain
        };
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(contract, Position(false, "open-ground")).Outcome);
        Assert.Equal("keeps-concealment", ScenarioA1RuleContracts.Evaluate(contract, Position(true, "open-ground")).Outcome);
        Assert.Equal("keeps-concealment", ScenarioA1RuleContracts.Evaluate(contract, Position(false, "woods")).Outcome);
    }
}
