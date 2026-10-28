namespace LimboDancer.Domains.Asl.Rules;

public enum TurnAmmoEligibility
{
    NotEvaluated, Eligible, Ineligible
}
public enum TurnAmmoBarrel
{
    Unknown, ShortBarrel, Standard, LongBarrel, ExtraLongBarrel
}
public enum TurnAmmoUse
{
    FireBenefit, Replenishment
}
public sealed record TurnAmmoArmament(decimal? CaliberMm, TurnAmmoBarrel Barrel);
public sealed record TurnAmmoMobility(bool? Bogged, bool? Mired, bool? Immobilized, bool? Abandoned,
    bool? InherentCrewBroken, bool? Shocked, bool? Stunned);
public sealed record TurnAmmoVehicleSelection(bool? Authorized, bool? IsVehicle, bool? RecipientHasLowAmmoBNumber,
    bool? HasSpecifiedVehicle, bool? MatchesSpecifiedVehicle, int? OrdinaryPassengerPp, decimal? RecipientCaliberMm);
public sealed record TurnAmmoSupplyFacts(string? Id, string? Owner, bool? Active, bool? Authorized, bool? Available,
    TurnAmmoArmament Ammunition);
public sealed record TurnAmmoRecipientFacts(string? Id, string? Owner, bool? Active, bool? IsVehicle,
    bool? HasLowAmmoBNumber, bool? LowAmmo, bool? CrewExposed, bool? SameOrAccessibleLocation, TurnAmmoArmament Armament);
public sealed record TurnAmmoEligibilityVerdict(TurnAmmoEligibility State, string Reason)
{
    public string Reference { get; } = "E10.1-.4; D.7; Accessible glossary; ASOP 8.43B";
}

/// <summary>E10 participant contracts. Hosts must prove these facts and bind assignments, timing and commands; no effects are executed here.</summary>
public static class TurnAmmoEligibilityRules
{
    /// <summary>E10.11 uses ordinary capacity before the Ammo Supply counter reduces usable capacity to zero.</summary>
    public static TurnAmmoEligibilityVerdict SelectVehicle(TurnAmmoVehicleSelection facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentOutOfRangeException.ThrowIfNegative(facts.OrdinaryPassengerPp ?? 0);
        ValidateCaliber(facts.RecipientCaliberMm);
        bool? capacity = facts.OrdinaryPassengerPp switch
        {
            null => null,
            < 15 => false,
            >= 20 => true,
            _ => facts.RecipientCaliberMm is null ? null : facts.RecipientCaliberMm < 100
        };
        var selection = facts.HasSpecifiedVehicle switch
        {
            true => facts.MatchesSpecifiedVehicle,
            false => capacity,
            null => null
        };
        return Assess([facts.Authorized, facts.IsVehicle, facts.RecipientHasLowAmmoBNumber, selection]);
    }

    /// <summary>D.7: TI, Prep Fire and radioless movement restrictions alone do not make a vehicle Immobile.</summary>
    public static bool? Immobile(TurnAmmoMobility facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        bool?[] conditions = [facts.Bogged, facts.Mired, facts.Immobilized, facts.Abandoned,
            facts.InherentCrewBroken, facts.Shocked, facts.Stunned];
        return conditions.Any(value => value == true) ? true : conditions.Any(value => value is null) ? null : false;
    }

    /// <summary>Accessible is an adjacent Location reachable by hypothetical Infantry in a normal APh, ignoring enemy presence. Same Location is separately sufficient.</summary>
    public static bool? SameOrAccessible(bool? sameLocation, bool? adjacent, bool? normalInfantryAdvanceIgnoringEnemy)
    {
        var accessible = Both(adjacent, normalInfantryAdvanceIgnoringEnemy);
        return sameLocation == true || accessible == true ? true
            : sameLocation == false && accessible == false ? false : null;
    }

