using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Decision;

namespace LimboDancer.Infrastructure.Decision;

public sealed class OpenAiDecisionProvider : IDecisionProvider, IDisposable
{
    public const string Id = "openai:responses/StructuredDecision";
    private const decimal TokensPerMillion = 1_000_000m;
    private const string Instructions = "Select only from the supplied permitted candidates. "
        + "Return Selected, Abstained, or Escalated using the required schema. "
        + "Use a stable concise reason code. Do not include hidden reasoning.";
    private readonly HttpClient httpClient;
    private readonly OpenAiDecisionProviderOptions options;
    private readonly TimeProvider timeProvider;

    public OpenAiDecisionProvider(
        HttpClient httpClient,
        OpenAiDecisionProviderOptions options,
        TimeProvider? timeProvider = null)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public string ProviderId => Id;

    public string ProviderVersion => options.Model;

    public void Dispose() => httpClient.Dispose();

    public async Task<DecisionResult> DecideAsync(
        DecisionContext context,
        IReadOnlyList<PermittedAction> candidates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);
        cancellationToken.ThrowIfCancellationRequested();
        if (candidates.Count == 0)
        {
            throw new ArgumentException("The OpenAI provider requires permitted candidates.", nameof(candidates));
        }

        var input = CreateInput(context, candidates);
        var inputTokenBound = Encoding.UTF8.GetByteCount(Instructions)
            + Encoding.UTF8.GetByteCount(input);
        var maxOutputTokens = OutputTokenLimit(context, inputTokenBound);
        EnsureCostBudget(context, inputTokenBound, maxOutputTokens);
        var requestBody = CreateRequestBody(input, maxOutputTokens);
        using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint)
        {
            Content = JsonContent.Create(requestBody),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        var remaining = context.Budget.Deadline - timeProvider.GetUtcNow();
        if (remaining <= TimeSpan.Zero)
        {
            throw new TimeoutException("The Decision deadline has expired.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(remaining < options.Timeout ? remaining : options.Timeout);
        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = await httpClient
                .SendAsync(request, timeout.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The OpenAI Decision request timed out.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"The OpenAI Decision request failed with status {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            stopwatch.Stop();
            var result = ParseResponse(document.RootElement, stopwatch.Elapsed);
            EnsureCandidateMembership(result, candidates);
            return result;
        }
    }

    private string CreateInput(DecisionContext context, IReadOnlyList<PermittedAction> candidates)
    {
        var payload = new
        {
            goal = new
            {
                intent = context.Goal.Intent,
                inputs = context.Goal.Inputs,
            },
            observations = context.Observations.Select(static observation => new
            {
                observation.ObservationId,
                observation.Version,
                observation.Provenance,
                observation.Data,
            }),
            candidates = candidates.Select(static permitted => new
            {
                permitted.Candidate.CandidateId,
                actionId = permitted.Candidate.Descriptor.Id.Value,
                actionVersion = permitted.Candidate.Descriptor.Version.Value,
                permitted.Candidate.Descriptor.Name,
                permitted.Candidate.Descriptor.Description,
                arguments = permitted.Candidate.Arguments,
                permitted.Candidate.EvidenceRefs,
                permitted.Candidate.StateVersions,
                constraints = permitted.ConstraintResults.Select(static result => new
                {
                    result.ConstraintId,
                    result.ReasonCode,
                }),
            }),
        };
        return JsonSerializer.Serialize(payload);
    }

    private int OutputTokenLimit(DecisionContext context, int inputTokenBound)
    {
        if (context.Budget.MaxTokens is not { } maxTokens)
        {
            return options.MaxOutputTokens;
        }

        var remaining = maxTokens - inputTokenBound;
        if (remaining <= 0)
        {
            throw new InvalidOperationException("The remaining Decision token budget cannot contain the request.");
        }

        return (int)Math.Min(options.MaxOutputTokens, remaining);
    }

    private void EnsureCostBudget(DecisionContext context, int inputTokenBound, int outputTokenLimit)
    {
        if (context.Budget.MaxCost is not { } maxCost)
        {
            return;
        }

        var worstCaseCost = CalculateCost(inputTokenBound, outputTokenLimit);
        if (worstCaseCost > maxCost)
        {
            throw new InvalidOperationException("The remaining Decision cost budget cannot contain the request.");
        }
    }

    private object CreateRequestBody(string input, int maxOutputTokens) => new
    {
        model = options.Model,
        instructions = Instructions,
        input,
        store = false,
        max_output_tokens = maxOutputTokens,
        text = new
        {
            format = new
            {
                type = "json_schema",
                name = "decision_result",
                strict = true,
                schema = DecisionSchema(),
            },
        },
    };

    private static JsonElement DecisionSchema() => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            outcome = new { type = "string", @enum = new[] { "Selected", "Abstained", "Escalated" } },
            selectedCandidateId = new { type = new[] { "string", "null" } },
            reasonCode = new { type = "string" },
            confidence = new { type = new[] { "number", "null" }, minimum = 0, maximum = 1 },
            distribution = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        candidateId = new { type = "string" },
                        probability = new { type = "number", minimum = 0, maximum = 1 },
                    },
                    required = new[] { "candidateId", "probability" },
                    additionalProperties = false,
                },
            },
        },
        required = new[] { "outcome", "selectedCandidateId", "reasonCode", "confidence", "distribution" },
        additionalProperties = false,
    });

    private DecisionResult ParseResponse(JsonElement response, TimeSpan latency)
    {
        if (!response.TryGetProperty("status", out var status)
            || !string.Equals(status.GetString(), "completed", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The OpenAI Decision response did not complete.");
        }

        var outputText = FindOutputText(response);
        using var resultDocument = JsonDocument.Parse(outputText);
        var value = resultDocument.RootElement;
        if (!Enum.TryParse<DecisionOutcome>(value.GetProperty("outcome").GetString(), out var outcome)
            || !Enum.IsDefined(outcome))
        {
            throw new InvalidOperationException("The OpenAI Decision outcome is invalid.");
        }

        var selected = value.GetProperty("selectedCandidateId");
        var confidence = value.GetProperty("confidence");
        var distribution = value.GetProperty("distribution")
            .EnumerateArray()
            .Select(static item => new KeyValuePair<string, double>(
                item.GetProperty("candidateId").GetString()!,
                item.GetProperty("probability").GetDouble()))
            .ToArray();
        var usage = response.GetProperty("usage");
        var tokenUsage = new DecisionTokenUsage(
            usage.GetProperty("input_tokens").GetInt64(),
            usage.GetProperty("output_tokens").GetInt64());
        return new DecisionResult(
            outcome,
            selected.ValueKind == JsonValueKind.Null ? null : selected.GetString(),
            ProviderId,
            value.GetProperty("reasonCode").GetString()!,
            confidence.ValueKind == JsonValueKind.Null ? null : confidence.GetDouble(),
            distribution,
            ProviderVersion,
            latency,
            CalculateCost(tokenUsage.InputTokens, tokenUsage.OutputTokens),
            tokenUsage);
    }

    private static string FindOutputText(JsonElement response)
    {
        foreach (var output in response.GetProperty("output").EnumerateArray())
        {
            if (!output.TryGetProperty("content", out var contents))
            {
                continue;
            }

            foreach (var content in contents.EnumerateArray())
            {
                var type = content.GetProperty("type").GetString();
                if (string.Equals(type, "refusal", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The OpenAI Decision response was refused.");
                }

                if (string.Equals(type, "output_text", StringComparison.Ordinal))
                {
                    return content.GetProperty("text").GetString()
                        ?? throw new InvalidOperationException("The OpenAI Decision output was empty.");
                }
            }
        }

        throw new InvalidOperationException("The OpenAI Decision response contained no structured output.");
    }

    private static void EnsureCandidateMembership(
        DecisionResult result,
        IReadOnlyList<PermittedAction> candidates)
    {
        var known = candidates
            .Select(static candidate => candidate.Candidate.CandidateId)
            .ToHashSet(StringComparer.Ordinal);
        if ((result.SelectedCandidateId is not null && !known.Contains(result.SelectedCandidateId))
            || result.Distribution.Keys.Any(candidateId => !known.Contains(candidateId)))
        {
            throw new InvalidOperationException("The OpenAI Decision response referenced an unknown candidate.");
        }
    }

    private decimal CalculateCost(long inputTokens, long outputTokens) =>
        inputTokens * options.InputCostPerMillionTokens / TokensPerMillion
        + outputTokens * options.OutputCostPerMillionTokens / TokensPerMillion;
}
