using System.Text.Json;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A reviewed catalog definition as the Scenario A1 packages read it: printed values only (manufactured ones for the MGs
/// of catalog 1.2.0, ruling R0.3). Values a definition does not print are null.
/// </summary>
public sealed record FireDefinition(
    string Id,
    string Kind,
    string Nationality,
    string? Class,
    int? Firepower,
    int? Range,
    int? Morale,
    int? BrokenMorale,
    int? Leadership,
    bool? UnderscoredMorale)
{
    /// <summary>Self-Rally capability: the broken Morale Level printed in a square (A10.63).</summary>
    public bool? SelfRally
    {
        get; init;
    }

    /// <summary>The Assault Fire bonus: an underlined FP (A1.21, A7.36).</summary>
    public bool? AssaultFire
    {
        get; init;
    }

    /// <summary>A SW's Breakdown Number (A9.7).</summary>
    public int? Breakdown
    {
        get; init;
    }

    /// <summary>A MG's Multiple ROF (A9.2); null when it has none.</summary>
    public int? RateOfFire
    {
        get; init;
    }

    /// <summary>A SW's Repair Number (A9.72).</summary>
    public int? Repair
    {
        get; init;
    }

    /// <summary>A hero's wounded side (A15.2): FP, range, and Morale Level; null for other units.</summary>
    public int? WoundedFirepower
    {
        get; init;
    }

    public int? WoundedRange
    {
        get; init;
    }

    public int? WoundedMorale
    {
        get; init;
    }

    public bool IsLeader => Kind == "asl:leader";

    public bool IsHero => Kind == "asl:hero";

    public bool IsMmc => Kind is "asl:squad" or "asl:half-squad";

    public bool IsMg => Kind == "asl:mg";

    /// <summary>A vehicle's movement type (D1.1): truck, half-tracked, and so on.</summary>
    public string? MovementType
    {
        get; init;
    }

    /// <summary>A vehicle's printed MP allotment (D1.1).</summary>
    public int? MovementPoints
    {
        get; init;
    }

    /// <summary>A vehicle's Towing Number (C10.1); null when it cannot tow.</summary>
    public int? Towing
    {
        get; init;
    }

    /// <summary>Whether a vehicle is unarmored (D1.21).</summary>
    public bool? Unarmored
    {
        get; init;
    }

    /// <summary>Whether an AFV is open-topped (D1.23).</summary>
    public bool? OpenTopped
    {
        get; init;
    }

    /// <summary>A vehicle's printed Passenger capacity in PP (D1.5, D6.1); null when it carries none.</summary>
    public int? PassengerCapacity
    {
        get; init;
    }

    /// <summary>Whether a Gun has Quick Set-Up, so it is never (un)limbered (C10.23).</summary>
    public bool? QuickSetUp
    {
        get; init;
    }

    /// <summary>A vehicle's MA weapon (D1.3): <c>aamg</c> for an MA AAMG.</summary>
    public string? MainArmament
    {
        get; init;
    }

    /// <summary>A vehicle's MA Type (D1.3 to D1.322): <c>t</c>, <c>st</c>, <c>rst</c>, <c>1mt</c>, or <c>nt</c>; null when it has none.</summary>
    public string? MaType
    {
        get; init;
    }

    /// <summary>
    /// D1.321, D1.322 (p. 194; pass 35, task 35.13 c): whether the vehicle's MA and CMG may not fire because its crew is CE: a Restricted Slow
    /// Traverse or One-Man Turret AFV fires neither while CE.
    /// </summary>
    public bool TurretBarredWhileCe(bool? crewExposed) => MaType is "rst" or "1mt" && crewExposed == true;

    /// <summary>A vehicle's AAMG FP (D1.8, D1.83).</summary>
    public int? AntiAircraftMg
    {
        get; init;
    }

    /// <summary>A vehicle's BMG FP (D1.8); null when it has none (backlog pass 11, ruling R11.11).</summary>
    public int? BowMg
    {
        get; init;
    }

    /// <summary>A vehicle's CMG FP (D1.8); null when it has none (ruling R11.11).</summary>
    public int? CoaxialMg
    {
        get; init;
    }

    /// <summary>A vehicle's MA caliber in mm (D1.3); null when its MA is not a Gun (ruling R11.11).</summary>
    public int? Caliber
    {
        get; init;
    }

    /// <summary>A vehicle's ground pressure (D8.21): <c>low</c> or <c>high</c> as printed; null when none is printed, which is Normal (ruling R11.9).</summary>
    public string? GroundPressure
    {
        get; init;
    }

    /// <summary>Whether a vehicle's MP are printed red: mechanically unreliable (D2.51; ruling R11.4).</summary>
    public bool? MechanicallyUnreliable
    {
        get; init;
    }

    public bool IsVehicle => Kind == "asl:vehicle";

    /// <summary>A LATW's type (C13.1); null otherwise.</summary>
    public string? LatwType
    {
        get; init;
    }

    /// <summary>An ATR, which attacks Personnel as 1 FP Small Arms Fire (C13.24; pass 9b).</summary>
    public bool IsAtr => Kind == "asl:latw" && LatwType == "atr";

    /// <summary>A FT (A22; backlog pass 15, ruling R15.1).</summary>
    public bool IsFt => Kind == "asl:ft";

    /// <summary>A DC (A23; rulings R15.2, R15.3).</summary>
    public bool IsDc => Kind == "asl:dc";

    /// <summary>Whether the SW's Breakdown Number is an X#: reaching it removes the SW (A.11).</summary>
    public bool? BreakdownRemoves
    {
        get; init;
    }
}

/// <summary>
/// The pinned reference data of the Fire package: the IFT and Terrain Chart transcriptions of the reviewed chart
/// supplement and the reviewed Scenario A1 catalog, each checked against the digest the case matrix records.
/// </summary>
public sealed class ScenarioA1FireReference
{
    public static readonly int[] ColumnFp = [1, 2, 4, 6, 8, 12, 16, 20, 24, 30, 36];

    // Direct Fire TEM of the admitted terrain: Terrain Chart p. 698, B1.1, B13.3, B14.3, B15.3, B23.3.
    public static readonly IReadOnlyDictionary<string, int> Tem = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["open-ground"] = 0,
        ["brush"] = 0,
        ["woods"] = 1,
        ["orchard"] = 0,
        ["grain"] = 0,
        ["wooden-building"] = 2,
        ["stone-building"] = 3,

        // Backlog pass 10 (ruling R10.1): marsh has no TEM (B16.3); rubble has its building's (B24.3).
        ["marsh"] = 0,
        ["wooden-rubble"] = 2,
        ["stone-rubble"] = 3,
    };

    /// <summary>The Direct Fire TEM of a wall and a hedge hexside (B9.3; the pass 10 Terrain Chart rows).</summary>
    public static readonly IReadOnlyDictionary<string, int> HexsideTem = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["wall"] = 2,
        ["hedge"] = 1,
    };

    // The Casualty Reduction a squad suffers (A7.302): its half-squad of the same class (catalogs 1.1.0 and 1.3.0).
    private static readonly IReadOnlyDictionary<string, string> HalfSquads = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["attacker-squad"] = "attacker-half-squad",
        ["attacker-2nd-line-squad"] = "attacker-2nd-line-half-squad",
        ["attacker-conscript-squad"] = "attacker-conscript-half-squad",
        ["attacker-elite-squad"] = "attacker-elite-half-squad",
        ["defender-squad"] = "defender-half-squad",
        ["defender-conscript-squad"] = "defender-conscript-half-squad",
        ["defender-elite-squad"] = "defender-elite-half-squad",
        ["defender-line-squad"] = "defender-line-half-squad",
        ["defender-guards-squad"] = "defender-guards-half-squad",

        // Backlog pass 15 (ruling R15.9): the NKVD squad's own HS, which its Casualty Reduction and its underscored Replacement make.
        ["defender-nkvd-squad"] = "defender-nkvd-half-squad",

        // Backlog pass 15 (ruling R15.13): the MMC of five more nationalities (catalog 1.10.0).
        ["american-elite-squad"] = "american-elite-half-squad",
        ["american-squad"] = "american-half-squad",
        ["american-2nd-line-squad"] = "american-2nd-line-half-squad",
        ["american-green-squad"] = "american-green-half-squad",
        ["british-elite-squad"] = "british-elite-half-squad",
        ["british-squad"] = "british-half-squad",
        ["british-2nd-line-squad"] = "british-2nd-line-half-squad",
        ["british-green-squad"] = "british-green-half-squad",
        ["italian-elite-squad"] = "italian-elite-half-squad",
        ["italian-squad"] = "italian-half-squad",
        ["italian-line-squad"] = "italian-line-half-squad",
        ["italian-conscript-squad"] = "italian-conscript-half-squad",
        ["finnish-elite-squad"] = "finnish-elite-half-squad",
        ["finnish-squad"] = "finnish-half-squad",
        ["finnish-square-squad"] = "finnish-square-half-squad",
        ["finnish-2nd-line-squad"] = "finnish-2nd-line-half-squad",
        ["finnish-green-squad"] = "finnish-green-half-squad",
        ["finnish-conscript-squad"] = "finnish-conscript-half-squad",
        ["french-elite-squad"] = "french-elite-half-squad",
        ["french-squad"] = "french-half-squad",
        ["french-green-squad"] = "french-green-half-squad",

        // Pass 19b (ruling R19.7): the German circled-E 5-4-8 and plain-E 8-3-8 of the scenario cards (National Capabilities Chart, p. 695).
        ["attacker-elite-squad-5-4-8"] = "attacker-elite-half-squad-2-3-8",
        ["attacker-elite-squad-8-3-8"] = "attacker-elite-half-squad-3-3-8",

        // Pass 27 (ruling R27.1): the Axis Minor MMC (catalog 1.13.0; National Capabilities Chart, p. 695).
        ["axis-minor-elite-squad"] = "axis-minor-elite-half-squad",
        ["axis-minor-square-squad"] = "axis-minor-square-half-squad",
        ["axis-minor-squad"] = "axis-minor-half-squad",
        ["axis-minor-conscript-squad"] = "axis-minor-conscript-half-squad",
    };

    // The leader grades from worst to best (Chapter H leader table, p. 331; A15.3): 6+1, 7-0, 8-0, 8-1, 9-1, 9-2, 10-2, 10-3.
    private static readonly string[] GermanLeaders =
        [.. new[] { "6-plus-1", "7-0", "8-0", "8-1", "9-1", "9-2", "10-2", "10-3" }.Select(grade => "attacker-leader-" + grade)];

    private static readonly string[] RussianLeaders =
    [
        "defender-leader-6-plus-1", "defender-leader-7-0", "defender-leader", "defender-leader-8-1", "defender-leader-9-1", "defender-leader-9-2",
        "defender-leader-10-2", "defender-leader-10-3",
    ];

    // Backlog pass 15 (ruling R15.13): every grade of the five nationalities added by catalog 1.10.0.
    // The Finns have their own ranks (A25.71, A25.72; referee, pass 15): 8+1, 8-0, 9-0, 9-1, 10-0, 10-1.
    private static readonly string[][] OtherLeaders = [.. new[] { "american", "british", "italian", "french", "axis-minor" }
        .Select(nationality => new[] { "6-plus-1", "7-0", "8-0", "8-1", "9-1", "9-2", "10-2", "10-3" }.Select(grade => $"{nationality}-leader-{grade}").ToArray()),
        [.. new[] { "8-plus-1", "8-0", "9-0", "9-1", "10-0", "10-1" }.Select(grade => $"finnish-leader-{grade}")]];

    // The Commissars (A25.22, A25.224; ruling R15.6): leaders outside the grades, never Replaced or Battle Hardened (A25.221).
    private static readonly HashSet<string> Commissars = new(StringComparer.Ordinal)
    {
        "defender-commissar-9-0", "defender-commissar-10-0", "defender-commissar-8-plus-1",
    };

    // Battle Hardening (A15.3): the unit of the same size and next higher quality, no part of whose Strength Factor falls,
    // gaining the least (ruling R28.6: the smallest summed increase, then the fewest added capabilities, so a German
    // Conscript becomes a 4-4-7, not a squared 5-3-7 that adds smoke and Assault Fire). A Russian 4-2-6 becomes a 5-2-7, as
    // A25.2 says in so many words (p. 93; pass 35, task 35.8), and its 2-2-6 HS the 5-2-7's own 2-2-7 (A25.211 pairs them);
    // until pass 35 the table sent both to the NKVD 6-2-8 and 3-2-8, reasoning from the classes. A25.211's exception for
    // scenarios before 1941 (a 4-4-7 and a 2-3-7, unless the Russian OB holds a 6-2-8 or a 5-2-7) is not built (backlog).
    // Elite MMC and the 10-3 are the highest quality and become Fanatic instead, as does an NKVD MMC (A25.25).
    private static readonly IReadOnlyDictionary<string, string> Hardened = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["attacker-conscript-squad"] = "attacker-2nd-line-squad",
        ["attacker-conscript-half-squad"] = "attacker-2nd-line-half-squad",
        ["attacker-2nd-line-squad"] = "attacker-squad",
        ["attacker-2nd-line-half-squad"] = "attacker-half-squad",
        ["attacker-squad"] = "attacker-elite-squad",
        ["attacker-half-squad"] = "attacker-elite-half-squad",
        ["defender-conscript-squad"] = "defender-line-squad",
        ["defender-conscript-half-squad"] = "defender-line-half-squad",
        ["defender-line-squad"] = "defender-guards-squad",
        ["defender-line-half-squad"] = "defender-guards-half-squad",
        ["defender-squad"] = "defender-elite-squad",
        ["defender-half-squad"] = "defender-elite-half-squad",

        // Backlog pass 15 (ruling R15.13; referee, pass 15): each nationality's next class; the Italians and Finns by their own progressions (A25.62, A25.72).
        ["american-green-squad"] = "american-2nd-line-squad",
        ["american-2nd-line-squad"] = "american-squad",
        ["american-squad"] = "american-elite-squad",
        ["american-green-half-squad"] = "american-2nd-line-half-squad",
        ["american-2nd-line-half-squad"] = "american-half-squad",
        ["american-half-squad"] = "american-elite-half-squad",
        ["british-green-squad"] = "british-2nd-line-squad",
        ["british-2nd-line-squad"] = "british-squad",
        ["british-squad"] = "british-elite-squad",
        ["british-green-half-squad"] = "british-2nd-line-half-squad",
        ["british-2nd-line-half-squad"] = "british-half-squad",
        ["british-half-squad"] = "british-elite-half-squad",
        ["italian-conscript-squad"] = "italian-line-squad",
        ["italian-line-squad"] = "italian-elite-squad",
        ["italian-squad"] = "italian-elite-squad",
        ["italian-conscript-half-squad"] = "italian-line-half-squad",
        ["italian-line-half-squad"] = "italian-elite-half-squad",
        ["italian-half-squad"] = "italian-elite-half-squad",
        ["finnish-green-squad"] = "finnish-squad",
        ["finnish-conscript-squad"] = "finnish-2nd-line-squad",
        ["finnish-2nd-line-squad"] = "finnish-square-squad",
        ["finnish-green-half-squad"] = "finnish-half-squad",
        ["finnish-conscript-half-squad"] = "finnish-2nd-line-half-squad",
        ["finnish-2nd-line-half-squad"] = "finnish-square-half-squad",
        ["french-green-squad"] = "french-squad",
        ["french-squad"] = "french-elite-squad",
        ["french-green-half-squad"] = "french-half-squad",
        ["french-half-squad"] = "french-elite-half-squad",

        // Pass 27 (ruling R27.1; A25.84): an Axis Minor Conscript becomes a 3-4-7, a 3-4-7 the elite 4-4-7 (the 5-3-7 would lower
        // the range); the 5-3-7 and its 2-2-7 HS become Fanatic instead (referee, pass 27).
        ["axis-minor-conscript-squad"] = "axis-minor-squad",
        ["axis-minor-squad"] = "axis-minor-elite-squad",
        ["axis-minor-conscript-half-squad"] = "axis-minor-half-squad",
        ["axis-minor-half-squad"] = "axis-minor-elite-half-squad",
    };

    private static readonly HashSet<string> HighestQuality = new(StringComparer.Ordinal)
    {
        "attacker-elite-squad", "attacker-elite-half-squad", "defender-elite-squad", "defender-elite-half-squad", "defender-guards-squad",
        "defender-guards-half-squad", "attacker-leader-10-3", "defender-leader-10-3", "defender-nkvd-squad", "defender-nkvd-half-squad",

        // Backlog pass 15 (ruling R15.13).
        "american-elite-squad",
        "american-elite-half-squad",
        "british-elite-squad",
        "british-elite-half-squad",
        "italian-elite-squad",
        "italian-elite-half-squad",
        "finnish-elite-squad",
        "finnish-elite-half-squad",
        "finnish-squad",
        "finnish-half-squad",
        "finnish-square-squad",
        "finnish-square-half-squad",
        "french-elite-squad",
        "french-elite-half-squad",
        "american-leader-10-3", "british-leader-10-3", "italian-leader-10-3", "finnish-leader-10-1", "french-leader-10-3",

        // Pass 19b (ruling R19.7): elite MMC, so Battle Hardening makes them Fanatic (A15.3).
        "attacker-elite-squad-5-4-8", "attacker-elite-half-squad-2-3-8", "attacker-elite-squad-8-3-8", "attacker-elite-half-squad-3-3-8",

        // Pass 27 (ruling R27.1): the Axis Minor elite MMC and 10-3, and the 5-3-7 and its 2-2-7, which A25.84 makes Fanatic when they Battle Harden.
        "axis-minor-elite-squad", "axis-minor-elite-half-squad", "axis-minor-square-squad", "axis-minor-square-half-squad", "axis-minor-leader-10-3",
    };

    // The NKVD MMC (A25.25): 2nd Line, ELR 5, a -1 Heat of Battle DRM, Fanatic when Battle Hardened, Commissars by Field Promotion.
    private static readonly HashSet<string> Nkvd = new(StringComparer.Ordinal) { "defender-nkvd-squad", "defender-nkvd-half-squad" };

    // ELR Replacement (A19.13): a unit of lesser quality and the same size, whose Class drops and no part of whose
    // Strength Factor rises; a leader of the next lower quality. A unit missing here cannot be Replaced (A19.12).
    private static readonly IReadOnlyDictionary<string, string> Replacements = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["attacker-squad"] = "attacker-2nd-line-squad",
        ["attacker-half-squad"] = "attacker-2nd-line-half-squad",
        ["attacker-2nd-line-squad"] = "attacker-conscript-squad",
        ["attacker-2nd-line-half-squad"] = "attacker-conscript-half-squad",
        ["attacker-elite-squad"] = "attacker-squad",
        ["attacker-elite-half-squad"] = "attacker-half-squad",
        ["defender-squad"] = "defender-conscript-squad",
        ["defender-half-squad"] = "defender-conscript-half-squad",
        ["defender-elite-squad"] = "defender-squad",
        ["defender-elite-half-squad"] = "defender-half-squad",
        ["defender-guards-squad"] = "defender-line-squad",
        ["defender-guards-half-squad"] = "defender-line-half-squad",
        ["defender-line-squad"] = "defender-conscript-squad",
        ["defender-line-half-squad"] = "defender-conscript-half-squad",
        ["defender-nkvd-squad"] = "defender-conscript-squad",
        ["defender-nkvd-half-squad"] = "defender-conscript-half-squad",

        // Backlog pass 15 (ruling R15.13; referee, pass 15): each nationality's next lower class; the Italians and Finns by their own progressions (A25.61,
        // A25.62, A25.72). A Finnish elite MMC is in neither progression, so it is not Replaced.
        ["american-elite-squad"] = "american-squad",
        ["american-squad"] = "american-2nd-line-squad",
        ["american-2nd-line-squad"] = "american-green-squad",
        ["american-elite-half-squad"] = "american-half-squad",
        ["american-half-squad"] = "american-2nd-line-half-squad",
        ["american-2nd-line-half-squad"] = "american-green-half-squad",
        ["british-elite-squad"] = "british-squad",
        ["british-squad"] = "british-2nd-line-squad",
        ["british-2nd-line-squad"] = "british-green-squad",
        ["british-elite-half-squad"] = "british-half-squad",
        ["british-half-squad"] = "british-2nd-line-half-squad",
        ["british-2nd-line-half-squad"] = "british-green-half-squad",
        ["italian-elite-squad"] = "italian-line-squad",
        ["italian-squad"] = "italian-conscript-squad",
        ["italian-line-squad"] = "italian-conscript-squad",
        ["italian-elite-half-squad"] = "italian-line-half-squad",
        ["italian-half-squad"] = "italian-conscript-half-squad",
        ["italian-line-half-squad"] = "italian-conscript-half-squad",
        ["finnish-squad"] = "finnish-green-squad",
        ["finnish-square-squad"] = "finnish-2nd-line-squad",
        ["finnish-2nd-line-squad"] = "finnish-conscript-squad",
        ["finnish-half-squad"] = "finnish-green-half-squad",
        ["finnish-square-half-squad"] = "finnish-2nd-line-half-squad",
        ["finnish-2nd-line-half-squad"] = "finnish-conscript-half-squad",
        ["french-elite-squad"] = "french-squad",
        ["french-squad"] = "french-green-squad",
        ["french-elite-half-squad"] = "french-half-squad",
        ["french-half-squad"] = "french-green-half-squad",

        // Pass 19b (ruling R19.7): the circled-E 5-4-8 by the 2nd Line 4-4-7 and its 2-3-8 HS by the 2-3-7 (a 1st Line 4-6-7 or
        // 2-4-7 would raise the range, and no Replacement makes a Volksgrenadier 5-3-7, A25.13). The plain-E 8-3-8 has an
        // underscored Morale Factor, so it is Replaced by its two broken 3-3-8 HS and a 3-3-8 is Disrupted (A19.13).
        ["attacker-elite-squad-5-4-8"] = "attacker-2nd-line-squad",
        ["attacker-elite-half-squad-2-3-8"] = "attacker-2nd-line-half-squad",

        // Pass 27 (ruling R27.1; A25.84): the Axis Minor elite by the 1st Line 3-4-7, and the 5-3-7 and 3-4-7 by the Conscript, with their HS.
        ["axis-minor-elite-squad"] = "axis-minor-squad",
        ["axis-minor-square-squad"] = "axis-minor-conscript-squad",
        ["axis-minor-squad"] = "axis-minor-conscript-squad",
        ["axis-minor-elite-half-squad"] = "axis-minor-half-squad",
        ["axis-minor-square-half-squad"] = "axis-minor-conscript-half-squad",
        ["axis-minor-half-squad"] = "axis-minor-conscript-half-squad",
    };

    // A19.13 EXC (ruling R19.7; referee, pass 19b): the broken HS of lesser quality a Casualty MC beyond its ELR Reduces a squad to, where
    // its own HS has an underscored Morale Factor and so is never Replaced: the 8-3-8 falls to the 2-3-7.
    private static readonly IReadOnlyDictionary<string, string> CasualtyHalfSquads = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["attacker-elite-half-squad-3-3-8"] = "attacker-2nd-line-half-squad",
    };

    private readonly string[][] results;

    private readonly int[] killNumbers;

    private ScenarioA1FireReference(string[][] results, int[] killNumbers, IReadOnlyDictionary<string, FireDefinition> definitions)
    {
        this.results = results;
        this.killNumbers = killNumbers;
        Definitions = definitions;
    }

    /// <summary>The Vehicle line's Kill Number for a column index (A7.308).</summary>
    public int KillNumber(int column) => killNumbers[column];

    /// <summary>
    /// The Morale Level of an AFV's Inherent crew (D5.1): that of its nationality's best unbroken elite Infantry MMC, read from the
    /// reviewed catalog's elite squads and half-squads; null when the catalog has none.
    /// </summary>
    public int? AfvCrewMorale(string nationality) =>
        Definitions.Values.Where(item => item.IsMmc && item.Nationality == nationality && item.Class == "elite").Max(item => item.Morale);

    public IReadOnlyDictionary<string, FireDefinition> Definitions
    {
        get;
    }

    /// <summary>The IFT result for a Final DR and a column index; <c>none</c> is the printed dash.</summary>
    public string Result(int finalDr, int column) => results[Math.Clamp(finalDr, 0, 15)][column];

    /// <summary>The half-squad a squad is Reduced to, or null when the catalog has none.</summary>
    public static string? HalfSquadOf(string definitionId) => HalfSquads.GetValueOrDefault(definitionId);

    /// <summary>The squad two HS of a definition Recombine into (A1.32), or null when the catalog has none.</summary>
    public static string? SquadOf(string halfSquadId) => HalfSquads.FirstOrDefault(pair => pair.Value == halfSquadId).Key;

    /// <summary>The unit that Replaces a definition under A19.13, or null when none can (A19.12): a leader of the next lower grade.</summary>
    public static string? ReplacementOf(string definitionId) =>
        Replacements.GetValueOrDefault(definitionId) ?? Step(definitionId, -1);

    /// <summary>
    /// The broken HS of lesser quality a squad whose Casualty MC exceeds its ELR is Reduced to (A19.13 EXC), given its own HS: that HS's
    /// Replacement, or for an underscored HS never Replaced the one ruled for it (ruling R19.7); null when none exists.
    /// </summary>
    public static string? CasualtyHalfSquadOf(string halfSquadId) => ReplacementOf(halfSquadId) ?? CasualtyHalfSquads.GetValueOrDefault(halfSquadId);

    /// <summary>The unit a definition is Battle Hardened into (A15.3), or null for the highest quality or an unreviewed one.</summary>
    public static string? HardenedOf(string definitionId) => Hardened.GetValueOrDefault(definitionId) ?? Step(definitionId, 1);

    /// <summary>Whether Battle Hardening makes a definition Fanatic rather than exchanging it (A15.3, A25.25).</summary>
    public static bool IsHighestQuality(string definitionId) => HighestQuality.Contains(definitionId);

    /// <summary>Whether a definition is an NKVD MMC (A25.25).</summary>
    public static bool IsNkvd(string definitionId) => Nkvd.Contains(definitionId);

    /// <summary>Whether a definition is a Commissar (A25.22; ruling R15.6).</summary>
    public static bool IsCommissar(string definitionId) => Commissars.Contains(definitionId);

    private static string? Step(string definitionId, int by)
    {
        foreach (var chain in new[] { GermanLeaders, RussianLeaders }.Concat(OtherLeaders))
        {
            var index = Array.IndexOf(chain, definitionId);
            if (index >= 0)
            {
                var next = index + by;
                return next >= 0 && next < chain.Length ? chain[next] : null;
            }
        }

        return null;
    }

    internal static ScenarioA1FireReference Load(JsonElement matrix)
    {
        using var ift = ScenarioA1FirePackage.Read("ScenarioA1.fire-ift.json", matrix.GetProperty("iftTranscriptionSha256").GetString()!);
        using var terrain = ScenarioA1FirePackage.Read("ScenarioA1.fire-terrain-chart.json",
            matrix.GetProperty("terrainChartTranscriptionSha256").GetString()!);
        using var catalog = ScenarioA1FirePackage.Read("ScenarioA1.fire-catalog.json", matrix.GetProperty("catalogSha256").GetString()!);

        var columns = ift.RootElement.GetProperty("columns").EnumerateArray().Select(item => item.GetProperty("fp").GetInt32()).ToArray();
        var rows = ift.RootElement.GetProperty("rows").EnumerateArray().ToArray();
        if (!columns.SequenceEqual(ColumnFp) || rows.Length != 16
            || rows.Select(row => row.GetProperty("dr").GetInt32()).Where((dr, index) => dr != index).Any())
        {
            throw new InvalidOperationException("The IFT transcription changed shape.");
        }

        var table = rows.Select(row => row.GetProperty("results").EnumerateArray().Select(cell => cell.GetString()!).ToArray()).ToArray();

        // The TEM the package uses must agree with the printed Terrain Chart cells.
        var printed = terrain.RootElement.GetProperty("rows").EnumerateArray()
            .ToDictionary(row => row.GetProperty("terrain").GetString()!, row => row.GetProperty("temIndirect").GetString()!, StringComparer.Ordinal);
        if (printed.GetValueOrDefault("1. Open Ground") != "FFMO: -1*" || printed.GetValueOrDefault("12. Brush") != "0"
            || printed.GetValueOrDefault("13. Woods") != "+1/-1" || printed.GetValueOrDefault("14. Orchard") != "0"
            || printed.GetValueOrDefault("15. Grain") != "0" || printed.GetValueOrDefault("23. Wooden Building") != "+2(+1*)"
            || printed.GetValueOrDefault("23. Stone Building") != "+3(+1*)")
        {
            throw new InvalidOperationException("The Terrain Chart TEM cells changed.");
        }

        // Backlog pass 10 (ruling R10.1): the wall, hedge, marsh, and rubble TEM cells of the pass 10 rows.
        using var pass10 = ScenarioA1FirePackage.Read("ScenarioA1.fire-terrain-chart-pass10.json",
            matrix.GetProperty("terrainChartPass10TranscriptionSha256").GetString()!);
        var pass10Cells = pass10.RootElement.GetProperty("rows").EnumerateArray()
            .ToDictionary(row => row.GetProperty("terrain").GetString()!, row => row.GetProperty("temIndirect").GetString()!, StringComparer.Ordinal);
        if (pass10Cells.GetValueOrDefault("9. Wall") != "+2/+1 ©" || pass10Cells.GetValueOrDefault("9. Hedge") != "+1/0 ©"
            || pass10Cells.GetValueOrDefault("16. Marsh [Mudflat]") != "0*" || pass10Cells.GetValueOrDefault("24. Rubble") != "+2 or +3*")
        {
            throw new InvalidOperationException("The pass 10 Terrain Chart TEM cells changed.");
        }

        // A7.308: the Vehicle line's Kill Number beneath each FP column (IFT, p. 692).
        using var vehicleLine = ScenarioA1FirePackage.Read("ScenarioA1.fire-ift-vehicle-line.json", matrix.GetProperty("vehicleLineTranscriptionSha256").GetString()!);
        var kills = vehicleLine.RootElement.GetProperty("killNumbers").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        if (!vehicleLine.RootElement.GetProperty("columns").EnumerateArray().Select(item => item.GetInt32()).SequenceEqual(ColumnFp)
            || !kills.SequenceEqual([3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13]))
        {
            throw new InvalidOperationException("The IFT Vehicle line transcription changed.");
        }

        return new ScenarioA1FireReference(table, kills, ReadDefinitions(catalog.RootElement));
    }

    /// <summary>Every definition of a reviewed catalog, by id.</summary>
    internal static Dictionary<string, FireDefinition> ReadDefinitions(JsonElement catalog) =>
        catalog.GetProperty("definitions").EnumerateArray().Select(Definition).ToDictionary(item => item.Id, StringComparer.Ordinal);

    private static FireDefinition Definition(JsonElement item)
    {
        string? Text(string face, string attribute)
        {
            foreach (var value in item.GetProperty("values").EnumerateArray())
            {
                if (value.GetProperty("face").GetString() == face && value.TryGetProperty("attribute", out var name)
                    && name.GetString() == attribute && value.TryGetProperty("value", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString();
                }
            }

            return null;
        }

        int? Value(string face, string attribute)
        {
            foreach (var value in item.GetProperty("values").EnumerateArray())
            {
                if (value.GetProperty("face").GetString() == face && value.TryGetProperty("attribute", out var name)
                    && name.GetString() == attribute && value.TryGetProperty("value", out var number) && number.ValueKind == JsonValueKind.Number)
                {
                    return number.GetInt32();
                }
            }

            return null;
        }

        bool? Trait(string face, string trait)
        {
            foreach (var value in item.GetProperty("values").EnumerateArray())
            {
                if (value.GetProperty("face").GetString() == face && value.TryGetProperty("trait", out var name) && name.GetString() == trait)
                {
                    return value.TryGetProperty("present", out var present) ? present.GetBoolean() : null;
                }
            }

            return null;
        }

        return new FireDefinition(
            item.GetProperty("id").GetString()!,
            item.GetProperty("kind").GetString()!,
            item.GetProperty("nationality").GetString()!,
            item.TryGetProperty("class", out var cls) && cls.ValueKind == JsonValueKind.String ? cls.GetString() : null,
            Value("front", "firepower"),
            Value("front", "range"),
            Value("front", "morale"),
            Value("broken", "broken-morale"),
            Value("front", "leadership"),
            Trait("front", "asl:elr-5"))
        {
            SelfRally = Trait("broken", "asl:self-rally"),
            AssaultFire = Trait("front", "asl:assault-fire"),
            Breakdown = Value("front", "breakdown"),
            RateOfFire = Value("front", "rate-of-fire"),
            Repair = Value("malfunctioned", "repair"),
            WoundedFirepower = Value("wounded", "firepower"),
            WoundedRange = Value("wounded", "range"),
            WoundedMorale = Value("wounded", "morale"),
            MovementType = Text("front", "movement-type"),
            MovementPoints = Value("front", "movement-points"),
            Towing = Value("front", "towing"),
            PassengerCapacity = Value("front", "passenger-capacity"),
            QuickSetUp = Trait("front", "asl:qsu"),
            Unarmored = Trait("front", "asl:unarmored"),
            OpenTopped = Trait("front", "asl:open-topped"),
            MainArmament = Text("front", "ma-weapon"),
            MaType = Text("front", "ma-type"),
            AntiAircraftMg = Value("front", "aamg"),
            BowMg = Value("front", "bmg"),
            CoaxialMg = Value("front", "cmg"),
            Caliber = Value("front", "caliber"),
            GroundPressure = Text("front", "ground-pressure"),
            MechanicallyUnreliable = Trait("front", "asl:mechanically-unreliable"),
            LatwType = Text("front", "latw-type"),
            BreakdownRemoves = Trait("front", "asl:breakdown-removes"),
        };
    }
}
