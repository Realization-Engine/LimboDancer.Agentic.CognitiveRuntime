using System.Buffers.Binary;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>
/// Java object-stream framing for streams written only with <c>writeInt</c> and <c>writeByte</c>
/// (VASL Board Ingestion Design, section 4.1): the <c>AC ED 00 05</c> header followed by block-data records.
/// </summary>
public static class JavaBlockData
{
    /// <summary>Java's <c>ObjectOutputStream</c> block buffer size; full blocks are flushed at this length.</summary>
    public const int MaxBlockSize = 1024;

    private const byte BlockData = 0x77;
    private const byte BlockDataLong = 0x7A;
    private static readonly byte[] StreamHeader = [0xAC, 0xED, 0x00, 0x05];

    /// <summary>Extracts the concatenated record payload, or returns a diagnostic describing the framing error.</summary>
    public static byte[]? ReadPayload(ReadOnlySpan<byte> stream, out MapDiagnostic? diagnostic)
    {
        diagnostic = null;
        if (stream.Length < StreamHeader.Length || !stream[..StreamHeader.Length].SequenceEqual(StreamHeader))
        {
            diagnostic = Error("VASL-LOS-001", "LOSData is not a Java object stream (missing AC ED 00 05 header).");
            return null;
        }

        using var payload = new MemoryStream(stream.Length);
        var position = StreamHeader.Length;
        while (position < stream.Length)
        {
            var tag = stream[position];
            int length;
            int headerLength;
            if (tag == BlockData && position + 2 <= stream.Length)
            {
                length = stream[position + 1];
                headerLength = 2;
            }
            else if (tag == BlockDataLong && position + 5 <= stream.Length)
            {
                length = BinaryPrimitives.ReadInt32BigEndian(stream.Slice(position + 1, 4));
                headerLength = 5;
            }
            else
            {
                diagnostic = Error("VASL-LOS-002", $"Unexpected or truncated record tag 0x{tag:X2} at stream offset {position}.");
                return null;
            }

            if (length < 0 || position + headerLength + length > stream.Length)
            {
                diagnostic = Error("VASL-LOS-002", $"Record at stream offset {position} declares {length} bytes, past the end of the stream.");
                return null;
            }

            payload.Write(stream.Slice(position + headerLength, length));
            position += headerLength + length;
        }

        return payload.ToArray();
    }

    /// <summary>
    /// Frames a payload exactly as Java's <c>ObjectOutputStream</c> does for <c>writeInt</c>/<c>writeByte</c>:
    /// 1024-byte blocks, with a 1-byte length header only for a final block shorter than 256 bytes.
    /// </summary>
    public static byte[] WriteStream(ReadOnlySpan<byte> payload)
    {
        var blockCount = (payload.Length + MaxBlockSize - 1) / MaxBlockSize;
        using var stream = new MemoryStream(StreamHeader.Length + payload.Length + (blockCount * 5));
        stream.Write(StreamHeader);
        Span<byte> lengthBytes = stackalloc byte[4];
        for (var offset = 0; offset < payload.Length; offset += MaxBlockSize)
        {
            var block = payload.Slice(offset, Math.Min(MaxBlockSize, payload.Length - offset));
            if (block.Length <= 0xFF)
            {
                stream.WriteByte(BlockData);
                stream.WriteByte((byte)block.Length);
            }
            else
            {
                stream.WriteByte(BlockDataLong);
                BinaryPrimitives.WriteInt32BigEndian(lengthBytes, block.Length);
                stream.Write(lengthBytes);
            }

            stream.Write(block);
        }

        return stream.ToArray();
    }

    private static MapDiagnostic Error(string code, string message) => new(code, MapDiagnosticSeverity.Error, message);
}
