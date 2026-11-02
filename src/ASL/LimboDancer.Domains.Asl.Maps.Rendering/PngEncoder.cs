using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;

namespace LimboDancer.Domains.Asl.Maps.Rendering;

/// <summary>
/// A minimal PNG writer for raster tiles (Raster Tiles Plan, section 2.3): 8-bit RGB or RGBA, each row filtered by "Sub", compressed by
/// .NET's own zlib. It needs no image library, so nothing native has to load on the Linux CI runners.
/// </summary>
public static class PngEncoder
{
    private static readonly byte[] Signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0d, 0x0a, 0x1a, 0x0a];
    private static readonly uint[] CrcTable = BuildCrcTable();

    /// <summary>The PNG of a width by height image whose pixels are rows of 3 (RGB) or 4 (RGBA) bytes, top row first.</summary>
    public static byte[] Encode(ReadOnlySpan<byte> pixels, int width, int height, bool alpha)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        var channels = alpha ? 4 : 3;
        var stride = width * channels;
        if (pixels.Length < stride * height)
        {
            throw new ArgumentException($"A {width} by {height} image needs {stride * height} bytes.", nameof(pixels));
        }

        using var output = new MemoryStream();
        output.Write(Signature);
        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8;
        header[9] = (byte)(alpha ? 6 : 2);
        header[10] = 0;
        header[11] = 0;
        header[12] = 0;
        WriteChunk(output, "IHDR"u8, header);

        using var compressed = new MemoryStream();
        var row = ArrayPool<byte>.Shared.Rent(stride + 1);
        try
        {
            using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
            {
                for (var y = 0; y < height; y++)
                {
                    var source = pixels.Slice(y * stride, stride);
                    row[0] = 1;
                    for (var i = 0; i < stride; i++)
                    {
                        row[i + 1] = unchecked((byte)(source[i] - (i >= channels ? source[i - channels] : 0)));
                    }

                    zlib.Write(row, 0, stride + 1);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(row);
        }

        WriteChunk(output, "IDAT"u8, compressed.GetBuffer().AsSpan(0, (int)compressed.Length));
        WriteChunk(output, "IEND"u8, []);
        return output.ToArray();
    }

    private static void WriteChunk(Stream output, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        output.Write(number);
        output.Write(type);
        output.Write(data);
        var crc = Update(Update(0xffffffffu, type), data) ^ 0xffffffffu;
        BinaryPrimitives.WriteUInt32BigEndian(number, crc);
        output.Write(number);
    }

    private static uint Update(uint crc, ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            crc = CrcTable[(crc ^ value) & 0xff] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (var n = 0u; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xedb88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}
