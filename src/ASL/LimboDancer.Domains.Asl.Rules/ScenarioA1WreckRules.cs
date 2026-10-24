namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The facts of a Scrounging attempt (D10.5; pass 65, ruling R65.23): the phase, the unit's state, and the vehicle's: a wreck and whether it burns, or an
/// Abandoned vehicle, and its Scrounged and Disabled counters.
/// </summary>
public sealed record ScroungeFacts(string Unit, string Vehicle, bool OwnRph, bool Mmc, bool Unbroken, bool Berserk, bool Melee, bool Ti, bool InLocation, bool Wreck,
    bool Burning, bool Abandoned, bool Scrounged, bool Disabled);

/// <summary>
/// Wrecks attacked as their vehicle, and Scrounging (D10.1, D10.5 to D10.52, pp. 210, 211; pass 65, ruling R65.23). A non-burning wreck is attacked as the
/// vehicle it was, by ordnance on the Vehicle Target Type, the IFT's Vehicle line, and a FT or MOL, and only a Burn does anything: it places a Blaze. An
/// Infantry MMC in its own RPh salvages a functioning SW from a non-burning wreck or an Abandoned vehicle on a dr of 1 to 3, the dr also being how many it
/// takes, by Random Selection. Play reads the state and writes the events. CC at a wreck is pass 70's; rearming unarmed units with scrounged small arms
/// (A20.552) is pass 75's.
/// </summary>
public static class ScenarioA1WreckRules
{
    /// <summary>The purpose of a Scrounging dr, and of its Random Selection.</summary>
    public const string Purpose = "scrounge";

    /// <summary>
    /// B25.14 (backlog pass 6): the id of the Blaze on a burning wreck, its vehicle's id with <c>-blaze</c>. The one name the planner writes and the
    /// projector reads (pass 65, the boundary review).
    /// </summary>
    public static string BlazeId(string vehicle) => vehicle + "-blaze";

    /// <summary>D10.51: the weapons a vehicle's Scrounging may salvage, by the mount they come from.</summary>
    public const string BowMg = "bmg";
    public const string MainArmament = "ma";
    public const string AntiAircraftMg = "aamg";

    /// <summary>
    /// D10.1 (p. 210): "A non-burning wreck may be attacked by either side, treating the wreck as if it were still the original vehicle". What a To Kill
    /// result does to a wreck: a Burn burns it (a Blaze is placed on it, B25.14, C7.6); any other result does nothing, and a wreck has no crew to check,
    /// survive, or take a Collateral Attack, and no PRC.
    /// </summary>
    public static OrdnanceKill WreckKill(OrdnanceKill kill)
    {
        ArgumentNullException.ThrowIfNull(kill);
        return kill with
        {
            Result = kill.Result == OrdnanceKill.Burn ? OrdnanceKill.Burn : OrdnanceKill.None,
            ShockCheck = null,
            Shocked = null,
            CrewCheck = null,
            SecondCause = null,
            Abandoned = null,
            CrewSurvival = null,
            PrcSurvival = null,
        };
    }

    /// <summary>
    /// D10.1: "for CC purposes it does not necessitate PAATC, cannot be Ambushed, and is considered CE and Abandoned". CC at a wreck is not built (pass
    /// 70's); the refusal says what may be done instead.
    /// </summary>
    public static string CloseCombatBar(string wreck) =>
        $"play.cc-vehicle: CC at the wreck {wreck} is not built yet (D10.1, pass 70); fire at it as its vehicle instead, by ordnance, the IFT, a FT, or a MOL";

    /// <summary>D10.1: the IFT's Vehicle line at a wreck: a burning wreck burns it; any other result does nothing.</summary>
    public static string WreckLineResult(string result) => result == FireVehicleEffect.BurningWreck ? FireVehicleEffect.BurningWreck : FireVehicleEffect.None;

