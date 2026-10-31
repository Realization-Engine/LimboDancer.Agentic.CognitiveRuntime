using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>Source and state gathering shared by vehicle repair proposals, owner choices, and replay. All verdicts belong to Rules.</summary>
public static class VehicleArmamentFacts
{
    private static readonly HashSet<string> EvidenceNames = new(StringComparer.Ordinal)
    {
        "asl:printed-breakdown", "asl:printed-elimination", "asl:breakdown-circled", "asl:breakdown-note-extends",
        "asl:sa-mount", "asl:sa-caliber", "asl:passenger-capacity", "asl:towing", "asl:ma-weapon", "asl:caliber",
        "asl:bmg", "asl:cmg", "asl:aamg", "asl:unarmored", "asl:open-topped", "asl:vehicle-class"
    };

    /// <summary>The catalog actually loaded for this state, with only a recorded breakdown/repair evidence override.</summary>
    public static UnitDefinition? Definition(GameState state, UnitInstance vehicle)
    {
        var definition = vehicle.Definition is { } reference ? state.VehicleArmamentCatalog?.Definition(reference.Definition) : null;
        return definition is not null && state.VehicleArmamentEvidence is { } evidence && evidence.Definition == definition.Id
            ? definition with
            {
                Values = evidence.Values
            } : definition;
    }

    public static VehicleArmamentEvidence? Evidence(GameState state, UnitInstance vehicle) => Definition(state, vehicle) is { } definition
        && state.VehicleArmamentCatalog is { } catalog
        ? new(1, catalog.Identity, definition.Id, [.. definition.Values.Where(value => value.Face == "front" && EvidenceNames.Contains(value.Name))]) : null;

    /// <summary>Evidence is authoritative for an earlier catalog. When its exact catalog is loaded, verify the source values too.</summary>
    public static string? EvidenceRefusal(GameState state, UnitInstance? vehicle, VehicleArmamentEvidence evidence)
    {
        if (evidence.Version != 1 || vehicle?.Definition is not { } reference || evidence.Catalog is null
            || evidence.Catalog.Catalog != state.Catalog.Catalog || evidence.Definition != reference.Definition
            || !Version.TryParse(evidence.Catalog.Version, out var recordedVersion) || !Version.TryParse(state.Catalog.Version, out var loadedVersion)
            || recordedVersion > loadedVersion || evidence.Catalog.Hash is not { Length: 64 } hash || !hash.All(char.IsAsciiHexDigit)
            || recordedVersion == loadedVersion && evidence.Catalog != state.Catalog
            || evidence.Values is null || evidence.Values.Any(value => value is null || value.Face != "front" || !EvidenceNames.Contains(value.Name))
            || evidence.Values.Select(value => value.Name).Distinct(StringComparer.Ordinal).Count() != evidence.Values.Count)
        {
            return "The vehicle armament evidence has an unknown version, source, definition, or duplicate value.";
        }
        if (state.VehicleArmamentCatalog?.Identity == evidence.Catalog && Evidence(state, vehicle) is { } current
            && JsonSerializer.Serialize(current.Values) != JsonSerializer.Serialize(evidence.Values))
        {
            return "The vehicle armament evidence conflicts with its loaded catalog.";
        }
        return null;
    }

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

/// <summary>A versioned snapshot of breakdown and repair source values only. Other rules continue to use the current catalog.</summary>
public sealed record VehicleArmamentEvidence(
    [property: JsonRequired] int Version,
    [property: JsonRequired] CatalogIdentity Catalog,
    [property: JsonRequired] string Definition,
    [property: JsonRequired] IReadOnlyList<PrintedValue> Values);
