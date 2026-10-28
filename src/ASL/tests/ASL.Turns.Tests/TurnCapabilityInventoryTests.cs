using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnCapabilityInventoryTests
{
    private static readonly TurnGuideCatalog Catalog = TurnGuideCatalog.Load();
    private static JsonObject Document()
    {
        using var stream = typeof(TurnCapabilityInventory).Assembly.GetManifestResourceStream("ASL.Turns.Inventory.json")!;
        return JsonNode.Parse(stream)!.AsObject();
    }
    private static TurnCapabilityInventory Read(JsonNode? node)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(node?.ToJsonString() ?? "null"));
        return TurnCapabilityInventory.Read(stream, Catalog);
    }

    [Fact]
    public void LaterPassTimingContractDoesNotPromoteSpotterCommandsOrRemovePendingRules()
    {
        var facts = new LimboDancer.Domains.Asl.Rules.TurnSpotterTimingFacts("a",
            new(1, 1, "a", "pfph", LimboDancer.Domains.Asl.Rules.TurnCheckpoint.DuringPhase), false,
            LimboDancer.Domains.Asl.Rules.TurnSpotterChange.None);
        Assert.Equal(LimboDancer.Domains.Asl.Rules.TurnSpotterTimingState.InitialDesignationWindow,
            LimboDancer.Domains.Asl.Rules.TurnSpotterTimingRules.Evaluate(facts).State);
        Assert.Equal(TurnSupport.Pending, TurnGuideProfiles.Get("existing-engine-fire-v1").CommandSupport("spotter", true));
        var inventory = TurnCapabilityInventory.Load(Catalog);
        Assert.Contains(inventory.Gaps, gap => gap.WorkItem == "B5-D1");
        Assert.Equal(TurnSupport.Pending, inventory.Bindings.Single(row => row.Id == "spotter").PendingRuleSupport);
        Assert.Equal(538, inventory.Bindings.Count(row => row.Kind == TurnInventoryKind.Topic));
    }

    [Fact]
    public void WhiteSmokeTimingDoesNotPromoteAtmosphereCommandsOrEraseOtherSources()
    {
        var inventory = TurnCapabilityInventory.Load(Catalog);
        Assert.Equal(TurnSupport.Pending, inventory.Bindings.Single(row => row.Id == "smoke").PendingRuleSupport);
        var snapshot = TurnAtmosphere.Compose([], [], ["unclassified-white-looking-counter"]);
        Assert.Equal(7, snapshot.PendingRules.Length);
        Assert.All(snapshot.PendingRules, item => Assert.Equal(TurnSupport.Pending, item.CommandSupport));
        Assert.Equal("unclassified-white-looking-counter", Assert.Single(snapshot.UnclassifiedSmoke));
        Assert.Equal(9, inventory.Gaps.Length);
        Assert.Equal(538, inventory.Bindings.Count(row => row.Kind == TurnInventoryKind.Topic));
    }

    [Fact]
    public void InventoryAccountsForEveryIdentityAndReferenceLinkWithoutPromotingCapabilities()
    {
        var inventory = TurnCapabilityInventory.Load(Catalog);
        Assert.Equal(1, inventory.SchemaVersion);
        Assert.Equal(538, inventory.Bindings.Count(item => item.Kind == TurnInventoryKind.Topic));
        Assert.Equal(38, inventory.Bindings.Count(item => item.Kind == TurnInventoryKind.Dependency));
        Assert.Equal(191, Catalog.Dependencies.Sum(item => item.TopicIds.Length));
        Assert.Equal(9, inventory.Gaps.Length);
        Assert.Equal(8, inventory.Evidence.Length);
        Assert.Equal(Enumerable.Range(1, 9).Select(i => "B5-D" + i), inventory.Gaps.Select(item => item.WorkItem));
        Assert.All(inventory.Gaps, gap => Assert.False(string.IsNullOrWhiteSpace(gap.Reason)));
        Assert.All(inventory.Bindings, row =>
        {
            Assert.Equal(TurnSupport.NotEvaluated, row.UnregisteredSupport);
            Assert.Equal(TurnSupport.Pending, row.PendingRuleSupport);
            Assert.Equal(TurnApplicability.NotEvaluated, row.Applicability);
            Assert.NotEmpty(row.GapIds);
            Assert.All(row.GapIds, id => Assert.Contains(inventory.Gaps, gap => gap.Id == id));
            Assert.All(row.EvidenceIds, id => Assert.Contains(inventory.Evidence, evidence => evidence.Id == id));
        });
        foreach (var topic in Catalog.Phases.SelectMany(phase => phase.Topics))
        {
            Assert.Single(inventory.Bindings, row => row.Kind == TurnInventoryKind.Topic && row.Id == topic.Id);
            Assert.NotEmpty(topic.References);
            Assert.NotEmpty(topic.SourcePages);
        }
        foreach (var dependency in Catalog.Dependencies)
        {
            Assert.Single(inventory.Bindings, row => row.Kind == TurnInventoryKind.Dependency && row.Id == dependency.Id);
            Assert.NotEmpty(dependency.References);
            Assert.All(dependency.TopicIds, id => Assert.Contains(inventory.Bindings, row => row.Kind == TurnInventoryKind.Topic && row.Id == id));
        }
        Assert.Equal(TurnSourceReview.SourceMissing, inventory.Bindings.Single(item => item.Id == "pack").SourceReview);
        Assert.Equal(TurnSourceReview.InterpretationOpen, inventory.Bindings.Single(item => item.Id == "mph:dropDuring").SourceReview);
        Assert.Equal(TurnSourceReview.Reconciled, inventory.Bindings.Single(item => item.Id == "rph:assisted").SourceReview);
        Assert.Equal(TurnEvidenceKind.AdjudicatedEffect, inventory.Evidence.Single(item => item.Id == "work-effects").Kind);
        Assert.Equal(TurnEvidenceKind.ObservedHistory, inventory.Evidence.Single(item => item.Id == "fire-history").Kind);
        Assert.Equal(TurnEvidenceKind.RulePredicate, inventory.Evidence.Single(item => item.Id == "clearance-timing").Kind);
        Assert.Equal(TurnEvidenceKind.GovernedSubset, inventory.Evidence.Single(item => item.Id == "dc").Kind);
        Assert.All(inventory.Evidence, item => { Assert.NotEmpty(item.Scope); Assert.NotEmpty(item.References); Assert.NotEmpty(item.Profiles); });
        var reloaded = Read(Document());
        Assert.Equal(JsonSerializer.Serialize(inventory), JsonSerializer.Serialize(reloaded));
    }

    [Theory]
    [InlineData("existing-engine")]
    [InlineData("existing-engine-dc-v1")]
    [InlineData("existing-engine-fire-v1")]
    [InlineData("existing-engine-atmosphere-v1")]
    [InlineData("existing-engine-work-v1")]
    public void EveryProfileKeepsEveryOptionAndInventsNoTopicRoute(string profileId)
    {
        var service = new TurnGuideService(Catalog, []);
        var inventory = service.Inventory;
        var profile = TurnGuideProfiles.Get(profileId);
        foreach (var dependency in profile.IntegratedDependencies)
        {
            var row = inventory.Bindings.Single(item => item.Kind == TurnInventoryKind.Dependency && item.Id == dependency);
            Assert.Contains(inventory.Evidence, item => row.EvidenceIds.Contains(item.Id)
                && item.Kind == TurnEvidenceKind.GovernedSubset && item.Profiles.Contains(profileId));
        }
        foreach (var phase in Catalog.Phases)
        {
            var context = new TurnEvaluationContext("game", "viewer", "side", profileId, phase.Id, phase.Id + ":phase", "stamp");
            var snapshot = service.Describe(new(context, phase.Id, false), new(context, []));
            Assert.Equal(phase.Topics.Select(item => item.Id), snapshot.Options.Select(item => item.Topic.Id));
            Assert.All(snapshot.Options, item =>
            {
                Assert.Empty(item.Capabilities);
                Assert.Null(Assert.Single(item.Decisions).ActionRoute);
                Assert.Equal(TurnApplicability.NotEvaluated, item.Decisions[0].Applicability);
                Assert.Contains(inventory.Bindings, row => row.Id == item.Topic.Id);
            });
        }
        Assert.Equal(TurnSupport.Pending, profile.CommandSupport("pack", true));
        Assert.Equal(TurnSupport.Pending, profile.CommandSupport("ammo", true));
        Assert.Equal(TurnSupport.Pending, profile.CommandSupport("clearance", true));
    }

    [Theory]
    [InlineData("schemaVersion", "2")]
    [InlineData("gaps", "[]")]
    [InlineData("evidence", "[]")]
    [InlineData("bindings", "[]")]
    public void IncompleteDocumentsFailClosed(string field, string json)
    {
        var node = Document();
        node[field] = JsonNode.Parse(json);
        Assert.Throws<InvalidDataException>(() => Read(node));
        node.Remove(field);
        Assert.Throws<InvalidDataException>(() => Read(node));
    }

    [Fact]
    public void MalformedMappingsCannotSilentlyOmitOrPromoteContent()
    {
        Reject(node => node["bindings"]!.AsArray().RemoveAt(0));
        Reject(node => node["bindings"]!.AsArray().Add(node["bindings"]![0]!.DeepClone()));
        Reject(node => node["bindings"]![0] = node["bindings"]![1]!.DeepClone());
        Reject(node => node["bindings"]![0]!["id"] = "unknown");
        Reject(node => node["bindings"]![0]!["kind"] = "Dependency");
        Reject(node => node["gaps"]![0]!["workItem"] = "B5-D99");
        Reject(node => node["bindings"]![0]!.AsObject().Remove("evidenceIds"));
        Reject(node => node["bindings"]![0]!["gapIds"] = new JsonArray());
        Reject(node => node["bindings"]![0]!["gapIds"] = new JsonArray("missing"));
        Reject(node => node["bindings"]![0]!["evidenceIds"] = new JsonArray("missing"));
        Reject(node => node["bindings"]![0]!["evidenceIds"] = new JsonArray("dc", "dc"));
        Reject(node => node["bindings"]![0]!["gapIds"] = new JsonArray("core", "core"));
        Reject(node => node["evidence"]![0]!["profiles"] = new JsonArray());
        Reject(node => node["evidence"]![0]!["references"] = new JsonArray());
        Reject(node => node["evidence"]![0]!["references"] = new JsonArray(" "));
        Reject(node => { var orphan = node["gaps"]![0]!.DeepClone(); orphan["id"] = "unused"; node["gaps"]!.AsArray().Add(orphan); });
        Reject(node => { var orphan = node["evidence"]![0]!.DeepClone(); orphan["id"] = "unused"; node["evidence"]!.AsArray().Add(orphan); });
    }

    [Fact]
    public void InvalidTextAndEnumsAreRejected()
    {
        using var stream = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() => TurnCapabilityInventory.Read(stream, null!));
        Assert.Throws<InvalidDataException>(() => Read(null));
        foreach (var (collection, field) in new[] { ("gaps", "id"), ("gaps", "reason"), ("evidence", "id"), ("evidence", "scope") })
        {
            var node = Document();
            node[collection]![0]![field] = " ";
            Assert.Throws<ArgumentException>(() => Read(node));
        }
        foreach (var collection in new[] { "gaps", "evidence" })
        {
            var node = Document();
            node[collection]!.AsArray().Add(node[collection]![0]!.DeepClone());
            Assert.Throws<ArgumentException>(() => Read(node));
        }
        var unknownProfile = Document();
        unknownProfile["evidence"]![0]!["profiles"] = new JsonArray("unknown");
        Assert.Throws<ArgumentException>(() => Read(unknownProfile));
        foreach (var (collection, field) in new[] { ("evidence", "kind"), ("bindings", "kind"), ("bindings", "sourceReview") })
        {
            foreach (var value in new JsonNode[] { JsonValue.Create("invented")!, JsonValue.Create(99)! })
            {
                var node = Document();
                node[collection]![0]![field] = value.DeepClone();
                Assert.Throws<JsonException>(() => Read(node));
            }
        }
    }

    private static void Reject(Action<JsonObject> mutate)
    {
        var node = Document();
        mutate(node);
        Assert.Throws<InvalidDataException>(() => Read(node));
    }
}
