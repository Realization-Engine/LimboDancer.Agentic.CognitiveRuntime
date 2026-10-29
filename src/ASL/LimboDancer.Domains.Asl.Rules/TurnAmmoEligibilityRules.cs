namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// E10 participant contracts, decided by <see cref="ScenarioA1AmmoVehicleCalculator"/> (the E10 claim, RE10.1): each member forwards with the same
/// answer. Its records moved to ScenarioA1AmmoVehicleModels.cs unchanged.
/// </summary>
public static class TurnAmmoEligibilityRules
{
    /// <summary>E10.11, by <see cref="ScenarioA1AmmoVehicleCalculator.Selection"/>.</summary>
    public static TurnAmmoEligibilityVerdict SelectVehicle(TurnAmmoVehicleSelection facts) => ScenarioA1AmmoVehicleCalculator.Selection(facts);

    /// <summary>D.7, by <see cref="ScenarioA1AmmoVehicleCalculator.Immobile"/>.</summary>
    public static bool? Immobile(TurnAmmoMobility facts) => ScenarioA1AmmoVehicleCalculator.Immobile(facts);

    /// <summary>Same or Accessible, by <see cref="ScenarioA1AmmoVehicleCalculator.SameOrAccessible"/>.</summary>
    public static bool? SameOrAccessible(bool? sameLocation, bool? adjacent, bool? normalInfantryAdvanceIgnoringEnemy) =>
        ScenarioA1AmmoVehicleCalculator.SameOrAccessible(sameLocation, adjacent, normalInfantryAdvanceIgnoringEnemy);

    /// <summary>E10.2's identical armament, by <see cref="ScenarioA1AmmoVehicleCalculator.Compatible"/>.</summary>
    public static bool? Compatible(TurnAmmoArmament supply, TurnAmmoArmament recipient) => ScenarioA1AmmoVehicleCalculator.Compatible(supply, recipient);

    /// <summary>E10.2/.3's recipient set, by <see cref="ScenarioA1AmmoVehicleCalculator.Recipients"/>.</summary>
    public static TurnAmmoEligibilityVerdict EvaluateRecipients(TurnAmmoSupplyFacts supply,
        IReadOnlyList<TurnAmmoRecipientFacts> recipients, bool? completeRecipientInventory, TurnAmmoUse use, bool? notBoundingFire = null) =>
        ScenarioA1AmmoVehicleCalculator.Recipients(supply, recipients, completeRecipientInventory, use, notBoundingFire);
}
