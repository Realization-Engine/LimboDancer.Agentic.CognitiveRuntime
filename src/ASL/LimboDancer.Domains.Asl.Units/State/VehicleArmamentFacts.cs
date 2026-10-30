using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>Source and state gathering shared by vehicle repair proposals, owner choices, and replay. All verdicts belong to Rules.</summary>
public static class VehicleArmamentFacts
{
    public sealed record Weapon(string Id, string RepairKind, string Malfunction, string Disabled, bool Secondary = false);

    public static IReadOnlyList<Weapon> Weapons(UnitDefinition definition)
    {
        var weapons = new List<Weapon>();
        var main = Text(definition, "ma-weapon");
        if (main is not null || Number(definition, "caliber") is not null)
        {
            weapons.Add(new("ma", main == "aamg" ? "aamg" : "ma", Conditions.Malfunctioned, Conditions.Disabled));
        }
        if (Number(definition, "bmg") is not null && main != "bmg")
        {
            weapons.Add(new("bmg", "bmg", Conditions.BmgMalfunctioned, Conditions.BmgDisabled));
        }
        if (Number(definition, "cmg") is not null && main != "cmg")
        {
            weapons.Add(new("cmg", "cmg", Conditions.CmgMalfunctioned, Conditions.CmgDisabled));
        }
        if (Number(definition, "aamg") is not null && main != "aamg")
        {
            weapons.Add(new("aamg", "aamg", Conditions.AamgMalfunctioned, Conditions.AamgDisabled));
        }
        if (HasSecondary(definition) == true)
        {
            weapons.Add(new("sa", "sa", Conditions.SaMalfunctioned, Conditions.SaDisabled, true));
        }
        return weapons;
    }

    public static VehicleRepairUseFacts Repair(GameState state, UnitInstance vehicle, UnitDefinition definition, Weapon weapon, string? heroId)
    {
        var hero = heroId is null ? null : state.Unit(heroId);
        var contained = hero?.Position as ContainedPosition;
        var heroFacts = heroId is null ? null : new VehicleRepairHeroFacts(heroId, hero?.Status == InstanceStatus.Active,
            hero?.Kind == "asl:hero" || hero is not null && Marked(hero, Conditions.Heroic), hero?.Side == vehicle.Side,
            contained?.Container, contained?.Role == ContainmentRole.Rider, hero is not null && Marked(hero, Conditions.Broken),
            hero is not null && Marked(hero, Conditions.Captured));
        var facts = new VehicleRepairFacts(vehicle.Id, weapon.RepairKind, Marked(vehicle, weapon.Malfunction), Marked(vehicle, weapon.Disabled),
            ScenarioA1RallyRules.VehicleCrewImpaired(Marked(vehicle, Conditions.Stunned), Marked(vehicle, Conditions.Shocked),
                Marked(vehicle, Conditions.UnconfirmedKill), Marked(vehicle, Conditions.Recalled), Marked(vehicle, Conditions.StunRecovery),
                Marked(vehicle, Conditions.Abandoned)),
            state.RepairsThisPhase.Contains(vehicle.Id + ":" + weapon.Id, StringComparer.Ordinal)
                || weapon.Id == "ma" && weapon.RepairKind == "aamg" && state.RepairsThisPhase.Contains(vehicle.Id, StringComparer.Ordinal),
            CrewExposed(vehicle, definition));
        return new(facts, true, weapon.Secondary, heroFacts);
    }

    public static bool CrewExposed(UnitInstance vehicle, UnitDefinition definition)
    {
        var afv = ScenarioA1VehicleMovementCalculator.IsAfv(vehicle.Kind == "asl:vehicle", Trait(definition, "unarmored"));
        return ScenarioA1CarrierRules.CrewExposed(Text(definition, "vehicle-class"), ScenarioA1FireEligibility.CrewExposed(new(
            GameState.Condition(vehicle, Conditions.ButtonedUp) switch
            {
                ConditionState.True => RuleState.True,
                ConditionState.False => RuleState.False,
                _ => RuleState.Unknown,
            }, Marked(vehicle, Conditions.Stunned), Marked(vehicle, Conditions.Recalled), Marked(vehicle, Conditions.Shocked),
            Marked(vehicle, Conditions.UnconfirmedKill), ScenarioA1VehicleMovementCalculator.IsClosedTopped(afv, Trait(definition, "open-topped")))));
    }

    public static string? RecallFactsRefusal(UnitDefinition definition) => HasSecondary(definition) is null
        || Capacity(definition, "passenger-capacity") is null || Capacity(definition, "towing") is null
        ? "play.vehicle-armament-facts: Secondary Armament and Passenger/Towing capability must be known (D3.7)" : null;

    public static bool RecalledAfterDisable(UnitInstance vehicle, UnitDefinition definition, Weapon disabled) =>
        RecalledAfterDisable(vehicle, disabled.Id == "ma" || Marked(vehicle, Conditions.Disabled), HasSecondary(definition),
            Capacity(definition, "passenger-capacity"), Capacity(definition, "towing"), disabled.Secondary);

    public static bool RecalledAfterDisable(UnitInstance vehicle, bool maDisabled, bool? hasSecondary, int? passengers, int? towing,
        bool secondaryJustDisabled = false) => hasSecondary is { } secondary && passengers is { } passengerCapacity && towing is { } towingCapacity
        && ScenarioA1VehicleBreakdownResolutionCalculator.Recalled(maDisabled, secondary,
            secondary ? secondaryJustDisabled ? true : GameState.Condition(vehicle, Conditions.SaDisabled) switch
            {
                ConditionState.True => true,
                ConditionState.False => false,
                _ => (bool?)null,
            } : null, passengerCapacity, towingCapacity);

    public static bool? HasSecondary(UnitDefinition definition) => (definition.Printed("front", "asl:sa-mount"), definition.Printed("front", "asl:sa-caliber")) switch
    {
        ({ State: PrintedState.NotPrinted }, { State: PrintedState.NotPrinted }) => false,
        ({ State: PrintedState.Printed, Value: not null }, { State: PrintedState.Printed, Value: not null }) => true,
        _ => null,
    };

    private static int? Capacity(UnitDefinition definition, string name) => definition.Printed("front", "asl:" + name) switch
    {
        { State: PrintedState.NotPrinted } => 0,
        { State: PrintedState.Printed, Value.Number: { } number } => number,
        _ => null,
    };

    private static string? Text(UnitDefinition definition, string name) => definition.Printed("front", "asl:" + name) is { State: PrintedState.Printed } value ? value.Value?.Text : null;
    private static int? Number(UnitDefinition definition, string name) => definition.Printed("front", "asl:" + name) is { State: PrintedState.Printed } value ? value.Value?.Number : null;
    private static bool? Trait(UnitDefinition definition, string name) => definition.Printed("front", "asl:" + name) is { State: PrintedState.Printed } value ? value.TraitPresent : null;
    private static bool Marked(UnitInstance vehicle, string condition) => GameState.Condition(vehicle, condition) == ConditionState.True;
}
