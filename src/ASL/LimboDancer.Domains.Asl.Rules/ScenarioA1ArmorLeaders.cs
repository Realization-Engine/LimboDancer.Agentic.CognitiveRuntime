namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Armor Leaders and Inexperienced Crews (D3.4 to D3.45, p. 200; pass 55, rulings R55.15 to R55.18). An Armor Leader has no counter in play
/// (D3.41): his vehicle carries his Morale Level and his leadership modifier, of the grades 8-1, 9-1, 9-2, and 10-2 (D3.4; the Availability
/// Table of H1.43). An Inexperienced Crew "is treated as if it contains an inherent 6+1 Armor Leader" (D3.45). The modifier is one fact of an
/// attack, negative for an Armor Leader and +1 for an Inexperienced Crew; the crew's Morale Level is one fact of a check.
/// </summary>
public static class ScenarioA1ArmorLeaders
{
    /// <summary>The DRM's name for an Armor Leader (D3.44).</summary>
    public const string LeaderModifier = "armor-leader";

    /// <summary>The DRM's name for an Inexperienced Crew's quasi-Armor Leader (D3.45).</summary>
    public const string InexperiencedModifier = "inexperienced-crew";

    /// <summary>D3.45: the Morale Level of an Inexperienced Crew's inherent 6+1 Armor Leader.</summary>
    public const int InexperiencedMorale = 6;

    /// <summary>D3.45: the leadership modifier of an Inexperienced Crew's inherent 6+1 Armor Leader.</summary>
    public const int InexperiencedLeadership = 1;

    /// <summary>D3.4, H1.43 (pp. 200, 330): "a Strength Factor ranging from 8-1 to 10-2"; the Availability Table lists 8-1, 9-1, 9-2, and 10-2.</summary>
    public static bool IsGrade(int morale, int leadership) => (morale, leadership) is (8, -1) or (9, -1) or (9, -2) or (10, -2);

    /// <summary>
    /// Why a record may not give a unit an Armor Leader (D3.4, D3.41, D3.45; ruling R55.15), or null: he is assigned to a vehicle, in one of the
    /// four grades, and not to one whose crew is Inexperienced, whose 6+1 is the crew's leader. A crew counter that left its vehicle may carry him
    /// too (<paramref name="crew"/>; D3.43; pass 65, ruling R65.20): he ceases to exist while it is a counter, and returns when it re-crews.
    /// </summary>
    public static RecordRefusal? AssignmentRefusal(string unit, bool vehicle, int morale, int leadership, bool inexperiencedCrew, bool crew = false) =>
        !vehicle && !crew ? new RecordRefusal("UNIT-STATE-047", $"'{unit}' is not a vehicle: an Armor Leader is assigned to an AFV (D3.41).")
        : !IsGrade(morale, leadership) ? new RecordRefusal("UNIT-STATE-047", $"'{unit}' names an Armor Leader {Grade(morale, leadership)}: the grades are 8-1, 9-1, 9-2, and 10-2 (D3.4, H1.43).")
        : inexperiencedCrew ? new RecordRefusal("UNIT-STATE-047", $"'{unit}' has an Inexperienced Crew, whose inherent 6+1 is its Armor Leader (D3.45): it takes no other.")
        : null;

    /// <summary>Whether a vehicle is an armed AFV (D3.4, D3.45, D5.1): armored, with a MA, a Gun's or a MG's.</summary>
    public static bool ArmedAfv(bool afv, string? mainArmament, int? caliber) => afv && (mainArmament is not null || caliber is not null);

    /// <summary>
    /// Why a setup may not give a unit an Armor Leader or an Inexperienced Crew (D3.41, D3.45; rulings R55.15, R55.18), or null: either belongs
    /// to an armed AFV.
    /// </summary>
    public static string? SetupBar(string unit, bool armorLeader, bool inexperiencedCrew, bool armedAfv) =>
        armedAfv || (!armorLeader && !inexperiencedCrew) ? null
        : armorLeader ? $"play.setup-armor-leader: {unit} is not an armed AFV, and an Armor Leader is assigned to an AFV (D3.41); give him to a tank or an armed halftrack"
        : $"play.setup-inexperienced-crew: {unit} is not an armed AFV, and only an armed vehicle's crew is Inexperienced (D3.45); give the condition to a tank or an armed halftrack";

