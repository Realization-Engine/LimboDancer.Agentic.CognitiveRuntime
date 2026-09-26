# Scenario A1 Fire: source and case review

**Date:** 2026-09-26

**Status:** Source and case review for unit step 17, published. The chart supplement, the verified fragments, the user's rulings, and the [case matrix](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.fire-case-matrix.json>) are recorded under the user's delegated xUnit review, and the [Fire package](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.fire-package.json>) publishes them read-only (`ScenarioA1FirePackage`, `ScenarioA1FireCalculator`, `ScenarioA1FireConclusionResolver`). No live play changes in this step.

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 17, and acceptance scenario U18.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## Scope

Decided by the user before the review:

- **Phases.** Prep Fire (PFPh) by the phasing side and Defensive Fire (DFPh) by the other side. Defensive First Fire, Final Fire, Subsequent First Fire, and Residual FP stay out.
- **Firers.** Good Order MMC in one Location firing together as one fire group, optionally directed by a leader in that Location. Multi-Location fire groups, support weapons, and MGs stay out.
- **Rally.** Not in this step. Broken units stay broken.

## Sources

### The chart supplement

The A7 Infantry Fire Table (p. 692) and the B. Terrain Chart (p. 698) lie outside the registered pages 6 to 253. They are registered as a bounded supplement, described in the [back-matter boundary review](<LimboDancer.Agentic.CognitiveRuntime ASL Back-Matter Source Boundary Review.md>):

- The IFT transcription holds the Personnel result cell of every DR row (0 or less to 15 or more) and FP column (1 to 36), and the DR/FP header.
- The Terrain Chart transcription holds the LOS, TEM, and Notes cells of Open Ground, brush, woods, orchard, grain, and the wooden and stone building rows, and five legend entries.
- `AslScenarioA1FireChartSupplement` rebuilds each row, note, and legend item from the transcription and requires it to hash to the value pinned from the `pdftotext 4.00 -table` extraction. A changed cell fails even when its transcription digest is re-pinned.
- The user spot-checked nine cells against rendered images of both pages ([review decision](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.fire-chart-review-decision.json>)).

### The rule fragments

Fifty-four registered fragments are newly verified through the [Fire PDF comparison](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.fire-pdf-comparison.json>) (`AslScenarioA1FireSourceReview`): each normalized fragment occurs whole in the text of its physical page. A.9 (p. 43), A10.1 (p. 65), and B23.3 (p. 136) were verified by the step 10 review.

| Area | Rules and physical pages |
|---|---|
| Phases | A3.2 and A3.4 (p. 47), A7.1 (p. 54) |
| Attack rules | A.5 (p. 43), A.17 (p. 44), A7.2 and A7.21 (pp. 54 to 55), A7.212, A7.22, A7.23, A7.3 to A7.306 (p. 55), A7.31 (p. 56) |
| Fire groups and direction | A7.4, A7.5, A7.52, A7.53, A7.531, A7.55, A7.6 (p. 57) |
| Pins and Cowering | A7.8 and A7.9 (p. 58) |
| LOS Hindrance | A6.7 (p. 54) |
| Morale and leadership | A10.2 (p. 65), A10.21, A10.22, A10.3, A10.31, A10.4 (p. 66), A10.7 (p. 68), A10.72 (p. 69) |
| Concealment | A12.14 (pp. 77 to 78), A12.141 (p. 78) |
| ELR | A19.1, A19.11, A19.12, A19.13 (p. 86) |
| Terrain | B1.1 (p. 113), B12.2 (p. 127), B13.3 (p. 128), B14.2 and B14.3 (p. 129), B15.2 (p. 129) |

Findings from the comparison:

- **Page markers.** Several conversion page markers are one page early (A7.31, A10.7, A12.14, and the end of A7.8). The comparison records the physical page.
- **A7.212.** The conversion appends a sidebar example printed elsewhere on page 55. The rule and the example each occur whole in the page text.
- **A7.8.** Its opening is printed beside the page 58 illustration and is in neither the conversion nor the text layer. Its end is registered under A7.72. The end is verified as text. The user read the opening from the rendered page and confirmed it: pinning also occurs when a unit attacked with an IFT MC passes it with the highest DR that still passes.

## What the sources establish

