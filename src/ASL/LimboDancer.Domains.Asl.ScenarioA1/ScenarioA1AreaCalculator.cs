namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// A light mortar's shot on the Area Target Type (backlog pass 9; rulings R9.2 to R9.4): the red Area row of the To Hit Table with the C4
/// modifications, the shot's DRM plus each unit's own, a hit judged for every unit in the target hex, friendly ones too (C3.33, C3.331), a
/// Critical Hit only on an Original 2 (C9.5, C3.7), and the IFT attacks of the units hit at half the HE FP, a Critical Hit at the full FP
/// doubled (C3.71).
/// </summary>
internal static class ScenarioA1AreaCalculator
{
    private const string Prefix = "asl.a1.ordnance.";

    public static OrdnanceResolution Run(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference)
    {
        var rolls = shot.Rolls!;
        var hit = shot.Hit!;
        var range = shot.Range!.Value;
        var crew = shot.Crew!;

        // C3.33, C4: the Area row's red Basic TH#, for every nationality, and the Gun and Ammo modifications.
        var basic = reference.AreaToHit(range);
        var modifications = reference.Modifications(gun, range);
        var modified = basic + (int)modifications.Sum(item => item.Value);

        // C3.331: Cases C1 to C4, E, G, L, and Q do not apply; a SW has no Case A (no CA) and no Case F.
        var drm = new List<FireModifier>();
        if (shot.Phase == "AFPh")
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

        // C6.13, C6.14 (ruling R8.1): FFNAM and FFMO as To Hit DRM of Defensive First Fire.
        if (shot.FireKind is not null && shot.Movement is { } movement)
        {
            if (movement.NonAssault == true)
            {
                drm.Add(new FireModifier("case-j3", -1, "C6.13"));
            }

            if (movement.OpenGround == true)
            {
                drm.Add(new FireModifier("case-j4", -1, "C6.14"));
            }
        }

        if (hit.Los!.HindranceDrm is int hindrance && hindrance > 0)
        {
            drm.Add(new FireModifier("case-r", hindrance, "C6.9"));
        }

        // C3.331 (ruling R9.3): each unit's own DRM: Case K for a concealed enemy unit, the Area Target Acquisition for every unit, concealed or
        // not (C6.521, C6.57; referee, pass 9), and the -1 per squad equivalent its side overstacks the Location (A5.131).
        var targets = hit.Targets!;
        var own = targets.Select(target =>
        {
            var list = new List<FireModifier>();
            var enemy = target.Dummy == true || reference.Fire.Definitions.GetValueOrDefault(target.DefinitionId ?? string.Empty)?.Nationality != gun.Nationality;
            var concealed = enemy && (target.Concealed == true || target.Hidden == true || target.Dummy == true);
            if (concealed)
            {
                list.Add(new FireModifier("case-k", 2, "C6.2"));
            }

            if (shot.Acquisition is int acquired && acquired < 0)
            {
                list.Add(new FireModifier("case-n", acquired, "C6.521"));
            }

            if (enemy && shot.TargetOverstack is { } crowded && crowded > 0)
            {
                list.Add(new FireModifier("overstack-target", -crowded, "A5.131"));
            }

            return (Target: target, Drm: list, Concealed: concealed);
        }).ToArray();

        if (rolls.ToHit is not { } dice)
        {
            return Refused([Prefix + "roll-missing:toHit"]);
        }

        var common = (int)drm.Sum(item => item.Value);
        var original = dice[0] + dice[1];
        int Lowest((FireTarget Target, List<FireModifier> Drm, bool Concealed) item) => 2 + common + (int)item.Drm.Sum(modifier => modifier.Value);

        // C3.7, C3.6, C3.331 (referee, pass 9): judged for each unit, an Original 2 is a Critical Hit on a unit more than the lowest Final DR could
        // hit; one only the lowest Final DR hits needs a subsequent dr of 1, and one no Final DR could hit is hit on a subsequent dr of 1 (a Critical
        // Hit) to 3. One subsequent dr serves them all.
        int? subsequent = null;
        if (original == 2 && own.Any(item => Lowest(item) >= modified))
        {
            if (rolls.Subsequent is not { } dr)
            {
                return Refused([Prefix + "roll-missing:subsequent"]);
            }

            subsequent = dr;
        }

        var judged = own.Select(item =>
        {
            var final = original + common + (int)item.Drm.Sum(modifier => modifier.Value);
            var isHit = Lowest(item) <= modified ? final <= modified : original == 2 && subsequent is >= 1 and <= 3;
            return (item.Target, item.Drm, item.Concealed, Final: final, Hit: isHit);
        }).ToArray();
        var hitUnits = judged.Where(item => item.Hit).Select(item => item.Target).ToArray();
        var eligible = judged.Where(item => item.Hit && original == 2 && (Lowest(own.First(unit => unit.Target == item.Target)) < modified || subsequent == 1))
            .Select(item => item.Target).ToArray();
        var critical = eligible.Length > 0;
        var improbable = own.All(item => Lowest(item) > modified);
        var toHit = new OrdnanceToHit("red", basic, modifications, modified, [.. dice], original, drm, original + common, improbable, subsequent,
            hitUnits.Length > 0, critical);

        // C2.28: an Original To Hit DR at or above the B# malfunctions the mortar.
        var malfunctioned = original >= ScenarioA1OrdnanceCalculator.Breakdown(shot, gun, reference);

        // C2.24, C9.31, C9.2: the Multiple ROF, one lower for Spotted fire, none for a lone SMC; C5.4: none when pinned; C5.2: none in the AFPh.
        var rof = gun.RateOfFire ?? 0;
        if (shot.Spotter is not null && rof > 0)
        {
            rof--;
        }

        if (pinned || shot.Phase == "AFPh" || reference.Fire.Definitions.GetValueOrDefault(crew.DefinitionId!)?.Kind is "asl:leader" or "asl:hero")
        {
            rof = 0;
        }

        var (kept, counter) = ScenarioA1OrdnanceCalculator.Counter(shot, malfunctioned, rof, dice[0]);

        FireResolution? normal = null, criticalHit = null;
        string? criticalTarget = null;
        if (hitUnits.Length > 0)
        {
            IReadOnlyList<FireTarget> criticalTargets = [];
            if (critical)
            {
                if (eligible.Length == 1)
                {
                    criticalTargets = eligible;
                }
                else
                {
                    // C3.74: a Critical Hit applies to the unit Random Selection picks among those it could fall on (the highest dr, ties alike).
                    var key = "criticalSelection:" + string.Join(",", eligible.Select(item => item.UnitId));
                    if (rolls.CriticalSelection is not { } drs || eligible.Any(item => !drs.ContainsKey(item.UnitId!)))
                    {
                        return Refused([Prefix + "roll-missing:" + key]);
                    }

                    var highest = eligible.Max(item => drs[item.UnitId!]);
                    criticalTargets = [.. eligible.Where(item => drs[item.UnitId!] == highest)];
                    criticalTarget = string.Join(",", criticalTargets.Select(item => item.UnitId));
                }

                criticalHit = ScenarioA1FireCalculator.Resolve(ScenarioA1OrdnanceCalculator.HitAttack(shot, gun, reference, true, criticalTargets, rolls.CriticalHit), reference.Fire);
                if (Pending(criticalHit, "critical-hit:") is { } pending)
                {
                    return pending;
                }
            }

            // C3.33: every unit hit is attacked with a single Effects DR, so the normal hit reuses the Critical Hit's.
            var others = hitUnits.Where(item => !criticalTargets.Contains(item)).ToArray();
            if (others.Length > 0)
            {
                var hitRolls = criticalHit?.Arithmetic?.Dice is { } shared ? (rolls.Hit ?? new FireRolls(null, null, null, null)) with
                {
                    Attack = shared
                } : rolls.Hit;
                normal = ScenarioA1FireCalculator.Resolve(ScenarioA1OrdnanceCalculator.HitAttack(shot, gun, reference, false, others, hitRolls), reference.Fire);
                if (Pending(normal, "hit:") is { } pending)
                {
                    return pending;
                }
            }
        }

        // C6.521, C6.57, C9.2 (referee, pass 9): the mortar acquires the target hex unless it malfunctions, concealed occupants or not, one step more
        // if already acquired.
        var acquires = !malfunctioned;
        var acquisition = !acquires ? 0 : Math.Max(shot.Acquisition!.Value - 1, -2);
        var gunEffect = new OrdnanceGunEffect(ScenarioA1OrdnanceCalculator.Breakdown(shot, gun, reference), malfunctioned, Math.Max(rof, 0), kept, counter, acquisition, acquires ? shot.TargetLocationId : null);
        return new OrdnanceResolution(OrdnanceResolution.Resolved, [], toHit, gunEffect)
        {
            CrewConcealmentLost = crew.Concealed == true && shot.CrewSeen != false ? true : null,
            Hit = normal,
            CriticalHit = criticalHit,
            CriticalTarget = criticalTarget,
            AreaTargets = [.. judged.Select(item => new OrdnanceAreaTarget(item.Target.UnitId!, item.Drm, item.Final, item.Hit))],
        };
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
