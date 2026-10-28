using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The Vehicle Target Type and To Kill resolution of the Ordnance package (backlog pass 7, rulings R7.2 to R7.10): a Gun's or a tank's shot
/// at one vehicle on the C3 Vehicle row with the reviewed To Hit Cases, where the hit strikes (C3.9), the Target Facing and AF it meets
/// (D3.2, D1.6), the To Kill number of the declared ammunition (C7), and the To Kill DR's result with the crew's checks (C7.4 to C7.7, D5.5,
/// D5.6). The special ammunition's Depletion Number decides whether it was fired at all (C8.9).
/// </summary>
internal static class ScenarioA1ArmorCalculator
{
    private const string Prefix = "asl.a1.ordnance.";

    /// <summary>The C5.1 Case A To Hit DRM of a turn of the given hexspines by a Gun of this type (T: +1 each; ST, RST, 1MT: +2 then +1; NT: +3 then +1).</summary>
    public static int CaseA(string? maType, int hexspines) => hexspines <= 0 ? 0 : maType switch
    {
        "t" => hexspines,
        "st" or "rst" or "1mt" => 2 + hexspines - 1,
        _ => 3 + hexspines - 1,
    };

    /// <summary>
    /// One item of a counter's special ammunition (C8.9, C8.91; ruling R55.4), written <c>&lt;type&gt;&lt;number&gt;[^&lt;year digit&gt;[m&lt;month&gt;][+][E]]</c>:
    /// the type (A for APCR, D for APDS, H for HEAT, HE, AP, C for Canister), its Depletion Number, the first year it holds in, the first month of
    /// that year, whether the counter prints "from that year on", and whether it prints the ETO alone. The last two are kept and not read: the last
    /// listed number holds after its year with or without the "+", and the game has no theater.
    /// </summary>
    internal readonly record struct SpecialAmmunition(string Type, int Number, int? Year, int? Month, bool Onward, bool EtoOnly);

    // The two-letter types first, so that HE7 is not read as HEAT and AP5 not as APCR.
    private static readonly string[] AmmunitionTypes = ["HE", "AP", "A", "D", "H", "C"];

    /// <summary>Reads one item of a counter's special ammunition (ruling R55.4); null for anything else the counter lists, such as <c>s7</c>.</summary>
    internal static SpecialAmmunition? ParseAmmunition(string item)
    {
        if (AmmunitionTypes.FirstOrDefault(name => item.StartsWith(name, StringComparison.Ordinal)) is not { } type)
        {
            return null;
        }

        var at = type.Length;
        int? Digits()
        {
            var start = at;
            while (at < item.Length && char.IsAsciiDigit(item[at]))
            {
                at++;
            }

            return at > start ? int.Parse(item.AsSpan(start, at - start), CultureInfo.InvariantCulture) : null;
        }

        bool Mark(char mark)
        {
            if (at < item.Length && item[at] == mark)
            {
                at++;
                return true;
            }

            return false;
        }

        if (Digits() is not { } number)
        {
            return null;
        }

        int? year = null;
        int? month = null;
        var onward = false;
        var eto = false;
        if (Mark('^'))
        {
            // C8.91: the exponent is the last digit of the year; the game's years run from 1936 to 1945.
            if (at >= item.Length || !char.IsAsciiDigit(item[at]))
            {
                return null;
            }

            var digit = item[at++] - '0';
            year = digit >= 6 ? 1930 + digit : 1940 + digit;
            if (Mark('m') && (month = Digits()) is not (>= 1 and <= 12))
            {
                return null;
            }

            onward = Mark('+');
            eto = Mark('E');
        }

        return at == item.Length ? new SpecialAmmunition(type, number, year, month, onward, eto) : null;
    }

    /// <summary>
    /// The Depletion Number of an ammunition at a date (C8.9, C8.91, C8.3, C8.8), one more for Elite forces (C8.2, p. 178), one less under Ammunition
    /// Shortage (A19.131, p. 86; pass 72, ruling R72.12), or null when the Gun has none then. A number holds from its year, and from its month when the
    /// counter gives one, until the next listed one.
    /// </summary>
    public static int? Depletion(GunDefinition gun, string ammunition, int? year, int? month, bool elite = false, bool shortage = false)
    {
        ArgumentNullException.ThrowIfNull(gun);
        var type = TypeOf(ammunition);
        var entries = gun.SpecialAmmo.Select(ParseAmmunition).OfType<SpecialAmmunition>().Where(entry => entry.Type == type).ToArray();
        if (entries.Length == 0)
        {
            return null;
        }

        int? Adjusted(int number) => number + (elite ? 1 : 0) - (shortage ? 1 : 0);

        // C8.3: HEAT is available to the Germans from May 1942, to the U.S., Britain, and Russia from 1943.
        if (type == "H" && entries.All(entry => entry.Year is null))
        {
            var from = gun.Nationality == "german" ? (Year: 1942, Month: 5) : (Year: 1943, Month: 1);
            return year > from.Year || (year == from.Year && (month ?? 0) >= from.Month) ? Adjusted(entries[0].Number) : null;
        }

        // A number with no year holds whatever the date (C8.8's AP# and HE#); a dated one needs the scenario's year, and its month where it names one.
        var held = entries.Where(entry => entry.Year is null
                || entry.Year < year || (entry.Year == year && (entry.Month is not { } first || (month ?? 0) >= first)))
            .OrderBy(entry => entry.Year ?? 0).ToArray();
        return held.Length > 0 ? Adjusted(held[^1].Number) : null;
    }

