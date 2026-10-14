"""Pass 45: the reviews' fixes in Rules, Play, and Units. Every anchor is checked before anything is written."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
R = "LimboDancer.Domains.Asl.Rules/"
P = "LimboDancer.Domains.Asl.Play/"
EDITS = {
    R + "ScenarioA1FireRuleModels.cs": [
        ("Func<IReadOnlyList<(bool FinalFire, bool FirstFire)>> Marks, bool NoneFiresAlone, bool AnyFiresAlone);",
         "Func<IReadOnlyList<(bool FinalFire, bool FirstFire)>> Marks, bool NoneFiresAlone, bool AnyFiresAlone,\n    bool EveryFirstFireMarkedFiresAlone = true);"),
    ],
    R + "ScenarioA1FireEligibility.cs": [
        # Referee 1, 1 (BLOCKER): First Fire at mark 1 only when every marked firer fires its MG alone.
        ("""            1 => facts.AnyFiresAlone ? ScenarioA1FireCalculator.FirstFire : ScenarioA1FireCalculator.SubsequentFirstFire,
""",
         """            // A9.2: a MG fired again alone on its Multiple ROF by a unit marked First Fire is First Fire still. Review of pass 45: only when every
            // unit marked First Fire fires so; one that fires its own FP uses Subsequent First Fire, whoever else fires a MG alone beside it.
            1 => facts.AnyFiresAlone && facts.EveryFirstFireMarkedFiresAlone ? ScenarioA1FireCalculator.FirstFire : ScenarioA1FireCalculator.SubsequentFirstFire,
"""),
        # Referee 1, 2: the "every MG" bar binds the firers that use Subsequent First Fire or FPF.
        ("""            && attack.Firers!.Any(item => !(item.Weapons?.Select(weapon => weapon.EquipmentId!).Order(StringComparer.Ordinal).ToArray() ?? [])
                .SequenceEqual(possessed(item.UnitId!)))
""",
         """            // Review of pass 45 (ruling R45.1): the rule binds "a unit [that] uses Subsequent First Fire"; a unit with no counter that fires First
            // Fire in the group "could use either its inherent FP or the LMG" (the example of p. 62).
            && attack.Firers!.Any(item => (item.FirstFireMarked == true || item.FinalFireMarked == true || (item.Weapons ?? []).Any(weapon => weapon.FirstFireMarked == true))
                && !(item.Weapons?.Select(weapon => weapon.EquipmentId!).Order(StringComparer.Ordinal).ToArray() ?? [])
                    .SequenceEqual(possessed(item.UnitId!)))
"""),
        # Boundary 7, referee 1, 6: the four conditions of C6.43 and the dm weapon are Rules' to judge.
        ("""    public static bool MgBoreSighted(IEnumerable<(bool FiresAlone, IReadOnlyList<bool> WeaponsSighted)> firers)
    {
        ArgumentNullException.ThrowIfNull(firers);
        var all = firers.ToArray();
        return all.Length > 0 && all.All(firer => firer.FiresAlone && firer.WeaponsSighted.Count > 0 && firer.WeaponsSighted.All(sighted => sighted));
    }
""",
         """    public static bool MgBoreSighted(IEnumerable<(bool FiresAlone, IReadOnlyList<MgSightingFacts> Weapons)> firers)
    {
        ArgumentNullException.ThrowIfNull(firers);
        var all = firers.ToArray();

        // C6.43 (p. 174): not "fired by other than its original ... manning-Infantry", and lost if the weapon "leaves the setup Location" or "is dm".
        return all.Length > 0 && all.All(firer => firer.FiresAlone && firer.Weapons.Count > 0
            && firer.Weapons.All(weapon => weapon.Sighted && weapon.ByItsPossessor && weapon.AtItsSetupLocation && weapon.OnTheTarget && !weapon.Dismantled));
    }

    /// <summary>A9.8 (p. 65; pass 45): "A dm SW may not be fired until reassembled": the refusal of a dismantled mortar's shot, or null.</summary>
    public static string? DismantledShotBar(bool dismantled, string weapon) =>
        dismantled ? $"play.ordnance-dismantled: {weapon} is dismantled and is not fired until it is assembled (A9.8)" : null;

    /// <summary>
    /// The definition a dismantled German MMG or HMG fires as (A9.8): its side's LMG, the first by id where the catalog holds more than one; null
    /// when it holds none.
    /// </summary>
    public static string? LmgDefinition(IEnumerable<FireDefinition> definitions, string? nationality)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        return definitions.Where(item => item.IsMg && item.Size == "light" && item.Nationality == nationality).OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault()?.Id;
    }
"""),
        ("""            : $"play.fire-field: {weapon} has fired this phase from woods, rubble, or a building, and fires again only inside the CA of that shot (A9.21)";
""",
         """            : $"play.fire-field: {weapon} has fired this phase from woods, rubble, or a building, and fires again only inside the CA of that shot: the 60-degree wedge that holds its earlier target, and its own Location (A9.21)";
