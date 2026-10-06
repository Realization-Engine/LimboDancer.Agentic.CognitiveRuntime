namespace LimboDancer.Domains.Asl.Rules;

// The fact records of the Fire rules that moved from Play and Units in pass 32.c (slice S4). None is recorded in a game: the recorded records of
// an attack stay in ScenarioA1FireModels.cs and keep their shape (the pass 32 design, D5).

/// <summary>A unit against the bars on firing (A15.432, A7.25, A11.15, A20.5, A20.52): its kind, side, conditions, whether it is a vehicle, the phase and phasing side, and the kinds of the prisoners in its custody.</summary>
public sealed record FireBarFacts(
    string Kind,
    string? Side,
    bool Berserk,
    bool BoundingFire,
    bool Melee,
    bool Captured,
    bool Unarmed,
    bool Vehicle,
    string? Phase,
    string? PhasingSide,
    IReadOnlyList<string> PrisonerKinds);

/// <summary>A weapon a unit possesses, as the read of its usable MGs finds it: its id, whether it has malfunctioned or is dismantled (true only when its condition is True), its kind, and its LATW type, read from the catalog only when asked.</summary>
public sealed record PossessedWeaponFacts(string Id, bool Malfunctioned, bool Dismantled, string Kind, Func<string?> LatwType);

/// <summary>A phasing vehicle against Bounding First Fire (D3.3): the phase, whether it is an active vehicle of the phasing side, the moving stack (a vehicle's, its window open, the vehicle among its movers), and whether the vehicle's move has ended.</summary>
public sealed record BoundingFireFacts(string? Phase, bool ActiveVehicleOfPhasingSide, bool MovementExists, bool MovementVehicle, bool WindowOpen, bool AmongMovers, bool MovementEnded);

/// <summary>A vehicle's crew state (D5.3, D5.34, D5.2, C7.42): its BU condition as three values, its Stun, Recall, Shock, and Unconfirmed Kill, and whether it is closed-topped.</summary>
public sealed record CrewExposedFacts(RuleState ButtonedUp, bool Stunned, bool Recalled, bool Shocked, bool UnconfirmedKill, bool ClosedTopped);

/// <summary>A vehicle in a target Location as the state has it (A7.307, A7.308, D.8B): its id, definition, Location text, crew exposure, and conditions.</summary>
public sealed record VehicleTargetFacts(string Id, string? Definition, string At, bool CrewExposed, bool Stunned, bool Recalled, bool StunRecovery, bool Immobilized, bool Concealed, bool Hidden);

/// <summary>A firing vehicle as the state has it (D2.42, A7.82, D3.7, C2.24): its id, definition, Location text, crew exposure, Motion counter, whether it moves unstopped as the moving vehicle, its conditions, its fire counters, and whether its last shot this phase kept its Multiple ROF.</summary>
public sealed record VehicleFirerFacts(
    string Id,
    string? Definition,
    string At,
    bool CrewExposed,
    bool Motion,
    bool MovingUnstopped,
    bool Pinned,
    bool Stunned,
    bool Recalled,
    bool Shocked,
    bool UnconfirmedKill,
    bool StunRecovery,
    bool Malfunctioned,
    bool Fired,
    bool FirstFire,
    bool BoundingFire,
    bool RateOfFireKept);

/// <summary>A unit in a target Location as the state has it (A10.8, A15.21, A19.2, A22.4): its id, definition, Location text, conditions, Dummy, its group's ELR (null outside a group), its Green Inexperience, and the FT it possesses.</summary>
public sealed record TargetUnitFacts(
    string Id,
    string? Definition,
    string At,
    bool Broken,
    bool Pinned,
    bool Concealed,
    bool Hidden,
    bool Dummy,
    bool Wounded,
    bool Disrupted,
    int? Elr,
    bool Fanatic,
    bool Heroic,
    bool Berserk,
    bool? Inexperienced,
    int? Flamethrowers);

/// <summary>A unit against the spending of its fire for the phase (A7.1, A8.4, A9.2, C2.24, D3.5, A7.351, A22.3): the phase, its fire and First Fire counters, whether it is a vehicle; and three reads made only when asked, as the planner made them: whether its last shot kept its Multiple ROF, whether it has used a SW this phase, and whether it holds an unfired, working MG, FT, or ATR.</summary>
public sealed record FireSpentFacts(string? Phase, bool Fired, bool FirstFire, bool Vehicle, Func<bool> RateOfFireKept, Func<bool> SupportWeaponUsed, Func<bool> UnfiredWeaponHeld);

/// <summary>A firer of a proposed attack as the state finds it (A4.8, C10.3): its id, whether it is an active unit from the catalog with a Location, and whether it is TI.</summary>
public sealed record FirerStateFacts(string Id, bool ActiveWithDefinition, bool OnMap, bool Ti);

