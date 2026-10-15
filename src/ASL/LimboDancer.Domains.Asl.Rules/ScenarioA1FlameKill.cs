namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The tables of a FT, MOL, or DC at an AFV (pass 60, rulings R60.2, R60.15, R60.16), read from renders of p. 190: the C7.34 HE and Flame To Kill
/// Table's Basic TK# for them (DC 16, FT 8, MOL 6; "[AF NA]" for the FT and MOL), the FT/MOL and DC columns of the C7.7 AFV Destruction Table, the
/// C7.11 AF Table's Aerial AF row, and the bands of the C7.346 DC Position DR (p. 177). Note A of C7.7 (a red CS#) is left out: the catalog has no color.
/// </summary>
public static class ScenarioA1FlameKill
{
    public const string Flamethrower = "ft";
    public const string Molotov = "mol";
    public const string DemolitionCharge = "dc";

    private static readonly string[] Hull = ["hull"];
    private static readonly string[] HullAndTurret = ["hull", "turret"];
    private static readonly string[] Facings = ["front", "side"];

    /// <summary>A modifier of a To Kill or Position DR in a player's words, with its sign: "CE +1", "Long Range -4".</summary>
    public static string ModifierWords(FireModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);
        var name = modifier.Name switch
        {
            "long-range" => "Long Range",
            "crew-exposed" => "CE",
            "open-topped" => "OT",
            "moving-target" => "moving target",
            "case-a" => "rear",
            "case-b" => "elevation advantage",
            "non-stopped-or-concealed" => "Non-Stopped, Motion, or concealed",
            "thrown-dc" => "Thrown",
            "hull-front" => "hull front",
            "hull-rear" => "hull rear",
            "thrown-dc-afph" => "Thrown in the AFPh",
            "immobile" => "Immobile",
            var other when other.StartsWith("cx", StringComparison.Ordinal) => "CX",
            var other => other,
        };
        return $"{name} {(modifier.Value >= 0 ? "+" : string.Empty)}{modifier.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}";
    }

    /// <summary>A list of modifiers, each in a player's words after a comma; empty with none.</summary>
    public static string ModifierText(IReadOnlyList<FireModifier> modifiers)
    {
        ArgumentNullException.ThrowIfNull(modifiers);
        return string.Concat(modifiers.Select(item => ", " + ModifierWords(item)));
    }

    /// <summary>The C7.34 Basic TK# (p. 190): DC 16, FT 8, MOL 6.</summary>
    public static int BasicTk(string weapon) => weapon switch
    {
        DemolitionCharge => 16,
        Flamethrower => 8,
        _ => 6,
    };

    /// <summary>
    /// The FT/MOL column of the C7.7 AFV Destruction Table (p. 190; A22.612, C7.5, C7.6): an Original 12 is a Dud (C7.35); a DR below the Final TK#
    /// burns, "all vehicles eliminated by a FT/MOL attack DR &lt; its To Kill Number are automatically burning wrecks"; equal eliminates, with Crew
    /// Survival; above it, nothing. A FT or MOL never immobilizes.
    /// </summary>
    public static string FlameResult(int dr, int finalTk) =>
        dr == 12 ? OrdnanceKill.Dud : dr < finalTk ? OrdnanceKill.Burn : dr == finalTk ? OrdnanceKill.Eliminated : OrdnanceKill.None;

    /// <summary>
    /// The DC column of the C7.7 AFV Destruction Table (p. 190; C7.41, C7.5, C7.6): an Original 12 is a Dud; at most half the Final TK# burns; below it
    /// eliminates; equal to it, and one above it, Shocks a turret hit and immobilizes a hull hit.
    /// </summary>
    public static string ChargeResult(int dr, int finalTk, bool turret) =>
        dr == 12 ? OrdnanceKill.Dud
            : dr * 2 <= finalTk ? OrdnanceKill.Burn
            : dr < finalTk ? OrdnanceKill.Eliminated
            : dr == finalTk || dr == finalTk + 1 ? (turret ? OrdnanceKill.Shock : OrdnanceKill.Immobilized)
            : OrdnanceKill.None;

    /// <summary>The result of a DC's Final Position DR (C7.346, p. 177): 5 or less optimal, 6 to 8 positioned, 9 to 11 poor, 12 or more none.</summary>
    public static string PositionResult(int finalDr) =>
        finalDr <= 5 ? FirePosition.Optimal : finalDr <= 8 ? FirePosition.Positioned : finalDr <= 11 ? FirePosition.Poor : FirePosition.None;

    /// <summary>
    /// The Aerial AF (C7.11, C7.12, pp. 176, 190): the value the AF Table lists beneath the AFV's worst AF, hull or turret: 0 and 1 give 0, 2 gives 1, 3
    /// gives 2, 4 and 6 give 3, 8 and 11 give 4. The table lists none beneath 14, 18, and 26; there the lowest printed hull AF stands (ruling R60.16).
    /// </summary>
    public static int? AerialAf(ArmorDefinition armor)
    {
        ArgumentNullException.ThrowIfNull(armor);
        int[] factors = [.. (armor.Turreted ? HullAndTurret : Hull).SelectMany(place => Facings.Select(facing => ScenarioA1ArmorReference.ArmorFactor(armor, place, facing))).OfType<int>()];
        if (factors.Length == 0)
        {
            return null;
        }

        return factors.Min() switch
        {
            0 or 1 => 0,
            2 => 1,
            3 => 2,
            4 or 6 => 3,
            8 or 11 => 4,
            _ => new[] { armor.FrontAf, armor.SideAf }.OfType<int>().Min(),
        };
    }
}
