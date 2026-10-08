namespace LimboDancer.Domains.Asl.Rules;

/// <summary>What the Player Turn's end does to a vehicle's Stun or Recall (D5.34, D5.341): nothing, or the conditions it sets, in order.</summary>
public sealed record StunEndVerdict(IReadOnlyList<(UnitCondition Condition, bool Value)> Conditions);

/// <summary>A source unit of a created unit's OB group (ruling R18.3): its group and its ELR.</summary>
public sealed record GroupSourceFacts(string? Group, int? Elr);

/// <summary>
/// The projector's checks of the sequence of play (pass 32.i, S2): the game's start and end, the phase change and what it removes and requires, the owners'
/// options, Bore Sighting, created units and lineage, the A1 entry's five records, and what every event does to the markers it ends. Units reads the records
/// and the state, hands the facts over, and applies the verdicts.
/// </summary>
public static class ScenarioA1SequenceProjection
{
    // The game's start (ASL-UNIT-050; A19.1, A25.8; rulings R18.3, R27.1).

    /// <summary>Sides need distinct slug ids other than 'adjudicator'.</summary>
    public static RecordRefusal? SidesRefusal(int sideCount, bool distinctIds, bool anyIdNotSlugOrAdjudicator) =>
        sideCount == 0 || !distinctIds || anyIdNotSlugOrAdjudicator ? new RecordRefusal("UNIT-STATE-005", "Sides need distinct slug ids other than 'adjudicator'.") : null;

    /// <summary>Ruling R18.3 (referee, pass 18): a side's OB groups have distinct ids and ELRs of 0 to 5 (A19.1), and its own ELR, when it names one, is the ELR its groups share.</summary>
    public static RecordRefusal? GroupsRefusal(string sideId, bool distinctGroupIds, bool anyGroupIdNotSlug, IReadOnlyList<int?> groupElrs, int? sideElr)
    {
        ArgumentNullException.ThrowIfNull(groupElrs);
        var shared = groupElrs.Distinct().ToArray();
        return !distinctGroupIds || anyGroupIdNotSlug || groupElrs.Any(elr => elr is < 0 or > 5) || (sideElr is { } elr && (shared.Length != 1 || shared[0] != elr))
            ? new RecordRefusal("UNIT-STATE-005", $"The OB groups of side '{sideId}' need distinct slug ids and ELRs of 0 to 5, and a side ELR only when all its groups share it (A19.1).")
            : null;
    }

    /// <summary>A side's nationality is declared in the vocabulary.</summary>
    public static RecordRefusal NationalityRefusal(string sideId, string nationality) =>
        new("UNIT-STATE-005", $"The nationality '{nationality}' of side '{sideId}' is not declared.");

    /// <summary>A25.8 (ruling R27.1): an Axis Minor side names its nation; no other side names one.</summary>
    public static bool NationMisnamed(bool axisMinor, bool nationKnown, bool nationNamed) => axisMinor ? !nationKnown : nationNamed;

    /// <summary>A25.8, in words.</summary>
    public static RecordRefusal NationRefusal(string sideId, string nationality, bool axisMinor, IReadOnlyList<string> nations)
    {
        ArgumentNullException.ThrowIfNull(nations);
        return new RecordRefusal("UNIT-STATE-005", axisMinor
            ? $"The Axis Minor side '{sideId}' needs its nation: {string.Join(", ", nations)} (A25.8)."
            : $"Side '{sideId}' is {nationality}; only an Axis Minor side names a nation (A25.8).");
    }

    /// <summary>E3.51, E3.71: whether rain has fallen at the start, as the SSRs name it.</summary>
    public static bool RainedAtStart(string? precipitation) => precipitation is "rain" or "heavy-rain";

    /// <summary>A25.8, A25.82 (ruling R27.1): Hungarians fighting Romanians face No Quarter on both sides from the start.</summary>
    public static bool HungariansVersusRomanians(IEnumerable<string?> nations)
    {
        ArgumentNullException.ThrowIfNull(nations);
        var list = nations.ToArray();
        return list.Any(nation => nation == "hungarian") && list.Any(nation => nation == "romanian");
    }

