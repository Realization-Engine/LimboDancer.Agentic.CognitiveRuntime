using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// Replays a game's events into its state at each revision (ASL-UNIT-040), checking the envelope, the model's
/// invariants (ASL-UNIT-021 to 026), and every map position against the location chains of the exact board versions
/// in play (ASL-UNIT-024). The first event that breaks a rule stops the replay; nothing after it is projected.
/// </summary>
public static class GameProjector
{
    public static GameHistory Project(IReadOnlyList<GameEvent> events, UnitVocabulary vocabulary, IReadOnlyList<UnitCatalog> catalogs,
        ILocationChains? chains = null, IReadOnlyCollection<string>? liveSources = null, IFireRecordVerifier? fire = null,
        IRallyRecordVerifier? rally = null, ICloseCombatRecordVerifier? closeCombat = null, IOrdnanceRecordVerifier? ordnance = null)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(vocabulary);
        ArgumentNullException.ThrowIfNull(catalogs);
        var diagnostics = new List<UnitDiagnostic>();
        var states = new List<GameState>();
        if (chains is null)
        {
            diagnostics.Add(UnitDiagnostic.Warning("UNIT-STATE-020", "No location chains were given, so map positions are not checked against the boards in play."));
        }

        var replay = new Replay(vocabulary, catalogs, chains, liveSources ?? [], fire, rally, closeCombat, ordnance, diagnostics);
        foreach (var gameEvent in events)
        {
            var errors = Errors(diagnostics);
            var state = replay.Apply(gameEvent, states.Count > 0 ? states[^1] : null);
            if (Errors(diagnostics) > errors || state is null)
            {
                break;
            }

            states.Add(state);
        }