    /// <summary>
    /// The month an ammunition starts in, when that alone keeps it from a game that gives a year and no month (ruling R55.31): the counter, or
    /// C8.3 for undated HEAT, starts it in a month of that very year, and with no month the game cannot say that month has come, so it has
    /// none. Null in every other case, also when the Gun has the ammunition.
    /// </summary>
    public static int? StartMonthUnknown(GunDefinition gun, string ammunition, int? year, int? month)
    {
        ArgumentNullException.ThrowIfNull(gun);
        if (month is not null || year is null || Depletion(gun, ammunition, year, null) is not null)
        {
            return null;
        }

        var type = TypeOf(ammunition);
        var entries = gun.SpecialAmmo.Select(ParseAmmunition).OfType<SpecialAmmunition>().Where(entry => entry.Type == type).ToArray();
        if (type == "H" && entries.Length > 0 && entries.All(entry => entry.Year is null))
        {
            var from = gun.Nationality == "german" ? (Year: 1942, Month: 5) : (Year: 1943, Month: 1);
            return year == from.Year ? from.Month : null;
        }

        return entries.Where(entry => entry.Year == year && entry.Month is not null).Select(entry => entry.Month).Min();
    }

    /// <summary>The detail a refusal's code carries when <see cref="StartMonthUnknown"/> is its cause: <c>:from-month-5</c>; empty otherwise.</summary>
    internal static string StartMonthDetail(OrdnanceShot shot, GunDefinition gun) =>
        shot.Ammunition is { } ammunition && StartMonthUnknown(gun, ammunition, shot.ScenarioYear, shot.Hit?.ScenarioMonth) is { } from
            ? ":" + StartMonthMark + from.ToString(CultureInfo.InvariantCulture) : string.Empty;

    /// <summary>The start of that detail.</summary>
    public const string StartMonthMark = "from-month-";

    private static string? TypeOf(string ammunition) => ammunition switch
    {
        "apcr" => "A",
        "apds" => "D",
        "heat" => "H",
        "he" => "HE",
        "ap" => "AP",
        "canister" => "C",
        _ => null,
    };

    /// <summary>Whether the counter lists a Depletion Number for the ammunition at any date (C8.9): its HE or AP is then Special Ammunition (C8.8, C2.21).</summary>
    internal static bool Lists(GunDefinition gun, string ammunition) =>
        TypeOf(ammunition) is { } type && gun.SpecialAmmo.Any(item => ParseAmmunition(item)?.Type == type);

    /// <summary>
    /// The Depletion Number a shot's ammunition is fired under (C8.9, p. 179; pass 55, ruling R55.7), one more for an Elite force (C8.2), or null
    /// when it has none: a LATW's (C8.3), AP or HE the counter gives no number, and a shot that names no ammunition, which is HE that is not
    /// Special Ammunition (C2.21). Intensive Fire does not change it (C8.92). One less under Ammunition Shortage (A19.131; pass 72, ruling R72.12).
    /// </summary>
    internal static int? ShotDepletion(OrdnanceShot shot, GunDefinition gun) =>
        gun.GunType == "latw" || shot.Ammunition is not { } ammunition ? null
            : Depletion(gun, ammunition, shot.ScenarioYear, shot.Hit?.ScenarioMonth, shot.Elite == true, shot.AmmunitionShortage == true);

    /// <summary>
    /// C8.9: an Original To Hit DR below the Depletion Number uses the ammunition; one equal to it uses it and the firer "runs out of that Special
    /// Ammunition in the process"; above it "the firer had no such ammunition".
    /// </summary>
    internal static string Use(int original, int depletion) => original < depletion ? "used" : original == depletion ? "depleted" : "none";

    /// <summary>
    /// C8.9: the shot of a Gun that had no such ammunition and did not malfunction: it "is considered not to have fired yet for any purpose", so it
    /// keeps its fire, takes no fire counter, and its Acquisition stands.
    /// </summary>
    internal static OrdnanceResolution Unused(OrdnanceShot shot, GunDefinition gun, OrdnanceToHit toHit)
    {
        var missed = toHit with
        {
            Hit = false,
            CriticalHit = false
        };
        return new OrdnanceResolution(OrdnanceResolution.Resolved, [], missed, new OrdnanceGunEffect(gun.Breakdown, false, gun.RateOfFire ?? 0, true, null, shot.Acquisition!.Value, null))
        {
            AmmunitionUse = "none"
        };
    }

    /// <summary>D3.2 (p. 199; pass 50): the Target Facing of a vehicle fired on from inside its own hex, by the colored dr of the To Hit DR: 1-2 rear, 3-4 side, 5-6 front.</summary>
    public static string SameHexFacing(int coloredDr) => coloredDr <= 2 ? "rear" : coloredDr <= 4 ? "side" : "front";

    /// <summary>The facts a Vehicle Target Type shot needs beyond an Infantry Target Type shot's.</summary>
    public static void Missing(OrdnanceShot shot, Action<object?, string> need)
    {
        var target = shot.VehicleTarget!;
        need(target.VehicleId, "vehicleTarget.vehicleId");
        need(target.DefinitionId, "vehicleTarget.definitionId");

        // D3.2 (pass 50): from inside the target's hex the colored dr of the To Hit DR gives the Target Facing, so none is read from the map.
        if (shot.SameHex != true)
        {
            need(target.HullFacing, "vehicleTarget.hullFacing");
        }

        need(target.Moving, "vehicleTarget.moving");
        need(target.NonStopped, "vehicleTarget.nonStopped");
        need(target.Concealed, "vehicleTarget.concealed");
        need(target.CrewImpaired, "vehicleTarget.crewImpaired");
        need(target.CrewMayTakeTc, "vehicleTarget.crewMayTakeTc");
        need(shot.Ammunition, "ammunition");
    }

