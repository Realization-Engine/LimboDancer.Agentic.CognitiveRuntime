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
        Assert.Equal(["zoom-in", "zoom-out", "fit", "rotate", "play", "proposal", "records", "units", "scripted-dice", "standing", "step", "selection", "los", "range", "evidence"], icons.Icons.Select(icon => icon.Key));
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
        Assert.Equal(15, CountOf(html, "<figure>"));
        Assert.Equal(15, CountOf(html, "width=\"144\" height=\"144\""));
        Assert.DoesNotContain("<canvas", html, StringComparison.Ordinal);
        Assert.Contains(">Lab</span>", html, StringComparison.Ordinal);
        Assert.Contains("href=\"lab/icons\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheBoardViewersToolbarAndInspectorTabsDrawTheirIconsAndKeepTheirWords()
    {
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync(new Uri("/boards/ab-synthetic", UriKind.Relative));
        foreach (var (id, label) in new[] { ("viewer-fit", "Fit"), ("map-rotate", "Rotate map") })
        {
            var button = Element(html, $"id=\"{id}\"");
            Assert.Contains("class=\"icon-button\"", button, StringComparison.Ordinal);
            Assert.Contains("<svg", button, StringComparison.Ordinal);
            Assert.Contains($"<span class=\"visually-hidden\">{label}</span>", button, StringComparison.Ordinal);
        }

        foreach (var (key, label) in new[] { ("selection", "Selection"), ("los", "LOS"), ("range", "Range"), ("evidence", "Evidence") })
        {
            var tab = Element(html, $"id=\"viewer-tab-{key}\"");
            Assert.Contains("icon-tab", tab, StringComparison.Ordinal);
            Assert.Contains($"title=\"{label}\"", tab, StringComparison.Ordinal);
            Assert.Contains($"<span class=\"visually-hidden\">{label}</span>", tab, StringComparison.Ordinal);
        }
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

    /// <summary>The button whose opening tag holds an attribute, from that tag to its closing tag.</summary>
    private static string Element(string html, string attribute)
    {
        var at = html.IndexOf(attribute, StringComparison.Ordinal);
        Assert.True(at >= 0, $"No element with {attribute}.");
        var start = html.LastIndexOf("<button", at, StringComparison.Ordinal);
        var end = html.IndexOf("</button>", at, StringComparison.Ordinal);
        return html[start..end];
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
