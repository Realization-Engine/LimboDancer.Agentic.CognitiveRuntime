using System.Diagnostics;
using System.Collections.Concurrent;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Rendering;

namespace LimboDancer.Campaign.PilotHost;

public sealed record GenerateRequest(string Seed, FormationSelection? Formation = null);
public sealed record OpenRequest(string Seed, PilotBundle? Bundle, DeploymentContext Context, PilotSetup? Setup, DeploymentContext? SavedContext = null);
public sealed record EditRequest(string Session, PilotSetup Setup, string Action, string? UnitId, string? Hex);
public sealed record PilotSession(PilotDeployment Deployment, DeploymentContext Context);

public sealed class PilotJob : IDisposable
{
    public CancellationTokenSource Cancellation { get; } = new();
    public Task<PilotDeployment>? Work { get; set; }
    public void Dispose() => Cancellation.Dispose();
}

public sealed class PilotSessions(PilotInputs inputs, string packagesRoot) : IDisposable
{
    private readonly ConcurrentDictionary<string, PilotJob> jobs = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PilotSession> sessions = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim generation = new(1);
    private readonly object jobLock = new();
    public object Start(string seed, FormationSelection? selection = null)
    {
        SeedRandom.ValidateSeed(seed);
        var crop = selection is null ? inputs.DefaultCrop : FormationDeployment.CropFor(inputs, selection);
        var key = FormationDeployment.JobKey(seed, selection);
        lock (jobLock)
        {
            if (jobs.TryGetValue(key, out var old) && old.Work is { IsCanceled: false, IsFaulted: false }) return Status(key);
            if (jobs.Count >= 8 && !jobs.ContainsKey(key)) throw new InvalidDataException("Pilot host is full. Restart it to release cached maps; campaign saves are retained.");
            var job = new PilotJob(); jobs[key] = job;
            job.Work = Task.Run(async () =>
            {
                await generation.WaitAsync(job.Cancellation.Token).ConfigureAwait(false);
                try
                {
                    var result = new PilotGenerator(inputs).Generate(seed, crop, cancellationToken: job.Cancellation.Token);
                    PackagePublisher.Publish(result, inputs, packagesRoot, job.Cancellation.Token);
                    return new PilotDeployment(inputs, result, seed, selection);
                }
                finally { generation.Release(); }
            }, job.Cancellation.Token);
            old?.Dispose();
            return Status(key);
        }
    }
    public object Status(string seed)
    {
        SeedRandom.ValidateSeed(seed);
        if (!jobs.TryGetValue(seed, out var job)) throw new InvalidDataException("Pilot generation is not running. Select Open pilot to retry with the saved seed.");
        var task = job.Work!;
        return new { id = seed, status = task.IsCanceled ? "cancelled" : task.IsFaulted ? "failed" : task.IsCompletedSuccessfully ? "ready" : "generating",
            error = task.Exception?.GetBaseException().Message };
    }
    public void Cancel(string seed)
    {
        SeedRandom.ValidateSeed(seed);
        if (jobs.TryGetValue(seed, out var job)) job.Cancellation.Cancel();
    }
    public async Task<object> Open(OpenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Context);
        SeedRandom.ValidateSeed(request.Seed);
        var timings = new Dictionary<string, double>(StringComparer.Ordinal);
        var watch = Stopwatch.StartNew();
        PilotDeployment deployment;
        if (request.Bundle is { } bundle)
        {
            if (bundle.Seed != request.Seed) throw new InvalidDataException("Saved campaign seed differs from its accepted map.");
            deployment = new PilotDeployment(inputs, bundle, timings, request.Context.Formation);
        }
        else if (jobs.TryGetValue(FormationDeployment.JobKey(request.Seed, request.Context.Formation), out var job) && job.Work!.IsCompletedSuccessfully)
            deployment = await job.Work.ConfigureAwait(false);
        else throw new InvalidDataException("Generate the pilot map before opening deployment.");
        timings["loadTotal"] = watch.Elapsed.TotalMilliseconds; watch.Restart();
        var state = request.Setup is { } savedSetup
            ? deployment.Rebind(request.SavedContext ?? request.Context, request.Context, savedSetup)
            : deployment.Create(request.Context);
        deployment.Validate(request.Context, state);
        var id = deployment.Package.Version + "-" + deployment.ContextHash(request.Context);
        if (sessions.Count >= 16 && !sessions.ContainsKey(id)) throw new InvalidDataException("Too many pilot sessions. Restart the host to release cached sessions.");
        sessions[id] = new(deployment, request.Context);
        timings["validateSetup"] = watch.Elapsed.TotalMilliseconds; watch.Restart();
        var render = BoardRenderInput.Create(deployment.Package.Board, deployment.Package.Name, deployment.Grid, inputs.Catalog, deployment.Facts,
            new StyledSource(deployment.Package.Model, deployment.Grid, deployment.Facts));
        var svg = BoardRenderer.Document(render, BoardView.Exact);
        timings["render"] = watch.Elapsed.TotalMilliseconds;
        return new { session = id, setup = state, bundle = deployment.Bundle, cells = deployment.Cells, roster = deployment.Units,
            width = deployment.Grid.Geometry.GridWidth, height = deployment.Grid.Geometry.GridHeight,
            svg, timingsMs = timings };
    }
    public PilotSetup Edit(EditRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!sessions.TryGetValue(request.Session, out var session)) throw new InvalidDataException("Pilot session expired. Close and reopen the pilot from its saved campaign.");
        var d = session.Deployment; var c = session.Context;
        return request.Action switch
        {
            "move" => d.Move(c, request.Setup, request.UnitId ?? "", request.Hex ?? ""),
            "deploy" => d.AutoDeploy(c, request.Setup),
            "complete" => d.SetComplete(c, request.Setup, true),
            "review" => d.SetComplete(c, request.Setup, false),
            _ => throw new InvalidDataException("Unknown pilot setup action.")
        };
    }
    public void Dispose()
    {
        foreach (var job in jobs.Values) job.Dispose();
        generation.Dispose();
    }
}
