namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// Resolves a declared Rally attempt under the reviewed Rally case matrix (unit step 19; Scenario A1 Rally Review
/// 2026-09-26). It is a pure function of the attempt and the pinned catalog: it rolls nothing and changes nothing. Facts
/// outside the reviewed scope abstain; facts the review leaves undecided, and missing rolls, are Indeterminate.
/// </summary>
public static class ScenarioA1RallyCalculator
{
    // A10.61: the Rally terrain bonus of the admitted terrain; woods and buildings give -1.
    public static readonly IReadOnlyDictionary<string, int> TerrainDrm = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["open-ground"] = 0,
        ["brush"] = 0,
        ["woods"] = -1,
        ["orchard"] = 0,
        ["grain"] = 0,
        ["wooden-building"] = -1,
        ["stone-building"] = -1,

        // Backlog pass 13 (ruling R13.2): marsh and rubble give no terrain bonus (A10.61).
        ["marsh"] = 0,
        ["wooden-rubble"] = 0,
        ["stone-rubble"] = 0,
    };

    /// <summary>
    /// A MMC's Self-Rally capability (A10.63; ruling R13.8): as its definition records it, and none for a German or Russian MMC whose definition leaves
    /// it unrecorded, since A10.6 grants MMC Self-Rally to Finns only.
    /// </summary>
    private static bool? SelfRally(FireDefinition definition) => definition.SelfRally ?? (definition.IsMmc && definition.Nationality is "german" or "russian" ? false : null);

    public static RallyResolution Resolve(RallyAttempt attempt, ScenarioA1RallyReference reference)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(reference);
        var missing = Missing(attempt);
        if (missing.Count != 0)
        {
            return new RallyResolution(RallyResolution.Indeterminate, missing, null, null);
        }

        var outside = Outside(attempt, reference);
        if (outside.Count != 0)
        {
            return new RallyResolution(RallyResolution.Abstained, outside, null, null);
        }

        var undecided = Undecided(attempt, reference);
        return undecided.Count != 0 ? new RallyResolution(RallyResolution.Indeterminate, undecided, null, null) : Run(attempt, reference);
    }

    /// <summary>
    /// The reasons an attempt cannot be committed before any roll: empty when every outcome the dice can reach is
    /// decided. Every reason other than a missing roll depends on the facts alone, so the check is exact: the facts
    /// must stop only at the missing Rally DR, and Fate must be able to Reduce the unit.
    /// </summary>
    public static IReadOnlyList<string> Precheck(RallyAttempt attempt, ScenarioA1RallyReference reference)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(reference);
        var first = Resolve(attempt with
        {
            Rolls = new RallyRolls(null, null)
        }, reference);
        if (!(first.Disposition == RallyResolution.Indeterminate && first.Reasons is ["asl.a1.rally.roll-missing:rally"]))
        {
            return first.Reasons;
        }

        // A15.44, A15.5: a leader's rally can reach Heat of Battle, so its Berserk and Surrender results need the planner's reads.
        return attempt.Leader is not null && ScenarioA1HeatOfBattle.Subject(reference.Definitions[attempt.Unit!.DefinitionId!], heroic: false)
            && (attempt.KnownEnemyInLos is null || attempt.Captors is null)
            ? ["asl.a1.rally.fact-missing:knownEnemyInLos-or-captors"]
            : [];
    }

    private static List<string> Missing(RallyAttempt attempt)
    {
        var missing = new List<string>();
        void Need(object? value, string name)
        {
            if (value is null || (value is string text && string.IsNullOrWhiteSpace(text)))
            {
                missing.Add("asl.a1.rally.fact-missing:" + name);
            }
        }

        Need(attempt.Phase, "phase");
        Need(attempt.RallyingSide, "rallyingSide");
        Need(attempt.LocationId, "locationId");
        Need(attempt.Terrain, "terrain");
        Need(attempt.GoodOrderLeaderInLocation, "goodOrderLeaderInLocation");
        Need(attempt.BrokenLeaderInLocation, "brokenLeaderInLocation");
        Need(attempt.FirstMmcRallyOfOwnPlayerTurn, "firstMmcRallyOfOwnPlayerTurn");
        Need(attempt.Rolls, "rolls");
        if (attempt.Unit is not { } unit)
        {
            missing.Add("asl.a1.rally.fact-missing:unit");
        }
        else
        {
            Need(unit.UnitId, "unit.unitId");
            Need(unit.DefinitionId, "unit.definitionId");
            Need(unit.LocationId, "unit.locationId");
            Need(unit.Broken, "unit.broken");
            Need(unit.Disrupted, "unit.disrupted");
            Need(unit.Wounded, "unit.wounded");
            Need(unit.DesperationMorale, "unit.desperationMorale");
            Need(unit.Concealed, "unit.concealed");
            Need(unit.AttemptedThisPlayerTurn, "unit.attemptedThisPlayerTurn");
            Need(unit.OtherActionThisPhase, "unit.otherActionThisPhase");
        }

        if (attempt.Leader is { } leader)
        {
            Need(leader.UnitId, "leader.unitId");
            Need(leader.DefinitionId, "leader.definitionId");
            Need(leader.LocationId, "leader.locationId");
            Need(leader.Broken, "leader.broken");
            Need(leader.Wounded, "leader.wounded");
            Need(leader.Concealed, "leader.concealed");
        }

        // A12.141: rallying costs a concealed unit or leader its "?" in the LOS of a Good Order enemy within 16 hexes.
        if ((attempt.Unit?.Concealed == true || attempt.Leader?.Concealed == true) && attempt.EnemyGoodOrderInLosWithin16 is null)
        {
            missing.Add("asl.a1.rally.fact-missing:enemyGoodOrderInLosWithin16");
        }

        return missing;
    }

    private static List<string> Outside(RallyAttempt attempt, ScenarioA1RallyReference reference)
    {
        var outside = new List<string>();
        var unit = attempt.Unit!;
        if (attempt.Phase != "RPh" || attempt.RallyingSide is not ("phasing" or "non-phasing"))
        {
            outside.Add("asl.a1.rally.phase-outside");
        }

        var definition = reference.Definitions.GetValueOrDefault(unit.DefinitionId!);
        if (definition is null || !(definition.IsMmc || definition.IsLeader) || unit.Broken != true || unit.LocationId != attempt.LocationId)
        {
            outside.Add("asl.a1.rally.unit-outside");
        }

        // A10.6: one attempt per unit per Player Turn; A3.1: one kind of RPh action per unit.
        if (unit.AttemptedThisPlayerTurn == true)
        {
            outside.Add("asl.a1.rally.already-attempted");
        }

        if (unit.OtherActionThisPhase == true)
        {
            outside.Add("asl.a1.rally.other-action");
        }

        if (!TerrainDrm.ContainsKey(attempt.Terrain!))
        {
            outside.Add("asl.a1.rally.terrain-outside");
        }

        var commissar = definition is not null && ScenarioA1FireReference.IsCommissar(definition.Id);
        if (attempt.Leader is { } leader)
        {
            // A10.6, A10.7: an unbroken friendly leader in the same Location, at the same level; Allied Troops of another nationality are admitted
            // (ruling R15.8), the planner reading the side.
            var leaderDefinition = reference.Definitions.GetValueOrDefault(leader.DefinitionId!);
            if (leaderDefinition is null || !leaderDefinition.IsLeader || leaderDefinition.Leadership is null
                || leader.Broken == true || leader.LocationId != attempt.LocationId || leader.UnitId == unit.UnitId)
            {
                outside.Add("asl.a1.rally.leader-outside");
            }

            // A25.221, A25.222 (ruling R15.6): a Commissar alone directs a Rally in his Location, and a broken Commissar always Self-Rallies.
            if ((attempt.Commissar is { } present && present != leader.UnitId) || commissar)
            {
                outside.Add("asl.a1.rally.commissar-rally");
            }
        }
        else if (definition is not null)
        {
            // A10.63: never with a Good Order friendly leader present, but a broken Commissar always attempts Self-Rally (A25.221; ruling R15.6);
            // A19.12: never when Disrupted.
            if (attempt.GoodOrderLeaderInLocation == true && !commissar)
            {
                outside.Add("asl.a1.rally.self-rally-with-leader-present");
            }

            if (unit.Disrupted == true)
            {
                outside.Add("asl.a1.rally.disrupted-self-rally");
            }

            // A10.71: a broken leader may Self-Rally. A18.11: the first MMC Rally attempt of the side's own RPh may be a
            // Self-Rally regardless of capability. A10.71 bars units without Self-Rally capability while their only
            // leader is broken; the review reads that bar as covering the A18.11 attempt too. An MMC whose capability
            // the catalog does not record is left undecided.
            if (definition.IsMmc && SelfRally(definition) != true
                && (FieldPromotionAttempt(attempt) ? attempt.BrokenLeaderInLocation == true : SelfRally(definition) == false))
            {
                outside.Add("asl.a1.rally.self-rally-not-capable");
            }
        }

        if (attempt.Rolls is { Rally: { } dice } && (dice.Count != 2 || dice.Any(die => die is < 1 or > 6))
            || attempt.Rolls?.WoundSeverity is < 1 or > 6
            || attempt.Rolls?.HeatOfBattle is { } heat && (heat.Count != 2 || heat.Any(die => die is < 1 or > 6))
            || attempt.Rolls?.LeaderCreation is < 1 or > 6
            || attempt.Rolls?.BerserkChecks?.Values.Any(dice => dice.Count != 2 || dice.Any(die => die is < 1 or > 6)) == true)
        {
            outside.Add("asl.a1.rally.roll-malformed");
        }

        return outside;
    }

    private static bool FieldPromotionAttempt(RallyAttempt attempt) =>
        attempt.Leader is null && attempt.RallyingSide == "phasing" && attempt.FirstMmcRallyOfOwnPlayerTurn == true && attempt.Unit?.Disrupted != true;

    private static List<string> Undecided(RallyAttempt attempt, ScenarioA1RallyReference reference)
    {
        var undecided = new List<string>();
        var unit = attempt.Unit!;
        var definition = reference.Definitions[unit.DefinitionId!];
        if (definition.BrokenMorale is null)
        {
            undecided.Add("asl.a1.rally.definition-incomplete:" + definition.Id);
        }

        // The catalog records no Self-Rally capability it cannot read from its sources.
        if (attempt.Leader is null && definition.IsMmc && SelfRally(definition) is null && !FieldPromotionAttempt(attempt))
        {
            undecided.Add("asl.a1.rally.self-rally-capability-unrecorded:" + definition.Id);
        }

        // A15.1: a Green or Conscript unit's Heat of Battle DRM depends on whether it is Inexperienced (A19.2).
        if (ScenarioA1HeatOfBattle.NeedsInexperience(definition) && unit.Inexperienced is null)
        {
            undecided.Add("asl.a1.rally.fact-missing:unit.inexperienced");
        }

        // Fate must be able to Reduce the unit (A10.64, A7.302).
        if (definition.Kind == "asl:squad"
            && (ScenarioA1FireReference.HalfSquadOf(definition.Id) is not { } half || !reference.Definitions.ContainsKey(half)))
        {
            undecided.Add("asl.a1.rally.reduction-counter-missing:" + definition.Id);
        }

        return undecided;
    }

    private static RallyResolution Run(RallyAttempt attempt, ScenarioA1RallyReference reference)
    {
        var unit = attempt.Unit!;
        var definition = reference.Definitions[unit.DefinitionId!];
        if (attempt.Rolls!.Rally is not { } dice)
        {
            return new RallyResolution(RallyResolution.Indeterminate, ["asl.a1.rally.roll-missing:rally"], null, null);
        }

        var selfRally = attempt.Leader is null;
        var fieldPromotion = selfRally && definition.IsMmc && FieldPromotionAttempt(attempt);
        var kind = !selfRally ? "leader-rally" : fieldPromotion && SelfRally(definition) != true ? "field-promotion-self-rally" : "self-rally";
        var drm = new List<FireModifier>();
        // A25.222 (ruling R15.6): a unit rallied by a Commissar is immune to DM for the attempt.
        var byCommissar = attempt.Leader is { } director && director.UnitId == attempt.Commissar;
        if (unit.DesperationMorale == true && !byCommissar)
        {
            drm.Add(new FireModifier("desperation-morale", 4m, "A10.62"));
        }

        if (attempt.Leader is { } leader)
        {
            // A10.7, A10.72: the rallying leader's modifier, one worse when he is wounded (A17.3), and one worse for Allied Troops of another
            // nationality (A10.7; ruling R15.8).
            var leaderDefinition = reference.Definitions[leader.DefinitionId!];
            var leadership = leaderDefinition.Leadership!.Value + (leader.Wounded == true ? 1 : 0) + (leaderDefinition.Nationality != definition.Nationality ? 1 : 0);
            drm.Add(new FireModifier("leadership:" + leader.UnitId, leadership, "A10.7"));
        }
        else
        {
            drm.Add(new FireModifier("self-rally", 1m, "A10.63"));
        }

        var terrain = TerrainDrm[attempt.Terrain!];
        if (terrain != 0)
        {
            drm.Add(new FireModifier("terrain:" + attempt.Terrain, terrain, "A10.61"));
        }

        var original = dice[0] + dice[1];
        var final = original + (int)drm.Sum(item => item.Value);
        // A17.3: one lower when wounded; A10.8: one higher when Fanatic; A25.221 (ruling R15.6): one higher with a Commissar in the Location, below 10.
        var morale = definition.BrokenMorale!.Value - (unit.Wounded == true ? 1 : 0) + (unit.Fanatic == true ? 1 : 0);
        if (attempt.Commissar is not null && !ScenarioA1FireReference.IsCommissar(definition.Id) && morale < 10)
        {
            morale++;
        }
        // A10.64; E3.742 (ruling R16.14): Extreme Winter makes an Original 11 Fate too for the units it names.
        var fate = original == 12 || (original == 11 && attempt.ExtremeWinterFate == true);

        // A18.11: an Original 2 on the first MMC Self-Rally rallies the unit and calls for a Leader Creation dr; A15.1: an
        // Original 2 on a Rally other than Self-Rally calls for a Heat of Battle DR.
        var rallied = !fate && (final <= morale || (fieldPromotion && original == 2));
        var heatOfBattle = original == 2 && !selfRally && ScenarioA1HeatOfBattle.Subject(definition, heroic: false);
        // A25.71 (backlog pass 15, ruling R15.13): the Finns create no leader.
        var leaderCreation = original == 2 && fieldPromotion && ScenarioA1FieldPromotion.Applicable(definition.Nationality);
        var usedChoices = new HashSet<string>(StringComparer.Ordinal);

        // A18.11, ruling R5.8: the rallying side may decline the Leader Creation dr.
        if (leaderCreation && attempt.Choices is { } answers)
        {
            var key = "leaderCreation:" + unit.UnitId;
            if (!answers.TryGetValue(key, out var answer))
            {
                return new RallyResolution(RallyResolution.Indeterminate, ["asl.a1.rally.choice-missing:" + key], null, null);
            }

            usedChoices.Add(key);
            leaderCreation = answer == "take";
        }

        var events = new List<string>();
        var finalDefinition = definition.Id;
        var eliminated = false;
        var wounded = unit.Wounded == true;
        if (fate)
        {
            // A10.64, A7.302: Casualty Reduction; a squad becomes its HS with the same broken status, a HS is eliminated,
            // and a leader is wounded (A17.11).
            if (definition.IsLeader)
            {
                if (attempt.Rolls.WoundSeverity is not { } severity)
                {
                    return new RallyResolution(RallyResolution.Indeterminate, ["asl.a1.rally.roll-missing:woundSeverity:" + unit.UnitId], null, null);
                }

                if (severity + (wounded ? 1 : 0) >= 5)
                {
                    eliminated = true;
                    events.Add("eliminated-mortal-wound");
                }
                else
                {
                    wounded = true;
                    events.Add("wounded");
                }
            }
            else if (definition.Kind == "asl:half-squad")
            {
                eliminated = true;
                events.Add("eliminated-fate");
            }
            else
            {
                finalDefinition = ScenarioA1FireReference.HalfSquadOf(definition.Id)!;
                events.Add("casualty-reduced-fate");
            }
        }
        else if (rallied)
        {
            events.Add(kind == "leader-rally" ? "rallied" : "self-rallied");
        }

        if (attempt.Rolls.WoundSeverity is not null && !(fate && definition.IsLeader))
        {
            return new RallyResolution(RallyResolution.Abstained, ["asl.a1.rally.extra-roll:woundSeverity"], null, null);
        }

        if (attempt.Rolls.HeatOfBattle is not null && !heatOfBattle)
        {
            return new RallyResolution(RallyResolution.Abstained, ["asl.a1.rally.extra-roll:heatOfBattle"], null, null);
        }

        if (attempt.Rolls.LeaderCreation is not null && !leaderCreation)
        {
            return new RallyResolution(RallyResolution.Abstained, ["asl.a1.rally.extra-roll:leaderCreation"], null, null);
        }

        // A15.1 to A15.5: the Heat of Battle DR, with the +1 for a broken unit though the 2 rallied it.
        HeatOfBattleOutcome? heat = null;
        var fanatic = unit.Fanatic == true;
        var berserk = false;
        var disrupted = false;
        if (heatOfBattle)
        {
            if (attempt.Rolls.HeatOfBattle is not { } heatDice)
            {
                return new RallyResolution(RallyResolution.Indeterminate, ["asl.a1.rally.roll-missing:heatOfBattle"], null, null);
            }

            var (outcome, reason) = ScenarioA1HeatOfBattle.Resolve(definition, true, unit.Inexperienced, fanatic, heatDice, reference.Definitions,
                attempt.KnownEnemyInLos, attempt.Captors, attempt.NoQuarter == true);
            if (outcome is null)
            {
                return new RallyResolution(RallyResolution.Indeterminate, [reason!], null, null);
            }

            // A15.3, ruling R5.8: the owner may refuse a Battle Hardening that would change the unit.
            if (attempt.Choices is { } hardeningAnswers && outcome.HardeningMatters(broken: !rallied, pinned: false, disrupted: unit.Disrupted == true && !rallied))
            {
                var hardeningKey = "battleHardening:" + unit.UnitId;
                if (!hardeningAnswers.TryGetValue(hardeningKey, out var hardeningAnswer))
                {
                    return new RallyResolution(RallyResolution.Indeterminate, ["asl.a1.rally.choice-missing:" + hardeningKey], null, null);
                }

                usedChoices.Add(hardeningKey);
                if (hardeningAnswer != "take")
                {
                    outcome = outcome.WithHardeningRefused();
                }
            }

            heat = outcome;
            events.Add("heat-of-battle:" + outcome.Result);

            // A15.21: a leader made heroic rallies; A15.3: a Battle Hardened unit is unbroken (ruling R28.7), though the Rally
            // DR failed.
            if (!rallied && (outcome.Heroic == true || outcome.Hardening))
            {
                rallied = true;
                events.Add("rallied-heat-of-battle");
            }
            if (outcome.HeroDefinitionId is { } hero)
            {
                events.Add("hero-created:" + hero);
            }

            if (outcome.HardenedDefinitionId is { } hardened)
            {
                finalDefinition = hardened;
                events.Add("battle-hardened");
            }

            if (outcome.HardeningRefused == true)
            {
                events.Add("battle-hardening-refused");
            }

            if (outcome.Fanatic == true)
            {
                fanatic = true;
                events.Add("became-fanatic");
            }

            // A15.4: a berserk unit is rallied; A15.5: a surrendering one is broken again, though the Rally DR rallied it, and
            // Disrupted.
            if (outcome.Result == HeatOfBattleOutcome.Berserk)
            {
                berserk = true;
                events.Add("went-berserk");
                if (!rallied)
                {
                    rallied = true;
                    events.Add("rallied-heat-of-battle");
                }
            }
            else if (outcome.Result == HeatOfBattleOutcome.Surrender)
            {
                rallied = false;
                disrupted = true;
                events.Add("disrupted");
                if (outcome.Captors is { Count: > 0 })
                {
                    events.Add("surrendered");
                }
            }
        }

        // A15.41: a leader who went berserk tries to take every other friendly unit in his Location subject to Heat of Battle
        // with him: a NTC with his leadership DRM, and a pass makes it berserk, rallying it if broken.
        List<RallyBerserkCheck>? berserkChecks = null;
        List<string>? berserkCompanions = null;
        if (berserk && definition.IsLeader)
        {
            var leadership = definition.Leadership!.Value + (unit.Wounded == true ? 1 : 0);
            var companions = (attempt.Companions ?? []).Where(item => reference.Definitions.GetValueOrDefault(item.DefinitionId ?? string.Empty) is { } other
                && ScenarioA1HeatOfBattle.Subject(other, item.Heroic == true, item.Berserk == true)).ToArray();
            foreach (var companion in companions.OrderBy(item => reference.Definitions[item.DefinitionId!].IsLeader ? 0 : 1)
                .ThenByDescending(item => reference.Definitions[item.DefinitionId!].Morale))
            {
                if (attempt.Rolls.BerserkChecks?.TryGetValue(companion.UnitId!, out var check) != true)
                {
                    return new RallyResolution(RallyResolution.Indeterminate, ["asl.a1.rally.roll-missing:berserkCheck:" + companion.UnitId], null, null);
                }

                var other = reference.Definitions[companion.DefinitionId!];
                var level = (companion.Broken == true ? other.BrokenMorale : other.Morale)!.Value - (companion.Wounded == true ? 1 : 0) + (companion.Fanatic == true ? 1 : 0);
                List<FireModifier> checkDrm = [new FireModifier("berserk-leader:" + unit.UnitId, leadership, "A15.41")];
                var checkOriginal = check![0] + check[1];
                var checkFinal = checkOriginal + leadership;
                (berserkChecks ??= []).Add(new RallyBerserkCheck(companion.UnitId!, check.ToArray(), checkOriginal, checkDrm, checkFinal, level, checkFinal <= level));
                if (checkFinal <= level)
                {
                    (berserkCompanions ??= []).Add(companion.UnitId!);
                }
            }
        }

        if (attempt.Rolls.BerserkChecks?.Keys.Any(id => berserkChecks?.Any(item => item.UnitId == id) != true) == true)
        {
            return new RallyResolution(RallyResolution.Abstained, ["asl.a1.rally.extra-roll:berserkCheck"], null, null);
        }


        // A25.222 (ruling R15.6): a unit that fails to rally under a Commissar is Replaced by its next lower quality; a squad already the lowest is
        // Casualty Reduced; a HS, crew, or SMC that cannot be Replaced is eliminated.
        var replacedByCommissar = false;
        if (!fate && !rallied && !disrupted && byCommissar)
        {
            replacedByCommissar = true;
            if (ScenarioA1FireReference.ReplacementOf(definition.Id) is { } lesser && reference.Definitions.ContainsKey(lesser))
            {
                finalDefinition = lesser;
                events.Add("replaced-commissar");
            }
            else if (definition.Kind == "asl:squad" && ScenarioA1FireReference.HalfSquadOf(definition.Id) is { } reduced)
            {
                finalDefinition = reduced;
                events.Add("casualty-reduced-commissar");
            }
            else
            {
                eliminated = true;
                events.Add("eliminated-commissar");
            }
        }

        // A18.11, A18.2: the Leader Creation dr and its drm; the broken unit's Morale Level is its broken one.
        LeaderCreationOutcome? creation = null;
        if (leaderCreation)
        {
            if (attempt.Rolls.LeaderCreation is not { } dr)
            {
                return new RallyResolution(RallyResolution.Indeterminate, ["asl.a1.rally.roll-missing:leaderCreation"], null, null);
            }

            var created = ScenarioA1FieldPromotion.Create(definition, morale, dr, [new FireModifier("broken", 1m, "A18.11")], reference.Definitions, "asl.a1.rally",
                unit.Fanatic == true);
            if (created.Undecided is { } reason)
            {
                return new RallyResolution(RallyResolution.Indeterminate, [reason], null, null);
            }

            creation = created.Outcome;
            if (creation!.LeaderDefinitionId is { } leaderId)
            {
                events.Add("leader-created:" + leaderId);
            }
        }
        else if (original == 2 && fieldPromotion)
        {
            events.Add("leader-creation-declined");
        }

        if (attempt.Choices?.Keys.FirstOrDefault(key => !usedChoices.Contains(key)) is { } unasked)
        {
            return new RallyResolution(RallyResolution.Abstained, ["asl.a1.rally.extra-choice:" + unasked], null, null);
        }

        // A12.141: the attempt costs "?" to the concealed unit and the concealed rallying leader in the LOS of a Good Order
        // enemy within 16 hexes; A12.14: a Reduced or wounded unit loses "?" regardless.
        var concealmentLost = new List<string>();
        if (unit.Concealed == true && (attempt.EnemyGoodOrderInLosWithin16 == true || (fate && !eliminated)))
        {
            concealmentLost.Add(unit.UnitId!);
        }

        if (attempt.Leader is { Concealed: true } concealedLeader && attempt.EnemyGoodOrderInLosWithin16 == true)
        {
            concealmentLost.Add(concealedLeader.UnitId!);
        }

        var arithmetic = new RallyArithmetic(kind, dice.ToArray(), original, drm, final, morale, rallied, fate)
        {
            HeatOfBattle = heat,
            LeaderCreation = creation,
            BerserkChecks = berserkChecks,
        };
        return new RallyResolution(RallyResolution.Resolved, [], arithmetic,
            new RallyEffect(unit.UnitId!, definition.Id, finalDefinition, rallied, eliminated, wounded && !eliminated, concealmentLost, events)
            {
                Fanatic = fanatic && !eliminated ? true : null,
                Heroic = heat?.Heroic,
                HeroDefinitionId = heat?.HeroDefinitionId,
                CreatedLeaderDefinitionId = creation?.LeaderDefinitionId,
                Berserk = berserk ? true : null,
                Disrupted = disrupted ? true : null,
                BerserkCompanions = berserkCompanions,
                ReplacedByCommissar = replacedByCommissar && !eliminated ? true : null,
            });
    }
}
