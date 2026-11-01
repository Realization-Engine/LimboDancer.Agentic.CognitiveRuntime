namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Owner-scoped adapters for the approved A4.431 delivery. Callers gather facts; the calculators decide readiness.</summary>
internal static class ScenarioA1SupportWeaponPossessionContract
{
    [RuleContract]
    private static RuleContract Recovery() => new("a4.44-recovery", "Support Weapons: Recovery", [new("A4.44", 50)],
        [
            new("unitId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The acting unit") { Concerns = "unitId" },
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = ScenarioA1RuleContracts.Phases },
            new("phasing", ContractFactType.Flag, false, ContractDisclosure.All, "The acting side is phasing"),
            new("active", ContractFactType.Flag, true, ContractDisclosure.Owner, "Active") { Concerns = "unitId" },
            new("goodOrder", ContractFactType.Flag, true, ContractDisclosure.Owner, "Good Order") { Concerns = "unitId" },
            new("pinned", ContractFactType.Flag, true, ContractDisclosure.Owner, "Pinned") { Concerns = "unitId" },
            new("infantry", ContractFactType.Flag, true, ContractDisclosure.Owner, "Infantry") { Concerns = "unitId" },
            new("unarmed", ContractFactType.Flag, true, ContractDisclosure.Owner, "Unarmed") { Concerns = "unitId" },
            new("bypassing", ContractFactType.Flag, true, ContractDisclosure.Owner, "Bypassing") { Concerns = "unitId" },
            new("weaponActive", ContractFactType.Flag, true, ContractDisclosure.Owner, "Weapon Active") { Concerns = "unitId" },
            new("sameLocation", ContractFactType.Flag, true, ContractDisclosure.Owner, "Same Location") { Concerns = "unitId" },
            new("held", ContractFactType.Flag, true, ContractDisclosure.Owner, "Held") { Concerns = "unitId" },
            new("friendlyHolder", ContractFactType.Flag, true, ContractDisclosure.Owner, "Friendly Holder") { Concerns = "unitId" },
            new("brokenHolder", ContractFactType.Flag, true, ContractDisclosure.Owner, "Broken Holder") { Concerns = "unitId" },
            new("recoveringSmc", ContractFactType.Flag, true, ContractDisclosure.Owner, "Recovering Smc") { Concerns = "unitId" },
            new("armedKnownEnemy", ContractFactType.Flag, true, ContractDisclosure.Owner, "Armed Known Enemy") { Concerns = "unitId" },
            new("attempted", ContractFactType.Flag, true, ContractDisclosure.Owner, "Attempted") { Concerns = "unitId" },
            new("rallyActionTaken", ContractFactType.Flag, true, ContractDisclosure.Owner, "Rally Action Taken") { Concerns = "unitId" },
            new("movementEnded", ContractFactType.Flag, true, ContractDisclosure.Owner, "Movement Ended") { Concerns = "unitId" },
            new("temporarilyImmobilized", ContractFactType.Flag, true, ContractDisclosure.Owner, "Temporarily Immobilized") { Concerns = "unitId" },
            new("halfMfRemaining", ContractFactType.Number, true, ContractDisclosure.Owner, "Half Mf Remaining") { Concerns = "unitId" },
            new("rallyRecoveryWindowOpen", ContractFactType.Flag, true, ContractDisclosure.Owner, "Rally Recovery Window Open") { Concerns = "unitId" },
            new("reactionPending", ContractFactType.Flag, true, ContractDisclosure.Owner, "Reaction Pending") { Concerns = "unitId" },
            new("anotherMoverActive", ContractFactType.Flag, true, ContractDisclosure.Owner, "Another Mover Active") { Concerns = "unitId" },
            new("terrain", ContractFactType.Text, true, ContractDisclosure.Owner, "Terrain") { Concerns = "unitId" },
            new("aboard", ContractFactType.Flag, true, ContractDisclosure.Owner, "Aboard") { Concerns = "unitId" },
            new("conditionsKnown", ContractFactType.Flag, true, ContractDisclosure.Owner, "Conditions Known") { Concerns = "unitId" },
            new("knownWirePresent", ContractFactType.Flag, true, ContractDisclosure.Owner, "Known Wire Present") { Concerns = "unitId" },
        ],
        ["allowed", "refused", ContractVerdict.NotEvaluated], [], new("RPh/MPh", "1.14B/3.32A"))
    {
        Decide = RecoveryVerdict,
    };

    private static ContractVerdict RecoveryVerdict(ContractFacts facts)
    {
        if (facts.Text("unitId") is null ||
            facts.Text("phase") is null ||
            facts.Flag("phasing") is null ||
            facts.Flag("active") is null ||
            facts.Flag("goodOrder") is null ||
            facts.Flag("pinned") is null ||
            facts.Flag("infantry") is null ||
            facts.Flag("unarmed") is null ||
            facts.Flag("bypassing") is null ||
            facts.Flag("weaponActive") is null ||
            facts.Flag("sameLocation") is null ||
            facts.Flag("held") is null ||
            facts.Flag("friendlyHolder") is null ||
            facts.Flag("brokenHolder") is null ||
            facts.Flag("recoveringSmc") is null ||
            facts.Flag("armedKnownEnemy") is null ||
            facts.Flag("attempted") is null ||
            facts.Flag("rallyActionTaken") is null ||
            facts.Flag("movementEnded") is null ||
            facts.Flag("temporarilyImmobilized") is null ||
            facts.Number("halfMfRemaining") is null ||
            facts.Number("halfMfRemaining") != decimal.Truncate(facts.Number("halfMfRemaining")!.Value) ||
            facts.Number("halfMfRemaining") < 0 || facts.Number("halfMfRemaining") > int.MaxValue ||
            facts.Flag("rallyRecoveryWindowOpen") is null ||
            facts.Flag("reactionPending") is null ||
            facts.Flag("anotherMoverActive") is null ||
            facts.Text("terrain") is null ||
            facts.Flag("aboard") is null ||
            facts.Flag("conditionsKnown") is null ||
            facts.Flag("knownWirePresent") is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("Supply the acting side's known facts; integer quantities must be whole numbers.", "A4.44", 50)], []);
        }

        var input = new SupportWeaponRecoveryFacts(
            facts.Text("phase")!.ToLowerInvariant(),
            facts.Flag("phasing")!.Value,
            facts.Flag("active")!.Value,
            facts.Flag("goodOrder")!.Value,
            facts.Flag("pinned")!.Value,
            facts.Flag("infantry")!.Value,
            facts.Flag("unarmed")!.Value,
            facts.Flag("bypassing")!.Value,
            facts.Flag("weaponActive")!.Value,
            facts.Flag("sameLocation")!.Value,
            facts.Flag("held")!.Value,
            facts.Flag("friendlyHolder")!.Value,
            facts.Flag("brokenHolder")!.Value,
            facts.Flag("recoveringSmc")!.Value,
            facts.Flag("armedKnownEnemy")!.Value,
            facts.Flag("attempted")!.Value,
            facts.Flag("rallyActionTaken")!.Value,
            facts.Flag("movementEnded")!.Value,
            facts.Flag("temporarilyImmobilized")!.Value,
            checked((int)facts.Number("halfMfRemaining")!.Value),
            facts.Flag("rallyRecoveryWindowOpen")!.Value,
            facts.Flag("reactionPending")!.Value,
            facts.Flag("anotherMoverActive")!.Value,
            facts.Text("terrain")!,
            facts.Flag("aboard")!.Value,
            facts.Flag("conditionsKnown")!.Value,
            facts.Flag("knownWirePresent")!.Value);
        var readiness = ScenarioA1SupportWeaponPossessionRules.RecoveryRefusal(input);
        var outcome = readiness.Status switch
        {
            SupportWeaponReadinessStatus.Allowed => "allowed",
            SupportWeaponReadinessStatus.Refused => "refused",
            _ => ContractVerdict.NotEvaluated,
        };
        return new(outcome, readiness.Reason is null ? [] : [new(readiness.Reason, "A4.44", 50)], []);
    }

    [RuleContract]
    private static RuleContract Transfer() => new("a4.431-transfer", "Support Weapons: Transfer", [new("A4.431", 50)],
        [
            new("unitId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The acting unit") { Concerns = "unitId" },
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = ScenarioA1RuleContracts.Phases },
            new("giver", ContractFactType.Unit, true, ContractDisclosure.Owner, "Giver") { Concerns = "giver" },
            new("recipient", ContractFactType.Unit, true, ContractDisclosure.Owner, "Recipient") { Concerns = "recipient" },
            new("weapon", ContractFactType.Text, true, ContractDisclosure.Owner, "Weapon") { Concerns = "unitId" },
            new("differentUnits", ContractFactType.Flag, true, ContractDisclosure.Owner, "Different Units") { Concerns = "unitId" },
            new("giverGoodOrder", ContractFactType.Flag, true, ContractDisclosure.Owner, "Giver Good Order") { Concerns = "giver" },
            new("recipientGoodOrder", ContractFactType.Flag, true, ContractDisclosure.Owner, "Recipient Good Order") { Concerns = "recipient" },
            new("giverPinned", ContractFactType.Flag, true, ContractDisclosure.Owner, "Giver Pinned") { Concerns = "giver" },
            new("recipientPinned", ContractFactType.Flag, true, ContractDisclosure.Owner, "Recipient Pinned") { Concerns = "recipient" },
            new("sameSide", ContractFactType.Flag, true, ContractDisclosure.Owner, "Same Side") { Concerns = "unitId" },
            new("sameLocation", ContractFactType.Flag, true, ContractDisclosure.Owner, "Same Location") { Concerns = "unitId" },
            new("bothInfantry", ContractFactType.Flag, true, ContractDisclosure.Owner, "Both Infantry") { Concerns = "unitId" },
            new("giverVehicle", ContractFactType.Text, true, ContractDisclosure.Owner, "Giver Vehicle (none when not aboard)") { Concerns = "giver" },
            new("recipientVehicle", ContractFactType.Text, true, ContractDisclosure.Owner, "Recipient Vehicle (none when not aboard)") { Concerns = "recipient" },
            new("recipientUnarmed", ContractFactType.Flag, true, ContractDisclosure.Owner, "Recipient Unarmed") { Concerns = "recipient" },
            new("possessed", ContractFactType.Flag, true, ContractDisclosure.Owner, "Possessed") { Concerns = "unitId" },
            new("recoveredThisPhase", ContractFactType.Flag, true, ContractDisclosure.Owner, "Recovered This Phase") { Concerns = "unitId" },
            new("giverRallyAction", ContractFactType.Flag, true, ContractDisclosure.Owner, "Giver Rally Action") { Concerns = "giver" },
            new("recipientRallyAction", ContractFactType.Flag, true, ContractDisclosure.Owner, "Recipient Rally Action") { Concerns = "recipient" },
            new("phasing", ContractFactType.Flag, false, ContractDisclosure.All, "The acting side is phasing"),
            new("advanceStarted", ContractFactType.Flag, true, ContractDisclosure.Owner, "Advance Started") { Concerns = "unitId" },
            new("vehicleInMotion", ContractFactType.Flag, true, ContractDisclosure.Owner, "Vehicle In Motion") { Concerns = "unitId" },
            new("knownWirePresent", ContractFactType.Flag, true, ContractDisclosure.Owner, "Known Wire Present") { Concerns = "unitId" },
        ],
        ["allowed", "refused", ContractVerdict.NotEvaluated], [], new("RPh/APh", "1.22B/7.11A"))
    {
        Decide = TransferVerdict,
    };

    private static ContractVerdict TransferVerdict(ContractFacts facts)
    {
        if (facts.Text("unitId") is null ||
            facts.Text("phase") is null ||
            facts.Text("giver") is null ||
            facts.Text("recipient") is null ||
            facts.Text("weapon") is null ||
            facts.Flag("differentUnits") is null ||
            facts.Flag("giverGoodOrder") is null ||
            facts.Flag("recipientGoodOrder") is null ||
            facts.Flag("giverPinned") is null ||
            facts.Flag("recipientPinned") is null ||
            facts.Flag("sameSide") is null ||
            facts.Flag("sameLocation") is null ||
            facts.Flag("bothInfantry") is null ||
            facts.Text("giverVehicle") is null ||
            facts.Text("recipientVehicle") is null ||
            facts.Flag("recipientUnarmed") is null ||
            facts.Flag("possessed") is null ||
            facts.Flag("recoveredThisPhase") is null ||
            facts.Flag("giverRallyAction") is null ||
            facts.Flag("recipientRallyAction") is null ||
            facts.Flag("phasing") is null ||
            facts.Flag("advanceStarted") is null ||
            facts.Flag("vehicleInMotion") is null ||
            facts.Flag("knownWirePresent") is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("Supply the acting side's known facts; integer quantities must be whole numbers.", "A4.431", 50)], []);
        }

        var input = new SupportWeaponTransferFacts(
            facts.Text("phase")!.ToLowerInvariant(),
            facts.Text("giver")!,
            facts.Text("recipient")!,
            facts.Text("weapon")!,
            facts.Flag("differentUnits")!.Value,
            facts.Flag("giverGoodOrder")!.Value,
            facts.Flag("recipientGoodOrder")!.Value,
            facts.Flag("giverPinned")!.Value,
            facts.Flag("recipientPinned")!.Value,
            facts.Flag("sameSide")!.Value,
            facts.Flag("sameLocation")!.Value,
            facts.Flag("bothInfantry")!.Value,
            facts.Text("giverVehicle") == "none" ? null : facts.Text("giverVehicle"),
            facts.Text("recipientVehicle") == "none" ? null : facts.Text("recipientVehicle"),
            facts.Flag("recipientUnarmed")!.Value,
            facts.Flag("possessed")!.Value,
            facts.Flag("recoveredThisPhase")!.Value,
            facts.Flag("giverRallyAction")!.Value,
            facts.Flag("recipientRallyAction")!.Value,
            facts.Flag("phasing")!.Value,
            facts.Flag("advanceStarted")!.Value,
            facts.Flag("vehicleInMotion")!.Value,
            facts.Flag("knownWirePresent")!.Value);
        var readiness = ScenarioA1SupportWeaponPossessionRules.TransferRefusal(input);
        var outcome = readiness.Status switch
        {
            SupportWeaponReadinessStatus.Allowed => "allowed",
            SupportWeaponReadinessStatus.Refused => "refused",
            _ => ContractVerdict.NotEvaluated,
        };
        return new(outcome, readiness.Reason is null ? [] : [new(readiness.Reason, "A4.431", 50)], []);
    }
}
