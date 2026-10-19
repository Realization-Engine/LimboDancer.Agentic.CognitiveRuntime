using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Units.Rendering;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Option D on the server (the Counter Layer Design, sections 6 and 9, decision L5): what <see cref="UnitLayerSync"/> sends, applied to a model of
/// the browser's layer, leaves the layer the browser holds equal to the new layer, fragment by fragment and in order, at every step of the
/// played games stepped forward, back, and by jumps; nothing is sent when nothing changed; the whole layer when the browser's is not known.
/// </summary>
public sealed class UnitLayerSyncTests(CounterLayerCases cases) : IClassFixture<CounterLayerCases>
{
    /// <summary>The browser's counter layer as <c>setUnits</c> and <c>applyUnitPatch</c> leave it.</summary>
    private sealed class BrowserLayer
    {
        public int Version { get; private set; }

        public List<(string Id, string Markup)> Fragments { get; private set; } = [];

        public string? UnitSet { get; private set; }

        public void Apply(UnitLayerSend send, UnitOverlay? overlay)
        {
            switch (send.Kind)
            {
                case UnitLayerSendKind.Whole:
                    Assert.Equal(overlay?.Svg, send.Svg);
                    (Version, Fragments, UnitSet) = (send.Version, [.. overlay?.Fragments.Select(fragment => (fragment.Id, fragment.Markup)) ?? []], overlay?.SetId);
                    break;
                case UnitLayerSendKind.Patch:
                    Assert.True(Patch(send.Patch!));
                    break;
            }
        }

        /// <summary>As <c>applyUnitPatch</c>: refused unless the layer is the patch's base; removals, then upserts in place or before their next.</summary>
        public bool Patch(UnitLayerPatch patch)
        {
            if (patch.Base != Version)
            {
                return false;
            }

            Fragments.RemoveAll(fragment => patch.Removals.Contains(fragment.Id));
            foreach (var upsert in patch.Upserts)
            {
                var at = Fragments.FindIndex(fragment => fragment.Id == upsert.Id);
                if (at >= 0)
                {
                    Fragments[at] = (upsert.Id, upsert.Markup);
                    continue;
                }

                var before = upsert.Before is null ? -1 : Fragments.FindIndex(fragment => fragment.Id == upsert.Before);
                Fragments.Insert(before < 0 ? Fragments.Count : before, (upsert.Id, upsert.Markup));
            }

            (Version, UnitSet) = (patch.Version, patch.UnitSet);
            return true;
        }
    }

    private static void Holds(BrowserLayer browser, UnitOverlay overlay)
    {
        Assert.Equal(overlay.Fragments.Select(fragment => (fragment.Id, fragment.Markup)), browser.Fragments);
        Assert.Equal(overlay.SetId, browser.UnitSet);
    }

    [Theory]
    [InlineData("forward")]
    [InlineData("back")]
    [InlineData("jumps")]
    public void ThePatchesLeaveTheBrowsersLayerAsTheNewLayer(string order)
    {
        var steps = cases.Steps("guards-dl-01", Perspective.AdjudicatorName, UnitStyles.Digital).Select(item => item.Build(null)).ToArray();
        IEnumerable<UnitOverlay> sequence = order switch
        {
            "forward" => steps,
            "back" => steps.Reverse(),
            _ => Enumerable.Range(0, steps.Length).Select(index => steps[(int)(index * 7919L % steps.Length)]),
        };
        var (sync, browser) = (new UnitLayerSync(), new BrowserLayer());
        var kinds = new Dictionary<UnitLayerSendKind, int>();
        foreach (var overlay in sequence)
        {
            var send = sync.Next(overlay);
            kinds[send.Kind] = kinds.GetValueOrDefault(send.Kind) + 1;
            browser.Apply(send, overlay);
            Holds(browser, overlay);
            Assert.Equal(sync.Version, browser.Version);
        }

        // The first layer goes whole; after it, a step that moved a few counters is a patch, and a step that moved none sends nothing.
        Assert.True(kinds.GetValueOrDefault(UnitLayerSendKind.Patch) > steps.Length / 3, string.Join(", ", kinds));
        Assert.True(kinds.GetValueOrDefault(UnitLayerSendKind.Nothing) > 0 || order == "jumps", string.Join(", ", kinds));
    }

    [Fact]
    public void APatchCarriesOnlyTheHexesThatChanged()
    {
        var steps = cases.Steps("guards-dl-01", Perspective.AdjudicatorName, UnitStyles.Digital).Select(item => item.Build(null)).ToArray();
        var sync = new UnitLayerSync();
        sync.Next(steps[^2]);
        var send = sync.Next(steps[^1]);
        var changed = steps[^1].Fragments.Count(fragment => !steps[^2].Fragments.Contains(fragment));
        if (send.Kind == UnitLayerSendKind.Patch)
        {
            Assert.Equal(changed, send.Patch!.Upserts.Count);
            Assert.True(send.Patch.Size < steps[^1].Svg.Length / 2);
        }
        else
        {
            Assert.Equal(0, changed);
        }
    }

    [Fact]
    public void TheWholeLayerGoesWhenTheBrowsersIsNotKnown()
    {
        var steps = cases.Steps("guards-dl-01", Perspective.AdjudicatorName, UnitStyles.Digital).Select(item => item.Build(null)).ToArray();
        var (first, second) = (steps[60], steps[61..].First(step => step.Svg != steps[60].Svg));
        var sync = new UnitLayerSync();
        Assert.Equal(UnitLayerSendKind.Whole, sync.Next(first).Kind);
        Assert.Equal(UnitLayerSendKind.Nothing, sync.Next(first).Kind);

        // A board load: the browser holds no layer.
        sync.Reset();
        Assert.Equal(UnitLayerSendKind.Whole, sync.Next(first).Kind);

        // A refused patch: the next layer goes whole.
        var patch = sync.Next(second);
        Assert.Equal(UnitLayerSendKind.Patch, patch.Kind);
        Assert.False(new BrowserLayer().Patch(patch.Patch!));
        sync.Refused();
        Assert.Equal(UnitLayerSendKind.Whole, sync.Next(second).Kind);

        // Another set (another view or game) goes whole; no layer clears it once.
        Assert.Equal(UnitLayerSendKind.Whole, sync.Next(second with { SetId = "another" }).Kind);
        Assert.Equal(UnitLayerSendKind.Whole, sync.Next(null).Kind);
        Assert.Equal(UnitLayerSendKind.Nothing, sync.Next(null).Kind);
    }

    [Fact]
    public void TheFragmentsMakeTheWholeLayer()
    {
        foreach (var item in cases.All.Where((_, index) => index % 50 == 0))
        {
            var overlay = item.Build(null);
            Assert.Equal(overlay.Svg, string.Concat([overlay.Open, .. overlay.Fragments.Select(fragment => fragment.Markup), "</g>"]));
            Assert.Equal(overlay.Fragments.Count, overlay.Fragments.Select(fragment => fragment.Id).Distinct(StringComparer.Ordinal).Count());
            Assert.All(overlay.Fragments, fragment => Assert.Matches("^[0-9a-f]{24}$", fragment.Hash));
        }
    }
}
