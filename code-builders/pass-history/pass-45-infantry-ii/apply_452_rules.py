"""Pass 45, tasks 45.2 and 45.3, the Rules core: Cowering's marks (A7.9), and the Residual FP an attack leaves (A8.2, A8.221, A8.23, A8.26).
Every anchor is checked before anything is written."""
import sys

RULES = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.Rules/"
EDITS = {
    "ScenarioA1FireModels.cs": [
        ("""    /// <summary>One dr per FPF firer, when a Casualty MC falls on two or more of them (A8.31).</summary>
""",
         """    /// <summary>One dr per firer that can cower, when a fire group of two or more of them cowers (A7.9; pass 45): the highest is marked, ties with it.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? CowerSelection
    {
        get; init;
    }

    /// <summary>One dr per FPF firer, when a Casualty MC falls on two or more of them (A8.31).</summary>
"""),
        ("""    /// <summary>What the attack did to each MG used in it; null when none was.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireWeaponEffect>? WeaponEffects
""",
         """    /// <summary>
    /// The units that cowered (A7.9, p. 58; pass 45): "any unit that cowers (as well as all of its SW regardless of whether it was using its inherent
    /// FP) is automatically marked with a Final or Prep Fire counter", one or more of a fire group by Random Selection. Null when none did.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? CoweredUnitIds
    {
        get; init;
    }

    /// <summary>What the attack did to each MG used in it; null when none was.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireWeaponEffect>? WeaponEffects
"""),
        ("""    /// <summary>The Residual FP the attack leaves in the target Location (A8.2, A8.26, A7.372); null when it leaves none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResidualFp
    {
        get; init;
    }
""",
         """    /// <summary>The Residual FP the attack leaves in the target Location (A8.2, A8.26, A7.372); null when it leaves none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResidualFp
    {
        get; init;
    }

    /// <summary>
    /// The Residual FP the attack would leave if the weapons that kept their Multiple ROF gave it up (A8.23, p. 60; pass 45): "The decision to
    /// forfeit possible remaining First Fire capability in order to leave Residual FP must be made immediately after resolving its attack." Given
    /// only when it is more than <see cref="ResidualFp"/>, which counts the other firers alone.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResidualFpIfForfeited
    {
        get; init;
    }
"""),
    ],
    "ScenarioA1FireCalculator.cs": [
        ("""        || rolls.FirerSelection?.Values.Any(dr => dr is < 1 or > 6) == true
""",
         """        || rolls.FirerSelection?.Values.Any(dr => dr is < 1 or > 6) == true
        || rolls.CowerSelection?.Values.Any(dr => dr is < 1 or > 6) == true
"""),
        ("""                .Concat(rolls.FirerSelection?.Keys.Select(id => "firerSelection:" + id) ?? [])
""",
         """                .Concat(rolls.FirerSelection?.Keys.Select(id => "firerSelection:" + id) ?? [])
                .Concat(rolls.CowerSelection?.Keys.Select(id => "cowerSelection:" + id) ?? [])
"""),
        # Cowering: who can cower, and the double shift.
        ("""            var cowered = !residual && hit is null && attack.VehicleFire is null && attack.Overrun is null && attack.DemolitionCharge is null && dice[0] == dice[1] && !directed
                && attack.Firers!.Any(item => !reference.Definitions[item.DefinitionId!].IsHero && !reference.Definitions[item.DefinitionId!].IsLeader && item.Fanatic != true
                    && !NeverCowers(reference.Definitions[item.DefinitionId!]))
                && attack.FireLane != true;
            var inexperienced = (attack.Firers ?? []).Any(item => reference.Definitions[item.DefinitionId!].Class is "green" or "conscript");
""",
         """            var cowered = !residual && hit is null && attack.VehicleFire is null && attack.Overrun is null && attack.DemolitionCharge is null && dice[0] == dice[1] && !directed
                && attack.Firers!.Any(CanCower)
                && attack.FireLane != true;

            // A7.9 (p. 58; pass 45): "doubled ... for an attack by Inexperienced (19.33) Personnel (even in conjunction with other troops)". A
            // Conscript is always Inexperienced; a Green unit is so by its fact (A19.3), and by its class where a record gives none.
            var inexperienced = (attack.Firers ?? []).Any(item => reference.Definitions[item.DefinitionId!].Class == "conscript"
                || (item.Inexperienced ?? reference.Definitions[item.DefinitionId!].Class == "green"));
"""),
        ("""        /// <summary>A25.45, A25.7 (referee, pass 15): British elite and 1st Line units, and Finns but their Conscripts, are immune to Cowering.</summary>
""",
         """        /// <summary>
        /// Whether a firer is subject to Cowering (A7.9, p. 58): "Cowering affects all fire except that from a SMC, berserk or Fanatic unit ...
        /// British Elite and First Line units, Finns". Pass 45 adds the berserk firer.
        /// </summary>
        private bool CanCower(FireFirer firer) => !reference.Definitions[firer.DefinitionId!].IsHero && !reference.Definitions[firer.DefinitionId!].IsLeader
            && firer.Fanatic != true && firer.Berserk != true && !NeverCowers(reference.Definitions[firer.DefinitionId!]);

        /// <summary>
        /// The units a cowering attack marks (A7.9; pass 45): its one firer that can cower, or of a fire group "Random Selection is used to determine
        /// the unit(s) (and its SW) that becomes marked with a Prep or Final Fire counter": the highest dr, ties with it. Empty when the attack did
        /// not cower; false when a selection dr is missing.
        /// </summary>
        private bool DecideCowering(FireArithmetic arithmetic)
        {
            cowering.Clear();
            if (!arithmetic.Cowered || attack.SprayShare == true)
            {
                return true;
            }

            var able = (attack.Firers ?? []).Where(CanCower).Select(item => item.UnitId!).ToArray();
            if (able.Length <= 1)
            {
                cowering.UnionWith(able);
                return true;
            }

            var drs = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var id in able)
            {
                if (attack.Rolls!.CowerSelection?.TryGetValue(id, out var dr) != true)
                {
                    undecided.Add("asl.a1.fire.roll-missing:cowerSelection:" + string.Join(",", able));
                    return false;
                }

                drs[id] = dr;
                usedRolls.Add("cowerSelection:" + id);
            }

            var highest = drs.Values.Max();
            cowering.UnionWith(drs.Where(item => item.Value == highest).Select(item => item.Key));
            return true;
        }

        /// <summary>
        /// The Residual FP once the weapons' fates are known (A8.221, A8.23; pass 45): none for a weapon that malfunctioned on this DR, and none for
        /// one that kept its Multiple ROF, unless its owner gives the ROF up, which <see cref="FireArithmetic.ResidualFpIfForfeited"/> says.
        /// </summary>
        private FireArithmetic WithWeaponResidual(FireArithmetic arithmetic, IReadOnlyList<FireWeaponEffect>? weapons)
        {
            if (weapons is null || arithmetic.ResidualFp is null && !weapons.Any(item => item.Malfunctioned || item.RateOfFireRetained))
            {
                return arithmetic;
            }

            decimal Fp(Func<FireWeaponEffect, bool> which) => arithmetic.Firers.Where(item => item.VsConcealed != true && weapons.Any(weapon => weapon.EquipmentId == item.UnitId && which(weapon)))
                .Sum(item => item.Firepower);
            var broken = Fp(weapon => weapon.Malfunctioned);
            var kept = Fp(weapon => !weapon.Malfunctioned && weapon.RateOfFireRetained);
            if (broken == 0 && kept == 0)
            {
                return arithmetic;
            }

            var left = Residual(arithmetic, residualHindrance, residualLeadership, broken + kept);
            var forfeited = kept == 0 ? left : Residual(arithmetic, residualHindrance, residualLeadership, broken);
            return arithmetic with
            {
                ResidualFp = left,
                ResidualFpIfForfeited = forfeited is { } more && more > (left ?? 0) ? more : null,
            };
        }

        /// <summary>A25.45, A25.7 (referee, pass 15): British elite and 1st Line units, and Finns but their Conscripts, are immune to Cowering.</summary>
"""),
        # Own counter: a cowering unit.
        ("""        private string? OwnCounter(FireFirer firer) =>
            attack.FireKind is SubsequentFirstFire or FinalProtectiveFire && !UsesSubsequentFire(attack, firer) ? "first-fire" : FireCounter();
""",
         """        private string? OwnCounter(FireFirer firer) =>
            // A7.9 (pass 45): a unit that cowers "is automatically marked with a Final or Prep Fire counter as appropriate", whatever its fire.
            cowering.Contains(firer.UnitId!) && FireCounter() is not null ? attack.Phase is "PFPh" or "AFPh" ? "prep-fire" : "final-fire"
            : attack.FireKind is SubsequentFirstFire or FinalProtectiveFire && !UsesSubsequentFire(attack, firer) ? "first-fire" : FireCounter();
"""),
        # The weapons of a cowering unit keep no ROF.
        ("""                var retained = !malfunctioned.Contains(id) && !sustained && !(attack.PinnedMgAreaFire == true && operatorUnit?.Pinned == true)
""",
         """                // A7.9 (pass 45): a cowering unit's SW are marked with it, so none keeps its Multiple ROF.
                var retained = !malfunctioned.Contains(id) && !sustained && !(attack.PinnedMgAreaFire == true && operatorUnit?.Pinned == true)
                    && !(operatorUnit is not null && cowering.Contains(operatorUnit.UnitId!))
"""),
        # Residual: the reductions of A8.26, the cap after them, and the withheld FP.
        ("""        private int? Residual(FireArithmetic arithmetic, int hindrance, int leadership)
        {
""",
         """        private int? Residual(FireArithmetic arithmetic, int hindrance, int leadership, decimal withheld = 0m)
        {
            residualHindrance = hindrance;
            residualLeadership = leadership;
"""),
        ("""            var highest = atr == 0 ? Math.Max(arithmetic.ColumnFp ?? 0, arithmetic.Concealed?.ColumnFp ?? 0)
                : ScenarioA1FireReference.ColumnFp.Where(fp => fp <= arithmetic.TotalFirepower - atr).DefaultIfEmpty(0).Max();
            var index = Array.FindLastIndex(ResidualCounters, fp => fp <= highest / 2m);
            // B9.31 (ruling R10.5): a wall or hedge TEM claimed against the attack lowers the Residual FP left as a Hindrance does.
            index -= hindrance + Math.Max(leadership, 0) + (attack.HexsideTem?.Tem ?? 0)
                // E3.62, E3.731 (rulings R16.12, R16.13): an HE attack cushioned by Mud or Deep Snow in Open Ground leaves one counter less.
                + (attack.OrdnanceHit is not null && attack.CushionedOpenGround == true && attack.TargetTerrain == "open-ground" ? 1 : 0);
            return index < 0 ? null : ResidualCounters[index];
        }
""",
         """            // A8.221, A8.23 (pass 45): the FP of a weapon that malfunctioned, or kept its Multiple ROF, is withheld; the rest's column is taken,
            // lowered by the attack's Cowering shift as the attack's own column was (A8.224).
            var apart = atr + withheld;
            var columns = ScenarioA1FireReference.ColumnFp;
            var rest = Array.FindLastIndex(columns, fp => fp <= arithmetic.TotalFirepower - apart) - (withheld == 0 ? 0 : arithmetic.ColumnShift);
            var highest = apart == 0 ? Math.Max(arithmetic.ColumnFp ?? 0, arithmetic.Concealed?.ColumnFp ?? 0) : rest < 0 ? 0 : columns[rest];

            // A8.2 (p. 60; pass 45): "half (up to a maximum of 12 after adjustment as per 8.26; FRD) of the highest IFT FP column used". The half
            // is taken down to an IFT column, lowered a column for each +1 of A8.26, and only then held to 12: a 36 FP attack with one +1 leaves 12.
            var index = Array.FindLastIndex(columns, fp => fp <= highest / 2m);

            // A8.26 (p. 60): "each positive DRM caused solely by conditions outside (including CX, BU, Stun, leadership modifier, or hexside/bridge
            // TEM ... but excluding Height Advantage and LV DRM) the target hex". B9.31 (ruling R10.5): a wall or hedge TEM claimed against the
            // attack lowers it as a Hindrance does. Pass 45 adds the firers' own DRM: CX, an Encircled firer, a hero's, a Stun, and overstacking.
            var firers = (int)arithmetic.Drm.Where(item => item.Value > 0 && (item.Name.StartsWith("cx:", StringComparison.Ordinal)
                || item.Name.StartsWith("encircled:", StringComparison.Ordinal) || item.Name.StartsWith("hero-mg:", StringComparison.Ordinal)
                || item.Name.StartsWith("stun-recovery:", StringComparison.Ordinal) || item.Name == "overstacked")).Sum(item => item.Value);
            index -= hindrance + Math.Max(leadership, 0) + (attack.HexsideTem?.Tem ?? 0) + firers
                // E3.62, E3.731 (rulings R16.12, R16.13): an HE attack cushioned by Mud or Deep Snow in Open Ground leaves one counter less.
                + (attack.OrdnanceHit is not null && attack.CushionedOpenGround == true && attack.TargetTerrain == "open-ground" ? 1 : 0);
            return index < 0 ? null : Math.Min(columns[index], 12);
        }
"""),
        # The main flow: decide the cowering units before the weapons, and the weapons' Residual after.
        ("""            var weapons = attack.VehicleFire is { } vehicleFire ? [VehicleWeaponEffect(vehicleFire, arithmetic)] : attack.SprayShare == true ? null : WeaponEffects(arithmetic);
            var overrunEffect = attack.Overrun is { } overrunning ? OverrunEffect(overrunning, arithmetic) : null;
""",
         """            // A7.9 (pass 45): who cowered, before the weapons' ROF and counters are read; then the Residual FP the weapons leave (A8.221, A8.23).
            if (!DecideCowering(arithmetic))
            {
                return Refused(FireResolution.Indeterminate, undecided.Distinct().ToArray());
            }

            var weapons = attack.VehicleFire is { } vehicleFire ? [VehicleWeaponEffect(vehicleFire, arithmetic)] : attack.SprayShare == true ? null : WeaponEffects(arithmetic);
            if (attack.VehicleFire is null && weapons is not null)
            {
                arithmetic = WithWeaponResidual(arithmetic, weapons);
            }

            var overrunEffect = attack.Overrun is { } overrunning ? OverrunEffect(overrunning, arithmetic) : null;
"""),
        ("""                FirerCounters = FirerCounters(),
                WeaponEffects = weapons,
""",
         """                FirerCounters = FirerCounters(),
                CoweredUnitIds = cowering.Count == 0 ? null : [.. (attack.Firers ?? []).Select(item => item.UnitId!).Where(cowering.Contains)],
                WeaponEffects = weapons,
"""),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(RULES + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:80]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(RULES + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
