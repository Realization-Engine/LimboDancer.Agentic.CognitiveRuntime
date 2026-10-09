namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Concealment at setup (pass 32.j, S10; A12.12, A12.3, A12.32; rulings R23.5, R23.6): the non-OB "?" each side places once both have set up, hidden units
/// placed beneath a "?" in play, and the projector's record of a non-OB "?". Play reads the state and the map, hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1Concealment
{
    /// <summary>Why no non-OB "?" may be placed now (ruling R23.6): play has started, or a card's setup order is still open; null when they may.</summary>
    public static string? NonObConcealmentBar(bool setupClosed, Func<bool> setupOrderOpen)
    {
        ArgumentNullException.ThrowIfNull(setupOrderOpen);
        if (setupClosed)
        {
            return "play.non-ob-concealment: play has started; a non-OB \"?\" is placed only after both sides set up and before play (A12.12; ruling R23.6)";
        }

        return setupOrderOpen()
            ? "play.non-ob-concealment: a non-OB \"?\" is placed only after both sides have set up (A12.12; ruling R23.6)"
            : null;
    }
}
