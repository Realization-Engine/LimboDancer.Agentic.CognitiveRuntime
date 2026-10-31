namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The participation boundary shared by Gun and mortar firing (A21.13 p. 88, C9.2-.3 p. 179, A7.35 p. 56). The primary crew's normal
/// authorization still applies. An additional SMC is a firer, never an inferred director, and contributes its own use and condition facts.
/// </summary>
public static class ScenarioA1OrdnanceParticipationCalculator
{
    /// <summary>The supported recorded reading; absent evidence remains the earlier reading.</summary>
    public const int CurrentVersion = 1;

    /// <summary>The participant selection belongs to a Gun or light mortar, not a vehicle MA, LATW or MG ordnance request.</summary>
    public static string? WeaponRefusal(string? kind) => kind is "asl:gun" or "asl:light-mortar" ? null
        : "play.ordnance-participant: this participant selection requires a Gun or light mortar (A21.13, C9.2)";

    /// <summary>A21.13's SMC classification, from the existing non-qualified-use calculator.</summary>
    public static bool Smc(string? kind) => ScenarioA1NonQualifiedUseCalculator.User(kind, 1) == CrewServedUser.LoneSmc;

    /// <summary>A7.35 p. 56 and C9.3 p. 179: an SMC cannot change from participating in one weapon to firing another, directing or spotting.</summary>
    public static string? PrimaryUseRefusal(string? kind, bool? usedOtherWeapon, bool? directedFire, bool? spotted) =>
        Smc(kind) && (usedOtherWeapon != false || directedFire != false || spotted != false)
            ? "play.ordnance-participant: a firer cannot also direct, spot, or use another weapon in this fire phase (A7.35, C9.3)" : null;

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
            yield break; // RC9P.1: A15.24's cumulative IFT DRM does not stack on this single weapon's To Hit DR.
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
            shot.Director?.UnitId, shot.Spotter?.UnitId, shot.Phase) is { } refusal)
        {
            return refusal;
        }

        var smcCount = SmcCount(primary?.Kind, participation);
        var user = ScenarioA1NonQualifiedUseCalculator.User(primary?.Kind, smcCount);
        NonQualifiedUseVerdict Use(bool captured) => ScenarioA1NonQualifiedUseCalculator.Use(new(user, captured,
            Heroic(shot, reference), Mortar(shot, gun), gun.Caliber)
        {
            Phase = shot.Phase,
            FiredThisGameTurn = participation.UseHistory?.FiredThisGameTurn ?? false,
        });
        var use = Use(participation.UseHistory?.Captured ?? false);
        // RC9P.2: older participation records leave capture and prior Game Turn use to the state verifier. Even without that
        // evidence, their multiple must be a possible calculator verdict for the actual participants, never a forged SMC count.
        if (participation.UseHistory is null && (use.Refusal is not null || use.Multiple != shot.UseMultiple))
        {
            use = Use(true);
        }
        if (use.Refusal is not null)
        {
            return use.Refusal;
        }
        if (use.Multiple != shot.UseMultiple)
        {
            return "play.ordnance-participation-use: the recorded use multiple does not match the actual participants (A21.13, C9.2)";
        }

        var lone = smcCount == 1;
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

    /// <summary>C2.5 p. 168: a turning non-vehicular NT Gun loses one ROF, or receives an IF counter if none remains; 76-82mm mortars are exempt.</summary>
    internal static (int Rof, bool IntensiveFireCounter) ConditionalRof(OrdnanceShot shot, GunDefinition gun,
        ScenarioA1OrdnanceReference reference, int rof, bool legacyArea = false)
    {
        var turned = shot.Vehicle is null && shot.HexspinesToTurn > 0;
        if (shot.Participation is null)
        {
            return (rof - (!legacyArea && turned ? 1 : 0), false);
        }
        var applies = turned && !gun.Mount360 && gun.MaType is null or "nt" && !FiresAsLight(shot, gun, reference)
            && !(Mortar(shot, gun) && gun.Caliber is >= 76 and <= 82);
        return applies ? (Math.Max(rof - 1, 0), rof <= 0) : (rof, false);
    }

    /// <summary>The declaration is authoritative; its absence keeps the earlier crew fact for recorded participants.</summary>
    internal static bool OpportunityFire(OrdnanceAdditionalFirer second) => second.OpportunityFireDeclared ?? second.Crew?.OpportunityFire == true;

    /// <summary>
    /// Why the named second SMC cannot participate. C9.2 and A21.13 require two actual SMC; A7.35 and C9.3 govern weapon use and spotting.
    /// Existing Gun restrictions decide TI, Melee and captivity. Pinned Infantry may fire, with Case D and no ROF (A7.81 p. 58, C5.4 p. 172).
    /// </summary>
    public static string? CandidateRefusal(string primaryId, string? primaryKind, string? secondKind, OrdnanceAdditionalFirer second,
        string? directorId = null, string? spotterId = null, string? phase = null)
    {
        ArgumentNullException.ThrowIfNull(second);
        var crew = second.Crew;
        if (phase == "PFPh" && OpportunityFire(second))
        {
            return "play.ordnance-participant: an Opportunity Firer cannot fire during the PFPh (A7.25)";
        }
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

        return PrimaryUseRefusal(secondKind, second.UsedOtherWeapon,
            crew.UnitId == directorId || second.DirectedFire == true ? true : second.DirectedFire,
            crew.UnitId == spotterId || second.Spotted == true ? true : second.Spotted);
    }

    /// <summary>A12.14 pp. 77-78: both participants perform the firing action; the existing visibility fact governs concealment loss.</summary>
    internal static bool Concealed(OrdnanceShot shot) => shot.Crew?.Concealed == true || shot.Participation?.Second?.Crew?.Concealed == true;

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
