namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The HE Equivalency Table of C8.31 (p. 178; pass 60, rulings R60.2, R60.4 to R60.6), read from a render of the page: the IFT FP that AP, APCR,
/// APDS, an ATR, and HEAT attack Personnel and Guns with. AP below 37mm 1, AP of 37mm and up 2; every APCR, APDS, and ATR 1 (note 2; never a MG);
/// a BAZ or PIAT 8; a PSK, a PFk, and the H#[9] of the German 37mm AT and AA Guns 12 (note 3); a PF 16; the HEAT of other ordnance the IFT column
/// to the left of the Gun's normal HE FP. None of them leaves Residual FP (notes 1 and 4).
/// </summary>
public static class ScenarioA1HeEquivalency
{
    /// <summary>A Gun or a vehicle's MA firing its ammunition.</summary>
    public const string Gun = "gun";

    public const string Panzerfaust = "pf";

    public const string PanzerfaustKlein = "pfk";

    public const string Panzerschreck = "psk";

    public const string Bazooka = "baz";

    public const string Piat = "piat";

    public const string AntiTankRifle = "atr";

    /// <summary>The HEAT of a German 37mm AT or AA Gun, printed H#[9] (note 3).</summary>
    public const string BracketedHeat = "h9";

    /// <summary>
    /// The equivalency FP of the weapon's round (C8.31), or null where the table gives none: HE, Canister, or no ammunition named by a Gun, and the
    /// HEAT of a Gun with no printed HE FP to step left from. <paramref name="heFirepower"/> is the Gun's normal HE FP, null when it has no HE.
    /// </summary>
    public static int? Firepower(string? ammunition, string weapon, int caliber, int? heFirepower) => weapon switch
    {
        Panzerfaust => 16,
        PanzerfaustKlein or Panzerschreck or BracketedHeat => 12,
        Bazooka or Piat => 8,
        AntiTankRifle => 1,
        Gun => ammunition switch
        {
            "ap" => caliber < 37 ? 1 : 2,
            "apcr" or "apds" => 1,
            "heat" => heFirepower is { } fp && ScenarioA1FireReference.ColumnFp.Where(column => column < fp).ToArray() is { Length: > 0 } left ? left.Max() : null,
            _ => null,
        },
        _ => null,
    };

    /// <summary>The weapon kind a Gun definition fires as (C8.31): a LATW by its type, any other Gun or MA as a Gun.</summary>
    public static string Weapon(GunDefinition gun)
    {
        ArgumentNullException.ThrowIfNull(gun);
        return gun.GunType == "latw" && gun.LatwType is { } latw ? latw : Gun;
    }
}
