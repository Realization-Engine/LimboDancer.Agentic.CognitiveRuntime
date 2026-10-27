using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

public static partial class GameProjector
{
    internal sealed partial class Replay
    {
        private GameState PrepareWorkEffects(GameState state, TurnLiveWork work)
        {
            if (work.Declaration.CompletionEffects is not { } effect)
            {
                return state;
            }
            if (effect.Kind is TurnWorkEffectKind.Replenish or TurnWorkEffectKind.TransferSupply or TurnWorkEffectKind.OffloadSupply)
            {
                var supply = state.TurnAmmoSupplies.FirstOrDefault(item => item.Id == effect.SupplyId);
                if (effect.CreateSupply)
                {
                    if (supply is not null || state.Find(effect.SupplyId!) is not null
                        || state.TurnAmmoSupplies.Any(item => item.Vehicle == effect.SupplyVehicle && item.Available))
                    {
                        throw new InvalidDataException("An adjudicated new Ammo Supply needs a new identity and an unassigned vehicle.");
                    }
                    state = state with
                    {
                        TurnAmmoSupplies = [.. state.TurnAmmoSupplies,
                        new(effect.SupplyId!, effect.SupplyVehicle!, work.Declaration.Owner, true)]
                    };
                }
                if (effect.RecordLowAmmo)
                {
                    state = state with
                    {
                        LowAmmoVehicles = [.. state.LowAmmoVehicles.Append(work.Declaration.Subject).Distinct(StringComparer.Ordinal)]
                    };
                }
            }
            ValidateWorkEffectState(state, work);
            return state;
        }

        private void ValidateWorkEffectState(GameState state, TurnLiveWork work)
        {
            var declaration = work.Declaration;
            var effect = declaration.CompletionEffects!;
            var subject = state.Find(declaration.Subject);
            if (subject is not { Status: InstanceStatus.Active, Position: MapPosition } || subject.Side != declaration.Owner)
            {
                throw new InvalidDataException("The effect needs its active own subject on the map.");
            }
            if (effect.Kind is TurnWorkEffectKind.TransferSupply or TurnWorkEffectKind.OffloadSupply)
            {
                ValidateSupplyRelocation(state, work, subject);
            }
            else if (effect.Kind == TurnWorkEffectKind.Replenish)
            {
                var supply = state.TurnAmmoSupplies.FirstOrDefault(item => item.Id == effect.SupplyId);
                if (subject is not UnitInstance || !vocabulary.IsA(subject.Kind, "asl:vehicle")
                    || state.Unit(effect.SupplyVehicle!) is not { Status: InstanceStatus.Active, Position: MapPosition } vehicle
                    || !vocabulary.IsA(vehicle.Kind, "asl:vehicle") || vehicle.Side != declaration.Owner
                    || supply is not { Available: true } || supply.Vehicle != vehicle.Id || supply.Owner != declaration.Owner
                    || state.Location(subject.Id)?.Location != state.Location(vehicle.Id)?.Location
                    || !state.LowAmmoVehicles.Contains(subject.Id, StringComparer.Ordinal)
                    || state.Units.Any(unit => unit.Status == InstanceStatus.Active
                        && unit.Position is ContainedPosition { Role: ContainmentRole.Passenger } aboard && aboard.Container == vehicle.Id))
                {
                    throw new InvalidDataException("Replenishment effects need recorded Low Ammo, available compatible adjudicated supply, co-located vehicles and no supply-vehicle Passengers.");
                }
            }
            else
            {
                var packed = state.PackedGuns.Contains(subject.Id, StringComparer.Ordinal);
                if (subject is not EquipmentInstance { Kind: "asl:gun" } gun
                    || state.Unit(effect.Crew!) is not { Status: InstanceStatus.Active } crew || crew.Side != declaration.Owner
                    || !vocabulary.IsA(crew.Kind, "asl:personnel") || GameState.GoodOrder(crew, vocabulary) != ConditionState.True
                    || state.Location(crew.Id)?.Location != state.Location(gun.Id)?.Location
                    || (effect.Kind == TurnWorkEffectKind.PackGun ? packed || gun.Holding != new Holding(crew.Id, HoldingRole.Manned)
                        : !packed || gun.Holding is not null))
                {
                    throw new InvalidDataException("Packing effects need the recorded Gun/crew in one Location and the expected assembly/manning state.");
                }
            }
        }

