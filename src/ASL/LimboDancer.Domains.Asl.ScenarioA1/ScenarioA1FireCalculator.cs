using System.Globalization;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// Resolves a declared fire attack under the reviewed Fire case matrix (unit step 17; Scenario A1 Fire Review
/// 2026-09-26). It is a pure function of the attack and the pinned reference data: it rolls nothing and changes nothing.
/// Facts outside the reviewed scope abstain; facts the review leaves undecided, and missing rolls, are Indeterminate.
/// </summary>
public static class ScenarioA1FireCalculator
{
    private static readonly string[] AdmittedPhases = ["PFPh", "DFPh"];

    public static FireResolution Resolve(FireAttack attack, ScenarioA1FireReference reference)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(reference);
        var missing = Missing(attack);
        if (missing.Count != 0)
        {
            return Refused(FireResolution.Indeterminate, missing);
        }

        var outside = Outside(attack, reference);
        if (outside.Count != 0)
        {
            return Refused(FireResolution.Abstained, outside);
        }

        var undecided = Undecided(attack, reference);
        if (undecided.Count != 0)
        {
            return Refused(FireResolution.Indeterminate, undecided);
        }

        return new Resolution(attack, reference).Run();
    }

    private static FireResolution Refused(string disposition, IReadOnlyList<string> reasons) =>
        new(disposition, reasons, null, [], [], null, []);

    private static List<string> Missing(FireAttack attack)
    {
        var missing = new List<string>();
        void Need(object? value, string name)
        {
            if (value is null || (value is string text && string.IsNullOrWhiteSpace(text)))
            {
                missing.Add("asl.a1.fire.fact-missing:" + name);
            }
        }

        Need(attack.Phase, "phase");
        Need(attack.FiringSide, "firingSide");
        Need(attack.FireGroupComplete, "fireGroupComplete");
        Need(attack.FirerLocationId, "firerLocationId");
        Need(attack.TargetLocationId, "targetLocationId");
        Need(attack.Range, "range");
        Need(attack.SameLevel, "sameLevel");
        Need(attack.TargetTerrain, "targetTerrain");
        Need(attack.Los, "los");
        Need(attack.Los?.Blocked, "los.blocked");
        Need(attack.Los?.HindranceDrm, "los.hindranceDrm");
        Need(attack.Los?.HindranceAttributed, "los.hindranceAttributed");
        Need(attack.Los?.GrainInLos, "los.grainInLos");
        Need(attack.Rolls, "rolls");
        if (attack.Firers is null || attack.Firers.Count == 0)
        {
            missing.Add("asl.a1.fire.fact-missing:firers");
        }
        else
        {
            foreach (var (firer, index) in attack.Firers.Select((item, index) => (item, index)))
            {
                var at = $"firers[{index}].";
                Need(firer.UnitId, at + "unitId");
                Need(firer.DefinitionId, at + "definitionId");
                Need(firer.LocationId, at + "locationId");
                Need(firer.Broken, at + "broken");
                Need(firer.Pinned, at + "pinned");
                Need(firer.Concealed, at + "concealed");
                Need(firer.FiredThisPlayerTurn, at + "firedThisPlayerTurn");
                Need(firer.UsesSupportWeapon, at + "usesSupportWeapon");
            }
        }

        if (attack.Director is { } director)
        {
            Need(director.UnitId, "director.unitId");
            Need(director.DefinitionId, "director.definitionId");
            Need(director.LocationId, "director.locationId");
            Need(director.Broken, "director.broken");
            Need(director.Pinned, "director.pinned");
            Need(director.Concealed, "director.concealed");
            Need(director.DirectedThisPlayerTurn, "director.directedThisPlayerTurn");
        }

        if (attack.Targets is null || attack.Targets.Count == 0)
        {
            missing.Add("asl.a1.fire.fact-missing:targets");
        }
        else
        {
            foreach (var (target, index) in attack.Targets.Select((item, index) => (item, index)))
            {
                var at = $"targets[{index}].";
                Need(target.UnitId, at + "unitId");
                Need(target.DefinitionId, at + "definitionId");
                Need(target.LocationId, at + "locationId");
                Need(target.Broken, at + "broken");
                Need(target.Pinned, at + "pinned");
                Need(target.Concealed, at + "concealed");
                Need(target.Hidden, at + "hidden");
                Need(target.Dummy, at + "dummy");
            }
        }

        return missing;
    }

    private static List<string> Outside(FireAttack attack, ScenarioA1FireReference reference)
    {
        var outside = new List<string>();
        var phase = attack.Phase!;
        if (!AdmittedPhases.Contains(phase)
            || (phase == "PFPh" && attack.FiringSide != "phasing")
            || (phase == "DFPh" && attack.FiringSide != "non-phasing"))
        {
            outside.Add("asl.a1.fire.phase-outside");
        }

        var firers = attack.Firers!;
        var targets = attack.Targets!;
        var ids = firers.Select(item => item.UnitId!).Concat(targets.Select(item => item.UnitId!))
            .Concat(attack.Director is null ? [] : [attack.Director.UnitId!]).ToArray();
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            outside.Add("asl.a1.fire.unit-listed-twice");
        }

        if (firers.Any(item => item.FiredThisPlayerTurn == true))
        {
            outside.Add("asl.a1.fire.firer-already-fired");
        }

        var firerDefinitions = firers.Select(item => reference.Definitions.GetValueOrDefault(item.DefinitionId!)).ToArray();
        if (attack.FireGroupComplete != true
            || firers.Any(item => item.LocationId != attack.FirerLocationId || item.Broken == true || item.UsesSupportWeapon == true)
            || firerDefinitions.Any(item => item is null || !item.IsMmc || item.Firepower is null || item.Range is null))
        {
            outside.Add("asl.a1.fire.firer-outside");
        }

        var side = firerDefinitions.FirstOrDefault()?.Nationality;
        if (firerDefinitions.Any(item => item is not null && item.Nationality != side))
        {
            outside.Add("asl.a1.fire.firers-of-two-sides");
        }

        if (attack.Director is { } director)
        {
            var definition = reference.Definitions.GetValueOrDefault(director.DefinitionId!);
            if (definition is null || !definition.IsLeader || definition.Leadership is null || definition.Nationality != side
                || director.LocationId != attack.FirerLocationId || director.Broken == true || director.Pinned == true
                || director.DirectedThisPlayerTurn == true)
            {
                outside.Add("asl.a1.fire.director-outside");
            }
        }

        var targetDefinitions = targets.Select(item => reference.Definitions.GetValueOrDefault(item.DefinitionId!)).ToArray();
        if (attack.TargetLocationId == attack.FirerLocationId
            || targets.Any(item => item.LocationId != attack.TargetLocationId)
            || targetDefinitions.Any(item => item is null || item.Nationality == side)
            || !ScenarioA1FireReference.Tem.ContainsKey(attack.TargetTerrain!))
        {
            outside.Add("asl.a1.fire.target-outside");
        }

        if (attack.Range < 1 || firerDefinitions.Any(item => item?.Range is { } range && attack.Range > 2 * range))
        {
            outside.Add("asl.a1.fire.out-of-range");
        }

        if (attack.Los!.Blocked == true)
        {
            outside.Add("asl.a1.fire.los-blocked");
        }

        if (attack.Rolls is { } rolls && (!Dice(rolls.Attack, allowEmpty: true)
            || rolls.RandomSelection?.Values.Any(dr => dr is < 1 or > 6) == true
            || rolls.Checks?.Values.Any(dice => !Dice(dice, allowEmpty: false)) == true
            || rolls.LeaderLoss?.Values.Any(dice => !Dice(dice, allowEmpty: false)) == true))
        {
            outside.Add("asl.a1.fire.roll-malformed");
        }

        return outside;
    }

    private static bool Dice(IReadOnlyList<int>? dice, bool allowEmpty) =>
        dice is null ? allowEmpty : dice.Count == 2 && dice.All(die => die is >= 1 and <= 6);

    private static List<string> Undecided(FireAttack attack, ScenarioA1FireReference reference)
    {
        var undecided = new List<string>();
        if (attack.SameLevel != true)
        {
            undecided.Add("asl.a1.fire.levels-differ");
        }

        var los = attack.Los!;
        if (los.HindranceAttributed != true || los.HindranceDrm < 0
            || (los.GrainInLos == true && attack.ScenarioMonth is not (>= 6 and <= 9)))
        {
            undecided.Add("asl.a1.fire.hindrance-unattributed");
        }

        if (attack.Targets!.Any(item => item.Hidden == true || item.Dummy == true))
        {
            undecided.Add("asl.a1.fire.concealment-unreviewed");
        }

        if (attack.Targets!.Count(item => reference.Definitions[item.DefinitionId!].IsLeader) > 1)
        {
            undecided.Add("asl.a1.fire.leaders-interact");
        }

        return undecided;
    }

    /// <summary>One resolution, with the target units' changing state.</summary>
    private sealed class Resolution(FireAttack attack, ScenarioA1FireReference reference)
    {
        private readonly List<string> undecided = [];
        private readonly HashSet<string> usedRolls = new(StringComparer.Ordinal);
        private readonly Dictionary<string, TargetState> state = new(StringComparer.Ordinal);

        public FireResolution Run()
        {
            foreach (var target in attack.Targets!)
            {
                state[target.UnitId!] = new TargetState(target, reference.Definitions[target.DefinitionId!]);
            }

            var arithmetic = Arithmetic();
            if (arithmetic is null)
            {
                return Refused(FireResolution.Indeterminate, undecided);
            }

            usedRolls.Add("attack");
            Apply(arithmetic.Result);
            LeaderLoss();
            if (undecided.Count != 0)
            {
                return Refused(FireResolution.Indeterminate, undecided.Distinct().ToArray());
            }

            var extra = ExtraRolls();
            if (extra.Count != 0)
            {
                return Refused(FireResolution.Abstained, extra);
            }

            if (arithmetic.Result != "none")
            {
                // A12.14: a concealed target loses "?" on a PTC or worse result.
                foreach (var unit in state.Values.Where(unit => unit.Target.Concealed == true))
                {
                    unit.ConcealmentLost = true;
                }
            }

            var firerConcealment = FirerConcealment();
            if (undecided.Count != 0)
            {
                return Refused(FireResolution.Indeterminate, undecided);
            }

            var marked = attack.Firers!.Select(item => item.UnitId!)
                .Concat(attack.Director is null ? [] : [attack.Director.UnitId!]).ToArray();
            return new FireResolution(FireResolution.Resolved, [], arithmetic,
                attack.Targets!.Select(item => state[item.UnitId!].Effect()).ToArray(), marked,
                attack.Phase == "PFPh" ? "prep-fire" : "final-fire", firerConcealment);
        }

        private FireArithmetic? Arithmetic()
        {
            var concealedTarget = attack.Targets!.Any(item => item.Concealed == true);
            var firers = attack.Firers!.Select(firer =>
            {
                var definition = reference.Definitions[firer.DefinitionId!];
                var multipliers = new List<FireModifier>();
                if (attack.Range == 1)
                {
                    multipliers.Add(new FireModifier("point-blank-fire", 2m, "A7.21"));
                }

                if (attack.Range > definition.Range)
                {
                    multipliers.Add(new FireModifier("long-range-fire", 0.5m, "A7.22"));
                }

                if (concealedTarget)
                {
                    multipliers.Add(new FireModifier("area-fire-concealed-target", 0.5m, "A7.23"));
                }

                if (firer.Pinned == true)
                {
                    multipliers.Add(new FireModifier("pinned-firer", 0.5m, "A7.8"));
                }

                var fp = multipliers.Aggregate((decimal)definition.Firepower!.Value, (value, item) => value * item.Value);
                return new FirerFirepower(firer.UnitId!, definition.Firepower.Value, multipliers, fp);
            }).ToArray();
            var total = firers.Sum(item => item.Firepower);
            var column = Array.FindLastIndex(ScenarioA1FireReference.ColumnFp, fp => fp <= total);

            var dice = attack.Rolls!.Attack;
            if (dice is null)
            {
                undecided.Add("asl.a1.fire.roll-missing:attack");
                return null;
            }

            var original = dice[0] + dice[1];
            var cowered = dice[0] == dice[1] && attack.Director is null;
            var inexperienced = attack.Firers!.Any(item => reference.Definitions[item.DefinitionId!].Class is "green" or "conscript");
            var shift = cowered ? (inexperienced ? 2 : 1) : 0;
            var shifted = column < 0 ? -1 : column - shift;

            var drm = new List<FireModifier>();
            var tem = ScenarioA1FireReference.Tem[attack.TargetTerrain!];
            if (tem != 0)
            {
                drm.Add(new FireModifier("tem:" + attack.TargetTerrain, tem, "A7.6"));
            }

            if (attack.Los!.HindranceDrm > 0)
            {
                drm.Add(new FireModifier("los-hindrance", attack.Los.HindranceDrm!.Value, "A6.7"));
            }

            if (attack.Director is { } director)
            {
                drm.Add(new FireModifier("leadership:" + director.UnitId, reference.Definitions[director.DefinitionId!].Leadership!.Value, "A7.531"));
            }

            var final = original + (int)drm.Sum(item => item.Value);
            var result = shifted < 0 ? "none" : reference.Result(final, shifted);
            return new FireArithmetic(firers, total, column < 0 ? null : ScenarioA1FireReference.ColumnFp[column], shift, cowered,
                shifted < 0 ? null : ScenarioA1FireReference.ColumnFp[shifted], dice.ToArray(), original, drm, final, result);
        }

        private void Apply(string result)
        {
            var kia = Regex.Match(result, "^([1-7])KIA$");
            var k = Regex.Match(result, "^K/([1-4])$");
            var mc = Regex.Match(result, "^([1-4])MC$");
            if (kia.Success)
            {
                var count = int.Parse(kia.Groups[1].Value, CultureInfo.InvariantCulture);
                var drs = RandomSelection();
                if (drs is null)
                {
                    return;
                }

                // A7.301: the # highest drs are eliminated, ties at the cut included; the rest break.
                var ordered = drs.OrderByDescending(item => item.Value).ToArray();
                var cut = count >= ordered.Length ? int.MinValue : ordered[count - 1].Value;
                foreach (var (id, dr) in ordered)
                {
                    var unit = state[id];
                    if (dr >= cut)
                    {
                        unit.Eliminate("eliminated-kia");
                    }
                    else if (!unit.Broken)
                    {
                        unit.Break("broken-kia");
                    }
                    else
                    {
                        Reduce(unit, "casualty-reduced-kia");
                    }
                }
            }
            else if (k.Success)
            {
                var drs = RandomSelection();
                if (drs is null)
                {
                    return;
                }

                // A7.302: the highest dr, ties included, is Casualty Reduced; every surviving unit then takes the #MC.
                var highest = drs.Values.Max();
                foreach (var (id, dr) in drs.Where(item => item.Value == highest))
                {
                    Reduce(state[id], "casualty-reduced-k");
                }

                MoraleChecks(int.Parse(k.Groups[1].Value, CultureInfo.InvariantCulture));
            }
            else if (mc.Success)
            {
                MoraleChecks(int.Parse(mc.Groups[1].Value, CultureInfo.InvariantCulture));
            }
            else if (result == "NMC")
            {
                MoraleChecks(0);
            }
            else if (result == "PTC")
            {
                PinTaskChecks();
            }
        }

        private Dictionary<string, int>? RandomSelection()
        {
            var drs = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var id in state.Keys)
            {
                if (attack.Rolls!.RandomSelection?.TryGetValue(id, out var dr) != true)
                {
                    undecided.Add("asl.a1.fire.roll-missing:randomSelection:" + id);
                    return null;
                }

                drs[id] = dr;
                state[id].RandomSelectionDr = dr;
                usedRolls.Add("randomSelection:" + id);
            }

            return drs;
        }

        // Leaders check first, higher Morale Level first (A10.2), then the other units in the declared order.
        private TargetState[] CheckOrder() =>
            state.Values.Where(unit => unit.Definition.IsLeader).OrderByDescending(unit => unit.Definition.Morale)
                .Concat(state.Values.Where(unit => !unit.Definition.IsLeader)).ToArray();

        private void MoraleChecks(int modifier)
        {
            foreach (var unit in CheckOrder().Where(unit => !unit.Eliminated))
            {
                var check = Check(unit, "MC", "checks", modifier, useLeadership: true);
                if (check is null)
                {
                    return;
                }

                MoraleOutcome(unit, check.Value.Dice, check.Value.Drm);
                if (undecided.Count != 0)
                {
                    return;
                }
            }
        }

        private void MoraleOutcome(TargetState unit, IReadOnlyList<int> dice, List<FireModifier> drm, string kind = "MC")
        {
            var morale = unit.MoraleLevel;
            if (morale is null)
            {
                undecided.Add("asl.a1.fire.leaders-interact:broken-morale-unrecorded:" + unit.Id);
                return;
            }

            var original = dice[0] + dice[1];
            var final = original + (int)drm.Sum(item => item.Value);
            var passed = final <= morale && original != 12;
            string consequence;
            if (original == 12 && !unit.Broken)
            {
                // A10.31: a Casualty MC, after any ELR Replacement.
                if (!WithinElr(unit, final - morale.Value))
                {
                    return;
                }

                if (!Reduce(unit, "casualty-mc"))
                {
                    return;
                }

                if (!unit.Eliminated)
                {
                    unit.Break(null);
                }

                consequence = unit.Eliminated ? "eliminated" : "casualty-reduced-and-broken";
            }
            else if (original == 12)
            {
                unit.Eliminate("eliminated-casualty-mc");
                consequence = "eliminated";
            }
            else if (!passed && !unit.Broken)
            {
                if (!WithinElr(unit, final - morale.Value))
                {
                    return;
                }

                unit.Break("broken-" + kind.ToLowerInvariant());
                consequence = "broken";
            }
            else if (!passed)
            {
                if (!Reduce(unit, "casualty-reduced-" + kind.ToLowerInvariant()))
                {
                    return;
                }

                consequence = unit.Eliminated ? "eliminated" : "casualty-reduced";
            }
            else if (!unit.Broken && final == morale)
            {
                // A7.8: passing with the highest passing DR pins an unbroken unit.
                unit.Pin("pinned-highest-passing-dr");
                consequence = "pinned";
            }
            else
            {
                consequence = "passed";
            }

            unit.Checks.Add(new FireCheck(kind, dice.ToArray(), original, drm, final, morale.Value, passed, consequence));
        }

        private bool WithinElr(TargetState unit, int margin)
        {
            // A19.13: an unbroken unit failing by more than its ELR is Replaced; Replacement is not reviewed.
            if (attack.TargetSideElr is not { } elr)
            {
                undecided.Add("asl.a1.fire.elr-undecided:elr-undeclared");
                return false;
            }

            if (margin > elr)
            {
                undecided.Add("asl.a1.fire.elr-undecided:failure-beyond-elr:" + unit.Id);
                return false;
            }

            return true;
        }

        private void PinTaskChecks()
        {
            foreach (var unit in CheckOrder().Where(unit => !unit.Eliminated && !unit.Broken && !unit.Pinned))
            {
                var check = Check(unit, "NTC", "checks", 0, useLeadership: true);
                if (check is null)
                {
                    return;
                }

                var (dice, drm) = check.Value;
                var morale = unit.MoraleLevel!.Value;
                var original = dice[0] + dice[1];
                var final = original + (int)drm.Sum(item => item.Value);
                var passed = final <= morale;
                if (!passed)
                {
                    unit.Pin("pinned-ptc");
                }

                unit.Checks.Add(new FireCheck("NTC", dice.ToArray(), original, drm, final, morale, passed, passed ? "passed" : "pinned"));
            }
        }

        private (IReadOnlyList<int> Dice, List<FireModifier> Drm)? Check(TargetState unit, string kind, string rolls, int modifier, bool useLeadership)
        {
            var source = rolls == "checks" ? attack.Rolls!.Checks : attack.Rolls!.LeaderLoss;
            if (source?.TryGetValue(unit.Id, out var dice) != true)
            {
                undecided.Add($"asl.a1.fire.roll-missing:{rolls}:{unit.Id}");
                return null;
            }

            usedRolls.Add(rolls + ":" + unit.Id);
            var drm = new List<FireModifier>();
            if (modifier != 0)
            {
                drm.Add(new FireModifier("ift-" + kind.ToLowerInvariant(), modifier, "A7.304"));
            }

            if (useLeadership)
            {
                // A10.21, A10.22: one unbroken, unpinned leader of the Location other than the checker, and of higher
                // morale when the checker is a leader.
                var leader = state.Values.FirstOrDefault(other => other.Definition.IsLeader && other != unit && !other.Eliminated
                    && !other.Broken && !other.Pinned
                    && (!unit.Definition.IsLeader || other.Definition.Morale > unit.Definition.Morale));
                if (leader?.Definition.Leadership is { } leadership and not 0)
                {
                    drm.Add(new FireModifier("leadership:" + leader.Id, leadership, "A10.21"));
                }
            }

            return (dice!, drm);
        }

        private void LeaderLoss()
        {
            if (undecided.Count != 0)
            {
                return;
            }

            foreach (var leader in state.Values.Where(unit => unit.Definition.IsLeader && (unit.Eliminated || unit.BrokeInThisAttack)).ToArray())
            {
                var leaderMorale = leader.Eliminated ? leader.MoraleAtLoss : leader.Definition.Morale;
                if (leaderMorale is null)
                {
                    undecided.Add("asl.a1.fire.leaders-interact:broken-morale-unrecorded:" + leader.Id);
                    return;
                }

                // A10.2: an eliminated leader causes LLMC; an unbroken leader that broke causes LLTC.
                var eliminated = leader.Eliminated;
                foreach (var unit in state.Values.Where(unit => unit != leader && !unit.Eliminated
                    && (eliminated || !unit.Broken) && unit.MoraleLevel < leaderMorale).ToArray())
                {
                    var check = Check(unit, eliminated ? "LLMC" : "LLTC", "leaderLoss", 0, useLeadership: false);
                    if (check is null)
                    {
                        return;
                    }

                    var (dice, drm) = check.Value;
                    if (leader.Definition.Leadership is int negative and < 0)
                    {
                        drm.Add(new FireModifier("reversed-leadership:" + leader.Id, -negative, "A10.2"));
                    }

                    if (eliminated)
                    {
                        MoraleOutcome(unit, dice, drm, "LLMC");
                        if (undecided.Count != 0)
                        {
                            return;
                        }
                    }
                    else
                    {
                        var morale = unit.MoraleLevel!.Value;
                        var original = dice[0] + dice[1];
                        var final = original + (int)drm.Sum(item => item.Value);
                        var passed = final <= morale;
                        if (!passed)
                        {
                            unit.Pin("pinned-lltc");
                        }

                        unit.Checks.Add(new FireCheck("LLTC", dice.ToArray(), original, drm, final, morale, passed, passed ? "passed" : "pinned"));
                    }
                }
            }
        }

        private bool Reduce(TargetState unit, string reason)
        {
            // A7.302: a HS is eliminated, a squad becomes its HS with the same broken status, a leader is wounded.
            if (unit.Definition.IsLeader)
            {
                undecided.Add("asl.a1.fire.leader-wounded:" + unit.Id);
                return false;
            }

            if (unit.Definition.Kind == "asl:half-squad")
            {
                unit.Eliminate(reason);
                return true;
            }

            var half = ScenarioA1FireReference.HalfSquadOf(unit.Definition.Id);
            if (half is null)
            {
                undecided.Add("asl.a1.fire.reduction-counter-missing:" + unit.Id);
                return false;
            }

            unit.ReduceTo(reference.Definitions[half], reason);
            return true;
        }

        private List<string> ExtraRolls()
        {
            var rolls = attack.Rolls!;
            var supplied = (rolls.RandomSelection?.Keys.Select(id => "randomSelection:" + id) ?? [])
                .Concat(rolls.Checks?.Keys.Select(id => "checks:" + id) ?? [])
                .Concat(rolls.LeaderLoss?.Keys.Select(id => "leaderLoss:" + id) ?? []);
            return supplied.Where(key => !usedRolls.Contains(key)).Select(key => "asl.a1.fire.extra-roll:" + key).ToList();
        }

        private List<string> FirerConcealment()
        {
            var concealed = attack.Firers!.Where(item => item.Concealed == true).Select(item => item.UnitId!)
                .Concat(attack.Director is { Concealed: true } director ? [director.UnitId!] : []).ToArray();
            if (concealed.Length == 0)
            {
                return [];
            }

            // A12.14: a concealed unit that fires or directs fire loses "?" in the LOS of a Good Order enemy ground
            // unit within 16 hexes. The package sees only the target Location, so it decides only when one of its
            // units was Good Order when the attack was made.
            if (attack.Range <= 16 && attack.Targets!.Any(item => item.Broken == false))
            {
                return concealed.ToList();
            }

            undecided.Add("asl.a1.fire.concealment-unreviewed:firer-concealment");
            return [];
        }
    }

    private sealed class TargetState(FireTarget target, FireDefinition definition)
    {
        private readonly List<string> events = [];

        public FireTarget Target { get; } = target;

        public string Id { get; } = target.UnitId!;

        public FireDefinition Definition { get; private set; } = definition;

        public bool Broken { get; private set; } = target.Broken == true;

        public bool Pinned { get; private set; } = target.Pinned == true;

        public bool Eliminated { get; private set; }

        public bool BrokeInThisAttack { get; private set; }

        public int? MoraleAtLoss { get; private set; }

        public bool ConcealmentLost { get; set; }

        public int? RandomSelectionDr { get; set; }

        public List<FireCheck> Checks { get; } = [];

        public int? MoraleLevel => Broken ? Definition.BrokenMorale : Definition.Morale;

        public void Eliminate(string reason)
        {
            MoraleAtLoss = MoraleLevel;
            Eliminated = true;
            ConcealmentLost = Target.Concealed == true;
            events.Add(reason);
        }

        public void Break(string? reason)
        {
            if (!Broken)
            {
                BrokeInThisAttack = true;
            }

            Broken = true;
            Pinned = false;
            ConcealmentLost = Target.Concealed == true;
            if (reason is not null)
            {
                events.Add(reason);
            }
        }

        public void Pin(string reason)
        {
            Pinned = true;
            events.Add(reason);
        }

        public void ReduceTo(FireDefinition half, string reason)
        {
            Definition = half;
            ConcealmentLost = Target.Concealed == true;
            events.Add(reason);
        }

        public FireUnitEffect Effect() => new(Id, Target.DefinitionId!, Definition.Id, RandomSelectionDr, Eliminated, Broken && !Eliminated,
            Pinned && !Broken && !Eliminated, ConcealmentLost, events.ToArray(), Checks.ToArray());
    }
}
