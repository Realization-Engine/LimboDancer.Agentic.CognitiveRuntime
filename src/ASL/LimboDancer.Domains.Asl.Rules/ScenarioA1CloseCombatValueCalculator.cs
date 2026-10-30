namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A11.5 CC vs a Vehicle (p. 74), the Close Combat Value, the one calculator of the rule (the A11.5 claim's design, ruling RA11C.1), with A11.51's leader
/// DRM for a berserk attacker and A11.62's defender's CCV: the base CCV by kind and national case, the attackers' CCV and its modifiers, who is an
/// Assault Engineer, what a Final DR does to the vehicle, who may attack and with whom, whether the PRC die with the vehicle, and the defender's CCV
/// against a vehicle's attack with its floor as a DRM. Facts in, verdicts out; it never rolls.
/// </summary>
public static class ScenarioA1CloseCombatValueCalculator
{
    /// <summary>A11.5 (p. 74): "The CCV of a squad is 5, a Crew is 4, a HS is 3, and a SMC is 2", by the unit's kind; any other kind is a SMC's.</summary>
    public static int BaseCcv(string kind) => kind switch
    {
        "asl:squad" => 5,
        var crew when ScenarioA1Definitions.IsCrew(crew) => 4,
        "asl:half-squad" => 3,
        _ => 2,
    };

    /// <summary>
    /// Ruling RA11C.4: the base CCV with the unit's national case. An Ethiopian MMC (a squad, HS, or crew) one lower (A25.934, p. 98); a T-H Hero, a SMC,
    /// 5 (G1.4231, p. 279); a KW TACP, a crew, 3 (KW 9.11, p. 666). A case given for a kind it does not name changes nothing.
    /// </summary>
    public static int BaseCcv(string kind, NationalCcv national)
    {
        if (!Enum.IsDefined(national))
        {
            throw new ArgumentException("Unknown national CCV case.", nameof(national));
        }

        var printed = BaseCcv(kind);
        var mmc = kind is "asl:squad" or "asl:half-squad" || ScenarioA1Definitions.IsCrew(kind);
        return national switch
        {
            NationalCcv.Ethiopian when mmc => printed - 1,
            NationalCcv.TankHunterHero when !mmc => 5,
            NationalCcv.Tacp when ScenarioA1Definitions.IsCrew(kind) => 3,
            _ => printed,
        };
    }

    /// <summary>
    /// A11.5 (p. 74; rulings RA11C.1, RA11C.2): the attackers' CCV, the vehicle's Basic Kill Number. The main unit's base CCV; +1 for a SMC combining with
    /// it, or failing one for the leader an Original 2 created (A18.12); +1 an Assault Engineer (H1.22, A25.01); -1 Inexperienced (A19.34); and -1 for each
    /// halving FP penalty, once per kind (A7.8: Pin effects are not cumulative): pinned, a concealed vehicle (A11.19), an outnumbered Guard (A20.52),
    /// D7.213's First or Final Fire counter in Reaction Fire. No floor: a CCV of 0 or less leaves the Unlikely Kill (A11.501). The modifiers come in the
    /// order the attack has always listed them, the new ones where an attack without them lists the same.
    /// </summary>
    public static CcvVerdict AttackCcv(CcvAttackFacts facts)
    {
        var ccv = BaseCcv(facts.MainKind, facts.National);
        var modifiers = new List<FireModifier>();
        if (facts.CombiningSmcId is { } smc)
        {
            modifiers.Add(new FireModifier("smc-combining:" + smc, 1m, "A11.5"));
        }
        else if (facts.CreatedLeaderId is { } created)
        {
            modifiers.Add(new FireModifier("created-leader-combining:" + created, 1m, "A18.12"));
        }

        if (facts.AssaultEngineer)
        {
            modifiers.Add(new FireModifier("assault-engineer:" + facts.MainUnitId, 1m, "A11.5"));
        }

        if (facts.Inexperienced)
        {
            modifiers.Add(new FireModifier("inexperienced:" + facts.MainUnitId, -1m, "A11.5"));
        }

        if (facts.Pinned)
        {
            modifiers.Add(new FireModifier("pinned", -1m, "A11.5"));
        }

        if (facts.VehicleConcealed)
        {
            modifiers.Add(new FireModifier("vehicle-concealed", -1m, "A11.19"));
        }

        if (facts.GuardOutnumbered)
        {
            modifiers.Add(new FireModifier("guard-outnumbered", -1m, "A20.52"));
        }

        if (facts.ReactionFireMarked)
        {
            modifiers.Add(new FireModifier("reaction-fire-marked", -1m, "D7.213"));
        }

        return new CcvVerdict(ccv, modifiers, ccv + (int)modifiers.Sum(item => item.Value));
    }

    /// <summary>
    /// Ruling RA11C.1 (A25.01, p. 93; A25.12; H1.22, p. 329): a unit is an Assault Engineer by its DC-symbol counter (the <c>asl:assault-engineer</c>
    /// trait, <see cref="FireDefinition.AssaultEngineer"/>, a HS of such a squad included) or by a DYO or SSR designation (the AVRE's inherent crew among
    /// them), and is none once Replaced by a lower-quality unit, "even if it subsequently Battle Hardens". An unknown trait is not one.
    /// </summary>
    public static bool AssaultEngineer(bool? printedTrait, bool designated, bool replaced) => !replaced && (printedTrait == true || designated);

    /// <summary>
    /// A11.5 (p. 74; the design's review, finding 7): what a Final DR does to the vehicle. Below the CCV eliminates it, as a burning wreck when the DR is at
    /// most half the CCV (D5.7: a red CS# burns on a DR one lower, through <see cref="ScenarioA1CrewSurvivalRules.BurnsRed"/>); equal immobilizes it;
    /// otherwise none. Burning is a kind of elimination, so a DR equal to a CCV of 0 or less immobilizes. The constants of
    /// <see cref="VehicleCloseCombatResolution"/>.
    /// </summary>
    public static string VehicleResult(int finalDr, int ccv, bool csRed)
    {
        if (finalDr < ccv)
        {
            return finalDr * 2 <= ccv || ScenarioA1CrewSurvivalRules.BurnsRed(csRed, finalDr, lower => lower * 2 <= ccv)
                ? VehicleCloseCombatResolution.BurningWreck
                : VehicleCloseCombatResolution.Eliminated;
        }

        return finalDr == ccv ? VehicleCloseCombatResolution.Immobilized : VehicleCloseCombatResolution.None;
    }

    /// <summary>
    /// A11.5 (p. 74): "No more than two units may combine into a single CC attack vs a vehicle and one of the units in any combined attack must be a SMC";
    /// null when allowed.
    /// </summary>
    public static string? CombinationRefusal(int attackers, int smcAmong) =>
        attackers < 1 ? "play.cc-vehicle: an attack on a vehicle needs an attacker (A11.5)"
        : attackers > 2 ? "play.cc-vehicle: no more than two units combine in one CC attack on a vehicle (A11.5)"
        : attackers == 2 && smcAmong < 1 ? "play.cc-vehicle: one of two units combining against a vehicle must be a SMC (A11.5)"
        : null;

    /// <summary>
    /// Who may attack a vehicle in CC (A11.5, p. 74: "Infantry/Cavalry (only)"), checked in this order: Infantry or Cavalry; an enemy of the vehicle; not
    /// broken; not captured; not Unarmed, whose "(1)" FP is usable only in CC vs Personnel (A20.5, p. 87; a T-H Hero, Unarmed for rout and Interdiction,
    /// has a CCV of 5 and is not Unarmed here, G1.4231); not swimming, for swimmers have no CCV (E6.2, p. 235). Null when allowed.
    /// </summary>
    public static string? AttackerRefusal(bool infantryOrCavalry, bool enemyOfVehicle, bool broken, bool captured, bool unarmed, bool swimming) =>
        !infantryOrCavalry ? "play.cc-vehicle: only Infantry or Cavalry attack a vehicle in CC (A11.5)"
        : !enemyOfVehicle ? "play.cc-vehicle: only the vehicle's enemy attacks it in CC (A11.5)"
        : broken ? "play.cc-vehicle: a broken unit makes no CC attack (A11.16)"
        : captured ? "play.cc-vehicle: a captured unit makes no CC attack on a vehicle (A11.5, A20.5)"
        : unarmed ? "play.cc-vehicle: an Unarmed unit's CC FP is usable only vs Personnel, never vs a vehicle (A20.5)"
        : swimming ? "play.cc-vehicle: a swimmer has no CCV and attacks no vehicle (E6.2)"
        : null;

    /// <summary>A11.5 (p. 74): currently supported Infantry kinds, before condition checks. Cavalry awaits its catalog representation.</summary>
    public static bool InfantryOrCavalry(string kind) =>
        kind is "asl:squad" or "asl:half-squad" or "asl:leader" or "asl:hero" || ScenarioA1Definitions.IsCrew(kind);

    /// <summary>
    /// A11.5 (p. 74): "A single CC attack cannot affect both a vehicle and Personnel (except for the PRC of a vehicle, which are eliminated if the vehicle is
    /// destroyed)": true when the result eliminates the vehicle, burning or not; not when it is captured or immobilized.
    /// </summary>
    public static bool PrcEliminated(string vehicleResult) =>
        vehicleResult is VehicleCloseCombatResolution.Eliminated or VehicleCloseCombatResolution.BurningWreck;

    /// <summary>
    /// A11.51 (p. 74): "A leadership DRM applies to CC vs a vehicle only when that leader is combining with another unit in that attack", and A15.42
    /// (p. 84): "a berserk unit never receives the leadership benefit of a friendly leader" (A11.141), so not when the unit he combines with is berserk.
    /// </summary>
    public static bool LeaderDrmApplies(bool leaderCombining, bool attackerBerserk) => leaderCombining && !attackerBerserk;

    /// <summary>
    /// A11.62 (p. 76; ruling RA11C.3): one MMC defender's CCV before the floor: its base CCV with its national case, +1 an Assault Engineer, -1
    /// Inexperienced, -1 pinned (the p. 75 example: "3 CCV -1 pinned").
    /// </summary>
    public static int DefenderCcv(CcvDefenderFacts defender) =>
        BaseCcv(defender.Kind, defender.National) + (defender.AssaultEngineer ? 1 : 0) - (defender.Inexperienced ? 1 : 0) - (defender.Pinned ? 1 : 0);

    /// <summary>
    /// A11.62 (p. 76): the defenders' CCV against a vehicle's attack. Each MMC's <see cref="DefenderCcv"/>, at least 1 ("A defender's CCV cannot be reduced
    /// below 1"); a SMC adds one when it defends with a MMC (A11.5), and defends alone with its own CCV (2, or a T-H Hero's 5). So any defender gives
    /// at least 1; no defender gives 0.
    /// </summary>
    public static int DefenseCcv(IReadOnlyList<CcvDefenderFacts> defenders)
    {
        ArgumentNullException.ThrowIfNull(defenders);
        var total = 0;
        var mmc = 0;
        foreach (var defender in defenders)
        {
            if (!defender.Smc)
            {
                total += Math.Max(1, DefenderCcv(defender));
                mmc++;
            }
        }

        foreach (var defender in defenders)
        {
            if (defender.Smc)
            {
                total += mmc > 0 ? 1 : BaseCcv(defender.Kind, defender.National);
            }
        }

        return total;
    }

    /// <summary>
    /// A11.62 (p. 76; ruling RA11C.1): "treat any condition that would [reduce a defender's CCV below 1] as a CC DRM instead": the DRM on the vehicle's
    /// attack against this MMC defender, the amount its CCV would fall below 1, as a negative DRM; 0 for a SMC or a CCV of 1 or more.
    /// </summary>
    public static int DefenseCcvDrm(CcvDefenderFacts defender) => defender.Smc ? 0 : Math.Min(0, DefenderCcv(defender) - 1);
}
