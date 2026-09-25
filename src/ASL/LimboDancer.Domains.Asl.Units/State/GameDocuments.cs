using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// Unit documents for a perspective's view (ASL-UNIT-070): what the display draws, made only from the view, so it
/// never receives what the perspective may not know (ASL-UNIT-031). Each unit is its definition's document
/// (<see cref="CatalogDocuments"/>) with the conditions that are vocabulary states as its states (ASL-UNIT-077) and its
/// facing; the equipment it possesses is attached to it; a sealed presence is a concealed placeholder; equipment
/// without a holder, or manned in its own Location, and entities are their kinds at their locations. Equipment has no
/// catalog definitions yet, so it is drawn by kind alone.
/// </summary>
public static class GameDocuments
{
    public static IReadOnlyList<UnitDocument> For(GameView view, UnitVocabulary vocabulary, IReadOnlyList<UnitCatalog> catalogs)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(vocabulary);
        ArgumentNullException.ThrowIfNull(catalogs);
        var packs = catalogs.Count > 0 ? catalogs[0].Vocabulary : [.. vocabulary.Packs.Select(pack => pack.Identity)];
        string[] States(IGameObject item) =>
            [.. vocabulary.States.Select(state => state.Name).Where(name => GameState.Condition(item, name) == ConditionState.True)];

        var documents = new List<UnitDocument>();
        foreach (var unit in view.Units)
        {
            if (!view.Locations.TryGetValue(unit.Id, out var position) || unit.Definition is null
                || catalogs.FirstOrDefault(catalog => catalog.Identity == unit.Definition.Catalog) is not { } catalog
                || catalog.Definition(unit.Definition.Definition) is not { } definition
                || CatalogDocuments.ToDocument(catalog, definition, vocabulary, unit.Id, position.Location.ToString()).Result is not { } made)
            {
                continue;
            }

            UnitDocument[] attached = [.. view.Equipment.Where(item => item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id)
                .Select(item => new UnitDocument(packs, item.Id, item.Kind, null, null, [], [], States(item), []))];
            documents.Add(made.Document with
            {
                States = States(unit),
                Attached = attached,
                Facing = unit.Position is MapPosition { Facing: { } facing } && vocabulary.HasFacing(unit.Kind) ? facing : null,
            });
        }

        foreach (var presence in view.Sealed)
        {
            documents.Add(new UnitDocument(packs, presence.PlacementId, VocabularyNames.RootKind, presence.Side, presence.Location.ToString(), [], [], [], [],
                Concealed: true));
        }

        foreach (var item in view.Equipment.Where(item => item.Holding is null or { Role: HoldingRole.Manned }))
        {
            if (view.Locations.TryGetValue(item.Id, out var position))
            {
                documents.Add(new UnitDocument(packs, item.Id, item.Kind, item.Side, position.Location.ToString(), [], [], States(item), [])
                {
                    Facing = item.Position is MapPosition { Facing: { } facing } && vocabulary.HasFacing(item.Kind) ? facing : null,
                });
            }
        }

        foreach (var entity in view.Entities)
        {
            if (view.Locations.TryGetValue(entity.Id, out var position))
            {
                documents.Add(new UnitDocument(packs, entity.Id, entity.Kind, entity.Side, position.Location.ToString(), [], [], States(entity), []));
            }
        }

        // Stack order follows the order listed at each location, entities first so units draw above them.
        var ordered = documents.OrderBy(document => vocabulary.IsA(document.Kind, "asl:entity") ? 0 : 1).ToList();
        return [.. ordered.Select(document => document with
        {
            StackOrder = ordered.Where(other => other.Location == document.Location).TakeWhile(other => other != document).Count()
        })];
    }

    /// <summary>The documents as a placement set the Studio's overlay can show; always synthetic until D2 is decided.</summary>
    public static UnitPlacementSet PlacementSet(GameView view, string setId, string label, UnitVocabulary vocabulary, IReadOnlyList<UnitCatalog> catalogs)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(vocabulary);
        return new UnitPlacementSet(setId, label, Synthetic: true, [.. vocabulary.Packs.Select(pack => pack.Identity)], For(view, vocabulary, catalogs),
            string.Empty);
    }
}
