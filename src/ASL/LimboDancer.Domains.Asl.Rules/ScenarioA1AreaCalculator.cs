using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A light mortar's shot on the Area Target Type (backlog pass 9; rulings R9.2 to R9.4): the red Area row of the To Hit Table with the C4
/// modifications, the shot's DRM plus each unit's own, a hit judged for every unit in the target hex, friendly ones too (C3.33, C3.331), a
/// Critical Hit only on an Original 2 (C9.5, C3.7), and the IFT attacks of the units hit at half the HE FP, a Critical Hit at the full FP
/// doubled (C3.71). Pass 60 (rulings R60.10 to R60.14): the vehicles of the hex (C1.55, C3.332), the Locations of a building hex (B23.32), a Gun
/// and its crew (C11.4, C11.5), and the Area Acquisition of the whole hex (C6.521).
/// </summary>
internal static class ScenarioA1AreaCalculator
{
    private const string Prefix = "asl.a1.ordnance.";

    // C1.55: the AF an AFV's every Aspect presents, hull and turret, front and side.
    private static readonly string[] Hull = ["hull"];
    private static readonly string[] HullAndTurret = ["hull", "turret"];
    private static readonly string[] Facings = ["front", "side"];

    public static OrdnanceResolution Run(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference) => Run(shot, gun, reference, null);

