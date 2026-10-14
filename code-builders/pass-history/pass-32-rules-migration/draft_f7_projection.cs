namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The projector's rout, Rally, Repair, Shock, surrender, and Massacre decisions (pass 32.f, S5): a record's legality and its agreement with its dice,
/// as refusals with their codes, and the state rules a record applies. The projector reads the state and rewrites it; these take plain values and
/// decide. Where the planner's copy of a rule differs (the pass 32 design, section 12), the projector's stays its own, named AsRecorded.
/// </summary>
public static class ScenarioA1RoutRallyProjection
{
    /// <summary>A10.6 (UNIT-STATE-027): a Rally attempt is made in the RPh; the projector asks before it reads the unit.</summary>
    public static bool RallyPhase(string? phase) => phase == "rph";

    /// <summary>A10.6 (UNIT-STATE-027): the attempt is not in the RPh, or its unit is not active.</summary>
    public static RecordRefusal RallyRefusal() => new("UNIT-STATE-027", "A Rally attempt is made in the RPh by an active unit (A10.6).");

    /// <summary>A10.6, A3.1 (UNIT-STATE-027): a unit rallies once per Player Turn, and not after repairing this RPh.</summary>
    public static RecordRefusal? VerifyRallyOnce(string unitId, bool attemptedThisPlayerTurn, bool repairedThisPhase) =>
        attemptedThisPlayerTurn || repairedThisPhase ? new RecordRefusal("UNIT-STATE-027", $"'{unitId}' already attempted to rally this Player Turn, or to repair this RPh (A10.6, A3.1).") : null;

    /// <summary>A18.11: a phasing MMC's attempt takes its side's first MMC Rally while that is open.</summary>
    public static bool TakesFirstMmcRally(bool mmc, bool phasing, bool firstMmcRallyTaken) => mmc && phasing && !firstMmcRallyTaken;

    /// <summary>
    /// D3.7 (ruling R6.10; UNIT-STATE-028), as the projector has it: a vehicle's malfunctioned MG, not disabled, is repaired once per RPh by its crew when
    /// the vehicle is not buttoned up, Stunned, Recalled, Shocked, or an Unconfirmed Kill. The planner's CE test differs (section 12).
    /// </summary>
    public static bool VehicleRepairAllowedAsRecorded(string? phase, bool malfunctioned, bool disabled, bool buttonedUp, bool stunned, bool recalled, bool shocked, bool unconfirmedKill,
        bool repairedThisPhase) =>
        !(phase != "rph" || !malfunctioned || disabled || buttonedUp || stunned || recalled || shocked || unconfirmedKill || repairedThisPhase);

    /// <summary>D3.7 (UNIT-STATE-028): the vehicle MG repair's refusal.</summary>
    public static RecordRefusal VehicleRepairRefusal() => new("UNIT-STATE-028", "A vehicle's malfunctioned MG is repaired once per RPh by its CE crew that is not Stunned or Recalled (D3.7).");

    /// <summary>D3.7 (UNIT-STATE-028): the record's Repair Number is 1 and its result is the dr's.</summary>
    public static bool VehicleRepairRecordAgrees(int repairNumber, bool resultAgrees) => repairNumber == 1 && resultAgrees;

    /// <summary>D3.7 (UNIT-STATE-028): the record disagrees with the vehicle MG's dr.</summary>
    public static RecordRefusal VehicleRepairDisagrees() => new("UNIT-STATE-028", "The Repair record disagrees with the vehicle MG's dr (D3.7).");

    /// <summary>A9.72 (UNIT-STATE-028): a SW is repaired in the RPh; the projector asks before it reads the unit and the SW.</summary>
    public static bool SwRepairPhase(string? phase) => phase == "rph";

    /// <summary>A9.72 (UNIT-STATE-028): the Repair is not in the RPh, or not on a malfunctioned SW its unit possesses.</summary>
    public static RecordRefusal SwRepairRefusal() => new("UNIT-STATE-028", "A Repair is attempted in the RPh on a malfunctioned SW its unit possesses (A9.72).");

    /// <summary>A3.1 (UNIT-STATE-028): a unit takes one kind of action in the RPh, so one that attempted to rally does not also repair.</summary>
    public static RecordRefusal? VerifySwRepairOnce(string unitId, bool attemptedThisPlayerTurn) =>
        attemptedThisPlayerTurn ? new RecordRefusal("UNIT-STATE-028", $"'{unitId}' attempted to rally this RPh, so it may not also repair (A3.1).") : null;

