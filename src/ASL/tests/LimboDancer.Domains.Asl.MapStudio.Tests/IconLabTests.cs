using LimboDancer.Domains.Asl.MapStudio.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>The Studio's icons in wwwroot/icons and the Icon Lab that shows them.</summary>
public sealed class IconLabTests(StudioFactory factory) : IClassFixture<StudioFactory>
{
    [Fact]
    public void EveryIconTheManifestNamesHasItsFileAndDrawsInThePaletteVariables()
    {
        var icons = factory.Services.GetRequiredService<IconLibrary>();
        Assert.Null(icons.Problem);
        Assert.Equal(["zoom-in", "zoom-out", "fit", "rotate", "selection", "los", "range", "evidence"], icons.Icons.Select(icon => icon.Key));
        Assert.Equal(["Map toolbar", "Inspector tab"], icons.Groups);
        Assert.All(icons.Icons, icon =>
        {
            Assert.NotNull(icon.Markup);
            Assert.StartsWith("<svg", icon.Markup, StringComparison.Ordinal);
            Assert.Contains("viewBox=\"0 0 24 24\"", icon.Markup, StringComparison.Ordinal);
            Assert.Contains("var(--i-ink,", icon.Markup, StringComparison.Ordinal);
            Assert.Contains("width=\"20\" height=\"20\"", IconLibrary.Sized(icon, 20), StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task TheIconLabShowsEachIconInPlaceAtEachSizeAndLarge()
    {
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync(new Uri("/lab/icons", UriKind.Relative));
        Assert.Contains("Icon Lab", html, StringComparison.Ordinal);
        Assert.Contains("class=\"icon-lab icon-palette-mj\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Rotate map\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Evidence\"", html, StringComparison.Ordinal);
        Assert.Equal(8, CountOf(html, "<figure>"));
        Assert.Equal(8, CountOf(html, "width=\"144\" height=\"144\""));
        Assert.DoesNotContain("<canvas", html, StringComparison.Ordinal);
        Assert.Contains(">Lab</span>", html, StringComparison.Ordinal);
        Assert.Contains("href=\"lab/icons\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingManifestOrFileIsReportedNotThrown()
    {
        var root = Directory.CreateTempSubdirectory("icon-lab-");
        try
        {
            using (var provider = new PhysicalFileProvider(root.FullName))
            {
                var none = new IconLibrary(provider);
                Assert.Empty(none.Icons);
                Assert.Contains("icons.json is missing", none.Problem, StringComparison.Ordinal);
            }

            Directory.CreateDirectory(Path.Combine(root.FullName, IconLibrary.Folder));
            File.WriteAllText(Path.Combine(root.FullName, IconLibrary.Folder, IconLibrary.Manifest), """[{ "key": "gone", "label": "Gone", "group": "Map toolbar" }]""");
            using (var provider = new PhysicalFileProvider(root.FullName))
            {
                var icon = Assert.Single(new IconLibrary(provider).Icons);
                Assert.Equal("gone", icon.Key);
                Assert.Null(icon.Markup);
                Assert.Null(IconLibrary.Sized(icon, 20));
            }
        }
        finally
        {
            root.Delete(true);
        }
    }

    private static int CountOf(string text, string part)
    {
        var count = 0;
        for (var at = text.IndexOf(part, StringComparison.Ordinal); at >= 0; at = text.IndexOf(part, at + part.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