    /// <summary>
    /// The Area Target Type shot. With <see cref="OrdnanceShot.StunEncircledReading"/> null it keeps the answer it had before the D5.34 claim: no Stun +1 on
    /// the To Hit DR of an AFV under a "+1" counter, which D5.34 (p. 203) gives to "any TH ... DR it makes"; set, it adds
    /// <see cref="ScenarioA1StunCalculator.Drm"/> ("stun"; the claim's ruling RD5.16, W-D5.34). No vehicle-mounted mortar is in the catalog today. Nor,
    /// null, does it add the Encircled +1 of an Encircled crew (A7.7, p. 57: "+1 DRM on the IFT (or To Hit DR if ordnance)"); set, it adds
    /// <see cref="ScenarioA1EncirclementCalculator.FirerDrm"/> for <see cref="OrdnanceCrew.Encircled"/> ("encircled").
    /// </summary>
    public static OrdnanceResolution Run(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, VehicleBreakdownUseFacts? breakdownFacts)
    {
        var rolls = shot.Rolls!;
        static bool MalformedEffects(IReadOnlyList<int>? dice) => dice is not null && (dice.Count != 2 || dice.Any(die => die is < 1 or > 6));
        if (breakdownFacts is not null && (MalformedEffects(rolls.ToHit) || MalformedEffects(rolls.Hit?.Attack) || MalformedEffects(rolls.CriticalHit?.Attack)))
        {
            return new(OrdnanceResolution.Abstained, [Prefix + "roll-malformed"], null, null);
        }

        var hit = shot.Hit!;
        var range = shot.Range!.Value;
        var crew = shot.Crew!;
        var levels = shot.AreaLocations;

        // C3.33, C4: the Area row's red Basic TH#, for every nationality, and the Gun and Ammo modifications.
        var basic = reference.AreaToHit(range);
        var modifications = reference.Modifications(gun, range);
        var modified = basic + (int)modifications.Sum(item => item.Value);

        // C3.331: Cases C1 to C4, E, G, L, and Q do not apply; a SW has no Case A (no CA) and no Case F.
        var drm = new List<FireModifier>();
        if (ScenarioA1OrdnanceCalculator.AfphFire(shot))
        {
            drm.Add(new FireModifier("case-b", shot.FirerInWoodsOrBuilding == true ? 3 : 2, "C5.2"));
        }

        // C5.4, C9.3: a pinned firer, or a pinned Spotter, which in effect pins the mortar's firer.
        var pinned = crew.Pinned == true || shot.Spotter?.Pinned == true;
        if (pinned)
        {
            drm.Add(new FireModifier("case-d", 2, "C5.4"));
        }

        if (crew.Cx == true)
        {
            drm.Add(new FireModifier("cx", 1, "A4.51"));
        }

        // D5.34 (p. 203; the D5.34 claim, ruling RD5.16): an AFV under a "+1" counter after a Stun adds one to its To Hit DR, under the shot's
        // StunEncircledReading.
        if (shot.StunEncircledReading == true && ScenarioA1StunCalculator.Drm(StunDr.ToHit, shot.Vehicle?.StunRecovery == true) is > 0 and var stun)
        {
            drm.Add(new FireModifier("stun", stun, "D5.34"));
        }

        // A7.7 (p. 57; the D5.34 claim, ruling RD5.16): an Encircled crew adds one to its To Hit DR, under the shot's StunEncircledReading.
        if (shot.StunEncircledReading == true && ScenarioA1EncirclementCalculator.FirerDrm(crew.Encircled == true) is > 0 and var encircled)
        {
            drm.Add(new FireModifier("encircled", encircled, "A7.7"));
        }

        // C9.31 (ruling R9.4): Spotted fire.
        if (shot.Spotter is not null)
        {
            drm.Add(new FireModifier("spotted", 2, "C9.31"));
        }

        drm.AddRange(ScenarioA1OrdnanceCalculator.Leadership(shot, reference));

        // A15.23 (pass 72, ruling R72.8): a hero's -1 on the To Hit DR, never on the IFT DR of the units hit.
        drm.AddRange(ScenarioA1OrdnanceCalculator.HeroDrm(shot, gun, reference));
        if (shot.FirerOverstack is { } over && over > 0)
        {
            drm.Add(new FireModifier("overstack-firer", over, "A5.12"));
        }

        // C6.13, C6.14 (ruling R8.1): FFNAM and FFMO as To Hit DRM of Defensive First Fire; C6.6: Case O in their place.
        if (shot.FireKind is not null && shot.Movement is { } movement)
        {
            drm.AddRange(ScenarioA1OrdnanceCalculator.MoverDrm(movement));
        }

        // C3.4 (pass 60, ruling R60.11): each Location of a building hex has its own Hindrance, so Case R is then each target's own.
        if (levels is null && hit.Los!.HindranceDrm is int hindrance && hindrance > 0)
        {
            drm.Add(new FireModifier("case-r", hindrance, "C6.9"));
        }

        // C6.521 (pass 60, rulings R60.3, R60.13): the Area Acquisition covers every Location of its hex in the firer's LOS.
        var acquisition = shot.AreaAcquisition ?? shot.Acquisition!.Value;

        // C3.331 (ruling R9.3): each unit's own DRM: Case K for a concealed enemy unit, the Area Target Acquisition for every unit, concealed or
        // not (C6.521, C6.57; referee, pass 9), and the -1 per squad equivalent its side overstacks the Location (A5.131).
        // B23.32, C3.4 (pass 60, ruling R60.11): each Location of the hex in the firer's LOS adds its own Case R. C3.33 (p. 169; ruling R60.24): one out
        // of it is not judged; its units and vehicles are hit when the shot hits the hardest non-hidden enemy target in LOS (below).
        // C3.332, C6.7 (pass 60, ruling R60.10): each vehicle of the hex is judged on its own as well, with its size (Case P); C11.2 (ruling R60.14):
        // a Gun's crew alone with the Gun's.
        bool OutOfLos(string? locationId) => Location(shot, locationId)?.Los is { Blocked: true };
        var targets = hit.Targets!.Where(item => !OutOfLos(item.LocationId)).ToArray();
        var vehicles = (hit.Vehicles ?? []).Where(item => !OutOfLos(item.LocationId)).ToArray();
        void Own(List<FireModifier> list, string? locationId, bool concealed)
        {
            if (concealed)
            {
                list.Add(new FireModifier("case-k", 2, "C6.2"));
            }

            if (acquisition < 0)
            {
                list.Add(new FireModifier("case-n", acquisition, "C6.521"));
            }

            if (levels is not null && Location(shot, locationId)?.Los?.HindranceDrm is > 0 and var own)
            {
                list.Add(new FireModifier("case-r", own, "C6.9"));
            }
        }

        var judging = new List<Judged>();
        foreach (var target in targets)
        {
            var list = new List<FireModifier>();
            var enemy = target.Dummy == true || reference.Fire.Definitions.GetValueOrDefault(target.DefinitionId ?? string.Empty)?.Nationality != ScenarioA1OrdnanceCalculator.FriendlyNationality(shot, gun);
            var concealed = enemy && (target.Concealed == true || target.Hidden == true || target.Dummy == true);
            if (concealed)
            {
                list.Add(new FireModifier("case-k", 2, "C6.2"));
            }

            if (acquisition < 0)
            {
                list.Add(new FireModifier("case-n", acquisition, "C6.521"));
            }

            if (enemy && shot.TargetOverstack is { } crowded && crowded > 0)
            {
                list.Add(new FireModifier("overstack-target", -crowded, "A5.131"));
            }

            if (levels is not null && Location(shot, target.LocationId)?.Los?.HindranceDrm is > 0 and var own)
            {
                list.Add(new FireModifier("case-r", own, "C6.9"));
            }

            if (hit.GunTarget is { CrewUnitId: { } gunCrew } gunTarget && gunCrew == target.UnitId
                && reference.Guns.GetValueOrDefault(gunTarget.DefinitionId ?? string.Empty)?.TargetSize is { } gunSize && gunSize != "average")
            {
                list.Add(new FireModifier("case-p:" + gunSize, gunSize == "small" ? 1 : -1, "C11.2"));
            }

            judging.Add(new Judged(target.UnitId!, target.LocationId, list, target, null));
        }

        foreach (var vehicle in vehicles)
        {
            var list = new List<FireModifier>();
            Own(list, vehicle.LocationId, vehicle.Concealed == true);
            var targetSize = reference.Armor.Vehicles.GetValueOrDefault(vehicle.DefinitionId ?? string.Empty)?.TargetSize;
            var size = targetSize switch
            {
                "very-small" => 2,
                "small" => 1,
                "large" => -1,
                "very-large" => -2,
                _ => 0,
            };
            if (size != 0)
            {
                list.Add(new FireModifier("case-p:" + targetSize, size, "C6.7"));
            }

            judging.Add(new Judged(vehicle.VehicleId!, vehicle.LocationId, list, null, vehicle));
        }

        if (rolls.ToHit is not { } dice)
        {
            return Refused([Prefix + "roll-missing:toHit"]);
        }

        var common = (int)drm.Sum(item => item.Value);
        var original = dice[0] + dice[1];
        var breakdownOutcome = breakdownFacts is null ? null
            : ScenarioA1VehicleBreakdownResolutionCalculator.ResolveArea(breakdownFacts, original, null);
        var ownEffectsRead = false;
        void ReadOwnEffects(IReadOnlyList<int>? effects)
        {
            if (breakdownFacts is not null && !ownEffectsRead && effects is not null)
            {
                breakdownOutcome = ScenarioA1VehicleBreakdownResolutionCalculator.ResolveArea(breakdownFacts, original, effects[0] + effects[1]);
                ownEffectsRead = true;
            }
        }
        int Lowest(Judged item) => 2 + common + (int)item.Drm.Sum(modifier => modifier.Value);

        // C3.7, C3.6, C3.331 (referee, pass 9): judged for each unit, an Original 2 is a Critical Hit on a unit more than the lowest Final DR could
        // hit; one only the lowest Final DR hits needs a subsequent dr of 1, and one no Final DR could hit is hit on a subsequent dr of 1 (a Critical
        // Hit) to 3. One subsequent dr serves them all.
        int? subsequent = null;
        if (original == 2 && judging.Any(item => Lowest(item) >= modified))
        {
            if (rolls.Subsequent is not { } dr)
            {
                return Refused([Prefix + "roll-missing:subsequent"]);
            }

            subsequent = dr;
        }

        var judged = judging.Select(item =>
        {
            var final = original + common + (int)item.Drm.Sum(modifier => modifier.Value);
            var isHit = Lowest(item) <= modified ? final <= modified : original == 2 && subsequent is >= 1 and <= 3;
            return (Item: item, Final: final, Hit: isHit);
        }).ToArray();
        // C9.5 (ruling RC9.6): only an Original 2 is a mortar's Critical Hit, as the mortar calculator says; C3.7's subsequent dr stays here.
        var eligible = judged.Where(item => item.Hit && ScenarioA1MortarCalculator.CriticalHitAllowed(original) && (Lowest(item.Item) < modified || subsequent == 1))
            .Select(item => item.Item).ToArray();

        // C3.33 (p. 169; pass 60, ruling R60.24): "a mortar also hits all target-hex units that are out of its firer's (Spotter's ...) LOS if that
        // shot hit the non-hidden enemy target that currently was the hardest for it to hit (i.e., that received the highest net TH DRM)": they are
        // hit, on no To Hit DR of their own, and never by the Critical Hit, which falls among the targets it was obtained against (C3.74).
        var hardest = judged.Where(item => item.Item.Vehicle is not null || item.Item.Target is { Hidden: not true }).ToArray();
        var beyondHit = hardest.Length > 0 && hardest.Where(item => item.Item.Drm.Sum(modifier => modifier.Value) == hardest.Max(other => other.Item.Drm.Sum(modifier => modifier.Value)))
            .Any(item => item.Hit);
        var beyondFinal = hardest.Length > 0 ? hardest.Max(item => item.Final) : original + common;
        Judged[] beyond = [.. hit.Targets!.Where(item => OutOfLos(item.LocationId)).Select(item => new Judged(item.UnitId!, item.LocationId, [], item, null)),
            .. (hit.Vehicles ?? []).Where(item => OutOfLos(item.LocationId)).Select(item => new Judged(item.VehicleId!, item.LocationId, [], null, item))];
        judged = [.. judged, .. beyond.Select(item => (Item: item, Final: beyondFinal, Hit: beyondHit))];
        var hitIds = judged.Where(item => item.Hit).Select(item => item.Item.Id).ToArray();
        var critical = eligible.Length > 0;
        var improbable = judging.All(item => Lowest(item) > modified);
        var toHit = new OrdnanceToHit("red", basic, modifications, modified, [.. dice], original, drm, original + common, improbable, subsequent,
            hitIds.Length > 0, critical);

        // C2.24, C9.31, C9.2: the Multiple ROF, one lower for Spotted fire, none for a lone SMC; C5.4: none when pinned; C5.2: none in the AFPh.
        // A21.12 (W-A21.13): the penalty multiple lowers it one each; a shot recorded before the wiring was not lowered.
        var rof = ScenarioA1OrdnanceCalculator.UnloweredRateOfFire(shot, gun);
        if (shot.Spotter is not null && rof > 0)
        {
            rof--;
        }

        // C9.2, A21.13 (ruling RC9.4): a lone SMC loses the Multiple ROF; only one firer is accepted, so a leader or hero firer is a lone SMC.
        var loneSmc = reference.Fire.Definitions.GetValueOrDefault(crew.DefinitionId!)?.Kind is "asl:leader" or "asl:hero";
        if (pinned || ScenarioA1OrdnanceCalculator.AfphFire(shot) || !ScenarioA1MortarCalculator.MultipleRofKept(loneSmc))
        {
            rof = 0;
        }

        FireResolution? normal = null, criticalHit = null;
        var levelHits = new List<FireResolution>();
        var afvs = new List<OrdnanceAreaVehicle>();
        string? criticalTarget = null;
        string? fate = null;
        if (hitIds.Length > 0)
        {
            IReadOnlyList<Judged> picked = [];
            if (critical)
            {
                var pool = eligible;

                // C3.74 (p. 171; pass 60, ruling R60.11): "if ... more than one occupied Location is hit, use Random Selection to determine the occupied
                // Location in which the CH occurs" (the highest dr, a tie to the lowest Location), then the unit there.
                string[] places = levels is null ? [] : [.. levels.Select(item => item.LocationId).Where(place => pool.Any(item => item.LocationId == place))];
                if (places.Length > 1)
                {
                    var key = "criticalSelection:" + string.Join(",", places);
                    if (rolls.CriticalSelection is not { } byPlace || places.Any(item => !byPlace.ContainsKey(item)))
                    {
                        return Refused([Prefix + "roll-missing:" + key]);
                    }

                    var top = places.Max(item => byPlace[item]);
                    var place = places.First(item => byPlace[item] == top);
                    pool = [.. pool.Where(item => item.LocationId == place)];
                    criticalTarget = string.Join(",", pool.Select(item => item.Id));
                }

                if (pool.Length == 1)
                {
                    picked = pool;
                }
                else
                {
                    // C3.74: a Critical Hit applies to the unit Random Selection picks among those it could fall on (the highest dr, ties alike).
                    var key = "criticalSelection:" + string.Join(",", pool.Select(item => item.Id));
                    if (rolls.CriticalSelection is not { } drs || pool.Any(item => !drs.ContainsKey(item.Id)))
                    {
                        return Refused([Prefix + "roll-missing:" + key]);
                    }

                    var highest = pool.Max(item => drs[item.Id]);
                    picked = [.. pool.Where(item => drs[item.Id] == highest)];
                    criticalTarget = string.Join(",", picked.Select(item => item.Id));
                }

                // C3.71 (ruling R9.3): the Critical Hit on its units and unarmored vehicles; C3.72: an AFV it falls on is resolved below.
                var criticalTargets = picked.Where(item => item.Target is not null).Select(item => item.Target!).ToArray();
                var criticalVehicles = picked.Where(item => item.Vehicle is { } vehicle && !Armored(shot, vehicle, reference)).Select(item => item.Vehicle!).ToArray();
                if (criticalTargets.Length > 0 || criticalVehicles.Length > 0)
                {
                    ReadOwnEffects(rolls.CriticalHit?.Attack);
                    criticalHit = ScenarioA1FireCalculator.Resolve(LocationAttack(shot, gun, reference, true, picked[0].LocationId, criticalTargets, criticalVehicles, rolls.CriticalHit),
                        reference.Fire);
                    if (Pending(criticalHit, "critical-hit:") is { } pending)
                    {
                        return pending;
                    }
                }
            }

            // C3.33: every unit hit is attacked with a single Effects DR, so the normal hit reuses the Critical Hit's.
            var shared = criticalHit?.Arithmetic?.Dice ?? rolls.Hit?.Attack;
            ReadOwnEffects(shared);

            // C1.55, C3.72, C9.1 (pass 60, ruling R60.10): each AFV hit is attacked on the IFT with the same DR, and its Vulnerable crew Collaterally
            // when the AFV is neither destroyed nor Shocked (C3.332 and its EX).
            var struckAfvs = judged.Where(item => item.Hit && item.Item.Vehicle is { } vehicle && Armored(shot, vehicle, reference)).Select(item => item.Item).ToArray();
            if (struckAfvs.Length > 0 && shared is null)
            {
                return Refused([Prefix + "roll-missing:hit:attack"]);
            }

            foreach (var afv in struckAfvs)
            {
                // C1.55, C3.9, C7.7 note E (pass 60, ruling R60.23): Indirect Fire takes the hit Location from the Original IFT DR's dice.
                var turret = reference.Armor.Vehicles[afv.Vehicle!.DefinitionId!].Turreted && shared![0] < shared[1];
                var (made, missing) = AfvAttack(shot, gun, reference, afv.Vehicle, picked.Contains(afv), turret, shared!);
                if (missing is not null)
                {
                    return Refused([Prefix + "roll-missing:" + missing]);
                }

                afvs.Add(made!);
            }

            // B23.32 (pass 60, ruling R60.11): one normal attack for each Location hit, on the same DR.
            var order = levels?.Select(item => item.LocationId).ToArray() ?? [shot.TargetLocationId!];
            foreach (var place in order)
            {
                var others = judged.Where(item => item.Hit && !picked.Contains(item.Item) && (levels is null || item.Item.LocationId == place)).Select(item => item.Item).ToArray();
                var units = others.Where(item => item.Target is not null).Select(item => item.Target!).ToArray();
                FireVehicle[] struck = [.. judged.Where(item => item.Hit && item.Item.Vehicle is { } vehicle && (levels is null || item.Item.LocationId == place)
                    && (Armored(shot, vehicle, reference) ? afvs.Single(made => made.VehicleId == item.Item.Id).Result is OrdnanceKill.None or OrdnanceKill.Immobilized
                        : !picked.Contains(item.Item)))
                    .Select(item => item.Item.Vehicle!)];
                if (units.Length == 0 && struck.Length == 0)
                {
                    continue;
                }

                // Pass 60: the Fire package's rolls of one Location's attack, when several are attacked.
                // A9.74 (ruling R60.17): with the dr of each weapon those units hold.
                // Pass 65 (ruling R65.7): with the rolls of the PRC the struck vehicles carry.
                var hitRolls = levels is null ? rolls.Hit : Filtered(rolls.Hit, [.. units.Select(item => item.UnitId!), .. struck.Select(item => item.VehicleId!),
                    .. units.SelectMany(item => item.HeldWeapons ?? []).Select(item => item.EquipmentId).OfType<string>(),
                    .. struck.SelectMany(item => item.Prc ?? []).Select(item => item.UnitId).OfType<string>()]);
                if (shared is not null)
                {
                    hitRolls = (hitRolls ?? new FireRolls(null, null, null, null)) with
                    {
                        Attack = shared
                    };
                }

                ReadOwnEffects(hitRolls?.Attack);
                var attack = ScenarioA1FireCalculator.Resolve(LocationAttack(shot, gun, reference, false, place, units, struck, hitRolls), reference.Fire);
                if (Pending(attack, "hit:") is { } pending)
                {
                    return pending;
                }

                shared ??= attack.Arithmetic?.Dice;
                if (normal is null)
                {
                    normal = attack;
                }
                else
                {
                    levelHits.Add(attack);
                }
            }

            // C11.4, C3.71 (p. 182; pass 60, ruling R60.14): a Critical Hit destroys the Gun in the hex; a KIA before the gunshield destroys it, a K
            // malfunctions it.
            if (hit.GunTarget is { CrewUnitId: { } gunCrew } && hitIds.Contains(gunCrew))
            {
                var crewAttack = new[] { normal }.Concat(levelHits).FirstOrDefault(item => item?.Effects.Any(effect => effect.UnitId == gunCrew) == true);
                var result = crewAttack?.Arithmetic?.Result ?? string.Empty;
                fate = picked.Any(item => item.Id == gunCrew) ? "destroyed"
                    : crewAttack?.Arithmetic?.Drm.Any(item => item.Name.StartsWith("gunshield:", StringComparison.Ordinal)) == true ? null
                    : Regex.IsMatch(result, "^([1-7])?KIA$") ? "destroyed"
                    : Regex.IsMatch(result, "^K/([1-4])$") ? "malfunctioned"
                    : null;
            }
        }

        // C6.521, C6.57, C9.2 (referee, pass 9): the mortar acquires the target hex unless it malfunctions, concealed occupants or not, one step more
        // if already acquired.
        var breakdown = breakdownOutcome?.Numbers.BNumber ?? ScenarioA1OrdnanceCalculator.Breakdown(shot, gun, reference);
        var failed = breakdownOutcome?.Failed ?? original >= breakdown;
        var malfunctioned = breakdownOutcome is null ? failed : breakdownOutcome.Outcome == VehicleBreakdownOutcome.Malfunction;
        var (kept, counter) = ScenarioA1OrdnanceCalculator.Counter(shot, failed, rof, dice[0]);
        var acquires = !failed;
        var gained = !acquires ? 0 : Math.Max(acquisition - 1, -2);
        // A.11 (W-A21.13): under a penalty multiple the Original B# is the X#, and a DR at or above it eliminates the Gun.
        var eliminated = breakdownOutcome is null ? ScenarioA1OrdnanceCalculator.Eliminated(shot, gun, original, failed) : null;
        var gunEffect = new OrdnanceGunEffect(breakdown, malfunctioned && eliminated != true, Math.Max(rof, 0), kept, counter, gained, acquires ? shot.TargetLocationId : null)
        {
            VehicleBreakdown = breakdownOutcome,
            Eliminated = eliminated,
        };
        return new OrdnanceResolution(OrdnanceResolution.Resolved, [], toHit, gunEffect)
        {
            CrewConcealmentLost = crew.Concealed == true && shot.CrewSeen != false ? true : null,
            Hit = normal,
            CriticalHit = criticalHit,
            CriticalTarget = criticalTarget,
            GunTargetFate = fate,
            AreaTargets = [.. judged.Select(item => new OrdnanceAreaTarget(item.Item.Id, item.Item.Drm, item.Final, item.Hit))],
            LevelHits = levelHits.Count > 0 ? levelHits : null,
            AreaVehicles = afvs.Count > 0 ? afvs : null,
        };
    }

