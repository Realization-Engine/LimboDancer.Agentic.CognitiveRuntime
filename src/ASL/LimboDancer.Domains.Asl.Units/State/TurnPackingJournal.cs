using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

public enum TurnPackingOperation
{
    Start, Rally, Midpoint, Interrupt, Resume, Reverse, Complete
}

/// <summary>Evidence identifying an ASOP 1.13B work step, not inferred from a phase name. The admitting host must verify its source.</summary>
public sealed record TurnPackingWorkStep(string EventId, long Revision, TurnPackingRally Rally);

/// <summary>A versioned period journal entry. Eligibility evidence belongs only to Start/Resume; it is not an adjudication permission.</summary>
public sealed record TurnPackingJournalEntry(int Version, string EventId, long Revision, string? PreviousEventId,
    TurnPackingOperation Operation, TurnPackingWorkStep WorkStep, TurnPackingSpecification? Specification = null,
    TurnPackingDirection? Direction = null, TurnPackingFacts? Eligibility = null);

public sealed record TurnPackingJournalDocument(int SchemaVersion, GameScope Scope, IReadOnlyList<TurnPackingJournalEntry> Entries);

public sealed record TurnPackingReceipt(string EventId, long Revision, TurnPackingOperation Operation, TurnPackingWorkStep WorkStep);
public sealed record TurnPackingHistory(TurnPackingPeriod Period, IReadOnlyList<TurnPackingReceipt> Receipts);

/// <summary>
/// D3.2 durable period codec/replay. This is a separate, not-yet-admitted journal format, not a GameEvent protocol.
/// Hosts must verify real actors, eligibility, work-step origins and atomic piece effects before accepting entries.
/// Replay checks causal integrity and Rules transitions; it cannot authenticate supplied evidence.
/// </summary>
public static class TurnPackingJournal
{
    public const int Version = 1;

    public static string Write(GameScope scope, IReadOnlyList<TurnPackingJournalEntry> entries)
    {
        ValidateScope(scope);
        _ = Replay(entries);
        return JsonSerializer.Serialize(new TurnPackingJournalDocument(Version, scope, entries), JsonSerializerOptions.Web);
    }

    public static IReadOnlyList<TurnPackingJournalEntry> Read(string json, GameScope expectedScope)
    {
        ArgumentNullException.ThrowIfNull(json);
        ValidateScope(expectedScope);
        var document = JsonSerializer.Deserialize<TurnPackingJournalDocument>(json, JsonSerializerOptions.Web)
            ?? throw new InvalidDataException("Missing packing journal.");
        if (document.SchemaVersion != Version || document.Scope != expectedScope || document.Entries is null)
        {
            throw new InvalidDataException("Unsupported packing journal schema, missing entries or different game scope.");
        }
        _ = Replay(document.Entries);
        return Array.AsReadOnly(document.Entries.ToArray());
    }

    public static TurnPackingHistory Replay(IReadOnlyList<TurnPackingJournalEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
        {
            throw new InvalidDataException("A packing journal must begin with a Start entry.");
        }
        TurnPackingPeriod? period = null;
        TurnPackingJournalEntry? prior = null;
        var eventIds = new HashSet<string>(StringComparer.Ordinal);
        var steps = new HashSet<string>(StringComparer.Ordinal);
        var receipts = new List<TurnPackingReceipt>();
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ValidateEnvelope(entry, prior, eventIds);
            if (steps.Contains(entry.EventId))
            {
                throw new InvalidDataException("A packing action cannot reuse a work-step event identity.");
            }
            var newStep = prior is null || entry.Operation == TurnPackingOperation.Rally;
            if (newStep)
            {
                if (!steps.Add(entry.WorkStep.EventId) || eventIds.Contains(entry.WorkStep.EventId) || prior is not null && entry.WorkStep.Revision <= prior.Revision)
                {
                    throw new InvalidDataException("A new packing work boundary needs a new, later source event.");
                }
            }
            else if (entry.WorkStep != prior!.WorkStep)
            {
                throw new InvalidDataException("A packing action cannot replace its recorded work-step origin.");
            }
            if (entry.Operation == TurnPackingOperation.Start)
            {
                if (period is not null || entry.Specification is null || entry.Direction is null || entry.Eligibility is null)
                {
                    throw new InvalidDataException("Start binds the specification, direction and eligibility exactly once.");
                }
                period = TurnPackingPeriods.Start(entry.Specification, entry.Direction.Value, entry.WorkStep.Rally, entry.Eligibility);
            }
            else
            {
                if (period is null || entry.Specification is not null || entry.Direction is not null
                    || (entry.Operation == TurnPackingOperation.Resume) != (entry.Eligibility is not null))
                {
                    throw new InvalidDataException("Packing changes retain their original actors and specification; only Resume supplies fresh eligibility.");
                }
                period = entry.Operation switch
                {
                    TurnPackingOperation.Rally => TurnPackingPeriods.NextRally(period, entry.WorkStep.Rally),
                    TurnPackingOperation.Midpoint => TurnPackingPeriods.ApplyMidpoint(period),
                    TurnPackingOperation.Interrupt => TurnPackingPeriods.Interrupt(period),
                    TurnPackingOperation.Resume => TurnPackingPeriods.Resume(period, entry.Eligibility!),
                    TurnPackingOperation.Reverse => TurnPackingPeriods.Reverse(period),
                    TurnPackingOperation.Complete => TurnPackingPeriods.Complete(period),
                    _ => throw new InvalidDataException("Unsupported packing operation.")
                };
            }
            receipts.Add(new(entry.EventId, entry.Revision, entry.Operation, entry.WorkStep));
            prior = entry;
        }
        return new(period!, receipts.AsReadOnly());
    }

    private static void ValidateScope(GameScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (scope.Tenant == Guid.Empty || string.IsNullOrWhiteSpace(scope.Game))
        {
            throw new InvalidDataException("Packing history requires a tenant and game identity.");
        }
    }

    private static void ValidateEnvelope(TurnPackingJournalEntry entry, TurnPackingJournalEntry? prior, HashSet<string> ids)
    {
        if (entry.Version != Version || !Enum.IsDefined(entry.Operation) || string.IsNullOrWhiteSpace(entry.EventId)
            || !ids.Add(entry.EventId) || entry.Revision < 1 || prior is not null && entry.Revision <= prior.Revision
            || entry.PreviousEventId != prior?.EventId)
        {
            throw new InvalidDataException("Invalid packing journal version, event identity, revision or predecessor.");
        }
        if (entry.WorkStep is not { Rally: { } rally } step || string.IsNullOrWhiteSpace(step.EventId)
            || step.Revision < 1 || step.Revision >= entry.Revision || rally.GameTurn < 1
            || step.EventId == entry.EventId)
        {
            throw new InvalidDataException("Packing entries require an earlier, identified RPh work-step source.");
        }
    }
}
