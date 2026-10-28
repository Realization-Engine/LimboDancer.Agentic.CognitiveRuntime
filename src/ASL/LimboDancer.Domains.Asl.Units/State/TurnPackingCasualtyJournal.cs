using System.Text.Json;
using System.Text.Json.Serialization;
using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

public sealed record TurnPackingEvidenceSource(string EventId, long Revision);
/// <summary>Actor binding and initial state supplied by a verified host. A period identity is a link, not proof of progress or release.</summary>
public sealed record TurnPackingCasualtyOrigin(string Owner, TurnPackingActors Actors, TurnPackingEvidenceSource Source,
    TurnPackingCasualtyState State, string? ExistingPeriodId = null);
public sealed record TurnPackingCasualtyEntry(int Version, string EventId, long Revision, string? PreviousEventId,
    TurnPackingEvidenceSource Casualty, TurnPackingEvidenceSource Roll, TurnPackingMuleLoss MuleLoss, int OriginalDr,
    TurnPackingUnloadTerrain Terrain);
public sealed record TurnPackingCasualtyDocument(int SchemaVersion, GameScope Scope, TurnPackingCasualtyOrigin Origin,
    IReadOnlyList<TurnPackingCasualtyEntry> Entries);
public sealed record TurnPackingCasualtyReceipt(TurnPackingCasualtyEntry Entry, TurnPackingVulnerabilityVerdict Verdict);
public sealed record TurnPackingCasualtyHistory(TurnPackingCasualtyOrigin Origin, TurnPackingCasualtyState Current,
    IReadOnlyList<TurnPackingCasualtyReceipt> Receipts);

/// <summary>
/// Scoped, causal G10.4 receipt codec. This is not a GameEvent or a live import path. The host must authenticate source events,
/// actor ownership, terrain, dice and absence of intervening load/period changes before binding these receipts to a game.
/// </summary>
public static class TurnPackingCasualtyJournal
{
    public const int Version = 1;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static string Write(GameScope scope, TurnPackingCasualtyOrigin origin, IReadOnlyList<TurnPackingCasualtyEntry> entries)
    {
        ValidateScope(scope);
        _ = Replay(origin, entries);
        return JsonSerializer.Serialize(new TurnPackingCasualtyDocument(Version, scope, origin, entries), Options);
    }

    public static TurnPackingCasualtyDocument Read(string json, GameScope expectedScope)
    {
        ArgumentNullException.ThrowIfNull(json);
        ValidateScope(expectedScope);
        var document = JsonSerializer.Deserialize<TurnPackingCasualtyDocument>(json, Options)
            ?? throw new InvalidDataException("Missing Animal-Pack casualty journal.");
        if (document.SchemaVersion != Version || document.Scope != expectedScope)
        {
            throw new InvalidDataException("Unsupported casualty schema or different game scope.");
        }
        _ = Replay(document.Origin, document.Entries);
        return document with
        {
            Entries = Array.AsReadOnly(document.Entries.ToArray())
        };
    }

    public static TurnPackingCasualtyHistory Replay(TurnPackingCasualtyOrigin origin, IReadOnlyList<TurnPackingCasualtyEntry> entries)
    {
        if (origin is not { Actors: { } actors, Source: { } source, State: { } state } || entries is null || entries.Count == 0)
        {
            throw new InvalidDataException("Casualty history requires bound actors, an initial state source and at least one receipt.");
        }
        string[] identities = [actors.Weapon, actors.Mule, actors.Personnel];
        if (string.IsNullOrWhiteSpace(origin.Owner) || identities.Any(string.IsNullOrWhiteSpace)
            || identities.Distinct(StringComparer.Ordinal).Count() != 3 || string.IsNullOrWhiteSpace(source.EventId) || source.Revision < 1
            || origin.ExistingPeriodId is not null && string.IsNullOrWhiteSpace(origin.ExistingPeriodId))
        {
            throw new InvalidDataException("Casualty origin requires an owner, distinct actors and a valid source identity.");
        }
        var ids = new HashSet<string>(StringComparer.Ordinal) { source.EventId };
        var revision = source.Revision;
        string? predecessor = null;
        var receipts = new List<TurnPackingCasualtyReceipt>();
        foreach (var entry in entries)
        {
            if (entry is not { Casualty: { } casualty, Roll: { } roll } || entry.Version != Version
                || entry.PreviousEventId != predecessor || string.IsNullOrWhiteSpace(entry.EventId)
                || string.IsNullOrWhiteSpace(casualty.EventId) || string.IsNullOrWhiteSpace(roll.EventId)
                || !ids.Add(casualty.EventId) || !ids.Add(roll.EventId) || !ids.Add(entry.EventId)
                || casualty.Revision <= revision || roll.Revision <= casualty.Revision || entry.Revision <= roll.Revision)
            {
                throw new InvalidDataException("Each casualty receipt needs unique, ordered casualty, subsequent-die and effect sources plus its exact predecessor.");
            }
            var transition = TurnPackingCasualtyRules.Apply(state, entry.MuleLoss, entry.OriginalDr, entry.Terrain);
            state = transition.State;
            receipts.Add(new(entry, transition.Verdict));
            revision = entry.Revision;
            predecessor = entry.EventId;
        }
        return new(origin, state, receipts.AsReadOnly());
    }

    private static void ValidateScope(GameScope scope)
    {
        if (scope is null || scope.Tenant == Guid.Empty || string.IsNullOrWhiteSpace(scope.Game))
        {
            throw new InvalidDataException("Casualty history requires a tenant and game identity.");
        }
    }
}
