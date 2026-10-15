using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Explicit immutable playable areas, separate from optional inspection metadata and terrain bytes.</summary>
public sealed class CampaignPlayableFootprints(string boardsRoot)
{
    private readonly ConcurrentDictionary<(BoardRef Board, string Version), IReadOnlySet<HexName>> accepted = new();

    public IReadOnlySet<HexName>? Read(BoardRef board, string version, BoardGeometry geometry)
    {
        if (accepted.TryGetValue((board, version), out var cached))
        {
            return cached;
        }
        var path = PathFor(board, version);
        if (!File.Exists(path))
        {
            return null;
        }
        var hexes = CampaignMapOwnership.Read(File.ReadAllText(path, Encoding.UTF8), board, version, geometry)
            .Keys.Select(HexName.Parse).ToFrozenSet();
        return accepted.GetOrAdd((board, version), hexes);
    }

    // Only the import path publishes, after replaying proposed and retained histories with this footprint.
    public void Publish(BoardRef board, string version, BoardGeometry geometry, string ownership)
    {
        var proposed = CampaignMapOwnership.Read(ownership, board, version, geometry).Keys.Select(HexName.Parse).ToFrozenSet();
        var path = PathFor(board, version);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var stage = path + ".stage-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(stage, ownership, new UTF8Encoding(false));
            try
            {
                File.Move(stage, path);
            }
            catch (IOException) when (File.Exists(path))
            {
                if (Read(board, version, geometry) is not { } prior || !prior.SetEquals(proposed))
                {
                    throw new InvalidDataException("An accepted playable footprint cannot be replaced.");
                }
            }
            accepted.TryAdd((board, version), proposed);
        }
        finally
        {
            if (File.Exists(stage))
            {
                File.Delete(stage);
            }
        }
    }

    private string PathFor(BoardRef board, string version)
    {
        var key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(board.Value + "\n" + version)));
        return Path.Combine(boardsRoot, ".campaign-footprints", key + ".json");
    }
}
