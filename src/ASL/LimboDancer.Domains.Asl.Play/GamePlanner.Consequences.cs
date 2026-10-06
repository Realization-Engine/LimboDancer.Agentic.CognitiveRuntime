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

/// <summary>One SW a unit possesses, with its PP (A4.4), and what it is: its counter and state, by which two like SW are the same to a player.</summary>
public sealed record RoutLoadItem(string Weapon, int Pp)
{
    public string Kind { get; init; } = string.Empty;
}

/// <summary>
/// What a broken unit carries before it routs (A10.4; pass 31d, ruling R31d.1): its IPC, its SW with their PP, and the sets of them it may rout with.
/// </summary>
public sealed record RoutLoad(int Ipc, IReadOnlyList<RoutLoadItem> Carried, IReadOnlyList<IReadOnlyList<string>> BestLoads)
{
    public int Total => Carried.Sum(item => item.Pp);

    /// <summary>Whether the unit carries more than its IPC, and so leaves a SW before it routs.</summary>
    public bool Laden => ScenarioA1ResultTables.RoutLaden(Total, Ipc);

    /// <summary>
    /// The best loads that differ to a player (the table player, pass 31d): two that differ only in which of two like counters is kept are one
    /// choice, since a SW has no name of its own. The first of each is given.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Choices => ScenarioA1ResultTables.RoutLoadChoices(BestLoads, id => Carried.First(item => item.Weapon == id).Kind);

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
    private static IEnumerable<string> FireWarnings(FireAttack facts) => ScenarioA1ResultTables.FireWarnings(facts);
}
