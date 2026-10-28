using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

public static partial class GameProjector
{
    internal sealed partial class Replay
    {
        private GameState? RecordPlacedDcWindow(GameState state, PlacedDcWindowReviewed payload, string type)
        {
            var review = payload.Review;
            var charge = state.PlacedCharges.SingleOrDefault(item => item.Charge == review?.Charge);
            if (type != "placed-dc-window-reviewed" || review is null || charge is null
                || !TurnPlacedDcWindowRules.Matches(PlacedDcWindowFacts.Read(state, charge), review))
            {
                return Fail<GameState>("UNIT-WINDOW-001", "Invalid Placed DC window adjudication, occurrence, owner or origin.");
            }
            return state with
            {
                PlacedDcWindowReview = review
            };
        }
    }
}

public static class PlacedDcWindowFacts
{
    public static TurnPlacedDcWindowFacts Read(GameState state, PlacedCharge charge) =>
        new(state.TurnPhaseOccurrence, charge.Charge, charge.OriginEventId, charge.OriginGameTurn,
            charge.OwnerSide, charge.Operable, state.PlacedDcWindowReview);
}
