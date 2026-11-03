using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Rendering;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// A Campaign game's Formation ownership as the tile painter reads it (Screen-Bounded Map Analysis, section 3.1): each hex's parent by ordinal,
/// the mean hexes a parent holds (for the level at which boundaries are drawn), and a token naming the ownership, which keys the tiles.
/// </summary>
public sealed record BoundaryParents(string Token, int[] Parents, double HexesPerParent);

/// <summary>
/// The boundary parents of the games the tile endpoint has been asked about, read from the game's ownership metadata
/// (<see cref="CampaignImport.ReadOwnership"/>) once per game and board version; the last few are kept.
/// </summary>
public sealed class BoundaryParentsCache(CampaignImport imports)
{
    private const int Bound = 4;

    private readonly ConcurrentDictionary<string, Lazy<BoundaryParents?>> held = new(StringComparer.Ordinal);

    /// <summary>The game's parents on the board, or null when the game has no ownership metadata for it.</summary>
    public BoundaryParents? For(string game, StudioBoard board)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(board);
        if (!game.StartsWith("campaign-", StringComparison.Ordinal) || board.InputFor(BoardView.Exact) is not { } input)
        {
            return null;
        }

        var key = game + "/" + board.Ref.Value + "/" + board.Version;
        if (held.Count >= Bound && !held.ContainsKey(key))
        {
            held.Clear();
        }

        return held.GetOrAdd(key, _ => new Lazy<BoundaryParents?>(() =>
        {
            IReadOnlyDictionary<string, string>? ownership;
            try
            {
                ownership = imports.ReadOwnership(game, board.Ref.Value, board.Version);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                return null;
            }

            if (ownership is null || ownership.Count == 0)
            {
                return null;
            }

            var (parents, count) = CampaignMapBoundaries.Parents(input.Grid.Geometry, ownership);
            var text = string.Join('\n', ownership.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + "=" + pair.Value));
            var token = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16];
            return new BoundaryParents(token, parents, ownership.Count / (double)Math.Max(1, count));
        }, LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }
}