    /// <summary>
    /// D10.5 (p. 211): "Any non-berserk, unbroken Infantry MMC not in Melee may attempt to salvage a functioning SW from a non-burning wreck or abandoned
    /// vehicle, provided that vehicle/wreck has not already been scrounged or marked with a Disabled counter (6.631) ... A unit in the same hex with a
    /// vehicle/wreck during its own RPh may attempt to scrounge". Why the unit may not scrounge the vehicle now, or null. A TI unit has already acted for
    /// the Player Turn. A Disabled counter is either mount's: D6.631 marks the halftrack whose MA or BMG a crew Removed.
    /// </summary>
    public static string? ScroungeBar(ScroungeFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var (unit, vehicle) = (facts.Unit, facts.Vehicle);
        return !facts.OwnRph ? $"play.scrounge: {unit} scrounges only in its own RPh (D10.5)"
            : !facts.Mmc ? $"play.scrounge: {unit} is not an Infantry MMC; a squad, HS, or crew scrounges (D10.5)"
            : !facts.Unbroken || facts.Berserk || facts.Melee || facts.Ti ? $"play.scrounge: {unit} scrounges only unbroken, not berserk, not in Melee, and not TI (D10.5)"
            : !(facts.Wreck ? !facts.Burning : facts.Abandoned) ? $"play.scrounge: {vehicle} is neither a non-burning wreck nor an Abandoned vehicle, which alone are scrounged (D10.5)"
            : !facts.InLocation ? $"play.scrounge: {unit} scrounges {vehicle} only from its Location (D10.5)"
            : facts.Scrounged ? $"play.scrounge: {vehicle} has been scrounged already; a wreck or vehicle is scrounged once (D10.5)"
            : facts.Disabled ? $"play.scrounge: {vehicle} is marked Disabled, so nothing on it is scrounged (D10.5, D6.631)"
            : null;
    }

    /// <summary>
    /// D10.51 (p. 211): "Only BMG/AAMG/SW carried by the vehicle as per Vehicle Listing Notes may be recovered by scrounging. Fixed-Mount (1.81) BMG,
    /// ordnance, CMG, smoke dispensers, FT, and MA other than mortars cannot be scrounged". The mounts a Scrounging may salvage, in order: a BMG not fixed and
    /// not disabled, the MA when it is an AAMG or a mortar, and an AAMG that is not the MA; each functioning (D10.5), so not malfunctioned. An AAMG that is
    /// not the MA shares the MA's malfunction in the state. The SW of the Vehicle Listing Notes are not in the catalog.
    /// </summary>
    public static IReadOnlyList<string> Scroungeable(string? mainArmament, int? antiAircraftMg, int? bowMg, bool fixedBmg, bool maMalfunctioned, bool bmgMalfunctioned,
        bool bmgDisabled)
    {
        var mounts = new List<string>();
        if (bowMg is not null && !fixedBmg && !bmgMalfunctioned && !bmgDisabled)
        {
            mounts.Add(BowMg);
        }

        if (mainArmament is "aamg" or "mortar" && !maMalfunctioned)
        {
            mounts.Add(MainArmament);
        }
        else if (antiAircraftMg is not null && !maMalfunctioned)
        {
            mounts.Add(AntiAircraftMg);
        }

        return mounts;
    }

    /// <summary>
    /// D10.5: "To scrounge anything of value, a 1-3 dr (∆) is necessary." A.10 (p. 43): the ∆ marks a dr that leadership does not modify, so no leader's
    /// modifier is added.
    /// </summary>
    public static bool Succeeds(int dr) => dr is >= 1 and <= 3;

    /// <summary>D10.5: "The dr also doubles as the number of SW that may be scrounged (selected randomly) if more than one exists": a Random Selection is made when a success finds more SW than the dr.</summary>
    public static bool SelectionNeeded(int dr, int count) => Succeeds(dr) && count > dr;

