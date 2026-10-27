namespace LimboDancer.Domains.Asl.Rules;

public enum TurnWorkKind
{
    AnimalPacking, AmmoReplenishment, AmmoTransfer
}
public enum TurnWorkWindow
{
    Unknown, FirstRally, SecondCloseCombat
}
public enum TurnWorkState
{
    NotEvaluated, InProgress, Interrupted, CompletionPending
}
public sealed record TurnWorkVerdict(int? RequiredGameTurns, TurnWorkState State);

/// <summary>E10.3-.4 complete Game Turns. This evaluates progress, never clears TI, rolls depletion or moves supply.</summary>
public static class TurnWorkRules
{
    public static TurnWorkVerdict Evaluate(TurnWorkKind kind, int? originalBNumber, int completedGameTurns,
        bool? eligible, bool originKnown, TurnWorkWindow window)
    {
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(window))
        {
            throw new ArgumentException("Unknown work kind or window.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(completedGameTurns);
        int? required = kind switch
        {
            TurnWorkKind.AmmoTransfer => 4,
            TurnWorkKind.AmmoReplenishment when originalBNumber is > 0 and <= 9 => 1,
            TurnWorkKind.AmmoReplenishment when originalBNumber == 10 => 2,
            TurnWorkKind.AmmoReplenishment when originalBNumber == 11 => 3,
            _ => null,
        };
        var state = !originKnown || eligible is null ? TurnWorkState.NotEvaluated
            : eligible == false ? TurnWorkState.Interrupted
            : required is null ? TurnWorkState.NotEvaluated
            : completedGameTurns >= required && window == TurnWorkWindow.SecondCloseCombat
                ? TurnWorkState.CompletionPending : TurnWorkState.InProgress;
        return new(required, state);
    }
}