    /// <summary>The reasons a Vehicle Target Type shot is outside the review.</summary>
    public static void Outside(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, List<string> outside)
    {
        var target = shot.VehicleTarget!;
        // D3.2 (pass 50): a shot from inside the target's hex names no Target Facing, and one from outside it names the hull's, and the turret's too.
        if (!reference.Armor.Vehicles.TryGetValue(target.DefinitionId!, out var armor) || armor.Nationality == gun.Nationality
            || (shot.SameHex == true ? target.HullFacing is not null || target.TurretFacing is not null
                : target.HullFacing is not ("front" or "side" or "rear") || (armor.Turreted && target.TurretFacing is not ("front" or "side" or "rear")))
            || shot.Hit!.Targets is { Count: > 0 })
        {
            outside.Add(Prefix + "vehicle-target-outside");
            return;
        }

        var ammunition = shot.Ammunition!;
        var armored = !armor.Unarmored && armor.FrontAf is not null;
        var latw = gun.GunType == "latw";

        // A9.6, A9.61 (p. 64; pass 60, ruling R60.19): a MG's To Kill attack is made at an AFV; an unarmored vehicle is attacked on the IFT's
        // Vehicle line, by fire, not as ordnance.
        if (gun.GunType == ScenarioA1MachineGunKill.GunType)
        {
            if (!armored)
            {
                outside.Add(Prefix + "mg-unarmored-target");
            }

            return;
        }

        // C13.3, C13.34, C8.31 (ruling R9.8): a PF fires HEAT only at an AFV; its other targets need HE Equivalency, not built. Pass 9b: a PSK fires
        // HEAT and an ATR AP at any vehicle (C13.43, C13.2).
        if (latw && (gun.LatwType switch
        {
            "pf" => ammunition != "heat" || !armored,
            "psk" => ammunition != "heat",
            _ => ammunition != "ap"
        }))
        {
            outside.Add(Prefix + "ammunition-outside");
            return;
        }

        var tk = armored ? reference.Armor.BasicTk(ammunition, gun) : reference.Armor.UnarmoredTk(ammunition, gun);
        // C8.8 (p. 179; pass 55): a Gun whose caliber is underscored fires HE all the same when its counter lists an HE Depletion Number. Canister
        // "is ineffective vs armored targets" (C8.4) and is not fired on the Vehicle Target Type.
        if (ammunition is not ("ap" or "apcr" or "apds" or "heat" or "he") || (ammunition == "ap" && gun.NoAp) || (ammunition == "he" && gun.NoHe && !Lists(gun, "he"))
            || tk is null)
        {
            outside.Add(Prefix + "ammunition-outside");
        }

        // C8.11, C8.3, C8.8 (pass 55, ruling R55.7): APCR, APDS, and HEAT always have a Depletion Number, AP and HE where the counter lists one;
        // none at the scenario's date is no such ammunition.
        if (!latw && (ammunition is "apcr" or "apds" or "heat" || Lists(gun, ammunition)) && ShotDepletion(shot, gun) is null)
        {
            outside.Add(Prefix + "ammunition-unavailable" + StartMonthDetail(shot, gun));
        }

        if (shot.Gun!.Depleted?.Contains(ammunition, StringComparer.Ordinal) == true)
        {
            outside.Add(Prefix + "ammunition-depleted");
        }

        // C7.24: AP, APCR, and APDS need a Case D value at the range (NA is out of reach).
        if (armored && tk is not null && reference.Armor.CaseD(ammunition, gun, shot.Range!.Value) is null)
        {
            outside.Add(Prefix + "out-of-range");
        }
    }