    // The game's end (A3.9; ruling R20.1).

    /// <summary>A game ends after its current Game Turn, with no entry attempt, choice, surrender, or CC left open.</summary>
    public static RecordRefusal? EndRefusal(int endedTurn, int turn, int openAttempts, bool choicePending, int pendingSurrenders, bool anyOpenCloseCombat) =>
        endedTurn != turn || openAttempts > 0 || choicePending || pendingSurrenders > 0 || anyOpenCloseCombat
            ? new RecordRefusal("UNIT-STATE-045", "A game ends after its current Game Turn, with no entry attempt, choice, surrender, or CC left open (A3.9).")
            : null;

    // The phase change (A3.1 to A3.8, A10.62, A11.12, A11.16, A11.32, A15.5, A4.51, D5.34, D7.21, E1.8, E1.923; rulings R5.3, R5.8, R16.2, R16.8, R29.11, R31c.5).

    /// <summary>The phase may not change to an earlier turn.</summary>
    public static RecordRefusal? TurnRefusal(int changeTurn, int turn) =>
        changeTurn < turn ? new RecordRefusal("UNIT-STATE-015", $"Turn {changeTurn} is before the current turn {turn}.") : null;

    /// <summary>An entry attempt is resolved within its phase: the phase may not change while one is open.</summary>
    public static RecordRefusal? OpenAttemptRefusal(string? openAttempt) =>
        openAttempt is not null ? new RecordRefusal("UNIT-STATE-018", $"The entry attempt '{openAttempt}' is open, so the phase may not change.") : null;

    /// <summary>Ruling R5.8: a pending choice is answered before anything else happens.</summary>
    public static RecordRefusal? ChoiceRefusal(string? choiceSide, string? choiceKey) =>
        choiceKey is not null ? new RecordRefusal("UNIT-STATE-035", $"The {choiceSide} side's choice '{choiceKey}' is pending, so the phase may not change.") : null;

    /// <summary>A15.5: a surrender waits for the captor's choice before anything else happens.</summary>
    public static RecordRefusal? SurrenderRefusal(string? surrenderedUnit) =>
        surrenderedUnit is not null ? new RecordRefusal("UNIT-STATE-031", $"'{surrenderedUnit}' surrenders and awaits its captor, so the phase may not change (A15.5).") : null;

    /// <summary>
    /// A11.3 (ruling R31c.5; the third play test): a unit eliminated before it attacks forfeits its attack, so a Location whose ambusher's attacks left no unit of
    /// the ambushed side there has nothing more to resolve and does not hold the phase.
    /// </summary>
    public static bool NothingLeft(bool hasAmbusher, int rounds, bool anyAmbushedLeft) => hasAmbusher && rounds > 0 && !anyAmbushedLeft;

    /// <summary>A11.12, A11.32: a Location's CC is completely resolved before the phase ends: after the ambusher's round, the other side's.</summary>
    public static RecordRefusal OpenCloseCombatRefusal(bool ambushed, string location) =>
        new("UNIT-STATE-030", !ambushed
            ? $"The CC in {location} awaits its round after the Ambush drs, even one with no attacks (A11.12)."
            : $"The CC in {location} awaits the ambushed side's round, which may declare no attacks (A11.32).");

    /// <summary>A11.16, A19.12 (ruling R29.11): a broken or Disrupted unit held in Melee, other than a Guard, has its elimination recorded before the CCPh ends.</summary>
    public static bool MeleeEliminationOwed(string phase, bool active, bool melee, bool captured, bool guard, bool broken, bool disrupted) =>
        phase == "ccph" && active && melee && !captured && !guard && (broken || disrupted);

    /// <summary>A11.16, A19.12, in words.</summary>
    public static RecordRefusal MeleeEliminationRefusal(string unitId) =>
        new("UNIT-STATE-030", $"'{unitId}' is broken or Disrupted in Melee and is eliminated at the end of the CCPh (A11.16, A19.12).");

