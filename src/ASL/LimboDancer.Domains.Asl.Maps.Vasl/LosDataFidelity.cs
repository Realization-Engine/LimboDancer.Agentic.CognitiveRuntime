using LimboDancer.Domains.Asl.Maps.Grid;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

public enum F1Status
{
    /// <summary>Re-encoding reproduces the decompressed LOSData stream byte for byte.</summary>
    Pass,

    /// <summary>The payloads are equal but the Java block framing differs.</summary>
    FramingOnly,

    /// <summary>The payloads differ.</summary>
    Fail,
}

public sealed record F1Result(F1Status Status, string Detail)
{
    public bool Passed => Status == F1Status.Pass;
}

/// <summary>
/// The F1 grid-fidelity check (ASL-MAP-040, VASL Board Ingestion Design section 9.1): encode the decoded grid
/// and compare with the decompressed source stream. Gzip container bytes are never compared.
/// </summary>
public static class LosDataFidelity
{
    public static F1Result CheckF1(ReadOnlySpan<byte> sourceStream, TerrainGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var encodedStream = LosDataCodec.EncodeStream(grid);
        if (sourceStream.SequenceEqual(encodedStream))
        {
            return new F1Result(F1Status.Pass, $"Decompressed stream reproduced exactly ({encodedStream.Length} bytes).");
        }

        var sourcePayload = JavaBlockData.ReadPayload(sourceStream, out var framingError);
        if (sourcePayload is null)
        {
            return new F1Result(F1Status.Fail, "Source stream framing is invalid: " + framingError?.Message);
        }

        var encodedPayload = LosDataCodec.EncodePayload(grid);
        var offset = sourcePayload.AsSpan().CommonPrefixLength(encodedPayload);
        if (offset == sourcePayload.Length && offset == encodedPayload.Length)
        {
            return new F1Result(F1Status.FramingOnly, "Payloads are equal; only the Java block framing differs.");
        }

        return new F1Result(F1Status.Fail, "Payloads differ at " + Describe(offset, grid) + ".");
    }

    private static string Describe(int payloadOffset, TerrainGrid grid)
    {
        const int headerLength = 16;
        if (payloadOffset < headerLength)
        {
            return $"header byte {payloadOffset}";
        }

        var cellBytes = 2 * grid.CellCount;
        if (payloadOffset < headerLength + cellBytes)
        {
            var cell = (payloadOffset - headerLength) / 2;
            var field = (payloadOffset - headerLength) % 2 == 0 ? "elevation" : "terrain code";
            return $"cell ({cell / grid.Geometry.GridHeight}, {cell % grid.Geometry.GridHeight}) {field}";
        }

        var ordinal = payloadOffset - headerLength - cellBytes;
        var hex = grid.Geometry.Hexes().ElementAtOrDefault(ordinal);
        return ordinal < grid.Geometry.HexCount
            ? $"stairway flag of hex {grid.Geometry.NameOf(hex)}"
            : $"payload byte {payloadOffset}, past the expected length";
    }
}
