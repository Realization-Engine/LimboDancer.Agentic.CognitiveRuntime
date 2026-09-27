using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Composition;
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
        IRallyRecordVerifier? rally = null)
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

        var replay = new Replay(vocabulary, catalogs, chains, liveSources ?? [], fire, rally, diagnostics);
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
        IFireRecordVerifier? fireVerifier, IRallyRecordVerifier? rallyVerifier, List<UnitDiagnostic> diagnostics)
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
                PhaseChanged phase => ChangePhase(previous, phase),
                InstanceCreated created => CreateInPlay(previous, created.Instance),
                InstanceMoved moved => Move(previous, moved),
                EquipmentTransferred transferred => Transfer(previous, transferred),
                ConditionsChanged changed => ChangeConditions(previous, changed),
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
                MovementStepped moving => StepMovement(previous, moving),
                MovementWindowClosed closed => CloseWindow(previous, closed),
                MovementEnded ended => EndMovement(previous, ended),
                _ => Fail<GameState>("UNIT-STATE-001", $"'{gameEvent.Type}' has no projection."),
            };

            if (next is null)
            {
                return null;
            }

            next = KeepMovingStack(next, gameEvent.Payload);
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
                ScenarioMonth = started.ScenarioMonth,
                FirstSide = started.PhasingSide,
                Source = gameEvent.Source,
            };
            return CheckPhase(state, started.Turn, started.Phase, started.PhasingSide) ? state : null;
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

            // MF are spent within a phase, so a new phase starts every unit at none spent and free to move. The phase that
            // ends removes its markers, from units and weapons alike: First and Final Fire at the end of the DFPh (A3.4), Prep
            // Fire at the end of the AFPh (A3.5), and pins in the CCPh (A3.8), all on p. 47; DM at the end of every RPh
            // (A10.62, p. 68). Residual FP is removed at the end of the MPh (A8.2, p. 60), and a new Player Turn gives every
            // unit a new Rally attempt (A10.6, p. 68).
            string[] cleared = state.Phase switch
            {
                "dfph" => [Conditions.FinalFire, Conditions.FirstFire],
                "afph" => [Conditions.PrepFire],
                "ccph" => [Conditions.Pinned],
                "rph" => [Conditions.DesperationMorale],
                _ => [],
            };
            var newPlayerTurn = change.PhasingSide != state.PhasingSide;
            return CheckPhase(state, change.Turn, change.Phase, change.PhasingSide)
                ? state with
                {
                    Turn = change.Turn,
                    Phase = change.Phase,
                    PhasingSide = change.PhasingSide,
                    FiresThisPhase = [],
                    RepairsThisPhase = [],
                    ResidualFire = [],
                    Movement = null,
                    RallyAttemptsThisPlayerTurn = newPlayerTurn ? [] : state.RallyAttemptsThisPlayerTurn,
                    FirstMmcRallyTaken = newPlayerTurn ? [] : state.FirstMmcRallyTaken,
                    Units = [.. state.Units.Select(unit => Clear(unit is { MfSpent: 0, MovementEnded: false, HalfMfSpent: false }
                        ? unit
                        : unit with { MfSpent = 0, MovementEnded = false, HalfMfSpent = false }, cleared))],
                    Equipment = [.. state.Equipment.Select(equipment => equipment.Conditions.Keys.Any(cleared.Contains)
                        ? equipment with { Conditions = Without(equipment.Conditions, cleared) }
                        : equipment)],
                }
                : null;
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
            // group; Residual FP has no firers and never forms one (A8.22).
            if (fire.Firers.Count > 0 && state.FiresThisPhase.Any(item => item.FirerLocation == fire.FirerLocation
                && item.TargetLocation == fire.TargetLocation && item.Step == fire.MovementStep))
            {
                return Fail<GameState>("UNIT-STATE-024", $"{fire.FirerLocation} has already fired at {fire.TargetLocation} this phase (A7.55).");
            }

            if (fireVerifier.Verify(state, fire, rolls) is { } reason)
            {
                return Fail<GameState>("UNIT-STATE-024", reason);
            }

            fires[eventId] = (fire, gameEvent.Visibility is not null, false);
            return state with
            {
                FiresThisPhase = [.. state.FiresThisPhase, new FireRecord(eventId, fire.FirerLocation, fire.TargetLocation) { Step = fire.MovementStep }],
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

            var mmc = vocabulary.IsA(unit.Kind, "asl:mmc") && unit.Side == state.PhasingSide && !state.FirstMmcRallyTaken.Contains(unit.Side);
            return state with
            {
                RallyAttemptsThisPlayerTurn = [.. state.RallyAttemptsThisPlayerTurn, unit.Id],
                FirstMmcRallyTaken = mmc ? [.. state.FirstMmcRallyTaken, unit.Side] : state.FirstMmcRallyTaken,
            };
        }

        /// <summary>A Repair record (A9.72): a dr at most the Repair Number repairs, a 6 eliminates, anything else changes nothing.</summary>
        private GameState? Repair(GameState state, RepairAttempted repair)
        {
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

            var next = state;
            foreach (var unit in movers)
            {
                var halves = (unit!.MfSpent * 2) + (unit.HalfMfSpent ? 1 : 0) + moving.HalfMf;
                next = Replace(next, unit with
                {
                    Position = new MapPosition(moving.To),
                    MfSpent = halves / 2,
                    HalfMfSpent = halves % 2 == 1
                })!;
            }

            return next with
            {
                Movement = new MovementState(moving.Movers, moving.To, moving.HalfMf, moving.Step, moving.Assault, WindowOpen: true)
                {
                    Members = current?.Members ?? moving.Movers
                },
            };
        }

        private GameState? CloseWindow(GameState state, MovementWindowClosed closed) =>
            state.Movement is { WindowOpen: true } movement && movement.Step == closed.Step
                ? state with
                {
                    Movement = movement with
                    {
                        WindowOpen = false
                    }
                }
                : Fail<GameState>("UNIT-STATE-029", $"No DEFENDER window is open on step {closed.Step}.");

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
                    next = Replace(next, unit with
                    {
                        MovementEnded = true
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

            var leaving = members.Where(id => next.Unit(id) is not { Status: InstanceStatus.Active } unit
                || GameState.Condition(unit, Conditions.Broken) == ConditionState.True || GameState.Condition(unit, Conditions.Pinned) == ConditionState.True)
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
                    : state with
                    {
                        Units = [.. state.Units, new UnitInstance(instance.Id, instance.Kind, null, instance.Side, position, instance.Conditions, InstanceStatus.Active, from)]
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

            var unit = new UnitInstance(instance.Id, instance.Kind, catalog.Reference(definition), instance.Side, position, instance.Conditions,
                InstanceStatus.Active, from);
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
                UnitInstance unit => Replace(state, unit with { Position = move.Position, MfSpent = unit.MfSpent + (move.Mf ?? 0) }),
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
            foreach (var unit in consumed)
            {
                next = Replace(next, unit with
                {
                    Status = InstanceStatus.Consumed
                });
            }

            // A Replacement, whether by a lesser unit (A19.13) or by Battle Hardening (A15.3), is a unit substitution: the new
            // unit keeps the SW; other lineage leaves it unpossessed.
            if (lineage.Action != LineageAction.Replaced)
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
                        MovementEnded = consumed.Any(item => item.MovementEnded)
                    })
                    : created;
            }

            if (lineage.Action == LineageAction.Replaced)
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

            return next;
        }

        /// <summary>
        /// A unit created in play, such as a hero (A15.21). One created in its own side's MPh moves no further that phase: the
        /// movement status it would share with its creator is not modelled (ASL Unit Backlog, section 10).
        /// </summary>
        private GameState? CreateInPlay(GameState state, NewInstance instance)
        {
            if (Create(state, instance, from: []) is not { } next)
            {
                return null;
            }

            return next.Phase == "mph" && next.Unit(instance.Id) is { } unit && unit.Side == next.PhasingSide
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

            var captured = new Dictionary<string, ConditionState>(prisoner.Conditions, StringComparer.Ordinal) { [Conditions.Captured] = ConditionState.True };
            return Replace(state, prisoner with
            {
                Conditions = captured,
                Custodian = capture.Custodian
            });
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
