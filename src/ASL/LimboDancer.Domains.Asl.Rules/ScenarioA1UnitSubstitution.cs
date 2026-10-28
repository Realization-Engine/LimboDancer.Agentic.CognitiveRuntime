using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Unit Substitution by SSR (A19.131, A19.132, p. 86; pass 72, rulings R72.10, R72.13): the two card tokens, <c>ammo-shortage:&lt;side&gt;</c> and
/// <c>underscored-elr:&lt;side&gt;[:&lt;group&gt;]:&lt;n&gt;</c>, and what they give a side or a unit. Play reads the state's special rules and the unit, hands the
/// facts over, and records what Rules answers.
/// </summary>
public static class ScenarioA1UnitSubstitution
{
    /// <summary>The prefix of the SSR that afflicts a side's on-board OB with Ammunition Shortages (A19.131; ruling R72.10).</summary>
    public const string AmmunitionShortageRule = "ammo-shortage:";

    /// <summary>The prefix of the SSR that assigns an ELR of 4 or less to a side's, or an OB group's, units with an underscored Morale Factor (A19.132; ruling R72.10).</summary>
    public const string UnderscoredElrRule = "underscored-elr:";

    /// <summary>The side an SSR <c>ammo-shortage:&lt;side&gt;</c> names (A19.131); null when the token is not a well-formed one.</summary>
    public static string? AmmunitionShortageToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        // The reviews R4.8: the prefix is tested before the split, since every SSR of the game passes here.
        return token.StartsWith(AmmunitionShortageRule, StringComparison.Ordinal) && token.Split(':') is ["ammo-shortage", { Length: > 0 } side] ? side : null;
    }

    /// <summary>
    /// The side, the OB group (null for the whole side), and the ELR an SSR <c>underscored-elr:&lt;side&gt;:&lt;n&gt;</c> or
    /// <c>underscored-elr:&lt;side&gt;:&lt;group&gt;:&lt;n&gt;</c> names (A19.132, p. 86: "Whenever a SSR ... specifically assigns an ELR ≤ 4 to units with an
    /// underscored Morale Factor"), n from 0 to 4; null when the token is not a well-formed one.
    /// </summary>
    public static (string Side, string? Group, int Elr)? UnderscoredElrToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (!token.StartsWith(UnderscoredElrRule, StringComparison.Ordinal))
        {
            return null;
        }

        return token.Split(':') switch
        {
            ["underscored-elr", { Length: > 0 } side, var n] when Elr(n) is { } elr => (side, null, elr),
            ["underscored-elr", { Length: > 0 } side, { Length: > 0 } group, var n] when Elr(n) is { } elr => (side, group, elr),
            _ => null,
        };

        static int? Elr(string text) =>
            text.Length == 1 && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value is >= 0 and <= 4 ? value : null;
    }

    /// <summary>Whether a token is one of the two, well-formed or not (ruling R72.10).</summary>
    public static bool IsSubstitutionToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return token.StartsWith(AmmunitionShortageRule, StringComparison.Ordinal) || token.StartsWith(UnderscoredElrRule, StringComparison.Ordinal);
    }

    /// <summary>
    /// The side and the OB group (null for the whole side) a well-formed token of the two names; null for a malformed one. A card checks that the side is
    /// one of its own and the group one of that side's.
    /// </summary>
    public static (string Side, string? Group)? Named(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return AmmunitionShortageToken(token) is { } side ? (side, null) : UnderscoredElrToken(token) is { } elr ? (elr.Side, elr.Group) : null;
    }

    /// <summary>Whether an SSR cites a side's on-board OB as afflicted by Ammunition Shortages (A19.131, p. 86; ruling R72.10). Pass 72's 5d builds what it does.</summary>
    public static bool AmmunitionShortage(IReadOnlyList<string> rules, string? side)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return side is not null && rules.Any(rule => AmmunitionShortageToken(rule) == side);
    }

    /// <summary>
    /// The SSR ELR of a unit with an underscored Morale Factor (A19.132, p. 86; ruling R72.13): for a MMC whose Morale Factor is underscored, the ELR a
    /// <c>underscored-elr:</c> token gives its OB group, else its side; null for any other unit, and when no token names its side, so R15.9's ELR of 5 holds.
    /// "With a mixed OB of MMC both with and without an underscored Morale Factor, an ELR ≤ 4 would apply to those MMC with an underscored Morale Factor only
    /// via SSR."
    /// </summary>
    public static int? UnderscoredElr(IReadOnlyList<string> rules, string? side, string? group, FireDefinition? definition)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.Count == 0 || side is null || definition is not { IsMmc: true, UnderscoredMorale: true })
        {
            return null;
        }

        // A token for the unit's own OB group before one for its whole side.
        int? sideWide = null;
        foreach (var rule in rules)
        {
            if (UnderscoredElrToken(rule) is not { } named || named.Side != side)
            {
                continue;
            }

            if (named.Group is null)
            {
                sideWide ??= named.Elr;
            }
            else if (named.Group == group)
            {
                return named.Elr;
            }
        }

        return sideWide;
    }

    /// <summary>Why the SSRs of a new game are refused for a malformed token of the two (ruling R72.10), or null.</summary>
    public static string? RulesBar(IReadOnlyList<string> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return rules.FirstOrDefault(rule => IsSubstitutionToken(rule) && Named(rule) is null) is { } malformed
            ? $"play.unit-substitution-rule: '{malformed}' is not ammo-shortage:<side>, or underscored-elr:<side>:<n> or underscored-elr:<side>:<group>:<n> with n "
                + "from 0 to 4 (A19.131, A19.132; ruling R72.10)"
            : null;
    }
}