    /// <summary>
    /// D10.5, A.9: the mounts a Scrounging takes: none on a dr above 3; every one when they are no more than the dr; else the dr's number by Random
    /// Selection, one die each, the highest first, a tie taken in the mounts' order. Null when a Random Selection is needed and not given.
    /// </summary>
    public static IReadOnlyList<string>? Taken(IReadOnlyList<string> mounts, int dr, IReadOnlyList<int>? selection)
    {
        ArgumentNullException.ThrowIfNull(mounts);
        if (!Succeeds(dr))
        {
            return [];
        }

        if (!SelectionNeeded(dr, mounts.Count))
        {
            return mounts;
        }

        return selection is { } dice && dice.Count == mounts.Count
            ? [.. mounts.Select((mount, index) => (Mount: mount, Index: index, Die: dice[index])).OrderByDescending(item => item.Die).ThenBy(item => item.Index).Take(dr)
                .OrderBy(item => item.Index).Select(item => item.Mount)]
            : null;
    }

    /// <summary>
    /// D10.5, D10.52: "a normal SW ... of that wreck/vehicle's nationality"; "U.S. LMG are represented by British LMG counters". The nationality whose
    /// counter a scrounged weapon takes. The captured-use penalties the American user is spared are not built.
    /// </summary>
    public static string? ScroungedNationality(string? vehicleNationality) => vehicleNationality == "american" ? "british" : vehicleNationality;

    /// <summary>
    /// D10.52 (p. 211): "A scrounged MG is represented by a LMG counter of that wreck/vehicle's nationality, regardless of its FP while mounted"; a mortar
    /// MA is the nationality's light mortar. The definition a scrounged mount becomes, the first by id; null when the catalog has none.
    /// </summary>
    public static string? ScroungedDefinition(IEnumerable<FireDefinition> definitions, string? nationality, string mount, string? mainArmament)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        return mount == MainArmament && mainArmament == "mortar"
            ? definitions.Where(item => item.Kind == "asl:light-mortar" && item.Nationality == nationality).OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault()?.Id
            : ScenarioA1FireEligibility.LmgDefinition(definitions, nationality);
    }

    /// <summary>
    /// D10.52: "A MG/mortar is always scrounged in its dm form if possible." Only a weapon with a dm form is (A9.8, p. 65): a LMG has none, so it is
    /// scrounged whole.
    /// </summary>
    public static bool ScroungedDismantled(string? kind, string? nationality, string? size, int? portage) => ScenarioA1FireEligibility.Dismantlable(kind, nationality, size, portage);

    /// <summary>
    /// D10.5: "Regardless of the outcome, place a Scrounged counter on the wreck/abandoned vehicle to signify that it cannot be scrounged again and all its
    /// scroungeable weapons are Disabled." The vehicle's conditions for the mounts it may no longer fire: the BMG's and the MA's Disabled counters; an
    /// AAMG that is not the MA has no condition of its own.
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> ScroungedConditions(IReadOnlyList<string> mounts)
    {
        ArgumentNullException.ThrowIfNull(mounts);
        return [.. mounts.Where(mount => mount is BowMg or MainArmament).Select(mount => (mount == BowMg ? UnitCondition.BmgDisabled : UnitCondition.Disabled, true))];
    }

    /// <summary>The proposal of a Scrounging (D10.5): the dr it needs, what it may salvage, and what it costs whatever the dr.</summary>
    public static string ScroungeText(string unit, string vehicle, string at, IReadOnlyList<string> mounts) =>
        $"play.scrounge: {unit} scrounges {vehicle} in {at}: a dr of 1 to 3 salvages that many of "
        + (mounts.Count == 0 ? "its SW, of which it has none that may be scrounged" : $"its {string.Join(" and ", mounts.Select(MountText))}") + " (D10.5, D10.51)"
        + $"; whatever the dr, {vehicle} is marked Scrounged, and {unit} and what it salvages are TI for the rest of the Player Turn (D10.5)";

    /// <summary>A scrounged mount in a text.</summary>
    public static string MountText(string mount) => mount switch
    {
        BowMg => "BMG",
        MainArmament => "MA",
        _ => "AAMG",
    };
}
