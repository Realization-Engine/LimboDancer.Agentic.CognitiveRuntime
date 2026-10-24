using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Units;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Units.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>The Unit Lab's Art (ASL Unit Figure Art Plan, stage 3): the sample counters, what each figure lacks, and the tab.</summary>
public sealed class FigureArtLabTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-figure-art-" + Guid.NewGuid().ToString("N"));
    private readonly BunitContext context = new();
    private readonly UnitLibrary library;
    private readonly FigureArtLab lab;

    public FigureArtLabTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        library = new UnitLibrary(options);
        var figures = Directory.CreateDirectory(Path.Combine(root, "figures")).FullName;
        File.Copy(Path.Combine(UnitsFolder(), "figures", FigureArtLab.PlanFile), Path.Combine(figures, FigureArtLab.PlanFile));
        lab = new FigureArtLab(figures);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(lab);
    }

    public void Dispose()
    {
        context.Dispose();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("standing", "asl:squad", null, null)]
    [InlineData("crew", "asl:crew", null, null)]
    [InlineData("leader", "asl:leader", null, null)]
    [InlineData("wounded", "asl:hero", "wounded", null)]
    [InlineData("broken", "asl:squad", "broken", null)]
    [InlineData("pinned", "asl:squad", null, "asl:pinned")]
    [InlineData("berserk", "asl:squad", null, "asl:berserk")]
    [InlineData("prisoner", "asl:squad", null, "asl:captured")]
    [InlineData("vehicle-crew", "asl:vehicle-crew", null, null)]
    public void EachPoseIsJudgedOnTheCounterThatTakesIt(string pose, string kind, string? face, string? state)
    {
        var sample = FigureArtLab.Sample(library.Examples, pose, "russian");
        Assert.NotNull(sample);
        var (document, shown) = sample.Value;
        Assert.Equal(kind, document.Kind);
        Assert.Equal("russian", document.Side);
        Assert.Equal(face, shown);
        Assert.Empty(document.Attached);
        if (state is not null)
        {
            Assert.Contains(state, document.States);
        }
    }

    [Fact]
    public void ASideWithoutKitLacksItsHelmetAndWeapon()
    {
        // Every catalog side has its kit drawn (stage 6); a side outside the catalog has none.
        Assert.Contains("helmet", lab.Missing("standing", "japanese"));
        // A traced pose carries its own weapon; a pose that still holds a drawn one lacks it on a side without kit.
        if (FigureArt.PoseNames.FirstOrDefault(pose => lab.Art.Pose(pose) is { Grip: not null }) is { } armed)
        {
            Assert.Contains(lab.Art.Pose(armed)!.Weapon == "smg" ? "SMG" : "rifle", lab.Missing(armed, "japanese"));
        }
        Assert.DoesNotContain("helmet", lab.Missing("standing", "german"));
        // The user accepted every infantry pose and helmet on 2026-10-18: a German rifleman lacks nothing.
        Assert.Empty(lab.Missing("standing", "german"));
        // Any pose with no art yet lacks its pose; which one changes as the picks are traced.
        if (FigureArt.PoseNames.FirstOrDefault(pose => lab.PoseState(pose) == "none") is { } bare)
        {
            Assert.Contains("pose", lab.Missing(bare, "german"));
        }
    }

    [Fact]
    public void KitIsDrawnOnceItExistsAndAPoseOnceItIsAccepted()
    {
        Assert.True(lab.Has("german-helmet"));
        Assert.True(lab.Has("russian-rifle"));
        Assert.True(lab.Has("british-helmet"));
        Assert.False(lab.Has("japanese-helmet"));
        Assert.Equal(FigureArt.BuiltIn.Pose("standing") is not null, lab.Has("pose-standing"));
        Assert.Contains("<path", lab.Thumbnail("german-helmet", 22), StringComparison.Ordinal);
        Assert.Null(lab.Thumbnail("japanese-helmet", 22));
    }

    [Fact]
    public void TheCoverageGridDrawsEverySideInEveryPose()
    {
        var view = context.Render<FigureArtBench>();
        Assert.Equal(FigureArtLab.Sides.Count, view.FindAll(".figure-art-row[data-side]").Count);
        Assert.Equal(FigureArtLab.Sides.Count * FigureArt.PoseNames.Count, view.FindAll(".figure-art-cell").Count);
        Assert.Equal(FigureArtLab.Sides.Count * FigureArt.PoseNames.Count, view.FindAll(".figure-art-cell svg").Count);
        // Every infantry pose and helmet was accepted on 2026-10-18: no catalog side's figure lacks anything.
        Assert.Empty(view.FindAll(".figure-art-missing"));
    }

    [Fact]
    public void ThePromptsTabAsksForOneFigureAtATimeAndWaitsOnNothingOnceEveryPoseIsDrawn()
    {
        context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("units/lab?tab=art&art=prompts");
        var view = context.Render<FigureArtBench>();
        // Every pose is traced and accepted, so no brief waits for a batch; the prompt itself is tested with the Icon Lab's bench.
        Assert.Empty(view.FindAll(".icon-bench-group li"));
        Assert.NotNull(view.Find("#icon-bench-queue-empty"));
        Assert.True(view.Find("#icon-bench-single").HasAttribute("checked"));
    }

    private static string UnitsFolder()
    {
        for (var directory = new DirectoryInfo(Environment.GetEnvironmentVariable("ASL_REPO") is { Length: > 0 } repo ? repo : AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var units = Path.Combine(directory.FullName, "src", "ASL", "units");
            if (Directory.Exists(units))
            {
                return units;
            }
        }

        throw new DirectoryNotFoundException("The repository's src/ASL/units folder was not found.");
    }
}
