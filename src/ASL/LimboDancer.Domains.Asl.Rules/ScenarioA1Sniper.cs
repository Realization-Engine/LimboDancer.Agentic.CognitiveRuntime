namespace LimboDancer.Domains.Asl.Rules;

/// <summary>What a Sniper dr of 1 or 2 does to the unit it picks (A14.3, A17.11; ruling R15.5).</summary>
public enum SniperEffect
{
    /// <summary>A SMC on dr 1, or a HS that cannot be Reduced: eliminated.</summary>
    Eliminated,

    /// <summary>A SMC on dr 2: a Wound Severity dr decides between wounded and eliminated.</summary>
    WoundSeverity,

    /// <summary>A MMC on dr 1 that can break: broken, under DM, no longer pinned.</summary>
    Broken,

    /// <summary>A broken or berserk MMC on dr 1: Casualty Reduced, a squad to its HS, a HS eliminated.</summary>
    CasualtyReduction,

    /// <summary>A MMC on dr 2 not broken, berserk, or pinned: pinned.</summary>
    Pinned,

    /// <summary>No effect.</summary>
    None,
}

/// <summary>A hex holding eligible Sniper targets as the target choice reads it (A14.21): its index in the caller's list, its distance from the Random Location (null when unread), its lowest TEM, and its name.</summary>
public sealed record SniperTargetHex(int Index, int? Distance, int Tem, string Hex);

/// <summary>A Location of the chosen hex as the Location choice reads it (ruling R15.5): its index, how many eligible targets it holds, and its level.</summary>
public sealed record SniperTargetLocation(int Index, int Count, int Level);

/// <summary>
/// A vehicle of the attacked side as A14.22 reads it (pass 71, ruling R71.3): unarmored or Partially Armored, whether its crew is CE, whether it is manned
/// (not Abandoned, its crew not broken), and the exposure of each Passenger and Rider aboard (<see cref="ScenarioA1PrcRules.Exposure"/>).
/// </summary>
public sealed record SniperVehicleFacts(bool Unarmored, bool PartiallyArmored, bool CrewExposed, bool Manned, IReadOnlyList<string> PrcExposures);

/// <summary>Who in or of a vehicle is a Sniper target (A14.22, A14.33): the indexes of its Vulnerable PRC, its Inherent crew, and the vehicle itself.</summary>
public sealed record SniperVehicleTargets(IReadOnlyList<int> Prc, bool Crew, bool Vehicle);

/// <summary>
/// A unit in a Sniper attack's target Location as A14.4 reads it (pass 71, ruling R71.10): Personnel on foot (not a Dummy, not aboard a vehicle), in Good
/// Order (A.7), pinned, TI, Unarmed, hidden, and whether it has fired or moved this Player Turn.
/// </summary>
public sealed record SniperCheckCandidate(bool PersonnelOnFoot, bool GoodOrder, bool Pinned, bool Ti, bool Unarmed, bool Hidden, bool FiredOrMoved);

/// <summary>
/// A participant in a Sniper Check as its DRM reads it (A14.4; ruling R71.10): its kind, a leader's leadership modifier as it now applies (a wounded
/// leader's one worse, A17.2), and whether a leader is heroic (A15.21).
/// </summary>
public sealed record SniperCheckParticipant(string Kind, int? Leadership, bool Heroic);

/// <summary>
/// Snipers (A14; backlog pass 15, ruling R15.5; pass 32.h, S9): which DRs call for a Sniper attack, the Sniper dr, the Random Location and the target it finds,
/// Random Selection among the targets, and the results. Play reads the records, the state, and the map, draws the dice, and writes the events.
/// </summary>
public static class ScenarioA1Sniper
{
    public const string NoEffect = "none";
    public const string NoTarget = "no-target";
    public const string DummyStackEliminated = "dummy-stack-eliminated";
    public const string Eliminated = "eliminated";
    public const string Wounded = "wounded";
    public const string Broken = "broken";
    public const string CasualtyReduced = "casualty-reduced";
    public const string Pinned = "pinned";

    // Pass 71 (ruling R71.5, R71.6): the results against a vehicle's crew, an unarmored vehicle, and the enemy Sniper counter (A14.3, A14.31, A14.33, D5.34,
    // D5.341, D5.342).
    public const string Stunned = "stunned";
    public const string Recalled = "recalled";
    public const string CrewBroken = "crew-broken";
    public const string CrewPinned = "crew-pinned";
    public const string Immobilized = "immobilized";
    public const string SniperEliminated = "sniper-eliminated";
    public const string SniperPinned = "sniper-pinned";

