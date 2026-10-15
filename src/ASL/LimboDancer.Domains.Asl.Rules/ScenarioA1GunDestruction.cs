using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The Gun Destruction Table of C11.6 (p. 182; pass 60, rulings R60.2, R60.17, R60.18) and Random SW/Gun Destruction (A9.74, p. 65). The table is read
/// by the Final IFT result before any gunshield DRM (note 4). Ordnance, a Bomb, or OBA: a KIA or lower eliminates the Gun, a K malfunctions it (and
/// eliminates it if AP was fired, note 5), a Critical Hit eliminates it. A MG, IFE, Small Arms, a FT, a MOL, or an OVR: a KIA is Random SW/Gun
/// Destruction, a K and a Critical Hit are NA. A DC: a KIA eliminates the Gun, a K malfunctions it. A Gun in tow dies only with its vehicle (note 1).
/// </summary>
public static class ScenarioA1GunDestruction
{
    /// <summary>The ordnance, Bomb, and OBA column.</summary>
    public const string Ordnance = "ordnance";

    /// <summary>The MG, IFE, Small Arms, FT, MOL, and OVR column.</summary>
    public const string SmallArms = "small-arms";

    /// <summary>The DC column.</summary>
    public const string DemolitionCharge = "dc";

    public const string Destroyed = "destroyed";

    public const string Malfunctioned = "malfunctioned";

    /// <summary>The Small Arms column's KIA: Random SW/Gun Destruction (A9.74).</summary>
    public const string RandomDestruction = "random-destruction";

    /// <summary>Whether an IFT result is a KIA of any number.</summary>
    public static bool IsKia(string result) => Regex.IsMatch(result ?? string.Empty, "^([1-7])?KIA$");

    /// <summary>Whether an IFT result is a K/#.</summary>
    public static bool IsK(string result) => Regex.IsMatch(result ?? string.Empty, "^K/([1-4])$");

    /// <summary>
    /// The Gun's fate on the table's <paramref name="column"/> (C11.4, C11.6): <see cref="Destroyed"/>, <see cref="Malfunctioned"/>,
    /// <see cref="RandomDestruction"/>, or null for none. <paramref name="armorPiercing"/> is AP, APCR, APDS, or an ATR (C11.52, note 5).
    /// </summary>
    public static string? Fate(string column, string result, bool criticalHit, bool armorPiercing = false) => column switch
    {
        Ordnance => criticalHit || IsKia(result) ? Destroyed : IsK(result) ? armorPiercing ? Destroyed : Malfunctioned : null,
        DemolitionCharge => IsKia(result) ? Destroyed : IsK(result) ? Malfunctioned : null,
        _ => IsKia(result) ? RandomDestruction : null,
    };

    /// <summary>
    /// A9.74 (p. 65): the dr of a SW or Gun that a unit eliminated by a Final KIA possessed, read as an IFT DR on the attack's column with no DRM: a KIA
    /// eliminates it; a K malfunctions it, or eliminates it if it was already malfunctioned or has an X#; null for none. A dm SW counts as a SW (A9.8).
    /// </summary>
    public static string? Random(string result, bool malfunctioned, bool removalNumber) =>
        IsKia(result) ? Destroyed : IsK(result) ? malfunctioned || removalNumber ? Destroyed : Malfunctioned : null;
}