    /// <summary>
    /// The markers the ending phase removes, from units and weapons alike: First and Final Fire at the end of the DFPh (A3.4), Prep Fire at the end of the AFPh
    /// (A3.5), pins and TI in the CCPh (A3.8), DM at the end of every RPh (A10.62), the CC Reaction Fire counter with the MPh (D7.21; ruling R11.13); E1.8 (ruling
    /// R16.2): at night First and Final Fire counters stay, as Gunflashes, until the end of the AFPh.
    /// </summary>
    public static IReadOnlyList<UnitCondition> ClearedMarkers(string phase, bool night) => phase switch
    {
        "dfph" when night => [UnitCondition.IntensiveFire],
        "dfph" => [UnitCondition.FinalFire, UnitCondition.FirstFire, UnitCondition.IntensiveFire],
        "afph" when night => [UnitCondition.PrepFire, UnitCondition.BoundingFire, UnitCondition.IntensiveFire, UnitCondition.FinalFire, UnitCondition.FirstFire],
        "afph" => [UnitCondition.PrepFire, UnitCondition.BoundingFire, UnitCondition.IntensiveFire],
        "ccph" => [UnitCondition.Pinned, UnitCondition.Ti],
        "mph" => [UnitCondition.CcReaction],
        "rph" => [UnitCondition.DesperationMorale],
        _ => [],
    };

    /// <summary>A4.51 (ruling R5.3): a side's CX counters leave at the start of its next MPh, and those units may not Double Time in it.</summary>
    public static bool CxRests(string nextPhase, bool active, bool ofPhasingSide, bool cx) => nextPhase == "mph" && active && ofPhasingSide && cx;

    /// <summary>A4.51: the rested unit's CX counter is removed.</summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> CxRestedConditions() => [(UnitCondition.Cx, false)];

    /// <summary>D5.34, D5.341: Stuns and Recalls change at the Player Turn's end.</summary>
    public static bool StunsEnd(bool newPlayerTurn) => newPlayerTurn;

    /// <summary>
    /// D5.34, D5.341: at the end of the Player Turn in which it was placed, a Stun becomes Stun +1, and a Recall becomes Recall; +1, after which the AFV must
    /// leave by its Friendly Board Edge (ruling R5.17); nothing for a unit not active, or a Recall already on its +1 side.
    /// </summary>
    public static StunEndVerdict StunEnds(bool active, bool recalled, bool stunRecovery, bool stunned)
    {
        if (!active)
        {
            return new StunEndVerdict([]);
        }

        if (recalled)
        {
            return new StunEndVerdict(stunRecovery ? [] : [(UnitCondition.StunRecovery, true)]);
        }

        return new StunEndVerdict(stunned ? [(UnitCondition.Stunned, false), (UnitCondition.StunRecovery, true)] : []);
    }

    /// <summary>A3.3 (table player, pass 12): as the PFPh ends within a Player Turn, a unit that fired only a SW has Prep Fired and does not move.</summary>
    public static bool NoMoveListGrows(string phase, bool newPlayerTurn) => !newPlayerTurn && phase == "pfph";

    /// <summary>A3.3: a firer of the PFPh joins the no-move list unless it is listed already or carries a Prep Fire counter.</summary>
    public static bool JoinsNoMoveList(bool listed, bool found, bool prepFired) => !listed && found && !prepFired;

    /// <summary>A24.11 (ruling R9.5): the SMOKE counters of grenades leave at the end of the MPh they were placed in.</summary>
    public static bool RemovesSmokeGrenades(string phase) => phase == "mph";

    /// <summary>A24.11: a grenade's SMOKE counter: an active SMOKE entity with the grenade id suffix.</summary>
    public static bool SmokeGrenade(bool active, string kind, bool grenadeSuffix) => active && kind == "asl:smoke" && grenadeSuffix;

