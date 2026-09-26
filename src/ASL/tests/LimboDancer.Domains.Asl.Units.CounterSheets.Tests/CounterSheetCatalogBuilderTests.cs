using System.Text;
using LimboDancer.Domains.Asl.Units.Catalog;
using static LimboDancer.Domains.Asl.Units.CounterSheets.Tests.CounterSheetsTestData;

namespace LimboDancer.Domains.Asl.Units.CounterSheets.Tests;

/// <summary>
/// Building a catalog from a counter-sheet transcription (Scenario A1 Catalog Design, section 7), with synthetic fills
/// of the committed worksheet. Their values are illustrative, never counter data.
/// </summary>
public sealed class CounterSheetCatalogBuilderTests
{
    private static void AssertRefused(CounterCatalogBuild build, string code)
    {
        Assert.Null(build.Catalog);
        Assert.Null(build.Json);
        Assert.Contains(build.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Severity == UnitDiagnosticSeverity.Error);
    }

    [Fact]
    public void AnUnreviewedTranscriptionBuildsADraft()
    {
        var transcription = SyntheticTranscription();
        var build = Build(transcription, Record(transcription));
        Assert.Empty(build.Diagnostics);
        var catalog = build.Catalog!;
        Assert.Equal(CatalogPublication.Draft, catalog.Publication);
        Assert.Equal("asl-scenario-a1", catalog.Identity.Catalog);
        Assert.Equal(
        [
            "attacker-squad", "attacker-half-squad", "defender-squad", "defender-leader", "defender-half-squad", "attacker-2nd-line-squad",
            "attacker-2nd-line-half-squad", "attacker-conscript-squad", "attacker-conscript-half-squad", "defender-conscript-squad",
            "defender-conscript-half-squad", "defender-leader-7-0", "defender-leader-6-plus-1",
        ],
        catalog.Definitions.Select(definition => definition.Id));

        var source = Assert.Single(catalog.Sources);
        Assert.Equal(CatalogSourceStatus.TranscribedUnreviewed, source.Status);
        Assert.Equal("Synthetic Transcriber", source.Transcriber);
        Assert.Null(source.Reviewer);
        Assert.Equal(Hash(transcription), source.TranscriptionSha256);
        Assert.Empty(catalog.VerifySource(source.Id, Encoding.UTF8.GetBytes(Record(transcription)), Encoding.UTF8.GetBytes(transcription)));
    }

    [Fact]
    public void EveryTranscribedRowBecomesOneSourcedValue()
    {
        var transcription = SyntheticTranscription();
        var catalog = Build(transcription, Record(transcription)).Catalog!;
        var rows = CounterTranscription.Read(Encoding.UTF8.GetBytes(transcription)).Rows;
        Assert.Equal(rows.Count, catalog.Definitions.Sum(definition => definition.Values.Count + 2));

        var squad = catalog.Definition("attacker-squad")!;
        Assert.Equal("asl:squad", squad.Kind);
        Assert.Equal("american", squad.Nationality);
        Assert.Equal("green", squad.Class);
        Assert.Equal(new CounterReference("asl-counter-sheets:scenario-a1", "SYN", "attacker-squad"), squad.Counter);
        var firepowerRow = rows.Single(row => row.Counter == "attacker-squad" && row.Attribute == "firepower");
        var firepower = squad.Printed("front", "asl:firepower")!;
        Assert.Equal(1, firepower.Value!.Number);
        Assert.Equal(firepowerRow.Row, firepower.Source.Row);
        Assert.True(squad.Printed("front", "asl:identity")!.NotPrinted);
        Assert.False(squad.Printed("front", "asl:assault-fire")!.TraitPresent);
        Assert.Equal(ApplicabilityStatus.Unreviewed, squad.Applicability.Status);

        var leader = catalog.Definition("defender-leader")!;
        Assert.Equal("asl:leader", leader.Kind);
        Assert.Null(leader.Class);
        Assert.Equal(0, leader.Printed("front", "asl:leadership")!.Value!.Number);
    }

    [Fact]
    public void NotInSourceIsRecordedApartFromNotPrinted()
    {
        var transcription = SyntheticTranscription()
            .Replace("defender-leader,broken,bpv,1", "defender-leader,broken,bpv,not-in-source", StringComparison.Ordinal)
            .Replace("defender-leader,broken,asl:self-rally,no", "defender-leader,broken,asl:self-rally,not-in-source", StringComparison.Ordinal);
        var leader = Build(transcription, Record(transcription)).Catalog!.Definition("defender-leader")!;
        Assert.True(leader.Printed("broken", "asl:bpv")!.NotInSource);
        Assert.True(leader.Printed("broken", "asl:self-rally")!.NotInSource);
        Assert.True(leader.Printed("front", "asl:identity")!.NotPrinted);
    }

    [Fact]
    public void TheBuiltCatalogIsCanonicalAndItsDefinitionsBecomeDocuments()
    {
        var transcription = SyntheticTranscription();
        var build = Build(transcription, Record(transcription));
        Assert.Equal(build.Json, UnitCatalogJson.Write(build.Catalog!, AslVocabulary.Value));
        var reread = UnitCatalogReader.Read(build.Json!, AslVocabulary.Value);
        Assert.Equal(build.Catalog!.Identity, reread.Catalog!.Identity);
        foreach (var definition in build.Catalog.Definitions)
        {
            var document = CatalogDocuments.ToDocument(build.Catalog, definition, AslVocabulary.Value, definition.Id);
            Assert.Empty(document.Diagnostics);
        }
    }

