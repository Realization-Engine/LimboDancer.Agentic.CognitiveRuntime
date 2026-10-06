namespace LimboDancer.Domains.Asl.Rules;

/// <summary>The conditions of a vehicle a fire result may set (D.7, D5.34, D5.341, A7.82).</summary>
public enum VehicleCondition
{
    Immobilized,
    Motion,
    Stunned,
    StunRecovery,
    Recalled,
    ButtonedUp,
    Pinned,
}

/// <summary>
/// The result tables and pure readings the game decides with (pass 32.a, slice S1): what a recorded dr or DR gives, what a result sets, and what a
/// recorded fact record says. Each was moved from Play or Units as it stood; the old member stays as a one-line forward.
/// </summary>
public static class ScenarioA1ResultTables
{
    private static readonly Lazy<ScenarioA1FireReference> FireReference = new(() => new ScenarioA1FirePackage().Reference);

    /// <summary>The kinds of a vehicle check (D2.5, D2.51, D8.21, D8.3) and their results, as the <c>vehicle-check-rolled</c> record names them.</summary>
    public const string BogCheck = "bog";
    public const string BogRemovalCheck = "bog-removal";
    public const string EsbCheck = "esb";
    public const string MechanicalCheck = "mechanical";
    public const string CheckPassed = "passed";
    public const string CheckBogged = "bogged";
    public const string CheckFreed = "freed";
    public const string CheckMired = "mired";
    public const string CheckImmobilized = "immobilized";

    /// <summary>The result a check's Final DR or dr gives (D2.5, D2.51, D8.21, D8.3).</summary>
    public static string VehicleCheck(string check, int final) => check switch
    {
        BogCheck => final >= 12 ? CheckBogged : CheckPassed,
        EsbCheck => final >= 12 ? CheckImmobilized : CheckPassed,
        MechanicalCheck => final >= 12 ? CheckImmobilized : CheckPassed,
        BogRemovalCheck => final <= 4 ? CheckFreed : final == 5 ? CheckMired : CheckImmobilized,
        _ => CheckPassed,
    };

    /// <summary>The results of an Interdiction NMC, as the <c>rout-interdicted</c> record names them.</summary>
    public const string InterdictionPassed = "passed";
    public const string InterdictionPinned = "pinned";
    public const string InterdictionReduced = "reduced";
    public const string InterdictionEliminated = "eliminated";

    /// <summary>The result of an Interdiction NMC (A10.53, A10.31): an Original 12 eliminates, a Final DR above the Morale Level reduces, equal pins.</summary>
    public static string RoutInterdiction(int original, int final, int morale) =>
        original == 12 ? InterdictionEliminated : final > morale ? InterdictionReduced : final == morale ? InterdictionPinned : InterdictionPassed;

    /// <summary>The results of a Shock recovery dr, as the <c>shock-recovery-rolled</c> record names them.</summary>
    public const string ShockRecovered = "recovered";
    public const string ShockUnconfirmedKill = "unconfirmed-kill";
    public const string ShockWrecked = "wrecked";

    /// <summary>
    /// The result of a dr for a Shocked AFV, or for an Unconfirmed Kill (C7.42; ruling R7.8): a Shock is removed on 1 or 2 and becomes an Unconfirmed
    /// Kill on 3 to 6; an Unconfirmed Kill is removed on 1 to 3 and wrecks the AFV on 4 to 6.
    /// </summary>
    public static string ShockRecovery(bool unconfirmedKill, int dr) => unconfirmedKill ? dr <= 3 ? ShockRecovered : ShockWrecked : dr <= 2 ? ShockRecovered : ShockUnconfirmedKill;

    /// <summary>The results of a Manhandling DR, as the <c>manhandling-rolled</c> record names them.</summary>
    public const string ManhandlingEnter = "enter";
    public const string ManhandlingEnterStop = "enter-stop";
    public const string ManhandlingStay = "stay";

