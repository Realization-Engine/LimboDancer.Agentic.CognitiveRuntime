using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 71, increment 4c (A14.01, A14.2; ruling R71.11): a game from a card whose side has a SAN of 2 or more starts play only once that side has placed its
/// Sniper counter, the DEFENDER first. A test that starts play from a card places them as a player would, each in the first hex the planner offers, before it
/// advances the phase; nothing is placed while a group of the card still sets up, or once play has started.
/// </summary>
internal static class CardSnipers
{
    public static async Task PlaceDue(Func<GamePlanner> planner, Func<IReadOnlyList<GameEvent>?> events, Func<object, Task<PlayResult>> setup)
    {
        for (var placed = 0; placed < 2; placed++)
        {
            if (events() is not { Count: > 0 } history || planner().Replay(history).Current is not { } state
                || planner().SniperPlacementsDue(state) is not [var side, ..] || planner().CardSetup(state, new HashSet<string>()) is not { CurrentOrder: null })
            {
                return;
            }

            var result = await setup(new
            {
                sniper = new
                {
                    side,
                    at = planner().SniperPlacements(state, side)[0].ToString()
                }
            });
            Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
        }
    }
}
