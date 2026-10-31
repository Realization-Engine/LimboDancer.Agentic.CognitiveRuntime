using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The vehicle of a CC attack (backlog pass 11, rulings R11.13 to R11.15), as the game reads it: its definition, its side, and the states that
/// modify CC against it or by it. Nullable members are required.
/// </summary>
public sealed record VehicleCloseCombatVehicle(
    string? VehicleId,
    string? DefinitionId,
    string? Side,
    bool? CrewExposed,
    bool? InMotion,
    bool? Immobile,
    bool? Stunned,
    bool? Shocked,
    bool? Abandoned,
    bool? MainArmamentMalfunctioned,
    bool? BmgMalfunctioned,
    bool? CmgMalfunctioned)
{
    /// <summary>D3.7: actual malfunctioned or Disabled weapons; null preserves the recorded availability reading.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? UnavailableWeapons
    {
        get; init;
    }

    /// <summary>Whether the vehicle is under a Stun "+1" counter (D5.34, p. 203; pass 35, task 35.13 d): one more on its CC DR; null is false.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? StunRecovery
    {
        get; init;
    }

    /// <summary>
    /// The leadership modifier of the vehicle's Armor Leader on the DR of its own CC attack (D3.44, A11.62; pass 55, ruling R55.16), or the +1
    /// of an Inexperienced Crew (D3.45); null is none, and it is never given for an attack made against the vehicle.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? ArmorLeadership
    {
        get; init;
    }
}

