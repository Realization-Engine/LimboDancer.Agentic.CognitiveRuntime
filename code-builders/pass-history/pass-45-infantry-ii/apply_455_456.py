"""Pass 45, tasks 45.5 and 45.6: a Fire Lane's forced cancellation (A9.223); Mandatory Fire Direction (A9.4); a leader directing a MG's later
shots (A7.53); and the Field of Fire (A9.21). Every anchor is checked before anything is written."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireEligibility.cs": [
        ("""        ArgumentNullException.ThrowIfNull(lanes);
        ArgumentNullException.ThrowIfNull(attack);
        return lanes.FirstOrDefault(lane => attack.Firers!.Any(item => item.Weapons?.Any(weapon => weapon.EquipmentId == lane.Weapon) == true
""",
         """        ArgumentNullException.ThrowIfNull(lanes);
        ArgumentNullException.ThrowIfNull(attack);

        // A9.223 (p. 63; pass 45): "A MG may cancel its Fire Lane in order to gain freedom to fire elsewhere (/its manning Infantry may use
        // Subsequent-First-Fire/FPF after establishing the Fire Lane) only if a TPBF ... situation occurs (8.312 ...) in which case the Fire Lane
        // must be cancelled". Fire at the firers' own Location is that situation; the lane goes with the attack's record.
        if (attack.FirerLocationId is not null && attack.FirerLocationId == attack.TargetLocationId)
        {
            return null;
        }

        return lanes.FirstOrDefault(lane => attack.Firers!.Any(item => item.Weapons?.Any(weapon => weapon.EquipmentId == lane.Weapon) == true
"""),
        ("""    /// <summary>A9.12 (referee, pass 12): a leader who directed fire this phase gives up leadership by firing a MG, so fires none; he fires one MG a phase.</summary>
""",
         """    /// <summary>
    /// A9.223 (pass 45): whether a fire record cancels a Fire Lane: an attack at its firers' own Location by the lane's MG or its manning Infantry.
    /// </summary>
    public static bool FireLaneCancelledBy(bool atOwnLocation, bool laneWeaponOrOperatorFired) => atOwnLocation && laneWeaponOrOperatorFired;

    /// <summary>
    /// The Field of Fire (A9.21, p. 62; pass 45): "if a SW MMG/HMG in a woods/rubble/building hex fires and is entitled to another shot, it may
    /// continue to fire during that phase only inside the CA of the prior shot (which ... includes the MG's own Location ...)". A CA is the
    /// 60-degree wedge on a hexspine (C3.2), so the shots of a phase must lie in one such wedge. <paramref name="earlierBearings"/> are the MG's
    /// earlier shots of the phase at other Locations; <paramref name="bearing"/> is null for its own hex. The refusal, or null.
    /// </summary>
    public static string? FieldOfFireBar(string? size, string? terrain, IReadOnlyList<double> earlierBearings, double? bearing, string weapon)
    {
        ArgumentNullException.ThrowIfNull(earlierBearings);
        if (size is not ("medium" or "heavy") || !(terrain is "woods" or "rubble" || terrain?.EndsWith("building", StringComparison.Ordinal) == true)
            || earlierBearings.Count == 0 || bearing is null)
        {
            return null;
        }

        double[] shots = [.. earlierBearings, bearing.Value];
        return Enumerable.Range(0, 6).Any(facing => shots.All(shot => ScenarioA1Geometry.AngleBetween(shot, ScenarioA1Geometry.FacingDegrees(facing)) <= 30.0001))
            ? null
            : $"play.fire-field: {weapon} has fired this phase from woods, rubble, or a building, and fires again only inside the CA of that shot (A9.21)";
    }

    /// <summary>A9.12 (referee, pass 12): a leader who directed fire this phase gives up leadership by firing a MG, so fires none; he fires one MG a phase.</summary>
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireModels.cs": [
        ("""    /// <summary>Whether the directing leader is CX (A4.51): +1 to the attack's IFT DR; null is false.</summary>
""",
         """    /// <summary>
    /// Whether the leader directed, earlier this phase, every MG this attack fires (A7.53, p. 57; pass 45): "a leader can direct the fire of a MG as
    /// many times as the MG can fire, even if he also directed other units as part of a FG in the MG's previous attack". The planner's read of the
    /// phase's records, recorded with the attack; null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DirectedTheseMgs
    {
        get; init;
    }

    /// <summary>Whether the directing leader is CX (A4.51): +1 to the attack's IFT DR; null is false.</summary>
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs": [
        ("""                || (director.DirectedThisPlayerTurn == true && !redirect))
            {
                outside.Add("asl.a1.fire.director-outside");
            }
""",
         """                // A7.53 (pass 45): he directs the later shots of a MG he has directed this phase, when nothing but those MGs fires.
                || (director.DirectedThisPlayerTurn == true && !redirect
                    && !(director.DirectedTheseMgs == true && firers.All(item => item.UsesInherentFp == false && item.Weapons is { Count: > 0 }))))
            {
                outside.Add("asl.a1.fire.director-outside");
            }
"""),
        ("""            var nextLevel = !tpbf && range == 0 && firer.LocationId != attack.TargetLocationId && Math.Abs(LevelAboveOf(attack, firer)) == 1;
""",
         """            var nextLevel = !tpbf && range == 0 && firer.LocationId != attack.TargetLocationId && Math.Abs(LevelAboveOf(attack, firer)) == 1;

            // A9.4 (p. 64; pass 45): "MG fire is limited to a 16 hex maximum range unless an Infantry leader (even a 0 or + DRM leader) is directing
            // that fire". An attack recorded before the pass has no such limit.
            if (attack.CoweringMarksAndWeaponResidual == true && range > 16 && directors.Length == 0
                && (firer.Weapons ?? []).Any(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId!) is { IsMg: true }))
            {
                outside.Add("asl.a1.fire.mg-direction-outside");
            }
"""),
        ("""                    // A9.12 (ruling R12.4): a leader fires a MG alone as Area Fire; two SMC together fire it at full FP.
""",
         """                    // A9.4 (p. 64; pass 45): "MG attacks vs unconcealed Infantry at a range >= 17 hexes treat those Infantry as concealed".
                    if (attack.CoweringMarksAndWeaponResidual == true && mg.IsMg && range >= 17 && !vsConcealed)
                    {
                        multipliers.Add(new FireModifier("mandatory-fire-direction", 0.5m, "A9.4"));
                    }

                    // A9.12 (ruling R12.4): a leader fires a MG alone as Area Fire; two SMC together fire it at full FP.
"""),
    ],
    "LimboDancer.Domains.Asl.Play/RefusalReasons.cs": [
        ("""        ["fire.firers-of-two-sides"] =""",
         """        ["fire.mg-direction-outside"] = "a MG fires beyond 16 hexes only when an Infantry leader directs it (A9.4)",
        ["fire.firers-of-two-sides"] ="""),
    ],
    "LimboDancer.Domains.Asl.Play/LiveFire.cs": [
        ("""            // The owners' answers are declared, and the projector checks them against the choices made (ruling R5.8).
            Choices = recorded.Choices,
""",
         """            // A7.53 (pass 45): whether the leader directed these MGs earlier in the phase is the planner's read of the records, recorded with the attack.
            Director = expected.Director is null || recorded.Director?.DirectedTheseMgs != true ? expected.Director : expected.Director with { DirectedTheseMgs = true },

            // The owners' answers are declared, and the projector checks them against the choices made (ruling R5.8).
            Choices = recorded.Choices,
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs": [
        ("""        // A9.12 (referee, pass 12): a leader who directed fire this phase gives up leadership by firing a MG, so fires none; he fires one MG a phase.
        // Rules decides it (pass 32.c); the record scan and the two state scans are read when asked.
""",
         """        // A9.21 (p. 62; pass 45): a MMG or HMG that has fired this phase from woods, rubble, or a building fires again only inside that shot's CA.
        // The phase's records give its earlier shots; Rules decides from their bearings.
        foreach (var firer in attack.Firers!.Where(item => item.Weapons is { Count: > 0 } && item.LocationId is not null && item.LocationId != target.ToString()))
        {
            if (!BoardLocation.TryParse(firer.LocationId!, out var mgAt) || ReadLocation(state, mgAt!) is not { } mgRead)
            {
                continue;
            }

            foreach (var weapon in firer.Weapons!)
            {
                double[] earlier = [.. ThisPhase(existing).Select(item => item.Payload).OfType<FireResolved>()
                    .Where(record => record.FirerLocation == firer.LocationId && record.TargetLocation != firer.LocationId
                        && record.Resolution.TryGetProperty("weaponEffects", out var effects) && effects.ValueKind == JsonValueKind.Array
                        && effects.EnumerateArray().Any(effect => effect.TryGetProperty("equipmentId", out var id) && id.GetString() == weapon.EquipmentId))
                    .Select(record => BoardLocation.TryParse(record.TargetLocation, out var was) ? Bearing(state, mgAt!, was!) : null).OfType<double>()];
                if (ScenarioA1FireEligibility.FieldOfFireBar(weapon.DefinitionId is { } named ? FireReference.Value.Definitions.GetValueOrDefault(named)?.Size : null,
                    TerrainKey(mgRead), earlier, Bearing(state, mgAt!, target), weapon.EquipmentId!) is { } fieldBar)
                {
                    return Refused(scope, label, expected, fieldBar);
                }
            }
        }

        // A7.53 (p. 57; pass 45): a leader who directed a MG this phase directs its later shots; the phase's records say whether he did.
        if (attack.Director is { UnitId: { } directing } && attack.Firers!.All(item => item.UsesInherentFp == false && item.Weapons is { Count: > 0 }))
        {
            string[] fired = [.. attack.Firers!.SelectMany(item => item.Weapons!).Select(weapon => weapon.EquipmentId!)];
            var directedThem = fired.All(id => ThisPhase(existing).Select(item => item.Payload).OfType<FireResolved>().Any(record => record.Director == directing
                && record.Resolution.TryGetProperty("weaponEffects", out var effects) && effects.ValueKind == JsonValueKind.Array
                && effects.EnumerateArray().Any(effect => effect.TryGetProperty("equipmentId", out var was) && was.GetString() == id)));
            if (directedThem)
            {
                attack = attack with
                {
                    Director = attack.Director with { DirectedTheseMgs = true },
                };
            }
        }

        // A9.12 (referee, pass 12): a leader who directed fire this phase gives up leadership by firing a MG, so fires none; he fires one MG a phase.
        // Rules decides it (pass 32.c); the record scan and the two state scans are read when asked.
"""),
    ],
    "LimboDancer.Domains.Asl.Units/State/GameProjector.cs": [
        ("""            return state with
            {
                FiresThisPhase = [.. state.FiresThisPhase, new FireRecord(eventId, fire.FirerLocation, fire.TargetLocation) { Step = fire.MovementStep, Vehicle = byVehicle }],
                OrdnanceShots = shots,
            };
""",
         """            // A9.223 (pass 45): an attack at its firers' own Location by a lane's MG or its manning Infantry cancels that Fire Lane.
            var weaponsFired = fire.Resolution.TryGetProperty("weaponEffects", out var fired) && fired.ValueKind == JsonValueKind.Array
                ? fired.EnumerateArray().Select(item => item.TryGetProperty("equipmentId", out var id) ? id.GetString() : null).ToArray() : [];
            return state with
            {
                FiresThisPhase = [.. state.FiresThisPhase, new FireRecord(eventId, fire.FirerLocation, fire.TargetLocation) { Step = fire.MovementStep, Vehicle = byVehicle }],
                OrdnanceShots = shots,
                FireLanes = state.FireLanes.Count == 0 ? state.FireLanes
                    : [.. state.FireLanes.Where(lane => !Rules.ScenarioA1FireEligibility.FireLaneCancelledBy(fire.FirerLocation == fire.TargetLocation,
                        fire.Firers.Contains(lane.Operator, StringComparer.Ordinal) || weaponsFired.Contains(lane.Weapon)))],
            };
"""),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:80]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