- **Phases (A3.2, A3.4, A7.1).** The ATTACKER fires in the PFPh and marks the firers with Prep Fire counters. The DEFENDER fires in the DFPh and marks them with Final Fire counters. No unit fires its inherent FP in more than one fire phase per Player Turn.
- **FP (A7.2 to A7.23).** FP is doubled for PBF against an ADJACENT target and halved at Long Range (beyond Normal Range, up to twice it). It is halved again for Area Fire against a concealed target. Fractions are kept.
- **Resolution (A7.3, A.17).** The FP of every unit in the attack is summed. The rightmost IFT column whose FP does not exceed the total is used. Every DRM is added to the DR.
- **Results (A7.301 to A7.306).** #KIA eliminates at least # units by Random Selection and breaks the rest. K/# Casualty Reduces at least one unit, and every other unit, including a just-Reduced HS, takes a #MC. NMC and #MC call for a MC by each unit, leaders first. PTC calls for a NTC by each unbroken unit, which is pinned on failure.
- **Targets (A7.4).** All Personnel in the target Location are affected, each with its own DR for a MC or TC.
- **Fire groups (A7.5 to A7.55).** Units in one Location that fire at the same target in the same phase must form one fire group. A leader directs one attack or fire group per Player Turn, and adds his leadership DRM to its IFT DR.
- **TEM and Hindrance (A7.6, A6.7).** The target's TEM and any LOS Hindrance between firer and target are both added to the IFT DR. Between same-level units, a Hindrance hex adds +1. Only the largest Hindrance counts where two hexes lie at the same range.
- **Pins (A7.8).** A pinned unit's inherent FP is halved.
- **Cowering (A7.9).** Original doubles without a directing leader shift the attack one column left, or two for Inexperienced firers. Below the lowest column the attack has no effect. British Elite and First Line units and Finns do not cower; German and Russian 1st Line squads do.
- **Morale (A10.1 to A10.4).** A unit fails a MC when its Final DR exceeds its Morale Level. An unbroken unit breaks, and a broken unit is Casualty Reduced. An Original 12 is a Casualty MC. The only MC DRM is the leadership modifier of one unbroken leader in the Location; a leader may not use his own. Leaders check first. An eliminated leader causes LLMC, and an unbroken leader that breaks causes LLTC.
- **Concealment (A12.14).** A concealed unit loses its "?" when attacked with a PTC or worse result, or when it breaks, fails a MC, or is Reduced. A concealed unit that fires or directs fire also loses it, if in LOS of a Good Order enemy ground unit within 16 hexes.
- **ELR (A19.1 to A19.13).** An unbroken squad, HS, or leader that fails a MC by more than its ELR is Replaced by a lesser unit, or Disrupted.
- **Terrain.** Direct Fire TEM is 0 for Open Ground, brush, orchard, and grain, +1 for woods (B13.3), +2 for a wooden building, and +3 for a stone building (B23.3). Brush and in-season grain are Hindrances (B12.2, B15.2, and the chart note: grain June to September).

## Rulings

The user ruled on 2026-09-26:

1. **Cowering.** Any directing leader prevents Cowering, whatever his leadership modifier. The 8-0 therefore prevents it and adds a DRM of 0.
2. **Hindrance.** The Hindrance DRM is the one the step 16 LOS read reports, for same-level firer and target only. Other level relationships, and counts the read cannot attribute, are Indeterminate. Grain counts only when the case declares a month from June to September.
3. **Leader loss.** LLMC and LLTC are admitted as A10.2 states, each with its own recorded DR.
4. **ELR.** The case declares the target side's ELR. A failure within it breaks the unit; a failure beyond it, or any failed MC when no ELR is declared, is Indeterminate, since Replacement units are not in the catalog.
5. **A7.8.** The opening as read from page 58 is correct and admitted: an unbroken unit that passes an IFT MC with a Final DR equal to its Morale Level is pinned.
6. **Wounds.** Casualty Reduction of a leader is Indeterminate until A17 is reviewed.
7. **The Russian HS.** Casualty Reduction of the Russian squad is Indeterminate until the catalog has a Russian HS. Printed values will come from the user.
8. **Concealment.** The A12.14 and A7.23 prose is admitted. The Concealment Table (p. 693) stays unregistered, and Dummies and hidden units are Indeterminate.

These points follow from the sources and the catalog:

- The fire group must hold every unit of the Location that fires at this target in this phase (A7.55), and the case declares that it does.
- A total below 1 FP has no IFT column and no effect.
- The catalog records no broken morale for the 8-0, so a MC by the broken leader, or an LLMC caused by his elimination while broken, is Indeterminate.
- With more than one leader in the target Location, the owner chooses whose DRM applies (A10.21) and leader losses can chain; the package refuses that case as Indeterminate.

## Cases

| Case | Disposition |
|---|---|
| `A1-fire-resolved` | Resolved: FP column, DRM, Final DR, IFT result, and each target unit's effect |
| `A1-fire-phase-outside` | Abstained: another phase or firing side, or a firer that already fired |
| `A1-fire-firer-outside` | Abstained: a firer or director outside the reviewed fire group |
| `A1-fire-target-outside` | Abstained: the firers' own Location, units outside the catalog, or unadmitted terrain |
| `A1-fire-range-or-los-denied` | Abstained: beyond twice Normal Range, or a blocked LOS |
| `A1-fire-levels-differ` | Indeterminate |
| `A1-fire-hindrance-unattributed` | Indeterminate |
| `A1-fire-concealment-unreviewed` | Indeterminate |
| `A1-fire-elr-undecided` | Indeterminate |
| `A1-fire-leader-wounded` | Indeterminate |
| `A1-fire-reduction-counter-missing` | Indeterminate |
| `A1-fire-leaders-interact` | Indeterminate |
| `A1-fire-roll-missing` | Indeterminate |

## The package