/// <summary>The rolls of one CC attack with a vehicle: its DR, the Unlikely Kill dr after an Original 2, and the Random Selection and Wound Severity drs of its effects.</summary>
public sealed record VehicleCloseCombatRolls(IReadOnlyList<int>? Attack)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? UnlikelyKill
    {
        get; init;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? RandomSelection
    {
        get; init;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? WoundSeverity
    {
        get; init;
    }

    /// <summary>The Leader Creation dr after a MMC's Original 2 (A18.12; pass 70, ruling R70.34); null when none was rolled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LeaderCreation
    {
        get; init;
    }
}

/// <summary>
/// One CC attack in a Location holding a vehicle (A11.5, A11.62; rulings R11.13 to R11.15): Infantry attacking the vehicle (one unit, or one with a
/// SMC), or the vehicle attacking Infantry; in the CCPh, or as CC Reaction Fire in the MPh (D7.21). <see cref="Units"/> are the Infantry of both sides
/// in the Location.
/// </summary>
public sealed record VehicleCloseCombatFacts(
    string? Phase,
    string? LocationId,
    VehicleCloseCombatVehicle? Vehicle,
    IReadOnlyList<CloseCombatUnit>? Units,
    IReadOnlyList<string>? Attackers,
    IReadOnlyList<string>? Defenders,
    bool? ByVehicle,
    bool? Reaction,
    VehicleCloseCombatRolls? Rolls)
{
    /// <summary>D3.7: actual firing armaments' breakdown evidence; null retains the reading before vehicle CC breakdown was wired.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireVehicleBreakdownFacts? VehicleBreakdown
    {
        get; init;
    }

    /// <summary>The attackers marked First or Final Fire, whose CC Reaction Fire CCV is one lower (D7.213); null for none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? FireMarked
    {
        get; init;
    }

    /// <summary>
    /// A21.2 (p. 88; pass 65, ruling R65.22): the attack is a capture attempt on an Abandoned AFV, which captures it where it would eliminate it; null is an
    /// attack to eliminate. A declaration, taken from the record.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Capture
    {
        get; init;
    }

    /// <summary>
    /// A21.2: the attack on an Abandoned AFV was planned since pass 65, so the AFV "is assumed to be CE and Immobile as long as it is unmanned"; null for
    /// every record made before, which resolves as it did.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Unmanned
    {
        get; init;
    }

    /// <summary>
    /// The side that ambushed in the Location this CCPh (A11.4, A11.51, the I5 example, pp. 73 to 75; pass 70, ruling R70.30): its attacks take -1 and attacks
    /// against it +1, in CC with a vehicle both ways. Null where no Ambush occurred, as in every record made before the pass.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ambusher
    {
        get; init;
    }

    /// <summary>
    /// The Passengers who join a vehicle's own attack on Infantry (A11.62, p. 76; pass 70, ruling R70.31): a halftrack's while CE, an unarmored vehicle's
    /// always. Null when none join, and in every record made before the pass, whose attack used the vehicle's armament alone.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<CloseCombatUnit>? Passengers
    {
        get; init;
    }

    /// <summary>
    /// The Infantry of the Location that qualify for Street Fighting (A11.8, p. 76; pass 70, ruling R70.33): each advanced this APh into the vehicle's road
    /// Location from the ground level of a building hex ADJACENT to it on either side of the road. Their attack on the vehicle takes -1, the vehicle's on them
    /// +1, unless an actual Ambush occurred. The planner's map read, taken as recorded; null for none, as in every record made before the pass.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? StreetFighters
    {
        get; init;
    }

    /// <summary>
    /// A18.12 (p. 85; ruling R70.34): the attack was planned since the pass, so a MMC's Original 2 draws a Leader Creation dr. A declaration of the record,
    /// taken from it; null for every record made before, whose Original 2 created no leader.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LeaderCreation
    {
        get; init;
    }

    /// <summary>
    /// A18.2 (p. 85; pass 71, ruling R71.17): the vehicle attacked is an AFV, so a Leader Creation dr takes the table's "-1 CC vs AFV". A declaration of the
    /// record written beside <see cref="LeaderCreation"/>, taken from it; null for an unarmored vehicle and for every record made before the pass, whose
    /// Leader Creation dr took no such drm.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AfvDrm
    {
        get; init;
    }

    /// <summary>
    /// A11.52 (p. 74; ruling R70.34): the defenders of the vehicle's attack that attempted this CCPh to capture its Passengers, who defend against the
    /// Passengers' attack with +2. Null for none, as before the pass, when no attack could capture a Passenger.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Captors
    {
        get; init;
    }
}

/// <summary>The answer to one CC attack with a vehicle: its arithmetic, what it did to the vehicle, and the effects on Infantry.</summary>
public sealed record VehicleCloseCombatResolution(
    string Disposition,
    IReadOnlyList<string> Reasons,
    int? KillNumber,
    IReadOnlyList<FireModifier> KillNumberModifiers,
    IReadOnlyList<FireModifier> Drm,
    IReadOnlyList<int> Dice,
    int OriginalDr,
    int FinalDr,
    string VehicleResult,
    IReadOnlyList<CloseCombatUnitEffect> Effects)
{
    public const string None = "none";
    /// <summary>D3.7/A9.71: each participating armament's outcome, absent in older records.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, VehicleBreakdownResolved>? VehicleBreakdowns
    {
        get; init;
    }

    public const string Immobilized = "immobilized";
    public const string Eliminated = "eliminated";
    public const string BurningWreck = "burning-wreck";

    /// <summary>A21.2 (pass 65, ruling R65.22): a capture attempt's success, where an attack to eliminate would have eliminated or burned the AFV.</summary>
    public const string Captured = "captured";

    /// <summary>The Unlikely Kill dr after an Original 2 (A11.501); null when none was rolled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? UnlikelyKillDr
    {
        get; init;
    }

    /// <summary>The Leader Creation after a MMC's Original 2 (A18.12; pass 70, ruling R70.34), with the leader created if any; null when none was rolled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LeaderCreationOutcome? LeaderCreation
    {
        get; init;
    }

    /// <summary>A vehicle's attack: its FP, the defenders' CCV, the odds, and each defender's Final DR and result (A11.62).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? AttackFirepower
    {
        get; init;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? DefenseValue
    {
        get; init;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Odds
    {
        get; init;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<CloseCombatDefenderResult>? Defending
    {
        get; init;
    }
}

/// <summary>
/// CC with a vehicle (backlog pass 11): Infantry against a vehicle, with the CCV as the Kill Number (A11.5, A11.501, A11.51, A11.61, A11.621;
/// ruling R11.14), and a vehicle against Infantry on the CCT (A11.62; ruling R11.15). It never rolls or changes a game.
/// </summary>
public static class ScenarioA1VehicleCloseCombat
{
    /// <summary>
    /// The attack as every recorded game has read it: <see cref="Resolve(VehicleCloseCombatFacts, ScenarioA1CloseCombatReference, CcvContext?)"/> with no
    /// context, the same answer. It keeps its answer where A11.5 differs (the A11.5 claim's design, ruling RA11C.6; W-A11.5): no Assault Engineer +1, no
    /// -1 for a concealed vehicle or an outnumbered Guard, no national CCV, an Unarmed or swimming attacker admitted, a combining leader's DRM for a berserk
    /// attacker, a Final DR equal to a CCV of 0 or less a burning wreck, and a defender's CCV floored at 1 with no DRM for the excess.
    /// </summary>
    public static VehicleCloseCombatResolution Resolve(VehicleCloseCombatFacts facts, ScenarioA1CloseCombatReference reference) => Resolve(facts, reference, null);

    /// <summary>
    /// A11.5, A11.51, A11.62 (the A11.5 claim's design, ruling RA11C.5): the attack with the facts no recorded record holds. With a context, even an empty
    /// one, the rule's reading through <see cref="ScenarioA1CloseCombatValueCalculator"/>: <see cref="ScenarioA1CloseCombatValueCalculator.AttackerRefusal"/>
    /// refuses an Unarmed or swimming attacker after <see cref="Precheck"/>; the CCV takes the Assault Engineer, the concealed vehicle, the outnumbered Guard,
    /// and the national case; <see cref="ScenarioA1CloseCombatValueCalculator.LeaderDrmApplies"/> drops a combining leader's DRM for a berserk attacker;
    /// <see cref="ScenarioA1CloseCombatValueCalculator.VehicleResult"/> gives the result; and a vehicle's attack takes the defenders' Assault Engineer +1 and
    /// <see cref="ScenarioA1CloseCombatValueCalculator.DefenseCcvDrm"/>. With null, the recorded reading.
    /// </summary>
    public static VehicleCloseCombatResolution Resolve(VehicleCloseCombatFacts facts, ScenarioA1CloseCombatReference reference, CcvContext? context)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(reference);
        var reasons = Precheck(facts, reference, context);
        if (reasons.Count != 0)
        {
            return Refused(CloseCombatResolution.Abstained, reasons);
        }

        if (context is not null && facts.ByVehicle != true && AttackersRefusal(facts, reference, context) is { } refusal)
        {
            return Refused(CloseCombatResolution.Abstained, [refusal]);
        }

        if (facts.Rolls?.Attack is not [>= 1 and <= 6, >= 1 and <= 6] dice)
        {
            return Refused(CloseCombatResolution.Indeterminate, ["asl.a1.cc-vehicle.roll-missing:attack"]);
        }

        if (facts.ByVehicle != true)
        {
            return AgainstVehicle(facts, reference, dice, context);
        }

        Dictionary<string, VehicleBreakdownResolved>? breakdowns = null;
        if (facts.VehicleBreakdown is { } breakdown)
        {
            breakdowns = ScenarioA1VehicleBreakdownResolutionCalculator.ResolveWeapons(facts.Vehicle!.VehicleId!, breakdown.Weapons,
                VehicleBreakdownUse.CloseCombat, dice[0] + dice[1], facts.Rolls.RandomSelection, out var keys);
            if (breakdowns is null)
            {
                return Refused(CloseCombatResolution.Indeterminate, ["asl.a1.cc-vehicle.roll-missing:randomSelection:" + string.Join(",", keys)]);
            }
        }
        return ByVehicle(facts, reference, dice, context) with
        {
            VehicleBreakdowns = breakdowns
        };
    }

    /// <summary>
    /// A11.5, A20.5, E6.2: the first attacker <see cref="ScenarioA1CloseCombatValueCalculator.AttackerRefusal"/> refuses, after <see cref="Precheck"/> has
    /// admitted each as an enemy, unbroken, and uncaptured; null when none.
    /// </summary>
    private static string? AttackersRefusal(VehicleCloseCombatFacts facts, ScenarioA1CloseCombatReference reference, CcvContext context)
    {
        foreach (var id in facts.Attackers!)
        {
            var unit = facts.Units!.First(item => item.UnitId == id);
            var infantryOrCavalry = ScenarioA1CloseCombatValueCalculator.InfantryOrCavalry(reference.Definitions[unit.DefinitionId!].Kind);
            if (ScenarioA1CloseCombatValueCalculator.AttackerRefusal(infantryOrCavalry, unit.Side != facts.Vehicle!.Side, unit.Broken == true, unit.Captured == true,
                unit.Unarmed == true, context.IsSwimming(id)) is { } refusal)
            {
                return refusal;
            }
        }

        return null;
    }

    /// <summary>
    /// A18.2 (p. 85; pass 71, ruling R71.17, R5's finding 10): the <see cref="VehicleCloseCombatFacts.AfvDrm"/> an attack's record declares: true with a
    /// Leader Creation against an AFV, a vehicle the catalog does not print unarmored; null otherwise, as before the pass.
    /// </summary>
    public static bool? AfvDrmDeclared(bool leaderCreation, bool? unarmored) => leaderCreation && Afv(unarmored) ? true : null;

    /// <summary>D1.1: a vehicle is an AFV unless the catalog prints it unarmored.</summary>
    private static bool Afv(bool? unarmored) => unarmored != true;

    /// <summary>
    /// Why the attack is outside the review; empty when it is admitted. It keeps its answer (the A11.5 claim's design, ruling RA11C.6): it admits an
    /// Unarmed attacker (A20.5) and knows no swimmer (E6.2), which <see cref="ScenarioA1CloseCombatValueCalculator.AttackerRefusal"/> refuses. Its own
    /// check of the number of attackers gives the answer <see cref="ScenarioA1CloseCombatValueCalculator.CombinationRefusal"/> gives, with its own texts.
    /// </summary>
    public static IReadOnlyList<string> Precheck(VehicleCloseCombatFacts facts, ScenarioA1CloseCombatReference reference)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(reference);
        var reasons = new List<string>();
        var vehicle = facts.Vehicle;
        if (facts.Phase is not ("CCPh" or "MPh") || facts.LocationId is null || facts.Units is null || facts.ByVehicle is null || facts.Reaction is null
            || vehicle is null || vehicle.VehicleId is null || vehicle.DefinitionId is null || vehicle.Side is null || vehicle.CrewExposed is null
            || vehicle.InMotion is null || vehicle.Immobile is null || vehicle.Stunned is null || vehicle.Shocked is null || vehicle.Abandoned is null
            || vehicle.MainArmamentMalfunctioned is null || vehicle.BmgMalfunctioned is null || vehicle.CmgMalfunctioned is null)
        {
            return ["asl.a1.cc-vehicle.fact-missing"];
        }

        if (reference.Definitions.GetValueOrDefault(vehicle.DefinitionId) is not { IsVehicle: true } definition)
        {
            reasons.Add("asl.a1.cc-vehicle.vehicle-outside");
            return reasons;
        }

        if (facts.VehicleBreakdown is { } breakdown)
        {
            var names = Weapons(vehicle, definition);
            if (facts.ByVehicle != true || breakdown.Weapons is null || names.Count != breakdown.Weapons.Count
                || names.Any(name => !breakdown.Weapons.ContainsKey(name))
                || breakdown.Weapons.Any(item => item.Value is null
                    || item.Value.Weapon != (item.Key == "ma" ? VehicleBreakdownWeapon.MainArmament
                        : item.Key == "cmg" ? VehicleBreakdownWeapon.CoaxialMg : VehicleBreakdownWeapon.AntiAircraftMg)
                    || item.Value.IntensiveFire
                    || ScenarioA1VehicleBreakdownResolutionCalculator.FactsRefusal(item.Value, VehicleBreakdownUse.CloseCombat) is not null))
            {
                reasons.Add("asl.a1.cc-vehicle.breakdown-facts-invalid");
            }
        }

        var units = facts.Units.Where(unit => unit.UnitId is not null).ToDictionary(unit => unit.UnitId!, StringComparer.Ordinal);
        if (units.Count != facts.Units.Count || facts.Units.Concat(facts.Passengers ?? []).Any(unit => unit.DefinitionId is null || !reference.Definitions.ContainsKey(unit.DefinitionId)))
        {
            reasons.Add("asl.a1.cc-vehicle.unit-outside");
            return reasons;
        }

        if (facts.ByVehicle == true)
        {
            // A11.62, D7.213: the vehicle attacks Infantry of the other side in the CCPh, never as Reaction Fire; a Stunned, Shocked, or Abandoned
            // vehicle has no attack.
            var defenders = facts.Defenders ?? [];
            if (facts.Reaction == true || facts.Phase != "CCPh" || defenders.Count == 0 || facts.Attackers is { Count: > 0 }
                || defenders.Any(id => !units.TryGetValue(id, out var unit) || unit.Side == vehicle.Side || unit.Captured == true)
                || defenders.Distinct(StringComparer.Ordinal).Count() != defenders.Count
                || vehicle.Stunned == true || vehicle.Shocked == true || vehicle.Abandoned == true)
            {
                reasons.Add("asl.a1.cc-vehicle.attack-outside");
            }
            else if (VehicleFirepower(facts, reference).Firepower <= 0)
            {
                reasons.Add("asl.a1.cc-vehicle.no-cc-armament");
            }

            return reasons;
        }

        // A11.5: one unit, or one with a SMC that adds one to its CCV; never a broken, captured, or berserk-less-than-armed unit; A11.51 ff.: the
        // vehicle may be Abandoned (A11.31) but is attacked only by the side it is not.
        var attackers = facts.Attackers ?? [];
        if (attackers.Count is < 1 or > 2 || attackers.Distinct(StringComparer.Ordinal).Count() != attackers.Count
            || attackers.Any(id => !units.TryGetValue(id, out var unit) || unit.Side == vehicle.Side || unit.Broken == true || unit.Captured == true)
            || (attackers.Count == 2 && !attackers.Any(id => IsSmc(reference.Definitions[units[id].DefinitionId!]))))
        {
            reasons.Add("asl.a1.cc-vehicle.attack-outside");
        }

        // D7.213: no pinned unit makes CC Reaction Fire (FPF CC Reaction Fire is not built).
        if (facts.Reaction == true && (facts.Phase != "MPh" || attackers.Any(id => units.TryGetValue(id, out var unit) && unit.Pinned == true)))
        {
            reasons.Add("asl.a1.cc-vehicle.reaction-outside");
        }

        if (attackers.Any(id => units.TryGetValue(id, out var unit) && Inexperience(reference.Definitions[unit.DefinitionId!]) && unit.Inexperienced is null))
        {
            reasons.Add("asl.a1.cc-vehicle.fact-missing:inexperienced");
        }

        // A11.52, A21.2 (pass 65, ruling R65.22): a capture attempt is made in the CCPh on an Abandoned AFV alone.
        if (facts.Capture == true && (facts.Reaction == true || vehicle.Abandoned != true || reference.Definitions.GetValueOrDefault(vehicle.DefinitionId) is not { Unarmored: not true }))
        {
            reasons.Add("asl.a1.cc-vehicle.capture-outside");
        }

        return reasons;
    }

    /// <summary>Current-context defense requires the experience fact used by A11.62; null context retains the recorded reading.</summary>
    public static IReadOnlyList<string> Precheck(VehicleCloseCombatFacts facts, ScenarioA1CloseCombatReference reference, CcvContext? context)
    {
        var reasons = Precheck(facts, reference);
        if (reasons.Count != 0 || context is null || facts.ByVehicle != true)
        {
            return reasons;
        }

        return facts.Units!.Any(unit => facts.Defenders!.Contains(unit.UnitId!, StringComparer.Ordinal)
            && Inexperience(reference.Definitions[unit.DefinitionId!]) && unit.Inexperienced is null)
            ? ["asl.a1.cc-vehicle.fact-missing:inexperienced"] : reasons;
    }

    private static bool IsSmc(FireDefinition definition) => definition.IsLeader || definition.IsHero;

    private static bool Inexperience(FireDefinition definition) => definition.Class is "green" or "conscript";

    /// <summary>
    /// The defenders' CCV against a vehicle's attack (ruling R11.15; referee, pass 11): each MMC's CCV, -1 Inexperienced and -1 pinned (A11.5), at least
    /// one; a SMC adds one when it defends with a MMC, as the A11.622 example counts it, and has its own CCV of 2 when it defends alone. It forwards to
    /// <see cref="ScenarioA1CloseCombatValueCalculator.DefenseCcv"/>: without a context no defender is an Assault Engineer or has a national case, the
    /// recorded reading (ruling RA11C.6), and with one they come from it.
    /// </summary>
    private static int DefenseCcv((CloseCombatUnit Facts, FireDefinition Definition)[] defenders, CcvContext? context) =>
        ScenarioA1CloseCombatValueCalculator.DefenseCcv([.. defenders.Select(item => DefenderFacts(item.Facts, item.Definition, context))]);

    private static CcvDefenderFacts DefenderFacts(CloseCombatUnit unit, FireDefinition definition, CcvContext? context) =>
        new(definition.Kind, IsSmc(definition), context?.IsAssaultEngineer(unit.UnitId!) == true, unit.Inexperienced == true, unit.Pinned == true,
            context?.NationalOf(unit.UnitId!) ?? NationalCcv.None);

    /// <summary>
    /// Whether the vehicle has inherent manned, functioning MG armament (A11.51, A11.61): a CMG or BMG not malfunctioned, or an AAMG while CE, with
    /// its crew neither Stunned nor Shocked; a BMG, though never used in CC, voids the DRM.
    /// </summary>
    private static bool MannedMg(VehicleCloseCombatVehicle vehicle, FireDefinition definition)
    {
        if (vehicle.Stunned == true || vehicle.Shocked == true || vehicle.Abandoned == true)
        {
            return false;
        }

        if (vehicle.UnavailableWeapons is { } unavailable)
        {
            return definition.BowMg is not null && vehicle.BmgMalfunctioned != true && !unavailable.Contains(definition.MainArmament == "bmg" ? "ma" : "bmg", StringComparer.Ordinal)
                || definition.CoaxialMg is not null && vehicle.CmgMalfunctioned != true && !unavailable.Contains(definition.MainArmament == "cmg" ? "ma" : "cmg", StringComparer.Ordinal)
                || definition.AntiAircraftMg is not null && vehicle.CrewExposed == true
                    && !(definition.MainArmament == "aamg" && vehicle.MainArmamentMalfunctioned == true)
                    && !unavailable.Contains(definition.MainArmament == "aamg" ? "ma" : "aamg", StringComparer.Ordinal);
        }

        var aamg = definition.MainArmament == "aamg" && definition.AntiAircraftMg is not null && vehicle.CrewExposed == true && vehicle.MainArmamentMalfunctioned != true
            && vehicle.UnavailableWeapons?.Contains("ma", StringComparer.Ordinal) != true;
        return aamg || (definition.BowMg is not null && vehicle.BmgMalfunctioned != true && vehicle.UnavailableWeapons?.Contains("bmg", StringComparer.Ordinal) != true)
            || (definition.CoaxialMg is not null && vehicle.CmgMalfunctioned != true && vehicle.UnavailableWeapons?.Contains("cmg", StringComparer.Ordinal) != true);
    }

    private static VehicleCloseCombatResolution AgainstVehicle(VehicleCloseCombatFacts facts, ScenarioA1CloseCombatReference reference, IReadOnlyList<int> dice,
        CcvContext? context)
    {
        var vehicle = facts.Vehicle!;
        var definition = reference.Definitions[vehicle.DefinitionId!];
        var units = facts.Units!.ToDictionary(unit => unit.UnitId!, StringComparer.Ordinal);
        var attackers = facts.Attackers!.Select(id => (Facts: units[id], Definition: reference.Definitions[units[id].DefinitionId!])).ToArray();
        var main = attackers.FirstOrDefault(item => !IsSmc(item.Definition));
        if (main.Facts is null)
        {
            main = attackers[0];
        }

        var smc = attackers.FirstOrDefault(item => item.Facts != main.Facts);

        // A18.12 (p. 85; pass 70, ruling R70.34): "Anytime a MMC attacking in CC rolls an Original 2 DR, the player makes an immediate dr on the Leader
        // Creation Table", with the Infantry CC's table drm (no odds column against a vehicle); A25.71: the Finns create none. An attack planned before the
        // pass rolls none. A18.2 (pass 71, ruling R71.17): "-1: CC vs AFV", when the record declares the vehicle an AFV; the NKVD's table has the same cell
        // (A25.25, p. 96).
        var original = dice[0] + dice[1];
        LeaderCreationOutcome? creation = null;
        if (original == 2 && facts.LeaderCreation == true && !IsSmc(main.Definition) && ScenarioA1FieldPromotion.Applicable(main.Definition.Nationality))
        {
            if (facts.Rolls!.LeaderCreation is not int creationDr || creationDr is < 1 or > 6)
            {
                return Refused(CloseCombatResolution.Indeterminate, ["asl.a1.cc-vehicle.roll-missing:leaderCreation"]);
            }

            List<FireModifier> afv = facts.AfvDrm == true && Afv(definition.Unarmored) ? [new FireModifier("cc-vs-afv", -1m, "A18.2")] : [];
            var (outcome, undecided) = ScenarioA1FieldPromotion.Create(main.Definition, ScenarioA1CloseCombatCalculator.BaseMorale(main.Facts, reference), creationDr, afv,
                reference.Definitions, "asl.a1.cc-vehicle", main.Facts.Fanatic == true);
            if (outcome is null)
            {
                return Refused(CloseCombatResolution.Indeterminate, [undecided!]);
            }

            creation = outcome;
        }

        var created = creation?.LeaderDefinitionId is { } createdId ? reference.Definitions[createdId] : null;

        // A11.5: the CCV, +1 for a combining SMC, -1 Inexperienced, and -1 for each halving penalty: pinned, and D7.213's fire counter. A18.12: a created
        // leader combines as a SMC, "just as if he had been there all along", only where no SMC already does (A11.5: "No more than two units").
        // The A11.5 claim (ruling RA11C.6): the block forwards to the calculator; without a context the Assault Engineer, the concealed vehicle, the
        // outnumbered Guard, and the national case are absent, the same answer as before, and with one they come from it.
        var ccv = ScenarioA1CloseCombatValueCalculator.AttackCcv(new CcvAttackFacts(main.Facts.UnitId!, main.Definition.Kind,
            context?.IsAssaultEngineer(main.Facts.UnitId!) == true, main.Facts.Inexperienced == true, smc.Facts?.UnitId, created?.Id,
            attackers.Any(item => item.Facts.Pinned == true),
            facts.Reaction == true && facts.FireMarked?.Any(id => facts.Attackers!.Contains(id)) == true,
            context?.VehicleConcealed == true, context is not null && attackers.Any(item => context.IsOutnumberedGuard(item.Facts.UnitId!)),
            context?.NationalOf(main.Facts.UnitId!) ?? NationalCcv.None)
        {
            Guard = context is not null && attackers.Any(item => context.IsGuard(item.Facts.UnitId!))
        });
        var modifiers = ccv.Modifiers;
        var kill = ccv.Ccv;

        var drm = new List<FireModifier>();
        if (definition.Unarmored == true)
        {
            drm.Add(new FireModifier("vs-unarmored", -3m, "A11.51"));
        }

        if (!MannedMg(vehicle, definition))
        {
            drm.Add(new FireModifier("vs-no-manned-mg", -1m, "A11.51"));
        }

        // A21.2 (pass 65, ruling R65.22): an Abandoned AFV "is assumed to be CE and Immobile as long as it is unmanned", for an attack planned since the pass.
        var unmanned = facts.Unmanned == true && vehicle.Abandoned == true && definition.Unarmored != true;
        if (definition.Unarmored != true && definition.OpenTopped == true)
        {
            drm.Add(new FireModifier("vs-open-topped", -2m, "A11.61"));
        }
        else if (definition.Unarmored != true && (vehicle.CrewExposed == true || unmanned))
        {
            drm.Add(new FireModifier("vs-ce-closed-topped", -1m, unmanned ? "A21.2" : "A11.61"));
        }

        // A11.61 (referee, pass 11): "Any Immobile AFV".
        if ((vehicle.Immobile == true || unmanned) && definition.Unarmored != true)
        {
            drm.Add(new FireModifier("vs-immobile", -1m, unmanned && vehicle.Immobile != true ? "A21.2" : "A11.61"));
        }

        // A11.51: +1 for each unbroken, unpinned, armed enemy HS or crew in the Location and +2 for each such squad.
        foreach (var escort in facts.Units!.Where(unit => unit.Side == vehicle.Side && unit.Broken != true && unit.Pinned != true && unit.Captured != true
            && unit.WithdrawingTo is null))
        {
            var escortDefinition = reference.Definitions[escort.DefinitionId!];
            if (escortDefinition.Kind is "asl:squad")
            {
                drm.Add(new FireModifier("escort:" + escort.UnitId, 2m, "A11.51"));
            }
            else if (escortDefinition.Kind is "asl:half-squad" || ScenarioA1Definitions.IsCrew(escortDefinition.Kind))
            {
                drm.Add(new FireModifier("escort:" + escort.UnitId, 1m, "A11.51"));
            }
        }

        if (vehicle.InMotion == true)
        {
            drm.Add(new FireModifier("vs-motion", 2m, "A11.51"));
        }

        // A11.51, A11.4 (pass 70, ruling R70.30): "An Ambush DRM ... can also apply to CC vs a vehicle", -1 by the ambusher and +1 against it.
        if (facts.Ambusher is { } ambusher)
        {
            drm.Add(attackers[0].Facts.Side == ambusher ? new FireModifier("ambush", -1m, "A11.51") : new FireModifier("vs-ambush", 1m, "A11.51"));
        }

        // A11.8 (p. 76; pass 70, ruling R70.33): Infantry advancing into the road Location from the ground level of an ADJACENT building "qualify for a
        // Streetfighting Ambush -1/+1 DRM in CC vs the vehicle (not cumulative with the normal -1/+1 CC DRM for Ambush)"; every attacker qualifies.
        else if (StreetFighting(facts, attackers.Select(item => item.Facts.UnitId!)))
        {
            drm.Add(new FireModifier("street-fighting", -1m, "A11.8"));
        }

        // A11.5: a leader's DRM only when he combines; A15.24: a hero's -1; A4.51 (ruling R5.2): +1 when a CX unit attacks.
        // A18.12: the created leader "must add his leadership to the Original 2 CC DR that created him (even if the modifier is a +1)". A10.7: leadership
        // modifiers are not cumulative, so he takes a combining leader's place; A11.5's "No more than two units" leaves him out where a SMC that is no
        // leader combines (pass 70, ruling R70.46, R29.12's reading).
        // The A11.5 claim (ruling RA11C.6): without a context the line keeps its answer, a combining leader's DRM even for a berserk attacker; with one,
        // ScenarioA1CloseCombatValueCalculator.LeaderDrmApplies drops it (A15.42, A11.141).
        var combiningLeader = smc.Facts is not null && smc.Definition.IsLeader;
        if (created is not null && (smc.Facts is null || combiningLeader))
        {
            drm.Add(new FireModifier("created-leader:" + created.Id, created.Leadership ?? 0, "A18.12"));
        }
        else if (combiningLeader && smc.Facts is { } leaderFacts && smc.Definition.Leadership is { } leadership
            && (context is null || ScenarioA1CloseCombatValueCalculator.LeaderDrmApplies(true, main.Facts.Berserk == true)))
        {
            drm.Add(new FireModifier("leadership:" + leaderFacts.UnitId, leadership + (leaderFacts.Wounded == true ? 1 : 0), "A11.51"));
        }

        foreach (var hero in attackers.Where(item => item.Definition.IsHero || (item.Definition.IsLeader && item.Facts.Heroic == true)))
        {
            drm.Add(new FireModifier("heroic:" + hero.Facts.UnitId, -1m, "A15.24"));
        }

        if (attackers.FirstOrDefault(item => item.Facts.Cx == true).Facts is { } exhausted)
        {
            drm.Add(new FireModifier("cx:" + exhausted.UnitId, 1m, "A4.51"));
        }

        var final = original + (int)drm.Sum(item => item.Value);
        // D5.7, C7.7 note A (pass 65, ruling R65.21): a red CS# burns on a Final DR one lower, for the burn alone. The A11.5 claim (the design's review,
        // finding 7; ruling RA11C.6): without a context the block keeps its answer, which tests burning first, so a Final DR equal to a CCV of 0 or less
        // burns the vehicle; with one, ScenarioA1CloseCombatValueCalculator.VehicleResult immobilizes it, as A11.5 does.
        var result = context is not null ? ScenarioA1CloseCombatValueCalculator.VehicleResult(final, kill, definition.CsRed == true)
            : final * 2 <= kill || (final < kill && ScenarioA1CrewSurvivalRules.BurnsRed(definition.CsRed == true, final, lower => lower * 2 <= kill))
                ? VehicleCloseCombatResolution.BurningWreck
            : final < kill ? VehicleCloseCombatResolution.Eliminated
            : final == kill ? VehicleCloseCombatResolution.Immobilized
            : VehicleCloseCombatResolution.None;

        // A11.501: after an Original 2 the attacker rolls the Unlikely Kill dr; the DR's own elimination or Immobilization stands unless the dr does better.
        int? unlikely = null;
        if (original == 2 && result != VehicleCloseCombatResolution.BurningWreck)
        {
            if (facts.Rolls!.UnlikelyKill is not int dr || dr is < 1 or > 6)
            {
                return Refused(CloseCombatResolution.Indeterminate, ["asl.a1.cc-vehicle.roll-missing:unlikelyKill"]);
            }

            unlikely = dr;
            var subsequent = dr switch
            {
                1 => VehicleCloseCombatResolution.BurningWreck,
                2 => VehicleCloseCombatResolution.Eliminated,
                3 => VehicleCloseCombatResolution.Immobilized,
                _ => VehicleCloseCombatResolution.None,
            };
            result = Severity(subsequent) > Severity(result) ? subsequent : result;
        }

        // A21.2 (pass 65, ruling R65.22): a capture attempt captures the Abandoned AFV where an attack to eliminate it would eliminate or burn it; it is
        // already Immobile, so nothing else.
        if (facts.Capture == true)
        {
            result = result is VehicleCloseCombatResolution.Eliminated or VehicleCloseCombatResolution.BurningWreck ? VehicleCloseCombatResolution.Captured : VehicleCloseCombatResolution.None;
        }

        // A11.621: an Original 12 against a crewed vehicle, not Abandoned, Shocked, or Stunned, Casualty Reduces the attackers.
        var effects = new List<CloseCombatUnitEffect>();
        if (original == 12 && vehicle.Abandoned != true && vehicle.Shocked != true && vehicle.Stunned != true)
        {
            foreach (var (attacker, attackerDefinition) in attackers)
            {
                if (Reduce(attacker, attackerDefinition, reference, facts.Rolls!) is not { } effect)
                {
                    return Refused(CloseCombatResolution.Indeterminate, ["asl.a1.cc-vehicle.roll-missing:woundSeverity:" + attacker.UnitId]);
                }

                effects.Add(effect);
            }
        }

        return new VehicleCloseCombatResolution(CloseCombatResolution.Resolved, [], kill, modifiers, drm, [.. dice], original, final, result, effects)
        {
            UnlikelyKillDr = unlikely,
            LeaderCreation = creation,
        };
    }

    private static bool StreetFighting(VehicleCloseCombatFacts facts, IEnumerable<string> units) => ScenarioA1StreetFighting.Applies(facts.Ambusher, facts.StreetFighters, units);

    private static int Severity(string result) => result switch
    {
        VehicleCloseCombatResolution.BurningWreck => 3,
        VehicleCloseCombatResolution.Eliminated => 2,
        VehicleCloseCombatResolution.Immobilized => 1,
        _ => 0,
    };

    /// <summary>
    /// A vehicle's CC FP (A11.62): its CMG and, while CE, its AAMG, neither malfunctioned; halved in Motion and against concealed defenders; none when
    /// Stunned or Shocked.
    /// </summary>
    private static (decimal Firepower, IReadOnlyList<FireModifier> Modifiers) VehicleFirepower(VehicleCloseCombatFacts facts, ScenarioA1CloseCombatReference reference)
    {
        var vehicle = facts.Vehicle!;
        var definition = reference.Definitions[vehicle.DefinitionId!];
        var fp = VehicleAttackFirepower(vehicle, definition, facts.Passengers ?? [], reference);
        var modifiers = new List<FireModifier>();
        if (vehicle.InMotion == true)
        {
            modifiers.Add(new FireModifier("motion", 0.5m, "A11.62"));
        }

        var units = facts.Units!.ToDictionary(unit => unit.UnitId!, StringComparer.Ordinal);
        if ((facts.Defenders ?? []).Any(id => units.TryGetValue(id, out var unit) && unit.Concealed == true))
        {
            modifiers.Add(new FireModifier("vs-concealed", 0.5m, "A11.62"));
        }

        return (modifiers.Aggregate(fp, (total, item) => total * item.Value), modifiers);
    }

    private static decimal VehicleAttackFirepower(VehicleCloseCombatVehicle vehicle, FireDefinition definition,
        IReadOnlyList<CloseCombatUnit> passengers, ScenarioA1CloseCombatReference reference)
    {
        decimal fp = 0;
        foreach (var weapon in Weapons(vehicle, definition))
        {
            fp += weapon == "cmg" || weapon == "ma" && definition.MainArmament == "cmg" ? definition.CoaxialMg!.Value : definition.AntiAircraftMg!.Value;
        }

        // A11.62 (p. 76; pass 70, ruling R70.31): "CE Passengers in a halftrack" add their FP to its attack: a MMC its printed FP, a SMC one (A11.14),
        // a pinned one half (A11.62); a broken or captured one none. The whole is then halved as the vehicle's own FP is.
        foreach (var passenger in passengers.Where(item => item.Broken != true && item.Captured != true))
        {
            var printed = reference.Definitions[passenger.DefinitionId!];
            decimal share = IsSmc(printed) ? 1 : printed.Firepower ?? 0;
            fp += passenger.Pinned == true ? share / 2 : share;
        }

        return fp;
    }

    /// <summary>A11.62/D1.321/D1.322 (PDF pp. 76, 199): the functioning CMG and exposed AAMG actually taking part.</summary>
    public static IReadOnlyList<string> Weapons(VehicleCloseCombatVehicle vehicle, FireDefinition definition)
    {
        var names = new List<string>();
        var cmgName = definition.MainArmament == "cmg" ? "ma" : "cmg";
        if (definition.CoaxialMg is not null && vehicle.CmgMalfunctioned != true && !definition.TurretBarredWhileCe(vehicle.CrewExposed)
            && vehicle.UnavailableWeapons?.Contains(cmgName, StringComparer.Ordinal) != true)
        {
            names.Add(cmgName);
        }

        var aamgName = definition.MainArmament == "aamg" ? "ma" : "aamg";
        if (definition.AntiAircraftMg is not null && vehicle.CrewExposed == true && !(aamgName == "ma" && vehicle.MainArmamentMalfunctioned == true)
            && vehicle.UnavailableWeapons?.Contains(aamgName, StringComparer.Ordinal) != true)
        {
            names.Add(aamgName);
        }
        return names;
    }

    /// <summary>A11.62: the same participating armament and crew facts used by the attack precheck, for turn order and choices.</summary>
    public static bool CanAttack(VehicleCloseCombatVehicle vehicle, FireDefinition definition, IReadOnlyList<CloseCombatUnit> passengers,
        ScenarioA1CloseCombatReference reference, bool captured, bool recalled, bool unconfirmedKill) =>
        !captured && !recalled && !unconfirmedKill && vehicle.Stunned != true && vehicle.Shocked != true && vehicle.Abandoned != true
        && VehicleAttackFirepower(vehicle, definition, passengers, reference) > 0;

    private static VehicleCloseCombatResolution ByVehicle(VehicleCloseCombatFacts facts, ScenarioA1CloseCombatReference reference, IReadOnlyList<int> dice,
        CcvContext? context)
    {
        var units = facts.Units!.ToDictionary(unit => unit.UnitId!, StringComparer.Ordinal);
        var defenders = facts.Defenders!.Select(id => (Facts: units[id], Definition: reference.Definitions[units[id].DefinitionId!])).ToArray();
        var (fp, modifiers) = VehicleFirepower(facts, reference);
        var defense = Math.Max(1, DefenseCcv(defenders, context));
        var (label, kill, _) = reference.Column(fp, defense);
        var original = dice[0] + dice[1];
        var defending = new List<CloseCombatDefenderResult>();
        foreach (var (defender, defenderDefinition) in defenders)
        {
            // A11.62 (p. 76; the A11.5 claim, ruling RA11C.6): with a context, a defender's CCV that would fall below 1 is a DRM instead; without one the
            // recorded reading, its CCV floored at 1 and no DRM.
            List<FireModifier> own = context is not null
                && ScenarioA1CloseCombatValueCalculator.DefenseCcvDrm(DefenderFacts(defender, defenderDefinition, context)) is var below and < 0
                ? [new FireModifier("ccv-below-one:" + defender.UnitId, below, "A11.62")] : [];
            // A11.16: -2 against a broken unit; A4.51 (ruling R5.2): -1 against a CX unit.
            if (defender.Broken == true)
            {
                own.Add(new FireModifier("vs-broken", -2m, "A11.16"));
            }

            if (defender.Cx == true)
            {
                own.Add(new FireModifier("vs-cx", -1m, "A4.51"));
            }

            // D5.34 (p. 203; pass 35, task 35.13 d; the D5.34 claim): a vehicle under a Stun "+1" counter adds one to its CC DR.
            if (ScenarioA1StunCalculator.Drm(StunDr.CloseCombat, facts.Vehicle!.StunRecovery == true) is > 0 and var stun)
            {
                own.Add(new FireModifier("stun-recovery", stun, "D5.34"));
            }

            // D3.44, D3.45 (p. 200; pass 55, rulings R55.16, R55.18): the vehicle's Armor Leader modifies its CC DR; an Inexperienced Crew adds one.
            if (ScenarioA1ArmorLeaders.Modifier(facts.Vehicle.ArmorLeadership) is { } armorLeader)
            {
                own.Add(armorLeader);
            }

            // A11.51, A11.4 (the I5 example, p. 75; pass 70, ruling R70.30): the vehicle's attack takes -1 when its side ambushed, +1 against an ambusher.
            if (facts.Ambusher is { } ambusher)
            {
                own.Add(facts.Vehicle.Side == ambusher ? new FireModifier("ambush", -1m, "A11.51") : new FireModifier("vs-ambush", 1m, "A11.51"));
            }

            // A11.8 (pass 70, ruling R70.33): the vehicle's attack on a unit that qualifies for Street Fighting takes +1 (the I5 example, p. 75).
            else if (StreetFighting(facts, [defender.UnitId!]))
            {
                own.Add(new FireModifier("vs-street-fighting", 1m, "A11.8"));
            }

            // A11.52 (p. 74; ruling R70.34): a unit that attempted to capture the Passengers is "eligible for ... a +2 DRM in defending against attacks from
            // those Passengers", whose attack is their vehicle's (A11.62).
            if (facts.Passengers is { Count: > 0 } && facts.Captors?.Contains(defender.UnitId!) == true)
            {
                own.Add(new FireModifier("vs-passenger-captor", 2m, "A11.52"));
            }

            var final = original + (int)own.Sum(item => item.Value);
            var result = final < kill ? CloseCombatDefenderResult.Eliminated
                : final == kill ? CloseCombatDefenderResult.PartialKill
                : CloseCombatDefenderResult.NoEffect;
            defending.Add(new CloseCombatDefenderResult(defender.UnitId!, own, final, result));
        }

        // A11.11: a Partial Kill Casualty Reduces one defender, chosen among several by Random Selection (the highest dr, ties included).
        var partial = defending.Where(item => item.Result == CloseCombatDefenderResult.PartialKill).ToArray();
        var reduced = new HashSet<string>(StringComparer.Ordinal);
        if (partial.Length == 1)
        {
            reduced.Add(partial[0].UnitId);
        }
        else if (partial.Length > 1)
        {
            var selection = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var item in partial)
            {
                if (facts.Rolls!.RandomSelection?.TryGetValue(item.UnitId, out var dr) != true)
                {
                    return Refused(CloseCombatResolution.Indeterminate, ["asl.a1.cc-vehicle.roll-missing:randomSelection:" + string.Join(",", partial.Select(entry => entry.UnitId))]);
                }

                selection[item.UnitId] = dr;
            }

            var highest = selection.Values.Max();
            reduced.UnionWith(selection.Where(item => item.Value == highest).Select(item => item.Key));
            defending = [.. defending.Select(item => selection.TryGetValue(item.UnitId, out var dr) ? item with { RandomSelectionDr = dr, CasualtyReduced = reduced.Contains(item.UnitId) } : item)];
        }

        var effects = new List<CloseCombatUnitEffect>();
        foreach (var (defender, definition) in defenders)
        {
            var outcome = defending.First(item => item.UnitId == defender.UnitId).Result;
            if (outcome == CloseCombatDefenderResult.Eliminated)
            {
                effects.Add(new CloseCombatUnitEffect(defender.UnitId!, definition.Id, definition.Id, true, false, ["eliminated-cc"]));
            }
            else if (reduced.Contains(defender.UnitId!))
            {
                if (Reduce(defender, definition, reference, facts.Rolls!) is not { } effect)
                {
                    return Refused(CloseCombatResolution.Indeterminate, ["asl.a1.cc-vehicle.roll-missing:woundSeverity:" + defender.UnitId]);
                }

                effects.Add(effect);
            }
        }

        return new VehicleCloseCombatResolution(CloseCombatResolution.Resolved, [], kill, modifiers, [], [.. dice], original, original,
            VehicleCloseCombatResolution.None, effects)
        {
            AttackFirepower = fp,
            DefenseValue = defense,
            Odds = label,
            Defending = defending,
        };
    }

    /// <summary>Casualty Reduction (A7.302): a squad becomes its HS, a HS or crew is eliminated, and a SMC is wounded or dies (A17.11); null when a Wound Severity dr is missing.</summary>
    private static CloseCombatUnitEffect? Reduce(CloseCombatUnit unit, FireDefinition definition, ScenarioA1CloseCombatReference reference, VehicleCloseCombatRolls rolls)
    {
        if (IsSmc(definition))
        {
            if (rolls.WoundSeverity?.TryGetValue(unit.UnitId!, out var dr) != true)
            {
                return null;
            }

            return dr + (unit.Wounded == true ? 1 : 0) >= 5
                ? new CloseCombatUnitEffect(unit.UnitId!, definition.Id, definition.Id, true, false, ["eliminated-mortal-wound"])
                : new CloseCombatUnitEffect(unit.UnitId!, definition.Id, definition.Id, false, true, ["wounded"]);
        }

        return ScenarioA1FireReference.HalfSquadOf(definition.Id) is { } half && reference.Definitions.ContainsKey(half)
            ? new CloseCombatUnitEffect(unit.UnitId!, definition.Id, half, false, false, ["casualty-reduced"])
            : new CloseCombatUnitEffect(unit.UnitId!, definition.Id, definition.Id, true, false, ["eliminated-casualty-reduction"]);
    }

    private static VehicleCloseCombatResolution Refused(string disposition, IReadOnlyList<string> reasons) =>
        new(disposition, reasons, null, [], [], [], 0, 0, VehicleCloseCombatResolution.None, []);
}
