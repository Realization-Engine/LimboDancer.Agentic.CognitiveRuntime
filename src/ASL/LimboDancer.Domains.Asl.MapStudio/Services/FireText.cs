using System.Globalization;
using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// A fire attack as the viewer may see it (pass 28b, moved from the Play page): the record, or the public report of one withheld from the viewer,
/// whose effects are then null.
/// </summary>
public sealed record FireView(string EventId, string Group, string Target, string? TargetSide, FireArithmetic Arithmetic,
    IReadOnlyList<FireUnitEffect>? Effects)
{
    /// <summary>
    /// Pass 31 (play test P-16): the targets that were broken, and those pinned, before the attack, so the Effect column says what the attack
    /// changed and not what was already so. Empty when the effects are withheld.
    /// </summary>
    public IReadOnlySet<string> BrokenBefore { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>The targets pinned before the attack.</summary>
    public IReadOnlySet<string> PinnedBefore { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>The Vehicle line and Collateral Attack results of the target Location's vehicles (A7.308, D.8B).</summary>
    public IReadOnlyList<FireVehicleEffect>? Vehicles
    {
        get; init;
    }

    /// <summary>What a FT, MOL, or DC did to the AFV it named and to its crew, in lines (pass 60); null when it named none.</summary>
    public IReadOnlyList<string>? NamedVehicle
    {
        get; init;
    }

    /// <summary>What the attack's KIA did to the weapons its victims held, and a DC to a manned Gun, in lines (pass 60, ruling R60.17); null when nothing.</summary>
    public IReadOnlyList<string>? WeaponDestruction
    {
        get; init;
    }

    /// <summary>Whether several units fire, so the record's verb agrees: "fire", not "fires".</summary>
    public bool Several
    {
        get; init;
    }

    /// <summary>Whether the attack's LOS was blocked (A6.11; pass 31d, design D7): it has no effect, and its firers have fired.</summary>
    public bool LosBlocked
    {
        get; init;
    }

    /// <summary>
    /// Whether the attack is a Canister shot's on a Location where the view knows of no unit (C8.4; the table player's review of pass 55): the
    /// record then says so, and neither shows a result on nobody nor says whether the FP was halved, which would tell of units out of view.
    /// </summary>
    public bool NoKnownTarget
    {
        get; init;
    }

    /// <summary>What the attack did to each MG used in it (pass 31c, play test P-17): a kept rate of fire, a malfunction. Null when none was used or the record is withheld.</summary>
    public IReadOnlyList<FireWeaponEffect>? Weapons
    {
        get; init;
    }

    /// <summary>
    /// A text of this record in the view's words (pass 31c, design D11): the units and counters its arithmetic and effects name by id, as the view
    /// could name them just before the attack. The records give it; without one a text keeps its identifiers.
    /// </summary>
    public Func<string, string> Say { get; init; } = text => text;
}

/// <summary>
/// The wording of fire results (pass 28b, moved from the Play page so the fire history's components and the page's other records share it). It
/// formats what a record already holds and never recalculates an adjudication.
/// </summary>
public static class FireText
{
    public static string Number(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    public static string ModifierText(IEnumerable<FireModifier> drm) =>
        string.Concat(drm.Select(item => $" {(item.Value < 0 ? "-" : "+")} {Number(Math.Abs(item.Value))} ({DisplayText.Modifier(item.Name)}, {item.Rule})"));

    /// <summary>
    /// A Heat of Battle DR in words, for example "Heat of Battle DR 2, 3 = 5 + 2 (nationality:russian, A15.1) = Final DR 7: Battle Hardened into
    /// defender-elite-squad".
    /// </summary>
    public static string HeatOfBattleText(HeatOfBattleOutcome heat)
    {
        ArgumentNullException.ThrowIfNull(heat);
        List<string> parts = [];
        if (heat.HeroDefinitionId is { } hero)
        {
            parts.Add($"creates a hero ({hero})");
        }

        if (heat.Heroic == true)
        {
            parts.Add("the leader becomes heroic");
        }

        if (heat.HardenedDefinitionId is { } hardened)
        {
            parts.Add($"Battle Hardened into {hardened}");
        }

        if (heat.Fanatic == true)
        {
            parts.Add("becomes Fanatic");
        }

        if (heat.NoKnownEnemyInLos == true)
        {
            parts.Add("Berserk with no Known enemy unit in its LOS, so Battle Hardening instead (A15.44)");
        }

        if (heat.Result == HeatOfBattleOutcome.Berserk)
        {
            parts.Add("goes berserk (A15.4)");
        }
        else if (heat.Result == HeatOfBattleOutcome.Surrender)
        {
            parts.Add(heat.Captors is { Count: > 0 } captors ? $"surrenders to {string.Join(" or ", captors)}, the captor's choice (A15.5)" : "is broken and Disrupted, with no captor next to it (A15.5)");
        }

        if (parts.Count == 0)
        {
            parts.Add("no change");
        }

        // Pass 31d (design D11): with no DRM the DR is its own Final DR, and is said once ("DR 4, 4 = 8", not "= 8 = Final DR 8").
        return $"Heat of Battle DR {string.Join(", ", heat.Dice)} = {heat.OriginalDr}{(heat.Drm.Count == 0 && heat.OriginalDr == heat.FinalDr ? string.Empty : $"{ModifierText(heat.Drm)} = Final DR {heat.FinalDr}")}"
            + $": {string.Join(", ", parts)} (A15.1)";
    }

    /// <summary>
    /// What an attack did to a MG, in words (A9.2, A9.7); null when there is nothing a player needs to be told. A MG that kept its rate of fire may
    /// fire again in the phase, which the page said nowhere before (play test P-17).
    /// </summary>
    public static string? WeaponText(FireWeaponEffect weapon)
    {
        ArgumentNullException.ThrowIfNull(weapon);
        // A.11, A9.7 (pass 45): a weapon whose lowered B# became an X# is removed, not malfunctioned.
        // D3.7, A9.71 (pass 50): a vehicle's BMG or CMG by its name, with the Random Selection dr when two fired together.
        if (weapon.VehicleWeapon is { } mg)
        {
            var selection = weapon.SelectionDr is { } dr ? $"; Random Selection dr {dr} (A9.71)" : string.Empty;
            return weapon.Malfunctioned ? $"{weapon.EquipmentId}'s {mg.ToUpperInvariant()} malfunctioned (D3.7){selection}"
                : selection.Length > 0 ? $"{weapon.EquipmentId}'s {mg.ToUpperInvariant()} did not malfunction{selection}" : null;
        }

        return weapon.Removed == true ? $"{weapon.EquipmentId} is removed: the DR reached its X# (A.11, A9.7)"
            : weapon.Malfunctioned ? $"{weapon.EquipmentId} malfunctioned (A9.7)"
            : weapon.RateOfFireRetained ? $"{weapon.EquipmentId} kept its rate of fire and may fire again this phase (A9.2)"
            : null;
    }

    /// <summary>
    /// One of a firer's FP modifiers as a player reads it (pass 31d, design D11): the Fire package lists Assault Fire's one FP among the multipliers
    /// with the value 1, and it adds one (A7.36), so it reads "+ 1", not "x 1".
    /// </summary>
    public static string MultiplierText(FireModifier item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Name == "assault-fire" ? $" + {Number(item.Value)} ({DisplayText.Modifier(item.Name)}, {item.Rule})" : $" × {Number(item.Value)} ({DisplayText.Modifier(item.Name)}, {item.Rule})";
    }

    /// <summary>A Canister attack on a Location where the view knows of no unit, in words: "no Known unit in [C9]: no effect to show".</summary>
    public static string NoKnownTargetText(string target) => $"no Known unit in {target}: no effect to show";

    /// <summary>An IFT result as a player says it: the record's "none" reads "no effect".</summary>
    public static string ResultText(string result) => result == "none" ? "no effect" : result;

    /// <summary>What an attack did to a target unit, in words.</summary>
    public static string EffectText(FireUnitEffect effect) => EffectText(effect, false, false);

    /// <summary>What an attack did to a target unit, in words; a state the unit was already in is said as such (pass 31, play test P-16).</summary>
    public static string EffectText(FireUnitEffect effect, bool brokenBefore, bool pinnedBefore)
    {
        ArgumentNullException.ThrowIfNull(effect);
        List<string> parts = [];
        if (effect.Eliminated)
        {
            parts.Add("eliminated");
        }
        else if (effect.FinalDefinitionId != effect.DefinitionId && effect.HeatOfBattle?.HardenedDefinitionId != effect.FinalDefinitionId)
        {
            parts.Add($"now {effect.FinalDefinitionId}");
        }

        foreach (var (applies, name) in new[] { (effect.Broken, brokenBefore ? "already broken" : "broken"), (effect.Pinned, pinnedBefore ? "already pinned" : "pinned"), (effect.Wounded, "wounded"), (effect.Disrupted, "Disrupted"),
            (effect.Berserk == true, "berserk"), (effect.ConcealmentLost, "concealment lost") })
        {
            if (applies)
            {
                parts.Add(name);
            }
        }

        if (effect.HeatOfBattle is { } heat)
        {
            parts.Add(HeatOfBattleText(heat));
        }

        return parts.Count == 0 ? "unaffected" : string.Join(", ", parts);
    }

    /// <summary>A MC, NTC, LLMC, or LLTC with its arithmetic, for example "NMC 4, 4 = 8 against 7: failed".</summary>
    public static string CheckText(FireCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        return $"{check.Kind} {string.Join(", ", check.Dice)}{string.Concat(check.Drm.Select(item => $" {(item.Value < 0 ? "-" : "+")} {Number(Math.Abs(item.Value))} ({DisplayText.Modifier(item.Name)})"))}"
            // D3.42, D3.45 (pass 55): a vehicle crew's check at its Armor Leader's Morale Level, or an Inexperienced Crew's, says whose it is.
            + $" = {check.FinalDr} against {MoraleWords(check)}"
            + $": {(check.Passed ? "passed" : "failed")}";
    }

    /// <summary>
    /// What a check is made against: the Morale Level, or "crew morale 10, its Armor Leader's (D3.42)" when it is a vehicle crew's by its Armor
    /// Leader or as an Inexperienced Crew (the table player's review of pass 55: one parenthesis, not one inside another).
    /// </summary>
    public static string MoraleWords(FireCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        return ScenarioA1ArmorLeaders.MoraleText(check.MoraleSource, check.MoraleLevel) is { Length: > 0 } source ? source : check.MoraleLevel.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// What a FT, MOL, or DC did to the AFV it named, in lines (pass 60, rulings R60.15, R60.16): the DC's Position DR, the To Kill DR with its Final TK#
    /// and the crew's checks, and the Specific Collateral Attack on its CE crew. Empty when the attack named none. A DC poorly positioned at an AFV
    /// whose crew is not CE (<paramref name="crewExposed"/> false) has no effect at all (the table player, pass 60).
    /// </summary>
    public static IReadOnlyList<string> NamedVehicleLines(string vehicleId, FireResolution resolution, bool crewExposed = true)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        var lines = new List<string>();
        if (resolution.Position is { } position)
        {
            lines.Add($"Position DR at {vehicleId} {string.Join(", ", position.Dice)} = {position.OriginalDr}{ScenarioA1FlameKill.ModifierText(position.Drm)}: Final DR {position.FinalDr}, "
                + (resolution.DemolitionChargeMalfunctioned == true ? "the DC malfunctions and does not detonate (A23.4)" : position.Result switch
                {
                    FirePosition.Optimal => $"optimally positioned, a {position.HitLocation} hit against the Aerial AF (C7.346)",
                    FirePosition.Positioned => $"positioned, a {position.HitLocation} hit against its AF (C7.346)",
                    FirePosition.Poor => crewExposed ? "poorly positioned: no effect on the AFV, and its CE crew alone is attacked on the second DR (C7.346)"
                        : "poorly positioned: no effect, as its crew is BU (C7.346)",
                    _ => "no effect on the AFV or its crew; the Location's other units are attacked as Area Fire on the second DR (C7.346)",
                }));
        }

        if (resolution.VehicleKill is { } kill)
        {
            var fate = kill.Result switch
            {
                OrdnanceKill.Burn => "burns (a burning wreck)",
                OrdnanceKill.Eliminated => "eliminated (a wreck)",
                OrdnanceKill.Immobilized => "immobilized",
                OrdnanceKill.Shock => "Shocked",
                OrdnanceKill.Dud => "no effect (an Original 12, C7.35)",
                _ => "no effect",
            };
            var checks = (kill.CrewCheck is { } tc ? $"; crew TC DR {tc.FinalDr} against {MoraleWords(tc)}: {(tc.Passed ? "passed" : "Abandoned")}" : "")
                + (kill.CrewSurvival is { } cs ? $"; Crew Survival DR {cs.FinalDr} against CS# {cs.CrewSurvival}: {(cs.Survived ? "the crew survives" : "no survivors")}" : "");
            lines.Add($"To Kill DR at {vehicleId} on the {kill.Ammunition.ToUpperInvariant()} column (C7.34), {kill.TargetFacing} Target Facing: Basic TK# {kill.BasicTk}"
                + $"{ScenarioA1FlameKill.ModifierText(kill.Modifications)}{(kill.ArmorFactor is { } af ? $", less AF {af}" : ", no AF")} = Final TK# {kill.FinalTk}; "
                + $"TK DR {string.Join(", ", kill.Dice)} = {kill.OriginalDr}: {fate}{checks}");

            // D6.9 (pass 65): the Survival DR of each PRC.
            lines.AddRange(PrcSurvivalLines(vehicleId, kill.PrcSurvival));
        }

        if (resolution.SpecificCollateral is { } collateral)
        {
            // Pass 65 (ruling R65.7): the attack may reach the PRC alone; the crew's line is written only when it took it.
            var crew = collateral.Effect;
            if (crew.CrewResult != FireVehicleEffect.NotVulnerable)
            {
                lines.Add($"its CE crew takes a Specific Collateral Attack of {collateral.Firepower} FP{ScenarioA1FlameKill.ModifierText(crew.Drm)} (D.8A): Final DR {crew.FinalDr}, {ResultText(collateral.Result)}"
                    + (crew.CrewCheck is { } made ? $"; {CheckText(made)}" : "")
                    + (crew.CrewResult == FireVehicleEffect.None ? "" : $": {crew.CrewResult}"));
            }

            lines.AddRange(PrcLines(crew));
        }

        return lines;
    }

    /// <summary>
    /// What a KIA did to each weapon its victims held (A9.74), and a DC to the Gun its target units manned (C11.6), in lines (pass 60, ruling R60.17):
    /// the dr on the attack's column, its result, and the weapon's fate. Empty when nothing was checked.
    /// </summary>
    public static IReadOnlyList<string> WeaponDestructionLines(IReadOnlyList<FireWeaponDestruction>? fates) =>
        [.. (fates ?? []).Select(fate =>
        {
            var outcome = fate.Fate switch
            {
                ScenarioA1GunDestruction.Destroyed => "destroyed",
                ScenarioA1GunDestruction.Malfunctioned => "malfunctioned",
                _ => "unharmed",
            };
            return fate.Dr is { } dr
                ? $"{fate.EquipmentId}, held by {fate.HolderId}: Random SW/Gun Destruction dr {dr} on the same column, {ResultText(fate.Result)}: {outcome} (A9.74)"
                : $"{fate.EquipmentId}, the Gun {fate.HolderId} manned: the DC's {ResultText(fate.Result)} leaves it {outcome} (C11.6)";
        })];

    /// <summary>
    /// What the Immobilization TC's second cause says before the TC (D5.5, D8.11, D8.22; pass 65, ruling R65.21): the target was already Immobile, and an
    /// Original TK DR of 5 would have destroyed or Shocked it.
    /// </summary>
    public const string SecondCauseWords = "already Immobile, and an Original TK DR of 5 would have destroyed or Shocked it (D5.5)";

    /// <summary>
    /// A vehicle's Vehicle line result (A7.308, A7.309): the Kill Number, its Final DR, and any Unlikely Kill dr; since pass 65 (ruling R65.21) its crew's
    /// Immobilization TC (D5.5) or Crew Survival DR (D5.6).
    /// </summary>
    public static string VehicleLineText(FireVehicleEffect hit)
    {
        ArgumentNullException.ThrowIfNull(hit);

        // A7.308 (pass 65, ruling R65.26): more unarmored vehicles than the column's highest KIA#, each with its Random Selection dr.
        if (hit.NotSelected == true)
        {
            return $"Random Selection dr {hit.RandomSelectionDr}: not selected, so not affected by the DR (A7.308)";
        }

        return hit.KillNumber is not { } kill
            ? "unharmed by small arms (A7.307)"
            : (hit.RandomSelectionDr is { } selected ? $"Random Selection dr {selected}: selected (A7.308); " : "") + $"Kill Number {kill}, Final DR {hit.FinalDr}{string.Concat(hit.Drm.Select(item => $" ({DisplayText.Modifier(item.Name)} {(item.Value < 0 ? "-" : "+")}{Number(Math.Abs(item.Value))})"))}"
                + (hit.UnlikelyKillDr is { } dr ? $", Unlikely Kill dr {dr}" : "") + $": {hit.Result.Replace('-', ' ')}"
                + (hit.TaskCheck is { } tc ? $"; crew TC DR {tc.FinalDr} against {MoraleWords(tc)}: {(tc.Passed ? "passed" : "Abandoned")} (D5.5)" : "")
                + (hit.CrewSurvival is { } cs ? $"; Crew Survival DR {cs.FinalDr}{(cs.Drm > 0 ? " (+1)" : "")} against CS# {cs.CrewSurvival}: {(cs.Survived ? "the crew survives" : "no survivors")} (D5.6)" : "")
                + (hit.UnprotectedCrewFinalDr is not null ? "; " + CrewText(hit) : "");
    }

    /// <summary>
    /// What a vehicle's result did to its PRC (D.8, D6.66, D6.9; pass 65, rulings R65.7, R65.9), a line for each unit: the Collateral Attack it took with
    /// its DRM, its check, and what it did; a broken BU Passenger put under DM; the Survival DR of each PRC of a destroyed vehicle. Unit ids, for the
    /// view to say in its words. Empty when the record names none.
    /// </summary>
    public static IReadOnlyList<string> PrcLines(FireVehicleEffect hit, IReadOnlyList<PrcSurvival>? survival = null)
    {
        ArgumentNullException.ThrowIfNull(hit);
        var lines = new List<string>();
        foreach (var attack in hit.PrcAttacks ?? [])
        {
            var kind = attack.Kind == FirePrcAttack.Specific ? "a Specific Collateral Attack (D.8A)" : "a General Collateral Attack (D.8B)";
            var drm = string.Concat(attack.Drm.Select(item => $" ({DisplayText.Modifier(item.Name)} {(item.Value < 0 ? "-" : "+")}{Number(Math.Abs(item.Value))}, {item.Rule})"));
            foreach (var unit in attack.Effects)
            {
                lines.Add($"{unit.UnitId} aboard {hit.VehicleId} takes {kind}, Final DR {attack.FinalDr}{drm}: {ResultText(attack.Result)}"
                    + string.Concat(unit.Checks.Select(check => "; " + CheckText(check))) + $": {EffectText(unit)}");
            }
        }

        foreach (var id in hit.PrcDesperation ?? [])
        {
            lines.Add($"{id} aboard {hit.VehicleId}, broken, is under DM (D6.66, A10.62)");
        }

        lines.AddRange(PrcSurvivalLines(hit.VehicleId, survival ?? hit.PrcSurvival));
        return lines;
    }

    /// <summary>The Survival DR of each PRC of a destroyed vehicle (D6.9, D5.6; pass 65, ruling R65.9), a line each.</summary>
    public static IReadOnlyList<string> PrcSurvivalLines(string vehicleId, IReadOnlyList<PrcSurvival>? survival) =>
        [.. (survival ?? []).Select(item => $"{item.UnitId} aboard {vehicleId}: Survival DR {string.Join(", ", item.Dice)}{(item.Drm > 0 ? " +1 (broken)" : "")} = {item.FinalDr}"
            + $" against cs# {item.CrewSurvival}: {(item.Survived ? "survives, beneath the wreck" : "eliminated with the vehicle")} (D6.9)")];

    /// <summary>A vehicle crew's Collateral Attack (D.8B, D5.31): its Final DR with the CE DRM, its MC or NTC, and the result.</summary>
    public static string CrewText(FireVehicleEffect hit)
    {
        ArgumentNullException.ThrowIfNull(hit);
        return hit.CrewResult switch
        {
            FireVehicleEffect.NotVulnerable => "not Vulnerable (BU or Stunned, D5.3, D5.34)",

            // D5.311 (pass 65, ruling R65.26): an OT AFV's crew under Air Bursts is attacked as Infantry.
            _ when hit.UnprotectedCrewFinalDr is { } unprotected => $"Unprotected crew under Air Bursts, Final DR {unprotected}"
                + $"{(hit.CrewCheck is { } made ? "; " + CheckText(made) : "")}: "
                + (hit.CrewResult switch
                {
                    FireVehicleEffect.Broken => "broken, it routs beneath and the vehicle is Abandoned",
                    FireVehicleEffect.Eliminated => "eliminated, and the vehicle is Abandoned",
                    _ => hit.CrewResult,
                }) + " (D5.311)",
            _ when hit.KillNumber is not null => "no crew attack (an Inherent Driver, D5.1)",
            _ => $"Collateral Final DR {hit.FinalDr}{string.Concat(hit.Drm.Select(item => $" ({DisplayText.Modifier(item.Name)} {(item.Value < 0 ? "-" : "+")}{Number(Math.Abs(item.Value))}, {item.Rule})"))}"
                + $"{(hit.CrewCheck is { } check ? "; " + CheckText(check) : "")}: {hit.CrewResult}",
        };
    }
}
