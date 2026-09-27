using System.Globalization;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// Resolves the Close Combat of one Location in one round (unit step 29; Scenario A1 Close Combat Review 2026-09-27), and
/// the Ambush drs that may precede it (A11.4). A pure function of the facts, the pinned CCT, and the catalog: it rolls
/// nothing and changes nothing. Facts outside the reviewed scope abstain; undecided facts and missing rolls are
/// Indeterminate, and rolls are asked for one at a time.
/// </summary>
public static class ScenarioA1CloseCombatCalculator
{
    private static readonly string[] AmbushTerrain = ["woods", "wooden-building", "stone-building"];

    /// <summary>Whether an Ambush can occur (A11.4): Infantry advanced into CC this APh, not into a Melee, in a woods or building Location.</summary>
    public static bool AmbushPossible(string? terrain, IReadOnlyList<CloseCombatUnit> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return AmbushTerrain.Contains(terrain, StringComparer.Ordinal) && units.Any(unit => unit.Advanced == true) && units.All(unit => unit.InMelee != true)
            && units.Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count() == 2;
    }

    public static AmbushResolution ResolveAmbush(AmbushFacts facts, ScenarioA1CloseCombatReference reference)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(reference);
        var reasons = new List<string>();
        if (facts.Phase != "CCPh")
        {
            reasons.Add("asl.a1.cc.phase-outside");
        }

        if (facts.Units is not { Count: > 0 } units || facts.LocationId is null || facts.AttackerSide is null)
        {
            return new AmbushResolution(CloseCombatResolution.Indeterminate, ["asl.a1.cc.fact-missing:ambush"], [], null);
        }

        reasons.AddRange(UnitsOutside(units, facts.LocationId, reference));
        if (reasons.Count == 0 && !AmbushPossible(facts.Terrain, units))
        {
            reasons.Add("asl.a1.cc.ambush-not-possible");
        }

        if (reasons.Count != 0)
        {
            return new AmbushResolution(CloseCombatResolution.Abstained, reasons.Distinct(StringComparer.Ordinal).ToList(), [], null);
        }

        // Lax (A11.18) needs each Green MMC's Inexperienced status (A19.3).
        var undecided = units.Where(unit => reference.Definitions[unit.DefinitionId!].Class == "green" && unit.Inexperienced is null)
            .Select(unit => "asl.a1.cc.fact-missing:inexperienced:" + unit.UnitId).ToList();
        if (undecided.Count != 0)
        {
            return new AmbushResolution(CloseCombatResolution.Indeterminate, undecided, [], null);
        }

        string[] sides = [facts.AttackerSide, .. units.Select(unit => unit.Side!).Where(side => side != facts.AttackerSide).Distinct(StringComparer.Ordinal)];
        var results = new List<AmbushSide>();
        foreach (var side in sides)
        {
            if (facts.Rolls?.TryGetValue(side, out var dr) != true)
            {
                return new AmbushResolution(CloseCombatResolution.Indeterminate, ["asl.a1.cc.roll-missing:ambush:" + side], [], null);
            }

            if (dr is < 1 or > 6)
            {
                return new AmbushResolution(CloseCombatResolution.Abstained, ["asl.a1.cc.roll-malformed"], [], null);
            }

            var drm = AmbushDrm([.. units.Where(unit => unit.Side == side)], reference);
            results.Add(new AmbushSide(side, dr, drm, dr + (int)drm.Sum(item => item.Value)));
        }

        if (facts.Rolls!.Keys.Any(side => !sides.Contains(side, StringComparer.Ordinal)))
        {
            return new AmbushResolution(CloseCombatResolution.Abstained, ["asl.a1.cc.extra-roll:ambush"], [], null);
        }

