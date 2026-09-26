using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Each side's view of a fire attack (Fire in Live Play, part 5): rolls are public; a concealed target left concealed is
/// never identified to the firing side, which learns the arithmetic from a public report; any other result reveals it
/// (A12.14, p. 77); a concealed firer is revealed by firing; and a refusal over a unit the firing side cannot see tells
/// that side only that the attack is undecided.
/// </summary>
public sealed partial class FireTests
{
    private static readonly string[] GermanTargets = ["g1", "gh"];

    private (GameView View, IReadOnlyList<GameEvent> Events) ViewOf(string perspective)
    {
        var history = Planner().Replay(store.Read(Scope)!.Events);
        var state = history.Current!;
        var viewer = state.Perspectives.Single(item => item.Name == perspective);
        return (GameView.Of(history, state.Revision, viewer), history.EventsFor(viewer, state.Revision));
    }

    private IReadOnlyList<GameEvent> Since(long revision) => [.. store.Read(Scope)!.Events.Skip((int)revision)];

    /// <summary>Whether the events, as written, name a unit anywhere: in an id, a roll key, the facts, or the resolution.</summary>
    private static bool Names(IReadOnlyList<GameEvent> events, string unit) =>
        Regex.IsMatch(GameEventWriter.Write(Scope, new GameRecord("view", false, events)), $@"\b{Regex.Escape(unit)}\b");

    [Fact]
    public async Task EachSideSeesAKnownTargetBrokenAndTheFirersMarked()
    {
        await Setup();
        var before = Revision;
        Assert.Equal(PlayOutcome.Committed, (await Commit(Play(Once(3, 4, 2, 3, 4, 4)), GameActions.Fire, Fire("fire-1", ["r1", "r2"]))).Outcome);
        Assert.All(Since(before), item => Assert.Null(item.Visibility));
        foreach (var perspective in new[] { "german", "russian", Perspective.AdjudicatorName })
        {
            var (view, _) = ViewOf(perspective);
            Assert.Equal(ConditionState.True, GameState.Condition(view.Units.Single(unit => unit.Id == "gh"), Conditions.Broken));
            Assert.All(FireGroup, id => Assert.Equal(ConditionState.True, GameState.Condition(view.Units.Single(unit => unit.Id == id), Conditions.PrepFire)));
        }
    }

    [Fact]
    public async Task AConcealedTargetLeftConcealedIsNeverIdentifiedToTheFiringSide()
    {
        await Setup(concealed: GermanTargets);
        var before = Revision;

        // IFT 6+6 = 12, +3 in the stone building: no effect, so g1 and gh keep "?".
        Assert.Equal(PlayOutcome.Committed, (await Commit(Play(Once(6, 6)), GameActions.Fire, Fire("fire-1", ["r1", "r2"]))).Outcome);
        var added = Since(before);
        Assert.Equal(["dice-rolled", "fire-resolved", "fire-reported", "conditions-changed", "conditions-changed", "conditions-changed"],
            added.Select(item => item.Type));
        Assert.Null(added[0].Visibility);
        Assert.Equal(["german"], added[1].Visibility);

        var (russian, seen) = ViewOf("russian");
        Assert.DoesNotContain(russian.Units, unit => unit.Side == "german" && unit.Id != "g2");
        Assert.Equal(2, russian.Sealed.Count(item => item.Location.ToString() == "bd01:E4:0"));
        Assert.All(GermanTargets, id => Assert.False(Names(seen, id), $"The Russian events name {id}."));
        Assert.DoesNotContain(seen, item => item.Payload is FireResolved);

        // The firing side learns the arithmetic from the public report, as it would at the table.
        var report = Assert.Single(seen.Select(item => item.Payload).OfType<FireReported>());
        Assert.Equal(("bd01:D4:0", "bd01:E4:0", "none", 15), (report.FirerLocation, report.TargetLocation,
            report.Arithmetic.GetProperty("result").GetString(), report.Arithmetic.GetProperty("finalDr").GetInt32()));
        Assert.Contains(seen, item => item.Payload is DiceRolled { Purpose: "fire-ift" });

        var (german, record) = ViewOf("german");
        Assert.Single(record.Select(item => item.Payload).OfType<FireResolved>());
        Assert.All(GermanTargets, id => Assert.Equal(ConditionState.True, GameState.Condition(german.Units.Single(unit => unit.Id == id), Conditions.Concealed)));
    }

    [Fact]
    public async Task AConcealedTargetIsRevealedByAnyOtherResult()
    {
        await Setup(concealed: GermanTargets);
        var before = Revision;

        // IFT 1+1 = 2, +3: an effect on the 8 column (Area Fire at concealed units), so every target loses "?"; every
        // later roll is 1 and passes.
        var result = await Commit(Play(Once([1, 1, .. Enumerable.Repeat(1, 12)])), GameActions.Fire, Fire("fire-1", ["r1", "r2"]));
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
        var added = Since(before);
        var fire = Assert.Single(added, item => item.Payload is FireResolved);
        Assert.Null(fire.Visibility);
        Assert.NotEqual("none", ((FireResolved)fire.Payload).Resolution.GetProperty("arithmetic").GetProperty("result").GetString());
        Assert.DoesNotContain(added, item => item.Payload is FireReported);

        var (russian, _) = ViewOf("russian");
        Assert.DoesNotContain(russian.Sealed, item => item.Location.ToString() == "bd01:E4:0");
        Assert.Contains(russian.Units, unit => unit.Side == "german" && russian.Locations.TryGetValue(unit.Id, out var at) && at.Location.ToString() == "bd01:E4:0");
    }

    [Fact]
    public async Task AConcealedFirerAndDirectorAreRevealedByFiring()
    {
        await Setup(concealed: FireGroup);
        Assert.Equal(3, ViewOf("german").View.Sealed.Count(item => item.Location.ToString() == "bd01:D4:0"));

        Assert.Equal(PlayOutcome.Committed, (await Commit(Play(Once(6, 6)), GameActions.Fire, Fire("fire-1", ["r1", "r2"]))).Outcome);
        var (german, _) = ViewOf("german");
        Assert.DoesNotContain(german.Sealed, item => item.Location.ToString() == "bd01:D4:0");
        Assert.All(FireGroup, id => Assert.Equal(ConditionState.True, GameState.Condition(german.Units.Single(unit => unit.Id == id), Conditions.PrepFire)));
    }

    [Fact]
    public async Task ARefusalOverAHiddenTargetTellsTheFiringSideOnlyThatTheAttackIsUndecided()
    {
        await Setup(hidden: ["g1"]);
        var revision = Revision;
        var result = await Commit(Play(NoRoll()), GameActions.Fire, Fire("fire-1", ["r1", "r2"]));
        Assert.Equal(PlayOutcome.Denied, result.Outcome);
        Assert.Equal(revision, Revision);

        // The known limitation (step 18, part 5): the firing side learns that something there is undecided, not what.
        var fire = result.Plan!.Fire!;
        Assert.Equal(("russian", "german"), (fire.FiringSide, fire.TargetSide));
        Assert.Equal([FireProposal.Undisclosed], fire.ReasonsFor(result.Reasons, Current.Perspectives.Single(item => item.Name == "russian")));
        Assert.Contains(fire.ReasonsFor(result.Reasons, Perspective.Adjudicator), reason => reason.Contains("concealment-unreviewed", StringComparison.Ordinal));
        Assert.Contains(fire.ReasonsFor(result.Reasons, Current.Perspectives.Single(item => item.Name == "german")),
            reason => reason.Contains("concealment-unreviewed", StringComparison.Ordinal));
    }
}
