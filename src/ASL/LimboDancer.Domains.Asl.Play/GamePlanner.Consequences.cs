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

    /// <summary>A SW is left behind, unpossessed (pass 31d: a broken unit's rout, A10.4).</summary>
    Left,

    /// <summary>The proposer's own Dummies are removed, or may be (pass 31d: A12.11, A11.19).</summary>
    Dummies,

    /// <summary>A Close Combat is left unfought as its phase ends (pass 31d: A11.15).</summary>
    Unfought,
}

/// <summary>One SW a unit possesses, with its PP (A4.4).</summary>
public sealed record RoutLoadItem(string Weapon, int Pp);

/// <summary>
/// What a broken unit carries before it routs (A10.4; pass 31d, ruling R31d.1): its IPC, its SW with their PP, and the sets of them it may rout with.
/// </summary>
public sealed record RoutLoad(int Ipc, IReadOnlyList<RoutLoadItem> Carried, IReadOnlyList<IReadOnlyList<string>> BestLoads)
{
    public int Total => Carried.Sum(item => item.Pp);

    /// <summary>Whether the unit carries more than its IPC, and so leaves a SW before it routs.</summary>
    public bool Laden => Total > Ipc;

    public int Pp(string weapon) => Carried.FirstOrDefault(item => item.Weapon == weapon)?.Pp ?? 0;

    /// <summary>The SW left behind when a load is kept.</summary>
    public IReadOnlyList<RoutLoadItem> Left(IReadOnlyList<string> kept)
    {
        ArgumentNullException.ThrowIfNull(kept);
        return [.. Carried.Where(item => !kept.Contains(item.Weapon, StringComparer.Ordinal))];
    }
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
        ("play.result:", ConsequenceKind.End),
        ("play.fire-own-units:", ConsequenceKind.OwnUnits),
        ("play.fire-los-blocked:", ConsequenceKind.Waste),
        ("play.rout-leaves:", ConsequenceKind.Left),
        ("play.dummies:", ConsequenceKind.Dummies),
        ("play.cc-unfought:", ConsequenceKind.Unfought),
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
    private static IEnumerable<string> FireWarnings(FireAttack facts)
    {
        if (facts.Firers is { Count: > 0 } firers && firers.All(firer => (firer.Los ?? facts.Los)?.Blocked == true))
        {
            yield return $"play.fire-los-blocked: no firer has a LOS to {facts.TargetLocationId}, so the attack has no effect and its firers are still marked as having fired (A6.11)";
        }

        // Referee, pass 31: the attack's own targets say who is hit; Defensive First Fire attacks only the moving stack (A8.1), so the firing side's
        // other units in the Location are not among them.
        // Pass 31d (design D6; A20.54, read in the PDF, p. 87): the firing side's captured units are said apart from its units in a Melee, with what
        // the rule does to them. The play test confirmed such an attack twice without reading a line that named no prisoner.
        string[] own = [.. (facts.Targets ?? []).Where(target => target.Friendly == true && target.Dummy != true && target.UnitId is not null && target.GuardId is null)
            .Select(target => target.UnitId!).Order(StringComparer.Ordinal)];
        if (own.Length > 0)
        {
            yield return $"play.fire-own-units: {string.Join(", ", own)} of the firing side {(own.Length == 1 ? "is" : "are")} in {facts.TargetLocationId} and {(own.Length == 1 ? "is" : "are")} attacked too (A11.15, A20.54)";
        }

        string[] captured = [.. (facts.Targets ?? []).Where(target => target.Friendly == true && target.Dummy != true && target.UnitId is not null && target.GuardId is not null)
            .Select(target => target.UnitId!).Order(StringComparer.Ordinal)];
        if (captured.Length > 0)
        {
            yield return $"play.fire-own-units: {string.Join(", ", captured)}, captured {(captured.Length == 1 ? "unit" : "units")} of the firing side, {(captured.Length == 1 ? "is" : "are")} in {facts.TargetLocationId} and "
                + $"{(captured.Length == 1 ? "is" : "are")} attacked with {(captured.Length == 1 ? "its" : "their")} Guard, as if in a Melee: one that fails a MC is Reduced, and one eliminated by its own side's fire counts double (A20.54)";
        }
    }
}
