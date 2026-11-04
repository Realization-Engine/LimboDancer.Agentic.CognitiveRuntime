namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Raw A25.75/.77 facts. Date is YYYY-MM or not-applicable; origin mark is r, none or not-applicable.</summary>
public sealed record CapturedEquipmentUseFacts(string? UserNationality, bool? Personnel, string? PrintedClass, string? WeaponNationality,
    string? WeaponKind, string? OriginMark, string ScenarioDate, bool? PhysicallyCaptured);

/// <summary>Captured Use and E1.76 treatment only. Physical ownership, repair, qualification and ammunition remain separate.</summary>
public sealed record CapturedEquipmentUseVerdict(string Outcome, bool? CapturedUsePenalties, bool? NightCapturedMgPenalty,
    bool? PhysicallyCaptured, string? UseRefusal, string Reason);
