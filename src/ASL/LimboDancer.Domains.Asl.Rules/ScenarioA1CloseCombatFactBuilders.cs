namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The facts of a live CC round, Ambush, or attack with a vehicle as the state decides them (unit step 29; pass 32.g, S6): which units are CC units, how a
/// unit's conditions read, the phase's name, the round that follows the Location's CC so far (A11.32, A11.33, A11.4), Hand-to-Hand for the CCPh, and a
/// vehicle's states. Play reads the state and the map and hands the facts over; these build the package's records.
/// </summary>
public static class ScenarioA1CloseCombatFactBuilders
{
    /// <summary>The Close Combat package reads one catalog at one version.</summary>
    public static string? CatalogBar(string catalog, string version, string expectedCatalog, string expectedVersion) =>
        catalog != expectedCatalog || version != expectedVersion
            ? $"play.cc-catalog: the Close Combat package reads {expectedCatalog}@{expectedVersion}, and this game uses {catalog}@{version}"
            : null;

    /// <summary>A11.19 (ruling R14.2): a CC unit is an active unit that is not a Dummy; Dummies are removed before any attack is declared.</summary>
    public static bool IsCcUnit(bool active, bool dummy) => active && !dummy;

    /// <summary>A11.19: a unit outside the catalog cannot be read.</summary>
    public static string OutsideCatalogText() => "play.cc-units: the Location holds a unit outside the catalog (A11.19)";

    /// <summary>
    /// A unit of a CC Location as the package reads it (rulings R14.3, R14.5, R14.8): concealed or hidden counts as concealed, TI, Unarmed, and CX are set
    /// only when true, a prisoner's Guard is its custodian, and its SW are the possessed ones (none when it has none).
    /// </summary>
    public static CloseCombatUnit Unit(string id, string definition, string side, bool broken, bool pinned, bool wounded, bool disrupted, bool berserk, bool fanatic, bool heroic,
        bool concealed, bool hidden, bool captured, bool advanced, bool melee, string? stackedWith, string? withdrawingTo, bool cx, bool ti, bool unarmed, string? custodian,
        string? infiltrateTo, IReadOnlyList<string> weapons)
    {
        ArgumentNullException.ThrowIfNull(weapons);
        return new CloseCombatUnit(id, definition, side, broken, pinned, wounded, disrupted, berserk, fanatic, heroic, concealed || hidden, captured, advanced, melee)
        {
            StackedWith = stackedWith,
            WithdrawingTo = withdrawingTo,
            Cx = cx ? true : null,
            Ti = ti ? true : null,
            Unarmed = unarmed ? true : null,
            GuardId = captured ? custodian : null,
            InfiltrateTo = infiltrateTo,
            Weapons = weapons.Count > 0 ? weapons : null,
        };
    }

    /// <summary>The phase's name in the package's records: the CCPh by name, any other phase as the state names it.</summary>
    public static string? PhaseName(string? phase) => phase == "ccph" ? "CCPh" : phase;

    /// <summary>The phase's name in a vehicle CC record: the CCPh and the MPh by name, any other phase as the state names it.</summary>
    public static string? VehiclePhaseName(string? phase) => phase == "ccph" ? "CCPh" : phase == "mph" ? "MPh" : phase;

    /// <summary>A11.4 (ruling R14.2): the Ambush facts of a Location before the drs; a hidden unit placed there as the CCPh began makes an Ambush possible.</summary>
    public static AmbushFacts Ambush(string? phase, string locationText, string? terrain, string? phasingSide, IReadOnlyList<CloseCombatUnit> units, bool hiddenPlaced) =>
        new(PhaseName(phase), locationText, terrain, phasingSide, units, null)
        {
            HiddenPlaced = hiddenPlaced ? true : null,
        };

    /// <summary>A4.152 (ruling R27.3): the CC of an Infantry OVR follows its entry at once in the MPh, as one simultaneous round with no Ambush.</summary>
    public static CloseCombatFacts Overrun(string locationText, string? terrain, string? phasingSide, IReadOnlyList<CloseCombatUnit> units, IReadOnlyList<CloseCombatDeclaration> attacks) =>
        new("MPh", locationText, terrain, phasingSide, CloseCombatFacts.Simultaneous, null, units, [], [], attacks, null)
        {
            InfantryOverrun = true,
        };

    /// <summary>A11.4: where no CC has begun and an Ambush can occur, the Ambush drs come first.</summary>
    public static string? AmbushFirstBar(bool ccBegun, bool ambushPossible, string locationText) =>
        !ccBegun && ambushPossible ? $"play.cc-ambush-first: Infantry advanced into CC in {locationText}, so the Ambush drs come first (A11.4)" : null;