/// <summary>A directing leader as the state finds it: its id, whether it is an active unit from the catalog, and whether its Location is one of the fire group's (null when it has none).</summary>
public sealed record DirectorStateFacts(string Id, bool ActiveWithDefinition, bool? InGroupLocation);

/// <summary>The MPh kind of a fire group (D3.3, A8.1, A8.3, A8.31; rulings R6.9, R12.3): whether a vehicle fires and whether it is the phasing side's, whether it may Bounding First Fire (read only for a phasing vehicle), each firer's Final Fire and First Fire marks (read only for Infantry in the MPh), whether no firer fires a MG alone, and whether any does.</summary>
public sealed record FireKindFacts(bool VehicleFires, bool VehicleIsPhasing, Func<bool> MayBoundingFire, Func<IReadOnlyList<(bool FinalFire, bool FirstFire)>> Marks, bool NoneFiresAlone, bool AnyFiresAlone);

/// <summary>A unit in the target Location as the state finds it (A8.1, A7.308, A15.41): its id, whether it is active, its side, whether it is a vehicle, a Dummy, has a catalog definition, and is among the moving stack (null when no stack moves there).</summary>
public sealed record TargetUnitStateFacts(string Id, bool Active, string? Side, bool Vehicle, bool Dummy, bool HasDefinition, bool? Mover);

/// <summary>The target Location's units sorted into the attack: the targets, the vehicles, the companions, or the refusal.</summary>
public sealed record TargetSelection(IReadOnlyList<string> Targets, IReadOnlyList<string> Vehicles, IReadOnlyList<string> Companions, string? Refusal);

/// <summary>A weapon a firer names (A7.35, A9.8, A22.3): its id, whether the firer possesses it as an active weapon, whether it is dismantled, and whether it is a FT.</summary>
public sealed record NamedWeaponFacts(string Id, bool PossessedByFirer, bool Dismantled, bool Ft);

/// <summary>A firer's named weapons against the limits on them (A7.35, A9.8, A22.3, A7.351; rulings R13.6, R12.4): the firer, its weapons, whether it has used a FT or DC this Player Turn, whether it fires a MG alone, and two reads made only when asked: whether another weapon it possesses has fired or is First Fire marked, and whether it has used a SW this phase.</summary>
public sealed record NamedWeaponsFacts(string FirerId, IReadOnlyList<NamedWeaponFacts> Named, bool AssaultWeaponUser, bool FiresAlone, Func<bool> OtherPossessedFired, Func<bool> SupportWeaponUsed);

/// <summary>A firer against the SW limits of a phase (A3.3, A7.1, A7.351): the phase, its Bounding Fire counter, whether it is a squad, fires a MG alone, has fired, names any weapon; and two reads made only when asked: whether a SW it possesses is marked Prep Fire, and how many other weapons it possesses have fired or are First Fire marked.</summary>
public sealed record FirerLimitFacts(string Id, string? Phase, bool BoundingFire, bool Squad, bool FiresAlone, bool Fired, bool NamesWeapons, Func<bool> PrepFiredWeaponHeld, Func<int> OtherFired);

/// <summary>A leader's MG partner as the state finds it (A9.12; ruling R12.4): the named id, whether it is an active leader or hero of the leader's side in his Location, not a firer or director, unfired, and unbroken.</summary>
public sealed record PartnerFacts(string Id, bool Active, bool LeaderOrHero, bool SameSide, bool SameLocation, bool FirerOrDirector, bool Fired, bool Broken);

/// <summary>An Infantry firer's facts for the record (A7.351, A7.352, A7.25, A22.611, A19.2; rulings R12.1, R15.4, R15.10): its conditions, the phase, whether its only fire this phase is one SW use, whether it fired a Gun as a non-squad, whether it fires a MG alone or is a leader, its partner, whether it makes the MOL Check, and its Green Inexperience.</summary>
public sealed record FirerRecordFacts(
    string Id,
    string Definition,
    string At,
    bool Broken,
    bool Pinned,
    bool Concealed,
    bool Fired,
    bool NamesWeapons,
    int? Elr,
    bool FirstFire,
    bool FinalFire,
    string? Phase,
    bool SwOnly,
    bool GunCrewFired,
    bool Squad,
    bool Leader,
    bool FiresAlone,
    IReadOnlyList<FireWeapon>? Weapons,
    bool Fanatic,
    bool Wounded,
    bool Cx,
    bool BoundingFire,
    bool Encircled,
    string? Partner,
    bool Mol,
    bool? Inexperienced);

/// <summary>A directing leader's facts for the record (A7.531): its conditions and whether it directed a SW use this phase.</summary>
public sealed record DirectorRecordFacts(string Id, string Definition, string At, bool Broken, bool Pinned, bool Concealed, bool Fired, bool FirstFire, bool DirectedSupportWeapon, bool Wounded, bool Cx);

