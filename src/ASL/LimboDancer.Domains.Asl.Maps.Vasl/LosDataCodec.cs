using System.Buffers.Binary;
using System.IO.Compression;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>The four header integers at the start of the LOSData payload.</summary>
public readonly record struct LosDataHeader(int WidthInHexes, int HeightInHexes, int GridWidth, int GridHeight);

public sealed record LosDataDecodeResult(
    TerrainGrid? Grid,
    LosDataHeader? Header,
    byte[]? DecompressedStream,
    IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool Succeeded => Grid is not null;
}

/// <summary>
/// Decodes and encodes VASL <c>LOSData</c> (VASL Board Ingestion Design, section 4): gzip over a Java object
/// stream holding the header, an elevation and terrain-code byte per cell in column-major order, and a
/// stairway byte per hex. A malformed file never produces a partial grid (ASL-MAP-032).
/// </summary>
public static class LosDataCodec
{
    private const int HeaderLength = 16;

    /// <summary>Decodes LOSData for a board whose metadata declares the given geometry.</summary>
    public static LosDataDecodeResult Decode(Stream losData, BoardGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(losData);
        ArgumentNullException.ThrowIfNull(geometry);
        byte[] decompressed;
        try
        {
            using var gzip = new GZipStream(losData, CompressionMode.Decompress, leaveOpen: true);
            using var buffer = new MemoryStream();
            gzip.CopyTo(buffer);
            decompressed = buffer.ToArray();
        }
        catch (InvalidDataException exception)
        {
            return Failed(null, null, Error("VASL-LOS-001", $"LOSData is not valid gzip: {exception.Message}"));
        }

        var payload = JavaBlockData.ReadPayload(decompressed, out var framingError);
        if (payload is null)
        {
            return Failed(null, decompressed, framingError!);
        }

        if (payload.Length < HeaderLength)
        {
            return Failed(null, decompressed, Error("VASL-LOS-003", $"LOSData payload has {payload.Length} bytes, too short for its header."));
        }

        var header = new LosDataHeader(
            BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(0, 4)),
            BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(4, 4)),
            BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(8, 4)),
            BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(12, 4)));

        if (header.WidthInHexes != geometry.WidthInHexes || header.HeightInHexes != geometry.HeightInHexes)
        {
            return Failed(header, decompressed, Error("VASL-LOS-004",
                $"LOSData declares {header.WidthInHexes} by {header.HeightInHexes} hexes; the metadata declares {geometry.WidthInHexes} by {geometry.HeightInHexes}."));
        }

        if (header.GridWidth != geometry.GridWidth || header.GridHeight != geometry.GridHeight)
        {
            return Failed(header, decompressed, Error("VASL-LOS-005",
                $"LOSData declares a {header.GridWidth} by {header.GridHeight} grid; the geometry implies {geometry.GridWidth} by {geometry.GridHeight}."));
        }

        var cellCount = geometry.GridWidth * geometry.GridHeight;
        var expectedLength = HeaderLength + (2 * cellCount) + geometry.HexCount;
        if (payload.Length != expectedLength)
        {
            return Failed(header, decompressed, Error("VASL-LOS-003",
                $"LOSData payload has {payload.Length} bytes; this geometry requires exactly {expectedLength}."));
        }

        var codes = new byte[cellCount];
        var elevations = new sbyte[cellCount];
        for (var cell = 0; cell < cellCount; cell++)
        {
            elevations[cell] = unchecked((sbyte)payload[HeaderLength + (2 * cell)]);
            codes[cell] = payload[HeaderLength + (2 * cell) + 1];
        }

        var stairwayOffset = HeaderLength + (2 * cellCount);
        var stairways = new bool[geometry.HexCount];
        for (var hex = 0; hex < stairways.Length; hex++)
        {
            stairways[hex] = payload[stairwayOffset + hex] == 1;
        }

        return new LosDataDecodeResult(new TerrainGrid(geometry, codes, elevations, stairways), header, decompressed, []);
    }

    /// <summary>The payload VASL's <c>BoardArchive.writeLOSData</c> would write for this grid.</summary>
    public static byte[] EncodePayload(TerrainGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var geometry = grid.Geometry;
        var payload = new byte[HeaderLength + (2 * grid.CellCount) + geometry.HexCount];
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(0, 4), geometry.WidthInHexes);
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(4, 4), geometry.HeightInHexes);
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(8, 4), geometry.GridWidth);
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(12, 4), geometry.GridHeight);
        var codes = grid.Codes;
        var elevations = grid.Elevations;
        for (var cell = 0; cell < grid.CellCount; cell++)
        {
            payload[HeaderLength + (2 * cell)] = unchecked((byte)elevations[cell]);
            payload[HeaderLength + (2 * cell) + 1] = codes[cell];
        }

        var stairwayOffset = HeaderLength + (2 * grid.CellCount);
        var stairways = grid.Stairways;
        for (var hex = 0; hex < stairways.Length; hex++)
        {
            payload[stairwayOffset + hex] = stairways[hex] ? (byte)1 : (byte)0;
        }

        return payload;
    }

    /// <summary>The decompressed Java object stream: header plus Java-framed payload.</summary>
    public static byte[] EncodeStream(TerrainGrid grid) => JavaBlockData.WriteStream(EncodePayload(grid));

    /// <summary>A complete LOSData file. Gzip bytes depend on the compressor and are never compared.</summary>
    public static byte[] Encode(TerrainGrid grid)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(EncodeStream(grid));
        }

        return output.ToArray();
    }

    private static LosDataDecodeResult Failed(LosDataHeader? header, byte[]? decompressed, MapDiagnostic diagnostic) =>
        new(null, header, decompressed, [diagnostic]);

    private static MapDiagnostic Error(string code, string message) => new(code, MapDiagnosticSeverity.Error, message);
}
