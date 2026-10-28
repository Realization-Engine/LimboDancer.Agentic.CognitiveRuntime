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
    public static TurnPackingVerdict Evaluate(TurnPackingFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!Enum.IsDefined(facts.Weapon))
        {
            throw new ArgumentException("Unknown packing weapon class.", nameof(facts));
        }
        bool?[] required = [facts.FirstRallyWorkWindow, facts.SameLocationAndTerrain, facts.MuleDesignated,
            facts.SquadMule, facts.MuleAvailableForSubject, facts.GoodOrder, facts.Unpinned, facts.NonPrc,
            facts.DismantledOrLimberedIfPossible,
            facts.Weapon switch { TurnPackingWeapon.Unknown => null, TurnPackingWeapon.Gun or TurnPackingWeapon.OtherMortar => facts.GunAllowedByNoteOrDyo, _ => true },
            facts.Weapon == TurnPackingWeapon.Unknown ? null : IsSupportWeapon(facts.Weapon) ? facts.QualifiedMmcForSw : Both(facts.CrewCounter, facts.SameNationality)];
        if (required.Any(item => item == false))
        {
            return new(TurnPackingEligibility.Ineligible, "An established participant, weapon, terrain, assembly or first-RPh requirement is not met (G10). Missing-Mule recovery after G10.4 requires its separate proof.");
        }
        return required.Any(item => item is null)
            ? new(TurnPackingEligibility.NotEvaluated, "Supply all G10 source and participant facts; unknowns are not eligibility.")
            : new(TurnPackingEligibility.Eligible, "Ordinary G10 (Un)Packing requirements are proven; the host must still bind the actors, opening and governed command.");
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