    /// <summary>
    /// The IFT attacks a mortar's hit would make (pass 60, rulings R60.10, R60.11, R60.24), for the precheck: one on each Location of its hex, in the
    /// firer's LOS or not, with its units and vehicles.
    /// </summary>
    internal static IEnumerable<FireAttack> PrecheckAttacks(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, bool critical)
    {
        var order = shot.AreaLocations?.Select(item => item.LocationId).ToArray() ?? [shot.TargetLocationId!];
        foreach (var place in order)
        {
            var targets = shot.Hit!.Targets!.Where(item => shot.AreaLocations is null || item.LocationId == place).ToArray();
            var vehicles = (shot.Hit.Vehicles ?? []).Where(item => shot.AreaLocations is null || item.LocationId == place).ToArray();
            if (targets.Length > 0 || vehicles.Length > 0 || shot.AreaLocations is null)
            {
                yield return LocationAttack(shot, gun, reference, critical, place, targets, vehicles, null);
            }
        }
    }

    /// <summary>One judged target: a unit or a vehicle, its Location, and its own To Hit DRM.</summary>
    private sealed record Judged(string Id, string? LocationId, List<FireModifier> Drm, FireTarget? Target, FireVehicle? Vehicle);

    /// <summary>The recorded facts of one Location of a mortar's building hex (B23.32), or null when the shot names one Location.</summary>
    private static OrdnanceAreaLocation? Location(OrdnanceShot shot, string? locationId) => shot.AreaLocations?.FirstOrDefault(item => item.LocationId == locationId);

