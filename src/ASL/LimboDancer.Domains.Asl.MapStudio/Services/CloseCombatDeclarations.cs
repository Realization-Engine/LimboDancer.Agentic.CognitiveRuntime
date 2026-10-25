using System.Collections.Concurrent;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The first side's CC declarations of a simultaneous round, kept until the other side has declared its own and the round is resolved (A11.11; the
/// user, 2026-10-22). At the table the ATTACKER declares its attacks openly, then the DEFENDER his, and then every attack is rolled together; a side's
/// view declares only its own side's, so the first side's declarations wait here while the screen is handed over.
/// </summary>
/// <remarks>
/// The cache statement (the Memory and Performance Guidelines): owner, this store on <see cref="LivePlay"/>; lifetime, the process, never a game event;
/// bound, one declaration per game; key, the game id, with the Location and the revision it was made at; invalidation, a read with any other revision
/// or Location treats it as absent, and the next declaration replaces it; missing result, the first side declares again (a restarted Studio loses
/// it, and the game is as recorded); concurrency, a concurrent dictionary whose entries are immutable records.
/// </remarks>
public sealed class CloseCombatDeclarations
{
    private readonly ConcurrentDictionary<string, CloseCombatSideDeclaration> kept = new(StringComparer.Ordinal);

    /// <summary>The declaration kept for a game's Location at a revision; null when there is none, or it was made at another.</summary>
    public CloseCombatSideDeclaration? Of(string game, string location, long revision)
    {
        ArgumentNullException.ThrowIfNull(game);
        return kept.TryGetValue(game, out var declaration) && declaration.Location == location && declaration.Revision == revision ? declaration : null;
    }

    /// <summary>The declaration kept for a game at a revision, in whatever Location; null when there is none, or it was made at another revision.</summary>
    public CloseCombatSideDeclaration? Of(string game, long revision)
    {
        ArgumentNullException.ThrowIfNull(game);
        return kept.TryGetValue(game, out var declaration) && declaration.Revision == revision ? declaration : null;
    }

    /// <summary>Keeps a side's declarations, replacing any kept for the game.</summary>
    public void Declare(string game, CloseCombatSideDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(declaration);
        kept[game] = declaration;
    }

    /// <summary>Drops a game's declaration (the side withdraws it, or the round was resolved).</summary>
    public void Drop(string game)
    {
        ArgumentNullException.ThrowIfNull(game);
        kept.TryRemove(game, out _);
    }
}

/// <summary>
/// A side's CC declarations in a Location: its attacks, its SMC stacking (A11.14), its withdrawals (A11.2), its infiltrations (A11.22), and the
/// ATTACKER's Hand-to-Hand (J2.31), made at <see cref="Revision"/> in <see cref="Round"/>.
/// </summary>
public sealed record CloseCombatSideDeclaration(string Side, string Location, long Revision, string Round, IReadOnlyList<CloseCombatPanel.DeclaredAttack> Attacks,
    IReadOnlyDictionary<string, string> Stacking, IReadOnlyDictionary<string, string> Withdrawals, IReadOnlyDictionary<string, string> Infiltrations, bool HandToHand)
{
    /// <summary>The SW its units drop to withdraw or infiltrate (A11.21; pass 70, ruling R70.26); none by default.</summary>
    public IReadOnlyList<string> Drops { get; init; } = [];
}