        private void ValidateSupplyRelocation(GameState state, TurnLiveWork work, IGameObject subject)
        {
            var effect = work.Declaration.CompletionEffects!;
            var supply = state.TurnAmmoSupplies.FirstOrDefault(item => item.Id == effect.SupplyId);
            if (subject is not UnitInstance source || !vocabulary.IsA(source.Kind, "asl:vehicle")
                || supply is not { Available: true, DumpLocation: null } || supply.Vehicle != source.Id || supply.Owner != source.Side
                || !TurnWorkEffects.CanRelocateSupply(GameState.Condition(source, Conditions.Immobilized) == ConditionState.True,
                    GameState.Condition(source, Conditions.Bogged) == ConditionState.True)
                || state.Units.Any(unit => unit.Status == InstanceStatus.Active && unit.Position is ContainedPosition aboard && aboard.Container == source.Id)
                || state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == source.Id))
            {
                throw new InvalidDataException("Supply relocation needs an available bound supply on an Immobile own vehicle, without explicit occupants or held equipment; their disposition remains pending.");
            }
            if (effect.Kind == TurnWorkEffectKind.OffloadSupply)
            {
                if (effect.DumpLocation != state.Location(source.Id)!.Location.ToString())
                {
                    throw new InvalidDataException("An Ammo Dump must use the source's recorded same Location.");
                }
            }
            else if (state.Unit(effect.DestinationVehicle!) is not { Status: InstanceStatus.Active, Position: MapPosition } destination
                || !vocabulary.IsA(destination.Kind, "asl:vehicle") || destination.Side != source.Side
                || state.Location(destination.Id)?.Location != state.Location(source.Id)?.Location
                || state.TurnAmmoSupplies.Any(item => item.Available && item.Vehicle == destination.Id)
                || state.Units.Any(unit => unit.Status == InstanceStatus.Active
                    && unit.Position is ContainedPosition { Role: ContainmentRole.Passenger } aboard && aboard.Container == destination.Id))
            {
                throw new InvalidDataException("Supply transfer needs a co-located active own, externally qualified vehicle without supply or Passengers.");
            }
        }

        private GameState CompleteWorkEffects(GameState state, TurnLiveWork work)
        {
            ValidateWorkEffectState(state, work); // Recheck current state; a declaration is not a timeless permission.
            var result = TurnWorkEffects.Resolve(work);
            var effect = work.Declaration.CompletionEffects!;
            var subject = work.Declaration.Subject;
            if (result.RelocateSupply)
            {
                var relocated = state with
                {
                    TurnAmmoSupplies = [.. state.TurnAmmoSupplies.Select(supply => supply.Id == effect.SupplyId
                        ? supply with { Vehicle = effect.DestinationVehicle, DumpLocation = effect.DumpLocation } : supply)],
                };
                return TurnWorkEffects.WreckAfterRelocation(GameState.Condition(state.Unit(subject)!, Conditions.Immobilized) == ConditionState.True)
                    ? Wreck(relocated, new VehicleWrecked(subject, false)) ?? throw new InvalidDataException("The supply source cannot become a wreck.")
                    : relocated;
            }
            if (result.Packed is { } packed)
            {
                return state with
                {
                    PackedGuns = packed ? [.. state.PackedGuns, subject] : [.. state.PackedGuns.Where(id => id != subject)],
                    Equipment = [.. state.Equipment.Select(gun => gun.Id == subject
                        ? gun with { Holding = packed ? null : new Holding(effect.Crew!, HoldingRole.Manned) } : gun)],
                };
            }
            return state with
            {
                LowAmmoVehicles = [.. state.LowAmmoVehicles.Where(id => id != subject)],
                DepletedAmmunition = [.. state.DepletedAmmunition.Where(item => item.Gun != subject)],
                TurnAmmoSupplies = [.. state.TurnAmmoSupplies.Select(supply => supply.Id == effect.SupplyId && result.DepleteSupply
                    ? supply with { Available = false } : supply)],
            };
        }
    }
}
