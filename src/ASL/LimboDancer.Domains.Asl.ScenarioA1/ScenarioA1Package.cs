using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>A read-only, exact-version projection of the delegated first-case decision.</summary>
public sealed class ScenarioA1Package : IDomainPackageResolver
{
    public const string DecisionSha256 = "75d6ec0573af0a251d6261515c7559237f71f9272453b53cc988567205936007";
    public static readonly DomainPackageRef Identity = new(new DomainId("asl"),
        "scenario-a1-declared-first-case", "sha256:" + DecisionSha256);
    private readonly DomainPackageDescriptor _descriptor;

    public ScenarioA1Package()
    {
        using var stream = typeof(ScenarioA1Package).Assembly.GetManifestResourceStream("ScenarioA1.review.json")
            ?? throw new InvalidOperationException("The reviewed decision is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        if (!string.Equals(Convert.ToHexStringLower(SHA256.HashData(buffer.ToArray())),
                DecisionSha256, StringComparison.Ordinal))
            throw new InvalidOperationException("The reviewed decision changed.");
        using var decision = JsonDocument.Parse(buffer.ToArray());
        var root = decision.RootElement;
        if (root.GetProperty("status").GetString() != "accepted-declared-case"
            || root.GetProperty("newlyVerifiedFragmentCount").GetInt32() != 10)
            throw new InvalidOperationException("The bounded decision has not been accepted.");
        var references = root.GetProperty("requiredRuleIds").EnumerateArray()
            .Select(rule => new CanonicalReference(Identity,
                rule.GetString()!.StartsWith('B') ? "asl-easlrb-3.10:chapter-b" : "asl-easlrb-3.10:chapter-a",
                rule.GetString()!, "3.01"))
            .Append(new CanonicalReference(Identity, "asl-easlrb-3.10:b-terrain-chart",
                "23. Wooden Building / 23. Stone Building", "3.01"));
        _descriptor = new DomainPackageDescriptor(Identity, references);
    }

    public ValueTask<DomainPackageResolution> ResolveAsync(DomainPackageRef requested,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(requested);
        return ValueTask.FromResult(requested == Identity
            ? new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Resolved,
                _descriptor, "asl.a1.exact-package")
            : new DomainPackageResolution(requested, DomainPackageResolutionOutcome.Unavailable,
                null, "asl.a1.package-unavailable"));
    }
}
