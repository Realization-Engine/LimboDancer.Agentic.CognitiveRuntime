using System.Text;
using ASL.Turns;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

public sealed partial class PlayTests
{
    private GamePlanner WorkAcceptanceCopy(GameScope scope, IReadOnlyList<GameEvent> events)
    {
        var copied = new FileGameStore(Path.Combine(root, Guid.NewGuid().ToString("N")));
        var reader = new GamePlanner(copied, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog, Synthetic]);
        Assert.Equal(AppendStatus.Committed, copied.Append(scope, "work acceptance", 0, events, reader.Replay).Status);
        return reader;
    }

    [Theory]
    [MemberData(nameof(TurnGuideAcceptanceCases.Profiles), MemberType = typeof(TurnGuideAcceptanceCases))]
    public async Task AcceptanceProfilesKeepCompletedEffectsAdjudicatedAndLegacyWorkExplicit(string profile)
    {
        var play = await PackingEffectsGame();
        var scope = Current().Scope;
        var pack = new TurnWorkDeclaration("pack", "german", "gun", ["gun", "g1"], TurnWorkKind.AnimalPacking, null, 1, "G10 adjudicated assembly")
        {
            CompletionEffects = new(TurnWorkEffectKind.PackGun, Crew: "g1")
        };
        Assert.Equal(PlayOutcome.Committed, (await WorkCommit(play, EffectsArgs("pack", "accept-start", "start", pack))).Outcome);
        var startAt = Current().Revision;
        var startEvents = store.Read(scope)!.Events;
        var legacy = startEvents.Select(item => item.Payload is TurnWorkRecorded ? item with { Type = TurnWorkRecorded.EffectsType } : item).ToArray();
        var legacyReader = WorkAcceptanceCopy(scope, legacy);
        foreach (var viewer in new[] { Perspective.Side("german"), Perspective.Side("russian"), Perspective.Adjudicator })
        {
            var guide = await legacyReader.ReadTurnGuideProfileAsync(scope, viewer, "rph", profile);
            TurnGuideAcceptanceCases.AssertProfile(guide, profile);
            if (profile == "existing-engine-work-v1" && viewer.Name != "russian")
            {
                Assert.Equal(0, Assert.Single(guide.Work).HostAuditVersion);
                Assert.Equal(TurnSupport.Pending, guide.Work[0].RuleAutomation);
            }
            else
            {
                Assert.Empty(guide.Work);
            }
        }
        await WorkPhases(play, 16);
        var complete = EffectsArgs("pack", "accept-complete", "complete");
        Assert.Equal(PlayOutcome.Committed, (await WorkCommit(play, complete)).Outcome);
        var finalAt = Current().Revision;
        Assert.Equal(PlayOutcome.Replay, (await WorkCommit(play, complete)).Outcome);
        Assert.Equal(finalAt, Current().Revision);
        Assert.Contains("gun", Current().PackedGuns);
        var journal = GameEventWriter.Write(scope, store.Read(scope)!);
        var restored = GameEventReader.Read(Encoding.UTF8.GetBytes(journal)).Record!;
        Assert.Equal(journal, GameEventWriter.Write(scope, restored));
        var reader = WorkAcceptanceCopy(scope, restored.Events);
        var prefix = WorkAcceptanceCopy(scope, restored.Events.Take((int)startAt).ToArray());
        foreach (var viewer in new[] { Perspective.Side("german"), Perspective.Side("russian"), Perspective.Adjudicator })
        {
            var guide = await Planner().ReadTurnGuideProfileAsync(scope, viewer, "rph", profile);
            TurnGuideAcceptanceCases.AssertProfile(guide, profile);
            Assert.Equal(TurnGuideAcceptanceCases.Json(guide), TurnGuideAcceptanceCases.Json(await reader.ReadTurnGuideProfileAsync(scope, viewer, "rph", profile)));
            Assert.Equal(TurnGuideAcceptanceCases.Json(guide), TurnGuideAcceptanceCases.Json(await Planner().ReadTurnGuideProfileAsync(scope, viewer, "rph", profile)));
            var historical = await reader.ReadTurnGuideProfileAsync(scope, viewer, "rph", profile, startAt);
            TurnGuideAcceptanceCases.AssertProfile(historical, profile);
            Assert.Equal(TurnGuideAcceptanceCases.Json(historical), TurnGuideAcceptanceCases.Json(await prefix.ReadTurnGuideProfileAsync(scope, viewer, "rph", profile, startAt)));
            if (profile == "existing-engine-work-v1" && viewer.Name != "russian")
            {
                Assert.Equal(TurnWorkEffectStatus.Applied, Assert.Single(guide.Work).EffectStatus);
                Assert.Equal(1, guide.Work[0].HostAuditVersion);
                Assert.NotEqual(TurnWorkEffectStatus.Applied, Assert.Single(historical.Work).EffectStatus);
            }
            else
            {
                Assert.Empty(guide.Work);
            }
        }
        var hidden = restored.Events.Select(item => item.Payload is TurnWorkRecorded ? item with { Visibility = ["russian"] } : item).ToArray();
        var hiddenReader = WorkAcceptanceCopy(scope, hidden);
        Assert.Empty((await hiddenReader.ReadTurnGuideProfileAsync(scope, Perspective.Side("german"), "rph", profile)).Work);
        var changedPrivate = restored.Events.Select(item => item.Payload is TurnWorkRecorded work ? item with
        {
            Payload = work with
            {
                RuleBasis = "private alternative ruling"
            }
        } : item).ToArray();
        var changedReader = WorkAcceptanceCopy(scope, changedPrivate);
        Assert.Equal(TurnGuideAcceptanceCases.Json(await reader.ReadTurnGuideProfileAsync(scope, Perspective.Side("russian"), "rph", profile)),
            TurnGuideAcceptanceCases.Json(await changedReader.ReadTurnGuideProfileAsync(scope, Perspective.Side("russian"), "rph", profile)));
        Assert.Equal(finalAt, Current().Revision);
    }
}