        // A11.4: a side whose Final dr is at least three below the other's ambushes it.
        var ambusher = results[0].FinalDr <= results[1].FinalDr - 3 ? results[0].Side
            : results[1].FinalDr <= results[0].FinalDr - 3 ? results[1].Side
            : null;
        return new AmbushResolution(CloseCombatResolution.Resolved, [], results, ambusher);
    }

    /// <summary>
    /// A side's Ambush drm (A11.4, A11.17, A11.18): each cause once when any unit of its force has it (ruling R29.9): +1 broken,
    /// +1 pinned, +1 berserk, +1 Lax (Inexperienced, A19.36, or berserk, A15.432), -1 Stealthy (a Good Order hero, A15.24),
    /// and the leadership of its best unpinned Good Order leader unless he is alone or any of the force is berserk.
    /// </summary>
    private static List<FireModifier> AmbushDrm(IReadOnlyList<CloseCombatUnit> force, ScenarioA1CloseCombatReference reference)
    {
        var drm = new List<FireModifier>();
        bool Any(Func<CloseCombatUnit, bool> test) => force.Any(test);
        if (Any(unit => unit.Broken == true))
        {
            drm.Add(new FireModifier("broken", 1m, "A11.4"));
        }

        if (Any(unit => unit.Pinned == true))
        {
            drm.Add(new FireModifier("pinned", 1m, "A11.4"));
        }

        var berserk = Any(unit => unit.Berserk == true);
        if (berserk)
        {
            drm.Add(new FireModifier("berserk", 1m, "A11.4"));
        }

        if (berserk || Any(unit => Inexperienced(unit, reference)))
        {
            drm.Add(new FireModifier("lax", 1m, "A11.18"));
        }

        if (Any(unit => reference.Definitions[unit.DefinitionId!].IsHero && GoodOrder(unit)))
        {
            drm.Add(new FireModifier("stealthy", -1m, "A11.17"));
        }

        var leader = force.Where(unit => reference.Definitions[unit.DefinitionId!].IsLeader && GoodOrder(unit) && unit.Pinned != true)
            .OrderBy(unit => Leadership(unit, reference)).FirstOrDefault();
        if (leader is not null && force.Count > 1 && !berserk && Leadership(leader, reference) is var value and not 0)
        {
            drm.Add(new FireModifier("leadership:" + leader.UnitId, value, "A11.4"));
        }

        return drm;
    }

    private static bool GoodOrder(CloseCombatUnit unit) => unit.Broken != true && unit.Berserk != true && unit.Captured != true;

    private static bool Inexperienced(CloseCombatUnit unit, ScenarioA1CloseCombatReference reference) =>
        reference.Definitions[unit.DefinitionId!] is { IsMmc: true } definition
        && (definition.Class == "conscript" || (definition.Class == "green" && unit.Inexperienced == true));

    /// <summary>A leader's modifier, one worse when wounded (A17.3).</summary>
    private static int Leadership(CloseCombatUnit unit, ScenarioA1CloseCombatReference reference) =>
        reference.Definitions[unit.DefinitionId!].Leadership!.Value + (unit.Wounded == true ? 1 : 0);

    public static CloseCombatResolution Resolve(CloseCombatFacts facts, ScenarioA1CloseCombatReference reference)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(reference);
        var missing = Missing(facts);
        if (missing.Count != 0)
        {
            return Refused(CloseCombatResolution.Indeterminate, missing);
        }

        var outside = Outside(facts, reference);
        if (outside.Count != 0)
        {
            return Refused(CloseCombatResolution.Abstained, outside);
        }

        var undecided = Undecided(facts, reference);
        return undecided.Count != 0 ? Refused(CloseCombatResolution.Indeterminate, undecided) : new Round(facts, reference).Run();
    }

    /// <summary>
    /// The reasons a round cannot be committed before any roll: empty when every outcome the dice can reach is decided. Every
    /// reason other than a missing roll depends on the facts alone, so the facts must stop at the first attack's DR (or
    /// resolve at once when the round declares no attack).
    /// </summary>
    public static IReadOnlyList<string> Precheck(CloseCombatFacts facts, ScenarioA1CloseCombatReference reference)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(reference);
        var first = Resolve(facts with { Rolls = new CloseCombatRolls(null) }, reference);
        return first.Disposition == CloseCombatResolution.Resolved || first.Reasons is ["asl.a1.cc.roll-missing:attack:0"] ? [] : first.Reasons;
    }

    private static CloseCombatResolution Refused(string disposition, IReadOnlyList<string> reasons) => new(disposition, reasons, [], [], [], []);

    private static List<string> Missing(CloseCombatFacts facts)
    {
        var missing = new List<string>();
        void Need(object? value, string name)
        {
            if (value is null || (value is string text && string.IsNullOrWhiteSpace(text)))
            {
                missing.Add("asl.a1.cc.fact-missing:" + name);
            }
        }

        Need(facts.Phase, "phase");
        Need(facts.LocationId, "locationId");
        Need(facts.Terrain, "terrain");
        Need(facts.AttackerSide, "attackerSide");
        Need(facts.Round, "round");
        Need(facts.Units, "units");
        Need(facts.AttackedBefore, "attackedBefore");
        Need(facts.AttackingBefore, "attackingBefore");
        Need(facts.Attacks, "attacks");
        Need(facts.Rolls, "rolls");
        if (facts.Round is CloseCombatFacts.AmbusherRound or CloseCombatFacts.AmbushedRound)
        {
            Need(facts.Ambusher, "ambusher");
        }

        foreach (var (unit, index) in (facts.Units ?? []).Select((item, index) => (item, index)))
        {
            var at = $"units[{index}].";
            Need(unit.UnitId, at + "unitId");
            Need(unit.DefinitionId, at + "definitionId");
            Need(unit.Side, at + "side");
            Need(unit.Broken, at + "broken");
            Need(unit.Pinned, at + "pinned");
            Need(unit.Wounded, at + "wounded");
            Need(unit.Disrupted, at + "disrupted");
            Need(unit.Berserk, at + "berserk");
            Need(unit.Fanatic, at + "fanatic");
            Need(unit.Heroic, at + "heroic");
            Need(unit.Concealed, at + "concealed");
            Need(unit.Captured, at + "captured");
            Need(unit.Advanced, at + "advanced");
            Need(unit.InMelee, at + "inMelee");
        }

        foreach (var (attack, index) in (facts.Attacks ?? []).Select((item, index) => (item, index)))
        {
            Need(attack.Attackers, $"attacks[{index}].attackers");
            Need(attack.Defenders, $"attacks[{index}].defenders");
        }

        return missing;
    }

    /// <summary>The units' own scope: Infantry of the catalog, two sides, none concealed or captured, and within stacking limits.</summary>
    private static List<string> UnitsOutside(IReadOnlyList<CloseCombatUnit> units, string location, ScenarioA1CloseCombatReference reference)
    {
        var outside = new List<string>();
        if (units.Select(unit => unit.UnitId).Distinct(StringComparer.Ordinal).Count() != units.Count)
        {
            outside.Add("asl.a1.cc.unit-listed-twice");
        }

        if (units.Any(unit => reference.Definitions.GetValueOrDefault(unit.DefinitionId ?? string.Empty) is not { } definition
            || !(definition.IsMmc || definition.IsLeader || definition.IsHero)))
        {
            outside.Add("asl.a1.cc.unit-outside");
            return outside;
        }

        if (units.Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count() > 2)
        {
            outside.Add("asl.a1.cc.unit-outside");
        }

        // A11.19 (concealment in CC) and A20.55 (prisoners in CC) are not reviewed.
        if (units.Any(unit => unit.Concealed == true))
        {
            outside.Add("asl.a1.cc.concealment-unreviewed");
        }

        if (units.Any(unit => unit.Captured == true))
        {
            outside.Add("asl.a1.cc.prisoners-unreviewed");
        }

        // A5.1, A5.5: three squad-equivalents and four SMC per side; overstacking (A5.12, A5.131) is not reviewed.
        foreach (var side in units.GroupBy(unit => unit.Side, StringComparer.Ordinal))
        {
            var definitions = side.Select(unit => reference.Definitions[unit.DefinitionId!]).ToArray();
            var squads = definitions.Count(item => item.Kind == "asl:squad") + (definitions.Count(item => item.Kind == "asl:half-squad") / 2m);
            if (squads > 3 || definitions.Count(item => item.IsLeader || item.IsHero) > 4)
            {
                outside.Add("asl.a1.cc.overstacked-unreviewed");
            }
        }

        return outside;
    }

    private static List<string> Outside(CloseCombatFacts facts, ScenarioA1CloseCombatReference reference)
    {
        var outside = new List<string>();
        if (facts.Phase != "CCPh")
        {
            outside.Add("asl.a1.cc.phase-outside");
        }

        var units = facts.Units!;
        outside.AddRange(UnitsOutside(units, facts.LocationId!, reference));
        if (outside.Contains("asl.a1.cc.unit-outside"))
        {
            return outside;
        }

        var byId = units.ToDictionary(unit => unit.UnitId!, StringComparer.Ordinal);
        var round = facts.Round;
        if (round is not (CloseCombatFacts.Simultaneous or CloseCombatFacts.AmbusherRound or CloseCombatFacts.AmbushedRound)
            || (round != CloseCombatFacts.Simultaneous && !units.Any(unit => unit.Side == facts.Ambusher)))
        {
            outside.Add("asl.a1.cc.round-outside");
        }

        var attacking = new List<string>(facts.AttackingBefore!);
        var attacked = new List<string>(facts.AttackedBefore!);
        var stacked = units.Where(unit => unit.StackedWith is not null).ToArray();

        // A11.14: a SMC stacks with one MMC of its own side, declared before either side designates its attacks.
        if (stacked.Any(unit => (!reference.Definitions[unit.DefinitionId!].IsLeader && !reference.Definitions[unit.DefinitionId!].IsHero)
            || !byId.TryGetValue(unit.StackedWith!, out var mmc) || !reference.Definitions[mmc.DefinitionId!].IsMmc || mmc.Side != unit.Side))
        {
            outside.Add("asl.a1.cc.stacking-outside");
        }

        foreach (var (attack, index) in facts.Attacks!.Select((item, index) => (item, index)))
        {
            var attackers = attack.Attackers!;
            var defenders = attack.Defenders!;
            if (attackers.Count == 0 || defenders.Count == 0 || attackers.Concat(defenders).Any(id => !byId.ContainsKey(id)))
            {
                outside.Add($"asl.a1.cc.attack-outside:{index}");
                continue;
            }

            var side = byId[attackers[0]].Side;
            if (attackers.Any(id => byId[id].Side != side) || defenders.Any(id => byId[id].Side == side))
            {
                outside.Add($"asl.a1.cc.attack-outside:{index}");
            }

            // A11.3, A11.32: in an Ambush the ambusher's attacks are resolved first, then the other side's.
            if ((round == CloseCombatFacts.AmbusherRound && side != facts.Ambusher) || (round == CloseCombatFacts.AmbushedRound && side == facts.Ambusher))
            {
                outside.Add($"asl.a1.cc.round-outside:{index}");
            }

            // A11.16: a broken unit never attacks; A11.12: no unit attacks or is attacked more than once per CCPh.
            if (attackers.Any(id => byId[id].Broken == true))
            {
                outside.Add($"asl.a1.cc.broken-attacker:{index}");
            }

            if (attackers.Any(attacking.Contains) || defenders.Any(attacked.Contains)
                || attackers.Distinct(StringComparer.Ordinal).Count() != attackers.Count || defenders.Distinct(StringComparer.Ordinal).Count() != defenders.Count)
            {
                outside.Add($"asl.a1.cc.attacked-twice:{index}");
            }

            attacking.AddRange(attackers);
            attacked.AddRange(defenders);

            // A11.14: a SMC stacked with an MMC attacks with it or not at all, and the MMC and its SMC are attacked together.
            foreach (var smc in stacked)
            {
                if (attackers.Contains(smc.UnitId) && !attackers.Contains(smc.StackedWith))
                {
                    outside.Add($"asl.a1.cc.stacking-outside:{index}");
                }

                if (defenders.Contains(smc.UnitId) != defenders.Contains(smc.StackedWith))
                {
                    outside.Add($"asl.a1.cc.stacking-outside:{index}");
                }
            }

            // A11.141: the director is a Good Order, unpinned leader of the attack (ruling R29.8).
            if (attack.Director is { } director && (!attackers.Contains(director) || !reference.Definitions[byId[director].DefinitionId!].IsLeader
                || byId[director].Berserk == true || byId[director].Pinned == true))
            {
                outside.Add($"asl.a1.cc.director-outside:{index}");
            }
        }

        // A15.43: a berserk unit charges to destroy the enemy in CC, so it attacks in its side's round (ruling R29.15).
        var attackingSides = round switch
        {
            CloseCombatFacts.AmbusherRound => [facts.Ambusher!],
            CloseCombatFacts.AmbushedRound => units.Select(unit => unit.Side!).Where(side => side != facts.Ambusher).Distinct(StringComparer.Ordinal).ToArray(),
            _ => units.Select(unit => unit.Side!).Distinct(StringComparer.Ordinal).ToArray(),
        };
        if (units.Any(unit => unit.Berserk == true && attackingSides.Contains(unit.Side) && !attacking.Contains(unit.UnitId!)))
        {
            outside.Add("asl.a1.cc.berserk-must-attack");
        }

        if (facts.Rolls is { } rolls && Malformed(rolls))
        {
            outside.Add("asl.a1.cc.roll-malformed");
        }

        return outside.Distinct(StringComparer.Ordinal).ToList();
    }

    private static bool Malformed(CloseCombatRolls rolls) =>
        rolls.Attacks?.Values.Any(dice => dice.Count != 2 || dice.Any(die => die is < 1 or > 6)) == true
        || new[] { rolls.RandomSelection, rolls.WoundSeverity, rolls.LeaderCreation, rolls.WeaponLoss }.Any(map => map?.Values.Any(dr => dr is < 1 or > 6) == true);

    private static List<string> Undecided(CloseCombatFacts facts, ScenarioA1CloseCombatReference reference)
    {
        var undecided = new List<string>();
        var byId = facts.Units!.ToDictionary(unit => unit.UnitId!, StringComparer.Ordinal);
        foreach (var unit in facts.Units!)
        {
            var definition = reference.Definitions[unit.DefinitionId!];
            if (definition.Morale is null || (definition.IsMmc && definition.Firepower is null) || (definition.IsLeader && definition.Leadership is null))
            {
                undecided.Add("asl.a1.cc.definition-incomplete:" + definition.Id);
            }

            // A11.11, A7.302: a squad's Casualty Reduction makes its HS.
            if (definition.Kind == "asl:squad" && (ScenarioA1FireReference.HalfSquadOf(definition.Id) is not { } half || !reference.Definitions.ContainsKey(half)))
            {
                undecided.Add("asl.a1.cc.reduction-counter-missing:" + definition.Id);
            }
        }

        // A18.12, A18.2: an Original 2 by an attacking MMC creates a leader by a dr whose drm depend on the base MMC; the MMC
        // of the highest BPV when several differ (the catalog has no BPV), and the MMC he defends with (Random Selection).
        foreach (var (attack, index) in facts.Attacks!.Select((item, index) => (item, index)))
        {
            var mmcs = attack.Attackers!.Select(id => byId[id]).Where(unit => reference.Definitions[unit.DefinitionId!].IsMmc).ToArray();
            if (mmcs.Length == 0)
            {
                continue;
            }

            if (mmcs.Select(unit => BaseMorale(unit, reference)).Distinct().Count() > 1)
            {
                undecided.Add($"asl.a1.cc.field-promotion-base-undecided:{index}");
            }

            if (mmcs.Any(unit => ScenarioA1FireReference.IsNkvd(unit.DefinitionId!)))
            {
                undecided.Add($"asl.a1.cc.field-promotion-commissar-unreviewed:{index}");
            }

            // The created leader defends with one MMC of the attack: when an enemy attack takes some of them and not the
            // others, which one matters.
            if (mmcs.Length > 1 && facts.Attacks!.Any(other => mmcs.Count(unit => other.Defenders!.Contains(unit.UnitId)) is var count && count > 0 && count < mmcs.Length))
            {
                undecided.Add($"asl.a1.cc.field-promotion-stacking-undecided:{index}");
            }

            var nationality = reference.Definitions[mmcs[0].DefinitionId!].Nationality;
            if (ScenarioA1FieldPromotion.Applicable(nationality) is false)
            {
                undecided.Add("asl.a1.cc.leader-creation-not-applicable:" + nationality);
            }
        }

        return undecided;
    }

    /// <summary>The Morale Level of a MMC as Leader Creation reads it (A18.2): berserk 10 (A15.42), one higher when Fanatic (A10.8).</summary>
    private static int BaseMorale(CloseCombatUnit unit, ScenarioA1CloseCombatReference reference) =>
        (unit.Berserk == true ? 10 : reference.Definitions[unit.DefinitionId!].Morale!.Value) + (unit.Fanatic == true ? 1 : 0);

    /// <summary>One round of CC in a Location, with the units' changing state.</summary>
    private sealed class Round(CloseCombatFacts facts, ScenarioA1CloseCombatReference reference)
    {
        private readonly HashSet<string> usedRolls = new(StringComparer.Ordinal);
        private readonly Dictionary<string, UnitState> units = facts.Units!.ToDictionary(unit => unit.UnitId!, unit => new UnitState(unit, reference.Definitions[unit.DefinitionId!]),
            StringComparer.Ordinal);

        private readonly List<CloseCombatCreatedLeader> created = [];

        public CloseCombatResolution Run()
        {
            var attacks = facts.Attacks!;
            var rolls = facts.Rolls!;

            // Every attack's DR is rolled before any is resolved: CC is simultaneous (A11.1, A11.12).
            var dice = new List<IReadOnlyList<int>>();
            for (var index = 0; index < attacks.Count; index++)
            {
                if (rolls.Attacks?.TryGetValue(Key(index), out var drawn) != true)
                {
                    return Missing("attack:" + Key(index));
                }

                usedRolls.Add("attack:" + Key(index));
                dice.Add(drawn!);
            }

            // A18.12: an Original 2 by an attacking MMC calls for a Leader Creation dr; the leader joins that attack and defends
            // with its MMC, and both attacks are re-figured as if he had been there all along.
            var leaders = new Dictionary<int, LeaderCreationOutcome>();
            var extraAttackers = new Dictionary<int, UnitState>();
            for (var index = 0; index < attacks.Count; index++)
            {
                var mmcs = attacks[index].Attackers!.Select(id => units[id]).Where(unit => unit.Definition.IsMmc).ToArray();
                if (dice[index].Sum() != 2 || mmcs.Length == 0)
                {
                    continue;
                }

                if (rolls.LeaderCreation?.TryGetValue(Key(index), out var dr) != true)
                {
                    return Missing("leaderCreation:" + Key(index));
                }

                usedRolls.Add("leaderCreation:" + Key(index));
                var odds = Odds(index, attacks, null);
                var columnsBelow = Math.Max(0, reference.OneToOneColumn - odds.Column);
                List<FireModifier> oddsDrm = columnsBelow > 0 ? [new FireModifier("odds-below-1-1", -columnsBelow, "A18.2")] : [];
                var (outcome, reason) = ScenarioA1FieldPromotion.Create(mmcs[0].Definition, BaseMorale(mmcs[0].Facts, reference), dr, oddsDrm, reference.Definitions,
                    "asl.a1.cc");
                if (outcome is null)
                {
                    return Refused(CloseCombatResolution.Indeterminate, [reason!]);
                }

                leaders[index] = outcome;
                if (outcome.LeaderDefinitionId is { } leader)
                {
                    var id = CloseCombatCreatedLeader.PlaceholderPrefix + Key(index);
                    var state = new UnitState(new CloseCombatUnit(id, leader, mmcs[0].Facts.Side, false, false, false, false, false, mmcs[0].Facts.Fanatic, false, false,
                        false, false, false), reference.Definitions[leader]);
                    units[id] = state;
                    extraAttackers[index] = state;
                    created.Add(new CloseCombatCreatedLeader(index, id, leader, mmcs[0].Facts.Side!, mmcs[0].Id, false, false));
                }
            }

            var arithmetic = new List<CloseCombatAttackArithmetic>();
            var partial = new List<(int Attack, UnitState Unit)>();
            for (var index = 0; index < attacks.Count; index++)
            {
                var attack = attacks[index];
                var odds = Odds(index, attacks, extraAttackers);
                var drm = CommonDrm(index, attack, extraAttackers);
                var original = dice[index].Sum();
                var defending = new List<CloseCombatDefenderResult>();
                var candidates = new List<UnitState>();
                foreach (var defender in Defenders(index, attacks, extraAttackers))
                {
                    // A11.16: the -2 against a broken unit applies to it alone (as A4.8 EX reads a status DRM in CC).
                    List<FireModifier> own = defender.Facts.Broken == true ? [new FireModifier("vs-broken", -2m, "A11.16")] : [];
                    var final = original + (int)drm.Concat(own).Sum(item => item.Value);
                    var result = final < odds.Kill ? CloseCombatDefenderResult.Eliminated
                        : final == odds.Kill ? CloseCombatDefenderResult.PartialKill
                        : CloseCombatDefenderResult.NoEffect;
                    defending.Add(new CloseCombatDefenderResult(defender.Id, own, final, result));
                    if (result == CloseCombatDefenderResult.PartialKill)
                    {
                        candidates.Add(defender);
                    }
                }

                // A11.11: a Partial Kill Casualty Reduces one defending unit (or more) chosen by Random Selection (A.9): the
                // highest dr, and every unit that ties it.
                if (candidates.Count > 1)
                {
                    var selection = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (var candidate in candidates)
                    {
                        if (rolls.RandomSelection?.TryGetValue(Key(index) + ":" + candidate.Id, out var dr) != true)
                        {
                            return Missing($"randomSelection:{Key(index)}:{string.Join(",", candidates.Select(item => item.Id))}");
                        }

                        usedRolls.Add($"randomSelection:{Key(index)}:{candidate.Id}");
                        selection[candidate.Id] = dr;
                    }

                    var highest = selection.Values.Max();
                    defending = [.. defending.Select(item => selection.TryGetValue(item.UnitId, out var dr)
                        ? item with { RandomSelectionDr = dr, CasualtyReduced = dr == highest }
                        : item)];
                    partial.AddRange(candidates.Where(item => selection[item.Id] == highest).Select(item => (index, item)));
                }
                else if (candidates.Count == 1)
                {
                    defending = [.. defending.Select(item => item.UnitId == candidates[0].Id ? item with { CasualtyReduced = true } : item)];
                    partial.Add((index, candidates[0]));
                }

                arithmetic.Add(new CloseCombatAttackArithmetic(index, units[attack.Attackers![0]].Facts.Side!, [.. Attackers(index, attack, extraAttackers).Select(item => item.Id)],
                    [.. Defenders(index, attacks, extraAttackers).Select(item => item.Id)], odds.Modifiers, odds.Attack, odds.Defense, odds.Label, odds.Kill,
                    dice[index].ToArray(), original, drm, defending)
                {
                    LeaderCreation = leaders.GetValueOrDefault(index),
                });
            }

            // The results take effect together (A11.12): eliminations, then Casualty Reductions (A7.302; A17.11 for a SMC).
            foreach (var attack in arithmetic)
            {
                foreach (var defender in attack.Defending.Where(item => item.Result == CloseCombatDefenderResult.Eliminated))
                {
                    units[defender.UnitId].Eliminate("eliminated-cc");
                }
            }

            foreach (var (_, unit) in partial.Where(item => !item.Unit.Eliminated))
            {
                if (!Reduce(unit))
                {
                    return pending!;
                }
            }

            // A11.13: each SW of a unit eliminated in CC is lost too on an Original colored dr of 1 and a dr at most the
            // attack's black Kill Number; any other stays in the Location unpossessed.
            var weaponEffects = new List<CloseCombatWeaponEffect>();
            foreach (var attack in arithmetic.Where(item => item.Dice[0] == 1))
            {
                foreach (var defender in attack.Defending.Where(item => units[item.UnitId].Eliminated))
                {
                    foreach (var weapon in units[defender.UnitId].Facts.Weapons ?? [])
                    {
                        if (rolls.WeaponLoss?.TryGetValue(weapon, out var dr) != true)
                        {
                            return Missing("weaponLoss:" + weapon);
                        }

                        usedRolls.Add("weaponLoss:" + weapon);
                        weaponEffects.Add(new CloseCombatWeaponEffect(weapon, defender.UnitId, dr, dr <= attack.KillNumber));
                    }
                }
            }

            // A15.46: a berserk unit returns to normal when its group's attack eliminated at least one enemy unit and no Known
            // enemy unit is left in its Location (ruling R29.15).
            foreach (var attack in arithmetic)
            {
                var berserk = attack.Attackers.Select(id => units[id]).Where(unit => unit.Facts.Berserk == true && !unit.Eliminated).ToArray();
                var enemiesLeft = units.Values.Any(unit => unit.Facts.Side != attack.Side && !unit.Eliminated);
                if (berserk.Length > 0 && !enemiesLeft && attack.Defending.Any(item => units[item.UnitId].Eliminated))
                {
                    foreach (var unit in berserk)
                    {
                        unit.EndBerserk();
                    }
                }
            }

            var extra = ExtraRolls();
            if (extra.Count != 0)
            {
                return Refused(CloseCombatResolution.Abstained, extra);
            }

            var effects = units.Values.Where(unit => !unit.Id.StartsWith(CloseCombatCreatedLeader.PlaceholderPrefix, StringComparison.Ordinal))
                .Select(unit => unit.Effect()).ToArray();
            var leadersCreated = created.Select(item => units[item.UnitId] is var state ? item with { Eliminated = state.Eliminated, Wounded = state.Wounded && !state.Eliminated } : item)
                .ToArray();
            return new CloseCombatResolution(CloseCombatResolution.Resolved, [], arithmetic, effects, leadersCreated, weaponEffects);
        }

        private CloseCombatResolution? pending;

        private static string Key(int index) => index.ToString(CultureInfo.InvariantCulture);

        private static CloseCombatResolution Missing(string key) => Refused(CloseCombatResolution.Indeterminate, ["asl.a1.cc.roll-missing:" + key]);

        private IEnumerable<UnitState> Attackers(int index, CloseCombatDeclaration attack, Dictionary<int, UnitState>? extra) =>
            attack.Attackers!.Select(id => units[id]).Concat(extra?.TryGetValue(index, out var leader) == true ? [leader] : []);

        /// <summary>An attack's defenders, with a created leader who defends with one of them (A18.12).</summary>
        private List<UnitState> Defenders(int index, IReadOnlyList<CloseCombatDeclaration> attacks, Dictionary<int, UnitState>? extra)
        {
            var defenders = attacks[index].Defenders!.Select(id => units[id]).ToList();
            foreach (var (source, leader) in extra ?? [])
            {
                var stack = created.First(item => item.Attack == source).StackedWith;
                if (defenders.Any(unit => unit.Id == stack))
                {
                    defenders.Add(leader);
                }
            }

            return defenders;
        }

        /// <summary>
        /// The odds of an attack (A11.11): the attackers' FP against the defenders', a SMC counting one (A11.14) and a pinned
        /// attacker half (the CCT), rounded down to the printed column.
        /// </summary>
        private (decimal Attack, decimal Defense, string Label, int Kill, int Column, IReadOnlyList<FireModifier> Modifiers) Odds(int index,
            IReadOnlyList<CloseCombatDeclaration> attacks, Dictionary<int, UnitState>? extra)
        {
            var modifiers = new List<FireModifier>();
            decimal attack = 0;
            foreach (var unit in Attackers(index, attacks[index], extra))
            {
                decimal fp = unit.Firepower;
                if (unit.Facts.Pinned == true)
                {
                    fp /= 2;
                    modifiers.Add(new FireModifier("pinned:" + unit.Id, 0.5m, "A7.8"));
                }

                attack += fp;
            }

            var defense = Defenders(index, attacks, extra).Sum(unit => (decimal)unit.Firepower);
            var (label, kill, column) = reference.Column(attack, defense);
            return (attack, defense, label, kill, column, modifiers);
        }

        /// <summary>
        /// The DRM of an attack for every defender: the Ambush -1 by and +1 against the ambusher (A11.4), the director's
        /// leadership unless he attacks alone or with a berserk unit (A11.141), each hero's -1 (A15.24), and a created
        /// leader's leadership (A18.12).
        /// </summary>
        private List<FireModifier> CommonDrm(int index, CloseCombatDeclaration attack, Dictionary<int, UnitState> extra)
        {
            var drm = new List<FireModifier>();
            var side = units[attack.Attackers![0]].Facts.Side;
            if (facts.Ambusher is { } ambusher && facts.Round != CloseCombatFacts.Simultaneous)
            {
                drm.Add(side == ambusher ? new FireModifier("ambush", -1m, "A11.4") : new FireModifier("vs-ambush", 1m, "A11.4"));
            }

            var attackers = attack.Attackers!.Select(id => units[id]).ToArray();
            var berserk = attackers.Any(unit => unit.Facts.Berserk == true);
            if (attack.Director is { } director && attackers.Length > 1 && !berserk)
            {
                drm.Add(new FireModifier("leadership:" + director, Leadership(units[director].Facts, reference), "A11.141"));
            }

            foreach (var unit in attackers.Where(unit => unit.IsHeroType && unit.Id != attack.Director))
            {
                drm.Add(new FireModifier("heroic:" + unit.Id, -1m, "A15.24"));
            }

            if (extra.TryGetValue(index, out var leader))
            {
                drm.Add(new FireModifier("created-leader:" + leader.Definition.Id, leader.Definition.Leadership!.Value, "A18.12"));
            }

            return drm;
        }

        /// <summary>Casualty Reduction (A7.302): a squad becomes its HS, a HS is eliminated, and a SMC is wounded (A17.11).</summary>
        private bool Reduce(UnitState unit)
        {
            if (unit.Definition.IsLeader || unit.Definition.IsHero)
            {
                var key = unit.Id;
                if (facts.Rolls!.WoundSeverity?.TryGetValue(key, out var dr) != true)
                {
                    pending = Missing("woundSeverity:" + key);
                    return false;
                }

                usedRolls.Add("woundSeverity:" + key);
                if (dr + (unit.Wounded ? 1 : 0) >= 5)
                {
                    unit.Eliminate("eliminated-mortal-wound");
                }
                else
                {
                    unit.Wound();
                }

                return true;
            }

            if (unit.Definition.Kind == "asl:half-squad")
            {
                unit.Eliminate("eliminated-casualty-reduction");
                return true;
            }

            unit.ReduceTo(reference.Definitions[ScenarioA1FireReference.HalfSquadOf(unit.Definition.Id)!]);
            return true;
        }

        private List<string> ExtraRolls()
        {
            var rolls = facts.Rolls!;
            var supplied = (rolls.Attacks?.Keys.Select(key => "attack:" + key) ?? [])
                .Concat(rolls.RandomSelection?.Keys.Select(key => "randomSelection:" + key) ?? [])
                .Concat(rolls.WoundSeverity?.Keys.Select(key => "woundSeverity:" + key) ?? [])
                .Concat(rolls.LeaderCreation?.Keys.Select(key => "leaderCreation:" + key) ?? [])
                .Concat(rolls.WeaponLoss?.Keys.Select(key => "weaponLoss:" + key) ?? []);
            return supplied.Where(key => !usedRolls.Contains(key)).Select(key => "asl.a1.cc.extra-roll:" + key).ToList();
        }
    }

    private sealed class UnitState(CloseCombatUnit facts, FireDefinition definition)
    {
        private readonly List<string> events = [];

        public CloseCombatUnit Facts { get; } = facts;

        public string Id { get; } = facts.UnitId!;

        public FireDefinition Definition { get; private set; } = definition;

        public FireDefinition Printed { get; } = definition;

        /// <summary>A SMC's CC FP is one (A11.14); an MMC's is its printed FP, broken or not (A11.16).</summary>
        public int Firepower => Printed.IsMmc ? Printed.Firepower!.Value : 1;

        public bool IsHeroType => Definition.IsHero || (Definition.IsLeader && Facts.Heroic == true);

        public bool Eliminated { get; private set; }

        public bool Wounded { get; private set; } = facts.Wounded == true;

        public bool BerserkEnded { get; private set; }

        public void Eliminate(string reason)
        {
            Eliminated = true;
            events.Add(reason);
        }

        public void Wound()
        {
            Wounded = true;
            events.Add("wounded");
        }

        public void ReduceTo(FireDefinition half)
        {
            Definition = half;
            events.Add("casualty-reduced");
        }

        public void EndBerserk()
        {
            BerserkEnded = true;
            events.Add("berserk-ended");
        }

        public CloseCombatUnitEffect Effect() => new(Id, Printed.Id, Definition.Id, Eliminated, Wounded && !Eliminated, events.ToArray())
        {
            BerserkEnded = BerserkEnded && !Eliminated ? true : null,
        };
    }
}
