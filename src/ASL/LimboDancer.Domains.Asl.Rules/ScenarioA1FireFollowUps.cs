namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// What follows a fire attack and its records (A7.7, A8.2, A9.22, A9.223, A10.62, A15.21, A23.6, C6.5; pass 32.c, slice S4, from the planner's Fire,
/// FireExtensions, and Acquisition files and the Units projector): a Fire Lane's attacks, the Encirclement placed, the effects on units and vehicles
/// as condition changes, the follow-ups of an attack, and the Acquisition that follows its units. The caller reads the state and the records and hands
/// the facts over; it writes the events in the order the verdicts give them.
/// </summary>
public static class ScenarioA1FireFollowUps
{
    /// <summary>
    /// A Fire Lane's attack on the moving stack in one of its Locations (A9.22, A9.222; ruling R12.7): Residual FP, never reduced, with no CX, leader, or
    /// hero DRM and no Cowering, taking the Hindrance of the LOS from its MG added to the Location's own.
    /// </summary>
    public static FireAttack FireLaneAttack(FireAttack facts, int laneHindranceDrm)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts with
        {
            FireLane = true,
            Los = (facts.Los ?? new FireLos(false, 0, true, false)) with
            {
                HindranceDrm = (facts.Los?.HindranceDrm ?? 0) + laneHindranceDrm
            },
        };
    }

    /// <summary>A9.22, A9.223: a Fire Lane attacks the moving stack still in its Location, once the other attacks there are made, while the lane is in place.</summary>
    public static bool FireLaneAttacks(bool laneInPlace, bool movingStackInLocation, int movers) => laneInPlace && movingStackInLocation && movers > 0;

    /// <summary>A9.223: an Original DR at least the MG's Breakdown Number (12 when the catalog gives none; two less for a captured MG, A21.11) malfunctions the MG, which ends the lane.</summary>
    public static bool FireLaneMalfunctions(int originalDr, int? breakdown, bool captured) => originalDr >= (breakdown ?? 12) - (captured ? 2 : 0);

    // The concealment gained as a Player Turn ends (GamePlanner.ConcealmentGains and AddConcealmentGains of Play, backlog pass 12, ruling R12.5; pass 32.c).

    /// <summary>
    /// The units that gain "?" as their Player Turn ends (A12.12, A12.121, A12.122, the Concealment Table; ruling R12.5): each active phasing Personnel
    /// unit from the catalog, in id order, not concealed, broken, berserk, in Melee, a prisoner, manning a Gun, or sharing its Location with enemy units
    /// (about to be held in Melee, A11.15); not in the LOS of an active, unbroken, uncaptured enemy within 16 hexes (at night, within its NVR or
    /// Illuminated, E1.101), nor beyond 16 out of Concealment Terrain; with the Final Concealment dr modifier it needs (+US#, + the best Good Order
    /// leader's Leadership in the Location, - the Location's TEM, -2 for SMOKE in the hex; A12.122, A6.7), or null when it gains "?" with no dr (at
    /// night, E1.32). A search through the fact reader (the design's D4).
    /// </summary>
    public static List<ConcealmentGain> ConcealmentGains(IEnumerable<ConcealmentCandidateFacts> units, IEnumerable<WatchingEnemyFacts> watchers, bool night, int? month,
        IConcealmentFactReader map)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(watchers);
        ArgumentNullException.ThrowIfNull(map);
        var gains = new List<ConcealmentGain>();
        var enemies = watchers.Where(unit => unit.Active && unit.Enemy && !unit.Broken && !unit.Captured && unit.Location is not null).ToArray();
        foreach (var unit in units.Where(unit => unit.Active && unit.PhasingSide && !unit.Vehicle && unit.Personnel && !unit.Dummy && unit.HasDefinition)
            .OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            if (unit.Location is not { } at || unit.Concealed || unit.Hidden || unit.Broken || unit.Berserk || unit.Melee || unit.Captured || unit.HoldsGun() || unit.EnemyInLocation())
            {
                // A unit sharing its Location with enemy units is about to be held in Melee (A11.15), and gains no "?" (a reading).
                continue;
            }

            var inLosNear = false;
            var inLosFar = false;
            var within16 = false;
            foreach (var enemy in enemies)
            {
                var range = map.Range(enemy.Location!.Value, at);
                within16 |= range <= 16;
                // E1.101 (backlog pass 16, ruling R16.4): at night an enemy sees a Location within its NVR or Illuminated.
                if (map.LosOpen(enemy.Location.Value, at) && (!night || range <= map.Nvr(enemy.Index) || map.Illuminated(at)))
                {
                    inLosNear |= range <= 16;
                    inLosFar |= range > 16;
                }
            }

            var terrain = unit.TerrainKey();
            var inSeason = month is >= 6 and <= 9;
            var concealmentTerrain = terrain is "brush" or "woods" or "orchard" or "marsh" or "wooden-building" or "stone-building" or "wooden-rubble" or "stone-rubble"
                || (terrain == "grain" && inSeason);
            if (inLosNear || (inLosFar && !concealmentTerrain))
            {
                continue;
            }

            // E1.32 (backlog pass 16, ruling R16.4): at night what would need a Concealment dr gains "?" without one.
            var needsDr = !night && (inLosFar || (!concealmentTerrain && within16));
            if (!needsDr)
            {
                gains.Add(new ConcealmentGain(unit.Id, null));
                continue;
            }

            // A12.122: +US#, + the best Good Order leader's Leadership in the Location unless alone, - the Location's TEM and in-hex Hindrance.
            var size = unit.Squad ? 3 : unit.HalfSquad || unit.Kind == "asl:crew" ? 2 : 1;
            var leadership = unit.BestLeadership();
            var tem = terrain is not null && ScenarioA1FireReference.Tem.TryGetValue(terrain, out var value) ? value : 0;
            // A6.7 (referee, pass 12): only SMOKE in the Location hinders its own units; brush, grain, orchard, and marsh do not.
            var smoke = unit.SmokeInHex() ? 2 : 0;
            gains.Add(new ConcealmentGain(unit.Id, size + leadership - tem - smoke));
        }

        return gains;
    }

    /// <summary>A12.122: a Final Concealment dr of 5 or less gains "?"; the sentence says the dr, its modifier, and the result.</summary>
    public static (bool Gains, string Reason) ConcealmentDr(string id, int dr, int modifier)
    {
        var final = dr + modifier;
        return (final <= 5, $"play.concealment: {id}'s Final Concealment dr is {dr}{modifier:+0;-0;+0} = {final}: {(final <= 5 ? "gains" : "no")} \"?\" (A12.122)");
    }

    /// <summary>A12.12, the Concealment Table: the sentence of a unit that gains "?" with no dr.</summary>
    public static string ConcealmentWithoutDr(string id) => $"play.concealment: {id} gains \"?\" (A12.12, the Concealment Table)";

    // The events of an attack (GamePlanner.AddFireEvents, VehicleEffectEvents, EffectEvents, HeroOf, EffectEvent, AddFireFollowUps, and
    // AcquisitionFollowUp of Play; pass 32.c): one function a block, called in the old order. The dice, the records, the lineage, and the events
    // themselves stay in Play, which maps each condition to its name and writes the conditions in the order given.

    /// <summary>
    /// What the Fire package asks for next (ruling R5.8): the attack resolved; an option its owner answers; a roll, named by its key, with its dice count and
    /// purpose (a Random Selection names the units it selects among, one die each, A.9, A8.31, A9.71); or an attack the package accepted and left undecided.
    /// </summary>
    public static FireNextStep NextFireStep(FireResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        if (resolution.Disposition == FireResolution.Resolved)
        {
            return new FireNextStep(true, null, null, 0, null, null);
        }

        // An option the attack reaches stops it until its owner answers (ruling R5.8).
        if (resolution.Reasons is [{ } option] && option.StartsWith("asl.a1.fire.choice-missing:", StringComparison.Ordinal))
        {
            return new FireNextStep(false, option["asl.a1.fire.choice-missing:".Length..], null, 0, null, null);
        }

        // The pre-check leaves only missing rolls, asked for one at a time.
        if (resolution.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal))
        {
            return new FireNextStep(false, null, null, 0, null, string.Join("; ", resolution.Reasons));
        }

        var key = missing["asl.a1.fire.roll-missing:".Length..];
        var split = key.IndexOf(':', StringComparison.Ordinal);
        var (kind, unit) = split < 0 ? (key, string.Empty) : (key[..split], key[(split + 1)..]);
        var selected = kind is "randomSelection" or "weaponSelection" or "firerSelection" ? unit.Split(',') : [];
        var (count, purpose) = kind switch
        {
            "attack" => (2, "fire-ift"),
            "randomSelection" => (selected.Length, "fire-random-selection"),
            "weaponSelection" => (selected.Length, "fire-weapon-selection"),
            "firerSelection" => (selected.Length, "fire-firer-selection"),
            "checks" => (2, "fire-check"),
            "leaderLoss" => (2, "fire-leader-loss"),
            "heatOfBattle" => (2, "fire-heat-of-battle"),
            "berserkCheck" => (2, "fire-berserk-check"),
            "crewCheck" => (2, "fire-crew-check"),
            "unlikelyKill" => (1, "fire-unlikely-kill"),
            "molCheck" => (1, "fire-mol-check"),
            _ => (1, "fire-wound-severity"),
        };
        return new FireNextStep(false, null, key, count, purpose, null);
    }

    /// <summary>A12.13, A12.14 (ruling R21.1): a result of none that leaves unseen targets, or nothing, unaffected is not identified to the firing side, which learns the arithmetic alone.</summary>
    public static bool HidesIdentity(FireArithmetic arithmetic, IReadOnlyList<FireTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(arithmetic);
        ArgumentNullException.ThrowIfNull(targets);
        var hiddenResult = arithmetic.Concealed?.Result ?? arithmetic.Result;
        return hiddenResult == "none" && (targets.Count == 0 || targets.Any(item => item.Concealed == true || item.Hidden == true || item.Dummy == true));
    }

    /// <summary>A10.62: a broken target attacked by FP that could inflict at least a NMC, allowing for Cowering, is under DM; <paramref name="couldCauseNmc"/> is the attack's read, by column (concealed or known).</summary>
    public static bool AttackedWhileBroken(FireTarget target, Func<bool, bool> couldCauseNmc)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(couldCauseNmc);
        return target.Broken == true && couldCauseNmc(target.Concealed == true || target.Hidden == true || target.Dummy == true);
    }

    /// <summary>D5.341, D5.41 (ruling R5.18): a Recalled AFV on its way off the map that is immobilized is Abandoned by its crew.</summary>
    public static bool AbandonsWhenImmobilized(FireVehicleEffect effect, bool mustLeave)
    {
        ArgumentNullException.ThrowIfNull(effect);
        return effect.Result == FireVehicleEffect.Immobilized && mustLeave;
    }

    /// <summary>A22.6111 (ruling R15.4): a colored dr of 6 breaks the MOL's user, under DM, when the user is active and not already broken; the conditions set, or null.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)>? MolUserBreaks(FireMolCheck? molCheck, bool userActive, bool userBroken) =>
        molCheck is { UserBroken: true } && userActive && !userBroken
            ? [(UnitCondition.Broken, true), (UnitCondition.Pinned, false), (UnitCondition.DesperationMorale, true)]
            : null;

    /// <summary>A fire counter's condition: prep-fire, first-fire, bounding-fire, or final-fire.</summary>
    public static UnitCondition Marker(string counter) => counter switch
    {
        "prep-fire" => UnitCondition.PrepFire,
        "first-fire" => UnitCondition.FirstFire,
        "bounding-fire" => UnitCondition.BoundingFire,
        _ => UnitCondition.FinalFire,
    };

    /// <summary>The MGs: a malfunction (A9.7), and the fire counter of a MG that lost its Multiple ROF (A9.2), a Final Fire counter replacing a First Fire one. <paramref name="firstFireMarked"/> is read for a Final Fire counter.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> WeaponEffectConditions(FireWeaponEffect weapon, Func<bool> firstFireMarked)
    {
        ArgumentNullException.ThrowIfNull(weapon);
        ArgumentNullException.ThrowIfNull(firstFireMarked);
        var conditions = new List<(UnitCondition, bool)>();
        if (weapon.Malfunctioned)
        {
            conditions.Add((UnitCondition.Malfunctioned, true));
        }

        if (weapon.FireCounter is { } counter)
        {
            conditions.Add((Marker(counter), true));
            if (counter == "final-fire" && firstFireMarked())
            {
                conditions.Add((UnitCondition.FirstFire, false));
            }
        }

        return conditions;
    }

    /// <summary>D7.17 (ruling R11.11): an OVR's Original 12 malfunctions a weapon that added FP (the BMG, the CMG, or the MA), or immobilizes a vehicle with none, which loses Motion; a wreck keeps no weapons.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> OverrunEffectConditions(FireOverrunEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        var conditions = new List<(UnitCondition, bool)>();
        foreach (var weapon in effect.MalfunctionedWeapons)
        {
            conditions.Add((weapon switch
            {
                FireOverrunEffect.BowMg => UnitCondition.BmgMalfunctioned,
                FireOverrunEffect.CoaxialMg => UnitCondition.CmgMalfunctioned,
                _ => UnitCondition.Malfunctioned,
            }, true));
        }

        if (effect.Immobilized)
        {
            conditions.Add((UnitCondition.Immobilized, true));
            conditions.Add((UnitCondition.Motion, false));
        }

        return conditions;
    }

    /// <summary>The fire markers (A3.2, A3.4, A3.5, A8.1, A8.3, A8.4): the units the package marks, in its order, but a unit whose MG fired alone on its Multiple ROF, which keeps its state, and a unit no longer active (<paramref name="inactive"/> is read for each).</summary>
    public static IEnumerable<string> FireMarkerUnits(FireAttack facts, FireResolution resolution, Func<string, bool> inactive)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(inactive);
        var alone = (facts.Firers ?? []).Where(item => item.UsesInherentFp == false && !resolution.FireCounterUnitIds.Contains(item.UnitId!))
            .Select(item => item.UnitId!).ToHashSet(StringComparer.Ordinal);
        return resolution.FireCounterUnitIds.Where(id => !alone.Contains(id) && !inactive(id));
    }

    /// <summary>A unit's fire marker: the counter, a Final Fire counter replacing a First Fire one, and the "?" a concealed firer or firing vehicle loses (A12.2; ruling R6.7).</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> FireMarkerConditions(string id, FireAttack facts, FireResolution resolution, bool firstFireMarked, bool concealedOrHidden)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(resolution);
        var conditions = new List<(UnitCondition, bool)> { (Marker(resolution.FireCounter!), true) };
        if (resolution.FireCounter == "final-fire" && firstFireMarked)
        {
            conditions.Add((UnitCondition.FirstFire, false));
        }

        if (resolution.FirerConcealmentLost.Contains(id) || (facts.VehicleFire?.VehicleId == id && concealedOrHidden))
        {
            conditions.Add((UnitCondition.Concealed, false));
        }

        return conditions;
    }

    /// <summary>A8.2, A8.21: Residual FP is placed, at the attack's value, unless a counter at least as large is already in the Location (<paramref name="counterAtLeast"/> is read for the value).</summary>
    public static int? ResidualFpPlaced(FireArithmetic arithmetic, bool targetKnown, Func<int, bool> counterAtLeast)
    {
        ArgumentNullException.ThrowIfNull(arithmetic);
        ArgumentNullException.ThrowIfNull(counterAtLeast);
        return arithmetic.ResidualFp is { } residual && targetKnown && !counterAtLeast(residual) ? residual : null;
    }

    /// <summary>A15.5: a surviving unit that surrendered to ADJACENT captors waits for the captor's choice, under the id its Reduction or Replacement gives it.</summary>
    public static IReadOnlyList<(string Id, IReadOnlyList<string> Captors)> SurrenderPendings(FireResolution resolution, string attemptId)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        var pending = new List<(string, IReadOnlyList<string>)>();
        foreach (var effect in resolution.Effects.Concat(resolution.FirerEffects ?? []))
        {
            if (!effect.Eliminated && (effect.SecondHeatOfBattle ?? effect.HeatOfBattle) is { Result: HeatOfBattleOutcome.Surrender, Captors.Count: > 0 } surrender)
            {
                pending.Add((effect.FinalDefinitionId != effect.DefinitionId ? $"{attemptId}-{effect.UnitId}" : effect.UnitId, surrender.Captors!));
            }
        }

        return pending;
    }

    /// <summary>
    /// A vehicle's effect (rulings R25.5, R25.6, R6.5, R6.7): a destroyed vehicle becomes a wreck, with a Blaze when it burns (D10.1, B25.14); else its
    /// conditions change as the result sets them (D.7, D5.34, D5.341, A7.82), and a concealed vehicle given a Vehicle line result, or whose crew took at
    /// least a PTC, loses its "?" to the firer in its LOS (A12.2); Residual FP has no firer.
    /// </summary>
    public static VehicleEffectVerdict VehicleEffect(FireVehicleEffect effect, string? fireKind, bool concealedOrHidden)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (effect.Result is FireVehicleEffect.Eliminated or FireVehicleEffect.BurningWreck)
        {
            return new VehicleEffectVerdict(true, effect.Result == FireVehicleEffect.BurningWreck, false, false);
        }

        var notResidual = fireKind != ScenarioA1FireCalculator.ResidualFire;
        if (ScenarioA1ResultTables.VehicleConditions(effect).Count > 0)
        {
            return new VehicleEffectVerdict(false, false, true,
                concealedOrHidden && notResidual && (effect.Result != FireVehicleEffect.None || effect.CrewCheck is not null || effect.CrewResult == FireVehicleEffect.Recalled));
        }

        return new VehicleEffectVerdict(false, false, false, concealedOrHidden && notResidual && effect.CrewCheck is not null);
    }

    /// <summary>A15.21: the heroes a unit's effect creates, in its Location, sharing its fire and movement status (ruling R5.11); a second Heat of Battle DR may create a second (ruling R5.10); concealed only when the unit kept its "?"; the creator named unless eliminated, under its Reduction's id.</summary>
    public static IReadOnlyList<HeroCreation> HeroCreations(FireUnitEffect effect, string attemptId)
    {
        ArgumentNullException.ThrowIfNull(effect);
        var creatorId = effect.FinalDefinitionId != effect.DefinitionId && !effect.Eliminated ? $"{attemptId}-{effect.UnitId}" : effect.UnitId;
        var alive = !effect.Eliminated;
        var concealed = !effect.ConcealmentLost && !effect.Eliminated;
        var heroes = new List<HeroCreation>();
        if (effect.HeatOfBattle?.HeroDefinitionId is { } hero)
        {
            heroes.Add(new HeroCreation(hero, "hero", concealed, alive ? creatorId : null));
        }

        if (effect.SecondHeatOfBattle?.HeroDefinitionId is { } second)
        {
            heroes.Add(new HeroCreation(second, "hero-2", concealed, alive ? creatorId : null));
        }

        return heroes;
    }

    /// <summary>The id of a hero a unit creates: the attempt, the creator, and the suffix.</summary>
    public static string HeroId(string attemptId, string creatorId, string suffix) => $"{attemptId}-{creatorId}-{suffix}";

    /// <summary>A hero a unit creates (A15.21): unbroken, unpinned, unwounded, with the unit's fire markers, Fanaticism, and CX; concealed when the unit is and keeps its "?" (A12.1; backlog pass 15, ruling R15.12). <paramref name="creatorHas"/> reads the creator's conditions.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> HeroConditions(bool concealed, Func<UnitCondition, bool> creatorHas)
    {
        ArgumentNullException.ThrowIfNull(creatorHas);
        var conditions = new List<(UnitCondition, bool)>
        {
            (UnitCondition.Broken, false),
            (UnitCondition.Pinned, false),
            (UnitCondition.Wounded, false),
            (UnitCondition.Concealed, concealed && creatorHas(UnitCondition.Concealed)),
            (UnitCondition.Hidden, false),
        };
        foreach (var marker in new[] { UnitCondition.PrepFire, UnitCondition.FirstFire, UnitCondition.FinalFire, UnitCondition.Fanatic, UnitCondition.Cx })
        {
            if (creatorHas(marker))
            {
                conditions.Add((marker, true));
            }
        }

        return conditions;
    }

    /// <summary>
    /// A unit's effect (A10.62, A10.8, A15.3, A15.21, A15.4, A15.42, A19.13, A7.302, A12.14): elimination; else the conditions it sets, only where they
    /// change (broken, pinned, wounded, disrupted; DM on breaking or when attacked broken; "?" lost; Fanatic; berserk, heroic, and Battle Hardened end
    /// DM), and a lineage: a squad Replaced by its two HS (ruling R15.9), Casualty Reduction, or Replacement, where only a Battle Hardened unit that
    /// did not lose it keeps "?". <paramref name="currentlyTrue"/> reads the unit's conditions; <paramref name="finalKind"/> the final definition's kind.
    /// </summary>
    public static EffectVerdict Effect(FireUnitEffect effect, bool attackedWhileBroken, Func<UnitCondition, bool> currentlyTrue, string unitKind, Func<string> finalKind)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ArgumentNullException.ThrowIfNull(currentlyTrue);
        ArgumentNullException.ThrowIfNull(finalKind);
        if (effect.Eliminated)
        {
            return new EffectVerdict(true, [], null, false);
        }

        var conditions = new List<(UnitCondition, bool)>();
        void Set(UnitCondition name, bool value)
        {
            if (currentlyTrue(name) != value)
            {
                conditions.Add((name, value));
            }
        }

        Set(UnitCondition.Broken, effect.Broken);
        Set(UnitCondition.Pinned, effect.Pinned);
        Set(UnitCondition.Wounded, effect.Wounded);
        Set(UnitCondition.Disrupted, effect.Disrupted);
        if (effect.Broken && (!currentlyTrue(UnitCondition.Broken) || attackedWhileBroken))
        {
            Set(UnitCondition.DesperationMorale, true);
        }

        if (effect.ConcealmentLost)
        {
            conditions.Add((UnitCondition.Concealed, false));
            conditions.Add((UnitCondition.Hidden, false));
        }

        // A10.8, A15.3: Fanaticism, once gained, lasts; A15.21: a heroic leader.
        if (effect.Fanatic == true)
        {
            Set(UnitCondition.Fanatic, true);
        }

        // A15.4, A15.42: a unit that goes berserk is rallied and no longer under DM.
        if (effect.Berserk == true)
        {
            Set(UnitCondition.Berserk, true);
            Set(UnitCondition.DesperationMorale, false);
        }

        if (effect.Heroic == true)
        {
            Set(UnitCondition.Heroic, true);
        }

        // A15.3: a Battle Hardened unit is unbroken, so no longer under DM; A15.21: nor is a leader made heroic.
        if (effect.HeatOfBattle?.Hardening == true || effect.HeatOfBattle?.Heroic == true)
        {
            Set(UnitCondition.DesperationMorale, false);
        }

        if (effect.SplitIntoHalfSquads == true)
        {
            // A19.13 (ruling R15.9): a squad with an underscored Morale Factor is Replaced by its two broken HS; the first keeps its SW, as a Deployment's does.
            return new EffectVerdict(false, conditions, EffectVerdict.Deployed, false);
        }

        if (effect.FinalDefinitionId != effect.DefinitionId)
        {
            // A7.302: Casualty Reduction makes a HS of the same broken status; A19.13: Replacement by a lesser unit; A15.3: Battle Hardening by an
            // unbroken, unpinned unit of the next higher quality. A12.14: a unit that passed its MC and was Battle Hardened keeps "?" unless the attack
            // cost it; any other Reduction or Replacement loses it.
            var reduced = unitKind == "asl:squad" && finalKind() == "asl:half-squad";
            var hardened = effect.HeatOfBattle?.HardenedDefinitionId == effect.FinalDefinitionId;
            return new EffectVerdict(false, conditions, reduced ? EffectVerdict.Reduced : EffectVerdict.Replaced, hardened && !effect.ConcealmentLost);
        }

        return new EffectVerdict(false, conditions, null, false);
    }

    /// <summary>A9.5: Spraying Fire's second Location takes the same Original DR, against the side of its first unit not the firing side's own, else the first attack's target side.</summary>
    public static string SprayTargetSide(FireAttack spray, Func<string, string> unitSide, string targetSide)
    {
        ArgumentNullException.ThrowIfNull(spray);
        ArgumentNullException.ThrowIfNull(unitSide);
        return spray.Targets!.FirstOrDefault(item => item.Friendly != true) is { } sprayed ? unitSide(sprayed.UnitId!) : targetSide;
    }

    /// <summary>A7.7: the Encirclement is placed when units of the sealed side remain in the target Location after the attack.</summary>
    public static bool PlacesEncirclement(bool recordMade, bool sealedSideRemains) => recordMade && sealedSideRemains;

    /// <summary>A9.22: no Fire Lane when the manning Infantry Cowered or the MG malfunctioned; a placed lane marks the MG First Fire unless it is already.</summary>
    public static (bool Placed, bool MarkFirstFire) PlacesFireLane(bool recordMade, bool cowered, bool weaponInPlay, bool malfunctioned, bool firstFireMarked)
    {
        var placed = recordMade && !cowered && weaponInPlay && !malfunctioned;
        return (placed, placed && !firstFireMarked);
    }

    /// <summary>A23.6 (ruling R15.3): a Thrown DC that did not malfunction attacks its thrower's Location next, at range 0, at the same level, in the thrower's terrain.</summary>
    public static FireAttack ThrowerAttack(FireAttack back, string? thrownFromTerrain)
    {
        ArgumentNullException.ThrowIfNull(back);
        return back with
        {
            Range = 0,
            SameLevel = true,
            TargetTerrain = thrownFromTerrain,
        };
    }

    /// <summary>
    /// The Acquisition events a commit calls for after it moved acquired units (C6.5, C6.51; ruling R5.13): a unit that entered a Location out of its
    /// Gun's LOS is no longer acquired, and when none is left the counter stays in the last Location in LOS; when the acquired units are in more than one
    /// Location as one of them ends its MPh, APh, or CCPh withdrawal, the Gun's side chooses which Location keeps it (once no choice is pending).
    /// </summary>
    public static IReadOnlyList<AcquisitionVerdict> AcquisitionFollowUp(IEnumerable<AcquisitionFacts> acquisitions, string? phase, bool choicePending)
    {
        ArgumentNullException.ThrowIfNull(acquisitions);
        var verdicts = new List<AcquisitionVerdict>();
        foreach (var acquisition in acquisitions.Where(item => item.Units.Count > 0))
        {
            if (!acquisition.GunOnMap || !acquisition.ExistedBefore)
            {
                continue;
            }

            var kept = new List<AcquiredUnitFacts>();
            string? last = null;
            foreach (var unit in acquisition.Units)
            {
                if (unit.Now is null || unit.Was is null || unit.Now == unit.Was || unit.LosClear())
                {
                    kept.Add(unit);
                }
                else
                {
                    last = unit.Was;
                }
            }

            string[] locations = [.. kept.Select(unit => unit.Now!).Distinct(StringComparer.Ordinal)];
            (string, IReadOnlyList<string>)? changed = kept.Count < acquisition.Units.Count
                ? (locations is [{ } only] ? only : kept.Count == 0 ? last ?? acquisition.PreviousLocation! : acquisition.Location, [.. kept.Select(unit => unit.Id)])
                : null;

            // C6.51: the choice is due once a split unit has finished its MPh, APh, or CCPh withdrawal.
            var ended = kept.Any(unit => (unit.EndedAfter && !unit.EndedBefore) || (phase != "mph" && unit.Was != unit.Now));
            IReadOnlyList<string>? choice = locations.Length > 1 && ended && !choicePending ? [.. locations.Order(StringComparer.Ordinal)] : null;
            verdicts.Add(new AcquisitionVerdict(acquisition.Gun, changed, choice));
        }

        return verdicts;
    }

    // The projector's fire, Residual FP, SW, Opportunity Fire, Encirclement, Fire Lane, and Acquisition records (GameProjector of Units; pass 32.c). A
    // refusal crosses as its code and text, so each diagnostic keeps its count in the text list.

    /// <summary>
    /// A fire record (A8.1, A7.55, A8.22, D3.4, D3.5, D7.14; rulings R25.7, R11.11): a Defensive fire record answers the open window of the moving
    /// stack's latest step; a Location's units fire at a target once per phase (in the MPh, once per MF expenditure), as one fire group; Residual FP has
    /// no firers and never forms one; a vehicle's MG shot and an OVR are the vehicle's own attacks.
    /// </summary>
    public static RecordRefusal? VerifyFireRecord(int? movementStep, bool windowOpen, int? windowStep, int firers, bool byVehicle, IEnumerable<PhaseFireFacts> fires,
        string firerLocation, string targetLocation)
    {
        ArgumentNullException.ThrowIfNull(fires);
        if (movementStep is { } answered && (!windowOpen || windowStep != answered))
        {
            return new RecordRefusal("UNIT-STATE-024", $"Defensive First Fire answers the open window on the moving stack's step {answered} (A8.1).");
        }

        return firers > 0 && fires.Any(item => !(byVehicle && item.Vehicle) && item.FirerLocation == firerLocation && item.TargetLocation == targetLocation && item.Step == movementStep)
            ? new RecordRefusal("UNIT-STATE-024", $"{firerLocation} has already fired at {targetLocation} this phase (A7.55).")
            : null;
    }

    /// <summary>C2.24, D3.5 (unit step 25): a vehicle's MG shot keeps its Multiple ROF for this phase when its one weapon effect says so.</summary>
    public static bool VehicleShotKeepsRof(int weaponEffects, bool? rateOfFireRetained) => weaponEffects == 1 && rateOfFireRetained == true;

    /// <summary>A vehicle's MG shots this phase: one more than before.</summary>
    public static int VehicleShotCount(int? previousShots) => (previousShots ?? 0) + 1;

    /// <summary>Residual FP a fire record left (A8.2, A8.21): placed in the MPh, in its record's target Location, at the record's own value; only a larger counter replaces one there.</summary>
    public static RecordRefusal? VerifyResidualFp(string? phase, bool recordKnown, bool sameTarget, int? recordedFp, int fp, string fireId, int? existingFp)
    {
        if (phase != "mph" || !recordKnown || !sameTarget || recordedFp != fp)
        {
            return new RecordRefusal("UNIT-STATE-026", $"Residual FP must be the value its fire record '{fireId}' leaves in its target Location (A8.2).");
        }

        return existingFp is { } present && present >= fp ? new RecordRefusal("UNIT-STATE-026", "Only a larger Residual FP counter replaces one already in the Location (A8.21).") : null;
    }

    /// <summary>
    /// A7.351 (rulings R9.2, R9.4, R9.7): a squad whose only fire this phase is one SW use: a squad not yet marked gains the entry for its weapon; an entry
    /// for another weapon, or a second Panzerfaust, ends it; a non-squad has none. The uses after this use.
    /// </summary>
    public static IReadOnlyList<(string Unit, string Weapon)> SupportWeaponUse(IReadOnlyList<(string Unit, string Weapon)> uses, string unitId, bool squad, bool marked, string weapon)
    {
        ArgumentNullException.ThrowIfNull(uses);
        var existing = uses.FirstOrDefault(item => item.Unit == unitId);
        return !squad ? uses
            : existing.Unit is null ? (marked ? uses : [.. uses, (unitId, weapon)])
            : existing.Weapon != weapon || weapon == "panzerfaust" ? [.. uses.Where(item => item.Unit != unitId)]
            : uses;
    }

    /// <summary>C13.31 (ruling R9.7): a Panzerfaust shot counts against its side's usage when its check's outcome is a shot.</summary>
    public static bool PanzerfaustShotCounts(bool panzerfaust, string? outcome) => panzerfaust && outcome == "shot";

    /// <summary>A7.351 (table player, pass 9): the units of a fire record have fired this phase, each with the count of SW it used, replacing their earlier entries.</summary>
    public static IReadOnlyList<(string Unit, string Weapon)> PhaseFirersAfterFire(IReadOnlyList<(string Unit, string Weapon)> phaseFirers, IReadOnlyList<string> firers, Func<string, int> weaponsUsed)
    {
        ArgumentNullException.ThrowIfNull(phaseFirers);
        ArgumentNullException.ThrowIfNull(firers);
        ArgumentNullException.ThrowIfNull(weaponsUsed);
        return [.. phaseFirers.Where(item => !firers.Contains(item.Unit, StringComparer.Ordinal)),
            .. firers.Select(unit => (unit, weaponsUsed(unit).ToString(System.Globalization.CultureInfo.InvariantCulture)))];
    }

    /// <summary>A7.351 (ruling R9.2): a squad that fires its inherent FP after its one SW use has fired, and its use ends: whether any firer of the record has a use.</summary>
    public static bool SupportWeaponUseEnds(IEnumerable<string> useUnits, IReadOnlyList<string> firers)
    {
        ArgumentNullException.ThrowIfNull(useUnits);
        ArgumentNullException.ThrowIfNull(firers);
        return useUnits.Any(unit => firers.Contains(unit, StringComparer.Ordinal));
    }

    /// <summary>Opportunity Fire (A7.25; ruling R12.1): declared in the PFPh for Good Order Infantry of the phasing side, named once each, that have not fired, not berserk, in Melee, or prisoners.</summary>
    public static RecordRefusal? VerifyOpportunityFire(string? phase, int units, bool namedOnce, IEnumerable<OpportunityRecordUnitFacts> declared)
    {
        ArgumentNullException.ThrowIfNull(declared);
        return phase != "pfph" || units == 0 || !namedOnce
            || declared.Any(unit => !unit.Found || !unit.PhasingSide || !unit.Personnel || unit.Broken || unit.Berserk || unit.Melee || unit.Captured || unit.PrepFire || unit.BoundingFire)
            ? new RecordRefusal("UNIT-STATE-040", "Opportunity Fire is declared in the PFPh for Good Order Infantry of the phasing side that have not fired (A7.25).")
            : null;
    }

    /// <summary>An Encirclement (A7.7; ruling R12.11): placed by a fire record of a fire phase at the Location, on a side with units there; one already there is kept as it is.</summary>
    public static (RecordRefusal? Refusal, bool Add) VerifyEncirclement(string? phase, bool recordKnown, bool sameTarget, bool sideThere, bool exists) =>
        phase is not ("pfph" or "dfph" or "afph") || !recordKnown || !sameTarget || !sideThere
            ? (new RecordRefusal("UNIT-STATE-040", "An Encirclement is placed by a fire record of a fire phase at a Location holding units of the Encircled side (A7.7)."), false)
            : (null, !exists);

    /// <summary>A7.7 (ruling R12.11): an Encirclement stands while an active, uncaptured, non-Dummy unit it Encircles is left in its Location.</summary>
    public static bool EncirclementStands(IEnumerable<EncircledLocationUnitFacts> unitsThere)
    {
        ArgumentNullException.ThrowIfNull(unitsThere);
        return unitsThere.Any(unit => unit.Active && !unit.Dummy && !unit.Captured && unit.Encircled);
    }

    /// <summary>A Fire Lane (A9.22; ruling R12.7): placed in the MPh by its MG's fire record, for an active MG and operator, every entry with FP, one per MG.</summary>
    public static RecordRefusal? VerifyFireLane(string? phase, bool recordKnown, bool weaponActive, bool operatorActive, IEnumerable<int> entryFps, bool laneExists)
    {
        ArgumentNullException.ThrowIfNull(entryFps);
        return phase != "mph" || !recordKnown || !weaponActive || !operatorActive || entryFps.Any(fp => fp <= 0) || laneExists
            ? new RecordRefusal("UNIT-STATE-040", "A Fire Lane is placed in the MPh by its MG's fire record (A9.22).")
            : null;
    }

    /// <summary>A9.223 (ruling R12.7): a Fire Lane stands while its MG is in play and working and its manning Infantry is in play, unbroken, and unpinned.</summary>
    public static bool FireLaneStands(bool mgActive, bool mgMalfunctioned, bool operatorActive, bool operatorBroken, bool operatorPinned) =>
        mgActive && !mgMalfunctioned && operatorActive && !operatorBroken && !operatorPinned;

    /// <summary>C6.5, C6.51 (ruling R5.13): an Acquisition changes only for a Gun that has one, onto active, unconcealed enemy units all in the named Location.</summary>
    public static RecordRefusal? VerifyAcquisitionChange(bool hasAcquisition, string? gunSide, IEnumerable<AcquiredUnitRecordFacts> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return !hasAcquisition || gunSide is null || units.Any(unit => !unit.Active || unit.Side == gunSide || !unit.InLocation || unit.Concealed)
            ? new RecordRefusal("UNIT-STATE-033", "An Acquisition changes for a Gun that has one, onto Known enemy units in its Location (C6.5, C6.51).")
            : null;
    }

    /// <summary>C6.5, D1.3, C9.2: an Acquisition is kept while its Gun is manned by an active crew not known to be out of Good Order, its light mortar is possessed by such a unit, or its tank is active and not Abandoned.</summary>
    public static bool AcquisitionHolds(AcquisitionHolderFacts holder)
    {
        ArgumentNullException.ThrowIfNull(holder);
        return holder.GunMannedByActiveCrew ? holder.HolderGoodOrder != RuleState.False
            : holder.LightMortarPossessedByActiveUnit ? holder.HolderGoodOrder != RuleState.False
            : holder.ActiveVehicle && !holder.Abandoned;
    }

    /// <summary>
    /// C6.5, C6.51 (ruling R5.13): an Acquisition follows its units into their successors (A7.302, A19.13), drops those no longer active or taken
    /// prisoner, and follows them while they share one Location (their texts); else it keeps its Location.
    /// </summary>
    public static (IReadOnlyList<string> Units, string Location) AcquisitionAfter(IReadOnlyList<string> units, IReadOnlyList<string>? consumed, IReadOnlyList<string>? produced,
        Func<string, bool> activeUncaptured, Func<string, string?> locationOf, string location)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(activeUncaptured);
        ArgumentNullException.ThrowIfNull(locationOf);
        var kept = units.ToList();
        if (consumed is not null && produced is not null && kept.Any(consumed.Contains))
        {
            kept = [.. kept.Where(id => !consumed.Contains(id)), .. produced];
        }

        kept = [.. kept.Where(activeUncaptured)];
        var locations = kept.Select(locationOf).Distinct(StringComparer.Ordinal).ToArray();
        return (kept, locations is [{ } only] ? only : location);
    }
}
