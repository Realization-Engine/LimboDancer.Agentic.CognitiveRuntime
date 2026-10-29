using System.Text.Json.Nodes;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The C9 claim's wiring contract, <c>c9-mortar-shot</c> (ruling RC9.7): its verdicts, and what a viewer that may not know the mortar's facts is told.
/// </summary>
public sealed class ScenarioA1MortarContractTests
{
    private static RuleContract Contract => ScenarioA1RuleContracts.Find("c9-mortar-shot")!;

    private static JsonObject Facts() => new()
    {
        ["firerId"] = "g-squad",
        ["weapon"] = "asl:light-mortar",
        ["caliber"] = 50m,
        ["minimumRange"] = 2m,
        ["range"] = 2m,
        ["areaTargetType"] = true,
        ["namesVehicle"] = false,
        ["loneSmc"] = false,
        ["hero"] = false,
    };

    [Fact]
    public void AWeaponThatIsNotAMortarIsNotEvaluatedAndAHeroFiresALargerMortarAsAGun()
    {
        // The C9 claim's review, finding 2: an AT Gun is not C9's, whatever its other facts say.
        var gun = Facts();
        gun["weapon"] = "at";
        gun["areaTargetType"] = false;
        Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(Contract, gun).Outcome);

        // Finding 1 (A21.13, A15.23, p. 83): a lone hero may fire a 120mm mortar; a lone leader may not.
        var heavy = Facts();
        heavy["weapon"] = "mtr";
        heavy["caliber"] = 120m;
        heavy["minimumRange"] = 3m;
        heavy["range"] = 3m;
        heavy["loneSmc"] = true;
        var leader = ScenarioA1RuleContracts.Evaluate(Contract, heavy);
        Assert.Equal("refused", leader.Outcome);
        Assert.Equal(("A21.13", 88), (leader.Reasons[0].Rule, leader.Reasons[0].Page));
        heavy["hero"] = true;
        var hero = ScenarioA1RuleContracts.Evaluate(Contract, heavy);
        Assert.Equal("may-fire", hero.Outcome);
        Assert.Contains(hero.Reasons, reason => reason.Rule == "A15.23");
    }

    [Fact]
    public void TheShotMayBeMadeOrIsRefusedAndALoneSmcLosesItsMultipleRof()
    {
        var verdict = ScenarioA1RuleContracts.Evaluate(Contract, Facts());
        Assert.Equal("may-fire", verdict.Outcome);
        Assert.Contains(verdict.Reasons, reason => reason.Rule == "C2.24");

        var near = Facts();
        near["range"] = 1m;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(Contract, near).Outcome);

        var truck = Facts();
        truck["namesVehicle"] = true;
        Assert.Equal("refused", ScenarioA1RuleContracts.Evaluate(Contract, truck).Outcome);

        var lone = Facts();
        lone["loneSmc"] = true;
        var leader = ScenarioA1RuleContracts.Evaluate(Contract, lone);
        Assert.Equal("may-fire", leader.Outcome);
        Assert.Contains(leader.Reasons, reason => reason.Rule == "C9.2" && reason.Text.Contains("loses its Multiple ROF", StringComparison.Ordinal));

        // A21.13: the same lone SMC with an 81mm Gun mortar fires it as light, on A21.13's word.
        lone["weapon"] = "mtr";
        lone["caliber"] = 81m;
        lone["minimumRange"] = 3m;
        lone["range"] = 3m;
        Assert.Contains(ScenarioA1RuleContracts.Evaluate(Contract, lone).Reasons, reason => reason.Rule == "A21.13");
    }

    [Fact]
    public void EveryFactOfTheShotIsItsOwnersAndAnUnknownOneIsNotEvaluated()
    {
        // The mortar may be under a "?", its firer a concealed stack's SMC: the opponent's view leaves them unknown.
        Assert.All(Contract.Facts, fact => Assert.Equal((ContractDisclosure.Owner, "firerId"), (fact.Disclosure, fact.Concerns)));
        foreach (var name in new[] { "weapon", "caliber", "minimumRange", "range", "areaTargetType", "namesVehicle", "loneSmc", "hero" })
        {
            var facts = Facts();
            facts[name] = null;
            Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(Contract, facts).Outcome);
        }

        // The firer's identity is not needed; a number that is not a whole one an int holds is not read.
        var anonymous = Facts();
        anonymous["firerId"] = null;
        Assert.Equal("may-fire", ScenarioA1RuleContracts.Evaluate(Contract, anonymous).Outcome);
        foreach (var number in new JsonNode[] { 2.5m, -1m, 2147483648m })
        {
            var facts = Facts();
            facts["range"] = number;
            Assert.Equal(ContractVerdict.NotEvaluated, ScenarioA1RuleContracts.Evaluate(Contract, facts).Outcome);
        }
    }
}
