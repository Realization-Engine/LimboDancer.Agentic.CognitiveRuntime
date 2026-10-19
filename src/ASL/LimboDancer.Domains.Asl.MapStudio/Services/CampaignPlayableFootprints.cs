using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Explicit immutable playable areas, separate from optional inspection metadata and terrain bytes.</summary>
/// <remarks>
/// A board without a footprint, which is every board of a game that is not a campaign's, is remembered as absent for a few seconds: the rules look
/// boards up many times a revision, and asking the disk each time was two thirds of a Play step's server time (the SVG memory analysis, section 13).
/// <see cref="Publish"/> forgets the absence at once; the few seconds cover a footprint published by another Studio sharing the boards folder.
/// </remarks>
public sealed class CampaignPlayableFootprints(string boardsRoot, TimeProvider? clock = null)
{
    /// <summary>How long a board found without a footprint is answered from memory before the disk is asked again.</summary>
    public static readonly TimeSpan AbsenceKept = TimeSpan.FromSeconds(5);

    private readonly ConcurrentDictionary<(BoardRef Board, string Version), IReadOnlySet<HexName>> accepted = new();
    private readonly ConcurrentDictionary<(BoardRef Board, string Version), DateTimeOffset> absent = new();
    private readonly TimeProvider time = clock ?? TimeProvider.System;

    public IReadOnlySet<HexName>? Read(BoardRef board, string version, BoardGeometry geometry)
    {
        if (accepted.TryGetValue((board, version), out var cached))
        {
            return cached;
        }

        var now = time.GetUtcNow();
        if (absent.TryGetValue((board, version), out var seen) && now - seen < AbsenceKept)
        {
            return null;
        }

        var path = PathFor(board, version);
        if (!File.Exists(path))
        {
            absent[(board, version)] = now;
            return null;
        }

        absent.TryRemove((board, version), out _);
        var hexes = CampaignMapOwnership.Read(File.ReadAllText(path, Encoding.UTF8), board, version, geometry)
            .Keys.Select(HexName.Parse).ToFrozenSet();
        return accepted.GetOrAdd((board, version), hexes);
    }

    // Only the import path publishes, after replaying proposed and retained histories with this footprint.
    public void Publish(BoardRef board, string version, BoardGeometry geometry, string ownership)
    {
        var proposed = CampaignMapOwnership.Read(ownership, board, version, geometry).Keys.Select(HexName.Parse).ToFrozenSet();
        absent.TryRemove((board, version), out _);
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