    public static OrdnanceResolution Run(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference)
    {
        var rolls = shot.Rolls!;
        var hit = shot.Hit!;
        var target = shot.VehicleTarget!;
        var armor = reference.Armor.Vehicles[target.DefinitionId!];
        var ammunition = shot.Ammunition!;
        var range = shot.Range!.Value;
        var column = reference.Column(range);
        var color = ScenarioA1OrdnanceCalculator.CapturedColor(shot, ScenarioA1OrdnanceReference.Color(gun, shot.ScenarioYear));

        // C3.31, C4: the Vehicle row's Basic TH#, the Gun's modifications, and APCR's (C4.3). C13.33 (ruling R9.8): a PF's Basic TH# is 10, less
        // two for each hex of range, with no C4 modification.
        var latw = gun.GunType == "latw";
        var pf = gun.LatwType == "pf";

        // Pass 9b: a PSK reads its own To Hit Table (C13.48); an ATR the black Vehicle row whatever its nationality (C13.2).
        // A9.6 (p. 64; pass 60): "A non-captured MG To Hit DR always uses the black To Hit numbers regardless of nationality when firing as
        // ordnance". C4.2 (p. 171; the second referee): "any weapon (including MG)" of 57mm or less takes -1 for every 12 hexes beyond 12, and another
        // for 40mm or less; a MG is read as caliber 0 with no barrel length, so only those rows apply to it.
        var machineGun = gun.GunType == ScenarioA1MachineGunKill.GunType;
        if (gun.LatwType == "atr" || machineGun)
        {
            color = "black";
        }

        var basic = pf ? 10 : gun.ToHitTable.Count > 0 ? gun.ToHitTable[range - 1] : reference.Armor.BasicToHit(color, column);
        var modifications = pf ? [new FireModifier("pf-range", -2 * range, "C13.33")] : gun.ToHitTable.Count > 0 ? [] : reference.Modifications(gun, range).ToList();
        // C4.3 (p. 171): "the Basic TH# of any shot using APCR/APDS is lessened by one for every 12 hexes (or fraction thereof) beyond 12 hex range".
        if (ammunition is "apcr" or "apds" && reference.Armor.ApcrToHit(column) is var apcr && apcr != 0)
        {
            modifications.Add(new FireModifier(ammunition, apcr, "C4.3"));
        }

        var modified = basic + (int)modifications.Sum(item => item.Value);
        var drm = new List<FireModifier>();
        var woods = shot.FirerInWoodsOrBuilding == true;
        if (shot.HexspinesToTurn is int turned && turned > 0)
        {
            drm.Add(new FireModifier("case-a:" + turned.ToString(CultureInfo.InvariantCulture), CaseA(gun.MaType, turned) * (woods ? 2 : 1), "C5.1"));
        }
        else if (shot.Vehicle?.Weapons?.MaintainedCaseA is > 0 and var maintained && shot.SameHex != true)
        {
            // D3.51 (p. 201; pass 50): the MA fired within a TCA its CMG changed in this fire phase pays what the CMG paid.
            drm.Add(new FireModifier("case-a:maintained", maintained, "D3.51"));
        }

        if (latw)
        {
            // C13.1, C13.8 (ruling R9.8): Case C3 instead of Case B, for the AFPh and for the Backblast of a shot from a ground-level building
            // or rubble; C5.34 (p. 172; pass 35, task 35.10): neither for an Opportunity Firer.
            if (ScenarioA1OrdnanceCalculator.AfphFire(shot))
            {
                drm.Add(new FireModifier("case-c3:afph", 2, "C13.1"));
            }

            if (shot.Panzerfaust?.FromBuilding == true && gun.LatwType is "pf" or "psk" && !ScenarioA1OrdnanceCalculator.OpportunityFire(shot))
            {
                drm.Add(new FireModifier("case-c3:backblast", 2, "C13.8"));
            }
        }
        else if (ScenarioA1OrdnanceCalculator.CaseC(shot, gun) is { } caseC)
        {
            drm.Add(caseC);
        }
        else if (ScenarioA1OrdnanceCalculator.AfphFire(shot))
        {
            drm.Add(new FireModifier("case-b", woods ? 3 : 2, "C5.2"));
        }

        if (shot.Crew!.Pinned == true)
        {
            drm.Add(new FireModifier("case-d", 2, "C5.4"));
        }

        if (shot.Crew.Cx == true)
        {
            drm.Add(new FireModifier("cx", 1, "A4.51"));
        }

        if (shot.Vehicle?.ButtonedUp == true)
        {
            drm.Add(new FireModifier("case-i", 1, "C5.9"));
        }

        // D5.34: an AFV under a "+1" counter after a Stun adds one to its To Hit DR.
        if (shot.Vehicle?.StunRecovery == true)
        {
            drm.Add(new FireModifier("stun", 1, "D5.34"));
        }

        drm.AddRange(ScenarioA1OrdnanceCalculator.PassEightFirerDrm(shot));
        drm.AddRange(ScenarioA1OrdnanceCalculator.Leadership(shot, reference));

        // A15.23, C13.25 (pass 72, ruling R72.8): a hero's DRM modify the To Hit DR of a PF, PSK, or ATR at a vehicle, never its To Kill DR.
        drm.AddRange(ScenarioA1OrdnanceCalculator.HeroDrm(shot, gun, reference));

        // D3.44, D3.45 (p. 200; pass 55, rulings R55.16, R55.18): the vehicle's Armor Leader modifies its MA's To Hit DR, BU or not; an
        // Inexperienced Crew adds one.
        if (ScenarioA1ArmorLeaders.Modifier(shot.Vehicle?.ArmorLeadership) is { } armorLeader)
        {
            drm.Add(armorLeader);
        }

        // C6.11, C6.12 (ruling R8.1): Defensive First Fire at a vehicle that has spent at most one MP in the firer's continuous LOS takes Case J2,
        // at most three Case J1, else Case J; a moving target in a fire phase takes Case J.
        // D8.4 (p. 209; pass 35, task 35.13 j): none against a vehicle that began its MPh bogged and spends its MP in its Bog hex.
        if (shot.Movement?.InBogHex != true && shot.FireKind is not null && shot.Movement?.MpInLos is { } seen)
        {
            var mpInLos = seen - (shot.Movement.MpClaimed ?? 0);
            drm.Add(mpInLos <= 1 ? new FireModifier("case-j2", 4, "C6.12") : mpInLos <= 3 ? new FireModifier("case-j1", 3, "C6.11") : new FireModifier("case-j", 2, "C6.1"));
        }
        else if (shot.Movement?.InBogHex != true && target.Moving == true)
        {
            drm.Add(new FireModifier("case-j", 2, "C6.1"));
        }

        // C5.5 (p. 172; pass 50): "any Gun firing at a target within its own hex must add the +2 DRM of Case E", doubled in woods, a building, or
        // rubble; "Cases J3, J4, L, and M are not applicable".
        // A SW MG is no Gun: the chart marks Case E "S", not with a SW (the second referee); D3.2's Target Facing and C6.3's own-hex bar of Case L stay.
        var sameHex = shot.SameHex == true;
        if (sameHex && !(machineGun && shot.Vehicle is null))
        {
            drm.Add(new FireModifier("case-e", woods ? 4 : 2, "C5.5"));
        }

        if (target.Concealed == true)
        {
            drm.Add(new FireModifier("case-k", 2, "C6.2"));
        }

        // C6.3: no Point Blank Range against a Non-Stopped or Motion target, nor by a Motion firer. Cases M and N are not LATW DRM; Case L is
        // the ATR's, which reads no To Hit Table of its own (p. 174; pass 35, task 35.10).
        if (!sameHex && (!latw || gun.LatwType == "atr") && range <= 2 && target.NonStopped != true && shot.Vehicle?.InMotion != true)
        {
            drm.Add(new FireModifier("case-l", range == 1 ? -2 : -1, "C6.3"));
        }

        if (!latw && shot.BoreSighted == true && !sameHex)
        {
            drm.Add(new FireModifier("case-m", -2, "C6.4"));
        }
        else if (!latw && shot.Acquisition is int acquired && acquired < 0 && target.Concealed != true && !ScenarioA1OrdnanceCalculator.AcquisitionDenied(shot))
        {
            drm.Add(new FireModifier("case-n", acquired, "C6.5"));
        }

        // C6.7, D1.7: the target's size.
        var size = armor.TargetSize switch
        {
            "very-small" => 2,
            "small" => 1,
            "large" => -1,
            "very-large" => -2,
            _ => 0,
        };
        if (size != 0)
        {
            drm.Add(new FireModifier("case-p:" + armor.TargetSize, size, "C6.7"));
        }

        // D2.38 (p. 198; pass 35, task 35.13 b): a vehicle in Bypass is in the Open Ground of its hex and takes no beneficial TEM of the woods or
        // building it Bypasses. A building Bypassed inside a woods hex (2I9, 3I1) is not told apart.
        var tem = ScenarioA1FireReference.Tem.GetValueOrDefault(hit.TargetTerrain!);
        if (tem != 0 && !(target.Bypass == true && tem > 0))
        {
            drm.Add(new FireModifier("case-q:" + hit.TargetTerrain, tem, "C6.8"));
        }

        // D4.22, B10.31 (pp. 202, 125; pass 55): "The +1 Height Advantage TEM vs Direct Fire also applies to vehicles, but only if they are not HD
        // to the firer"; no vehicle is HD (D4.2 is not built). A vehicle in Bypass took no TEM above, so it may take this one.
        if (ScenarioA1OrdnanceCalculator.HeightAdvantageApplies(shot, drm))
        {
            drm.Add(new FireModifier("case-q:height-advantage", 1, "B10.31"));
        }

        if (hit.Los!.HindranceDrm is int hindrance && hindrance > 0)
        {
            drm.Add(new FireModifier("case-r", hindrance, "C6.9"));
        }

        if (rolls.ToHit is not { } dice)
        {
            return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:toHit"]);
        }

        var total = (int)drm.Sum(item => item.Value);
        var original = dice[0] + dice[1];

        // C5.35 (p. 172; pass 50): a Motion firer with no Gyrostabilizer doubles the lower dr of its TH DR; the lowest Final DR is then one more.
        var motion = ScenarioA1OrdnanceCalculator.MotionFirer(shot) ? Math.Min(dice[0], dice[1]) : 0;
        if (motion > 0)
        {
            drm.Add(new FireModifier("case-c4:lower-dr", motion, "C5.35"));
        }

        var final = original + total + motion;
        var lowest = 2 + total + (motion > 0 ? 1 : 0);
        var improbable = lowest > modified;
        bool isHit, critical;
        int? subsequent = null;
        if (improbable)
        {
            // C3.6: an Original 2 still hits on a subsequent dr of 1 (a Critical Hit) to 3.
            if (original == 2)
            {
                if (rolls.Subsequent is not { } dr)
                {
                    return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:subsequent"]);
                }

                subsequent = dr;
            }

            // A9.61, C3.8 (pass 60): a MG scores no Critical Hit.
            isHit = subsequent is >= 1 and <= 3;
            critical = subsequent == 1 && !machineGun;
        }
        else
        {
            // C3.7: on the Vehicle Target Type an Original 2 is a Critical Hit, unless only the lowest Final DR hits, when it takes a
            // subsequent dr of 1 (a 2 to 6 is a normal hull hit).
            isHit = final <= modified;
            critical = false;
            if (isHit && original == 2 && !machineGun)
            {
                if (lowest == modified)
                {
                    if (rolls.Subsequent is not { } dr)
                    {
                        return Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:subsequent"]);
                    }

                    subsequent = dr;
                    critical = dr == 1;
                }
                else
                {
                    critical = true;
                }
            }
        }

        var toHit = new OrdnanceToHit(color, basic, modifications, modified, [.. dice], original, drm, final, improbable, subsequent, isHit, critical);
        var malfunctioned = original >= ScenarioA1OrdnanceCalculator.Breakdown(shot, gun, reference);

        // C13.36 (ruling R9.8): a PF never malfunctions, but an Original 12 misses and gives its firer Casualty Reduction; an 11 or 12 for
        // Inexperienced Infantry (A19.32; referee, pass 9).
        var inexperienced = reference.Fire.Definitions.GetValueOrDefault(shot.Crew!.DefinitionId ?? string.Empty)?.Class is "green" or "conscript";
        var casualty = pf && original >= (inexperienced ? 11 : 12);
        if (casualty)
        {
            isHit = critical = false;
            toHit = toHit with
            {
                Hit = false,
                CriticalHit = false
            };
        }

        // C8.9: special ammunition is used below its Depletion Number, used and run out at it, and was never there above it, when the Gun
        // did not fire at all unless it malfunctioned. Pass 55 (ruling R55.7): every ammunition with a Depletion Number, APDS, and AP or HE the
        // counter gives one (C8.8), beside APCR and HEAT.
        string? use = null;
        if (ShotDepletion(shot, gun) is { } depletion)
        {
            use = Use(original, depletion);
            if (use == "none" && !malfunctioned)
            {
                return Unused(shot, gun, toHit);
            }

            // A round that was not there hits nothing, though the Gun malfunctioned on the DR. An APCR or HEAT record made before pass 55 keeps
            // the To Hit result it shows; no To Kill DR follows it either way.
            if (use == "none" && ammunition is not ("apcr" or "heat"))
            {
                isHit = critical = false;
                toHit = toHit with
                {
                    Hit = false,
                    CriticalHit = false
                };
            }
        }

        // A21.12 (pass 65, ruling R65.22): a Temporary crew's captured-weapon penalty lowers the Multiple ROF one each.
        var rof = (gun.RateOfFire ?? 0) - (shot.CapturedPenalty ?? 0);
        if (shot.HexspinesToTurn > 0 && shot.Vehicle is null)
        {
            rof--;
        }

        if (shot.Crew.Pinned == true || shot.Phase == "AFPh" || latw)
        {
            rof = 0;
        }

        var (kept, counter) = ScenarioA1OrdnanceCalculator.Counter(shot, malfunctioned, rof, dice[0]);
        OrdnanceKill? kill = null;
        if (isHit && use != "none")
        {
            var (resolved, pending) = Kill(shot, gun, armor, reference, dice, critical, ammunition, range, improbable ? subsequent : null);
            if (pending is not null)
            {
                return pending;
            }

            kill = resolved;
        }

        // D.8A (p. 192; pass 60, rulings R60.3, R60.7): a hit that leaves the AFV standing attacks its Vulnerable crew, when the record carries the rule.
        OrdnanceCollateral? collateral = null;
        if (kill is not null && shot.SpecificCollateral == true)
        {
            var (made, pending) = SpecificCollateral(shot, gun, armor, kill, reference);
            if (pending is not null)
            {
                return pending;
            }

            collateral = made;
        }

        // C6.5: the shot acquires the target's Location, one step more if already acquired, unless the Gun malfunctions; a concealed vehicle
        // is acquired only when the shot costs it its "?" (the game decides that, R6.7).
        // C6.5 (the chart's G): a non-mortar SW acquires nothing; nor does a MG fired as ordnance, a vehicle's MA MG included (pass 60).
        // D10.1 (pass 65, ruling R65.23): a wreck is no unit to carry an Acquisition (C6.5: "Known enemy units"), and gives none.
        var acquires = !malfunctioned && target.Concealed != true && !latw && !machineGun && target.Wreck != true;
        var acquisition = !acquires ? 0 : Math.Max((ScenarioA1OrdnanceCalculator.AcquisitionDenied(shot) ? 0 : shot.Acquisition!.Value) - 1, -2);
        var gunEffect = new OrdnanceGunEffect(ScenarioA1OrdnanceCalculator.Breakdown(shot, gun, reference), malfunctioned, Math.Max(rof, 0), kept, counter, acquisition,
            acquires ? shot.TargetLocationId : null);
        return new OrdnanceResolution(OrdnanceResolution.Resolved, [], toHit, gunEffect)
        {
            CrewConcealmentLost = shot.Crew.Concealed == true && shot.CrewSeen != false ? true : null,
            Kill = kill,
            SpecificCollateral = collateral,
            AmmunitionUse = use,
            FirerEffect = casualty ? OrdnancePanzerfaustCheck.CasualtyReduction : null,
        };
    }

