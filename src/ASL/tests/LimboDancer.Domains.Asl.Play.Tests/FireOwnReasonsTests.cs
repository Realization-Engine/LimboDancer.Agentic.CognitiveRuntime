using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 31d (design D8; ruling R31d.5): which of a refusal's reasons the firing side reads where the target Location holds a unit it cannot see.
/// A reason that rests on its own group and the map alone is told (here a FT fired with another unit, A22.31, p. 89), and then nothing else is;
/// with no such reason it reads the one sentence. So what it reads depends on its own group alone, and never says "and something more".
/// </summary>
public sealed partial class FireTests
{
    private static readonly string[] FtGroup = ["r1", "r2"];
    private static readonly string[] FtOnly = ["ft"];

    /// <summary>The Russian PFPh: r1 with a FT, and r2, in D4; the German g1 concealed in the stone building E4.</summary>
    private async Task SetupFt(int? germanElr = 3)
    {
        Assert.Equal(PlayOutcome.Committed, (await Commit(Play(), GameActions.Setup, Args(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new
            {
                label = "A flamethrower in the village",
                catalog = "asl-scenario-a1@1.13.0",
                boards = Bd01,
                firstSide = "russian",
                scenarioMonth = 7,
                sides = new object[] { new { id = "german", nationality = "german", elr = germanElr }, new { id = "russian", nationality = "russian", elr = 2 } },
            },
            placements = new object[]
            {
                Placement("r1", "asl:squad", "defender-squad", "bd01:D4:0", "russian"),
                Placement("r2", "asl:squad", "defender-squad", "bd01:D4:0", "russian"),
                new Dictionary<string, object>
                {
                    ["id"] = "ft",
                    ["kind"] = "asl:ft",
                    ["definition"] = "attacker-ft",
                    ["side"] = "russian",
                    ["holding"] = new
                    {
                        holder = "r1",
                        role = "possessed"
                    },
                    ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
                },
                Placement("g1", "asl:squad", "attacker-squad", "bd01:E4:0", "german", concealed: true),
                Placement("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german"),
            },
        }))).Outcome);
        await Advance();
        Assert.Equal("pfph", Current.Phase);
    }

    private Task<PlayResult> FireWithFt() => Commit(Play(NoRoll()), GameActions.Fire, Args(new
    {
        gameId = Scope.Game,
        attemptId = "fire-ft",
        expectedRevision = Revision,
        firers = FtGroup,
        target = "bd01:E4:0",
        weapons = new Dictionary<string, string[]> { ["r1"] = FtOnly },
    }));

    [Fact]
    public async Task AFtFiredWithAnotherUnitAtAConcealedStackIsToldAsTheGroupsOwnFault()
    {
        await SetupFt();
        var revision = Revision;
        var result = await FireWithFt();
        Assert.Equal(PlayOutcome.Denied, result.Outcome);
        Assert.Equal(revision, Revision);

        // Before pass 31d the firing side read only "the Fire package does not decide every outcome ... for reasons about units the firing side
        // cannot see", though the reason was its own group's.
        var russian = result.Plan!.Fire!.ReasonsFor(result.Reasons, Current.Perspectives.Single(item => item.Name == "russian"));
        Assert.DoesNotContain(FireProposal.Undisclosed, russian);
        Assert.Contains(russian, reason => reason.StartsWith("asl.a1.fire.flamethrower-outside", StringComparison.Ordinal));
        Assert.All(russian.Skip(1), reason => Assert.DoesNotContain("g1", reason, StringComparison.Ordinal));
    }

    [Fact]
    public async Task WithAReasonOfItsOwnGroupTheFiringSideReadsThatAndNoWordOfTheTarget()
    {
        // The German ELR is not declared, which alone would leave the attack on the concealed g1 undecided, a reason about a unit the firing side
        // cannot see. The group's own fault is what the firing side reads: no reason of the target's, and not the sentence that says there is one.
        await SetupFt(germanElr: null);
        var result = await FireWithFt();
        Assert.Equal(PlayOutcome.Denied, result.Outcome);
        var fire = result.Plan!.Fire!;
        var russian = fire.ReasonsFor(result.Reasons, Current.Perspectives.Single(item => item.Name == "russian"));
        Assert.Contains(russian, reason => reason.StartsWith("asl.a1.fire.flamethrower-outside", StringComparison.Ordinal));
        Assert.DoesNotContain(FireProposal.Undisclosed, russian);
        Assert.DoesNotContain(russian, reason => reason.Contains("elr", StringComparison.OrdinalIgnoreCase) || reason.Contains("g1", StringComparison.Ordinal));

        // Every reason the firing side reads is one the adjudicator reads too: it is told less, never something else.
        Assert.Subset(fire.ReasonsFor(result.Reasons, Perspective.Adjudicator).Skip(1).ToHashSet(StringComparer.Ordinal), russian.Skip(1).ToHashSet(StringComparer.Ordinal));
    }

    [Fact]
    public async Task WithNoReasonOfItsOwnGroupTheFiringSideReadsTheOneSentence()
    {
        // The same target and the same undeclared ELR, and a group with nothing wrong in it: the squads alone.
        await SetupFt(germanElr: null);
        var result = await Commit(Play(NoRoll()), GameActions.Fire, Args(new
        {
            gameId = Scope.Game,
            attemptId = "fire-squads",
            expectedRevision = Revision,
            firers = FtGroup,
            target = "bd01:E4:0",
        }));
        Assert.Equal(PlayOutcome.Denied, result.Outcome);
        Assert.Equal([FireProposal.Undisclosed], result.Plan!.Fire!.ReasonsFor(result.Reasons, Current.Perspectives.Single(item => item.Name == "russian")));
    }
}
