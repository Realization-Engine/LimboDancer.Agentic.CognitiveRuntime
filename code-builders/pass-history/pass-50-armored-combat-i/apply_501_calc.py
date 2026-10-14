"""Pass 50, tasks 50.1 and 50.2: the fire calculator's part. Every anchor is checked before the file is written."""
import io

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs"
text = io.open(PATH, encoding="utf-8", newline="").read()
crlf = "\r\n" in text
text = text.replace("\r\n", "\n")
EDITS = []


def edit(old, new):
    EDITS.append((old, new))


edit("""            Need(vehicle.RateOfFireShot, "vehicleFire.rateOfFireShot");
""", """            Need(vehicle.RateOfFireShot, "vehicleFire.rateOfFireShot");
            foreach (var (weapon, index) in (vehicle.Weapons ?? []).Select((item, index) => (item, index)))
            {
                Need(weapon.Weapon, $"vehicleFire.weapons[{index}].weapon");
                Need(weapon.Malfunctioned, $"vehicleFire.weapons[{index}].malfunctioned");
                Need(weapon.Fired, $"vehicleFire.weapons[{index}].fired");
                Need(weapon.Turn, $"vehicleFire.weapons[{index}].turn");
            }

""")

edit("""        var definition = reference.Definitions.GetValueOrDefault(vehicle.DefinitionId!);
        if (definition is not { IsVehicle: true, MainArmament: "aamg", AntiAircraftMg: not null } || attack.Firers is { Count: > 0 } || Directors(attack).Any()
            || vehicle.LocationId != attack.FirerLocationId || vehicle.CrewExposed != true || vehicle.Stunned == true || vehicle.Malfunctioned == true
            || (attack.Phase == "PFPh" && vehicle.InMotion == true))
        {
            outside.Add("asl.a1.fire.vehicle-fire-outside");
        }
""", """        var definition = reference.Definitions.GetValueOrDefault(vehicle.DefinitionId!);

        // D1.81, D1.82, D3.5 (pp. 195, 200; pass 50): the BMG and CMG it names, each printed on the counter, working, and named once. A BMG fires
        // only inside the VCA; a CMG's turret turns for the shot (D3.52), but never in Bounding First Fire (C5.13: "must always fire within its
        // current CA"); the CMG of a CE tank with a RST or 1MT turret does not fire (D1.321, D1.322). Neither needs a CE crew (D1.83).
        var named = vehicle.Weapons is { Count: > 0 } list ? list : null;
        var weaponsOutside = named is null
            ? definition is not { IsVehicle: true, MainArmament: "aamg", AntiAircraftMg: not null } || vehicle.CrewExposed != true || vehicle.Malfunctioned == true
            : definition is not { IsVehicle: true } || named.Select(item => item.Weapon).Distinct(StringComparer.Ordinal).Count() != named.Count
                || named.Any(item => VehicleWeaponFp(definition, item.Weapon) is null || item.Malfunctioned == true
                    || (item.Weapon == FireVehicleWeapon.CoaxialMg && definition.TurretBarredWhileCe(vehicle.CrewExposed))
                    || (item.Turn > 0 && (item.Weapon != FireVehicleWeapon.CoaxialMg || attack.FireKind == BoundingFirstFire)));
        if (weaponsOutside || attack.Firers is { Count: > 0 } || Directors(attack).Any() || vehicle.LocationId != attack.FirerLocationId || vehicle.Stunned == true
            || (attack.Phase == "PFPh" && vehicle.InMotion == true))
        {
            outside.Add("asl.a1.fire.vehicle-fire-outside");
        }
""")

edit("""        if (vehicle.FiredThisPlayerTurn == true && vehicle.RateOfFireShot != true)
        {
            outside.Add("asl.a1.fire.firer-already-fired");
        }
""", """        // D3.5 (pass 50): a BMG or CMG makes one fire attack a Player Turn, in the fire phase the vehicle's other weapons fire in.
        if (named is null ? vehicle.FiredThisPlayerTurn == true && vehicle.RateOfFireShot != true : vehicle.OtherPhase == true || named.Any(item => item.Fired == true))
        {
            outside.Add("asl.a1.fire.firer-already-fired");
        }
""")

