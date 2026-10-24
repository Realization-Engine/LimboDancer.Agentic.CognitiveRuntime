namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A unit in the Location of a vehicle that may be captured (A21.2, A11.52): its side, whether it is Personnel on foot, in Good Order, and a SMC.</summary>
public sealed record CaptureUnitFacts(string Id, string Side, bool Personnel, bool GoodOrder, bool Smc);

/// <summary>
/// Captured vehicles (A21.2, A21.21, A21.22, D5.343, A26.12, pp. 88, 89, 99, 204; pass 65, ruling R65.22): an Abandoned AFV is captured at the end of the
/// CCPh, or by a capture attempt in CC; an unarmed vehicle runs for its captor with a Temporary Driver; a Temporary crew mans a vehicle under the
/// penalties of who it is; a SMC crew is Wounded on a Stun and eliminated on a Recall. Play reads the Location, the units, and the vehicle, and writes
/// the events.
/// </summary>
public static class ScenarioA1CapturedVehicleRules
{
    /// <summary>The causes of a capture, as the <c>vehicle-captured</c> record names them.</summary>
    public const string Automatic = "automatic";
    public const string CloseCombat = "cc";
    public const string Unarmed = "unarmed";

    /// <summary>A21.22: how a Temporary crew mans a vehicle, as the <c>vehicle-recrewed</c> record names it.</summary>
    public const string VehicleCrew = "vehicle-crew";
    public const string InfantryCrew = "infantry-crew";
    public const string HalfSquad = "half-squad";
    public const string Smc = "smc";

    /// <summary>
    /// A21.2 (p. 88): "If there are no enemy Personnel in the Location with an Abandoned enemy AFV, it may be captured automatically by any Infantry unit in
    /// the same location at the end of a CCPh." The side that captures an Abandoned AFV as the CCPh ends: the one side with Good Order Infantry there, when
    /// no Personnel of the vehicle's side is; null otherwise. An unarmed vehicle (A11.52) and a vehicle captured before the pass (ruling R11.16) are not.
    /// </summary>
    public static string? AutomaticCaptor(string vehicleSide, bool armed, bool abandoned, bool capturedBefore, IReadOnlyList<CaptureUnitFacts> here)
    {
        ArgumentNullException.ThrowIfNull(here);
        if (!armed || !abandoned || capturedBefore || here.Any(unit => unit.Side == vehicleSide && unit.Personnel))
        {
            return null;
        }

        return here.Where(unit => unit.Side != vehicleSide && unit.Personnel && unit.GoodOrder).Select(unit => unit.Side).Distinct(StringComparer.Ordinal).ToArray() is [var side]
            ? side : null;
    }

    /// <summary>A21.2: "at the end of a CCPh": the phase that is ending is the CCPh.</summary>
    public static bool AutomaticCaptureDue(string? endingPhase) => endingPhase == "ccph";

    /// <summary>
    /// A21.21 (p. 88): "An unarmed vehicle which is captured takes on a new inherent driver at no cost to a MMC captor ... If the vehicle was captured by a
    /// SMC, that SMC is removed until the vehicle takes on a new crew (D5.42) or is Abandoned." The SMC removed into the vehicle as its Temporary Driver
    /// (the first by id) when every captor is a SMC; null when a MMC is among them.
    /// </summary>
    public static string? TemporaryDriver(IReadOnlyList<CaptureUnitFacts> captors)
    {
        ArgumentNullException.ThrowIfNull(captors);
        return captors.Count == 0 || captors.Any(unit => !unit.Smc) ? null : captors.Select(unit => unit.Id).Order(StringComparer.Ordinal).First();
    }

    /// <summary>
    /// A11.52, A21.2: "Infantry may not attempt to capture a manned AFV ... but may attempt to capture or eliminate an Abandoned AFV as per 21.2." Why a CC
    /// capture attempt on a vehicle is refused, or null.
    /// </summary>
    public static string? CaptureAttemptBar(string vehicle, bool armed, bool abandoned) =>
        !armed || !abandoned ? $"play.cc-vehicle: only an Abandoned AFV is the target of a capture attempt; {vehicle} is not one, so attack it to eliminate it (A11.52, A21.2)" : null;

    /// <summary>A21.2: a capture attempt is made in the CCPh; in the MPh only CC Reaction Fire attacks a vehicle (D7.21). Why it is refused then, or null.</summary>
    public static string? CaptureAttemptPhaseBar(string? phase) =>
        phase == "mph" ? "play.cc-vehicle: a capture attempt is made in the CCPh, not as CC Reaction Fire (A21.2)" : null;

