using System.Globalization;
using LimboDancer.Domains.Asl.ScenarioA1;

namespace LimboDancer.Domains.Asl.MapStudio.Components.Play;

/// <summary>
/// A fire attack as the viewer may see it (pass 28b, moved from the Play page): the record, or the public report of one withheld from the viewer,
/// whose effects are then null.
/// </summary>
public sealed record FireView(string EventId, string Group, string Target, string? TargetSide, FireArithmetic Arithmetic,
    IReadOnlyList<FireUnitEffect>? Effects)
{
    /// <summary>The Vehicle line and Collateral Attack results of the target Location's vehicles (A7.308, D.8B).</summary>
    public IReadOnlyList<FireVehicleEffect>? Vehicles
    {
        get; init;
    }
}

/// <summary>
/// The wording of fire results (pass 28b, moved from the Play page so the fire history's components and the page's other records share it). It
/// formats what a record already holds and never recalculates an adjudication.
/// </summary>
internal static class FireText
{
    public static string Number(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    public static string ModifierText(IEnumerable<FireModifier> drm) =>
        string.Concat(drm.Select(item => $" {(item.Value < 0 ? "-" : "+")} {Number(Math.Abs(item.Value))} ({item.Name}, {item.Rule})"));

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

    /// <summary>What an attack did to a target unit, in words.</summary>
    public static string EffectText(FireUnitEffect effect)
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

        foreach (var (applies, name) in new[] { (effect.Broken, "broken"), (effect.Pinned, "pinned"), (effect.Wounded, "wounded"), (effect.Disrupted, "Disrupted"),
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
        return $"{check.Kind} {string.Join(", ", check.Dice)}{string.Concat(check.Drm.Select(item => $" {(item.Value < 0 ? "-" : "+")} {Number(Math.Abs(item.Value))} ({item.Name})"))}"
            + $" = {check.FinalDr} against {check.MoraleLevel}: {(check.Passed ? "passed" : "failed")}";
    }

    /// <summary>A vehicle's Vehicle line result (A7.308, A7.309): the Kill Number, its Final DR, and any Unlikely Kill dr.</summary>
    public static string VehicleLineText(FireVehicleEffect hit)
    {
        ArgumentNullException.ThrowIfNull(hit);
        return hit.KillNumber is not { } kill
            ? "unharmed by small arms (A7.307)"
            : $"Kill Number {kill}, Final DR {hit.FinalDr}{string.Concat(hit.Drm.Select(item => $" ({item.Name} {(item.Value < 0 ? "-" : "+")}{Number(Math.Abs(item.Value))})"))}"
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
            _ => $"Collateral Final DR {hit.FinalDr}{string.Concat(hit.Drm.Select(item => $" ({item.Name} {(item.Value < 0 ? "-" : "+")}{Number(Math.Abs(item.Value))}, {item.Rule})"))}"
                + $"{(hit.CrewCheck is { } check ? "; " + CheckText(check) : "")}: {hit.CrewResult}",
        };
    }
}
