using System.Collections.Immutable;
using LimboDancer.Domains.Asl.Rules;

namespace ASL.Turns;

/// <summary>Origin identity survives reads, retries, marker removal and side handover.</summary>
public sealed record TurnObligationKey(string GameId, string OriginEventId, string DefinitionId, string SubjectId, int Occurrence);
public sealed record TurnProgressReceipt(int CompletedGameTurn, string EventId, long Sequence);
public sealed record TurnOutcomeReceipt(TurnRecordedOutcome Outcome, string EventId, long Sequence);
public sealed record TurnObligationSubject(bool Active, bool Stunned, bool Recalled, bool StunRecovery,
    bool Cx, bool Broken, bool Berserk, bool? ClearanceEligible);
public sealed record TurnObligationCandidate(TurnObligationKey Key, long OriginSequence, string OwnerSideId,
    string DecisionOwnerSideId, TurnRulePosition? Origin, TurnObligationSubject Subject,
    ImmutableArray<TurnProgressReceipt> Progress, TurnOutcomeReceipt? Outcome,
    ImmutableArray<TurnObligationKey> Prerequisites);
public sealed record TurnObligationRead(string GameId, long AsOfSequence, TurnRulePosition Position);
public sealed record TurnObligation(TurnObligationCandidate Facts, TurnObligationRuleVerdict Verdict,
    bool WaitingForPrerequisites, TurnSupport TimingSupport, TurnSupport CommandSupport);
public sealed record TurnObligationAssessment(ImmutableArray<TurnDependency> Definitions, ImmutableArray<TurnObligation> Obligations);

/// <summary>
/// Internal host service. Inputs and results may contain hidden facts and require the B4 disclosure adapter.
/// Derives views from domain evidence, never persists state or applies effects. All definitions remain visible.
/// </summary>
public sealed class TurnObligationService
{
    private readonly TurnGuideCatalog catalog;
    public TurnObligationService(TurnGuideCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        this.catalog = catalog;
    }

    public TurnObligationAssessment Assess(TurnObligationRead read, IEnumerable<TurnObligationCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(read.GameId);
        ArgumentOutOfRangeException.ThrowIfNegative(read.AsOfSequence);
        TurnObligationRules.ValidatePosition(read.Position);
        var items = candidates.ToImmutableArray();
        var known = catalog.Dependencies.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var byKey = items.ToDictionary(item => item.Key);
        foreach (var item in items)
        {
            Validate(read, item, known);
            if (item.Prerequisites.Any(key => !byKey.ContainsKey(key)))
            {
                throw new InvalidDataException("A prerequisite origin is missing from the assessment.");
            }
        }
        var ordered = new List<TurnObligation>();
        var visiting = new HashSet<TurnObligationKey>();
        var completed = new Dictionary<TurnObligationKey, TurnObligation>();
        foreach (var item in items.OrderBy(item => item.OriginSequence).ThenBy(item => item.Key.DefinitionId, StringComparer.Ordinal)
            .ThenBy(item => item.Key.OriginEventId, StringComparer.Ordinal).ThenBy(item => item.Key.SubjectId, StringComparer.Ordinal).ThenBy(item => item.Key.Occurrence))
        {
            Visit(item);
        }

        return new TurnObligationAssessment(catalog.Dependencies, [.. ordered]);

        void Visit(TurnObligationCandidate item)
        {
            if (completed.ContainsKey(item.Key))
            {
                return;
            }

            if (!visiting.Add(item.Key))
            {
                throw new InvalidDataException("Obligation prerequisites contain a cycle.");
            }

            foreach (var key in item.Prerequisites.OrderBy(key => key.OriginEventId, StringComparer.Ordinal)
                .ThenBy(key => key.DefinitionId, StringComparer.Ordinal).ThenBy(key => key.SubjectId, StringComparer.Ordinal).ThenBy(key => key.Occurrence))
            {
                Visit(byKey[key]);
            }

            var family = Family(item.Key.DefinitionId);
            var subject = item.Subject;
            var verdict = TurnObligationRules.Evaluate(new TurnObligationRuleFacts(family, item.Origin, read.Position,
                item.OwnerSideId, item.Outcome?.Outcome ?? TurnRecordedOutcome.None, subject.Active, subject.Stunned,
                subject.Recalled, subject.StunRecovery, subject.Cx, subject.Broken, subject.Berserk, subject.ClearanceEligible));
            // An interrupted/cancelled child is not assumed to satisfy a rule-required consequence.
            var waiting = item.Prerequisites.Any(key => completed[key].Verdict.State != TurnObligationState.Resolved);
            if (verdict.State == TurnObligationState.Resolved && (waiting || item.Prerequisites.Any(key => completed[key].Facts.Outcome!.Sequence > item.Outcome!.Sequence)))
            {
                throw new InvalidDataException("Parent resolution precedes its required consequence evidence.");
            }
            var result = new TurnObligation(item, verdict, waiting,
                family == TurnObligationFamily.Unsupported ? TurnSupport.Pending : TurnSupport.Partial, TurnSupport.Pending);
            completed.Add(item.Key, result);
            visiting.Remove(item.Key);
            ordered.Add(result);
        }
    }

    private static void Validate(TurnObligationRead read, TurnObligationCandidate item, HashSet<string> definitions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Key.OriginEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Key.SubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.OwnerSideId);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.DecisionOwnerSideId);
        ArgumentOutOfRangeException.ThrowIfNegative(item.Key.Occurrence);
        if (item.Key.GameId != read.GameId || !definitions.Contains(item.Key.DefinitionId))
        {
            throw new InvalidDataException("Unknown definition or wrong game.");
        }

        if (item.OriginSequence < 0 || item.OriginSequence > read.AsOfSequence)
        {
            throw new InvalidDataException("Origin lies outside the supplied history.");
        }

        if (item.Progress.IsDefault || item.Prerequisites.IsDefault)
        {
            throw new InvalidDataException("Progress and prerequisites must be supplied, even when empty.");
        }

        if (item.Progress.Select(receipt => receipt.CompletedGameTurn).Distinct().Count() != item.Progress.Length)
        {
            throw new InvalidDataException("A completed Game Turn interval cannot be counted twice.");
        }

        foreach (var receipt in item.Progress)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(receipt.EventId);
            if (receipt.CompletedGameTurn < 1 || receipt.CompletedGameTurn > read.Position.GameTurn || receipt.CompletedGameTurn < item.Origin?.GameTurn ||
                receipt.Sequence < item.OriginSequence || receipt.Sequence > read.AsOfSequence)
            {
                throw new InvalidDataException("Progress receipt lies outside the supplied history.");
            }
        }
        if (item.Outcome is { } outcome)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outcome.EventId);
            if (!Enum.IsDefined(outcome.Outcome) || outcome.Outcome == TurnRecordedOutcome.None ||
                outcome.Sequence < item.OriginSequence || outcome.Sequence > read.AsOfSequence || item.Progress.Any(receipt => receipt.Sequence > outcome.Sequence))
            {
                throw new InvalidDataException("Invalid terminal evidence.");
            }
        }
    }

    private static TurnObligationFamily Family(string definition) => definition switch
    {
        "stun" => TurnObligationFamily.Stun,
        "cx" => TurnObligationFamily.Cx,
        "tripFlare" => TurnObligationFamily.TripFlare,
        "clearance" => TurnObligationFamily.NonFlameClearance,
        _ => TurnObligationFamily.Unsupported,
    };
}
