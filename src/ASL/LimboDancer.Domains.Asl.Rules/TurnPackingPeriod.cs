namespace LimboDancer.Domains.Asl.Rules;

public enum TurnPackingDirection
{
    Pack, Unpack
}
public sealed record TurnPackingActors(string Weapon, string Mule, string Personnel);
/// <summary>A proven ASOP 1.13B boundary. FirstRally refers to the Game Turn, not the owner's Player Turn.</summary>
public sealed record TurnPackingRally(int GameTurn, bool FirstRally);
public sealed record TurnPackingSpecification(string Id, string Owner, TurnPackingActors Actors,
    TurnPackingWeapon Weapon, int? ManhandlingNumber, bool Night);

/// <summary>Serializable G10 period state. Separate from legacy adjudicated work: interruption releases only Personnel Pack-TI.</summary>
public sealed record TurnPackingPeriod(TurnPackingSpecification Specification, TurnPackingDirection Direction,
    int RequiredGameTurns, int CompletedGameTurns, TurnPackingRally LastRally,
    bool FirstHalfCompleted, bool PersonnelPackTi, bool Loaded, TurnPackingRally? MidpointAt, bool Completed,
    TurnPackingRally? InterruptedAt)
{
    public bool WeaponAndMulePackTi => !Completed;
}

/// <summary>G10.3/.31/.33 period transitions. A host must prove actors and boundary facts and commit the resulting load/release effects atomically.</summary>
public static class TurnPackingPeriods
{
    public static TurnPackingPeriod Start(TurnPackingSpecification specification, TurnPackingDirection direction,
        TurnPackingRally rally, TurnPackingFacts facts)
    {
        ValidateSpecification(specification);
        ValidateRally(rally);
        if (!Enum.IsDefined(direction) || !rally.FirstRally)
        {
            throw new InvalidDataException("Packing starts at the first RPh work step with a known direction.");
        }
        RequireEligibility(specification, facts);
        var duration = TurnPackingRules.RequiredGameTurns(specification.Weapon, specification.ManhandlingNumber, specification.Night)
            ?? throw new InvalidDataException("Packing duration is unknown.");
        return new(specification, direction, duration, 0, rally, false, true,
            direction == TurnPackingDirection.Unpack, null, false, null);
    }

    public static bool MidpointDue(TurnPackingPeriod period) => period.PersonnelPackTi && !period.Completed && period.MidpointAt is null
        && period.CompletedGameTurns * 2L + (period.FirstHalfCompleted ? 1 : 0) >= period.RequiredGameTurns;

    public static bool CompletionDue(TurnPackingPeriod period) => period.PersonnelPackTi && !period.Completed
        && period.LastRally.FirstRally && period.CompletedGameTurns >= period.RequiredGameTurns;

    public static TurnPackingPeriod NextRally(TurnPackingPeriod period, TurnPackingRally rally)
    {
        RequireOpen(period);
        ValidateRally(rally);
        if (MidpointDue(period) || CompletionDue(period))
        {
            throw new InvalidDataException("Resolve the due packing midpoint or release before passing its work step.");
        }
        var next = period.LastRally.FirstRally
            ? new TurnPackingRally(period.LastRally.GameTurn, false)
            : new TurnPackingRally(checked(period.LastRally.GameTurn + 1), true);
        if (rally != next)
        {
            throw new InvalidDataException("Packing history needs consecutive first and second RPh boundaries.");
        }
        return period with
        {
            LastRally = rally,
            CompletedGameTurns = period.CompletedGameTurns + (rally.FirstRally && period.PersonnelPackTi && period.FirstHalfCompleted ? 1 : 0),
            FirstHalfCompleted = !rally.FirstRally && period.PersonnelPackTi,
        };
    }

    public static TurnPackingPeriod ApplyMidpoint(TurnPackingPeriod period)
    {
        RequireOpen(period);
        if (!MidpointDue(period))
        {
            throw new InvalidDataException("The G10.31 load/unload midpoint is not due.");
        }
        return period with
        {
            Loaded = period.Direction == TurnPackingDirection.Pack,
            MidpointAt = period.LastRally
        };
    }

    public static TurnPackingPeriod Interrupt(TurnPackingPeriod period)
    {
        RequireOpen(period);
        if (!period.PersonnelPackTi || MidpointDue(period) || CompletionDue(period))
        {
            throw new InvalidDataException("Packing is already interrupted or has an unresolved opening consequence.");
        }
        // G10.11: Gun/Mule stay Pack-TI. A completed physical midpoint is not undone by a later crew interruption.
        return period with
        {
            PersonnelPackTi = false,
            FirstHalfCompleted = false,
            InterruptedAt = period.LastRally
        };
    }

