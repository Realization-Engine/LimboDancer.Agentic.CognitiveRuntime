using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class DiagnosticSummaryTests
{
    [Fact]
    public void GroupsByCodeAndSeverityMostSevereFirst()
    {
        MapDiagnostic[] diagnostics =
        [
            new("VASL-META-003", MapDiagnosticSeverity.Info, "colors"),
            new("VASL-META-004", MapDiagnosticSeverity.Warning, "override A1"),
            new("VASL-META-003", MapDiagnosticSeverity.Info, "overlay rules"),
            new("VASL-META-004", MapDiagnosticSeverity.Warning, "override B2"),
            new("VASL-META-003", MapDiagnosticSeverity.Info, "color SSR"),
            new("VASL-LOS-005", MapDiagnosticSeverity.Error, "rows"),
        ];

        var groups = DiagnosticSummary.Group(diagnostics);

        Assert.Equal(["VASL-LOS-005 (error)", "2 × VASL-META-004 (warning)", "3 × VASL-META-003 (info)"], groups.Select(group => group.Label));
        Assert.Equal(["colors", "overlay rules", "color SSR"], groups[2].Messages);
    }

    [Fact]
    public void EmptyInputGivesNoGroups() => Assert.Empty(DiagnosticSummary.Group([]));
}
