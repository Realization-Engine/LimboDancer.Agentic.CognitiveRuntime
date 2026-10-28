namespace LimboDancer.Domains.Asl.Rules;

public enum TurnPackingWeapon
{
    Unknown, Gun, Mortar76To82, OtherMortar, LightMortar, MediumMachineGun, HeavyMachineGun, InfantryGun37
}
public enum TurnPackingEligibility
{
    NotEvaluated, Eligible, Ineligible
}
public enum TurnPackingCrewActivity
{
    None, OtherTiProhibited, Repair, Assemble, Dismantle, Limber, Unlimber
}

/// <summary>G10 qualification facts to be proven from source and participants by a host. Eligibility is not an executable command.</summary>
public sealed record TurnPackingFacts(TurnPackingWeapon Weapon, bool? GunAllowedByNoteOrDyo,
    bool? FirstRallyWorkWindow, bool? SameLocationAndTerrain, bool? MuleDesignated, bool? SquadMule,
    bool? MuleAvailableForSubject, bool? CrewCounter, bool? SameNationality, bool? QualifiedMmcForSw,
    bool? GoodOrder, bool? Unpinned, bool? NonPrc, bool? DismantledOrLimberedIfPossible);
public sealed record TurnPackingVerdict(TurnPackingEligibility State, string Reason)
{
    public string Reference { get; } = "G10.1-.3; G10.61; G10.7; ASOP 1.13B";
}

/// <summary>G10 ordinary eligibility and period rules. Animal identities, midpoint actions, transport and vulnerability consequences are separate host work.</summary>
public static class TurnPackingRules
{
    public static TurnPackingVerdict Evaluate(TurnPackingFacts facts) => EvaluateCore(facts, false, true);