/// <summary>A target of the attack with what the firing side's relation adds (rulings R12.8, R12.9, R12.11): whether it is of the firing side, a prisoner's custodian, and whether it is Encircled.</summary>
public sealed record AttackTargetFacts(FireTarget Target, bool SameSide, bool Captured, string? Custodian, bool Encircled);

/// <summary>The assembly of a fire group's attack (unit steps 18 to 23): the phase, the firing side and whether it is phasing, the group's Location, the target, the firers, the directors, the month, the targets, the target side's ELR, the fire kind, the Assault flag of the moving stack, the firing side's ELR, the vehicles, the firing vehicle, the sides under No Quarter, the firing nationalities, and the companions.</summary>
public sealed record AttackAssemblyFacts(
    string? Phase,
    bool Phasing,
    string FirerLocation,
    string Target,
    IReadOnlyList<FireFirer> Firers,
    IReadOnlyList<FireDirector> Directors,
    int? ScenarioMonth,
    IReadOnlyList<FireTarget> Targets,
    int? TargetSideElr,
    string? Kind,
    bool Assault,
    Func<int?> FiringSideElr,
    bool FriendlyTargets,
    IReadOnlyList<FireVehicle> Vehicles,
    FireVehicleFire? VehicleFire,
    bool TargetSideNoQuarter,
    bool FiringSideNoQuarter,
    IReadOnlyList<string>? FiringNationalities,
    IReadOnlyList<FireTarget> Companions);

/// <summary>A unit in a Location as a Residual FP attack or an OVR finds it (A8.2, A8.22, D7.1): its id, whether it is active, its side, whether it is a vehicle, has a catalog definition, is captured, and is among the moving stack.</summary>
public sealed record LocationUnitFacts(string Id, bool Active, string? Side, bool Vehicle, bool HasDefinition, bool Captured, bool Mover);

/// <summary>A Residual FP attack as the state has it (A8.2, A8.22, A8.222; ruling R6.6): whether the moving stack is in the target Location and moved by Assault Movement, the units there, the phasing side, its ELR and No Quarter, the month, the counter's FP, and the reads of a target and a vehicle, made for the units the attack takes.</summary>
public sealed record ResidualFacts(bool MovementAtTarget, bool Assault, IReadOnlyList<LocationUnitFacts> Units, string? PhasingSide, int? PhasingSideElr, bool PhasingSideNoQuarter, int? ScenarioMonth, int Fp, string Target, Func<string, FireTarget> TargetOf, Func<string, FireVehicle> VehicleOf);

/// <summary>An OVR as the state has it (D7.1, D7.11; ruling R11.11): the vehicle (found active or wrecked, a vehicle, on the map, with a definition), its id, definition, Location text, side, crew exposure, whether it is Immobilized, Bogged, or Wrecked, its weapons' malfunctions, the units in its Location, the enemy side's ELR and No Quarter, the month, and the reads of a target and a vehicle.</summary>
public sealed record OverrunFacts(
    bool VehicleFound,
    bool IsVehicle,
    bool OnMap,
    bool HasDefinition,
    string VehicleId,
    string? Definition,
    string At,
    string? Side,
    bool CrewExposed,
    bool Immobilized,
    bool Bogged,
    bool Wrecked,
    bool MainMalfunctioned,
    bool Disabled,
    bool BmgMalfunctioned,
    bool CmgMalfunctioned,
    IReadOnlyList<LocationUnitFacts> Units,
    int? TargetSideElr,
    bool TargetSideNoQuarter,
    int? ScenarioMonth,
    Func<string, FireTarget> TargetOf,
    Func<string, FireVehicle> VehicleOf);

/// <summary>A DC's attack as the state has it (A23; rulings R15.2, R15.3): the charge, its placement, its user, the mode, the units in the target Location, the sides, and the reads of a target and a vehicle.</summary>
public sealed record DemolitionChargeFacts(
    string ChargeId,
    bool ChargeActiveWithDefinition,
    string? ChargeDefinition,
    string Mode,
    bool PlacementOperable,
    string? PlacementUnit,
    bool? PlacementCx,
    bool? PlacementTargetsConcealed,
    string? PossessorId,
    bool UserFoundWithDefinition,
    string? UserDefinition,
    string? UserSide,
    bool UserCx,
    bool UserBoundingFire,
    bool? UserInexperienced,
    bool? Captured,
    string? UserLocation,
    bool Phasing,
    string? Phase,
    string Target,
    bool MovementAtTarget,
    bool Assault,
    IReadOnlyList<LocationUnitFacts> Units,
    string? TargetSide,
    int? TargetSideElr,
    bool TargetSideNoQuarter,
    int? UserSideElr,
    bool UserSideNoQuarter,
    IReadOnlyList<string>? FiringNationalities,
    int? ScenarioMonth,
    Func<string, FireTarget> TargetOf,
    Func<string, FireVehicle> VehicleOf);
