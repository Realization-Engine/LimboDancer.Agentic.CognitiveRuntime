namespace LimboDancer.Domains.Asl.Rules;

/// <summary>C2.21 (p. 167): select ordinary HE vs unarmored targets or AP vs armored targets, never a substitute round.</summary>
public static class ScenarioA1AmmunitionDefaultCalculator
{
    /// <summary>Decide the default component alone; an explicit declaration is validated by its own ammunition rules.</summary>
    public static AmmunitionDefaultVerdict SelectDefault(AmmunitionDefaultFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Declared is null)
        {
            return Unknown("Whether ammunition was explicitly declared is unknown.");
        }

        if (facts.Declared == true)
        {
            return new(AmmunitionDefaultDisposition.NotApplicable, null, "Explicit ammunition is not a default; its own legality still needs checking.", "C2.21", 167);
        }

        if (facts.WeaponKind is not (AmmunitionDefaultWeapon.Gun or AmmunitionDefaultWeapon.Mortar)
            || facts.TargetType is not (AmmunitionDefaultTargetType.Infantry or AmmunitionDefaultTargetType.Vehicle or AmmunitionDefaultTargetType.Area))
        {
            return Unknown("A supported weapon and Target Type must be known.");
        }

        if (facts.WeaponKind == AmmunitionDefaultWeapon.Mortar && facts.TargetType != AmmunitionDefaultTargetType.Area)
        {
            return Unknown("A mortar requires Area Target Type.", "C3.33", 169);
        }

        if (facts.TargetProtection is not (AmmunitionTargetProtection.Unarmored or AmmunitionTargetProtection.Armored))
        {
            return Unknown("The target's armored or unarmored status must be known.");
        }

        if (facts.TargetProtection == AmmunitionTargetProtection.Armored && facts.TargetType == AmmunitionDefaultTargetType.Infantry)
        {
            return Unknown("An armored target cannot use this Infantry Target Type combination.", "C3.32", 169);
        }

        var ammunition = facts.TargetProtection == AmmunitionTargetProtection.Armored ? DefaultAmmunition.Ap : DefaultAmmunition.He;
        if (ammunition == DefaultAmmunition.Ap && facts.TargetType == AmmunitionDefaultTargetType.Area)
        {
            return Declaration("The preferred AP round is not allowed on Area Target Type; declare a permitted round separately.", "C3.33", 169);
        }

        var stock = ammunition == DefaultAmmunition.Ap ? facts.Ap : facts.He;
        if (stock?.ListsDepletionNumber == true)
        {
            return Declaration("The preferred round has a listed Depletion Number and must be declared as Special Ammunition.", "C8.8", 179);
        }

        if (stock?.CounterProhibits == true)
        {
            return Declaration("The counter prohibits the preferred ordinary round; no substitute is assumed.");
        }

        if (stock?.Exhausted == true)
        {
            return Declaration("The preferred round is exhausted; no substitute is assumed.");
        }

        if (stock?.CounterProhibits is null || stock.ListsDepletionNumber is null || stock.Exhausted is null)
        {
            return Unknown("A required stock fact for the preferred round is unknown.");
        }

        return new(AmmunitionDefaultDisposition.DefaultSelected, ammunition,
            ammunition == DefaultAmmunition.He ? "Default HE against the unarmored target; other shot rules still apply."
                : "Default AP against the armored target; other shot rules still apply.", "C2.21", 167);
    }

    /// <summary>Map an authorized, known Rules definition without interpreting null exhaustion as false. No state or catalog lookup.</summary>
    public static (AmmunitionStockFacts Ap, AmmunitionStockFacts He) StockFacts(GunDefinition gun, bool? apExhausted, bool? heExhausted)
    {
        ArgumentNullException.ThrowIfNull(gun);
        return (new(gun.NoAp, ScenarioA1ArmorCalculator.Lists(gun, "ap"), apExhausted),
            new(gun.NoHe, ScenarioA1ArmorCalculator.Lists(gun, "he"), heExhausted));
    }

    /// <summary>Compatibility only: definition-only HE assumption. New callers use SelectDefault with protection, Target Type and stock.</summary>
    public static bool LegacyHeAssumed(GunDefinition gun) => !gun.NoHe && !ScenarioA1ArmorCalculator.Lists(gun, "he");

    /// <summary>Compatibility only, inside the old non-vehicle, non-Canister, non-equivalency, listed-HE guard. Empty is not null.</summary>
    public static bool LegacyHeNeedsDeclaration(string? ammunition) => ammunition is null;

    /// <summary>Compatibility only: the old duel accepts AP at every vehicle and unnamed HE otherwise, including unarmored vehicles.</summary>
    public static bool LegacyDuelAmmunitionMatches(bool vehicleTarget, string? ammunition) => ammunition == (vehicleTarget ? "ap" : null);

    private static AmmunitionDefaultVerdict Unknown(string reason, string rule = "C2.21", int page = 167) =>
        new(AmmunitionDefaultDisposition.NotEvaluated, null, reason, rule, page);

    private static AmmunitionDefaultVerdict Declaration(string reason, string rule = "C2.21", int page = 167) =>
        new(AmmunitionDefaultDisposition.DeclarationRequired, null, reason, rule, page);
}
