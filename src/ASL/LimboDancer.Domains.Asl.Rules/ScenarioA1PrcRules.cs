namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Passengers and Riders as targets (pass 65, task 65.1; rulings R65.6, R65.7): how a Collateral Attack reaches each, and the CE DRM it takes.
/// </summary>
public static class ScenarioA1PrcRules
{
    /// <summary>Vulnerable with the CE DRM (D5.31, D6.62).</summary>
    public const string Exposed = "exposed";

    /// <summary>Always Vulnerable, with the TEM and no CE DRM (D6.7, D6.23).</summary>
    public const string Open = "open";

    /// <summary>BU: attacked only from an elevation advantage beyond the range or by TPBF (D6.61, A7.211).</summary>
    public const string Buttoned = "buttoned";

    /// <summary>Not attacked: an AFV's Passengers while its crew is stunned or shocked (D.8, p. 192).</summary>
    public const string Spared = "spared";

    /// <summary>
    /// How a Collateral Attack reaches one PRC (D.8, D6.23, D6.61, D6.62, D6.7, D6.84, A7.821, D9.1; pp. 192, 205 to 207, 58, 209): a Rider and the PRC
    /// of an unarmored vehicle are always Vulnerable with no CE DRM; a Carrier's Passengers are always CE; an AFV's Passengers are spared while its crew
    /// is stunned or shocked, and otherwise CE or BU with its crew, BU when broken or pinned.
    /// </summary>
    public static string Exposure(FirePrc prc, FireDefinition vehicle, bool crewExposed, bool crewImpaired)
    {
        ArgumentNullException.ThrowIfNull(prc);
        ArgumentNullException.ThrowIfNull(vehicle);
        if (prc.Role == FirePrc.Rider || vehicle.Unarmored == true)
        {
            return Open;
        }

        // D6.84 (p. 207): a Carrier's PRC "remain CE if stunned or shocked, and are subject to the Collateral Attack".
        if (vehicle.VehicleClass == "carrier")
        {
            return Exposed;
        }

        if (crewImpaired)
        {
            return Spared;
        }

        return crewExposed && prc.Broken != true && prc.Pinned != true ? Exposed : Buttoned;
    }

    /// <summary>
    /// The CE DRM (D5.31, D6.61, pp. 203, 205): +2, one less for each full level of the firer's elevation advantage beyond the range, never below 0.
    /// </summary>
    public static int CeDrm(int levelsBeyondRange) => Math.Max(0, 2 - Math.Max(0, levelsBeyondRange));
}
