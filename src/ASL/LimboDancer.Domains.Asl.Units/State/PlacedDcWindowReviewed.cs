using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>Adjudication for one charge at one journal state. Any subsequent event expires it.</summary>
public sealed record PlacedDcWindowReviewed(TurnPlacedDcReview Review) : EventPayload;
