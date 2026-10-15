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

    public static OrdnanceResolution Run(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference)
    {
        var rolls = shot.Rolls!;
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

        // C9.31 (ruling R9.4): Spotted fire.
        if (shot.Spotter is not null)
        {
            drm.Add(new FireModifier("spotted", 2, "C9.31"));
        }

        drm.AddRange(ScenarioA1OrdnanceCalculator.Leadership(shot, reference));
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
        // B23.32, C3.4 (pass 60, ruling R60.11): a Location of the hex out of the firer's LOS is not attacked, and each in it adds its own Case R.
        // C3.332, C6.7 (pass 60, ruling R60.10): each vehicle of the hex is judged on its own as well, with its size (Case P); C11.2 (ruling R60.14):
        // a Gun's crew alone with the Gun's.
        var targets = hit.Targets!.Where(item => Location(shot, item.LocationId)?.Los is not { Blocked: true }).ToArray();
        var vehicles = (hit.Vehicles ?? []).Where(item => Location(shot, item.LocationId)?.Los is not { Blocked: true }).ToArray();
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
            var enemy = target.Dummy == true || reference.Fire.Definitions.GetValueOrDefault(target.DefinitionId ?? string.Empty)?.Nationality != gun.Nationality;
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
        var hitIds = judged.Where(item => item.Hit).Select(item => item.Item.Id).ToArray();
        var eligible = judged.Where(item => item.Hit && original == 2 && (Lowest(item.Item) < modified || subsequent == 1)).Select(item => item.Item).ToArray();
        var critical = eligible.Length > 0;
        var improbable = judging.All(item => Lowest(item) > modified);
        var toHit = new OrdnanceToHit("red", basic, modifications, modified, [.. dice], original, drm, original + common, improbable, subsequent,
            hitIds.Length > 0, critical);

        // C2.28: an Original To Hit DR at or above the B# malfunctions the mortar.
        var malfunctioned = original >= ScenarioA1OrdnanceCalculator.Breakdown(shot, gun, reference);

        // C2.24, C9.31, C9.2: the Multiple ROF, one lower for Spotted fire, none for a lone SMC; C5.4: none when pinned; C5.2: none in the AFPh.
        var rof = gun.RateOfFire ?? 0;
        if (shot.Spotter is not null && rof > 0)
        {
            rof--;
        }

        if (pinned || ScenarioA1OrdnanceCalculator.AfphFire(shot) || reference.Fire.Definitions.GetValueOrDefault(crew.DefinitionId!)?.Kind is "asl:leader" or "asl:hero")
        {
            rof = 0;
        }

        var (kept, counter) = ScenarioA1OrdnanceCalculator.Counter(shot, malfunctioned, rof, dice[0]);

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
                var hitRolls = levels is null ? rolls.Hit : Filtered(rolls.Hit, [.. units.Select(item => item.UnitId!), .. struck.Select(item => item.VehicleId!)]);
                if (shared is not null)
                {
                    hitRolls = (hitRolls ?? new FireRolls(null, null, null, null)) with
                    {
                        Attack = shared
                    };
                }

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
        var acquires = !malfunctioned;
        var gained = !acquires ? 0 : Math.Max(acquisition - 1, -2);
        var gunEffect = new OrdnanceGunEffect(ScenarioA1OrdnanceCalculator.Breakdown(shot, gun, reference), malfunctioned, Math.Max(rof, 0), kept, counter, gained, acquires ? shot.TargetLocationId : null);
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
    /// The IFT attacks a mortar's hit would make (pass 60, rulings R60.10, R60.11), for the precheck: one on each Location of its hex in the firer's
    /// LOS, with its units and vehicles.
    /// </summary>
    internal static IEnumerable<FireAttack> PrecheckAttacks(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, bool critical)
    {
        var order = shot.AreaLocations?.Select(item => item.LocationId).ToArray() ?? [shot.TargetLocationId!];
        foreach (var place in order.Where(item => Location(shot, item)?.Los is not { Blocked: true }))
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
            Los = level.Los,
            LevelsAbove = level.LevelsAbove is > 0 ? level.LevelsAbove : null,
            Companions = others.Length > 0 ? [.. others, .. shot.Hit.Companions ?? []] : shot.Hit.Companions,
            AfvCover = locationId == shot.TargetLocationId ? attack.AfvCover : null,
            GunTarget = locationId == shot.TargetLocationId ? attack.GunTarget : null,
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
        var firepower = critical ? reference.HeFirepower(gun.Caliber) * 2 : ScenarioA1OrdnanceCalculator.AreaFirepower(shot, gun, reference);
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

        var terrain = Location(shot, vehicle.LocationId)?.TargetTerrain ?? shot.Hit!.TargetTerrain!;
        if (!critical && terrain != "woods" && ScenarioA1FireReference.Tem.GetValueOrDefault(terrain) is var tem and not 0)
        {
            drm.Add(new FireModifier("tem:" + terrain, tem, "C3.331"));
        }

        var final = dice[0] + dice[1] + (int)drm.Sum(item => item.Value);
        var killDr = reference.Fire.KillDr(column);
        var cell = reference.Fire.Result(final, column);
        var result = Regex.IsMatch(cell, "^([1-9])?KIA$") ? final * 2 <= killDr ? OrdnanceKill.Burn : OrdnanceKill.Eliminated
            : Regex.IsMatch(cell, "^K/([1-9])$") || final == killDr + 1 ? turret ? OrdnanceKill.Shock : OrdnanceKill.Immobilized
            : OrdnanceKill.None;
        var made = new OrdnanceAreaVehicle(vehicle.VehicleId!, armor.Id, turret ? "turret" : "hull", firepower, [.. dice], drm, final, killDr, result)
        {
            Critical = critical ? true : null,
        };

        // D5.6: a Final DR at most the CS#, +1 if the crew was Stunned or under a "+1" counter, places a crew beneath the wreck.
        if (result == OrdnanceKill.Eliminated && armor.CrewSurvival is { } cs && !armor.CrewSurvivalPassengersOnly)
        {
            if (shot.Rolls!.AreaSurvival?.GetValueOrDefault(vehicle.VehicleId!) is not { } survival)
            {
                return (null, "areaSurvival:" + vehicle.VehicleId);
            }

            var plusOne = vehicle.Stunned == true || vehicle.StunRecovery == true ? 1 : 0;
            var survivalDr = survival[0] + survival[1] + plusOne;
            made = made with
            {
                CrewSurvival = new OrdnanceCrewSurvival([.. survival], plusOne, survivalDr, cs, survivalDr <= cs)
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
