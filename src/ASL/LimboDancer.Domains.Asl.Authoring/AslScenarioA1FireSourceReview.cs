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

    /// <summary>
    /// The unit step 24 comparison: a Gun's HE shot at Infantry: ordnance (C.3 to C.6), Gun counters (C2.1 to C2.6), the To Hit
    /// process (C3.2 to C3.8), the Basic TH# modifications (C4), the firer- and target-based DRM of the reviewed Cases (C5, C6),
    /// crews (A1.123), and non-qualified use (A21.13).
    /// </summary>
    public const string OrdnanceComparisonFile = "asl-scenario-a1.ordnance-pdf-comparison.json";
    public const string OrdnanceComparisonSha256 = "cebc0e782dd0a67ec7fa65f04ecc0eab1ee94800a260e5395e95f5a4b107c662";

    /// <summary>
    /// The unit step 25 comparison: vehicles in the target Location (A7.307 to A7.309), Collateral Attacks on a CE crew (D.8, D5.3 to
    /// D5.341, A10.31, A7.82, A15.1), a vehicle's MG (D1.83, D3.5 to D3.7), vehicle movement (D2.1 to D2.6), Residual FP (A8.2), and an
    /// AFV's cover for Infantry (D9.3, D9.4).
    /// </summary>
    public const string VehicleComparisonFile = "asl-scenario-a1.vehicle-pdf-comparison.json";
    public const string VehicleComparisonSha256 = "c9f59e1ca5009c2fc8fd0be8583016d1c9059afcd20e73cfaf1d4aacd0021559";

    /// <summary>
    /// The backlog pass 5 comparison: exit (A2.6), portage and possession (A4.42 to A4.431), Double Time and CX (A4.5 to A4.72), a HS's SW
    /// (A7.302), the Unlikely Kill (A7.309), follow-up fire (A8.14), withdrawal (A11.21), Heat of Battle and Battle Hardening (A15.1 to A15.5),
    /// Leader Creation (A18.11), No Quarter and Massacre (A20.3, A20.4), Acquisition (C6.5, C6.51), MP left and Motion (D2.1, D2.4), the Recall
    /// and Abandonment (D5.341, D5.41), and grain out of season (B15.6).
    /// </summary>
    public const string Pass5ComparisonFile = "asl-scenario-a1.pass5-pdf-comparison.json";
    public const string Pass5ComparisonSha256 = "10c5936cfb3c2d0f3d01c0b342322eb77c1a65e82e559b197817f611fb74640b";
    public const string Pass6ComparisonFile = "asl-scenario-a1.pass6-pdf-comparison.json";
    public const string Pass6ComparisonSha256 = "adb5ff41193f76d9a3e7ec4bfda47788a4d64ece2cb24d9811d10ec168b2a081";

    /// <summary>
    /// The backlog pass 7 comparison: the Vehicle Target Type (C3.31), Target Facing and hit location (C3.9, C5.9), To Kill and its
    /// modifications (C7.1 to C7.35), To Kill results and Shock (C7.4 to C7.7), Special Ammunition and Depletion Numbers (C8.1 to C8.91),
    /// and tank main armament, armor, and crews (D1.3 to D1.74, D3.12, D3.2, D5.2 to D5.7).
    /// </summary>
    public const string Pass7ComparisonFile = "asl-scenario-a1.pass7-pdf-comparison.json";
    public const string Pass7ComparisonSha256 = "c778b4f77726489c421b56a76d8dbad2270a70a18844f02e80381fef43a298e7";

    private const string ChapterA = "asl-easlrb-3.10:chapter-a";
    private const string ChapterB = "asl-easlrb-3.10:chapter-b";
    private const string ChapterC = "asl-easlrb-3.10:chapter-c";
    private const string ChapterD = "asl-easlrb-3.10:chapter-d";

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

    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] VehicleSubjects =
    [
        ("A4.6", "A4.6", ChapterA, 311, SourceFragmentKind.RuleText, 51),
        ("A7.307", "A7.307", ChapterA, 483, SourceFragmentKind.RuleText, 55),
        ("A7.308", "A7.308", ChapterA, 485, SourceFragmentKind.RuleText, 55),
        ("A7.309", "A7.309", ChapterA, 487, SourceFragmentKind.RuleText, 56),
        ("A7.82", "A7.82", ChapterA, 578, SourceFragmentKind.RuleText, 58),
        ("A7.9", "A7.9", ChapterA, 586, SourceFragmentKind.RuleText, 58),
        ("A8.2", "A8.2", ChapterA, 608, SourceFragmentKind.RuleText, 60),
        ("A10.31", "A10.31", ChapterA, 776, SourceFragmentKind.RuleText, 66),
        ("A15.1", "A15.1", ChapterA, 1214, SourceFragmentKind.RuleText, 83),
        ("D.3", "D.3", ChapterD, 22, SourceFragmentKind.RuleText, 192),
        ("D.6", "D.6", ChapterD, 34, SourceFragmentKind.RuleText, 192),
        ("D.7", "D.7", ChapterD, 36, SourceFragmentKind.RuleText, 192),
        ("D.8", "D.8", ChapterD, 38, SourceFragmentKind.RuleText, 192),
        ("D.8", "D.8", ChapterD, 40, SourceFragmentKind.RuleContinuation, 192),
        ("D.8", "D.8", ChapterD, 42, SourceFragmentKind.RuleContinuation, 192),
        ("D1.21", "D1.21", ChapterD, 99, SourceFragmentKind.RuleText, 193),
        ("D1.23", "D1.23", ChapterD, 111, SourceFragmentKind.RuleText, 194),
        ("D1.8", "D1.8", ChapterD, 195, SourceFragmentKind.RuleText, 195),
        ("D1.83", "D1.83", ChapterD, 201, SourceFragmentKind.RuleText, 195),
        ("D2.1", "D2.1", ChapterD, 211, SourceFragmentKind.RuleText, 195),
        ("D2.11", "D2.11", ChapterD, 213, SourceFragmentKind.RuleText, 195),
        ("D2.12", "D2.12", ChapterD, 219, SourceFragmentKind.RuleText, 195),
        ("D2.13", "D2.13", ChapterD, 225, SourceFragmentKind.RuleText, 196),
        ("D2.14", "D2.14", ChapterD, 227, SourceFragmentKind.RuleText, 196),
        ("D2.16", "D2.16", ChapterD, 231, SourceFragmentKind.RuleText, 196),
        ("D2.2", "D2.2", ChapterD, 239, SourceFragmentKind.RuleText, 196),
        ("D2.4", "D2.4", ChapterD, 295, SourceFragmentKind.RuleText, 198),
        ("D2.41", "D2.41", ChapterD, 301, SourceFragmentKind.RuleText, 198),
        ("D2.42", "D2.42", ChapterD, 303, SourceFragmentKind.RuleText, 198),
        ("D2.6", "D2.6", ChapterD, 325, SourceFragmentKind.RuleText, 199),
        ("D3.11", "D3.11", ChapterD, 335, SourceFragmentKind.RuleText, 199),
        ("D3.5", "D3.5", ChapterD, 383, SourceFragmentKind.RuleText, 200),
        ("D3.53", "D3.53", ChapterD, 397, SourceFragmentKind.RuleText, 201),
        ("D3.7", "D3.7", ChapterD, 407, SourceFragmentKind.RuleText, 201),
        ("D5.1", "D5.1", ChapterD, 486, SourceFragmentKind.RuleText, 203),
        ("D5.2", "D5.2", ChapterD, 490, SourceFragmentKind.RuleText, 203),
        ("D5.3", "D5.3", ChapterD, 496, SourceFragmentKind.RuleText, 203),
        ("D5.31", "D5.31", ChapterD, 504, SourceFragmentKind.RuleText, 203),
        ("D5.311", "D5.311", ChapterD, 506, SourceFragmentKind.RuleText, 203),
        ("D5.33", "D5.33", ChapterD, 510, SourceFragmentKind.RuleText, 203),
        ("D5.34", "D5.34", ChapterD, 512, SourceFragmentKind.RuleText, 203),
        ("D5.341", "D5.341", ChapterD, 518, SourceFragmentKind.RuleText, 203),
        ("D5.341", "D5.341", ChapterD, 526, SourceFragmentKind.RuleContinuation, 204),
        ("D9.3", "D9.3", ChapterD, 766, SourceFragmentKind.RuleText, 209),
        ("D9.4", "D9.4", ChapterD, 770, SourceFragmentKind.RuleText, 210),
    ];

    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] Pass5Subjects =
    [
        ("A2.6", "A2.6", ChapterA, 161, SourceFragmentKind.RuleText, 46),
        ("A2.6", "A2.6", ChapterA, 165, SourceFragmentKind.RuleContinuation, 46),
        ("A4.42", "A4.42", ChapterA, 289, SourceFragmentKind.RuleText, 50),
        ("A4.43", "A4.43", ChapterA, 291, SourceFragmentKind.RuleText, 50),
        ("A4.43", "A4.43", ChapterA, 295, SourceFragmentKind.RuleContinuation, 50),
        ("A4.431", "A4.431", ChapterA, 297, SourceFragmentKind.RuleText, 50),
        ("A4.5", "A4.5", ChapterA, 303, SourceFragmentKind.RuleText, 51),
        ("A4.51", "A4.51", ChapterA, 305, SourceFragmentKind.RuleText, 51),
        ("A4.52", "A4.52", ChapterA, 309, SourceFragmentKind.RuleText, 51),
        ("A4.72", "A4.72", ChapterA, 333, SourceFragmentKind.RuleText, 52),
        ("A7.302", "A7.302", ChapterA, 467, SourceFragmentKind.RuleText, 55),
        ("A7.309", "A7.309", ChapterA, 487, SourceFragmentKind.RuleText, 56),
        ("A8.14", "A8.14", ChapterA, 600, SourceFragmentKind.RuleText, 59),
        ("A11.21", "A11.21", ChapterA, 908, SourceFragmentKind.RuleText, 73),
        ("A15.1", "A15.1", ChapterA, 1214, SourceFragmentKind.RuleText, 83),
        ("A15.1", "A15.1", ChapterA, 1226, SourceFragmentKind.RuleContinuation, 83),
        ("A15.21", "A15.21", ChapterA, 1234, SourceFragmentKind.RuleText, 83),
        ("A15.21", "A15.21", ChapterA, 1240, SourceFragmentKind.RuleContinuation, 83),
        ("A15.3", "A15.3", ChapterA, 1246, SourceFragmentKind.RuleText, 83),
        ("A15.5", "A15.5", ChapterA, 1276, SourceFragmentKind.RuleText, 84),
        ("A18.11", "A18.11", ChapterA, 1332, SourceFragmentKind.RuleText, 85),
        ("A20.3", "A20.3", ChapterA, 1416, SourceFragmentKind.RuleText, 87),
        ("A20.4", "A20.4", ChapterA, 1418, SourceFragmentKind.RuleText, 87),
        ("C6.5", "C6.5", ChapterC, 534, SourceFragmentKind.RuleText, 174),
        ("C6.51", "C6.51", ChapterC, 542, SourceFragmentKind.RuleText, 174),
        ("D2.1", "D2.1", ChapterD, 211, SourceFragmentKind.RuleText, 195),
        ("D2.4", "D2.4", ChapterD, 295, SourceFragmentKind.RuleText, 198),
        ("D5.341", "D5.341", ChapterD, 518, SourceFragmentKind.RuleText, 203),
        ("D5.341", "D5.341", ChapterD, 526, SourceFragmentKind.RuleContinuation, 204),
        ("D5.41", "D5.41", ChapterD, 538, SourceFragmentKind.RuleText, 204),
        ("B15.6", "B15.6", ChapterB, 968, SourceFragmentKind.RuleText, 129),
    ];

    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] Pass6Subjects =
    [
        ("A8.222", "A8.222", ChapterA, 630, SourceFragmentKind.RuleText, 60),
        ("D10.1", "D10.1", ChapterD, 790, SourceFragmentKind.RuleText, 210),
        ("D10.2", "D10.2", ChapterD, 794, SourceFragmentKind.RuleText, 210),
        ("D10.3", "D10.3", ChapterD, 796, SourceFragmentKind.RuleText, 210),
        ("B25.14", "B25.14", ChapterB, 1683, SourceFragmentKind.RuleText, 143),
        ("B25.141", "B25.141", ChapterB, 1685, SourceFragmentKind.RuleText, 143),
        ("B25.2", "B25.2", ChapterB, 1691, SourceFragmentKind.RuleText, 143),
        ("A24.2", "A24.2", ChapterA, 1568, SourceFragmentKind.RuleText, 91),
        ("A24.8", "A24.8", ChapterA, 1608, SourceFragmentKind.RuleText, 93),
        ("A12.2", "A12.2", ChapterA, 1082, SourceFragmentKind.RuleText, 79),
        ("A12.12", "A12.12", ChapterA, 994, SourceFragmentKind.RuleText, 77),
        ("A12.4", "A12.4", ChapterA, 1098, SourceFragmentKind.RuleText, 80),
        ("A12.41", "A12.41", ChapterA, 1102, SourceFragmentKind.RuleText, 80),
        ("D3.3", "D3.3", ChapterD, 347, SourceFragmentKind.RuleText, 199),
    ];

    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] OrdnanceSubjects =
    [
        ("A1.123", "A1.123", ChapterA, 99, SourceFragmentKind.RuleText, 44),
        ("A21.13", "A21.13", ChapterA, 1454, SourceFragmentKind.RuleText, 88),
        ("C.3", "C.3", ChapterC, 22, SourceFragmentKind.RuleText, 162),
        ("C.4", "C.4", ChapterC, 24, SourceFragmentKind.RuleText, 162),
        ("C.6", "C.6", ChapterC, 34, SourceFragmentKind.RuleText, 162),
        ("C2.1", "C2.1", ChapterC, 230, SourceFragmentKind.RuleText, 167),
        ("C2.21", "C2.21", ChapterC, 246, SourceFragmentKind.RuleText, 167),
        ("C2.22", "C2.22", ChapterC, 254, SourceFragmentKind.RuleText, 167),
        ("C2.23", "C2.23", ChapterC, 256, SourceFragmentKind.RuleText, 167),
        ("C2.24", "C2.24", ChapterC, 258, SourceFragmentKind.RuleText, 167),
        ("C2.28", "C2.28", ChapterC, 278, SourceFragmentKind.RuleText, 168),
        ("C2.3", "C2.3", ChapterC, 282, SourceFragmentKind.RuleText, 168),
        ("C2.5", "C2.5", ChapterC, 286, SourceFragmentKind.RuleText, 168),
        ("C2.6", "C2.6", ChapterC, 288, SourceFragmentKind.RuleText, 168),
        ("C3.2", "C3.2", ChapterC, 308, SourceFragmentKind.RuleText, 169),
        ("C3.21", "C3.21", ChapterC, 314, SourceFragmentKind.RuleText, 169),
        ("C3.3", "C3.3", ChapterC, 318, SourceFragmentKind.RuleText, 169),
        ("C3.32", "C3.32", ChapterC, 322, SourceFragmentKind.RuleText, 169),
        ("C3.4", "C3.4", ChapterC, 338, SourceFragmentKind.RuleText, 170),
        ("C3.5", "C3.5", ChapterC, 342, SourceFragmentKind.RuleText, 170),
        ("C3.51", "C3.51", ChapterC, 344, SourceFragmentKind.RuleText, 170),
        ("C3.52", "C3.52", ChapterC, 346, SourceFragmentKind.RuleText, 170),
        ("C3.53", "C3.53", ChapterC, 348, SourceFragmentKind.RuleText, 170),
        ("C3.6", "C3.6", ChapterC, 350, SourceFragmentKind.RuleText, 170),
        ("C3.7", "C3.7", ChapterC, 352, SourceFragmentKind.RuleText, 170),
        ("C3.71", "C3.71", ChapterC, 354, SourceFragmentKind.RuleText, 170),
        ("C3.74", "C3.74", ChapterC, 362, SourceFragmentKind.RuleText, 171),
        ("C3.8", "C3.8", ChapterC, 370, SourceFragmentKind.RuleText, 171),
        ("C4.1", "C4.1", ChapterC, 378, SourceFragmentKind.RuleText, 171),
        ("C4.11", "C4.11", ChapterC, 382, SourceFragmentKind.RuleText, 171),
        ("C4.12", "C4.12", ChapterC, 386, SourceFragmentKind.RuleText, 171),
        ("C4.13", "C4.13", ChapterC, 390, SourceFragmentKind.RuleText, 171),
        ("C4.2", "C4.2", ChapterC, 392, SourceFragmentKind.RuleText, 171),
        ("C4.5", "C4.5", ChapterC, 400, SourceFragmentKind.RuleText, 171),
        ("C5.1", "C5.1", ChapterC, 406, SourceFragmentKind.RuleText, 171),
        ("C5.11", "C5.11", ChapterC, 420, SourceFragmentKind.RuleText, 172),
        ("C5.12", "C5.12", ChapterC, 424, SourceFragmentKind.RuleText, 172),
        ("C5.2", "C5.2", ChapterC, 428, SourceFragmentKind.RuleText, 172),
        ("C5.4", "C5.4", ChapterC, 442, SourceFragmentKind.RuleText, 172),
        ("C5.5", "C5.5", ChapterC, 446, SourceFragmentKind.RuleText, 172),
        ("C5.8", "C5.8", ChapterC, 478, SourceFragmentKind.RuleText, 173),
        ("C6.2", "C6.2", ChapterC, 512, SourceFragmentKind.RuleText, 174),
        ("C6.3", "C6.3", ChapterC, 514, SourceFragmentKind.RuleText, 174),
        ("C6.5", "C6.5", ChapterC, 534, SourceFragmentKind.RuleText, 174),
        ("C6.51", "C6.51", ChapterC, 542, SourceFragmentKind.RuleText, 174),
        ("C6.53", "C6.53", ChapterC, 560, SourceFragmentKind.RuleText, 175),
        ("C6.57", "C6.57", ChapterC, 570, SourceFragmentKind.RuleText, 175),
        ("C6.8", "C6.8", ChapterC, 582, SourceFragmentKind.RuleText, 175),
        ("C6.9", "C6.9", ChapterC, 584, SourceFragmentKind.RuleText, 175),
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

    private static readonly (string Rule, string Registered, string Source, int Line, SourceFragmentKind Kind, int Page)[] Pass7Subjects =
    [
        ("C.8", "C.8", ChapterC, 38, SourceFragmentKind.RuleText, 162),
        ("C3.31", "C3.31", ChapterC, 320, SourceFragmentKind.RuleText, 169),
        ("C3.9", "C3.9", ChapterC, 372, SourceFragmentKind.RuleText, 171),
        ("C5.9", "C5.9", ChapterC, 480, SourceFragmentKind.RuleText, 173),
        ("C6.1", "C6.1", ChapterC, 486, SourceFragmentKind.RuleText, 173),
        ("C6.7", "C6.7", ChapterC, 580, SourceFragmentKind.RuleText, 175),
        ("C7.1", "C7.1", ChapterC, 590, SourceFragmentKind.RuleText, 175),
        ("C7.11", "C7.11", ChapterC, 592, SourceFragmentKind.RuleText, 175),
        ("C7.11", "C7.11", ChapterC, 598, SourceFragmentKind.RuleContinuation, 176),
        ("C7.2", "C7.2", ChapterC, 602, SourceFragmentKind.RuleText, 176),
        ("C7.21", "C7.21", ChapterC, 604, SourceFragmentKind.RuleText, 176),
        ("C7.23", "C7.23", ChapterC, 608, SourceFragmentKind.RuleText, 176),
        ("C7.24", "C7.24", ChapterC, 610, SourceFragmentKind.RuleText, 176),
        ("C7.24", "C7.24", ChapterC, 612, SourceFragmentKind.RuleContinuation, 176),
        ("C7.31", "C7.31", ChapterC, 616, SourceFragmentKind.RuleText, 176),
        ("C7.311", "C7.311", ChapterC, 618, SourceFragmentKind.RuleText, 176),
        ("C7.32", "C7.32", ChapterC, 620, SourceFragmentKind.RuleText, 176),
        ("C7.33", "C7.33", ChapterC, 624, SourceFragmentKind.RuleText, 176),
        ("C7.331", "C7.331", ChapterC, 626, SourceFragmentKind.RuleText, 176),
        ("C7.34", "C7.34", ChapterC, 630, SourceFragmentKind.RuleText, 176),
        ("C7.342", "C7.342", ChapterC, 638, SourceFragmentKind.RuleText, 176),
        ("C7.35", "C7.35", ChapterC, 682, SourceFragmentKind.RuleText, 177),
        ("C7.4", "C7.4", ChapterC, 684, SourceFragmentKind.RuleText, 177),
        ("C7.41", "C7.41", ChapterC, 686, SourceFragmentKind.RuleText, 177),
        ("C7.42", "C7.42", ChapterC, 688, SourceFragmentKind.RuleText, 177),
        ("C7.5", "C7.5", ChapterC, 694, SourceFragmentKind.RuleText, 177),
        ("C7.6", "C7.6", ChapterC, 698, SourceFragmentKind.RuleText, 177),
        ("C7.7", "C7.7", ChapterC, 700, SourceFragmentKind.RuleText, 177),
        ("C8.1", "C8.1", ChapterC, 706, SourceFragmentKind.RuleText, 177),
        ("C8.3", "C8.3", ChapterC, 735, SourceFragmentKind.RuleText, 178),
        ("C8.9", "C8.9", ChapterC, 792, SourceFragmentKind.RuleText, 179),
        ("C8.91", "C8.91", ChapterC, 796, SourceFragmentKind.RuleText, 179),
        ("D1.3", "D1.3", ChapterD, 119, SourceFragmentKind.RuleText, 194),
        ("D1.31", "D1.31", ChapterD, 121, SourceFragmentKind.RuleText, 194),
        ("D1.32", "D1.32", ChapterD, 125, SourceFragmentKind.RuleText, 194),
        ("D1.321", "D1.321", ChapterD, 129, SourceFragmentKind.RuleText, 194),
        ("D1.6", "D1.6", ChapterD, 155, SourceFragmentKind.RuleText, 194),
        ("D1.61", "D1.61", ChapterD, 157, SourceFragmentKind.RuleText, 194),
        ("D1.62", "D1.62", ChapterD, 159, SourceFragmentKind.RuleText, 194),
        ("D1.63", "D1.63", ChapterD, 161, SourceFragmentKind.RuleText, 194),
        ("D1.64", "D1.64", ChapterD, 165, SourceFragmentKind.RuleText, 194),
        ("D1.7", "D1.7", ChapterD, 169, SourceFragmentKind.RuleText, 194),
        ("D1.73", "D1.73", ChapterD, 179, SourceFragmentKind.RuleText, 194),
        ("D1.74", "D1.74", ChapterD, 183, SourceFragmentKind.RuleText, 194),
        ("D3.12", "D3.12", ChapterD, 341, SourceFragmentKind.RuleText, 199),
        ("D3.2", "D3.2", ChapterD, 343, SourceFragmentKind.RuleText, 199),
        ("D5.2", "D5.2", ChapterD, 490, SourceFragmentKind.RuleText, 203),
        ("D5.5", "D5.5", ChapterD, 552, SourceFragmentKind.RuleText, 204),
        ("D5.6", "D5.6", ChapterD, 556, SourceFragmentKind.RuleText, 204),
        ("D5.7", "D5.7", ChapterD, 560, SourceFragmentKind.RuleText, 204),
    ];

    // Fragments a column break, a boxed example, or a page break interrupts: each part occurs whole in the page text.
    private static readonly HashSet<(string Rule, int Line)> TwoPartSubjects =
        [("A7.212", 445), ("A8.26", 644), ("A8.31", 658), ("A9.2", 690), ("B3.4", 138), ("A12.11", 992), ("A11.41", 940), ("A20.21", 1402),
            ("A7.308", 485), ("D3.5", 383), ("A12.2", 1082)];

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

    /// <summary>The unit step 25 subjects of vehicles in the target Location, their crews, their MG, and their movement.</summary>
    public static AslScenarioA1VerificationBatch BuildVehicles(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, VehicleComparisonFile, VehicleComparisonSha256, VehicleSubjects,
            "unit step 25 Vehicle review");

    /// <summary>The backlog pass 5 subjects: CX, No Quarter and Massacre, the owners' options, Acquisition, Motion, the Recall, and grain.</summary>
    public static AslScenarioA1VerificationBatch BuildPass5(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, Pass5ComparisonFile, Pass5ComparisonSha256, Pass5Subjects,
            "backlog pass 5 review");

    /// <summary>The backlog pass 6 subjects: Residual FP against vehicles, wrecks and their smoke, vehicle concealment, and Bounding First Fire.</summary>
    public static AslScenarioA1VerificationBatch BuildPass6(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, Pass6ComparisonFile, Pass6ComparisonSha256, Pass6Subjects,
            "backlog pass 6 review");

    /// <summary>The backlog pass 7 subjects: the Vehicle Target Type, To Kill, Shock, Special Ammunition, and tanks.</summary>
    public static AslScenarioA1VerificationBatch BuildPass7(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, Pass7ComparisonFile, Pass7ComparisonSha256, Pass7Subjects,
            "backlog pass 7 review");

    /// <summary>The unit step 24 subjects of a Gun's HE shot at Infantry.</summary>
    public static AslScenarioA1VerificationBatch BuildOrdnance(string repositoryRoot,
        GeneratedManifests manifests, AslScenarioA1SourceAttestation attestation) =>
        Build(repositoryRoot, manifests, attestation, OrdnanceComparisonFile, OrdnanceComparisonSha256, OrdnanceSubjects,
            "unit step 24 Ordnance review");

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

            var normalized = Normalize(fragment.Content, file == Pass7ComparisonFile);
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

    private static string Normalize(string value, bool tagsOnly)
    {
        // From the backlog pass 7 comparison on, only markup tags go: an escaped "\<" or "\>" is the rulebook's dice sign. The earlier
        // comparisons were recorded with every bracketed span removed, on the fragment and the page alike, and keep that method.
        var withoutMarkup = Regex.Replace(value, tagsOnly ? @"(?<!\\)<[A-Za-z/!][^>]*>" : "<[^>]+>", string.Empty)
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
