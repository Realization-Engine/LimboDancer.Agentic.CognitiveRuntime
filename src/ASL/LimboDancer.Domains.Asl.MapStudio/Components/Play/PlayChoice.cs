namespace LimboDancer.Domains.Asl.MapStudio.Components.Play;

/// <summary>
/// Pass 28b: one option of a Play panel's select, already read for the view (ruling R23.1): its value and the label a player reads, such as
/// "r1 (russian)".
/// </summary>
public sealed record PlayChoice(string Id, string Label);