    /// <summary>
    /// A21.2, A21.22 (the referee's review of pass 65): whether a vehicle is captured, from the record of its capture: it belongs to another side than the
    /// one it was captured from. A vehicle that no <c>vehicle-captured</c> record took (<paramref name="capturedFrom"/> null), or that its own side took
    /// back, is not; nor is a friendly vehicle of another nationality.
    /// </summary>
    public static bool Captured(string? capturedFrom, string? side) => capturedFrom is not null && capturedFrom != side;

    /// <summary>
    /// A21.22 (p. 88), D5.42: how a unit entering a vehicle to crew it mans it, or null for an Inherent crew: the vehicle's own crew, and a vehicle or
    /// infantry crew of a vehicle not captured. In a captured vehicle (<see cref="Captured"/>) a vehicle crew, an infantry crew, a HS, or a SMC is a
    /// Temporary crew; in a friendly one a MMC other than a crew, or a SMC, is (ruling R65.20).
    /// </summary>
    public static string? Manning(string? kind, bool smc, bool ownCrew, bool captured) =>
        ownCrew ? null
        : smc ? Smc
        : kind == "asl:vehicle-crew" ? captured ? VehicleCrew : null
        : ScenarioA1Definitions.IsCrew(kind) ? captured ? InfantryCrew : null
        : HalfSquad;

    /// <summary>
    /// A21.22: "takes in a new inherent crew by removing the MMC that entered it (or exchanging a capturing squad for a HS)". The exchange is not built: a
    /// squad that would be a Temporary crew is refused, and it Deploys first so that a HS enters.
    /// </summary>
    public static string? SquadBar(string unit, string vehicle, string? kind, string? manning) =>
        kind == "asl:squad" && manning == HalfSquad
            ? $"play.recrew: {unit} is a squad, and a squad crews {vehicle} as a HS; it Deploys first, and a HS enters (A21.22, A1.31)"
            : null;

    /// <summary>
    /// A21.22: "Should it be manned by a now-removed HS or Infantry crew counter, its MP allotment is halved." A vehicle's allotment in half MP with its
    /// Temporary crew's penalty: the printed MP once less (an ESB's MP are not halved).
    /// </summary>
    public static int AllotmentHalfMp(int allotmentHalfMp, int printedMp, string? manning) => Halved(manning) ? allotmentHalfMp - printedMp : allotmentHalfMp;

    private static bool Halved(string? manning) => manning is InfantryCrew or HalfSquad;

    /// <summary>
    /// A21.22: "each attempt to start the vehicle must be preceded by a dr ≤ 4. A dr of 5 results in no movement allowed during this phase ... a dr ≥ 6
    /// results in Bog in that hex. A Russian/Japanese captor receives a +1 drm to this dr." Whether the vehicle's Temporary crew makes the dr, and its drm.
    /// </summary>
    public static bool StartDrRequired(string? manning) => Halved(manning);

    public static int StartDrm(string? crewNationality) => crewNationality is "russian" or "japanese" ? 1 : 0;

    /// <summary>A21.22: the start dr's no-movement result, like a Bog, ends the vehicle's move once the DEFENDER passes.</summary>
    public static bool StartStops(string result) => result == ScenarioA1ResultTables.CheckNoMove;

    /// <summary>A21.22: "Should the vehicle be manned by a now-removed vehicular crew counter, normal captured weapon (21.11-.12) and red MP (D2.51) penalties apply."</summary>
    public static bool RedMp(string? manning, bool captured) => manning == VehicleCrew && captured;

    /// <summary>
    /// A21.22, A21.11 to A21.13: the captured-weapon penalty of the vehicle's weapons, as a multiple: one for a vehicle crew or an infantry crew in a captured
    /// vehicle, and for a SMC ("captured weapon penalties do apply"); two for a HS in a captured vehicle ("doubled captured weapon penalties (21.13)"); one for
    /// a non-qualified friendly HS (A21.13); null for none. Its B# is lowered two each, its Multiple ROF one each, and its ordnance fires on the red To Hit
    /// numbers with Case H's +2 each.
    /// </summary>
    public static int? WeaponPenalty(string? manning, bool captured) => manning switch
    {
        VehicleCrew => captured ? 1 : null,
        InfantryCrew or Smc => 1,
        HalfSquad => captured ? 2 : 1,
        _ => null,
    };

