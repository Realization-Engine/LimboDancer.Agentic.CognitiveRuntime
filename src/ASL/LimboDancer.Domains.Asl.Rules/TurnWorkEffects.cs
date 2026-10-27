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
            TurnWorkEffectKind.Replenish => new(null, true, work.SupplyDepleted),
            _ => new(null, false, false, true),
        };
    }

    // E10.12 permits removal only when Immobile (or Depleted); E10.4 moves available supply.
    public static bool CanRelocateSupply(bool immobilized, bool bogged) => !ScenarioA1PassengerActionRules.VehicleMobile(immobilized, bogged);
    public static bool WreckAfterRelocation(bool immobilized) => immobilized;

    public static int? PassengerCapacity(int? ordinary, bool hasAmmoSupply) => hasAmmoSupply ? 0 : ordinary;
    public static string? PackedGunBar(bool packed) => packed ? "play.packed-gun: unpack the Gun before using it; animal loading/transport remains pending (G10)" : null;
}
