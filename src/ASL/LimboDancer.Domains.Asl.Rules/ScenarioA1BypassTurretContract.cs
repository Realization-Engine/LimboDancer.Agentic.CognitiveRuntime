using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Shared bounded fact adapters, with no map or hidden-definition reads.</summary>
internal static class ScenarioA1BypassTurretContract
{
    internal static ContractFact Flag(string name, bool owner = false) => Fact(name, ContractFactType.Flag, owner);
    internal static ContractFact Fact(string name, ContractFactType type, bool owner = false) =>
        new(name, type, true, owner ? ContractDisclosure.Owner : ContractDisclosure.All,
            owner ? "Owner-only " + name : "Announced or hypothetical " + name)
        {
            Concerns = owner ? "vehicleId" : null
        };
    internal static ContractFact Choice<T>(string name, bool owner = false) where T : struct, Enum => Fact(name, ContractFactType.Choice, owner) with { Choices = Enum.GetNames<T>() };
    internal static ContractFact Anchor() => Fact("vehicleId", ContractFactType.Unit, true);
    internal static IEnumerable<ContractFact> HexFields(string prefix) => [Fact(prefix + "Q", ContractFactType.Number), Fact(prefix + "R", ContractFactType.Number)];
    internal static IEnumerable<ContractFact> FrameFields(string prefix) => [.. HexFields(prefix + "Obstacle"), .. HexFields(prefix + "Across"), .. HexFields(prefix + "Front")];
    internal static T? EnumFact<T>(ContractFacts facts, string name, bool owner = false) where T : struct, Enum =>
        owner && facts.Text("vehicleId") is null ? null : Enum.TryParse<T>(facts.Text(name), out var value) && Enum.IsDefined(value) ? value : null;
    internal static bool? OwnerFlag(ContractFacts facts, string name) => facts.Text("vehicleId") is null ? null : facts.Flag(name);
    internal static BypassHex? Hex(ContractFacts facts, string prefix) => facts.Number(prefix + "Q") is { } q && facts.Number(prefix + "R") is { } r ? new((int)q, (int)r) : null;
    internal static BypassFrameFacts? Frame(ContractFacts facts, string prefix)
    {
        var a = Hex(facts, prefix + "Obstacle");
        var b = Hex(facts, prefix + "Across");
        var f = Hex(facts, prefix + "Front");
        return a is null && b is null && f is null ? null : new(a, b, f);
    }
    internal static string? BadNumbers(ContractFacts facts, IEnumerable<ContractFact> fields)
    {
        foreach (var field in fields)
        {
            if (field.Type == ContractFactType.Number && facts.Number(field.Name) is { } number
                && (number != decimal.Truncate(number) || number < -ScenarioA1BypassTurretCalculator.CoordinateLimit || number > ScenarioA1BypassTurretCalculator.CoordinateLimit))
            {
                return "A supplied number is nonintegral or outside the supported bound: " + field.Name;
            }
        }
        return null;
    }
    internal static string Outcome(BypassDisposition disposition) => disposition switch
    {
        BypassDisposition.Evaluated => "evaluated",
        BypassDisposition.Refused => "refused",
        BypassDisposition.NotApplicable => "not-applicable",
        BypassDisposition.ForcedEnd => "forced-end",
        _ => ContractVerdict.NotEvaluated,
    };
    internal static ContractVerdict Invalid(string reason, string rule, int page) => new(ContractVerdict.NotEvaluated, [new(reason, rule, page)], []);

    [RuleContract]
    private static RuleContract Turret()
    {
        ContractFact[] fields = [Flag("inBypass"), Anchor(), .. FrameFields("frame"), .. HexFields("target"),
            Choice<BypassTurretPosition>("current", true), Choice<BypassTurretPosition>("proposed", true), Choice<BypassTurnDirection>("direction", true),
            Choice<BypassTraverse>("traverse", true), Choice<BypassWeaponKind>("weapon", true), Choice<BypassFireMode>("mode"), Flag("specialTerrain"),
            Flag("applicableAcquisition", true), Flag("sameKnownTarget", true), Flag("sameTargetFacing", true), Flag("earlierMpTcaChange", true)];
        return new("d2.321-bypass-tca", "Bypass turret arc and local fire components",
            [new("D2.321", 197), new("D2.32", 197), new("C5.13", 172)], fields,
            ["evaluated", "refused", "not-applicable", ContractVerdict.NotEvaluated], [], new("PFPh", "2.22A"))
        {
            Decide = facts =>
            {
                if (BadNumbers(facts, fields) is { } bad)
                {
                    return Invalid(bad, "D2.321", 197);
                }
                var result = ScenarioA1BypassTurretCalculator.FireAdjustment(new(facts.Flag("inBypass"), Frame(facts, "frame"), Hex(facts, "target"),
                    EnumFact<BypassTurretPosition>(facts, "current", true), EnumFact<BypassTurretPosition>(facts, "proposed", true), EnumFact<BypassTurnDirection>(facts, "direction", true),
                    EnumFact<BypassTraverse>(facts, "traverse", true), EnumFact<BypassWeaponKind>(facts, "weapon", true), EnumFact<BypassFireMode>(facts, "mode"), facts.Flag("specialTerrain"),
                    OwnerFlag(facts, "applicableAcquisition"), OwnerFlag(facts, "sameKnownTarget"), OwnerFlag(facts, "sameTargetFacing"), OwnerFlag(facts, "earlierMpTcaChange")));
                var detail = result.Disposition == BypassDisposition.Evaluated
                    ? string.Create(CultureInfo.InvariantCulture, $" Sector={result.Sector}; Target Facing steps={result.Steps}; Case A={result.CaseA}; separate To Hit side DRM={result.SideChangeToHitDrm}.") : "";
                return new(Outcome(result.Disposition), [new(result.Reason + detail, result.Rule, result.Page)], []);
            },
        };
    }
}
