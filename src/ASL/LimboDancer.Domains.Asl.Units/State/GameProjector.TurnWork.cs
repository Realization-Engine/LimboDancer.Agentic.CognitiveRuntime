using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

public static partial class GameProjector
{
    internal sealed partial class Replay
    {
        private GameState? RecordTurnWork(GameState state, TurnWorkRecorded change, string eventType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(change.RuleBasis))
                {
                    throw new InvalidDataException("Work adjudication needs its rule basis.");
                }
                var existing = state.TurnWork.SingleOrDefault(item => item.Declaration.Id == change.WorkId);
                var effects = change.Declaration?.CompletionEffects is not null || existing?.Declaration.CompletionEffects is not null;
                var audited = existing is null ? eventType == TurnWorkRecorded.AuditedType : existing.HostAuditVersion == 1;
                if (eventType != (audited ? TurnWorkRecorded.AuditedType : effects ? TurnWorkRecorded.EffectsType : TurnWorkRecorded.LifecycleType))
                {
                    throw new InvalidDataException("The work event protocol must match its recorded completion specification.");
                }
                TurnLiveWork next;
                if (change.Operation == "start")
                {
                    var declaration = change.Declaration;
                    if (existing is not null || declaration is null || declaration.Id != change.WorkId || change.Roll is not null)
                    {
                        throw new InvalidDataException("Duplicate or invalid work declaration.");
                    }
                    next = TurnWorkLifecycle.Start(declaration, state.Turn, WorkWindow(state) == TurnWorkWindow.FirstRally) with
                    {
                        HostAuditVersion = audited ? 1 : 0
                    };
                    if (declaration.Participants.Any(id => state.Find(id) is not { Status: InstanceStatus.Active } participant
                        || participant is not UnitInstance and not EquipmentInstance || participant.Side != declaration.Owner || state.Location(id) is null || GameState.Condition(participant, "asl:ti") == ConditionState.True
                        || state.TurnWork.Any(work => !work.Completed && !work.Cancelled && work.Declaration.Participants.Contains(id))))
                    {
                        throw new InvalidDataException("Work needs active own participants without another open work assignment.");
                    }
                }
                else
                {
                    if (existing is null || change.Declaration is not null)
                    {
                        throw new InvalidDataException("Unknown work or unexpected declaration.");
                    }
                    if (change.Operation == "resume" && existing.Declaration.Participants.Any(id => state.Find(id) is not { Status: InstanceStatus.Active } participant
                        || participant is not UnitInstance and not EquipmentInstance || participant.Side != existing.Declaration.Owner || state.Location(id) is null || GameState.Condition(participant, "asl:ti") == ConditionState.True))
                    {
                        throw new InvalidDataException("Resume requires active own participants and uncontested TI ownership.");
                    }
                    int? dr = null;
                    if (change.Roll is not null)
                    {
                        if (!rolls.TryGetValue(change.Roll, out var roll) || roll.Count != 2 || roll.Sides != 6 || roll.Purpose != "work-ammunition"
                            || roll.Source != DiceRolled.SystemSource || state.TurnWorkRolls.Contains(change.Roll))
                        {
                            throw new InvalidDataException("Missing work Ammunition DR.");
                        }
                        dr = roll.Values.Sum();
                    }
                    next = TurnWorkLifecycle.Change(existing, change.Operation, state.Turn, WorkWindow(state), dr);
                }
                if (change.Operation == "start")
                {
                    state = PrepareWorkEffects(state, next);
                    if (audited && !AuditedWorkStillValid(state, next))
                    {
                        throw new InvalidDataException("Audited work needs active own, uncaptured participants and supported effect facts.");
                    }
                }
                else if (audited && change.Operation == "resume" && !AuditedWorkStillValid(state, next))
                {
                    throw new InvalidDataException("Resume cannot re-admit invalid participant or effect facts.");
                }
                else if (change.Operation == "complete" && next.Declaration.CompletionEffects is not null)
                {
                    state = CompleteWorkEffects(state, next);
                    next = next with
                    {
                        EffectsApplied = true
                    };
                }
                var updated = state with
                {
                    TurnWork = [.. state.TurnWork.Where(item => item.Declaration.Id != change.WorkId), next],
                    TurnWorkRolls = change.Roll is null ? state.TurnWorkRolls : [.. state.TurnWorkRolls, change.Roll]
                };
                return next.Working || existing?.Working == true
                    ? MarkWorkTi(updated, next.Declaration.Participants, next.Working) : updated;
            }
            catch (Exception error) when (error is InvalidDataException or ArgumentException)
            {
                return Fail<GameState>("UNIT-WORK-001", error.Message);
            }
        }

        internal static TurnWorkWindow WorkWindow(GameState state) => TurnWorkRules.Window(state.Phase, state.PhasingSide == state.FirstSide);

        private bool AuditedWorkStillValid(GameState state, TurnLiveWork work)
        {
            if (work.Declaration.Participants.Any(id => state.Find(id) is not { Status: InstanceStatus.Active } participant
                || participant.Side != work.Declaration.Owner || state.Location(id) is null
                || GameState.Condition(participant, Conditions.Captured) == ConditionState.True))
            {
                return false;
            }
            try
            {
                if (work.Declaration.CompletionEffects is not null)
                {
                    ValidateWorkEffectState(state, work);
                }
                return true;
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        private GameState MaintainTurnWork(GameState? previous, GameState next, EventPayload payload)
        {
            if (previous is null || previous.TurnWork.Count == 0 || payload is TurnWorkRecorded)
            {
                return next;
            }
            var work = next.TurnWork.ToArray();
            for (var i = 0; i < work.Length; i++)
            {
                var item = work[i];
                if (!item.Working)
                {
                    continue;
                }
                // The event's facts for each participant; Rules decides whether they interrupt the work.
                var participants = item.Declaration.Participants;
                var changed = payload as ConditionsChanged;
                var interrupted = TurnWorkRules.Interrupted(new(
                    AuditedEligibilityLost: item.HostAuditVersion == 1 && !AuditedWorkStillValid(next, item),
                    ParticipantGoneOrMoved: participants.Any(id => next.Find(id) is not { Status: InstanceStatus.Active }
                        || previous.Location(id)?.Location != next.Location(id)?.Location),
                    TiCleared: changed is not null && participants.Contains(changed.Id)
                        && changed.Conditions.GetValueOrDefault("asl:ti") == ConditionState.False && changed.Conditions.ContainsKey("asl:ti"),
                    SubjectButtonedUp: changed is not null && changed.Id == item.Declaration.Subject && participants.Contains(changed.Id)
                        && changed.Conditions.GetValueOrDefault(Conditions.ButtonedUp) == ConditionState.True,
                    Moved: payload is MovementStepped movement && movement.Movers.Any(participants.Contains),
                    Fired: payload is FireResolved fire && fire.Firers.Any(participants.Contains),
                    OrdnanceFired: payload is OrdnanceFired ordnance && (participants.Contains(ordnance.Gun) || participants.Contains(ordnance.Crew)),
                    CloseCombat: payload is CloseCombatResolved cc && (cc.Attackers.Any(participants.Contains) || cc.Defenders.Any(participants.Contains)),
                    VehicleStepped: payload is VehicleStepped vehicle && participants.Contains(vehicle.Vehicle)));
                if (interrupted)
                {
                    work[i] = TurnWorkLifecycle.Change(item, "interrupt", previous.Turn, WorkWindow(previous));
                    next = MarkWorkTi(next, participants, false);
                }
                else if (payload is PhaseChanged && next.Turn != previous.Turn)
                {
                    work[i] = TurnWorkLifecycle.NextGameTurn(item, previous.Turn, next.Turn);
                }
            }
            next = next with
            {
                TurnWork = work
            };
            foreach (var item in work.Where(item => item.Working))
            {
                next = MarkWorkTi(next, item.Declaration.Participants, true);
            }
            return next;
        }

        private static GameState MarkWorkTi(GameState state, string[] participants, bool active)
        {
            var value = active ? ConditionState.True : ConditionState.False;
            return state with
            {
                Units = [.. state.Units.Select(unit => participants.Contains(unit.Id) ? unit with
                { Conditions = new Dictionary<string, ConditionState>(unit.Conditions) { ["asl:ti"] = value } } : unit)],
                Equipment = [.. state.Equipment.Select(item => participants.Contains(item.Id) ? item with
                { Conditions = new Dictionary<string, ConditionState>(item.Conditions) { ["asl:ti"] = value } } : item)],
            };
        }
    }
}
