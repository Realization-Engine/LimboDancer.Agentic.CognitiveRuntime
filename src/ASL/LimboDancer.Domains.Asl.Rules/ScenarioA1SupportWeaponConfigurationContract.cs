namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Owner-scoped adapters for the approved A9.8 delivery. Callers gather facts; the calculators decide readiness.</summary>
internal static class ScenarioA1SupportWeaponConfigurationContract
{
    [RuleContract]
    private static RuleContract Configuration() => new("a9.8-configuration", "Support Weapons: Configuration", [new("A9.8", 65)],
        [
            new("unitId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The acting unit") { Concerns = "unitId" },
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = ScenarioA1RuleContracts.Phases },
            new("phasing", ContractFactType.Flag, false, ContractDisclosure.All, "The acting side is phasing"),
            new("active", ContractFactType.Flag, true, ContractDisclosure.Owner, "Active") { Concerns = "unitId" },
            new("goodOrder", ContractFactType.Flag, true, ContractDisclosure.Owner, "Good Order") { Concerns = "unitId" },
            new("temporarilyImmobilized", ContractFactType.Flag, true, ContractDisclosure.Owner, "Temporarily Immobilized") { Concerns = "unitId" },
            new("possessed", ContractFactType.Flag, true, ContractDisclosure.Owner, "Possessed") { Concerns = "unitId" },
            new("weaponActive", ContractFactType.Flag, true, ContractDisclosure.Owner, "Weapon Active") { Concerns = "unitId" },
            new("weaponKind", ContractFactType.Text, true, ContractDisclosure.Owner, "Weapon Kind") { Concerns = "unitId" },
            new("weaponUsed", ContractFactType.Flag, true, ContractDisclosure.Owner, "Weapon Used") { Concerns = "unitId" },
            new("usesThisPhase", ContractFactType.Number, true, ContractDisclosure.Owner, "Uses This Phase") { Concerns = "unitId" },
            new("unitKind", ContractFactType.Text, true, ContractDisclosure.Owner, "Unit Kind") { Concerns = "unitId" },
            new("weaponNationality", ContractFactType.Text, true, ContractDisclosure.Owner, "Weapon Nationality") { Concerns = "unitId" },
            new("weaponSize", ContractFactType.Text, true, ContractDisclosure.Owner, "Weapon Size") { Concerns = "unitId" },
            new("printedPortage", ContractFactType.Number, true, ContractDisclosure.Owner, "Printed Portage") { Concerns = "unitId" },
            new("inherentFireUsed", ContractFactType.Flag, true, ContractDisclosure.Owner, "Inherent Fire Used") { Concerns = "unitId" },
            new("aboard", ContractFactType.Flag, true, ContractDisclosure.Owner, "Aboard") { Concerns = "unitId" },
        ],
        ["allowed", "refused", ContractVerdict.NotEvaluated], [], new("PFPh/DFPh", "2.22A/4.21D"))
    {
        Decide = ConfigurationVerdict,
    };

    private static ContractVerdict ConfigurationVerdict(ContractFacts facts)
    {
        if (facts.Text("unitId") is null ||
            facts.Text("phase") is null ||
            facts.Flag("phasing") is null ||
            facts.Flag("active") is null ||
            facts.Flag("goodOrder") is null ||
            facts.Flag("temporarilyImmobilized") is null ||
            facts.Flag("possessed") is null ||
            facts.Flag("weaponActive") is null ||
            facts.Text("weaponKind") is null ||
            facts.Flag("weaponUsed") is null ||
            facts.Number("usesThisPhase") is null ||
            facts.Number("usesThisPhase") != decimal.Truncate(facts.Number("usesThisPhase")!.Value) ||
            facts.Number("usesThisPhase") < 0 || facts.Number("usesThisPhase") > int.MaxValue ||
            facts.Text("unitKind") is null ||
            facts.Text("weaponNationality") is null ||
            facts.Text("weaponSize") is null ||
            facts.Number("printedPortage") is null ||
            facts.Number("printedPortage") != decimal.Truncate(facts.Number("printedPortage")!.Value) ||
            facts.Number("printedPortage") < 0 || facts.Number("printedPortage") > int.MaxValue ||
            facts.Flag("inherentFireUsed") is null ||
            facts.Flag("aboard") is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("Supply the acting side's known facts; integer quantities must be whole numbers.", "A9.8", 65)], []);
        }

        var input = new SupportWeaponConfigurationFacts(
            facts.Text("phase")!.ToLowerInvariant(),
            facts.Flag("phasing")!.Value,
            facts.Flag("active")!.Value,
            facts.Flag("goodOrder")!.Value,
            facts.Flag("temporarilyImmobilized")!.Value,
            facts.Flag("possessed")!.Value,
            facts.Flag("weaponActive")!.Value,
            facts.Text("weaponKind")!,
            facts.Flag("weaponUsed")!.Value,
            checked((int)facts.Number("usesThisPhase")!.Value),
            facts.Text("unitKind")!,
            facts.Text("weaponNationality")!,
            facts.Text("weaponSize")!,
            checked((int)facts.Number("printedPortage")!.Value),
            facts.Flag("inherentFireUsed")!.Value,
            facts.Flag("aboard")!.Value);
        var readiness = ScenarioA1SupportWeaponConfigurationRules.Evaluate(input);
        var outcome = readiness.Status switch
        {
            SupportWeaponReadinessStatus.Allowed => "allowed",
            SupportWeaponReadinessStatus.Refused => "refused",
            _ => ContractVerdict.NotEvaluated,
        };
        return new(outcome, readiness.Reason is null ? [] : [new(readiness.Reason, "A9.8", 65)], []);
    }
}