    /// <summary>
    /// A21.22: "A vehicle captured by a SMC is both Immobile and limited to AAMG attacks while he is CE in an AFV. If not CE the vehicle may move, or fire
    /// either its BMG or CMG [EXC: CMG/MA if an AFV with a one man turret; D1.322], but may never both move and fire in the same Player Turn." Why a SMC
    /// crew may not fire this weapon now, or null. <paramref name="weapon"/> is <c>aamg</c>, <c>bmg</c>, <c>cmg</c>, or <c>ma</c>.
    /// </summary>
    public static string? SmcFireBar(string vehicle, bool crewExposed, string weapon, bool oneManTurret, bool movedThisPlayerTurn) =>
        crewExposed && weapon != "aamg" ? $"play.fire: {vehicle}'s crew is a SMC, who fires only its AAMG while CE (A21.22)"
        : !crewExposed && weapon is not ("bmg" or "cmg") && !(oneManTurret && weapon == "ma")
            ? $"play.fire: {vehicle}'s crew is a SMC, who fires only its BMG or CMG while BU{(oneManTurret ? ", or its MA in a one man turret" : string.Empty)} (A21.22)"
        : movedThisPlayerTurn ? $"play.fire: {vehicle}'s SMC crew has moved it this Player Turn, and may never both move and fire in one (A21.22)"
        : null;

    /// <summary>A21.22: why a SMC crew may not start its vehicle now, or null: it is Immobile while he is CE, and never moves after firing in the Player Turn.</summary>
    public static string? SmcMoveBar(string vehicle, bool crewExposed, bool firedThisPlayerTurn) =>
        crewExposed ? $"play.move-vehicle: {vehicle}'s crew is a SMC, and the vehicle is Immobile while he is CE; he buttons up to move it (A21.22)"
        : firedThisPlayerTurn ? $"play.move-vehicle: {vehicle}'s SMC crew has fired this Player Turn, and may never both move and fire in one (A21.22)"
        : null;

    /// <summary>
    /// D5.343 (p. 204): "A SMC acting as a Temporary crew (A21.22) also becomes Wounded if he suffers a Stun result (even while in a 1MT AFV). If he suffers
    /// Recall [EXC: due to Disabled MA (3.7); Stunned in a 1MT AFV (5.341)] he is eliminated." What a Stun or Recall the vehicle just took does to its SMC
    /// crew; an eliminated SMC leaves the vehicle Abandoned, unmanned. A17.2 (the referee's review): a SMC already Wounded that is Wounded again is
    /// eliminated.
    /// </summary>
    public static (bool Wounded, bool Eliminated) SmcCrewOutcome(bool stunned, bool recalled, bool byDisabledMa, bool oneManTurret, bool alreadyWounded = false)
    {
        var eliminated = recalled && !byDisabledMa && !(stunned && oneManTurret);
        var wounded = !eliminated && (stunned || (recalled && oneManTurret && !byDisabledMa));
        return (wounded && !alreadyWounded, eliminated || (wounded && alreadyWounded));
    }

    /// <summary>The record's text for a capture (A21.2, A21.21): "[G4]: the PzKpfw IIIH is captured by the Russian side (A21.2)".</summary>
    public static string CapturedText(string vehicle, string side, string at, string cause, string? driver) => cause switch
    {
        Unarmed => $"play.vehicle-captured: {vehicle} in {at} is unarmed and alone with the {side} side's Infantry, so it is captured (A11.52); "
            + (driver is null ? "a Temporary Driver runs it for its MMC captor at no cost (A21.21)" : $"{driver} is removed into it as its Temporary Driver until it takes on a new crew or is Abandoned (A21.21)"),
        CloseCombat => $"play.vehicle-captured: {vehicle} in {at} is captured by the {side} side's capture attempt (A21.2); it stays Abandoned until a crew enters it (D5.42, A21.22)",
        _ => $"play.vehicle-captured: {vehicle} in {at} is captured by the {side} side at the end of the CCPh, with no Personnel of its own side there (A21.2); it stays "
            + "Abandoned until a crew enters it (D5.42, A21.22)",
    };

    /// <summary>The proposal's text for a Temporary crew's penalties (A21.22, D5.343).</summary>
    public static string ManningText(string manning, bool captured) => manning switch
    {
        VehicleCrew => "a Temporary crew, an enemy vehicle crew: its weapons take the captured-weapon penalties and its MP are red (A21.22, A21.11, A21.12, D2.51)",
        InfantryCrew => "a Temporary crew, an infantry crew: its MP allotment is halved, each start needs a dr of 4 or less, and its weapons take the captured-weapon penalties (A21.22)",
        HalfSquad => "a Temporary crew, a HS: its MP allotment is halved, each start needs a dr of 4 or less, and its weapons take "
            + (captured ? "doubled captured-weapon penalties (A21.22, A21.13)" : "the non-qualified use penalties (A21.22, A21.13)"),
        _ => "a Temporary crew, a SMC: Immobile and limited to its AAMG while CE, never moving and firing in one Player Turn, Wounded by a Stun and eliminated by a Recall (A21.22, D5.343)",
    };
}
