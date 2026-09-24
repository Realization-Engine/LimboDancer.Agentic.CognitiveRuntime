using System.Buffers.Binary;
using System.IO.Compression;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

public sealed class LosDataCodecTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    [Fact]
    public void FramingSplitsIntoJavaSizedBlocks()
    {
        var payload = Enumerable.Range(0, 2100).Select(value => (byte)(value % 256)).ToArray();
        var stream = JavaBlockData.WriteStream(payload);

        // Header, two full 1024-byte TC_BLOCKDATALONG blocks, then a 52-byte TC_BLOCKDATA block.
        Assert.Equal(new byte[] { 0xAC, 0xED, 0x00, 0x05, 0x7A, 0x00, 0x00, 0x04, 0x00 }, stream[..9]);
        Assert.Equal(4 + 5 + 1024 + 5 + 1024 + 2 + 52, stream.Length);
        Assert.Equal(0x77, stream[4 + 5 + 1024 + 5 + 1024]);
        Assert.Equal(payload, JavaBlockData.ReadPayload(stream, out var diagnostic));
        Assert.Null(diagnostic);
    }

    [Fact]
    public void AFinalBlockOf256BytesOrMoreUsesTheLongTag()
    {
        var stream = JavaBlockData.WriteStream(new byte[1024 + 300]);
        Assert.Equal(0x7A, stream[4 + 5 + 1024]);
    }

    [Theory]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00 }, "VASL-LOS-001")]
    [InlineData(new byte[] { 0xAC, 0xED, 0x00, 0x05, 0x73 }, "VASL-LOS-002")]
    [InlineData(new byte[] { 0xAC, 0xED, 0x00, 0x05, 0x77, 0x05, 0x01 }, "VASL-LOS-002")]
    [InlineData(new byte[] { 0xAC, 0xED, 0x00, 0x05, 0x7A, 0xFF, 0xFF, 0xFF, 0xFF }, "VASL-LOS-002")]
    public void MalformedFramingIsReported(byte[] stream, string code)
    {
        Assert.Null(JavaBlockData.ReadPayload(stream, out var diagnostic));
        Assert.Equal(code, diagnostic?.Code);
    }

    [Fact]
    public void DecodeReadsWhatEncodeWrites()
    {
        var grid = SyntheticGrid();
        using var file = new MemoryStream(LosDataCodec.Encode(grid));
        var decoded = LosDataCodec.Decode(file, Geometry);

        Assert.True(decoded.Succeeded);
        Assert.Equal(new LosDataHeader(33, 10, 1800, 645), decoded.Header);
        var result = decoded.Grid!;
        Assert.True(result.Codes.SequenceEqual(grid.Codes));
        Assert.True(result.Elevations.SequenceEqual(grid.Elevations));
        Assert.True(result.Stairways.SequenceEqual(grid.Stairways));
        Assert.Equal((sbyte)-1, result.ElevationAt(0, 1));
        Assert.Equal((sbyte)-2, result.ElevationAt(3, 0));
        Assert.Equal(LosDataCodec.EncodeStream(grid), decoded.DecompressedStream);
    }

    [Fact]
    public void PayloadLengthMatchesTheBoard01Layout()
    {
        // 16 header bytes, 2 bytes for each of 1,161,000 cells, and 346 stairway bytes.
        Assert.Equal(2_322_362, LosDataCodec.EncodePayload(SyntheticGrid()).Length);
        Assert.Equal(2_333_706, LosDataCodec.EncodeStream(SyntheticGrid()).Length);
    }

    [Fact]
    public void HeaderAndLengthMismatchesAreReported()
    {
        var payload = LosDataCodec.EncodePayload(SyntheticGrid());
        Assert.Equal("VASL-LOS-004", DecodeCode(WithHeaderInt(payload, 0, 32)));
        Assert.Equal("VASL-LOS-005", DecodeCode(WithHeaderInt(payload, 8, 1799)));
        Assert.Equal("VASL-LOS-003", DecodeCode(payload[..^1]));
        Assert.Equal("VASL-LOS-003", DecodeCode([.. payload, 0]));
        Assert.Equal("VASL-LOS-003", DecodeCode(payload[..10]));

        using var notGzip = new MemoryStream([1, 2, 3, 4]);
        Assert.Equal("VASL-LOS-001", LosDataCodec.Decode(notGzip, Geometry).Diagnostics.Single().Code);
    }

    [Fact]
    public void F1PassesForAnExactStreamAndLocatesDifferences()
    {
        var grid = SyntheticGrid();
        var stream = LosDataCodec.EncodeStream(grid);
        Assert.Equal(F1Status.Pass, LosDataFidelity.CheckF1(stream, grid).Status);

        // Same payload, framed as one long block: the data is equal but the framing is not Java's.
        var payload = LosDataCodec.EncodePayload(grid);
        var oneBlock = new byte[4 + 5 + payload.Length];
        stream.AsSpan(0, 4).CopyTo(oneBlock);
        oneBlock[4] = 0x7A;
        BinaryPrimitives.WriteInt32BigEndian(oneBlock.AsSpan(5, 4), payload.Length);
        payload.CopyTo(oneBlock, 9);
        Assert.Equal(F1Status.FramingOnly, LosDataFidelity.CheckF1(oneBlock, grid).Status);

        var changedCell = (byte[])payload.Clone();
        changedCell[16 + (2 * ((5 * 645) + 7)) + 1] ^= 0xFF;
        var failed = LosDataFidelity.CheckF1(JavaBlockData.WriteStream(changedCell), grid);
        Assert.Equal(F1Status.Fail, failed.Status);
        Assert.Contains("cell (5, 7) terrain code", failed.Detail, StringComparison.Ordinal);

        var changedStairway = (byte[])payload.Clone();
        changedStairway[^1] ^= 1;
        Assert.Contains("stairway flag of hex GG10", LosDataFidelity.CheckF1(JavaBlockData.WriteStream(changedStairway), grid).Detail, StringComparison.Ordinal);
    }

    private static string? DecodeCode(byte[] payload)
    {
        using var file = new MemoryStream(Gzip(JavaBlockData.WriteStream(payload)));
        return LosDataCodec.Decode(file, Geometry).Diagnostics.SingleOrDefault()?.Code;
    }

    private static byte[] WithHeaderInt(byte[] payload, int offset, int value)
    {
        var copy = (byte[])payload.Clone();
        BinaryPrimitives.WriteInt32BigEndian(copy.AsSpan(offset, 4), value);
        return copy;
    }

    private static byte[] Gzip(byte[] content)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest))
        {
            gzip.Write(content);
        }

        return output.ToArray();
    }

    private static TerrainGrid SyntheticGrid()
    {
        var codes = new byte[Geometry.GridWidth * Geometry.GridHeight];
        var elevations = new sbyte[codes.Length];
        for (var cell = 0; cell < codes.Length; cell++)
        {
            codes[cell] = (byte)(cell % 251);
            elevations[cell] = (sbyte)((cell % 5) - 2);
        }

        var stairways = Geometry.Hexes().Select(hex => (hex.Column + hex.Row) % 9 == 0).ToArray();
        return new TerrainGrid(Geometry, codes, elevations, stairways);
    }
}
