using System.Collections.Concurrent;
using System.Security.Cryptography;
using LimboDancer.Dice;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// Dice for UI test runs only: the roller takes each die from a queue the Play page fills, and draws a random die when the
/// queue is empty. It exists only in the Development environment with <c>Play:ScriptedDice</c> set to true; the Play page
/// says so while it is on. A roll it gives is still recorded as a system roll, so games played with it are test data.
/// </summary>
public sealed class ScriptedDice
{
    private readonly ConcurrentQueue<int> queue = new();

    public ScriptedDice()
    {
        Roller = new DiceRoller(Next);
    }

    public DiceRoller Roller
    {
        get;
    }

    /// <summary>The dice still queued, in the order they will be drawn.</summary>
    public IReadOnlyList<int> Queued => [.. queue];

    /// <summary>Queues die values, each 1 to 6; returns false, queuing nothing, if any is outside that range.</summary>
    public bool Enqueue(IEnumerable<int> values)
    {
        var list = values.ToList();
        if (list.Any(value => value is < 1 or > 6))
        {
            return false;
        }

        foreach (var value in list)
        {
            queue.Enqueue(value);
        }

        return true;
    }

    public void Clear() => queue.Clear();

    // The roller asks for a zero-based draw below the number of sides.
    private int Next(int sides) => queue.TryDequeue(out var value) && value <= sides ? value - 1 : RandomNumberGenerator.GetInt32(sides);

    /// <summary>Whether this run of the Studio uses scripted dice.</summary>
    public static bool Enabled(IHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() && configuration.GetValue<bool>("Play:ScriptedDice");
}
