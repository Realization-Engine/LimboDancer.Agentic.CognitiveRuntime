namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// A package's refusal codes as a player reads them (backlog, section 13): each code keeps its name and gains a sentence with its
/// rule, and the headline says whether the package refuses the action or only cannot decide it.
/// </summary>
public sealed class RefusalReasonsTests
{
    [Fact]
    public void ARefusedAttackIsExplainedCodeByCodeUnderARefusalHeadline()
    {
        var reasons = RefusalReasons.Refusal("play.fire-refused", "Fire", "attack", ["asl.a1.fire.firer-already-fired", "asl.a1.fire.levels-differ"]);

        Assert.Equal([
            "play.fire-refused: the Fire package refuses this attack as proposed",
            "asl.a1.fire.firer-already-fired: a firer has already fired this phase or Player Turn (A7.1, A8.1, A8.4)",
            "asl.a1.fire.levels-differ: firer and target at different levels are not reviewed",
        ], reasons);
    }

    [Fact]
    public void OnlyUndecidedCodesKeepTheUndecidedHeadline()
    {
        var reasons = RefusalReasons.Refusal("play.fire-refused", "Fire", "attack", ["asl.a1.fire.hindrance-unattributed", "asl.a1.fire.fact-missing:targets[0].inexperienced"]);

        Assert.Equal("play.fire-refused: the Fire package does not decide every outcome of this attack", reasons[0]);
        Assert.Equal("asl.a1.fire.fact-missing:targets[0].inexperienced: the game did not supply a fact the package needs", reasons[2]);
    }

    [Theory]
    [InlineData("asl.a1.cc.round-outside:0", "asl.a1.cc.round-outside:0: after an Ambush the ambusher's attacks come first, and only then the ambushed side's round (A11.3, A11.32)")]
    [InlineData("asl.a1.ordnance.gun-already-fired", "asl.a1.ordnance.gun-already-fired: the Gun has fired and did not keep its Multiple ROF (C2.24)")]
    [InlineData("asl.a1.rally.already-attempted", "asl.a1.rally.already-attempted: each unit attempts to rally once per Player Turn (A10.6)")]
    [InlineData("asl.a1.unknown.code", "asl.a1.unknown.code")]
    public void ACodeKeepsItsNameAndDetailAndGainsItsSentence(string code, string expected) => Assert.Equal(expected, RefusalReasons.Explain(code));
}
