using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Who may propose what (pass 31, ruling R31.6). A proposal may name the view it comes from in <c>proposedBy</c>: a side's id, or
/// <c>adjudicator</c>. A side's view proposes only what its side may do: an action of its own units, the DEFENDER's pass, the answer to a
/// choice or a surrender put to it, and the end of a phase its side ends. The adjudicator, and a caller that names no view (the tests and
/// every game recorded before the pass), may propose anything, as before. The rules are <see cref="ScenarioA1SequenceCalculator"/>'s (pass 32.i);
/// this file reads the request and the state.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The argument that names the proposing view.</summary>
    public const string ProposedByArgument = "proposedBy";

    /// <summary>The value of <see cref="ProposedByArgument"/> for the adjudicator's view.</summary>
    public const string AdjudicatorView = "adjudicator";

    /// <summary>The side whose view proposes, or null for the adjudicator's view and for a caller that names none.</summary>
    public static string? ProposedBy(JsonElement arguments) =>
        arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(ProposedByArgument, out var by) && by.ValueKind == JsonValueKind.String
            && by.GetString() is { Length: > 0 } side && side != AdjudicatorView ? side : null;

    /// <summary>The side that ends the present phase: either side in the RPh, RtPh, and CCPh, where both act; the DEFENDER in the DFPh; else the ATTACKER.</summary>
    public static string? PhaseEndedBy(GameState state, string proposer)
    {
        ArgumentNullException.ThrowIfNull(state);
        return ScenarioA1SequenceCalculator.PhaseEndedBy(state.Phase, proposer, state.Sides.FirstOrDefault(side => side.Id != state.PhasingSide)?.Id, state.PhasingSide);
    }

    /// <summary>Why the <paramref name="proposer"/> side's view may not propose this action (ruling R31.6), or null when it may; Rules decides, over the state's reads (pass 32.i).</summary>
    private string? ProposerBar(GameState state, string action, JsonElement arguments, string proposer, IReadOnlyList<GameEvent> existing)
    {
        if (!ScenarioA1SequenceCalculator.ProposerChecked(state.Sides.Any(side => side.Id == proposer)))
        {
            return null;
        }

        var defender = state.Sides.FirstOrDefault(side => side.Id != state.PhasingSide)?.Id;
        var owner = ScenarioA1SequenceCalculator.Owner(action, () => PhaseEndedBy(state, proposer), defender, () => state.Choice?.Side, () => CaptorSide(state, arguments));
        if (ScenarioA1SequenceCalculator.NotYourActionBar(owner, proposer) is { } notYours)
        {
            return notYours;
        }

        if (ScenarioA1SequenceCalculator.RoutPhaseEndBar(action, state.Phase,
            () => FailureToRout(state, existing).Select(item => item.Unit).FirstOrDefault(unit => unit.Side != proposer && RoutStillOwed(state, unit))?.Side) is { } owed)
        {
            return owed;
        }

        foreach (var name in ScenarioA1SequenceCalculator.ActorArguments.GetValueOrDefault(action) ?? [])
        {
            if (!arguments.TryGetProperty(name, out var value))
            {
                continue;
            }

            var ids = value.ValueKind switch
            {
                JsonValueKind.String => [value.GetString()!],
                JsonValueKind.Array => value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!).ToArray(),
                _ => Array.Empty<string>(),
            };
            foreach (var id in ids)
            {
                if (ScenarioA1SequenceCalculator.NotYourUnitBar(id, SideOf(state, id), proposer) is { } notYourUnit)
                {
                    return notYourUnit;
                }
            }
        }

        return null;
    }

    /// <summary>The side of the captors a surrender waits on.</summary>
    private static string? CaptorSide(GameState state, JsonElement arguments)
    {
        var unit = arguments.TryGetProperty("unitId", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null;
        var pending = state.PendingSurrenders.FirstOrDefault(item => item.Unit == unit) ?? (state.PendingSurrenders.Count > 0 ? state.PendingSurrenders[0] : null);
        return pending?.Captors.Select(state.Unit).OfType<UnitInstance>().Select(captor => captor.Side).FirstOrDefault();
    }

    /// <summary>The side a unit belongs to, or the side of the unit that holds a weapon; null for anything else.</summary>
    private static string? SideOf(GameState state, string id) => state.Find(id) switch
    {
        UnitInstance unit => unit.Side,
        EquipmentInstance { Holding: { } holding } => state.Unit(holding.Holder)?.Side,
        _ => null,
    };
}
