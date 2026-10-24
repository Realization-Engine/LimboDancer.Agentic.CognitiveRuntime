namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// What becomes of a vehicle's inherent crew when an attack immobilizes or destroys it (pass 65, task 65.7; ruling R65.21), one member for every path:
/// the ordnance To Kill DR, a mortar's hit (C1.55), a FT, MOL, or DC, and the IFT Vehicle line. The Immobilization TC (D5.5, p. 204) and its second
/// cause (D8.11, D8.22, pp. 208, 209), Crew Survival (D5.6, D9.2, pp. 204, 209), and the red CS# (D5.7, p. 204; C7.7 note A, p. 176).
/// </summary>
public static class ScenarioA1CrewSurvivalRules
{
    /// <summary>The Original DR the second cause of the Immobilization TC reads (D5.5): "with an Original TK or IFT DR of 5".</summary>
    public const int SecondCauseDr = 5;

    /// <summary>
    /// D5.6 (p. 204): "There is a +1 DRM to the CS DR if the crew had been broken or under the effects of Shock/Stun at the time of the vehicle
    /// elimination." A Recall is treated as a Stun (D5.341), an Unconfirmed Kill is still Shocked (C7.42), and a "+1" counter after a Stun keeps its +1
    /// (D5.34).
    /// </summary>
    public static int SurvivalDrm(bool broken, bool stunned, bool shocked, bool stunRecovery) => broken || stunned || shocked || stunRecovery ? 1 : 0;

    /// <summary>
    /// The CS# a crew rolls against when its vehicle is eliminated without burning (D5.6, D9.2): none for a cs# printed in lower case, which serves only
    /// its PRC, and none for an Abandoned vehicle, which has no crew inside to survive (D5.41). Null when no Crew Survival DR is made.
    /// </summary>
    public static int? CrewSurvival(ArmorDefinition armor, bool abandoned)
    {
        ArgumentNullException.ThrowIfNull(armor);
        return armor.CrewSurvival is { } cs && !armor.CrewSurvivalPassengersOnly && !abandoned ? cs : null;
    }

    /// <summary>The Crew Survival DR (D5.6): a Final DR at most its CS# places a vehicle-crew counter beneath the wreck.</summary>
    public static OrdnanceCrewSurvival Survival(int crewSurvival, IReadOnlyList<int> dice, int drm)
    {
        ArgumentNullException.ThrowIfNull(dice);
        var finalDr = dice[0] + dice[1] + drm;
        return new OrdnanceCrewSurvival([.. dice], drm, finalDr, crewSurvival, finalDr <= crewSurvival);
    }

    /// <summary>
    /// The Immobilization TC (D5.5): the crew's 2d6 plus the "+1" of a Stun's recovery (D5.34) against its Morale Level; failure Abandons the vehicle.
    /// <paramref name="recordedMorale"/> is the Armor Leader's or an Inexperienced Crew's Morale Level when it is the crew's (D3.42, D3.45).
    /// </summary>
    public static FireCheck TaskCheck(IReadOnlyList<int> dice, int morale, bool stunRecovery, int? recordedMorale)
    {
        ArgumentNullException.ThrowIfNull(dice);
        var plusOne = stunRecovery ? 1 : 0;
        IReadOnlyList<FireModifier> drm = plusOne == 0 ? [] : [new FireModifier("stun", 1, "D5.34")];
        var total = dice[0] + dice[1];
        var passed = total + plusOne <= morale;
        return new FireCheck("TC", [.. dice], total, drm, total + plusOne, morale, passed, passed ? "none" : "abandoned")
        {
            MoraleSource = ScenarioA1ArmorLeaders.MoraleSource(recordedMorale),
        };
    }

    /// <summary>
    /// The TC's second cause (D5.5, D8.11, D8.22): a vehicle "already bogged/immobilized and ... hit by Direct Fire ordnance which fails to destroy, Stun,
    /// or Shock it but that would have destroyed or Shocked it with an Original TK ... DR of 5". <paramref name="result"/> is the shot's own To Kill
    /// result, <paramref name="resultOn"/> the same To Kill resolution on another Original DR; only a burn, an elimination, or an automatic Shock counts,
    /// never a possible one. The crew must be inside, neither Stunned nor Shocked (<paramref name="crewMayCheck"/>).
    /// </summary>
    public static bool SecondCause(string result, Func<int, string> resultOn, bool immobile, bool crewMayCheck)
    {
        ArgumentNullException.ThrowIfNull(resultOn);
        return immobile && crewMayCheck && result is OrdnanceKill.None or OrdnanceKill.Immobilized
            && resultOn(SecondCauseDr) is OrdnanceKill.Burn or OrdnanceKill.Eliminated or OrdnanceKill.Shock;
    }

    /// <summary>
    /// D5.7 (p. 204), C7.7 note A (p. 176): an AFV with a red CS# takes "a special -1 DRM ... to the Final To Kill DR for Burning Wreck determination
    /// purposes only". Whether an elimination on <paramref name="finalDr"/> burns instead, by the attack's own burn test <paramref name="burns"/>; false
    /// for a vehicle whose CS# is not red.
    /// </summary>
    public static bool BurnsRed(bool csRed, int finalDr, Func<int, bool> burns)
    {
        ArgumentNullException.ThrowIfNull(burns);
        return csRed && burns(finalDr - 1);
    }
}
