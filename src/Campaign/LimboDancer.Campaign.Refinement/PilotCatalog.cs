using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Campaign.Refinement;

// Read the existing ASL catalog. Campaign setup does not publish or manufacture definitions.
public static class PilotCatalog
{
    private static readonly Lazy<UnitCatalog> Current = new(() =>
        UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitVocabulary.Asl())?.Catalog
        ?? throw new InvalidDataException("The ASL unit catalog could not be loaded."));

    public static DefinitionReference GermanCrew() => Reviewed("attacker-crew", "asl:crew", "german");

    private static DefinitionReference Reviewed(string id, string kind, string nationality)
    {
        var catalog = Current.Value;
        var definition = catalog.Definition(id)
            ?? throw new InvalidDataException("The ASL definition is missing: " + id);
        if (catalog.Publication != CatalogPublication.Published || definition.Kind != kind ||
            definition.Nationality != nationality || catalog.Source(definition.Counter.Source)?.Status != CatalogSourceStatus.Reviewed)
            throw new InvalidDataException("The ASL definition requires reviewed printed catalog data: " + id);
        return catalog.Reference(definition);
    }

    // Preserve existing prototype defaults, without treating printed-data review as scenario approval.
    public static PilotUnit Infantry(string id, string parent, string side, string label, string kind = "rifle", string symbol = "R")
    {
        var (definition, nationality) = (side, kind) switch
        {
            ("Allied", "rifle") => ("british-squad", "british"),
            ("German", "rifle") => ("attacker-squad", "german"),
            ("Allied", "engineer") => ("british-elite-squad", "british"),
            _ => throw new InvalidDataException("Unsupported infantry catalog mapping.")
        };
        return new(id, parent, side, label, kind, symbol, Reviewed(definition, "asl:squad", nationality),
            "Reviewed printed squad data; Goodwood applicability and four-squad composition remain provisional." +
            (kind == "engineer" ? " The existing elite-squad default does not establish assault-engineer or sapper capabilities." : ""),
            AllotmentStatus: "0 separate equipment counters issued by this pilot. Allotment unresolved; source inventory is not an ASL weapon allocation." +
            (kind == "engineer" ? " No Bren, demolition charge, flamethrower or mine-clearing equipment is inferred." : ""));
    }

    public static string Identity => Current.Value.Identity.ToString();
    public static bool Recognizes(string? recorded) => UnitCatalogs.For([Current.Value], recorded) is not null;

    public static string CrewStatus => "Reviewed printed German crew data; Goodwood applicability and one-crew-per-mortar allotment are provisional.";
    public const string MortarStatus = "Unresolved: no reviewed German 81 mm mortar definition in the ASL catalog. No weapon factors or play admission.";
}