    /// <summary>What a Sniper target is (A14.2 EXC, A14.22; ruling R71.3): a unit, a vehicle's Inherent crew, an unarmored vehicle itself, or the enemy Sniper counter.</summary>
    public const string UnitTarget = "unit";
    public const string CrewTarget = "crew";
    public const string VehicleTarget = "vehicle";
    public const string SniperTarget = "sniper";

    /// <summary>A14.1: Sniper attacks follow the DRs of the PFPh, MPh, DFPh, and AFPh.</summary>
    public static bool TriggerPhase(string? phase) => phase is "pfph" or "mph" or "dfph" or "afph";

    /// <summary>A fire record's roll key, split into its kind and its unit; pass 31 (ruling R31.5): a unit's second Leader Loss check, keyed "unit#2", is the same unit's DR.</summary>
    public static (string Kind, string UnitId) RollKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var split = key.IndexOf(':', StringComparison.Ordinal);
        var (kind, unitId) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
        if (unitId.IndexOf('#', StringComparison.Ordinal) is var numbered and > 0)
        {
            unitId = unitId[..numbered];
        }

        return (kind, unitId);
    }

    /// <summary>
    /// A14.1: whose DR a roll is: an IFT DR is its firing side's; a MC or TC DR (a check, a leader loss check, a berserk NTC, a crew's) its unit's, never a
    /// prisoner's; any other roll is no side's. <paramref name="checker"/> reads the unit's side and whether it is captured, only for a check.
    /// </summary>
    public static string? RollMaker(string kind, string? firingSide, Func<(string? Side, bool Captured)?> checker)
    {
        ArgumentNullException.ThrowIfNull(checker);
        return kind switch
        {
            "attack" => firingSide,
            "checks" or "leaderLoss" or "berserkCheck" or "crewCheck" when checker() is { Captured: false } found => found.Side,
            _ => null,
        };
    }

    /// <summary>E1.76 (backlog pass 16, ruling R16.7): at night each side's SAN is two higher, to at most 7.</summary>
    public static int SniperActivationNumber(int printed, bool night) => night ? Math.Min(printed + 2, 7) : printed;

    /// <summary>
    /// The actual SAN (A14.4, E1.76, pp. 83, 225; pass 71, ruling R71.9): a Sniper eliminated "reduces that side's SAN by one for the duration of the
    /// scenario"; by day the printed SAN less the reductions; at night the printed SAN plus two (to at most 7) less the reductions, which "can never be
    /// reduced below the printed (or recorded) SAN".
    /// </summary>
    public static int ActualSan(int printed, bool night, int reductions) =>
        night ? Math.Max(printed, SniperActivationNumber(printed, true) - reductions) : printed - reductions;

    /// <summary>A14.1 (p. 82; ruling R71.9): "If a SAN is (reduced to) &lt; 2, that Sniper counter is removed from play and that side may make no (further) Sniper attacks."</summary>
    public static bool CounterRemoved(int actualSan) => actualSan < 2;

    /// <summary>A14.1: a two-dice DR equal to the enemy's SAN calls for the Sniper attack.</summary>
    public static bool Triggers(int sum, int printedSan, bool night) => Triggers(sum, printedSan, night, 0);

    /// <summary>A14.1 (pass 71, ruling R71.9): a two-dice DR equal to the enemy's actual SAN, after its reductions, calls for the Sniper attack.</summary>
    public static bool Triggers(int sum, int printedSan, bool night, int reductions) => sum == ActualSan(printedSan, night, reductions);

    /// <summary>The causes of a SAN reduction (A14.4; ruling R71.9): a Sniper Check, or an enemy Sniper's attack, that eliminates the Sniper.</summary>
    public const string SniperCheckCause = "sniper-check";
    public const string SniperAttackCause = "sniper-attack";

    /// <summary>The projector's check of a recorded SAN reduction (A14.4; ruling R71.9): the side has a SAN, and the cause is one of the two.</summary>
    public static RecordRefusal? VerifySanReduced(bool sideHasSan, string cause) =>
        !sideHasSan || cause is not (SniperCheckCause or SniperAttackCause)
            ? new RecordRefusal("UNIT-STATE-042", "A SAN reduction names a side with a SAN and a Sniper Check or a Sniper attack as its cause (A14.4).")
            : null;

    /// <summary>
    /// A14.4 (p. 82; pass 71, ruling R71.10): "any unpinned, non-TI, armed Good Order Infantry ... unit in the target Location which has not yet fired/moved
    /// during that Player Turn" may take the Sniper Check. A hidden unit is left out, so the combined check never costs a unit its hidden status unasked.
    /// </summary>
    public static bool MayTakeSniperCheck(SniperCheckCandidate unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return unit.PersonnelOnFoot && unit.GoodOrder && !unit.Pinned && !unit.Ti && !unit.Unarmed && !unit.Hidden && !unit.FiredOrMoved;
    }

    /// <summary>
    /// The Sniper Check DRM (A14.4, p. 83; ruling R71.10), the most benefit the participants can have, with its causes: the leadership of one leader directing
    /// any unit other than himself, none for a leader alone ("a leader cannot modify his own DR", the EX); -1 each hero; -1 each crew or HS equivalent, a squad
    /// two. A heroic leader gives his leadership or his hero's -1, never both (A15.21).
    /// </summary>
    public static (int Drm, IReadOnlyList<string> Causes) SniperCheckDrm(IReadOnlyList<SniperCheckParticipant> participants)
    {
        ArgumentNullException.ThrowIfNull(participants);
        var heroes = participants.Count(unit => unit.Kind == "asl:hero");
        var halfSquads = participants.Sum(unit => ScenarioA1OrdnanceProjection.HalfSquadEquivalents(unit.Kind));
        var heroicLeaders = participants.Count(unit => unit.Leadership is not null && unit.Heroic);
        var (drm, director) = (-heroes - halfSquads - heroicLeaders, (SniperCheckParticipant?)null);
        if (participants.Count > 1)
        {
            foreach (var leader in participants.Where(unit => unit.Leadership is not null))
            {
                var directed = -heroes - halfSquads - heroicLeaders + (leader.Heroic ? 1 : 0) + leader.Leadership!.Value;
                if (directed < drm)
                {
                    (drm, director) = (directed, leader);
                }
            }
        }

        var causes = new List<string>();
        if (director is not null)
        {
            causes.Add($"leadership {director.Leadership:+0;-0}");
        }

        if (heroes + heroicLeaders - (director?.Heroic == true ? 1 : 0) is > 0 and var heroic)
        {
            causes.Add($"{heroic} hero{(heroic == 1 ? string.Empty : "es")} -{heroic}");
        }

        if (halfSquads > 0)
        {
            causes.Add($"{halfSquads} HS equivalent{(halfSquads == 1 ? string.Empty : "s")} -{halfSquads}");
        }

        return (drm, causes);
    }

    /// <summary>A14.4 (p. 82; ruling R71.10): a Sniper Check Final DR of 2 or less eliminates the Sniper, a 3 pins it (no further effect on one already pinned).</summary>
    public static string SniperCheckResult(int finalDr, bool pinned) => finalDr <= 2 ? SniperEliminated : finalDr == 3 && !pinned ? SniperPinned : NoEffect;

    /// <summary>
    /// What a Sniper Check does to each participant (A14.4, A12.141; ruling R71.10): it becomes TI, and loses its "?" and hidden status when it is in the LOS
    /// of a Good Order enemy ground unit within 16 hexes (A12.141 names "make a Sniper Check").
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> SniperCheckConditions(bool losesConcealment) =>
        losesConcealment ? [(UnitCondition.Ti, true), (UnitCondition.Concealed, false), (UnitCondition.Hidden, false)] : [(UnitCondition.Ti, true)];

    /// <summary>The projector's check of a recorded Sniper Check (A14.4; ruling R71.10): its Sniper and units in play, its DR recorded, and its result the DR's.</summary>
    public static RecordRefusal? VerifySniperCheck(bool sniperFound, bool unitsActive, IReadOnlyList<int>? dice, int drm, bool pinned, string result) =>
        !sniperFound || !unitsActive || dice is not { Count: 2 } || SniperCheckResult(dice.Sum() + drm, pinned) != result
            ? new RecordRefusal("UNIT-STATE-042", "A Sniper Check names a Sniper counter and units in play, its recorded DR, and the result of that DR (A14.4).")
            : null;

    /// <summary>The words of a pending Sniper Check (A14.4; ruling R71.10), by the target Location's hex.</summary>
    public static string SniperCheckChoiceText(string side, string sniperSide, string hex) =>
        $"the {side} units in {hex} that may take it make a Sniper Check against the {sniperSide} Sniper, all of them in one combined DR, and become TI: a Final DR of 2 or less eliminates the Sniper, a 3 pins it (A14.4)";

    /// <summary>A14.1, A8.2: the side that made a fire record's IFT DR: its firers', a DC's user's, or the DEFENDER's for Residual FP; each read when the one before gives none.</summary>
    public static string? FiringSide(bool hasFirers, Func<string?> firstFirerSide, Func<string?> demolitionChargeUserSide, Func<bool> residualFire, Func<string?> defenderSide)
    {
        ArgumentNullException.ThrowIfNull(firstFirerSide);
        ArgumentNullException.ThrowIfNull(demolitionChargeUserSide);
        ArgumentNullException.ThrowIfNull(residualFire);
        ArgumentNullException.ThrowIfNull(defenderSide);
        if (hasFirers)
        {
            return firstFirerSide();
        }

        if (demolitionChargeUserSide() is { } userSide)
        {
            return userSide;
        }

        return residualFire() ? defenderSide() : null;
    }

    /// <summary>A14.31: a pinned Sniper makes no further attack this Player Turn.</summary>
    public static bool Attacks(bool pinned) => !pinned;

    /// <summary>A14.3: the Sniper dr attacks only on 1 or 2.</summary>
    public static bool Effective(int dr) => dr <= 2;

    /// <summary>A14.2 (ruling R15.5): the Random Location DR: the colored dr a hexside direction (1 the top hexside, clockwise, as 0 to 5), the white dr the hexes along it.</summary>
    public static (int Direction, int Extent) RandomLocation(int colored, int white) => (colored - 1, white);

    /// <summary>
    /// A14.22 (ruling R15.5; pass 71, ruling R71.3): who may be a target at all: the attacked side's units and vehicles on the map, not hidden, not prisoners.
    /// A unit on foot is then a target; a vehicle, and a unit inside one, as <see cref="VehicleTargets"/> says.
    /// </summary>
    public static bool Eligible(bool active, bool attackedSide, bool hidden, bool captured, bool onMap) => active && attackedSide && !hidden && !captured && onMap;

    /// <summary>
    /// A14.22, A14.33 (p. 82; pass 71, rulings R71.3, R71.4): "All units are eligible Sniper targets except AFV with no Vulnerable PRC"; PRC "are Vulnerable
    /// to Sniper attack if they are CE and/or occupy a Partially Armored AFV or unarmored vehicle": a Passenger or Rider whose exposure is open or exposed,
    /// or any aboard a Partially Armored AFV; the Inherent crew of a manned vehicle when CE, Partially Armored, or unarmored; and a manned or loaded unarmored
    /// vehicle itself, a separate target, so it and its PRC are never attacked by the same dr. An empty unarmored vehicle is no target (R71.4).
    /// </summary>
    public static SniperVehicleTargets VehicleTargets(SniperVehicleFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        int[] prc = [.. Enumerable.Range(0, facts.PrcExposures.Count)
            .Where(index => facts.PartiallyArmored || facts.PrcExposures[index] is ScenarioA1PrcRules.Open or ScenarioA1PrcRules.Exposed)];
        var crew = facts.Manned && (facts.CrewExposed || facts.PartiallyArmored || facts.Unarmored);
        return new SniperVehicleTargets(prc, crew, facts.Unarmored && (facts.Manned || facts.PrcExposures.Count > 0));
    }

    /// <summary>The TEM of a target hex's terrain, for the tie between hexes (A14.21).</summary>
    public static int TargetTem(string? terrain) => terrain is not null ? ScenarioA1FireReference.Tem.GetValueOrDefault(terrain) : 0;

    /// <summary>
    /// A14.21 (p. 82; pass 71, ruling R71.7): "the lowest (to a minimum of zero) in-hex TEM/SMOKE DRM applicable to any eligible target": a target's TEM
    /// plus the in-hex SMOKE DRM of its hex, +2 a source to at most +3 (A24.2).
    /// </summary>
    public static int TargetTem(string? terrain, int smokeSources) =>
        Math.Max(0, TargetTem(terrain) + (smokeSources > 0 ? ScenarioA1VehicleSightRules.SmokeDrm(smokeSources, false, true, static () => false) : 0));

    /// <summary>A14.21: the target hex, or the closest hex holding an eligible target; ties go to the lowest TEM, then the first hex by name; an unread distance is the farthest.</summary>
    public static int TargetHex(IReadOnlyList<SniperTargetHex> hexes)
    {
        ArgumentNullException.ThrowIfNull(hexes);
        return hexes.OrderBy(item => item.Distance ?? int.MaxValue).ThenBy(item => item.Tem).ThenBy(item => item.Hex, StringComparer.Ordinal).First().Index;
    }

    /// <summary>Ruling R15.5: of several Locations in the hex, the one holding the most eligible targets, then the lowest.</summary>
    public static int TargetLocation(IReadOnlyList<SniperTargetLocation> locations)
    {
        ArgumentNullException.ThrowIfNull(locations);
        return locations.OrderByDescending(item => item.Count).ThenBy(item => item.Level).First().Index;
    }

    /// <summary>A14.23: a concealed stack is one possible target; the others one each. The targets come in id order, each with whether it is concealed or a Dummy.</summary>
    public static IReadOnlyList<IReadOnlyList<int>> Candidates(IReadOnlyList<bool> concealedOrDummy)
    {
        ArgumentNullException.ThrowIfNull(concealedOrDummy);
        var candidates = Enumerable.Range(0, concealedOrDummy.Count).Where(index => !concealedOrDummy[index]).Select(index => (IReadOnlyList<int>)[index]).ToList();
        int[] hidden = [.. Enumerable.Range(0, concealedOrDummy.Count).Where(index => concealedOrDummy[index])];
        if (hidden.Length > 0)
        {
            candidates.Add(hidden);
        }

        return candidates;
    }

    /// <summary>A.9: Random Selection: the candidates whose die shows the highest value, in order; the first takes the Sniper dr, each other a new Sniper dr of its own (A14.2).</summary>
    public static IReadOnlyList<int> Tied(IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var highest = values.Max();
        return [.. Enumerable.Range(0, values.Count).Where(index => values[index] == highest)];
    }

    /// <summary>A.9: among several real units of a concealed stack, the first whose die shows the highest value.</summary>
    public static int Picked(IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Array.IndexOf([.. values], values.Max());
    }

    /// <summary>
    /// A14.3 (ruling R15.5): dr 1 eliminates a SMC and breaks a MMC, Casualty Reducing one that cannot break (broken or berserk); dr 2 wounds a SMC (its Wound
    /// Severity dr, A17.11) and pins a MMC not immune to Pin results.
    /// </summary>
    public static SniperEffect UnitEffect(bool smc, int dr, bool broken, bool berserk, bool pinned) =>
        smc && dr == 1 ? SniperEffect.Eliminated
        : smc ? SniperEffect.WoundSeverity
        : dr == 1 && !broken && !berserk ? SniperEffect.Broken
        : dr == 1 ? SniperEffect.CasualtyReduction
        : !broken && !berserk && !pinned ? SniperEffect.Pinned
        : SniperEffect.None;

    /// <summary>A17.11: a Wound Severity dr of 5 or more (+1 for a unit already wounded) is mortal.</summary>
    public static bool Mortal(int severityDr, bool alreadyWounded) => ScenarioA1Wounds.Mortal(severityDr, alreadyWounded);

    /// <summary>A7.302: a squad with a HS definition is Reduced to it; any other unit is eliminated.</summary>
    public static string ReductionResult(bool squadWithHalfSquad) => squadWithHalfSquad ? CasualtyReduced : Eliminated;

    /// <summary>A unit the attack wounds loses its "?" and hidden status (A12.14).</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> WoundedConditions() => [(UnitCondition.Wounded, true), (UnitCondition.Concealed, false), (UnitCondition.Hidden, false)];

    /// <summary>A unit the attack breaks goes under DM, is no longer pinned, and loses its "?" and hidden status.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> BrokenConditions() =>
        [(UnitCondition.Broken, true), (UnitCondition.Pinned, false), (UnitCondition.DesperationMorale, true), (UnitCondition.Concealed, false), (UnitCondition.Hidden, false)];

    /// <summary>A unit the attack pins loses its "?" and hidden status, but one that keeps its "?" by A12.14 (pass 71, ruling R71.8).</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> PinnedConditions(bool keepsConcealment) =>
        keepsConcealment ? [(UnitCondition.Pinned, true)] : [(UnitCondition.Pinned, true), (UnitCondition.Concealed, false), (UnitCondition.Hidden, false)];

    /// <summary>
    /// A12.14 (p. 77; pass 71, ruling R71.8): a concealed unit "is attacked by a Sniper (dr 1 or 2; 14.3)" loses its "?" only "while in the LOS of a Good
    /// Order enemy ground unit"; one that "breaks or becomes Wounded/Reduced" loses it "even if out of the LOS of all enemy units". Whether it keeps it.
    /// </summary>
    public static bool KeepsConcealment(bool concealed, string result, Func<bool> inGoodOrderEnemyLos)
    {
        ArgumentNullException.ThrowIfNull(inGoodOrderEnemyLos);
        return concealed && result is not (Broken or Wounded or CasualtyReduced or Eliminated or CrewBroken) && !inGoodOrderEnemyLos();
    }

    /// <summary>
    /// A14.3, D5.34, D5.341, D5.342 (pp. 82, 203, 204; pass 71, ruling R71.5): the CE crew of a fully armored AFV is Recalled by dr 1 and Stunned by dr 2,
    /// Recalled instead when already under Stun +1; the Inherent crew of an unarmored vehicle or Partially Armored AFV breaks on dr 1 and is pinned on dr 2
    /// (no effect on a crew already pinned).
    /// </summary>
    public static string CrewResult(int dr, bool fullyArmored, bool stunRecovery, bool pinned) =>
        fullyArmored ? dr == 1 || stunRecovery ? Recalled : Stunned
        : dr == 1 ? CrewBroken
        : pinned ? NoEffect : CrewPinned;

    /// <summary>A14.33 (p. 82; ruling R71.5): a Sniper attack on an unarmored vehicle itself immobilizes it on dr 1 only, with no further effect if already immobilized.</summary>
    public static string VehicleResult(int dr, bool immobilized) => dr == 1 && !immobilized ? Immobilized : NoEffect;

    /// <summary>A14.3, A14.31 (p. 82; ruling R71.6): dr 1 eliminates the enemy Sniper; dr 2 pins it (no effect on one already pinned).</summary>
    public static string SniperResult(int dr, bool pinned) => dr == 1 ? SniperEliminated : pinned ? NoEffect : SniperPinned;

    /// <summary>
    /// The conditions a crew or vehicle result sets (ruling R71.5), as the Fire path's vehicle results set them (D.7, D5.34, D5.341): a Stun, a Recall, an
    /// Immobilization, or a crew's pin; empty for any other result.
    /// </summary>
    public static IReadOnlyList<(VehicleCondition Condition, bool Value)> VehicleConditions(string result) =>
        ScenarioA1ResultTables.VehicleConditions(new FireVehicleEffect(string.Empty, string.Empty, result == Immobilized ? FireVehicleEffect.Immobilized : FireVehicleEffect.None,
            null, [], 0, null, null, result switch
            {
                Stunned => FireVehicleEffect.Stunned,
                Recalled => FireVehicleEffect.Recalled,
                CrewPinned => FireVehicleEffect.Pinned,
                _ => FireVehicleEffect.None,
            }));

    /// <summary>The projector's check of a recorded Sniper attack (A14; ruling R15.5): its Sniper counter is in play, its rolls recorded, and its dr 1 to 6; the events after it apply it.</summary>
    public static RecordRefusal? VerifySniperAttack(bool sniperActive, bool rollRecorded, bool triggerRecorded, int dr) =>
        !sniperActive || !rollRecorded || !triggerRecorded || dr is < 1 or > 6
            ? new RecordRefusal("UNIT-STATE-042", "A Sniper attack names a Sniper counter in play and recorded rolls (A14.1).")
            : null;

    /// <summary>A14.3: an effective Sniper attack puts every broken unit of the attacked side in the Location under DM.</summary>
    public static bool ComesUnderDm(bool active, bool attackedSide, bool broken, bool desperationMorale) => active && attackedSide && broken && !desperationMorale;
}
