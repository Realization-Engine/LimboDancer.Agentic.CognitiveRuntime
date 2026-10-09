namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Which reviewed case an entry is routed to (Occupied and Concealed Entry Design, section 6).</summary>
public enum ScenarioA1EntryRoute
{
    /// <summary>The target is empty: the reviewed first case.</summary>
    Empty,

    /// <summary>Every occupant is a known, unconcealed enemy MMC: the Occupied package's A4.14 case.</summary>
    KnownEnemy,

    /// <summary>One concealed or hidden enemy MMC: the PostReveal forced back.</summary>
    Concealed,

    /// <summary>One concealed SMC: revealed, and the attempt waits for the attacker's OVR declaration.</summary>
    LoneSmc,

    /// <summary>Several concealed or hidden enemy units: a Random Selection roll decides the reveal.</summary>
    RandomSelection,

    /// <summary>No reviewed case covers the target.</summary>
    Outside,
}

/// <summary>An occupant of an entry's target as the route reads it: a unit or not, of the mover's side, a MMC, a SMC, visible to the mover's side, concealed, hidden.</summary>
public sealed record EntryOccupantFacts(bool Unit, bool OwnSide, bool Mmc, bool Smc, bool Visible, bool Concealed, bool Hidden);

/// <summary>
/// The state and map facts the nine entry facts are derived from (Governed Writes Design, section 8): a three-valued fact is a <c>bool?</c>, null when the state
/// cannot establish it.
/// </summary>
public sealed record EntryFactInputs(bool Squad, bool? GoodOrder, bool? Concealed, bool? Hidden, bool AttackerMovementPhase, bool? Broken, bool? Ti, bool? Melee, bool MovementEnded,
    bool OpenAttempt, bool Definitive, bool Adjacent, int TargetLevel, bool TargetSideNamed, string? TargetTerrain, int OccupantCount, bool CrossedKnown, bool SameBaseLevel,
    bool NoHexsideTerrain, bool NotRoad, bool NoCliffSlopeOrEmbankment, bool NoDepression, int? MfAllowance, int MfSpent, int SpecialRuleCount);

/// <summary>What an elected OVR's first branch asks the OVR NTC package (Infantry OVR Design, section 5), by the mover's MF left.</summary>
public sealed record OvrElectionFacts(ScenarioA1OvrElection Election, ScenarioA1OvrRemainingMf RemainingMf, ScenarioA1OvrNtcResult? Ntc, string CaseId);

/// <summary>
/// The A1 entry (A4.14, A4.15, A12.15, A.9; the Occupied and Concealed Entry Design, the Random Selection and Declined OVR Design, the Infantry OVR Design;
/// pass 32.i, S2): which reviewed case covers an entry, the nine facts, what the moving side may be told, the forced back, the OVR declaration, the election's
/// NTC, and every word. Play reads the state and the map, builds the packages' snapshots, asks the packages, and writes the events.
/// </summary>
public static class ScenarioA1EntryRules
{
    public const string ResolvedOnConfirmation = "play.resolved-on-confirmation: the entry is declared, and its outcome is resolved when it is confirmed";

    public const string CannotResolve = "play.adjudicator-cannot-resolve: no reviewed case covers this entry, and nothing was committed";

    public const string Committed = "play.committed";

    /// <summary>The reason the concealed-SMC OVR resolver gives when it delegates a declined election to the PostReveal package.</summary>
    public const string DeclinedDelegation = "asl.a1.ovr.declined-use-exact-post-reveal-package";

    /// <summary>The facts of the first case that depend only on the mover and the terrain; the others read the target's occupants. A known-enemy or concealed entry needs every one of them true.</summary>
    public static IReadOnlyList<string> MoverFacts
    {
        get;
    } =
    [
        "isKnownGoodOrderInfantrySquad", "isAttackerMovementPhase", "canMoveThisPhase", "isAdjacentGroundLevelOrdinaryBuilding",
        "hasNoRoadBypassElevationOrAdditionalTerrain", "hasEnoughMovementFactors", "hasNoSpecialRuleOrOtherModifier",
    ];

    /// <summary>Ruling R10.12 (table player, pass 10): move options the reviewed entry cases do not take.</summary>
    public static IReadOnlyList<string> MoveOptions { get; } = ["assault", "doubleTime", "minimumMove"];

