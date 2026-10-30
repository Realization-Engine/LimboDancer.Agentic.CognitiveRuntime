namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Who fires a crew-served ordnance weapon (A21.13, p. 88; the A21.13 claim's design, ruling RA21.2): a crew counter (an infantry crew, or a vehicle
/// crew, D5.1), a MMC that is not a crew (a squad or HS), two or more SMC firing it together, or one SMC alone.
/// </summary>
public enum CrewServedUser
{
    /// <summary>A crew counter, <c>asl:crew</c> or <c>asl:vehicle-crew</c> (C2.1, D5.1): qualified.</summary>
    Crew,

    /// <summary>A squad or HS: a non-qualified MMC.</summary>
    Mmc,

    /// <summary>Two or more SMC firing it together, which fire it as a non-qualified MMC.</summary>
    SmcGroup,

    /// <summary>One SMC alone, who fires it only if heroic (A15.23), or a mortar of 82mm or less as a light mortar (C9.2).</summary>
    LoneSmc,
}

/// <summary>
/// The facts of one use of a crew-served ordnance weapon (ruling RA21.2). <see cref="Captured"/> is the weapon possessed from an enemy side (A21.1,
/// ruling RA21.4), not of another nationality. <see cref="Hero"/>, <see cref="Mortar"/>, and <see cref="Caliber"/> are read for a lone SMC and a light
/// mortar; <see cref="Phase"/> and <see cref="FiredThisGameTurn"/> only for a lone hero with a Gun over 82mm (A15.23, ruling RA21.9). An unknown fact the
/// verdict needs refuses the use.
/// </summary>
public sealed record NonQualifiedUseFacts(CrewServedUser? User, bool? Captured, bool? Hero, bool? Mortar, int? Caliber)
{
    /// <summary>The phase of the shot ("MPh", "DFPh", ...), for A15.23's limit on a Gun over 82mm.</summary>
    public string? Phase
    {
        get; init;
    }

    /// <summary>Whether the Gun has already been fired this Game Turn, for A15.23's limit on a Gun over 82mm.</summary>
    public bool? FiredThisGameTurn
    {
        get; init;
    }
}

/// <summary>
/// The verdict of one use (rulings RA21.2, RA21.3, RA21.9): the refusal (null when allowed) and its rule; the penalty multiple m of A21.11 and A21.12
/// (0 none, 1 the penalties, 2 doubled); whether it fires as a light mortar (C9.2, no A21.13 penalty); whether the hero's own DRM applies to the shot
/// (A15.23: not on a crew-served Gun); and whether A15.23's once-per-Game-Turn, MPh-or-DFPh limit binds it.
/// </summary>
public sealed record NonQualifiedUseVerdict(string? Refusal, string? RefusalRule, int Multiple, bool FiresAsLight, bool HeroDrmApplies, bool HeroGunLimit);
