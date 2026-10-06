namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Inexperienced Personnel and the MF allowance it sets (A19.2, A19.3, A4.11, A19.31; pass 32.a, slice S11, from Units' <c>Experience</c>). Green and
/// Conscript MMC are Inexperienced (A19.2, p. 86); a Green MMC stacked with an unbroken leader is exempt, a Conscript never is (A19.3, p. 86). The
/// caller reads the unit's kind, its current definition's class, and the leaders in its Location, and hands them over.
/// </summary>
public static class ScenarioA1Experience
{
    /// <summary>
    /// Whether an MMC is Inexperienced: unknown when its definition, its class, or a stacked leader's condition cannot establish it; inapplicable to
    /// units that are not MMC. <paramref name="classKnown"/> says the class is a member of the vocabulary; <paramref name="onMap"/> that the unit has a
    /// Location; <paramref name="leadersBroken"/> gives the Broken condition of each same-side leader in that Location.
    /// </summary>
    public static RuleState Inexperienced(bool mmc, string? @class, bool classKnown, bool onMap, IReadOnlyList<RuleState> leadersBroken)
    {
        ArgumentNullException.ThrowIfNull(leadersBroken);
        if (!mmc)
        {
            return RuleState.Inapplicable;
        }

        if (@class is null || !classKnown)
        {
            return RuleState.Unknown;
        }

        if (!ScenarioA1Definitions.InexperiencedClasses.Contains(@class))
        {
            return RuleState.False;
        }

        if (@class == "conscript")
        {
            return RuleState.True;
        }

        // A19.3: a Green MMC stacked with an unbroken leader is exempt.
        if (!onMap)
        {
            return RuleState.True;
        }

        return leadersBroken.Contains(RuleState.False) ? RuleState.False
            : leadersBroken.Contains(RuleState.Unknown) ? RuleState.Unknown
            : RuleState.True;
    }

    /// <summary>The MF allotment of a Good Order MMC moving on its own: four, three if Inexperienced (A4.11, p. 48; A19.31, p. 86); null when unknown or not an MMC.</summary>
    public static int? MfAllowance(RuleState inexperienced) => inexperienced switch
    {
        RuleState.True => 3,
        RuleState.False => 4,
        _ => null,
    };

    /// <summary>
    /// The MF allotment of a Good Order unit moving hex by hex: a MMC's as <paramref name="mmcAllowance"/> gives, a SMC's six, three if wounded
    /// (A4.11, p. 48; A17.2, p. 85), and a berserk unit's eight, a wounded one still three (A15.431, p. 84; A17.2).
    /// </summary>
    public static int? MoveAllowance(bool berserk, bool wounded, bool smc, Func<int?> mmcAllowance)
    {
        ArgumentNullException.ThrowIfNull(mmcAllowance);
        if (berserk)
        {
            return wounded ? 3 : 8;
        }

        return !smc ? mmcAllowance() : wounded ? 3 : 6;
    }
}