    /// <summary>
    /// The leadership modifier a vehicle's crew brings to an attack D3.44 lets it modify (rulings R55.16, R55.18): an Inexperienced Crew's +1,
    /// which "must be used whenever it is optionally usable" (D3.45), else its Armor Leader's; null with neither.
    /// </summary>
    public static int? Leadership(int? leaderLeadership, bool inexperiencedCrew) => inexperiencedCrew ? InexperiencedLeadership : leaderLeadership;

    /// <summary>
    /// The Morale Level a vehicle's Inherent crew checks at when it is not its nationality's (D3.42, D3.45, D5.1; rulings R55.17, R55.18): "the
    /// inherent crew of a vehicle always has the same Morale Level as any Armor Leader in that vehicle"; 6 for an Inexperienced Crew; null with neither.
    /// </summary>
    public static int? CrewMorale(int? leaderMorale, bool inexperiencedCrew) => inexperiencedCrew ? InexperiencedMorale : leaderMorale;

    /// <summary>
    /// D3.44 (p. 200): the modifier of a vehicle's own CC attack; "CC DR" is the vehicle's, and an attack made against the vehicle takes none
    /// (ruling R55.16).
    /// </summary>
    public static int? CloseCombatLeadership(int? leadership, bool byVehicle) => byVehicle ? leadership : null;

    /// <summary>
    /// The DRM line of an Armor Leader or of an Inexperienced Crew (D3.44, D3.45): "can be used only to modify a vehicle's MA DR (To Hit if
    /// ordnance; IFT otherwise), OVR DR, CC DR ... and Bog Removal DR". "Being BU does not void an Armor Leader's effects."
    /// </summary>
    public static FireModifier? Modifier(int? leadership) => leadership switch
    {
        null or 0 => null,
        > 0 and var plus => new FireModifier(InexperiencedModifier, plus, "D3.45"),
        var minus => new FireModifier(LeaderModifier, minus.Value, "D3.44"),
    };

    /// <summary>Whether a DRM's recorded name is an Armor Leader's or an Inexperienced Crew's.</summary>
    public static bool IsModifier(string name) => name is LeaderModifier or InexperiencedModifier;

    /// <summary>
    /// D8.3, D8.31 (p. 209): the drm on the colored dr of a Bog Removal: +1 when Mired, and "one armor leader in either AFV also modifies the
    /// colored dr" (ruling R55.16), an Inexperienced Crew's +1 with it (D3.45). The MP cost is the Original colored dr times the white dr.
    /// </summary>
    public static int BogRemovalDrm(bool mired, int? leadership) => (mired ? 1 : 0) + (leadership ?? 0);

    /// <summary>
    /// The end of a Bog Removal's proposal (D8.3, D8.31, D3.44, D3.45; the table player's review of pass 55): the rules alone, or the Armor
    /// Leader's or the Inexperienced Crew's modifier named as what it is, a modifier of the colored dr, so that "freed on 1 to 4, Mired on 5" is
    /// not read as the Original dr's.
    /// </summary>
    public static string BogRemovalText(int? leadership) => leadership switch
    {
        null or 0 => " (D8.3, D8.31)",
        > 0 and var plus => $"; Inexperienced Crew +{plus} on the colored dr (D3.45, D8.3, D8.31)",
        var minus => $"; Armor Leader {minus} on the colored dr (D3.44, D8.3, D8.31)",
    };

    /// <summary>
    /// Why a vehicle check's record is refused for its Armor Leader (D8.3, D3.44), or null: a Bog Removal names the modifier the state gives
    /// the vehicle, and no other check names one.
    /// </summary>
    public static RecordRefusal? CheckRefusal(bool bogRemoval, int? recorded, int? state) =>
        recorded == (bogRemoval ? state : null) ? null
            : new RecordRefusal("UNIT-STATE-039", "The vehicle check's Armor Leader modifier is not the one its vehicle has: it modifies the colored dr of a Bog Removal alone (D8.3, D3.44).");

    /// <summary>The DRM line in a proposal's or a record's words: "Armor Leader -1 (D3.44)", "Inexperienced Crew +1 (D3.45)"; empty with none.</summary>
    public static string ModifierText(int? leadership) => leadership switch
    {
        null or 0 => string.Empty,
        > 0 and var plus => $"Inexperienced Crew +{plus} (D3.45)",
        var minus => $"Armor Leader {minus} (D3.44)",
    };