    /// <summary>G10.41 recovery of a surviving, already-unloaded weapon. The historical Mule may be a HS or eliminated; no loading permission is granted.</summary>
    public static TurnPackingVerdict EvaluateRecovery(TurnPackingFacts facts, TurnPackingCasualtyState casualty)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(casualty);
        if (!casualty.WeaponAlive || casualty.Loaded || casualty.Recovery != TurnPackingRecovery.FullOrdinaryPeriod
            || casualty.Weapon != facts.Weapon || casualty.MuleSize is not (TurnHorseSize.Squad or TurnHorseSize.HalfSquad)
            || casualty.MuleAlive && !casualty.MulePackTi)
        {
            return new(TurnPackingEligibility.Ineligible, "A full Unpacking recovery requires the surviving unloaded weapon and its G10.4 casualty history.");
        }
        return EvaluateCore(facts, true, casualty.MuleAlive);
    }

    private static TurnPackingVerdict EvaluateCore(TurnPackingFacts facts, bool recovery, bool muleAlive)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!Enum.IsDefined(facts.Weapon))
        {
            throw new ArgumentException("Unknown packing weapon class.", nameof(facts));
        }
        bool?[] required = [facts.FirstRallyWorkWindow, facts.SameLocationAndTerrain, recovery && !muleAlive ? true : facts.MuleDesignated,
            recovery ? true : facts.SquadMule, recovery ? true : facts.MuleAvailableForSubject, facts.GoodOrder, facts.Unpinned, facts.NonPrc,
            recovery ? true : facts.DismantledOrLimberedIfPossible,
            facts.Weapon switch { TurnPackingWeapon.Unknown => null, TurnPackingWeapon.Gun or TurnPackingWeapon.OtherMortar => facts.GunAllowedByNoteOrDyo, _ => true },
            facts.Weapon == TurnPackingWeapon.Unknown ? null : IsSupportWeapon(facts.Weapon) ? facts.QualifiedMmcForSw : Both(facts.CrewCounter, facts.SameNationality)];
        if (required.Any(item => item == false))
        {
            return new(TurnPackingEligibility.Ineligible, "An established participant, weapon, terrain, assembly or first-RPh requirement is not met (G10). Missing-Mule recovery after G10.4 requires its separate proof.");
        }
        return required.Any(item => item is null)
            ? new(TurnPackingEligibility.NotEvaluated, "Supply all G10 source and participant facts; unknowns are not eligibility.")
            : new(TurnPackingEligibility.Eligible, recovery
                ? "G10.41 Unpacking recovery facts are proven; the host must bind surviving actors, casualty and the first-RPh work step."
                : "Ordinary G10 (Un)Packing requirements are proven; the host must still bind the actors, opening and governed command.");
    }

    /// <summary>Classify known counter facts for G10.2/.7. Gun permission, DYO and qualified use remain separate facts.</summary>
    public static TurnPackingWeapon ClassifyWeapon(string? kind, string? gunType, int? caliber, string? mgSize) => kind switch
    {
        "asl:light-mortar" => TurnPackingWeapon.LightMortar,
        "asl:mg" => mgSize switch { "medium" => TurnPackingWeapon.MediumMachineGun, "heavy" => TurnPackingWeapon.HeavyMachineGun, _ => TurnPackingWeapon.Unknown },
        "asl:gun" when gunType == "mtr" => caliber is null ? TurnPackingWeapon.Unknown
            : caliber is >= 76 and <= 82 ? TurnPackingWeapon.Mortar76To82 : TurnPackingWeapon.OtherMortar,
        "asl:gun" when gunType is "at" or "inf" or "art" or "aa" or "rcl" => TurnPackingWeapon.Gun,
        _ => TurnPackingWeapon.Unknown
    };

    /// <summary>G10.1/.2/.7 structural load requirements. This neither authorizes loading nor proves assembly, accompaniment or an ASOP work step.</summary>
    public static string? LoadRefusal(TurnPackingWeapon weapon, bool? gunAllowedByNoteOrDyo,
        TurnHorseIdentity? mule, bool soleLoad, bool sameOwner, bool separatelyHeld)
    {
        if (mule is not { Size: TurnHorseSize.Squad, Mule: true })
        {
            return "An Animal-Pack load requires a designated squad Mule (G10.1-.2).";
        }
        if (!soleLoad || !sameOwner || separatelyHeld)
        {
            return "A Mule carries one own Pack Gun or listed SW, without a separate holding relationship (G10.2/.7).";
        }
        return weapon switch
        {
            TurnPackingWeapon.Mortar76To82 or TurnPackingWeapon.LightMortar or TurnPackingWeapon.MediumMachineGun
                or TurnPackingWeapon.HeavyMachineGun or TurnPackingWeapon.InfantryGun37 => null,
            TurnPackingWeapon.Gun or TurnPackingWeapon.OtherMortar when gunAllowedByNoteOrDyo == true => null,
            _ => "The weapon's Animal-Pack capability must be established by G10.2/.61/.7 or an authoritative note."
        };
    }

    /// <summary>Combine the host's disclosed actor binding with the rule facts; a missing actor is unknown, not a false permission.</summary>
    public static TurnPackingVerdict EvaluateActors(TurnPackingFacts facts, bool? actorsBound)
    {
        var verdict = Evaluate(facts);
        if (actorsBound == false)
        {
            return new(TurnPackingEligibility.Ineligible, "Packing needs distinct active own weapon, Mule and Personnel actors with the required possession (G10.2-.3).");
        }
        return actorsBound is null && verdict.State != TurnPackingEligibility.Ineligible
            ? new(TurnPackingEligibility.NotEvaluated, "One or more actor identities, roles, owners or possession facts are unavailable to this viewer.") : verdict;
    }

    public static int? RequiredGameTurns(TurnPackingWeapon weapon, int? manhandlingNumber, bool night)
    {
        if (!Enum.IsDefined(weapon))
        {
            throw new ArgumentException("Unknown packing weapon class.", nameof(weapon));
        }
        if (manhandlingNumber < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(manhandlingNumber));
        }
        if (weapon == TurnPackingWeapon.Unknown)
        {
            return null;
        }
        var number = IsSupportWeapon(weapon) ? 12 : manhandlingNumber;
        if (number is null)
        {
            return null;
        }
        var mortar = weapon is TurnPackingWeapon.Mortar76To82 or TurnPackingWeapon.OtherMortar or TurnPackingWeapon.LightMortar;
        var normal = Math.Max(1, 12 - number.Value + (mortar ? 0 : 1));
        return normal * (night ? 2 : 1);
    }

    /// <summary>G10.11/.32: only actual activity is assessed here; illegal loaded-Gun operations must first be refused by their own commands.</summary>
    public static bool CrewRetainsPackTi(bool packTi, bool goodOrder, bool unpinned, TurnPackingCrewActivity activity, bool gunLoaded)
    {
        if (!Enum.IsDefined(activity))
        {
            throw new ArgumentException("Unknown packing crew activity.", nameof(activity));
        }
        return packTi && goodOrder && unpinned && (activity == TurnPackingCrewActivity.None
            || !gunLoaded && activity is TurnPackingCrewActivity.Repair or TurnPackingCrewActivity.Assemble
                or TurnPackingCrewActivity.Dismantle or TurnPackingCrewActivity.Limber or TurnPackingCrewActivity.Unlimber);
    }

    /// <summary>G10.33: reversal uses net completed Game Turns without doubling them again at night. Involuntary unloading instead restarts a full G10.3 period.</summary>
    public static int ReversalGameTurns(int netCompletedGameTurns, int ordinaryRequiredGameTurns, bool involuntarilyUnloaded)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(netCompletedGameTurns);
        ArgumentOutOfRangeException.ThrowIfLessThan(ordinaryRequiredGameTurns, 1);
        return involuntarilyUnloaded ? ordinaryRequiredGameTurns : Math.Max(1, netCompletedGameTurns);
    }

    private static bool IsSupportWeapon(TurnPackingWeapon weapon) => weapon is TurnPackingWeapon.LightMortar
        or TurnPackingWeapon.MediumMachineGun or TurnPackingWeapon.HeavyMachineGun or TurnPackingWeapon.InfantryGun37;
    private static bool? Both(bool? first, bool? second) => first == false || second == false ? false : first == true && second == true ? true : null;
}
