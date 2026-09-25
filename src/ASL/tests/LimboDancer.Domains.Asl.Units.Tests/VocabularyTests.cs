using System.Text;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>The vocabulary model (Unit Display Design, section 3.1) and the asl pack (section 4).</summary>
public sealed class VocabularyTests
{
    [Fact]
    public void TheAslPackLoadsWithAVersionIdentity()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        Assert.Equal("asl@1.4.0", vocabulary.Identity);
        Assert.Equal(64, vocabulary.Hash.Length);
        Assert.Equal(vocabulary.Hash, UnitVocabulary.Asl().Hash);
    }

    [Theory]
    [InlineData("asl:squad", "asl:mmc")]
    [InlineData("asl:squad", "asl:personnel")]
    [InlineData("asl:leader", "asl:smc")]
    [InlineData("asl:hero", "asl:personnel")]
    [InlineData("asl:mg", "asl:sw")]
    [InlineData("asl:radio", "asl:equipment")]
    [InlineData("asl:light-mortar", "asl:unit")]
    [InlineData("asl:crew", "unit")]
    public void KindsFormATree(string kind, string ancestor) => Assert.True(UnitsTestData.Asl.Value.IsA(kind, ancestor));

    [Fact]
    public void DepthGrowsDownTheTreeAndFacesAccumulate()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        Assert.Equal(1, vocabulary.Depth("unit"));
        Assert.True(vocabulary.Depth("asl:squad") > vocabulary.Depth("asl:personnel"));
        Assert.Equal(["front", "broken", "wounded"], vocabulary.Faces("asl:hero"));
        Assert.Equal(["front", "malfunctioned", "reverse"], vocabulary.Faces("asl:radio"));
        Assert.Equal(3, vocabulary.SizeClass("asl:squad"));
        Assert.Equal(2, vocabulary.SizeClass("asl:half-squad"));
        Assert.Equal(1, vocabulary.SizeClass("asl:leader"));
        Assert.False(vocabulary.IsA("asl:mg", "asl:personnel"));
    }

    /// <summary>Every vocabulary term the section 4 parity tables name.</summary>
    [Theory]
    [InlineData("kind", "asl:squad")]
    [InlineData("kind", "asl:half-squad")]
    [InlineData("kind", "asl:crew")]
    [InlineData("kind", "asl:leader")]
    [InlineData("kind", "asl:hero")]
    [InlineData("kind", "asl:mg")]
    [InlineData("kind", "asl:ft")]
    [InlineData("kind", "asl:dc")]
    [InlineData("kind", "asl:latw")]
    [InlineData("kind", "asl:light-mortar")]
    [InlineData("kind", "asl:radio")]
    [InlineData("attribute", "asl:firepower")]
    [InlineData("attribute", "asl:smoke-exponent")]
    [InlineData("attribute", "asl:range")]
    [InlineData("attribute", "asl:morale")]
    [InlineData("attribute", "asl:identity")]
    [InlineData("attribute", "asl:class")]
    [InlineData("attribute", "asl:class-variant")]
    [InlineData("attribute", "asl:broken-morale")]
    [InlineData("attribute", "asl:bpv")]
    [InlineData("attribute", "asl:unit-size")]
    [InlineData("attribute", "asl:leadership")]
    [InlineData("attribute", "asl:size")]
    [InlineData("attribute", "asl:rate-of-fire")]
    [InlineData("attribute", "asl:breakdown")]
    [InlineData("attribute", "asl:portage")]
    [InlineData("attribute", "asl:repair")]
    [InlineData("attribute", "asl:contact")]
    [InlineData("attribute", "asl:contact-dates")]
    [InlineData("trait", "asl:assault-fire")]
    [InlineData("trait", "asl:spraying-fire")]
    [InlineData("trait", "asl:elr-5")]
    [InlineData("trait", "asl:self-rally")]
    [InlineData("trait", "asl:sapper")]
    [InlineData("trait", "asl:assault-engineer")]
    [InlineData("trait", "asl:breakdown-removes")]
    [InlineData("trait", "asl:field-phone")]
    [InlineData("state", "asl:broken")]
    [InlineData("state", "asl:pinned")]
    [InlineData("state", "asl:cx")]
    [InlineData("state", "asl:dm")]
    [InlineData("state", "asl:ti")]
    [InlineData("state", "asl:berserk")]
    [InlineData("state", "asl:fanatic")]
    [InlineData("state", "asl:wounded")]
    [InlineData("state", "asl:disrupted")]
    [InlineData("state", "asl:concealed")]
    [InlineData("state", "asl:malfunctioned")]
    public void TheParityTablesTermsAreDeclared(string what, string name)
    {
        var vocabulary = UnitsTestData.Asl.Value;
        Assert.True(what switch
        {
            "kind" => vocabulary.HasKind(name),
            "attribute" => vocabulary.TryGetAttribute(name, out _),
            "trait" => vocabulary.TryGetTrait(name, out _),
            _ => vocabulary.TryGetState(name, out _),
        });
    }

    [Fact]
    public void StatesSwitchFacesAndBrokenExcludesBerserk()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        Assert.True(vocabulary.TryGetState("asl:broken", out var broken));
        Assert.Equal("broken", broken.Face);
        Assert.True(vocabulary.TryGetState("asl:berserk", out var berserk));
        Assert.Equal(broken.Group, berserk.Group);
        Assert.True(vocabulary.TryGetState("asl:malfunctioned", out var malfunctioned));
        Assert.Equal("malfunctioned", malfunctioned.Face);
    }

    [Fact]
    public void LocalAttributeNamesResolvePerKind()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        Assert.True(vocabulary.TryResolveAttribute("asl:squad", "firepower", out var firepower, out _));
        Assert.Equal("asl:firepower", firepower.Name);
        Assert.True(vocabulary.TryResolveAttribute("asl:squad", "asl:firepower", out _, out _));
        Assert.False(vocabulary.TryResolveAttribute("asl:squad", "leadership", out _, out _));
        Assert.False(vocabulary.TryResolveAttribute("asl:leader", "size", out _, out _));
        Assert.Equal("firepower", vocabulary.ShortName("asl:squad", "asl:firepower"));
    }

    [Fact]
    public void APackCanExtendAslKindsAndAddAttributes()
    {
        var vocabulary = UnitsTestData.WithSla();
        Assert.Equal("asl@1.4.0+sla@0.1.0", vocabulary.Identity);
        Assert.True(vocabulary.IsA("sla:scavenger-band", "asl:personnel"));
        Assert.True(vocabulary.IsA("sla:mutant", "unit"));
        Assert.False(vocabulary.IsA("sla:mutant", "asl:unit"));
        Assert.Contains("sla:supply", vocabulary.AcceptedAttributes("sla:scavenger-band"));
        Assert.Contains("sla:supply", vocabulary.AcceptedAttributes("asl:squad"));
        Assert.Contains("sla:scrounger", vocabulary.AcceptedTraits("asl:crew"));
        Assert.True(vocabulary.TryResolveAttribute("sla:scavenger-band", "supply", out var supply, out _));
        Assert.Equal("sla:supply", supply.Name);
    }

    [Theory]
    [InlineData("""{ "pack": "sla", "version": "0.1.0", "kinds": [ { "name": "sla:band", "extends": "asl:mmc" } ] }""", "UNIT-VOC-005")]
    [InlineData("""{ "pack": "sla", "version": "0.1.0", "extends": ["asl@1.0.0"], "kinds": [ { "name": "asl:squad" } ] }""", "UNIT-VOC-003")]
    [InlineData("""{ "pack": "sla", "version": "0.1.0", "extends": ["asl@2.0.0"] }""", "UNIT-VOC-008")]
    [InlineData("""{ "pack": "sla", "version": "0.1.0", "kinds": [ { "name": "sla:a", "faces": ["upside-down"] } ] }""", "UNIT-VOC-005")]
    [InlineData("""{ "pack": "sla", "version": "0.1.0", "attributes": [ { "name": "sla:x", "type": "decimal" } ] }""", "UNIT-VOC-006")]
    [InlineData("""{ "pack": "sla", "version": "0.1.0", "kinds": [ { "name": "sla:a", "extends": "sla:b" }, { "name": "sla:b", "extends": "sla:a" } ] }""", "UNIT-VOC-009")]
    [InlineData("""{ "pack": "sla", "version": "1", "kinds": [] }""", "UNIT-VOC-001")]
    [InlineData("""{ "pack": "sla", "version": "0.1.0", "extends": ["asl@1.0.0"], "sides": [ { "name": "german", "label": "Germany" } ] }""", "UNIT-VOC-007")]
    public void APackThatChangesOrMisnamesDeclarationsIsRefused(string json, string code)
    {
        var read = VocabularyPackReader.Read(Encoding.UTF8.GetBytes(json));
        var diagnostics = read.Diagnostics.ToList();
        if (read.Pack is { } pack)
        {
            diagnostics.AddRange(UnitVocabulary.Create([VocabularyPackReader.Asl(), pack]).Diagnostics);
        }

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == code);
    }

    [Fact]
    public void ThePackHashIgnoresLineEndings()
    {
        var path = Path.Combine(UnitsTestData.UnitsDirectory(), "vocabulary", "asl.vocab.json");
        var text = File.ReadAllText(path).ReplaceLineEndings("\n");
        var lf = VocabularyPackReader.Read(Encoding.UTF8.GetBytes(text)).Pack!;
        var crlf = VocabularyPackReader.Read(Encoding.UTF8.GetBytes(text.Replace("\n", "\r\n", StringComparison.Ordinal))).Pack!;
        Assert.Equal(lf.Hash, crlf.Hash);
        Assert.Equal(VocabularyPackReader.Asl().Hash, lf.Hash);
    }
}