    /// <summary>
    /// Whose Morale Level a crew's check names when the vehicle's facts give one (D3.42, D3.45): an Inexperienced Crew's is 6, below every
    /// Armor Leader's grade; null for the nationality's.
    /// </summary>
    public static string? MoraleSource(int? crewMorale) => crewMorale switch
    {
        null => null,
        InexperiencedMorale => InexperiencedModifier,
        _ => LeaderModifier,
    };

    /// <summary>Where a crew's Morale Level comes from, for a check's line: "crew morale 9, its Armor Leader's (D3.42)"; empty for its nationality's.</summary>
    public static string MoraleText(string? moraleSource, int morale) => moraleSource switch
    {
        LeaderModifier => $"crew morale {morale}, its Armor Leader's (D3.42)",
        InexperiencedModifier => $"crew morale {morale}, an Inexperienced Crew's (D3.45)",
        _ => string.Empty,
    };

    /// <summary>An Armor Leader as a label names him: "9-1".</summary>
    public static string Grade(int morale, int leadership) => leadership < 0 ? $"{morale}{leadership}" : $"{morale}+{leadership}";

    /// <summary>
    /// Whether a vehicle loses its Armor Leader (D5.341, D3.43; ruling R55.17): "Recall eliminates any Armor Leader present in that AFV", and he
    /// "ceases to exist once the crew takes counter form", as it does from a wreck or an Abandoned vehicle; an eliminated vehicle has none.
    /// </summary>
    public static bool Lost(bool active, bool recalled, bool abandoned) => !active || recalled || abandoned;

    /// <summary>
    /// D3.41 (p. 200): whether a side's view shows an Armor Leader: his owner's always; the enemy's only once a record has carried his
    /// modifier or his morale, since his "presence is noted on a side record" and his counter "remains out of play until the owner needs to
    /// use his morale/leadership factor".
    /// </summary>
    public static bool Shown(bool enemy, bool revealed) => !enemy || revealed;

    /// <summary>
    /// The vehicles of an attack as its record keeps them (D3.41): a crew's Morale Level stays out of the record unless the attack made that
    /// crew check against it, so that a record the enemy reads does not tell of an Armor Leader who was not used.
    /// </summary>
    public static IReadOnlyList<FireVehicle>? Recorded(IReadOnlyList<FireVehicle>? vehicles, IReadOnlyCollection<string> checkedVehicles)
    {
        ArgumentNullException.ThrowIfNull(checkedVehicles);
        return vehicles is null || vehicles.All(item => item.CrewMorale is null) ? vehicles
            : [.. vehicles.Select(item => item.CrewMorale is not null && !checkedVehicles.Contains(item.VehicleId ?? string.Empty) ? item with { CrewMorale = null } : item)];
    }

    /// <summary>The vehicles of an IFT resolution whose crews took a check.</summary>
    public static IReadOnlyCollection<string> Checked(FireResolution? resolution) =>
        resolution?.VehicleEffects is { } effects ? [.. effects.Where(item => item.CrewCheck is not null).Select(item => item.VehicleId)] : [];

    /// <summary>An IFT attack's facts as its record keeps them (D3.41): a crew's Morale Level only for a vehicle whose crew the attack made check.</summary>
    public static FireAttack Recorded(FireAttack attack, FireResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(resolution);
        var vehicles = Recorded(attack.Vehicles, Checked(resolution));

        // Pass 60: the AFV a FT, MOL, or DC names keeps its crew's Morale Level only when its TC or Specific Collateral Attack made the crew check.
        var named = resolution.VehicleKill is { CrewCheck: not null } || resolution.SpecificCollateral is { Effect.CrewCheck: not null } ? attack.TargetVehicle
            : WithoutMorale(attack.TargetVehicle);
        return ReferenceEquals(vehicles, attack.Vehicles) && ReferenceEquals(named, attack.TargetVehicle) ? attack : attack with
        {
            Vehicles = vehicles,
            TargetVehicle = named,
        };
    }

