using System.Collections.Concurrent;
using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Features;

namespace LimboDancer.Campaign.PilotHost;

public sealed record SituationStartRequest(string CampaignId, string Seed, FormationSelection[] Formations,
    string CampaignContext, string? Saved = null);
public sealed record SituationSave(string Version, string CampaignId, string Seed, string TerrainManifest,
    string CampaignContext, SituationDeploymentDraft Setup, SituationRosterSnapshot Roster);
public sealed record SituationProgress(string Version, SituationHandoff Handoff, string GameRecord);
public sealed record SituationPortableSave(SituationSave Save, SituationBundle Terrain, SituationProgress? Progress = null);
public sealed record SituationJobStatus(string Id, string Status, string Stage, int Done, int Total, string? Result, string? Error);

/// <summary>Background preparation and immutable disk saves for complete Goodwood deployments.</summary>
public sealed class SituationSessions(PilotInputs inputs, string storageRoot) : IAsyncDisposable
{
    public const string Version = "goodwood-situation-save-1";
    private readonly string root = Path.GetFullPath(storageRoot);
    private readonly ConcurrentDictionary<string, Job> jobs = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim workGate = new(1);
    private readonly object jobsGate = new();
    private bool disposed;

    private sealed class Job
    {
        public CancellationTokenSource Cancel { get; } = new();
        public Task<string> Work { get; set; } = null!;
        public volatile string Stage = "queued";
        public volatile int Done;
        public volatile int Total;
    }

    public string Start(SituationStartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        CheckContext(request.CampaignId, request.Seed, request.CampaignContext);
        if (request.Formations is null || request.Formations.Length > 114)
            throw new InvalidDataException("Invalid Goodwood formation deployment.");
        // Request-owned arrays must not change after the background job starts.
        request = JsonSerializer.Deserialize<SituationStartRequest>(JsonSerializer.Serialize(request))!;
        var previous = request.Saved is null ? null : Read(request.Saved);
        if (previous is not null && (previous.CampaignId != request.CampaignId || previous.Seed != request.Seed))
            throw new InvalidDataException("The saved Situation belongs to another campaign or seed.");
        return Run(async job =>
        {
            var token = job.Cancel.Token;
            string path;
            if (previous is not null) path = TerrainPath(previous.TerrainManifest);
            else
            {
                job.Stage = "terrain";
                var world = PilotIdentity.World(inputs.ForVersion(PilotInputs.SituationGeneratorVersion), request.Seed);
                var directory = Path.Combine(root, "terrain", world);
                var existing = Directory.Exists(directory) ? Directory.GetFiles(directory, "situation-*.json") : [];
                if (existing.Length > 1) throw new InvalidDataException("Ambiguous accepted terrain. Retain the saved maps for review.");
                path = existing.Length == 1 ? existing[0] : SituationPublisher.Publish(inputs, request.Seed, directory,
                    (done, total) => { job.Done = done; job.Total = total; }, token);
            }
            var map = new SituationMap(inputs, path, cancellationToken: token);
            if (map.Manifest.Seed != request.Seed) throw new InvalidDataException("Saved terrain has another seed.");
            job.Stage = "deployment";
            var deployment = new SituationDeployment(inputs, map);
            var setup = previous is null ? deployment.Assemble(request.Formations, cancellationToken: token)
                : deployment.Review(previous.Setup with { Formations = request.Formations }, token);
            var save = new SituationSave(Version, request.CampaignId, request.Seed, Path.GetRelativePath(root, path),
                request.CampaignContext, setup, previous?.Roster ?? SituationRoster.Create(inputs));
            token.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            return Write(save);
        });
    }

    public SituationJobStatus Status(string id)
    {
        if (!jobs.TryGetValue(id, out var job)) throw new InvalidDataException("Situation job expired. Reopen its saved deployment.");
        var work = job.Work;
        return new(id, work.IsCanceled ? "cancelled" : work.IsFaulted ? "failed" : work.IsCompletedSuccessfully ? "ready" : "working",
            job.Stage, job.Done, job.Total, work.IsCompletedSuccessfully ? work.Result : null, work.Exception?.GetBaseException().Message);
    }

    public void Cancel(string id)
    {
        if (jobs.TryGetValue(id, out var job)) job.Cancel.Cancel();
    }

