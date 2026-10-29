namespace LimboDancer.Domains.Asl.Rules;

public enum TurnWorkEffectKind
{
    PackGun = 1, UnpackGun = 2, Replenish = 3, TransferSupply = 4, OffloadSupply = 5
}

/// <summary>Frozen at declaration. Eligibility/duration remain externally adjudicated; this is not an arbitrary mutation list.</summary>
public sealed record TurnWorkEffectSpec(TurnWorkEffectKind Kind, string? Crew = null, string? SupplyId = null,
    string? SupplyVehicle = null, bool CreateSupply = false, bool RecordLowAmmo = false, string? DestinationVehicle = null, string? DumpLocation = null);

public sealed record TurnWorkEffectResult(bool? Packed, bool RestoreAmmunition, bool DepleteSupply, bool RelocateSupply = false);

public static class TurnWorkEffects
{
    public static void Validate(TurnWorkDeclaration declaration)
    {
        var effect = declaration.CompletionEffects;
        if (effect is null)
        {
            return; // Old records are lifecycle-only, never retroactively given effects.
        }
        var valid = (effect.Kind is TurnWorkEffectKind.TransferSupply or TurnWorkEffectKind.OffloadSupply
            || effect.DestinationVehicle is null && effect.DumpLocation is null) && effect.Kind switch
            {
                TurnWorkEffectKind.PackGun or TurnWorkEffectKind.UnpackGun => declaration.Kind == TurnWorkKind.AnimalPacking
                    && !string.IsNullOrWhiteSpace(effect.Crew) && effect.Crew != declaration.Subject
                    && declaration.Participants.Contains(effect.Crew, StringComparer.Ordinal)
                    && effect.SupplyId is null && effect.SupplyVehicle is null && !effect.CreateSupply && !effect.RecordLowAmmo,
                TurnWorkEffectKind.Replenish => declaration.Kind == TurnWorkKind.AmmoReplenishment && effect.Crew is null
                    && !string.IsNullOrWhiteSpace(effect.SupplyId) && !string.IsNullOrWhiteSpace(effect.SupplyVehicle)
                    && effect.SupplyVehicle != declaration.Subject && declaration.Participants.Contains(effect.SupplyVehicle, StringComparer.Ordinal),
                TurnWorkEffectKind.TransferSupply or TurnWorkEffectKind.OffloadSupply => declaration.Kind == TurnWorkKind.AmmoTransfer
                    && effect.Crew is null && !effect.RecordLowAmmo && !string.IsNullOrWhiteSpace(effect.SupplyId)
                    && effect.SupplyVehicle == declaration.Subject
                    && (effect.Kind == TurnWorkEffectKind.TransferSupply
                        ? !string.IsNullOrWhiteSpace(effect.DestinationVehicle) && effect.DestinationVehicle != effect.SupplyVehicle
                            && declaration.Participants.Contains(effect.DestinationVehicle, StringComparer.Ordinal) && effect.DumpLocation is null
                        : effect.DestinationVehicle is null && !string.IsNullOrWhiteSpace(effect.DumpLocation)),
                _ => false,
            };
        if (!valid)
        {
            throw new InvalidDataException("The completion specification must match the work kind and its declared participant roles.");
        }
    }

    public static TurnWorkEffectResult Resolve(TurnLiveWork work)
    {
        Validate(work.Declaration);
        if (!work.Completed || work.EffectsApplied || work.Declaration.CompletionEffects is not { } effect)
        {
            throw new InvalidDataException("Effects require a newly completed work order with a recorded specification.");
        }
        return effect.Kind switch
        {
            TurnWorkEffectKind.PackGun => new(true, false, false),
            TurnWorkEffectKind.UnpackGun => new(false, false, false),
            // E10.31 keeps a different answer here (the E10 claim, RE10.10): every depleted Ammo Type is restored with no Low Ammo condition;
            // ScenarioA1AmmoVehicleCalculator.Restored restores only a Low Ammo vehicle's. Track 3 moves the caller (W-E10).
            TurnWorkEffectKind.Replenish => new(null, true, work.SupplyDepleted),
            _ => new(null, false, false, true),
        };
    }

