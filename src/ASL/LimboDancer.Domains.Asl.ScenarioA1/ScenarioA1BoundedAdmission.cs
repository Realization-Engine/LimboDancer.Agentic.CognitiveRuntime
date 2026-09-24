using System.Security.Cryptography;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Delegated approval of seven exact case labels, not publication of an ASL package.</summary>
public static class ScenarioA1BoundedAdmission
{
    public const string Sha256 = "007dbe2512c7bd5f0b17c51ad6ab2cea800b4f8e2c47267fb6cc0e5fbc57b2ee";
    private static readonly string[] Accepted =
    [
        "A1-empty-ordinary-mph", "A1-known-enemy-mmc-mph",
        "A1-fortified-unbreached-enemy-squad", "A1-single-known-enemy-smc-overrun",
        "A1-fortified-breached-entry", "A1-stacking-equivalents-needed", "A1-advance-phase-entry",
    ];
    private static readonly string[] NonDefinitive =
    [
        "A1-concealed-occupancy-attempt", "A1-unknown-location-or-modifier",
    ];

    public static IReadOnlySet<string> Validate(string manifestRootSha256,
        IReadOnlyCollection<ScenarioA1SemanticCase> cases)
    {
        using var stream = typeof(ScenarioA1BoundedAdmission).Assembly
            .GetManifestResourceStream("ScenarioA1.bounded-admission.json")
            ?? throw new InvalidOperationException("The bounded admission record is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var data = buffer.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(data)) != Sha256)
        {
            throw new InvalidOperationException("The delegated admission record changed.");
        }
        using var document = JsonDocument.Parse(data);
        var root = document.RootElement;
        if (root.GetProperty("schemaVersion").GetString() != "1.0.0"
            || root.GetProperty("status").GetString() != "accepted-bounded-case-profile-by-delegated-xunit-review"
            || root.GetProperty("authority").GetString() != "user-directed-xunit-review-2026-09-24"
            || root.GetProperty("candidateManifestRootSha256").GetString() != manifestRootSha256
            || root.GetProperty("verifiedSourceSubjectCount").GetInt32() != 28
            || !Ids(root, "acceptedCaseIds").SequenceEqual(Accepted)
            || !Ids(root, "nonDefinitiveCaseIds").SequenceEqual(NonDefinitive)
            || cases.Count != 9
            || cases.Any(item =>
                item.ReviewStatus != (Accepted.Contains(item.Id, StringComparer.Ordinal)
                    ? "reviewed-bounded" : "reviewed-nondefinitive"))
            || !cases.Select(item => item.Id).ToHashSet(StringComparer.Ordinal)
                .SetEquals(Accepted.Concat(NonDefinitive)))
        {
            throw new InvalidOperationException("The case inventory differs from bounded admission.");
        }
        return Accepted.ToHashSet(StringComparer.Ordinal);
    }

    private static string[] Ids(JsonElement root, string name) => root.GetProperty(name)
        .EnumerateArray().Select(item => item.GetString()!).ToArray();
}