    /// <summary>E1.923 (ruling R16.8): Starshells are removed at the end of each CCPh.</summary>
    public static bool RemovesStarshells(string phase) => phase == "ccph";

    /// <summary>E1.923: a Starshell entity.</summary>
    public static bool Starshell(string kind) => kind == "asl:starshell";

    // After every event (A24.1, C13.31, C6.1 Case J, C6.17, A4.51; rulings R9.7, R6.1, R8.1).

    /// <summary>A24.1 (table player, pass 9): a placed SMOKE counter is created by the event right after its attempt, or not at all.</summary>
    public static bool SmokePendingLapses(bool pending, bool movementStepOrCreation) => pending && !movementStepOrCreation;

    /// <summary>C13.31 (ruling R9.7): setup ends with the first event of play; each side's Personnel then are its OB for the PF usage limit.</summary>
    public static bool SetupCloses(bool setupClosed, bool setupEvent) => !setupClosed && !setupEvent;

    /// <summary>C6.1 Case J (ruling R6.1): a vehicle that enters a new hex, or moves while under a Motion counter, this Player Turn.</summary>
    public static bool MovedThisPlayerTurn(bool listed, bool entersOrExits, bool motionBefore) => !listed && (entersOrExits || motionBefore);

    /// <summary>C6.17 (ruling R8.1): Defensive First Fire shots count per Location the moving stack enters.</summary>
    public static bool ClearsShotsHere(bool movementStepOrVehicleEntry, int shotsHere) => movementStepOrVehicleEntry && shotsHere > 0;

    /// <summary>A4.51: a unit's CX counter is removed when it is broken or berserk.</summary>
    public static bool LosesCx(bool active, bool cx, bool broken, bool berserk) => active && cx && (broken || berserk);

    // The owners' options (ruling R5.8; C6.51).

    /// <summary>Ruling R5.8: no record resolves while a choice is pending.</summary>
    public static string ChoicePendingText(string side, string key) => $"The {side} side's choice '{key}' is pending, so no record resolves before it is answered.";