"""),
    ],
    R + "ScenarioA1FireModels.cs": [
        ("""/// <summary>The MOL Check of an attack (A22.611, A22.6111; ruling R15.4)""",
         """/// <summary>
/// One MG of an attack against its Bore Sighting (C6.42, C6.43; pass 45): whether it has one, whether its firer is the unit that set up with it,
/// whether it stands in its setup Location, whether the sighting is on the target Location, and whether it is dismantled.
/// </summary>
public sealed record MgSightingFacts(bool Sighted, bool ByItsPossessor, bool AtItsSetupLocation, bool OnTheTarget, bool Dismantled);

/// <summary>The MOL Check of an attack (A22.611, A22.6111; ruling R15.4)"""),
    ],
    R + "ScenarioA1GameStart.cs": [
        ("""    public static string? MgBoreSightBar(string? size, bool scenarioDefenders, bool ownHex, Func<bool> losClearWithinNormalRange, string mg)
    {
        ArgumentNullException.ThrowIfNull(losClearWithinNormalRange);
        return size is not ("medium" or "heavy") || !scenarioDefenders || ownHex || !losClearWithinNormalRange()
""",
         """    public static string? MgBoreSightBar(string? size, bool scenarioDefenders, bool ownHex, int? normalRange, Func<(bool Clear, int Range)?> los, string mg)
    {
        ArgumentNullException.ThrowIfNull(los);
        return size is not ("medium" or "heavy") || !scenarioDefenders || ownHex || normalRange is not { } normal
            || los() is not { Clear: true } read || read.Range > Math.Min(16, normal)
"""),
    ],
    R + "ScenarioA1MandatoryFire.cs": [
        ("""    public static string OtherActBar(IReadOnlyList<string> owed, string location, string act)
    {
        ArgumentNullException.ThrowIfNull(owed);
        return $"play.act-mandatory: {string.Join(", ", owed)} must first attack the units that entered {location}; the {act} waits for that attack (A8.312)";
    }
""",
         """    public static string? OtherActBar(bool mph, bool namesDefender, Func<(IReadOnlyList<string> Owed, string Location)?> owing, string act)
    {
        ArgumentNullException.ThrowIfNull(owing);

        // The bar is on the DEFENDER's proposal alone, so the attack owed is read only for one.
        return mph && namesDefender && owing() is { Owed.Count: > 0 } owed
            ? $"play.act-mandatory: {string.Join(", ", owed.Owed)} must first attack the units that entered {owed.Location}; the {act} waits for that attack (A8.312)"
            : null;
    }
"""),
    ],
    R + "ScenarioA1UpperLevelEncirclement.cs": [
        ("""                if (seen.Count < SearchLimit)
                {
                    seen.Add(next);
                    open.Enqueue(next);
                }
            }
        }

        return true;
""",
         """                if (seen.Count >= SearchLimit)
                {
                    // A search cut short has not shown that no path exists, so it calls no unit Encircled.
                    return false;
                }

                seen.Add(next);
                open.Enqueue(next);
            }
        }

        return true;
"""),
    ],
    R + "ScenarioA1ResultTables.cs": [
        ("""                else if (reading.Band is FireRangeBand.SameHexOtherLevel or FireRangeBand.SameHexFartherLevel)
""",
         """                else if (reading.Band is FireRangeBand.SameHexFartherLevel)
"""),
    ],
    R + "FireRange.cs": [
        ("""            : range < 1 ? Math.Abs(levelAbove) == 1 && !noPointBlank ? FireRangeBand.SameHexOtherLevel : FireRangeBand.SameHexFartherLevel
""",
         """            // A FT, which is never raised for PBF, fires at the next level at its own FP.
            : range < 1 ? Math.Abs(levelAbove) != 1 ? FireRangeBand.SameHexFartherLevel : noPointBlank ? FireRangeBand.Normal : FireRangeBand.SameHexOtherLevel
"""),
    ],
    R + "ScenarioA1FireCalculator.cs": [
        # Referee 1, 3: the crew's own kind, not the attack's.
        ("""                attack.FireKind == FinalProtectiveFire || OwnKind(attack, item) == SubsequentFirstFire || IsFinalFireAgain(attack, item))))
""",
         """                OwnKind(attack, item) is FinalProtectiveFire or SubsequentFirstFire || IsFinalFireAgain(attack, item))))
"""),
        # Referee 1, 4: a leader who has directed does not direct a unit with no counter.
        ("""                || (director.DirectedThisPlayerTurn == true && !redirect
""",
         """                // A7.53 (review of pass 45; the example of p. 62: "leader direction is NA; 7.53"): his later direction "can only include firers he
                // directed during First Fire", which a unit with no counter in a mixed group is not.
                || (director.DirectedThisPlayerTurn == true && kind is SubsequentFirstFire or FinalProtectiveFire && firers.Any(item => !UsesSubsequentFire(attack, item)))
                || (director.DirectedThisPlayerTurn == true && !redirect
"""),
        # Referee 1, 5: a Dummy is Infantry to the firer.
        ("""                && (attack.FireKind == FirstFire || !state.Values.Any(unit => !unit.IsDummy)))
""",
         """                && (attack.FireKind == FirstFire || state.Count == 0))
"""),
        # Referee 2, 2: only the causes A.11 lists make an X#; weather lowers B# and X# alike.
        ("""                var removed = attack.CoweringMarksAndWeaponResidual == true && !definition.IsFt && malfunctioned.Contains(id) && breakdown[id] < printed && original >= printed;
""",
         """                var lowered = sustained || weapon.Captured == true || AtrInexperience(weapon) > 0;
                var removed = attack.CoweringMarksAndWeaponResidual == true && !definition.IsFt && malfunctioned.Contains(id) && lowered
                    && original >= printed - (attack.BreakdownReduction ?? 0);
"""),
        # Referee 2, 7: an ATR is set apart once.
        ("""            decimal Fp(Func<FireWeaponEffect, bool> which) => arithmetic.Firers.Where(item => item.VsConcealed != true && weapons.Any(weapon => weapon.EquipmentId == item.UnitId && which(weapon)))
                .Sum(item => item.Firepower);
""",
         """            // An ATR leaves no Residual FP in any case (C13.24), and Residual sets it apart itself.
            bool Atr(string id) => (attack.Firers ?? []).SelectMany(firer => firer.Weapons ?? [])
                .Any(weapon => weapon.EquipmentId == id && reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsAtr == true);
            decimal Fp(Func<FireWeaponEffect, bool> which) => arithmetic.Firers.Where(item => item.VsConcealed != true && !Atr(item.UnitId)
                    && weapons.Any(weapon => weapon.EquipmentId == item.UnitId && which(weapon)))
                .Sum(item => item.Firepower);
"""),
        ("""            var rest = Array.FindLastIndex(columns, fp => fp <= arithmetic.TotalFirepower - apart) - (withheld == 0 ? 0 : arithmetic.ColumnShift);
""",
         """            var rest = Array.FindLastIndex(columns, fp => fp <= arithmetic.TotalFirepower - apart)
                - (withheld != 0 || (atr != 0 && attack.CoweringMarksAndWeaponResidual == true) ? arithmetic.ColumnShift : 0);
"""),
        # Referee 2, 6: a hero is a SMC.
        ("""Any(pair => pair.Second is { IsLeader: true } && pair.First.Partner is null
""",
         """Any(pair => (pair.Second is { IsLeader: true } || pair.Second is { IsHero: true }) && pair.First.Partner is null
"""),
        # Referee 2, 9: only against Personnel, and one halving with a Gunflash.
        ("""                    if (attack.CoweringMarksAndWeaponResidual == true && mg.IsMg && range >= 17 && !vsConcealed)
""",
         """                    if (attack.CoweringMarksAndWeaponResidual == true && mg.IsMg && range >= 17 && !vsConcealed && attack.BeyondNvr != true && state.Count > 0)
"""),
        # Referee 2, 1: the firer's own FP joins a spray as it joins any attack.
        ("""        bool Sprays(FireFirer item, FireDefinition? definition) => item.Weapons is { } used
            && used.All(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty) is { IsMg: true })
            && (item.UsesInherentFp == false ? used.Count > 0
                : definition is { Kind: "asl:squad", SprayingFire: true } && RangeOf(attack, item) <= 3);
""",
         """        // A7.351, A7.352: a squad firing two SW, and a HS or hero firing one, add no FP of their own, as in any attack.
        bool AddsOwnFp(FireFirer item, FireDefinition? definition) => item.UsesInherentFp != false
            && !(definition?.Kind == "asl:squad" && item.Weapons!.Count >= 2) && !((definition?.Kind == "asl:half-squad" || definition?.IsHero == true) && item.Weapons!.Count >= 1);
        bool Sprays(FireFirer item, FireDefinition? definition) => item.Weapons is { } used
            && used.All(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty) is { IsMg: true })
            && (!AddsOwnFp(item, definition) ? used.Count > 0
                : definition is { Kind: "asl:squad", SprayingFire: true } && RangeOf(attack, item) <= 3);
"""),
    ],
    R + "ScenarioA1FireFollowUps.cs": [
        ("""    public static bool RofForfeitOpen(bool mph, bool nothingSince, int? forfeitedFp, int weaponsStillKeeping) =>
        mph && nothingSince && forfeitedFp is > 0 && weaponsStillKeeping > 0;

    /// <summary>Why the ROF may not be given up.</summary>
    public static string RofForfeitClosedText() =>
        "play.forfeit-rof: a weapon's Multiple ROF is given up for Residual FP only at once after its Defensive First Fire attack, when that leaves more Residual FP (A8.23)";

    /// <summary>The choice in words.</summary>
    public static string RofForfeitText(IReadOnlyList<string> weapons, int fp, string location)
    {
        ArgumentNullException.ThrowIfNull(weapons);
        return $"play.forfeit-rof: {string.Join(", ", weapons)} gives up its Multiple ROF and is marked First Fire; {fp} Residual FP stands in {location} (A8.23)";
    }
""",
         """    // A8.21 (review of pass 45): and only when the counter it would leave is larger than the one that stands in the Location.
    public static bool RofForfeitOpen(bool mph, bool nothingSince, int? forfeitedFp, int weaponsStillKeeping, int? standingFp = null) =>
        mph && nothingSince && forfeitedFp is { } more && more > (standingFp ?? 0) && weaponsStillKeeping > 0;

    /// <summary>Whether a weapon the attack's record says kept its Multiple ROF keeps it still: in play, with no fire counter, and working.</summary>
    public static bool StillKeepsRof(bool active, bool firstFire, bool finalFire, bool malfunctioned) => active && !firstFire && !finalFire && !malfunctioned;

    /// <summary>
    /// Whether an event after an attack leaves the choice open (A8.23: "immediately after resolving its attack"): one the attack caused, or one
    /// written with it in the same act (a Sniper Check on its DR).
    /// </summary>
    public static bool PartOfTheAttack(bool causedByIt, bool ofItsAct) => causedByIt || ofItsAct;

    /// <summary>The counter a weapon takes that gives its Multiple ROF up in the MPh: First Fire, as if it had lost it on the DR (A9.2).</summary>
    public static UnitCondition RofForfeitCounter => UnitCondition.FirstFire;

    /// <summary>Why the ROF may not be given up.</summary>
    public static string RofForfeitClosedText() =>
        "play.forfeit-rof: a weapon's Multiple ROF is given up for Residual FP only immediately after its Defensive First Fire attack, before anything else is done, and only when that leaves more Residual FP than stands in the Location (A8.23, A8.21)";

    /// <summary>The choice in words.</summary>
    public static string RofForfeitText(IReadOnlyList<string> weapons, int fp, string location)
    {
        ArgumentNullException.ThrowIfNull(weapons);
        return weapons.Count == 1
            ? $"play.forfeit-rof: {weapons[0]} gives up its Multiple ROF and is marked First Fire; {fp} Residual FP stands in {location} (A8.23)"
            : $"play.forfeit-rof: {string.Join(", ", weapons)} give up their Multiple ROF and are marked First Fire; {fp} Residual FP stands in {location} (A8.23)";
    }
"""),
        ("""    public static bool ResidualAttacksAgain(int fp, int drm, IEnumerable<(int Fp, int Drm)> earlier)
    {
        ArgumentNullException.ThrowIfNull(earlier);
        return earlier.All(attack => fp > attack.Fp || drm < attack.Drm);
    }
""",
         """    public static bool ResidualAttacksAgain(int fp, int drm, IEnumerable<(int Fp, int Drm)> earlier)
    {
        ArgumentNullException.ThrowIfNull(earlier);
        return earlier.All(attack => fp > attack.Fp || drm < attack.Drm);
    }

    /// <summary>
    /// The same for the units an attack would hit together (A8.22 speaks of "a unit"): the Residual FP attacks when any one of them has not yet
    /// taken it at this strength and DRM. <paramref name="earlierByUnit"/> has each unit's earlier attacks in the Location.
    /// </summary>
    public static bool ResidualAttacksAgain(int fp, int drm, IEnumerable<IReadOnlyList<(int Fp, int Drm)>> earlierByUnit)
    {
        ArgumentNullException.ThrowIfNull(earlierByUnit);
        return earlierByUnit.Any(earlier => ResidualAttacksAgain(fp, drm, earlier));
    }

    /// <summary>The DRM of an attack, summed as A8.22 compares it.</summary>
    public static int TotalDrm(IEnumerable<decimal> drm)
    {
        ArgumentNullException.ThrowIfNull(drm);
        return (int)drm.Sum();
    }
"""),
    ],
    R + "ScenarioA1MovementModels.cs": [
        ("""    int MfSpent,
    bool HalfMfSpent,
    IReadOnlyList<string>? MovedWith);
""",
         """    int MfSpent,
    bool HalfMfSpent,
    IReadOnlyList<string>? MovedWith,
    bool Cx = false);
"""),
    ],
    R + "ScenarioA1MovementCalculator.cs": [
        ("""        var leader = recipient is null ? null : movers.FirstOrDefault(item => item.Leader && !item.Broken && !item.Wounded
            && item.Nationality == recipient.Nationality);
""",
         """        // A4.52 (review of pass 45): the leader that lends is one that is not CX; another leader of the stack that is fresh still lends.
        var leader = recipient is null ? null : movers.FirstOrDefault(item => item.Leader && !item.Broken && !item.Wounded && !item.Cx
            && item.Nationality == recipient.Nationality);
"""),
    ],
    P + "GamePlanner.Movement.cs": [
        ("""            Is(unit, Conditions.Berserk), Is(unit, Conditions.Broken), Is(unit, Conditions.Wounded), Nationality(unit), unit.MfSpent, unit.HalfMfSpent, unit.MovedWith);
""",
         """            Is(unit, Conditions.Berserk), Is(unit, Conditions.Broken), Is(unit, Conditions.Wounded), Nationality(unit), unit.MfSpent, unit.HalfMfSpent, unit.MovedWith,
            Is(unit, Conditions.Cx) || unit.DoubleTimeMf > 0);
"""),
    ],
    P + "GamePlanner.Terrain.cs": [
        ("""        ScenarioA1MovementCalculator.LeaderIpcRecipient([.. movers.Select(MovingUnit)], index => Laden(state, movers[index]), chosen,
            doubleTime || movers.Any(unit => vocabulary.IsA(unit.Kind, "asl:leader") && (unit.DoubleTimeMf > 0 || Is(unit, Conditions.Cx))));
""",
         """        ScenarioA1MovementCalculator.LeaderIpcRecipient([.. movers.Select(MovingUnit)], index => Laden(state, movers[index]), chosen, doubleTime);
"""),
    ],
    P + "LiveFire.cs": [
        ("""            firers.All(item => withoutInherent?.Contains(item.Unit.Id) != true), withoutInherent is { Count: > 0 }));
""",
         """            firers.All(item => withoutInherent?.Contains(item.Unit.Id) != true), withoutInherent is { Count: > 0 },
            firers.Where(item => Is(item.Unit, Conditions.FirstFire)).All(item => withoutInherent?.Contains(item.Unit.Id) == true)));
"""),
        ("""        var boreSighted = state.BoreSights.Count > 0 && ScenarioA1FireEligibility.MgBoreSighted(firerFacts.Select(firer => (firer.UsesInherentFp == false,
            (IReadOnlyList<bool>)[.. (firer.Weapons ?? []).Select(weapon => state.BoreSights.Any(sight => sight.Gun == weapon.EquipmentId && sight.Crew == firer.UnitId
                && sight.Location == target && state.Location(firer.UnitId!)?.Location == sight.SetupLocation))])));
""",
         """        var boreSighted = state.BoreSights.Count > 0 && ScenarioA1FireEligibility.MgBoreSighted(firerFacts.Select(firer => (firer.UsesInherentFp == false,
            (IReadOnlyList<MgSightingFacts>)[.. (firer.Weapons ?? []).Select(weapon => state.BoreSights.FirstOrDefault(sight => sight.Gun == weapon.EquipmentId) is { } sight
                ? new MgSightingFacts(true, sight.Crew == firer.UnitId, state.Location(firer.UnitId!)?.Location == sight.SetupLocation, sight.Location == target,
                    state.Find(weapon.EquipmentId!) is EquipmentInstance sighted && Is(sighted, Conditions.Dismantled))
                : new MgSightingFacts(false, false, false, false, false))])));
"""),
        ("""        var nationality = CatalogReference.Value.Definitions[own].Nationality;
        return CatalogReference.Value.Definitions.Values.Where(item => item.IsMg && item.Size == "light" && item.Nationality == nationality)
            .OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault()?.Id ?? own;
""",
         """        return ScenarioA1FireEligibility.LmgDefinition(CatalogReference.Value.Definitions.Values, CatalogReference.Value.Definitions[own].Nationality) ?? own;
"""),
    ],
    P + "GamePlanner.cs": [
        ("""                    if (ScenarioA1GameStart.MgBoreSightBar(mg?.Size, sightState.Unit(possession.Holder)?.Side == sightState.ScenarioDefender,
                        boreAt.Board == mgAt.Board && boreAt.Hex == mgAt.Hex,
                        () => Los(sightState, mgAt, boreAt) is { Status: LosStatus.Clear, Range: var range } && range <= Math.Min(16, mg?.Range ?? 0), boreGun) is { } mgBar)
""",
         """                    if (ScenarioA1GameStart.MgBoreSightBar(mg?.Size, sightState.Unit(possession.Holder)?.Side == sightState.ScenarioDefender,
                        boreAt.Board == mgAt.Board && boreAt.Hex == mgAt.Hex, mg?.Range,
                        () => Los(sightState, mgAt, boreAt) is { } sightLos ? (sightLos.Status == LosStatus.Clear, sightLos.Range) : null, boreGun) is { } mgBar)
"""),
    ],
    P + "GamePlanner.MandatoryFire.cs": [
        ("""        bool Defenders(string id) => state.Unit(id) is { } unit ? unit.Side != state.PhasingSide
            : state.Find(id) is EquipmentInstance { Holding.Holder: { } holder } && state.Unit(holder) is { } possessor && possessor.Side != state.PhasingSide;
        var named = arguments.EnumerateObject().SelectMany(property => property.Value.ValueKind switch
""",
         """        bool Defenders(string id) => state.Unit(id) is { } unit ? unit.Side != state.PhasingSide
            : state.Find(id) is EquipmentInstance { Holding.Holder: { } holder } && state.Unit(holder) is { } possessor && possessor.Side != state.PhasingSide;

        // Review of pass 45: the acting units and weapons, never a target: an ATTACKER's shot at a DEFENDER's vehicle must not be told who owes.
        var named = arguments.EnumerateObject().Where(property => !property.Name.StartsWith("target", StringComparison.Ordinal)).SelectMany(property => property.Value.ValueKind switch
"""),
        ("""        return named.Any(Defenders) && MandatoryFire(state, existing) is { } owed
            ? Refused(scope, label, expected, ScenarioA1MandatoryFire.OtherActBar(owed.Firers, owed.At.ToString(), act))
            : null;
""",
         """        return ScenarioA1MandatoryFire.OtherActBar(true, named.Any(Defenders),
            () => MandatoryFire(state, existing) is { } owed ? (owed.Firers, owed.At.ToString()) : null, act) is { } bar
            ? Refused(scope, label, expected, bar)
            : null;
"""),
        ("""        existing.Count > 0 && Text(arguments, "gunId", out var gunId) && Replay(existing).Current?.Find(gunId) is EquipmentInstance weapon && Is(weapon, Conditions.Dismantled)
            ? Refused(scope, label, expected, $"play.ordnance-dismantled: {gunId} is dismantled and is not fired until it is assembled (A9.8)")
            : null;
""",
         """        existing.Count > 0 && Text(arguments, "gunId", out var gunId) && Replay(existing).Current?.Find(gunId) is EquipmentInstance weapon
            && ScenarioA1FireEligibility.DismantledShotBar(Is(weapon, Conditions.Dismantled), gunId) is { } bar
            ? Refused(scope, label, expected, bar)
            : null;
"""),
    ],
    P + "GamePlanner.RateOfFire.cs": [
        ("""        var latest = existing.Skip(index + 1).All(later => later.Causes.Contains(fire, StringComparer.Ordinal));
        string[] still = [.. kept.Where(id => state.Find(id) is EquipmentInstance { Status: InstanceStatus.Active } weapon && !Is(weapon, Conditions.FirstFire)
            && !Is(weapon, Conditions.FinalFire) && !Is(weapon, Conditions.Malfunctioned))];
        return ScenarioA1FireFollowUps.RofForfeitOpen(state.Phase == "mph", latest, forfeited, still.Length) ? new RofForfeit(fire, target!, forfeited!.Value, still) : null;
""",
         """        var act = fire[..(fire.LastIndexOf('-') + 1)];
        var latest = existing.Skip(index + 1).All(later => ScenarioA1FireFollowUps.PartOfTheAttack(later.Causes.Contains(fire, StringComparer.Ordinal),
            later.EventId.StartsWith(act, StringComparison.Ordinal)));
        string[] still = [.. kept.Where(id => state.Find(id) is EquipmentInstance weapon && ScenarioA1FireFollowUps.StillKeepsRof(weapon.Status == InstanceStatus.Active,
            Is(weapon, Conditions.FirstFire), Is(weapon, Conditions.FinalFire), Is(weapon, Conditions.Malfunctioned)))];
        return ScenarioA1FireFollowUps.RofForfeitOpen(state.Phase == "mph", latest, forfeited, still.Length, state.ResidualFire.FirstOrDefault(item => item.Location == target)?.Fp)
            ? new RofForfeit(fire, target!, forfeited!.Value, still) : null;
"""),
        ("""                new ConditionsChanged(weapon, ConditionChanges([(UnitCondition.FirstFire, true)])), ScenarioA1FirePackage.Identity.ToString(), null, [forfeit.Fire]));
        }

        // A8.21: only a larger counter replaces one in the Location.
        if (!state.ResidualFire.Any(item => item.Location == forfeit.At && item.Fp >= forfeit.Fp))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "residual-fp-placed", new ResidualFirePlaced(forfeit.Fire, forfeit.At, forfeit.Fp),
                ScenarioA1FirePackage.Identity.ToString(), null, [forfeit.Fire]));
        }
""",
         """                new ConditionsChanged(weapon, ConditionChanges([(ScenarioA1FireFollowUps.RofForfeitCounter, true)])), ScenarioA1FirePackage.Identity.ToString(), null, [forfeit.Fire]));
        }

        // A8.21: the offer is open only when this counter is larger than the one that stands there, so it is always placed.
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "residual-fp-placed", new ResidualFirePlaced(forfeit.Fire, forfeit.At, forfeit.Fp),
            ScenarioA1FirePackage.Identity.ToString(), null, [forfeit.Fire]));
"""),
    ],
    P + "GamePlanner.Fire.cs": [
        # Boundary 1, referee 2, 3 and 12: a shot within the MG's own hex has no bearing; its earlier shots are found by the weapon.
        ("""            foreach (var weapon in firer.Weapons!)
            {
                double[] earlier = [.. ThisPhase(existing).Select(item => item.Payload).OfType<FireResolved>()
                    .Where(record => record.FirerLocation == firer.LocationId && record.TargetLocation != firer.LocationId
                        && record.Resolution.TryGetProperty("weaponEffects", out var effects) && effects.ValueKind == JsonValueKind.Array
                        && effects.EnumerateArray().Any(effect => effect.TryGetProperty("equipmentId", out var id) && id.GetString() == weapon.EquipmentId))
                    .Select(record => BoardLocation.TryParse(record.TargetLocation, out var was) ? Bearing(state, mgAt!, was!) : null).OfType<double>()];
                if (ScenarioA1FireEligibility.FieldOfFireBar(weapon.DefinitionId is { } mgNamed ? FireReference.Value.Definitions.GetValueOrDefault(mgNamed)?.Size : null,
                    TerrainKey(mgRead), earlier, Bearing(state, mgAt!, target), weapon.EquipmentId!) is { } fieldBar)
""",
         """            // A shot within the MG's own hex (its own Location, or up or down its stairwell) has no bearing: the CA "includes the MG's own Location".
            double? BearingFrom(BoardLocation to) => to.Board == mgAt!.Board && to.Hex == mgAt.Hex ? null : Bearing(state, mgAt, to);
            foreach (var weapon in firer.Weapons!)
            {
                double[] earlier = [.. ThisPhase(existing).Select(item => item.Payload).OfType<FireResolved>()
                    .Where(record => record.Resolution.TryGetProperty("weaponEffects", out var effects) && effects.ValueKind == JsonValueKind.Array
                        && effects.EnumerateArray().Any(effect => effect.TryGetProperty("equipmentId", out var id) && id.GetString() == weapon.EquipmentId))
                    .Select(record => BoardLocation.TryParse(record.TargetLocation, out var was) ? BearingFrom(was!) : null).OfType<double>()];
                if (ScenarioA1FireEligibility.FieldOfFireBar(weapon.DefinitionId is { } mgNamed ? FireReference.Value.Definitions.GetValueOrDefault(mgNamed)?.Size : null,
                    TerrainKey(mgRead), earlier, BearingFrom(target), weapon.EquipmentId!) is { } fieldBar)
"""),
        # Referee 1, 7: Residual FP reads the A7.72 Encirclement too.
        ("""            return (ScenarioA1FireMapRules.ResidualMapFacts(attack, terrain, SmokeSources(state).Count(place => place == target)), null);
""",
         """            return UpperLevelEncircled(state, (ScenarioA1FireMapRules.ResidualMapFacts(attack, terrain, SmokeSources(state).Count(place => place == target)), null));
"""),
    ],
    P + "GamePlanner.Vehicles.cs": [
        ("""    private static int ResidualDrm(FireAttack attack) =>
        (int)(ScenarioA1FireCalculator.Preview(attack, FireReference.Value)?.Drm.Sum(item => item.Value) ?? 0m);
""",
         """    private static int ResidualDrm(FireAttack attack) =>
        ScenarioA1FireFollowUps.TotalDrm(ScenarioA1FireCalculator.Preview(attack, FireReference.Value)?.Drm.Select(item => item.Value) ?? []);
"""),
        ("""    private static List<(int Fp, int Drm)> EarlierResidualAttacks(IReadOnlyList<GameEvent> existing, BoardLocation at, FireAttack planned)
    {
        string[] hit = [.. (planned.Targets ?? []).Select(item => item.UnitId!), .. (planned.Vehicles ?? []).Select(item => item.VehicleId!)];
        var earlier = new List<(int, int)>();
""",
         """    private static List<IReadOnlyList<(int Fp, int Drm)>> EarlierResidualAttacks(IReadOnlyList<GameEvent> existing, BoardLocation at, FireAttack planned)
    {
        string[] hit = [.. (planned.Targets ?? []).Select(item => item.UnitId!), .. (planned.Vehicles ?? []).Select(item => item.VehicleId!)];
        var earlier = hit.ToDictionary(id => id, _ => new List<(int Fp, int Drm)>(), StringComparer.Ordinal);
"""),
        ("""            string[] then = [.. (recorded.Targets ?? []).Select(item => item.UnitId!), .. (recorded.Vehicles ?? []).Select(item => item.VehicleId!)];
            if (hit.Length > 0 && hit.All(id => then.Contains(id, StringComparer.Ordinal)))
            {
                var drm = record.Resolution.TryGetProperty("arithmetic", out var arithmetic) && arithmetic.TryGetProperty("drm", out var list) && list.ValueKind == JsonValueKind.Array
                    ? (int)list.EnumerateArray().Sum(item => item.TryGetProperty("value", out var value) ? value.GetDecimal() : 0m) : 0;
                earlier.Add((fp, drm));
            }
        }

        return earlier;
""",
         """            string[] then = [.. (recorded.Targets ?? []).Select(item => item.UnitId!), .. (recorded.Vehicles ?? []).Select(item => item.VehicleId!)];
            var drm = ScenarioA1FireFollowUps.TotalDrm(record.Resolution.TryGetProperty("arithmetic", out var arithmetic) && arithmetic.TryGetProperty("drm", out var list)
                && list.ValueKind == JsonValueKind.Array ? list.EnumerateArray().Select(item => item.TryGetProperty("value", out var value) ? value.GetDecimal() : 0m) : []);
            foreach (var id in hit.Where(id => then.Contains(id, StringComparer.Ordinal)))
            {
                earlier[id].Add((fp, drm));
            }
        }

        // A8.22 speaks of "a unit": each one's own earlier attacks, for Rules to judge.
        return [.. hit.Select(id => (IReadOnlyList<(int Fp, int Drm)>)earlier[id])];
"""),
    ],
    P + "GamePlanner.SupportWeapons.cs": [
        ("""            return Refused(scope, label, expected, $"play.recover-unit: '{unitId}' is not unpinned Good Order Infantry on the map (A4.44)");
""",
         """            return Refused(scope, label, expected, state.Unit(unitId) is { } tied && Is(tied, "asl:ti") && GoodOrder(tied)
                ? $"play.recover-unit: '{unitId}' is TI and may not Recover a SW in the MPh, which costs a MF (A4.8)"
                : $"play.recover-unit: '{unitId}' is not unpinned Good Order Infantry on the map (A4.44)");
"""),
        ("""            return Refused(scope, label, expected, $"play.dismantle-weapon: '{unitId}' is not a Good Order unit possessing '{equipmentId}' (A9.8)");
""",
         """            return Refused(scope, label, expected, state.Unit(unitId) is { } tied && Is(tied, "asl:ti") && GoodOrder(tied)
                ? $"play.dismantle-weapon: '{unitId}' is TI and may not dismantle or assemble a SW, which counts as its use (A4.8, A9.8)"
                : $"play.dismantle-weapon: '{unitId}' is not a Good Order unit possessing '{equipmentId}' (A9.8)");
"""),
        ("""$"play.dismantle-weapon: {weapon.Id} is not dismantled: only a light mortar of four PP or more and a non-Russian MMG or HMG are (A9.8)");""",
         """$"play.dismantle-weapon: {weapon.Id} is not a weapon that is dismantled or assembled: only a light mortar of four PP or more and a non-Russian MMG or HMG are (A9.8)");"""),
    ],
    P + "GameActions.cs": [
        ("A dismantled weapon portages at half its PP and is not fired, but a German MMG or HMG, which fires as a LMG (A9.8).",
         "A dismantled weapon portages at half its PP and is not fired, except a German MMG or HMG, which fires as a LMG (A9.8)."),
    ],
    P + "RefusalReasons.cs": [
        ("""Spraying Fire is by MGs, and by squads whose Range is underscored at three hexes or less; every member of the group must be capable of it, in a fire phase and never at its own Location (A7.34, A9.5, A9.52)""",
         """Spraying Fire is by MGs, and by squads whose Range is underscored, to a range of three hexes; every member of the group must be capable of it, in the PFPh, AFPh, or DFPh, and never at its own Location (A7.34, A9.5, A9.52)"""),
    ],
    "LimboDancer.Domains.Asl.Units/State/GameProjector.cs": [
        # Referee 2, 5: the exemption is for the later shots of weapons that kept their ROF from an earlier record of the phase.
        ("""                fire.Facts.TryGetProperty("firers", out var firingUnits) && firingUnits.ValueKind == JsonValueKind.Array && firingUnits.GetArrayLength() > 0
                    && firingUnits.EnumerateArray().All(item => item.TryGetProperty("usesInherentFp", out var own) && own.ValueKind == JsonValueKind.False)) is { } refused)
""",
         """                fire.Facts.TryGetProperty("firers", out var firingUnits) && firingUnits.ValueKind == JsonValueKind.Array && firingUnits.GetArrayLength() > 0
                    && firingUnits.EnumerateArray().All(item => item.TryGetProperty("usesInherentFp", out var own) && own.ValueKind == JsonValueKind.False
                        && item.TryGetProperty("weapons", out var used) && used.ValueKind == JsonValueKind.Array && used.GetArrayLength() > 0
                        && used.EnumerateArray().All(weapon => weapon.TryGetProperty("equipmentId", out var usedId) && KeptRofThisPhase(state, usedId.GetString())))) is { } refused)
"""),
        ("""        /// <summary>Residual FP a fire record left (A8.2, A8.21): the record's own value, in its target Location, in the MPh.</summary>
""",
         """        /// <summary>Whether an earlier fire record of this phase says the weapon kept its Multiple ROF (A9.2), so that this is one of its later shots.</summary>
        private bool KeptRofThisPhase(GameState state, string? weapon) => weapon is not null && state.FiresThisPhase.Any(item => fires.TryGetValue(item.EventId, out var earlier)
            && earlier.Fire.Resolution.TryGetProperty("weaponEffects", out var effects) && effects.ValueKind == JsonValueKind.Array
            && effects.EnumerateArray().Any(effect => effect.TryGetProperty("equipmentId", out var id) && id.GetString() == weapon
                && effect.TryGetProperty("rateOfFireRetained", out var kept) && kept.ValueKind == JsonValueKind.True));

        /// <summary>Residual FP a fire record left (A8.2, A8.21): the record's own value, in its target Location, in the MPh.</summary>
"""),
        # Referee 2, 10; boundary 4: the second figure only when the weapons that kept their ROF carry a fire counter.
        ("""            var forfeitedFp = recorded is not null && recorded.Resolution.TryGetProperty("arithmetic", out var withRof) && withRof.TryGetProperty("residualFpIfForfeited", out var forfeit)
                ? forfeit.GetInt32() : (int?)null;
""",
         """            var givenUp = recorded is not null && recorded.Resolution.TryGetProperty("weaponEffects", out var kept) && kept.ValueKind == JsonValueKind.Array
                && kept.EnumerateArray().Where(effect => effect.TryGetProperty("rateOfFireRetained", out var retained) && retained.ValueKind == JsonValueKind.True)
                    .All(effect => effect.TryGetProperty("equipmentId", out var id) && Active(state, id.GetString()!) is { } weapon
                        && (GameState.Condition(weapon, Conditions.FirstFire) == ConditionState.True || GameState.Condition(weapon, Conditions.FinalFire) == ConditionState.True));
            var forfeitedFp = givenUp && recorded!.Resolution.TryGetProperty("arithmetic", out var withRof) && withRof.TryGetProperty("residualFpIfForfeited", out var forfeit)
                ? forfeit.GetInt32() : (int?)null;
"""),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:90]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
