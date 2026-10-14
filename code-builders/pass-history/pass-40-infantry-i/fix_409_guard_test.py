"""Pass 40 task 40.9: the Guard squad's Deployment at will is tested where a Guard is made by play (a capture in CC), not by a staged condition."""
import re

ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests/"

path = ROOT + "BacklogPass13Tests.cs"
text = open(path, encoding="utf-8", newline="").read()
start = text.index("    [Fact]\n    public async Task AGuardSquadDeploysAtWillInAnyPhaseWithNoLeaderAndNoNtc()")
end = text.index("    [Fact]\n    public async Task TwoHalfSquadsRecombineWithTheirLeader()")
assert start < end
open(path, "w", encoding="utf-8", newline="").write(text[:start] + text[end:])

TEST = '''    [Fact]
    public async Task AGuardSquadDeploysAtWillInAnyPhaseWithNoLeaderAndNoNtc()
    {
        // Pass 40, task 40.9 (A20.5, p. 88): "An unbroken Guard squad can Deploy into HS automatically at any time, regardless of nationality or
        // leader requirements." g1 captures the HS in CC and, still in the CCPh, Deploys; a HS of it keeps the prisoner. g2 guards no one.
        await Setup("german", Unit("g1", "asl:squad", "attacker-squad", "bd01:B1:0", "german"), Unit("g2", "asl:squad", "attacker-squad", "bd01:B1:0", "german"),
            Unit("r1", "asl:half-squad", "defender-half-squad", "bd01:B1:0", "russian"));
        await Advance(7);
        Committed(await Do(GameActions.CloseCombat, Once(2, 3), new
        {
            location = "bd01:B1:0",
            attacks = new object[] { new { attackers = G1, defenders = R1, capture = true } },
        }));
        Assert.Equal("g1", Current.Unit("r1")!.Custodian);
        Assert.NotEqual("rph", Current.Phase);
        Assert.Contains((await Do(GameActions.Deploy, NoRoll(), new
        {
            squadId = "g2"
        })).Reasons, reason => reason.StartsWith("play.deploy-phase", StringComparison.Ordinal));
        Committed(await Do(GameActions.Deploy, NoRoll(), new
        {
            squadId = "g1",
            attemptId = "dep"
        }));
        Assert.Equal("asl:half-squad", Current.Unit("dep-g1-1")!.Kind);
        Assert.StartsWith("dep-g1-", Current.Unit("r1")!.Custodian, StringComparison.Ordinal);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ACaptureAttemptTakesAPrisonerWhomItsGuardCanHandOverOrAbandon()'''
path = ROOT + "BacklogPass14Tests.cs"
text = open(path, encoding="utf-8", newline="").read()
anchor = "    [Fact]\n    public async Task ACaptureAttemptTakesAPrisonerWhomItsGuardCanHandOverOrAbandon()"
assert text.count(anchor) == 1
assert re.search(r"private static readonly string\[\] R1 = ", text) and re.search(r"private static readonly string\[\] G1 = ", text)
open(path, "w", encoding="utf-8", newline="").write(text.replace(anchor, TEST))
print("ok")
