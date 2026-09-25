using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Documents;

/// <summary>
/// A saved set of placed units: unit documents that each carry a hex location. A set is display input only
/// (ASL-UNIT-070): <see cref="Synthetic"/> marks sets authored in the Unit Lab or as fixtures, which are never game
/// state. A set can show on any board or map that contains the boards its locations name.
/// </summary>
public sealed record UnitPlacementSet(string SetId, string Label, bool Synthetic, IReadOnlyList<string> Vocabulary, IReadOnlyList<UnitDocument> Units,
    string Hash)
{
    public const int SchemaVersion = 1;

    /// <summary>The boards the set's locations name.</summary>
    public IReadOnlySet<BoardRef> Boards => Units
        .Select(unit => BoardLocation.TryParse(unit.Location, out var location) ? location.Board : null)
        .OfType<BoardRef>()
        .ToHashSet();
}

public sealed record UnitPlacementSetResult(UnitPlacementSet? Set, IReadOnlyList<UnitDiagnostic> Diagnostics);

/// <summary>
/// Reads a placement set file. Each unit is read as a document (see <see cref="UnitDocumentReader"/>); a refused unit or
/// one without a location is left out with a diagnostic, never moved.
/// </summary>
public static class UnitPlacementSetReader
{
    public static UnitPlacementSetResult Read(ReadOnlySpan<byte> json, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        var diagnostics = new List<UnitDiagnostic>();
        var hash = JsonFields.ContentHash(json);
        if (!UnitDocumentReader.TryParse(json, diagnostics, out var document))
        {
            return new UnitPlacementSetResult(null, diagnostics);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-SET-001", "A placement set must be a JSON object."));
                return new UnitPlacementSetResult(null, diagnostics);
            }

            var fields = new JsonFields(diagnostics, "UNIT-SET-001");
            var schema = fields.OptionalInteger(root, "schemaVersion", "$");
            if (schema != UnitPlacementSet.SchemaVersion)
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-SET-001", $"The schema version must be {UnitPlacementSet.SchemaVersion}.", "schemaVersion"));
                return new UnitPlacementSetResult(null, diagnostics);
            }

            var setId = fields.RequiredString(root, "setId", "$");
            if (setId is null || !VocabularyNames.IsSlug(setId))
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-SET-001", "The set id must be a lowercase slug.", "setId"));
                return new UnitPlacementSetResult(null, diagnostics);
            }

            var label = fields.OptionalString(root, "label", "$") ?? setId;
            var synthetic = fields.OptionalBoolean(root, "synthetic", "$");
            var packs = fields.StringList(root, "vocabulary", "$");
            var units = new List<UnitDocument>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (root.TryGetProperty("units", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var item in list.EnumerateArray())
                {
                    var path = $"units[{index++}]";
                    if (UnitDocumentReader.ReadElement(item, vocabulary, path, packs, attached: false, diagnostics) is not { } unit)
                    {
                        continue;
                    }

                    if (unit.Location is null)
                    {
                        diagnostics.Add(UnitDiagnostic.Error("UNIT-SET-002", $"The unit '{unit.Id}' has no location.", path));
                    }
                    else if (!ids.Add(unit.Id))
                    {
                        diagnostics.Add(UnitDiagnostic.Error("UNIT-DOC-010", $"The id '{unit.Id}' is used twice in this set.", path));
                    }
                    else
                    {
                        units.Add(unit);
                    }
                }
            }
            else
            {
                diagnostics.Add(UnitDiagnostic.Error("UNIT-SET-001", "'units' must be a list of unit documents.", "units"));
            }

            return new UnitPlacementSetResult(new UnitPlacementSet(setId, label, synthetic, packs, units, hash), diagnostics);
        }
    }
}
