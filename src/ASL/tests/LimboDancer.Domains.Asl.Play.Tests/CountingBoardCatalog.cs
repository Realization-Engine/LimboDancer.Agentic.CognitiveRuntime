using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Read;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// A board catalog that counts its lookups (the Play Step Plan, tasks 1 and 2): every search of the map looks a board up at each step, so an
/// answer the planner kept for a state is one that looks no board up when asked again.
/// </summary>
internal sealed class CountingBoardCatalog(IBoardCatalog inner) : IBoardCatalog
{
    public int Lookups
    {
        get; private set;
    }

    public BoardReadResult TryGetBoard(BoardRef board, string? version = null)
    {
        Lookups++;
        return inner.TryGetBoard(board, version);
    }
}
