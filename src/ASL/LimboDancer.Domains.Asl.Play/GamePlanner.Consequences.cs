using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>What a proposal does beyond its checks, by kind (pass 31; play test P-06, P-12, P-13).</summary>
public enum ConsequenceKind
{
    /// <summary>A unit is eliminated, captured, or surrenders when the proposal is confirmed.</summary>
    Loss,

    /// <summary>The game ends.</summary>
    End,

    /// <summary>The proposer's own units or prisoners are attacked too.</summary>
    OwnUnits,

    /// <summary>The action is spent to no effect.</summary>
    Waste,
}

/// <summary>A consequence of a proposal: its kind, and the sentence the review shows.</summary>
public sealed record PlanConsequence(ConsequenceKind Kind, string Text);

/// <summary>
/// The consequences of a proposal (pass 31). A proposal that passes every check may still eliminate units, end the game, hit the proposer's own
/// units, or spend a shot for nothing; the planner names these reasons, and the review shows them apart from the routine ones.
/// </summary>
public sealed partial class GamePlanner
{
    private static readonly (string Code, ConsequenceKind Kind)[] ConsequenceCodes =
    [
        ("play.failure-to-rout-surrender:", ConsequenceKind.Loss),
        ("play.failure-to-rout:", ConsequenceKind.Loss),
        ("play.melee-eliminated:", ConsequenceKind.Loss),
        ("play.game-ended:", ConsequenceKind.End),
        ("play.fire-own-units:", ConsequenceKind.OwnUnits),
        ("play.fire-los-blocked:", ConsequenceKind.Waste),
    ];

    /// <summary>The consequence a reason of a plan states, or null for a routine reason.</summary>
    public static PlanConsequence? ConsequenceOf(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        foreach (var (code, kind) in ConsequenceCodes)
        {
            if (reason.StartsWith(code, StringComparison.Ordinal))
            {
                return new PlanConsequence(kind, reason[code.Length..].Trim());
            }
        }

        return null;
    }

    /// <summary>
    /// What an attack does beyond its target (play test P-12, P-13): every firer's LOS is blocked, so the shot is spent for nothing (A6.1), or the
    /// target Location holds units of the firing side, in a Melee or as Guards of prisoners, which the attack hits too (A11.15, A20.54).
    /// </summary>
    private static IEnumerable<string> FireWarnings(GameState state, FireAttack facts)
    {
        if (facts.Firers is { Count: > 0 } firers && firers.All(firer => (firer.Los ?? facts.Los)?.Blocked == true))
        {
            yield return $"play.fire-los-blocked: no firer has a LOS to {facts.TargetLocationId}, so the attack has no effect and its firers are still marked as having fired (A6.1)";
        }

        var side = facts.Firers?.Select(firer => firer.UnitId is { } id ? state.Unit(id)?.Side : null).OfType<string>().FirstOrDefault();
        if (side is null || facts.TargetLocationId is null || !BoardLocation.TryParse(facts.TargetLocationId, out var target))
        {
            yield break;
        }

        string[] own = [.. state.At(target).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side && unit.Kind != UnitKinds.Dummy)
            .Select(unit => unit.Id).Order(StringComparer.Ordinal)];
        if (own.Length > 0)
        {
            yield return $"play.fire-own-units: {string.Join(", ", own)} of the firing side {(own.Length == 1 ? "is" : "are")} in {facts.TargetLocationId} and {(own.Length == 1 ? "is" : "are")} attacked too (A11.15, A20.54)";
        }
    }
}
