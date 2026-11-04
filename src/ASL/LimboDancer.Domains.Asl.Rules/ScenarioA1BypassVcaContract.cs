using System.Globalization;
using static LimboDancer.Domains.Asl.Rules.ScenarioA1BypassTurretContract;

namespace LimboDancer.Domains.Asl.Rules;

internal static class ScenarioA1BypassVcaContract
{
    private static IEnumerable<ContractFact> PositionFields() => [Choice<BypassPositionKind>("positionKind"), .. FrameFields("position"),
        .. HexFields("ordinary"), Fact("ordinaryFacing", ContractFactType.Number)];
    private static BypassPositionFacts? Position(ContractFacts facts)
    {
        var kind = EnumFact<BypassPositionKind>(facts, "positionKind");
        var frame = Frame(facts, "position");
        var hex = Hex(facts, "ordinary");
        var facing = (int?)facts.Number("ordinaryFacing");
        return kind is null && frame is null && hex is null && facing is null ? null : new(kind, frame, hex, facing);
    }
    private static BypassEdge? Edge(ContractFacts facts)
    {
        var one = Hex(facts, "edgeOne");
        var two = Hex(facts, "edgeTwo");
        return one is null && two is null ? null : new(one, two);
    }
    private static RuleContract Make(string id, string title, ContractFact[] fields, Func<ContractFacts, BypassVcaVerdict> decide, bool end = false) =>
        new(id, title, [new("D2.33", 197), new("D2.33", 198), new("D2.32", 197)], fields,
            end ? ["evaluated", "refused", "not-applicable", "forced-end", ContractVerdict.NotEvaluated]
                : ["evaluated", "refused", "not-applicable", ContractVerdict.NotEvaluated], [], new("MPh", "3.2"))
        {
            Decide = facts =>
            {
                if (BadNumbers(facts, fields) is { } bad)
                {
                    return Invalid(bad, "D2.33", 198);
                }
                var position = Position(facts);
                if (!ScenarioA1BypassTurretCalculator.Valid(Frame(facts, "frame")) || !ScenarioA1BypassVcaCalculator.Valid(Edge(facts))
                    || !ScenarioA1BypassVcaCalculator.Valid(position) || facts.Flag("pendingTurn") == true && facts.Flag("entryCompleted") == true
                    || position?.Kind == BypassPositionKind.Ordinary && fields.Any(field => field.Name.StartsWith("position", StringComparison.Ordinal)
                        && field.Type == ContractFactType.Number && facts.Number(field.Name) is not null)
                    || position?.Kind == BypassPositionKind.Bypass && (facts.Number("ordinaryQ") is not null || facts.Number("ordinaryR") is not null))
                {
                    return Invalid("A supplied movement shape is invalid or contradictory.", "D2.33", 198);
                }
                var result = decide(facts);
                var detail = result.Disposition is BypassDisposition.Evaluated or BypassDisposition.ForcedEnd or BypassDisposition.NotApplicable
                    ? string.Create(CultureInfo.InvariantCulture, $" Half MP={result.HalfMp}; proposed VCA={result.ProposedFacing}; continuation required={result.ContinuationRequired}; Target Facing={result.TargetFacing}; position={result.Position}.") : "";
                return new(Outcome(result.Disposition), [new(result.Reason + detail, result.Rule, result.Page)], []);
            },
        };

    [RuleContract]
    private static RuleContract Turn() => Make("d2.33-bypass-turn", "Bypass VCA turn component",
        [Flag("inBypass"), Anchor(), .. FrameFields("frame"), Flag("pendingTurn"), .. HexFields("edgeOne"), .. HexFields("edgeTwo"), Choice<BypassTurnPurpose>("purpose")],
        f => ScenarioA1BypassVcaCalculator.TurnAtCafp(new(f.Flag("inBypass"), Frame(f, "frame"), f.Flag("pendingTurn"), Edge(f), EnumFact<BypassTurnPurpose>(f, "purpose"))));

    [RuleContract]
    private static RuleContract Continue() => Make("d2.33-bypass-continuation", "Bypass continuation or movement end component",
        [Flag("inBypass"), Anchor(), Fact("action", ContractFactType.Choice) with { Choices = ["Entry", "EndMovement"] },
            .. FrameFields("frame"), Flag("pendingTurn"), Flag("entryCompleted"), .. HexFields("edgeOne"), .. HexFields("edgeTwo"), Flag("reverse"),
            .. PositionFields(), Choice<BypassEndCause>("endCause"), Flag("immobilized")],
        f => f.Text("action") switch
        {
            "Entry" => ScenarioA1BypassVcaCalculator.Continuation(new(f.Flag("inBypass"), Frame(f, "frame"), f.Flag("pendingTurn"), Edge(f), f.Flag("reverse"), Position(f))),
            "EndMovement" => ScenarioA1BypassVcaCalculator.EndMovement(new(f.Flag("inBypass"), f.Flag("pendingTurn"), f.Flag("entryCompleted"), EnumFact<BypassEndCause>(f, "endCause"), f.Flag("immobilized"))),
            _ => new(BypassDisposition.NotEvaluated, null, null, null, null, null, "Choose Entry or EndMovement."),
        }, true);

    [RuleContract]
    private static RuleContract Defensive() => Make("d2.33-bypass-defensive-position", "Bypass defensive position at an expenditure",
        [Flag("inBypass"), Anchor(), .. FrameFields("frame"), Choice<BypassMovementStage>("stage"), .. PositionFields(), Flag("immobilized"),
            Choice<BypassOriginKind>("originKind"), .. HexFields("origin")],
        f => ScenarioA1BypassVcaCalculator.DefensivePosition(new(f.Flag("inBypass"), Frame(f, "frame"), EnumFact<BypassMovementStage>(f, "stage"), Position(f),
            f.Flag("immobilized"), EnumFact<BypassOriginKind>(f, "originKind"), Hex(f, "origin"))));
}