    /// <summary>
    /// An ordnance shot's facts as its record keeps them (D3.41): the target crew's Morale Level only when the hit made it take its Shock NTC, its
    /// Immobilization TC, or the check of a Specific Collateral Attack (C7.41, D5.5, D.8A), and that of a vehicle in the hit Location only when the hit's IFT attack made its crew check.
    /// </summary>
    public static OrdnanceShot Recorded(OrdnanceShot shot, OrdnanceResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(resolution);
        var target = KillChecked(resolution) ? shot.VehicleTarget : WithoutMorale(shot.VehicleTarget);
        var vehicles = Recorded(shot.Hit?.Vehicles, [.. Checked(resolution.Hit), .. Checked(resolution.CriticalHit), .. (resolution.LevelHits ?? []).SelectMany(Checked)]);
        if (ReferenceEquals(target, shot.VehicleTarget) && ReferenceEquals(vehicles, shot.Hit?.Vehicles))
        {
            return shot;
        }

        var hit = shot.Hit is null ? null : shot.Hit with
        {
            Vehicles = vehicles
        };
        return shot with
        {
            VehicleTarget = target,
            Hit = hit,
        };
    }

    /// <summary>
    /// The vehicles of an attack as a pending choice's resume data keeps them (D3.41; the referee's review of pass 55): the resume is read by
    /// both sides before the attack has made any crew check, so the Morale Level of a crew whose Armor Leader no record has revealed stays out
    /// of it. <paramref name="morale"/> gives those vehicles' Morale Levels when the resolution resumes and null when it stops.
    /// </summary>
    public static IReadOnlyList<FireVehicle>? Pending(IReadOnlyList<FireVehicle>? vehicles, IReadOnlyCollection<string> unrevealed, Func<string, int?>? morale = null)
    {
        ArgumentNullException.ThrowIfNull(unrevealed);
        return vehicles is null || !vehicles.Any(item => unrevealed.Contains(item.VehicleId ?? string.Empty)) ? vehicles
            : [.. vehicles.Select(item => unrevealed.Contains(item.VehicleId ?? string.Empty) ? item with { CrewMorale = morale?.Invoke(item.VehicleId!) } : item)];
    }