    /// <summary>
    /// A11.32, A11.3, A11.33, A11.34 (ruling R14.6): the round that follows the Location's CC so far: the prisoners' escape round when asked for before any
    /// other; one simultaneous round with no Ambush; after an Ambush the ambusher's round while an ambusher is there and no round has been resolved, or when
    /// it is asked for again; else the ambushed side's. With it, the rounds resolved other than the prisoners'.
    /// </summary>
    public static (string Round, int Rounds) NextRound(string? requested, IReadOnlyList<string> recorded, string? ambusher, bool ambusherPresent)
    {
        ArgumentNullException.ThrowIfNull(recorded);
        var rounds = recorded.Count(item => item != CloseCombatFacts.PrisonersRound);
        var ambushers = ambusher is not null && ambusherPresent;
        var round = requested == CloseCombatFacts.PrisonersRound && recorded.Count == 0 ? CloseCombatFacts.PrisonersRound
            : ambusher is null ? CloseCombatFacts.Simultaneous
            : ambushers && (rounds == 0 || requested == CloseCombatFacts.AmbusherRound) ? CloseCombatFacts.AmbusherRound
            : CloseCombatFacts.AmbushedRound;
        return (round, rounds);
    }

    /// <summary>J2.31 (ruling R14.1): the Location's first round other than the prisoners' declares Hand-to-Hand for the CCPh; later rounds keep it.</summary>
    public static bool HandToHand(int rounds, bool entryHandToHand, string round, bool declared) => rounds > 0 ? entryHandToHand : round != CloseCombatFacts.PrisonersRound && declared;

    /// <summary>The facts of the next CC round in a Location, before the rolls.</summary>
    public static CloseCombatFacts Round(string? phase, string locationText, string? terrain, string? phasingSide, string round, string? ambusher, IReadOnlyList<CloseCombatUnit> units,
        IReadOnlyList<string> attacked, IReadOnlyList<string> attacking, IReadOnlyList<CloseCombatDeclaration> attacks, bool handToHand) =>
        new(PhaseName(phase), locationText, terrain, phasingSide, round, ambusher, units, attacked, attacking, attacks, null)
        {
            HandToHand = handToHand ? true : null,
        };

    /// <summary>A CC attack with a vehicle names an active vehicle in the Location.</summary>
    public static string VehicleNotActiveText(string vehicleId, string locationText) => $"play.cc-vehicle: {vehicleId} is not an active vehicle in {locationText}";

    /// <summary>A11.51: a vehicle moving in the MPh is Non-Stopped: in a reaction, when its move has started and not stopped and it is a member of the moving stack.</summary>
    public static bool VehicleMoving(bool reaction, bool moveStartedNotStopped, bool member) => reaction && moveStartedNotStopped && member;

    /// <summary>
    /// The vehicle's states as the package groups them (rulings R11.13 to R11.15): in Motion or moving, immobile (Immobilized or Bogged), stunned (Stunned or
    /// Recalled), shocked (Shocked or an Unconfirmed Kill), Abandoned, its MA malfunctioned or disabled, its BMG and CMG malfunctioned.
    /// </summary>
    public static VehicleCloseCombatVehicle Vehicle(string id, string definition, string side, bool crewExposed, bool motion, bool moving, bool immobilized, bool bogged, bool stunned,
        bool recalled, bool shocked, bool unconfirmedKill, bool abandoned, bool malfunctioned, bool disabled, bool bmgMalfunctioned, bool cmgMalfunctioned,
        bool stunRecovery = false) =>
        new(id, definition, side, crewExposed, motion || moving, immobilized || bogged, stunned || recalled, shocked || unconfirmedKill, abandoned, malfunctioned || disabled,
            bmgMalfunctioned, cmgMalfunctioned)
        {
            StunRecovery = stunRecovery ? true : null,
        };

    /// <summary>D7.213: in CC Reaction Fire, the attackers already marked First or Final Fire are named; none otherwise.</summary>
    public static IReadOnlyList<string>? FireMarked(bool reaction, IReadOnlyList<string> marked)
    {
        ArgumentNullException.ThrowIfNull(marked);
        return reaction && marked.Count > 0 ? marked : null;
    }

    /// <summary>The facts of one CC attack with a vehicle, before the rolls: the Infantry of the Location, the vehicle, the attackers or defenders, and whether it is CC Reaction Fire.</summary>
    public static VehicleCloseCombatFacts VehicleAttack(string? phase, string locationText, VehicleCloseCombatVehicle vehicle, IReadOnlyList<CloseCombatUnit> infantry,
        IReadOnlyList<string> attackers, IReadOnlyList<string> defenders, bool byVehicle, bool reaction, IReadOnlyList<string>? fireMarked) =>
        new(VehiclePhaseName(phase), locationText, vehicle, infantry, byVehicle ? [] : attackers, byVehicle ? defenders : [], byVehicle, reaction, null)
        {
            FireMarked = fireMarked,
        };
}