    /// <summary>
    /// Whether a vehicle of the hex is an AFV the hit resolves on the IFT itself (C1.55): not unarmored, and not an OT AFV in woods, which Air Bursts
    /// make unarmored (D5.311).
    /// </summary>
    private static bool Armored(OrdnanceShot shot, FireVehicle vehicle, ScenarioA1OrdnanceReference reference) =>
        reference.Fire.Definitions.GetValueOrDefault(vehicle.DefinitionId ?? string.Empty) is { } definition && definition.Unarmored != true
            && !ScenarioA1FireCalculator.AirBurstUnarmored(true, Location(shot, vehicle.LocationId)?.TargetTerrain ?? shot.Hit!.TargetTerrain, definition);

    /// <summary>
    /// The IFT attack of a hit on one Location of the hex (C3.33, B23.32; pass 60, ruling R60.11): the shot's hit attack, with that Location's terrain,
    /// LOS, building levels above it, and companions when the shot names several.
    /// </summary>
    private static FireAttack LocationAttack(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, bool critical, string? locationId,
        IReadOnlyList<FireTarget> targets, IReadOnlyList<FireVehicle> vehicles, FireRolls? rolls)
    {
        var attack = ScenarioA1OrdnanceCalculator.HitAttack(shot, gun, reference, critical, targets, rolls, vehicles);
        if (Location(shot, locationId) is not { } level)
        {
            return attack;
        }

        var others = shot.Hit!.Targets!.Where(item => item.LocationId == locationId && !targets.Contains(item)).ToArray();
        return attack with
        {
            TargetLocationId = locationId,
            TargetTerrain = level.TargetTerrain,

            // C3.33 (ruling R60.24): a Location out of the firer's LOS is attacked by the hit on the same Effects DR, with no LOS of its own to read.
            Los = level.Los is { Blocked: true } blocked
                ? blocked with
                {
                    Blocked = false,
                    HindranceDrm = 0,
                }
                : level.Los,
            LevelsAbove = level.LevelsAbove is > 0 ? level.LevelsAbove : null,
            Companions = others.Length > 0 ? [.. others, .. shot.Hit.Companions ?? []] : shot.Hit.Companions,
            AfvCover = locationId == shot.TargetLocationId ? attack.AfvCover : null,
            GunTarget = locationId == shot.TargetLocationId ? attack.GunTarget : null,

            // B23.741 (the B23.74 claim): the hit's Factory TEM describes its own target Location; another Location (a rooftop, B23.81) keeps its key's.
            FactoryTem = locationId == shot.TargetLocationId ? attack.FactoryTem : null,
        };
    }