    /// <summary>The result of a Final Manhandling DR against the M# (C10.3): <c>enter</c> below the M#, <c>enter-stop</c> at it, <c>stay</c> above it.</summary>
    public static string Manhandling(int final, int manhandling) => final < manhandling ? ManhandlingEnter : final == manhandling ? ManhandlingEnterStop : ManhandlingStay;

    /// <summary>Whether SMOKE grenades are placed (A24.1): the dr, plus one if the squad is CX (A4.51; table player, pass 9), is at most the Smoke Placement Exponent.</summary>
    public static bool SmokePlaced(int dr, bool cx, int exponent) => dr + (cx ? 1 : 0) <= exponent;

    /// <summary>
    /// The conditions a vehicle result sets (D.7, D5.34, D5.341, A7.82): Immobilized loses Motion; Stunned buttons up and Stops; Recalled is a Stun that
    /// removes the vehicle at the end of the Player Turn, so it is marked Recalled, not Stunned, buttoned up, and Stopped; Pinned pins. The changes are
    /// in the order the result sets them, which is the order a record writes them; empty when the result sets none.
    /// </summary>
    public static IReadOnlyList<(VehicleCondition Condition, bool Value)> VehicleConditions(FireVehicleEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        var changes = new List<(VehicleCondition Condition, bool Value)>();
        if (effect.Result == FireVehicleEffect.Immobilized)
        {
            changes.Add((VehicleCondition.Immobilized, true));
            changes.Add((VehicleCondition.Motion, false));
        }

        switch (effect.CrewResult)
        {
            case FireVehicleEffect.Stunned:
                changes.Add((VehicleCondition.Stunned, true));
                changes.Add((VehicleCondition.ButtonedUp, true));
                changes.Add((VehicleCondition.Motion, false));
                break;
            case FireVehicleEffect.Recalled:
                // A Recall is a Stun that removes the vehicle at the end of the Player Turn (D5.341); every check reads either.
                changes.Add((VehicleCondition.Recalled, true));
                changes.Add((VehicleCondition.Stunned, false));
                changes.Add((VehicleCondition.StunRecovery, false));
                changes.Add((VehicleCondition.ButtonedUp, true));
                changes.Add((VehicleCondition.Motion, false));
                break;
            case FireVehicleEffect.Pinned:
                changes.Add((VehicleCondition.Pinned, true));
                break;
        }

        return changes;
    }