    /// <summary>An IFT attack's facts as a pending choice's resume data keeps them, or as the resumed resolution reads them again (D3.41).</summary>
    public static FireAttack Pending(FireAttack attack, IReadOnlyCollection<string> unrevealed, Func<string, int?>? morale = null)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(unrevealed);
        var vehicles = Pending(attack.Vehicles, unrevealed, morale);
        var named = attack.TargetVehicle is { VehicleId: { } id } target && unrevealed.Contains(id) ? target with
        {
            CrewMorale = morale?.Invoke(id)
        } : attack.TargetVehicle;
        return ReferenceEquals(vehicles, attack.Vehicles) && ReferenceEquals(named, attack.TargetVehicle) ? attack : attack with
        {
            Vehicles = vehicles,
            TargetVehicle = named,
        };
    }

    /// <summary>The same for an ordnance shot: its target's crew and the crews of the vehicles in the Location its hit attacks.</summary>
    public static OrdnanceShot Pending(OrdnanceShot shot, IReadOnlyCollection<string> unrevealed, Func<string, int?>? morale = null)
    {
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(unrevealed);
        var target = shot.VehicleTarget;
        if (target is { VehicleId: { } id } && unrevealed.Contains(id))
        {
            target = target with
            {
                CrewMorale = morale?.Invoke(id)
            };
        }

        var vehicles = Pending(shot.Hit?.Vehicles, unrevealed, morale);
        if (ReferenceEquals(target, shot.VehicleTarget) && ReferenceEquals(vehicles, shot.Hit?.Vehicles))
        {
            return shot;
        }

        var hit = shot.Hit is null ? null : shot.Hit with
        {
            Vehicles = vehicles
        };
        return shot with
        {
            VehicleTarget = target,
            Hit = hit,
        };
    }

    /// <summary>
    /// A shot as the other side may read of it before it is fired (D3.41; rulings R55.15, R55.29): a proposal, a choice's text, or a summary
    /// worded for the side that does not own the firing vehicle leaves out the line of an Armor Leader no record has revealed. The shot itself
    /// is fired with his modifier, and its record reveals him. An Inexperienced Crew's +1 is public and stays.
    /// </summary>
    public static OrdnanceShot ToldToTheEnemy(OrdnanceShot shot, bool leaderRevealed)
    {
        ArgumentNullException.ThrowIfNull(shot);
        return leaderRevealed || shot.Vehicle is not { ArmorLeadership: < 0 } led ? shot : shot with
        {
            Vehicle = led with
            {
                ArmorLeadership = null
            }
        };
    }

    private static OrdnanceVehicleTarget? WithoutMorale(OrdnanceVehicleTarget? target) => target is not { CrewMorale: not null } ? target : target with
    {
        CrewMorale = null
    };

    // D.8A (pass 60): the crew's check in a Specific Collateral Attack is a check of the target's crew too.
    private static bool KillChecked(OrdnanceResolution resolution) =>
        resolution.Kill is { ShockCheck: not null } or { CrewCheck: not null } || resolution.SpecificCollateral is { Effect.CrewCheck: not null };

    /// <summary>
    /// The state's vehicles compared with a record's (D3.41): a crew's Morale Level the record left out is left out of the comparison; one the
    /// record names must be the state's. The record's refusal below then holds a check to it.
    /// </summary>
    public static IReadOnlyList<FireVehicle>? AsRecorded(IReadOnlyList<FireVehicle>? state, IReadOnlyList<FireVehicle>? recorded) =>
        state is null || recorded is null || state.Count != recorded.Count || state.All(item => item.CrewMorale is null) ? state
            : [.. state.Zip(recorded, (fact, record) => fact.CrewMorale is not null && record.CrewMorale is null && fact.VehicleId == record.VehicleId ? fact with { CrewMorale = null } : fact)];

    /// <summary>The state's vehicle target compared with a record's (D3.41), in the same way.</summary>
    public static OrdnanceVehicleTarget? AsRecorded(OrdnanceVehicleTarget? state, OrdnanceVehicleTarget? recorded) =>
        recorded is { CrewMorale: null } ? WithoutMorale(state) : state;

    /// <summary>
    /// Why a record's crew checks are refused (D3.42, D3.45), or null: a record that leaves a crew's Morale Level out while the state gives the
    /// vehicle an Armor Leader or an Inexperienced Crew may not hold a check of that crew, which would be made at its nationality's morale.
    /// </summary>
    public static string? RecordRefusal(IReadOnlyList<FireVehicle>? state, IReadOnlyList<FireVehicle>? recorded, IReadOnlyCollection<string> checkedVehicles)
    {
        ArgumentNullException.ThrowIfNull(checkedVehicles);
        return (state ?? []).Any(fact => fact.CrewMorale is not null && checkedVehicles.Contains(fact.VehicleId ?? string.Empty)
            && (recorded ?? []).FirstOrDefault(record => record.VehicleId == fact.VehicleId) is not { CrewMorale: not null }) ? UnrecordedMorale : null;
    }

    /// <summary>The same for an ordnance record: its target's Shock NTC or Immobilization TC, and the crews its hit made check.</summary>
    public static string? RecordRefusal(OrdnanceShot state, OrdnanceShot recorded, OrdnanceResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(recorded);
        ArgumentNullException.ThrowIfNull(resolution);
        return KillChecked(resolution) && state.VehicleTarget is { CrewMorale: not null } && recorded.VehicleTarget is not { CrewMorale: not null } ? UnrecordedMorale
            : RecordRefusal(state.Hit?.Vehicles, recorded.Hit?.Vehicles, [.. Checked(resolution.Hit), .. Checked(resolution.CriticalHit)]);
    }

    /// <summary>The same for the AFV a FT, MOL, or DC named (pass 60): its crew's TC or Specific Collateral Attack check.</summary>
    public static string? RecordRefusal(OrdnanceVehicleTarget? state, OrdnanceVehicleTarget? recorded, FireResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        return (resolution.VehicleKill is { CrewCheck: not null } || resolution.SpecificCollateral is { Effect.CrewCheck: not null })
            && state is { CrewMorale: not null } && recorded is not { CrewMorale: not null } ? UnrecordedMorale : null;
    }

    private const string UnrecordedMorale = "The record's crew check is not made at the Morale Level of the vehicle's Armor Leader or Inexperienced Crew (D3.42, D3.45).";
}
