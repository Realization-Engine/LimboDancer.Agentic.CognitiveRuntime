namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A21.11, A21.12, and A21.13 (p. 88), the one calculator of captured and Non-Qualified Use of a crew-served ordnance weapon (the A21.13 claim's
/// design, ruling RA21.1): who may fire it, the penalty multiple, and the arithmetic of that multiple (A.11 p. 43; C5.8 p. 173; A15.23 p. 83). The
/// light mortar exception is <see cref="ScenarioA1MortarCalculator"/>'s, asked here. A vehicle's manning (A21.22) stays
/// <see cref="ScenarioA1CapturedVehicleRules.WeaponPenalty"/>'s; its multiple takes this arithmetic. Facts in, verdicts out.
/// </summary>
public static class ScenarioA1NonQualifiedUseCalculator
{
    /// <summary>A15.23: a hero fires a Gun over 82mm only once per Game Turn; at 82mm or less he has no such limit.</summary>
    public const int HeroGunCaliber = 82;

    private static readonly string[] HeroGunPhaseList = ["MPh", "DFPh"];

    /// <summary>A15.23 (p. 83): the phases in which a hero fires a Gun over 82mm.</summary>
    public static IReadOnlyList<string> HeroGunPhases => HeroGunPhaseList;

    /// <summary>Ruling RA21.2 (C2.1 p. 167, D5.1 p. 203): a crew counter, an infantry crew or a vehicle crew, is qualified to man a Gun.</summary>
    public static bool Qualified(string? kind) => ScenarioA1Definitions.IsCrew(kind);

    /// <summary>
    /// Ruling RA21.2: who fires the weapon, from the firer's kind and the number of SMC firing it together (the firer included): a crew, a squad or
    /// HS, two or more SMC, or a lone SMC. Null for a kind that fires no crew-served weapon (a vehicle).
    /// </summary>
    public static CrewServedUser? User(string? kind, int smcFiring) =>
        Qualified(kind) ? CrewServedUser.Crew
        : kind is "asl:squad" or "asl:half-squad" ? CrewServedUser.Mmc
        : kind is "asl:leader" or "asl:hero" ? smcFiring >= 2 ? CrewServedUser.SmcGroup : CrewServedUser.LoneSmc
        : null;

    /// <summary>
    /// C5.8 (p. 173), A21.13: the multiple of the A21.11 and A21.12 penalties, one for a captured weapon and one for a non-qualified user, so "doubled"
    /// when both apply (Case H "+4 if both apply").
    /// </summary>
    public static int Multiple(bool captured, bool nonQualified) => (captured ? 1 : 0) + (nonQualified ? 1 : 0);

    /// <summary>
    /// Rulings RA21.2 and RA21.3 (A21.13, A15.23): a qualified crew takes the penalties only on a captured weapon; a squad, HS, or two or more SMC
    /// takes them on its own side's weapon and doubled on a captured one; a lone hero takes them once, captured or not ("as if it were captured and
    /// being fired by an enemy crew"). A lone SMC that is not a hero fires only a mortar as a light mortar, which takes no A21.13 penalty, so only its
    /// capture's.
    /// </summary>
    public static int PenaltyMultiple(CrewServedUser user, bool captured, bool hero) => user switch
    {
        CrewServedUser.Crew => Multiple(captured, nonQualified: false),
        CrewServedUser.Mmc or CrewServedUser.SmcGroup => Multiple(captured, nonQualified: true),
        _ => hero ? 1 : Multiple(captured, nonQualified: false),
    };

    /// <summary>A21.11 (p. 88), A21.13: the B# (and a X#) is two lower for each multiple.</summary>
    public static int BreakdownLowering(int multiple) => 2 * multiple;

    /// <summary>
    /// A.11 (p. 43: "Captured Weapons, unqualified crews [21.13]"): a use that lowers the B# makes the Original B# the weapon's X# for that use, so an
    /// Original DR at or above it removes the weapon. Null when no penalty applies.
    /// </summary>
    public static int? XNumber(int originalBreakdown, int multiple) => multiple > 0 ? originalBreakdown : null;

    /// <summary>A21.12 (p. 88; the C3 chart's "Lowered ROF"): the Multiple ROF is one lower for each multiple.</summary>
    public static int RofLowering(int multiple) => multiple;

    /// <summary>A21.12: the Multiple ROF after the lowering, never below 0.</summary>
    public static int MultipleRof(int printed, int multiple) => Math.Max(0, printed - RofLowering(multiple));

    /// <summary>A21.12, C5.8: any penalty uses the red To Hit Numbers; a color is not doubled.</summary>
    public static string ToHitColor(string color, int multiple) => multiple > 0 ? "red" : color;

    /// <summary>C5.8 (p. 173): Case H, +2 To Hit DRM for each multiple ("+4 if both apply").</summary>
    public static int CaseHDrm(int multiple) => 2 * multiple;

    /// <summary>The refusal of a use (ruling RA21.2), null when allowed: <see cref="Use"/>'s.</summary>
    public static string? UseRefusal(NonQualifiedUseFacts facts) => Use(facts).Refusal;

    /// <summary>
    /// The verdict of one use (rulings RA21.2, RA21.3, RA21.9), its first refusal in this order: who fires it, whether it is captured, whether it is a
    /// mortar and its caliber; for a lone SMC, the light mortar exception (C9.2, asked of <see cref="ScenarioA1MortarCalculator.FiresAsLight"/>), then
    /// the hero (A15.23), and for a hero with a Gun over 82mm the phase and once per Game Turn. A light mortar needs no crew (C9.2): any user fires it
    /// as one, with its capture's penalty only.
    /// </summary>
    public static NonQualifiedUseVerdict Use(NonQualifiedUseFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.User is not { } user)
        {
            return Refused("Who fires the crew-served weapon is not known (A21.13).", "A21.13");
        }

        if (facts.Captured is not { } captured)
        {
            return Refused("Whether the weapon is captured is not known (A21.11).", "A21.11");
        }

        if (facts.Mortar is not { } mortar)
        {
            return Refused("Whether the weapon is a mortar is not known (C9.2).", "C9.2");
        }

        if (mortar && facts.Caliber is null)
        {
            return Refused("The mortar's caliber is not known (C9.2).", "C9.2");
        }

        var lone = user == CrewServedUser.LoneSmc;
        if (mortar && (ScenarioA1MortarCalculator.IsLight(facts.Caliber) || (lone && ScenarioA1MortarCalculator.FiresAsLight(facts.Caliber, loneSmc: true))))
        {
            return new NonQualifiedUseVerdict(null, null, Multiple(captured, nonQualified: false), true, true, false);
        }

        if (!lone)
        {
            return new NonQualifiedUseVerdict(null, null, PenaltyMultiple(user, captured, hero: false), false, true, false);
        }

        if (facts.Hero is not { } hero)
        {
            return Refused("Whether the lone SMC is a hero is not known (A15.23).", "A15.23");
        }

        if (!hero)
        {
            return mortar
                ? Refused(ScenarioA1MortarCalculator.LoneSmcRefusal(facts.Caliber, loneSmc: true)!, "A21.13")
                : Refused("A lone SMC fires a crew-served weapon only if heroic (A21.13, A15.23).", "A21.13");
        }

        if (facts.Caliber is not { } caliber)
        {
            return Refused("The Gun's caliber is not known (A15.23).", "A15.23");
        }

        var limited = caliber > HeroGunCaliber;
        if (limited)
        {
            if (facts.Phase is not { } phase)
            {
                return Refused("The phase of the shot is not known (A15.23).", "A15.23");
            }

            if (!HeroGunPhaseList.Contains(phase, StringComparer.Ordinal))
            {
                return Refused("A hero fires a Gun over 82mm only in the MPh or DFPh (A15.23).", "A15.23");
            }

            if (facts.FiredThisGameTurn is not { } fired)
            {
                return Refused("Whether the Gun has fired this Game Turn is not known (A15.23).", "A15.23");
            }

            if (fired)
            {
                return Refused("A hero fires a Gun over 82mm only once per Game Turn (A15.23).", "A15.23");
            }
        }

        return new NonQualifiedUseVerdict(null, null, PenaltyMultiple(user, captured, hero: true), false, false, limited);
    }

    private static NonQualifiedUseVerdict Refused(string text, string rule) => new(text, rule, 0, false, true, false);
}
