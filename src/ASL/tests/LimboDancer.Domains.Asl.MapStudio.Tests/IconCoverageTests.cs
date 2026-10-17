using LimboDancer.Domains.Asl.MapStudio.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The Icon Lab's coverage: wwwroot/icons/icon-coverage.json is what <see cref="IconCoverageScanner"/> reads from the Razor sources. Set
/// <c>ASL_ICONS_UPDATE_COVERAGE=1</c> to rewrite it after adding or changing a button or a tab.
/// </summary>
public sealed class IconCoverageTests(StudioFactory factory) : IClassFixture<StudioFactory>
{
    private const string UpdateVariable = "ASL_ICONS_UPDATE_COVERAGE";

    private string ProjectRoot => factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;

    private string CoveragePath => Path.Combine(ProjectRoot, "wwwroot", IconLibrary.Folder, IconCoverage.File);

    [Fact]
    public void TheCoverageFileMatchesTheSources()
    {
        var scanned = IconCoverageScanner.Scan(ProjectRoot).Write();
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            File.WriteAllText(CoveragePath, scanned);
        }

        Assert.True(File.Exists(CoveragePath), $"{CoveragePath} is missing; run this test with {UpdateVariable}=1.");
        Assert.True(File.ReadAllText(CoveragePath).ReplaceLineEndings("\n") == scanned,
            $"The buttons or tabs changed since {IconCoverage.File} was written; run this test with {UpdateVariable}=1 and commit the file.");
    }

    [Fact]
    public void EveryIconACoveredControlNamesIsInTheLibrary()
    {
        var icons = factory.Services.GetRequiredService<IconLibrary>();
        var coverage = IconCoverageScanner.Scan(ProjectRoot);
        Assert.All(coverage.Controls.Where(control => control.Status == IconCoverage.HasIcon && !control.Dynamic), control =>
            Assert.True(icons.Find(control.Icon!) is not null, $"No icon {control.Icon} for {control.Label}."));
    }

    [Fact]
    public void TheScannerFindsTheMapToolbarAndTheInspectorTabsAndWhereTheyAppear()
    {
        var coverage = IconCoverageScanner.Scan(ProjectRoot);
        var play = Assert.Single(coverage.Components, component => component.Name == "Play");
        Assert.Contains("/games/play", play.Pages);
        Assert.Contains(play.Controls, control => control.Id == "play-map-fit" && control.Icon == "fit" && control.Status == IconCoverage.HasIcon);
        Assert.Contains(play.Controls, control => control is { Kind: "tab", Id: "proposal", Status: IconCoverage.Needed });

        var inspector = Assert.Single(coverage.Components, component => component.Name == "BoardInspector");
        Assert.Equal(["selection", "los", "range", "evidence"], inspector.Controls.Where(control => control.Kind == "tab").Select(control => control.Icon));
        Assert.Contains("BoardWorkspace", inspector.UsedBy);
        Assert.Contains("/games/replay", inspector.Pages);

        // Proposal is written in Play but shown in BoardInspector's strip, beside its own four tabs; the map toolbar is one strip.
        var proposal = Assert.Single(play.Controls, control => control.Id == "proposal");
        Assert.Equal(("BoardInspector tabs", "BoardInspector"), (proposal.Strip, proposal.RendersIn));
        Assert.All(inspector.Controls.Where(control => control.Kind == "tab"), control => Assert.Equal("BoardInspector tabs", control.Strip));
        Assert.Single(play.Controls.Where(control => control.Id is "play-map-zoom-in" or "play-map-fit").Select(control => control.Strip).Distinct());
        Assert.Equal("Play PaneTabs", Assert.Single(play.Controls, control => control.Id == "map").Strip);

        Assert.DoesNotContain(coverage.Components, component => component.Name is "IconGallery" or "InspectorTabs");
        var lab = Assert.Single(coverage.Components, component => component.Name == "IconLab");
        Assert.Equal(["icons", "coverage", "prompts", "batches"], lab.Controls.Select(control => control.Id));
    }
}