    /// <summary>
    /// Whether the attack could inflict at least a NMC on a target of a group (A10.62): over every DR, with the Cowering a doubles DR brings when no
    /// leader directs, on the group's column and with the attack's DRM. The function answers for the concealed column (true) or the known one.
    /// </summary>
    public static Func<bool, bool> CouldCauseNmc(FireAttack facts, FireArithmetic arithmetic)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(arithmetic);
        var reference = FireReference.Value;
        var drm = (int)arithmetic.Drm.Sum(item => item.Value);
        var directed = facts.Director is not null;
        // Residual FP and an ordnance hit are never subject to Cowering (A8.224, C.2).
        var residual = facts.FireKind == ScenarioA1FireCalculator.ResidualFire || facts.OrdnanceHit is not null;
        bool Could(int? columnFp)
        {
            var column = columnFp is { } fp ? Array.IndexOf(ScenarioA1FireReference.ColumnFp, fp) : -1;
            if (column < 0)
            {
                return false;
            }

            for (var first = 1; first <= 6; first++)
            {
                for (var second = 1; second <= 6; second++)
                {
                    var shifted = column - (!residual && !directed && first == second ? 1 : 0);
                    if (shifted >= 0 && ScenarioA1Definitions.AtLeastNmc.Contains(reference.Result(first + second + drm, shifted)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        var known = Could(arithmetic.UnshiftedColumnFp);
        var concealed = arithmetic.Concealed is { } second ? Could(second.UnshiftedColumnFp) : known;
        return vsConcealed => vsConcealed ? concealed : known;
    }

    /// <summary>
    /// The Known units a shot leaves acquired (C6.5, C6.51): the target units that were Known or lost "?" to the shot and survived it, under
    /// the ids their Reduction or Replacement gives them; empty when there are none, and the Acquisition then stays on the Location.
    /// </summary>
    public static List<string> AcquiredUnits(OrdnanceShot facts, IReadOnlyList<FireUnitEffect> effects, string attemptId)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(effects);
        var units = new List<string>();
        foreach (var target in facts.Hit?.Targets ?? [])
        {
            if (target.Dummy == true)
            {
                continue;
            }

            var effect = effects.FirstOrDefault(item => item.UnitId == target.UnitId);
            var known = !(target.Concealed == true || target.Hidden == true) || effect?.ConcealmentLost == true;
            if (known && effect?.Eliminated != true)
            {
                units.Add(effect is not null && effect.FinalDefinitionId != effect.DefinitionId ? $"{attemptId}-{target.UnitId}" : target.UnitId!);
            }
        }

        return units;
    }

    /// <summary>
    /// What an attack does beyond its target (play test P-12, P-13): every firer's LOS is blocked, so the shot is spent for nothing (A6.1), or the
    /// target Location holds units of the firing side, in a Melee or as Guards of prisoners, which the attack hits too (A11.15, A20.54).
    /// </summary>
    public static IEnumerable<string> FireWarnings(FireAttack facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return Warnings(facts);
    }

    private static IEnumerable<string> Warnings(FireAttack facts)
    {
        if (facts.Firers is { Count: > 0 } firers && firers.All(firer => (firer.Los ?? facts.Los)?.Blocked == true))
        {
            yield return $"play.fire-los-blocked: no firer has a LOS to {facts.TargetLocationId}, so the attack has no effect and its firers are still marked as having fired (A6.11)";
        }

        // Referee, pass 31: the attack's own targets say who is hit; Defensive First Fire attacks only the moving stack (A8.1), so the firing side's
        // other units in the Location are not among them.
        // Pass 31d (design D6; A20.54, read in the PDF, p. 87): the firing side's captured units are said apart from its units in a Melee, with what
        // the rule does to them. The play test confirmed such an attack twice without reading a line that named no prisoner.
        string[] own = [.. (facts.Targets ?? []).Where(target => target.Friendly == true && target.Dummy != true && target.UnitId is not null && target.GuardId is null)
            .Select(target => target.UnitId!).Order(StringComparer.Ordinal)];
        if (own.Length > 0)
        {
            yield return $"play.fire-own-units: {string.Join(", ", own)} of the firing side {(own.Length == 1 ? "is" : "are")} in {facts.TargetLocationId} and {(own.Length == 1 ? "is" : "are")} attacked too (A11.15, A20.54)";
        }

        string[] captured = [.. (facts.Targets ?? []).Where(target => target.Friendly == true && target.Dummy != true && target.UnitId is not null && target.GuardId is not null)
            .Select(target => target.UnitId!).Order(StringComparer.Ordinal)];
        if (captured.Length > 0)
        {
            yield return captured.Length == 1
                ? $"play.fire-own-units: {captured[0]}, a captured unit of the firing side, is in {facts.TargetLocationId} and is attacked with its Guard, as if in a Melee: if it fails a MC it is Reduced, "
                    + "and if its own side's fire eliminates it, it counts double for the Victory Conditions (A20.54)"
                : $"play.fire-own-units: {string.Join(", ", captured)}, captured units of the firing side, are in {facts.TargetLocationId} and are attacked with their Guard, as if in a Melee: "
                    + "one that fails a MC is Reduced, and one that its own side's fire eliminates counts double for the Victory Conditions (A20.54)";
        }
    }

    /// <summary>What fires in a firer's attack, each counted on its own (A8.3, A9.2): the unit when it uses its own FP, and each weapon it fires.</summary>
    public static IEnumerable<string> FiringParts(FireFirer firer)
    {
        ArgumentNullException.ThrowIfNull(firer);
        return (firer.UsesInherentFp == false || firer.UnitId is null ? [] : new[] { firer.UnitId }).Concat(firer.Weapons?.Select(weapon => weapon.EquipmentId).OfType<string>() ?? []);
    }

    /// <summary>
    /// A refusal for range says what was out of range (pass 31c, design section 14; A7.21, A7.22): the package's one sentence for
    /// <c>out-of-range</c> gives way to a line for each firer and weapon, with its range, its Normal Range, and how far it fires. The code stays.
    /// A refusal records nothing, so no recorded game reads this.
    /// </summary>
    public static (string[] Reasons, IReadOnlyList<string> Named) RangeNamed(FireAttack part, string[] reasons)
    {
        ArgumentNullException.ThrowIfNull(part);
        ArgumentNullException.ThrowIfNull(reasons);
        const string code = "asl.a1.fire.out-of-range";
        if (!reasons.Any(reason => reason.StartsWith(code, StringComparison.Ordinal)) || part.TargetLocationId is not { } target)
        {
            return (reasons, []);
        }

        var named = new List<string>();
        foreach (var firer in part.Firers ?? [])
        {
            if ((firer.Range ?? part.Range) is not { } range || firer.LocationId is not { } at)
            {
                continue;
            }

            var own = at == target;
            var levelAbove = firer.TargetLevelAbove ?? part.TargetLevelAbove ?? 0;
            void Say(string who, FireDefinition? definition, bool wounded)
            {
                if (definition is null || FireRange.Band(definition, range, own, levelAbove, wounded) is not { } reading)
                {
                    return;
                }

                if (reading.Band == FireRangeBand.Out)
                {
                    named.Add($"{code}: {who} in {at} is {range} {(range == 1 ? "hex" : "hexes")} from {target}; its Normal Range is {reading.NormalRange}, so it fires to {reading.Limit} "
                        + (reading.Limit == reading.NormalRange ? "(C13.24: an ATR has no Long Range)" : "(A7.22)"));
                }
                else if (reading.Band == FireRangeBand.SameHexOtherLevel)
                {
                    named.Add($"{code}: {who} in {at} fires at {target}, another level of its own hex, which is not built (A7.21)");
                }
            }

            if (firer.UsesInherentFp != false)
            {
                Say(firer.UnitId!, FireReference.Value.Definitions.GetValueOrDefault(firer.DefinitionId ?? string.Empty), firer.Wounded == true);
            }

            foreach (var weapon in firer.Weapons ?? [])
            {
                Say($"{weapon.EquipmentId} of {firer.UnitId}", FireReference.Value.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty), false);
            }
        }

        named = [.. named.Distinct(StringComparer.Ordinal)];
        return named.Count == 0 ? (reasons, [])
            : ([.. reasons.SelectMany(reason => reason.StartsWith(code, StringComparison.Ordinal) ? named : [reason]).Distinct(StringComparer.Ordinal)], named);
    }

    /// <summary>
    /// MF to enter the admitted terrain, in half MF: Open Ground and orchard 1 (A4.13; B14.4), brush and woods 2 (B12.4, B13.4), grain 1½ (B15.4),
    /// a building 2 (B23.4); entry across a road hexside 1 (A4.132, B3.4); rubble 3 (B24.4; ruling R10.1).
    /// </summary>
    public static IReadOnlyDictionary<string, int> EntryHalfMf
    {
        get;
    } = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["open-ground"] = 2,
        ["orchard"] = 2,
        ["brush"] = 4,
        ["woods"] = 4,
        ["grain"] = 3,
        ["wooden-building"] = 4,
        ["stone-building"] = 4,
        ["wooden-rubble"] = 6,
        ["stone-rubble"] = 6,
    };

    /// <summary>The Terrain Chart's MP Entrance Cost (p. 698) in half MP, by movement type: Fully Tracked, Halftrack, Truck (ruling R11.7).</summary>
    public static IReadOnlyDictionary<(string MovementType, string Terrain), int> VehicleTerrainHalfMp
    {
        get;
    } = new Dictionary<(string MovementType, string Terrain), int>
    {
        [("fully-tracked", "open-ground")] = 2,
        [("half-tracked", "open-ground")] = 2,
        [("truck", "open-ground")] = 8,
        [("fully-tracked", "grain")] = 2,
        [("half-tracked", "grain")] = 2,
        [("truck", "grain")] = 10,
        [("fully-tracked", "brush")] = 4,
        [("half-tracked", "brush")] = 4,
        [("truck", "brush")] = 12,
    };

    /// <summary>Whether a broken unit carries more PP than its IPC (A10.4; ruling R31d.1), and so leaves a SW before it routs.</summary>
    public static bool RoutLaden(int totalPp, int ipc) => totalPp > ipc;

    /// <summary>
    /// The best rout loads that differ to a player (the table player, pass 31d): two that differ only in which of two like counters is kept are one
    /// choice, since a SW has no name of its own. The first of each is given. <paramref name="kindOf"/> gives a SW's kind by its id.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<string>> RoutLoadChoices(IReadOnlyList<IReadOnlyList<string>> bestLoads, Func<string, string> kindOf)
    {
        ArgumentNullException.ThrowIfNull(bestLoads);
        ArgumentNullException.ThrowIfNull(kindOf);
        return [.. bestLoads.GroupBy(best => string.Join("|", best.Select(kindOf).Order(StringComparer.Ordinal)), StringComparer.Ordinal).Select(group => group.First())];
    }

    /// <summary>
    /// Whether the Player Turn ending now is the game's last (A3.9; ruling R20.1): the second of the card's last Game Turn, or its first when the card
    /// gives that Game Turn only one Player Turn.
    /// </summary>
    public static bool EndsAfter(int turnCount, bool halfTurn, int turn, bool firstSidePhasing) => turn >= turnCount && (!firstSidePhasing || halfTurn);

    /// <summary>The order a group sets up in (R17.12): its own, else 1 for the side that sets up first and 2 for the other.</summary>
    public static int SetupOrder(int? groupOrder, bool sideSetsUpFirst) => groupOrder ?? (sideSetsUpFirst ? 1 : 2);

    /// <summary>
    /// Which entry area an OB line enters by (ruling R20.5), as an index into the group's entry areas ordered by turn: the one the line names, or, when
    /// it names none, the first when the group has an area whose SSR fixes its counters or every area of the group is an entry; null when the line sets
    /// up on board.
    /// </summary>
    public static int? EntryAreaIndex(IReadOnlyList<string> entryAreaIds, string? named, bool anyAreaFixesCounters, bool allAreasEnter)
    {
        ArgumentNullException.ThrowIfNull(entryAreaIds);
        if (named is not null)
        {
            var index = entryAreaIds.ToList().IndexOf(named);
            return index < 0 ? null : index;
        }

        return entryAreaIds.Count > 0 && (anyAreaFixesCounters || allAreasEnter) ? 0 : null;
    }

    /// <summary>Setup is complete when every group that sets up on board has finished and every entering counter waits off board (rulings R19.2, R20.5).</summary>
    public static bool SetupComplete(IEnumerable<(bool SetsUp, bool Complete, int OffBoard)> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        return groups.All(group => (!group.SetsUp || group.Complete) && group.OffBoard == 0);
    }

    /// <summary>Whether a card is a minimal card (ruling R22.3): sides, none of which has an OB group.</summary>
    public static bool CardIsMinimal(int sides, IEnumerable<int> groupsPerSide)
    {
        ArgumentNullException.ThrowIfNull(groupsPerSide);
        return sides > 0 && groupsPerSide.All(count => count == 0);
    }
}
