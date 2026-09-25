using LimboDancer.Domains.Asl.Units.Documents;

namespace LimboDancer.Domains.Asl.Units.Catalog;

/// <summary>Where a catalog's definitions stand (Scenario A1 Catalog Design, section 5).</summary>
public enum CatalogPublication
{
    /// <summary>Illustrative values for tests and fixtures, never used as counter data.</summary>
    Synthetic,

    /// <summary>Transcribed from a registered source that has not yet been reviewed. Not publishable.</summary>
    Draft,

    /// <summary>Every source is reviewed by someone other than its transcriber (ASL-UNIT-012).</summary>
    Published,
}

/// <summary>The review status of a registered counter data source.</summary>
public enum CatalogSourceStatus
{
    Synthetic,
    AwaitingTranscription,
    TranscribedUnreviewed,
    Reviewed,
}

/// <summary>
/// A registered source the catalog's values come from (ASL-UNIT-081). The catalog pins the source record and the
/// transcription by SHA-256, so a changed source changes the catalog and its identity (ASL-UNIT-080).
/// </summary>
public sealed record CatalogSource(
    string Id,
    CatalogSourceStatus Status,
    string? Record,
    string? RecordSha256,
    string? Transcription,
    string? TranscriptionSha256,
    string? Transcriber,
    string? Reviewer);

/// <summary>
/// A role the slice needs a definition for, such as the moving squad of the Scenario A1 cases (ASL-UNIT-062). Every
/// definition filling the slot must be of its kind or a kind below it.
/// </summary>
public sealed record CatalogSlot(string Id, string Kind, string Label);

/// <summary>A counter on a registered counter sheet: the source, the sheet, and the transcription's counter id.</summary>
public sealed record CounterReference(string Source, string Sheet, string Counter)
{
    public override string ToString() => $"{Source}/{Sheet}/{Counter}";
}

/// <summary>Where one printed value was read: the counter, the face it is printed on, and the transcription row.</summary>
public sealed record ValueSource(CounterReference Counter, string Face, int Row)
{
    public override string ToString() => $"{Counter} {Face}, row {Row}";
}

/// <summary>What a transcription row says about a characteristic.</summary>
public enum PrintedState
{
    /// <summary>The source shows it: an attribute's value, or a trait marking present or absent.</summary>
    Printed,

    /// <summary>The attribute was checked and the counter does not print it.</summary>
    NotPrinted,

    /// <summary>The source consulted does not show it, so nothing is known about it. Distinct from not printed.</summary>
    NotInSource,
}

/// <summary>
/// One printed characteristic of a counter face (ASL-UNIT-011): an attribute with its typed value, an attribute the
/// counter does not print, a trait marking that is present or absent, or a characteristic the source does not show.
/// Printed values are never changed by rules; rules produce <see cref="EffectiveValue"/>s instead (ASL-UNIT-013).
/// </summary>
public sealed record PrintedValue(string Face, string Name, bool IsTrait, PrintedState State, UnitValue? Value, bool? TraitPresent, ValueSource Source)
{
    public bool NotPrinted => State == PrintedState.NotPrinted;

    public bool NotInSource => State == PrintedState.NotInSource;
}

public enum ApplicabilityStatus
{
    /// <summary>No registered source for the definition's dates, modules, or SSR conditions has been reviewed yet.</summary>
    Unreviewed,
    Reviewed,
}

/// <summary>
/// When a definition applies (ASL-UNIT-014): its dates as <c>YYYY-MM</c>, inclusive, open when null; the modules and
/// SSR or other conditions; and the rule or source that says so.
/// </summary>
public sealed record Applicability(
    ApplicabilityStatus Status,
    string? From,
    string? To,
    IReadOnlyList<string> Modules,
    IReadOnlyList<string> Conditions,
    string? Basis)
{
    public static Applicability Unreviewed { get; } = new(ApplicabilityStatus.Unreviewed, null, null, [], [], null);

    /// <summary>Whether a <c>YYYY-MM</c> date falls inside the declared dates.</summary>
    public bool Covers(string date) =>
        (From is null || string.CompareOrdinal(From, date) <= 0) && (To is null || string.CompareOrdinal(date, To) <= 0);
}

/// <summary>
/// A definition's key (ASL-UNIT-011): more than the printed strength triple. Two counters with the same front values
/// but a different nationality, class, broken face, or applicability are different definitions.
/// </summary>
public sealed record DefinitionKey(string Nationality, string Kind, string? Class, CounterReference Counter, string? From, string? To);

/// <summary>
/// A reference definition (ASL-UNIT-010 to 014): the printed values of one counter, keyed by nationality, kind, class,
/// counter, and applicability, each value with its source. It is not game state; instances refer to it (step 4).
/// </summary>
public sealed record UnitDefinition(
    string Id,
    string Kind,
    string Nationality,
    string? Class,
    CounterReference Counter,
    int KindRow,
    int NationalityRow,
    Applicability Applicability,
    IReadOnlyList<string> Slots,
    IReadOnlyList<PrintedValue> Values)
{
    public DefinitionKey Key => new(Nationality, Kind, Class, Counter, Applicability.From, Applicability.To);

    /// <summary>The printed value of an attribute or trait on a face, by qualified name.</summary>
    public PrintedValue? Printed(string face, string name) =>
        Values.FirstOrDefault(value => value.Face == face && value.Name == name);

    /// <summary>The faces the definition records values for, in the order first seen.</summary>
    public IReadOnlyList<string> Faces => [.. Values.Select(value => value.Face).Distinct(StringComparer.Ordinal)];
}

/// <summary>
/// A catalog's version identity (ASL-UNIT-080): its declared id and version, and the SHA-256 of its canonical form.
/// The canonical form includes every definition and the pinned hashes of every source, so the hash changes whenever
/// a definition or its source does. Instances record <see cref="ToString"/>.
/// </summary>
public sealed record CatalogIdentity(string Catalog, string Version, string Hash)
{
    public override string ToString() => $"{Catalog}@{Version}+sha256:{Hash}";
}

/// <summary>A definition in a catalog, as an instance or a unit document records it.</summary>
public sealed record DefinitionReference(CatalogIdentity Catalog, string Definition)
{
    public override string ToString() => $"{Catalog}#{Definition}";
}
