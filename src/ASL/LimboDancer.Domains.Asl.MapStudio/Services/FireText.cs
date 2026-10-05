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

        return $"Heat of Battle DR {string.Join(", ", heat.Dice)} = {heat.OriginalDr}{ModifierText(heat.Drm)} = Final DR {heat.FinalDr}: {string.Join(", ", parts)} (A15.1)";
    }

    /// <summary>
    /// What an attack did to a MG, in words (A9.2, A9.7); null when there is nothing a player needs to be told. A MG that kept its rate of fire may
    /// fire again in the phase, which the page said nowhere before (play test P-17).
    /// </summary>
    public static string? WeaponText(FireWeaponEffect weapon)
    {
        ArgumentNullException.ThrowIfNull(weapon);
        return weapon.Malfunctioned ? $"{weapon.EquipmentId} malfunctioned (A9.7)"
            : weapon.RateOfFireRetained ? $"{weapon.EquipmentId} kept its rate of fire and may fire again this phase (A9.2)"
            : null;
    }

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
            + $" = {check.FinalDr} against {check.MoraleLevel}: {(check.Passed ? "passed" : "failed")}";
    }

    /// <summary>A vehicle's Vehicle line result (A7.308, A7.309): the Kill Number, its Final DR, and any Unlikely Kill dr.</summary>
    public static string VehicleLineText(FireVehicleEffect hit)
    {
        ArgumentNullException.ThrowIfNull(hit);
        return hit.KillNumber is not { } kill
            ? "unharmed by small arms (A7.307)"
            : $"Kill Number {kill}, Final DR {hit.FinalDr}{string.Concat(hit.Drm.Select(item => $" ({DisplayText.Modifier(item.Name)} {(item.Value < 0 ? "-" : "+")}{Number(Math.Abs(item.Value))})"))}"
                + (hit.UnlikelyKillDr is { } dr ? $", Unlikely Kill dr {dr}" : "") + $": {hit.Result.Replace('-', ' ')}";
    }

    /// <summary>A vehicle crew's Collateral Attack (D.8B, D5.31): its Final DR with the CE DRM, its MC or NTC, and the result.</summary>
    public static string CrewText(FireVehicleEffect hit)
    {
        ArgumentNullException.ThrowIfNull(hit);
        return hit.CrewResult switch
        {
            FireVehicleEffect.NotVulnerable => "not Vulnerable (BU or Stunned, D5.3, D5.34)",
            _ when hit.KillNumber is not null => "no crew attack (an Inherent Driver, D5.1)",
            _ => $"Collateral Final DR {hit.FinalDr}{string.Concat(hit.Drm.Select(item => $" ({DisplayText.Modifier(item.Name)} {(item.Value < 0 ? "-" : "+")}{Number(Math.Abs(item.Value))}, {item.Rule})"))}"
                + $"{(hit.CrewCheck is { } check ? "; " + CheckText(check) : "")}: {hit.CrewResult}",
        };
    }
}