    public SituationSave Read(string id)
    {
        CheckId(id);
        var path = Path.Combine(root, "saves", id + ".json");
        if (!File.Exists(path) || new FileInfo(path).Length > 4_000_000)
            throw new InvalidDataException("Situation save is missing or oversized. Load the campaign file to restore it.");
        var bytes = File.ReadAllBytes(path);
        if (BoardPackage.Hash(bytes) != id) throw new InvalidDataException("Situation save identity does not match its contents.");
        var save = JsonSerializer.Deserialize<SituationSave>(bytes) ?? throw new InvalidDataException("Invalid Situation save.");
        CheckSave(save);
        var terrain = TerrainPath(save.TerrainManifest);
        if (!File.Exists(terrain)) throw new InvalidDataException("Accepted Situation terrain is missing. Restore it from the campaign file.");
        var map = new SituationMap(inputs, terrain);
        if (map.Manifest.Seed != save.Seed || map.Manifest.WorldIdentity != save.Setup.WorldIdentity)
            throw new InvalidDataException("Situation save and accepted terrain differ.");
        return save;
    }

    public SituationPortableSave Export(string id, CancellationToken cancellationToken = default)
    {
        var save = Read(id);
        var path = TerrainPath(save.TerrainManifest);
        var reviewed = new SituationDeployment(inputs, new SituationMap(inputs, path)).Review(save.Setup, cancellationToken);
        return new(save with { Setup = reviewed }, SituationBundles.Export(inputs, path, cancellationToken));
    }

    public SituationHandoff TransferHandoff(string saveId, string prepared)
    {
        var save = Read(saveId);
        var handoff = ReadHandoff(prepared);
        if (save.CampaignId != handoff.CampaignId || save.Setup.WorldIdentity != handoff.WorldIdentity
            || save.CampaignContext != handoff.CampaignContext
            || JsonSerializer.Serialize(save.Setup) != JsonSerializer.Serialize(handoff.Setup)
            || JsonSerializer.Serialize(save.Roster) != JsonSerializer.Serialize(handoff.Roster))
            throw new InvalidDataException("The prepared ASL handoff does not match the campaign save.");
        return handoff;
    }

    public string Import(SituationPortableSave portable,
        Func<SituationHandoff, string, CancellationToken, Task<string>>? restoreProgress = null)
    {
        ArgumentNullException.ThrowIfNull(portable);
        CheckSave(portable.Save);
        if (portable.Progress is { } attachment && (attachment.Version != "goodwood-asl-progress-1"
            || string.IsNullOrWhiteSpace(attachment.GameRecord) || attachment.GameRecord.Length > 20_000_000 || restoreProgress is null))
            throw new InvalidDataException("ASL progress is unsupported or cannot be restored by this host.");
        return Run(async job =>
        {
            job.Stage = "restore terrain";
            var token = job.Cancel.Token;
            var path = SituationBundles.Restore(inputs, portable.Terrain, Path.Combine(root, "restored"), token);
            var map = new SituationMap(inputs, path, cancellationToken: token);
            if (map.Manifest.Seed != portable.Save.Seed || map.Manifest.WorldIdentity != portable.Save.Setup.WorldIdentity)
                throw new InvalidDataException("Imported deployment and terrain belong to different worlds.");
            job.Stage = "review deployment";
            var reviewed = new SituationDeployment(inputs, map).Review(portable.Save.Setup, token);
            token.ThrowIfCancellationRequested();
            var restored = portable.Save with { TerrainManifest = Path.GetRelativePath(root, path), Setup = reviewed };
            if (portable.Progress is not { } progress) return Write(restored);
            job.Stage = "validate saved ASL progress";
            var expected = await SituationHandoffs.Prepare(inputs, map, reviewed, restored.CampaignId, restored.CampaignContext, token);
            if (JsonSerializer.Serialize(expected) != JsonSerializer.Serialize(progress.Handoff))
                throw new InvalidDataException("Saved ASL progress belongs to different terrain or deployment.");
            token.ThrowIfCancellationRequested();
            var url = await restoreProgress!(expected, progress.GameRecord, token);
            token.ThrowIfCancellationRequested();
            var prepared = Publish("handoffs", JsonSerializer.SerializeToUtf8Bytes(expected));
            var save = Write(restored);
            return JsonSerializer.Serialize(new { save, prepared, url });
        });
    }

    // Explicit recovery only. Ordinary reopen and import never fill accepted gaps.
    public string PlaceRemaining(string id)
    {
        var save = Read(id);
        return Run(async job =>
        {
            job.Stage = "place remaining counters";
            var token = job.Cancel.Token;
            var deployment = new SituationDeployment(inputs, new SituationMap(inputs, TerrainPath(save.TerrainManifest)));
            _ = deployment.Review(save.Setup, token); // Reject incompatible policies before assigning anything.
            var setup = deployment.Assemble(save.Setup.Formations, save.Setup.Positions, token);
            token.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            return Write(save with { Setup = setup });
        });
    }