    /// <summary>The Fire package's rolls kept for the given units and vehicles alone, and the attack's DR.</summary>
    private static FireRolls? Filtered(FireRolls? rolls, IReadOnlyCollection<string> ids)
    {
        if (rolls is null)
        {
            return null;
        }

        Dictionary<string, T>? Keep<T>(IReadOnlyDictionary<string, T>? items) =>
            items?.Where(item => item.Key.Split(',').All(ids.Contains)).ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) is { Count: > 0 } kept ? kept : null;
        return new FireRolls(rolls.Attack, Keep(rolls.RandomSelection), Keep(rolls.Checks), Keep(rolls.LeaderLoss), Keep(rolls.WoundSeverity))
        {
            HeatOfBattle = Keep(rolls.HeatOfBattle),
            BerserkChecks = Keep(rolls.BerserkChecks),
            CrewChecks = Keep(rolls.CrewChecks),
            UnlikelyKill = Keep(rolls.UnlikelyKill),
            WeaponDestruction = Keep(rolls.WeaponDestruction),
            PrcSurvival = Keep(rolls.PrcSurvival),
            VehicleSurvival = Keep(rolls.VehicleSurvival),
        };
    }

    /// <summary>
    /// A mortar's hit on an AFV (C1.55, C3.72, C9.1, p. 165; pass 60, ruling R60.10): half the HE FP, or the full FP doubled by a Critical Hit; -1 if every
    /// AF is 4 or less, -1 if OT, +1 if every AF is 8 or more, and the TEM that gives no HD, none on a Critical Hit and none in woods, where the Air
    /// Bursts spare an armored target (B13.3); HD is not built. A KIA destroys it, burning at most half the K/# DR, its crew surviving on the CS# (D5.6);
    /// the K/# DR and the one above it Shock a turret hit or immobilize a hull hit. A MC or PTC does nothing to it. The missing roll's key otherwise.
    /// </summary>
    private static (OrdnanceAreaVehicle? Made, string? Missing) AfvAttack(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, FireVehicle vehicle,
        bool critical, bool turret, IReadOnlyList<int> dice)
    {
        var armor = reference.Armor.Vehicles[vehicle.DefinitionId!];
        var firepower = critical ? ScenarioA1MortarCalculator.CriticalFirepower(reference.HeFirepower(gun.Caliber)) * 2 : ScenarioA1OrdnanceCalculator.AreaFirepower(shot, gun, reference);
        var column = Array.IndexOf(ScenarioA1FireReference.ColumnFp, firepower);
        var drm = new List<FireModifier>();
        int[] factors = [.. (armor.Turreted ? HullAndTurret : Hull).SelectMany(place => Facings.Select(facing => ScenarioA1ArmorReference.ArmorFactor(armor, place, facing))).OfType<int>()];
        if (factors.Length > 0 && factors.All(af => af <= 4))
        {
            drm.Add(new FireModifier("all-af-4-or-less", -1, "C1.55"));
        }

        if (armor.OpenTopped)
        {
            drm.Add(new FireModifier("open-topped", -1, "C1.55"));
        }

        if (factors.Length > 0 && factors.All(af => af >= 8))
        {
            drm.Add(new FireModifier("all-af-8-or-more", 1, "C1.55"));
        }

        // B23.741 (the B23.74 claim, ruling RB23F.5): the hit's Factory TEM describes only its own target Location, named among the shot's Locations
        // or read from the hit's terrain when the shot names none for the vehicle; another Location keeps its key's TEM.
        var other = Location(shot, vehicle.LocationId);
        var terrain = other?.TargetTerrain ?? shot.Hit!.TargetTerrain!;
        var factoryTem = other is null || vehicle.LocationId == shot.TargetLocationId ? shot.Hit!.FactoryTem : null;
        if (!critical && terrain != "woods" && (factoryTem ?? ScenarioA1FireReference.Tem.GetValueOrDefault(terrain)) is var tem and not 0)
        {
            drm.Add(new FireModifier("tem:" + (factoryTem is null ? terrain : "factory"), tem, "C3.331"));
        }

        var final = dice[0] + dice[1] + (int)drm.Sum(item => item.Value);
        var killDr = reference.Fire.KillDr(column);
        var cell = reference.Fire.Result(final, column);
        // D5.7, C7.7 note A (pass 65, ruling R65.21): a red CS# burns on a Final DR one lower, for the burn alone.
        var result = Regex.IsMatch(cell, "^([1-9])?KIA$")
                ? final * 2 <= killDr || ScenarioA1CrewSurvivalRules.BurnsRed(armor.CsRed, final, lower => lower * 2 <= killDr) ? OrdnanceKill.Burn : OrdnanceKill.Eliminated
            : Regex.IsMatch(cell, "^K/([1-9])$") || final == killDr + 1 ? turret ? OrdnanceKill.Shock : OrdnanceKill.Immobilized
            : OrdnanceKill.None;
        var made = new OrdnanceAreaVehicle(vehicle.VehicleId!, armor.Id, turret ? "turret" : "hull", firepower, [.. dice], drm, final, killDr, result)
        {
            Critical = critical ? true : null,
        };

        // Pass 65 (ruling R65.21): the crew's Shock and the vehicle's Abandonment count, and the TC and the PRC's Survival are rolled, only for a hit whose
        // record carries the vehicle's PRC, as every shot planned since the pass does; an older record resolves as it did.
        var fact = vehicle.Prc is not null;
        var shocked = fact && vehicle.Shocked == true;
        var abandoned = fact && vehicle.Abandoned == true;

        // D5.5 (p. 204), C1.55: a hull hit that immobilizes the AFV makes its crew, neither Stunned nor Shocked and still inside, take a TC; failure
        // Abandons it.
        if (fact && result == OrdnanceKill.Immobilized && vehicle.Immobilized != true && vehicle.Stunned != true && !shocked && !abandoned)
        {
            if (shot.Rolls!.AreaCrewCheck?.GetValueOrDefault(vehicle.VehicleId!) is not { } check)
            {
                return (null, "areaCrewCheck:" + vehicle.VehicleId);
            }

            var tc = ScenarioA1CrewSurvivalRules.TaskCheck(check, vehicle.CrewMorale ?? reference.Fire.AfvCrewMorale(armor.Nationality)!.Value, vehicle.StunRecovery == true,
                vehicle.CrewMorale);
            made = made with
            {
                CrewCheck = tc,
                Abandoned = tc.Passed ? null : true,
            };
        }

        // D5.6: a Final DR at most the CS#, +1 if the crew was Stunned, Shocked, or under a "+1" counter, places a crew beneath the wreck; an
        // Abandoned AFV has no crew to survive.
        if (result == OrdnanceKill.Eliminated && ScenarioA1CrewSurvivalRules.CrewSurvival(armor, abandoned) is { } cs)
        {
            if (shot.Rolls!.AreaSurvival?.GetValueOrDefault(vehicle.VehicleId!) is not { } survival)
            {
                return (null, "areaSurvival:" + vehicle.VehicleId);
            }

            made = made with
            {
                CrewSurvival = ScenarioA1CrewSurvivalRules.Survival(cs, survival,
                    ScenarioA1CrewSurvivalRules.SurvivalDrm(false, vehicle.Stunned == true, shocked, vehicle.StunRecovery == true, vehicle.Prc is not null))
            };
        }

        // D9.2, D6.9 (p. 209, 207): an attack that destroys but does not burn the AFV rolls each PRC's Survival instead of attacking it.
        if (result == OrdnanceKill.Eliminated)
        {
            var (prcSurvival, missing) = ScenarioA1FireCalculator.PrcSurvivals(vehicle.Prc, armor.CrewSurvival, shot.Rolls!.Prc?.PrcSurvival);
            if (missing is not null)
            {
                return (null, "prc:" + missing);
            }

            made = made with
            {
                PrcSurvival = prcSurvival
            };
        }

        return (made, null);
    }

    private static OrdnanceResolution Refused(IReadOnlyList<string> reasons) => new(OrdnanceResolution.Indeterminate, reasons, null, null);

    /// <summary>A nested IFT resolution that stopped: its missing roll or option asked for under the prefix, or its reasons.</summary>
    private static OrdnanceResolution? Pending(FireResolution resolution, string prefix)
    {
        if (resolution.Disposition == FireResolution.Resolved)
        {
            return null;
        }

        if (resolution.Reasons is [{ } choice] && choice.StartsWith("asl.a1.fire.choice-missing:", StringComparison.Ordinal))
        {
            return Refused([Prefix + "choice-missing:" + choice["asl.a1.fire.choice-missing:".Length..]]);
        }

        return resolution.Reasons is [{ } reason] && reason.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal)
            ? Refused([Prefix + "roll-missing:" + prefix + reason["asl.a1.fire.roll-missing:".Length..]])
            : new OrdnanceResolution(resolution.Disposition == FireResolution.Abstained ? OrdnanceResolution.Abstained : OrdnanceResolution.Indeterminate,
                [.. resolution.Reasons.Select(item => Prefix + prefix + item)], null, null);
    }
}
