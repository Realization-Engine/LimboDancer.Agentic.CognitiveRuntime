namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>Evidence projected from a recorded SW repair, bounded to one Player Turn.</summary>
public sealed record SupportWeaponRepairUse(string Unit, string Weapon, int PlayerTurn);
