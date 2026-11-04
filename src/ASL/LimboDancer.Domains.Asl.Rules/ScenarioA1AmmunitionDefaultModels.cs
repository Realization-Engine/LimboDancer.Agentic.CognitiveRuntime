namespace LimboDancer.Domains.Asl.Rules;

/// <summary>The weapon family for C2.21; fixed-ammunition LATW and MG are outside this selector.</summary>
public enum AmmunitionDefaultWeapon
{
    Gun, Mortar, Unsupported
}

/// <summary>Protection of the actual target, not merely whether it is a vehicle.</summary>
public enum AmmunitionTargetProtection
{
    Unarmored, Armored
}

/// <summary>The declared To Hit Target Type, distinct from target protection.</summary>
public enum AmmunitionDefaultTargetType
{
    Infantry, Vehicle, Area
}

/// <summary>Only ordinary AP and HE can be assumed under C2.21.</summary>
public enum DefaultAmmunition
{
    Ap, He
}

/// <summary>A default decision is never complete shot legality.</summary>
public enum AmmunitionDefaultDisposition
{
    DefaultSelected, DeclarationRequired, NotApplicable, NotEvaluated
}

/// <summary>Null is unknown. A listed AP#/HE# remains special ammunition even when its dated number is unavailable.</summary>
public sealed record AmmunitionStockFacts(bool? CounterProhibits = null, bool? ListsDepletionNumber = null, bool? Exhausted = null);

/// <summary>C2.21 facts only. Neither an absent declaration nor a missing stock fact implies a permitted shot.</summary>
public sealed record AmmunitionDefaultFacts(bool? Declared = null, AmmunitionDefaultWeapon? WeaponKind = null,
    AmmunitionTargetProtection? TargetProtection = null, AmmunitionDefaultTargetType? TargetType = null,
    AmmunitionStockFacts? Ap = null, AmmunitionStockFacts? He = null);

/// <summary>Only DefaultSelected carries ammunition. Other firing and declared-ammunition rules still apply.</summary>
public sealed record AmmunitionDefaultVerdict(AmmunitionDefaultDisposition Disposition, DefaultAmmunition? Ammunition,
    string Reason, string Rule, int Page);
