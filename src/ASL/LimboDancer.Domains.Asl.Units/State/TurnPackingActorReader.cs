using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>Revision-bound, disclosed candidate facts. Missing timing, terrain or load evidence cannot become execution permission.</summary>
public sealed record TurnPackingActorRead(StateStamp Stamp, TurnPackingActors Actors, string? Owner,
    TurnPackingWeapon Weapon, int? ManhandlingNumber, bool Night, bool? ActorsBound,
    TurnPackingFacts Facts, TurnPackingVerdict Verdict, int? RequiredGameTurns)
{
    /// <summary>Disclosed physical load only. Null does not establish that the weapon is unloaded.</summary>
    public string? LoadedOnMule
    {
        get; init;
    }
}

/// <summary>Reads G10 actor facts from actual visible counters and pinned catalog definitions. No writes, openings, load state or adjudications are invented.</summary>
public static class TurnPackingActorReader
{
    public static TurnPackingActorRead Read(GameState state, Perspective perspective, TurnPackingActors actors,
        UnitVocabulary vocabulary, IReadOnlyList<UnitCatalog> catalogs)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(actors);
        ArgumentNullException.ThrowIfNull(vocabulary);
        ArgumentNullException.ThrowIfNull(catalogs);
        var view = GameView.Of(state, perspective, []);
        IGameObject[] shown = [.. view.Units, .. view.Equipment, .. view.Entities];
        var weaponObject = shown.FirstOrDefault(item => item.Id == actors.Weapon);
        var muleObject = shown.FirstOrDefault(item => item.Id == actors.Mule);
        var personnelObject = shown.FirstOrDefault(item => item.Id == actors.Personnel);
        var weapon = weaponObject as EquipmentInstance;
        var mule = muleObject as EntityInstance;
        var personnel = personnelObject as UnitInstance;
        var weaponDefinition = Definition(weapon?.Definition, weapon?.Kind, state.Synthetic, catalogs);
        var personnelDefinition = Definition(personnel?.Definition, personnel?.Kind, state.Synthetic, catalogs);
        var weaponClass = weaponDefinition is null ? TurnPackingWeapon.Unknown : TurnPackingRules.ClassifyWeapon(weapon?.Kind,
            Text(weaponDefinition, "asl:gun-type"), weaponDefinition.Printed("front", "asl:caliber") is { State: PrintedState.Printed } caliber ? caliber.Value?.Number : null,
            Text(weaponDefinition, "asl:size"));
        var number = weaponDefinition?.Printed("front", "asl:manhandling") is { State: PrintedState.Printed, Value.Number: { } m } ? m : (int?)null;
        var ids = new[] { actors.Weapon, actors.Mule, actors.Personnel };
        bool? bound = ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct(StringComparer.Ordinal).Count() != 3 ? false
            : weaponObject is null || muleObject is null || personnelObject is null ? null
            : weapon is not null && mule is { Kind: "asl:horse" } && personnel is not null && vocabulary.IsA(personnel.Kind, "asl:personnel")
                && weapon.Status == InstanceStatus.Active && mule.Status == InstanceStatus.Active && personnel.Status == InstanceStatus.Active
                && weapon.Side is not null && weapon.Side == mule.Side && weapon.Side == personnel.Side;
        var loadedOn = weapon?.Position is ContainedPosition { Role: ContainmentRole.AnimalPack } load ? load.Container : null;
        bool? available = mule is { Kind: "asl:horse" }
            && shown.Any(item => item.Id != actors.Weapon && item.Position is ContainedPosition { Role: ContainmentRole.AnimalPack } other && other.Container == actors.Mule)
                ? false : loadedOn == actors.Mule && mule is { Kind: "asl:horse" } ? true : null;
        // Only the owning side/adjudicator has the complete load inventory. Unbound legacy packing is not evidence of an empty Mule.
        if (available is null && mule is { Kind: "asl:horse", Status: InstanceStatus.Active, Side: not null }
            && weapon is { Status: InstanceStatus.Active } && weapon.Side == mule.Side
            && (perspective.IsAdjudicator || perspective.Name == mule.Side)
            && !shown.Any(item => item.Side == mule.Side && state.PackedGuns.Contains(item.Id, StringComparer.Ordinal)
                && item.Position is not ContainedPosition { Role: ContainmentRole.AnimalPack }))
        {
            available = true;
        }
        // G10.2 possession while Animal-Packed depends on accompaniment. Legacy PackedGuns alone cannot identify a Mule or prove that relationship.
        if (bound == true)
        {
            bound = loadedOn is not null ? loadedOn == actors.Mule ? null : false
                : weapon!.Holding is not { } holding ? null : holding.Holder == actors.Personnel
                && holding.Role == (weapon.Kind == "asl:gun" ? HoldingRole.Manned : HoldingRole.Possessed);
        }
        bool? colocated = null;
        if (weaponObject is not null && muleObject is not null && personnelObject is not null)
        {
            var locations = ids.Select(id => view.Locations.GetValueOrDefault(id)).ToArray();
            if (locations.All(item => item is not null)
                && (locations.Select(item => (item!.Location, item.OnBridge)).Distinct().Count() != 1
                    || personnelObject.Position is ContainedPosition { Role: ContainmentRole.InFortification }))
            {
                colocated = false;
            }
            // Equal map coordinates alone do not prove shared terrain, Panji side or entrenchment eligibility.
        }
        var horse = mule is { Kind: "asl:horse" } ? mule.Horse : null;
        var canPackByNote = weaponDefinition?.Printed("front", "asl:animal-pack") is { State: PrintedState.Printed, TraitPresent: true } ? true : (bool?)null;
        bool? sameNationality = weaponDefinition is not null && personnelDefinition is not null ? weaponDefinition.Nationality == personnelDefinition.Nationality : null;
        var swQualification = TurnPackingRules.SameNationalitySwQualification(weaponClass,
            personnel is not null ? vocabulary.IsA(personnel.Kind, "asl:mmc") : null, sameNationality);
        var facts = new TurnPackingFacts(weaponClass, canPackByNote,
            view.Phase != "rph" || view.PhasingSide != state.FirstSide ? false : null,
            colocated, horse?.Mule, horse is { Size: not TurnHorseSize.Unknown } ? horse.Size == TurnHorseSize.Squad : null,
            available, personnel is not null ? vocabulary.IsA(personnel.Kind, "asl:crew") : null,
            sameNationality, swQualification, personnel is not null ? Boolean(GameState.GoodOrder(personnel, vocabulary)) : null,
            personnel is not null ? Negate(Boolean(GameState.Condition(personnel, Conditions.Pinned))) : null,
            personnel?.Position switch
            {
                MapPosition => true,
                ContainedPosition { Role: ContainmentRole.InFortification } => true,
                ContainedPosition => false,
                _ => null
            },
            TurnPackingRules.AssemblyReady(weaponClass, weaponDefinition?.Nationality,
                weaponDefinition?.Printed("front", "asl:portage") is { State: PrintedState.Printed, Value.Number: { } pp } ? pp : null,
                weapon is null ? null : Boolean(GameState.Condition(weapon, Conditions.Dismantled)),
                weapon is null ? null : Boolean(GameState.Condition(weapon, "asl:limbered"))));
        return new(view.Stamp, actors, weapon?.Side, weaponClass, number, state.Night, bound, facts,
            TurnPackingRules.EvaluateActors(facts, bound), TurnPackingRules.RequiredGameTurns(weaponClass, number, state.Night))
        {
            LoadedOnMule = loadedOn
        };
    }

    private static UnitDefinition? Definition(DefinitionReference? reference, string? kind, bool synthetic, IReadOnlyList<UnitCatalog> catalogs)
    {
        if (reference is null)
        {
            return null;
        }
        var catalog = catalogs.FirstOrDefault(item => item.Identity == reference.Catalog
            && (item.Publication == CatalogPublication.Published || synthetic && item.Publication == CatalogPublication.Synthetic));
        var definition = catalog?.Definition(reference.Definition);
        return definition?.Kind == kind ? definition : null;
    }

    private static string? Text(UnitDefinition definition, string name) => definition.Printed("front", name) is { State: PrintedState.Printed } value ? value.Value?.Text : null;
    private static bool? Boolean(ConditionState state) => state switch { ConditionState.True => true, ConditionState.False => false, _ => null };
    private static bool? Negate(bool? value) => value is null ? null : !value.Value;
}
