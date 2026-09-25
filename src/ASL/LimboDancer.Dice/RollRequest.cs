namespace LimboDancer.Dice;

/// <summary>A request for Count dice, each with Sides faces.</summary>
public sealed record RollRequest(int Count, int Sides);