    public static bool? Compatible(TurnAmmoArmament supply, TurnAmmoArmament recipient)
    {
        ValidateArmament(supply);
        ValidateArmament(recipient);
        bool? caliber = supply.CaliberMm is null || recipient.CaliberMm is null ? null : supply.CaliberMm == recipient.CaliberMm;
        bool? barrel = supply.Barrel == TurnAmmoBarrel.Unknown || recipient.Barrel == TurnAmmoBarrel.Unknown ? null : supply.Barrel == recipient.Barrel;
        return Both(caliber, barrel);
    }

    /// <summary>E10.2/.3 complete simultaneous recipient set. A partial inventory cannot establish the two-recipient limit.</summary>
    public static TurnAmmoEligibilityVerdict EvaluateRecipients(TurnAmmoSupplyFacts supply,
        IReadOnlyList<TurnAmmoRecipientFacts> recipients, bool? completeRecipientInventory, TurnAmmoUse use, bool? notBoundingFire = null)
    {
        ArgumentNullException.ThrowIfNull(supply);
        ArgumentNullException.ThrowIfNull(recipients);
        ValidateArmament(supply.Ammunition);
        if (!Enum.IsDefined(use))
        {
            throw new ArgumentException("Unknown ammunition use.", nameof(use));
        }
        if (recipients.Count is < 1 or > 2)
        {
            return new(TurnAmmoEligibility.Ineligible, "An E10 use needs one or two recipients, never more than two simultaneously.");
        }
        List<bool?> requirements = [supply.Active, supply.Authorized, supply.Available,
            KnownIdentity(supply.Id), KnownIdentity(supply.Owner), completeRecipientInventory == true ? true : null,
            use == TurnAmmoUse.FireBenefit ? notBoundingFire : true];
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var recipient in recipients)
        {
            ArgumentNullException.ThrowIfNull(recipient);
            requirements.AddRange([KnownIdentity(recipient.Id), KnownIdentity(recipient.Owner),
                string.IsNullOrWhiteSpace(recipient.Id) ? null : ids.Add(recipient.Id) && recipient.Id != supply.Id,
                string.IsNullOrWhiteSpace(recipient.Owner) || string.IsNullOrWhiteSpace(supply.Owner) ? null : recipient.Owner == supply.Owner,
                recipient.Active, recipient.IsVehicle, recipient.HasLowAmmoBNumber, recipient.CrewExposed,
                recipient.SameOrAccessibleLocation, Compatible(supply.Ammunition, recipient.Armament),
                use == TurnAmmoUse.Replenishment ? recipient.LowAmmo : true]);
        }
        return Assess(requirements);
    }

    private static TurnAmmoEligibilityVerdict Assess(IEnumerable<bool?> requirements)
    {
        var values = requirements.ToArray();
        return values.Any(value => value == false)
            ? new(TurnAmmoEligibility.Ineligible, "A proven E10 participant, compatibility, capacity, posture or use requirement is not met.")
            : values.Any(value => value is null)
                ? new(TurnAmmoEligibility.NotEvaluated, "Supply the missing E10 identity, source, participant and complete-assignment facts.")
                : new(TurnAmmoEligibility.Eligible, "The supplied E10 participant facts qualify; timing, live assignments, command authority and consequences remain separate checks.");
    }

    private static bool? KnownIdentity(string? value) => string.IsNullOrWhiteSpace(value) ? null : true;
    private static bool? Both(bool? first, bool? second) => first == false || second == false ? false : first == true && second == true ? true : null;
    private static void ValidateCaliber(decimal? caliber)
    {
        if (caliber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(caliber));
        }
    }
    private static void ValidateArmament(TurnAmmoArmament armament)
    {
        ArgumentNullException.ThrowIfNull(armament);
        ValidateCaliber(armament.CaliberMm);
        if (!Enum.IsDefined(armament.Barrel))
        {
            throw new ArgumentException("Unknown barrel designation.", nameof(armament));
        }
    }
}