    public string Prepare(string id)
    {
        var save = Read(id);
        return Run(async job =>
        {
            job.Stage = "validate ASL handoff";
            var handoff = await SituationHandoffs.Prepare(inputs, new SituationMap(inputs, TerrainPath(save.TerrainManifest)),
                save.Setup, save.CampaignId, save.CampaignContext, job.Cancel.Token);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(handoff);
            job.Cancel.Token.ThrowIfCancellationRequested();
            return Publish("handoffs", bytes);
        });
    }

    public string Launch(string id, Func<SituationHandoff, CancellationToken, Task<string>> import)
    {
        var handoff = ReadHandoff(id);
        return Run(async job =>
        {
            job.Stage = "open ASL setup";
            return await import(handoff, job.Cancel.Token);
        });
    }

    public SituationHandoff ReadHandoff(string id)
    {
        CheckId(id);
        var path = Path.Combine(root, "handoffs", id + ".json");
        if (!File.Exists(path) || new FileInfo(path).Length > 8_000_000)
            throw new InvalidDataException("ASL handoff is missing or oversized. Prepare it again from the accepted Situation save.");
        var bytes = File.ReadAllBytes(path);
        if (BoardPackage.Hash(bytes) != id) throw new InvalidDataException("ASL handoff identity differs from its contents.");
        var handoff = JsonSerializer.Deserialize<SituationHandoff>(bytes) ?? throw new InvalidDataException("Invalid ASL handoff.");
        if (handoff.Version != SituationHandoffs.Version) throw new InvalidDataException("Unsupported ASL handoff version.");
        return handoff;
    }

    private string Write(SituationSave save)
    {
        CheckSave(save);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(save);
        if (bytes.Length > 4_000_000) throw new InvalidDataException("Situation save exceeds its size limit.");
        return Publish("saves", bytes);
    }

    private string Publish(string category, byte[] bytes)
    {
        var id = BoardPackage.Hash(bytes);
        var directory = Path.Combine(root, category);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, id + ".json");
        var stage = path + ".stage-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllBytes(stage, bytes);
            try { File.Move(stage, path); }
            catch (IOException) when (File.Exists(path))
            {
                if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) throw new InvalidDataException("Existing Situation save is corrupt.");
            }
            return id;
        }
        finally { if (File.Exists(stage)) File.Delete(stage); }
    }

    private void CheckSave(SituationSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != Version || save.Setup is null || save.Roster is null || save.Setup.Policy != SituationDeployment.Policy ||
            save.Setup.Formations is null || save.Setup.Positions is null || save.Setup.Review is null ||
            save.Setup.Catalog != PilotCatalog.Identity)
            throw new InvalidDataException("Unsupported Situation save version or deployment policy.");
        CheckContext(save.CampaignId, save.Seed, save.CampaignContext);
        SituationRoster.Validate(inputs, save.Roster);
    }

    private static void CheckContext(string campaign, string seed, string context)
    {
        if (string.IsNullOrWhiteSpace(campaign) || campaign.Length > 100 || context is not { Length: > 0 and <= 1_000_000 })
            throw new InvalidDataException("Invalid campaign identity or context.");
        SeedRandom.ValidateSeed(seed);
        using var json = JsonDocument.Parse(context);
        if (json.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Campaign context must be an object.");
    }

    private static void CheckId(string id)
    {
        if (id is not { Length: 64 } || id.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new InvalidDataException("Invalid Situation save identity.");
    }

    private string TerrainPath(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new InvalidDataException("Invalid saved terrain reference.");
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Saved terrain reference is outside the Situation store.");
        return path;
    }

    private string Run(Func<Job, Task<string>> action)
    {
        lock (jobsGate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            foreach (var old in jobs.Where(p => p.Value.Work.IsCompleted).ToArray())
                if (jobs.Count >= 8 && jobs.TryRemove(old.Key, out var removed)) removed.Cancel.Dispose();
            if (jobs.Count >= 8) throw new InvalidDataException("Wait for a Situation job to finish before starting another.");
            var id = Guid.NewGuid().ToString("N");
            var job = new Job();
            job.Work = Task.Run(async () =>
            {
                await workGate.WaitAsync(job.Cancel.Token);
                try { return await action(job); }
                finally { workGate.Release(); }
            }, job.Cancel.Token);
            jobs[id] = job;
            return id;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Job[] pending;
        lock (jobsGate)
        {
            disposed = true;
            pending = jobs.Values.ToArray();
            foreach (var job in pending) job.Cancel.Cancel();
        }
        try { await Task.WhenAll(pending.Select(job => job.Work)); }
        catch (Exception) when (pending.All(job => job.Work.IsCompleted)) { }
        foreach (var job in pending) job.Cancel.Dispose();
        workGate.Dispose();
    }
}
