namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Owner-scoped retained-load contract; destination and withdrawal eligibility are evaluated separately.</summary>
internal static class ScenarioA1WithdrawalEquipmentContract
{
    [RuleContract]
    private static RuleContract Load() => new("a11.21-retained-load", "Withdrawal equipment load", [new("A11.21", 73)],
        [
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The withdrawal phase") { Choices = ["CCPh"] },
            new("unitId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The withdrawing unit") { Concerns = "unitId" },
            new("smc", ContractFactType.Flag, true, ContractDisclosure.Owner, "The unit is a SMC") { Concerns = "unitId" },
            new("wounded", ContractFactType.Flag, true, ContractDisclosure.Owner, "The unit is wounded") { Concerns = "unitId" },
            new("cx", ContractFactType.Flag, true, ContractDisclosure.Owner, "The unit is CX") { Concerns = "unitId" },
            new("retainedPp", ContractFactType.Number, true, ContractDisclosure.Owner, "Total retained PP after selected drops") { Concerns = "unitId" },
        ],
        ["allowed", "refused", ContractVerdict.NotEvaluated], [], new("CCPh", "8.24B"))
    {
        Decide = Verdict,
    };

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        if (facts.Text("unitId") is not { } unitId || facts.Flag("smc") is not { } smc || facts.Flag("wounded") is not { } wounded
            || facts.Flag("cx") is not { } cx || facts.Number("retainedPp") is not { } pp || pp < 0 || pp > int.MaxValue || pp != decimal.Truncate(pp))
        {
            return new(ContractVerdict.NotEvaluated, [], []);
        }
        var load = ScenarioA1WithdrawalEquipmentRules.Load(unitId, smc, wounded, cx, [checked((int)pp)]);
        return new(load.Readiness.Status == SupportWeaponReadinessStatus.Allowed ? "allowed" : "refused",
            load.Readiness.Reason is null ? [] : [new(load.Readiness.Reason, "A11.21", 73)], []);
    }
}
