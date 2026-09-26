using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Decision;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Evidence;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Abstractions.Reasoning;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Abstractions.State.Graph;
using LimboDancer.Abstractions.State.History;
using LimboDancer.Abstractions.State.Memory;
using LimboDancer.Abstractions.State.Ontology;
using LimboDancer.Adapters.Mcp;
using LimboDancer.Infrastructure.Audit;
using LimboDancer.Infrastructure.Decision;
using LimboDancer.Infrastructure.Graph;
using LimboDancer.Infrastructure.Ontology;
using LimboDancer.Infrastructure.Relational;
using LimboDancer.Infrastructure.Vector;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Actions.Graph;
using LimboDancer.Runtime.Actions.History;
using LimboDancer.Runtime.Actions.Memory;
using LimboDancer.Runtime.Diagnostics;
using LimboDancer.Runtime.Decision;
using LimboDancer.Runtime.Directed;
using LimboDancer.Runtime.Domain;
using LimboDancer.Runtime.Execution;
using LimboDancer.Runtime.Orchestration;
using LimboDancer.Runtime.Reasoning;
using LimboDancer.Runtime.Verification;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LimboDancer.Host;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLimboDancerHost(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IValidateOptions<LimboDancerHostOptions>, LimboDancerHostOptionsValidator>();
        services.AddOptions<LimboDancerHostOptions>()
            .Bind(configuration.GetSection(LimboDancerHostOptions.SectionName))
            .ValidateOnStart();

        services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationHandler.SchemeName,
                static _ => { });
        services.AddAuthorization();
        services.AddHttpClient();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<RuntimeTelemetry>();
        services.AddSingleton<IMcpCallerContextFactory, McpCallerContextFactory>();

        AddInfrastructure(services);
        AddRuntime(services);

        services.AddSingleton<RuntimeStructureValidator>();
        services.AddHostedService<RuntimeStartupValidationService>();
        services.AddHealthChecks()
            .AddCheck<RuntimeReadinessHealthCheck>("runtime_structure", tags: ["ready"]);
        return services;
    }

    private static void AddInfrastructure(IServiceCollection services)
    {
        services.AddSingleton<InMemoryHistoryStore>();
        services.AddSingleton<IHistoryReader>(static provider => provider.GetRequiredService<InMemoryHistoryStore>());
        services.AddSingleton<IHistoryWriter>(static provider => provider.GetRequiredService<InMemoryHistoryStore>());

        services.AddSingleton<InMemoryGraphStore>();
        services.AddSingleton<IGraphQueryReader>(static provider => provider.GetRequiredService<InMemoryGraphStore>());
        services.AddSingleton<IGraphStateWriter>(static provider => provider.GetRequiredService<InMemoryGraphStore>());

        services.AddSingleton<InMemoryMemorySearch>();
        services.AddSingleton<IMemorySearch>(static provider => provider.GetRequiredService<InMemoryMemorySearch>());
        services.AddSingleton<InMemoryOntologyResolver>();
        services.AddSingleton<IOntologyResolver>(static provider => provider.GetRequiredService<InMemoryOntologyResolver>());

        services.AddSingleton<InMemoryAuditSink>();
        services.AddSingleton<IAuditSink>(static provider => provider.GetRequiredService<InMemoryAuditSink>());
        services.AddSingleton<InMemoryReplayEvidenceSink>();
        services.AddSingleton<IReplayEvidenceSink>(static provider =>
            provider.GetRequiredService<InMemoryReplayEvidenceSink>());
    }

    private static void AddRuntime(IServiceCollection services)
    {
        services.AddSingleton<IActionBindingRegistry>(static provider =>
            new ActionBindingRegistry(BuiltInActionCatalog.CreateMcpBindings()
                .Concat(provider.GetServices<ActionBinding>())));
        services.AddSingleton<IActionRegistry>(static provider =>
            new ActionRegistry(BuiltInActionCatalog.CreateDescriptors()
                .Concat(provider.GetServices<ActionDescriptor>())));
        services.AddSingleton<IActionResolver, RegisteredActionResolver>();
        services.AddSingleton<IActionConstraintPipeline>(static provider =>
            new SemanticActionConstraintPipeline(provider.GetServices<ISemanticPreconditionEvaluator>()));
        services.AddSingleton<IDomainPackageResolver>(static provider =>
            new DomainPackageRegistry(provider.GetServices<DomainPackageDescriptor>()));
        services.AddSingleton<IDecisionProvider>(static provider =>
        {
            var options = provider.GetRequiredService<IOptions<LimboDancerHostOptions>>().Value;
            if (!string.Equals(options.DecisionProvider, "OpenAI", StringComparison.Ordinal))
            {
                return new RuleDecisionProvider();
            }

            var openAi = options.OpenAiDecision;
            return new OpenAiDecisionProvider(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient(),
                new OpenAiDecisionProviderOptions(
                    openAi.ApiKey,
                    openAi.Model,
                    new Uri(openAi.Endpoint, UriKind.Absolute),
                    TimeSpan.FromSeconds(openAi.TimeoutSeconds),
                    openAi.MaxOutputTokens,
                    openAi.InputCostPerMillionTokens,
                    openAi.OutputCostPerMillionTokens),
                provider.GetRequiredService<TimeProvider>());
        });
        services.AddSingleton<IDecisionPlane, DecisionPlane>();
        services.AddSingleton<IReasoningProvider, PassThroughReasoningProvider>();
        services.AddSingleton<ReasoningGuard>();
        services.AddSingleton<IReasoningEngine, ReasoningEngine>();
        services.TryAddSingleton<IGoalAdmissionPolicy, DenyAllGoalAdmissionPolicy>();
        services.TryAddSingleton<IObservationProvider, EmptyObservationProvider>();
        services.AddSingleton<IEffectVerifier>(static provider =>
            new EffectVerifier(
                provider.GetServices<IEffectEvaluator>(),
                provider.GetRequiredService<IAuditSink>(),
                provider.GetRequiredService<TimeProvider>()));
        services.AddSingleton<IEffectVerificationPolicy, DefaultEffectVerificationPolicy>();
        services.AddSingleton<IGoalOrchestrator, GoalOrchestrator>();

        services.AddSingleton<IActionExecutor, HistoryReadExecutor>();
        services.AddSingleton<IActionExecutor, HistoryAppendExecutor>();
        services.AddSingleton<IActionExecutor, GraphQueryExecutor>();
        services.AddSingleton<IActionExecutor, MemorySearchExecutor>();
        services.AddSingleton<IActionExecutorResolver>(static provider =>
            new ActionExecutorResolver(provider.GetServices<IActionExecutor>()));

        services.AddSingleton<IDiagnosticCheck<DiagnosticContext>, ActionDescriptorCurrentCheck>();
        services.AddSingleton<IDiagnosticCheck<DiagnosticContext>, ExecutorBindingResolvableCheck>();
        services.AddSingleton<IDiagnosticCheck<DiagnosticContext>, TenantContextPresentCheck>();
        services.AddSingleton<DiagnosticRunner>(static provider =>
            new DiagnosticRunner(
                provider.GetServices<IDiagnosticCheck<DiagnosticContext>>(),
                timeProvider: provider.GetRequiredService<TimeProvider>()));
        services.AddSingleton<IDiagnosticRunner>(static provider => provider.GetRequiredService<DiagnosticRunner>());
        services.AddSingleton<IDiagnosticProfileValidator>(static provider => provider.GetRequiredService<DiagnosticRunner>());
        services.AddSingleton<IDiagnosticPolicy, DiagnosticPolicy>();

        services.AddSingleton<IActionConstraintEvaluator, FailClosedActionConstraintEvaluator>();
        services.AddSingleton<IExecutionRiskPolicy, DefaultExecutionRiskPolicy>();
        services.AddSingleton<IExecutionGate, ExecutionGate>();
        services.AddSingleton<IDirectedActionRuntime, DirectedActionRuntime>();

        services.AddSingleton<McpServerIdentity>(static provider =>
        {
            var options = provider.GetRequiredService<IOptions<LimboDancerHostOptions>>().Value;
            return new McpServerIdentity(options.ServerName, options.ServerVersion);
        });
        services.AddSingleton<IMcpInteractionAdapter>(static provider =>
        {
            var options = provider.GetRequiredService<IOptions<LimboDancerHostOptions>>().Value;
            return new McpInteractionAdapter(
                provider.GetRequiredService<IDirectedActionRuntime>(),
                provider.GetRequiredService<IActionBindingRegistry>(),
                provider.GetRequiredService<IActionRegistry>(),
                provider.GetRequiredService<McpServerIdentity>(),
                provider.GetRequiredService<TimeProvider>(),
                TimeSpan.FromSeconds(options.InvocationTimeoutSeconds));
        });
    }
}
