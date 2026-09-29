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

    /// <summary>
    /// Whether an Ambush can occur (A11.4; ruling R14.2): Infantry advanced into CC this APh, not into a Melee, in a woods or building Location or with
    /// or against a concealed unit in any terrain, or a hidden unit was placed there this CCPh. Prisoners are neither side's force.
    /// </summary>
    public static bool AmbushPossible(string? terrain, IReadOnlyList<CloseCombatUnit> units, bool hiddenPlaced = false)
    {
        ArgumentNullException.ThrowIfNull(units);
        var force = units.Where(unit => unit.Captured != true).ToArray();
        return force.All(unit => unit.InMelee != true) && force.Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count() == 2
            && (hiddenPlaced || (force.Any(unit => unit.Advanced == true)
                && (AmbushTerrain.Contains(terrain, StringComparer.Ordinal) || force.Any(unit => unit.Concealed == true))));
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
        if (reasons.Count == 0 && !AmbushPossible(facts.Terrain, units, facts.HiddenPlaced == true))
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

        units = [.. units.Where(unit => unit.Captured != true)];
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

        // A11.4: a side whose Final dr is at least three below the other's ambushes it; E1.77 (ruling R16.7; table player, pass 16): the ATTACKER's
        // need be only two below at night, unless Illuminated.
        int Margin(AmbushSide side) => facts.DarkNight == true && side.Side == facts.AttackerSide ? 2 : 3;
        var ambusher = results[0].FinalDr <= results[1].FinalDr - Margin(results[0]) ? results[0].Side
            : results[1].FinalDr <= results[0].FinalDr - Margin(results[1]) ? results[1].Side
            : null;
        return new AmbushResolution(CloseCombatResolution.Resolved, [], results, ambusher);
    }

    /// <summary>
    /// A side's Ambush drm (A11.4, A11.17, A11.18): each cause once when any unit of its force has it (rulings R29.9, R14.13): +1 broken,
    /// +1 pinned, +1 berserk, +1 Lax (Inexperienced, A19.36, or berserk, A15.432), -1 Stealthy (a Good Order hero or heroic leader,
    /// A15.24, A15.21), -2 concealed (ruling R14.2),
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

        // A4.51, A11.4 (ruling R5.2): +1 when any of the force is CX.
        if (Any(unit => unit.Cx == true))
        {
            drm.Add(new FireModifier("cx", 1m, "A4.51"));
        }

        if (berserk || Any(unit => Inexperienced(unit, reference)))
        {
            drm.Add(new FireModifier("lax", 1m, "A11.18"));
        }

        // A11.4 (ruling R14.2): -2 when any of the force is concealed.
        if (Any(unit => unit.Concealed == true))
        {
            drm.Add(new FireModifier("concealed", -2m, "A11.4"));
        }

        if (Any(unit => (reference.Definitions[unit.DefinitionId!].IsHero || (reference.Definitions[unit.DefinitionId!].IsLeader && unit.Heroic == true)) && GoodOrder(unit)))
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
        && (definition.Class == "conscript" || unit.Unarmed == true || (definition.Class == "green" && unit.Inexperienced == true));

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
    /// reason other than a missing roll depends on the facts alone, so the facts must stop at the first roll (an escape NTC or the
    /// first attack's DR), or resolve at once when the round declares no attack.
    /// </summary>
    public static IReadOnlyList<string> Precheck(CloseCombatFacts facts, ScenarioA1CloseCombatReference reference)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(reference);
        var first = Resolve(facts with
        {
            Rolls = new CloseCombatRolls(null)
        }, reference);
        return first.Disposition == CloseCombatResolution.Resolved || first.Reasons is [{ } missing] && missing.StartsWith("asl.a1.cc.roll-missing:", StringComparison.Ordinal)
            ? []
            : first.Reasons;
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

        // A20.5 (ruling R14.5): a prisoner's Guard is a unit of the other side in the Location.
        if (units.Any(unit => unit.Captured == true && (unit.GuardId is not { } guard || units.FirstOrDefault(other => other.UnitId == guard) is not { } found
            || found.Side == unit.Side || found.Captured == true)))
        {
            outside.Add("asl.a1.cc.prisoner-guard-outside");
        }

        return outside;
    }

    /// <summary>
    /// A side's overstacking excess in the Location (A5.1, A5.5; ruling R14.10): its squad-equivalents above three, rounded up, a HS or crew half,
    /// each five SMC a HS and four or fewer none; prisoners never count (A20.51). Unarmed MMC are Inexperienced (A19.3; referee, pass 14).
    /// </summary>
    public static int Overstack(IEnumerable<CloseCombatUnit> units, string side, ScenarioA1CloseCombatReference reference)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(reference);
        var definitions = units.Where(unit => unit.Side == side && unit.Captured != true).Select(unit => reference.Definitions[unit.DefinitionId!]).ToArray();
        var smc = definitions.Count(item => item.IsLeader || item.IsHero);
        var squads = definitions.Count(item => item.Kind == "asl:squad") + (definitions.Count(item => item.Kind is "asl:half-squad" or "asl:crew") / 2m)
            + (Math.Floor(smc / 5m) / 2m);
        return squads > 3 ? (int)Math.Ceiling(squads - 3) : 0;
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
        if (round is not (CloseCombatFacts.Simultaneous or CloseCombatFacts.AmbusherRound or CloseCombatFacts.AmbushedRound or CloseCombatFacts.PrisonersRound)
            || (round == CloseCombatFacts.AmbusherRound && !units.Any(unit => unit.Side == facts.Ambusher))
            || (round == CloseCombatFacts.AmbushedRound && facts.Ambusher is null)
            || (round == CloseCombatFacts.PrisonersRound && (facts.AttackingBefore!.Count > 0 || facts.AttackedBefore!.Count > 0)))
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

            // A11.14: a SMC combines with MMC only by attacking with the MMC it is stacked with; a SMC that attacks alone defends alone.
            if (attackers.Any(id => byId[id] is { StackedWith: null } smc && !reference.Definitions[smc.DefinitionId!].IsMmc)
                && attackers.Any(id => reference.Definitions[byId[id].DefinitionId!].IsMmc))
            {
                outside.Add($"asl.a1.cc.stacking-outside:{index}");
            }

            // A11.2: a withdrawing unit makes no CC attack.
            if (attackers.Any(id => byId[id].WithdrawingTo is not null))
            {
                outside.Add($"asl.a1.cc.withdrawing-attacker:{index}");
            }

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

            // A20.55, A20.54 (rulings R14.5, R14.6): prisoners attack only in their escape round, each attack taking in the Guard of each of its
            // attackers, whose Guard is broken or held in Melee; they are never attacked, and a CC attack against a Guard does not affect them.
            if (round == CloseCombatFacts.PrisonersRound)
            {
                if (attackers.Any(id => byId[id] is not { Captured: true, GuardId: { } guard } || !byId.TryGetValue(guard, out var custodian)
                    || !(custodian.Broken == true || custodian.InMelee == true) || !defenders.Contains(guard)) || defenders.Any(id => byId[id].Captured == true))
                {
                    outside.Add($"asl.a1.cc.escape-outside:{index}");
                }
            }
            else if (attackers.Concat(defenders).Any(id => byId[id].Captured == true))
            {
                outside.Add($"asl.a1.cc.prisoner-outside:{index}");
            }

            // A20.22, A20.2 (ruling R14.4): a capture attempt is made by and against units none of which is berserk; the defenders' order of choice
            // names only defenders, and the Guard is an armed attacker.
            if (attack.Capture == true && attackers.Concat(defenders).Any(id => byId[id].Berserk == true))
            {
                outside.Add($"asl.a1.cc.capture-outside:{index}");
            }

            if (attack.Yield is { } yielded && (attack.Capture != true || yielded.Any(id => !defenders.Contains(id)) || yielded.Distinct(StringComparer.Ordinal).Count() != yielded.Count))
            {
                outside.Add($"asl.a1.cc.capture-outside:{index}");
            }

            if (attack.Guard is { } named && (attack.Capture != true || !attackers.Contains(named) || byId[named].Unarmed == true))
            {
                outside.Add($"asl.a1.cc.capture-outside:{index}");
            }

            // A11.141: the director is a Good Order, unpinned leader of the attack (ruling R29.8).
            if (attack.Director is { } director && (!attackers.Contains(director) || !reference.Definitions[byId[director].DefinitionId!].IsLeader
                || byId[director].Berserk == true || byId[director].Pinned == true))
            {
                outside.Add($"asl.a1.cc.director-outside:{index}");
            }
        }

        var attackingSides = round switch
        {
            CloseCombatFacts.AmbusherRound => [facts.Ambusher!],
            CloseCombatFacts.AmbushedRound => units.Select(unit => unit.Side!).Where(side => side != facts.Ambusher).Distinct(StringComparer.Ordinal).ToArray(),
            _ => units.Select(unit => unit.Side!).Distinct(StringComparer.Ordinal).ToArray(),
        };

        // A11.22 (ruling R14.8): Infiltration is declared by a unit that is neither berserk nor Disrupted (A11.2 EXC) and is not withdrawing.
        if (units.Any(unit => unit.InfiltrateTo is not null && (unit.Berserk == true || unit.Disrupted == true || unit.WithdrawingTo is not null)))
        {
            outside.Add("asl.a1.cc.infiltration-outside");
        }

        // A11.2: only a unit held in Melee withdraws, never a pinned, berserk, or Disrupted one, and a SMC withdraws with the MMC it is
        // stacked with or declared alone.
        if (units.Any(unit => unit.WithdrawingTo is not null && (unit.InMelee != true || unit.Pinned == true || unit.Berserk == true || unit.Disrupted == true
            || (unit.StackedWith is { } mmc && byId.TryGetValue(mmc, out var under) && under.WithdrawingTo is null))))
        {
            outside.Add("asl.a1.cc.withdrawal-outside");
        }

        // The ambusher's attacks are sequential (A11.3), so its units that must attack are checked when the ambushed side's round begins.
        // The prisoners' round comes before the others, which check the units that must attack (ruling R14.6).
        string[] mustSides = round is CloseCombatFacts.AmbusherRound or CloseCombatFacts.PrisonersRound ? [] : [.. units.Select(unit => unit.Side!).Distinct(StringComparer.Ordinal)];

        // A11.15: a unit that advanced this APh into a Location already in Melee engages in CC: it attacks in its side's round; a concealed one
        // may decline to keep its "?" (ruling R14.2).
        var reinforcing = units.Any(unit => unit.InMelee == true);
        if (reinforcing && units.Any(unit => unit.Advanced == true && unit.Broken != true && unit.Captured != true && unit.Concealed != true && mustSides.Contains(unit.Side)
            && !attacking.Contains(unit.UnitId!)))
        {
            outside.Add("asl.a1.cc.reinforcement-must-attack");
        }

        // A15.43: a berserk unit charges to destroy the enemy in CC, so it attacks in its side's round (ruling R29.15).
        if (units.Any(unit => unit.Berserk == true && mustSides.Contains(unit.Side) && !attacking.Contains(unit.UnitId!)))
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
        new[] { rolls.Attacks, rolls.EscapeNtc }.Any(map => map?.Values.Any(dice => dice.Count != 2 || dice.Any(die => die is < 1 or > 6)) == true)
        || new[] { rolls.RandomSelection, rolls.WoundSeverity, rolls.LeaderCreation, rolls.WeaponLoss, rolls.LeaderStack }.Any(map => map?.Values.Any(dr => dr is < 1 or > 6) == true);

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

        // A18.12, A18.2 (ruling R14.12): an Original 2 by an attacking MMC creates a leader by a dr whose drm depend on the base MMC, the one of the
        // highest BPV, so each MMC of an attack mixing definitions needs its BPV.
        foreach (var (attack, index) in facts.Attacks!.Select((item, index) => (item, index)))
        {
            var mmcs = attack.Attackers!.Select(id => byId[id]).Where(unit => reference.Definitions[unit.DefinitionId!].IsMmc).ToArray();
            if (mmcs.Length == 0)
            {
                continue;
            }

            if (mmcs.Select(unit => unit.DefinitionId).Distinct(StringComparer.Ordinal).Count() > 1
                && mmcs.FirstOrDefault(unit => !reference.Bpv.ContainsKey(unit.DefinitionId!)) is { } priceless)
            {
                undecided.Add("asl.a1.cc.field-promotion-bpv-missing:" + priceless.DefinitionId);
            }

            // A20.551 (ruling R14.6): an attacking Unarmed MMC may be rearmed as a Conscript MMC of its size and nationality.
            foreach (var unarmed in mmcs.Where(unit => unit.Unarmed == true))
            {
                if (ConscriptOf(reference, reference.Definitions[unarmed.DefinitionId!]) is null)
                {
                    undecided.Add("asl.a1.cc.rearm-counter-missing:" + unarmed.DefinitionId);
                }
            }
        }

        // A20.22 (ruling R14.4): the defender's choice of the unit captured at the Kill Number is declared for an attack on several units.
        foreach (var (attack, index) in facts.Attacks!.Select((item, index) => (item, index)))
        {
            if (attack.Capture == true && attack.Defenders!.Count > 1 && (attack.Yield is null || attack.Defenders.Any(id => !attack.Yield.Contains(id))))
            {
                undecided.Add($"asl.a1.cc.capture-choice-undecided:{index}");
            }
        }

        return undecided.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>The Conscript MMC of a MMC's nationality and size (A20.551; ruling R14.6): a squad for a squad, a HS for a HS or crew.</summary>
    private static string? ConscriptOf(ScenarioA1CloseCombatReference reference, FireDefinition unit) =>
        reference.Definitions.Values.Where(item => item.Nationality == unit.Nationality && item.Class == "conscript"
            && item.Kind == (unit.Kind == "asl:squad" ? "asl:squad" : "asl:half-squad")).Select(item => item.Id).Order(StringComparer.Ordinal).FirstOrDefault();

    /// <summary>A unit's US# (A1.6, A20.51): a squad 3, a HS or crew 2, a SMC 1.</summary>
    private static int Size(FireDefinition definition) => definition.Kind == "asl:squad" ? 3 : definition.Kind is "asl:half-squad" or "asl:crew" ? 2 : 1;

    /// <summary>The Morale Level of a MMC as Leader Creation reads it (A18.2): berserk 10 (A15.42), one higher when Fanatic (A10.8).</summary>
    private static int BaseMorale(CloseCombatUnit unit, ScenarioA1CloseCombatReference reference) =>
        (unit.Berserk == true ? 10 : reference.Definitions[unit.DefinitionId!].Morale!.Value) + (unit.Fanatic == true ? 1 : 0);

    /// <summary>
    /// One round of CC in a Location, with the units' changing state. The ATTACKER's attacks are resolved before the DEFENDER's (A11.12), which
    /// decides what an Infiltration withdraws from (A11.22; ruling R14.8); otherwise the results take effect together.
    /// </summary>
    private sealed class Round(CloseCombatFacts facts, ScenarioA1CloseCombatReference reference)
    {
        private readonly HashSet<string> usedRolls = new(StringComparer.Ordinal);
        private readonly Dictionary<string, UnitState> units = facts.Units!.ToDictionary(unit => unit.UnitId!, unit => new UnitState(unit, reference.Definitions[unit.DefinitionId!]),
            StringComparer.Ordinal);

        private readonly List<CloseCombatCreatedLeader> created = [];

        // A18.12 (referee, pass 14): the leaders whose MMC infiltrated; no attack but the one that created them is re-figured for them.
        private readonly HashSet<string> goneLeaders = new(StringComparer.Ordinal);

        // A11.14 EXC (ruling R14.2): a concealed unit declared stacked with a Known SMC or MMC forfeits its concealment before the attacks.
        private readonly HashSet<string> forfeited = new(StringComparer.Ordinal);

        private CloseCombatResolution? pending;

        public CloseCombatResolution Run()
        {
            var attacks = facts.Attacks!;
            var rolls = facts.Rolls!;
            foreach (var smc in facts.Units!.Where(unit => unit.StackedWith is not null))
            {
                var mmc = units[smc.StackedWith!].Facts;
                if ((smc.Concealed == true) != (mmc.Concealed == true))
                {
                    forfeited.Add(smc.Concealed == true ? smc.UnitId! : mmc.UnitId!);
                }
            }

            // A20.55 (ruling R14.6): each escaping prisoner whose Guard is not held in Melee first passes a NTC; one that fails does not attack.
            var checks = new List<CloseCombatEscapeCheck>();
            var failed = new HashSet<string>(StringComparer.Ordinal);
            if (facts.Round == CloseCombatFacts.PrisonersRound)
            {
                foreach (var id in attacks.SelectMany(item => item.Attackers!).Distinct(StringComparer.Ordinal))
                {
                    if (units[units[id].Facts.GuardId!].Facts.InMelee == true)
                    {
                        continue;
                    }

                    if (rolls.EscapeNtc?.TryGetValue(id, out var ntc) != true)
                    {
                        return Missing("escapeNtc:" + id);
                    }

                    usedRolls.Add("escapeNtc:" + id);
                    var morale = units[id].Printed.Morale!.Value;
                    var passed = ntc!.Sum() <= morale;
                    checks.Add(new CloseCombatEscapeCheck(id, [.. ntc!], morale, passed));
                    if (!passed)
                    {
                        failed.Add(id);
                    }
                }
            }

            // A11.12: the ATTACKER's attacks first, then the DEFENDER's, each in its declared order.
            int[] order = [.. Enumerable.Range(0, attacks.Count).OrderBy(index => units[attacks[index].Attackers![0]].Facts.Side == facts.AttackerSide ? 0 : 1)];
            var position = order.Select((index, at) => (index, at)).ToDictionary(item => item.index, item => item.at);
            var departed = new Dictionary<string, int>(StringComparer.Ordinal);
            var dice = new Dictionary<int, IReadOnlyList<int>>();
            var leaders = new Dictionary<int, LeaderCreationOutcome>();
            var extra = new Dictionary<int, UnitState>();
            var firstPass = new Dictionary<int, CloseCombatAttackArithmetic>();

            bool Gone(string id, int at) => departed.TryGetValue(id, out var left) && left < at;
            List<string> ActiveAttackers(int index) => [.. attacks[index].Attackers!.Where(id => !failed.Contains(id) && !Gone(id, position[index]))];
            List<string> ActiveDefenders(int index) => [.. attacks[index].Defenders!.Where(id => !Gone(id, position[index]))];

            // Each attack's DR as its turn comes: an attack none of whose attackers or defenders is left is forfeited (A11.22), and an Original 2 by an
            // attacking MMC calls for a Leader Creation dr at once (A18.12).
            foreach (var index in order)
            {
                if (ActiveAttackers(index).Count == 0 || ActiveDefenders(index).Count == 0)
                {
                    if (rolls.Attacks?.ContainsKey(Key(index)) == true)
                    {
                        usedRolls.Add("attack:" + Key(index));
                    }

                    continue;
                }

                if (rolls.Attacks?.TryGetValue(Key(index), out var drawn) != true)
                {
                    return Missing("attack:" + Key(index));
                }

                usedRolls.Add("attack:" + Key(index));
                dice[index] = drawn!;
                // A25.71 (backlog pass 15, ruling R15.13): the Finns create no leader.
                var mmcs = ActiveAttackers(index).Select(id => units[id]).Where(unit => unit.Definition.IsMmc).ToArray();
                if (drawn!.Sum() == 2 && mmcs.Length > 0 && ScenarioA1FieldPromotion.Applicable(mmcs[0].Definition.Nationality))
                {
                    if (rolls.LeaderCreation?.TryGetValue(Key(index), out var dr) != true)
                    {
                        return Missing("leaderCreation:" + Key(index));
                    }

                    usedRolls.Add("leaderCreation:" + Key(index));
                    var odds = Odds(index, ActiveAttackers(index), ActiveDefenders(index), null);
                    var columnsBelow = Math.Max(0, reference.OneToOneColumn - odds.Column);
                    List<FireModifier> oddsDrm = columnsBelow > 0 ? [new FireModifier("odds-below-1-1", -columnsBelow, "A18.2")] : [];

                    // A18.2 (ruling R14.12): the MMC of the highest BPV founds the leader; he defends with one MMC of the attack, by Random Selection
                    // when an enemy attack takes some of them and not the others.
                    var founder = mmcs.OrderByDescending(unit => reference.Bpv.GetValueOrDefault(unit.Definition.Id)).First();
                    var (outcome, reason) = ScenarioA1FieldPromotion.Create(founder.Definition, BaseMorale(founder.Facts, reference), dr, oddsDrm, reference.Definitions,
                        "asl.a1.cc", founder.Facts.Fanatic == true);
                    if (outcome is null)
                    {
                        return Refused(CloseCombatResolution.Indeterminate, [reason!]);
                    }

                    leaders[index] = outcome;
                    if (outcome.LeaderDefinitionId is { } leader)
                    {
                        var stack = mmcs[0];
                        if (mmcs.Length > 1 && attacks.Any(other => mmcs.Count(unit => other.Defenders!.Contains(unit.Id)) is var count && count > 0 && count < mmcs.Length))
                        {
                            var key = $"leaderStack:{Key(index)}:{string.Join(",", mmcs.Select(unit => unit.Id))}";
                            var selection = new Dictionary<string, int>(StringComparer.Ordinal);
                            foreach (var unit in mmcs)
                            {
                                if (rolls.LeaderStack?.TryGetValue(Key(index) + ":" + unit.Id, out var pick) != true)
                                {
                                    return Missing(key);
                                }

                                usedRolls.Add($"leaderStack:{Key(index)}:{unit.Id}");
                                selection[unit.Id] = pick;
                            }

                            // A.9: the highest dr; ties go to the first in the attack's declared order (a reading).
                            stack = mmcs.First(unit => selection[unit.Id] == selection.Values.Max());
                        }

                        var id = CloseCombatCreatedLeader.PlaceholderPrefix + Key(index);
                        var state = new UnitState(new CloseCombatUnit(id, leader, stack.Facts.Side, false, false, false, false, false, stack.Facts.Fanatic, false, false,
                            false, false, false), reference.Definitions[leader]);
                        units[id] = state;
                        extra[index] = state;
                        created.Add(new CloseCombatCreatedLeader(index, id, leader, stack.Facts.Side!, stack.Id, false, false));
                    }
                }

                // A11.22 (ruling R14.8): the departures this attack allows, read from the attacks resolved so far.
                var figured = Figure(index, ActiveAttackers(index), ActiveDefenders(index), extra, dice[index]);
                firstPass[index] = figured;
                var at = position[index];
                if (drawn!.Sum() == 2)
                {
                    foreach (var id in ActiveAttackers(index).Where(id => units[id].Facts.InfiltrateTo is not null && units[id].Facts.Pinned != true
                        && !firstPass.Where(item => position[item.Key] < at).Any(item => item.Value.Defending.Any(result => result.UnitId == id
                            && result.Result is CloseCombatDefenderResult.Eliminated or CloseCombatDefenderResult.Captured))))
                    {
                        departed[id] = at;
                        if (extra.TryGetValue(index, out var joined) && created.First(item => item.Attack == index).StackedWith == id)
                        {
                            departed[joined.Id] = at;
                            goneLeaders.Add(joined.Id);
                        }
                    }
                }
                else if (drawn!.Sum() == 12)
                {
                    foreach (var result in figured.Defending.Where(item => item.Result is not (CloseCombatDefenderResult.Eliminated or CloseCombatDefenderResult.Captured)
                        && units.TryGetValue(item.UnitId, out var unit) && unit.Facts.InfiltrateTo is not null && unit.Facts.Pinned != true
                        && !firstPass.Where(earlier => position[earlier.Key] < at).Any(earlier => earlier.Value.Defending.Any(other => other.UnitId == item.UnitId
                            && other.Result is CloseCombatDefenderResult.Eliminated or CloseCombatDefenderResult.Captured))))
                    {
                        departed[result.UnitId] = at;
                    }
                }
            }

            // Every attack is figured again with each created leader as if he had been there all along (A18.12), the departures kept.
            var arithmetic = new List<CloseCombatAttackArithmetic>();
            var partial = new List<(int Attack, UnitState Unit)>();
            foreach (var index in dice.Keys.OrderBy(index => index))
            {
                var figured = Figure(index, ActiveAttackers(index), ActiveDefenders(index), extra, dice[index]) with
                {
                    LeaderCreation = leaders.GetValueOrDefault(index)
                };
                var candidates = figured.Defending.Where(item => item.Result == CloseCombatDefenderResult.PartialKill).Select(item => units[item.UnitId]).ToList();
                var defending = figured.Defending.ToList();
                if (attacks[index].Capture == true)
                {
                    // A20.22 (ruling R14.4): at the Kill Number one unit of the defender's choice is captured, the others unharmed.
                    candidates = [.. candidates.Where(item => !item.Id.StartsWith(CloseCombatCreatedLeader.PlaceholderPrefix, StringComparison.Ordinal))];
                    var chosen = candidates.Count == 0 ? null
                        : candidates.Count == 1 ? candidates[0]
                        : units[attacks[index].Yield!.First(id => candidates.Any(item => item.Id == id))];
                    defending = [.. defending.Select(item => item.Result != CloseCombatDefenderResult.PartialKill ? item
                        : item.UnitId == chosen?.Id ? item with { Result = CloseCombatDefenderResult.Captured, CasualtyReduced = null }
                        : item with { Result = CloseCombatDefenderResult.NoEffect })];
                    if (chosen is not null && chosen.Definition.Kind == "asl:squad")
                    {
                        chosen.CaptureHalf();
                    }
                }
                else if (candidates.Count > 1)
                {
                    // A11.11: a Partial Kill Casualty Reduces one defending unit (or more) chosen by Random Selection (A.9): the highest dr, and every
                    // unit that ties it.
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

                arithmetic.Add(figured with
                {
                    Defending = defending
                });
            }

            // A20.221 (ruling R14.4): a side whose every non-prisoner unit here is eliminated or captured in a simultaneous round captures no one.
            var voided = new HashSet<string>(StringComparer.Ordinal);
            if (facts.Round == CloseCombatFacts.Simultaneous)
            {
                foreach (var side in units.Values.Where(unit => unit.Facts.Captured != true).Select(unit => unit.Facts.Side!).Distinct(StringComparer.Ordinal).ToArray())
                {
                    var lost = units.Values.Where(unit => unit.Facts.Side == side && unit.Facts.Captured != true).All(unit => departed.ContainsKey(unit.Id) is false
                        && arithmetic.Any(attack => attack.Defending.Any(item => item.UnitId == unit.Id && item.Result is CloseCombatDefenderResult.Eliminated
                            or CloseCombatDefenderResult.Captured)));
                    if (lost)
                    {
                        foreach (var attack in arithmetic.Where(item => item.Side == side))
                        {
                            voided.UnionWith(attack.Defending.Where(item => item.Result == CloseCombatDefenderResult.Captured).Select(item => item.UnitId));
                        }
                    }
                }
            }

            arithmetic = [.. arithmetic.Select(attack => attack with
            {
                Defending = [.. attack.Defending.Select(item => item.Result == CloseCombatDefenderResult.Captured && voided.Contains(item.UnitId)
                    ? item with { Result = CloseCombatDefenderResult.NoEffect }
                    : item)],
            })];
            foreach (var id in voided)
            {
                units[id].UndoHalf();
            }

            // The results take effect together (A11.12): eliminations and captures, then Casualty Reductions (A7.302; A17.11 for a SMC).
            foreach (var attack in arithmetic)
            {
                foreach (var defender in attack.Defending)
                {
                    if (defender.Result == CloseCombatDefenderResult.Eliminated)
                    {
                        units[defender.UnitId].Eliminate("eliminated-cc");
                    }
                    else if (defender.Result == CloseCombatDefenderResult.Captured && !units[defender.UnitId].Eliminated)
                    {
                        units[defender.UnitId].Capture();
                    }
                }
            }

            foreach (var (_, unit) in partial.Where(item => !item.Unit.Eliminated && !item.Unit.Captured))
            {
                if (!Reduce(unit))
                {
                    return pending!;
                }
            }

            // A11.13: each SW of a unit eliminated in CC is lost too on an Original colored dr of 1 and a dr at most the attack's black Kill Number;
            // any other stays in the Location unpossessed, as do a captured unit's (A20.24).
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

            // A15.46: a berserk unit returns to normal when its group's attack eliminated every enemy unit in its Location that was
            // eliminated, at least one, and none is left (ruling R29.15).
            foreach (var attack in arithmetic)
            {
                var berserk = attack.Attackers.Select(id => units[id]).Where(unit => unit.Facts.Berserk == true && !unit.Eliminated).ToArray();
                var enemies = units.Values.Where(unit => unit.Facts.Side != attack.Side && unit.Facts.Captured != true).ToArray();
                var enemiesLeft = enemies.Any(unit => !unit.Eliminated);
                var byThisGroup = enemies.Where(unit => unit.Eliminated).All(unit => attack.Defenders.Contains(unit.Id));
                if (berserk.Length > 0 && !enemiesLeft && byThisGroup && attack.Defending.Any(item => units[item.UnitId].Eliminated))
                {
                    foreach (var unit in berserk)
                    {
                        unit.EndBerserk();
                    }
                }
            }

            foreach (var (id, _) in departed.Where(item => units[item.Key].Present))
            {
                units[id].Infiltrate();
            }

            if (facts.Round == CloseCombatFacts.PrisonersRound)
            {
                foreach (var unit in attacks.SelectMany(item => item.Attackers!).Distinct(StringComparer.Ordinal).Where(id => !failed.Contains(id)).Select(id => units[id]))
                {
                    unit.Escape();
                }
            }

            Guards(arithmetic);
            Rearm(arithmetic);
            Concealment(arithmetic);

            // A20.55: an escape succeeds when no enemy unit but a prisoner is left in the Location, or by Infiltration; only then is a SMC Armed (A20.551).
            foreach (var unit in units.Values.Where(unit => unit.Escaping))
            {
                unit.EscapeSucceeds(unit.InfiltratedTo is not null
                    || !units.Values.Any(other => other.Facts.Side != unit.Facts.Side && other.Facts.Captured != true && other.Present));
            }

            var extraRolls = ExtraRolls();
            if (extraRolls.Count != 0)
            {
                return Refused(CloseCombatResolution.Abstained, extraRolls);
            }

            var effects = units.Values.Where(unit => !unit.Id.StartsWith(CloseCombatCreatedLeader.PlaceholderPrefix, StringComparison.Ordinal))
                .Select(unit => unit.Effect()).ToArray();
            var leadersCreated = created.Select(item => units[item.UnitId] is var state ? item with { Eliminated = state.Eliminated, Wounded = state.Wounded && !state.Eliminated } : item)
                .ToArray();
            return new CloseCombatResolution(CloseCombatResolution.Resolved, [], arithmetic, effects, leadersCreated, weaponEffects)
            {
                EscapeChecks = checks.Count > 0 ? checks : null,
            };
        }

        private static string Key(int index) => index.ToString(CultureInfo.InvariantCulture);

        private static CloseCombatResolution Missing(string key) => Refused(CloseCombatResolution.Indeterminate, ["asl.a1.cc.roll-missing:" + key]);

        /// <summary>One attack's odds, Kill Number, DRM, and each defender's result, before any Partial Kill is placed.</summary>
        private CloseCombatAttackArithmetic Figure(int index, IReadOnlyList<string> attackerIds, IReadOnlyList<string> defenderIds, Dictionary<int, UnitState> extra,
            IReadOnlyList<int> dice)
        {
            var attack = facts.Attacks![index];
            var odds = Odds(index, attackerIds, defenderIds, extra);
            var drm = CommonDrm(index, attack, attackerIds, extra);
            var original = dice.Sum();
            var defenders = Defenders(defenderIds, extra);
            var defending = new List<CloseCombatDefenderResult>();
            var side = units[attack.Attackers![0]].Facts.Side!;
            foreach (var defender in defenders)
            {
                // A11.16: the -2 against a broken unit applies to it alone (as A4.8 EX reads a status DRM in CC); A11.2: -2 against a
                // withdrawing unit, +1 for each friendly unit in the Melee not withdrawing.
                List<FireModifier> own = defender.Facts.Broken == true ? [new FireModifier("vs-broken", -2m, "A11.16")] : [];

                // A4.51 (ruling R5.2): -1 to a CC attack against a CX unit, for that unit alone; A4.8 (ruling R14.3): -1 against a TI unit.
                if (defender.Facts.Cx == true)
                {
                    own.Add(new FireModifier("vs-cx", -1m, "A4.51"));
                }

                if (defender.Facts.Ti == true)
                {
                    own.Add(new FireModifier("vs-ti", -1m, "A4.8"));
                }

                if (defender.Facts.WithdrawingTo is not null)
                {
                    own.Add(new FireModifier("vs-withdrawing", -2m, "A11.2"));
                    var covering = units.Values.Count(other => other.Facts.Side == defender.Facts.Side && other.Facts.WithdrawingTo is null && other.Facts.Captured != true
                        && !other.Id.StartsWith(CloseCombatCreatedLeader.PlaceholderPrefix, StringComparison.Ordinal));
                    if (covering > 0)
                    {
                        own.Add(new FireModifier("covering", covering, "A11.2"));
                    }
                }

                // A5.131 (ruling R14.10): -1 per squad-equivalent by which the defender's side overstacks the Location.
                if (Overstack(facts.Units!, defender.Facts.Side!, reference) is var excess and > 0)
                {
                    own.Add(new FireModifier("vs-overstacked", -excess, "A5.131"));
                }

                // A20.22, A19.35 (ruling R14.4): a capture attempt adds one, or subtracts one against an Inexperienced unit.
                if (attack.Capture == true)
                {
                    own.Add(Inexperienced(defender.Facts, reference) ? new FireModifier("capture-vs-inexperienced", -1m, "A19.35") : new FireModifier("capture", 1m, "A20.22"));
                }

                var final = original + (int)drm.Concat(own).Sum(item => item.Value);
                // A created leader is not captured: the attempt eliminates him instead (a reading; he has no counter yet to exchange).
                var capturing = attack.Capture == true && !defender.Id.StartsWith(CloseCombatCreatedLeader.PlaceholderPrefix, StringComparison.Ordinal);
                var result = final < odds.Kill ? (capturing ? CloseCombatDefenderResult.Captured : CloseCombatDefenderResult.Eliminated)
                    : final == odds.Kill ? CloseCombatDefenderResult.PartialKill
                    : CloseCombatDefenderResult.NoEffect;
                defending.Add(new CloseCombatDefenderResult(defender.Id, own, final, result));
            }

            return new CloseCombatAttackArithmetic(index, side, [.. Attackers(index, attackerIds, extra).Select(item => item.Id)], [.. defenders.Select(item => item.Id)],
                odds.Modifiers, odds.Attack, odds.Defense, odds.Label, odds.Kill, dice.ToArray(), original, drm, defending);
        }

        private IEnumerable<UnitState> Attackers(int index, IReadOnlyList<string> ids, Dictionary<int, UnitState>? extra) =>
            ids.Select(id => units[id]).Concat(extra?.TryGetValue(index, out var leader) == true && ids.Contains(created.First(item => item.Attack == index).StackedWith)
                ? [leader]
                : []);

        /// <summary>An attack's defenders, with a created leader who defends with one of them (A18.12).</summary>
        private List<UnitState> Defenders(IReadOnlyList<string> ids, Dictionary<int, UnitState>? extra)
        {
            var defenders = ids.Select(id => units[id]).ToList();
            foreach (var (source, leader) in extra ?? [])
            {
                var stack = created.First(item => item.Attack == source).StackedWith;
                if (defenders.Any(unit => unit.Id == stack) && !goneLeaders.Contains(leader.Id))
                {
                    defenders.Add(leader);
                }
            }

            return defenders;
        }

        /// <summary>
        /// The odds of an attack (A11.11): the attackers' FP against the defenders', a SMC and an Unarmed unit counting one (A11.14, A20.5), a pinned
        /// attacker half and a Guard half against non-prisoners (the CCT, A20.52), the whole halved against a concealed defender (A11.19; ruling
        /// R14.2), rounded down to the printed column, with the red Kill Number in Hand-to-Hand CC (ruling R14.1).
        /// </summary>
        private (decimal Attack, decimal Defense, string Label, int Kill, int Column, IReadOnlyList<FireModifier> Modifiers) Odds(int index,
            IReadOnlyList<string> attackerIds, IReadOnlyList<string> defenderIds, Dictionary<int, UnitState>? extra)
        {
            var modifiers = new List<FireModifier>();
            decimal attack = 0;
            foreach (var unit in Attackers(index, attackerIds, extra))
            {
                decimal fp = unit.Firepower;
                if (unit.Facts.Pinned == true)
                {
                    fp /= 2;
                    modifiers.Add(new FireModifier("pinned:" + unit.Id, 0.5m, "A7.8"));
                }

                if (units.Values.Any(other => other.Facts.Captured == true && other.Facts.GuardId == unit.Id) && facts.Round != CloseCombatFacts.PrisonersRound)
                {
                    fp /= 2;
                    modifiers.Add(new FireModifier("guarding:" + unit.Id, 0.5m, "A20.52"));
                }

                attack += fp;
            }

            var defenders = Defenders(defenderIds, extra);
            if (defenders.Any(unit => unit.Facts.Concealed == true && !forfeited.Contains(unit.Id)))
            {
                attack /= 2;
                modifiers.Add(new FireModifier("vs-concealed", 0.5m, "A11.19"));
            }

            var defense = defenders.Sum(unit => (decimal)unit.Firepower);
            var (label, kill, column) = reference.Column(attack, defense);
            return (attack, defense, label, facts.HandToHand == true ? reference.RedKill(column) : kill, column, modifiers);
        }

        /// <summary>
        /// The DRM of an attack for every defender: the Ambush -1 by and +1 against the ambusher (A11.4), the director's leadership unless he attacks
        /// alone or with a berserk unit (A11.141), each hero's -1 (A15.24), a created leader's leadership (A18.12), CX and TI once (A4.51, A4.8), and
        /// +1 per squad-equivalent the attacking side overstacks the Location by (A5.12).
        /// </summary>
        private List<FireModifier> CommonDrm(int index, CloseCombatDeclaration attack, IReadOnlyList<string> attackerIds, Dictionary<int, UnitState> extra)
        {
            var drm = new List<FireModifier>();
            var side = units[attack.Attackers![0]].Facts.Side;
            if (facts.Ambusher is { } ambusher && facts.Round is CloseCombatFacts.AmbusherRound or CloseCombatFacts.AmbushedRound)
            {
                drm.Add(side == ambusher ? new FireModifier("ambush", -1m, "A11.4") : new FireModifier("vs-ambush", 1m, "A11.4"));
            }

            var attackers = attackerIds.Select(id => units[id]).ToArray();
            var berserk = attackers.Any(unit => unit.Facts.Berserk == true);

            // A10.7: leadership modifiers are not cumulative, so a leader created by the attack's Original 2, whose modifier A18.12 makes
            // mandatory, takes the director's place (ruling R29.12); A11.141, A15.42: no leadership DRM with a berserk attacker.
            var promoted = extra.ContainsKey(index);
            if (attack.Director is { } director && attackerIds.Contains(director) && attackers.Length > 1 && !berserk && !promoted)
            {
                drm.Add(new FireModifier("leadership:" + director, Leadership(units[director].Facts, reference), "A11.141"));
            }

            foreach (var unit in attackers.Where(unit => unit.IsHeroType && unit.Id != attack.Director))
            {
                drm.Add(new FireModifier("heroic:" + unit.Id, -1m, "A15.24"));
            }

            // A4.51 (ruling R5.2): +1 to a CC attack a CX unit makes, once however many do; A4.8 (ruling R14.3): the same for a TI unit.
            if (attackers.FirstOrDefault(unit => unit.Facts.Cx == true) is { } exhausted)
            {
                drm.Add(new FireModifier("cx:" + exhausted.Id, 1m, "A4.51"));
            }

            if (attackers.FirstOrDefault(unit => unit.Facts.Ti == true) is { } busy)
            {
                drm.Add(new FireModifier("ti:" + busy.Id, 1m, "A4.8"));
            }

            if (Overstack(facts.Units!, side!, reference) is var excess and > 0)
            {
                drm.Add(new FireModifier("overstacked", excess, "A5.12"));
            }

            if (extra.TryGetValue(index, out var leader) && !berserk)
            {
                drm.Add(new FireModifier("created-leader:" + leader.Definition.Id, leader.Definition.Leadership!.Value, "A18.12"));
            }

            return drm;
        }

        /// <summary>
        /// The Guard of each captured unit (A20.5, A20.51; ruling R14.4): the unit the capturing side named, else the attack's first attacker, else any
        /// unit of that side here, left in the Location, armed, and with capacity for it; with none it is freed as Unarmed.
        /// </summary>
        private void Guards(IReadOnlyList<CloseCombatAttackArithmetic> arithmetic)
        {
            var load = units.Values.Where(unit => unit.Facts.Captured == true && !unit.Escaping && unit.Facts.GuardId is not null)
                .GroupBy(unit => unit.Facts.GuardId!, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Sum(unit => unit.Size), StringComparer.Ordinal);
            foreach (var attack in arithmetic)
            {
                var declaration = facts.Attacks![attack.Index];
                string[] candidates = [.. new[] { declaration.Guard }.OfType<string>(), .. attack.Attackers,
                    .. units.Values.Where(unit => unit.Facts.Side == attack.Side).Select(unit => unit.Id).Order(StringComparer.Ordinal)];
                foreach (var prisoner in attack.Defending.Where(item => units[item.UnitId].Captured).Select(item => units[item.UnitId]))
                {
                    var guard = candidates.Where(id => !id.StartsWith(CloseCombatCreatedLeader.PlaceholderPrefix, StringComparison.Ordinal))
                        .Select(id => units[id]).FirstOrDefault(unit => unit.Present && unit.Facts.Captured != true && unit.Facts.Unarmed != true
                            && unit.Facts.Berserk != true && unit.Facts.WithdrawingTo is null
                            && load.GetValueOrDefault(unit.Id) + prisoner.PrisonerSize <= 5 * unit.Size);
                    if (guard is not null)
                    {
                        load[guard.Id] = load.GetValueOrDefault(guard.Id) + prisoner.PrisonerSize;
                    }

                    prisoner.GuardedBy(guard?.Id);
                }
            }
        }

        /// <summary>
        /// A20.551 (ruling R14.6): an attacking Unarmed MMC is rearmed as a Conscript MMC for each armed enemy unit of at least its size its attack
        /// eliminated or captured.
        /// </summary>
        private void Rearm(IReadOnlyList<CloseCombatAttackArithmetic> arithmetic)
        {
            foreach (var attack in arithmetic)
            {
                var taken = attack.Defending.Where(item => item.Result is CloseCombatDefenderResult.Eliminated or CloseCombatDefenderResult.Captured
                    && units[item.UnitId].Facts.Unarmed != true && (units[item.UnitId].Eliminated || units[item.UnitId].Captured))
                    .Select(item => units[item.UnitId].Captured ? units[item.UnitId].PrisonerSize : units[item.UnitId].Size).OrderByDescending(size => size).ToList();
                foreach (var size in taken)
                {
                    var rearmed = attack.Attackers.Select(id => units[id]).Where(unit => unit.Facts.Unarmed == true && unit.Printed.IsMmc && unit.Present && unit.RearmedAs is null
                        && unit.Size <= size).OrderByDescending(unit => unit.Size).FirstOrDefault();
                    rearmed?.Rearm(ConscriptOf(reference, rearmed.Definition)!);
                }
            }
        }

        /// <summary>
        /// A11.19, A11.4, A12.14 (ruling R14.2): a concealed unit that makes or directs an attack loses its "?", unless its side has Ambush status and
        /// the attack eliminated or captured every defender; so does a concealed unit Casualty Reduced or wounded.
        /// </summary>
        private void Concealment(IReadOnlyList<CloseCombatAttackArithmetic> arithmetic)
        {
            foreach (var attack in arithmetic)
            {
                var ambushing = facts.Ambusher == attack.Side && facts.Round is CloseCombatFacts.AmbusherRound or CloseCombatFacts.AmbushedRound;
                var clean = attack.Defending.All(item => units[item.UnitId].Eliminated || units[item.UnitId].Captured);
                if (ambushing && clean)
                {
                    continue;
                }

                foreach (var id in attack.Attackers.Append(facts.Attacks![attack.Index].Director).OfType<string>().Where(units.ContainsKey))
                {
                    units[id].Reveal();
                }
            }

            foreach (var unit in units.Values.Where(unit => unit.Reduced || (unit.Wounded && unit.Facts.Wounded != true) || forfeited.Contains(unit.Id)))
            {
                unit.Reveal();
            }
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
                .Concat(rolls.WeaponLoss?.Keys.Select(key => "weaponLoss:" + key) ?? [])
                .Concat(rolls.EscapeNtc?.Keys.Select(key => "escapeNtc:" + key) ?? [])
                .Concat(rolls.LeaderStack?.Keys.Select(key => "leaderStack:" + key) ?? []);
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

        /// <summary>A SMC's and an Unarmed unit's CC FP is one (A11.14, A20.5); an MMC's is its printed FP, broken or not (A11.16).</summary>
        public int Firepower => Facts.Unarmed == true ? 1 : Printed.IsMmc ? Printed.Firepower!.Value : 1;

        public bool IsHeroType => Definition.IsHero || (Definition.IsLeader && Facts.Heroic == true);

        /// <summary>The unit's US# (A1.6): squad 3, HS or crew 2, SMC 1, as it is after a Casualty Reduction this round.</summary>
        public int Size => Size(Definition);

        /// <summary>The US# the unit adds to its Guard's prisoners: one HS of a squad captured at the Kill Number (A20.22 EX).</summary>
        public int PrisonerSize => CapturedHalf ? 2 : Size;

        public bool Eliminated
        {
            get; private set;
        }

        public bool Captured
        {
            get; private set;
        }

        public bool CapturedHalf
        {
            get; private set;
        }

        public bool Reduced => Definition != Printed && RearmedAs is null;

        public bool Wounded { get; private set; } = facts.Wounded == true;

        public bool BerserkEnded
        {
            get; private set;
        }

        public bool Escaping
        {
            get; private set;
        }

        public string? RearmedAs
        {
            get; private set;
        }

        /// <summary>Whether the unit is still in the Location after the round, free: neither eliminated, captured, nor gone by Infiltration.</summary>
        public bool Present => !Eliminated && !Captured && InfiltratedTo is null;

        private bool? guardAssigned;

        private string? guard;

        private bool revealed;

        public string? InfiltratedTo
        {
            get; private set;
        }

        public void Eliminate(string reason)
        {
            Eliminated = true;
            events.Add(reason);
        }

        public void Capture()
        {
            Captured = true;
            events.Add(CapturedHalf ? "half-captured" : "captured");
        }

        public void CaptureHalf() => CapturedHalf = true;

        public void UndoHalf() => CapturedHalf = false;

        public void GuardedBy(string? id)
        {
            guardAssigned = true;
            guard = id;
            if (id is null)
            {
                events.Add("freed-unarmed");
            }
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

        public void Escape()
        {
            Escaping = true;
            events.Add("escaped");
        }

        private bool escapeSucceeded;

        public void EscapeSucceeds(bool succeeded) => escapeSucceeded = succeeded;

        public void Rearm(string definitionId)
        {
            RearmedAs = definitionId;
            events.Add("rearmed");
        }

        public void Reveal()
        {
            if (Facts.Concealed == true && !revealed)
            {
                revealed = true;
                events.Add("concealment-lost");
            }
        }

        public void Infiltrate()
        {
            InfiltratedTo = Facts.InfiltrateTo;
            events.Add("infiltrated");
        }

        /// <summary>A11.2: a withdrawing unit not eliminated, Reduced, wounded (a SMC's Casualty Reduction), or captured withdraws.</summary>
        public string? WithdrewTo => Facts.WithdrawingTo is { } to && !Eliminated && !Captured && Definition == Printed && Wounded == (Facts.Wounded == true) ? to : null;

        public CloseCombatUnitEffect Effect() => new(Id, Printed.Id, Captured ? Printed.Id : Definition.Id, Eliminated, Wounded && !Eliminated,
            events.ToArray())
        {
            BerserkEnded = BerserkEnded && !Eliminated ? true : null,
            WithdrewTo = InfiltratedTo is null ? WithdrewTo : null,
            Captured = Captured && !Eliminated ? true : null,
            CapturedHalf = Captured && CapturedHalf ? true : null,
            GuardId = Captured && guardAssigned == true ? guard : null,
            ConcealmentLost = revealed && !Eliminated ? true : null,
            InfiltratedTo = !Eliminated ? InfiltratedTo : null,
            Escaped = Escaping && !Eliminated ? true : null,
            Armed = Escaping && escapeSucceeded && !Eliminated && !Printed.IsMmc && Facts.Unarmed == true ? true : null,
            RearmedAs = !Eliminated ? RearmedAs : null,
        };
    }
}
