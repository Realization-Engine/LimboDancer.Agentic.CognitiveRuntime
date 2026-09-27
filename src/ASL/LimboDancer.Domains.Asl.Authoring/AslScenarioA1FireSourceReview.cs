using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Authoring;

/// <summary>
/// Delegated, bounded PDF-fidelity disposition for the Fire package's prose sources (unit step 17): the PFPh and DFPh,
/// LOS Hindrance, fire attacks and their results, fire groups and fire direction, TEM, pins, Cowering, morale checks,
/// leadership, concealment loss, ELR, and the TEM and Hindrance rules of the fixture terrain. A.9, A10.1, and B23.3
/// were verified by the step 10 review and are not repeated. The records are separate from the earlier reviews, so
/// their digests are unchanged.
/// </summary>
public static class AslScenarioA1FireSourceReview
{
    public const string ComparisonFile = "asl-scenario-a1.fire-pdf-comparison.json";
    public const string ComparisonSha256 = "00673414dabddc4469018c1abb9664325afac84016e63fc7645c5b84dcc023f4";

    /// <summary>The unit step 18 comparison: Self-Rally (A10.63) and Wounds (A17.1, A17.11, A17.3).</summary>
    public const string BranchesComparisonFile = "asl-scenario-a1.fire-branches-pdf-comparison.json";
    public const string BranchesComparisonSha256 = "417ca7726c27e070543f4b8bf75ef5ff79652e0cf3f3509a1c4da252968af155";

    /// <summary>
    /// The unit step 19 Rally comparison: the RPh, Rally, the terrain bonus, DM, Fate, leader rally, and the Heat of
    /// Battle and Field Promotion rules whose Original 2 the Rally package records as not taken (ruling R0.2).
    /// </summary>
    public const string RallyComparisonFile = "asl-scenario-a1.rally-pdf-comparison.json";
    public const string RallyComparisonSha256 = "0af34cd4bbdb228b0e20c9edcf706a6cb6c2db045b2754de455782d3d782ef06";

    /// <summary>
    /// The unit steps 20 to 23 comparison for the revised Fire package: the MPh and AFPh, movement and its DRM,
    /// Defensive First Fire, Subsequent First Fire, FPF, Residual FP, Final Fire, Advancing Fire and Assault Fire, fire
    /// groups across Locations (ADJACENT), concealment of hidden units and Dummies, support weapons and MGs, and the
    /// MF costs of the admitted terrain.
    /// </summary>
    public const string ExtensionsComparisonFile = "asl-scenario-a1.fire-extensions-pdf-comparison.json";
    public const string ExtensionsComparisonSha256 = "b9cb4d0107c23927e7d9e26541087ebb11e3526063fc220a4fc53d85533e45aa";

    /// <summary>
    /// The unit steps 27 and 28 comparison: heroes (A15.2 to A15.24), Battle Hardening (A15.3), Berserk and Surrender (A15.4,
    /// A15.5, whose results are recorded as not taken), Fanaticism (A10.8), the Leader Creation Table (A18.2), and NKVD MMC
    /// (A25.25).
    /// </summary>
    public const string HeatOfBattleComparisonFile = "asl-scenario-a1.heat-of-battle-pdf-comparison.json";
    public const string HeatOfBattleComparisonSha256 = "41cfbe214712c39d540059e6be5e3fb85da03838a0e341c5d6a95c6ca4da092b";

    /// <summary>
    /// The unit step 29 comparison: Advance (A3.7, A4.7, A4.72), stacking (A1.6, A5.1, A5.5), Close Combat and Withdrawal from Melee
    /// (A11.1 to A11.41), Field Promotion in CC (A18.12), Lax (A19.36), and a pinned leader (A7.831).
    /// </summary>
    public const string CloseCombatComparisonFile = "asl-scenario-a1.close-combat-pdf-comparison.json";
    public const string CloseCombatComparisonSha256 = "2cbb74c2a1af7fe74fe217007c3090f19fde4fb3d492f860e7d865ad1ffce920";

