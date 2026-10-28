namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A bound weapon/Mule pair between load or period changes. Unknown prior ammunition and malfunction facts stay unknown.</summary>
public sealed record TurnPackingCasualtyState(TurnPackingWeapon Weapon, TurnHorseSize MuleSize,
    bool Loaded, bool MulePackTi, bool? Malfunctioned, int? LowAmmoOccurrences,
    TurnPackingRecovery Recovery, bool MuleAlive, bool WeaponAlive);
public sealed record TurnPackingCasualtyTransition(TurnPackingCasualtyState State, TurnPackingVulnerabilityVerdict Verdict);

/// <summary>Accumulate G10.4 consequences without changing actors or crediting/restarting any packing period.</summary>
public static class TurnPackingCasualtyRules
{
    public static TurnPackingCasualtyTransition Apply(TurnPackingCasualtyState state, TurnPackingMuleLoss loss,
        int originalDr, TurnPackingUnloadTerrain terrain)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.MuleAlive || !state.WeaponAlive || state.MuleSize is not (TurnHorseSize.Squad or TurnHorseSize.HalfSquad)
            || state.Loaded && state.MuleSize != TurnHorseSize.Squad || state.LowAmmoOccurrences < 0
            || !Enum.IsDefined(state.Recovery) || state.Recovery == TurnPackingRecovery.FullOrdinaryPeriod && state.Loaded)
        {
            throw new InvalidDataException("The bound Animal-Pack casualty state is terminal or inconsistent.");
        }
        if (loss is not (TurnPackingMuleLoss.CasualtyReduced or TurnPackingMuleLoss.Eliminated)
            || loss == TurnPackingMuleLoss.CasualtyReduced && state.MuleSize != TurnHorseSize.Squad)
        {
            throw new InvalidDataException("A squad Mule can reduce to a HS; a HS casualty eliminates the Mule (G10.41).");
        }
        var verdict = TurnPackingVulnerabilityRules.Resolve(new(state.Weapon, loss, state.Loaded, state.MulePackTi, originalDr, terrain));
        if (verdict.Assessment != TurnPackingVulnerabilityAssessment.Resolved || verdict.Effects is not { } effects)
        {
            throw new InvalidDataException("All casualty and required terrain facts must be resolved before recording effects.");
        }
        var recovery = effects.EliminateWeapon ? TurnPackingRecovery.None
            : state.Recovery == TurnPackingRecovery.FullOrdinaryPeriod ? state.Recovery : effects.Recovery;
        return new(state with
        {
            MuleSize = loss == TurnPackingMuleLoss.CasualtyReduced ? TurnHorseSize.HalfSquad : state.MuleSize,
            MuleAlive = loss != TurnPackingMuleLoss.Eliminated,
            WeaponAlive = !effects.EliminateWeapon,
            Loaded = state.Loaded && !effects.UnloadWeapon && !effects.EliminateWeapon,
            MulePackTi = loss != TurnPackingMuleLoss.Eliminated && (state.MulePackTi || effects.MarkMulePackTi),
            Malfunctioned = effects.MarkMalfunctioned ? true : state.Malfunctioned,
            LowAmmoOccurrences = effects.AddLowAmmo && state.LowAmmoOccurrences is { } count ? checked(count + 1) : state.LowAmmoOccurrences,
            Recovery = recovery
        }, verdict);
    }
}
