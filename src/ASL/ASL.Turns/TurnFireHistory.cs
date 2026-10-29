using System.Collections.Immutable;
using LimboDancer.Domains.Asl.Rules;

namespace ASL.Turns;

public enum TurnFireKind
{
    Fire, Ordnance
}
public enum TurnSpotterState
{
    NotEvaluated, Retained, Lost, Superseded
}

/// <summary>Already-disclosed firing facts in event order. No target identities, raw payload, event IDs or revisions.</summary>
public sealed record TurnFireRecord(TurnFireKind Kind, int GameTurn, string PhasingSide, string Phase,
    ImmutableArray<string> Firers, string? Weapon, string? Spotter, bool? RateOfFireKept, bool InCurrentPhase);

/// <summary>Authorized present facts. Unknown observations never imply a usable or lost Spotter.</summary>
public sealed record TurnSpotterRead(string Weapon, string Spotter, bool Known, bool Active,
    bool SameSide, bool Broken, bool Captured, bool SpottedThisPhase, bool RecordedLoss = false, string? Successor = null);

public sealed record TurnSpotterHistory(string Weapon, string Spotter, int FirstObservedGameTurn, string FirstObservedPhase,
    TurnSpotterState State, bool? SpottedThisPhase, string? Successor = null);
/// <summary>Disclosed explicit declaration, distinct from the first shot that observed a Spotter. No journal identities.</summary>
public sealed record TurnSpotterDeclaration(string Weapon, string Spotter, string OwnerSide, int GameTurn, string Phase);

public sealed record TurnFireHistorySnapshot(ImmutableArray<TurnFireRecord> Shots,
    ImmutableArray<TurnSpotterHistory> Spotters, ImmutableArray<string> EvaluationGaps)
{
    public ImmutableArray<TurnSpotterDeclaration> Declarations { get; init; } = [];
}

/// <summary>Composes disclosed evidence only. A past shot or retained ROF is never permission to fire again.</summary>
public static class TurnFireHistory
{
    public static TurnFireHistorySnapshot Compose(ImmutableArray<TurnFireRecord> shots, ImmutableArray<TurnSpotterRead> reads)
    {
        if (shots.IsDefault || reads.IsDefault)
        {
            throw new ArgumentException("Supply history and observations, even when empty.");
        }
        var observations = reads.ToDictionary(item => (item.Weapon, item.Spotter));
        var assignments = new List<TurnSpotterHistory>();
        var latest = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var shot in shots)
        {
            if (shot.Kind != TurnFireKind.Ordnance || shot.Weapon is null || shot.Spotter is null)
            {
                continue;
            }
            if (latest.TryGetValue(shot.Weapon, out var index))
            {
                // C9.3 (the C9.3 claim, ruling RC93.7): whether a later Spotter supersedes the assignment is the calculator's.
                if (!ScenarioA1SpotterCalculator.Supersedes(assignments[index].Spotter, shot.Spotter, assignments[index].Successor))
                {
                    continue;
                }
                assignments[index] = assignments[index] with
                {
                    State = TurnSpotterState.Superseded,
                    SpottedThisPhase = null
                };
            }
            latest[shot.Weapon] = assignments.Count;
            var known = observations.GetValueOrDefault((shot.Weapon, shot.Spotter));
            var state = TurnSpotterState.NotEvaluated;
            bool? spotted = null;
            string? successor = null;
            if (known is { Known: true })
            {
                state = ScenarioA1SpotterCalculator.Retains(known.Active, known.SameSide, known.Broken, known.Captured, known.RecordedLoss)
                    ? TurnSpotterState.Retained : TurnSpotterState.Lost;
                spotted = known.SpottedThisPhase;
                successor = known.Successor;
            }
            assignments.Add(new(shot.Weapon, shot.Spotter, shot.GameTurn, shot.Phase, state, spotted, successor));
        }
        return new(shots, [.. assignments],
            ["Recorded fire and ROF are history, not current firing eligibility. Undisclosed attacks are omitted.",
             "Explicit daytime original declarations are recorded when supplied; setup activation, replacement commands, Opportunity Fire and its breakdown check remain pending.",
             "Vehicle/PRC fire combinations and other unregistered rule families remain pending."]);
    }
}
