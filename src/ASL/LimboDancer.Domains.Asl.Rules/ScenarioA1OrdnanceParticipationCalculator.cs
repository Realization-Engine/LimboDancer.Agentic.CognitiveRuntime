namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The participation boundary shared by Gun and mortar firing (A21.13 p. 88, C9.2 p. 179, A9.12 p. 62). The primary crew's normal
/// authorization still applies. An additional SMC is a firer, never an inferred director, and contributes its own use and condition facts.
/// </summary>
public static class ScenarioA1OrdnanceParticipationCalculator
{
    /// <summary>The supported recorded reading; absent evidence remains the earlier reading.</summary>
    public const int CurrentVersion = 1;

    /// <summary>A21.13's SMC classification, from the existing non-qualified-use calculator.</summary>
    public static bool Smc(string? kind) => ScenarioA1NonQualifiedUseCalculator.User(kind, 1) == CrewServedUser.LoneSmc;

    /// <summary>The actual SMC count, after CandidateRefusal has admitted any second participant. A directing leader is never counted.</summary>
    public static int SmcCount(string? primaryKind, OrdnanceParticipation? participation) =>
        Smc(primaryKind) ? participation?.Second is null ? 1 : 2 : 0;

    /// <summary>C9 identity for the new reading; a legacy record keeps the exact gun-type comparison used before this adapter.</summary>
    internal static bool Mortar(OrdnanceShot shot, GunDefinition gun) => shot.Participation is null
        ? gun.GunType == "mortar" : ScenarioA1MortarCalculator.IsMortar(null, gun.GunType);

    /// <summary>The effective mortar firing mode, independently of its physical Gun counter.</summary>
    internal static bool FiresAsLight(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference) =>
        Mortar(shot, gun) && (shot.Participation is null || ScenarioA1MortarCalculator.FiresAsLight(gun.Caliber,
            SmcCount(reference.Fire.Definitions.GetValueOrDefault(shot.Crew!.DefinitionId!)?.Kind, shot.Participation) == 1));

    /// <summary>A15.23's once-per-Game-Turn Gun use cannot retain a Multiple ROF. Its phase and prior use are authorized by the existing Use calculator.</summary>
    internal static bool HeroGunLimited(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference) =>
        shot.Participation is not null && SmcCount(reference.Fire.Definitions.GetValueOrDefault(shot.Crew!.DefinitionId!)?.Kind, shot.Participation) == 1
        && Heroic(shot, reference) && ScenarioA1NonQualifiedUseCalculator.HeroGunLimited(gun.Caliber);

    private static bool Heroic(OrdnanceShot shot, ScenarioA1OrdnanceReference reference) =>
        shot.HeroWeapons == true || reference.Fire.Definitions.GetValueOrDefault(shot.Crew!.DefinitionId!)?.IsHero == true;

    /// <summary>A15.23 p. 83: a hero contributes his To Hit DRM when firing the mortar as a SW, never when it is fired as a crew-served Gun.</summary>
    internal static IEnumerable<FireModifier> MortarHeroDrm(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference)
    {
        if (!FiresAsLight(shot, gun, reference))
        {
            yield break;
        }

        if (Heroic(shot, reference))
        {
            yield return new FireModifier("hero:" + shot.Crew!.UnitId, -1, "A15.23");
        }

        if (shot.Participation?.Second is { Crew: { } crew } second
            && (second.Heroic == true || reference.Fire.Definitions.GetValueOrDefault(crew.DefinitionId!)?.IsHero == true))
        {
            yield return new FireModifier("hero:" + crew.UnitId, -1, "A15.23");
        }
    }

    /// <summary>The existing Gun modifiers omitted by the old light-mortar-only Area path. Null participation never adds them.</summary>
    internal static IEnumerable<FireModifier> MortarDrm(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference)
    {
        if (shot.Participation is null)
        {
            yield break;
        }

        if (!FiresAsLight(shot, gun, reference))
        {
            if (shot.HexspinesToTurn is > 0 and var turned)
            {
                yield return new FireModifier("case-a:" + turned.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ScenarioA1ArmorCalculator.CaseA(gun.Mount360 ? "t" : gun.MaType, turned) * (shot.FirerInWoodsOrBuilding == true ? 2 : 1), "C5.1");
            }

            if (shot.IntensiveFire == true)
            {
                yield return new FireModifier("case-f", 2, "C5.61");
            }
        }

        if (shot.UseMultiple is > 0 and var multiple)
        {
            yield return new FireModifier("case-h", ScenarioA1NonQualifiedUseCalculator.CaseHDrm(multiple), "C5.8");
        }
    }

