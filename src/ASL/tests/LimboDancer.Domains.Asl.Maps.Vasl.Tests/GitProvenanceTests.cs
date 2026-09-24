using System.Buffers.Binary;
using System.Text;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

public sealed class GitProvenanceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-git-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void BlobShaMatchesGit()
    {
        // `printf 'hello\n' | git hash-object --stdin` and the empty blob.
        Assert.Equal("ce013625030ba8dba906f756967f9e9ca394464a", GitBlob.Sha("hello\n"u8));
        Assert.Equal("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", GitBlob.Sha([]));
    }

    [Fact]
    public void ReadsHeadThroughPackedRefsAndBlobsFromAVersion2Index()
    {
        const string commit = "33324f9adb3b9b97dd93700685103b6940dc9c3c";
        var gitDirectory = Path.Combine(root, ".git");
        Directory.CreateDirectory(Path.Combine(gitDirectory, "refs", "heads"));
        File.WriteAllText(Path.Combine(gitDirectory, "HEAD"), "ref: refs/heads/production\n");
        File.WriteAllText(Path.Combine(gitDirectory, "packed-refs"),
            "# pack-refs with: peeled fully-peeled sorted\n" + commit + " refs/heads/production\n");
        File.WriteAllBytes(Path.Combine(gitDirectory, "index"), Index(
            ("boards/src/bd01/BoardMetadata.xml", "e91b0d99a7a788812444753cae245841875a8de6"),
            ("boards/src/bd01/LOSData", "8d77d26222b7bb21d8c1fdda6ba05b447f63c317")));

        var git = Assert.IsType<GitCheckout>(GitCheckout.TryOpen(root));
        Assert.Equal(commit, git.HeadCommit);
        Assert.Equal("e91b0d99a7a788812444753cae245841875a8de6", git.IndexBlob("boards/src/bd01/BoardMetadata.xml"));
        Assert.Equal("8d77d26222b7bb21d8c1fdda6ba05b447f63c317", git.IndexBlob(@"boards\src\bd01\LOSData"));
        Assert.Null(git.IndexBlob("boards/src/bd02/LOSData"));
    }

    [Fact]
    public void LooseRefsAndDetachedHeadsResolve()
    {
        var gitDirectory = Path.Combine(root, ".git");
        Directory.CreateDirectory(Path.Combine(gitDirectory, "refs", "heads"));
        File.WriteAllText(Path.Combine(gitDirectory, "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(gitDirectory, "refs", "heads", "main"), new string('a', 40) + "\n");
        Assert.Equal(new string('a', 40), GitCheckout.TryOpen(root)?.HeadCommit);

        File.WriteAllText(Path.Combine(gitDirectory, "HEAD"), new string('b', 40) + "\n");
        Assert.Equal(new string('b', 40), GitCheckout.TryOpen(root)?.HeadCommit);
        Assert.Null(GitCheckout.TryOpen(Path.Combine(root, "missing")));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // A minimal version 2 index: header, then 62-byte entry headers, NUL-terminated paths, and 8-byte padding.
    private static byte[] Index(params (string Path, string Sha)[] entries)
    {
        using var stream = new MemoryStream();
        stream.Write("DIRC"u8);
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, 2);
        stream.Write(number);
        BinaryPrimitives.WriteInt32BigEndian(number, entries.Length);
        stream.Write(number);
        Span<byte> flags = stackalloc byte[2];
        foreach (var (path, sha) in entries)
        {
            var start = stream.Position;
            stream.Write(new byte[40]);
            stream.Write(Convert.FromHexString(sha));
            var pathBytes = Encoding.UTF8.GetBytes(path);
            BinaryPrimitives.WriteUInt16BigEndian(flags, (ushort)Math.Min(pathBytes.Length, 0xFFF));
            stream.Write(flags);
            stream.Write(pathBytes);
            var length = stream.Position - start + 1;
            stream.Write(new byte[((length + 7) / 8 * 8) - length + 1]);
        }

        return stream.ToArray();
    }
}
