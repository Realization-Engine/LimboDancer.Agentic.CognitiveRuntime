namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A7.7 Encirclement (p. 57), A7.71 (p. 58), one calculator (the D5.34 claim's design, rulings RD5.7 to RD5.13): who may be Encircled, which fire counts
/// toward an Encirclement, the geometry of the LOS entries, when the attacks seal it, and its three effects: the morale drop, the firer's +1, and the
/// doubled MF of the first Location entered. A7.72's upper-level Encirclement is decided by its search, <see cref="ScenarioA1UpperLevelEncirclement"/>,
/// whose verdict the callers read beside the marker. The consecutive scan of a phase's attacks stays with its records in
/// <see cref="ScenarioA1FireMapRules.EncirclementSeal"/>, which asks <see cref="Seals"/>. Facts in, verdict out; no state.
/// </summary>
public static class ScenarioA1EncirclementCalculator
{
    /// <summary>
    /// A10.532 (p. 67): no weapon counts toward an Encirclement beyond "its Normal Range or 16 hexes, whichever is less" (ruling RD5.15). Every SW and
    /// inherent Normal Range of the catalog is at most 16, so the cap reaches only ordnance and vehicular armament.
    /// </summary>
    public const int NormalRangeCap = 16;

    /// <summary>The position of a LOF from the Location directly above (A7.7), as <see cref="ScenarioA1FireMapRules.FromAbove"/> numbers it.</summary>
    public const int FromAbove = ScenarioA1FireMapRules.FromAbove;

    /// <summary>The position of a LOF from the Location directly below (A7.7), as <see cref="ScenarioA1FireMapRules.FromBelow"/> numbers it.</summary>
    public const int FromBelow = ScenarioA1FireMapRules.FromBelow;

    /// <summary>
    /// Whether a unit may be Encircled (A7.7, p. 57; B30.32, p. 150; rulings RD5.7, RD5.11): "any non-Aerial Infantry, or Vulnerable PRC of an Immobile
    /// vehicle"; never berserk or heroic, a hero, or a vehicle; never in a pillbox ("Neither a pillbox nor its contents can be Encircled"); a unit aboard
    /// a vehicle only as the Vulnerable PRC of an Immobile vehicle. A null pillbox or aboard fact reads as before the claim (not in one, not aboard).
    /// </summary>
    public static bool Subject(string? kind, bool berserk, bool heroic, bool? inPillbox, bool? aboard, bool? vulnerablePrcOfImmobileVehicle) =>
        !berserk && !heroic && kind != "asl:hero" && kind != "asl:vehicle" && inPillbox != true && (aboard != true || vulnerablePrcOfImmobileVehicle == true);

    /// <summary>
    /// Whether a unit may be Encircled (A7.7, B30.32), from its facts record: <see cref="Subject(string?, bool, bool, bool?, bool?, bool?)"/>, and never an
    /// Aerial unit (A7.7: "any non-Aerial Infantry") when <see cref="EncirclementSubjectFacts.Aerial"/> is true.
    /// </summary>
    public static bool Subject(EncirclementSubjectFacts unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return unit.Aerial != true && Subject(unit.Kind, unit.Berserk, unit.Heroic, unit.InPillbox, unit.Aboard, unit.VulnerablePrcOfImmobileVehicle);
    }

    /// <summary>
    /// Whether a unit on the map is Encircled under a marker (A7.7, p. 57; ruling R12.11): of the Encircled side there
    /// (<paramref name="encircledForSide"/>), or in Melee there while any side is Encircled there (<paramref name="encircledForAnySide"/>; "every ...
    /// enemy/Melee Personnel unit"), and a <see cref="Subject(string?, bool, bool, bool?, bool?, bool?)"/>.
    /// </summary>
    public static bool Encircled(bool onMap, bool encircledForSide, bool encircledForAnySide, bool melee, bool subject) =>
        onMap && (encircledForSide || (encircledForAnySide && melee)) && subject;

    /// <summary>Whether a unit on the map is Encircled under a marker (A7.7), from its facts record.</summary>
    public static bool Encircled(bool onMap, bool encircledForSide, bool encircledForAnySide, EncirclementSubjectFacts unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return Encircled(onMap, encircledForSide, encircledForAnySide, unit.Melee, Subject(unit));
    }

    /// <summary>
    /// Whether one unit's fire counts toward an Encirclement (A7.7, p. 57; A10.532, p. 67; rulings RD5.8, RD5.9, RD5.15): in the PFPh, DFPh, or AFPh;
    /// not Aerial; by inherent FP, a SW, ordnance, or vehicular armament (not Residual FP); at a range of 1 to the lesser of its Normal Range and 16
    /// hexes, or as a LOF from the Location directly above or below; ordnance only when it hits ("ordnance weapons must secure a hit on the target"), other fire only when it
    /// could inflict at least a NMC, Cowering counted. An unknown range, Normal Range, hit, or NMC test is no valid fire.
    /// </summary>
    public static bool ValidFire(EncirclementFireFacts fire)
    {
        ArgumentNullException.ThrowIfNull(fire);
        if (!FirePhase(fire.Phase) || fire.Aerial || fire.Kind is not (EncirclementFireKind.InherentFp or EncirclementFireKind.SupportWeapon
            or EncirclementFireKind.Ordnance or EncirclementFireKind.VehicularArmament))
        {
            return false;
        }

        if (fire.NormalRange is not { } normal || fire.Range is not { } range)
        {
            return false;
        }

        var inReach = fire.AboveOrBelow ? range == 0 : range >= 1 && range <= Math.Min(normal, NormalRangeCap);
        return inReach && (fire.Kind == EncirclementFireKind.Ordnance ? fire.Hit == true : fire.CouldInflictNmc == true);
    }

    /// <summary>A7.7 (p. 57): Encirclement is made by fire "during the same PFPh, DFPh, or AFPh", never in the MPh.</summary>
    public static bool FirePhase(string? phase) =>
        string.Equals(phase, "pfph", StringComparison.OrdinalIgnoreCase) || string.Equals(phase, "dfph", StringComparison.OrdinalIgnoreCase)
        || string.Equals(phase, "afph", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether LOS entries Encircle a hex (A7.7, p. 57; A7.71, p. 58; ruling R12.11): two opposite hexspines, two opposite hexsides (exactly three
    /// vertices between them both ways), or three hexsides no two of which are adjacent; or a LOF from both the Location directly above
    /// (<see cref="FromAbove"/>) and directly below (<see cref="FromBelow"/>). Positions as <see cref="ScenarioA1FireMapRules.PerimeterPosition"/> gives
    /// them: hexside i is 2i + 1, the hexspine after it 2i + 2.
    /// </summary>
    public static bool Encircles(IReadOnlyCollection<int> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        var set = positions.ToHashSet();

        // A7.7 (pass 45): a LOF from both the Location directly above (12) and the one directly below (13).
        if (set.Contains(12) && set.Contains(13))
        {
            return true;
        }

        if (set.Any(position => position < 12 && set.Contains((position + 6) % 12)))
        {
            return true;
        }

        var sides = set.Where(position => position < 12 && position % 2 == 1).Select(position => (position - 1) / 2).ToHashSet();
        return sides.Any(side => sides.Contains((side + 2) % 6) && sides.Contains((side + 4) % 6));
    }

    /// <summary>
    /// Whether the phase's consecutive attacks on a Location seal its Encirclement (A7.7, p. 57; A7.71, p. 58): "fired on ... by two or more non-Aerial
    /// units" whose LOS entries <see cref="Encircles"/> it. <paramref name="countingUnits"/> counts every unit of valid fire (<see cref="ValidFire"/>)
    /// in the sequence, this attack's included; a FG's units each count, so one FG can Encircle alone (A7.71).
    /// </summary>
    public static bool Seals(int countingUnits, IReadOnlyCollection<int> entries) => countingUnits >= 2 && Encircles(entries);

    /// <summary>
    /// Why the attacks on a Location cannot seal an Encirclement, or null (A7.7, p. 57; B30.32, p. 150): not in a fire phase; the sequence broken by
    /// fire at another target ("consecutively"); the target in a pillbox. An unknown fact refuses, and an unknown pillbox says so.
    /// </summary>
    public static string? SealRefusal(string? phase, bool? consecutive, bool? inPillbox) =>
        !FirePhase(phase) ? "Encirclement is made only by fire in the same PFPh, DFPh, or AFPh (A7.7)"
        : consecutive != true ? "The attacks were not consecutive: fire at another target between them breaks the sequence (A7.7)"
        : inPillbox is null ? "Whether the target is in a pillbox is not known (B30.32)"
        : inPillbox.Value ? "Neither a pillbox nor its contents can be Encircled (B30.32)"
        : null;

    /// <summary>
    /// The Encircled unit's morale drop (A7.7, p. 57): "every non-berserk, non-heroic enemy/Melee Personnel unit therein suffers an immediate one level
    /// drop in morale" against the sealing attack and every later attack on the Location; 1 or 0.
    /// </summary>
    public static int MoraleDrop(bool encircled, bool berserk, bool heroic) => encircled && !berserk && !heroic ? 1 : 0;

    /// <summary>
    /// The Encircled firer's DRM (A7.7, p. 57): "All fire by an Encircled unit is subject to a +1 DRM on the IFT (or To Hit DR if ordnance)"; once per
    /// attack however many of its firers are Encircled (A7.52); 1 or 0.
    /// </summary>
    public static int FirerDrm(bool anyFirerEncircled) => anyFirerEncircled ? 1 : 0;

    /// <summary>
    /// The multiplier of an entry's MF (A7.7, p. 57): "The MF cost of the first Location entered (regardless of phase) by an Encircled unit is doubled
    /// (after all modification)"; 2 for the first Location an Encircled unit enters (its leaving ends its Encirclement), 1 otherwise.
    /// </summary>
    public static int EntryMultiplier(bool encircledFirstLocation) => encircledFirstLocation ? 2 : 1;

    /// <summary>
    /// Whether an Encirclement marker stands (A7.7, p. 57; ruling R12.11): until all the units it Encircles "leave the Location ..., become
    /// berserk/heroic, are eliminated, or are captured"; so while an active, uncaptured, non-Dummy unit it Encircles is left in its Location.
    /// </summary>
    public static bool Stands(IEnumerable<EncircledLocationUnitFacts> unitsThere)
    {
        ArgumentNullException.ThrowIfNull(unitsThere);
        return unitsThere.Any(unit => unit.Active && !unit.Dummy && !unit.Captured && unit.Encircled);
    }
}
