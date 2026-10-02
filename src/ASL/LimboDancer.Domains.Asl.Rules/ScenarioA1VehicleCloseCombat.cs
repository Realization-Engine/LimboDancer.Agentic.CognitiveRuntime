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
    bool? CmgMalfunctioned);

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
    /// <summary>The attackers marked First or Final Fire, whose CC Reaction Fire CCV is one lower (D7.213); null for none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? FireMarked
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
    public const string Immobilized = "immobilized";
    public const string Eliminated = "eliminated";
    public const string BurningWreck = "burning-wreck";

    /// <summary>The Unlikely Kill dr after an Original 2 (A11.501); null when none was rolled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? UnlikelyKillDr
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
    public static VehicleCloseCombatResolution Resolve(VehicleCloseCombatFacts facts, ScenarioA1CloseCombatReference reference)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(reference);
        var reasons = Precheck(facts, reference);
        if (reasons.Count != 0)
        {
            return Refused(CloseCombatResolution.Abstained, reasons);
        }

        if (facts.Rolls?.Attack is not [>= 1 and <= 6, >= 1 and <= 6] dice)
        {
            return Refused(CloseCombatResolution.Indeterminate, ["asl.a1.cc-vehicle.roll-missing:attack"]);
        }

        return facts.ByVehicle == true ? ByVehicle(facts, reference, dice) : AgainstVehicle(facts, reference, dice);
    }

    /// <summary>Why the attack is outside the review; empty when it is admitted.</summary>
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

        if (reference.Definitions.GetValueOrDefault(vehicle.DefinitionId) is not { IsVehicle: true })
        {
            reasons.Add("asl.a1.cc-vehicle.vehicle-outside");
        }

        var units = facts.Units.Where(unit => unit.UnitId is not null).ToDictionary(unit => unit.UnitId!, StringComparer.Ordinal);
        if (units.Count != facts.Units.Count || facts.Units.Any(unit => unit.DefinitionId is null || !reference.Definitions.ContainsKey(unit.DefinitionId)))
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

        return reasons;
    }

    private static bool IsSmc(FireDefinition definition) => definition.IsLeader || definition.IsHero;

    private static bool Inexperience(FireDefinition definition) => definition.Class is "green" or "conscript";

    /// <summary>A unit's CCV (A11.5): squad 5, crew 4, HS 3, SMC 2.</summary>
    private static int BaseCcv(FireDefinition definition) => definition.Kind switch
    {
        "asl:squad" => 5,
        "asl:crew" => 4,
        "asl:half-squad" => 3,
        _ => 2,
    };

    /// <summary>
    /// The defenders' CCV against a vehicle's attack (ruling R11.15; referee, pass 11): each MMC's CCV, -1 Inexperienced and -1 pinned (A11.5), at least
    /// one; a SMC adds one when it defends with a MMC, as the A11.622 example counts it, and has its own CCV of 2 when it defends alone.
    /// </summary>
    private static int DefenseCcv((CloseCombatUnit Facts, FireDefinition Definition)[] defenders)
    {
        var mmc = defenders.Where(item => !IsSmc(item.Definition)).ToArray();
        var total = mmc.Sum(item => Math.Max(1, BaseCcv(item.Definition) - (item.Facts.Inexperienced == true ? 1 : 0) - (item.Facts.Pinned == true ? 1 : 0)));
        var smc = defenders.Length - mmc.Length;
        return total + (mmc.Length > 0 ? smc : smc * 2);
    }

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

        var aamg = definition.MainArmament == "aamg" && definition.AntiAircraftMg is not null && vehicle.CrewExposed == true && vehicle.MainArmamentMalfunctioned != true;
        return aamg || (definition.BowMg is not null && vehicle.BmgMalfunctioned != true) || (definition.CoaxialMg is not null && vehicle.CmgMalfunctioned != true);
    }

    private static VehicleCloseCombatResolution AgainstVehicle(VehicleCloseCombatFacts facts, ScenarioA1CloseCombatReference reference, IReadOnlyList<int> dice)
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

        // A11.5: the CCV, +1 for a combining SMC, -1 Inexperienced, and -1 for each halving penalty: pinned, and D7.213's fire counter.
        var ccv = BaseCcv(main.Definition);
        var modifiers = new List<FireModifier>();
        if (smc.Facts is not null)
        {
            modifiers.Add(new FireModifier("smc-combining:" + smc.Facts.UnitId, 1m, "A11.5"));
        }

        if (main.Facts.Inexperienced == true)
        {
            modifiers.Add(new FireModifier("inexperienced:" + main.Facts.UnitId, -1m, "A11.5"));
        }

        if (attackers.Any(item => item.Facts.Pinned == true))
        {
            modifiers.Add(new FireModifier("pinned", -1m, "A11.5"));
        }

        if (facts.Reaction == true && facts.FireMarked?.Any(id => facts.Attackers!.Contains(id)) == true)
        {
            modifiers.Add(new FireModifier("reaction-fire-marked", -1m, "D7.213"));
        }

        var kill = ccv + (int)modifiers.Sum(item => item.Value);

        var drm = new List<FireModifier>();
        if (definition.Unarmored == true)
        {
            drm.Add(new FireModifier("vs-unarmored", -3m, "A11.51"));
        }

        if (!MannedMg(vehicle, definition))
        {
            drm.Add(new FireModifier("vs-no-manned-mg", -1m, "A11.51"));
        }

        if (definition.Unarmored != true && definition.OpenTopped == true)
        {
            drm.Add(new FireModifier("vs-open-topped", -2m, "A11.61"));
        }
        else if (definition.Unarmored != true && vehicle.CrewExposed == true)
        {
            drm.Add(new FireModifier("vs-ce-closed-topped", -1m, "A11.61"));
        }

        // A11.61 (referee, pass 11): "Any Immobile AFV".
        if (vehicle.Immobile == true && definition.Unarmored != true)
        {
            drm.Add(new FireModifier("vs-immobile", -1m, "A11.61"));
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
            else if (escortDefinition.Kind is "asl:half-squad" or "asl:crew")
            {
                drm.Add(new FireModifier("escort:" + escort.UnitId, 1m, "A11.51"));
            }
        }

        if (vehicle.InMotion == true)
        {
            drm.Add(new FireModifier("vs-motion", 2m, "A11.51"));
        }

        // A11.5: a leader's DRM only when he combines; A15.24: a hero's -1; A4.51 (ruling R5.2): +1 when a CX unit attacks.
        if (smc.Facts is not null && smc.Definition.IsLeader && smc.Definition.Leadership is { } leadership)
        {
            drm.Add(new FireModifier("leadership:" + smc.Facts.UnitId, leadership + (smc.Facts.Wounded == true ? 1 : 0), "A11.51"));
        }

        foreach (var hero in attackers.Where(item => item.Definition.IsHero || (item.Definition.IsLeader && item.Facts.Heroic == true)))
        {
            drm.Add(new FireModifier("heroic:" + hero.Facts.UnitId, -1m, "A15.24"));
        }

        if (attackers.FirstOrDefault(item => item.Facts.Cx == true).Facts is { } exhausted)
        {
            drm.Add(new FireModifier("cx:" + exhausted.UnitId, 1m, "A4.51"));
        }

        var original = dice[0] + dice[1];
        var final = original + (int)drm.Sum(item => item.Value);
        var result = final * 2 <= kill ? VehicleCloseCombatResolution.BurningWreck
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
        };
    }

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
        decimal fp = 0;
        if (definition.CoaxialMg is { } cmg && vehicle.CmgMalfunctioned != true)
        {
            fp += cmg;
        }

        if (definition.AntiAircraftMg is { } aamg && vehicle.CrewExposed == true && !(definition.MainArmament == "aamg" && vehicle.MainArmamentMalfunctioned == true))
        {
            fp += aamg;
        }

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

    private static VehicleCloseCombatResolution ByVehicle(VehicleCloseCombatFacts facts, ScenarioA1CloseCombatReference reference, IReadOnlyList<int> dice)
    {
        var units = facts.Units!.ToDictionary(unit => unit.UnitId!, StringComparer.Ordinal);
        var defenders = facts.Defenders!.Select(id => (Facts: units[id], Definition: reference.Definitions[units[id].DefinitionId!])).ToArray();
        var (fp, modifiers) = VehicleFirepower(facts, reference);
        var defense = Math.Max(1, DefenseCcv(defenders));
        var (label, kill, _) = reference.Column(fp, defense);
        var original = dice[0] + dice[1];
        var defending = new List<CloseCombatDefenderResult>();
        foreach (var (defender, _) in defenders)
        {
            // A11.16: -2 against a broken unit; A4.51 (ruling R5.2): -1 against a CX unit.
            List<FireModifier> own = defender.Broken == true ? [new FireModifier("vs-broken", -2m, "A11.16")] : [];
            if (defender.Cx == true)
            {
                own.Add(new FireModifier("vs-cx", -1m, "A4.51"));
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
