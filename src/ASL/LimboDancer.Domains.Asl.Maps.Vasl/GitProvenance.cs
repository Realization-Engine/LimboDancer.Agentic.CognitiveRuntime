using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>Git object identifiers, computed without running git.</summary>
public static class GitBlob
{
    /// <summary>The Git blob id of the given bytes: SHA-1 of <c>blob &lt;length&gt;\0&lt;bytes&gt;</c>.</summary>
    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "Git object ids are SHA-1 by definition; this is identification, not security.")]
    public static string Sha(ReadOnlySpan<byte> content)
    {
        var header = Encoding.ASCII.GetBytes($"blob {content.Length}\0");
        using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        sha1.AppendData(header);
        sha1.AppendData(content);
        return Convert.ToHexStringLower(sha1.GetHashAndReset());
    }
}

/// <summary>
/// Read-only facts about a local git checkout: the HEAD commit and the blob ids recorded in the index
/// (versions 2 and 3). Index blob ids are the committed content, independent of checkout line-ending conversion.
/// </summary>
public sealed class GitCheckout
{
    private readonly IReadOnlyDictionary<string, string> indexBlobs;

    private GitCheckout(string root, string? headCommit, IReadOnlyDictionary<string, string> indexBlobs)
    {
        Root = root;
        HeadCommit = headCommit;
        this.indexBlobs = indexBlobs;
    }

    public string Root
    {
        get;
    }

    /// <summary>The commit HEAD resolves to, or null when it cannot be resolved.</summary>
    public string? HeadCommit
    {
        get;
    }

    public static GitCheckout? TryOpen(string root)
    {
        var gitDirectory = Path.Combine(root, ".git");
        if (!Directory.Exists(gitDirectory))
        {
            return null;
        }

        return new GitCheckout(root, ResolveHead(gitDirectory), ReadIndex(Path.Combine(gitDirectory, "index")));
    }

    /// <summary>The index blob id for a path relative to the checkout root, or null when unknown.</summary>
    public string? IndexBlob(string relativePath) =>
        indexBlobs.TryGetValue(relativePath.Replace('\\', '/'), out var sha) ? sha : null;

    private static string? ResolveHead(string gitDirectory)
    {
        var headPath = Path.Combine(gitDirectory, "HEAD");
        if (!File.Exists(headPath))
        {
            return null;
        }

        var head = File.ReadAllText(headPath).Trim();
        if (!head.StartsWith("ref: ", StringComparison.Ordinal))
        {
            return IsSha(head) ? head : null;
        }

        var reference = head[5..];
        var looseRef = Path.Combine(gitDirectory, reference.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(looseRef))
        {
            var sha = File.ReadAllText(looseRef).Trim();
            return IsSha(sha) ? sha : null;
        }

        var packedRefs = Path.Combine(gitDirectory, "packed-refs");
        if (!File.Exists(packedRefs))
        {
            return null;
        }

        foreach (var line in File.ReadLines(packedRefs))
        {
            var space = line.IndexOf(' ', StringComparison.Ordinal);
            if (space == 40 && line[(space + 1)..] == reference && IsSha(line[..space]))
            {
                return line[..space];
            }
        }

        return null;
    }

    // Git index format versions 2 and 3; version 4 prefix-compresses paths and is not read here.
    private static Dictionary<string, string> ReadIndex(string indexPath)
    {
        var blobs = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(indexPath))
        {
            return blobs;
        }

        var data = File.ReadAllBytes(indexPath);
        if (data.Length < 12 || Encoding.ASCII.GetString(data, 0, 4) != "DIRC")
        {
            return blobs;
        }

        var version = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(4, 4));
        if (version is not (2 or 3))
        {
            return blobs;
        }

        var count = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(8, 4));
        var position = 12;
        for (var entry = 0; entry < count && position + 62 <= data.Length; entry++)
        {
            var start = position;
            var sha = Convert.ToHexStringLower(data.AsSpan(position + 40, 20));
            var flags = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position + 60, 2));
            position += 62;
            if (version == 3 && (flags & 0x4000) != 0)
            {
                position += 2;
            }

            var end = Array.IndexOf(data, (byte)0, position);
            if (end < 0)
            {
                break;
            }

            var path = Encoding.UTF8.GetString(data, position, end - position);
            var stage = (flags >> 12) & 0x3;
            if (stage == 0)
            {
                blobs[path] = sha;
            }

            // Entries are padded with 1 to 8 NUL bytes to a multiple of 8, measured from the entry start.
            var entryLength = end - start + 1;
            position = start + ((entryLength + 7) / 8 * 8);
        }

        return blobs;
    }

    private static bool IsSha(string value) => value.Length == 40 && value.All(char.IsAsciiHexDigitLower);
}
