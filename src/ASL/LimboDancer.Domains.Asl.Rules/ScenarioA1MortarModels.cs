namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The facts of a mortar's shot that C9 decides (the C9 claim's design, ruling RC9.7): whether the weapon is a mortar, its caliber in mm and its
/// minimum range (null when not known), the hex range to the target (null when not read), whether the shot is on the Area Target Type, whether it
/// names a vehicle as a To Kill target, whether its firer is a lone SMC (one SMC firing without another, A7.5, A9.12), and whether that SMC is a
/// hero (A15.23, A21.13).
/// </summary>
public sealed record MortarShotFacts(bool Mortar, int? Caliber, int? MinimumRange, int? Range, bool AreaTargetType, bool NamesVehicle, bool LoneSmc,
    bool Hero = false);

/// <summary>
/// The verdict on a mortar's shot (C9.1, C9.2, C9.4, A21.13): the refusal, null when the shot may be made, with the rule and page it rests on;
/// whether it fires as a light mortar; and whether it keeps its Multiple ROF, before the other rules that lower it (C9.31's Spotter, C5.4's pin,
/// C5.2's AFPh).
/// </summary>
public sealed record MortarShotVerdict(string? Refusal, bool FiresAsLight, bool MultipleRofKept, string? RefusalRule = null, int? RefusalPage = null)
{
    public string Reference { get; } = "C9.1, C9.2, C9.4; A21.13";
}