edit("""        if (attack.Range is < 1 or > 16)
        {
            outside.Add("asl.a1.fire.out-of-range");
        }

        if (attack.Los!.Blocked == true)
        {
            outside.Add("asl.a1.fire.los-blocked");
        }

        if (attack.Rolls is { } rolls && Malformed(rolls))
        {
            outside.Add("asl.a1.fire.roll-malformed");
        }
    }
""", """        // A7.22: within twice the Normal Range of each MG that fires: eight hexes for a BMG and an AAMG, twelve for a CMG (D1.81 to D1.83).
        if (attack.Range is < 1 || attack.Range > (named is null ? 16 : named.Min(item => 2 * VehicleWeaponRange(item.Weapon))))
        {
            outside.Add("asl.a1.fire.out-of-range");
        }

        if (attack.Los!.Blocked == true)
        {
            outside.Add("asl.a1.fire.los-blocked");
        }

        if (attack.Rolls is { } rolls && Malformed(rolls))
        {
            outside.Add("asl.a1.fire.roll-malformed");
        }
    }

    /// <summary>The printed FP of a vehicle's BMG or CMG (D1.8, p. 195; pass 50); null when the counter prints none or the name is neither.</summary>
    public static int? VehicleWeaponFp(FireDefinition definition, string? weapon)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return weapon switch
        {
            FireVehicleWeapon.BowMg => definition.BowMg,
            FireVehicleWeapon.CoaxialMg => definition.CoaxialMg,
            _ => null,
        };
    }

    /// <summary>The Normal Range of a vehicle's MG (D1.81 to D1.83, p. 195; pass 50): twelve hexes for a CMG, eight for a BMG and an AAMG.</summary>
    public static int VehicleWeaponRange(string? weapon) => weapon == FireVehicleWeapon.CoaxialMg ? 12 : 8;

    /// <summary>
    /// The Case A DRM a vehicle's MG attack adds to its IFT DR (D3.52, D3.51, C5.11, pp. 172 and 201; pass 50): for a CMG whose turret turns, the
    /// To Hit DRM of Case A for the turret's type, doubled in woods, a building, or rubble; for one that fires within the current TCA after
    /// another turret weapon turned it in this fire phase, what that weapon paid; the worst of the weapons that fire together.
    /// </summary>
    public static int VehicleCaseA(FireVehicleFire vehicle, FireDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(definition);
        return (vehicle.Weapons ?? []).Select(item => item.Turn is > 0 and var turn
            ? ScenarioA1ArmorCalculator.CaseA(definition.MaType, turn) * (vehicle.InWoodsOrBuilding == true ? 2 : 1)
            : item.MaintainedCaseA ?? 0).DefaultIfEmpty(0).Max();
    }
""")

edit("""            var weapons = attack.VehicleFire is { } vehicleFire ? [VehicleWeaponEffect(vehicleFire, arithmetic)] : attack.SprayShare == true ? null : WeaponEffects(arithmetic);
""", """            var weapons = attack.VehicleFire is { } vehicleFire ? VehicleWeaponEffects(vehicleFire, arithmetic) : attack.SprayShare == true ? null : WeaponEffects(arithmetic);
""")

edit("""            return new FireWeaponEffect(vehicle.VehicleId!, breakdown, malfunctioned, retained, false, FireCounter(), null);
        }
""", """            return new FireWeaponEffect(vehicle.VehicleId!, breakdown, malfunctioned, retained, false, FireCounter(), null);
        }

        /// <summary>
        /// A vehicle's weapons after its attack (D3.7, A9.71, pp. 65 and 201; pass 50). Its MA MG as before. A BMG and a CMG have no B# of their
        /// own ("a printed B# &lt; 12 applies only to the MA") and no ROF: each malfunctions on an Original 12, and of two that fired together
        /// Random Selection finds the one, or both on a tie (A9.71: "this rule also applies to multiple MG ... attacks by the same AFV"). The
        /// vehicle's fire counter is carried by the first.
        /// </summary>
        private List<FireWeaponEffect> VehicleWeaponEffects(FireVehicleFire vehicle, FireArithmetic arithmetic)
        {
            if (vehicle.Weapons is not { Count: > 0 } named)
            {
                return [VehicleWeaponEffect(vehicle, arithmetic)];
            }

            var id = vehicle.VehicleId!;
            var breakdown = 12 - (attack.BreakdownReduction ?? 0);
            var selection = new Dictionary<string, int>(StringComparer.Ordinal);
            if (arithmetic.OriginalDr >= breakdown && named.Count > 1)
            {
                var keys = named.Select(item => id + ":" + item.Weapon).ToArray();
                foreach (var key in keys)
                {
                    if (attack.Rolls!.WeaponSelection?.TryGetValue(key, out var dr) != true)
                    {
                        undecided.Add("asl.a1.fire.roll-missing:weaponSelection:" + string.Join(",", keys));
                        return [];
                    }

                    selection[key] = dr;
                    usedRolls.Add("weaponSelection:" + key);
                }
            }

            var highest = selection.Count == 0 ? 0 : selection.Values.Max();
            return [.. named.Select((item, index) => new FireWeaponEffect(id, breakdown,
                arithmetic.OriginalDr >= breakdown && (selection.Count == 0 || selection[id + ":" + item.Weapon] == highest), false, false,
                index == 0 ? FireCounter() : null, selection.Count == 0 ? null : selection[id + ":" + item.Weapon])
            {
                VehicleWeapon = item.Weapon,
            })];
        }
""")

