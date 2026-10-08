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
        IRallyRecordVerifier? rally = null, ICloseCombatRecordVerifier? closeCombat = null, IOrdnanceRecordVerifier? ordnance = null) =>
        Begin(events, vocabulary, catalogs, chains, liveSources, fire, rally, closeCombat, ordnance).History;

    /// <summary>
    /// Replays a game's events as <see cref="Project"/> does, and keeps what the replay carries from one event to the next, so the
    /// events that follow can be applied later without the first ones being replayed again (pass 31d, design D1).
    /// </summary>
    public static ReplayedGame Begin(IReadOnlyList<GameEvent> events, UnitVocabulary vocabulary, IReadOnlyList<UnitCatalog> catalogs,
        ILocationChains? chains = null, IReadOnlyCollection<string>? liveSources = null, IFireRecordVerifier? fire = null,
        IRallyRecordVerifier? rally = null, ICloseCombatRecordVerifier? closeCombat = null, IOrdnanceRecordVerifier? ordnance = null)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(vocabulary);
        ArgumentNullException.ThrowIfNull(catalogs);
        var diagnostics = new List<UnitDiagnostic>();
        if (chains is null)
        {
            diagnostics.Add(UnitDiagnostic.Warning("UNIT-STATE-020", "No location chains were given, so map positions are not checked against the boards in play."));
        }

        return Run(new Replay(vocabulary, catalogs, chains, liveSources ?? [], fire, rally, closeCombat, ordnance, diagnostics), [.. events], [], diagnostics);
    }

    /// <summary>Applies the events that have no state yet, stopping at the first that breaks a rule.</summary>
    internal static ReplayedGame Run(Replay replay, GameEvent[] events, List<GameState> states, List<UnitDiagnostic> diagnostics)
    {
        for (var index = states.Count; index < events.Length; index++)
        {
            var errors = Errors(diagnostics);
            var state = replay.Apply(events[index], states.Count > 0 ? states[^1] : null);
            if (Errors(diagnostics) > errors || state is null)
            {
                break;
            }

            states.Add(state);
        }

        return new ReplayedGame(replay, new GameHistory(events, states, diagnostics));
    }

    private static int Errors(List<UnitDiagnostic> diagnostics) =>
        diagnostics.Count(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error);

    internal sealed class Replay(UnitVocabulary vocabulary, IReadOnlyList<UnitCatalog> catalogs, ILocationChains? chains, IReadOnlyCollection<string> liveSources,
        IFireRecordVerifier? fireVerifier, IRallyRecordVerifier? rallyVerifier, ICloseCombatRecordVerifier? closeCombatVerifier,
        IOrdnanceRecordVerifier? ordnanceVerifier, List<UnitDiagnostic> diagnostics)
    {
        private readonly HashSet<string> eventIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DiceRolled> rolls = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (FireResolved Fire, bool Withheld, bool Reported)> fires = new(StringComparer.Ordinal);
        private UnitCatalog? catalog;
        private MapLayout? layout;
        private string path = string.Empty;

        /// <summary>
        /// A replay that carries what this one carries and writes to its own diagnostics (pass 31d, design D1): what it applies leaves this one as
        /// it is. Everything carried between events is one of these six fields.
        /// </summary>
        public Replay Copy(List<UnitDiagnostic> ownDiagnostics)
        {
            var copy = new Replay(vocabulary, catalogs, chains, liveSources, fireVerifier, rallyVerifier, closeCombatVerifier, ordnanceVerifier, ownDiagnostics)
            {
                catalog = catalog,
                layout = layout,
                path = path,
            };
            copy.eventIds.UnionWith(eventIds);
            foreach (var roll in rolls)
            {
                copy.rolls[roll.Key] = roll.Value;
            }

            foreach (var fire in fires)
            {
                copy.fires[fire.Key] = fire.Value;
            }

            return copy;
        }

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
                BuildingMoppedUp mopped => MopUp(previous, mopped),
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
                        .ToDictionary(group => group.Key, group => group.Sum(unit => Rules.ScenarioA1OrdnanceProjection.HalfSquadEquivalents(unit.Kind)),
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

            // A7.351 (table player, pass 9): the units of a fire record have fired this phase, with the SW each used (Rules decides it, pass 32.c).
            if (gameEvent.Payload is FireResolved firing && firing.Firers.Count > 0)
            {
                var used = firing.Facts.TryGetProperty("firers", out var firerFacts) && firerFacts.ValueKind == JsonValueKind.Array
                    ? firerFacts.EnumerateArray().ToDictionary(item => item.GetProperty("unitId").GetString() ?? string.Empty,
                        item => item.TryGetProperty("weapons", out var weapons) && weapons.ValueKind == JsonValueKind.Array ? weapons.GetArrayLength() : 0, StringComparer.Ordinal)
                    : [];
                next = next with
                {
                    PhaseFirers = [.. Rules.ScenarioA1FireFollowUps.PhaseFirersAfterFire([.. next.PhaseFirers.Select(item => (item.Unit, item.Weapon))], firing.Firers, unit => used.GetValueOrDefault(unit))
                        .Select(item => new SupportWeaponUse(item.Unit, item.Weapon))],
                };
            }

            // A7.351 (ruling R9.2): a squad that fires its inherent FP after its one SW use has fired (Rules decides it, pass 32.c).
            if (gameEvent.Payload is FireResolved resolved && Rules.ScenarioA1FireFollowUps.SupportWeaponUseEnds(next.SupportWeaponUses.Select(item => item.Unit), resolved.Firers))
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
            next = KeepPassengers(next);
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

            // A25.8 (ruling R27.1): an Axis Minor side names its nation; no other side names one.
            foreach (var side in started.Sides.Where(side => side.Nationality == "axis-minor"
                ? !SideState.AxisMinorNations.Contains(side.Nation ?? "", StringComparer.Ordinal) : side.Nation is not null))
            {
                Error("UNIT-STATE-005", side.Nationality == "axis-minor"
                    ? $"The Axis Minor side '{side.Id}' needs its nation: {string.Join(", ", SideState.AxisMinorNations)} (A25.8)."
                    : $"Side '{side.Id}' is {side.Nationality}; only an Axis Minor side names a nation (A25.8).");
            }

            // The version a game records is where it was set up, not a lock: it reads the loaded catalog of that name (UnitCatalogs.For).
            catalog = Catalog.UnitCatalogs.For(catalogs, started.Catalog);
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

                // A25.8 (ruling R27.1): Hungarians fighting Romanians face No Quarter on both sides from the start.
                NoQuarter = SideState.HungariansVersusRomanians(started.Sides) ? [.. started.Sides.Select(side => side.Id)] : [],
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
            // A11.3 (ruling R31c.5; the third play test): a unit eliminated before it attacks forfeits its attack, so a Location whose
            // ambusher's attacks left no unit of the ambushed side there has nothing more to resolve and does not hold the phase.
            bool NothingLeft(CloseCombatLocation item) => item.Ambusher is { } ambusher && item.Rounds.Count > 0
                && !state.Units.Any(unit => unit.Status == InstanceStatus.Active && unit.Side != ambusher
                    && GameState.Condition(unit, Conditions.Captured) != ConditionState.True && state.Location(unit.Id)?.Location == item.Location);
            if (state.CloseCombats.FirstOrDefault(item => !item.Closed && !NothingLeft(item)) is { } open)
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
                    // A12.153 (ruling R24.2): a building is Mopped Up once per Player Turn.
                    MoppedUpThisPlayerTurn = newPlayerTurn ? [] : state.MoppedUpThisPlayerTurn,
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

        /// <summary>The facts of a recorded SMOKE attempt against the state, the catalog, and the recorded dr (pass 32.a), for Rules to check.</summary>
        private Rules.SmokeRecordFacts SmokeFacts(GameState state, MovementStepped moving, SmokeAttempt smoke)
        {
            var placer = Active(state, smoke.Unit) as UnitInstance;
            var rollFound = rolls.TryGetValue(smoke.Roll, out var roll);
            return new Rules.SmokeRecordFacts(moving.Movers.Contains(smoke.Unit, StringComparer.Ordinal), state.SmokeAttempts.Contains(smoke.Unit, StringComparer.Ordinal),
                moving.To == state.Location(smoke.Unit)?.Location, rollFound, roll?.Count ?? 0, roll is not null && roll.Count == 1 ? roll.Values[0] : null, smoke.Dr, smoke.Exponent,
                placer is not null, placer is null ? null : SmokeExponent(placer), smoke.Cx, moving.DoubleTime,
                placer is not null && GameState.Condition(placer, Conditions.Cx) == ConditionState.True, moving.HalfMf, smoke.Target == moving.To);
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
            var units = state.Units.Where(unit => Rules.ScenarioA1CloseCombatProjection.StartUnit(unit.Status == InstanceStatus.Active, GameState.Condition(unit, Conditions.Captured) == ConditionState.True,
                state.Location(unit.Id) is not null)).ToArray();
            foreach (var group in units.GroupBy(unit => state.Location(unit.Id)!.Location).Where(group => Rules.ScenarioA1CloseCombatProjection.Contested(group.Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count())))
            {
                foreach (var unit in group.OrderBy(unit => unit.Id, StringComparer.Ordinal))
                {
                    if (Rules.ScenarioA1CloseCombatProjection.StartEliminates(unit.Kind == "asl:dummy"))
                    {
                        state = Replace(state, unit with
                        {
                            Status = InstanceStatus.Eliminated
                        });
                    }
                    else if (Rules.ScenarioA1CloseCombatProjection.StartPlaces(unit.Kind == "asl:dummy", GameState.Condition(unit, Conditions.Hidden) == ConditionState.True))
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
            var units = state.Units.Where(unit => Rules.ScenarioA1CloseCombatProjection.MeleeCandidate(unit.Status == InstanceStatus.Active, GameState.Condition(unit, Conditions.Captured) == ConditionState.True,
                GameState.Condition(unit, Conditions.Concealed) == ConditionState.True, GameState.Condition(unit, Conditions.Hidden) == ConditionState.True,
                unit.Kind == "asl:dummy", state.Location(unit.Id) is not null)).ToArray();
            return units.GroupBy(unit => state.Location(unit.Id)!.Location)
                .SelectMany(group => Rules.ScenarioA1CloseCombatProjection.InMelee([.. group.Select(unit => new Rules.MeleeUnitFacts(unit.Id, unit.Side, vocabulary.IsA(unit.Kind, "asl:vehicle"), HoldsInMelee(unit),
                    GameState.Condition(unit, Conditions.Concealed) == ConditionState.True, GameState.Condition(unit, Conditions.Hidden) == ConditionState.True))]))
                .ToHashSet(StringComparer.Ordinal);
        }

        /// <summary>Whether a unit holds the enemy Infantry in its Location in Melee (A11.15, A11.7): any Infantry, or a vehicle not Abandoned and not in Motion.</summary>
        private bool HoldsInMelee(UnitInstance unit) => Rules.ScenarioA1CloseCombatProjection.HoldsInMelee(vocabulary.IsA(unit.Kind, "asl:vehicle"),
            GameState.Condition(unit, Conditions.Abandoned) == ConditionState.True, GameState.Condition(unit, Conditions.Motion) == ConditionState.True);

        private static UnitInstance HoldInMelee(UnitInstance unit, HashSet<string>? melee) =>
            melee is null || Rules.ScenarioA1CloseCombatProjection.MeleeChange(melee.Contains(unit.Id), GameState.Condition(unit, Conditions.Melee) == ConditionState.True) is not { } held
                ? unit
                : unit with
                {
                    Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal)
                    {
                        [Conditions.Melee] = held ? ConditionState.True : ConditionState.False
                    }
                };

        /// <summary>A11.15: a unit is no longer held in Melee once no enemy unit that is not a prisoner shares its Location.</summary>
        private static GameState KeepMelee(GameState next)
        {
            var freed = next.Units.Where(unit => unit.Status == InstanceStatus.Active && next.Location(unit.Id) is { } at && Rules.ScenarioA1CloseCombatProjection.LeavesMelee(GameState.Condition(unit, Conditions.Melee) == ConditionState.True,
                next.Units.Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side
                    && GameState.Condition(other, Conditions.Captured) != ConditionState.True && next.Location(other.Id)?.Location == at.Location))).ToArray();
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
            if (Rules.ScenarioA1CloseCombatProjection.AdvanceRefusal(state.Phase, units.Length, units.Any(unit => unit is null || unit.Side != state.PhasingSide || unit.MovementEnded),
                units.Any(unit => unit is null) ? 0 : units.Select(unit => state.Location(unit!.Id)?.Location).Distinct().Count(), advance.Units.Distinct(StringComparer.Ordinal).Count()) is { } refused)
            {
                return Fail<GameState>(refused.Code, refused.Text);
            }

            if (units.FirstOrDefault(unit => Rules.ScenarioA1CloseCombatProjection.AdvanceBarred(GameState.Condition(unit!, Conditions.Broken) == ConditionState.True, GameState.Condition(unit!, Conditions.Pinned) == ConditionState.True,
                GameState.Condition(unit!, Conditions.Berserk) == ConditionState.True, GameState.Condition(unit!, Conditions.Melee) == ConditionState.True,
                GameState.Condition(unit!, Conditions.Captured) == ConditionState.True)) is { } barred)
            {
                var barredRefusal = Rules.ScenarioA1CloseCombatProjection.AdvanceBarredRefusal(barred.Id);
                return Fail<GameState>(barredRefusal.Code, barredRefusal.Text);
            }

            // A2.6 (ruling R25.5): an advance may leave the map from the units' own Location.
            if (advance.Exit is { } edge)
            {
                return Rules.ScenarioA1CloseCombatProjection.ExitRefusal(units.Any(unit => state.Location(unit!.Id)?.Location != advance.To)) is { } exitRefusal
                    ? Fail<GameState>(exitRefusal.Code, exitRefusal.Text)
                    : ExitUnits(state, [.. units.Select(unit => unit!)], advance.To, edge);
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

        /// <summary>
        /// Units leave the map (A2.6; rulings R21.5, R25.5): each is Exited, not eliminated, with the SW it carries (table player, pass 21); a Guard's
        /// prisoners leave with it (A20.53), recorded as held by the Guard's side, and the Guard as escorting them (A26.221).
        /// </summary>
        private static GameState ExitUnits(GameState state, IReadOnlyList<UnitInstance> movers, BoardLocation from, string edge)
        {
            var prisoners = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Custodian is { } guard && movers.Any(mover => mover.Id == guard)).ToArray();
            string[] leaving = [.. movers.Select(unit => unit.Id), .. prisoners.Select(unit => unit.Id)];
            var exited = state;
            foreach (var unit in movers.Concat(prisoners))
            {
                exited = Replace(exited, unit with
                {
                    Position = OffMapPosition.Instance,
                    Status = InstanceStatus.Exited,
                })!;
            }

            return exited with
            {
                Equipment = [.. exited.Equipment.Select(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding
                    && leaving.Contains(holding.Holder, StringComparer.Ordinal) ? item with { Status = InstanceStatus.Exited } : item)],
                Exits = [.. state.Exits,
                    .. movers.Select(unit => new UnitExit(unit.Id, from, edge, state.Turn, GameState.Condition(unit, Conditions.Broken) == ConditionState.True)
                    {
                        Escort = prisoners.Any(prisoner => prisoner.Custodian == unit.Id),
                    }),
                    .. prisoners.Select(unit => new UnitExit(unit.Id, from, edge, state.Turn, false, movers.First(mover => mover.Id == unit.Custodian).Side))],
            };
        }

        /// <summary>
        /// D6.1 (ruling R26.2): Passengers and Riders whose vehicle is no longer in play share its fate: eliminated with it, since Crew Survival for
        /// Passengers (D5.6) is not built; exited with it, as its exit records.
        /// </summary>
        private static GameState KeepPassengers(GameState state)
        {
            var next = state;
            foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Position is ContainedPosition { Role: ContainmentRole.Passenger or ContainmentRole.Rider }))
            {
                var container = state.Find(((ContainedPosition)unit.Position).Container);
                if (container is null || container.Status == InstanceStatus.Active)
                {
                    continue;
                }

                next = Replace(next, unit with
                {
                    Status = Rules.ScenarioA1VehicleProjection.PassengerExits(container.Status == InstanceStatus.Exited) ? InstanceStatus.Exited : InstanceStatus.Eliminated,
                })!;
                next = next with
                {
                    Equipment = [.. next.Equipment.Select(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id
                        ? item with { Status = Rules.ScenarioA1VehicleProjection.PassengerExits(container.Status == InstanceStatus.Exited) ? InstanceStatus.Exited : InstanceStatus.Eliminated }
                        : item)],
                };
            }

            return next;
        }

        /// <summary>A20.53: a Guard's prisoners move with it.</summary>
        private static GameState MovePrisoners(GameState state, string guard, Position position)
        {
            foreach (var prisoner in state.Units.Where(unit => Rules.ScenarioA1CloseCombatProjection.FollowsGuard(unit.Status == InstanceStatus.Active, unit.Custodian == guard)).ToArray())
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
                var noVerifier = Rules.ScenarioA1CloseCombatProjection.AmbushVerifierRefusal();
                return Fail<GameState>(noVerifier.Code, noVerifier.Text);
            }

            if (ambush.Rolls.Values.FirstOrDefault(roll => !rolls.ContainsKey(roll)) is { } missing)
            {
                var noRoll = Rules.ScenarioA1CloseCombatProjection.AmbushRollRefusal(missing);
                return Fail<GameState>(noRoll.Code, noRoll.Text);
            }

            if (Rules.ScenarioA1CloseCombatProjection.AmbushOrderRefusal(state.Phase, state.CloseCombats.Any(item => item.Location == ambush.Location || !item.Closed)) is { } order)
            {
                return Fail<GameState>(order.Code, order.Text);
            }

            if (closeCombatVerifier.VerifyAmbush(state, ambush, rolls) is { } reason)
            {
                var verifier = Rules.ScenarioA1CloseCombatProjection.VerifierRefusal(reason);
                return Fail<GameState>(verifier.Code, verifier.Text);
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
                var noVerifier = Rules.ScenarioA1CloseCombatProjection.CcVerifierRefusal();
                return Fail<GameState>(noVerifier.Code, noVerifier.Text);
            }

            if (combat.Rolls.Values.FirstOrDefault(roll => !rolls.ContainsKey(roll)) is { } missing)
            {
                var noRoll = Rules.ScenarioA1CloseCombatProjection.CcRollRefusal(missing);
                return Fail<GameState>(noRoll.Code, noRoll.Text);
            }

            if (combat.Reaction)
            {
                var window = state.Movement;
                if (Rules.ScenarioA1CloseCombatProjection.ReactionRecordRefusal(state.Phase, window is { WindowOpen: true }, window is { Vehicle: true }, window?.Members.Contains(combat.Vehicle, StringComparer.Ordinal) == true) is { } reaction)
                {
                    return Fail<GameState>(reaction.Code, reaction.Text);
                }
            }
            else
            {
                var entry = state.CloseCombats.FirstOrDefault(item => item.Location == combat.Location);
                var attacker = combat.ByVehicle ? Active(state, combat.Vehicle) as UnitInstance : combat.Attackers.Select(id => Active(state, id) as UnitInstance).FirstOrDefault();
                if (Rules.ScenarioA1CloseCombatProjection.SequentialRecordRefusal(state.Phase, entry is { Closed: true }, attacker?.Side, entry?.Next, combat.Attackers.Any(id => entry?.Attacking.Contains(id) == true),
                    state.CloseCombats.Any(item => !item.Closed && item.Location != combat.Location)) is { } sequential)
                {
                    return Fail<GameState>(sequential.Code, sequential.Text);
                }
            }

            if (closeCombatVerifier.VerifyVehicle(state, combat, rolls) is { } reason)
            {
                var verifier = Rules.ScenarioA1CloseCombatProjection.VerifierRefusal(reason);
                return Fail<GameState>(verifier.Code, verifier.Text);
            }

            if (Rules.ScenarioA1CloseCombatProjection.ReactionLeavesState(combat.Reaction))
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
            if (Rules.ScenarioA1CloseCombatProjection.PassRecordRefusal(state.Phase, entry is { Closed: true }, entry?.Next, passed.Side, entry?.Passed.Contains(passed.Side) == true, state.Side(passed.Side) is not null) is { } refused)
            {
                return Fail<GameState>(refused.Code, refused.Text);
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
                var noVerifier = Rules.ScenarioA1CloseCombatProjection.CcVerifierRefusal();
                return Fail<GameState>(noVerifier.Code, noVerifier.Text);
            }

            if (combat.Rolls.Values.FirstOrDefault(roll => !rolls.ContainsKey(roll)) is { } missing)
            {
                var noRoll = Rules.ScenarioA1CloseCombatProjection.CcRollRefusal(missing);
                return Fail<GameState>(noRoll.Code, noRoll.Text);
            }

            var entry = state.CloseCombats.FirstOrDefault(item => item.Location == combat.Location);

            // A4.152 (ruling R27.3): or at once in the MPh, the CC of a berserk Infantry OVR, once the DEFENDER's window on the entry has closed.
            var overrun = Rules.ScenarioA1CloseCombatProjection.OverrunRecord(state.Phase, combat.Facts.ValueKind == JsonValueKind.Object && combat.Facts.TryGetProperty("infantryOverrun", out var flag)
                && flag.ValueKind == JsonValueKind.True, state.Movement is { WindowOpen: false } movement && movement.Location == combat.Location);
            if (Rules.ScenarioA1CloseCombatProjection.CcRecordRefusal(state.Phase, overrun, entry is { Closed: true }, state.CloseCombats.Any(item => !item.Closed && item.Location != combat.Location)) is { } refused)
            {
                return Fail<GameState>(refused.Code, refused.Text);
            }

            // A11.3: the ambusher's attacks are sequential, one record each, until the ambushed side's round closes the Location; A11.33, A11.34
            // (ruling R14.6): the prisoners' escape round comes before any other. A11.41 (referee, pass 14): with every ambusher gone by Ambush Withdrawal,
            // the ambushed side's round closes the Location.
            var allowed = Rules.ScenarioA1CloseCombatProjection.AllowedRounds(entry?.Ambusher, entry?.Ambusher is { } ambusher && state.Units.Any(unit => unit.Status == InstanceStatus.Active && unit.Side == ambusher
                && GameState.Condition(unit, Conditions.Captured) != ConditionState.True && state.Location(unit.Id)?.Location == combat.Location),
                entry?.Rounds.Count(item => item != CloseCombatResolved.PrisonersRound) ?? 0, entry?.Rounds.Count ?? 0);
            var attacking = entry?.Attacking ?? [];
            var attacked = entry?.Attacked ?? [];
            if (Rules.ScenarioA1CloseCombatProjection.RoundRecordRefusal(allowed, combat.Round, combat.Attackers.Any(attacking.Contains), combat.Defenders.Any(attacked.Contains), combat.Location.ToString()) is { } roundRefusal)
            {
                return Fail<GameState>(roundRefusal.Code, roundRefusal.Text);
            }

            if (closeCombatVerifier.Verify(state, combat, rolls) is { } reason)
            {
                var verifier = Rules.ScenarioA1CloseCombatProjection.VerifierRefusal(reason);
                return Fail<GameState>(verifier.Code, verifier.Text);
            }

            var updated = (entry ?? new CloseCombatLocation(combat.Location, false, null, [], false)) with
            {
                Rounds = [.. entry?.Rounds ?? [], combat.Round],
                Closed = Rules.ScenarioA1CloseCombatProjection.Closes(combat.Round),
                Attacking = [.. attacking, .. combat.Attackers],
                Attacked = [.. attacked, .. combat.Defenders],

                // J2.31 (ruling R14.1): the first round declares the Location Hand-to-Hand for the CCPh.
                HandToHand = Rules.ScenarioA1CloseCombatProjection.RecordHandToHand(entry?.HandToHand == true, combat.Round, () => combat.Facts.TryGetProperty("handToHand", out var hand) && hand.ValueKind == JsonValueKind.True),
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
            var panzerfaust = Rules.ScenarioA1OrdnanceProjection.IsPanzerfaust(fired.Gun, fired.Crew);
            if (!Rules.ScenarioA1OrdnanceProjection.FirePhase(state.Phase)
                || !(tank || mortar || panzerfaust || (firer is EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Manned } manning } && manning.Holder == fired.Crew))
                || Active(state, fired.Crew) is not UnitInstance shooter
                || !Rules.ScenarioA1OrdnanceProjection.FiresAgain(shots is not null, shots?.RateOfFireKept ?? false, panzerfaust,
                    () => fired.Facts.TryGetProperty("intensiveFire", out var intensive) && intensive.ValueKind == JsonValueKind.True))
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
            if (Rules.ScenarioA1OrdnanceProjection.AmmunitionDepleted(use) && fired.Facts.TryGetProperty("ammunition", out var ammunition) && ammunition.GetString() is { } depleted)
            {
                state = state with
                {
                    DepletedAmmunition = [.. state.DepletedAmmunition, new DepletedAmmunition(fired.Gun, depleted)]
                };
            }

            // C8.9: unless the Gun malfunctioned, when the shot counts as fired.
            if (Rules.ScenarioA1OrdnanceProjection.NeverFired(use, malfunctioned))
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
                GunCrewsFired = Rules.ScenarioA1OrdnanceProjection.CrewLosesInherentFp(tank, panzerfaust, state.GunCrewsFired.Contains(fired.Crew)) ? [.. state.GunCrewsFired, fired.Crew] : state.GunCrewsFired,
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
            // Rules decides a squad's one SW use (pass 32.c).
            IReadOnlyList<SupportWeaponUse> Use(IReadOnlyList<SupportWeaponUse> uses, UnitInstance unit, string weapon) =>
                [.. Rules.ScenarioA1FireFollowUps.SupportWeaponUse([.. uses.Select(item => (item.Unit, item.Weapon))], unit.Id, unit.Kind == "asl:squad", Marked(unit), weapon)
                    .Select(item => new SupportWeaponUse(item.Unit, item.Weapon))];

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
            if (Rules.ScenarioA1FireFollowUps.PanzerfaustShotCounts(panzerfaust,
                fired.Resolution.TryGetProperty("panzerfaustCheck", out var check) && check.TryGetProperty("outcome", out var outcome) ? outcome.GetString() : null)
                && shooter.Side is { } side)
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
            if (Active(state, surrender.Unit) is not UnitInstance unit || !Rules.ScenarioA1RoutRallyProjection.SurrenderAllowed(GameState.Condition(unit, Conditions.Broken) == ConditionState.True,
                GameState.Condition(unit, Conditions.Captured) == ConditionState.True, surrender.Captors.Count,
                surrender.Captors.Select(id => state.Unit(id) is { Status: InstanceStatus.Active } captor ? (true, captor.Side != unit.Side) : (false, false)),
                () => state.PendingSurrenders.Any(item => item.Unit == unit.Id)))
            {
                var refused = Rules.ScenarioA1RoutRallyProjection.SurrenderRefusal();
                return Fail<GameState>(refused.Code, refused.Text);
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
            if (Active(state, wreck.Id) is not UnitInstance vehicle || !Rules.ScenarioA1VehicleProjection.WreckAllowed(vocabulary.IsA(vehicle.Kind, "asl:vehicle"), () => state.Location(vehicle.Id) is not null))
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
                var refused = Rules.ScenarioA1RoutRallyProjection.RejectSurrenderRefusal(rejected.Unit);
                return Fail<GameState>(refused.Code, refused.Text);
            }

            var next = Eliminate(state with
            {
                PendingSurrenders = [.. state.PendingSurrenders.Where(item => item.Unit != unit.Id)],
            }, unit.Id);
            return next is null ? null : next with
            {
                NoQuarter = Rules.ScenarioA1RoutRallyProjection.NoQuarterAfter(next.NoQuarter, unit.Side),
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
            if (!Rules.ScenarioA1RoutRallyProjection.MassacreNamesActive(units.Length, prisoners.Length, units.All(unit => unit is { Status: InstanceStatus.Active }),
                prisoners.All(unit => unit is { Status: InstanceStatus.Active })))
            {
                var refused = Rules.ScenarioA1RoutRallyProjection.MassacreNamesRefusal();
                return Fail<GameState>(refused.Code, refused.Text);
            }

            var side = units[0]!.Side;
            var at = state.Location(units[0]!.Id)?.Location;
            var firePhase = Rules.ScenarioA1RoutRallyProjection.MassacreFirePhase(state.Phase, state.PhasingSide == side);
            string Nationality(UnitInstance unit) => unit.Definition is { } reference ? catalog?.Definition(reference.Definition)?.Nationality ?? string.Empty : string.Empty;
            if (!firePhase || at is null
                || units.Any(unit => !Rules.ScenarioA1RoutRallyProjection.MassacreUnitAllowed(unit!.Side == side, state.Location(unit.Id)?.Location == at, vocabulary.IsA(unit.Kind, "asl:personnel"),
                    GameState.Condition(unit, Conditions.Melee) == ConditionState.True, GameState.Condition(unit, Conditions.Captured) == ConditionState.True, massacre.Berserk,
                    GameState.Condition(unit, Conditions.Berserk) == ConditionState.True, () => Nationality(unit)))
                || prisoners.Any(unit => !Rules.ScenarioA1RoutRallyProjection.MassacrePrisonerAllowed(unit!.Side == side, GameState.Condition(unit, Conditions.Captured) == ConditionState.True,
                    state.Location(unit.Id)?.Location == at)))
            {
                var refused = Rules.ScenarioA1RoutRallyProjection.MassacreRefusal();
                return Fail<GameState>(refused.Code, refused.Text);
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

            if (Rules.ScenarioA1RoutRallyProjection.MassacreEndsBerserk(massacre.Berserk))
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
            var raise = Rules.ScenarioA1RoutRallyProjection.MassacreRaisesElr(next!.MassacreElrRaised.Contains(victims, StringComparer.Ordinal));
            return next with
            {
                NoQuarter = Rules.ScenarioA1RoutRallyProjection.NoQuarterAfter(next.NoQuarter, victims),
                MassacreElrRaised = raise ? [.. next.MassacreElrRaised, victims] : next.MassacreElrRaised,
                // A20.4: the ELR of every OB group of the massacred side rises too (ruling R18.3).
                Sides = raise ? [.. next.Sides.Select(item => item.Id != victims ? item : item with
                {
                    Elr = Rules.ScenarioA1RoutRallyProjection.MassacreRaisedElr(item.Elr),
                    Groups = [.. item.Groups.Select(group => group.Elr is { } groupElr ? group with { Elr = Rules.ScenarioA1RoutRallyProjection.MassacreRaisedElr(groupElr) } : group)],
                })] : next.Sides,
            };
        }

        /// <summary>
        /// Where a Gun's Acquisition is now (C6.5, C6.51, ruling R5.13), as the planner read it: on Known enemy units in its Location, or on the
        /// Location alone when its target left the Gun's LOS. The DRM is unchanged.
        /// </summary>
        private GameState? ChangeAcquisition(GameState state, AcquisitionChanged change)
        {
            // Rules decides it (pass 32.c).
            var acquisition = state.Acquisitions.FirstOrDefault(item => item.Gun == change.Gun);
            if (Rules.ScenarioA1FireFollowUps.VerifyAcquisitionChange(acquisition is not null, state.Find(change.Gun)?.Side, change.Units.Select(id => state.Unit(id) is { } unit
                ? new Rules.AcquiredUnitRecordFacts(unit.Status == InstanceStatus.Active, unit.Side, state.Location(id)?.Location == change.Location,
                    GameState.Condition(unit, Conditions.Concealed) == ConditionState.True)
                : new Rules.AcquiredUnitRecordFacts(false, null, false, false))) is { } refused)
            {
                return Fail<GameState>(refused.Code, refused.Text);
            }

            return state with
            {
                Acquisitions = [.. state.Acquisitions.Select(item => item.Gun != change.Gun ? item : acquisition! with { Location = change.Location, Units = change.Units })],
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
                // A Gun keeps it while its Good Order crew mans it; a light mortar while its Good Order possessor holds it (C9.2; table player, pass 9); a
                // tank while it is active and not Abandoned (C6.5, D1.3). Rules decides it (pass 32.c) from the holder's facts.
                var holder = next.Find(acquisition.Gun);
                var manning = holder is EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Manned } manned } ? manned.Holder : null;
                var possessing = holder is EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } possession } mortar
                    && vocabulary.IsA(mortar.Kind, "asl:light-mortar") ? possession.Holder : null;
                var crew = manning is not null ? next.Unit(manning) : possessing is not null ? next.Unit(possessing) : null;
                var holds = Rules.ScenarioA1FireFollowUps.AcquisitionHolds(new Rules.AcquisitionHolderFacts(manning is not null && crew is { Status: InstanceStatus.Active },
                    possessing is not null && crew is { Status: InstanceStatus.Active },
                    crew is { Status: InstanceStatus.Active } ? GameState.RuleStateOf(GameState.GoodOrder(crew, vocabulary)) : Rules.RuleState.Unknown,
                    holder is UnitInstance { Status: InstanceStatus.Active } tank && vocabulary.IsA(tank.Kind, "asl:vehicle"),
                    holder is UnitInstance abandoned && GameState.Condition(abandoned, Conditions.Abandoned) == ConditionState.True));
                if (!holds)
                {
                    continue;
                }

                // The units it follows, and the one Location they share (their texts), as Rules decides them.
                var locations = new Dictionary<string, BoardLocation>(StringComparer.Ordinal);
                string? Text(BoardLocation? at)
                {
                    if (at is null)
                    {
                        return null;
                    }

                    var text = at.ToString();
                    locations[text] = at;
                    return text;
                }

                var lineage = payload as LineageRecorded;
                var (units, location) = Rules.ScenarioA1FireFollowUps.AcquisitionAfter(acquisition.Units, lineage?.Consumed, lineage?.Produced.Select(item => item.Id).ToArray(),
                    id => next.Unit(id) is { Status: InstanceStatus.Active } unit && GameState.Condition(unit, Conditions.Captured) != ConditionState.True,
                    id => Text(next.Location(id)?.Location), Text(acquisition.Location)!);
                kept.Add(acquisition with
                {
                    Units = units,
                    Location = locations[location],
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

            // A Defensive fire record answers the open window of the moving stack's latest step (A8.1). A7.55: a Location's units fire at a target
            // once per phase (in the MPh, once per MF expenditure), as one fire group; Residual FP has no firers and never forms one (A8.22). A
            // vehicle's MG fires alone (D3.4, ruling R25.7), but the Mandatory Fire Group binds it with its Location's Infantry (D3.5): only its own
            // Multiple ROF fires again. D7.14 (ruling R11.11): each OVR is its vehicle's own attack, as a vehicle's MG shot is. Rules decides both (pass 32.c).
            var byVehicle = (fire.Facts.TryGetProperty("vehicleFire", out var firing) && firing.ValueKind == JsonValueKind.Object)
                || (fire.Facts.TryGetProperty("overrun", out var overrunning) && overrunning.ValueKind == JsonValueKind.Object);
            if (Rules.ScenarioA1FireFollowUps.VerifyFireRecord(fire.MovementStep, state.Movement is { WindowOpen: true }, state.Movement?.Step, fire.Firers.Count, byVehicle,
                state.FiresThisPhase.Select(item => new Rules.PhaseFireFacts(item.FirerLocation, item.TargetLocation, item.Step, item.Vehicle)), fire.FirerLocation, fire.TargetLocation) is { } refused)
            {
                return Fail<GameState>(refused.Code, refused.Text);
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
                // Rules decides the kept ROF and the count (pass 32.c).
                var effects = fire.Resolution.TryGetProperty("weaponEffects", out var weaponEffects) && weaponEffects.ValueKind == JsonValueKind.Array ? weaponEffects : (JsonElement?)null;
                var kept = Rules.ScenarioA1FireFollowUps.VehicleShotKeepsRof(effects?.GetArrayLength() ?? 0,
                    effects is { } list && list.GetArrayLength() == 1 && list[0].TryGetProperty("rateOfFireRetained", out var retained) ? retained.GetBoolean() : null);
                var previous = shots.FirstOrDefault(item => item.Gun == vehicle);
                shots = [.. shots.Where(item => item.Gun != vehicle), new OrdnanceShotRecord(vehicle, Rules.ScenarioA1FireFollowUps.VehicleShotCount(previous?.Shots), kept)];
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
            // Rules decides it (pass 32.c).
            var recorded = fires.TryGetValue(residual.Fire, out var found) ? found.Fire : null;
            var recordedFp = recorded is not null && recorded.Resolution.TryGetProperty("arithmetic", out var arithmetic) && arithmetic.TryGetProperty("residualFp", out var value)
                ? value.GetInt32() : (int?)null;
            if (Rules.ScenarioA1FireFollowUps.VerifyResidualFp(state.Phase, recorded is not null, recorded?.TargetLocation == residual.Location.ToString(), recordedFp, residual.Fp,
                residual.Fire, state.ResidualFire.FirstOrDefault(item => item.Location == residual.Location)?.Fp) is { } refused)
            {
                return Fail<GameState>(refused.Code, refused.Text);
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

            if (!Rules.ScenarioA1RoutRallyProjection.RallyPhase(state.Phase) || Active(state, rally.Unit) is not UnitInstance unit)
            {
                var refused = Rules.ScenarioA1RoutRallyProjection.RallyRefusal();
                return Fail<GameState>(refused.Code, refused.Text);
            }

            if (Rules.ScenarioA1RoutRallyProjection.VerifyRallyOnce(unit.Id, state.RallyAttemptsThisPlayerTurn.Contains(unit.Id), state.RepairsThisPhase.Contains(unit.Id)) is { } once)
            {
                return Fail<GameState>(once.Code, once.Text);
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

            var mmc = Rules.ScenarioA1RoutRallyProjection.TakesFirstMmcRally(vocabulary.IsA(unit.Kind, "asl:mmc"), unit.Side == state.PhasingSide, state.FirstMmcRallyTaken.Contains(unit.Side));
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
                || !Rules.ScenarioA1OrdnanceProjection.TurnGunAllowed(state.Phase, crew.Side == state.PhasingSide,
                    () => new[] { Conditions.Broken, Conditions.Pinned }.Any(name => GameState.Condition(crew, name) == ConditionState.True),
                    () => new[] { Conditions.Malfunctioned, Conditions.PrepFire, Conditions.FinalFire, Conditions.FirstFire, Conditions.IntensiveFire }.Any(name => GameState.Condition(gun, name) == ConditionState.True),
                    shots is not null, shots?.RateOfFireKept ?? false))
            {
                return Fail<GameState>("UNIT-STATE-039", "A Gun changes its CA without firing in a friendly fire phase while its Good Order, unpinned crew could still fire it (C3.22).");
            }

            return state with
            {
                Equipment = [.. state.Equipment.Select(item => item.Id == gun.Id ? gun with { Position = at with { Facing = turned.Facing } } : item)],
                OrdnanceShots = [.. state.OrdnanceShots.Where(item => item.Gun != gun.Id), new OrdnanceShotRecord(gun.Id, shots?.Shots ?? 0, false)],
                NoMoveThisPlayerTurn = Rules.ScenarioA1OrdnanceProjection.TurnFreezesMovement(state.Phase) ? [.. state.NoMoveThisPlayerTurn, gun.Id, crew.Id] : state.NoMoveThisPlayerTurn,
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

            var (mfSpent, halfMfSpent) = Rules.ScenarioA1OrdnanceProjection.HookMp(vehicle.MfSpent, vehicle.HalfMfSpent, hooked.Mp);
            var next = Replace(state, vehicle with
            {
                MfSpent = mfSpent,
                HalfMfSpent = halfMfSpent
            })!;

            // C10.11, C10.12 (ruling R26.2): the crew may board the towing vehicle as it hooks up, and disembarks beneath it as it unhooks.
            if (hooked.Boards && hooked.Hooked)
            {
                next = Replace(next, crew with
                {
                    Position = new ContainedPosition(vehicle.Id, ContainmentRole.Passenger),
                    MovementEnded = true,
                })!;
            }
            else if (!hooked.Hooked && crew.Position is ContainedPosition { Role: ContainmentRole.Passenger } aboard && aboard.Container == vehicle.Id)
            {
                next = Replace(next, crew with
                {
                    Position = new MapPosition(at.Location),
                })!;
            }
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
            if (!Rules.ScenarioA1RallyRules.ShockRollPhase(state.Phase) || Active(state, shock.Vehicle) is not UnitInstance vehicle
                || !Rules.ScenarioA1RoutRallyProjection.ShockRecoveryAllowed(vocabulary.IsA(vehicle.Kind, "asl:vehicle"), state.ShockRollsThisPhase.Contains(vehicle.Id),
                    GameState.Condition(vehicle, Conditions.Shocked) == ConditionState.True, GameState.Condition(vehicle, Conditions.UnconfirmedKill) == ConditionState.True))
            {
                var refused = Rules.ScenarioA1RoutRallyProjection.ShockRecoveryRefusal();
                return Fail<GameState>(refused.Code, refused.Text);
            }

            if (!rolls.TryGetValue(shock.Roll, out var roll) || roll.Count != 1 || roll.Sides != 6)
            {
                return Fail<GameState>("UNIT-STATE-038", $"The Shock recovery record's roll '{shock.Roll}' is not one recorded die.");
            }

            var unconfirmed = GameState.Condition(vehicle, Conditions.UnconfirmedKill) == ConditionState.True;
            if (!Rules.ScenarioA1RoutRallyProjection.ShockRecoveryAgrees(shock.Result, unconfirmed, roll.Values[0]))
            {
                var disagrees = Rules.ScenarioA1RoutRallyProjection.ShockRecoveryDisagrees();
                return Fail<GameState>(disagrees.Code, disagrees.Text);
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
                if (!Rules.ScenarioA1RoutRallyProjection.VehicleRepairAllowedAsRecorded(state.Phase, GameState.Condition(vehicle, Conditions.Malfunctioned) == ConditionState.True,
                    GameState.Condition(vehicle, Conditions.Disabled) == ConditionState.True, GameState.Condition(vehicle, Conditions.ButtonedUp) == ConditionState.True,
                    GameState.Condition(vehicle, Conditions.Stunned) == ConditionState.True, GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True,
                    GameState.Condition(vehicle, Conditions.Shocked) == ConditionState.True, GameState.Condition(vehicle, Conditions.UnconfirmedKill) == ConditionState.True,
                    state.RepairsThisPhase.Contains(vehicle.Id)))
                {
                    var refused = Rules.ScenarioA1RoutRallyProjection.VehicleRepairRefusal();
                    return Fail<GameState>(refused.Code, refused.Text);
                }

                if (!rolls.TryGetValue(repair.Roll, out var vehicleRoll) || vehicleRoll.Count != 1 || vehicleRoll.Sides != 6)
                {
                    return Fail<GameState>("UNIT-STATE-028", $"The Repair record's roll '{repair.Roll}' is not one recorded die.");
                }

                var vehicleDr = vehicleRoll.Values[0];
                var vehicleResult = RepairResult(Rules.ScenarioA1RallyRules.VehicleMgRepairResult(vehicleDr));
                if (!Rules.ScenarioA1RoutRallyProjection.VehicleRepairRecordAgrees(repair.RepairNumber, repair.Result == vehicleResult))
                {
                    var disagrees = Rules.ScenarioA1RoutRallyProjection.VehicleRepairDisagrees();
                    return Fail<GameState>(disagrees.Code, disagrees.Text);
                }

                return state with
                {
                    RepairsThisPhase = [.. state.RepairsThisPhase, vehicle.Id]
                };
            }

            if (!Rules.ScenarioA1RoutRallyProjection.SwRepairPhase(state.Phase) || Active(state, repair.Unit) is not UnitInstance unit
                || Active(state, repair.Equipment) is not EquipmentInstance { Holding: { Role: HoldingRole.Possessed } holding } equipment
                || !Rules.ScenarioA1RallyRules.SwRepairWeaponAllowed(holding.Holder == unit.Id, GameState.Condition(equipment, Conditions.Malfunctioned) == ConditionState.True))
            {
                var refused = Rules.ScenarioA1RoutRallyProjection.SwRepairRefusal();
                return Fail<GameState>(refused.Code, refused.Text);
            }

            // A3.1: a unit takes one kind of action in the RPh, so one that attempted to rally does not also repair.
            if (Rules.ScenarioA1RoutRallyProjection.VerifySwRepairOnce(unit.Id, state.RallyAttemptsThisPlayerTurn.Contains(unit.Id)) is { } once)
            {
                return Fail<GameState>(once.Code, once.Text);
            }

            if (!rolls.TryGetValue(repair.Roll, out var roll) || roll.Count != 1 || roll.Sides != 6)
            {
                return Fail<GameState>("UNIT-STATE-028", $"The Repair record's roll '{repair.Roll}' is not one recorded die.");
            }

            var printed = equipment.Definition is { } reference && catalog?.Definition(reference.Definition) is { } definition
                ? definition.Printed("malfunctioned", "asl:repair")?.Value?.Number
                : null;
            var dr = roll.Values[0];
            var expected = RepairResult(Rules.ScenarioA1RallyRules.SwRepairResult(dr, repair.RepairNumber));
            if (!Rules.ScenarioA1RoutRallyProjection.SwRepairRecordAgrees(printed, repair.RepairNumber, repair.Result == expected))
            {
                var disagrees = Rules.ScenarioA1RoutRallyProjection.SwRepairDisagrees();
                return Fail<GameState>(disagrees.Code, disagrees.Text);
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

            // Pass 32.b: the record's fields and the state are read here, and Rules decides (ScenarioA1MovementCalculator.VerifyStepStart and
            // VerifyStepRecord); the SMOKE attempt's check between them is its own calculator's (pass 32.a), made where it was made.
            var current = state.Movement;
            var facts = new Rules.StepRecordFacts(state.Phase, state.PhasingSide,
                [.. moving.Movers.Select((id, index) => movers[index] is { } unit
                    ? new Rules.StepMoverFacts(unit.Id, true, unit.Side, state.Location(unit.Id)?.Location.ToString(), unit.MovementEnded,
                        GameState.Condition(unit, Conditions.PrepFire) == ConditionState.True, GameState.Condition(unit, Conditions.Melee) == ConditionState.True,
                        GameState.Condition(unit, Conditions.Captured) == ConditionState.True, vocabulary.IsA(unit.Kind, "asl:personnel"), state.NoDoubleTime.Contains(unit.Id),
                        GameState.Condition(unit, Conditions.Broken) == ConditionState.True, GameState.Condition(unit, Conditions.Wounded) == ConditionState.True,
                        GameState.Condition(unit, Conditions.Berserk) == ConditionState.True, GameState.Condition(unit, Conditions.Cx) == ConditionState.True,
                        unit.Position is OffMapPosition && state.Location(unit.Id) is null, unit.MfSpent, unit.HalfMfSpent)
                    : new Rules.StepMoverFacts(id, false, null, null, false, false, false, false, false, false, false, false, false, false, false, 0, false))],
                moving.HalfMf, moving.Assault, moving.Step, moving.DoubleTime, moving.MinimumMove, moving.Attempted?.ToString(), moving.To.ToString(), moving.Bypass?.Count,
                moving.Exit is not null, current is not null, current?.Members ?? [], current is { WindowOpen: true }, current?.Assault == true, current?.Step ?? 0);
            if (Rules.ScenarioA1MovementCalculator.VerifyStepStart(facts) is { } refused)
            {
                return Fail<GameState>(refused.Code, refused.Text);
            }

            // A24.1 (ruling R9.5): a SMOKE attempt is made once per MPh by a moving squad, in its Location, with its recorded dr.
            // Table player, pass 9: the exponent is the catalog's, CX (or Double Time with this step) adds one, and the cost is 1 MF in the own
            // Location and 2 in another.
            // Pass 32.a (the worked action): the projector reads the record's facts and Rules decides (ScenarioA1SmokeCalculator.Verify).
            if (moving.Smoke is { } smoke && !Rules.ScenarioA1SmokeCalculator.Verify(SmokeFacts(state, moving, smoke)))
            {
                return Fail<GameState>("UNIT-STATE-029", "A SMOKE placement is one attempt per MPh by a squad of the moving stack, in its Location, with its dr (A24.1).");
            }

            var verdict = Rules.ScenarioA1MovementCalculator.VerifyStepRecord(facts);
            if (verdict.Refusal is { } refusal)
            {
                return Fail<GameState>(refusal.Code, refusal.Text);
            }

            // A12.15, A2.51 (ruling R25.3): a stack entering from off board that is forced back stays off board, its MF spent and its move over; no fire
            // reaches it there, so no window opens.
            if (verdict.OffBoardForcedBack)
            {
                var waiting = state;
                foreach (var unit in movers)
                {
                    var spent = (unit!.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0) + moving.HalfMf;
                    waiting = Replace(waiting, unit with
                    {
                        MfSpent = spent / 2,
                        HalfMfSpent = spent % 2 == 1,
                        MovementEnded = true,
                    })!;
                }

                return waiting;
            }

            // A2.6 (ruling R21.5): an exit leaves the map from the stack's Location; the units are Exited, not eliminated, and the move ends.
            if (moving.Exit is { } edge)
            {
                // C10.3 (ruling R26.4): a pushed Gun leaves with its crew.
                var gone = ExitUnits(state, [.. movers.Select(unit => unit!)], moving.To, edge);
                var pushedGun = moving.PushedGun is { } gunPushed ? gone.Find(gunPushed) as EquipmentInstance : null;
                var mannedPushed = pushedGun is { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Manned } };
                if (Rules.ScenarioA1MovementCalculator.PushedGunExits(mannedPushed, mannedPushed && movers.Any(unit => unit!.Id == pushedGun!.Holding!.Holder)))
                {
                    gone = gone with
                    {
                        Equipment = [.. gone.Equipment.Select(item => item.Id == pushedGun!.Id ? item with { Status = InstanceStatus.Exited, Position = OffMapPosition.Instance } : item)],
                        Exits = [.. gone.Exits, new UnitExit(pushedGun!.Id, moving.To, edge, state.Turn, false)],
                    };
                }

                return gone with
                {
                    Movement = null
                };
            }

            // A4.5, B3.4, A4.12 (ruling R10.8): each mover's MF, CX, Double Time MF, Road Bonus, and leaders moved with are decided by Rules.
            string[] leaders = [.. movers.Where(unit => vocabulary.IsA(unit!.Kind, "asl:leader")).Select(unit => unit!.Id)];
            var next = state;
            foreach (var unit in movers)
            {
                var update = Rules.ScenarioA1MovementCalculator.StepMover(unit!.MfSpent, unit.HalfMfSpent, unit.DoubleTimeMf, unit.OffRoad, unit.MovedWith, unit.Id, moving.HalfMf, moving.DoubleTime, moving.Road, leaders);
                var conditions = update.SetCx
                    ? new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { [Conditions.Cx] = ConditionState.True }
                    : unit.Conditions;
                next = Replace(next, unit with
                {
                    Position = new MapPosition(moving.To),
                    MfSpent = update.MfSpent,
                    HalfMfSpent = update.HalfMfSpent,
                    Conditions = conditions,
                    DoubleTimeMf = update.DoubleTimeMf,
                    OffRoad = update.OffRoad,
                    MovedWith = [.. update.MovedWith],
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

            // A4.134, A24.1, A4.41 (rulings R9.5, R10.9): the members whose move ends and the light mortars marked moved are decided by Rules.
            var locationChanged = movers[0]!.Position is MapPosition before && before.Location != moving.To;
            return next with
            {
                Movement = new MovementState(moving.Movers, moving.To, moving.HalfMf, moving.Step, moving.Assault, WindowOpen: true)
                {
                    Members = current?.Members ?? moving.Movers,
                    Charge = moving.Charge,
                    EndingMembers = Rules.ScenarioA1MovementCalculator.EndingMembers(moving.MinimumMove, moving.Attempted is not null, moving.Movers, moving.Smoke?.Unit, moving.Smoke?.Dr),
                    MinimumMove = moving.MinimumMove,
                    Bypass = moving.Bypass,
                    From = movers[0]!.Position is MapPosition left && left.Location != moving.To ? left.Location : null,
                    PushedGun = moving.PushedGun,
                },
                SmokeAttempts = moving.Smoke is { } attempt ? [.. next.SmokeAttempts, attempt.Unit] : next.SmokeAttempts,
                SmokePending = moving.Smoke is { Placed: true } placing ? placing.Target : null,
                MovedWeapons = [.. next.MovedWeapons, .. Rules.ScenarioA1MovementCalculator.MovedLightMortars(
                    [.. next.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && moving.Movers.Contains(holding.Holder, StringComparer.Ordinal))
                        .Select(item => new Rules.CarriedWeaponFacts(item.Id, vocabulary.IsA(item.Kind, "asl:light-mortar"), next.MovedWeapons.Contains(item.Id, StringComparer.Ordinal)))],
                    locationChanged)],
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
            if (!Rules.ScenarioA1VehicleProjection.StepPhase(state.Phase) || Active(state, step.Vehicle) is not UnitInstance vehicle || !Rules.ScenarioA1VehicleProjection.StepAllowed(vocabulary.IsA(vehicle.Kind, "asl:vehicle"),
                vehicle.Side == state.PhasingSide, vehicle.MovementEnded, step.HalfMp, step.Kind == VehicleStepped.Load))
            {
                return Fail<GameState>("UNIT-STATE-034", "A vehicle step moves a vehicle of the phasing side that has not ended its move, in the MPh (D2.1).");
            }

            // A2.52, D2.4 (ruling R26.1): a vehicle waiting off board is in Motion and enters across its edge as its first MP expenditure, with its VCA.
            var entering = vehicle.Position is OffMapPosition && step.Kind == VehicleStepped.Enter && step.Entry is not null && step.Facing is not null && state.Movement is null;
            MapPosition at;
            Documents.UnitFacing facing;
            if (entering)
            {
                at = new MapPosition(step.At) { Facing = step.Facing };
                facing = step.Facing!.Value;
            }
            else if (state.Location(vehicle.Id) is { } here && vehicle.Position is MapPosition { Facing: { } faced })
            {
                at = here;
                facing = faced;
            }
            else
            {
                return Fail<GameState>("UNIT-STATE-034", "A vehicle step moves a vehicle on the map, or one entering from off board across its edge with its VCA (D2.1, A2.52).");
            }

            // D5.341: a Recall stops the AFV like a Stun for the rest of that Player Turn; once its counter shows Recall; +1 it must move.
            var recalling = Rules.ScenarioA1VehicleProjection.Recalling(GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True, GameState.Condition(vehicle, Conditions.StunRecovery) == ConditionState.True);
            // D8.3 (ruling R11.10): a bogged vehicle's only expenditure is its Bog Removal Start MP.
            var bogged = GameState.Condition(vehicle, Conditions.Bogged) == ConditionState.True;
            // D6.5, D6.1 (ruling R26.2): Passengers leave a vehicle that Prep Fired, is immobilized, or is Abandoned.
            if (new[] { Conditions.PrepFire, Conditions.Immobilized, Conditions.Stunned, Conditions.Shocked, Conditions.UnconfirmedKill, Conditions.Abandoned }
                .Where(name => step.Kind != VehicleStepped.Unload || name is not (Conditions.PrepFire or Conditions.Immobilized or Conditions.Abandoned))
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

            var inMotion = entering || GameState.Condition(vehicle, Conditions.Motion) == ConditionState.True;
            var started = current is not null ? current.Started : inMotion;
            var moving = started && current?.Stopped != true;
            var stopped = current is not null ? current.Stopped || !current.Started : !inMotion;

            // D2.23 (ruling R11.1): Reverse is declared with the Start MP, and every entry until the vehicle stops keeps that direction.
            var reverse = current?.Reverse == true && moving;
            var valid = current?.Ending != true && step.Kind switch
            {
                VehicleStepped.Start => !moving && step.At == at.Location && step.Facing is null && (step.BogRemoval ? bogged : step.HalfMp == 2),
                VehicleStepped.Turn => moving && step.At == at.Location && step.Facing is { } turned && Rules.ScenarioA1VehicleProjection.OneHexspine((int)turned, (int)facing)
                    && step.HalfMp is 2 or 4,
                VehicleStepped.Enter => entering ? !step.Reverse && step.Straddling is null
                    : moving && (step.At != at.Location || (step.Straddling is { } lane && lane != vehicle.Straddling)) && step.Facing is null && step.Reverse == reverse,

                // D6.4, D6.5 (ruling R26.2): Infantry board a vehicle that has spent no MP this MPh and is not in Motion; Passengers disembark from a Stopped one.
                VehicleStepped.Load => current is null && !inMotion && step.At == at.Location && step.Facing is null && step.Units is { Count: > 0 } boarding
                    && boarding.All(id => Active(state, id) is UnitInstance unit && unit.Side == vehicle.Side && !unit.MovementEnded && state.Aboard(unit.Id) is null
                        && state.Location(unit.Id)?.Location == at.Location && vocabulary.IsA(unit.Kind, "asl:personnel")),
                VehicleStepped.Unload => stopped && vehicle.Straddling is null && step.At == at.Location && step.Facing is null && step.Units is { Count: > 0 } leaving
                    && leaving.All(id => Active(state, id) is UnitInstance { Position: ContainedPosition { Role: ContainmentRole.Passenger } contained } && contained.Container == vehicle.Id),
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
                // A2.6, D5.341: the vehicle leaves the playing area, not to return; it is recorded as exited, not eliminated. Ruling R26.4: its exit, its
                // Passengers', and its towed Gun's are recorded with the edge, so the Exit VP and CVP read them.
                var stilled = Replace(state, vehicle with
                {
                    MovementEnded = true,
                    Conditions = new Dictionary<string, ConditionState>(vehicle.Conditions, StringComparer.Ordinal) { [Conditions.Motion] = ConditionState.False },
                })!;
                var edge = step.Edge ?? string.Empty;
                var gone = ExitUnits(stilled, [stilled.Unit(vehicle.Id)!, .. stilled.Passengers(vehicle.Id)], at.Location, edge);
                if (recalling || GameState.Condition(vehicle, Conditions.Recalled) == ConditionState.True)
                {
                    gone = gone with
                    {
                        Exits = [.. gone.Exits.Select(exit => exit.Unit == vehicle.Id && exit.Turn == state.Turn ? exit with { Recalled = true } : exit)],
                    };
                }
                var towedGuns = gone.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id)
                    .Select(item => item.Id).ToArray();
                gone = gone with
                {
                    Equipment = [.. gone.Equipment.Select(item => towedGuns.Contains(item.Id) ? item with { Status = InstanceStatus.Exited, Position = OffMapPosition.Instance } : item)],
                    Exits = [.. gone.Exits, .. towedGuns.Select(gun => new UnitExit(gun, at.Location, edge, state.Turn, false))],
                };
                return Settle(DropHeldBy(gone, [vehicle.Id]), new MovementState([vehicle.Id], at.Location, 0, step.Step, false, WindowOpen: false) { Vehicle = true, Members = [] });
            }

            if (step.Kind is VehicleStepped.Load or VehicleStepped.Unload)
            {
                var (loadMf, loadHalfMf) = Rules.ScenarioA1VehicleProjection.Spend(vehicle.MfSpent, vehicle.HalfMfSpent, step.HalfMp);
                var loaded = Replace(state, vehicle with
                {
                    MfSpent = loadMf,
                    HalfMfSpent = loadHalfMf,
                })!;
                foreach (var id in step.Units!)
                {
                    var unit = loaded.Unit(id)!;
                    loaded = Replace(loaded, step.Kind == VehicleStepped.Load
                        ? unit with
                        {
                            Position = new ContainedPosition(vehicle.Id, ContainmentRole.Passenger),
                            MfSpent = unit.MfSpent + 1,
                            MovementEnded = true,
                        }
                        : unit with
                        {
                            Position = new MapPosition(at.Location),
                            MfSpent = unit.MfSpent + step.UnitMf,
                            MovementEnded = GameState.Condition(vehicle, Conditions.PrepFire) == ConditionState.True,
                        })!;
                }

                return loaded with
                {
                    Movement = new MovementState([vehicle.Id], at.Location, (current?.HalfMfInLocation ?? 0) + step.HalfMp, step.Step, false, WindowOpen: true)
                    {
                        Vehicle = true,
                        Started = current?.Started ?? false,
                        Stopped = current?.Stopped ?? false,
                        Reverse = current?.Reverse == true,
                        EnteredFrom = current?.EnteredFrom,
                    },
                };
            }

            var (stepMf, stepHalfMf) = Rules.ScenarioA1VehicleProjection.Spend(vehicle.MfSpent, vehicle.HalfMfSpent, step.HalfMp);
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
                MfSpent = stepMf,
                HalfMfSpent = stepHalfMf,
                Conditions = conditions,
                Straddling = step.Kind == VehicleStepped.Enter ? step.Straddling : vehicle.Straddling,
            })!;

            // C10.1 (ruling R8.6): a Gun in tow goes with its vehicle, onto the map with it when it enters from off board (ruling R26.1).
            next = next with
            {
                Equipment = [.. next.Equipment.Select(item => item.Holding is { Role: HoldingRole.Towed } tow && tow.Holder == vehicle.Id
                    ? item.Position switch
                    {
                        MapPosition towedAt => item with { Position = towedAt with { Location = step.At } },
                        _ => item with { Position = new MapPosition(step.At) },
                    }
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
                    EnteredFrom = step.Kind == VehicleStepped.Enter && !entering && (step.At.Board != at.Location.Board || step.At.Hex != at.Location.Hex) ? at.Location : current?.EnteredFrom,
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
            var final = Rules.ScenarioA1VehicleProjection.CheckFinal(check.Check == VehicleCheckRolled.BogRemoval, roll.Values[0], roll.Values[1], check.Drm);
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
            var stuck = Rules.ScenarioA1VehicleProjection.CheckStops(check.Result is VehicleCheckRolled.Bogged or VehicleCheckRolled.Immobilized, check.Check == VehicleCheckRolled.BogRemoval,
                check.Result == VehicleCheckRolled.Freed);
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
            if (state.Movement is not { } movement || !Rules.ScenarioA1VehicleProjection.OverrunResolvable(movement is { Vehicle: true, WindowOpen: false }, movement.Overrun == overrun.At,
                () => movement.Movers.Contains(overrun.Vehicle, StringComparer.Ordinal), () => fires.ContainsKey(overrun.Fire)))
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
            // Rules decides it (pass 32.c).
            var units = declared.Units.Select(id => Active(state, id)).ToArray();
            if (Rules.ScenarioA1FireFollowUps.VerifyOpportunityFire(state.Phase, units.Length, declared.Units.Distinct(StringComparer.Ordinal).Count() == units.Length,
                units.Select(unit => unit is UnitInstance infantry
                    ? new Rules.OpportunityRecordUnitFacts(true, infantry.Side == state.PhasingSide, vocabulary.IsA(infantry.Kind, "asl:personnel"),
                        GameState.Condition(infantry, Conditions.Broken) == ConditionState.True, GameState.Condition(infantry, Conditions.Berserk) == ConditionState.True,
                        GameState.Condition(infantry, Conditions.Melee) == ConditionState.True, GameState.Condition(infantry, Conditions.Captured) == ConditionState.True,
                        GameState.Condition(infantry, Conditions.PrepFire) == ConditionState.True, GameState.Condition(infantry, Conditions.BoundingFire) == ConditionState.True)
                    : new Rules.OpportunityRecordUnitFacts(false, false, false, false, false, false, false, false, false))) is { } refused)
            {
                return Fail<GameState>(refused.Code, refused.Text);
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
            if (!Rules.ScenarioA1RoutRallyProjection.RoutPhase(state.Phase) || Active(state, routed.Unit) is not UnitInstance unit
                || !Rules.ScenarioA1RoutRallyProjection.RoutStepAllowed(GameState.Condition(unit, Conditions.Broken) == ConditionState.True, GameState.Condition(unit, Conditions.Melee) == ConditionState.True,
                    GameState.Condition(unit, Conditions.Pinned) == ConditionState.True, routed.HalfMf, routed.LowCrawl, unit.MfSpent, unit.HalfMfSpent, RoutHalfMf(unit),
                    state.RoutedThisPhase.Contains(unit.Id, StringComparer.Ordinal)))
            {
                var refused = Rules.ScenarioA1RoutRallyProjection.RoutStepRefusal();
                return Fail<GameState>(refused.Code, refused.Text);
            }

            var (mfSpent, halfMfSpent) = Rules.ScenarioA1RoutRallyProjection.RoutMfSpent(unit.MfSpent, unit.HalfMfSpent, routed.HalfMf);
            return Replace(state, unit with
            {
                Position = new MapPosition(routed.To),
                MfSpent = mfSpent,
                HalfMfSpent = halfMfSpent,
            }) is { } routedTo ? MovePrisoners(routedTo, unit.Id, new MapPosition(routed.To)) is var moved ? moved with
            {
                RoutedThisPhase = moved.RoutedThisPhase.Contains(unit.Id, StringComparer.Ordinal) ? moved.RoutedThisPhase : [.. moved.RoutedThisPhase, unit.Id],
            } : null : null;
        }

        /// <summary>A10.5: a broken unit has six MF in the RtPh, a wounded SMC three.</summary>
        private static int RoutHalfMf(UnitInstance unit) =>
            Rules.ScenarioA1RoutRallyProjection.RoutHalfMfAsRecorded(unit.Kind is "asl:leader" or "asl:hero", GameState.Condition(unit, Conditions.Wounded) == ConditionState.True);

        /// <summary>A Repair outcome of Rules as the record names it.</summary>
        private static string RepairResult(Rules.RepairOutcome outcome) => outcome switch
        {
            Rules.RepairOutcome.Eliminated => RepairAttempted.Eliminated,
            Rules.RepairOutcome.Repaired => RepairAttempted.Repaired,
            _ => RepairAttempted.NoChange,
        };

        /// <summary>An Interdiction NMC (A10.53; ruling R13.3): in the RtPh, on a routing unit, its DR agreeing with its result.</summary>
        private GameState? Interdict(GameState state, RoutInterdicted interdicted)
        {
            if (!Rules.ScenarioA1RoutRallyProjection.RoutPhase(state.Phase) || Active(state, interdicted.Unit) is not UnitInstance
                || !Rules.ScenarioA1RoutRallyProjection.InterdictionByRoutingUnit(state.RoutedThisPhase.Contains(interdicted.Unit, StringComparer.Ordinal))
                || !rolls.TryGetValue(interdicted.Roll, out var roll) || roll.Count != 2 || roll.Sides != 6
                || !Rules.ScenarioA1RoutRallyProjection.InterdictionAgrees(roll.Values[0], roll.Values[1], interdicted.Drm, interdicted.Morale, interdicted.Result))
            {
                var refused = Rules.ScenarioA1RoutRallyProjection.InterdictionRefusal();
                return Fail<GameState>(refused.Code, refused.Text);
            }

            return state;
        }

        /// <summary>A Deployment attempt (A1.31; ruling R13.4): in the RPh, its NTC agreeing with its result; the squad and leader spend their RPh action.</summary>
        private GameState? AttemptDeployment(GameState state, DeploymentAttempted deployment)
        {
            if (state.Phase != "rph" || Active(state, deployment.Squad) is not UnitInstance squad || GameState.Condition(squad, Conditions.Broken) == ConditionState.True
                || !rolls.TryGetValue(deployment.Roll, out var roll) || roll.Count != 2 || roll.Sides != 6
                || Rules.ScenarioA1OrdnanceProjection.DeploymentPassed(roll.Values[0], roll.Values[1], deployment.Drm, deployment.Morale) != deployment.Passed
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
            if (!Rules.ScenarioA1OrdnanceProjection.RallyPhaseActionPhase(state.Phase) || action.Units.Count == 0 || action.Units.Any(id => state.Unit(id) is null)
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
                || !rolls.TryGetValue(recovery.Roll, out var roll) || roll.Count != 1 || roll.Sides != 6 || Rules.ScenarioA1OrdnanceProjection.Recovered(roll.Values[0], recovery.Drm) != recovery.Recovered
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
            // Rules decides it (pass 32.c).
            var recorded = fires.TryGetValue(encirclement.Fire, out var found) ? found.Fire : null;
            var (refused, add) = Rules.ScenarioA1FireFollowUps.VerifyEncirclement(state.Phase, recorded is not null, recorded?.TargetLocation == encirclement.Location.ToString(),
                state.At(encirclement.Location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side == encirclement.Side),
                state.Encirclements.Any(item => item.Location == encirclement.Location && item.Side == encirclement.Side));
            if (refused is not null)
            {
                return Fail<GameState>(refused.Code, refused.Text);
            }

            return !add ? state : state with
            {
                Encirclements = [.. state.Encirclements, new Encirclement(encirclement.Location, encirclement.Side)],
            };
        }

        /// <summary>A7.7 (ruling R12.11): an Encirclement ends when no unit it Encircles is left in its Location.</summary>
        private static GameState KeepEncirclements(GameState next) =>
            next.Encirclements.Count == 0 ? next : next with
            {
                // Rules decides it (pass 32.c).
                Encirclements = [.. next.Encirclements.Where(item => Rules.ScenarioA1FireFollowUps.EncirclementStands(next.At(item.Location).OfType<UnitInstance>()
                    .Select(unit => new Rules.EncircledLocationUnitFacts(unit.Status == InstanceStatus.Active, unit.Kind == UnitKinds.Dummy,
                        GameState.Condition(unit, Conditions.Captured) == ConditionState.True, next.Encircled(unit)))))],
            };

        /// <summary>A Fire Lane (A9.22; ruling R12.7): placed in the MPh by its MG's fire record, with at least one Location.</summary>
        private GameState? PlaceFireLane(GameState state, FireLanePlaced lane)
        {
            // Rules decides it (pass 32.c).
            if (Rules.ScenarioA1FireFollowUps.VerifyFireLane(state.Phase, fires.ContainsKey(lane.Fire), state.Find(lane.Weapon) is EquipmentInstance { Status: InstanceStatus.Active },
                Active(state, lane.Operator) is UnitInstance, lane.Entries.Select(entry => entry.Fp), state.FireLanes.Any(item => item.Weapon == lane.Weapon)) is { } refused)
            {
                return Fail<GameState>(refused.Code, refused.Text);
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
                // Rules decides it (pass 32.c).
                FireLanes = [.. next.FireLanes.Where(lane =>
                {
                    var mg = next.Find(lane.Weapon) as EquipmentInstance;
                    var manning = next.Unit(lane.Operator);
                    return Rules.ScenarioA1FireFollowUps.FireLaneStands(mg is { Status: InstanceStatus.Active }, mg is not null && GameState.Condition(mg, Conditions.Malfunctioned) == ConditionState.True,
                        manning is { Status: InstanceStatus.Active }, manning is not null && GameState.Condition(manning, Conditions.Broken) == ConditionState.True,
                        manning is not null && GameState.Condition(manning, Conditions.Pinned) == ConditionState.True);
                })],
            };

        /// <summary>A PAATC (A11.6, A12.41): its DR agrees with its result; the units that pass need no other PAATC against that vehicle this phase.</summary>
        private GameState? TakePaatc(GameState state, PaatcTaken paatc)
        {
            if (paatc.Units.Count == 0 || paatc.Units.Any(id => Active(state, id) is not UnitInstance) || Active(state, paatc.Vehicle) is not UnitInstance)
            {
                return Fail<GameState>("UNIT-STATE-039", "A PAATC names active units and an active vehicle (A11.6).");
            }

            if (!rolls.TryGetValue(paatc.Roll, out var roll) || roll.Count != 2 || roll.Sides != 6
                || Rules.ScenarioA1VehicleProjection.PaatcPassed(roll.Values[0], roll.Values[1], paatc.Drm, paatc.Morale) != paatc.Passed)
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
            // Pass 32.b: the moving stack is read here, and Rules decides whose move ends and who is pinned.
            var movement = state.Movement;
            var next = movement is null ? state : state with
            {
                Movement = movement with
                {
                    WindowOpen = false
                }
            };
            var verdict = Rules.ScenarioA1MovementCalculator.VerifyCloseWindow(
                new Rules.CloseWindowFacts(movement is { WindowOpen: true }, movement?.Step ?? 0, closed.Step, movement?.EndingMembers ?? [], movement?.Overrun is not null, movement?.Ending == true,
                    movement?.Members ?? [], movement?.Movers ?? [], movement?.MinimumMove == true, movement?.Vehicle == true),
                id => Active(next, id) is UnitInstance { MovementEnded: false });
            if (verdict.Refusal is { } refusal)
            {
                return Fail<GameState>(refusal.Code, refusal.Text);
            }

            var ended = verdict.EndMove is { } ending ? EndMovement(next, new MovementEnded([.. ending])) : next;
            if (ended is not null && verdict.PinMinimumMovers)
            {
                foreach (var id in movement!.Movers)
                {
                    if (Active(ended, id) is UnitInstance { } mover && Rules.ScenarioA1MovementCalculator.MinimumMoveSurvivorPinned(true, GameState.Condition(mover, Conditions.Broken) == ConditionState.True))
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
            var movement = state.Movement;
            if (Rules.ScenarioA1MovementCalculator.VerifyEndMovement(movement is not null, movement?.WindowOpen == true, ended.Movers, movement?.Members ?? [], movement?.Movers ?? []) is { } refusal)
            {
                return Fail<GameState>(refusal.Code, refusal.Text);
            }

            // Pass 25 (table player, pass 15): a mover eliminated or Replaced by the DEFENDER's fire is ended with the rest, not refused.
            var next = state;
            foreach (var id in ended.Movers)
            {
                if (next.Unit(id) is { Status: InstanceStatus.Active } unit)
                {
                    // D2.4: a vehicle that ends its move without stopping is in Motion; one that stopped, or was stopped by a Stun or
                    // immobilization (D5.34), is not.
                    var conditions = movement!.Vehicle
                        ? new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal)
                        {
                            [Conditions.Motion] = Rules.ScenarioA1MovementCalculator.InMotionAfterMove(movement.Stopped, movement.Members.Contains(id, StringComparer.Ordinal)) ? ConditionState.True : ConditionState.False,
                        }
                        : unit.Conditions;
                    next = Replace(next, unit with
                    {
                        MovementEnded = true,
                        Conditions = conditions,
                    })!;
                }
            }

            return Settle(next, movement! with
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
            !Rules.ScenarioA1MovementCalculator.MoveIsOver(movement.Members.Count)
                ? state with
                {
                    Movement = movement
                }
                : state with
                {
                    Movement = null,
                    FiresThisPhase = [.. state.FiresThisPhase.Where(record => Rules.ScenarioA1MovementCalculator.FireRecordStays(record.Step is not null))],
                };

        /// <summary>
        /// A4.2, A7.8: a member of the moving stack that is broken, pinned, or no longer active leaves the stack and ends its
        /// MPh, and the others may move on; a member Reduced to a HS is followed by the HS (A7.302). The member stays a target
        /// in the open window (A8.14), and the stack's move ends only when the ATTACKER ends it, even with no member left.
        /// </summary>
        private static GameState KeepMovingStack(GameState next, EventPayload payload)
        {
            if (next.Movement is not { } movement)
            {
                return next;
            }

            // Pass 32.b: the stack, the lineage, and each member's conditions are read here, and Rules decides who leaves the stack.
            var lineage = payload as LineageRecorded;
            var verdict = Rules.ScenarioA1MovementCalculator.KeepMovingStack(next.Phase, movement.Members, movement.Movers, movement.Vehicle,
                lineage?.Consumed, lineage is null ? null : [.. lineage.Produced.Select(item => item.Id)], payload is VehicleCheckRolled,
                id => next.Unit(id) is { } unit
                    ? new Rules.StackMemberFacts(unit.Id, unit.Status == InstanceStatus.Active,
                        GameState.Condition(unit, Conditions.Stunned) == ConditionState.True, GameState.Condition(unit, Conditions.Shocked) == ConditionState.True,
                        GameState.Condition(unit, Conditions.UnconfirmedKill) == ConditionState.True, GameState.Condition(unit, Conditions.Immobilized) == ConditionState.True,
                        GameState.Condition(unit, Conditions.Abandoned) == ConditionState.True, GameState.Condition(unit, Conditions.Bogged) == ConditionState.True,
                        GameState.Condition(unit, Conditions.Recalled) == ConditionState.True, GameState.Condition(unit, Conditions.StunRecovery) == ConditionState.True,
                        GameState.Condition(unit, Conditions.Broken) == ConditionState.True, GameState.Condition(unit, Conditions.Pinned) == ConditionState.True)
                    : null);
            if (verdict.Unchanged)
            {
                return next;
            }

            foreach (var id in verdict.Leaving)
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
                    Members = [.. verdict.Members],
                    Movers = [.. verdict.Movers],
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

            // Pass 32.b: the item and the open attempts are read here, and Rules decides (A4.1, A20.53).
            if (Rules.ScenarioA1MovementCalculator.VerifyMove(item.Id, item is UnitInstance, item is UnitInstance { MovementEnded: true }, state.OpenAttempts.Any(open => open.Unit == item.Id), move.Mf) is { } refusal)
            {
                return Fail<GameState>(refusal.Code, refusal.Text);
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

        /// <summary>
        /// A12.153 (pass 24, ruling R24.2): Mopping Up declared in the PFPh by active units of the phasing side, once per building per Player Turn; the
        /// units become TI, and a secured building is recorded for the Control of its Locations (A26.11).
        /// </summary>
        private GameState? MopUp(GameState state, BuildingMoppedUp mopped)
        {
            var units = mopped.Units.Select(state.Unit).ToArray();
            if (state.Phase != "pfph" || mopped.Side != state.PhasingSide || units.Length == 0
                || units.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side != mopped.Side)
                || state.MoppedUpThisPlayerTurn.Contains(mopped.Building, StringComparer.Ordinal))
            {
                return Fail<GameState>("UNIT-STATE-046", "Mopping Up is declared in the PFPh by active units of the phasing side, once per building per Player Turn (A12.153).");
            }

            var next = state;
            foreach (var unit in units)
            {
                next = Replace(next, unit! with
                {
                    Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal) { ["asl:ti"] = ConditionState.True },
                });
            }

            return next with
            {
                MoppedUpThisPlayerTurn = [.. state.MoppedUpThisPlayerTurn, mopped.Building],
                Secured = mopped.Secured is { } secured ? [.. state.Secured, new SecuredBuilding(mopped.Building, mopped.Side, secured)] : state.Secured,
            };
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
            if (Rules.ScenarioA1CloseCombatProjection.ReducedAttackerHasAttacked(next.CloseCombats.Any(item => item.Attacking.Any(ids.Contains))))
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
                var notActive = Rules.ScenarioA1CloseCombatProjection.CaptureNotActiveRefusal(capture.Id);
                return Fail<GameState>(notActive.Code, notActive.Text);
            }

            if (Rules.ScenarioA1CloseCombatProjection.CaptureBerserkRefusal(GameState.Condition(prisoner, Conditions.Berserk) == ConditionState.True) is { } berserk)
            {
                return Fail<GameState>(berserk.Code, berserk.Text);
            }

            // A15.5, A20.21: a pending surrender is taken by one of its captors.
            if (Rules.ScenarioA1CloseCombatProjection.CaptorRefusal(prisoner.Id, state.PendingSurrenders.FirstOrDefault(item => item.Unit == prisoner.Id)?.Captors, capture.Custodian) is { } captor)
            {
                return Fail<GameState>(captor.Code, captor.Text);
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
            var loose = Active(state, freed.Unit) as UnitInstance;
            if (Rules.ScenarioA1CloseCombatProjection.AlreadyFreed(loose is not null, loose?.Custodian is null, loose is not null && GameState.Condition(loose, Conditions.Unarmed) == ConditionState.True,
                loose is not null && GameState.Condition(loose, Conditions.Captured) == ConditionState.True))
            {
                return state;
            }

            if (loose is not { Custodian: not null } prisoner)
            {
                var notGuarded = Rules.ScenarioA1CloseCombatProjection.NotGuardedRefusal(freed.Unit);
                return Fail<GameState>(notGuarded.Code, notGuarded.Text);
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
            // Pass 32.h: the record's reads are made here, and Rules decides (B25.65, E1.12).
            if (Rules.ScenarioA1NightAndWeather.VerifyWindChange(state.Phase, rolls.ContainsKey(wind.Roll), wind.NvrRoll is not { } nvrRoll || rolls.ContainsKey(nvrRoll), state.Night,
                wind.Nvr, wind.Precipitation) is { } refusal)
            {
                return Fail<GameState>(refusal.Code, refusal.Text);
            }

            return state with
            {
                Nvr = wind.Nvr,
                Precipitation = wind.Precipitation,
                Rained = Rules.ScenarioA1NightAndWeather.RainedAfter(state.Rained, wind.Precipitation),
            };
        }

        /// <summary>
        /// A Starshell attempt (E1.92 to E1.923; ruling R16.8): by an active unit at night, once per hex per phase, its rolls recorded; a Starshell that
        /// passed its Usage dr and landed on the map is placed in its Location until the end of the CCPh.
        /// </summary>
        private GameState? FireStarshell(GameState state, StarshellFired starshell)
        {
            // Pass 32.h: the record's reads are made here, and Rules decides (E1.92 to E1.923).
            if (Rules.ScenarioA1Starshells.VerifyStarshell(state.Night, state.Unit(starshell.Unit) is { Status: InstanceStatus.Active }, rolls.ContainsKey(starshell.UsageRoll),
                starshell.PlacementRoll is not { } placement || rolls.ContainsKey(placement), starshell.At is not null, starshell.Starshell is not null, starshell.Passed,
                state.StarshellAttempts.Contains(starshell.From.ToString(), StringComparer.Ordinal)) is { } refusal)
            {
                return Fail<GameState>(refusal.Code, refusal.Text);
            }

            var next = state with
            {
                StarshellAttempts = [.. state.StarshellAttempts, starshell.From.ToString()],
                StarshellUsed = state.StarshellUsed || starshell.Passed,
                StarshellTurn = Rules.ScenarioA1Starshells.StarshellTurnAfter(state.StarshellTurn, starshell.Passed, $"{state.Turn}|{state.PhasingSide}"),
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
            // Pass 32.h: the record's reads are made here, and Rules decides (A14.1).
            if (Rules.ScenarioA1Sniper.VerifySniperAttack(state.Find(sniper.Sniper) is EntityInstance { Kind: "asl:sniper", Status: InstanceStatus.Active }, rolls.ContainsKey(sniper.Roll),
                rolls.ContainsKey(sniper.Trigger), sniper.Dr) is { } refusal)
            {
                return Fail<GameState>(refusal.Code, refusal.Text);
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
            static int Size(UnitInstance unit) => Rules.ScenarioA1PrisonerCalculator.UnitSize(unit.Kind == "asl:squad", unit.Kind is "asl:half-squad" or "asl:crew");
            foreach (var prisoner in next.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Custodian is not null).ToArray())
            {
                var at = next.Location(prisoner.Id)?.Location;
                var guard = next.Unit(prisoner.Custodian!);
                if (!Rules.ScenarioA1CloseCombatProjection.GuardLeft(guard is { Status: InstanceStatus.Active }, guard is not null && GameState.Condition(guard, Conditions.Captured) == ConditionState.True))
                {
                    continue;
                }

                var side = guard?.Side;
                var heir = next.Units.Where(unit => Rules.ScenarioA1CloseCombatProjection.GuardHeir(unit.Status == InstanceStatus.Active, unit.Side == side, at is not null && next.Location(unit.Id)?.Location == at,
                        GameState.Condition(unit, Conditions.Captured) == ConditionState.True, GameState.Condition(unit, Conditions.Unarmed) == ConditionState.True,
                        vocabulary.IsA(unit.Kind, "asl:personnel"), vocabulary.IsA(unit.Kind, "asl:vehicle"),
                        Rules.ScenarioA1PrisonerCalculator.CanGuard(next.Units.Where(other => other.Status == InstanceStatus.Active && other.Custodian == unit.Id).Sum(Size), Size(prisoner), Size(unit))))
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

/// <summary>
/// A game's history with what its replay carried at its last event (pass 31d, design D1). It never changes: <see cref="Continue"/> makes another
/// projection and leaves this one as it is, so a kept projection serves any number of continuations.
/// </summary>
public sealed class ReplayedGame
{
    private readonly GameProjector.Replay replay;

    internal ReplayedGame(GameProjector.Replay replay, GameHistory history)
    {
        this.replay = replay;
        History = history;
    }

    /// <summary>The history: its events are a copy of the list given, so a caller's later change to its list changes nothing here.</summary>
    public GameHistory History
    {
        get;
    }

    /// <summary>Whether every event was applied, so that events may follow.</summary>
    public bool Complete => !History.HasErrors && History.States.Count == History.Events.Count;

    /// <summary>Whether a list begins with this projection's events, object for object.</summary>
    public bool Begins(IReadOnlyList<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var own = History.Events;
        if (own.Count == 0 || own.Count > events.Count || !ReferenceEquals(own[^1], events[own.Count - 1]))
        {
            return false;
        }

        for (var index = 0; index < own.Count - 1; index++)
        {
            if (!ReferenceEquals(own[index], events[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The projection of a list that begins with this one's events: only the events after them are applied, by the code a whole replay runs, and
    /// each is verified as a whole replay verifies it.
    /// </summary>
    public ReplayedGame Continue(IReadOnlyList<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (!Complete || !Begins(events))
        {
            throw new ArgumentException("The events do not continue this projection.", nameof(events));
        }

        var diagnostics = new List<UnitDiagnostic>(History.Diagnostics);
        return GameProjector.Run(replay.Copy(diagnostics), [.. events], [.. History.States], diagnostics);
    }
}
