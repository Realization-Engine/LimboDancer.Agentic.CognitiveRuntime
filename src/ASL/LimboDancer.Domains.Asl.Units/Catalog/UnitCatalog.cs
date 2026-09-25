using System.Security.Cryptography;

namespace LimboDancer.Domains.Asl.Units.Catalog;

public enum DefinitionLookupStatus
{
    /// <summary>Exactly one definition matches, and the date, when given, is inside its reviewed applicability.</summary>
    Found,

    /// <summary>No definition has this nationality, kind, and class.</summary>
    NotInCatalog,

    /// <summary>Definitions match, but the date is outside every one's reviewed applicability.</summary>
    OutsideApplicability,

    /// <summary>Definitions match, but their applicability has not been reviewed, so the date cannot be checked.</summary>
    ApplicabilityUnreviewed,

    /// <summary>More than one definition matches; the caller must name the counter.</summary>
    Ambiguous,
}

/// <summary>
/// The answer to a definition lookup (acceptance scenario U1). Only <see cref="DefinitionLookupStatus.Found"/> is
/// definitive; every other status is an explicit miss or a nondefinitive answer, never the nearest match (ASL-UNIT-014).
/// </summary>
public sealed record DefinitionLookupResult(DefinitionLookupStatus Status, IReadOnlyList<UnitDefinition> Candidates, string Reason)
{
    public bool IsDefinitive => Status == DefinitionLookupStatus.Found;

    public UnitDefinition? Definition => IsDefinitive ? Candidates[0] : null;
}

/// <summary>
/// A definition catalog (Scenario A1 Catalog Design): versioned, source-backed definitions for one reviewed slice,
/// the slots the slice needs filled, and the sources the values were read from.
/// </summary>
public sealed record UnitCatalog(
    CatalogIdentity Identity,
    string Label,
    CatalogPublication Publication,
    IReadOnlyList<string> Vocabulary,
    IReadOnlyList<CatalogSource> Sources,
    IReadOnlyList<CatalogSlot> Slots,
    IReadOnlyList<UnitDefinition> Definitions)
{
    public UnitDefinition? Definition(string id) => Definitions.FirstOrDefault(definition => definition.Id == id);

    public CatalogSource? Source(string id) => Sources.FirstOrDefault(source => source.Id == id);

    public DefinitionReference Reference(UnitDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new DefinitionReference(Identity, definition.Id);
    }

    /// <summary>The definitions filling a slot.</summary>
    public IReadOnlyList<UnitDefinition> Filling(string slot) =>
        [.. Definitions.Where(definition => definition.Slots.Contains(slot, StringComparer.Ordinal))];

    /// <summary>
    /// Finds the definition for a nationality, kind, and class (null for a kind without a printed class, such as a
    /// leader), optionally on a <c>YYYY-MM</c> date. The kind must match exactly; a kind above it is not a match.
    /// </summary>
    public DefinitionLookupResult Find(string nationality, string kind, string? @class, string? date = null)
    {
        ArgumentNullException.ThrowIfNull(nationality);
        ArgumentNullException.ThrowIfNull(kind);
        if (date is not null && !UnitCatalogReader.IsYearMonth(date))
        {
            throw new ArgumentException($"'{date}' is not a date such as 1944-06.", nameof(date));
        }

        UnitDefinition[] matches = [.. Definitions.Where(definition =>
            definition.Nationality == nationality && definition.Kind == kind && definition.Class == @class)];
        var what = $"{nationality} {kind}{(@class is null ? string.Empty : " " + @class)}";
        if (matches.Length == 0)
        {
            return new(DefinitionLookupStatus.NotInCatalog, [], $"No definition for {what} in {Identity}.");
        }

        if (date is null)
        {
            return matches.Length == 1
                ? new(DefinitionLookupStatus.Found, matches, $"{what} is {matches[0].Id}.")
                : new(DefinitionLookupStatus.Ambiguous, matches, $"{matches.Length} definitions match {what}; name the counter.");
        }

        UnitDefinition[] reviewed = [.. matches.Where(definition =>
            definition.Applicability.Status == ApplicabilityStatus.Reviewed && definition.Applicability.Covers(date))];
        if (reviewed.Length == 1)
        {
            return new(DefinitionLookupStatus.Found, reviewed, $"{what} on {date} is {reviewed[0].Id}.");
        }

        if (reviewed.Length > 1)
        {
            return new(DefinitionLookupStatus.Ambiguous, reviewed, $"{reviewed.Length} definitions match {what} on {date}; name the counter.");
        }

        UnitDefinition[] unreviewed = [.. matches.Where(definition => definition.Applicability.Status == ApplicabilityStatus.Unreviewed)];
        return unreviewed.Length > 0
            ? new(DefinitionLookupStatus.ApplicabilityUnreviewed, unreviewed,
                $"The applicability of {what} has not been reviewed, so {date} cannot be checked.")
            : new(DefinitionLookupStatus.OutsideApplicability, [], $"No definition of {what} applies on {date}.");
    }

    /// <summary>
    /// Checks the pinned hashes of a source against the bytes of its record and transcription, with CRLF read as LF.
    /// Returns an empty list when both match.
    /// </summary>
    public IReadOnlyList<UnitDiagnostic> VerifySource(string sourceId, ReadOnlySpan<byte> record, ReadOnlySpan<byte> transcription)
    {
        ArgumentNullException.ThrowIfNull(sourceId);
        var diagnostics = new List<UnitDiagnostic>();
        if (Source(sourceId) is not { } source)
        {
            diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-012", $"The catalog has no source '{sourceId}'."));
            return diagnostics;
        }

        Check(diagnostics, "record", source.RecordSha256, JsonFields.ContentHash(record));
        Check(diagnostics, "transcription", source.TranscriptionSha256, JsonFields.ContentHash(transcription));
        return diagnostics;

        static void Check(List<UnitDiagnostic> diagnostics, string what, string? pinned, string actual)
        {
            if (!string.Equals(pinned, actual, StringComparison.Ordinal))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-CAT-012",
                    $"The source {what} has SHA-256 {actual}, but the catalog pins {pinned ?? "nothing"}."));
            }
        }
    }

    internal static string Hash(string canonical) =>
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical)));
}
