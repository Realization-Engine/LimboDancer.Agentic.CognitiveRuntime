namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Where a firer is relative to the Factory of the target's Location (B23.741, B23.743, pp. 139 and 140): a ground Location of the same Factory, a
/// rubbled hex of it, its rooftop, or outside it.
/// </summary>
public enum FactoryFirerPlace
{
    Inside, Rubble, Rooftop, Outside
}

/// <summary>
/// How a moving unit entered a Factory Location, for the TEM of Residual FP there (B23.741, p. 140): across a building hexside, across a non-building
/// hexside, or by Bypass in the hex.
/// </summary>
public enum FactoryResidualEntry
{
    BuildingHexside, NonBuildingHexside, Bypass
}

/// <summary>
/// The facts of one attack on a ground-level Factory Location (B23.741, B23.743; the B23.74 claim, ruling RB23F.6). <paramref name="Stone"/>: the
/// building's material. <paramref name="Firer"/>: where the firer is, read for direct fire only. <paramref name="TracedWithinDepiction"/>: the LOS is
/// traced entirely within the building depiction (a map reading). <paramref name="OverAdjacentRubbleEnteringAcrossDepiction"/>: from outside, the LOS is
/// over an adjacent rubble hex of the Factory and crosses the depiction while entering the target Location (a map reading).
/// <paramref name="ResidualEntry"/>: how the moving unit entered, for Residual FP. A null fact is unknown.
/// </summary>
public sealed record FactoryTemFacts(bool Stone, FactoryFirerPlace? Firer, bool? TracedWithinDepiction, bool? OverAdjacentRubbleEnteringAcrossDepiction,
    bool IndirectFire, bool Roofless, bool Residual, FactoryResidualEntry? ResidualEntry);

/// <summary>
/// The TEM an attack meets in a Factory Location (B23.741): <paramref name="Tem"/>, null when not evaluated; <paramref name="FactoryTem"/>, true when it is
/// the Factory's +1 rather than the building's; and the reason, citing the rule.
/// </summary>
public sealed record FactoryTemVerdict(int? Tem, bool FactoryTem, string Reason);

/// <summary>
/// The facts of one step that touches a Factory (B23.742, B23.743, pp. 139 and 140). <paramref name="FromFactory"/>: it starts in a Factory hex;
/// <paramref name="FromRubbleOfFactory"/>: it starts in a rubbled hex of the Factory it enters; <paramref name="ToFactory"/>: it ends in a Factory hex;
/// <paramref name="SameFactory"/>: both ends belong to one Factory; <paramref name="AcrossFactoryHexside"/>: the hexside crossed is a Factory hexside of
/// that building; <paramref name="VehicularEntrance"/>: the Factory Location the step enters from outside, or leaves to the outside, has a printed
/// stairwell, or the step crosses a road hexside; <paramref name="EntranceRoofless"/> and <paramref name="EntranceFriendlyControlled"/>: that
/// Location is Roofless, and its hex is controlled by the mover's side. Every one is a map or state reading Play hands over.
/// </summary>
public sealed record FactoryStepFacts(bool FromFactory, bool ToFactory, bool SameFactory, bool FromRubbleOfFactory, bool AcrossFactoryHexside,
    bool VehicularEntrance, bool EntranceRoofless, bool EntranceFriendlyControlled);

/// <summary>
/// The facts of a vehicle's step that touches a Factory (B23.742, B23.41): its movement type (<c>fully-tracked</c>, <c>half-tracked</c>,
/// <c>truck</c>), whether it is an AFV, its printed MP allotment, the building's material, the step, and whether it Bypasses.
/// </summary>
public sealed record FactoryVehicleStepFacts(string? MovementType, bool Afv, int MpAllotment, bool Stone, FactoryStepFacts Step, bool Bypass);

/// <summary>
/// The verdict of a vehicle's step that touches a Factory (B23.742): the cost in half MP, null when the step is not the Factory's case (the normal entry
/// or Bypass applies) or is refused; the Bog Check it requires and its Factory or building DRM; whether a rubble check is made before the next hex
/// (B23.41); and the refusal, citing the rule, or null.
/// </summary>
public sealed record FactoryVehicleStepVerdict(int? HalfMp, bool BogCheck, int? BogDrm, bool RubbleCheck, string? Refusal);