    /// <summary>A9.72 (UNIT-STATE-028): the record's Repair Number is the printed one and its result is the dr's.</summary>
    public static bool SwRepairRecordAgrees(int? printedRepairNumber, int recordedRepairNumber, bool resultAgrees) => printedRepairNumber == recordedRepairNumber && resultAgrees;

    /// <summary>A9.72 (UNIT-STATE-028): the record disagrees with the SW's Repair Number or its dr.</summary>
    public static RecordRefusal SwRepairDisagrees() => new("UNIT-STATE-028", "The Repair record disagrees with the SW's Repair Number or its dr (A9.72).");

    /// <summary>C7.42 (ruling R7.8; UNIT-STATE-038): one dr per RPh for a Shocked AFV or an Unconfirmed Kill; the phase is asked before the vehicle is read.</summary>
    public static bool ShockRecoveryAllowed(bool vehicle, bool rolledThisPhase, bool shocked, bool unconfirmedKill) => vehicle && !rolledThisPhase && (shocked || unconfirmedKill);

    /// <summary>C7.42 (UNIT-STATE-038): the Shock recovery's refusal.</summary>
    public static RecordRefusal ShockRecoveryRefusal() => new("UNIT-STATE-038", "A Shocked AFV or an Unconfirmed Kill makes one dr in the RPh (C7.42).");

    /// <summary>C7.42 (UNIT-STATE-038): the recorded result follows from the dr.</summary>
    public static bool ShockRecoveryAgrees(string recorded, bool unconfirmedKill, int dr) => recorded == ScenarioA1ResultTables.ShockRecovery(unconfirmedKill, dr);

    /// <summary>C7.42 (UNIT-STATE-038): the record disagrees with its dr.</summary>
    public static RecordRefusal ShockRecoveryDisagrees() => new("UNIT-STATE-038", "The Shock recovery record disagrees with its dr (C7.42).");

    /// <summary>A10.5 (UNIT-STATE-041): a rout step and an Interdiction are in the RtPh; the projector asks before it reads the unit.</summary>
    public static bool RoutPhase(string? phase) => phase == "rtph";

    /// <summary>
    /// A10.5, A10.52 (UNIT-STATE-041): a rout step moves a broken unit not in Melee or pinned, within its MF (in halves), unless by Low Crawl, which is its
    /// only step.
    /// </summary>
    public static bool RoutStepAllowed(bool broken, bool melee, bool pinned, int halfMf, bool lowCrawl, int mfSpent, bool halfMfSpent, int routHalfMf, bool routedThisPhase) =>
        !(!broken || melee || pinned || halfMf < 0 || (!lowCrawl && (mfSpent * 2) + (halfMfSpent ? 1 : 0) + halfMf > routHalfMf) || (lowCrawl && routedThisPhase));

    /// <summary>A10.5, A10.52 (UNIT-STATE-041): the rout step's refusal.</summary>
    public static RecordRefusal RoutStepRefusal() => new("UNIT-STATE-041", "A rout step moves a broken unit not in Melee or pinned in the RtPh, within its MF (A10.5, A10.52).");

    /// <summary>A10.5: the unit's MF after a rout step, counted in halves.</summary>
    public static (int MfSpent, bool HalfMfSpent) RoutMfSpent(int mfSpent, bool halfMfSpent, int halfMf)
    {
        var spent = (mfSpent * 2) + (halfMfSpent ? 1 : 0) + halfMf;
        return (spent / 2, spent % 2 == 1);
    }

    /// <summary>A10.5, as the projector has it: a broken unit has six MF in the RtPh, a wounded leader or hero three; the planner counts any wounded SMC (section 12).</summary>
    public static int RoutHalfMfAsRecorded(bool leaderOrHero, bool wounded) => leaderOrHero && wounded ? 6 : 12;

    /// <summary>A10.53 (ruling R13.3; UNIT-STATE-041): an Interdiction NMC is taken by a unit that routed this phase.</summary>
    public static bool InterdictionByRoutingUnit(bool routedThisPhase) => routedThisPhase;

    /// <summary>A10.53 (UNIT-STATE-041): the record's result agrees with its DR, DRM, and Morale Level.</summary>
    public static bool InterdictionAgrees(int first, int second, int drm, int morale, string result) =>
        ScenarioA1ResultTables.RoutInterdiction(first + second, first + second + drm, morale) == result;

    /// <summary>A10.53 (UNIT-STATE-041): the Interdiction's refusal.</summary>
    public static RecordRefusal InterdictionRefusal() => new("UNIT-STATE-041", "An Interdiction NMC is taken by a routing unit, and its result agrees with its DR (A10.53).");

