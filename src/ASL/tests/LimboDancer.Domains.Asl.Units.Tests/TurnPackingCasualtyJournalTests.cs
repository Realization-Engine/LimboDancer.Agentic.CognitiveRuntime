using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

public sealed class TurnPackingCasualtyJournalTests
{
    private static readonly GameScope Scope = new(Guid.Parse("d71fe88d-cfcb-4722-9c58-253880c005ad"), "packing-casualty");
    private static readonly TurnPackingCasualtyOrigin Origin = new("german", new("mortar", "mule", "crew"), new("setup", 1),
        new(TurnPackingWeapon.Mortar76To82, TurnHorseSize.Squad, true, false, false, 0, TurnPackingRecovery.None, true, true));
    private static readonly TurnPackingCasualtyEntry First = new(1, "effect-1", 4, null, new("casualty-1", 2), new("die-1", 3),
        TurnPackingMuleLoss.CasualtyReduced, 3, TurnPackingUnloadTerrain.Allowed);
    private static readonly TurnPackingCasualtyEntry Second = new(1, "effect-2", 7, "effect-1", new("casualty-2", 5), new("die-2", 6),
        TurnPackingMuleLoss.Eliminated, 4, TurnPackingUnloadTerrain.Unknown);
    private static TurnPackingCasualtyHistory Reload(TurnPackingCasualtyOrigin origin, params TurnPackingCasualtyEntry[] entries)
    {
        var document = TurnPackingCasualtyJournal.Read(TurnPackingCasualtyJournal.Write(Scope, origin, entries), Scope);
        return TurnPackingCasualtyJournal.Replay(document.Origin, document.Entries);
    }
    private static string Json(TurnPackingCasualtyDocument document) => JsonSerializer.Serialize(document, JsonSerializerOptions.Web);

    [Fact]
    public void PrintedMortarHistoryReplaysCausalDiceAndEffectsWithoutLosingFullRecoveryOrAmmunition()
    {
        var first = Reload(Origin, First);
        var full = Reload(Origin, First, Second);
        Assert.Equal(Origin, full.Origin);
        Assert.Equal(2, full.Current.LowAmmoOccurrences);
        Assert.True(full.Current.Malfunctioned);
        Assert.False(full.Current.Loaded);
        Assert.False(full.Current.MuleAlive);
        Assert.Equal(TurnPackingRecovery.FullOrdinaryPeriod, full.Current.Recovery);
        Assert.Equal(First, full.Receipts[0].Entry);
        Assert.Equal(Second, full.Receipts[1].Entry);
        Assert.Equal(3, full.Receipts[0].Verdict.FinalDr);
        Assert.Equal(3, full.Receipts[1].Verdict.FinalDr);
        Assert.Equal(1, first.Current.LowAmmoOccurrences);
        Assert.True(first.Current.MuleAlive);
        Assert.Equal(0, Origin.State.LowAmmoOccurrences);
        var document = TurnPackingCasualtyJournal.Read(TurnPackingCasualtyJournal.Write(Scope, Origin, [First, Second]), Scope);
        Assert.Throws<NotSupportedException>(() => ((IList<TurnPackingCasualtyEntry>)document.Entries).Add(First));
    }

    [Fact]
    public void ExistingPeriodLinkSurvivesWithoutCreatingProgressAndUnknownAmmoStaysUnknown()
    {
        var origin = Origin with
        {
            ExistingPeriodId = "unpacking-turn-2",
            State = Origin.State with
            {
                Loaded = false,
                MulePackTi = true,
                Recovery = TurnPackingRecovery.ContinueExistingPeriod,
                LowAmmoOccurrences = null,
                Malfunctioned = null
            }
        };
        var history = Reload(origin, First, Second);
        Assert.Equal("unpacking-turn-2", history.Origin.ExistingPeriodId);
        Assert.Equal(TurnPackingRecovery.ContinueExistingPeriod, history.Current.Recovery);
        Assert.Null(history.Current.LowAmmoOccurrences);
        Assert.Null(history.Current.Malfunctioned);
        Assert.All(history.Receipts, item => Assert.True(item.Verdict.Effects!.AddLowAmmo));
        Assert.All(history.Receipts, item => Assert.False(item.Verdict.Effects!.UnloadWeapon));
        Assert.Equal(origin.State, history.Origin.State);
    }

    [Fact]
    public void AlteredSourcesReorderedReceiptsAndRepeatedDiceAreRejected()
    {
        TurnPackingCasualtyEntry[] invalid =
        [Second with { Version = 0 }, Second with { Version = 2 }, Second with { PreviousEventId = null },
            Second with { EventId = "" }, Second with { EventId = "setup" }, Second with { EventId = First.EventId },
            Second with { EventId = First.Roll.EventId }, Second with { Casualty = null! }, Second with { Roll = null! },
            Second with { Casualty = new("", 5) }, Second with { Roll = new("", 6) },
            Second with { Casualty = First.Casualty }, Second with { Roll = First.Roll },
            Second with { Roll = Second.Casualty }, Second with { EventId = Second.Roll.EventId },
            Second with { Casualty = Second.Casualty with { Revision = First.Revision } },
            Second with { Roll = Second.Roll with { Revision = Second.Casualty.Revision } },
            Second with { Revision = Second.Roll.Revision },
            Second with { MuleLoss = TurnPackingMuleLoss.CasualtyReduced }];
        foreach (var entry in invalid)
        {
            Assert.Throws<InvalidDataException>(() => Reload(Origin, First, entry));
        }
        Assert.Throws<ArgumentException>(() => Reload(Origin, First, Second with { OriginalDr = 7 }));
        Assert.Throws<InvalidDataException>(() => Reload(Origin, Second, First));
        Assert.Throws<InvalidDataException>(() => Reload(Origin, First, First));
    }

