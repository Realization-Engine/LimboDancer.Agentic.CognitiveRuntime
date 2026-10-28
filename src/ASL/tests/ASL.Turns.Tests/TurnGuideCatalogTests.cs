using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnGuideCatalogTests
{
    [Fact]
    public void HeadlessInventoryRetainsEveryReviewedPhaseAndDependency()
    {
        var catalog = TurnGuideCatalog.Load();
        Assert.Equal(["rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph"], catalog.Phases.Select(phase => phase.Id));
        Assert.Equal(538, catalog.Phases.Sum(phase => phase.Topics.Length));
        Assert.Equal(38, catalog.Dependencies.Length);
        Assert.Equal(191, catalog.Dependencies.Sum(item => item.TopicIds.Length));
        Assert.All(catalog.Dependencies.SelectMany(item => item.TopicIds), id => Assert.NotNull(catalog.FindTopic(id)));
    }

    [Fact]
    public void PendingRulesRemainReadableAndLinked()
    {
        var catalog = TurnGuideCatalog.Load();
        var topic = Assert.IsType<TurnTopic>(catalog.FindTopic("ccph:hiddenHero"));
        Assert.Contains("pending", topic.IntegrationNote, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(topic.Checks);
        var packing = Assert.Single(catalog.DependenciesFor("rph:packStart"), item => item.Id == "pack");
        Assert.Contains("1.13B", packing.Due, StringComparison.Ordinal);
        Assert.Contains("ccph:pinTi", packing.TopicIds);
    }

    [Fact]
    public void TopicIdentityAndVocabularyDoNotCollapseAcrossPhasesOrCase()
    {
        var catalog = TurnGuideCatalog.Load();
        Assert.NotEqual(catalog.FindTopic("mph:cleanup"), catalog.FindTopic("afph:cleanup"));
        Assert.Null(catalog.FindTopic("CCPH:hiddenHero"));
        var glossary = Assert.Single(catalog.Phases, phase => phase.Id == "ccph").Glossary;
        Assert.NotEqual(glossary["DR"], glossary["dr"]);
        Assert.Equal(17, Assert.Single(catalog.Phases, phase => phase.Id == "ccph").AsopLedger.Length);
    }

    [Fact]
    public void LateFlareAndMphFlareHaveDifferentDocumentedDeadlines()
    {
        var dependency = Assert.Single(TurnGuideCatalog.Load().Dependencies, item => item.Id == "tripFlare");
        Assert.Contains("after concealment", dependency.Due, StringComparison.Ordinal);
        Assert.Contains("next Player Turn", dependency.Due, StringComparison.Ordinal);
        Assert.Contains("afph:cleanup", dependency.TopicIds);
    }

    [Fact]
    public void AssemblyHasNoUiOrPlayDependency()
    {
        var names = typeof(TurnGuideCatalog).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name ?? "");
        Assert.DoesNotContain(names, name => name.Contains("MapStudio", StringComparison.Ordinal)
            || name.Contains("AspNetCore", StringComparison.Ordinal)
            || name.Contains(".Play", StringComparison.Ordinal));
    }
}
