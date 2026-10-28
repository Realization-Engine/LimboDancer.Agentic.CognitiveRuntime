using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

public static partial class GameProjector
{
    internal sealed partial class Replay
    {
        private static readonly Lazy<ScenarioA1OrdnanceReference> SpotterOrdnance = new(() => new ScenarioA1OrdnancePackage().Reference);

        private GameState? DesignateSpotter(GameState state, MortarSpotterDesignated declaration, GameEvent gameEvent)
        {
            if (gameEvent.Type != "mortar-spotter-designated" || state.TurnPhaseOccurrence is not { } occurrence
                || state.Equipment.FirstOrDefault(item => item.Id == declaration.Gun) is not { } gun
                || state.Unit(declaration.Spotter) is not { } unit || gun.Holding is not { } holding
                || state.Unit(holding.Holder) is not { } holder || holder.Side is not { } owner)
            {
                return Fail<GameState>("UNIT-SPOTTER-001", "A designation needs an identified mortar, holder, Spotter and phase occurrence.");
            }
            var existing = state.MortarSpotters.FirstOrDefault(item => item.Gun == gun.Id);
            var origin = state.MortarSpotterDesignations.LastOrDefault(item => item.Gun == gun.Id);
            bool? hasDesignation = origin is not null ? true : existing is null ? false : null;
            var phaseFirer = state.PhaseFirers.FirstOrDefault(item => item.Unit == unit.Id);
            bool Is(string condition) => GameState.Condition(unit, condition) == ConditionState.True;
            var fired = Is(Conditions.PrepFire) || Is(Conditions.FinalFire) || Is(Conditions.FirstFire);
            var support = new SquadSupportFacts(unit.Id, unit.Kind == "asl:squad", Is(Conditions.FirstFire),
                Is(Conditions.PrepFire) || Is(Conditions.FinalFire), phaseFirer is not null, phaseFirer?.Weapon,
                state.SupportWeaponUses.Any(item => item.Unit == unit.Id));
            var gunAt = state.Location(gun.Id)?.Location;
            var spotterAt = state.Location(unit.Id)?.Location;
            var otherMortars = state.MortarSpotters.Where(item => item.Spotter == unit.Id && item.Gun != gun.Id).ToArray();
            var facts = new TurnSpotterDesignationFacts(new(owner,
                new(state.Turn, state.PlayerTurnOrdinal, state.PhasingSide, state.Phase, TurnCheckpoint.DuringPhase), hasDesignation, TurnSpotterChange.None),
                gun.Definition is { } definition && SpotterOrdnance.Value.Guns.GetValueOrDefault(definition.Definition)?.GunType == "mortar",
                gun.Status == InstanceStatus.Active && holder.Status == InstanceStatus.Active && unit.Status == InstanceStatus.Active && unit.Side == owner
                    && holder.Position is MapPosition && unit.Position is MapPosition,
                GameState.GoodOrder(unit, vocabulary) == ConditionState.True,
                SpotterHexRange(gunAt, spotterAt) is { } range ? range <= 1 : null,
                otherMortars.All(item => SpotterHexRange(gunAt, state.Location(item.Gun)?.Location) == 0), state.Night,
                Is("asl:ti"), unit.Kind == "asl:squad", fired, state.SpottedThisPhase.Any(item => item.Spotter == unit.Id), support,
                state.Choice is not null || state.OpenAttempts.Count > 0 || state.PendingSurrenders.Count > 0 || state.Movement is not null);
            if (TurnSpotterDesignationRules.Refusal(facts) is { } refusal)
            {
                return Fail<GameState>("UNIT-SPOTTER-001", refusal);
            }
            var marked = Replace(state, unit with
            {
                Conditions = new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal)
                {
                    [ConditionName(TurnSpotterDesignationRules.FireCounter(state.Phase))] = ConditionState.True
                }
            });
            return marked with
            {
                MortarSpotters = [.. marked.MortarSpotters, new(gun.Id, unit.Id)],
                MortarSpotterDesignations = [.. marked.MortarSpotterDesignations,
                    new(gun.Id, unit.Id, owner, gameEvent.EventId, gameEvent.Revision, occurrence)],
                SpottedThisPhase = [.. marked.SpottedThisPhase, new(gun.Id, unit.Id)],
                NoMoveThisPlayerTurn = state.Phase == "pfph" ? [.. marked.NoMoveThisPlayerTurn, unit.Id] : marked.NoMoveThisPlayerTurn,
                SupportWeaponUses = facts.UsedAsSpotter ? marked.SupportWeaponUses
                    : [.. ScenarioA1FireFollowUps.SupportWeaponUse([.. marked.SupportWeaponUses.Select(item => (item.Unit, item.Weapon))],
                        unit.Id, facts.Squad, fired, gun.Id).Select(item => new SupportWeaponUse(item.Unit, item.Weapon))],
            };
        }

        private int? SpotterHexRange(BoardLocation? from, BoardLocation? to)
        {
            if (from is null || to is null)
            {
                return null;
            }
            if (layout?.Locate(from.Board, from.Hex) is { } source && layout.Locate(to.Board, to.Hex) is { } target)
            {
                return layout.Geometry.Distance(source, target);
            }
            if (from.Board == to.Board && chains?.Geometry(from.Board) is { } geometry
                && geometry.TryGetIndex(from.Hex, out var a) && geometry.TryGetIndex(to.Hex, out var b))
            {
                return geometry.Distance(a, b);
            }
            return from.Board == to.Board && from.Hex == to.Hex ? 0 : null;
        }
    }
}