    [Fact]
    public void MissingOriginsActorsAndUnknownInitialLoadRequirementsCannotBecomeReceiptHistory()
    {
        TurnPackingCasualtyOrigin[] invalid =
        [null!, Origin with { Owner = "" }, Origin with { Actors = null! }, Origin with { State = null! },
            Origin with { Source = null! }, Origin with { Source = new("", 1) }, Origin with { Source = new("setup", 0) },
            Origin with { Actors = new("mortar", "mortar", "crew") }, Origin with { Actors = new("", "mule", "crew") },
            Origin with { ExistingPeriodId = " " }, Origin with { State = Origin.State with { Weapon = TurnPackingWeapon.Unknown } }];
        foreach (var origin in invalid)
        {
            Assert.Throws<InvalidDataException>(() => Reload(origin, First));
        }
        Assert.Throws<InvalidDataException>(() => Reload(Origin));
        Assert.Throws<InvalidDataException>(() => TurnPackingCasualtyJournal.Replay(Origin, null!));
        Assert.Throws<InvalidDataException>(() => TurnPackingCasualtyJournal.Replay(Origin, [null!]));
    }

    [Fact]
    public void ScopeAndSchemaCannotBeChangedByImportAndBadLaterHistoryDoesNotMutateEarlierResults()
    {
        var json = TurnPackingCasualtyJournal.Write(Scope, Origin, [First]);
        Assert.Throws<InvalidDataException>(() => TurnPackingCasualtyJournal.Read(json, Scope with { Game = "other" }));
        Assert.Throws<InvalidDataException>(() => TurnPackingCasualtyJournal.Read(json, Scope with { Tenant = Guid.NewGuid() }));
        Assert.Throws<InvalidDataException>(() => TurnPackingCasualtyJournal.Read(json, Scope with { Tenant = Guid.Empty }));
        Assert.Throws<InvalidDataException>(() => TurnPackingCasualtyJournal.Read(json, Scope with { Game = "" }));
        Assert.Throws<InvalidDataException>(() => TurnPackingCasualtyJournal.Read(json, null!));
        Assert.Throws<InvalidDataException>(() => TurnPackingCasualtyJournal.Read("null", Scope));
        Assert.Throws<ArgumentNullException>(() => TurnPackingCasualtyJournal.Read(null!, Scope));
        Assert.Throws<JsonException>(() => TurnPackingCasualtyJournal.Read("{", Scope));
        var document = TurnPackingCasualtyJournal.Read(json, Scope);
        Assert.Throws<InvalidDataException>(() => TurnPackingCasualtyJournal.Read(Json(document with { SchemaVersion = 2 }), Scope));
        Assert.Throws<InvalidDataException>(() => TurnPackingCasualtyJournal.Read(Json(document with { Scope = null! }), Scope));
        Assert.Throws<InvalidDataException>(() => TurnPackingCasualtyJournal.Read(Json(document with { Entries = null! }), Scope));
        var before = Reload(Origin, First);
        Assert.Throws<InvalidDataException>(() => Reload(Origin, First, Second with { PreviousEventId = "other" }));
        Assert.Equal(1, before.Current.LowAmmoOccurrences);
        Assert.Equal(TurnHorseSize.HalfSquad, before.Current.MuleSize);
    }

    [Fact]
    public void MissingFactsAndUnexpectedFieldsNeverDefaultToAUsableHistory()
    {
        var json = TurnPackingCasualtyJournal.Write(Scope, Origin, [First]);
        foreach (var name in new[] { "loaded", "mulePackTi", "recovery", "muleAlive", "weaponAlive", "lowAmmoOccurrences", "malfunctioned" })
        {
            var document = JsonNode.Parse(json)!;
            Assert.True(document["origin"]!["state"]!.AsObject().Remove(name));
            Assert.Throws<JsonException>(() => TurnPackingCasualtyJournal.Read(document.ToJsonString(), Scope));
        }
        var altered = JsonNode.Parse(json)!;
        altered["entries"]![0]!["actors"] = JsonSerializer.SerializeToNode(new TurnPackingActors("other", "other-mule", "other-crew"));
        Assert.Throws<JsonException>(() => TurnPackingCasualtyJournal.Read(altered.ToJsonString(), Scope));
        var unknown = Origin with
        {
            State = Origin.State with
            {
                LowAmmoOccurrences = null,
                Malfunctioned = null
            }
        };
        Assert.Null(Reload(unknown, First).Current.LowAmmoOccurrences);
    }

    [Fact]
    public void EliminatedWeaponOrMuleCannotAcceptMoreCasualtiesOrAnInventedNewActor()
    {
        var eliminated = First with
        {
            OriginalDr = 1
        };
        Assert.False(Reload(Origin, eliminated).Current.WeaponAlive);
        Assert.Throws<InvalidDataException>(() => Reload(Origin, eliminated, Second));
        var terminal = Reload(Origin, First, Second);
        Assert.False(terminal.Current.MuleAlive);
        var third = Second with
        {
            EventId = "effect-3",
            Revision = 10,
            PreviousEventId = Second.EventId,
            Casualty = new("casualty-3", 8),
            Roll = new("die-3", 9)
        };
        Assert.Throws<InvalidDataException>(() => Reload(Origin, First, Second, third));
        Assert.Equal(Origin.Actors, terminal.Origin.Actors);
    }
}
