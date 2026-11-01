namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Owner-scoped adapters for the approved A9.8 delivery. Callers gather facts; the calculators decide readiness.</summary>
internal static class ScenarioA1SupportWeaponConfigurationContract
{
    [RuleContract]
    private static RuleContract Configuration() => new("a9.8-configuration", "Support Weapons: Configuration", [new("A9.8", 65)],
        [
            new("unitId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The acting unit") { Concerns = "unitId" },
            new("phase", ContractFactType.Text, true, ContractDisclosure.Owner, "Phase") { Concerns = "unitId" },
            new("phasing", ContractFactType.Flag, true, ContractDisclosure.Owner, "Phasing") { Concerns = "unitId" },
            new("active", ContractFactType.Flag, true, ContractDisclosure.Owner, "Active") { Concerns = "unitId" },
            new("goodOrder", ContractFactType.Flag, true, ContractDisclosure.Owner, "Good Order") { Concerns = "unitId" },
            new("temporarilyImmobilized", ContractFactType.Flag, true, ContractDisclosure.Owner, "Temporarily Immobilized") { Concerns = "unitId" },
            new("possessed", ContractFactType.Flag, true, ContractDisclosure.Owner, "Possessed") { Concerns = "unitId" },
            new("weaponActive", ContractFactType.Flag, true, ContractDisclosure.Owner, "Weapon Active") { Concerns = "unitId" },
            new("supportedConfiguration", ContractFactType.Flag, true, ContractDisclosure.Owner, "Supported Configuration") { Concerns = "unitId" },
            new("weaponUsed", ContractFactType.Flag, true, ContractDisclosure.Owner, "Weapon Used") { Concerns = "unitId" },
            new("usesThisPhase", ContractFactType.Number, true, ContractDisclosure.Owner, "Uses This Phase") { Concerns = "unitId" },
            new("capacity", ContractFactType.Number, true, ContractDisclosure.Owner, "Capacity") { Concerns = "unitId" },
            new("typePermitted", ContractFactType.Flag, true, ContractDisclosure.Owner, "Type Permitted") { Concerns = "unitId" },
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
            facts.Flag("supportedConfiguration") is null ||
            facts.Flag("weaponUsed") is null ||
            facts.Number("usesThisPhase") is null ||
            facts.Number("usesThisPhase") != decimal.Truncate(facts.Number("usesThisPhase")!.Value) ||
            facts.Number("usesThisPhase") < 0 || facts.Number("usesThisPhase") > int.MaxValue ||
            facts.Number("capacity") is null ||
            facts.Number("capacity") != decimal.Truncate(facts.Number("capacity")!.Value) ||
            facts.Number("capacity") < 0 || facts.Number("capacity") > int.MaxValue ||
            facts.Flag("typePermitted") is null ||
            facts.Flag("inherentFireUsed") is null ||
            facts.Flag("aboard") is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("Supply the acting side's known facts; integer quantities must be whole numbers.", "A9.8", 65)], []);
        }

        var input = new SupportWeaponConfigurationFacts(
            facts.Text("phase")!,
            facts.Flag("phasing")!.Value,
            facts.Flag("active")!.Value,
            facts.Flag("goodOrder")!.Value,
            facts.Flag("temporarilyImmobilized")!.Value,
            facts.Flag("possessed")!.Value,
            facts.Flag("weaponActive")!.Value,
            facts.Flag("supportedConfiguration")!.Value,
            facts.Flag("weaponUsed")!.Value,
            checked((int)facts.Number("usesThisPhase")!.Value),
            checked((int)facts.Number("capacity")!.Value),
            facts.Flag("typePermitted")!.Value,
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