    /// <summary>The kinds of counter in the mover's Location that bar the reviewed forced back: a fortification, Residual FP, a fire.</summary>
    public static IReadOnlyList<string> HazardKinds { get; } = ["asl:fortification", "asl:residual", "asl:fire"];

    /// <summary>
    /// Ruling R10.12: a move goes to the reviewed entry cases when one unberserk squad takes its stack's first step in the MPh, with no SMOKE, Bypass, or move
    /// option, into a Location that holds an enemy unit not a prisoner, which the board catalog binds.
    /// </summary>
    public static bool ReviewedEntry(bool oneUnit, bool toParsed, bool smoke, bool bypass, bool anyMoveOption, bool movementPhase, bool movementOpen, bool activeSquad, bool berserk,
        Func<bool> enemyAtTarget, Func<bool> targetBound)
    {
        ArgumentNullException.ThrowIfNull(enemyAtTarget);
        ArgumentNullException.ThrowIfNull(targetBound);
        return oneUnit && toParsed && !smoke && !bypass && !anyMoveOption && movementPhase && !movementOpen && activeSquad && !berserk && enemyAtTarget() && targetBound();
    }

    /// <summary>The reasons the mover's side may see for a plan, before or after confirmation (Occupied and Concealed Entry Design, section 7).</summary>
    public static IReadOnlyList<string> ReasonsForMover(bool withheld, IReadOnlyList<string> planReasons, IReadOnlyList<string> moverReasons, bool confirmed, bool canCommit)
    {
        ArgumentNullException.ThrowIfNull(planReasons);
        ArgumentNullException.ThrowIfNull(moverReasons);
        if (!withheld)
        {
            return planReasons;
        }

        if (moverReasons.Count > 0)
        {
            return moverReasons;
        }

        return !confirmed ? [ResolvedOnConfirmation] : canCommit ? [Committed] : [CannotResolve];
    }

    /// <summary>An entry into a Location the side sees as empty, or as a sealed presence, is withheld: its outcome is disclosed only by committing it.</summary>
    public static bool Withheld(int occupantCount, bool anyOccupantInvisible) => occupantCount == 0 || anyOccupantInvisible;

    /// <summary>The refusals that depend only on the mover and the terrain, which the side may always see.</summary>
    public static IReadOnlyList<string> MoverReasons(IReadOnlyDictionary<string, bool?> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return [.. MoverFacts.Where(name => facts[name] != true).Select(name => facts[name] is null ? $"play.fact-unknown: {name}" : $"play.fact-false: {name}")];
    }

    /// <summary>Whether a side can see an object: its own, or an enemy's that is neither concealed nor hidden.</summary>
    public static bool VisibleTo(bool ownSide, bool? concealed, bool? hidden) => ownSide || (concealed == false && hidden != true);

    /// <summary>Whether a unit may be an A4.14 exception (p. 49): Berserk, Disrupted, or captured (and so Unarmed). Null when unknown.</summary>
    public static bool? A414Exception(bool? berserk, bool? disrupted, bool? captured)
    {
        bool?[] states = [berserk, disrupted, captured];
        return states.Contains(true) ? true : states.All(item => item == false) ? false : null;
    }

    /// <summary>
    /// Which reviewed case covers a target with occupants (Occupied and Concealed Entry Design, section 6): known enemy MMC only; one concealed or hidden enemy
    /// MMC; one SMC that was concealed, not hidden; several concealed or hidden enemy units with no hidden SMC; anything else is outside the cases.
    /// </summary>
    public static ScenarioA1EntryRoute Route(IReadOnlyList<EntryOccupantFacts> occupants)
    {
        ArgumentNullException.ThrowIfNull(occupants);
        var units = occupants.Where(item => item.Unit).ToArray();
        var enemies = units.Length == occupants.Count && units.All(item => !item.OwnSide);
        var knownEnemy = enemies && units.All(item => item.Mmc) && units.All(item => item.Visible);
        var concealedAll = enemies && units.All(item => item.Mmc || item.Smc) && units.All(item => item.Concealed || item.Hidden);
        return knownEnemy ? ScenarioA1EntryRoute.KnownEnemy
            : !concealedAll ? ScenarioA1EntryRoute.Outside
            : units.Length == 1 && units[0].Mmc ? ScenarioA1EntryRoute.Concealed

            // The reviewed decline covers only an SMC that was concealed, not hidden, when the attempt began.
            : units.Length == 1 && units[0].Concealed && !units[0].Hidden ? ScenarioA1EntryRoute.LoneSmc
            : units.Length > 1 && !units.Any(item => item.Smc && item.Hidden) ? ScenarioA1EntryRoute.RandomSelection
            : ScenarioA1EntryRoute.Outside;
    }

