namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A unit of a Location holding a vehicle, as the sequential CC reads it (A11.31; ruling R11.16): its side, whether it is a vehicle, armed for CC (a
/// vehicle's CC armament), an Infantry CC attacker, Personnel, a prisoner, berserk, Known, or a Gun's crew.
/// </summary>
public sealed record VehicleCcUnitFacts(string Id, string Side, bool Vehicle, bool Armed, bool Attacker, bool Personnel, bool Captured, bool Berserk, bool Known, bool Crew);

/// <summary>
/// CC with vehicles as the planner decides it around <see cref="ScenarioA1VehicleCloseCombat"/> (backlog pass 11, rulings R11.13 to R11.16; pass 32.g,
/// S6): a vehicle's CC armament, the CC attacker, the sides with attacks left and their order, the berserk unit owing an attack, the berserk OVR's CC,
/// CC Reaction Fire's bars, the captured vehicle, the sequential CC's bars and words, the attack's rolls and effects. Play reads the state and the
/// catalog, hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1VehicleCloseCombatRules
{
    /// <summary>Whether a vehicle has a CC attack (A11.62): a CMG not malfunctioned, or an AAMG while CE, with its crew able to fight.</summary>
    public static bool VehicleCcArmament(bool fromCatalog, bool stunned, bool shocked, bool unconfirmedKill, bool abandoned, bool recalled, bool captured,
        bool coaxialMg, bool cmgMalfunctioned, bool antiAircraftMg, bool crewExposed, bool mainArmamentAamg, bool malfunctioned, bool disabled) =>
        fromCatalog && !stunned && !shocked && !unconfirmedKill && !abandoned && !recalled && !captured
        && ((coaxialMg && !cmgMalfunctioned) || (antiAircraftMg && crewExposed && !(mainArmamentAamg && (malfunctioned || disabled))));

    /// <summary>Whether an Infantry unit may make a CC attack in the CCPh (A11.16, A20.5): active, Personnel, unbroken, not a prisoner, and not a Dummy.</summary>
    public static bool CcAttacker(bool active, bool vehicle, bool personnel, bool broken, bool captured, bool dummy) => active && !vehicle && personnel && !broken && !captured && !dummy;

    /// <summary>
    /// The sides with an attack left in a CC Location holding a vehicle (ruling R11.16): Infantry that have not attacked and face an enemy vehicle, and
    /// vehicles with CC armament that have not attacked and face enemy Infantry; a side that passed has none.
    /// </summary>
    public static HashSet<string> SidesWithAttacks(IReadOnlyList<VehicleCcUnitFacts> here, IReadOnlyCollection<string> attacked, IReadOnlyCollection<string> passed)
    {
        ArgumentNullException.ThrowIfNull(here);
        ArgumentNullException.ThrowIfNull(attacked);
        ArgumentNullException.ThrowIfNull(passed);
        var sides = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in here.Where(unit => !attacked.Contains(unit.Id) && !passed.Contains(unit.Side)))
        {
            var enemies = here.Where(other => other.Side != unit.Side && !other.Captured).ToArray();
            if (unit.Vehicle ? unit.Armed && enemies.Any(other => (!other.Vehicle && other.Attacker) || (!other.Vehicle && other.Personnel))
                : unit.Attacker && enemies.Any(other => other.Vehicle))
            {
                sides.Add(unit.Side);
            }
        }

        return sides;
    }

    /// <summary>The side that attacks first in a CC Location holding a vehicle (A11.31): the non-vehicular side, or with vehicles on both sides the ATTACKER.</summary>
    public static string FirstCcSide(IReadOnlyList<(string Side, bool Vehicle)> uncaptured, string phasingSide)
    {
        ArgumentNullException.ThrowIfNull(uncaptured);
        var vehicular = uncaptured.Where(unit => unit.Vehicle).Select(unit => unit.Side).Distinct(StringComparer.Ordinal).ToArray();
        return vehicular.Length == 1 ? uncaptured.Select(unit => unit.Side).FirstOrDefault(side => side != vehicular[0]) ?? phasingSide : phasingSide;
    }

    /// <summary>After an attack or a pass (A11.31; ruling R11.16): the other side while it has attacks left, else the same side, else the CC is over.</summary>
    public static (string? Next, bool Closed) NextCcSide(HashSet<string> left, string moved)
    {
        ArgumentNullException.ThrowIfNull(left);
        var other = left.FirstOrDefault(side => side != moved);
        return other is not null ? (other, false) : left.Contains(moved) ? (moved, false) : (null, true);
    }

    /// <summary>A15.43, A11.31: a berserk unit may owe a vehicle attack in the CCPh, in a Location whose CC is not closed and whose vehicle CC is open.</summary>
    public static bool OwingPossible(string? phase, bool closed, bool open) => !(phase != "ccph" || closed || !open);

    /// <summary>
    /// A berserk unit (of <paramref name="side"/>, or of either side) that still owes an attack on a Known enemy vehicle in a Location's sequential CC
    /// this CCPh (A15.43, A11.31; ruling R27.2): it may attack, has not, and the Location holds no Gun's crew, whose CC is not built. Null when none does.
    /// </summary>
    public static string? BerserkOwingVehicleAttack(IReadOnlyList<VehicleCcUnitFacts> here, string? side, IReadOnlyList<string> attacking)
    {
        ArgumentNullException.ThrowIfNull(here);
        ArgumentNullException.ThrowIfNull(attacking);
        return here.Any(unit => unit.Crew) ? null
            : here.Where(unit => (side is null || unit.Side == side) && unit.Berserk && unit.Attacker && !attacking.Contains(unit.Id)
                && here.Any(other => other.Side != unit.Side && other.Vehicle && other.Known && !other.Captured))
                .OrderBy(unit => unit.Id, StringComparer.Ordinal).FirstOrDefault()?.Id;
    }

    /// <summary>A4.152, A15.432 (ruling R27.3): a berserk Infantry OVR's CC may be pending in the MPh once the charging stack has entered the charged Location, the window on that entry has closed, and no CC is there yet.</summary>
    public static bool OverrunPendingPossible(string? phase, bool windowOpen, bool charging, bool atCharge, bool ccBegun) => phase == "mph" && !windowOpen && charging && atCharge && !ccBegun;

    /// <summary>A15.432, A4.15 (referee and table player, pass 27): the OVR is by a berserk MMC of the stack onto the only enemy unit there, a Known SMC not held in Melee.</summary>
    public static bool OverrunSmc(bool berserkMmcCharger, int enemyCount, bool smc, bool known, bool melee) => berserkMmcCharger && enemyCount == 1 && smc && known && !melee;

    /// <summary>
    /// The attacks of a berserk OVR's CC (A4.152, A15.432; ruling R27.3): every berserk unit of the phasing side there attacks the SMC, and the SMC attacks
    /// them back when it may attack in CC (not broken or captured), which never costs it anything.
    /// </summary>
    public static IReadOnlyList<CloseCombatDeclaration> OverrunAttacks(IReadOnlyList<string> berserk, string smcId, bool smcAttacks) =>
        smcAttacks ? [new CloseCombatDeclaration(berserk, [smcId]), new CloseCombatDeclaration([smcId], berserk)] : [new CloseCombatDeclaration(berserk, [smcId])];

    /// <summary>Ruling R11.16: a vehicle's CC Location waits for a side in the CCPh, while its CC is not closed and a vehicle is there.</summary>
    public static bool TurnOpen(string? phase, bool closed, bool vehicleThere) => !(phase != "ccph" || closed || !vehicleThere);

    /// <summary>Ruling R11.16: the side a vehicle's CC Location waits for: the side named next when it has attacks left, else the first with any; none when none has.</summary>
    public static (string? Next, bool Open) Turn(HashSet<string> left, string next)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Count == 0 ? (null, false) : (left.Contains(next) ? next : left.First(), true);
    }

    /// <summary>A11.5, D7.21: CC with a vehicle is made in the CCPh, or as CC Reaction Fire in the MPh.</summary>
    public static string? VehicleCcPhaseBar(string? phase) =>
        phase is "mph" or "ccph" ? null : "play.cc-vehicle-phase: CC with a vehicle is made in the CCPh, or as CC Reaction Fire in the MPh (A11.5, D7.21)";

    /// <summary>D7.2, D7.21: CC Reaction Fire attacks the moving vehicle in the attackers' Location while the DEFENDER's window on its MP expenditure is open.</summary>
    public static string? ReactionWindowBar(bool open) =>
        open ? null : "play.reaction-fire: CC Reaction Fire attacks the moving vehicle in the attackers' Location while the DEFENDER's window on its MP expenditure is open (D7.2, D7.21)";

    /// <summary>D7.1, D7.2 (referee, pass 11): Reaction Fire at an OVRing vehicle comes after the OVR is resolved.</summary>
    public static string? ReactionOverrunBar(bool overrun) => overrun ? "play.reaction-fire: the OVR is resolved first; CC Reaction Fire follows it (D7.1, D7.2)" : null;

    /// <summary>D7.21: a CC Reaction Fire attacker: an active DEFENDER unit in the Location, not a vehicle, unbroken, unpinned, armed, and not in Melee.</summary>
    public static bool ReactionAttacker(bool active, bool enemyOfVehicle, bool atLocation, bool vehicle, bool broken, bool pinned, bool captured, bool melee) =>
        active && enemyOfVehicle && atLocation && !vehicle && !broken && !pinned && !captured && !melee;

    /// <summary>D7.21: the attackers are one or two eligible units.</summary>
    public static string? ReactionAttackersBar(int count, bool anyIneligible, string locationText) =>
        count is < 1 or > 2 || anyIneligible ? $"play.reaction-fire: the attackers are one or two unbroken, unpinned, armed DEFENDER units in {locationText}, not in Melee (D7.21)" : null;

    /// <summary>D7.212 (ruling R11.13): FPF CC Reaction Fire is not built.</summary>
    public static string? ReactionFinalFireBar(string? finalFireId) =>
        finalFireId is { } id ? $"play.reaction-fire: {id} is marked Final Fire; FPF CC Reaction Fire is not built (D7.212; ruling R11.13)" : null;

    /// <summary>
    /// A11.52 (ruling R11.16): at the start of the CCPh an unarmed vehicle, not in Motion, with no Personnel of its side in its Location but enemy
    /// Infantry there, is captured.
    /// </summary>
    public static bool CapturedVehicle(bool active, bool vehicle, bool captured, bool motion, bool unarmed, bool located, bool friendlyPersonnel, bool enemyAttacker) =>
        active && vehicle && !captured && !motion && unarmed && located && !friendlyPersonnel && enemyAttacker;

    /// <summary>A11.31: the Location holds no vehicle CC left this CCPh.</summary>
    public static string NoVehicleCcText(string locationText) => $"play.cc-vehicle: {locationText} holds no vehicle CC left this CCPh (A11.31)";

    /// <summary>A11.12: one Location's CC at a time.</summary>
    public static string? OtherOpenBar(bool otherOpen) => otherOpen ? "play.cc-vehicle: another Location's CC is open; one Location at a time (A11.12)" : null;

    /// <summary>A15.43 (ruling R27.2): a side with a berserk unit facing a Known enemy vehicle there does not pass.</summary>
    public static string BerserkNoPassText(string owingId, string locationText) =>
        $"play.cc-vehicle-berserk: {owingId} is berserk and attacks the enemy vehicle in {locationText}; its side does not pass (A15.43)";

    /// <summary>A11.31: the pass's words.</summary>
    public static string PassSummary(string side, string locationText) => $"play.cc-vehicle: the {side} side passes in {locationText} and makes no more attacks there (A11.31)";

    /// <summary>Ruling R11.16: a CC attack here names the vehicle attacking or attacked.</summary>
    public static string? VehicleNamedBar(bool named) =>
        named ? null : "play.cc-vehicle: a CC attack here names the vehicle attacking or attacked; CC between Infantry in a Location holding a vehicle is not built (ruling R11.16)";

    /// <summary>A11.31: the vehicle attacks when its side is next and it has not attacked.</summary>
    public static string? VehicleAttackOrderBar(string next, bool vehicleIsNext, bool vehicleAttacked) =>
        !vehicleIsNext || vehicleAttacked ? $"play.cc-vehicle-order: the {next} side attacks next, and each unit attacks once (A11.31)" : null;

    /// <summary>A11.31, A11.5: an Infantry attacker of the sequential CC: a CC attacker of the side named next, an enemy of the vehicle, in the Location, that has not attacked.</summary>
    public static bool SequentialAttacker(bool found, bool attacker, bool ofNext, bool enemyOfVehicle, bool atLocation, bool attacked) =>
        found && attacker && ofNext && enemyOfVehicle && atLocation && !attacked;

    /// <summary>A11.31, A11.5: the side named next attacks with one or two eligible Infantry units.</summary>
    public static string? InfantryAttackersBar(int count, bool anyIneligible, string next, string locationText) =>
        count is < 1 or > 2 || anyIneligible
            ? $"play.cc-vehicle-order: the {next} side attacks next, with one or two of its unbroken Infantry units in {locationText} that have not attacked (A11.31, A11.5)"
            : null;

    /// <summary>The Infantry of a CC attack on a vehicle, with each unit's Inexperience as A19.2 reads it (ruling R11.14): true, false, or unchanged when unknown.</summary>
    public static VehicleCloseCombatFacts WithInexperience(VehicleCloseCombatFacts facts, Func<string, bool?> inexperienced)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(inexperienced);
        return facts with
        {
            Units = [.. facts.Units!.Select(unit => inexperienced(unit.UnitId!) switch
            {
                true => unit with { Inexperienced = true },
                false => unit with { Inexperienced = false },
                _ => unit,
            })],
        };
    }

    /// <summary>The roll the Close Combat package asks for next in a vehicle's CC: its key, its kind, and the rest of its key; null when the reasons name no missing roll.</summary>
    public static (string Key, string Kind, string Remainder)? VehicleRollMissing(IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        if (reasons is not [{ } missing] || !missing.StartsWith("asl.a1.cc-vehicle.roll-missing:", StringComparison.Ordinal))
        {
            return null;
        }

        var key = missing["asl.a1.cc-vehicle.roll-missing:".Length..];
        var split = key.IndexOf(':', StringComparison.Ordinal);
        return split < 0 ? (key, key, string.Empty) : (key, key[..split], key[(split + 1)..]);
    }

    /// <summary>The package left an attack on a vehicle it had accepted undecided.</summary>
    public static string VehicleAttackUndecidedText(IReadOnlyList<string> reasons) =>
        "The Close Combat package left an attack on a vehicle it had accepted undecided: " + string.Join("; ", reasons);

    /// <summary>The package left a vehicle's attack it had accepted undecided.</summary>
    public static string VehicleOwnAttackUndecidedText(IReadOnlyList<string> reasons) =>
        "The Close Combat package left a vehicle's attack it had accepted undecided: " + string.Join("; ", reasons);

    /// <summary>A11.5, A11.6: the attack on a vehicle takes two dice, an Unlikely Kill and a Wound Severity dr one each.</summary>
    public static int VehicleAttackDice(string kind) => kind == "attack" ? 2 : 1;

    /// <summary>The rolls of an attack on a vehicle with the dice drawn for a kind.</summary>
    public static VehicleCloseCombatRolls WithVehicleAttackRoll(VehicleCloseCombatRolls rolls, string kind, string rest, IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(rolls);
        ArgumentNullException.ThrowIfNull(values);
        return kind switch
        {
            "attack" => rolls with { Attack = values },
            "unlikelyKill" => rolls with { UnlikelyKill = values[0] },
            _ => rolls with { WoundSeverity = With(rolls.WoundSeverity, rest, values[0]) },
        };
    }

    /// <summary>A11.611: the vehicle is destroyed, as a wreck or a burning wreck.</summary>
    public static bool Destroyed(string vehicleResult) => vehicleResult is VehicleCloseCombatResolution.Eliminated or VehicleCloseCombatResolution.BurningWreck;

    /// <summary>A11.5: an immobilized vehicle loses Motion, in the record's order.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> ImmobilizedConditions() => [(UnitCondition.Immobilized, true), (UnitCondition.Motion, false)];

    /// <summary>D7.21: a CC Reaction Fire attacker that was neither eliminated nor Reduced is marked.</summary>
    public static bool Survives(IReadOnlyList<CloseCombatUnitEffect> effects, string unitId)
    {
        ArgumentNullException.ThrowIfNull(effects);
        return !effects.Any(effect => effect.UnitId == unitId && (effect.Eliminated || effect.FinalDefinitionId != effect.DefinitionId));
    }

    /// <summary>D7.21: the attackers of CC Reaction Fire carry a First Fire counter (Final Fire when already marked First Fire), and a CC counter while the vehicle survives; in the record's order.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> ReactionFireConditions(bool firstFire, bool destroyed)
    {
        var conditions = new List<(UnitCondition, bool)>();
        if (firstFire)
        {
            conditions.Add((UnitCondition.FinalFire, true));
            conditions.Add((UnitCondition.FirstFire, false));
        }
        else
        {
            conditions.Add((UnitCondition.FirstFire, true));
        }

        if (!destroyed)
        {
            conditions.Add((UnitCondition.CcReaction, true));
        }

        return conditions;
    }

    /// <summary>A11.6: the PAATC sentence of the attack's words.</summary>
    public static string PaatcText(IReadOnlyList<string> testing)
    {
        ArgumentNullException.ThrowIfNull(testing);
        return testing.Count > 0 ? $"; {string.Join(", ", testing)} first pass{(testing.Count == 1 ? "es" : string.Empty)} a PAATC (A11.6)" : string.Empty;
    }

    /// <summary>A11.5, D7.21: the attack's words.</summary>
    public static string VehicleAttackSummary(IReadOnlyList<string> unitIds, string vehicleId, bool reaction, string paatc)
    {
        ArgumentNullException.ThrowIfNull(unitIds);
        return $"play.cc-vehicle: {string.Join(" and ", unitIds)} attack{(unitIds.Count == 1 ? "s" : string.Empty)} {vehicleId} in CC{(reaction ? " as CC Reaction Fire (D7.21)" : " (A11.5)")}{paatc}";
    }

    /// <summary>The units a Random Selection dr names, from the rest of its key.</summary>
    public static string[] RandomSelected(string kind, string rest) => kind == "randomSelection" ? rest.Split(',') : [];

    /// <summary>A11.62: a vehicle's attack on Infantry takes two dice, a Random Selection one per unit named, others one.</summary>
    public static int VehicleInfantryDice(string kind, int selected) => kind switch
    {
        "attack" => 2,
        "randomSelection" => selected,
        _ => 1,
    };

    /// <summary>The rolls of a vehicle's attack on Infantry with the dice drawn for a kind.</summary>
    public static VehicleCloseCombatRolls WithVehicleInfantryRoll(VehicleCloseCombatRolls rolls, string kind, string rest, IReadOnlyList<string> selected, IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(rolls);
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(values);
        return kind switch
        {
            "attack" => rolls with { Attack = values },
            "randomSelection" => rolls with
            {
                RandomSelection = selected.Select((id, index) => (id, index)).Aggregate(rolls.RandomSelection, (map, pair) => With(map, pair.id, values[pair.index])),
            },
            _ => rolls with { WoundSeverity = With(rolls.WoundSeverity, rest, values[0]) },
        };
    }

    /// <summary>A11.62: the words of a vehicle's attack on Infantry.</summary>
    public static string VehicleAttacksInfantrySummary(string vehicleId, IReadOnlyList<string> defenders) =>
        $"play.cc-vehicle: {vehicleId} attacks {string.Join(", ", defenders)} in CC with its CC armament (A11.62)";

    /// <summary>A roll map with one more roll, the key's earlier value replaced.</summary>
    private static Dictionary<string, T> With<T>(IReadOnlyDictionary<string, T>? existing, string key, T value)
    {
        var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new Dictionary<string, T>(StringComparer.Ordinal);
        next[key] = value;
        return next;
    }
}
