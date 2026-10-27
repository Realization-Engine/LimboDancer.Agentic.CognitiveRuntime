using System.Collections.Immutable;
using LimboDancer.Domains.Asl.Rules;

namespace ASL.Turns;

/// <summary>Internal authenticated evidence. Each boundary must come from the corresponding first-RPh/second-CCPh domain event.</summary>
public sealed record TurnWorkInterval(int GameTurn, string? FirstRallyEvent, long? FirstRallySequence,
    string? SecondCloseCombatEvent, long? SecondCloseCombatSequence, bool Interrupted);
public sealed record TurnWorkEvidence(TurnObligationKey Key, long OriginSequence, int? FirstGameTurn,
    ImmutableArray<string> Participants, TurnWorkKind Kind, int? OriginalBNumber, bool? Eligible,
    ImmutableArray<TurnWorkInterval> Intervals);
public sealed record TurnWorkRead(string GameId, long Sequence, int GameTurn, TurnWorkWindow Window);
public sealed record TurnWorkProgressItem(TurnObligationKey Key, ImmutableArray<string> Participants,
    int CompletedGameTurns, ImmutableArray<int> InterruptedGameTurns, ImmutableArray<int> IncompleteGameTurns,
    TurnWorkVerdict Verdict, TurnSupport CommandSupport);
public sealed record TurnWorkProgressSnapshot(ImmutableArray<TurnWorkProgressItem> Items, ImmutableArray<string> PendingRules);

/// <summary>Internal ledger, not a UI completion API. Hosts must authorize evidence and disclose results before returning instances.</summary>
public static class TurnWorkProgress
{
    public static TurnWorkProgressSnapshot Assess(TurnWorkRead read, ImmutableArray<TurnWorkEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentException.ThrowIfNullOrWhiteSpace(read.GameId);
        ArgumentOutOfRangeException.ThrowIfNegative(read.Sequence);
        ArgumentOutOfRangeException.ThrowIfLessThan(read.GameTurn, 1);
        if (evidence.IsDefault)
        {
            throw new ArgumentException("Supply work evidence, even when empty.");
        }

        var keys = new HashSet<TurnObligationKey>();
        var results = new List<TurnWorkProgressItem>();
        foreach (var item in evidence)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Key.OriginEventId);
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Key.SubjectId);
            if (!keys.Add(item.Key) || item.Key.GameId != read.GameId || item.OriginSequence < 0 || item.OriginSequence > read.Sequence)
            {
                throw new InvalidDataException("Duplicate, wrong-game or out-of-history work origin.");
            }

            if (item.FirstGameTurn is < 1 || item.FirstGameTurn > read.GameTurn || item.Participants.IsDefaultOrEmpty
                || item.Participants.Any(string.IsNullOrWhiteSpace) || item.Intervals.IsDefault)
            {
                throw new InvalidDataException("Incomplete work participants or invalid interval origin.");
            }

            var turns = new HashSet<int>();
            var boundaryIds = new HashSet<string>(StringComparer.Ordinal);
            var completed = 0;
            var interrupted = new List<int>();
            var incomplete = new List<int>();
            long lastEnd = item.OriginSequence;
            foreach (var interval in item.Intervals.OrderBy(value => value.GameTurn))
            {
                if (!turns.Add(interval.GameTurn) || interval.GameTurn < 1 || interval.GameTurn > read.GameTurn || interval.GameTurn < item.FirstGameTurn)
                {
                    throw new InvalidDataException("Duplicate or out-of-history Game Turn interval.");
                }

                ValidateBoundary(interval.FirstRallyEvent, interval.FirstRallySequence);
                ValidateBoundary(interval.SecondCloseCombatEvent, interval.SecondCloseCombatSequence);
                if ((interval.FirstRallySequence ?? interval.SecondCloseCombatSequence) < lastEnd || interval.SecondCloseCombatSequence <= interval.FirstRallySequence)
                {
                    throw new InvalidDataException("Work interval boundaries are reversed or overlap.");
                }

                lastEnd = interval.SecondCloseCombatSequence ?? interval.FirstRallySequence ?? lastEnd;

                if (interval.Interrupted)
                {
                    interrupted.Add(interval.GameTurn);
                }
                else if (interval.FirstRallySequence is not null && interval.SecondCloseCombatSequence is not null)
                {
                    completed++;
                }
                else
                {
                    incomplete.Add(interval.GameTurn);
                }
            }
            var verdict = TurnWorkRules.Evaluate(item.Kind, item.OriginalBNumber, completed, item.Eligible,
                item.FirstGameTurn is not null, read.Window);
            results.Add(new(item.Key, item.Participants, completed, [.. interrupted], [.. incomplete], verdict, TurnSupport.Pending));

            void ValidateBoundary(string? id, long? sequence)
            {
                if ((id is null) != (sequence is null) || id is not null && (string.IsNullOrWhiteSpace(id) || !boundaryIds.Add(id))
                    || sequence < item.OriginSequence || sequence > read.Sequence)
                {
                    throw new InvalidDataException("Invalid boundary receipt.");
                }
            }
        }
        return new([.. results],
            ["Packing: G10.11, G10.3-.31; complete Game Turns, linked Gun/Mule/crew, night doubling and minimum duration require rule-specific facts. Release is at next first RPh 1.13B, not ordinary CCPh TI cleanup.",
             "Packing midpoint loading/unloading and reversal remain pending; no duration is guessed from the counter or elapsed Player Turns.",
             "Replenishment: E10.3; original B# <=9/10/11 requires 1/2/3 complete Game Turns. Loss of CE, MP expenditure, firing or CC attack can invalidate the current interval; prior completed intervals remain.",
             "Ammo Dump/offload/transfer: E10.4, E10.6; four complete Game Turns and separate supply/status movement consequences remain pending.",
             "Permissioned work adjudication records declarations, interruptions and completion with TI ownership and an Ammunition DR. Automatic packing/supply eligibility, packing midpoint/reversal, ammo combat effects and supply counter/portage mutations remain pending."]);
    }
}
