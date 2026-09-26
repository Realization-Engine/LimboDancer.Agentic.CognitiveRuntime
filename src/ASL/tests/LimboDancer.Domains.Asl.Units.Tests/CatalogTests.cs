using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// The definition catalog (Scenario A1 Catalog Design): reading, validation, version identity, lookup, and conversion
/// to unit documents. Every catalog here is synthetic: its values are illustrative, never counter data.
/// </summary>
public sealed class CatalogTests
{
    private static UnitCatalog Synthetic()
    {
        var result = UnitCatalogs.Read(UnitCatalogs.ScenarioA1Synthetic, UnitsTestData.Asl.Value);
        Assert.NotNull(result);
        Assert.Empty(result.Diagnostics);
        return result.Catalog!;
    }

    private static JsonObject SyntheticJson() =>
        JsonNode.Parse(UnitCatalogJson.Write(Synthetic(), UnitsTestData.Asl.Value))!.AsObject();

    private static UnitCatalogResult Read(JsonNode json) => UnitCatalogReader.Read(json.ToJsonString(), UnitsTestData.Asl.Value);

    private static void AssertRefused(JsonNode json, string code)
    {
        var result = Read(json);
        Assert.Null(result.Catalog);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Severity == UnitDiagnosticSeverity.Error);
    }

    private static JsonObject Definition(JsonObject catalog, string id) =>
        catalog["definitions"]!.AsArray().Select(node => node!.AsObject()).Single(definition => (string?)definition["id"] == id);

    private static JsonObject Value(JsonObject definition, string face, string name) =>
        definition["values"]!.AsArray().Select(node => node!.AsObject())
            .Single(value => (string?)value["face"] == face && ((string?)value["attribute"] ?? (string?)value["trait"]) == name);

    /// <summary>The synthetic catalog as a draft over a registered source whose record and transcription are these bytes.</summary>
    private static JsonObject Draft(byte[] record, byte[] transcription, string reviewer = "")
    {
        var json = SyntheticJson();
        json["publication"] = "draft";
        json["sources"] = new JsonArray(new JsonObject
        {
            ["id"] = "sheets",
            ["status"] = reviewer.Length > 0 ? "reviewed" : "transcribed-unreviewed",
            ["record"] = "record.json",
            ["recordSha256"] = Hash(record),
            ["transcription"] = "transcription.csv",
            ["transcriptionSha256"] = Hash(transcription),
            ["transcriber"] = "Transcriber",
        });
        if (reviewer.Length > 0)
        {
            json["sources"]![0]!["reviewer"] = reviewer;
        }

        foreach (var definition in json["definitions"]!.AsArray())
        {
            definition!["counter"]!["source"] = "sheets";
        }

        return json;
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    [Fact]
    public void TheSyntheticCatalogIsEmbeddedAndReads()
    {
        Assert.Contains(UnitCatalogs.ScenarioA1Synthetic, UnitCatalogs.Names);
        var catalog = Synthetic();
        Assert.Equal(CatalogPublication.Synthetic, catalog.Publication);
        Assert.Equal("asl-scenario-a1-synthetic", catalog.Identity.Catalog);
        Assert.Equal(64, catalog.Identity.Hash.Length);
        Assert.StartsWith("asl-scenario-a1-synthetic@1.0.0+sha256:", catalog.Identity.ToString(), StringComparison.Ordinal);
        Assert.Equal(
            [
                "attacker-squad", "attacker-half-squad", "defender-squad", "defender-leader", "defender-half-squad", "attacker-2nd-line-squad",
                "attacker-2nd-line-half-squad", "attacker-conscript-squad", "attacker-conscript-half-squad", "defender-conscript-squad",
                "defender-conscript-half-squad", "defender-leader-7-0", "defender-leader-6-plus-1",
            ],
            catalog.Definitions.Select(definition => definition.Id));
        Assert.All(catalog.Slots, slot => Assert.NotEmpty(catalog.Filling(slot.Id)));
    }

    [Fact]
    public void WritingIsCanonicalAndKeepsTheIdentity()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var catalog = Synthetic();
        var written = UnitCatalogJson.Write(catalog, vocabulary);
        var reread = UnitCatalogReader.Read(written, vocabulary);
        Assert.Empty(reread.Diagnostics);
        Assert.Equal(catalog.Identity, reread.Catalog!.Identity);
        Assert.Equal(written, UnitCatalogJson.Write(reread.Catalog, vocabulary));
        Assert.DoesNotContain("\r", written, StringComparison.Ordinal);
        Assert.EndsWith("\n", written, StringComparison.Ordinal);

        // Layout is not content: the same catalog on one line has the same identity.
        var compact = UnitCatalogReader.Read(JsonNode.Parse(written)!.ToJsonString(), vocabulary);
        Assert.Equal(catalog.Identity, compact.Catalog!.Identity);
    }

    [Fact]
    public void TheIdentityChangesWhenADefinitionOrItsSourceChanges()
    {
        var identity = Synthetic().Identity;
        var changedValue = SyntheticJson();
        Value(Definition(changedValue, "attacker-squad"), "front", "firepower")["value"] = 2;
        var afterValue = Read(changedValue).Catalog!.Identity;
        Assert.Equal(identity.Version, afterValue.Version);
        Assert.NotEqual(identity.Hash, afterValue.Hash);

        var record = Encoding.UTF8.GetBytes("{}");
        var draft = Read(Draft(record, Encoding.UTF8.GetBytes("a"))).Catalog!;
        var otherTranscription = Read(Draft(record, Encoding.UTF8.GetBytes("b"))).Catalog!;
        Assert.NotEqual(draft.Identity.Hash, otherTranscription.Identity.Hash);
    }

    [Fact]
    public void PrintedValuesAreTypedAndKeepTheirSource()
    {
        var squad = Synthetic().Definition("attacker-squad")!;
        Assert.Equal(new DefinitionKey("american", "asl:squad", "green", new CounterReference("synthetic", "synthetic", "attacker-squad"), null, null), squad.Key);
        var firepower = squad.Printed("front", "asl:firepower")!;
        Assert.Equal(1, firepower.Value!.Number);
        Assert.Equal(new ValueSource(squad.Counter, "front", 4), firepower.Source);
        Assert.True(squad.Printed("front", "asl:class-variant")!.NotPrinted);
        Assert.True(squad.Printed("front", "asl:assault-fire")!.TraitPresent);
        Assert.False(squad.Printed("front", "asl:spraying-fire")!.TraitPresent);
        Assert.Equal(["front", "broken"], squad.Faces);

        // Not in the source is not the same as not printed: nothing is known about it.
        var leader = Synthetic().Definition("defender-leader")!;
        var identity = leader.Printed("front", "asl:identity")!;
        Assert.True(identity.NotInSource);
        Assert.False(identity.NotPrinted);
        Assert.Null(identity.Value);
        var elr = leader.Printed("front", "asl:elr-5")!;
        Assert.True(elr.IsTrait);
        Assert.True(elr.NotInSource);
        Assert.Null(elr.TraitPresent);
    }

    [Fact]
    public void EffectiveValuesNeverOverwritePrintedOnes()
    {
        var squad = Synthetic().Definition("attacker-squad")!;
        var printed = squad.Printed("front", "asl:firepower")!;
        var effective = EffectiveValue.From(printed).Apply(new ValueAdjustment("Synthetic rule", "test", 3));
        Assert.Equal(4, effective.Value);
        Assert.Equal(1, effective.Printed.Value!.Number);
        Assert.Equal(1, squad.Printed("front", "asl:firepower")!.Value!.Number);
        Assert.Throws<ArgumentException>(() => EffectiveValue.From(squad.Printed("front", "asl:class-variant")!));
    }

    [Fact]
    public void ALookupReturnsTheDefinitionOrAnExplicitMiss()
    {
        var catalog = Synthetic();
        var found = catalog.Find("italian", "asl:squad", "conscript", "1901-06");
        Assert.Equal(DefinitionLookupStatus.Found, found.Status);
        Assert.Equal("defender-squad", found.Definition!.Id);

        var outside = catalog.Find("italian", "asl:squad", "conscript", "1944-06");
        Assert.Equal(DefinitionLookupStatus.OutsideApplicability, outside.Status);
        Assert.Null(outside.Definition);
        Assert.Empty(outside.Candidates);

        // Unreviewed applicability cannot say yes or no to a date.
        var unreviewed = catalog.Find("american", "asl:squad", "green", "1944-06");
        Assert.Equal(DefinitionLookupStatus.ApplicabilityUnreviewed, unreviewed.Status);
        Assert.False(unreviewed.IsDefinitive);
        Assert.Equal("attacker-squad", Assert.Single(unreviewed.Candidates).Id);
        Assert.Equal(DefinitionLookupStatus.Found, catalog.Find("american", "asl:squad", "green").Status);

        Assert.Equal(DefinitionLookupStatus.NotInCatalog, catalog.Find("german", "asl:squad", "green").Status);
        Assert.Equal(DefinitionLookupStatus.NotInCatalog, catalog.Find("american", "asl:squad", "elite").Status);
        Assert.Equal(DefinitionLookupStatus.NotInCatalog, catalog.Find("american", "asl:mmc", "green").Status);
        Assert.Equal(DefinitionLookupStatus.Found, catalog.Find("italian", "asl:leader", null).Status);
        Assert.Throws<ArgumentException>(() => catalog.Find("american", "asl:squad", "green", "1944-13"));
    }

    [Fact]
    public void TwoDefinitionsWithTheSameKeyFactsAreAmbiguousNotMerged()
    {
        var json = SyntheticJson();
        var copy = Definition(json, "attacker-squad").DeepClone().AsObject();
        copy["id"] = "attacker-squad-2";
        copy["counter"]!["counter"] = "attacker-squad-2";
        var row = 1000;
        copy["kindRow"] = row++;
        copy["nationalityRow"] = row++;
        foreach (var value in copy["values"]!.AsArray())
        {
            value!["row"] = row++;
        }

        json["definitions"]!.AsArray().Add(copy);
        var catalog = Read(json).Catalog!;
        var result = catalog.Find("american", "asl:squad", "green");
        Assert.Equal(DefinitionLookupStatus.Ambiguous, result.Status);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public void EachDefinitionBecomesAValidUnitDocument()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var catalog = Synthetic();
        foreach (var definition in catalog.Definitions)
        {
            var result = CatalogDocuments.ToDocument(catalog, definition, vocabulary, "doc-" + definition.Id, "bd01:E4:0");
            Assert.Empty(result.Diagnostics);
            var document = result.Result!.Document;
            Assert.Equal(definition.Kind, document.Kind);
            Assert.Equal(definition.Nationality, document.Side);
            Assert.Equal("bd01:E4:0", document.Location);
            Assert.Equal($"{catalog.Identity}#{definition.Id}", result.Result.Definition.ToString());

            // The document round-trips through the display's own writer and reader.
            var written = UnitDocumentJson.Write(document, vocabulary);
            var reread = UnitDocumentReader.Read(written, vocabulary);
            Assert.Empty(reread.Diagnostics);
            Assert.Equal(written, UnitDocumentJson.Write(reread.Documents[0], vocabulary));
        }
    }

    [Fact]
    public void ADocumentShowsPrintedValuesOnTheirFaces()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var catalog = Synthetic();
        var squad = CatalogDocuments.ToDocument(catalog, catalog.Definition("attacker-squad")!, vocabulary, "squad").Result!.Document;
        Assert.Equal(1, squad.Value("front", "asl:firepower")!.Number);
        Assert.Equal("green", squad.Value("front", "asl:class")!.Text);
        Assert.Null(squad.Face("front")!.Value("asl:class-variant"));
        Assert.True(squad.Face("front")!.HasTrait("asl:assault-fire"));
        Assert.False(squad.Face("front")!.HasTrait("asl:spraying-fire"));
        Assert.True(squad.Face("broken")!.HasTrait("asl:self-rally"));
        Assert.Equal(1, Assert.Single(squad.Unit).Number);
        Assert.Empty(squad.States);

        var leader = CatalogDocuments.ToDocument(catalog, catalog.Definition("defender-leader")!, vocabulary, "leader").Result!.Document;
        Assert.Equal(0, leader.Value("front", "asl:leadership")!.Number);
        Assert.Null(leader.Face("front")!.Value("asl:identity"));
        Assert.False(leader.Face("front")!.HasTrait("asl:elr-5"));
        Assert.Equal(5, leader.Value("broken", "asl:broken-morale")!.Number);
    }

    [Fact]
    public void ADefinitionFromAnotherCatalogIsRefused()
    {
        var catalog = Synthetic();
        var stranger = catalog.Definitions[0] with
        {
            Id = "stranger"
        };
        Assert.Throws<ArgumentException>(() => CatalogDocuments.ToDocument(catalog, stranger, UnitsTestData.Asl.Value, "x"));
    }

    [Fact]
    public void PinnedSourcesAreVerifiedAgainstTheirBytes()
    {
        var record = Encoding.UTF8.GetBytes("{\"status\":\"transcribed-unreviewed\"}\n");
        var transcription = Encoding.UTF8.GetBytes("sheet,counter\n");
        var catalog = Read(Draft(record, transcription)).Catalog!;
        Assert.Equal(CatalogPublication.Draft, catalog.Publication);
        Assert.Empty(catalog.VerifySource("sheets", record, transcription));

        // CRLF checkouts hash the same as LF.
        Assert.Empty(catalog.VerifySource("sheets", record, Encoding.UTF8.GetBytes("sheet,counter\r\n")));
        var changed = catalog.VerifySource("sheets", record, Encoding.UTF8.GetBytes("sheet,counter,face\n"));
        Assert.Equal("UNIT-CAT-012", Assert.Single(changed).Code);
        Assert.Equal("UNIT-CAT-012", Assert.Single(catalog.VerifySource("other", record, transcription)).Code);
    }

    [Fact]
    public void PublicationNeedsAReviewByASecondPerson()
    {
        var record = Encoding.UTF8.GetBytes("{}");
        var transcription = Encoding.UTF8.GetBytes("a");
        var unreviewed = Draft(record, transcription);
        unreviewed["publication"] = "published";
        AssertRefused(unreviewed, "UNIT-CAT-016");

        var selfReviewed = Draft(record, transcription, reviewer: "transcriber");
        selfReviewed["publication"] = "published";
        AssertRefused(selfReviewed, "UNIT-CAT-016");

        var reviewed = Draft(record, transcription, reviewer: "Reviewer");
        reviewed["publication"] = "published";
        Assert.Equal(CatalogPublication.Published, Read(reviewed).Catalog!.Publication);

        var synthetic = Draft(record, transcription);
        synthetic["publication"] = "synthetic";
        AssertRefused(synthetic, "UNIT-CAT-016");

        var syntheticSourceInDraft = SyntheticJson();
        syntheticSourceInDraft["publication"] = "draft";
        AssertRefused(syntheticSourceInDraft, "UNIT-CAT-016");
    }

    [Fact]
    public void ADraftFillsEverySlot()
    {
        var json = Draft(Encoding.UTF8.GetBytes("{}"), Encoding.UTF8.GetBytes("a"));
        Definition(json, "attacker-half-squad")["slots"] = new JsonArray();
        AssertRefused(json, "UNIT-CAT-016");
    }

    [Fact]
    public void ARegisteredSourcePinsItsFilesAndTranscriber()
    {
        var json = Draft(Encoding.UTF8.GetBytes("{}"), Encoding.UTF8.GetBytes("a"));
        json["sources"]![0]!["transcriptionSha256"] = "ABC";
        AssertRefused(json, "UNIT-CAT-005");

        json = Draft(Encoding.UTF8.GetBytes("{}"), Encoding.UTF8.GetBytes("a"));
        json["sources"]![0]!.AsObject().Remove("transcriber");
        AssertRefused(json, "UNIT-CAT-005");

        json = Draft(Encoding.UTF8.GetBytes("{}"), Encoding.UTF8.GetBytes("a"));
        json["sources"]![0]!["status"] = "reviewed";
        AssertRefused(json, "UNIT-CAT-005");
    }

    public static TheoryData<string, string> Refusals => new()
    {
        { "kind", "UNIT-CAT-008" },
        { "nationality", "UNIT-CAT-008" },
        { "class-mismatch", "UNIT-CAT-009" },
        { "class-without-value", "UNIT-CAT-009" },
        { "counter-source", "UNIT-CAT-010" },
        { "slot-kind", "UNIT-CAT-013" },
        { "slot-unknown", "UNIT-CAT-013" },
        { "duplicate-row", "UNIT-CAT-011" },
        { "duplicate-id", "UNIT-CAT-007" },
        { "duplicate-value", "UNIT-CAT-011" },
        { "face", "UNIT-CAT-011" },
        { "attribute-for-kind", "UNIT-CAT-011" },
        { "attribute-face", "UNIT-CAT-011" },
        { "trait-face", "UNIT-CAT-011" },
        { "value-type", "UNIT-CAT-011" },
        { "value-and-not-printed", "UNIT-CAT-011" },
        { "value-and-not-in-source", "UNIT-CAT-011" },
        { "trait-present-and-not-in-source", "UNIT-CAT-011" },
        { "row", "UNIT-CAT-011" },
        { "applicability-missing", "UNIT-CAT-015" },
        { "applicability-date", "UNIT-CAT-015" },
        { "applicability-order", "UNIT-CAT-015" },
        { "applicability-unreviewed-dates", "UNIT-CAT-015" },
        { "applicability-basis", "UNIT-CAT-015" },
        { "vocabulary", "UNIT-CAT-004" },
        { "schema", "UNIT-CAT-002" },
        { "version", "UNIT-CAT-003" },
        { "publication", "UNIT-CAT-003" },
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public void AnInvalidCatalogIsRefusedWithACode(string change, string code)
    {
        var json = SyntheticJson();
        var squad = Definition(json, "attacker-squad");
        var leader = Definition(json, "defender-leader");
        switch (change)
        {
            case "kind":
                squad["kind"] = "asl:platoon";
                break;
            case "nationality":
                squad["nationality"] = "martian";
                break;
            case "class-mismatch":
                squad["class"] = "elite";
                break;
            case "class-without-value":
                leader["class"] = "elite";
                break;
            case "counter-source":
                squad["counter"]!["source"] = "elsewhere";
                break;
            case "slot-kind":
                leader["slots"] = new JsonArray("a1-moving-squad");
                break;
            case "slot-unknown":
                leader["slots"] = new JsonArray("a1-nowhere");
                break;
            case "duplicate-row":
                Value(leader, "front", "leadership")["row"] = 4;
                break;
            case "duplicate-id":
                leader["id"] = "attacker-squad";
                break;
            case "duplicate-value":
                squad["values"]!.AsArray().Add(Value(squad, "front", "firepower").DeepClone());
                break;
            case "face":
                Value(squad, "front", "firepower")["face"] = "wounded";
                break;
            case "attribute-for-kind":
                Value(squad, "front", "firepower")["attribute"] = "leadership";
                break;
            case "attribute-face":
                Value(squad, "broken", "broken-morale")["face"] = "front";
                break;
            case "trait-face":
                Value(squad, "broken", "asl:self-rally")["face"] = "front";
                break;
            case "value-type":
                Value(squad, "front", "firepower")["value"] = "four";
                break;
            case "value-and-not-printed":
                Value(squad, "front", "firepower")["printed"] = false;
                break;
            case "value-and-not-in-source":
                Value(squad, "front", "firepower")["recorded"] = false;
                break;
            case "trait-present-and-not-in-source":
                Value(squad, "front", "asl:assault-fire")["recorded"] = false;
                break;
            case "row":
                Value(squad, "front", "firepower")["row"] = 0;
                break;
            case "applicability-missing":
                squad.Remove("applicability");
                break;
            case "applicability-date":
                Definition(json, "defender-squad")["applicability"]!["from"] = "1901";
                break;
            case "applicability-order":
                Definition(json, "defender-squad")["applicability"]!["from"] = "1902-01";
                break;
            case "applicability-unreviewed-dates":
                squad["applicability"]!["from"] = "1901-01";
                break;
            case "applicability-basis":
                Definition(json, "defender-squad")["applicability"]!.AsObject().Remove("basis");
                break;
            case "vocabulary":
                json["vocabulary"] = new JsonArray("asl@9.0.0");
                break;
            case "schema":
                json["schemaVersion"] = 2;
                break;
            case "version":
                json["version"] = "1.0";
                break;
            case "publication":
                json["publication"] = "final";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(change));
        }

        AssertRefused(json, code);
    }

    [Fact]
    public void UnknownFieldsWarnButDoNotRefuse()
    {
        var json = SyntheticJson();
        json["extra"] = 1;
        Definition(json, "attacker-squad")["colour"] = "green";
        var result = Read(json);
        Assert.NotNull(result.Catalog);
        Assert.Equal(2, result.Diagnostics.Count(diagnostic => diagnostic.Code == "UNIT-CAT-017"));
    }

    [Fact]
    public void BrokenJsonIsRefused()
    {
        var result = UnitCatalogReader.Read("{", UnitsTestData.Asl.Value);
        Assert.Null(result.Catalog);
        Assert.Equal("UNIT-CAT-001", Assert.Single(result.Diagnostics).Code);
    }
}
