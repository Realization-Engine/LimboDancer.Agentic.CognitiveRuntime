using System.Text.Json;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Who may propose what (pass 31, ruling R31.6). A proposal may name the view it comes from in <c>proposedBy</c>: a side's id, or
/// <c>adjudicator</c>. A side's view proposes only what its side may do: an action of its own units, the DEFENDER's pass, the answer to a
/// choice or a surrender put to it, and the end of a phase its side ends. The adjudicator, and a caller that names no view (the tests and
/// every game recorded before the pass), may propose anything, as before.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The argument that names the proposing view.</summary>
    public const string ProposedByArgument = "proposedBy";

    /// <summary>The value of <see cref="ProposedByArgument"/> for the adjudicator's view.</summary>
    public const string AdjudicatorView = "adjudicator";

    /// <summary>The arguments that name the units an action is taken by; an action not listed is taken by no single side's units.</summary>
    private static readonly Dictionary<string, string[]> ActorArguments = new(StringComparer.Ordinal)
    {
        ["asl.game.rout"] = ["unitId"],
        ["asl.game.deploy"] = ["squadId"],
        ["asl.game.recombine"] = ["halfSquads"],
        ["asl.game.transfer"] = ["unitId"],
        ["asl.game.drop"] = ["unitId"],
        ["asl.game.recover"] = ["unitId"],
        ["asl.game.dismantle"] = ["unitId"],
        ["asl.game.enter-empty-building"] = ["unitId"],
        ["asl.game.enter-building"] = ["unitId"],
        ["asl.game.declare-overrun"] = ["unitId"],
        ["asl.game.fire"] = ["firers"],
        ["asl.game.opportunity-fire"] = ["unitIds"],
        ["asl.game.rally"] = ["unitId"],
        ["asl.game.repair"] = ["unitId", "equipmentId"],
        ["asl.game.move"] = ["unitIds"],
        ["asl.game.end-move"] = ["unitIds"],
        ["asl.game.advance"] = ["unitIds"],
        ["asl.game.ambush-withdraw"] = ["unitIds"],
        ["asl.game.guard-prisoners"] = ["guardId"],
        ["asl.game.fire-ordnance"] = ["gunId"],
        ["asl.game.recover-shock"] = ["vehicleId"],
        ["asl.game.turn-gun"] = ["gunId"],
        ["asl.game.hook-gun"] = ["vehicleId"],
        ["asl.game.move-vehicle"] = ["vehicleId"],
        ["asl.game.overrun"] = ["vehicleId"],
        ["asl.game.button-up"] = ["vehicleId"],
        ["asl.game.massacre"] = ["unitId"],
        ["asl.game.throw-dc"] = ["unitId"],
        ["asl.game.fire-starshell"] = ["unitId"],
        ["asl.game.place-hidden"] = ["unitIds"],
        ["asl.game.mop-up"] = ["unitIds"],
    };

    /// <summary>The side whose view proposes, or null for the adjudicator's view and for a caller that names none.</summary>
    public static string? ProposedBy(JsonElement arguments) =>
        arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(ProposedByArgument, out var by) && by.ValueKind == JsonValueKind.String
            && by.GetString() is { Length: > 0 } side && side != AdjudicatorView ? side : null;

    /// <summary>The side that ends the present phase: either side in the RPh, RtPh, and CCPh, where both act; the DEFENDER in the DFPh; else the ATTACKER.</summary>
    public static string? PhaseEndedBy(GameState state, string proposer)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Phase is "rph" or "rtph" or "ccph" ? proposer
            : state.Phase == "dfph" ? state.Sides.FirstOrDefault(side => side.Id != state.PhasingSide)?.Id ?? state.PhasingSide
            : state.PhasingSide;
    }

    /// <summary>Why the <paramref name="proposer"/> side's view may not propose this action (ruling R31.6), or null when it may.</summary>
    private string? ProposerBar(GameState state, string action, JsonElement arguments, string proposer, IReadOnlyList<GameEvent> existing)
    {
        // A view that is not one of the game's sides is not checked: the game has no side to hold it to.
        if (state.Sides.All(side => side.Id != proposer))
        {
            return null;
        }

        var defender = state.Sides.FirstOrDefault(side => side.Id != state.PhasingSide)?.Id;
        var (owner, what) = action switch
        {
            "asl.game.advance-phase" => (PhaseEndedBy(state, proposer), "ends this phase"),
            "asl.game.pass-fire" => (defender, "is the DEFENDER and passes"),
            "asl.game.choose" => (state.Choice?.Side, "answers this choice"),
            "asl.game.take-prisoner" => (CaptorSide(state, arguments), "answers this surrender"),
            _ => (null, string.Empty),
        };
        if (owner is not null && owner != proposer)
        {
            return $"play.not-your-action: the {owner} side {what}, not the {proposer} side (ruling R31.6)";
        }

        // Table player, pass 31: either side ends the Rout Phase, but not while the other side still has a unit that must rout: the end would eliminate
        // it, or make it surrender, on its opponent's word (A10.5, A20.21).
        if (action == "asl.game.advance-phase" && state.Phase == "rtph"
            && FailureToRout(state, existing).Select(item => item.Unit).FirstOrDefault(unit => unit.Side != proposer) is { } owed)
        {
            return $"play.not-your-action: the {owed.Side} side still has a unit that must rout, and ending the Rout Phase now would eliminate it or make it surrender; hand over to the {owed.Side} side first (A10.5; ruling R31.6)";
        }

        foreach (var name in ActorArguments.GetValueOrDefault(action) ?? [])
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
                if (SideOf(state, id) is { } side && side != proposer)
                {
                    return $"play.not-your-unit: {id} belongs to the {side} side; the {proposer} side's view does not act for it (ruling R31.6)";
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
