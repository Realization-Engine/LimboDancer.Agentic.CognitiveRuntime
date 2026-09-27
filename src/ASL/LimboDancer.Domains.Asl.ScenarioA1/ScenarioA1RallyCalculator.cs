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
    };

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
        var first = Resolve(attempt with { Rolls = new RallyRolls(null, null) }, reference);
        return first.Disposition == RallyResolution.Indeterminate && first.Reasons is ["asl.a1.rally.roll-missing:rally"] ? [] : first.Reasons;
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

        if (attempt.Leader is { } leader)
        {
            // A10.6, A10.7: an unbroken friendly leader in the same Location, at the same level.
            var leaderDefinition = reference.Definitions.GetValueOrDefault(leader.DefinitionId!);
            if (leaderDefinition is null || !leaderDefinition.IsLeader || leaderDefinition.Leadership is null
                || leaderDefinition.Nationality != definition?.Nationality || leader.Broken == true || leader.LocationId != attempt.LocationId
                || leader.UnitId == unit.UnitId)
            {
                outside.Add("asl.a1.rally.leader-outside");
            }
        }
        else if (definition is not null)
        {
            // A10.63: never with a Good Order friendly leader present; A19.12: never when Disrupted.
            if (attempt.GoodOrderLeaderInLocation == true)
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
            if (definition.IsMmc && definition.SelfRally != true
                && (FieldPromotionAttempt(attempt) ? attempt.BrokenLeaderInLocation == true : definition.SelfRally == false))
            {
                outside.Add("asl.a1.rally.self-rally-not-capable");
            }
        }

        if (attempt.Rolls is { Rally: { } dice } && (dice.Count != 2 || dice.Any(die => die is < 1 or > 6))
            || attempt.Rolls?.WoundSeverity is < 1 or > 6)
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
        if (attempt.Leader is null && definition.IsMmc && definition.SelfRally is null && !FieldPromotionAttempt(attempt))
        {
            undecided.Add("asl.a1.rally.self-rally-capability-unrecorded:" + definition.Id);
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
        var kind = !selfRally ? "leader-rally" : fieldPromotion && definition.SelfRally != true ? "field-promotion-self-rally" : "self-rally";
        var drm = new List<FireModifier>();
        if (unit.DesperationMorale == true)
        {
            drm.Add(new FireModifier("desperation-morale", 4m, "A10.62"));
        }

        if (attempt.Leader is { } leader)
        {
            // A10.7, A10.72: the rallying leader's modifier, one worse when he is wounded (A17.3).
            var leadership = reference.Definitions[leader.DefinitionId!].Leadership!.Value + (leader.Wounded == true ? 1 : 0);
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
        var morale = definition.BrokenMorale!.Value - (unit.Wounded == true ? 1 : 0);
        var fate = original == 12;

        // A18.11: an Original 2 on the first MMC Self-Rally rallies the unit; A15.1: an Original 2 on a Rally other than
        // Self-Rally calls for Heat of Battle. Neither Leader Creation nor Heat of Battle is reviewed (ruling R0.2).
        var rallied = !fate && (final <= morale || (fieldPromotion && original == 2));
        var heatOfBattle = original == 2 && !selfRally;
        var leaderCreation = original == 2 && fieldPromotion;

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

        if (heatOfBattle)
        {
            events.Add("heat-of-battle-not-taken");
        }

        if (leaderCreation)
        {
            events.Add("leader-creation-not-taken");
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

        var arithmetic = new RallyArithmetic(kind, dice.ToArray(), original, drm, final, morale, rallied, fate, heatOfBattle, leaderCreation);
        return new RallyResolution(RallyResolution.Resolved, [], arithmetic,
            new RallyEffect(unit.UnitId!, definition.Id, finalDefinition, rallied, eliminated, wounded && !eliminated, concealmentLost, events));
    }
}