`scenario-a1-fire` pins the case matrix, the chart supplement and its review decision, both transcriptions, the Scenario A1 catalog, 57 verified fragments, the A7.8 reading, the rulings, and the resolution by digest, and has no execution authority. The IFT and Terrain Chart transcriptions and the catalog are embedded resources, each checked against its digest.

`ScenarioA1FireCalculator` takes a declared attack and returns its arithmetic and effects:

- **Facts.** The phase and firing side; the firers and the director with their catalog definitions and states; the range; whether firer and target are at the same level; the LOS result (blocked, Hindrance DRM, whether the read attributes it, whether grain is in the LOS); the scenario month; the target terrain; the target units; the target side's ELR.
- **Rolls.** The IFT DR, one Random Selection dr per target unit, and each unit's MC, NTC, LLMC, or LLTC DR. Every roll the attack needs must be recorded, and no other.
- **Result.** The FP of each firer with its multipliers, the total, the column before and after Cowering, the DRM, the Original and Final DR, the IFT result, each target unit's checks and events, the fire counter, and the concealment lost.

`ScenarioA1FireConclusionResolver` reads the declared attack from one observation of the target Location and concludes Definitive, Abstained, or Indeterminate with its reasons. Missing, contrary, extra, and stale facts refuse a conclusion, as in the earlier packages.

## For later steps (as of step 17)

- Fire in live play: a governed fire action that draws its rolls on demand inside the commit, records the attack as events, and places the fire counters.
- The Russian HS in the catalog, from the user's printed values, so a Russian squad's Reduction is decided.
- A17 (Wounds), so a leader's Casualty Reduction is decided.
- ELR from the scenario card, once scenario cards are a registered source.
- Rally, and the fire phases, weapons, and concealment rules this step leaves out.

## Revision at unit step 18

Step 18 must commit fire only when every outcome the dice can reach is decided, so part 1 closed the branches this review left Indeterminate. The package `scenario-a1-fire` was republished; its step 17 manifest digest (`45011b56...ff2b69`) is pinned in the new manifest as `priorFirePackageManifestSha256`.

**Sources.** Four more fragments are verified through the [Fire branches PDF comparison](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.fire-branches-pdf-comparison.json>) (`AslScenarioA1FireSourceReview.BuildBranches`): A10.63 (Self-Rally, p. 68), A17.1, A17.11, and A17.3 (Wounds, p. 85). A19.12 and A19.13 were verified in step 17.

**Catalog.** The Scenario A1 catalog 1.1.0 adds the Russian HS and the A19.13 Replacement units and leaders, read from the National Capabilities Chart (p. 695), with the leaders' values supplied by the user from the printed counters, since the rulebook shows a leader's broken morale only in counter artwork ([Catalog Design](<ASL Scenario A1 Catalog Design.md>), section 9.2). The boxed class of the Russian 1st Line row marks Self-Rally (A10.63), not the underscored Morale Factor of A19.13.

**What the sources establish.**

- **ELR (A19.12, A19.13).** An unbroken unit that fails a MC by more than its ELR is Replaced by a broken unit of lesser quality and the same size, whose Class drops and no part of whose Strength Factor rises. A leader is Replaced by the next lower quality. A unit that cannot be Replaced is broken and Disrupted. A Casualty MC failure beyond ELR Reduces a squad to a broken HS of lesser quality.
- **Wounds (A17.1, A17.11, A17.3).** Casualty Reduction wounds a leader. A Wound Severity dr of 5 or 6 (+1 if already wounded) is mortal and treated as a KIA. A wounded man's Morale Level is one lower and his leadership modifier one worse; he never loses more than one of each.

**Rulings** (the user, 2026-09-26):

9. **A Conscript squad whose Casualty MC exceeds its ELR** has no HS of lesser quality; it becomes its own Conscript HS, broken and Disrupted, reading A19.12 with the Casualty Reduction.
10. **A just-wounded leader takes the #MC** of a K/# result, like a just-Reduced HS, at his lowered Morale Level.
11. **A wounded leader's +1** applies to each other friendly unit's MC and NTC while he is unbroken in the Location, since A10.72 does not let a player decline a non-zero modifier.

Rulings 4, 6, and 7 of step 17 are superseded: a failure beyond a declared ELR, a leader's wound, and the Russian squad's Reduction are now decided. With no declared ELR, a failed MC by an unbroken unit stays Indeterminate, and A19.13's exception for an underscored Morale Factor stays unreviewed (no catalog unit has one).

**Cases.** `A1-fire-leader-wounded` and `A1-fire-reduction-counter-missing` are removed; the other eleven are unchanged in their dispositions. The calculator takes two more facts per target unit (wounded, Disrupted) and one per director (wounded), and one more roll, the Wound Severity dr. It Replaces or Disrupts on a failure beyond ELR, Reduces beyond ELR on a Casualty MC, wounds a Casualty Reduced leader, and applies a wounded leader's Morale Level and modifier.

**Still Indeterminate.** More than one leader in the target Location, an undeclared ELR, an underscored Morale Factor, levels that differ, an unattributed Hindrance, Dummies and hidden units, and a missing roll.

