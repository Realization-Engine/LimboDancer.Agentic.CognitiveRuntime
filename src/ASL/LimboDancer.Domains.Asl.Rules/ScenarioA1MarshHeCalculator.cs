namespace LimboDancer.Domains.Asl.Rules;

/// <summary>B16.31's HE FP multiplier, with the narrow B16.6-B16.8 inheritance (p. 130). No final rounding, weapon eligibility or map traversal.</summary>
public static class ScenarioA1MarshHeCalculator
{
    /// <summary>Evaluate one effect Location/target, independently of CH, Area Fire, and a thrown DC's other effect Location.</summary>
    public static MarshHeVerdict Evaluate(MarshHeFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (Invalid(facts.Effect) || Invalid(facts.Terrain) || Invalid(facts.SnowCover) || Invalid(facts.Target))
        {
            return Unknown("A supplied marsh fact has an unsupported value.");
        }

        if (facts.BridgePresent == false && (facts.Target == MarshHeTarget.Bridge || facts.OnBridge == true)
            || facts.OnBridge == true && facts.Target is MarshHeTarget.Bridge or MarshHeTarget.GroundLocation)
        {
            return Unknown("The bridge, target kind and on-bridge facts contradict one another.");
        }

        if (facts.Effect is MarshHeEffect.NonHe or MarshHeEffect.Residual)
        {
            return Factor(1m, "This is not an HE component; Residual originating from HE is not halved again.");
        }

        if (facts.Effect == MarshHeEffect.UnsupportedVtFfe)
        {
            return Unknown("VT FFE needs its own special profile, not ordinary marsh halving.", "W.7B", 656);
        }

        if (facts.Terrain == MarshHeTerrain.Swamp)
        {
            return Unknown("Swamp has a separate HE reading; ordinary marsh halving is not assumed.", "G7.2", 285);
        }

        if (facts.Effect is null || facts.Terrain is null)
        {
            return Unknown("The effect family and terrain kind must be known.");
        }

        if (facts.Terrain == MarshHeTerrain.Other)
        {
            return Factor(1m, "The effect is not into marsh or mudflat.");
        }

        if (facts.SnowCover is MarshHeSnow.Snow or MarshHeSnow.DeepSnow || facts.SubZero == true)
        {
            return Factor(1m, "Scenario-defined Snow, Deep Snow or sub-zero temperature removes marsh treatment.", "B16.8");
        }

        if (facts.ConnectedToFloodedWater == true)
        {
            return Factor(1m, "Connected flooded marsh is treated as stream/river instead.", "B16.6");
        }

        if (facts.BridgePresent == true && (facts.Target == MarshHeTarget.Bridge || facts.Target == MarshHeTarget.Unit && facts.OnBridge == true))
        {
            return Factor(1m, "An attack against the bridge or a unit on it is exempt.");
        }

        if (facts.SnowCover is null || facts.SubZero is null || facts.ConnectedToFloodedWater is null
            || facts.BridgePresent != false && facts.Target != MarshHeTarget.GroundLocation
                && !(facts.Target == MarshHeTarget.Unit && facts.OnBridge == false))
        {
            return Unknown("A weather, flooding or bridge-position fact that can change the multiplier is unknown.");
        }

        return Factor(0.5m, "HE into marsh is halved; mudflat inherits this rule (B16.7). Other FP modifiers and final rounding remain separate.");
    }

    /// <summary>
    /// Compatibility only: the old ordnance hit divisor, with identical decimal representation and null handling. Current-reading callers use
    /// Evaluate with full facts. Preserve the existing multiplication/division order; multiplying by 0.5 can change recorded decimal scale.
    /// </summary>
    public static decimal LegacyOrdnanceDivisor(string? terrain) => terrain == "marsh" ? 2m : 1m;

    private static bool Invalid<T>(T? value) where T : struct, Enum => value is { } known && !Enum.IsDefined(known);

    private static MarshHeVerdict Unknown(string reason, string rule = "B16.31", int page = 130) =>
        new(MarshHeDisposition.NotEvaluated, null, reason, rule, page);

    private static MarshHeVerdict Factor(decimal factor, string reason, string rule = "B16.31") =>
        new(MarshHeDisposition.Evaluated, factor, reason, rule, 130);
}