    /// <summary>Ruling R5.8: a record's declared choices equal the answers given.</summary>
    public static string? ChoicesMismatch(IReadOnlyDictionary<string, string> declared, IReadOnlyDictionary<string, string> made)
    {
        ArgumentNullException.ThrowIfNull(declared);
        ArgumentNullException.ThrowIfNull(made);
        return declared.Count == made.Count && declared.All(item => made.TryGetValue(item.Key, out var answer) && answer == item.Value)
            ? null
            : $"The record declares the choices {Describe(declared)}, but the choosing sides answered {Describe(made)} (ruling R5.8).";

        static string Describe(IReadOnlyDictionary<string, string> map) =>
            map.Count == 0 ? "none" : string.Join(", ", map.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => $"{item.Key}={item.Value}"));
    }

    /// <summary>A pending choice (ruling R5.8): one at a time, by a side of the game, with the options it may answer, of a known kind, once per key.</summary>
    public static RecordRefusal? PendChoiceRefusal(bool choicePending, bool sideKnown, int optionCount, bool kindKnown, bool keyAnswered) =>
        choicePending || !sideKnown || optionCount == 0 || !kindKnown || keyAnswered
            ? new RecordRefusal("UNIT-STATE-035", "A choice is pending one at a time, for a side of the game, with its options, and once per key (ruling R5.8).")
            : null;

    /// <summary>Ruling R5.8: the answer is one of the pending choice's options.</summary>
    public static RecordRefusal? MakeChoiceRefusal(bool pending, bool keyMatches, bool optionAllowed, string key, string option) =>
        !pending || !keyMatches || !optionAllowed
            ? new RecordRefusal("UNIT-STATE-035", $"'{key}' is not the pending choice, or '{option}' is not one of its options (ruling R5.8).")
            : null;

    /// <summary>C6.51: an Acquisition choice names a Gun with an Acquisition and one of its Locations.</summary>
    public static RecordRefusal? AcquisitionChoiceRefusal(bool acquisitionFound, bool locationParsed) =>
        !acquisitionFound || !locationParsed ? new RecordRefusal("UNIT-STATE-035", "An Acquisition choice names a Gun with an Acquisition and one of its Locations (C6.51).") : null;

    // Bore Sighting (C6.41, C6.42; ruling R8.8).

    /// <summary>One Location per Gun of the Scenario Defender, recorded at setup with the Gun's crew and setup Location, outside the Gun's hex.</summary>
    public static RecordRefusal? BoreSightRefusal(bool mannedGunOnMap, bool crewMatches, bool setupLocationMatches, bool ownLocation, bool scenarioDefenders, bool alreadySighted) =>
        !mannedGunOnMap || !crewMatches || !setupLocationMatches || ownLocation || !scenarioDefenders || alreadySighted
            ? new RecordRefusal("UNIT-STATE-039", "A Bore Sighting is one Location outside the hex of a Gun the Scenario Defender set up manned (C6.41, C6.42).")
            : null;

    // Created units (A12.11; ruling R18.3, R19.5) and lineage (A7.302, A10.53, A10.6, A19.13, A20.5; rulings R5.12, R18.3).

    /// <summary>A12.11: a Dummy is a concealment counter with no unit beneath, so it has no definition.</summary>
    public static RecordRefusal? DummyDefinitionRefusal(bool hasDefinition) => hasDefinition ? new RecordRefusal("UNIT-STATE-008", "A Dummy has no definition.") : null;

    /// <summary>Pass 19 (ruling R19.5): a Dummy keeps its OB group, whose "?" it uses; the group is one of its side's.</summary>
    public static RecordRefusal? GroupRefusal(string? group, string side, bool groupOfSide) =>
        group is not null && !groupOfSide ? new RecordRefusal("UNIT-STATE-005", $"'{group}' is not an OB group of side '{side}'.") : null;

    /// <summary>Ruling R18.3: a unit names an OB group of its side, or keeps the group of the units it comes from; units of two groups recombined take the group with the lower ELR (referee, pass 18).</summary>
    public static string? InheritedGroup(string? named, IReadOnlyList<GroupSourceFacts> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        return named ?? sources.Where(item => item.Group is not null).OrderBy(item => item.Elr ?? int.MaxValue).Select(item => item.Group).FirstOrDefault();
    }

    /// <summary>How many units a lineage consumes and produces: Deployed 1 into 2, Recombined 2 into 1, others 1 into 1.</summary>
    public static (int Consumed, int Produced) LineageCounts(string action) => action switch
    {
        "Deployed" => (1, 2),
        "Recombined" => (2, 1),
        _ => (1, 1),
    };

    /// <summary>The lineage's counts, in words.</summary>
    public static RecordRefusal? LineageCountRefusal(string action, int consumed, int produced)
    {
        var (consumedCount, producedCount) = LineageCounts(action);
        return consumed != consumedCount || produced != producedCount
            ? new RecordRefusal("UNIT-STATE-013", $"{action} consumes {consumedCount} and produces {producedCount}; this event consumes {consumed} and produces {produced}.")
            : null;
    }

    /// <summary>Lineage keeps one side: the consumed units and the produced ones.</summary>
    public static RecordRefusal? LineageSideRefusal(int consumedSides, bool anyProducedOfOtherSide) =>
        consumedSides != 1 || anyProducedOfOtherSide ? new RecordRefusal("UNIT-STATE-013", "Lineage keeps one side: the consumed units and the produced ones.") : null;

    /// <summary>A7.302, A19.13: Reduced and Deployed turn a squad into half-squads, Recombined the reverse; other actions keep the kind open.</summary>
    public static (string? Consumed, string? Produced) LineageKinds(string action) => action switch
    {
        "Reduced" or "Deployed" => ("asl:squad", "asl:half-squad"),
        "Recombined" => ("asl:half-squad", "asl:squad"),
        _ => (null, null),
    };

    /// <summary>The lineage's kinds, in words.</summary>
    public static RecordRefusal LineageKindRefusal(string action, string consumedKind, string producedKind) =>
        new("UNIT-STATE-013", $"{action} turns a {consumedKind} into a {producedKind}.");

    /// <summary>A19.13 (p. 86): a Replacement unit is the same size as the unit it replaces.</summary>
    public static RecordRefusal? ReplacementSizeRefusal(string action, bool producedKindKnown, bool sameSize) =>
        action == "Replaced" && producedKindKnown && !sameSize ? new RecordRefusal("UNIT-STATE-013", "A Replacement unit is the same size as the unit it replaces (A19.13, p. 86).") : null;

    /// <summary>A10.53 (referee, pass 13): a HS Reduced from a routing squad has routed this RtPh.</summary>
    public static bool InheritsRout(string phase, bool anyConsumedRouted) => phase == "rtph" && anyConsumedRouted;

    /// <summary>A Replacement (A19.13, A15.3) or a Reduction (A7.302, A4.431; ruling R5.12) is a unit substitution: the new unit keeps the SW, the prisoners, and the held equipment.</summary>
    public static bool LineageKeepsPossessions(string action) => action is "Replaced" or "Reduced";

    // Units created in play (A24.1, A15.21; rulings R5.11, R18.3).

    /// <summary>A24.1 (table player, pass 9): a SMOKE grenade counter is created only by the attempt that placed it, where it placed it.</summary>
    public static RecordRefusal? SmokeCreationRefusal(bool smoke, bool pending, bool grenadeSuffix, bool atPendingLocation) =>
        smoke && (!pending || !grenadeSuffix || !atPendingLocation)
            ? new RecordRefusal("UNIT-STATE-006", "A SMOKE counter is created only by a SMOKE attempt that placed it, in the Location it named (A24.1).")
            : null;

    /// <summary>The creator is an active unit of the created unit's side.</summary>
    public static RecordRefusal? CreatorRefusal(string? creatorId, bool creatorActiveOfSide) =>
        creatorId is not null && !creatorActiveOfSide ? new RecordRefusal("UNIT-STATE-006", $"The creator '{creatorId}' is not an active unit of the created unit's side.") : null;

    /// <summary>Ruling R18.3 (referee, pass 18): a hero, a created leader, or a crew bailing out joins the OB group of the unit that made it, or of a unit of its side in its Location.</summary>
    public static string? JoinedGroup(string? creatorGroup, Func<string?> groupInLocation)
    {
        ArgumentNullException.ThrowIfNull(groupInLocation);
        return creatorGroup ?? groupInLocation();
    }

    /// <summary>Ruling R5.11: one created in its own side's MPh with no creator named, as records made before pass 5 are, moves no further that phase.</summary>
    public static bool CreatedMoveEnds(string phase, bool ofPhasingSide) => phase == "mph" && ofPhasingSide;

    // The A1 entry's five records (A12.15, A.9, A10.1; the OVR NTC review).

    /// <summary>An entry attempt is made by a unit in its own side's MPh that has not ended its move and has no open attempt, for no negative MF.</summary>
    public static RecordRefusal? AttemptRefusal(bool isUnit, string phase, bool ofPhasingSide, bool movementEnded, bool openAttempt, int mf, string unitId) =>
        !isUnit ? new RecordRefusal("UNIT-STATE-018", $"'{unitId}' is not a unit, so it cannot attempt an entry.")
        : phase != "mph" || !ofPhasingSide ? new RecordRefusal("UNIT-STATE-018", $"'{unitId}' can attempt an entry only in the MPh of its own side.")
        : movementEnded ? new RecordRefusal("UNIT-STATE-018", $"'{unitId}' may not move again this phase (A4.1, p. 48).")
        : openAttempt ? new RecordRefusal("UNIT-STATE-018", $"'{unitId}' already has an open entry attempt.")
        : mf < 0 ? new RecordRefusal("UNIT-STATE-010", "An attempt cannot cost negative MF.")
        : null;

    /// <summary>A.9: a Random Selection names an open attempt.</summary>
    public static RecordRefusal? SelectionAttemptRefusal(bool attemptOpen, string attempt) =>
        attemptOpen ? null : new RecordRefusal("UNIT-STATE-020", $"'{attempt}' is not an open entry attempt.");

    /// <summary>After an election, one selection may follow a passed OVR NTC, for the second defender; otherwise an attempt has at most one selection, for its first reveal.</summary>
    public static bool SecondSelection(bool elected) => elected;

    /// <summary>The selection's place in the attempt, in words.</summary>
    public static RecordRefusal? SelectionOrderRefusal(bool second, bool? taskCheckPassed, bool secondSelectionMade, int revealingCount, string attempt) =>
        (second ? taskCheckPassed != true || secondSelectionMade : revealingCount > 0)
            ? new RecordRefusal("UNIT-STATE-020", second ? $"After an election, one Random Selection follows only a passed NTC." : $"The attempt '{attempt}' already has a Random Selection.")
            : null;

    /// <summary>The selection's roll is recorded.</summary>
    public static RecordRefusal? SelectionRollRefusal(bool rollRecorded, string roll) =>
        rollRecorded ? null : new RecordRefusal("UNIT-STATE-020", $"'{roll}' is not a recorded roll.");

    /// <summary>A selection names one distinct unit for each die.</summary>
    public static RecordRefusal? SelectionSubjectsRefusal(int subjects, int distinctSubjects, int dice) =>
        subjects != dice || distinctSubjects != subjects ? new RecordRefusal("UNIT-STATE-020", $"A selection names one distinct unit for each of the {dice} dice.") : null;

    /// <summary>Every subject is an active unit at the attempt's target.</summary>
    public static RecordRefusal? SelectionSubjectRefusal(bool activeAtTarget, string subject, string target) =>
        activeAtTarget ? null : new RecordRefusal("UNIT-STATE-020", $"'{subject}' is not an active unit at {target}.");

    /// <summary>A.9: the units with the highest value are the ones revealed, in the selection's order.</summary>
    public static IReadOnlyList<string> Revealing(IReadOnlyList<string> subjects, IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(subjects);
        ArgumentNullException.ThrowIfNull(values);
        var highest = values.Max();
        return [.. subjects.Where((_, index) => values[index] == highest)];
    }

    /// <summary>A12.15 (p. 78): the attacker's OVR choice is made once, by the attempt's unit, as declined or elected, after every selected unit is revealed.</summary>
    public static RecordRefusal? DeclarationRefusal(bool attemptOfUnit, string attempt, string unitId, bool choiceKnown, string choice, bool alreadyDeclared, bool everySelectedRevealed) =>
        !attemptOfUnit ? new RecordRefusal("UNIT-STATE-021", $"'{attempt}' is not an open entry attempt by '{unitId}'.")
        : !choiceKnown ? new RecordRefusal("UNIT-STATE-021", $"'{choice}' is not declined or elected.")
        : alreadyDeclared ? new RecordRefusal("UNIT-STATE-021", $"The attempt '{attempt}' already has a declaration.")
        : !everySelectedRevealed ? new RecordRefusal("UNIT-STATE-021", "A declaration follows the reveal of every selected unit.")
        : null;

    /// <summary>A10.1 (p. 65): the reviewed Task Check is the OVR NTC.</summary>
    public static RecordRefusal? TaskCheckPurposeRefusal(bool ovrNtc, string purpose) =>
        ovrNtc ? null : new RecordRefusal("UNIT-STATE-022", $"'{purpose}' is not a reviewed Task Check.");

    /// <summary>The NTC follows an elected OVR, once.</summary>
    public static RecordRefusal? TaskCheckAttemptRefusal(bool electedAwaitingNtc, string unitId) =>
        electedAwaitingNtc ? null : new RecordRefusal("UNIT-STATE-022", $"'{unitId}' has no elected OVR awaiting its NTC.");

    /// <summary>Its roll is two recorded dice of six sides.</summary>
    public static RecordRefusal? TaskCheckRollRefusal(bool rollRecorded, int count, int sides, string roll) =>
        !rollRecorded || count != 2 || sides != 6 ? new RecordRefusal("UNIT-STATE-022", $"'{roll}' is not a recorded roll of two dice.") : null;

    /// <summary>A10.1: the final DR is the dice plus every modifier, and it passes at or below the Morale Level.</summary>
    public static RecordRefusal? TaskCheckArithmeticRefusal(int diceSum, int modifierSum, int finalDr, bool passed, int moraleLevel)
    {
        var expected = diceSum + modifierSum;
        return finalDr != expected || passed != (finalDr <= moraleLevel)
            ? new RecordRefusal("UNIT-STATE-022", $"The final DR is {expected}, and against Morale Level {moraleLevel} it {(expected <= moraleLevel ? "passes" : "fails")}.")
            : null;
    }

    /// <summary>The Morale Level is the unit's printed morale.</summary>
    public static RecordRefusal? TaskCheckMoraleRefusal(int? printed, int moraleLevel) =>
        printed != moraleLevel ? new RecordRefusal("UNIT-STATE-022", $"The Morale Level {moraleLevel} is not the unit's printed morale.") : null;

    /// <summary>A12.15 (p. 78): a forced back names its attempt, by its unit, among its causes, for the attempt's MF.</summary>
    public static RecordRefusal? ForceBackRefusal(bool attemptOfUnit, string attempt, string unitId, bool attemptAmongCauses, int attemptMf, int forcedMf) =>
        !attemptOfUnit ? new RecordRefusal("UNIT-STATE-018", $"'{attempt}' is not an open entry attempt by '{unitId}'.")
        : !attemptAmongCauses ? new RecordRefusal("UNIT-STATE-018", "A forced back names its attempt among its causes.")
        : forcedMf != attemptMf ? new RecordRefusal("UNIT-STATE-018", $"The attempt cost {attemptMf} MF, not {forcedMf}.")
        : null;

    /// <summary>After an elected OVR the mover is forced back only in the two reviewed outcomes: a failed NTC, or a passed NTC followed by a second reveal (Scenario A1 OVR NTC Review).</summary>
    public static RecordRefusal? ForceBackElectionRefusal(bool elected, bool? taskCheckPassed, bool secondSelection, string attempt) =>
        elected && !(taskCheckPassed == false || secondSelection)
            ? new RecordRefusal("UNIT-STATE-021", $"The attempt '{attempt}' elected an OVR; it is forced back only after a failed NTC or a second reveal.")
            : null;

    /// <summary>A unit selected for the reveal is revealed before the forced back.</summary>
    public static RecordRefusal? ForceBackUnrevealedRefusal(string? unrevealed) =>
        unrevealed is not null ? new RecordRefusal("UNIT-STATE-020", $"'{unrevealed}' was selected for the reveal but is still concealed.") : null;

    /// <summary>The unit returns to where it is.</summary>
    public static RecordRefusal? ForceBackLocationRefusal(bool atReturn, string unitId, string returnedTo) =>
        atReturn ? null : new RecordRefusal("UNIT-STATE-018", $"'{unitId}' is not at {returnedTo}, the location it is forced back to.");

    /// <summary>A.9 (p. 43): a Random Selection decides which units an attempt reveals; no other unit at its target loses concealment.</summary>
    public static RecordRefusal? RevealOutsideSelectionRefusal(bool revealed, string? selectedAttempt, bool selected, string unitId) =>
        revealed && selectedAttempt is not null && !selected
            ? new RecordRefusal("UNIT-STATE-020", $"'{unitId}' was not selected for the reveal of the attempt '{selectedAttempt}'.")
            : null;

    /// <summary>A9.8 (table player, pass 13): dismantling or assembling a MG in the PFPh is its possessor's use of a SW, so it does not move in the MPh.</summary>
    public static bool DismantlingUsesSupportWeapon(bool possessed, string phase, bool dismantledChanged) => possessed && phase == "pfph" && dismantledChanged;
}