edit("""                if (hasKnown)
                {
                    firers.Add(VehicleFirepower(vehicle, false));
                    known = firers[^1].Firepower;
                }

                if (hasConcealed)
                {
                    var concealedFp = VehicleFirepower(vehicle, true);
                    firers.Add(hasKnown ? concealedFp with
                    {
                        VsConcealed = true
                    } : concealedFp);
                    vsConcealed = concealedFp.Firepower;
                }
""", """                // D3.5 (pass 50): the FP of the MGs that fire are added together for the one attack.
                string?[] mgs = vehicle.Weapons is { Count: > 0 } named ? [.. named.Select(item => item.Weapon)] : [null];
                if (hasKnown)
                {
                    firers.AddRange(mgs.Select(mg => VehicleFirepower(vehicle, false, mg)));
                    known = firers.Sum(item => item.Firepower);
                }

                if (hasConcealed)
                {
                    FirerFirepower[] concealedFp = [.. mgs.Select(mg => VehicleFirepower(vehicle, true, mg))];
                    firers.AddRange(concealedFp.Select(item => hasKnown ? item with
                    {
                        VsConcealed = true
                    } : item));
                    vsConcealed = concealedFp.Sum(item => item.Firepower);
                }
""")

edit("""            // D5.34 (p. 203; pass 35, task 35.13 d): and to its OVR DR.
""", """            // D3.52, D3.51 (p. 201; pass 50): a CMG fired outside its TCA adds the To Hit DRM of Case A to the IFT DR, and one fired within a TCA
            // another turret weapon changed in this fire phase pays what that one paid.
            if (attack.VehicleFire is { Weapons.Count: > 0 } turning && VehicleCaseA(turning, reference.Definitions[turning.DefinitionId!]) is > 0 and var caseA)
            {
                drm.Add(new FireModifier("case-a:" + turning.VehicleId, caseA, "D3.52"));
            }

            // D5.34 (p. 203; pass 35, task 35.13 d): and to its OVR DR.
""")

edit("""        private FirerFirepower VehicleFirepower(FireVehicleFire vehicle, bool vsConcealed)
        {
            var definition = reference.Definitions[vehicle.DefinitionId!];
            var range = attack.Range!.Value;
            var multipliers = new List<FireModifier>();
            if (range == 1)
            {
                multipliers.Add(new FireModifier("point-blank-fire", 2m, "A7.21"));
            }

            if (range > 8)
""", """        private FirerFirepower VehicleFirepower(FireVehicleFire vehicle, bool vsConcealed, string? mg = null)
        {
            var definition = reference.Definitions[vehicle.DefinitionId!];
            var range = attack.Range!.Value;
            var multipliers = new List<FireModifier>();
            if (range == 1)
            {
                multipliers.Add(new FireModifier("point-blank-fire", 2m, "A7.21"));
            }

            if (range > VehicleWeaponRange(mg))
""")

edit("""            var printed = definition.AntiAircraftMg!.Value;
            return new FirerFirepower(vehicle.VehicleId!, printed, multipliers, multipliers.Aggregate((decimal)printed, (value, item) => value * item.Value));
""", """            // D1.8 (pass 50): the BMG's or the CMG's own FP when the attack names it, under the vehicle's id and the weapon's name.
            var printed = mg is null ? definition.AntiAircraftMg!.Value : VehicleWeaponFp(definition, mg)!.Value;
            return new FirerFirepower(mg is null ? vehicle.VehicleId! : vehicle.VehicleId + ":" + mg, printed, multipliers,
                multipliers.Aggregate((decimal)printed, (value, item) => value * item.Value));
""")

for old, new in EDITS:
    assert text.count(old) == 1, (text.count(old), old[:90])
for old, new in EDITS:
    text = text.replace(old, new)
if crlf:
    text = text.replace("\n", "\r\n")
io.open(PATH, "w", encoding="utf-8", newline="").write(text)
print("applied", len(EDITS))
