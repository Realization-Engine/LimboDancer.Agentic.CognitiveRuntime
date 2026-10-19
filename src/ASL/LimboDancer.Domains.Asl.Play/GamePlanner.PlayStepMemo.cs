using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// What a Play step asks the rules again and again about one state, kept with that state (the Play Step Plan, V5; the analysis's ruling P4). A
/// <see cref="GameState"/> is an immutable snapshot of one revision, so an answer read from it stays true for as long as the state lives; the memo
/// lives exactly as long, in a <see cref="ConditionalWeakTable{TKey, TValue}"/> keyed by the state object. The planner is shared by every circuit,
/// so the stores are concurrent; two circuits that fill the same entry at once compute the same answer, and either may be kept.
/// </summary>
public sealed partial class GamePlanner
{
    private readonly ConditionalWeakTable<GameState, PlayStepMemo> playStepMemos = [];

    private PlayStepMemo MemoOf(GameState state) => playStepMemos.GetValue(state, static _ => new PlayStepMemo());

    /// <summary>The answers kept for one state.</summary>
    private sealed class PlayStepMemo
    {
        /// <summary>
        /// A7.72's answer per unit, keyed by the unit object itself: the answer reads the unit's own fields (its side, status, kind, and conditions)
        /// as well as the state, so only the state's own instance of a unit may share it. It reads no view (the rule counts unconcealed enemy units
        /// only) and no enemy list, so it is kept at once.
        /// </summary>
        public ConcurrentDictionary<UnitInstance, bool> UpperLevelEncircled { get; } = new(ReferenceEqualityComparer.Instance);
    }
}