    public static TurnPackingPeriod Resume(TurnPackingPeriod period, TurnPackingFacts facts)
    {
        RequireOpen(period);
        if (period.PersonnelPackTi || !period.LastRally.FirstRally || period.InterruptedAt is not { } interruption
            || period.LastRally.GameTurn <= interruption.GameTurn)
        {
            throw new InvalidDataException("Resume the interrupted crew only at a later Game Turn's first RPh work step.");
        }
        RequireEligibility(period.Specification, facts);
        return period with
        {
            PersonnelPackTi = true
        };
    }

    public static TurnPackingPeriod Reverse(TurnPackingPeriod period)
    {
        RequireOpen(period);
        if (!period.PersonnelPackTi || !period.LastRally.FirstRally || MidpointDue(period) || CompletionDue(period))
        {
            throw new InvalidDataException("Voluntary reversal needs all participants Pack-TI at a first RPh work step before completion.");
        }
        return period with
        {
            Direction = period.Direction == TurnPackingDirection.Pack ? TurnPackingDirection.Unpack : TurnPackingDirection.Pack,
            RequiredGameTurns = TurnPackingRules.ReversalGameTurns(period.CompletedGameTurns, period.RequiredGameTurns, false),
            CompletedGameTurns = 0,
            FirstHalfCompleted = false,
            MidpointAt = null,
        };
    }

    public static TurnPackingPeriod Complete(TurnPackingPeriod period)
    {
        RequireOpen(period);
        if (!CompletionDue(period) || period.MidpointAt is null)
        {
            throw new InvalidDataException("Release requires the complete period, midpoint receipt and following first RPh work step.");
        }
        return period with
        {
            Completed = true,
            PersonnelPackTi = false
        };
    }

    private static void RequireEligibility(TurnPackingSpecification specification, TurnPackingFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Weapon != specification.Weapon || TurnPackingRules.Evaluate(facts).State != TurnPackingEligibility.Eligible)
        {
            throw new InvalidDataException("Prove matching weapon and participant eligibility before starting or resuming packing.");
        }
    }

    private static void ValidateSpecification(TurnPackingSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(specification.Actors);
        string[] identities = [specification.Actors.Weapon, specification.Actors.Mule, specification.Actors.Personnel];
        if (string.IsNullOrWhiteSpace(specification.Id) || string.IsNullOrWhiteSpace(specification.Owner)
            || identities.Any(string.IsNullOrWhiteSpace) || identities.Distinct(StringComparer.Ordinal).Count() != 3)
        {
            throw new InvalidDataException("Packing binds an owner and distinct weapon, Mule and Personnel identities.");
        }
        _ = TurnPackingRules.RequiredGameTurns(specification.Weapon, specification.ManhandlingNumber, specification.Night);
    }

    private static void ValidateRally(TurnPackingRally rally)
    {
        ArgumentNullException.ThrowIfNull(rally);
        ArgumentOutOfRangeException.ThrowIfLessThan(rally.GameTurn, 1);
    }

    private static void RequireOpen(TurnPackingPeriod period)
    {
        ArgumentNullException.ThrowIfNull(period);
        ValidateSpecification(period.Specification);
        ValidateRally(period.LastRally);
        if (period.Completed || !Enum.IsDefined(period.Direction) || period.RequiredGameTurns < 1 || period.CompletedGameTurns < 0
            || period.CompletedGameTurns > period.RequiredGameTurns || period.FirstHalfCompleted && (!period.PersonnelPackTi || period.LastRally.FirstRally))
        {
            throw new InvalidDataException("Packing period is terminal or inconsistent.");
        }
        if (period.InterruptedAt is { } interruption)
        {
            ValidateRally(interruption);
            if (interruption.GameTurn > period.LastRally.GameTurn
                || interruption.GameTurn == period.LastRally.GameTurn && !interruption.FirstRally && period.LastRally.FirstRally)
            {
                throw new InvalidDataException("Packing interruption cannot follow the current boundary.");
            }
        }
        if (period.MidpointAt is { } midpoint)
        {
            ValidateRally(midpoint);
            if (period.Loaded != (period.Direction == TurnPackingDirection.Pack))
            {
                throw new InvalidDataException("Recorded packing midpoint and physical load disagree.");
            }
            if (midpoint.GameTurn > period.LastRally.GameTurn
                || midpoint.GameTurn == period.LastRally.GameTurn && !midpoint.FirstRally && period.LastRally.FirstRally)
            {
                throw new InvalidDataException("Packing midpoint cannot follow the current boundary.");
            }
        }
    }
}