    /// <summary>
    /// E10.12 permits removal only when Immobile (or Depleted); E10.4 moves available supply. Keeps its answer (the E10 claim, RE10.10): Immobile
    /// is read as Immobilized or Bogged only, where <see cref="ScenarioA1AmmoVehicleCalculator.SupplyRemovalRefusal"/> reads D.7's
    /// <see cref="ScenarioA1AmmoVehicleCalculator.Immobile"/> (Mired, Abandoned, crew broken, Shocked, Stunned too).
    /// </summary>
    public static bool CanRelocateSupply(bool immobilized, bool bogged) => !ScenarioA1PassengerActionRules.VehicleMobile(immobilized, bogged);

    /// <summary>E10.4, by <see cref="ScenarioA1AmmoVehicleCalculator.WreckAfterTransfer"/>.</summary>
    public static bool WreckAfterRelocation(bool immobilized) => ScenarioA1AmmoVehicleCalculator.WreckAfterTransfer(immobilized);

    /// <summary>E10.3's CE, by <see cref="ScenarioA1AmmoVehicleCalculator.ReplenishmentPosture"/>.</summary>
    public static bool ReplenishmentPostureSupported(bool? buttonedUp) => ScenarioA1AmmoVehicleCalculator.ReplenishmentPosture(buttonedUp);

    /// <summary>
    /// Replenishment's state at declaration and completion (E10.3): recorded Low Ammo, an own supply vehicle whose available supply is bound
    /// to it, both vehicles in one Location, and no Passengers aboard the supply vehicle. Keeps its answer (the E10 claim, RE10.10): one Location
    /// only, where <see cref="ScenarioA1AmmoVehicleCalculator.ReplenishmentRefusal"/> allows an Accessible one and also asks TI and the armament.
    /// </summary>
    public static bool ReplenishmentReady(bool ownSupplyVehicle, bool supplyBoundAndAvailable, bool coLocated, bool lowAmmoRecorded,
        bool supplyVehicleHasPassengers) =>
        ownSupplyVehicle && supplyBoundAndAvailable && coLocated && lowAmmoRecorded && !supplyVehicleHasPassengers;

    /// <summary>
    /// Packing's state (G10): an own Good Order crew in the Gun's Location; packing needs the Gun manned by that crew and not yet packed,
    /// unpacking a packed Gun that nobody holds.
    /// </summary>
    public static bool PackingReady(TurnWorkEffectKind kind, bool ownCrew, bool crewGoodOrder, bool coLocated, bool gunPacked,
        bool crewMansGun, bool gunHeld) =>
        ownCrew && crewGoodOrder && coLocated
        && (kind == TurnWorkEffectKind.PackGun ? !gunPacked && crewMansGun : gunPacked && !gunHeld);

    /// <summary>
    /// The source of a supply transfer or offload (E10.4, E10.12): its available supply, not yet in a dump, bound to it and its side; an
    /// Immobile vehicle; no occupants and no held equipment. Keeps its answer (the E10 claim, RE10.10): E10 asks neither the occupants nor the
    /// equipment, and <see cref="ScenarioA1AmmoVehicleCalculator.TransferRefusal"/> reads Immobile by D.7 or a Depleted supply.
    /// </summary>
    public static bool RelocationSourceReady(bool supplyBoundAndAvailable, bool immobilized, bool bogged, bool hasOccupants, bool holdsEquipment) =>
        supplyBoundAndAvailable && CanRelocateSupply(immobilized, bogged) && !hasOccupants && !holdsEquipment;

    /// <summary>
    /// The destination of a supply transfer (E10.4): an own vehicle in the source's Location with no supply and no Passengers. Keeps its answer (the
    /// E10 claim, RE10.10): one Location only and no E10.11 check, where <see cref="ScenarioA1AmmoVehicleCalculator.TransferRefusal"/> allows an
    /// Accessible Location and asks that the vehicle qualify.
    /// </summary>
    public static bool TransferDestinationReady(bool sameSide, bool coLocated, bool hasSupply, bool hasPassengers) =>
        sameSide && coLocated && !hasSupply && !hasPassengers;

    /// <summary>E10.12, by <see cref="ScenarioA1AmmoVehicleCalculator.PassengerCapacity"/>.</summary>
    public static int? PassengerCapacity(int? ordinary, bool hasAmmoSupply) => ScenarioA1AmmoVehicleCalculator.PassengerCapacity(ordinary, hasAmmoSupply);
    public static string? PackedGunBar(bool packed) => packed ? "play.packed-gun: unpack the Gun before using it; animal loading/transport remains pending (G10)" : null;
}
