using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 35, the Infantry increment, read directly in Rules: Good Order as A.7 has it and the scans that read it (task 35.3), the Morale Level
/// ceiling of A.18 (task 35.3), and the Russian Conscript's Battle Hardening (task 35.8). The wound procedure of task 35.1 is tested with the rout.
/// </summary>
public sealed class ScenarioA1Pass35RulesTests
{
    [Fact]
    public void ARussianConscriptBattleHardensToTheFiveTwoSeven()
    {
        // A25.2 (p. 93): "A 4-2-6 squad Battle Hardens to a 5-2-7", and its 2-2-6 HS to the 5-2-7's own 2-2-7; the 2-2-7 to a 3-2-8.
        Assert.Equal("defender-line-squad", ScenarioA1FireReference.HardenedOf("defender-conscript-squad"));
        Assert.Equal("defender-line-half-squad", ScenarioA1FireReference.HardenedOf("defender-conscript-half-squad"));
        Assert.Equal("defender-guards-half-squad", ScenarioA1FireReference.HardenedOf("defender-line-half-squad"));
    }
}
