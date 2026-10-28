using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class BackendSchemaTests
{
    private static readonly TurnGuideCatalog Catalog = TurnGuideCatalog.Load();

    [Fact]
    public void EmptyCapabilityManifestRetainsEverythingWithoutMakingLegalityClaims()
    {
        var result = TurnCapabilityCatalog.Create(Catalog, []);
        Assert.Equal(538, result.Topics.Length);
        Assert.All(result.Topics, topic =>
        {
            Assert.Empty(topic.Registrations);
            Assert.Equal(TurnSupport.NotEvaluated, topic.UnregisteredSupport);
            Assert.Equal(TurnApplicability.NotEvaluated, topic.Applicability);
        });
        Assert.Contains(result.Topics, topic => topic.Topic.Id == "ccph:hiddenHero");
        Assert.Contains(result.Topics, topic => topic.Topic.Id == "rph:campaign");
    }

    [Fact]
    public void SubActionsKeepProfileAndIndependentSupportAndSourceDimensions()
    {
        var supported = Capability() with
        {
            Evaluator = TurnSupport.IntegratedSubset,
            Evidence = ["Rules.Test.Example"]
        };
        var pending = Capability() with
        {
            Id = "example:external",
            Profile = "external-module",
            SourceReview = TurnSourceReview.SourceMissing,
            Command = TurnSupport.Deferred
        };
        var result = TurnCapabilityCatalog.Create(Catalog, [supported, pending]);
        Assert.Equal(538, result.Topics.Length);
        var topic = Assert.Single(result.Topics, item => item.Topic.Id == supported.TopicId);
        Assert.Equal<TurnCapability>([supported, pending], topic.Registrations);
        Assert.Equal(TurnApplicability.NotEvaluated, topic.Applicability);
        Assert.Contains("external-module", JsonSerializer.Serialize(topic), StringComparison.Ordinal);
        var partialCommand = supported with
        {
            Evaluator = TurnSupport.NotEvaluated,
            Command = TurnSupport.Partial
        };
        Assert.NotEmpty(TurnCapabilityCatalog.Create(Catalog, [partialCommand]).Topics);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("topic")]
    [InlineData("source")]
    [InlineData("evaluator")]
    [InlineData("command")]
    [InlineData("default-evidence")]
    [InlineData("blank-evidence")]
    [InlineData("unsupported-evaluator")]
    [InlineData("unsupported-command")]
    public void RejectsUntraceableCapabilities(string fault)
    {
        var item = Capability();
        item = fault switch
        {
            "topic" => item with { TopicId = "unknown" },
            "source" => item with { SourceReview = (TurnSourceReview)999 },
            "evaluator" => item with { Evaluator = (TurnSupport)999 },
            "command" => item with { Command = (TurnSupport)999 },
            "default-evidence" => item with { Evidence = default },
            "blank-evidence" => item with { Evidence = [" "] },
            "unsupported-evaluator" => item with { Evaluator = TurnSupport.Partial },
            "unsupported-command" => item with { Command = TurnSupport.IntegratedSubset },
            _ => item,
        };
        Assert.Throws<InvalidDataException>(() => TurnCapabilityCatalog.Create(Catalog, fault == "duplicate" ? [item, item] : [item]));
    }

    [Fact]
    public void CapabilityArgumentsAreRequired()
    {
        Assert.Throws<ArgumentNullException>(() => TurnCapabilityCatalog.Create(null!, []));
        Assert.Throws<ArgumentNullException>(() => TurnCapabilityCatalog.Create(Catalog, null!));
        Assert.Throws<ArgumentException>(() => TurnCapabilityCatalog.Create(Catalog, [Capability() with { Id = " " }]));
        Assert.Throws<ArgumentException>(() => TurnCapabilityCatalog.Create(Catalog, [Capability() with { Profile = " " }]));
    }

    [Fact]
    public void TimingIdentityIsIndependentOfLabelsAndGroupPositions()
    {
        var schema = TurnTimingSchema.Load(Catalog);
        Assert.Equal(1, schema.SchemaVersion);
        Assert.Equal(117, schema.Groups.Length);
        Assert.Equal(176, schema.Windows.Length);
        Assert.Equal(538, schema.Bindings.Length);
        Assert.All(schema.Groups, group => Assert.False(string.IsNullOrWhiteSpace(group.Label)));
        Assert.Equal(10, Catalog.Phases[0].AsopLedger.Length);
        var skis = Assert.Single(schema.Bindings, item => item.TopicId == "rph:skis");
        Assert.Equal<string>(["rph:asop:1.14B", "rph:asop:1.22B"], skis.WindowIds);
        Assert.Contains(schema.Windows, window => window.Id == "rph:supplemental:phone" && window.Kind == TurnWindowKind.SupplementalUnclassified);
        var reordered = TurnGuideCatalog.Load(name => MutatedResource(name, root =>
        {
            if (name != "ASL.Turns.Dependencies.json")
            {
                root["groups"]![0] = "Renamed display label";
                foreach (var option in root["options"]!.AsArray())
                {
                    option!["g"] = 0;
                }
            }
        }));
        Assert.Equal(JsonSerializer.Serialize(schema.Bindings), JsonSerializer.Serialize(TurnTimingSchema.Load(reordered).Bindings));
        Assert.NotEqual(new TurnTermId("rph", "DR"), new TurnTermId("rph", "dr"));
        Assert.NotEqual(new TurnTermId("rph", "DR"), new TurnTermId("ccph", "DR"));
        Assert.Equal("rph", new TurnTermId("rph", "DR").PhaseId);
        Assert.Equal("DR", new TurnTermId("rph", "DR").Term);
        Assert.Contains("edition", JsonSerializer.Serialize(Catalog).ToLowerInvariant(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("missing-groups")]
    [InlineData("missing-windows")]
    [InlineData("missing-bindings")]
    [InlineData("group-phase")]
    [InlineData("group-id")]
    [InlineData("window-phase")]
    [InlineData("locator")]
    [InlineData("window-id")]
    [InlineData("supplemental-locator")]
    [InlineData("supplemental-id")]
    [InlineData("missing-topic")]
    [InlineData("unknown-topic")]
    [InlineData("unknown-group")]
    [InlineData("cross-phase-group")]
    [InlineData("empty-windows")]
    [InlineData("unknown-window")]
    [InlineData("cross-phase-window")]
    [InlineData("wrong-ledger-topic")]
    public void TimingRejectsBrokenTraceability(string fault)
    {
        var root = TimingJson();
        var group = root["groups"]![0]!;
        var window = root["windows"]![0]!;
        var supplemental = root["windows"]!.AsArray().First(item => item!["kind"]!.GetValue<string>() == "SupplementalUnclassified")!;
        var binding = root["bindings"]![0]!;
        switch (fault)
        {
            case "version":
                root["schemaVersion"] = 2;
                break;
            case "missing-groups":
                root.AsObject().Remove("groups");
                break;
            case "missing-windows":
                root.AsObject().Remove("windows");
                break;
            case "missing-bindings":
                root.AsObject().Remove("bindings");
                break;
            case "group-phase":
                group["phaseId"] = "unknown";
                break;
            case "group-id":
                group["id"] = "unknown";
                break;
            case "window-phase":
                window["phaseId"] = "unknown";
                break;
            case "locator":
                window["locator"] = "1.99A";
                break;
            case "window-id":
                window["id"] = "unknown";
                break;
            case "supplemental-locator":
                supplemental["locator"] = "1.99A";
                break;
            case "supplemental-id":
                supplemental["id"] = "unknown";
                break;
            case "missing-topic":
                root["bindings"]!.AsArray().RemoveAt(0);
                break;
            case "unknown-topic":
                binding["topicId"] = "unknown";
                break;
            case "unknown-group":
                binding["groupId"] = "unknown";
                break;
            case "cross-phase-group":
                binding["groupId"] = root["groups"]!.AsArray().First(item => item!["phaseId"]!.GetValue<string>() == "pfph")!["id"]!.GetValue<string>();
                break;
            case "empty-windows":
                binding["windowIds"] = new JsonArray();
                break;
            case "unknown-window":
                binding["windowIds"]![0] = "unknown";
                break;
            case "cross-phase-window":
                binding["windowIds"]![0] = "pfph:asop:2.11A";
                break;
            case "wrong-ledger-topic":
                binding["windowIds"]![0] = "rph:asop:1.12A";
                break;
        }
        using var stream = StreamOf(root.ToJsonString());
        Assert.Throws<InvalidDataException>(() => TurnTimingSchema.Read(stream, Catalog));
    }

    [Theory]
    [InlineData("groups")]
    [InlineData("windows")]
    [InlineData("bindings")]
    public void DuplicateTimingIdentitiesFail(string array)
    {
        var root = TimingJson();
        root[array]!.AsArray().Add(root[array]![0]!.DeepClone());
        using var stream = StreamOf(root.ToJsonString());
        Assert.Throws<ArgumentException>(() => TurnTimingSchema.Read(stream, Catalog));
    }

    [Fact]
    public void NullAndUnknownTimingFormatsFail()
    {
        using var nil = StreamOf("null");
        Assert.Throws<InvalidDataException>(() => TurnTimingSchema.Read(nil, Catalog));
        Assert.Throws<ArgumentNullException>(() => TurnTimingSchema.Read(nil, null!));
        var root = TimingJson();
        root["windows"]![0]!["kind"] = "FutureUnknown";
        using var stream = StreamOf(root.ToJsonString());
        Assert.Throws<JsonException>(() => TurnTimingSchema.Read(stream, Catalog));
    }

    [Fact]
    public void DueSchemaKeepsOwnerRelativeOriginsAndEventClocksDistinct()
    {
        TurnDueSpec[] values = [new TurnDueSpec.AfterEvent("event-1"), new TurnDueSpec.BeforeWindowCloses("rph:asop:1.22B"),
            new TurnDueSpec.AtWindow("afph:asop:5.33B"), new TurnDueSpec.NextFriendlyPhase("side-a", "rph", "turn-1"),
            new TurnDueSpec.PlayerTurnEnd("turn-1"), new TurnDueSpec.CompletedGameTurns("side-a", "event-1", 2),
            new TurnDueSpec.SourceChanges("smoke-1", "event-1")];
        Assert.Equal(7, values.Select(value => value.GetType()).Distinct().Count());
        Assert.All(values, value => Assert.NotEqual("{}", JsonSerializer.Serialize(value, value.GetType())));
        Assert.NotEqual(new TurnDueSpec.NextFriendlyPhase("side-a", "rph", "turn-1"), new TurnDueSpec.NextFriendlyPhase("side-b", "rph", "turn-1"));
    }

    [Theory]
    [InlineData("duplicate-dependency")]
    [InlineData("missing-dependency-target")]
    [InlineData("group-negative")]
    [InlineData("group-overflow")]
    [InlineData("undefined-term")]
    [InlineData("ledger-target")]
    [InlineData("null-term")]
    [InlineData("null-text")]
    [InlineData("null-check")]
    [InlineData("empty-pages")]
    [InlineData("low-page")]
    [InlineData("high-page")]
    [InlineData("schema-missing")]
    [InlineData("schema-unknown")]
    [InlineData("schema-two-nonrally")]
    public void CatalogRejectsInvalidSourceData(string fault)
    {
        Assert.Throws<InvalidDataException>(() => TurnGuideCatalog.Load(name => MutatedResource(name, root =>
        {
            if (name == "ASL.Turns.Dependencies.json")
            {
                if (fault == "duplicate-dependency")
                {
                    root["chains"]!.AsArray().Add(root["chains"]![0]!.DeepClone());
                }

                if (fault == "missing-dependency-target")
                {
                    root["chains"]![0]!["links"]![0]!["topic"] = "unknown";
                }
            }
            if (name != "ASL.Turns.ASL Prep Fire Phase Checklist Catalog.json")
            {
                return;
            }

            var topic = root["options"]![0]!;
            switch (fault)
            {
                case "group-negative":
                    topic["g"] = -1;
                    break;
                case "group-overflow":
                    topic["g"] = 999;
                    break;
                case "undefined-term":
                    topic["terms"]!.AsArray().Add("unknown");
                    break;
                case "ledger-target":
                    root["asopLedger"]![0]!["optionIds"]![0] = "unknown";
                    break;
                case "null-term":
                    root["glossary"]!["DR"] = null;
                    break;
                case "null-text":
                    topic["title"] = null;
                    break;
                case "null-check":
                    topic["checks"]![0] = null;
                    break;
                case "empty-pages":
                    topic["sourcePages"] = new JsonArray();
                    break;
                case "low-page":
                    topic["sourcePages"]![0] = 0;
                    break;
                case "high-page":
                    topic["sourcePages"]![0] = 717;
                    break;
                case "schema-missing":
                    root.AsObject().Remove("schemaVersion");
                    break;
                case "schema-unknown":
                    root["schemaVersion"] = 99;
                    break;
                case "schema-two-nonrally":
                    root["schemaVersion"] = 2;
                    break;
            }
        })));
    }

    [Fact]
    public void CatalogHandlesMissingResourceAndLegacyAbsentLedger()
    {
        Assert.Throws<InvalidDataException>(() => TurnGuideCatalog.Load(_ => null));
        Assert.Throws<ArgumentNullException>(() => TurnGuideCatalog.Load(null!));
        var legacy = TurnGuideCatalog.Load(name => MutatedResource(name, root => root.AsObject().Remove("asopLedger")));
        Assert.All(legacy.Phases, phase => Assert.Empty(phase.AsopLedger));
    }

    [Theory]
    [InlineData("missing-linked-window")]
    [InlineData("duplicate-reference")]
    [InlineData("supplemental-owner")]
    [InlineData("unused-window")]
    public void TimingRequiresAllAndOnlyReviewedReferences(string fault)
    {
        var root = TimingJson();
        var bindings = root["bindings"]!.AsArray();
        var skis = bindings.First(item => item!["topicId"]!.GetValue<string>() == "rph:skis")!;
        switch (fault)
        {
            case "missing-linked-window":
                skis["windowIds"]!.AsArray().RemoveAt(1);
                break;
            case "duplicate-reference":
                skis["windowIds"]![1] = skis["windowIds"]![0]!.DeepClone();
                break;
            case "supplemental-owner":
                bindings.First(item => item!["topicId"]!.GetValue<string>() == "rph:phone")!["windowIds"]![0] = "rph:supplemental:ammo";
                break;
            case "unused-window":
                var window = root["windows"]!.AsArray().First(item => item!["kind"]!.GetValue<string>() == "SupplementalUnclassified")!.DeepClone();
                window["id"] = "rph:supplemental:unused";
                root["windows"]!.AsArray().Add(window);
                break;
        }
        using var stream = StreamOf(root.ToJsonString());
        Assert.Throws<InvalidDataException>(() => TurnTimingSchema.Read(stream, Catalog));
    }
    private static TurnCapability Capability() => new("example:base", "rph:campaign", "base", TurnSourceReview.NotReviewed, TurnSupport.Pending, TurnSupport.NotEvaluated, []);
    private static MemoryStream StreamOf(string text) => new(Encoding.UTF8.GetBytes(text));
    private static MemoryStream MutatedResource(string name, Action<JsonNode> edit)
    {
        using var stream = typeof(TurnGuideCatalog).Assembly.GetManifestResourceStream(name)!;
        var root = JsonNode.Parse(stream)!;
        edit(root);
        return StreamOf(root.ToJsonString());
    }
    private static JsonNode TimingJson()
    {
        using var stream = typeof(TurnGuideCatalog).Assembly.GetManifestResourceStream("ASL.Turns.Timing.json")!;
        return JsonNode.Parse(stream)!;
    }
}
