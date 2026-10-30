namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A unit's national CCV case (the A11.5 claim's design, ruling RA11C.4), a fact the caller gives: none; an Ethiopian MMC, one lower (A25.934, p. 98); a
/// Japanese T-H Hero, 5 (G1.4231, p. 279); a KW TACP, 3 (KW 9.11, p. 666). A Reduced-Strength Japanese or CPVA unit keeps its Full-Strength CCV (G1.64,
/// p. 281; KW 7.97, p. 666): the caller gives the Full-Strength kind. A swimmer has no CCV (E6.2, p. 235): <see cref="ScenarioA1CloseCombatValueCalculator.AttackerRefusal"/>.
/// </summary>
public enum NationalCcv
{
    None, Ethiopian, TankHunterHero, Tacp
}

/// <summary>
/// The facts of an attack on a vehicle that set its Kill Number, the attackers' CCV (A11.5, p. 74; ruling RA11C.1): the main attacking unit, its kind,
/// whether it is an Assault Engineer (<see cref="ScenarioA1CloseCombatValueCalculator.AssaultEngineer"/>) and Inexperienced; the SMC combining with it, or
/// failing one the leader an Original 2 created (A18.12); and each halving penalty, once per kind (ruling RA11C.2): pinned (A7.8), D7.213's First or
/// Final Fire counter in Reaction Fire, a concealed vehicle (A11.19), an outnumbered Guard (A20.52); and the main unit's national case.
/// </summary>
public sealed record CcvAttackFacts(
    string MainUnitId,
    string MainKind,
    bool AssaultEngineer,
    bool Inexperienced,
    string? CombiningSmcId,
    string? CreatedLeaderId,
    bool Pinned,
    bool ReactionFireMarked,
    bool VehicleConcealed,
    bool GuardOutnumbered,
    NationalCcv National = NationalCcv.None);

/// <summary>The attackers' CCV (A11.5): the main unit's base CCV, the modifiers in their order, and the CCV, which has no floor (ruling RA11C.2).</summary>
public sealed record CcvVerdict(int Base, IReadOnlyList<FireModifier> Modifiers, int Ccv);

/// <summary>
/// One defender of a vehicle's CC attack (A11.62, p. 76): its kind, whether it is a SMC, an Assault Engineer (ruling RA11C.3), Inexperienced, and pinned
/// (the p. 75 example: "3 CCV -1 pinned"), and its national case.
/// </summary>
public sealed record CcvDefenderFacts(string Kind, bool Smc, bool AssaultEngineer, bool Inexperienced, bool Pinned, NationalCcv National = NationalCcv.None);

/// <summary>
/// The facts a vehicle CC attack's <see cref="ScenarioA1VehicleCloseCombat.Resolve(VehicleCloseCombatFacts, ScenarioA1CloseCombatReference, CcvContext?)"/>
/// takes beside its recorded facts (ruling RA11C.5), so no recorded record gains a field: the units that are Assault Engineers, whether the vehicle is
/// concealed, the Guards whose US# is below their prisoners', each unit's national case, and the swimmers. A null collection is none. A context, even
/// an empty one, asks for the rule's reading in full: the Unarmed and swimming refusals, no leader's DRM for a berserk attacker, the result of
/// <see cref="ScenarioA1CloseCombatValueCalculator.VehicleResult"/>, and A11.62's floor of a defender's CCV as a DRM.
/// </summary>
public sealed record CcvContext(
    IReadOnlyCollection<string>? AssaultEngineers = null,
    bool VehicleConcealed = false,
    IReadOnlyCollection<string>? OutnumberedGuards = null,
    IReadOnlyDictionary<string, NationalCcv>? National = null,
    IReadOnlyCollection<string>? Swimming = null)
{
    /// <summary>Whether the unit is an Assault Engineer.</summary>
    public bool IsAssaultEngineer(string unitId) => AssaultEngineers?.Contains(unitId) == true;

    /// <summary>Whether the unit is a Guard whose US# is below its prisoners' (A20.52).</summary>
    public bool IsOutnumberedGuard(string unitId) => OutnumberedGuards?.Contains(unitId) == true;

    /// <summary>The unit's national case; none when not given.</summary>
    public NationalCcv NationalOf(string unitId) => National?.GetValueOrDefault(unitId) ?? NationalCcv.None;

    /// <summary>Whether the unit is swimming (E6.2).</summary>
    public bool IsSwimming(string unitId) => Swimming?.Contains(unitId) == true;
}
