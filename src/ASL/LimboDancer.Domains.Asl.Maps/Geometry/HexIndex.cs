namespace LimboDancer.Domains.Asl.Maps.Geometry;

/// <summary>
/// A zero-based hex grid index: column from the left edge and row within the column,
/// matching VASL's <c>hexGrid[col][row]</c>.
/// </summary>
public readonly record struct HexIndex(int Column, int Row);
