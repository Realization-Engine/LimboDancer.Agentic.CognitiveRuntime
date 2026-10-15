using System.Buffers.Binary;
using System.Security.Cryptography;
using LimboDancer.Domains.Asl.Maps.Features;

namespace LimboDancer.Campaign.Refinement;

/// <summary>Independent SHA-256 counter stream. No runtime PRNG or traversal-order state.</summary>
public sealed class SeedRandom
{
    public const string Version = "sha256-counter-v1";
    private readonly byte[] key;
    private byte[] block = [];
    private int position;
    private ulong counter;
    public SeedRandom(params string[] fields) => key = SHA256.HashData(CanonicalJson.Serialize(fields));
    public static string NewSeed() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
    public static void ValidateSeed(string seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        if (seed.Length != 32 || seed.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new ArgumentException("Seed must contain exactly 32 lowercase hexadecimal characters.", nameof(seed));
    }
    public uint Next()
    {
        if (position == block.Length)
        {
            Span<byte> input = stackalloc byte[40]; key.CopyTo(input);
            BinaryPrimitives.WriteUInt64BigEndian(input[32..], counter);
            counter = checked(counter + 1);
            block = SHA256.HashData(input); position = 0;
        }
        var value = BinaryPrimitives.ReadUInt32BigEndian(block.AsSpan(position, 4)); position += 4;
        return value;
    }
    public uint Below(ulong bound)
    {
        const ulong range = 1UL << 32;
        if (bound is 0 or > range) throw new ArgumentOutOfRangeException(nameof(bound));
        var cutoff = range / bound * bound;
        uint value; do { value = Next(); } while (value >= cutoff);
        return (uint)(value % bound);
    }
}
