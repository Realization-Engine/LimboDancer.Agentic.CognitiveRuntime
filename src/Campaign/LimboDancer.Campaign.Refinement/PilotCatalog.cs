using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Campaign.Refinement;

// Read the existing ASL catalog. Campaign setup does not publish or manufacture definitions.
public static class PilotCatalog
{
    private static readonly Lazy<UnitCatalog> Current = new(() =>
        UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitVocabulary.Asl())?.Catalog
        ?? throw new InvalidDataException("The ASL unit catalog could not be loaded."));

    public static DefinitionReference GermanCrew()
    {
        var catalog = Current.Value;
        var definition = catalog.Definition("attacker-crew")
            ?? throw new InvalidDataException("The German crew definition is missing.");
        if (catalog.Publication != CatalogPublication.Published || definition.Kind != "asl:crew" ||
            definition.Nationality != "german" || catalog.Source(definition.Counter.Source)?.Status != CatalogSourceStatus.Reviewed)
            throw new InvalidDataException("The German crew requires reviewed printed catalog data.");
        return catalog.Reference(definition);
    }

    public static string Identity => Current.Value.Identity.ToString();
    public static bool Recognizes(string? recorded) => UnitCatalogs.For([Current.Value], recorded) is not null;

    public static string CrewStatus => "Reviewed printed German crew data; Goodwood applicability and one-crew-per-mortar allotment are provisional.";
    public const string MortarStatus = "Unresolved: no reviewed German 81 mm mortar definition in the ASL catalog. No weapon factors or play admission.";
}
