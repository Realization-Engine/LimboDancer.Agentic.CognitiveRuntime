using Bunit;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.MapStudio.Components.Board;
using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>Styled and Hex Facts Views Plan, step 1b (decision V5): the map style a game is drawn in, in Play and Replay.</summary>
public sealed class MapStylePickerTests : IDisposable
{
    // The fake board with a Styled input made on first use, as a VASL board's vectorization is.
    private static readonly StudioBoard Styled = FakeBoardProvider.Board with
    {
        StyledRender = new Lazy<BoardRenderInput>(() => FakeBoardProvider.Board.Render),
    };

    private readonly BunitContext context = new();

    public MapStylePickerTests() => context.JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void OffersExactStyledAndHexFactsButNotComparison()
    {
        var board = Styled;
        Assert.True(board.HasStyled);
        Assert.Equal([BoardView.Exact, BoardView.Styled, BoardView.HexFacts], MapStylePicker.ViewsFor(board));

        // A board without a Styled input offers Exact and Hex facts.
        var plain = FakeBoardProvider.Board;
        Assert.False(plain.HasStyled);
        Assert.Equal([BoardView.Exact, BoardView.HexFacts], MapStylePicker.ViewsFor(plain));

        Assert.Equal(BoardView.HexFacts, MapStylePicker.Offered(board, "hexfacts"));
        Assert.Null(MapStylePicker.Offered(board, "comparison"));
        Assert.Null(MapStylePicker.Offered(plain, "styled"));
        Assert.Null(MapStylePicker.Offered(board, null));
    }

    [Fact]
    public void ChoosingAStyleRaisesItAndKeepsItInTheBrowserAndTheAddress()
    {
        BoardView? chosen = null;
        var picker = context.Render<MapStylePicker>(parameters => parameters
            .Add(component => component.Board, Styled)
            .Add(component => component.Value, BoardView.Exact)
            .Add(component => component.Id, "play-map-style")
            .Add(component => component.ValueChanged, (BoardView view) => chosen = view));

        Assert.Equal(["exact", "styled", "hexfacts"], picker.FindAll("#play-map-style option").Select(option => option.GetAttribute("value")));
        // The Styled input is made first, on a background task, as the board viewer makes it.
        picker.Find("#play-map-style").Change("styled");
        picker.WaitForAssertion(() =>
        {
            Assert.Equal(BoardView.Styled, chosen);
            Assert.Contains(context.JSInterop.Invocations, call => call.Identifier == "localStorage.setItem"
                && call.Arguments.SequenceEqual(new object?[] { MapStylePicker.StorageKey, "styled" }));
            Assert.Contains(context.JSInterop.Invocations, call => call.Identifier == "history.replaceState"
                && call.Arguments[2] is string address && address.Contains("style=styled", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void TheAddressStyleWinsOverTheKeptOneWhenThePageOpens()
    {
        context.JSInterop.Setup<string?>("localStorage.getItem", MapStylePicker.StorageKey).SetResult("styled");
        BoardView? chosen = null;
        context.Render<MapStylePicker>(parameters => parameters
            .Add(component => component.Board, Styled)
            .Add(component => component.Value, BoardView.Exact)
            .Add(component => component.Asked, "hexfacts")
            .Add(component => component.ValueChanged, (BoardView view) => chosen = view));
        Assert.Equal(BoardView.HexFacts, chosen);

        // Without one in the address, the kept style is shown.
        BoardView? kept = null;
        context.Render<MapStylePicker>(parameters => parameters
            .Add(component => component.Board, Styled)
            .Add(component => component.Value, BoardView.Exact)
            .Add(component => component.ValueChanged, (BoardView view) => kept = view));
        Assert.Equal(BoardView.Styled, kept);
    }

    public void Dispose() => context.Dispose();
}
