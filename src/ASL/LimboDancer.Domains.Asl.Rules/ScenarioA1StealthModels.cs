namespace LimboDancer.Domains.Asl.Rules;

/// <summary>What a unit is for the four drs of A11.17 and A11.18 (p. 73): Stealthy (-1), Lax (+1), or neither. "Stealthy unless Lax": Lax wins.</summary>
public enum StealthStatus
{
    /// <summary>Neither Stealthy nor Lax: no drm.</summary>
    Normal,

    /// <summary>Stealthy (A11.17): -1.</summary>
    Stealthy,

    /// <summary>Lax (A11.18): +1.</summary>
    Lax,
}

/// <summary>The dr a status is asked for (A11.17, A11.18): it matters only for a berserk unit, Lax in CC alone (A15.432, p. 84).</summary>
public enum StealthPurpose
{
    /// <summary>The Ambush status dr (A11.4, p. 73), once per side.</summary>
    Ambush,

    /// <summary>The Concealment Growth dr (A12.122, p. 77), the unit's own.</summary>
    Concealment,

    /// <summary>The Searching dr (A12.152, p. 79), per searching unit.</summary>
    Search,

    /// <summary>The Search Casualties dr (A12.154, p. 79), per concealed defending unit.</summary>
    SearchCasualty,
}

/// <summary>
/// A force the rules name Stealthy by what it is, not by its nationality and class (A11.17; A25.44 ANZAC, A25.43 and H1.24 Gurkhas and Commandos,
/// A25.24 Partisans, G17.111 U.S.M.C. Raiders). No card or OB field gives it yet (ruling RA11S.4): the caller gives it, and None until one does.
/// </summary>
public enum StealthDesignation
{
    None,
    Anzac,
    Gurkha,
    Commando,
    Partisan,
    Raider,
}

/// <summary>Which reading an Ambush is resolved under (ruling RA11S.8): the one recorded games were made under, or this calculator's.</summary>
public enum StealthReading
{
    /// <summary>The reading before the A11.17 claim: only a Good Order hero or heroic leader is Stealthy; Lax is berserk or an Inexperienced MMC.</summary>
    HeroOnly,

    /// <summary><see cref="ScenarioA1StealthCalculator.Status"/> with each unit's nationality, class, designation, and the card's SSR.</summary>
    Calculator,
}

/// <summary>
/// The facts of one unit for A11.17 and A11.18, all plain: its catalog kind (<c>asl:squad</c>, <c>asl:half-squad</c>, <c>asl:crew</c> for an Infantry
/// crew, <c>asl:vehicle-crew</c>, <c>asl:leader</c>, <c>asl:hero</c>, anything else is not Personnel); whether it is heroic (A15.21); its nationality and
/// class as the catalog gives them (null when unknown; an SMC and a vehicle crew have no class); its designation; whether an SSR makes it Stealthy or Lax;
/// whether it is Inexperienced (A19.2, A19.3, Unarmed included; null when unknown); whether it is berserk; whether it is in Good Order (A.7); and whether it
/// is Lax for this Player Turn by a Banzai Charge or a Human Wave (G1.6, A25.23). A struct, so the forwards allocate nothing.
/// </summary>
public readonly record struct StealthFacts(string? Kind, bool Heroic, string? Nationality, string? Class, StealthDesignation Designation, bool SsrStealthy,
    bool SsrLax, bool? Inexperienced, bool Berserk, bool GoodOrder, bool BanzaiOrHumanWave = false);

/// <summary>
/// What an Ambush needs beyond its recorded <see cref="AmbushFacts"/> to be read under this calculator (ruling RA11S.8; <c>AmbushFacts</c> gains no
/// field, the Rules conventions' section 1, rule 6): the reading, each unit's designation by unit id, the sides an SSR makes Stealthy or Lax, and the
/// units Lax for this Player Turn by a Banzai Charge or a Human Wave. The wiring decides which reading a recorded Ambush is verified under.
/// </summary>
public sealed record StealthContext(StealthReading Reading)
{
    /// <summary>Each designated unit's designation, by unit id; a unit not named has none.</summary>
    public IReadOnlyDictionary<string, StealthDesignation>? Designations
    {
        get; init;
    }

    /// <summary>The sides whose troops an SSR makes Stealthy (A11.17).</summary>
    public IReadOnlyList<string>? SsrStealthySides
    {
        get; init;
    }

    /// <summary>The sides whose units an SSR makes Lax (A11.18).</summary>
    public IReadOnlyList<string>? SsrLaxSides
    {
        get; init;
    }

    /// <summary>The units Lax for the rest of this Player Turn by a Banzai Charge (G1.6) or a Human Wave (A25.23), by unit id.</summary>
    public IReadOnlyList<string>? LaxThisPlayerTurn
    {
        get; init;
    }
}
