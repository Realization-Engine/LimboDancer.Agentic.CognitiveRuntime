using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Runtime.Diagnostics;

public sealed record DiagnosticPolicyContext(
    DiagnosticReference Reference,
    ActionRiskProfile? ActionRisk);
