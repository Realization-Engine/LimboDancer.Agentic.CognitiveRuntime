namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// The synthetic game records in <c>src/ASL/units/games</c>, by name. They are fixtures, never game state: no live game
/// source has been chosen (ASL-UNIT-050, D2).
/// </summary>
public static class UnitGames
{
    private const string Prefix = "Games.";
    private const string Suffix = ".game.json";

    public static IReadOnlyList<string> Names => [.. typeof(UnitGames).Assembly.GetManifestResourceNames()
        .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal) && name.EndsWith(Suffix, StringComparison.Ordinal))
        .Select(name => name[Prefix.Length..^Suffix.Length])
        .Order(StringComparer.Ordinal)];

    /// <summary>Reads an embedded game record; null when none of that name is embedded.</summary>
    public static GameRecordResult? Read(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        using var stream = typeof(UnitGames).Assembly.GetManifestResourceStream(Prefix + name + Suffix);
        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return GameEventReader.Read(buffer.ToArray());
    }
}