    [Fact]
    public void AReviewByASecondPersonPublishes()
    {
        var transcription = SyntheticTranscription(reviewer: "Synthetic Reviewer");
        var build = Build(transcription, Record(transcription, "reviewed", reviewer: "Synthetic Reviewer"));
        Assert.Empty(build.Diagnostics);
        Assert.Equal(CatalogPublication.Published, build.Catalog!.Publication);
        Assert.Equal("Synthetic Reviewer", Assert.Single(build.Catalog.Sources).Reviewer);
    }

    [Fact]
    public void ATranscriberCannotReviewTheirOwnTranscription()
    {
        var transcription = SyntheticTranscription(reviewer: "Synthetic Transcriber");
        AssertRefused(Build(transcription, Record(transcription, "reviewed", reviewer: "Synthetic Transcriber")), "UNIT-CAT-016");
    }

    [Fact]
    public void AReviewChangesTheCatalogIdentity()
    {
        var unreviewed = SyntheticTranscription();
        var reviewed = SyntheticTranscription(reviewer: "Synthetic Reviewer");
        var draft = Build(unreviewed, Record(unreviewed)).Catalog!;
        var published = Build(reviewed, Record(reviewed, "reviewed", reviewer: "Synthetic Reviewer")).Catalog!;
        Assert.NotEqual(draft.Identity.Hash, published.Identity.Hash);
    }

    [Fact]
    public void AnAwaitingSourceBuildsNothing()
    {
        var transcription = SyntheticTranscription();
        AssertRefused(Build(transcription, Record(transcription, "awaiting-transcription")), "UNIT-CS-011");
    }

    [Fact]
    public void AChangedTranscriptionNoLongerMatchesItsRecord()
    {
        var transcription = SyntheticTranscription();
        var record = Record(transcription);
        AssertRefused(Build(transcription.Replace("attacker-squad,front,firepower,1", "attacker-squad,front,firepower,2", StringComparison.Ordinal), record),
            "UNIT-CS-011");
    }

    [Fact]
    public void ABlankValueIsNotTranscribed()
    {
        var transcription = SyntheticTranscription().Replace("attacker-squad,front,firepower,1", "attacker-squad,front,firepower,", StringComparison.Ordinal);
        AssertRefused(Build(transcription, Record(transcription)), "UNIT-CS-004");
    }

    [Fact]
    public void EveryWorksheetRowMustBeTranscribed()
    {
        var transcription = string.Join('\n', SyntheticTranscription().Split('\n')
            .Where(line => !line.Contains("defender-leader,broken,bpv", StringComparison.Ordinal)));
        AssertRefused(Build(transcription, Record(transcription)), "UNIT-CS-003");
    }

    [Fact]
    public void RowsNameADeclaredSheetAndTheRecordedPeople()
    {
        var otherSheet = SyntheticTranscription(sheet: "S9");
        AssertRefused(Build(otherSheet, Record(otherSheet)), "UNIT-CS-005");

        var otherTranscriber = SyntheticTranscription(transcriber: "Someone Else");
        AssertRefused(Build(otherTranscriber, Record(otherTranscriber)), "UNIT-CS-006");

        var earlyReviewer = SyntheticTranscription(reviewer: "Synthetic Reviewer");
        AssertRefused(Build(earlyReviewer, Record(earlyReviewer)), "UNIT-CS-006");
    }

    [Fact]
    public void ValuesMustFitTheirType()
    {
        var trait = SyntheticTranscription().Replace("front,asl:elr-5,no", "front,asl:elr-5,maybe", StringComparison.Ordinal);
        AssertRefused(Build(trait, Record(trait)), "UNIT-CS-022");

        var number = SyntheticTranscription().Replace("attacker-squad,front,range,1", "attacker-squad,front,range,six", StringComparison.Ordinal);
        AssertRefused(Build(number, Record(number)), "UNIT-CS-022");

        var kind = SyntheticTranscription().Replace("attacker-squad,counter,kind,asl:squad", "attacker-squad,counter,kind,asl:leader", StringComparison.Ordinal);
        AssertRefused(Build(kind, Record(kind)), "UNIT-CAT-013");

        var enumeration = SyntheticTranscription().Replace("attacker-squad,front,class,green", "attacker-squad,front,class,veteran", StringComparison.Ordinal);
        AssertRefused(Build(enumeration, Record(enumeration)), "UNIT-CAT-009");
    }

    [Fact]
    public void ACounterOutsideTheManifestIsRefused()
    {
        var transcription = SyntheticTranscription() + "SYN,stray-squad,counter,kind,asl:squad,Synthetic Transcriber,,synthetic\n";
        AssertRefused(Build(transcription, Record(transcription)), "UNIT-CS-021");
    }

    [Fact]
    public void TheTranscriptionFormatIsStrict()
    {
        var header = CounterTranscription.Read("sheet,counter,value\n"u8);
        Assert.Equal("UNIT-CS-001", Assert.Single(header.Diagnostics).Code);

        var fields = CounterTranscription.Read("sheet,counter,face,attribute,value,transcriber,reviewer,note\nS1,c1,front\n"u8);
        Assert.Equal("UNIT-CS-001", Assert.Single(fields.Diagnostics).Code);

        var twice = CounterTranscription.Read(
            "sheet,counter,face,attribute,value,transcriber,reviewer,note\nS1,c1,front,morale,1,A,,\nS1,c1,front,morale,2,A,,\n"u8);
        Assert.Equal("UNIT-CS-002", Assert.Single(twice.Diagnostics).Code);

        var quoted = CounterTranscription.Read(
            "sheet,counter,face,attribute,value,transcriber,reviewer,note\r\n\r\nS1,c1,front,morale,1,A,,\"a, \"\"quoted\"\" note\"\r\n"u8);
        Assert.Empty(quoted.Diagnostics);
        var row = Assert.Single(quoted.Rows);
        Assert.Equal(3, row.Row);
        Assert.Equal("a, \"quoted\" note", row.Note);
    }
}