    /// <summary>
    /// A15.5 (UNIT-STATE-031): a pending surrender names an active, broken, uncaptured unit with no surrender pending and at least one captor, each an
    /// active enemy unit; the captors are read as they are enumerated, and the pending list last.
    /// </summary>
    public static bool SurrenderAllowed(bool broken, bool captured, int captorCount, IEnumerable<(bool Active, bool Enemy)> captors, Func<bool> alreadyPending)
    {
        ArgumentNullException.ThrowIfNull(captors);
        ArgumentNullException.ThrowIfNull(alreadyPending);
        return !(!broken || captured || captorCount == 0 || captors.Any(captor => !captor.Active || !captor.Enemy) || alreadyPending());
    }

    /// <summary>A15.5 (UNIT-STATE-031): the surrender's refusal.</summary>
    public static RecordRefusal SurrenderRefusal() => new("UNIT-STATE-031", "A surrender names a broken unit and the enemy units it may surrender to (A15.5).");

    /// <summary>A20.3 (ruling R5.6; UNIT-STATE-031): a rejection names a pending surrender of an active unit.</summary>
    public static RecordRefusal RejectSurrenderRefusal(string unitId) => new("UNIT-STATE-031", $"'{unitId}' has no pending surrender to reject (A20.3).");

    /// <summary>A20.3, A20.4: a side faced with No Quarter joins the list once.</summary>
    public static IReadOnlyList<string> NoQuarterAfter(IReadOnlyList<string> noQuarter, string side)
    {
        ArgumentNullException.ThrowIfNull(noQuarter);
        return noQuarter.Contains(side, StringComparer.Ordinal) ? noQuarter : [.. noQuarter, side];
    }

    /// <summary>A20.4 (UNIT-STATE-036): a Massacre names active units and the active prisoners they eliminate.</summary>
    public static bool MassacreNamesActive(int unitCount, int prisonerCount, bool everyUnitActive, bool everyPrisonerActive) =>
        !(unitCount == 0 || prisonerCount == 0 || !everyUnitActive || !everyPrisonerActive);

    /// <summary>A20.4 (UNIT-STATE-036): the Massacre's names are wrong.</summary>
    public static RecordRefusal MassacreNamesRefusal() => new("UNIT-STATE-036", "A Massacre names active units and the active prisoners they eliminate (A20.4).");

    /// <summary>A20.4 (ruling R5.7): a side massacres in its own PFPh or AFPh, or in the DFPh when it is not phasing.</summary>
    public static bool MassacreFirePhase(string? phase, bool phasing) => phase is "pfph" or "afph" ? phasing : phase == "dfph" && !phasing;

    /// <summary>
    /// A20.4 (ruling R5.7): a massacring unit is of the first unit's side, in its Location, Personnel, not in Melee or captured, and berserk for a berserk
    /// Massacre, else berserk or Russian (the nationality read lazily).
    /// </summary>
    public static bool MassacreUnitAllowed(bool sameSide, bool sameLocation, bool personnel, bool melee, bool captured, bool berserkMassacre, bool berserk, Func<string> nationality)
    {
        ArgumentNullException.ThrowIfNull(nationality);
        return !(!sameSide || !sameLocation || !personnel || melee || captured || (berserkMassacre ? !berserk : !berserk && nationality() != "russian"));
    }

    /// <summary>A20.4: a massacred prisoner is of the other side, captured, and in the massacrers' Location.</summary>
    public static bool MassacrePrisonerAllowed(bool sameSideAsMassacrers, bool captured, bool sameLocation) => !(sameSideAsMassacrers || !captured || !sameLocation);

    /// <summary>A20.4 (UNIT-STATE-036): the Massacre's refusal.</summary>
    public static RecordRefusal MassacreRefusal() =>
        new("UNIT-STATE-036", "Only Russian or berserk Infantry not in Melee massacre the prisoners in their Location, in a fire phase of their own side (A20.4).");

    /// <summary>A20.4: a berserk Massacre returns its units to normal.</summary>
    public static bool MassacreEndsBerserk(bool berserkMassacre) => berserkMassacre;

    /// <summary>A20.4 (ruling R18.3): the victims' ELR rises by one, once, to at most 6.</summary>
    public static bool MassacreRaisesElr(bool raisedBefore) => !raisedBefore;

    /// <summary>A20.4 (ruling R18.3): an ELR raised by one, to at most 6; none stays none.</summary>
    public static int? MassacreRaisedElr(int? elr) => elr is { } value ? Math.Min(value + 1, 6) : null;
}