    /// <summary>
    /// The Specific Collateral Attack (D.8, D.8A, p. 192; pass 60, ruling R60.7): when a hit on an AFV does not destroy it, its CE crew takes the
    /// round's IFT FP (its HE FP, or its HE Equivalency, C8.31) on the Original TK DR, with the CE DRM alone, "the only DRM which applies" (D5.31). None
    /// after a Dud (C7.35, the table's note 4) or a destroying hit; none at the crew after a Shock (C7.42) or an Abandonment, nor at a crew stunned,
    /// shocked, Recalled, or BU. One attack for the hit, never doubled, and no Residual FP. The PRC the shot carries (pass 65) take it whenever the AFV
    /// stands, its Passengers spared once the crew is or becomes stunned or shocked. Null when there is none.
    /// </summary>
    private static (OrdnanceCollateral? Collateral, OrdnanceResolution? Pending) SpecificCollateral(OrdnanceShot shot, GunDefinition gun, ArmorDefinition armor,
        OrdnanceKill kill, ScenarioA1OrdnanceReference reference)
    {
        var target = shot.VehicleTarget!;
        // D6.84 (p. 207; pass 65, ruling R65.18): a Carrier's crew "remain CE if stunned or shocked, and are subject to the Collateral Attack that may result
        // from that attack", where the shot carries the target's PRC (ruling R65.5), as every shot planned since the pass does.
        var carrier = target.Prc is not null && ScenarioA1CarrierRules.IsCarrier(reference.Fire.Definitions.GetValueOrDefault(armor.Id)?.VehicleClass);
        // A9.611, D5.34 (pass 60): a MG's attack is at its IFT FP (the D1.22 example), and none follows its Stun, which leaves the crew BU at once.
        // D.8, D.8A and the Collateral Attack Table's note 1 (p. 192 to 193; pass 65, the referee's review): a Shock, a Stun, or an Abandonment stops only
        // the crew's attack; "If crew/Passengers are/become stunned or shocked, Collateral Attack applies only to the vehicle's Riders", and the PRC are
        // attacked whenever the vehicle is not destroyed. Only a shot that carries the PRC (ruling R65.5) attacks them, so an older record reads as before.
        var standing = kill.Result is not (OrdnanceKill.Dud or OrdnanceKill.Burn or OrdnanceKill.Eliminated);
        var becomeImpaired = kill.Result is OrdnanceKill.Shock or OrdnanceKill.Stun || kill.Shocked == true;
        var crewAttacked = kill.Abandoned != true && (carrier || !becomeImpaired);
        var firepower = gun.GunType == ScenarioA1MachineGunKill.GunType ? gun.Firepower
            : kill.Ammunition == "he" ? reference.HeFirepower(gun.Caliber)
            // C8.31 note 1, C13.24: an ATR that is not 20mm has no HE Equivalency, so its hit makes no Collateral Attack.
            : gun.LatwType == "atr" && !ScenarioA1HeEquivalency.AtrHasEquivalency(gun.Caliber) ? null
            : ScenarioA1HeEquivalency.Firepower(kill.Ammunition, ScenarioA1HeEquivalency.Weapon(gun), gun.Caliber, ScenarioA1OrdnanceCalculator.HeFirepowerOf(gun, reference));
        // A7.3 (p. 55): "the rightmost column of the IFT whose listed FP ... does not exceed the total adjusted FP": a 3 FP LMG attacks on the 2 column.
        var column = firepower is not { } fp ? -1
            : gun.GunType == ScenarioA1MachineGunKill.GunType ? Array.FindLastIndex(ScenarioA1FireReference.ColumnFp, item => item <= fp)
            : Array.IndexOf(ScenarioA1FireReference.ColumnFp, fp);
        // D10.1 (pass 65, ruling R65.23): a wreck has no crew and no PRC to take one.
        if (!standing || column < 0 || target.Wreck == true)
        {
            return (null, null);
        }

        FireVehicleEffect? effect = null;
        string? result = null;
        if (crewAttacked && !armor.Unarmored && armor.FrontAf is not null && target.CrewExposed == true && (carrier || target.CrewImpaired != true) && target.Abandoned != true)
        {
            if ((target.CrewMorale ?? reference.Fire.AfvCrewMorale(armor.Nationality)) is not { } morale)
            {
                return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "definition-incomplete:crew-morale:" + armor.Nationality]));
            }

            IReadOnlyList<FireModifier> drm = [new FireModifier("crew-exposed", 2m, "D5.31")];
            var final = kill.OriginalDr + 2;
            result = reference.Fire.Result(final, column);
            if (ScenarioA1FireCalculator.CrewChecks(result) && shot.Rolls!.CollateralCheck is null)
            {
                return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:collateralCheck"]));
            }

            // D5.342 (p. 204): a Carrier's crew stunned by this very hit is Recalled by a second Stun (D6.84).
            var (check, crew) = ScenarioA1FireCalculator.CrewOutcome(result, shot.Rolls!.CollateralCheck, target.StunRecovery == true || (carrier && kill.Result == OrdnanceKill.Stun),
                morale, target.CrewMorale);
            effect = new FireVehicleEffect(target.VehicleId!, armor.Id, FireVehicleEffect.None, null, drm, final, null, check, crew);
        }

        // D.8A, D6.71 (pp. 192, 206; pass 65, ruling R65.7): the PRC the target carries take the same attack, an unarmored vehicle's included, when the
        // record carries them; their checks and Random Selection are keyed under "prc:".
        var reached = !becomeImpaired ? target : target with
        {
            CrewImpaired = true
        };
        var (attacks, desperation, missing) = ScenarioA1FireCalculator.SpecificCollateralOnPrc(reached, column, kill.OriginalDr, shot.Rolls!.Prc, reference.Fire);
        if (missing is not null)
        {
            return (null, Refused(OrdnanceResolution.Indeterminate, [missing.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal)
                ? Prefix + "roll-missing:prc:" + missing["asl.a1.fire.roll-missing:".Length..] : missing]));
        }

        if (attacks is not null || desperation is not null)
        {
            effect = (effect ?? new FireVehicleEffect(target.VehicleId!, armor.Id, FireVehicleEffect.None, null, [], kill.OriginalDr, null, null,
                FireVehicleEffect.NotVulnerable)) with
            {
                PrcAttacks = attacks,
                PrcDesperation = desperation,
            };
        }

        return effect is null ? (null, null)
            : (new OrdnanceCollateral(firepower!.Value, result ?? attacks?[0].Result ?? reference.Fire.Result(kill.OriginalDr, column), effect), null);
    }

    private static (OrdnanceKill? Kill, OrdnanceResolution? Pending) Kill(OrdnanceShot shot, GunDefinition gun, ArmorDefinition armor, ScenarioA1OrdnanceReference reference,
        IReadOnlyList<int> dice, bool critical, string ammunition, int range, int? improbable)
    {
        var target = shot.VehicleTarget!;
        var rolls = shot.Rolls!;

        // C3.9: the colored dr below the white dr strikes a turret; otherwise the hull. C3.6: an improbable hit's subsequent dr decides
        // instead: 2 strikes the turret, 3 the hull (1, the Critical Hit, strikes as its Original 2, the hull). The turret's Target Facing
        // follows its TCA (D3.12).
        var location = armor.Turreted && (improbable is { } subsequent ? subsequent == 2 : dice[0] < dice[1]) ? "turret" : "hull";

        // D3.2 (p. 199; pass 50): "if the fire originates from within the target hex (Case E To Hit DRM; C5.5) the colored dr of the To Hit DR
        // determines the Target Facing: 1-2 Rear, 3-4 Side, 5-6 Front", for the hull and the turret alike.
        var facing = shot.SameHex == true ? SameHexFacing(dice[0]) : location == "turret" ? target.TurretFacing ?? target.HullFacing! : target.HullFacing!;
        var armored = !armor.Unarmored && armor.FrontAf is not null;
        var machineGun = gun.GunType == ScenarioA1MachineGunKill.GunType;
        int finalTk;
        int? basicTk = null, modifiedTk = null, af = null;
        var modifiers = new List<FireModifier>();
        if (armored)
        {
            // C7.11, C7.2: the Basic TK#, doubled by a Critical Hit (Case C), +1 against the rear (Case A), the range change (Case D), less the AF.
            // A9.61 (pass 60): a MG's is the @MG entry, 4, and its Case D the row of 25mm and below (its caliber is read as 0).
            basicTk = machineGun ? ScenarioA1MachineGunKill.BasicTk : reference.Armor.BasicTk(ammunition, gun)!.Value;
            var value = basicTk.Value;
            if (critical)
            {
                modifiers.Add(new FireModifier("case-c", value, "C7.23"));
                value *= 2;
            }

            if (facing == "rear")
            {
                modifiers.Add(new FireModifier("case-a", 1, "C7.21"));
                value++;
            }

            if (reference.Armor.CaseD(ammunition, gun, range) is int caseD && caseD != 0)
            {
                modifiers.Add(new FireModifier("case-d", caseD, "C7.24"));
                value += caseD;
            }

            modifiedTk = value;
            af = ScenarioA1ArmorReference.ArmorFactor(armor, location, facing)!.Value;
            finalTk = value - af.Value;
        }
        else
        {
            // C7.311, C7.331, C7.342: the unarmored Final TK#, doubled by a Critical Hit.
            finalTk = reference.Armor.UnarmoredTk(ammunition, gun)!.Value * (critical ? 2 : 1);
        }

        if (rolls.ToKill is not { } kill)
        {
            return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:toKill"]));
        }

        var dr = kill[0] + kill[1];
        OrdnanceKill Made(string result) => new(ammunition, location, facing, basicTk, modifiers, modifiedTk, af, finalTk, [.. kill], dr, result);
        var plusOne = target.StunRecovery == true ? 1 : 0;
        IReadOnlyList<FireModifier> checkDrm = plusOne == 0 ? [] : [new FireModifier("stun", 1, "D5.34")];

        // C7.35: an Original 12 is a dud.
        if (dr == 12)
        {
            return (Made(OrdnanceKill.Dud), null);
        }

        // C7.7, C7.6: at most half the Final TK# burns, less eliminates, equal immobilizes (hull) or Shocks (turret); one more Shocks or
        // immobilizes an HE hit and gives any other ammunition a possible Shock. The unarmored table has no Shock.
        // A9.61, D5.34 (pass 60): a MG's To Kill DR burns, eliminates, or Stuns; it neither Shocks nor immobilizes.
        string ResultOn(int original) => machineGun ? ScenarioA1MachineGunKill.Result(original, finalTk)
            : original * 2 <= finalTk ? OrdnanceKill.Burn
            : original < finalTk ? OrdnanceKill.Eliminated
            : original == finalTk ? (armored && location == "turret" ? OrdnanceKill.Shock : OrdnanceKill.Immobilized)
            : armored && original == finalTk + 1 ? (ammunition == "he" ? (location == "turret" ? OrdnanceKill.Shock : OrdnanceKill.Immobilized) : OrdnanceKill.PossibleShock)
            : OrdnanceKill.None;
        var result = ResultOn(dr);

        // D5.7, C7.7 note A (pass 65, ruling R65.21): a red CS# burns on a Final TK DR one lower, for the burn alone.
        if (result == OrdnanceKill.Eliminated && ScenarioA1CrewSurvivalRules.BurnsRed(armor.CsRed, dr, lower => lower * 2 <= finalTk))
        {
            result = OrdnanceKill.Burn;
        }

        var made = Made(result);

        // D10.1 (p. 210; pass 65, ruling R65.23): a wreck fired at as its vehicle burns on a Burn; nothing else touches it, and it has no crew.
        if (target.Wreck == true)
        {
            return (ScenarioA1WreckRules.WreckKill(made), null);
        }

        // D5.5, D8.11, D8.22 (pp. 204, 208, 209; pass 65, ruling R65.21): an AFV already bogged or immobilized that the shot neither destroys, Stuns, nor
        // Shocks takes the TC when an Original TK DR of 5 would have destroyed or Shocked it; only a shot that carries its PRC, as every shot planned since
        // the pass does, so that an older record resolves as it did. An unarmed vehicle's Inherent Driver "is never treated as a crew" (D5.1): its cs# is
        // printed in lower case. A MG's To Kill attack is no ordnance; a Possible Shock calls for the TC once its NTC has passed (the referee's review).
        var secondCause = target.Prc is not null
            && ScenarioA1CrewSurvivalRules.SecondCause(result, ResultOn, target.Immobile == true,
                target.CrewImpaired != true && target.Abandoned != true && ScenarioA1CrewSurvivalRules.CrewSurvival(armor, false) is not null, machineGun);

        // D5.1: an AFV's crew has Elite morale, an unarmored vehicle's the 1st Line morale of its nationality.
        // D3.42, D3.45 (p. 200; pass 55, rulings R55.17, R55.18): "[EXC: Armor Leaders (3.42) & Inexperienced Crews (3.45)]": the crew "always has
        // the same Morale Level as any Armor Leader in that vehicle", 6 when Inexperienced. His leadership modifier is not added: he takes the check.
        var morale = target.CrewMorale ?? reference.Fire.Definitions.Values.Where(item => item.IsMmc && item.Nationality == armor.Nationality && item.Class == (armored ? "elite" : "1st-line"))
            .Max(item => item.Morale);
        if (morale is null && (result == OrdnanceKill.PossibleShock || (result == OrdnanceKill.Immobilized && target.CrewMayTakeTc == true) || secondCause))
        {
            return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "definition-incomplete:crew-morale:" + armor.Nationality]));
        }

        if (result == OrdnanceKill.Shock)
        {
            made = made with
            {
                Shocked = true
            };
        }
        else if (result == OrdnanceKill.PossibleShock && morale is { } shockMorale)
        {
            // C7.41: the crew takes a NTC; failure Shocks the AFV.
            if (rolls.ShockCheck is not { } check)
            {
                return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:shockCheck"]));
            }

            var total = check[0] + check[1];
            var passed = total + plusOne <= shockMorale;
            made = made with
            {
                ShockCheck = new FireCheck("NTC", [.. check], total, checkDrm, total + plusOne, shockMorale, passed, passed ? "none" : OrdnanceKill.Shock)
                {
                    MoraleSource = ScenarioA1ArmorLeaders.MoraleSource(target.CrewMorale),
                },
                Shocked = passed ? null : true,
            };
        }

        // D5.5: the TC follows a Possible Shock's NTC, when it passed, in the same resolution; an older record has no second cause.
        if (((result == OrdnanceKill.Immobilized && target.CrewMayTakeTc == true) || (secondCause && made.Shocked != true)) && morale is { } tcMorale)
        {
            // D5.5: an immobilized vehicle's crew takes a TC at once; failure Abandons it.
            if (rolls.CrewCheck is not { } check)
            {
                return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:crewCheck"]));
            }

            var tc = ScenarioA1CrewSurvivalRules.TaskCheck(check, tcMorale, plusOne == 1, target.CrewMorale);
            made = made with
            {
                CrewCheck = tc,
                Abandoned = tc.Passed ? null : true,
                SecondCause = secondCause ? true : null,
            };
        }
        else if (result == OrdnanceKill.Eliminated && ScenarioA1CrewSurvivalRules.CrewSurvival(armor, target.Abandoned == true) is { } cs)
        {
            // D5.6: a Final DR at most the CS#, +1 if the crew was Stunned, Shocked, Recalled, or under a "+1" counter, places a crew beneath the
            // wreck.
            if (rolls.CrewSurvival is not { } survival)
            {
                return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:crewSurvival"]));
            }

            made = made with
            {
                CrewSurvival = ScenarioA1CrewSurvivalRules.Survival(cs, survival,
                    ScenarioA1CrewSurvivalRules.SurvivalDrm(false, target.CrewImpaired == true, false, target.StunRecovery == true, target.Prc is not null))
            };
        }

        // D6.9, D5.6 (pp. 207, 204; pass 65, ruling R65.9): each Passenger and Rider of a vehicle eliminated, not burned, rolls its own Survival DR against
        // the CS# or cs#, +1 if broken, when the record carries them.
        if (result == OrdnanceKill.Eliminated)
        {
            var (prcSurvival, missing) = ScenarioA1FireCalculator.PrcSurvivals(target.Prc, armor.CrewSurvival, rolls.Prc?.PrcSurvival);
            if (missing is not null)
            {
                return (null, Refused(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:prc:" + missing]));
            }

            made = made with
            {
                PrcSurvival = prcSurvival
            };
        }

        return (made, null);
    }

    private static OrdnanceResolution Refused(string disposition, IReadOnlyList<string> reasons) => new(disposition, reasons, null, null);
}
