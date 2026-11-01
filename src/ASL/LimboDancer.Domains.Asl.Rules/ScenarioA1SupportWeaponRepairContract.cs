namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Owner-scoped adapters for the approved A9.72 delivery. Callers gather facts; the calculators decide readiness.</summary>
internal static class ScenarioA1SupportWeaponRepairContract
{
    [RuleContract]
    private static RuleContract Repair() => new("a9.72-repair", "Support Weapons: Repair", [new("A9.72", 65)],
        [
            new("unitId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The acting unit") { Concerns = "unitId" },
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = ScenarioA1RuleContracts.Phases },
            new("active", ContractFactType.Flag, true, ContractDisclosure.Owner, "Active") { Concerns = "unitId" },
            new("goodOrder", ContractFactType.Flag, true, ContractDisclosure.Owner, "Good Order") { Concerns = "unitId" },
            new("possessed", ContractFactType.Flag, true, ContractDisclosure.Owner, "Possessed") { Concerns = "unitId" },
            new("weaponActive", ContractFactType.Flag, true, ContractDisclosure.Owner, "Weapon Active") { Concerns = "unitId" },
            new("malfunctioned", ContractFactType.Flag, true, ContractDisclosure.Owner, "Malfunctioned") { Concerns = "unitId" },
            new("captured", ContractFactType.Flag, true, ContractDisclosure.Owner, "Captured") { Concerns = "unitId" },
            new("repairNumber", ContractFactType.Number, true, ContractDisclosure.Owner, "Repair Number") { Concerns = "unitId" },
            new("otherRallyAction", ContractFactType.Flag, true, ContractDisclosure.Owner, "Other Rally Action") { Concerns = "unitId" },
            new("attemptedThisPlayerTurn", ContractFactType.Flag, true, ContractDisclosure.Owner, "Attempted This Player Turn") { Concerns = "unitId" },
            new("attemptsThisPhase", ContractFactType.Number, true, ContractDisclosure.Owner, "Attempts This Phase") { Concerns = "unitId" },
            new("unitKind", ContractFactType.Text, true, ContractDisclosure.Owner, "Unit Kind") { Concerns = "unitId" },
            new("gun", ContractFactType.Flag, true, ContractDisclosure.Owner, "Gun") { Concerns = "unitId" },
            new("ralliedThisPlayerTurn", ContractFactType.Flag, true, ContractDisclosure.Owner, "Rallied This Player Turn") { Concerns = "unitId" },
        ],
        ["allowed", "refused", ContractVerdict.NotEvaluated], [], new("RPh", "1.22B"))
    {
        Decide = RepairVerdict,
    };

    private static ContractVerdict RepairVerdict(ContractFacts facts)
    {
        if (facts.Text("unitId") is null ||
            facts.Text("phase") is null ||
            facts.Flag("active") is null ||
            facts.Flag("goodOrder") is null ||
            facts.Flag("possessed") is null ||
            facts.Flag("weaponActive") is null ||
            facts.Flag("malfunctioned") is null ||
            facts.Flag("captured") is null ||
            facts.Number("repairNumber") is null ||
            facts.Number("repairNumber") != decimal.Truncate(facts.Number("repairNumber")!.Value) ||
            facts.Number("repairNumber") < 0 || facts.Number("repairNumber") > int.MaxValue ||
            facts.Flag("otherRallyAction") is null ||
            facts.Flag("attemptedThisPlayerTurn") is null ||
            facts.Number("attemptsThisPhase") is null ||
            facts.Number("attemptsThisPhase") != decimal.Truncate(facts.Number("attemptsThisPhase")!.Value) ||
            facts.Number("attemptsThisPhase") < 0 || facts.Number("attemptsThisPhase") > int.MaxValue ||
            facts.Text("unitKind") is null ||
            facts.Flag("gun") is null ||
            facts.Flag("ralliedThisPlayerTurn") is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("Supply the acting side's known facts; integer quantities must be whole numbers.", "A9.72", 65)], []);
        }

        var input = new SupportWeaponRepairFacts(
            facts.Text("phase")!.ToLowerInvariant(),
            facts.Flag("active")!.Value,
            facts.Flag("goodOrder")!.Value,
            facts.Flag("possessed")!.Value,
            facts.Flag("weaponActive")!.Value,
            facts.Flag("malfunctioned")!.Value,
            facts.Flag("captured")!.Value,
            checked((int)facts.Number("repairNumber")!.Value),
            facts.Flag("otherRallyAction")!.Value,
            facts.Flag("attemptedThisPlayerTurn")!.Value,
            checked((int)facts.Number("attemptsThisPhase")!.Value),
            facts.Text("unitKind")!,
            facts.Flag("gun")!.Value,
            facts.Flag("ralliedThisPlayerTurn")!.Value);
        var readiness = ScenarioA1SupportWeaponRepairRules.Evaluate(input);
        var outcome = readiness.Status switch
        {
            SupportWeaponReadinessStatus.Allowed => "allowed",
            SupportWeaponReadinessStatus.Refused => "refused",
            _ => ContractVerdict.NotEvaluated,
        };
        return new(outcome, readiness.Reason is null ? [] : [new(readiness.Reason, "A9.72", 65)], []);
    }
}