    public const string OutsideReviewedCases = "play.outside-reviewed-cases: the target holds units no reviewed case covers "
        + "(a hidden SMC, a friendly unit, known and concealed units together, or an entity)";

    public const string MoverCannotAttempt = "play.mover-cannot-attempt";

    public const string A414ExceptionText = "play.a414-exception: the mover may be Berserk, Disrupted, or captured, so A4.14 (p. 49) may not apply";

    public const string OutsideReviewedBoard = "play.outside-reviewed-board: the reviewed cases cover only the explicit building overrides of VASL board 01";

    /// <summary>A4.14 (p. 49) prohibits entering a location with a known enemy unit in the MPh.</summary>
    public static string ProhibitedText(string? conclusionId) => $"play.prohibited: A4.14 (p. 49) prohibits entering a location with a known enemy unit in the MPh ({conclusionId})";

    /// <summary>The reviewed first case gave no Definitive conclusion.</summary>
    public static string FirstCaseNotDefinitive(string disposition) => $"play.not-definitive: the reviewed first case is {disposition}";

    public const string OccupiedObservedNone = "play.not-definitive: the Occupied package observed no reviewed case";

    /// <summary>The Occupied case gave no Definitive conclusion.</summary>
    public static string OccupiedCaseNotDefinitive(string disposition) => $"play.not-definitive: the Occupied case is {disposition}";

    public const string PostRevealObservedNone = "play.not-definitive: the PostReveal package observed no reviewed case";

    /// <summary>The PostReveal case gave no Definitive conclusion.</summary>
    public static string PostRevealNotDefinitive(string disposition) => $"play.not-definitive: the PostReveal case is {disposition}";

    public const string ConcealedSmcObservedNone = "play.not-definitive: the concealed-SMC OVR package observed no reviewed case";

    public const string OvrNtcObservedNone = "play.not-definitive: the OVR NTC package observed no reviewed case";

    /// <summary>The unit named is not an active unit of the game.</summary>
    public static string UnitUnavailableText(string unitId) => $"play.unit-unavailable: no active unit '{unitId}'";

    /// <summary>The reviewed first case's entry in words: two MF.</summary>
    public static string EnterText(string unitId, string target, string? conclusionId) => $"play.enter: {unitId} into {target} for 2 MF ({conclusionId})";

    /// <summary>The reviewed first case's entry costs 2 MF.</summary>
    public const int EntryMf = 2;

    /// <summary>The forced back covers only a clear return; the mover's Location holds a hazard.</summary>
    public static string ReturnHazardText(IReadOnlyList<string> kinds, string from)
    {
        ArgumentNullException.ThrowIfNull(kinds);
        return $"play.return-hazard: {string.Join(", ", kinds)} at {from}; the forced back covers only a clear return";
    }

    /// <summary>
    /// The forced back is checked once, before any roll: the PostReveal facts do not depend on which non-Dummy unit is revealed, so a candidate that reveals one
    /// MMC, else the first unit (the first two when there are several), stands for every branch that forces the mover back.
    /// </summary>
    public static IReadOnlyList<string> SampleBranch(IReadOnlyList<(string Id, bool Mmc)> defenders)
    {
        ArgumentNullException.ThrowIfNull(defenders);
        return defenders.FirstOrDefault(item => item.Mmc) is { Id: { } mmc } ? [mmc] : [.. defenders.Select(item => item.Id).Take(defenders.Count > 1 ? 2 : 1)];
    }

    /// <summary>A12.15: a revealed MMC forces the mover back, with the attempt's MF spent and its MPh ended.</summary>
    public static string ForcedBackText(string unitId, string target, string revealedId, string from, string? conclusionId) =>
        $"play.forced-back: {unitId} attempts {target}, {revealedId} is revealed, and {unitId} returns to {from} with 2 MF spent and its MPh ended ({conclusionId})";