    /// <summary>Validates the opt-in reading before resolution; older records never acquire new conditions.</summary>
    internal static string? ReadingRefusal(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference)
    {
        if (shot.Participation is not { } participation)
        {
            return null;
        }

        if (participation.Version != CurrentVersion || shot.Vehicle is not null || shot.UseMultiple is null
            || gun.GunType is "latw" or ScenarioA1MachineGunKill.GunType)
        {
            return "play.ordnance-participation-reading: this Gun or mortar requires participation version 1 and its recorded use verdict";
        }

        var primary = reference.Fire.Definitions.GetValueOrDefault(shot.Crew!.DefinitionId!);
        if (participation.Second is { } second && CandidateRefusal(shot.Crew.UnitId!, primary?.Kind,
            reference.Fire.Definitions.GetValueOrDefault(second.Crew?.DefinitionId ?? string.Empty)?.Kind, second,
            shot.Director?.UnitId, shot.Spotter?.UnitId) is { } refusal)
        {
            return refusal;
        }

        var lone = SmcCount(primary?.Kind, participation) == 1;
        if (Mortar(shot, gun) && ScenarioA1MortarCalculator.Shot(new(true, gun.Caliber, gun.RangeMinimum, shot.Range,
            shot.TargetType == OrdnanceTargetTypes.Area, shot.VehicleTarget is not null, lone, Heroic(shot, reference))).Refusal is { } mortarRefusal)
        {
            return mortarRefusal;
        }

        // A15.23 p. 83: a lone hero cannot turn a NT weapon unless its M# is at least 9. Unknown M# cannot grant the exception.
        return lone && Heroic(shot, reference) && !FiresAsLight(shot, gun, reference) && !gun.Mount360 && gun.MaType is null or "nt"
            && shot.HexspinesToTurn > 0 && !(gun.Manhandling >= 9)
            ? "play.ordnance-hero-ca: a lone hero cannot change this NT weapon's CA without a known M# of at least 9 (A15.23)"
            : null;
    }

    /// <summary>C2.5 p. 168: only a NT mortar outside 76-82mm loses one ROF for turning; a 360-degree mount is T (C2.3).</summary>
    internal static bool MortarConditionalRof(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference) =>
        shot.Participation is not null && !FiresAsLight(shot, gun, reference) && !gun.Mount360 && gun.MaType is null or "nt"
        && gun.Caliber is not (>= 76 and <= 82) && shot.HexspinesToTurn > 0;

    /// <summary>
    /// Why the named second SMC cannot participate. C9.2 and A21.13 require two actual SMC; A9.12 distinguishes use from direction.
    /// Existing Gun restrictions decide TI, Melee and captivity. Pinned Infantry may fire, with Case D and no ROF (A7.81 p. 58, C5.4 p. 172).
    /// </summary>
    public static string? CandidateRefusal(string primaryId, string? primaryKind, string? secondKind, OrdnanceAdditionalFirer second,
        string? directorId = null, string? spotterId = null)
    {
        ArgumentNullException.ThrowIfNull(second);
        var crew = second.Crew;
        if (string.IsNullOrWhiteSpace(primaryId) || second.ActiveWithDefinition != true || second.SameSide != true || second.AtFirer != true
            || crew?.UnitId is not { Length: > 0 } || crew.DefinitionId is not { Length: > 0 })
        {
            return "play.ordnance-participant: the second firer must be an active defined SMC of the firing side in the same Location (A21.13, C9.2)";
        }

        if (crew.UnitId == primaryId || !Smc(primaryKind) || !Smc(secondKind))
        {
            return "play.ordnance-participant: two distinct SMC must actually fire the weapon (A21.13, C9.2)";
        }

        if (crew.Broken != false || crew.Berserk != false || crew.Pinned is null || crew.FiredInherentFp != false
            || second.Melee is null || second.Captured is null || second.Ti is null)
        {
            return "play.ordnance-participant: the second firer's condition and prior fire must permit firing this weapon (A7.1, C2.1)";
        }

        if (ScenarioA1OrdnanceEligibility.HaltedRefusal("selected Gun", crew.UnitId, false, second.Ti.Value, static () => false,
            second.Melee.Value, second.Captured.Value) is { } halted)
        {
            return halted;
        }

        return crew.UnitId == directorId || crew.UnitId == spotterId || second.UsedOtherWeapon != false || second.DirectedFire != false || second.Spotted != false
            ? "play.ordnance-participant: the second firer cannot also direct, spot, or use another weapon in this fire phase (A9.12, C9.3)"
            : null;
    }

    /// <summary>C5.4 and A7.81: either participating firer's pin applies to their shot. Absent participation preserves the primary crew alone.</summary>
    public static bool Pinned(OrdnanceShot shot)
    {
        ArgumentNullException.ThrowIfNull(shot);
        return shot.Crew?.Pinned == true || shot.Participation?.Second?.Crew?.Pinned == true;
    }

    /// <summary>A4.51: a CX participant contributes the shot's CX DRM. Absent participation preserves the primary crew alone.</summary>
    public static bool Cx(OrdnanceShot shot)
    {
        ArgumentNullException.ThrowIfNull(shot);
        return shot.Crew?.Cx == true || shot.Participation?.Second?.Crew?.Cx == true;
    }

    /// <summary>A7.7: either participating firer's Encircled condition contributes to the shot, under its existing Encircled reading.</summary>
    public static bool Encircled(OrdnanceShot shot)
    {
        ArgumentNullException.ThrowIfNull(shot);
        return shot.Crew?.Encircled == true || shot.Participation?.Second?.Crew?.Encircled == true;
    }
}
