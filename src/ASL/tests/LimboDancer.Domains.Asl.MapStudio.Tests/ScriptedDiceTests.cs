using Bunit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.MapStudio.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The Studio's scripted dice for UI test runs: queued dice are drawn first, then system dice; a value outside 1 to 6 queues
/// nothing; the option exists only in Development with <c>Play:ScriptedDice</c> set; and the Play page shows its panel only
/// when the option is on.
/// </summary>
public sealed class ScriptedDiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-scripted-dice-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void QueuedDiceAreDrawnFirstThenSystemDice()
    {
        var scripted = new ScriptedDice();
        Assert.True(scripted.Enqueue([1, 1, 6]));
        Assert.Equal([1, 1, 6], scripted.Queued);

        Assert.Equal([1, 1], scripted.Roller.Roll(new RollRequest(2, 6)).Values);
        Assert.Equal(6, Assert.Single(scripted.Roller.Roll(new RollRequest(1, 6)).Values));
        Assert.Empty(scripted.Queued);
        Assert.All(scripted.Roller.Roll(new RollRequest(20, 6)).Values, value => Assert.InRange(value, 1, 6));
    }

    [Fact]
    public void AValueOutsideOneToSixQueuesNothing()
    {
        var scripted = new ScriptedDice();
        Assert.False(scripted.Enqueue([1, 7]));
        Assert.False(scripted.Enqueue([0]));
        Assert.Empty(scripted.Queued);

        Assert.True(scripted.Enqueue([2, 3]));
        scripted.Clear();
        Assert.Empty(scripted.Queued);
    }

    [Theory]
    [InlineData("Development", "true", true)]
    [InlineData("Development", "false", false)]
    [InlineData("Development", null, false)]
    [InlineData("Production", "true", false)]
    public void TheOptionExistsOnlyInDevelopmentWithTheFlag(string environment, string? flag, bool enabled)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(flag is null ? [] : new Dictionary<string, string?> { ["Play:ScriptedDice"] = flag })
            .Build();
        Assert.Equal(enabled, ScriptedDice.Enabled(new Environment(environment), configuration));
    }

    [Fact]
    public void ThePlayPageShowsTheQueueOnlyWhenTheOptionIsOn()
    {
        var scripted = new ScriptedDice();
        using (var off = Context(null))
        {
            Assert.Empty(off.Render<PlayPage>().FindAll("#scripted-dice"));
        }

        using var on = Context(scripted);
        var page = on.Render<PlayPage>();
        Assert.Contains("Scripted dice are on (development only).", page.Find("#scripted-dice").TextContent, StringComparison.Ordinal);

        page.Find("#scripted-dice-values").Change("1 1 7");
        page.Find("#scripted-dice-add").Click();
        Assert.Contains("Each die is a number from 1 to 6.", page.Find("#scripted-dice").TextContent, StringComparison.Ordinal);
        Assert.Empty(scripted.Queued);

        page.Find("#scripted-dice-values").Change("1 1 3 3");
        page.Find("#scripted-dice-add").Click();
        Assert.Equal("Queued: 1 1 3 3", page.Find("#scripted-dice-queued").TextContent);
        Assert.Equal([1, 1, 3, 3], scripted.Queued);

        page.Find("#scripted-dice-clear").Click();
        Assert.Equal("Queued: none", page.Find("#scripted-dice-queued").TextContent);
    }

    private BunitContext Context(ScriptedDice? scripted)
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        var boards = new BuildingBoards(maps);
        var live = new LivePlay(library, boards, scripted?.Roller);
        var games = new GameLibrary(library, boards, live);
        var context = new BunitContext();
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(live);
        context.Services.AddSingleton(games);
        context.Services.AddSingleton(new GameMaps(boards, maps, new RenderCache(), library, games));
        context.UseViewport();
        context.Services.AddSingleton(new StudioLos(boards, maps, options));
        if (scripted is not null)
        {
            context.Services.AddSingleton(scripted);
        }

        return context;
    }

    private sealed class Environment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "LimboDancer.Domains.Asl.MapStudio";

        public string ContentRootPath { get; set; } = "";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