        return new GameHistory(events, states, diagnostics);
    }

    private static int Errors(List<UnitDiagnostic> diagnostics) =>
        diagnostics.Count(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error);

    private sealed class Replay(UnitVocabulary vocabulary, IReadOnlyList<UnitCatalog> catalogs, ILocationChains? chains, IReadOnlyCollection<string> liveSources,
        IFireRecordVerifier? fireVerifier, IRallyRecordVerifier? rallyVerifier, ICloseCombatRecordVerifier? closeCombatVerifier,
        IOrdnanceRecordVerifier? ordnanceVerifier, List<UnitDiagnostic> diagnostics)
    {
        private readonly HashSet<string> eventIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DiceRolled> rolls = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (FireResolved Fire, bool Withheld, bool Reported)> fires = new(StringComparer.Ordinal);
        private UnitCatalog? catalog;
        private MapLayout? layout;
        private string path = string.Empty;

        public GameState? Apply(GameEvent gameEvent, GameState? previous)
        {
            path = $"revision {gameEvent.Revision} ({gameEvent.EventId})";
            if (!CheckEnvelope(gameEvent, previous))
            {
                return null;
            }

            var next = gameEvent.Payload switch
            {
                GameStarted started when previous is null => Start(gameEvent, started),
                _ when previous is null => Fail<GameState>("UNIT-STATE-004", "The first event must be game-started."),
                GameStarted => Fail<GameState>("UNIT-STATE-004", "A game starts only once."),
                _ when previous.Ended is not null => Fail<GameState>("UNIT-STATE-045", "The game has ended (A3.9; ruling R20.1); nothing more happens in it."),
                GameEnded ended => End(previous, ended),
                PhaseChanged phase => ChangePhase(previous, phase),
                InstanceCreated created => CreateInPlay(previous, created),
                InstanceMoved moved => Move(previous, moved),
                EquipmentTransferred transferred => Transfer(previous, transferred),
                ConditionsChanged changed => ChangeConditions(previous, changed),
                SetupConcealed concealed => SetupConceal(previous, concealed),
                LineageRecorded lineage => RecordLineage(previous, lineage),
                InstanceEliminated eliminated => Eliminate(previous, eliminated.Id),
                InstanceCaptured captured => Capture(previous, captured),
                EntryAttempted attempted => Attempt(previous, attempted, gameEvent.EventId),
                EntryForcedBack forced => ForceBack(previous, forced, gameEvent.Causes),
                DiceRolled rolled => Roll(previous, rolled),
                RandomSelection selection => Select(previous, selection),
                OverrunDeclared declared => Declare(previous, declared),
                TaskCheck check => Check(previous, check),
                FireResolved fire => Fire(previous, fire, gameEvent),
                FireReported report => Report(previous, report, gameEvent),
                ResidualFirePlaced residual => PlaceResidual(previous, residual),
                RallyAttempted rally => Rally(previous, rally, gameEvent),
                RepairAttempted repair => Repair(previous, repair),
                ShockRecoveryRolled shock => ShockRecovery(previous, shock),
                BoreSighted sighted => BoreSight(previous, sighted),
                GunTurned turned => TurnGun(previous, turned),
                ManhandlingRolled manhandling => Manhandle(previous, manhandling),
                GunHooked hooked => HookGun(previous, hooked),
                MovementStepped moving => StepMovement(previous, moving),
                VehicleStepped vehicle => StepVehicle(previous, vehicle),
                VehicleCheckRolled check => CheckVehicle(previous, check),
                OverrunResolved overrun => ResolveOverrun(previous, overrun),
                PaatcTaken paatc => TakePaatc(previous, paatc),
                OpportunityFireDeclared opportunity => DeclareOpportunityFire(previous, opportunity),
                RoutStepped routed => Rout(previous, routed),
                RoutInterdicted interdicted => Interdict(previous, interdicted),
                DeploymentAttempted deployment => AttemptDeployment(previous, deployment),
                RallyPhaseActionTaken rphAction => TakeRallyPhaseAction(previous, rphAction),
                RecoveryAttempted recovery => AttemptRecovery(previous, recovery),
                EncirclementPlaced encirclement => Encircle(previous, encirclement),
                FireLanePlaced lane => PlaceFireLane(previous, lane),
                MovementWindowClosed closed => CloseWindow(previous, closed),
                MovementEnded ended => EndMovement(previous, ended),
                AdvanceMoved advanced => Advance(previous, advanced),
                AmbushRolled ambush => Ambush(previous, ambush),
                CloseCombatResolved combat => CloseCombat(previous, combat),
                VehicleCloseCombatResolved vehicleCombat => VehicleCloseCombat(previous, vehicleCombat),
                VehicleCloseCombatPassed passed => PassVehicleCloseCombat(previous, passed),
                OrdnanceFired fired => Ordnance(previous, fired),
                SurrenderPending surrender => Surrender(previous, surrender, gameEvent.EventId),
                SurrenderRejected rejected => RejectSurrender(previous, rejected),
                PrisonerFreed freed => FreePrisoner(previous, freed),
                SniperAttacked sniper => Snipe(previous, sniper),
                WindChanged wind => ChangeWind(previous, wind),
                StarshellFired starshell => FireStarshell(previous, starshell),
                VehicleWrecked wreck => Wreck(previous, wreck),
                PrisonersMassacred massacre => Massacre(previous, massacre),
                ChoicePending pending => PendChoice(previous, pending, gameEvent.EventId),
                ChoiceMade made => MakeChoice(previous, made),
                AcquisitionChanged acquisition => ChangeAcquisition(previous, acquisition),
                _ => Fail<GameState>("UNIT-STATE-001", $"'{gameEvent.Type}' has no projection."),
            };

            if (next is null)
            {
                return null;
            }

            // A24.1 (table player, pass 9): a placed SMOKE counter is created by the event right after its attempt, or not at all.
            if (next.SmokePending is not null && gameEvent.Payload is not (MovementStepped or InstanceCreated))
            {
                next = next with
                {
                    SmokePending = null
                };
            }

            // C13.31 (ruling R9.7): setup ends with the first event of play; each side's Personnel then are its OB for the PF usage limit.
            if (previous is not null && !previous.SetupClosed && !GameState.IsSetupEvent(gameEvent.Payload))
            {
                next = next with
                {
                    SetupClosed = true,
                    SetupHalfSquads = previous.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side is not null)
                        .GroupBy(unit => unit.Side!, StringComparer.Ordinal)
                        .ToDictionary(group => group.Key, group => group.Sum(unit => unit.Kind == "asl:squad" ? 2 : unit.Kind is "asl:half-squad" or "asl:crew" ? 1 : 0),
                            StringComparer.Ordinal),
                };
            }

            // C6.1 Case J (ruling R6.1): a vehicle that enters a new hex, or moves while under a Motion counter, this Player Turn.
            if (gameEvent.Payload is VehicleStepped stepped && !next.MovedVehicles.Contains(stepped.Vehicle, StringComparer.Ordinal)
                && (stepped.Kind is VehicleStepped.Enter or VehicleStepped.Exit
                    || (previous?.Unit(stepped.Vehicle) is { } before && GameState.Condition(before, Conditions.Motion) == ConditionState.True)))
            {
                next = next with
                {
                    MovedVehicles = [.. next.MovedVehicles, stepped.Vehicle]
                };
            }

            // C6.17 (ruling R8.1): Defensive First Fire shots count per Location the moving stack enters.
            if (gameEvent.Payload is MovementStepped or VehicleStepped { Kind: VehicleStepped.Enter } && next.OrdnanceShotsHere.Count > 0)
            {
                next = next with
                {
                    OrdnanceShotsHere = []
                };
            }

            // A7.351 (table player, pass 9): the units of a fire record have fired this phase, with the SW each used.
            if (gameEvent.Payload is FireResolved firing && firing.Firers.Count > 0)
            {
                var used = firing.Facts.TryGetProperty("firers", out var firerFacts) && firerFacts.ValueKind == JsonValueKind.Array
                    ? firerFacts.EnumerateArray().ToDictionary(item => item.GetProperty("unitId").GetString() ?? string.Empty,
                        item => item.TryGetProperty("weapons", out var weapons) && weapons.ValueKind == JsonValueKind.Array ? weapons.GetArrayLength() : 0, StringComparer.Ordinal)
                    : [];
                next = next with
                {
                    PhaseFirers = [.. next.PhaseFirers.Where(item => !firing.Firers.Contains(item.Unit, StringComparer.Ordinal)),
                        .. firing.Firers.Select(unit => new SupportWeaponUse(unit, used.GetValueOrDefault(unit).ToString(System.Globalization.CultureInfo.InvariantCulture)))],
                };
            }

            // A7.351 (ruling R9.2): a squad that fires its inherent FP after its one SW use has fired.
            if (gameEvent.Payload is FireResolved resolved && next.SupportWeaponUses.Any(item => resolved.Firers.Contains(item.Unit, StringComparer.Ordinal)))
            {
                next = next with
                {
                    SupportWeaponUses = [.. next.SupportWeaponUses.Where(item => !resolved.Firers.Contains(item.Unit, StringComparer.Ordinal))]
                };
            }

            next = AssaultWeapons(next, gameEvent.Payload);
            next = KeepMovingStack(next, gameEvent.Payload);
            next = KeepEncirclements(next);
            next = KeepFireLanes(next);
            next = KeepGuards(next);
            next = KeepPlacedCharges(next, gameEvent.Payload);
            next = KeepMelee(next);
            next = KeepCx(next);
            next = KeepAcquisitions(next, gameEvent.Payload);
            next = next with
            {
                Revision = gameEvent.Revision,
                Time = gameEvent.Time
            };
            CheckInvariants(next);
            eventIds.Add(gameEvent.EventId);
            return next;
        }

        private bool CheckEnvelope(GameEvent gameEvent, GameState? previous)
        {
            var ok = true;
            if (previous is not null && gameEvent.Scope != previous.Scope)
            {
                ok = Error("UNIT-STATE-002", $"The event belongs to {gameEvent.Scope}, not {previous.Scope}.");
            }

            var expected = (previous?.Revision ?? 0) + 1;
            if (gameEvent.Revision != expected)
            {
                ok = Error("UNIT-STATE-003", $"Revisions rise by one: expected {expected}, found {gameEvent.Revision}.");
            }

            if (eventIds.Contains(gameEvent.EventId))
            {
                ok = Error("UNIT-STATE-003", $"The event id '{gameEvent.EventId}' is used twice.");
            }

            foreach (var cause in gameEvent.Causes.Where(cause => !eventIds.Contains(cause)))
            {
                ok = Error("UNIT-STATE-016", $"The cause '{cause}' is not an earlier event.");
            }

            var perspectives = previous?.Perspectives.Select(perspective => perspective.Name).ToHashSet(StringComparer.Ordinal)
                ?? (gameEvent.Payload is GameStarted started
                    ? [.. started.Sides.Select(side => side.Id), Perspective.AdjudicatorName]
                    : []);
            foreach (var name in gameEvent.Visibility?.Where(name => !perspectives.Contains(name)) ?? [])
            {
                ok = Error("UNIT-STATE-005", $"'{name}' is not a perspective of this game.");
            }

            return ok;
        }

        private GameState? Start(GameEvent gameEvent, GameStarted started)
        {
            if (!started.Synthetic && !liveSources.Contains(gameEvent.Source, StringComparer.Ordinal))
            {
                // ASL-UNIT-050, D2: a game that is not synthetic must come from an accepted live source.
                return Fail<GameState>("UNIT-STATE-017", $"'{gameEvent.Source}' is not an accepted live game source, so the game must be synthetic.");
            }

            if (started.Sides.Count == 0 || started.Sides.Select(side => side.Id).Distinct(StringComparer.Ordinal).Count() != started.Sides.Count
                || started.Sides.Any(side => !VocabularyNames.IsSlug(side.Id) || side.Id == Perspective.AdjudicatorName))
            {
                return Fail<GameState>("UNIT-STATE-005", "Sides need distinct slug ids other than 'adjudicator'.");
            }

            // Ruling R18.3 (referee, pass 18): a side's OB groups have distinct ids and ELRs of 0 to 5 (A19.1), and its own ELR, when it names one,
            // is the ELR its groups share.
            foreach (var side in started.Sides.Where(side => side.Groups.Count > 0))
            {
                var shared = side.Groups.Select(group => group.Elr).Distinct().ToArray();
                if (side.Groups.Select(group => group.Id).Distinct(StringComparer.Ordinal).Count() != side.Groups.Count
                    || side.Groups.Any(group => !VocabularyNames.IsSlug(group.Id) || group.Elr is < 0 or > 5)
                    || (side.Elr is { } sideElr && (shared.Length != 1 || shared[0] != sideElr)))
                {
                    return Fail<GameState>("UNIT-STATE-005",
                        $"The OB groups of side '{side.Id}' need distinct slug ids and ELRs of 0 to 5, and a side ELR only when all its groups share it (A19.1).");
                }
            }

            foreach (var side in started.Sides.Where(side => !vocabulary.TryGetSide(side.Nationality, out _)))
            {
                Error("UNIT-STATE-005", $"The nationality '{side.Nationality}' of side '{side.Id}' is not declared.");
            }

            catalog = catalogs.FirstOrDefault(candidate => $"{candidate.Identity.Catalog}@{candidate.Identity.Version}" == started.Catalog);
            if (catalog is null)
            {
                return Fail<GameState>("UNIT-STATE-008", $"The catalog '{started.Catalog}' is not loaded.");
            }

            if (started.Map.Boards.Count == 0)
            {
                return Fail<GameState>("UNIT-STATE-010", "The map in play places no boards.");
            }

            foreach (var board in started.Map.Boards)
            {
                if (chains?.Version(board.Board) is { } version && version != board.Version)
                {
                    Error("UNIT-STATE-010", $"The game plays {board.Board} version {board.Version}, but its location chains are for version {version}.");
                }
                else if (chains is not null && chains.Version(board.Board) is null)
                {
                    Error("UNIT-STATE-010", $"There are no location chains for {board.Board}.");
                }
            }

            CheckPlacement(started.Map);
            var state = new GameState(gameEvent.Scope, gameEvent.Revision, gameEvent.Time, started.Synthetic, started.Sides, started.Map,
                catalog.Identity, started.Turn, started.Phase, started.PhasingSide, [], [], [])
            {
                SpecialRules = started.SpecialRules,
                // E1.1, E3.51, E3.71 (backlog pass 16, rulings R16.1, R16.9): the SSRs' Base NVR and precipitation at the start.
                Nvr = GameState.NightRule(started.SpecialRules),
                Precipitation = GameState.PrecipitationRule(started.SpecialRules),
                Rained = GameState.PrecipitationRule(started.SpecialRules) is "rain" or "heavy-rain",
                ScenarioMonth = started.ScenarioMonth,
                ScenarioYear = started.ScenarioYear,
                ScenarioDefender = started.ScenarioDefender,
                Scenario = started.Scenario,
                FirstSide = started.PhasingSide,
                Source = gameEvent.Source,
            };
            return CheckPhase(state, started.Turn, started.Phase, started.PhasingSide) ? state : null;
        }

        /// <summary>The Turn counter reaches END (A3.9; ruling R20.1): after the current Game Turn, with nothing left open in it.</summary>
        private GameState? End(GameState state, GameEnded ended)
        {
            if (ended.Turn != state.Turn || state.OpenAttempts.Count > 0 || state.Choice is not null || state.PendingSurrenders.Count > 0
                || state.CloseCombats.Any(item => !item.Closed))
            {
                return Fail<GameState>("UNIT-STATE-045", "A game ends after its current Game Turn, with no entry attempt, choice, surrender, or CC left open (A3.9).");
            }

            return state with
            {
                Ended = ended,
            };
        }

        private GameState? ChangePhase(GameState state, PhaseChanged change)
        {
            if (change.Turn < state.Turn)
            {
                return Fail<GameState>("UNIT-STATE-015", $"Turn {change.Turn} is before the current turn {state.Turn}.");
            }

            // An entry attempt is resolved within its phase: the phase may not change while one is open.
            if (state.OpenAttempts.Count > 0)
            {
                return Fail<GameState>("UNIT-STATE-018", $"The entry attempt '{state.OpenAttempts[0].EventId}' is open, so the phase may not change.");
            }

            // Ruling R5.8: a pending choice is answered before anything else happens.
            if (state.Choice is { } choice)
            {
                return Fail<GameState>("UNIT-STATE-035", $"The {choice.Side} side's choice '{choice.Key}' is pending, so the phase may not change.");
            }

            // A15.5: a surrender waits for the captor's choice before anything else happens.
            if (state.PendingSurrenders.Count > 0)
            {
                return Fail<GameState>("UNIT-STATE-031", $"'{state.PendingSurrenders[0].Unit}' surrenders and awaits its captor, so the phase may not change (A15.5).");
            }

            // A11.12, A11.32: a Location's CC is completely resolved before the phase ends: after the ambusher's round, the other side's.
            if (state.CloseCombats.FirstOrDefault(item => !item.Closed) is { } open)
            {
                return Fail<GameState>("UNIT-STATE-030", open.Ambusher is null
                    ? $"The CC in {open.Location} awaits its round after the Ambush drs, even one with no attacks (A11.12)."
                    : $"The CC in {open.Location} awaits the ambushed side's round, which may declare no attacks (A11.32).");
            }

            // A11.16, A19.12: a broken or Disrupted unit held in Melee, other than a Guard, is eliminated at the end of the CCPh unless it
            // withdrew (ruling R29.11); its elimination is recorded before the phase changes.
            if (state.Phase == "ccph" && state.Units.FirstOrDefault(unit => unit.Status == InstanceStatus.Active
                && GameState.Condition(unit, Conditions.Melee) == ConditionState.True && GameState.Condition(unit, Conditions.Captured) != ConditionState.True
                && !state.Units.Any(prisoner => prisoner.Status == InstanceStatus.Active && prisoner.Custodian == unit.Id)
                && (GameState.Condition(unit, Conditions.Broken) == ConditionState.True || GameState.Condition(unit, Conditions.Disrupted) == ConditionState.True)) is { } held)
            {
                return Fail<GameState>("UNIT-STATE-030", $"'{held.Id}' is broken or Disrupted in Melee and is eliminated at the end of the CCPh (A11.16, A19.12).");
            }

            // MF are spent within a phase, so a new phase starts every unit at none spent and free to move. The phase that
            // ends removes its markers, from units and weapons alike: First and Final Fire at the end of the DFPh (A3.4), Prep
            // Fire at the end of the AFPh (A3.5), and pins in the CCPh (A3.8), all on p. 47; DM at the end of every RPh
            // (A10.62, p. 68). Residual FP is removed at the end of the MPh (A8.2, p. 60), and a new Player Turn gives every
            // unit a new Rally attempt (A10.6, p. 68).
            // E1.8 (backlog pass 16, ruling R16.2): at night First and Final Fire counters stay, as Gunflashes, until the end of the AFPh.
            string[] cleared = state.Phase switch
            {
                "dfph" when state.Night => [Conditions.IntensiveFire],
                "dfph" => [Conditions.FinalFire, Conditions.FirstFire, Conditions.IntensiveFire],
                "afph" when state.Night => [Conditions.PrepFire, Conditions.BoundingFire, Conditions.IntensiveFire, Conditions.FinalFire, Conditions.FirstFire],
                "afph" => [Conditions.PrepFire, Conditions.BoundingFire, Conditions.IntensiveFire],
                "ccph" => [Conditions.Pinned, "asl:ti"],

                // D7.21 (ruling R11.13): the CC counter of a CC Reaction Fire attack leaves with the MPh.
                "mph" => [Conditions.CcReaction],
                "rph" => [Conditions.DesperationMorale],
                _ => [],
            };
            var newPlayerTurn = change.PhasingSide != state.PhasingSide;
            var melee = state.Phase == "ccph" ? InMelee(state) : null;

            // D5.34, D5.341: at the end of the Player Turn in which it was placed, a Stun becomes Stun +1, and a Recall becomes Recall; +1,
            // after which the AFV must leave by its Friendly Board Edge (ruling R5.17).
            if (newPlayerTurn)
            {
                state = EndStuns(state);
            }

            // A4.51 (ruling R5.3): a side's CX counters leave at the start of its next MPh, and those units may not Double Time in it.
            string[] rested = change.Phase == "mph"
                ? [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == change.PhasingSide
                    && GameState.Condition(unit, Conditions.Cx) == ConditionState.True).Select(unit => unit.Id)]
                : [];
            foreach (var id in rested)
            {
                var unit = state.Unit(id)!;
                state = Replace(state, unit with
                {
                    Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { [Conditions.Cx] = ConditionState.False }
                });
            }

            // A11.19 (ruling R14.2): as the CCPh begins, Dummies sharing a Location with an enemy unit are removed, and hidden units there are placed
            // beneath a "?".
            var placed = new List<string>();
            if (change.Phase == "ccph")
            {
                (state, placed) = StartCloseCombat(state);
            }

            return CheckPhase(state, change.Turn, change.Phase, change.PhasingSide)
                ? state with
                {
                    HiddenPlaced = placed,
                    CloseCombats = [],
                    OrdnanceShots = [],
                    Advances = newPlayerTurn ? [] : state.Advances,
                    MovedVehicles = newPlayerTurn ? [] : state.MovedVehicles,
                    GunCrewsFired = newPlayerTurn ? [] : state.GunCrewsFired,
                    // A3.3 (table player, pass 12): a unit that fired only a SW in the PFPh has Prep Fired and does not move.
                    NoMoveThisPlayerTurn = newPlayerTurn ? [] : state.Phase == "pfph"
                        ? [.. state.NoMoveThisPlayerTurn, .. state.PhaseFirers.Select(item => item.Unit).Concat(state.SupportWeaponUses.Select(item => item.Unit)).Distinct().Where(id => !state.NoMoveThisPlayerTurn.Contains(id)
                            && state.Unit(id) is { } fired && GameState.Condition(fired, Conditions.PrepFire) != ConditionState.True)]
                        : state.NoMoveThisPlayerTurn,
                    OrdnanceShotsHere = [],
                    GunsTurnedThisPhase = [],
                    SmokeAttempts = [],
                    SupportWeaponUses = [],
                    SupportWeaponDirectors = [],
                    PhaseFirers = [],
                    SpottedThisPhase = [],
                    MovedWeapons = newPlayerTurn ? [] : state.MovedWeapons,
                    // A23.3 (ruling R15.2): a DC Placement lasts its Player Turn.
                    PlacedCharges = newPlayerTurn ? [] : state.PlacedCharges,
                    AssaultWeaponUsers = newPlayerTurn ? [] : state.AssaultWeaponUsers,
                    StarshellAttempts = [],
                    // E1.923 (ruling R16.8): Starshells are removed at the end of each CCPh.
                    Entities = state.Phase == "mph" ? RemoveSmokeGrenades(state.Entities)
                        : state.Phase == "ccph" ? [.. state.Entities.Where(entity => entity.Kind != "asl:starshell")]
                        : state.Entities,
                    Turn = change.Turn,
                    Phase = change.Phase,
                    PhasingSide = change.PhasingSide,
                    FiresThisPhase = [],
                    RepairsThisPhase = [],
                    ShockRollsThisPhase = [],
                    PaatcPassed = [],
                    FireLanes = [],
                    RoutedThisPhase = [],
                    RallyPhaseActions = [],
                    RecoveryAttempts = [],
                    RecoveredThisPhase = [],
                    ResidualFire = [],
                    Movement = null,
                    NoDoubleTime = rested,
                    RallyAttemptsThisPlayerTurn = newPlayerTurn ? [] : state.RallyAttemptsThisPlayerTurn,
                    FirstMmcRallyTaken = newPlayerTurn ? [] : state.FirstMmcRallyTaken,
                    Units = [.. state.Units.Select(unit => HoldInMelee(Clear(unit is { MfSpent: 0, MovementEnded: false, HalfMfSpent: false, DoubleTimeMf: 0, OffRoad: false, MovedWith: null, EsbMp: 0 }
                        ? unit
                        : unit with { MfSpent = 0, MovementEnded = false, HalfMfSpent = false, DoubleTimeMf = 0, OffRoad = false, MovedWith = null, EsbMp = 0 }, cleared), melee))],
                    Equipment = [.. state.Equipment.Select(equipment => equipment.Conditions.Keys.Any(cleared.Contains)
                        ? equipment with { Conditions = Without(equipment.Conditions, cleared) }
                        : equipment)],
                }
                : null;
        }

        /// <summary>A Smoke Placement Exponent as the unit's catalog definition prints it (A1.21); null when none is printed.</summary>
        private int? SmokeExponent(UnitInstance unit) => unit.Definition is { } reference
            ? catalogs.FirstOrDefault(catalog => catalog.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:smoke-exponent")?.Value?.Number
            : null;

        /// <summary>A24.11 (ruling R9.5): the 1/2" SMOKE counters of grenades leave at the end of the MPh they were placed in.</summary>
        private static IReadOnlyList<EntityInstance> RemoveSmokeGrenades(IReadOnlyList<EntityInstance> entities) =>
            [.. entities.Select(entity => entity.Status == InstanceStatus.Active && entity.Kind == "asl:smoke" && entity.Id.EndsWith(GameState.SmokeGrenadeSuffix, StringComparison.Ordinal)
                ? entity with { Status = InstanceStatus.Eliminated } : entity)];

        private static GameState EndStuns(GameState state)
        {
            var units = state.Units.Select(unit =>
            {
                if (unit.Status != InstanceStatus.Active)
                {
                    return unit;
                }

                // D5.341: a Recall's counter is flipped to its "Recall; +1" side: the AFV adds one as under Stun +1 and must now leave.
                if (GameState.Condition(unit, Conditions.Recalled) == ConditionState.True)
                {
                    return GameState.Condition(unit, Conditions.StunRecovery) == ConditionState.True
                        ? unit
                        : unit with
                        {
                            Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { [Conditions.StunRecovery] = ConditionState.True },
                        };
                }

                return GameState.Condition(unit, Conditions.Stunned) == ConditionState.True
                    ? unit with
                    {
                        Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal)
                        {
                            [Conditions.Stunned] = ConditionState.False,
                            [Conditions.StunRecovery] = ConditionState.True,
                        },
                    }
                    : unit;
            }).ToArray();
            return state with
            {
                Units = units
            };
        }

        /// <summary>
        /// The start of a CCPh (A11.19; ruling R14.2): in each Location holding units of both sides that are not prisoners, Dummies are removed and hidden
        /// units are placed beneath a "?"; the placed units are returned.
        /// </summary>
        private static (GameState State, List<string> Placed) StartCloseCombat(GameState state)
        {
            var placed = new List<string>();
            var units = state.Units.Where(unit => unit.Status == InstanceStatus.Active && GameState.Condition(unit, Conditions.Captured) != ConditionState.True
                && state.Location(unit.Id) is not null).ToArray();
            foreach (var group in units.GroupBy(unit => state.Location(unit.Id)!.Location).Where(group => group.Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count() > 1))
            {
                foreach (var unit in group.OrderBy(unit => unit.Id, StringComparer.Ordinal))
                {
                    if (unit.Kind == "asl:dummy")
                    {
                        state = Replace(state, unit with
                        {
                            Status = InstanceStatus.Eliminated
                        });
                    }
                    else if (GameState.Condition(unit, Conditions.Hidden) == ConditionState.True)
                    {
                        state = Replace(state, unit with
                        {
                            Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal)
                            {
                                [Conditions.Hidden] = ConditionState.False,
                                [Conditions.Concealed] = ConditionState.True,
                            },
                        });
                        placed.Add(unit.Id);
                    }
                }
            }

            return (state, placed);
        }

        /// <summary>
        /// A11.15: at the end of the CCPh, Infantry of both sides that remain in one Location are held in Melee; prisoners are
        /// neither held nor hold (A20.5). The units are those of Locations that still hold units of both sides.
        /// </summary>
        private HashSet<string> InMelee(GameState state)
        {
            // A11.7 (ruling R11.16): a vehicle is never held in Melee, and holds the enemy Infantry in its Location unless it is Abandoned or in Motion.
            // A11.15 EXC (ruling R14.2): a unit that keeps its "?" is neither held in Melee nor holds an enemy unit in Melee.
            var units = state.Units.Where(unit => unit.Status == InstanceStatus.Active && GameState.Condition(unit, Conditions.Captured) != ConditionState.True
                && GameState.Condition(unit, Conditions.Concealed) != ConditionState.True && GameState.Condition(unit, Conditions.Hidden) != ConditionState.True
                && unit.Kind != "asl:dummy" && state.Location(unit.Id) is not null).ToArray();
            return units.GroupBy(unit => state.Location(unit.Id)!.Location)
                .SelectMany(group => group.Where(unit => !vocabulary.IsA(unit.Kind, "asl:vehicle") && group.Any(other => other.Side != unit.Side && HoldsInMelee(other)
                    && (!vocabulary.IsA(other.Kind, "asl:vehicle") || (GameState.Condition(unit, Conditions.Concealed) != ConditionState.True
                        && GameState.Condition(unit, Conditions.Hidden) != ConditionState.True)))))
                .Select(unit => unit.Id).ToHashSet(StringComparer.Ordinal);
        }

        /// <summary>Whether a unit holds the enemy Infantry in its Location in Melee (A11.15, A11.7): any Infantry, or a vehicle not Abandoned and not in Motion.</summary>
        private bool HoldsInMelee(UnitInstance unit) =>
            !vocabulary.IsA(unit.Kind, "asl:vehicle")
            || (GameState.Condition(unit, Conditions.Abandoned) != ConditionState.True && GameState.Condition(unit, Conditions.Motion) != ConditionState.True);

        private static UnitInstance HoldInMelee(UnitInstance unit, HashSet<string>? melee) =>
            melee is null || (melee.Contains(unit.Id) ? GameState.Condition(unit, Conditions.Melee) == ConditionState.True
                : GameState.Condition(unit, Conditions.Melee) != ConditionState.True)
                ? unit
                : unit with
                {
                    Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal)
                    {
                        [Conditions.Melee] = melee.Contains(unit.Id) ? ConditionState.True : ConditionState.False
                    }
                };

        /// <summary>A11.15: a unit is no longer held in Melee once no enemy unit that is not a prisoner shares its Location.</summary>
        private static GameState KeepMelee(GameState next)
        {
            var freed = next.Units.Where(unit => unit.Status == InstanceStatus.Active && GameState.Condition(unit, Conditions.Melee) == ConditionState.True
                && next.Location(unit.Id) is { } at && !next.Units.Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side
                    && GameState.Condition(other, Conditions.Captured) != ConditionState.True && next.Location(other.Id)?.Location == at.Location)).ToArray();
            foreach (var unit in freed)
            {
                next = Replace(next, unit with
                {
                    Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { [Conditions.Melee] = ConditionState.False }
                });
            }

            return next;
        }

        /// <summary>
        /// An advance in the APh (A4.7): units of the phasing side in one Location, neither broken, pinned, berserk (A15.431), held
        /// in Melee (A11.15), nor captured, that have not advanced this APh, enter one Location; a Guard's prisoners go with it
        /// (A20.53). The planner checks the terrain and ADJACENCY.
        /// </summary>
        private GameState? Advance(GameState state, AdvanceMoved advance)
        {
            var units = advance.Units.Select(id => Active(state, id) as UnitInstance).ToArray();
            if (state.Phase != "aph" || units.Length == 0 || units.Any(unit => unit is null || unit.Side != state.PhasingSide || unit.MovementEnded)
                || units.Select(unit => state.Location(unit!.Id)?.Location).Distinct().Count() != 1
                || advance.Units.Distinct(StringComparer.Ordinal).Count() != advance.Units.Count)
            {
                return Fail<GameState>("UNIT-STATE-032", "An advance moves units of the phasing side from one Location, once each, in the APh (A4.7).");
            }

            if (units.FirstOrDefault(unit => new[] { Conditions.Broken, Conditions.Pinned, Conditions.Berserk, Conditions.Melee, Conditions.Captured }
                .Any(condition => GameState.Condition(unit!, condition) == ConditionState.True)) is { } barred)
            {
                return Fail<GameState>("UNIT-STATE-032", $"'{barred.Id}' is broken, pinned, berserk, in Melee, or captured, so it may not advance (A4.7, A15.431, A11.15).");
            }

            var next = state;
            foreach (var unit in units)
            {
                next = Replace(next, unit! with
                {
                    Position = new MapPosition(advance.To),
                    MovementEnded = true
                })!;
                next = MovePrisoners(next, unit.Id, new MapPosition(advance.To));
            }

            return next with
            {
                Advances = [.. next.Advances, .. advance.Units.Select(id => new AdvanceRecord(id, advance.To))],
            };
        }

        /// <summary>A20.53: a Guard's prisoners move with it.</summary>
        private static GameState MovePrisoners(GameState state, string guard, Position position)
        {
            foreach (var prisoner in state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Custodian == guard).ToArray())
            {
                state = Replace(state, prisoner with
                {
                    Position = position
                });
            }

            return state;
        }

        /// <summary>
        /// The Ambush drs of a Location (A11.4): once, in the CCPh, before any CC there, reproduced by the Close Combat verifier,
        /// which also decides that an Ambush can occur there.
        /// </summary>
        private GameState? Ambush(GameState state, AmbushRolled ambush)
        {
            if (closeCombatVerifier is null)
            {
                return Fail<GameState>("UNIT-STATE-023", "An Ambush record can be replayed only with the Close Combat verifier.");
            }

            if (ambush.Rolls.Values.FirstOrDefault(roll => !rolls.ContainsKey(roll)) is { } missing)
            {
                return Fail<GameState>("UNIT-STATE-023", $"The Ambush record's roll '{missing}' is not recorded before it.");
            }

            if (state.Phase != "ccph" || state.CloseCombats.Any(item => item.Location == ambush.Location || !item.Closed))
            {
                return Fail<GameState>("UNIT-STATE-030", "The Ambush drs are made in the CCPh before any CC in the Location, with no other Location's CC open (A11.4, A11.12).");
            }

            if (closeCombatVerifier.VerifyAmbush(state, ambush, rolls) is { } reason)
            {
                return Fail<GameState>("UNIT-STATE-030", reason);
            }

            return state with
            {
                CloseCombats = [.. state.CloseCombats, new CloseCombatLocation(ambush.Location, true, ambush.Ambusher, [], false)],
            };
        }

        /// <summary>
        /// A round of CC in a Location (A11.12, A11.32): in the CCPh, with no other Location's CC open; with an Ambush the
        /// ambusher's round first and then the other side's, otherwise one round; no unit attacks or is attacked twice. The
        /// Close Combat verifier reproduces it and decides that the Location's Ambush drs were made where they had to be.
        /// </summary>
        /// <summary>
        /// One CC attack with a vehicle (rulings R11.13 to R11.16): CC Reaction Fire answers the open window on a vehicle's MP expenditure in the MPh
        /// (D7.21); in the CCPh the attacks in a Location holding a vehicle are sequential, the side named next attacking (A11.31), no Infantry unit
        /// attacking twice; the record's next side and closure are the planner's reads.
        /// </summary>
        private GameState? VehicleCloseCombat(GameState state, VehicleCloseCombatResolved combat)
        {
            if (closeCombatVerifier is null)
            {
                return Fail<GameState>("UNIT-STATE-023", "A CC record can be replayed only with the Close Combat verifier.");
            }

            if (combat.Rolls.Values.FirstOrDefault(roll => !rolls.ContainsKey(roll)) is { } missing)
            {
                return Fail<GameState>("UNIT-STATE-023", $"The CC record's roll '{missing}' is not recorded before it.");
            }

            if (combat.Reaction)
            {
                if (state.Phase != "mph" || state.Movement is not { WindowOpen: true, Vehicle: true } window || !window.Members.Contains(combat.Vehicle, StringComparer.Ordinal))
                {
                    return Fail<GameState>("UNIT-STATE-030", "CC Reaction Fire answers the open window on the moving vehicle's MP expenditure (D7.21).");
                }
            }
            else
            {
                var entry = state.CloseCombats.FirstOrDefault(item => item.Location == combat.Location);
                var attacker = combat.ByVehicle ? Active(state, combat.Vehicle) as UnitInstance : combat.Attackers.Select(id => Active(state, id) as UnitInstance).FirstOrDefault();
                if (state.Phase != "ccph" || entry is { Closed: true } || attacker is null || (entry?.Next is { } next && next != attacker.Side)
                    || combat.Attackers.Any(id => entry?.Attacking.Contains(id) == true) || state.CloseCombats.Any(item => !item.Closed && item.Location != combat.Location))
                {
                    return Fail<GameState>("UNIT-STATE-030", "CC with a vehicle is sequential in the CCPh: the side named attacks next, and no unit attacks twice (A11.31).");
                }
            }

            if (closeCombatVerifier.VerifyVehicle(state, combat, rolls) is { } reason)
            {
                return Fail<GameState>("UNIT-STATE-030", reason);
            }

            if (combat.Reaction)
            {
                return state;
            }

            var current = state.CloseCombats.FirstOrDefault(item => item.Location == combat.Location);
            var updated = (current ?? new CloseCombatLocation(combat.Location, false, null, [], false)) with
            {
                Rounds = [.. current?.Rounds ?? [], CloseCombatResolved.Simultaneous],
                Attacking = [.. current?.Attacking ?? [], .. combat.Attackers],
                Attacked = [.. current?.Attacked ?? [], .. combat.Defenders],
                Next = combat.Next,
                Closed = combat.Closed,
            };
            return state with
            {
                CloseCombats = [.. state.CloseCombats.Where(item => item.Location != combat.Location), updated],
            };
        }

        private GameState? PassVehicleCloseCombat(GameState state, VehicleCloseCombatPassed passed)
        {
            var entry = state.CloseCombats.FirstOrDefault(item => item.Location == passed.Location);
            if (state.Phase != "ccph" || entry is { Closed: true } || (entry?.Next is { } next && next != passed.Side) || entry?.Passed.Contains(passed.Side) == true
                || state.Side(passed.Side) is null)
            {
                return Fail<GameState>("UNIT-STATE-030", "A side passes in a CC Location holding a vehicle when its attack is next (A11.31).");
            }

            var updated = (entry ?? new CloseCombatLocation(passed.Location, false, null, [], false)) with
            {
                Passed = [.. entry?.Passed ?? [], passed.Side],
                Next = passed.Next,
                Closed = passed.Closed,
            };
            return state with
            {
                CloseCombats = [.. state.CloseCombats.Where(item => item.Location != passed.Location), updated],
            };
        }

        private GameState? CloseCombat(GameState state, CloseCombatResolved combat)
        {
            if (closeCombatVerifier is null)
            {
                return Fail<GameState>("UNIT-STATE-023", "A CC record can be replayed only with the Close Combat verifier.");
            }

            if (combat.Rolls.Values.FirstOrDefault(roll => !rolls.ContainsKey(roll)) is { } missing)
            {
                return Fail<GameState>("UNIT-STATE-023", $"The CC record's roll '{missing}' is not recorded before it.");
            }

            var entry = state.CloseCombats.FirstOrDefault(item => item.Location == combat.Location);
            if (state.Phase != "ccph" || entry is { Closed: true } || state.CloseCombats.Any(item => !item.Closed && item.Location != combat.Location))
            {
                return Fail<GameState>("UNIT-STATE-030", "CC is resolved in the CCPh, once per Location, one Location at a time (A11.12).");
            }

            // A11.3: the ambusher's attacks are sequential, one record each, until the ambushed side's round closes the Location; A11.33, A11.34
            // (ruling R14.6): the prisoners' escape round comes before any other.
            var rounds = entry?.Rounds.Where(item => item != CloseCombatResolved.PrisonersRound).ToArray() ?? [];
            // A11.41 (referee, pass 14): with every ambusher gone by Ambush Withdrawal, the ambushed side's round closes the Location.
            var ambushers = entry?.Ambusher is { } ambusher && state.Units.Any(unit => unit.Status == InstanceStatus.Active && unit.Side == ambusher
                && GameState.Condition(unit, Conditions.Captured) != ConditionState.True && state.Location(unit.Id)?.Location == combat.Location);
            string[] allowed = entry?.Ambusher is null ? [CloseCombatResolved.Simultaneous]
                : !ambushers ? [CloseCombatResolved.AmbushedRound]
                : rounds.Length == 0 ? [CloseCombatResolved.AmbusherRound]
                : [CloseCombatResolved.AmbusherRound, CloseCombatResolved.AmbushedRound];
            if (entry is null || entry.Rounds.Count == 0)
            {
                allowed = [.. allowed, CloseCombatResolved.PrisonersRound];
            }
            var attacking = entry?.Attacking ?? [];
            var attacked = entry?.Attacked ?? [];
            if (!allowed.Contains(combat.Round) || combat.Attackers.Any(attacking.Contains) || combat.Defenders.Any(attacked.Contains))
            {
                return Fail<GameState>("UNIT-STATE-030", $"The next CC round in {combat.Location} is {string.Join(" or ", allowed)}, and no unit attacks or is attacked twice (A11.3, A11.12, A11.32).");
            }

            if (closeCombatVerifier.Verify(state, combat, rolls) is { } reason)
            {
                return Fail<GameState>("UNIT-STATE-030", reason);
            }

            var updated = (entry ?? new CloseCombatLocation(combat.Location, false, null, [], false)) with
            {
                Rounds = [.. entry?.Rounds ?? [], combat.Round],
                Closed = combat.Round is not (CloseCombatResolved.AmbusherRound or CloseCombatResolved.PrisonersRound),
                Attacking = [.. attacking, .. combat.Attackers],
                Attacked = [.. attacked, .. combat.Defenders],

                // J2.31 (ruling R14.1): the first round declares the Location Hand-to-Hand for the CCPh.
                HandToHand = entry?.HandToHand == true || (combat.Round != CloseCombatResolved.PrisonersRound && combat.Facts.TryGetProperty("handToHand", out var hand)
                    && hand.ValueKind == JsonValueKind.True),
            };
            return state with
            {
                CloseCombats = [.. state.CloseCombats.Where(item => item.Location != combat.Location), updated],
            };
        }

        /// <summary>
        /// A Gun's shot (C3.3; unit step 24): in a fire phase, by an active Gun manned by an active crew in its Location, that has not
        /// used up its fire this phase; the Ordnance verifier reproduces it. The shot turns the Gun when it names a facing, counts
        /// against its Multiple ROF, and sets its Acquisition.
        /// </summary>
        private GameState? Ordnance(GameState state, OrdnanceFired fired)
        {
            if (ordnanceVerifier is null)
            {
                return Fail<GameState>("UNIT-STATE-023", "An ordnance record can be replayed only with the Ordnance verifier.");
            }

            if (fired.Rolls.Values.FirstOrDefault(roll => !rolls.ContainsKey(roll)) is { } missing)
            {
                return Fail<GameState>("UNIT-STATE-023", $"The ordnance record's roll '{missing}' is not recorded before it.");
            }

            var shots = state.OrdnanceShots.FirstOrDefault(item => item.Gun == fired.Gun);
            // D1.3 (ruling R7.10): an AFV's MA is fired by the AFV itself, its crew inherent.
            var firer = state.Find(fired.Gun);
            var tank = firer is UnitInstance { Status: InstanceStatus.Active } vehicle && vocabulary.IsA(vehicle.Kind, "asl:vehicle") && fired.Crew == vehicle.Id;

            // C9.2, C13.3 (rulings R9.2, R9.7): a light mortar is fired by the unit possessing it; a PF, named "<unit>:pf", by the unit making the check.
            // Rulings R9.10, R9.11: an ATR or PSK is fired by its possessor too.
            var mortar = firer is EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } possession } weapon
                && (vocabulary.IsA(weapon.Kind, "asl:light-mortar") || vocabulary.IsA(weapon.Kind, "asl:latw")) && possession.Holder == fired.Crew;
            var panzerfaust = fired.Gun == fired.Crew + ":pf";
            if (state.Phase is not ("pfph" or "afph" or "dfph" or "mph")
                || !(tank || mortar || panzerfaust || (firer is EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Manned } manning } && manning.Holder == fired.Crew))
                || Active(state, fired.Crew) is not UnitInstance shooter
                || (shots is not null && !shots.RateOfFireKept && !panzerfaust && !(fired.Facts.TryGetProperty("intensiveFire", out var intensive) && intensive.ValueKind == JsonValueKind.True)))
            {
                return Fail<GameState>("UNIT-STATE-033", "A Gun fires in a fire phase, manned by its crew, and again only on a kept Multiple ROF (C2.24).");
            }

            if (ordnanceVerifier.Verify(state, fired, rolls) is { } reason)
            {
                return Fail<GameState>("UNIT-STATE-033", reason);
            }

            if (Choices(state, fired.Facts.TryGetProperty("hit", out var hitFacts) && hitFacts.ValueKind == JsonValueKind.Object ? hitFacts : fired.Facts) is { } choiceReason)
            {
                return Fail<GameState>("UNIT-STATE-035", choiceReason);
            }

            state = state with
            {
                ChoicesMade = new Dictionary<string, string>(StringComparer.Ordinal)
            };

            // C8.9 (ruling R7.6): Special Ammunition used at its Depletion Number runs out; above it the Gun had none, may not use it again,
            // and was never fired: nothing else about the Gun changes.
            var use = fired.Resolution.TryGetProperty("ammunitionUse", out var used) && used.ValueKind == JsonValueKind.String ? used.GetString() : null;
            var malfunctioned = fired.Resolution.TryGetProperty("gun", out var gunResult) && gunResult.ValueKind == JsonValueKind.Object && gunResult.TryGetProperty("malfunctioned", out var broke) && broke.ValueKind == JsonValueKind.True;
            if (use is "depleted" or "none" && fired.Facts.TryGetProperty("ammunition", out var ammunition) && ammunition.GetString() is { } depleted)
            {
                state = state with
                {
                    DepletedAmmunition = [.. state.DepletedAmmunition, new DepletedAmmunition(fired.Gun, depleted)]
                };
            }

            // C8.9: unless the Gun malfunctioned, when the shot counts as fired.
            if (use == "none" && !malfunctioned)
            {
                return state;
            }

            // C3.21, D3.12: a Gun turns its barrel; an AFV turns its turret, whose TCA keeps its direction apart from the hull's VCA. A SW has no CA.
            if (tank)
            {
                state = fired.Facing is { } turret
                    ? state with
                    {
                        TurretFacings = [.. state.TurretFacings.Where(item => item.Vehicle != fired.Gun), new TurretFacing(fired.Gun, turret)]
                    }
                    : state;
            }
            else if (!panzerfaust && !mortar)
            {
                var gun = (EquipmentInstance)firer!;
                var turned = fired.Facing is { } facing && gun.Position is MapPosition map
                    ? gun with
                    {
                        Position = map with
                        {
                            Facing = facing
                        }
                    }
                    : gun;
                state = state with
                {
                    Equipment = [.. state.Equipment.Select(item => item.Id == gun.Id ? turned : item)]
                };
            }

            // C6.17: a Defensive First Fire shot counts against the moving stack's Location; A7.352: a crew that fires its Gun loses its inherent FP.
            var here = state.OrdnanceShotsHere.FirstOrDefault(item => item.Gun == fired.Gun);
            state = state with
            {
                OrdnanceShotsHere = state.Phase == "mph"
                    ? [.. state.OrdnanceShotsHere.Where(item => item.Gun != fired.Gun), new OrdnanceShotRecord(fired.Gun, (here?.Shots ?? 0) + 1, fired.RateOfFireKept)
                    {
                        Mp = fired.Facts.TryGetProperty("movement", out var moved) && moved.TryGetProperty("spentHere", out var spent) && spent.ValueKind == JsonValueKind.Number
                            ? spent.GetInt32() : 0,
                    }]
                    : state.OrdnanceShotsHere,
                GunCrewsFired = tank || panzerfaust || state.GunCrewsFired.Contains(fired.Crew) ? state.GunCrewsFired : [.. state.GunCrewsFired, fired.Crew],
            };
            state = SupportWeapons(state, fired, shooter, mortar, panzerfaust);
            return state with
            {
                OrdnanceShots = [.. state.OrdnanceShots.Where(item => item.Gun != fired.Gun), new OrdnanceShotRecord(fired.Gun, (shots?.Shots ?? 0) + 1, fired.RateOfFireKept)],
                Acquisitions = [.. state.Acquisitions.Where(item => item.Gun != fired.Gun),
                    .. fired.Acquired is { } acquired && fired.Acquisition < 0 ? new[] { new GunAcquisition(fired.Gun, acquired, fired.Acquisition) } : []],
            };
        }

        /// <summary>
        /// The backlog pass 9 bookkeeping of a SW's shot (rulings R9.2, R9.4, R9.7): a mortar's Spotter, the PF shots of the firer's side, and the
        /// squads whose only fire this phase is one SW use (A7.351): the firer of a mortar or a PF, and a squad Spotter.
        /// </summary>
        private static GameState SupportWeapons(GameState state, OrdnanceFired fired, UnitInstance shooter, bool mortar, bool panzerfaust)
        {
            if (!mortar && !panzerfaust)
            {
                return state;
            }

            static bool Marked(UnitInstance unit) => new[] { Conditions.PrepFire, Conditions.FinalFire, Conditions.FirstFire }
                .Any(name => GameState.Condition(unit, name) == ConditionState.True);
            IReadOnlyList<SupportWeaponUse> Use(IReadOnlyList<SupportWeaponUse> uses, UnitInstance unit, string weapon)
            {
                var existing = uses.FirstOrDefault(item => item.Unit == unit.Id);
                return unit.Kind != "asl:squad" ? uses
                    : existing is null ? (Marked(unit) ? uses : [.. uses, new SupportWeaponUse(unit.Id, weapon)])
                    : existing.Weapon != weapon || weapon == "panzerfaust" ? [.. uses.Where(item => item.Unit != unit.Id)]
                    : uses;
            }

            var uses = Use(state.SupportWeaponUses, shooter, panzerfaust ? "panzerfaust" : fired.Gun);
            var spotters = state.MortarSpotters;
            if (fired.Facts.TryGetProperty("spotter", out var spotter) && spotter.TryGetProperty("unitId", out var spotterId) && spotterId.GetString() is { } id)
            {
                spotters = [.. spotters.Where(item => item.Gun != fired.Gun), new MortarSpotter(fired.Gun, id)];
                state = state with
                {
                    SpottedThisPhase = [.. state.SpottedThisPhase.Where(item => item.Spotter != id), new MortarSpotter(fired.Gun, id)]
                };
                if (state.Unit(id) is { } spotting)
                {
                    uses = Use(uses, spotting, fired.Gun);
                }
            }

            var shots = state.PanzerfaustShots;
            if (panzerfaust && fired.Resolution.TryGetProperty("panzerfaustCheck", out var check) && check.TryGetProperty("outcome", out var outcome)
                && outcome.GetString() == "shot" && shooter.Side is { } side)
            {
                shots = new Dictionary<string, int>(shots, StringComparer.Ordinal) { [side] = shots.GetValueOrDefault(side) + 1 };
            }

            var directors = state.SupportWeaponDirectors;
            if (fired.Facts.TryGetProperty("director", out var director) && director.TryGetProperty("unitId", out var directorId) && directorId.GetString() is { } leader)
            {
                directors = [.. directors.Where(item => item.Gun != fired.Gun), new SupportWeaponDirector(fired.Gun, leader)];
            }

            return state with
            {
                SupportWeaponUses = uses,
                MortarSpotters = spotters,
                PanzerfaustShots = shots,
                SupportWeaponDirectors = directors,
            };
        }

        /// <summary>
        /// A surrender awaiting its captor (A15.5): the unit is active, broken, and not yet captured; each captor is an active
        /// enemy unit. It is resolved by the capture.
        /// </summary>
        private GameState? Surrender(GameState state, SurrenderPending surrender, string eventId)
        {
            if (Active(state, surrender.Unit) is not UnitInstance unit || GameState.Condition(unit, Conditions.Broken) != ConditionState.True
                || GameState.Condition(unit, Conditions.Captured) == ConditionState.True || surrender.Captors.Count == 0
                || surrender.Captors.Any(id => state.Unit(id) is not { Status: InstanceStatus.Active } captor || captor.Side == unit.Side)
                || state.PendingSurrenders.Any(item => item.Unit == unit.Id))
            {
                return Fail<GameState>("UNIT-STATE-031", "A surrender names a broken unit and the enemy units it may surrender to (A15.5).");
            }

            return state with
            {
                PendingSurrenders = [.. state.PendingSurrenders, new PendingSurrender(unit.Id, surrender.Captors, eventId)],
            };
        }

        /// <summary>
        /// Ruling R5.8: a record that resolves after pending choices declares exactly the answers given to them, in its facts' <c>choices</c>;
        /// one with no choices declares none. No choice may be pending when a record is made. Null when the record agrees.
        /// </summary>
        private static string? Choices(GameState state, JsonElement facts)
        {
            if (state.Choice is { } pending)
            {
                return $"The {pending.Side} side's choice '{pending.Key}' is pending, so no record resolves before it is answered.";
            }

            var declared = new Dictionary<string, string>(StringComparer.Ordinal);
            if (facts.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Object)
            {
                foreach (var item in choices.EnumerateObject())
                {
                    declared[item.Name] = item.Value.ValueKind == JsonValueKind.String ? item.Value.GetString()! : item.Value.ToString();
                }
            }

            return declared.Count == state.ChoicesMade.Count && declared.All(item => state.ChoicesMade.TryGetValue(item.Key, out var made) && made == item.Value)
                ? null
                : $"The record declares the choices {Describe(declared)}, but the choosing sides answered {Describe(state.ChoicesMade)} (ruling R5.8).";

            static string Describe(IReadOnlyDictionary<string, string> map) =>
                map.Count == 0 ? "none" : string.Join(", ", map.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => $"{item.Key}={item.Value}"));
        }

        /// <summary>A pending choice (ruling R5.8): one at a time, by a side of the game, with the options it may answer.</summary>
        private GameState? PendChoice(GameState state, ChoicePending pending, string eventId)
        {
            if (state.Choice is not null || state.Side(pending.Side) is null || pending.Options.Count == 0
                || pending.Kind is not (ChoicePending.LeaderCreation or ChoicePending.BattleHardening or ChoicePending.UnlikelyKill or ChoicePending.Acquisition or ChoicePending.Paatc)
                || state.ChoicesMade.ContainsKey(pending.Key))
            {
                return Fail<GameState>("UNIT-STATE-035", "A choice is pending one at a time, for a side of the game, with its options, and once per key (ruling R5.8).");
            }

            return state with
            {
                Choice = new PendingChoice(pending.Key, pending.Kind, pending.Side, pending.Options, eventId, pending.Resume),
            };
        }

        /// <summary>
        /// The choosing side's answer (ruling R5.8): one of the pending choice's options. An Acquisition choice keeps the counter on the
        /// acquired units in the chosen Location (C6.51); every other answer waits for the record that resolves with it.
        /// </summary>
        private GameState? MakeChoice(GameState state, ChoiceMade made)
        {
            if (state.Choice is not { } pending || pending.Key != made.Key || !pending.Options.Contains(made.Option, StringComparer.Ordinal))
            {
                return Fail<GameState>("UNIT-STATE-035", $"'{made.Key}' is not the pending choice, or '{made.Option}' is not one of its options (ruling R5.8).");
            }

            var next = state with
            {
                Choice = null
            };
            // A12.41 (ruling R11.12): the reveal or the PAATC follows the answer at once, as its own events.
            if (pending.Kind == ChoicePending.Paatc)
            {
                return next;
            }

            if (pending.Kind != ChoicePending.Acquisition)
            {
                return next with
                {
                    ChoicesMade = new Dictionary<string, string>(state.ChoicesMade, StringComparer.Ordinal) { [made.Key] = made.Option },
                };
            }

            var gun = pending.Resume.TryGetProperty("gun", out var gunElement) ? gunElement.GetString() : null;
            var acquisition = state.Acquisitions.FirstOrDefault(item => item.Gun == gun);
            if (acquisition is null || !BoardLocation.TryParse(made.Option, out var kept))
            {
                return Fail<GameState>("UNIT-STATE-035", "An Acquisition choice names a Gun with an Acquisition and one of its Locations (C6.51).");
            }

            return next with
            {
                Acquisitions = [.. state.Acquisitions.Select(item => item.Gun != gun ? item : item with
                {
                    Location = kept,
                    Units = [.. acquisition.Units.Where(id => state.Location(id)?.Location == kept)],
                })],
            };
        }

        /// <summary>
        /// D10.1 (ruling R6.5): a destroyed vehicle becomes a wreck at its Location, on its wreck face and out of Motion; it no longer acts.
        /// </summary>
        private GameState? Wreck(GameState state, VehicleWrecked wreck)
        {
            if (Active(state, wreck.Id) is not UnitInstance vehicle || !vocabulary.IsA(vehicle.Kind, "asl:vehicle") || state.Location(vehicle.Id) is null)
            {
                return Fail<GameState>("UNIT-STATE-037", $"'{wreck.Id}' is not an active vehicle on the map, so it cannot become a wreck (D10.1).");
            }

            var conditions = new Dictionary<string, ConditionState>(vehicle.Conditions, StringComparer.Ordinal)
            {
                [Conditions.Wrecked] = ConditionState.True,
                [Conditions.Motion] = ConditionState.False,
                [Conditions.Concealed] = ConditionState.False,
                [Conditions.Hidden] = ConditionState.False,
            };
            // C10.1 (ruling R8.6): a Gun in tow is destroyed with its vehicle.
            var wrecked = Replace(state, vehicle with
            {
                Status = InstanceStatus.Wrecked,
                Conditions = conditions
            })!;
            wrecked = wrecked with
            {
                Equipment = [.. wrecked.Equipment.Select(item => item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id
                    ? item with { Status = InstanceStatus.Eliminated, Holding = null }
                    : item)],
            };
            return DropHeldBy(wrecked, [vehicle.Id]);
        }

        /// <summary>A20.3 (ruling R5.6): the captor's side rejects a pending surrender; the unit is eliminated and its side faced with No Quarter.</summary>
        private GameState? RejectSurrender(GameState state, SurrenderRejected rejected)
        {
            if (state.PendingSurrenders.FirstOrDefault(item => item.Unit == rejected.Unit) is not { } pending || Active(state, pending.Unit) is not UnitInstance unit)
            {
                return Fail<GameState>("UNIT-STATE-031", $"'{rejected.Unit}' has no pending surrender to reject (A20.3).");
            }

            var next = Eliminate(state with
            {
                PendingSurrenders = [.. state.PendingSurrenders.Where(item => item.Unit != unit.Id)],
            }, unit.Id);
            return next is null ? null : next with
            {
                NoQuarter = next.NoQuarter.Contains(unit.Side, StringComparer.Ordinal) ? next.NoQuarter : [.. next.NoQuarter, unit.Side],
            };
        }

        /// <summary>
        /// A20.4 (ruling R5.7): prisoners eliminated by units of their Guard's side that may massacre (Russian or berserk Infantry, not in
        /// Melee), in the same Location, in a fire phase of that side. The victims' side is faced with No Quarter, and its ELR is raised by one,
        /// once, to at most 6. A berserk massacre returns its units to normal.
        /// </summary>
        private GameState? Massacre(GameState state, PrisonersMassacred massacre)
        {
            var units = massacre.Units.Select(state.Unit).ToArray();
            var prisoners = massacre.Prisoners.Select(state.Unit).ToArray();
            if (units.Length == 0 || prisoners.Length == 0 || units.Any(unit => unit is not { Status: InstanceStatus.Active })
                || prisoners.Any(unit => unit is not { Status: InstanceStatus.Active }))
            {
                return Fail<GameState>("UNIT-STATE-036", "A Massacre names active units and the active prisoners they eliminate (A20.4).");
            }

            var side = units[0]!.Side;
            var at = state.Location(units[0]!.Id)?.Location;
            var firePhase = state.Phase is "pfph" or "afph" ? state.PhasingSide == side : state.Phase == "dfph" && state.PhasingSide != side;
            string Nationality(UnitInstance unit) => unit.Definition is { } reference ? catalog?.Definition(reference.Definition)?.Nationality ?? string.Empty : string.Empty;
            if (!firePhase || at is null
                || units.Any(unit => unit!.Side != side || state.Location(unit.Id)?.Location != at || !vocabulary.IsA(unit.Kind, "asl:personnel")
                    || GameState.Condition(unit, Conditions.Melee) == ConditionState.True || GameState.Condition(unit, Conditions.Captured) == ConditionState.True
                    || (massacre.Berserk ? GameState.Condition(unit, Conditions.Berserk) != ConditionState.True
                        : GameState.Condition(unit, Conditions.Berserk) != ConditionState.True && Nationality(unit) != "russian"))
                || prisoners.Any(unit => unit!.Side == side || GameState.Condition(unit, Conditions.Captured) != ConditionState.True || state.Location(unit.Id)?.Location != at))
            {
                return Fail<GameState>("UNIT-STATE-036",
                    "Only Russian or berserk Infantry not in Melee massacre the prisoners in their Location, in a fire phase of their own side (A20.4).");
            }

            GameState? next = state;
            foreach (var prisoner in prisoners)
            {
                next = Eliminate(next!, prisoner!.Id);
                if (next is null)
                {
                    return null;
                }
            }

            if (massacre.Berserk)
            {
                foreach (var unit in units)
                {
                    var current = next!.Unit(unit!.Id)!;
                    next = Replace(next, current with
                    {
                        Conditions = new Dictionary<string, ConditionState>(current.Conditions, StringComparer.Ordinal) { [Conditions.Berserk] = ConditionState.False }
                    });
                }
            }

            var victims = prisoners[0]!.Side;
            var raise = !next!.MassacreElrRaised.Contains(victims, StringComparer.Ordinal);
            return next with
            {
                NoQuarter = next.NoQuarter.Contains(victims, StringComparer.Ordinal) ? next.NoQuarter : [.. next.NoQuarter, victims],
                MassacreElrRaised = raise ? [.. next.MassacreElrRaised, victims] : next.MassacreElrRaised,
                // A20.4: the ELR of every OB group of the massacred side rises too (ruling R18.3).
                Sides = raise ? [.. next.Sides.Select(item => item.Id != victims ? item : item with
                {
                    Elr = item.Elr is { } elr ? Math.Min(elr + 1, 6) : null,
                    Groups = [.. item.Groups.Select(group => group.Elr is { } groupElr ? group with { Elr = Math.Min(groupElr + 1, 6) } : group)],
                })] : next.Sides,
            };
        }

        /// <summary>
        /// Where a Gun's Acquisition is now (C6.5, C6.51, ruling R5.13), as the planner read it: on Known enemy units in its Location, or on the
        /// Location alone when its target left the Gun's LOS. The DRM is unchanged.
        /// </summary>
        private GameState? ChangeAcquisition(GameState state, AcquisitionChanged change)
        {
            if (state.Acquisitions.FirstOrDefault(item => item.Gun == change.Gun) is not { } acquisition || state.Find(change.Gun)?.Side is not { } side
                || change.Units.Any(id => state.Unit(id) is not { Status: InstanceStatus.Active } unit || unit.Side == side
                    || state.Location(id)?.Location != change.Location || GameState.Condition(unit, Conditions.Concealed) == ConditionState.True))
            {
                return Fail<GameState>("UNIT-STATE-033", "An Acquisition changes for a Gun that has one, onto Known enemy units in its Location (C6.5, C6.51).");
            }

            return state with
            {
                Acquisitions = [.. state.Acquisitions.Select(item => item.Gun != change.Gun ? item : acquisition with { Location = change.Location, Units = change.Units })],
            };
        }

        /// <summary>A4.51 (ruling R5.3): a unit's CX counter is removed when it breaks; A15.42: and when it goes berserk.</summary>
        private static GameState KeepCx(GameState next)
        {
            foreach (var unit in next.Units.Where(unit => unit.Status == InstanceStatus.Active && GameState.Condition(unit, Conditions.Cx) == ConditionState.True
                && (GameState.Condition(unit, Conditions.Broken) == ConditionState.True || GameState.Condition(unit, Conditions.Berserk) == ConditionState.True)).ToArray())
            {
                next = Replace(next, unit with
                {
                    Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { [Conditions.Cx] = ConditionState.False }
                });
            }

            return next;
        }

        /// <summary>
        /// C6.5, C6.51 (ruling R5.13): an Acquisition is lost when its Gun is no longer manned by an active Good Order crew; it follows its units
        /// into their successors (A7.302, A19.13), drops those no longer active or taken prisoner, and follows them while they share one
        /// Location. The planner moves it back to the last Location in the Gun's LOS when they leave it.
        /// </summary>
        private GameState KeepAcquisitions(GameState next, EventPayload payload)
        {
            if (next.Acquisitions.Count == 0)
            {
                return next;
            }

            var kept = new List<GunAcquisition>();
            foreach (var acquisition in next.Acquisitions)
            {
                // A Gun keeps it while its Good Order crew mans it; a tank while it is active and not Abandoned (C6.5, D1.3).
                var holds = next.Find(acquisition.Gun) switch
                {
                    EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Manned } manning } =>
                        next.Unit(manning.Holder) is { Status: InstanceStatus.Active } crew && GameState.GoodOrder(crew, vocabulary) != ConditionState.False,

                    // C9.2 (table player, pass 9): a light mortar keeps its Area Target Acquisition while its Good Order possessor holds it.
                    EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } possession } mortar when vocabulary.IsA(mortar.Kind, "asl:light-mortar") =>
                        next.Unit(possession.Holder) is { Status: InstanceStatus.Active } possessor && GameState.GoodOrder(possessor, vocabulary) != ConditionState.False,
                    UnitInstance { Status: InstanceStatus.Active } tank => vocabulary.IsA(tank.Kind, "asl:vehicle")
                        && GameState.Condition(tank, Conditions.Abandoned) != ConditionState.True,
                    _ => false,
                };
                if (!holds)
                {
                    continue;
                }

                var units = acquisition.Units.ToList();
                if (payload is LineageRecorded lineage && units.Any(lineage.Consumed.Contains))
                {
                    units = [.. units.Where(id => !lineage.Consumed.Contains(id)), .. lineage.Produced.Select(item => item.Id)];
                }

                units = [.. units.Where(id => next.Unit(id) is { Status: InstanceStatus.Active } unit && GameState.Condition(unit, Conditions.Captured) != ConditionState.True)];
                var locations = units.Select(id => next.Location(id)?.Location).Distinct().ToArray();
                kept.Add(acquisition with
                {
                    Units = units,
                    Location = locations is [{ } only] ? only : acquisition.Location,
                });
            }

            return next with
            {
                Acquisitions = kept
            };
        }

        private static UnitInstance Clear(UnitInstance unit, string[] conditions) =>
            !unit.Conditions.Keys.Any(conditions.Contains) ? unit : unit with
            {
                Conditions = Without(unit.Conditions, conditions)
            };

        private static Dictionary<string, ConditionState> Without(IReadOnlyDictionary<string, ConditionState> conditions, string[] removed) =>
            conditions.Where(item => !removed.Contains(item.Key)).ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);

        private GameState? Fire(GameState state, FireResolved fire, GameEvent gameEvent)
        {
            var eventId = gameEvent.EventId;
            // Fire in Live Play: the record must reproduce through the Fire package, which the Units project does not
            // reference, so replay refuses it without a verifier.
            if (fireVerifier is null)
            {
                return Fail<GameState>("UNIT-STATE-023", "A fire record can be replayed only with the Fire verifier.");
            }

            if (fire.Rolls.Values.FirstOrDefault(roll => !rolls.ContainsKey(roll)) is { } missing)
            {
                return Fail<GameState>("UNIT-STATE-023", $"The fire record's roll '{missing}' is not recorded before it.");
            }

            // A Defensive fire record answers the open window of the moving stack's latest step (A8.1).
            if (fire.MovementStep is { } answered && (state.Movement is not { WindowOpen: true } open || open.Step != answered))
            {
                return Fail<GameState>("UNIT-STATE-024", $"Defensive First Fire answers the open window on the moving stack's step {answered} (A8.1).");
            }

            // A7.55: a Location's units fire at a target once per phase (in the MPh, once per MF expenditure), as one fire
            // group; Residual FP has no firers and never forms one (A8.22). A vehicle's MG fires alone (D3.4, ruling R25.7), but the Mandatory
            // Fire Group binds it with its Location's Infantry (D3.5): only its own Multiple ROF fires again.
            // D7.14 (ruling R11.11): each OVR is its vehicle's own attack, as a vehicle's MG shot is.
            var byVehicle = (fire.Facts.TryGetProperty("vehicleFire", out var firing) && firing.ValueKind == JsonValueKind.Object)
                || (fire.Facts.TryGetProperty("overrun", out var overrunning) && overrunning.ValueKind == JsonValueKind.Object);
            if (fire.Firers.Count > 0 && state.FiresThisPhase.Any(item => !(byVehicle && item.Vehicle) && item.FirerLocation == fire.FirerLocation
                && item.TargetLocation == fire.TargetLocation && item.Step == fire.MovementStep))
            {
                return Fail<GameState>("UNIT-STATE-024", $"{fire.FirerLocation} has already fired at {fire.TargetLocation} this phase (A7.55).");
            }

            if (fireVerifier.Verify(state, fire, rolls) is { } reason)
            {
                return Fail<GameState>("UNIT-STATE-024", reason);
            }

            if (Choices(state, fire.Facts) is { } choiceReason)
            {
                return Fail<GameState>("UNIT-STATE-035", choiceReason);
            }

            state = state with
            {
                ChoicesMade = new Dictionary<string, string>(StringComparer.Ordinal)
            };
            fires[eventId] = (fire, gameEvent.Visibility is not null, false);

            // C2.24, D3.5 (unit step 25): a vehicle's MG shot keeps its Multiple ROF for this phase when its record says so, as a Gun's does.
            var shots = state.OrdnanceShots;
            if (fire.Facts.TryGetProperty("vehicleFire", out var vehicleFire) && vehicleFire.ValueKind == JsonValueKind.Object
                && vehicleFire.TryGetProperty("vehicleId", out var vehicleId) && vehicleId.GetString() is { } vehicle)
            {
                var kept = fire.Resolution.TryGetProperty("weaponEffects", out var effects) && effects.ValueKind == JsonValueKind.Array
                    && effects.GetArrayLength() == 1 && effects[0].TryGetProperty("rateOfFireRetained", out var retained) && retained.GetBoolean();
                var previous = shots.FirstOrDefault(item => item.Gun == vehicle);
                shots = [.. shots.Where(item => item.Gun != vehicle), new OrdnanceShotRecord(vehicle, (previous?.Shots ?? 0) + 1, kept)];
            }

            return state with
            {
                FiresThisPhase = [.. state.FiresThisPhase, new FireRecord(eventId, fire.FirerLocation, fire.TargetLocation) { Step = fire.MovementStep, Vehicle = byVehicle }],
                OrdnanceShots = shots,
            };
        }

        /// <summary>Residual FP a fire record left (A8.2, A8.21): the record's own value, in its target Location, in the MPh.</summary>
        private GameState? PlaceResidual(GameState state, ResidualFirePlaced residual)
        {
            if (state.Phase != "mph" || !fires.TryGetValue(residual.Fire, out var recorded) || recorded.Fire.TargetLocation != residual.Location.ToString()
                || !recorded.Fire.Resolution.TryGetProperty("arithmetic", out var arithmetic)
                || !arithmetic.TryGetProperty("residualFp", out var value) || value.GetInt32() != residual.Fp)
            {
                return Fail<GameState>("UNIT-STATE-026", $"Residual FP must be the value its fire record '{residual.Fire}' leaves in its target Location (A8.2).");
            }

            var existing = state.ResidualFire.FirstOrDefault(item => item.Location == residual.Location);
            if (existing is not null && existing.Fp >= residual.Fp)
            {
                return Fail<GameState>("UNIT-STATE-026", "Only a larger Residual FP counter replaces one already in the Location (A8.21).");
            }

            return state with
            {
                ResidualFire = [.. state.ResidualFire.Where(item => item.Location != residual.Location), new ResidualFire(residual.Location, residual.Fp, residual.Fire)],
            };
        }

        /// <summary>A Rally record (unit step 19): once per unit per Player Turn, in the RPh, reproduced by the Rally verifier.</summary>
        private GameState? Rally(GameState state, RallyAttempted rally, GameEvent gameEvent)
        {
            if (rallyVerifier is null)
            {
                return Fail<GameState>("UNIT-STATE-023", "A Rally record can be replayed only with the Rally verifier.");
            }

            if (rally.Rolls.Values.FirstOrDefault(roll => !rolls.ContainsKey(roll)) is { } missing)
            {
                return Fail<GameState>("UNIT-STATE-023", $"The Rally record's roll '{missing}' is not recorded before it.");
            }

            if (state.Phase != "rph" || Active(state, rally.Unit) is not UnitInstance unit)
            {
                return Fail<GameState>("UNIT-STATE-027", "A Rally attempt is made in the RPh by an active unit (A10.6).");
            }

            if (state.RallyAttemptsThisPlayerTurn.Contains(unit.Id) || state.RepairsThisPhase.Contains(unit.Id))
            {
                return Fail<GameState>("UNIT-STATE-027", $"'{unit.Id}' already attempted to rally this Player Turn, or to repair this RPh (A10.6, A3.1).");
            }

            if (rallyVerifier.Verify(state, rally, rolls) is { } reason)
            {
                return Fail<GameState>("UNIT-STATE-027", reason);
            }

            if (Choices(state, rally.Facts) is { } choiceReason)
            {
                return Fail<GameState>("UNIT-STATE-035", choiceReason);
            }

            state = state with
            {
                ChoicesMade = new Dictionary<string, string>(StringComparer.Ordinal)
            };

            var mmc = vocabulary.IsA(unit.Kind, "asl:mmc") && unit.Side == state.PhasingSide && !state.FirstMmcRallyTaken.Contains(unit.Side);
            return state with
            {
                RallyAttemptsThisPlayerTurn = [.. state.RallyAttemptsThisPlayerTurn, unit.Id],
                FirstMmcRallyTaken = mmc ? [.. state.FirstMmcRallyTaken, unit.Side] : state.FirstMmcRallyTaken,
            };
        }

        /// <summary>A Repair record (A9.72): a dr at most the Repair Number repairs, a 6 eliminates, anything else changes nothing.</summary>
        private GameState? BoreSight(GameState state, BoreSighted sighted)
        {
            // C6.41, C6.42 (ruling R8.8): one Location per Gun of the Scenario Defender, recorded at setup with the Gun's crew and setup Location.
            if (Active(state, sighted.Gun) is not EquipmentInstance { Holding: { Role: HoldingRole.Manned } manning, Position: MapPosition at }
                || manning.Holder != sighted.Crew || at.Location != sighted.SetupLocation || sighted.Location == at.Location
                || state.Unit(sighted.Crew)?.Side is not { } side || side != state.ScenarioDefender || state.BoreSights.Any(item => item.Gun == sighted.Gun))
            {
                return Fail<GameState>("UNIT-STATE-039", "A Bore Sighting is one Location outside the hex of a Gun the Scenario Defender set up manned (C6.41, C6.42).");
            }

            return state with
            {
                BoreSights = [.. state.BoreSights, sighted]
            };
        }

        private GameState? TurnGun(GameState state, GunTurned turned)
        {
            // C3.22 (ruling R8.9): in a friendly fire phase, a Gun its Good Order, unpinned crew could still fire changes its CA and fires no
            // more that phase; in the PFPh neither it nor its crew moves that Player Turn.
            var shots = state.OrdnanceShots.FirstOrDefault(item => item.Gun == turned.Gun);
            if (Active(state, turned.Gun) is not EquipmentInstance { Holding: { Role: HoldingRole.Manned } manning, Position: MapPosition at } gun
                || Active(state, manning.Holder) is not UnitInstance crew
                || state.Phase is not ("pfph" or "afph" or "dfph") || (crew.Side == state.PhasingSide) != (state.Phase != "dfph")
                || new[] { Conditions.Broken, Conditions.Pinned }.Any(name => GameState.Condition(crew, name) == ConditionState.True)
                || new[] { Conditions.Malfunctioned, Conditions.PrepFire, Conditions.FinalFire, Conditions.FirstFire, Conditions.IntensiveFire }.Any(name => GameState.Condition(gun, name) == ConditionState.True)
                    && !(shots?.RateOfFireKept ?? false)
                || (shots is not null && !shots.RateOfFireKept))
            {
                return Fail<GameState>("UNIT-STATE-039", "A Gun changes its CA without firing in a friendly fire phase while its Good Order, unpinned crew could still fire it (C3.22).");
            }

            return state with
            {
                Equipment = [.. state.Equipment.Select(item => item.Id == gun.Id ? gun with { Position = at with { Facing = turned.Facing } } : item)],
                OrdnanceShots = [.. state.OrdnanceShots.Where(item => item.Gun != gun.Id), new OrdnanceShotRecord(gun.Id, shots?.Shots ?? 0, false)],
                NoMoveThisPlayerTurn = state.Phase == "pfph" ? [.. state.NoMoveThisPlayerTurn, gun.Id, crew.Id] : state.NoMoveThisPlayerTurn,
                GunsTurnedThisPhase = [.. state.GunsTurnedThisPhase, gun.Id],
            };
        }

        private GameState? Manhandle(GameState state, ManhandlingRolled manhandling)
        {
            // C10.3 (ruling R8.6): the Manhandling DR against the Gun's M#.
            if (state.Phase != "mph" || Active(state, manhandling.Gun) is not EquipmentInstance || !rolls.TryGetValue(manhandling.Roll, out var roll) || roll.Count != 2
                || manhandling.Result != ManhandlingRolled.For(roll.Values.Sum() + manhandling.Drm, manhandling.Manhandling))
            {
                return Fail<GameState>("UNIT-STATE-039", "A Manhandling DR is made in the MPh for an active Gun, and its result follows from its Final DR and the M# (C10.3).");
            }

            return state;
        }

        private GameState? HookGun(GameState state, GunHooked hooked)
        {
            // C10.11, C10.12 (ruling R8.6): a Stopped vehicle of the phasing side spends half its MP to hook up, or unhook, a Gun in its hex with the
            // Gun's crew on foot there.
            if (state.Phase != "mph" || Active(state, hooked.Vehicle) is not UnitInstance vehicle || vehicle.Side != state.PhasingSide
                || !vocabulary.IsA(vehicle.Kind, "asl:vehicle") || state.Location(vehicle.Id) is not { } at || Active(state, hooked.Gun) is not EquipmentInstance gun
                || Active(state, hooked.Crew) is not UnitInstance crew || state.Location(crew.Id)?.Location != at.Location || hooked.Mp <= 0
                || GameState.Condition(vehicle, Conditions.Motion) == ConditionState.True
                || (hooked.Hooked
                    ? (gun.Holding is { Role: HoldingRole.Manned } manning && manning.Holder != crew.Id) || gun.Holding is { Role: not HoldingRole.Manned }
                        || (gun.Position as MapPosition)?.Location != at.Location
                    : gun.Holding is not { Role: HoldingRole.Towed } tow || tow.Holder != vehicle.Id || hooked.Facing is null))
            {
                return Fail<GameState>("UNIT-STATE-039", "A Stopped vehicle hooks up a Gun its crew mans in its hex, or unhooks its towed Gun there for a crew on foot (C10.11, C10.12).");
            }

            var halves = (vehicle.MfSpent * 2) + (vehicle.HalfMfSpent ? 1 : 0) + (hooked.Mp * 2);
            var next = Replace(state, vehicle with
            {
                MfSpent = halves / 2,
                HalfMfSpent = halves % 2 == 1
            })!;
            var facing = hooked.Facing ?? (gun.Position as MapPosition)?.Facing;
            return next with
            {
                Equipment = [.. next.Equipment.Select(item => item.Id != gun.Id ? item : gun with
                {
                    Holding = hooked.Hooked ? new Holding(vehicle.Id, HoldingRole.Towed) : new Holding(crew.Id, HoldingRole.Manned),
                    Position = new MapPosition(at.Location) { Facing = facing },
                })],
                UnemplacedGuns = next.UnemplacedGuns.Contains(gun.Id) ? next.UnemplacedGuns : [.. next.UnemplacedGuns, gun.Id],
                BoreSights = [.. next.BoreSights.Where(item => item.Gun != gun.Id)],
            };
        }

        private GameState? ShockRecovery(GameState state, ShockRecoveryRolled shock)
        {
            // C7.42 (ruling R7.8): one dr per RPh for a Shocked AFV or an Unconfirmed Kill.
            if (state.Phase != "rph" || Active(state, shock.Vehicle) is not UnitInstance vehicle || !vocabulary.IsA(vehicle.Kind, "asl:vehicle")
                || state.ShockRollsThisPhase.Contains(vehicle.Id)
                || (GameState.Condition(vehicle, Conditions.Shocked) != ConditionState.True && GameState.Condition(vehicle, Conditions.UnconfirmedKill) != ConditionState.True))
            {
                return Fail<GameState>("UNIT-STATE-038", "A Shocked AFV or an Unconfirmed Kill makes one dr in the RPh (C7.42).");
            }

            if (!rolls.TryGetValue(shock.Roll, out var roll) || roll.Count != 1 || roll.Sides != 6)
            {
                return Fail<GameState>("UNIT-STATE-038", $"The Shock recovery record's roll '{shock.Roll}' is not one recorded die.");
            }

            var unconfirmed = GameState.Condition(vehicle, Conditions.UnconfirmedKill) == ConditionState.True;
            if (shock.Result != ShockRecoveryRolled.For(unconfirmed, roll.Values[0]))
            {
                return Fail<GameState>("UNIT-STATE-038", "The Shock recovery record disagrees with its dr (C7.42).");
            }

            return state with
            {
                ShockRollsThisPhase = [.. state.ShockRollsThisPhase, vehicle.Id]
            };
        }

        private GameState? Repair(GameState state, RepairAttempted repair)
        {
            // D3.7 (ruling R6.10): a vehicle's malfunctioned MG, repaired by its CE crew that is not Stunned or Recalled, on a dr of 1; a 6
            // disables it.
            if (repair.Unit == repair.Equipment && Active(state, repair.Unit) is UnitInstance vehicle && vocabulary.IsA(vehicle.Kind, "asl:vehicle"))
            {
                if (state.Phase != "rph" || GameState.Condition(vehicle, Conditions.Malfunctioned) != ConditionState.True
                    || GameState.Condition(vehicle, Conditions.Disabled) == ConditionState.True || GameState.Condition(vehicle, Conditions.ButtonedUp) == ConditionState.True
                    || GameState.Condition(vehicle, Conditions.Stunned) == ConditionState.True || GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True
                    || GameState.Condition(vehicle, Conditions.Shocked) == ConditionState.True || GameState.Condition(vehicle, Conditions.UnconfirmedKill) == ConditionState.True
                    || state.RepairsThisPhase.Contains(vehicle.Id))
                {
                    return Fail<GameState>("UNIT-STATE-028", "A vehicle's malfunctioned MG is repaired once per RPh by its CE crew that is not Stunned or Recalled (D3.7).");
                }

                if (!rolls.TryGetValue(repair.Roll, out var vehicleRoll) || vehicleRoll.Count != 1 || vehicleRoll.Sides != 6)
                {
                    return Fail<GameState>("UNIT-STATE-028", $"The Repair record's roll '{repair.Roll}' is not one recorded die.");
                }

                var vehicleDr = vehicleRoll.Values[0];
                var vehicleResult = vehicleDr == 6 ? RepairAttempted.Eliminated : vehicleDr == 1 ? RepairAttempted.Repaired : RepairAttempted.NoChange;
                if (repair.RepairNumber != 1 || repair.Result != vehicleResult)
                {
                    return Fail<GameState>("UNIT-STATE-028", "The Repair record disagrees with the vehicle MG's dr (D3.7).");
                }

                return state with
                {
                    RepairsThisPhase = [.. state.RepairsThisPhase, vehicle.Id]
                };
            }

            if (state.Phase != "rph" || Active(state, repair.Unit) is not UnitInstance unit
                || Active(state, repair.Equipment) is not EquipmentInstance { Holding: { Role: HoldingRole.Possessed } holding } equipment
                || holding.Holder != unit.Id || GameState.Condition(equipment, Conditions.Malfunctioned) != ConditionState.True)
            {
                return Fail<GameState>("UNIT-STATE-028", "A Repair is attempted in the RPh on a malfunctioned SW its unit possesses (A9.72).");
            }

            // A3.1: a unit takes one kind of action in the RPh, so one that attempted to rally does not also repair.
            if (state.RallyAttemptsThisPlayerTurn.Contains(unit.Id))
            {
                return Fail<GameState>("UNIT-STATE-028", $"'{unit.Id}' attempted to rally this RPh, so it may not also repair (A3.1).");
            }

            if (!rolls.TryGetValue(repair.Roll, out var roll) || roll.Count != 1 || roll.Sides != 6)
            {
                return Fail<GameState>("UNIT-STATE-028", $"The Repair record's roll '{repair.Roll}' is not one recorded die.");
            }

            var printed = equipment.Definition is { } reference && catalog?.Definition(reference.Definition) is { } definition
                ? definition.Printed("malfunctioned", "asl:repair")?.Value?.Number
                : null;
            var dr = roll.Values[0];
            var expected = dr == 6 ? RepairAttempted.Eliminated : dr <= repair.RepairNumber ? RepairAttempted.Repaired : RepairAttempted.NoChange;
            if (printed != repair.RepairNumber || repair.Result != expected)
            {
                return Fail<GameState>("UNIT-STATE-028", "The Repair record disagrees with the SW's Repair Number or its dr (A9.72).");
            }

            return state with
            {
                RepairsThisPhase = state.RepairsThisPhase.Contains(unit.Id) ? state.RepairsThisPhase : [.. state.RepairsThisPhase, unit.Id],
            };
        }

        /// <summary>
        /// A movement step (unit step 22): a stack of the phasing side that may still move enters a Location in the MPh; the
        /// same stack continues only after the DEFENDER's window on its last step closes, and another stack only after the
        /// ATTACKER ends this one's move (A8.11).
        /// </summary>
        private GameState? StepMovement(GameState state, MovementStepped moving)
        {
            var movers = moving.Movers.Select(id => Active(state, id) as UnitInstance).ToArray();
            if (state.Phase != "mph" || movers.Length == 0 || movers.Any(unit => unit is null || unit.Side != state.PhasingSide || unit.MovementEnded)
                || movers.Select(unit => state.Location(unit!.Id)?.Location).Distinct().Count() != 1 || moving.HalfMf <= 0)
            {
                return Fail<GameState>("UNIT-STATE-029", "A movement step moves a stack of the phasing side that may still move, in one Location, in the MPh.");
            }

            // A3.3 (p. 47): a unit that fired in the PFPh does not move in the MPh.
            if (movers.FirstOrDefault(unit => GameState.Condition(unit!, Conditions.PrepFire) == ConditionState.True) is { } fired)
            {
                return Fail<GameState>("UNIT-STATE-029", $"'{fired.Id}' fired in the PFPh, so it may not move in the MPh (A3.3, p. 47).");
            }

            // A11.15: a unit held in Melee does not leave its Location; a prisoner moves only with its Guard (A20.53).
            if (movers.FirstOrDefault(unit => GameState.Condition(unit!, Conditions.Melee) == ConditionState.True
                || GameState.Condition(unit!, Conditions.Captured) == ConditionState.True) is { } held)
            {
                return Fail<GameState>("UNIT-STATE-029", $"'{held.Id}' is held in Melee or captured, so it does not move (A11.15, A20.53).");
            }

            // A4.2: the stack's members may move on together or apart, but only they may move until every one has ended.
            var current = state.Movement;
            if (current is not null && (moving.Movers.Any(id => !current.Members.Contains(id, StringComparer.Ordinal))
                || current.WindowOpen || current.Assault != moving.Assault))
            {
                return Fail<GameState>("UNIT-STATE-029",
                    "The moving stack's members continue, together or apart, only after the DEFENDER's window closes; another stack moves after every member ends (A4.2, A8.11).");
            }

            if (moving.Step != (current?.Step ?? 0) + 1)
            {
                return Fail<GameState>("UNIT-STATE-029", $"The next movement step is {(current?.Step ?? 0) + 1}.");
            }

            // A4.5 (ruling R5.1): Double Time by Infantry neither broken, wounded, berserk, nor CX, nor rested from CX at this MPh's start.
            if (moving.DoubleTime && movers.FirstOrDefault(unit => !vocabulary.IsA(unit!.Kind, "asl:personnel") || state.NoDoubleTime.Contains(unit.Id)
                || new[] { Conditions.Broken, Conditions.Wounded, Conditions.Berserk, Conditions.Cx }.Any(name => GameState.Condition(unit, name) == ConditionState.True)) is { } tired)
            {
                return Fail<GameState>("UNIT-STATE-029", $"'{tired.Id}' may not Double Time: it is broken, wounded, berserk, or CX, or its CX counter left at this MPh's start (A4.5, A4.51).");
            }

            // A24.1 (ruling R9.5): a SMOKE attempt is made once per MPh by a moving squad, in its Location, with its recorded dr.
            // Table player, pass 9: the exponent is the catalog's, CX (or Double Time with this step) adds one, and the cost is 1 MF in the own
            // Location and 2 in another.
            if (moving.Smoke is { } smoke && (!moving.Movers.Contains(smoke.Unit, StringComparer.Ordinal) || state.SmokeAttempts.Contains(smoke.Unit, StringComparer.Ordinal)
                || moving.To != state.Location(smoke.Unit)?.Location || !rolls.TryGetValue(smoke.Roll, out var smokeRoll) || smokeRoll.Count != 1
                || smokeRoll.Values[0] != smoke.Dr || smoke.Exponent < 1 || Active(state, smoke.Unit) is not UnitInstance placer
                || SmokeExponent(placer) != smoke.Exponent
                || smoke.Cx != (moving.DoubleTime || GameState.Condition(placer, Conditions.Cx) == ConditionState.True)
                || moving.HalfMf != (smoke.Target == moving.To ? 2 : 4)))
            {
                return Fail<GameState>("UNIT-STATE-029", "A SMOKE placement is one attempt per MPh by a squad of the moving stack, in its Location, with its dr (A24.1).");
            }

            // A4.134 (ruling R10.9): a Minimum Move is a stack's first and only step; A12.15 (ruling R10.11): a forced-back stack stays in its Location.
            if ((moving.MinimumMove && (current is not null || movers.Any(unit => unit!.MfSpent != 0 || unit.HalfMfSpent)))
                || (moving.Attempted is { } attempted && (attempted == moving.To || movers.Any(unit => state.Location(unit!.Id)?.Location != moving.To)))
                || (moving.Bypass is { Count: < 1 or > 2 }))
            {
                return Fail<GameState>("UNIT-STATE-029", "A Minimum Move is a stack's only step, a forced back leaves the stack in its Location, and a Bypass follows one or two hexsides (A4.134, A12.15, A4.31).");
            }

            // A2.6 (ruling R21.5): an exit leaves the map from the stack's Location; the units are Exited, not eliminated, and the move ends.
            if (moving.Exit is { } edge)
            {
                if (movers.Any(unit => state.Location(unit!.Id)?.Location != moving.To))
                {
                    return Fail<GameState>("UNIT-STATE-029", "An exit leaves the map from the moving stack's own Location (A2.6).");
                }

                var exited = state;
                foreach (var unit in movers)
                {
                    exited = Replace(exited, unit! with
                    {
                        Position = OffMapPosition.Instance,
                        Status = InstanceStatus.Exited,
                    })!;
                }

                // Table player, pass 21: the SW the movers carry leave with them.
                return exited with
                {
                    Equipment = [.. exited.Equipment.Select(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding
                        && moving.Movers.Contains(holding.Holder, StringComparer.Ordinal) ? item with { Status = InstanceStatus.Exited } : item)],
                    Movement = null,
                    Exits = [.. state.Exits, .. movers.Select(unit => new UnitExit(unit!.Id, moving.To, edge, state.Turn,
                        GameState.Condition(unit, Conditions.Broken) == ConditionState.True))],
                };
            }

            string[] leaders = [.. movers.Where(unit => vocabulary.IsA(unit!.Kind, "asl:leader")).Select(unit => unit!.Id)];
            var next = state;
            foreach (var unit in movers)
            {
                var halves = (unit!.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0) + moving.HalfMf;
                var conditions = moving.DoubleTime
                    ? new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { [Conditions.Cx] = ConditionState.True }
                    : unit.Conditions;
                next = Replace(next, unit with
                {
                    Position = new MapPosition(moving.To),
                    MfSpent = halves / 2,
                    HalfMfSpent = halves % 2 == 1,
                    Conditions = conditions,
                    DoubleTimeMf = moving.DoubleTime ? (unit.MfSpent == 0 && !unit.HalfMfSpent ? 2 : 1) : unit.DoubleTimeMf,

                    // B3.4, A4.12 (ruling R10.8): the Road Bonus needs every step at the road rate; the leader bonus a leader at every step.
                    OffRoad = unit.OffRoad || !moving.Road,
                    MovedWith = [.. (unit.MovedWith ?? (unit.MfSpent == 0 && !unit.HalfMfSpent ? leaders : [])).Where(id => id != unit.Id && leaders.Contains(id, StringComparer.Ordinal))],
                })!;
                next = MovePrisoners(next, unit.Id, new MapPosition(moving.To));
            }

            // C10.3 (ruling R8.6): the pushed Gun goes with its crew and loses its Emplacement; any other Gun a mover mans is abandoned.
            foreach (var manned in next.Equipment.Where(item => item.Holding is { Role: HoldingRole.Manned } holding && moving.Movers.Contains(holding.Holder)).ToArray())
            {
                next = next with
                {
                    Equipment = [.. next.Equipment.Select(item => item.Id != manned.Id ? item
                        : manned.Id == moving.PushedGun && manned.Position is MapPosition gunAt ? manned with { Position = gunAt with { Location = moving.To } }
                        : manned with { Holding = null })],
                    UnemplacedGuns = manned.Id == moving.PushedGun && !next.UnemplacedGuns.Contains(manned.Id) ? [.. next.UnemplacedGuns, manned.Id] : next.UnemplacedGuns,
                    BoreSights = manned.Id == moving.PushedGun ? [.. next.BoreSights.Where(item => item.Gun != manned.Id)] : next.BoreSights,
                };
            }

            return next with
            {
                Movement = new MovementState(moving.Movers, moving.To, moving.HalfMf, moving.Step, moving.Assault, WindowOpen: true)
                {
                    Members = current?.Members ?? moving.Movers,
                    Charge = moving.Charge,
                    EndingMembers = moving.MinimumMove || moving.Attempted is not null ? moving.Movers : moving.Smoke is { Dr: 6 } sixed ? [sixed.Unit] : [],
                    MinimumMove = moving.MinimumMove,
                    Bypass = moving.Bypass,
                    From = movers[0]!.Position is MapPosition left && left.Location != moving.To ? left.Location : null,
                    PushedGun = moving.PushedGun,
                },
                SmokeAttempts = moving.Smoke is { } attempt ? [.. next.SmokeAttempts, attempt.Unit] : next.SmokeAttempts,
                SmokePending = moving.Smoke is { Placed: true } placing ? placing.Target : null,

                // A4.41 (referee, pass 9): a light mortar carried into a new Location does not fire in the AFPh.
                MovedWeapons = [.. next.MovedWeapons, .. next.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding
                    && moving.Movers.Contains(holding.Holder, StringComparer.Ordinal) && vocabulary.IsA(item.Kind, "asl:light-mortar")
                    && movers[0]!.Position is MapPosition before && before.Location != moving.To && !next.MovedWeapons.Contains(item.Id, StringComparer.Ordinal)).Select(item => item.Id)],
            };
        }

        /// <summary>
        /// One MP expenditure of a moving vehicle (D2.1, unit step 25; UNIT-STATE-034): a Mobile vehicle of the phasing side that did not
        /// Prep Fire (D.3, D.7), is not Stunned or Recalled (D5.34), and has not ended its move starts (D2.12; not when it began its MPh in
        /// Motion, D2.4), turns one hexspine (D2.11), enters an ADJACENT Location, or stops (D2.13); after stopping it must start again to
        /// move on. Each expenditure opens the DEFENDER's window (A8.1) and removes any Motion counter (D2.4). The MP costs are the
        /// planner's reads of the Terrain Chart, as a movement step's MF are.
        /// </summary>
        private GameState? StepVehicle(GameState state, VehicleStepped step)
        {
            if (state.Phase != "mph" || Active(state, step.Vehicle) is not UnitInstance vehicle || !vocabulary.IsA(vehicle.Kind, "asl:vehicle")
                || vehicle.Side != state.PhasingSide || vehicle.MovementEnded || step.HalfMp <= 0 || state.Location(vehicle.Id) is not { } at
                || vehicle.Position is not MapPosition { Facing: { } facing })
            {
                return Fail<GameState>("UNIT-STATE-034", "A vehicle step moves a vehicle of the phasing side that has not ended its move, in the MPh (D2.1).");
            }

            // D5.341: a Recall stops the AFV like a Stun for the rest of that Player Turn; once its counter shows Recall; +1 it must move.
            var recalling = GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True && GameState.Condition(vehicle, Conditions.StunRecovery) != ConditionState.True;
            // D8.3 (ruling R11.10): a bogged vehicle's only expenditure is its Bog Removal Start MP.
            var bogged = GameState.Condition(vehicle, Conditions.Bogged) == ConditionState.True;
            if (new[] { Conditions.PrepFire, Conditions.Immobilized, Conditions.Stunned, Conditions.Shocked, Conditions.UnconfirmedKill, Conditions.Abandoned }
                .FirstOrDefault(name => GameState.Condition(vehicle, name) == ConditionState.True) is { } barred)
            {
                return Fail<GameState>("UNIT-STATE-034", $"'{vehicle.Id}' is marked {barred.Replace("asl:", "", StringComparison.Ordinal)} and may not move (D.3, D.7, D5.34, D5.41).");
            }

            if (recalling)
            {
                return Fail<GameState>("UNIT-STATE-034", $"'{vehicle.Id}' is Recalled and may not move for the rest of this Player Turn (D5.341).");
            }

            var current = state.Movement;
            if (current is not null && (!current.Vehicle || current.WindowOpen || !current.Members.SequenceEqual([vehicle.Id], StringComparer.Ordinal)))
            {
                return Fail<GameState>("UNIT-STATE-034", "A vehicle moves alone, after the DEFENDER's window on its last MP expenditure closes, and no other unit moves until it ends (A8.11).");
            }

            if (step.Step != (current?.Step ?? 0) + 1)
            {
                return Fail<GameState>("UNIT-STATE-034", $"The next movement step is {(current?.Step ?? 0) + 1}.");
            }

            if (current?.Overrun is not null)
            {
                return Fail<GameState>("UNIT-STATE-034", "A vehicle resolves its declared OVR before it spends any more MP (D7.1).");
            }

            if (bogged && !(step.Kind == VehicleStepped.Start && step.BogRemoval) && step.Kind != VehicleStepped.Remain)
            {
                return Fail<GameState>("UNIT-STATE-034", $"'{vehicle.Id}' is bogged: it spends MP only on its Bog Removal (D8.2, D8.3).");
            }

            var inMotion = GameState.Condition(vehicle, Conditions.Motion) == ConditionState.True;
            var started = current is not null ? current.Started : inMotion;
            var moving = started && current?.Stopped != true;

            // D2.23 (ruling R11.1): Reverse is declared with the Start MP, and every entry until the vehicle stops keeps that direction.
            var reverse = current?.Reverse == true && moving;
            var valid = current?.Ending != true && step.Kind switch
            {
                VehicleStepped.Start => !moving && step.At == at.Location && step.Facing is null && (step.BogRemoval ? bogged : step.HalfMp == 2),
                VehicleStepped.Turn => moving && step.At == at.Location && step.Facing is { } turned && Math.Abs(((int)turned - (int)facing + 6) % 6) is 1 or 5
                    && step.HalfMp is 2 or 4,
                VehicleStepped.Enter => moving && (step.At != at.Location || (step.Straddling is { } lane && lane != vehicle.Straddling)) && step.Facing is null
                    && step.Reverse == reverse,
                VehicleStepped.Stop => moving && step.At == at.Location && step.Facing is null && step.HalfMp == 2,

                // D2.1 (ruling R5.15): the MP left at the end of its move, spent in its hex, moving or stopped; A2.6: an exit from its edge hex.
                VehicleStepped.Remain => current is not null && step.At == at.Location && step.Facing is null,
                VehicleStepped.Exit => moving && step.At == at.Location && step.Facing is null && !reverse,

                // D7.1, A12.41 (ruling R11.12): an OVR declared in the vehicle's own Location once the concealed units there have chosen.
                VehicleStepped.Overrun => moving && step.At == at.Location && step.Facing is null && step.Overrunning,
                _ => false,
            };
            if (!valid)
            {
                return Fail<GameState>("UNIT-STATE-034",
                    "A vehicle starts when not moving; turns one hexspine, enters a Location, stops, or exits only while moving; turns, starts, and stops for one MP; and spends its MP left in its hex only to end its move (D2.1, D2.11 to D2.13).");
            }

            if (step.Kind == VehicleStepped.Exit)
            {
                // A2.6, D5.341: the vehicle leaves the playing area, not to return; it is recorded as exited, not eliminated.
                var gone = Replace(state, vehicle with
                {
                    Position = OffMapPosition.Instance,
                    Status = InstanceStatus.Exited,
                    MovementEnded = true,
                    Conditions = new Dictionary<string, ConditionState>(vehicle.Conditions, StringComparer.Ordinal) { [Conditions.Motion] = ConditionState.False },
                })!;
                return Settle(DropHeldBy(gone, [vehicle.Id]), new MovementState([vehicle.Id], at.Location, 0, step.Step, false, WindowOpen: false) { Vehicle = true, Members = [] });
            }

            var halves = (vehicle.MfSpent * 2) + (vehicle.HalfMfSpent ? 1 : 0) + step.HalfMp;
            var conditions = new Dictionary<string, ConditionState>(vehicle.Conditions, StringComparer.Ordinal);
            if (inMotion)
            {
                conditions[Conditions.Motion] = ConditionState.False;
            }

            // D2.3, D2.34 (ruling R11.2): an entry says where the vehicle straddles a hexside in Bypass, or that it is at its hex center; any other
            // expenditure leaves it where it is.
            var next = Replace(state, vehicle with
            {
                Position = new MapPosition(step.At) { Facing = step.Facing ?? facing },
                MfSpent = halves / 2,
                HalfMfSpent = halves % 2 == 1,
                Conditions = conditions,
                Straddling = step.Kind == VehicleStepped.Enter ? step.Straddling : vehicle.Straddling,
            })!;

            // C10.1 (ruling R8.6): a Gun in tow goes with its vehicle.
            next = next with
            {
                Equipment = [.. next.Equipment.Select(item => item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id && item.Position is MapPosition towedAt
                    ? item with { Position = towedAt with { Location = step.At } }
                    : item)],
            };
            return next with
            {
                Movement = new MovementState([vehicle.Id], step.At, step.Kind == VehicleStepped.Enter ? step.HalfMp : (current?.HalfMfInLocation ?? 0) + step.HalfMp,
                    step.Step, false, WindowOpen: true)
                {
                    Vehicle = true,
                    Started = step.Kind == VehicleStepped.Remain ? current!.Started : step.Kind != VehicleStepped.Stop,
                    Stopped = step.Kind == VehicleStepped.Remain ? current!.Stopped : step.Kind == VehicleStepped.Stop,

                    // D2.15 (ruling R11.5): a Minimum Move ends the vehicle's move in Motion once the DEFENDER passes.
                    Ending = step.Kind == VehicleStepped.Remain || step.MinimumMove,
                    Reverse = step.Kind == VehicleStepped.Start ? step.Reverse : step.Kind != VehicleStepped.Stop && current?.Reverse == true,
                    Overrun = step.Overrunning ? step.At : null,
                    MinimumMove = step.MinimumMove,
                    EnteredFrom = step.Kind == VehicleStepped.Enter && (step.At.Board != at.Location.Board || step.At.Hex != at.Location.Hex) ? at.Location : current?.EnteredFrom,
                },
            };
        }

        /// <summary>
        /// A vehicle's check outside combat (rulings R11.3, R11.4, R11.9, R11.10): its dice agree with its result; a Bog bogs it (D8.21), and an ESB or
        /// Mechanical Reliability failure or a Bog Removal dr of 6 or more immobilizes it (D2.5, D2.51, D8.3), each ending its move once the DEFENDER
        /// passes; a passed ESB adds its MP (D2.5); a Bog Removal frees or Mires it (D8.3, D8.31).
        /// </summary>
        private GameState? CheckVehicle(GameState state, VehicleCheckRolled check)
        {
            if (state.Phase != "mph" || Active(state, check.Vehicle) is not UnitInstance vehicle || !vocabulary.IsA(vehicle.Kind, "asl:vehicle")
                || vehicle.Side != state.PhasingSide)
            {
                return Fail<GameState>("UNIT-STATE-039", "A vehicle check is made in its own MPh (D2.5, D2.51, D8.2, D8.3).");
            }

            if (!rolls.TryGetValue(check.Roll, out var roll) || roll.Count != 2 || roll.Sides != 6)
            {
                return Fail<GameState>("UNIT-STATE-039", $"The vehicle check's roll '{check.Roll}' is not a recorded DR.");
            }

            // D8.3: a Bog Removal is decided by its colored dr, the first die.
            var final = (check.Check == VehicleCheckRolled.BogRemoval ? roll.Values[0] : roll.Values[0] + roll.Values[1]) + check.Drm;
            if (check.Result != VehicleCheckRolled.For(check.Check, final) || (check.Check == VehicleCheckRolled.Esb) != (check.Mp > 0))
            {
                return Fail<GameState>("UNIT-STATE-039", "The vehicle check's result disagrees with its DR.");
            }

            var conditions = new Dictionary<string, ConditionState>(vehicle.Conditions, StringComparer.Ordinal);
            var esb = vehicle.EsbMp;
            switch (check.Result)
            {
                // D2.4: a Motion counter leaves a vehicle that becomes Immobile.
                case VehicleCheckRolled.Bogged:
                    conditions[Conditions.Bogged] = ConditionState.True;
                    conditions[Conditions.Motion] = ConditionState.False;
                    break;
                case VehicleCheckRolled.Immobilized:
                    conditions[Conditions.Immobilized] = ConditionState.True;
                    conditions[Conditions.Motion] = ConditionState.False;
                    break;
                case VehicleCheckRolled.Freed:
                    conditions[Conditions.Bogged] = ConditionState.False;
                    conditions[Conditions.Mired] = ConditionState.False;
                    break;
                case VehicleCheckRolled.Mired:
                    conditions[Conditions.Mired] = ConditionState.True;
                    break;
                case VehicleCheckRolled.Passed when check.Check == VehicleCheckRolled.Esb:
                    esb += check.Mp;
                    break;
            }

            var next = Replace(state, vehicle with
            {
                Conditions = conditions,
                EsbMp = esb
            })!;
            var stuck = check.Result is VehicleCheckRolled.Bogged or VehicleCheckRolled.Immobilized
                || (check.Check == VehicleCheckRolled.BogRemoval && check.Result != VehicleCheckRolled.Freed);
            return stuck && next.Movement is { Vehicle: true } movement && movement.Members.Contains(vehicle.Id, StringComparer.Ordinal)
                ? next with
                {
                    Movement = movement with
                    {
                        Ending = true
                    }
                }
                : next;
        }

        /// <summary>An OVR's resolution (D7.1, D7.2; ruling R11.11): the declared OVR is done, and the DEFENDER's Reaction Fire window opens.</summary>
        private GameState? ResolveOverrun(GameState state, OverrunResolved overrun)
        {
            if (state.Movement is not { Vehicle: true, WindowOpen: false } movement || movement.Overrun != overrun.At
                || !movement.Movers.Contains(overrun.Vehicle, StringComparer.Ordinal) || !fires.ContainsKey(overrun.Fire))
            {
                return Fail<GameState>("UNIT-STATE-039", "An OVR is resolved by its vehicle's fire record after the DEFENDER's window on its declaration closes (D7.1).");
            }

            return state with
            {
                Movement = movement with
                {
                    Overrun = null,
                    WindowOpen = true,
                    Reaction = true
                },
            };
        }

        /// <summary>
        /// Opportunity Fire (A7.25; ruling R12.1): in their PFPh, Good Order Infantry of the phasing side that have not fired or directed fire this Player
        /// Turn, not berserk, in Melee, or prisoners, are marked with a Bounding Fire counter and may not move in the MPh.
        /// </summary>
        private GameState? DeclareOpportunityFire(GameState state, OpportunityFireDeclared declared)
        {
            var units = declared.Units.Select(id => Active(state, id)).ToArray();
            if (state.Phase != "pfph" || units.Length == 0 || declared.Units.Distinct(StringComparer.Ordinal).Count() != units.Length
                || units.Any(unit => unit is not UnitInstance { } infantry || infantry.Side != state.PhasingSide || !vocabulary.IsA(infantry.Kind, "asl:personnel")
                    || new[] { Conditions.Broken, Conditions.Berserk, Conditions.Melee, Conditions.Captured, Conditions.PrepFire, Conditions.BoundingFire }
                        .Any(name => GameState.Condition(infantry, name) == ConditionState.True)))
            {
                return Fail<GameState>("UNIT-STATE-040", "Opportunity Fire is declared in the PFPh for Good Order Infantry of the phasing side that have not fired (A7.25).");
            }

            var next = state;
            foreach (var unit in units.OfType<UnitInstance>())
            {
                next = Replace(next, unit with
                {
                    Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { [Conditions.BoundingFire] = ConditionState.True },
                })!;
            }

            return next with
            {
                NoMoveThisPlayerTurn = [.. next.NoMoveThisPlayerTurn, .. declared.Units],
            };
        }

        /// <summary>
        /// A rout step (A10.5; ruling R13.3): in the RtPh, a broken unit not in Melee enters a Location for its MF, at most six (twelve half MF) in the phase
        /// unless by Low Crawl, which is its only step.
        /// </summary>
        private GameState? Rout(GameState state, RoutStepped routed)
        {
            if (state.Phase != "rtph" || Active(state, routed.Unit) is not UnitInstance unit || GameState.Condition(unit, Conditions.Broken) != ConditionState.True
                || GameState.Condition(unit, Conditions.Melee) == ConditionState.True || GameState.Condition(unit, Conditions.Pinned) == ConditionState.True
                || routed.HalfMf < 0 || (!routed.LowCrawl && (unit.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0) + routed.HalfMf > RoutHalfMf(unit))
                || (routed.LowCrawl && state.RoutedThisPhase.Contains(unit.Id, StringComparer.Ordinal)))
            {
                return Fail<GameState>("UNIT-STATE-041", "A rout step moves a broken unit not in Melee or pinned in the RtPh, within its MF (A10.5, A10.52).");
            }

            var spent = unit.MfSpent * 2 + (unit.HalfMfSpent ? 1 : 0) + routed.HalfMf;
            return Replace(state, unit with
            {
                Position = new MapPosition(routed.To),
                MfSpent = spent / 2,
                HalfMfSpent = spent % 2 == 1,
            }) is { } routedTo ? MovePrisoners(routedTo, unit.Id, new MapPosition(routed.To)) is var moved ? moved with
            {
                RoutedThisPhase = moved.RoutedThisPhase.Contains(unit.Id, StringComparer.Ordinal) ? moved.RoutedThisPhase : [.. moved.RoutedThisPhase, unit.Id],
            } : null : null;
        }

        /// <summary>A10.5: a broken unit has six MF in the RtPh, a wounded SMC three.</summary>
        private static int RoutHalfMf(UnitInstance unit) =>
            unit.Kind is "asl:leader" or "asl:hero" && GameState.Condition(unit, Conditions.Wounded) == ConditionState.True ? 6 : 12;

        /// <summary>An Interdiction NMC (A10.53; ruling R13.3): in the RtPh, on a routing unit, its DR agreeing with its result.</summary>
        private GameState? Interdict(GameState state, RoutInterdicted interdicted)
        {
            if (state.Phase != "rtph" || Active(state, interdicted.Unit) is not UnitInstance || !state.RoutedThisPhase.Contains(interdicted.Unit, StringComparer.Ordinal)
                || !rolls.TryGetValue(interdicted.Roll, out var roll) || roll.Count != 2 || roll.Sides != 6
                || RoutInterdicted.For(roll.Values[0] + roll.Values[1], roll.Values[0] + roll.Values[1] + interdicted.Drm, interdicted.Morale) != interdicted.Result)
            {
                return Fail<GameState>("UNIT-STATE-041", "An Interdiction NMC is taken by a routing unit, and its result agrees with its DR (A10.53).");
            }

            return state;
        }

        /// <summary>A Deployment attempt (A1.31; ruling R13.4): in the RPh, its NTC agreeing with its result; the squad and leader spend their RPh action.</summary>
        private GameState? AttemptDeployment(GameState state, DeploymentAttempted deployment)
        {
            if (state.Phase != "rph" || Active(state, deployment.Squad) is not UnitInstance squad || GameState.Condition(squad, Conditions.Broken) == ConditionState.True
                || !rolls.TryGetValue(deployment.Roll, out var roll) || roll.Count != 2 || roll.Sides != 6
                || (roll.Values[0] + roll.Values[1] + deployment.Drm <= deployment.Morale) != deployment.Passed
                || state.RallyPhaseActions.Contains(squad.Id, StringComparer.Ordinal)
                || (deployment.Leader is { } leader && state.RallyPhaseActions.Contains(leader, StringComparer.Ordinal)))
            {
                return Fail<GameState>("UNIT-STATE-041", "A Deployment attempt is a Good Order squad's RPh action, and its NTC agrees with its DR (A1.31).");
            }

            return state with
            {
                RallyPhaseActions = [.. state.RallyPhaseActions, squad.Id, .. deployment.Leader is { } directing ? [directing] : Array.Empty<string>()],
            };
        }

        /// <summary>A Recombination's or a Transfer's RPh action (A1.32, A4.431; rulings R13.4, R13.5): each unit's first this RPh.</summary>
        private GameState? TakeRallyPhaseAction(GameState state, RallyPhaseActionTaken action)
        {
            if (state.Phase is not ("rph" or "aph") || action.Units.Count == 0 || action.Units.Any(id => state.Unit(id) is null)
                || (state.Phase == "rph" && action.Units.Any(id => state.RallyPhaseActions.Contains(id, StringComparer.Ordinal))))
            {
                return Fail<GameState>("UNIT-STATE-041", "A Recombination or Transfer is its units' RPh action, or a Transfer at the start of their APh (A1.32, A4.431).");
            }

            return state with
            {
                RallyPhaseActions = [.. state.RallyPhaseActions, .. action.Units],
            };
        }

        /// <summary>A Recovery attempt (A4.44; ruling R13.5): in the RPh as the unit's action, or in its MPh for one MF, once per SW per phase, its dr agreeing.</summary>
        private GameState? AttemptRecovery(GameState state, RecoveryAttempted recovery)
        {
            var key = recovery.Unit + "|" + recovery.Weapon;
            if (state.Phase is not ("rph" or "mph") || Active(state, recovery.Unit) is not UnitInstance unit || state.Find(recovery.Weapon) is not EquipmentInstance
                || !rolls.TryGetValue(recovery.Roll, out var roll) || roll.Count != 1 || roll.Sides != 6 || (roll.Values[0] + recovery.Drm < 6) != recovery.Recovered
                || state.RecoveryAttempts.Contains(key, StringComparer.Ordinal)
                || (state.Phase == "rph" && state.RallyPhaseActions.Contains(unit.Id, StringComparer.Ordinal)))
            {
                return Fail<GameState>("UNIT-STATE-041", "A Recovery attempt is made once per SW in the RPh or MPh, and its dr agrees with its result (A4.44).");
            }

            var next = state.Phase == "mph" && state.Find(recovery.Weapon) is EquipmentInstance { Holding: null } ? Replace(state, unit with
            {
                MfSpent = unit.MfSpent + 1
            })! : state;
            return next with
            {
                RecoveryAttempts = [.. next.RecoveryAttempts, key],
                RecoveredThisPhase = recovery.Recovered ? [.. next.RecoveredThisPhase, recovery.Weapon] : next.RecoveredThisPhase,
                RallyPhaseActions = state.Phase == "rph" ? [.. next.RallyPhaseActions, unit.Id] : next.RallyPhaseActions,
            };
        }

        /// <summary>An Encirclement (A7.7; ruling R12.11): placed by a fire record of a fire phase at the Location, on a side with units there.</summary>
        private GameState? Encircle(GameState state, EncirclementPlaced encirclement)
        {
            if (state.Phase is not ("pfph" or "dfph" or "afph") || !fires.TryGetValue(encirclement.Fire, out var recorded)
                || recorded.Fire.TargetLocation != encirclement.Location.ToString()
                || !state.At(encirclement.Location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side == encirclement.Side))
            {
                return Fail<GameState>("UNIT-STATE-040", "An Encirclement is placed by a fire record of a fire phase at a Location holding units of the Encircled side (A7.7).");
            }

            return state.Encirclements.Any(item => item.Location == encirclement.Location && item.Side == encirclement.Side) ? state : state with
            {
                Encirclements = [.. state.Encirclements, new Encirclement(encirclement.Location, encirclement.Side)],
            };
        }

        /// <summary>A7.7 (ruling R12.11): an Encirclement ends when no unit it Encircles is left in its Location.</summary>
        private static GameState KeepEncirclements(GameState next) =>
            next.Encirclements.Count == 0 ? next : next with
            {
                Encirclements = [.. next.Encirclements.Where(item => next.At(item.Location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active
                    && unit.Kind != UnitKinds.Dummy && GameState.Condition(unit, Conditions.Captured) != ConditionState.True && next.Encircled(unit)))],
            };

        /// <summary>A Fire Lane (A9.22; ruling R12.7): placed in the MPh by its MG's fire record, with at least one Location.</summary>
        private GameState? PlaceFireLane(GameState state, FireLanePlaced lane)
        {
            if (state.Phase != "mph" || !fires.ContainsKey(lane.Fire) || state.Find(lane.Weapon) is not EquipmentInstance { Status: InstanceStatus.Active }
                || Active(state, lane.Operator) is not UnitInstance || lane.Entries.Any(entry => entry.Fp <= 0)
                || state.FireLanes.Any(item => item.Weapon == lane.Weapon))
            {
                return Fail<GameState>("UNIT-STATE-040", "A Fire Lane is placed in the MPh by its MG's fire record (A9.22).");
            }

            return state with
            {
                FireLanes = [.. state.FireLanes, new FireLane(lane.Fire, lane.Weapon, lane.Operator, lane.Entries)],
            };
        }

        /// <summary>A9.223 (ruling R12.7): a Fire Lane ends when its MG malfunctions or its manning Infantry breaks, is pinned, or is eliminated.</summary>
        private static GameState KeepFireLanes(GameState next) =>
            next.FireLanes.Count == 0 ? next : next with
            {
                FireLanes = [.. next.FireLanes.Where(lane => next.Find(lane.Weapon) is EquipmentInstance { Status: InstanceStatus.Active } mg
                    && GameState.Condition(mg, Conditions.Malfunctioned) != ConditionState.True
                    && next.Unit(lane.Operator) is { Status: InstanceStatus.Active } manning
                    && GameState.Condition(manning, Conditions.Broken) != ConditionState.True && GameState.Condition(manning, Conditions.Pinned) != ConditionState.True)],
            };

        /// <summary>A PAATC (A11.6, A12.41): its DR agrees with its result; the units that pass need no other PAATC against that vehicle this phase.</summary>
        private GameState? TakePaatc(GameState state, PaatcTaken paatc)
        {
            if (paatc.Units.Count == 0 || paatc.Units.Any(id => Active(state, id) is not UnitInstance) || Active(state, paatc.Vehicle) is not UnitInstance)
            {
                return Fail<GameState>("UNIT-STATE-039", "A PAATC names active units and an active vehicle (A11.6).");
            }

            if (!rolls.TryGetValue(paatc.Roll, out var roll) || roll.Count != 2 || roll.Sides != 6
                || (roll.Values[0] + roll.Values[1] + paatc.Drm <= paatc.Morale) != paatc.Passed)
            {
                return Fail<GameState>("UNIT-STATE-039", "The PAATC's result disagrees with its DR (A11.6).");
            }

            return paatc.Passed
                ? state with
                {
                    PaatcPassed = [.. state.PaatcPassed, .. paatc.Units.Select(id => id + "|" + paatc.Vehicle)]
                }
                : state;
        }

        private GameState? CloseWindow(GameState state, MovementWindowClosed closed)
        {
            if (state.Movement is not { WindowOpen: true } movement || movement.Step != closed.Step)
            {
                return Fail<GameState>("UNIT-STATE-029", $"No DEFENDER window is open on step {closed.Step}.");
            }

            var next = state with
            {
                Movement = movement with
                {
                    WindowOpen = false
                }
            };

            // Ruling R5.15: a vehicle that spent its MP left in its final hex ends its move when the DEFENDER passes; A24.1 (ruling R9.5): so does a squad
            // whose SMOKE dr was a 6.
            // D7.1 (ruling R11.11): a vehicle that bogs or is immobilized entering the Location it OVRs still resolves the OVR, and its move ends only
            // after the Reaction window that follows (table-player finding).
            var ending = movement.EndingMembers.Where(id => movement.Members.Contains(id, StringComparer.Ordinal) && Active(next, id) is UnitInstance { MovementEnded: false }).ToArray();
            var ended = movement.Overrun is not null ? next
                : movement.Ending ? EndMovement(next, new MovementEnded([.. movement.Members.Count > 0 ? movement.Members : movement.Movers]))
                : ending.Length > 0 ? EndMovement(next, new MovementEnded(ending)) : next;

            // A4.134 (ruling R10.9): once all First Fire at a Minimum Move is done, its unbroken survivors are pinned and CX; a vehicle's Minimum Move
            // (D2.15) is not Infantry's and leaves it in Motion only (table-player finding).
            if (ended is not null && movement.MinimumMove && !movement.Vehicle)
            {
                foreach (var id in movement.Movers)
                {
                    if (Active(ended, id) is UnitInstance { } mover && GameState.Condition(mover, Conditions.Broken) != ConditionState.True)
                    {
                        ended = Replace(ended, mover with
                        {
                            Conditions = new Dictionary<string, ConditionState>(mover.Conditions, StringComparer.Ordinal)
                            {
                                [Conditions.Pinned] = ConditionState.True,
                                [Conditions.Cx] = ConditionState.True,
                            },
                        });
                    }
                }
            }

            return ended;
        }

        /// <summary>
        /// The ATTACKER ends the move of some or all of the stack's members (A4.2, A8.11). A unit that already left the stack
        /// by breaking or pinning may be named too, as a record made before stacks could split names every mover.
        /// </summary>
        private GameState? EndMovement(GameState state, MovementEnded ended)
        {
            if (state.Movement is not { WindowOpen: false } movement || ended.Movers.Count == 0
                || ended.Movers.Distinct(StringComparer.Ordinal).Count() != ended.Movers.Count
                || ended.Movers.Any(id => !movement.Members.Contains(id, StringComparer.Ordinal) && !movement.Movers.Contains(id, StringComparer.Ordinal)))
            {
                return Fail<GameState>("UNIT-STATE-029", "The ATTACKER ends the move of the moving stack's members once the DEFENDER's window closes (A4.2, A8.11).");
            }

            var next = state;
            foreach (var id in ended.Movers)
            {
                if (Active(next, id) is UnitInstance unit)
                {
                    // D2.4: a vehicle that ends its move without stopping is in Motion; one that stopped, or was stopped by a Stun or
                    // immobilization (D5.34), is not.
                    var conditions = movement.Vehicle
                        ? new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal)
                        {
                            [Conditions.Motion] = movement.Stopped || !movement.Members.Contains(id, StringComparer.Ordinal) ? ConditionState.False : ConditionState.True,
                        }
                        : unit.Conditions;
                    next = Replace(next, unit with
                    {
                        MovementEnded = true,
                        Conditions = conditions,
                    })!;
                }
            }

            return Settle(next, movement with
            {
                Members = [.. movement.Members.Where(id => !ended.Movers.Contains(id, StringComparer.Ordinal))],
                Movers = [.. movement.Movers.Where(id => !ended.Movers.Contains(id, StringComparer.Ordinal))],
            });
        }

        /// <summary>
        /// The moving stack after the ATTACKER ends some of its members: while a member may still move it goes on; when none
        /// may, its move is over (A4.2), and the step-keyed fire records of A7.55, which concern only its own MF expenditures,
        /// go with it.
        /// </summary>
        private static GameState Settle(GameState state, MovementState movement) =>
            movement.Members.Count > 0
                ? state with
                {
                    Movement = movement
                }
                : state with
                {
                    Movement = null,
                    FiresThisPhase = [.. state.FiresThisPhase.Where(record => record.Step is null)],
                };

        /// <summary>
        /// A4.2, A7.8: a member of the moving stack that is broken, pinned, or no longer active leaves the stack and ends its
        /// MPh, and the others may move on; a member Reduced to a HS is followed by the HS (A7.302). The member stays a target
        /// in the open window (A8.14), and the stack's move ends only when the ATTACKER ends it, even with no member left.
        /// </summary>
        private static GameState KeepMovingStack(GameState next, EventPayload payload)
        {
            if (next.Movement is not { } movement || next.Phase != "mph")
            {
                return next;
            }

            var members = movement.Members.ToList();
            var movers = movement.Movers.ToList();
            if (payload is LineageRecorded lineage && lineage.Consumed.Any(id => members.Contains(id, StringComparer.Ordinal) || movers.Contains(id, StringComparer.Ordinal)))
            {
                var produced = lineage.Produced.Select(item => item.Id).ToArray();
                if (lineage.Consumed.Any(id => members.Contains(id, StringComparer.Ordinal)))
                {
                    members = [.. members.Where(id => !lineage.Consumed.Contains(id, StringComparer.Ordinal)), .. produced];
                }

                if (lineage.Consumed.Any(id => movers.Contains(id, StringComparer.Ordinal)))
                {
                    movers = [.. movers.Where(id => !lineage.Consumed.Contains(id, StringComparer.Ordinal)), .. produced];
                }
            }

            // D5.34, A7.82 (unit step 25): a moving vehicle stops when its crew is Stunned or Recalled or it is immobilized, but a pin
            // never stops it.
            var leaving = members.Where(id => next.Unit(id) is not { Status: InstanceStatus.Active } unit
                || (movement.Vehicle
                    ? new[] { Conditions.Stunned, Conditions.Shocked, Conditions.UnconfirmedKill, Conditions.Immobilized, Conditions.Abandoned }.Any(name => GameState.Condition(unit, name) == ConditionState.True)

                        // D8.2 (ruling R11.9): a Bog, or a Bog Removal that does not free it, stops it too.
                        || (GameState.Condition(unit, Conditions.Bogged) == ConditionState.True && payload is VehicleCheckRolled)
                        || (GameState.Condition(unit, Conditions.Recalled) == ConditionState.True && GameState.Condition(unit, Conditions.StunRecovery) != ConditionState.True)
                    : GameState.Condition(unit, Conditions.Broken) == ConditionState.True || GameState.Condition(unit, Conditions.Pinned) == ConditionState.True))
                .ToArray();
            if (leaving.Length == 0 && members.Count == movement.Members.Count && movers.SequenceEqual(movement.Movers))
            {
                return next;
            }

            foreach (var id in leaving)
            {
                if (next.Unit(id) is { Status: InstanceStatus.Active, MovementEnded: false } unit)
                {
                    next = Replace(next, unit with
                    {
                        MovementEnded = true
                    })!;
                }
            }

            return next with
            {
                Movement = movement with
                {
                    Members = [.. members.Where(id => !leaving.Contains(id, StringComparer.Ordinal))],
                    Movers = movers,
                }
            };
        }

        /// <summary>
        /// A public report of a fire record withheld from the firing side: it must name such a record, once, and repeat
        /// its Locations and its arithmetic exactly.
        /// </summary>
        private GameState? Report(GameState state, FireReported report, GameEvent gameEvent)
        {
            if (!fires.TryGetValue(report.Fire, out var recorded))
            {
                return Fail<GameState>("UNIT-STATE-025", $"The fire report names '{report.Fire}', which is not a fire record before it.");
            }

            if (!recorded.Withheld || gameEvent.Visibility is not null || recorded.Reported)
            {
                return Fail<GameState>("UNIT-STATE-025",
                    "A fire report is public, and stands once for a fire record that only the target side may see.");
            }

            if (report.FirerLocation != recorded.Fire.FirerLocation || report.TargetLocation != recorded.Fire.TargetLocation
                || !recorded.Fire.Resolution.TryGetProperty("arithmetic", out var arithmetic) || !JsonElement.DeepEquals(arithmetic, report.Arithmetic))
            {
                return Fail<GameState>("UNIT-STATE-025", $"The fire report differs from the record '{report.Fire}'.");
            }

            fires[report.Fire] = recorded with
            {
                Reported = true
            };
            return state;
        }

        private bool CheckPhase(GameState state, int turn, string phase, string phasingSide)
        {
            var ok = true;
            if (turn < 1)
            {
                ok = Error("UNIT-STATE-015", "The turn starts at 1.");
            }

            if (!Phases.All.Contains(phase, StringComparer.Ordinal))
            {
                ok = Error("UNIT-STATE-015", $"'{phase}' is not one of {string.Join(", ", Phases.All)} (A3.1 to A3.8, p. 47).");
            }

            if (state.Side(phasingSide) is null)
            {
                ok = Error("UNIT-STATE-015", $"'{phasingSide}' is not a side of this game.");
            }

            return ok;
        }

        private GameState? Create(GameState state, NewInstance instance, IReadOnlyList<string> from)
        {
            if (!VocabularyNames.IsSlug(instance.Id) || state.Find(instance.Id) is not null)
            {
                return Fail<GameState>("UNIT-STATE-006", $"The instance id '{instance.Id}' must be a slug not already in the game.");
            }

            if (!vocabulary.HasKind(instance.Kind))
            {
                return Fail<GameState>("UNIT-STATE-008", $"The kind '{instance.Kind}' is not declared.");
            }

            if (instance.Side is not null && state.Side(instance.Side) is null)
            {
                return Fail<GameState>("UNIT-STATE-005", $"'{instance.Side}' is not a side of this game.");
            }

            if (!CheckConditions(instance.Conditions))
            {
                return null;
            }

            var position = instance.Position;
            if (vocabulary.IsA(instance.Kind, "asl:equipment"))
            {
                if (instance.Holding is { Role: not HoldingRole.Manned } && position is not null)
                {
                    return Fail<GameState>("UNIT-STATE-012", "Possessed or towed equipment is where its holder is and takes no position of its own.");
                }

                if ((instance.Holding is null || instance.Holding.Role == HoldingRole.Manned) && position is null)
                {
                    return Fail<GameState>("UNIT-STATE-010", "Equipment without a holder, or a manned Gun, needs a position.");
                }

                // Catalog 1.2.0 gives SW definitions; equipment may still be created by kind alone.
                DefinitionReference? equipmentDefinition = null;
                if (instance.Definition is not null)
                {
                    if (catalog?.Definition(instance.Definition) is not { } swDefinition || swDefinition.Kind != instance.Kind)
                    {
                        return Fail<GameState>("UNIT-STATE-008", $"'{instance.Definition}' is not a {instance.Kind} definition of the game's catalog.");
                    }

                    equipmentDefinition = catalog.Reference(swDefinition);
                }

                var equipment = new EquipmentInstance(instance.Id, instance.Kind, instance.Side, position ?? NotEnteredPosition.Instance, instance.Holding,
                    instance.Conditions, InstanceStatus.Active)
                {
                    Definition = equipmentDefinition,
                };
                return CheckPosition(state, equipment.Kind, equipment.Position) ? state with
                {
                    Equipment = [.. state.Equipment, equipment]
                } : null;
            }

            if (instance.Holding is not null)
            {
                return Fail<GameState>("UNIT-STATE-012", "Only equipment has a holder.");
            }

            position ??= NotEnteredPosition.Instance;
            if (!CheckPosition(state, instance.Kind, position))
            {
                return null;
            }

            if (vocabulary.IsA(instance.Kind, "asl:entity"))
            {
                return state with
                {
                    Entities = [.. state.Entities, new EntityInstance(instance.Id, instance.Kind, instance.Side, position, instance.Conditions, InstanceStatus.Active)]
                };
            }

            if (instance.Side is null)
            {
                return Fail<GameState>("UNIT-STATE-005", "A unit has an owning side (ASL-UNIT-021).");
            }

            // A Dummy is a concealment counter with no unit beneath (A12.11), so it has no definition.
            if (instance.Kind == UnitKinds.Dummy)
            {
                return instance.Definition is not null
                    ? Fail<GameState>("UNIT-STATE-008", "A Dummy has no definition.")
                    : instance.Group is { } dummyGroup && state.Side(instance.Side)?.Groups.All(item => item.Id != dummyGroup) != false
                        ? Fail<GameState>("UNIT-STATE-005", $"'{dummyGroup}' is not an OB group of side '{instance.Side}'.")
                        : state with
                        {
                            // Pass 19 (ruling R19.5): a Dummy keeps its OB group, whose "?" it uses.
                            Units = [.. state.Units, new UnitInstance(instance.Id, instance.Kind, null, instance.Side, position, instance.Conditions, InstanceStatus.Active, from)
                            {
                                Group = instance.Group,
                            }]
                        };
            }

            if (instance.Definition is null || catalog!.Definition(instance.Definition) is not { } definition)
            {
                return Fail<GameState>("UNIT-STATE-008", $"The unit '{instance.Id}' needs a definition from {catalog!.Identity}.");
            }

            if (definition.Kind != instance.Kind)
            {
                return Fail<GameState>("UNIT-STATE-008", $"The definition '{definition.Id}' is a {definition.Kind}, not a {instance.Kind}.");
            }

            if (state.Side(instance.Side)!.Nationality != definition.Nationality)
            {
                diagnostics.Add(UnitDiagnostic.Warning("UNIT-STATE-008",
                    $"The {definition.Nationality} definition '{definition.Id}' serves the {state.Side(instance.Side)!.Nationality} side '{instance.Side}'.", path));
            }

            // Ruling R18.3: a unit names an OB group of its side, or keeps the group of the units it comes from (Replacement, Deployment).
            // Units of two groups recombined take the group with the lower ELR (referee, pass 18).
            var group = instance.Group ?? from.Select(id => state.Unit(id)).Where(item => item?.Group is not null)
                .OrderBy(item => state.ElrOf(item!) ?? int.MaxValue).Select(item => item!.Group).FirstOrDefault();
            if (instance.Group is { } named && instance.Side is { } owner && state.Side(owner)?.Groups.All(item => item.Id != named) != false)
            {
                return Fail<GameState>("UNIT-STATE-005", $"'{named}' is not an OB group of side '{owner}'.");
            }

            var unit = new UnitInstance(instance.Id, instance.Kind, catalog.Reference(definition), instance.Side, position, instance.Conditions,
                InstanceStatus.Active, from)
            {
                Group = group,
            };
            return state with
            {
                Units = [.. state.Units, unit]
            };
        }

        private GameState? Move(GameState state, InstanceMoved move)
        {
            var item = Active(state, move.Id);
            if (item is null || !CheckPosition(state, item.Kind, move.Position))
            {
                return null;
            }

            if (item is UnitInstance { MovementEnded: true } && move.Mf is > 0)
            {
                return Fail<GameState>("UNIT-STATE-018", $"'{item.Id}' may not move again this phase (A4.1, p. 48).");
            }

            if (state.OpenAttempts.Any(open => open.Unit == item.Id))
            {
                return Fail<GameState>("UNIT-STATE-018", $"'{item.Id}' has an open entry attempt, so it may not move.");
            }

            if (move.Mf is < 0)
            {
                return Fail<GameState>("UNIT-STATE-010", "A move cannot spend negative MF.");
            }

            if (move.Mf is not null && item is not UnitInstance)
            {
                return Fail<GameState>("UNIT-STATE-010", "Only units spend MF.");
            }

            return item switch
            {
                // A20.53 (ruling R14.5): a Guard's prisoners go with it.
                UnitInstance unit => MovePrisoners(Replace(state, unit with { Position = move.Position, MfSpent = unit.MfSpent + (move.Mf ?? 0) })!, unit.Id, move.Position),
                EntityInstance entity => Replace(state, entity with { Position = move.Position }),
                _ => Fail<GameState>("UNIT-STATE-012", "Equipment moves with equipment-transferred."),
            };
        }

        /// <summary>An entry attempt in the MPh by a unit of the phasing side that may still move; nothing changes until it resolves.</summary>
        private GameState? Attempt(GameState state, EntryAttempted attempt, string eventId)
        {
            if (Active(state, attempt.Id) is not { } item)
            {
                return null;
            }

            if (item is not UnitInstance unit)
            {
                return Fail<GameState>("UNIT-STATE-018", $"'{attempt.Id}' is not a unit, so it cannot attempt an entry.");
            }

            if (state.Phase != "mph" || state.PhasingSide != unit.Side)
            {
                return Fail<GameState>("UNIT-STATE-018", $"'{unit.Id}' can attempt an entry only in the MPh of its own side.");
            }

            if (unit.MovementEnded)
            {
                return Fail<GameState>("UNIT-STATE-018", $"'{unit.Id}' may not move again this phase (A4.1, p. 48).");
            }

            if (state.OpenAttempts.Any(open => open.Unit == unit.Id))
            {
                return Fail<GameState>("UNIT-STATE-018", $"'{unit.Id}' already has an open entry attempt.");
            }

            if (attempt.Mf < 0)
            {
                return Fail<GameState>("UNIT-STATE-010", "An attempt cannot cost negative MF.");
            }

            if (!CheckPosition(state, unit.Kind, new MapPosition(attempt.Target)))
            {
                return null;
            }

            return state with
            {
                OpenAttempts = [.. state.OpenAttempts, new OpenAttempt(eventId, unit.Id, attempt.Target, attempt.Mf)]
            };
        }

        /// <summary>
        /// A Random Selection for an open attempt (A.9, p. 43): its roll is recorded, has one die per subject, and every
        /// subject is an active unit at the attempt's target. The units with the highest value must be the ones revealed.
        /// </summary>
        private GameState? Select(GameState state, RandomSelection selection)
        {
            if (state.OpenAttempts.FirstOrDefault(open => open.EventId == selection.Attempt) is not { } attempt)
            {
                return Fail<GameState>("UNIT-STATE-020", $"'{selection.Attempt}' is not an open entry attempt.");
            }

            // After an election, one selection may follow a passed OVR NTC, for the second defender; otherwise an attempt
            // has at most one selection, for its first reveal.
            var second = attempt.Declaration == OverrunDeclared.Elected;
            if (second ? attempt.TaskCheckPassed != true || attempt.SecondSelection : attempt.Revealing.Count > 0)
            {
                return Fail<GameState>("UNIT-STATE-020", second
                    ? $"After an election, one Random Selection follows only a passed NTC."
                    : $"The attempt '{attempt.EventId}' already has a Random Selection.");
            }

            if (!rolls.TryGetValue(selection.Roll, out var roll))
            {
                return Fail<GameState>("UNIT-STATE-020", $"'{selection.Roll}' is not a recorded roll.");
            }

            if (selection.Subjects.Count != roll.Count || selection.Subjects.Distinct(StringComparer.Ordinal).Count() != selection.Subjects.Count)
            {
                return Fail<GameState>("UNIT-STATE-020", $"A selection names one distinct unit for each of the {roll.Count} dice.");
            }

            foreach (var subject in selection.Subjects)
            {
                if (state.Unit(subject) is not { Status: InstanceStatus.Active } || state.Location(subject)?.Location != attempt.Target)
                {
                    return Fail<GameState>("UNIT-STATE-020", $"'{subject}' is not an active unit at {attempt.Target}.");
                }
            }

            var highest = roll.Values.Max();
            string[] revealing = [.. selection.Subjects.Where((_, index) => roll.Values[index] == highest)];
            return state with
            {
                OpenAttempts = [.. state.OpenAttempts.Select(open => open.EventId == attempt.EventId
                    ? open with { Revealing = revealing, SecondSelection = second }
                    : open)]
            };
        }

        /// <summary>
        /// The attacker's OVR choice for an open attempt (A12.15, p. 78): made once, by the attempt's unit, after every unit
        /// the attempt had to reveal is known.
        /// </summary>
        private GameState? Declare(GameState state, OverrunDeclared declared)
        {
            if (state.OpenAttempts.FirstOrDefault(open => open.EventId == declared.Attempt) is not { } attempt || attempt.Unit != declared.Id)
            {
                return Fail<GameState>("UNIT-STATE-021", $"'{declared.Attempt}' is not an open entry attempt by '{declared.Id}'.");
            }

            if (declared.Choice is not (OverrunDeclared.Declined or OverrunDeclared.Elected))
            {
                return Fail<GameState>("UNIT-STATE-021", $"'{declared.Choice}' is not declined or elected.");
            }

            if (attempt.Declaration is not null)
            {
                return Fail<GameState>("UNIT-STATE-021", $"The attempt '{attempt.EventId}' already has a declaration.");
            }

            if (attempt.Revealing.Any(id => state.Unit(id) is not { } selected || GameState.Condition(selected, Conditions.Concealed) != ConditionState.False))
            {
                return Fail<GameState>("UNIT-STATE-021", "A declaration follows the reveal of every selected unit.");
            }

            return state with
            {
                OpenAttempts = [.. state.OpenAttempts.Select(open => open.EventId == attempt.EventId ? open with { Declaration = declared.Choice } : open)]
            };
        }

        /// <summary>
        /// A Task Check for an elected OVR (A10.1, p. 65): its roll is two recorded dice, its final DR is the dice plus
        /// every modifier, it passes at or below the Morale Level, and that Morale Level is the unit's printed morale.
        /// </summary>
        private GameState? Check(GameState state, TaskCheck check)
        {
            if (check.Purpose != TaskCheck.OvrNtc)
            {
                return Fail<GameState>("UNIT-STATE-022", $"'{check.Purpose}' is not a reviewed Task Check.");
            }

            if (state.OpenAttempts.FirstOrDefault(open => open.Unit == check.Id) is not { } attempt
                || attempt.Declaration != OverrunDeclared.Elected || attempt.TaskCheckPassed is not null)
            {
                return Fail<GameState>("UNIT-STATE-022", $"'{check.Id}' has no elected OVR awaiting its NTC.");
            }

            if (!rolls.TryGetValue(check.Roll, out var roll) || roll.Count != 2 || roll.Sides != 6)
            {
                return Fail<GameState>("UNIT-STATE-022", $"'{check.Roll}' is not a recorded roll of two dice.");
            }

            var expected = roll.Values.Sum() + check.Modifiers.Sum(modifier => modifier.Value);
            if (check.FinalDr != expected || check.Passed != (check.FinalDr <= check.MoraleLevel))
            {
                return Fail<GameState>("UNIT-STATE-022",
                    $"The final DR is {expected}, and against Morale Level {check.MoraleLevel} it {(expected <= check.MoraleLevel ? "passes" : "fails")}.");
            }

            var printed = state.Unit(check.Id) is { Definition: { } reference }
                ? catalogs.FirstOrDefault(item => item.Identity == reference.Catalog)?.Definition(reference.Definition)?.Printed("front", "asl:morale")?.Value?.Number
                : null;
            if (printed != check.MoraleLevel)
            {
                return Fail<GameState>("UNIT-STATE-022", $"The Morale Level {check.MoraleLevel} is not the unit's printed morale.");
            }

            return state with
            {
                OpenAttempts = [.. state.OpenAttempts.Select(open => open.EventId == attempt.EventId ? open with { TaskCheckPassed = check.Passed } : open)]
            };
        }

        /// <summary>The unit returns to where it is, the attempt's MF are spent there, and its movement ends (A12.15, p. 78).</summary>
        private GameState? ForceBack(GameState state, EntryForcedBack forced, IReadOnlyList<string> causes)
        {
            if (state.OpenAttempts.FirstOrDefault(open => open.EventId == forced.Attempt) is not { } attempt || attempt.Unit != forced.Id)
            {
                return Fail<GameState>("UNIT-STATE-018", $"'{forced.Attempt}' is not an open entry attempt by '{forced.Id}'.");
            }

            if (!causes.Contains(forced.Attempt, StringComparer.Ordinal))
            {
                return Fail<GameState>("UNIT-STATE-018", "A forced back names its attempt among its causes.");
            }

            if (forced.Mf != attempt.Mf)
            {
                return Fail<GameState>("UNIT-STATE-018", $"The attempt cost {attempt.Mf} MF, not {forced.Mf}.");
            }

            if (Active(state, forced.Id) is not UnitInstance unit)
            {
                return null;
            }

            // After an elected OVR the mover is forced back only in the two reviewed outcomes: a failed NTC, or a passed NTC
            // followed by a second reveal that denies the OVR (Scenario A1 OVR NTC Review).
            if (attempt.Declaration == OverrunDeclared.Elected && !(attempt.TaskCheckPassed == false || attempt.SecondSelection))
            {
                return Fail<GameState>("UNIT-STATE-021",
                    $"The attempt '{attempt.EventId}' elected an OVR; it is forced back only after a failed NTC or a second reveal.");
            }

            if (attempt.Revealing.FirstOrDefault(id => state.Unit(id) is { } selected
                && (GameState.Condition(selected, Conditions.Concealed) != ConditionState.False || GameState.Condition(selected, Conditions.Hidden) == ConditionState.True)) is { } unrevealed)
            {
                return Fail<GameState>("UNIT-STATE-020", $"'{unrevealed}' was selected for the reveal but is still concealed.");
            }

            if (state.Location(unit.Id)?.Location != forced.ReturnedTo)
            {
                return Fail<GameState>("UNIT-STATE-018", $"'{unit.Id}' is not at {forced.ReturnedTo}, the location it is forced back to.");
            }

            return Replace(state with
            {
                OpenAttempts = [.. state.OpenAttempts.Where(open => open.EventId != attempt.EventId)]
            }, unit with
            {
                MfSpent = unit.MfSpent + forced.Mf,
                MovementEnded = true
            });
        }

        /// <summary>
        /// A recorded roll (DICE-12): replay uses its values and checks their count and bounds; it never draws. The roll
        /// changes no state.
        /// </summary>
        private GameState? Roll(GameState state, DiceRolled roll)
        {
            if (roll.Source != DiceRolled.SystemSource)
            {
                return Fail<GameState>("UNIT-STATE-019", $"A roll comes from the system, not '{roll.Source}'.");
            }

            if (roll.Count is < 1 or > 100 || roll.Sides < 2 || roll.Values.Count != roll.Count)
            {
                return Fail<GameState>("UNIT-STATE-019", $"A roll of {roll.Count} dice with {roll.Sides} sides cannot record {roll.Values.Count} values.");
            }

            if (roll.Values.Any(value => value < 1 || value > roll.Sides))
            {
                return Fail<GameState>("UNIT-STATE-019", $"Every value of a roll is between 1 and {roll.Sides}.");
            }

            if (!rolls.TryAdd(roll.Roll, roll))
            {
                return Fail<GameState>("UNIT-STATE-019", $"The roll '{roll.Roll}' is recorded twice.");
            }

            return state;
        }

        private GameState? Transfer(GameState state, EquipmentTransferred transfer)
        {
            if (Active(state, transfer.Id) is not { } item)
            {
                return null;
            }

            if (item is not EquipmentInstance equipment)
            {
                return Fail<GameState>("UNIT-STATE-012", $"'{transfer.Id}' is not equipment.");
            }

            var position = transfer.Holding is { Role: not HoldingRole.Manned } ? null : transfer.Position;
            if (position is null && (transfer.Holding is null || transfer.Holding.Role == HoldingRole.Manned))
            {
                return Fail<GameState>("UNIT-STATE-010", "Equipment left without a holder, or a manned Gun, needs a position.");
            }

            if (transfer.Holding is { Role: not HoldingRole.Manned } && transfer.Position is not null)
            {
                return Fail<GameState>("UNIT-STATE-012", "Possessed or towed equipment is where its holder is and takes no position of its own.");
            }

            var moved = equipment with
            {
                Holding = transfer.Holding,
                Position = position ?? NotEnteredPosition.Instance
            };
            return CheckPosition(state, moved.Kind, moved.Position) ? Replace(state, moved) : null;
        }

        /// <summary>A non-OB "?" placed at the end of setup (A12.12; ruling R23.6): the unit is concealed, and recorded as such.</summary>
        private GameState? SetupConceal(GameState state, SetupConcealed concealed) =>
            ChangeConditions(state, new ConditionsChanged(concealed.Id,
                new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.Concealed] = ConditionState.True })) is { } gained
                ? gained with
                {
                    NonObConcealed = [.. state.NonObConcealed, concealed.Id]
                }
                : null;

        private GameState? ChangeConditions(GameState state, ConditionsChanged change)
        {
            if (Active(state, change.Id) is not { } item || !CheckConditions(change.Conditions))
            {
                return null;
            }

            var merged = new Dictionary<string, ConditionState>(item.Conditions, StringComparer.Ordinal);
            foreach (var (name, value) in change.Conditions)
            {
                merged[name] = value;
            }

            if (!CheckConditions(merged))
            {
                return null;
            }

            // A Random Selection decides which units an attempt reveals (A.9, p. 43): no other unit at its target loses concealment.
            var revealed = change.Conditions.TryGetValue(Conditions.Concealed, out var concealed) && concealed == ConditionState.False;
            if (revealed && state.Location(item.Id)?.Location is { } at
                && state.OpenAttempts.FirstOrDefault(open => open.Target == at && open.Revealing.Count > 0) is { } selected && !selected.Revealing.Contains(item.Id))
            {
                return Fail<GameState>("UNIT-STATE-020", $"'{item.Id}' was not selected for the reveal of the attempt '{selected.EventId}'.");
            }

            // A9.8 (table player, pass 13): dismantling or assembling a MG in the PFPh is its possessor's use of a SW, so it does not move in the MPh.
            if (item is EquipmentInstance { Holding: { Role: HoldingRole.Possessed } dismantler } && state.Phase == "pfph" && change.Conditions.ContainsKey(Conditions.Dismantled))
            {
                state = state with
                {
                    SupportWeaponUses = [.. state.SupportWeaponUses, new SupportWeaponUse(dismantler.Holder, item.Id)]
                };
            }

            return item switch
            {
                UnitInstance unit => Replace(state, unit with { Conditions = merged }),
                EquipmentInstance equipment => Replace(state, equipment with { Conditions = merged }),
                EntityInstance entity => Replace(state, entity with { Conditions = merged }),
                _ => null,
            };
        }

        private GameState? RecordLineage(GameState state, LineageRecorded lineage)
        {
            var consumed = new List<UnitInstance>();
            foreach (var id in lineage.Consumed)
            {
                if (Active(state, id) is not UnitInstance unit)
                {
                    return Fail<GameState>("UNIT-STATE-013", $"'{id}' is not an active unit that lineage can consume.");
                }

                consumed.Add(unit);
            }

            var (consumedCount, producedCount) = lineage.Action switch
            {
                LineageAction.Deployed => (1, 2),
                LineageAction.Recombined => (2, 1),
                _ => (1, 1),
            };
            if (consumed.Count != consumedCount || lineage.Produced.Count != producedCount)
            {
                return Fail<GameState>("UNIT-STATE-013",
                    $"{lineage.Action} consumes {consumedCount} and produces {producedCount}; this event consumes {consumed.Count} and produces {lineage.Produced.Count}.");
            }

            if (consumed.Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count() != 1
                || lineage.Produced.Any(produced => produced.Side is not null && produced.Side != consumed[0].Side))
            {
                return Fail<GameState>("UNIT-STATE-013", "Lineage keeps one side: the consumed units and the produced ones.");
            }

            var (consumedKind, producedKind) = lineage.Action switch
            {
                LineageAction.Reduced or LineageAction.Deployed => ("asl:squad", "asl:half-squad"),
                LineageAction.Recombined => ("asl:half-squad", "asl:squad"),
                _ => ((string?)null, (string?)null),
            };
            if (consumedKind is not null && (consumed.Any(unit => !vocabulary.IsA(unit.Kind, consumedKind))
                || lineage.Produced.Any(produced => !vocabulary.IsA(produced.Kind, producedKind!))))
            {
                return Fail<GameState>("UNIT-STATE-013", $"{lineage.Action} turns a {consumedKind} into a {producedKind}.");
            }

            if (lineage.Action == LineageAction.Replaced && vocabulary.HasKind(lineage.Produced[0].Kind)
                && vocabulary.SizeClass(lineage.Produced[0].Kind) != vocabulary.SizeClass(consumed[0].Kind))
            {
                return Fail<GameState>("UNIT-STATE-013", "A Replacement unit is the same size as the unit it replaces (A19.13, p. 86).");
            }

            var ids = consumed.Select(unit => unit.Id).ToArray();
            var next = state;

            // A10.6 (backlog pass 15): a unit Replaced or Reduced by its own Rally attempt has attempted to rally this Player Turn.
            if (ids.Any(id => state.RallyAttemptsThisPlayerTurn.Contains(id, StringComparer.Ordinal)))
            {
                next = next with
                {
                    RallyAttemptsThisPlayerTurn = [.. next.RallyAttemptsThisPlayerTurn, .. lineage.Produced.Select(item => item.Id)]
                };
            }

            // A10.53 (referee, pass 13): a HS Reduced from a routing squad has routed this RtPh.
            if (state.Phase == "rtph" && ids.Any(id => state.RoutedThisPhase.Contains(id, StringComparer.Ordinal)))
            {
                next = next with
                {
                    RoutedThisPhase = [.. next.RoutedThisPhase, .. lineage.Produced.Select(item => item.Id)]
                };
            }
            foreach (var unit in consumed)
            {
                next = Replace(next, unit with
                {
                    Status = InstanceStatus.Consumed
                });
            }

            // A Replacement, whether by a lesser unit (A19.13) or by Battle Hardening (A15.3), is a unit substitution, and a squad Reduced
            // to a HS (A7.302) leaves the HS in its place as a sub-unit (A4.431; ruling R5.12): the new unit keeps the SW. Deployment and
            // recombination leave it unpossessed.
            var keeps = lineage.Action is LineageAction.Replaced or LineageAction.Reduced;
            if (!keeps)
            {
                next = DropHeldBy(next, ids);
            }

            foreach (var produced in lineage.Produced)
            {
                var instance = produced with
                {
                    Side = produced.Side ?? consumed[0].Side,
                    Position = produced.Position ?? consumed[0].Position
                };
                if (Create(next, instance, ids) is not { } created)
                {
                    return null;
                }

                // The produced unit has spent what the consumed ones spent this phase, and has ended its move if they had
                // (A4.2), so a Replacement, a HS, or a Battle Hardened unit gains no fresh MF.
                next = created.Unit(instance.Id) is { } unit
                    ? Replace(created, unit with
                    {
                        MfSpent = consumed.Max(item => item.MfSpent),
                        HalfMfSpent = consumed.Any(item => item.HalfMfSpent),
                        MovementEnded = consumed.Any(item => item.MovementEnded),
                        DoubleTimeMf = consumed.Max(item => item.DoubleTimeMf),
                        OffRoad = consumed.Any(item => item.OffRoad),
                        MovedWith = consumed.Select(item => item.MovedWith).FirstOrDefault(item => item is not null),
                    })
                    : created;
            }

            // A20.5 (referee, pass 14): a Guard Reduced or Replaced keeps its prisoners in the unit that takes its place.
            if (keeps)
            {
                foreach (var prisoner in next.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Custodian is { } custodian && ids.Contains(custodian)).ToArray())
                {
                    next = Replace(next, prisoner with
                    {
                        Custodian = lineage.Produced[0].Id
                    });
                }
            }

            if (keeps)
            {
                var holder = lineage.Produced[0].Id;
                foreach (var equipment in next.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { } holding
                    && ids.Contains(holding.Holder, StringComparer.Ordinal)).ToArray())
                {
                    next = Replace(next, equipment with
                    {
                        Holding = equipment.Holding! with
                        {
                            Holder = holder
                        }
                    });
                }
            }

            // A11.31 (table-player finding, pass 11): a HS a CC attacker is Reduced to has made its attack in the sequential CC of that Location.
            if (next.CloseCombats.Any(item => item.Attacking.Any(ids.Contains)))
            {
                var produced = lineage.Produced.Select(item => item.Id).ToArray();
                next = next with
                {
                    CloseCombats = [.. next.CloseCombats.Select(item => item.Attacking.Any(ids.Contains) ? item with
                    {
                        Attacking = [.. item.Attacking, .. produced]
                    } : item)],
                };
            }

            return next;
        }

        /// <summary>
        /// A unit created in play, such as a hero (A15.21). One with a creator shares its movement status (ruling R5.11): its MF spent, whether
        /// it has ended its move, and its place among the moving stack's members, so a hero created mid-move may move on with his creator. One
        /// created in its own side's MPh with no creator named, as records made before pass 5 are, moves no further that phase.
        /// </summary>
        private GameState? CreateInPlay(GameState state, InstanceCreated created)
        {
            var instance = created.Instance;

            // A24.1 (table player, pass 9): a SMOKE grenade counter is created only by the attempt that placed it, where it placed it.
            if (instance.Kind == "asl:smoke")
            {
                if (state.SmokePending is not { } pending || !instance.Id.EndsWith(GameState.SmokeGrenadeSuffix, StringComparison.Ordinal)
                    || (instance.Position as MapPosition)?.Location != pending)
                {
                    return Fail<GameState>("UNIT-STATE-006", "A SMOKE counter is created only by a SMOKE attempt that placed it, in the Location it named (A24.1).");
                }

                state = state with
                {
                    SmokePending = null
                };
            }
            UnitInstance? creator = null;
            if (created.Creator is { } creatorId && (Active(state, creatorId) is not UnitInstance found || found.Side != instance.Side))
            {
                return Fail<GameState>("UNIT-STATE-006", $"The creator '{creatorId}' is not an active unit of the created unit's side.");
            }
            else if (created.Creator is { } named)
            {
                creator = state.Unit(named);
            }

            // Ruling R18.3 (referee, pass 18): a hero, a created leader, or a crew bailing out joins the OB group of the unit that made it,
            // or of a unit of its side in its Location.
            if (instance.Group is null && instance.Side is { } createdSide)
            {
                var joined = creator?.Group ?? (instance.Position is MapPosition createdAt
                    ? state.Units.Where(item => item.Status == InstanceStatus.Active && item.Side == createdSide && item.Group is not null
                        && item.Position is MapPosition itemAt && itemAt.Location == createdAt.Location).Select(item => item.Group).FirstOrDefault()
                    : null);
                if (joined is not null)
                {
                    instance = instance with
                    {
                        Group = joined
                    };
                }
            }

            if (Create(state, instance, from: []) is not { } next || next.Unit(instance.Id) is not { } unit)
            {
                return Create(state, instance, from: []) is { } other ? other : null;
            }

            if (creator is not null)
            {
                next = Replace(next, unit with
                {
                    MfSpent = creator.MfSpent,
                    HalfMfSpent = creator.HalfMfSpent,
                    MovementEnded = creator.MovementEnded,
                    DoubleTimeMf = creator.DoubleTimeMf,
                    OffRoad = creator.OffRoad,
                    MovedWith = creator.MovedWith,
                });
                return next.Movement is { } movement && movement.Members.Contains(creator.Id, StringComparer.Ordinal)
                    ? next with
                    {
                        Movement = movement with
                        {
                            Members = [.. movement.Members, unit.Id],
                            Movers = movement.Movers.Contains(creator.Id, StringComparer.Ordinal) ? [.. movement.Movers, unit.Id] : movement.Movers,
                        }
                    }
                    : next;
            }

            return next.Phase == "mph" && unit.Side == next.PhasingSide
                ? Replace(next, unit with
                {
                    MovementEnded = true
                })
                : next;
        }

        private GameState? Eliminate(GameState state, string id)
        {
            if (Active(state, id) is not { } item)
            {
                return null;
            }

            var next = item switch
            {
                UnitInstance unit => Replace(state, unit with { Status = InstanceStatus.Eliminated }),
                EquipmentInstance equipment => Replace(state, equipment with { Status = InstanceStatus.Eliminated }),
                EntityInstance entity => Replace(state, entity with { Status = InstanceStatus.Eliminated }),
                _ => state,
            };
            return DropHeldBy(next, [id]);
        }

        private GameState? Capture(GameState state, InstanceCaptured capture)
        {
            if (Active(state, capture.Id) is not UnitInstance prisoner)
            {
                return Fail<GameState>("UNIT-STATE-014", $"'{capture.Id}' is not an active unit.");
            }

            if (GameState.Condition(prisoner, Conditions.Berserk) == ConditionState.True)
            {
                return Fail<GameState>("UNIT-STATE-014", "Berserk units cannot be captured (A20.2, p. 86).");
            }

            // A15.5, A20.21: a pending surrender is taken by one of its captors.
            if (state.PendingSurrenders.FirstOrDefault(item => item.Unit == prisoner.Id) is { } pending && !pending.Captors.Contains(capture.Custodian, StringComparer.Ordinal))
            {
                return Fail<GameState>("UNIT-STATE-031", $"'{prisoner.Id}' surrenders to one of {string.Join(", ", pending.Captors)} (A15.5).");
            }

            // A20.5 (ruling R14.5): a captured unit is Unarmed.
            var captured = new Dictionary<string, ConditionState>(prisoner.Conditions, StringComparer.Ordinal)
            {
                [Conditions.Captured] = ConditionState.True,
                [Conditions.Unarmed] = ConditionState.True,
            };
            return Replace(state with
            {
                PendingSurrenders = [.. state.PendingSurrenders.Where(item => item.Unit != prisoner.Id)],
            }, prisoner with
            {
                Conditions = captured,
                Custodian = capture.Custodian
            });
        }

        /// <summary>A prisoner freed by its escape or abandoned by its Guard (A20.5, A20.55; rulings R14.6, R14.7): an Unarmed unit of its own side.</summary>
        private GameState? FreePrisoner(GameState state, PrisonerFreed freed)
        {
            // Table player, pass 14: a prisoner whose escape eliminated its Guard was already freed when the Guard left play.
            if (Active(state, freed.Unit) is UnitInstance { Custodian: null } loose && GameState.Condition(loose, Conditions.Unarmed) == ConditionState.True
                && GameState.Condition(loose, Conditions.Captured) != ConditionState.True)
            {
                return state;
            }

            if (Active(state, freed.Unit) is not UnitInstance { Custodian: not null } prisoner)
            {
                return Fail<GameState>("UNIT-STATE-014", $"'{freed.Unit}' is not a guarded prisoner.");
            }

            return Replace(state, prisoner with
            {
                Custodian = null,
                Conditions = new Dictionary<string, ConditionState>(prisoner.Conditions, StringComparer.Ordinal)
                {
                    [Conditions.Captured] = ConditionState.False,
                    [Conditions.Unarmed] = ConditionState.True,
                },
            });
        }

        /// <summary>
        /// A22.3, A23.2 (table player, pass 15): a unit that fires a FT, Throws or Places a DC, is recorded for its Player Turn, since it uses one FT or DC
        /// in a Player Turn; and a squad whose only fire this phase is a Thrown DC has used one SW, so it still fires its inherent FP in this phase.
        /// </summary>
        private static GameState AssaultWeapons(GameState next, EventPayload payload)
        {
            var users = new List<string>();
            var thrownBy = (string?)null;
            if (payload is MovementStepped { DcPlacement: { } placed })
            {
                users.Add(placed.Unit);
            }
            else if (payload is FireResolved fired)
            {
                if (fired.Facts.TryGetProperty("firers", out var firers) && firers.ValueKind == JsonValueKind.Array)
                {
                    users.AddRange(firers.EnumerateArray()
                        .Where(firer => firer.TryGetProperty("weapons", out var weapons) && weapons.ValueKind == JsonValueKind.Array
                            && weapons.EnumerateArray().Any(weapon => weapon.TryGetProperty("equipmentId", out var id)
                                && next.Find(id.GetString() ?? string.Empty) is EquipmentInstance { Kind: "asl:ft" }))
                        .Select(firer => firer.GetProperty("unitId").GetString() ?? string.Empty));
                }

                if (fired.Facts.TryGetProperty("demolitionCharge", out var charge) && charge.ValueKind == JsonValueKind.Object
                    && charge.TryGetProperty("userId", out var user) && user.GetString() is { } userId)
                {
                    users.Add(userId);
                    thrownBy = charge.TryGetProperty("mode", out var mode) && mode.GetString() == "thrown" ? userId : null;
                }
            }

            if (users.Count == 0)
            {
                return next;
            }

            static bool Marked(UnitInstance unit) => new[] { Conditions.PrepFire, Conditions.FinalFire, Conditions.FirstFire }
                .Any(name => GameState.Condition(unit, name) == ConditionState.True);
            return next with
            {
                AssaultWeaponUsers = [.. next.AssaultWeaponUsers.Union(users.Where(id => id.Length > 0), StringComparer.Ordinal)],
                SupportWeaponUses = thrownBy is not null && next.Unit(thrownBy) is { Kind: "asl:squad" } squad && !Marked(squad)
                    && !next.PhaseFirers.Any(item => item.Unit == thrownBy) && !next.SupportWeaponUses.Any(item => item.Unit == thrownBy)
                    ? [.. next.SupportWeaponUses, new SupportWeaponUse(thrownBy, "dc")]
                    : next.SupportWeaponUses,
            };
        }

        /// <summary>
        /// A23.3 (backlog pass 15, ruling R15.2): a DC Placement is recorded with its step; it is operably Placed, and its DC left in its target Location,
        /// once its placer leaves the Placement Location or ends its move neither broken, pinned, captured, nor eliminated; a placer so affected first keeps
        /// its DC. A DC no longer in play is gone. Every Placement ends with its Player Turn's CCPh.
        /// </summary>
        private static GameState KeepPlacedCharges(GameState next, EventPayload payload)
        {
            var charges = next.PlacedCharges;
            if (payload is MovementStepped { DcPlacement: { } placed } step)
            {
                charges = [.. charges, new PlacedCharge(placed.Charge, placed.Unit, step.To, placed.Target, placed.Cx, placed.TargetsConcealed, false)];
            }

            if (charges.Count == 0)
            {
                return next;
            }

            var kept = new List<PlacedCharge>();
            var equipment = next.Equipment;
            foreach (var charge in charges)
            {
                if (next.Find(charge.Charge) is not EquipmentInstance { Status: InstanceStatus.Active } dc)
                {
                    continue;
                }

                if (charge.Operable)
                {
                    kept.Add(charge);
                    continue;
                }

                if (next.Unit(charge.Unit) is not { Status: InstanceStatus.Active } placer || GameState.Condition(placer, Conditions.Broken) == ConditionState.True
                    || GameState.Condition(placer, Conditions.Pinned) == ConditionState.True || GameState.Condition(placer, Conditions.Captured) == ConditionState.True)
                {
                    continue;
                }

                if (next.Movement is { } moving && moving.Movers.Contains(charge.Unit) && moving.Location == charge.From && !placer.MovementEnded)
                {
                    kept.Add(charge);
                    continue;
                }

                kept.Add(charge with
                {
                    Operable = true
                });
                equipment = [.. equipment.Select(item => item.Id == dc.Id ? item with { Holding = null, Position = new MapPosition(charge.Target) } : item)];
            }

            return next with
            {
                PlacedCharges = kept,
                Equipment = equipment,
            };
        }

        /// <summary>
        /// The Wind Change DR (B25.65; backlog pass 16, ruling R16.10): made at the start of a RPh of a game with night or changing weather, its roll
        /// recorded; it sets the Base NVR (0 to 9) and the precipitation it names.
        /// </summary>
        private GameState? ChangeWind(GameState state, WindChanged wind)
        {
            if (state.Phase != "rph" || !rolls.ContainsKey(wind.Roll) || (wind.NvrRoll is { } nvrRoll && !rolls.ContainsKey(nvrRoll))
                || (wind.Nvr is not null) != state.Night || wind.Nvr is < 0 or > 9 || wind.Precipitation is not (null or "rain" or "heavy-rain" or "snow" or "heavy-snow"))
            {
                return Fail<GameState>("UNIT-STATE-043", "A Wind Change DR is made in the RPh with a recorded roll, and sets a Base NVR of 0 to 9 only at night (B25.65, E1.12).");
            }

            return state with
            {
                Nvr = wind.Nvr,
                Precipitation = wind.Precipitation,
                Rained = state.Rained || wind.Precipitation is "rain" or "heavy-rain",
            };
        }

        /// <summary>
        /// A Starshell attempt (E1.92 to E1.923; ruling R16.8): by an active unit at night, once per hex per phase, its rolls recorded; a Starshell that
        /// passed its Usage dr and landed on the map is placed in its Location until the end of the CCPh.
        /// </summary>
        private GameState? FireStarshell(GameState state, StarshellFired starshell)
        {
            if (!state.Night || state.Unit(starshell.Unit) is not { Status: InstanceStatus.Active } || !rolls.ContainsKey(starshell.UsageRoll)
                || (starshell.PlacementRoll is { } placement && !rolls.ContainsKey(placement)) || (starshell.At is not null) != (starshell.Starshell is not null)
                || (!starshell.Passed && starshell.At is not null) || state.StarshellAttempts.Contains(starshell.From.ToString(), StringComparer.Ordinal))
            {
                return Fail<GameState>("UNIT-STATE-044", "A Starshell is fired at night by a unit in play, once per hex per phase, with its rolls recorded (E1.92).");
            }

            var next = state with
            {
                StarshellAttempts = [.. state.StarshellAttempts, starshell.From.ToString()],
                StarshellUsed = state.StarshellUsed || starshell.Passed,
                StarshellTurn = state.StarshellTurn ?? (starshell.Passed ? $"{state.Turn}|{state.PhasingSide}" : null),
            };
            return starshell.At is not { } at ? next : next with
            {
                Entities = [.. next.Entities, new EntityInstance(starshell.Starshell!, "asl:starshell", state.Unit(starshell.Unit)!.Side, new MapPosition(at),
                    new Dictionary<string, ConditionState>(StringComparer.Ordinal), InstanceStatus.Active)],
            };
        }

        /// <summary>A Sniper attack (A14; ruling R15.5): its Sniper counter and roll are in play and recorded; the events after it apply it.</summary>
        private GameState? Snipe(GameState state, SniperAttacked sniper)
        {
            if (state.Find(sniper.Sniper) is not EntityInstance { Kind: "asl:sniper", Status: InstanceStatus.Active } || !rolls.ContainsKey(sniper.Roll)
                || !rolls.ContainsKey(sniper.Trigger) || sniper.Dr is < 1 or > 6)
            {
                return Fail<GameState>("UNIT-STATE-042", "A Sniper attack names a Sniper counter in play and recorded rolls (A14.1).");
            }

            return state;
        }

        /// <summary>
        /// A20.5 (ruling R14.5): when a Guard leaves play or is captured, another armed unit of its side in the prisoners' Location with Guard capacity takes
        /// them, the first by id; with none, they are freed as Unarmed units of their own side. A Guard's moves take its prisoners along; a Guard in another
        /// Location is an invariant error, not repaired here.
        /// </summary>
        private GameState KeepGuards(GameState next)
        {
            static int Size(UnitInstance unit) => unit.Kind == "asl:squad" ? 3 : unit.Kind is "asl:half-squad" or "asl:crew" ? 2 : 1;
            foreach (var prisoner in next.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Custodian is not null).ToArray())
            {
                var at = next.Location(prisoner.Id)?.Location;
                if (next.Unit(prisoner.Custodian!) is { Status: InstanceStatus.Active } guard && GameState.Condition(guard, Conditions.Captured) != ConditionState.True)
                {
                    continue;
                }

                var side = next.Unit(prisoner.Custodian!)?.Side;
                var heir = next.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side && at is not null && next.Location(unit.Id)?.Location == at
                        && GameState.Condition(unit, Conditions.Captured) != ConditionState.True && GameState.Condition(unit, Conditions.Unarmed) != ConditionState.True
                        && vocabulary.IsA(unit.Kind, "asl:personnel") && !vocabulary.IsA(unit.Kind, "asl:vehicle")
                        && next.Units.Where(other => other.Status == InstanceStatus.Active && other.Custodian == unit.Id).Sum(Size) + Size(prisoner) <= 5 * Size(unit))
                    .OrderBy(unit => unit.Id, StringComparer.Ordinal).FirstOrDefault();
                next = Replace(next, heir is not null ? prisoner with
                {
                    Custodian = heir.Id
                } : prisoner with
                {
                    Custodian = null,
                    Conditions = new Dictionary<string, ConditionState>(prisoner.Conditions, StringComparer.Ordinal)
                    {
                        [Conditions.Captured] = ConditionState.False,
                        [Conditions.Unarmed] = ConditionState.True,
                    },
                });
            }

            return next;
        }

        /// <summary>Equipment held by instances that leave play is left at their last location with no holder.</summary>
        private static GameState DropHeldBy(GameState state, IReadOnlyList<string> ids)
        {
            var next = state;
            foreach (var equipment in state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { } holding && ids.Contains(holding.Holder)))
            {
                var left = (Position?)state.Location(equipment.Id) ?? OffMapPosition.Instance;
                next = Replace(next, equipment with
                {
                    Holding = null,
                    Position = left
                });
            }

            return next;
        }

        /// <summary>
        /// A composed map's placement (Composed Maps Design, section 5): every board placed or none, each board once, the
        /// slot rules, and, when the chains give every board's geometry, the whole layout, which positions then use.
        /// </summary>
        private void CheckPlacement(MapInPlay map)
        {
            if (map.Boards.Select(board => board.Board).Distinct().Count() != map.Boards.Count)
            {
                Error("UNIT-STATE-010", "A board is placed more than once in the map in play.");
                return;
            }

            if (!map.IsPlaced)
            {
                if (map.Boards.Any(board => board.Slot is not null))
                {
                    Error("UNIT-STATE-010", "Either every board of the map in play has a slot or none does.");
                }

                return;
            }

            if (MapLayout.CheckSlots(map.Placements()) is { } problem)
            {
                Error("UNIT-STATE-010", $"{problem.Code}: {problem.Message}");
                return;
            }

            if (chains is null || map.Boards.Any(board => chains.Geometry(board.Board) is null))
            {
                return;
            }

            var result = MapLayout.Create([.. map.Placements().Select(placement => (placement, chains.Geometry(placement.Board)!))]);
            layout = result.Layout;
            foreach (var diagnostic in result.Diagnostics)
            {
                Error("UNIT-STATE-010", $"{diagnostic.Code}: {diagnostic.Message}");
            }
        }

        private bool CheckPosition(GameState state, string kind, Position position)
        {
            switch (position)
            {
                case MapPosition map:
                    if (state.Map.Board(map.Location.Board) is null)
                    {
                        return Error("UNIT-STATE-010", $"{map.Location.Board} is not a board in the map in play.");
                    }

                    if (map.Facing is not null && !vocabulary.HasFacing(kind))
                    {
                        return Error("UNIT-STATE-010", $"A {kind} has no facing.");
                    }

                    if (chains is null)
                    {
                        return true;
                    }

                    if (chains.Levels(map.Location.Board, map.Location.Hex) is not { } levels)
                    {
                        return Error("UNIT-STATE-010", $"{map.Location.Hex} is not a hex on {map.Location.Board}.");
                    }

                    if (layout is not null && !layout.IsOwnerName(map.Location.Board, map.Location.Hex)
                        && layout.Locate(map.Location.Board, map.Location.Hex) is { } shared && layout.OwnerOf(shared) is { } owner)
                    {
                        return Error("UNIT-STATE-010", $"{map.Location.Board}:{map.Location.Hex} is a hex shared with {owner.Board}, which names it {owner.Hex}.");
                    }

                    if (map.OnBridge ? !chains.HasBridge(map.Location.Board, map.Location.Hex) : !levels.Contains(map.Location.Level))
                    {
                        return Error("UNIT-STATE-010", map.OnBridge
                            ? $"{map.Location.Board}:{map.Location.Hex} has no bridge."
                            : $"{map.Location} is not in the hex's location chain ({string.Join(", ", levels)}).");
                    }

                    return true;
                case ContainedPosition contained:
                    return state.Find(contained.Container) is not null || Error("UNIT-STATE-011", $"The container '{contained.Container}' is not in the game.");
                default:
                    return true;
            }
        }

        private bool CheckConditions(IReadOnlyDictionary<string, ConditionState> conditions)
        {
            var ok = true;
            foreach (var name in conditions.Keys.Where(name => !Conditions.IsDeclared(name, vocabulary)))
            {
                ok = Error("UNIT-STATE-009", $"The condition '{name}' is not declared.");
            }

            foreach (var group in conditions.Where(pair => pair.Value == ConditionState.True && vocabulary.TryGetState(pair.Key, out _))
                .Select(pair => vocabulary.States.First(state => state.Name == pair.Key))
                .Where(state => state.Group is not null).GroupBy(state => state.Group, StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                ok = Error("UNIT-STATE-009", $"The conditions {string.Join(" and ", group.Select(state => state.Name))} exclude each other ({group.Key}).");
            }

            return ok;
        }

        /// <summary>Containment, holding, and custody hold after every event (ASL-UNIT-022, 025).</summary>
        private void CheckInvariants(GameState state)
        {
            foreach (var item in state.Objects.Where(item => item.Status == InstanceStatus.Active))
            {
                if (item.Position is ContainedPosition contained)
                {
                    var container = state.Find(contained.Container);
                    var needs = contained.Role == ContainmentRole.InFortification ? "asl:fortification" : "asl:vehicle";
                    if (container is null || container.Status != InstanceStatus.Active)
                    {
                        Error("UNIT-STATE-011", $"'{item.Id}' is in '{contained.Container}', which is not active in the game.");
                    }
                    else if (!vocabulary.IsA(container.Kind, needs))
                    {
                        Error("UNIT-STATE-011", $"'{item.Id}' is {contained.Role} in a {container.Kind}, which is not a {needs}.");
                    }
                    else if (InCycle(state, item.Id))
                    {
                        Error("UNIT-STATE-011", $"'{item.Id}' is contained in a cycle.");
                    }
                }

                if (item is EquipmentInstance { Holding: { } holding } equipment)
                {
                    var holder = state.Find(holding.Holder);
                    var needs = holding.Role == HoldingRole.Towed ? "asl:vehicle" : "asl:personnel";
                    if (holder is not UnitInstance { Status: InstanceStatus.Active })
                    {
                        Error("UNIT-STATE-012", $"'{equipment.Id}' is {holding.Role} by '{holding.Holder}', which is not an active unit.");
                    }
                    else if (!vocabulary.IsA(holder.Kind, needs))
                    {
                        Error("UNIT-STATE-012", $"'{equipment.Id}' is {holding.Role} by a {holder.Kind}, which is not a {needs}.");
                    }
                    else if (holding.Role == HoldingRole.Manned && state.Location(holder.Id)?.Location != state.Location(equipment.Id)?.Location)
                    {
                        Error("UNIT-STATE-012", $"'{holder.Id}' mans '{equipment.Id}' from another Location.");
                    }
                }

                if (item is UnitInstance { Custodian: { } custodianId } prisoner)
                {
                    if (state.Unit(custodianId) is not { Status: InstanceStatus.Active } custodian)
                    {
                        Error("UNIT-STATE-014", $"The custodian '{custodianId}' of '{prisoner.Id}' is not an active unit.");
                    }
                    else if (custodian.Side == prisoner.Side)
                    {
                        Error("UNIT-STATE-014", $"'{prisoner.Id}' is held by a unit of its own side.");
                    }
                    else if (state.Location(custodian.Id)?.Location != state.Location(prisoner.Id)?.Location)
                    {
                        Error("UNIT-STATE-014", $"'{prisoner.Id}' and its custodian '{custodian.Id}' are in different Locations (A20.5, p. 87).");
                    }
                }
            }
        }

        private static bool InCycle(GameState state, string id)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var item = state.Find(id); item?.Position is ContainedPosition contained; item = state.Find(contained.Container))
            {
                if (!seen.Add(item.Id))
                {
                    return true;
                }
            }

            return false;
        }

        private IGameObject? Active(GameState state, string id)
        {
            var item = state.Find(id);
            if (item is null)
            {
                return Fail<IGameObject>("UNIT-STATE-006", $"'{id}' is not in the game.");
            }

            return item.Status == InstanceStatus.Active
                ? item
                : Fail<IGameObject>("UNIT-STATE-007", $"'{id}' is {item.Status.ToString().ToLowerInvariant()} and cannot act (ASL-UNIT-025).");
        }

        private static GameState Replace(GameState state, IGameObject item) => item switch
        {
            UnitInstance unit => state with { Units = [.. state.Units.Select(existing => existing.Id == unit.Id ? unit : existing)] },
            EquipmentInstance equipment => state with { Equipment = [.. state.Equipment.Select(existing => existing.Id == equipment.Id ? equipment : existing)] },
            EntityInstance entity => state with { Entities = [.. state.Entities.Select(existing => existing.Id == entity.Id ? entity : existing)] },
            _ => state,
        };

        private bool Error(string code, string message)
        {
            diagnostics.Add(UnitDiagnostic.Error(code, message, path));
            return false;
        }

        private T? Fail<T>(string code, string message)
            where T : class
        {
            Error(code, message);
            return null;
        }
    }
}