    /// <summary>A12.15 (p. 78): a lone revealed SMC leaves the attempt open for the attacker's OVR declaration.</summary>
    public static string DeclarationPendingText(string unitId, string target, string revealedId) =>
        $"play.declaration-pending: {unitId} attempts {target} and {revealedId} is revealed; the attacker may decline or elect an Infantry OVR (A12.15, p. 78)";

    /// <summary>A.9 (p. 43): one dr for each unit at the target decides the reveal.</summary>
    public static string RandomSelectionText(int count, string target, string unitId, string? conclusionId) =>
        $"play.random-selection: one dr for each of the {count} units at {target} decides the reveal (A.9, p. 43); a revealed MMC, or more than one revealed SMC, forces {unitId} back ({conclusionId})";

    /// <summary>A12.15 and A4.15 (p. 49): a lone revealed SMC leaves the attempt open; a revealed MMC, or more than one revealed SMC, forces the mover back.</summary>
    public static bool LoneSmcRevealed(int revealedCount, bool revealedSmc) => revealedCount == 1 && revealedSmc;

    /// <summary>The attacker's OVR declaration needs an open attempt by the unit with no declaration yet.</summary>
    public static string NoPendingDeclarationText(string unitId) => $"play.no-pending-declaration: {unitId} has no entry attempt awaiting an OVR declaration";

    /// <summary>A12.15 (p. 78): the declaration exists only when the attempt revealed exactly one SMC and nothing else.</summary>
    public static bool DeclarationExists(int revealedCount, bool revealedSmc, bool revealingMatches) => revealedCount == 1 && revealedSmc && revealingMatches;

    public const string NotExactlyOneSmc = "play.no-pending-declaration: the attempt did not reveal exactly one SMC";

    /// <summary>The attempt's initial occupancy for the concealed-SMC OVR package: hidden when the SMC was first placed beneath a "?", concealed otherwise.</summary>
    public static ScenarioA1InitialConcealedOccupancy InitialOccupancy(bool placedBeneathQuestionMark) =>
        placedBeneathQuestionMark ? ScenarioA1InitialConcealedOccupancy.Hidden : ScenarioA1InitialConcealedOccupancy.Concealed;

    public const string NotDelegated = "play.not-delegated: the concealed-SMC OVR package did not delegate the declined case";

    /// <summary>The declined OVR in words: the reviewed matrix delegates the case, and the mover returns with the attempt's MF spent.</summary>
    public static string DeclinedText(string unitId, string smcId, string? delegationId, string from, int mf, string? conclusionId) =>
        $"play.declined: {unitId} declines the OVR against {smcId}; the reviewed matrix delegates the case ({delegationId}), "
            + $"and {unitId} returns to {from} with {mf} MF spent and its MPh ended ({conclusionId})";

    /// <summary>A4.15, A12.15: the mover's MF allowance must be known to elect an OVR.</summary>
    public const string ElectionAllowanceUnknown = "play.election-unavailable: the mover's MF allowance is unknown";

    /// <summary>A12.15 offers the OVR only if possible, and A4.15 doubles the MF of entry: four MF must be left. The attempt's own MF are not yet spent.</summary>
    public static OvrElectionFacts Election(int remaining) =>
        remaining >= 4
            ? new OvrElectionFacts(ScenarioA1OvrElection.Elected, ScenarioA1OvrRemainingMf.AtLeastFour, ScenarioA1OvrNtcResult.Failed, "A1-ovr-ntc-failed")
            : new OvrElectionFacts(ScenarioA1OvrElection.Requested, ScenarioA1OvrRemainingMf.BelowFour, null, "A1-ovr-ntc-mf-insufficient");

    /// <summary>A4.15 (p. 49): fewer than four MF left.</summary>
    public static string? ElectionMfBar(int remaining, string unitId, string? conclusionId) =>
        remaining < 4 ? $"play.election-unavailable: {unitId} has {remaining} MF left, and an Infantry OVR needs four (A4.15, p. 49) ({conclusionId})" : null;

    /// <summary>Another concealed non-Dummy unit must be present; against a lone SMC a passed NTC leads to its options and CC, which are unreviewed (Infantry OVR Design, section 6).</summary>
    public static string? NoOtherConcealedBar(int otherCount) => otherCount == 0 ? CannotResolve + " (the election could lead to outcomes that are not yet reviewed)" : null;

