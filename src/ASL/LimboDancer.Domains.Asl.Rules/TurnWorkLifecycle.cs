namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Adjudicated facts, not engine certification of packing or supply eligibility.</summary>
public sealed record TurnWorkDeclaration(string Id, string Owner, string Subject, string[] Participants,
    TurnWorkKind Kind, int? OriginalBNumber, int? PackingGameTurns, string RuleBasis)
{
    public TurnWorkEffectSpec? CompletionEffects
    {
        get; init;
    }
}
public sealed record TurnLiveWork(TurnWorkDeclaration Declaration, int RequiredGameTurns, int CompletedGameTurns,
    int? OpenGameTurn, bool Working, bool Completed, bool Cancelled, bool SupplyDepleted)
{
    public bool EffectsApplied
    {
        get; init;
    }
}

/// <summary>E10.3-.4 and the G10 first-RPh release contract. Facts outside this bounded lifecycle remain adjudicated.</summary>
public static class TurnWorkLifecycle
{
    public static TurnLiveWork Start(TurnWorkDeclaration declaration, int turn, bool firstRally)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        if (!firstRally || turn < 1 || string.IsNullOrWhiteSpace(declaration.Id) || string.IsNullOrWhiteSpace(declaration.Owner)
            || string.IsNullOrWhiteSpace(declaration.Subject) || string.IsNullOrWhiteSpace(declaration.RuleBasis)
            || declaration.Participants is null || declaration.Participants.Length < (declaration.CompletionEffects?.Kind == TurnWorkEffectKind.OffloadSupply ? 1 : 2)
            || declaration.Participants.Any(string.IsNullOrWhiteSpace)
            || declaration.Participants.Distinct(StringComparer.Ordinal).Count() != declaration.Participants.Length
            || !declaration.Participants.Contains(declaration.Subject, StringComparer.Ordinal))
        {
            throw new InvalidDataException("Work requires a first-RPh declaration, distinct participants and an adjudicated rule basis.");
        }
        TurnWorkEffects.Validate(declaration);
        var required = TurnWorkRules.Evaluate(declaration.Kind, declaration.OriginalBNumber, 0, true, true, TurnWorkWindow.FirstRally).RequiredGameTurns;
        if (declaration.Kind == TurnWorkKind.AnimalPacking)
        {
            required = declaration.PackingGameTurns;
        }
        if (required is null or < 1 || declaration.Kind != TurnWorkKind.AnimalPacking && declaration.PackingGameTurns is not null)
        {
            throw new InvalidDataException("The work duration is unresolved or conflicts with its Rules-owned duration.");
        }
        return new(declaration, required.Value, 0, turn, true, false, false, false);
    }

    public static bool CompletionDue(TurnLiveWork work, int turn, TurnWorkWindow window) =>
        work.Working && !work.Completed && !work.Cancelled &&
        (work.Declaration.Kind == TurnWorkKind.AnimalPacking
            ? window == TurnWorkWindow.FirstRally && work.CompletedGameTurns >= work.RequiredGameTurns
            : window == TurnWorkWindow.SecondCloseCombat && work.OpenGameTurn == turn
                && work.CompletedGameTurns + 1 >= work.RequiredGameTurns);

    public static TurnLiveWork Change(TurnLiveWork work, string operation, int turn, TurnWorkWindow window, int? ammunitionDr = null)
    {
        ArgumentNullException.ThrowIfNull(work);
        if (work.Completed || work.Cancelled)
        {
            throw new InvalidDataException("Work has already ended.");
        }
        switch (operation)
        {
            case "interrupt" when work.Working && ammunitionDr is null:
                return work with
                {
                    Working = false,
                    OpenGameTurn = null
                };
            case "cancel" when ammunitionDr is null:
                return work with
                {
                    Working = false,
                    OpenGameTurn = null,
                    Cancelled = true
                };
            case "resume" when !work.Working && window == TurnWorkWindow.FirstRally && ammunitionDr is null:
                return work with
                {
                    Working = true,
                    OpenGameTurn = turn
                };
            case "complete" when CompletionDue(work, turn, window):
                if (work.Declaration.Kind == TurnWorkKind.AmmoReplenishment ? ammunitionDr is not (>= 2 and <= 12) : ammunitionDr is not null)
                {
                    throw new InvalidDataException("Replenishment requires its recorded Ammunition DR; other work takes no DR.");
                }
                return work with
                {
                    Working = false,
                    OpenGameTurn = null,
                    Completed = true,
                    CompletedGameTurns = work.Declaration.Kind == TurnWorkKind.AnimalPacking ? work.CompletedGameTurns : work.CompletedGameTurns + 1,
                    SupplyDepleted = ammunitionDr == 12
                };
            default:
                throw new InvalidDataException("The work transition is not available in this window.");
        }
    }

    public static TurnLiveWork NextGameTurn(TurnLiveWork work, int previousTurn, int nextTurn)
    {
        if (!work.Working || work.Completed || work.Cancelled)
        {
            return work;
        }
        if (nextTurn != previousTurn + 1 || work.OpenGameTurn != previousTurn)
        {
            throw new InvalidDataException("A complete Game Turn needs consecutive first-RPh and second-CCPh boundaries.");
        }
        return work with
        {
            CompletedGameTurns = work.CompletedGameTurns + 1,
            OpenGameTurn = nextTurn
        };
    }
}