    /// <summary>The unit step 30 comparison: Berserk (A15.41 to A15.46), capture and prisoners (A20.2 to A20.55).</summary>
    public const string BerserkSurrenderComparisonFile = "asl-scenario-a1.berserk-surrender-pdf-comparison.json";
    public const string BerserkSurrenderComparisonSha256 = "4a247af92b724b281954933a5157b88fb8f06d2b246b25f6e50d67651e55061a";

    private const string ChapterA = "asl-easlrb-3.10:chapter-a";
    private const string ChapterB = "asl-easlrb-3.10:chapter-b";

    // Rule, the element id the conversion registered the fragment under, source, line, kind, physical page.
    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] Subjects =
    [
        ("A3.2", "A3.2", ChapterA, 199, SourceFragmentKind.RuleText, 47),
        ("A3.4", "A3.4", ChapterA, 203, SourceFragmentKind.RuleText, 47),
        ("A6.7", "A6.7", ChapterA, 407, SourceFragmentKind.RuleText, 54),
        ("A6.7", "A6.7", ChapterA, 413, SourceFragmentKind.RuleContinuation, 54),
        ("A7.1", "A7.1", ChapterA, 431, SourceFragmentKind.RuleText, 54),
        ("A7.2", "A7.2", ChapterA, 433, SourceFragmentKind.RuleText, 54),
        ("A7.21", "A7.21", ChapterA, 435, SourceFragmentKind.RuleText, 54),
        ("A7.21", "A7.21", ChapterA, 439, SourceFragmentKind.RuleContinuation, 55),
        ("A7.212", "A7.212", ChapterA, 445, SourceFragmentKind.RuleText, 55),
        ("A7.22", "A7.22", ChapterA, 449, SourceFragmentKind.RuleText, 55),
        ("A7.23", "A7.23", ChapterA, 451, SourceFragmentKind.RuleText, 55),
        ("A7.3", "A7.3", ChapterA, 459, SourceFragmentKind.RuleText, 55),
        ("A7.301", "A7.301", ChapterA, 463, SourceFragmentKind.RuleText, 55),
        ("A7.302", "A7.302", ChapterA, 467, SourceFragmentKind.RuleText, 55),
        ("A7.303", "A7.303", ChapterA, 471, SourceFragmentKind.RuleText, 55),
        ("A7.304", "A7.304", ChapterA, 475, SourceFragmentKind.RuleText, 55),
        ("A7.305", "A7.305", ChapterA, 479, SourceFragmentKind.RuleText, 55),
        ("A7.306", "A7.306", ChapterA, 481, SourceFragmentKind.RuleText, 55),
        ("A7.31", "A7.31", ChapterA, 491, SourceFragmentKind.RuleText, 56),
        ("A7.4", "A7.4", ChapterA, 527, SourceFragmentKind.RuleText, 57),
        ("A7.5", "A7.5", ChapterA, 529, SourceFragmentKind.RuleText, 57),
        ("A7.52", "A7.52", ChapterA, 533, SourceFragmentKind.RuleText, 57),
        ("A7.53", "A7.53", ChapterA, 535, SourceFragmentKind.RuleText, 57),
        ("A7.531", "A7.531", ChapterA, 537, SourceFragmentKind.RuleText, 57),
        ("A7.55", "A7.55", ChapterA, 541, SourceFragmentKind.RuleText, 57),
        ("A7.6", "A7.6", ChapterA, 543, SourceFragmentKind.RuleText, 57),
        ("A7.8", "A7.72", ChapterA, 574, SourceFragmentKind.RuleContinuation, 58),
        ("A7.9", "A7.9", ChapterA, 586, SourceFragmentKind.RuleText, 58),
        ("A10.2", "A10.2", ChapterA, 764, SourceFragmentKind.RuleText, 65),
        ("A10.21", "A10.21", ChapterA, 768, SourceFragmentKind.RuleText, 66),
        ("A10.22", "A10.22", ChapterA, 772, SourceFragmentKind.RuleText, 66),
        ("A10.3", "A10.3", ChapterA, 774, SourceFragmentKind.RuleText, 66),
        ("A10.31", "A10.31", ChapterA, 776, SourceFragmentKind.RuleText, 66),
        ("A10.4", "A10.4", ChapterA, 778, SourceFragmentKind.RuleText, 66),
        ("A10.7", "A10.7", ChapterA, 832, SourceFragmentKind.RuleText, 68),
        ("A10.72", "A10.72", ChapterA, 844, SourceFragmentKind.RuleText, 69),
        ("A12.14", "A12.14", ChapterA, 1019, SourceFragmentKind.RuleText, 77),
        ("A12.14", "A12.14", ChapterA, 1021, SourceFragmentKind.RuleContinuation, 77),
        ("A12.14", "A12.14", ChapterA, 1026, SourceFragmentKind.RuleContinuation, 78),
        ("A12.14", "A12.14", ChapterA, 1028, SourceFragmentKind.RuleContinuation, 78),
        ("A12.14", "A12.14", ChapterA, 1034, SourceFragmentKind.RuleContinuation, 78),
        ("A12.141", "A12.141", ChapterA, 1040, SourceFragmentKind.RuleText, 78),
        ("A19.1", "A19.1", ChapterA, 1360, SourceFragmentKind.RuleText, 86),
        ("A19.11", "A19.11", ChapterA, 1362, SourceFragmentKind.RuleText, 86),
        ("A19.12", "A19.12", ChapterA, 1364, SourceFragmentKind.RuleText, 86),
        ("A19.13", "A19.13", ChapterA, 1368, SourceFragmentKind.RuleText, 86),
        ("A.5", "A.5", ChapterA, 31, SourceFragmentKind.RuleText, 43),
        ("A.17", "A.17", ChapterA, 73, SourceFragmentKind.RuleText, 44),
        ("B1.1", "B1.1", ChapterB, 66, SourceFragmentKind.RuleText, 113),
        ("B12.2", "B12.2", ChapterB, 796, SourceFragmentKind.RuleText, 127),
        ("B13.3", "B13.3", ChapterB, 836, SourceFragmentKind.RuleText, 128),
        ("B14.2", "B14.2", ChapterB, 908, SourceFragmentKind.RuleText, 129),
        ("B14.3", "B14.3", ChapterB, 918, SourceFragmentKind.RuleText, 129),
        ("B15.2", "B15.2", ChapterB, 950, SourceFragmentKind.RuleText, 129),
    ];

    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] BranchSubjects =
    [
        ("A10.63", "A10.63", ChapterA, 828, SourceFragmentKind.RuleText, 68),
        ("A17.1", "A17.1", ChapterA, 1316, SourceFragmentKind.RuleText, 85),
        ("A17.11", "A17.11", ChapterA, 1320, SourceFragmentKind.RuleText, 85),
        ("A17.3", "A17.3", ChapterA, 1324, SourceFragmentKind.RuleText, 85),
    ];

    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] RallySubjects =
    [
        ("A3.1", "A3.1", ChapterA, 197, SourceFragmentKind.RuleText, 47),
        ("A10.6", "A10.6", ChapterA, 820, SourceFragmentKind.RuleText, 68),
        ("A10.61", "A10.61", ChapterA, 822, SourceFragmentKind.RuleText, 68),
        ("A10.62", "A10.62", ChapterA, 824, SourceFragmentKind.RuleText, 68),
        ("A10.64", "A10.64", ChapterA, 830, SourceFragmentKind.RuleText, 68),
        ("A10.71", "A10.71", ChapterA, 838, SourceFragmentKind.RuleText, 69),
        ("A15.1", "A15.1", ChapterA, 1214, SourceFragmentKind.RuleText, 83),
        ("A15.1", "A15.1", ChapterA, 1226, SourceFragmentKind.RuleContinuation, 83),
        ("A18.11", "A18.11", ChapterA, 1332, SourceFragmentKind.RuleText, 85),
    ];

    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] ExtensionSubjects =
    [
        ("A3.3", "A3.3", ChapterA, 201, SourceFragmentKind.RuleText, 47),
        ("A3.5", "A3.5", ChapterA, 205, SourceFragmentKind.RuleText, 47),
        ("A.8", "A.8", ChapterA, 41, SourceFragmentKind.RuleText, 43),
        ("A4.1", "A4.1", ChapterA, 231, SourceFragmentKind.RuleText, 48),
        ("A4.11", "A4.11", ChapterA, 233, SourceFragmentKind.RuleText, 48),
        ("A4.13", "A4.13", ChapterA, 237, SourceFragmentKind.RuleText, 48),
        ("A4.132", "A4.132", ChapterA, 241, SourceFragmentKind.RuleText, 48),
        ("A4.2", "A4.2", ChapterA, 265, SourceFragmentKind.RuleText, 49),
        ("A4.4", "A4.4", ChapterA, 285, SourceFragmentKind.RuleText, 50),
        ("A4.6", "A4.6", ChapterA, 311, SourceFragmentKind.RuleText, 51),
        ("A4.61", "A4.61", ChapterA, 313, SourceFragmentKind.RuleText, 51),
        ("A4.61", "A4.61", ChapterA, 315, SourceFragmentKind.RuleContinuation, 51),
        ("A7.24", "A7.24", ChapterA, 453, SourceFragmentKind.RuleText, 55),
        ("A7.35", "A7.35", ChapterA, 503, SourceFragmentKind.RuleText, 56),
        ("A7.351", "A7.351", ChapterA, 509, SourceFragmentKind.RuleText, 56),
        ("A7.352", "A7.352", ChapterA, 511, SourceFragmentKind.RuleText, 56),
        ("A7.353", "A7.353", ChapterA, 513, SourceFragmentKind.RuleText, 56),
        ("A7.36", "A7.36", ChapterA, 515, SourceFragmentKind.RuleText, 56),
        ("A7.372", "A7.372", ChapterA, 525, SourceFragmentKind.RuleText, 57),
        ("A7.83", "A7.83", ChapterA, 582, SourceFragmentKind.RuleText, 58),
        ("A8.1", "A8.1", ChapterA, 592, SourceFragmentKind.RuleText, 59),
        ("A8.11", "A8.11", ChapterA, 594, SourceFragmentKind.RuleText, 59),
        ("A8.12", "A8.12", ChapterA, 596, SourceFragmentKind.RuleText, 59),
        ("A8.13", "A8.13", ChapterA, 598, SourceFragmentKind.RuleText, 59),
        ("A8.14", "A8.14", ChapterA, 600, SourceFragmentKind.RuleText, 59),
        ("A8.2", "A8.2", ChapterA, 608, SourceFragmentKind.RuleText, 60),
        ("A8.21", "A8.21", ChapterA, 618, SourceFragmentKind.RuleText, 60),
        ("A8.22", "A8.22", ChapterA, 620, SourceFragmentKind.RuleText, 60),
        ("A8.22", "A8.22", ChapterA, 622, SourceFragmentKind.RuleContinuation, 60),
        ("A8.26", "A8.26", ChapterA, 644, SourceFragmentKind.RuleText, 60),
        ("A8.3", "A8.3", ChapterA, 656, SourceFragmentKind.RuleText, 61),
        ("A8.31", "A8.31", ChapterA, 658, SourceFragmentKind.RuleText, 61),
        ("A8.4", "A8.4", ChapterA, 668, SourceFragmentKind.RuleText, 61),
        ("A9.1", "A9.1", ChapterA, 682, SourceFragmentKind.RuleText, 62),
        ("A9.11", "A9.11", ChapterA, 686, SourceFragmentKind.RuleText, 62),
        ("A9.2", "A9.2", ChapterA, 690, SourceFragmentKind.RuleText, 62),
        ("A9.3", "A9.3", ChapterA, 716, SourceFragmentKind.RuleText, 63),
        ("A9.7", "A9.7", ChapterA, 742, SourceFragmentKind.RuleText, 65),
        ("A9.71", "A9.71", ChapterA, 744, SourceFragmentKind.RuleText, 65),
        ("A9.72", "A9.72", ChapterA, 746, SourceFragmentKind.RuleText, 65),
        ("A12.11", "A12.11", ChapterA, 992, SourceFragmentKind.RuleText, 76),
        ("A12.13", "A12.13", ChapterA, 1017, SourceFragmentKind.RuleText, 77),
        ("A12.3", "A12.3", ChapterA, 1084, SourceFragmentKind.RuleText, 80),
        ("A12.31", "A12.31", ChapterA, 1088, SourceFragmentKind.RuleText, 80),
        ("B3.4", "B3.4", ChapterB, 138, SourceFragmentKind.RuleText, 114),
        ("B12.4", "B12.4", ChapterB, 804, SourceFragmentKind.RuleText, 127),
        ("B13.4", "B13.4", ChapterB, 848, SourceFragmentKind.RuleText, 128),
        ("B14.4", "B14.4", ChapterB, 922, SourceFragmentKind.RuleText, 129),
        ("B15.4", "B15.4", ChapterB, 962, SourceFragmentKind.RuleText, 129),
        ("B23.4", "B23.4", ChapterB, 1382, SourceFragmentKind.RuleText, 136),
    ];

    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] HeatOfBattleSubjects =
    [
        ("A15.2", "A15.2", ChapterA, 1228, SourceFragmentKind.RuleText, 83),
        ("A15.21", "A15.21", ChapterA, 1234, SourceFragmentKind.RuleText, 83),
        ("A15.21", "A15.21", ChapterA, 1240, SourceFragmentKind.RuleContinuation, 83),
        ("A15.23", "A15.23", ChapterA, 1242, SourceFragmentKind.RuleText, 83),
        ("A15.24", "A15.24", ChapterA, 1244, SourceFragmentKind.RuleText, 83),
        ("A15.3", "A15.3", ChapterA, 1246, SourceFragmentKind.RuleText, 83),
        ("A15.4", "A15.4", ChapterA, 1248, SourceFragmentKind.RuleText, 83),
        ("A15.5", "A15.5", ChapterA, 1276, SourceFragmentKind.RuleText, 84),
        ("A18.2", "A18.2", ChapterA, 1340, SourceFragmentKind.RuleText, 85),
        ("A18.2", "A18.2", ChapterA, 1356, SourceFragmentKind.RuleContinuation, 86),
        ("A10.8", "A10.8", ChapterA, 846, SourceFragmentKind.RuleText, 69),
        ("A25.25", "A25.25", ChapterA, 1758, SourceFragmentKind.RuleText, 96),
    ];

    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] CloseCombatSubjects =
    [
        ("A1.6", "A1.6", ChapterA, 133, SourceFragmentKind.RuleText, 45),
        ("A3.7", "A3.7", ChapterA, 209, SourceFragmentKind.RuleText, 47),
        ("A3.8", "A3.8", ChapterA, 211, SourceFragmentKind.RuleText, 47),
        ("A4.7", "A4.7", ChapterA, 329, SourceFragmentKind.RuleText, 52),
        ("A4.72", "A4.72", ChapterA, 333, SourceFragmentKind.RuleText, 52),
        ("A5.1", "A5.1", ChapterA, 341, SourceFragmentKind.RuleText, 52),
        ("A5.5", "A5.5", ChapterA, 363, SourceFragmentKind.RuleText, 53),
        ("A11.1", "A11.1", ChapterA, 876, SourceFragmentKind.RuleText, 72),
        ("A11.11", "A11.11", ChapterA, 878, SourceFragmentKind.RuleText, 72),
        ("A11.12", "A11.12", ChapterA, 880, SourceFragmentKind.RuleText, 72),
        ("A11.13", "A11.13", ChapterA, 884, SourceFragmentKind.RuleText, 72),
        ("A11.14", "A11.14", ChapterA, 886, SourceFragmentKind.RuleText, 72),
        ("A11.141", "A11.141", ChapterA, 888, SourceFragmentKind.RuleText, 72),
        ("A11.15", "A11.15", ChapterA, 890, SourceFragmentKind.RuleText, 72),
        ("A11.16", "A11.16", ChapterA, 894, SourceFragmentKind.RuleText, 72),
        ("A11.17", "A11.17", ChapterA, 898, SourceFragmentKind.RuleText, 73),
        ("A11.18", "A11.18", ChapterA, 900, SourceFragmentKind.RuleText, 73),
        ("A11.19", "A11.19", ChapterA, 902, SourceFragmentKind.RuleText, 73),
        ("A11.2", "A11.2", ChapterA, 906, SourceFragmentKind.RuleText, 73),
        ("A11.21", "A11.21", ChapterA, 908, SourceFragmentKind.RuleText, 73),
        ("A11.22", "A11.22", ChapterA, 910, SourceFragmentKind.RuleText, 73),
        ("A11.3", "A11.3", ChapterA, 912, SourceFragmentKind.RuleText, 73),
        ("A11.32", "A11.32", ChapterA, 916, SourceFragmentKind.RuleText, 73),
        ("A11.4", "A11.4", ChapterA, 922, SourceFragmentKind.RuleText, 73),
        ("A11.41", "A11.41", ChapterA, 940, SourceFragmentKind.RuleText, 73),
        ("A18.12", "A18.12", ChapterA, 1336, SourceFragmentKind.RuleText, 85),
        ("A19.36", "A19.36", ChapterA, 1394, SourceFragmentKind.RuleText, 86),
        ("A7.831", "A7.831", ChapterA, 584, SourceFragmentKind.RuleText, 58),
    ];

    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] BerserkSurrenderSubjects =
    [
        ("A15.41", "A15.41", ChapterA, 1252, SourceFragmentKind.RuleText, 83),
        ("A15.42", "A15.42", ChapterA, 1256, SourceFragmentKind.RuleText, 84),
        ("A15.43", "A15.43", ChapterA, 1258, SourceFragmentKind.RuleText, 84),
        ("A15.431", "A15.431", ChapterA, 1262, SourceFragmentKind.RuleText, 84),
        ("A15.432", "A15.432", ChapterA, 1266, SourceFragmentKind.RuleText, 84),
        ("A15.44", "A15.44", ChapterA, 1270, SourceFragmentKind.RuleText, 84),
        ("A15.45", "A15.45", ChapterA, 1272, SourceFragmentKind.RuleText, 84),
        ("A15.46", "A15.46", ChapterA, 1274, SourceFragmentKind.RuleText, 84),
        ("A20.2", "A20.2", ChapterA, 1400, SourceFragmentKind.RuleText, 86),
        ("A20.21", "A20.21", ChapterA, 1402, SourceFragmentKind.RuleText, 86),
        ("A20.24", "A20.24", ChapterA, 1414, SourceFragmentKind.RuleText, 87),
        ("A20.5", "A20.5", ChapterA, 1420, SourceFragmentKind.RuleText, 87),
        ("A20.51", "A20.51", ChapterA, 1426, SourceFragmentKind.RuleText, 87),
        ("A20.52", "A20.52", ChapterA, 1428, SourceFragmentKind.RuleText, 87),
        ("A20.53", "A20.53", ChapterA, 1430, SourceFragmentKind.RuleText, 87),
        ("A20.54", "A20.54", ChapterA, 1432, SourceFragmentKind.RuleText, 87),
        ("A20.54", "A20.54", ChapterA, 1438, SourceFragmentKind.RuleContinuation, 88),
        ("A20.55", "A20.55", ChapterA, 1440, SourceFragmentKind.RuleText, 88),
    ];

    // Fragments a column break, a boxed example, or a page break interrupts: each part occurs whole in the page text.
    private static readonly HashSet<(string Rule, int Line)> TwoPartSubjects =
        [("A7.212", 445), ("A8.26", 644), ("A8.31", 658), ("A9.2", 690), ("B3.4", 138), ("A12.11", 992), ("A11.41", 940), ("A20.21", 1402)];

    /// <summary>The verified fragments, in subject order, keyed by rule id for the Fire package.</summary>
    public static IReadOnlyList<(string Rule, int Page, SourceFragment Fragment)> Fragments(GeneratedManifests manifests)
    {
        ArgumentNullException.ThrowIfNull(manifests);
        return Subjects.Select(subject => (subject.Rule, subject.Page, Find(manifests, subject))).ToArray();
    }

    public static AslScenarioA1VerificationBatch Build(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, ComparisonFile, ComparisonSha256, Subjects, "unit step 17 Fire review");

    /// <summary>The unit step 18 subjects that close the Fire package's undecided branches.</summary>
    public static AslScenarioA1VerificationBatch BuildBranches(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, BranchesComparisonFile, BranchesComparisonSha256, BranchSubjects,
            "unit step 18 Fire branches review");

    /// <summary>The unit step 19 subjects of the Rally package.</summary>
    public static AslScenarioA1VerificationBatch BuildRally(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, RallyComparisonFile, RallyComparisonSha256, RallySubjects, "unit step 19 Rally review");

    /// <summary>The unit steps 20 to 23 subjects of the revised Fire package.</summary>
    public static AslScenarioA1VerificationBatch BuildExtensions(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, ExtensionsComparisonFile, ExtensionsComparisonSha256, ExtensionSubjects,
            "unit steps 20 to 23 Fire extensions review");

    /// <summary>The unit steps 27 and 28 subjects of Heat of Battle, heroes, Battle Hardening, and Leader Creation.</summary>
    public static AslScenarioA1VerificationBatch BuildHeatOfBattle(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, HeatOfBattleComparisonFile, HeatOfBattleComparisonSha256, HeatOfBattleSubjects,
            "unit steps 27 and 28 Heat of Battle review");

    /// <summary>The unit step 29 subjects of Advance and Close Combat.</summary>
    public static AslScenarioA1VerificationBatch BuildCloseCombat(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, CloseCombatComparisonFile, CloseCombatComparisonSha256, CloseCombatSubjects,
            "unit step 29 Close Combat review");

    /// <summary>The unit step 30 subjects of Berserk, Surrender, and prisoners.</summary>
    public static AslScenarioA1VerificationBatch BuildBerserkSurrender(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, BerserkSurrenderComparisonFile, BerserkSurrenderComparisonSha256, BerserkSurrenderSubjects,
            "unit step 30 Berserk and Surrender review");

    private static AslScenarioA1VerificationBatch Build(string repositoryRoot, GeneratedManifests manifests,
        AslScenarioA1SourceAttestation attestation, string file, string digest,
        (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] subjectList, string review)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(manifests);
        var path = Path.Combine(repositoryRoot, "docs", "ASL", "SourceRegistry", file);
        Require(Hashing.Sha256File(path) == digest, "The reviewed Fire PDF comparison changed.");

        using var report = JsonDocument.Parse(File.ReadAllText(path));
        var root = report.RootElement;
        Require(root.GetProperty("pdfSha256").GetString() == AslScenarioA1SourceInventory.PdfDigest
            && root.GetProperty("status").GetString() == "pdf-comparison-complete-delegated-xunit-review",
            "The reviewed PDF identity or status changed.");

        var source = AslScenarioA1VerificationBatchBuilder.Build(manifests, attestation);
        var subjects = root.GetProperty("subjects").EnumerateArray().ToArray();
        Require(subjects.Length == subjectList.Length, "The Fire source subjects changed.");

        var records = new List<TirSourceVerificationRecord>();
        for (var index = 0; index < subjectList.Length; index++)
        {
            var subject = subjectList[index];
            var evidence = subjects[index];
            var fragment = Find(manifests, subject);
            var registered = evidence.TryGetProperty("registeredElementId", out var element)
                ? element.GetString()
                : subject.Rule;
            Require(evidence.GetProperty("ruleId").GetString() == subject.Rule
                && registered == subject.Registered
                && evidence.GetProperty("startLine").GetInt32() == subject.Line
                && evidence.GetProperty("endLine").GetInt32() == fragment.Locator.EndLine
                && evidence.GetProperty("physicalPdfPage").GetInt32() == subject.Page
                && evidence.GetProperty("fragmentId").GetString() == fragment.FragmentId
                && evidence.GetProperty("sourceId").GetString() == fragment.SourceId
                && evidence.GetProperty("sourceSha256").GetString() == fragment.SourceSha256
                && evidence.GetProperty("contentSha256").GetString() == fragment.ContentSha256
                && evidence.GetProperty("kind").GetString() == (subject.Kind == SourceFragmentKind.RuleText ? "ruleText" : "ruleContinuation"),
                "Comparison no longer names the exact registered fragment.");

            var normalized = Normalize(fragment.Content);
            var comparison = evidence.GetProperty("comparison").GetString();
            Require(evidence.GetProperty("normalizedAlphanumericSha256").GetString() == Hashing.Sha256Text(normalized)
                && evidence.GetProperty("normalizedAlphanumericLength").GetInt32() == normalized.Length
                && (comparison == "complete-alphanumeric-match"
                    || (comparison == "complete-alphanumeric-match-in-two-parts" && TwoPartSubjects.Contains((subject.Rule, subject.Line))
                        && evidence.GetProperty("partLengths").EnumerateArray().Sum(part => part.GetInt32()) == normalized.Length)),
                "The reviewed text differs from the PDF comparison.");

            var artifact = source.SourceDocument.Artifacts.OfType<TirSourceFragmentArtifact>()
                .Single(item => item.Envelope.SourceFragments.Count == 1
                    && item.Envelope.SourceFragments[0].FragmentId == fragment.FragmentId);
            records.Add(TirSourceVerificationService.CreateRecord(
                source.SourceDocument, artifact.Envelope.ArtifactId, manifests.Registry, fragment,
                $"Delegated xUnit source review; PDF SHA-256 {AslScenarioA1SourceInventory.PdfDigest}; "
                    + $"physical PDF page {subject.Page}; comparison SHA-256 {digest}. "
                    + $"Source for {subject.Rule} in the {review}.",
                TirSourceVerificationDisposition.Verified, null, [],
                new DateTimeOffset(2026, 9, 26, 0, 0, 0, TimeSpan.Zero),
                "user-directed-xunit-review-2026-09-26", "source-provider:delegated-xunit-review"));
        }

        return new AslScenarioA1VerificationBatch(source.SourceDocument, records);
    }

    private static SourceFragment Find(GeneratedManifests manifests,
        (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page) subject) =>
        manifests.Fragments.SingleOrDefault(candidate =>
            candidate.SourceId == subject.Source && candidate.Locator.StartLine == subject.Line
            && candidate.Kind == subject.Kind && candidate.Locator.NormalizedElementId == subject.Registered)
        ?? throw new InvalidOperationException($"The exact source fragment for {subject.Rule} is missing.");

    private static string Normalize(string value)
    {
        var withoutMarkup = Regex.Replace(value, "<[^>]+>", string.Empty)
            .Replace("ﬂ", "fl", StringComparison.Ordinal)
            .Replace("ﬁ", "fi", StringComparison.Ordinal)
            .Normalize(NormalizationForm.FormKD);
        return new string(withoutMarkup.Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant).ToArray());
    }

    private static void Require(bool valid, string message)
    {
        if (!valid)
        {
            throw new InvalidOperationException(message);
        }
    }
}