    public const string NtcFailedCaseNotDefinitive = "play.not-definitive: the OVR NTC failed case is not Definitive";

    public const string NtcPassedCaseNotDefinitive = "play.not-definitive: the OVR NTC passed case is not Definitive";

    public const string NtcUnavailable = "play.ntc-unavailable: the mover's printed morale or the building's construction is unknown";

    /// <summary>B23.3 (p. 136): the building TEM as the NTC's DRM: +3 stone, +2 otherwise.</summary>
    public static int NtcTem(string? material) => material == "stone" ? 3 : 2;

    /// <summary>The NTC's DRM is cited as B23.3.</summary>
    public const string NtcTemRule = "B23.3";

    /// <summary>A10.1 (p. 65): two dice plus the TEM against the Morale Level; it passes at or under.</summary>
    public static bool NtcPassed(int finalDr, int moraleLevel) => finalDr <= moraleLevel;

    /// <summary>The elected OVR in words.</summary>
    public static string ElectedText(string unitId, string smcId, int tem, int moraleLevel, string? failedId, string? passedId) =>
        $"play.elected: {unitId} attempts an Infantry OVR against {smcId}. The NTC is two dice + {tem} (B23.3) against morale {moraleLevel}; "
            + $"a failure forces {unitId} back ({failedId}), and a pass reveals another unit by Random Selection and forces it back ({passedId})";

    /// <summary>
    /// The nine facts of the reviewed first case (Governed Writes Design, section 8), from the state and map facts: a fact the state cannot establish is null,
    /// never guessed, so the reviewed resolver stays indeterminate.
    /// </summary>
    public static IReadOnlyDictionary<string, bool?> EntryFacts(EntryFactInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        bool? And(params bool?[] values) => values.Any(value => value == false) ? false : values.All(value => value == true) ? true : null;
        var known = inputs.Concealed is { } concealed && inputs.Hidden is { } hidden ? !concealed && !hidden : (bool?)null;
        return new Dictionary<string, bool?>(StringComparer.Ordinal)
        {
            ["isKnownGoodOrderInfantrySquad"] = And(inputs.Squad, inputs.GoodOrder, known),
            ["isAttackerMovementPhase"] = inputs.AttackerMovementPhase,

            // A4.1 (p. 48): a unit that is broken, TI, or held in Melee cannot move. Fire and Opportunity Fire are not
            // actions of the live source yet, so no live unit has fired.
            // A unit forced back has ended its MPh (A12.15, p. 78) and cannot move again in it.
            ["canMoveThisPhase"] = And(inputs.Broken is { } broken ? !broken : null, inputs.Ti is { } ti ? !ti : null,
                inputs.Melee is { } melee ? !melee : null, !inputs.MovementEnded, !inputs.OpenAttempt),
            ["isAdjacentGroundLevelOrdinaryBuilding"] = !inputs.Definitive ? null
                : inputs.Adjacent && inputs.TargetLevel == 0 && !inputs.TargetSideNamed && ScenarioA1Definitions.OrdinaryBuildings.Contains(inputs.TargetTerrain ?? string.Empty),
            ["isDestinationKnownEmpty"] = inputs.OccupantCount == 0,
            ["hasNoRoadBypassElevationOrAdditionalTerrain"] = !inputs.Definitive || !inputs.CrossedKnown ? (inputs.Adjacent ? (bool?)null : false)
                : inputs.SameBaseLevel && inputs.NoHexsideTerrain && inputs.NotRoad && inputs.NoCliffSlopeOrEmbankment && inputs.NoDepression,

            // A4.11 (p. 48) and A19.31 (p. 86): a Good Order MMC has four MF, three if Inexperienced. When the status is
            // unknown, two MF remain under either allotment after one MF spent, and under neither after three.
            ["hasEnoughMovementFactors"] = inputs.MfAllowance is { } allowance
                ? allowance - inputs.MfSpent >= 2
                : inputs.MfSpent <= 1 ? true : inputs.MfSpent >= 3 ? false : null,
            ["isBelowStackingLimit"] = inputs.OccupantCount == 0,
            ["hasNoSpecialRuleOrOtherModifier"] = inputs.SpecialRuleCount == 0,
        };
    }
}
