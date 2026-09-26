# Scenario A1 OVR NTC: source and case review

**Date:** 2026-09-26

**Status:** Case review for unit step 10, published. The rulings and the [case matrix](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.ovr-ntc-case-matrix.json>) are recorded under the user's delegated xUnit review, and the [OVR NTC package](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.ovr-ntc-package.json>) publishes them read-only (`ScenarioA1OvrNtcPackage`, `ScenarioA1OvrNtcConclusionResolver`). No live play changes in this step.

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 10.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## Sources

Two subjects were verified earlier: A4.15 (Infantry OVR, p. 49) and A12.15 (concealment lost by attempted entry, p. 78).

Four subjects are newly verified for this review, through the [OVR NTC PDF comparison](<../../../docs/ASL/SourceRegistry/asl-scenario-a1.ovr-ntc-pdf-comparison.json>). Each normalized paragraph occurs whole in its PDF page text.

- A.9, Random Selection (p. 43).
- A10.1, Morale Check and Task Check (p. 65).
- B23.3, building TEM (p. 136).
- The NTC entry of the Index and Glossary (p. 30).

## What the sources establish

- **A12.15.** When the only concealed unit revealed is an SMC, the ATTACKER may, at his option, attempt an Infantry OVR if possible. Doing so forces the DEFENDER to reveal another non-Dummy unit in the Location at once, if he has one.
- **A4.15.** An Infantry OVR enters a Location containing only one Known enemy SMC, at double the MF cost of entry, after passing a NTC. For a Location containing only a concealed SMC, the NTC may be taken in the enemy Location. It adds a DRM equal to the Location's TEM, and any LOS Hindrance in that Location. More than one SMC denies the Location to an OVR.
- **The NTC entry.** An NTC is a Task Check requiring a DR at or below the unit's current Morale Level. Leadership benefits apply.
- **A10.1.** A failed Task Check means the unit cannot perform that task in that phase, and may attempt no other action in that phase.
- **B23.3.** A stone building has a TEM of +3, a wooden building +2.
- **A.9.** Random Selection makes one dr per unit. The highest is affected, and every unit tied for the highest.

## Rulings

The sources leave three points open. The user ruled on each on 2026-09-26.

1. **Order.** The second reveal comes on the election, before any NTC. A12.15 calls it immediate on the OVR attempt. If another non-Dummy unit is present, the Location no longer holds only one Known SMC, so A4.15 denies the OVR and no NTC is taken.
2. **The second defender.** When several concealed units remain, Random Selection (A.9) chooses which is revealed, as A12.15 does for the first reveal.
3. **A failed NTC.** The OVR does not enter. The mover is forced back to its last Location under A12.15, with the ordinary attempted-entry MF (2) expended there and its MPh ended. No doubled OVR cost is inferred, as the additional-defender review already recorded.

Two further points follow from the sources:

- **Election availability.** A12.15 offers the OVR only if possible, and A4.15 charges double the MF of entry. With fewer than four MF left, the OVR is not possible, and only a decline remains.
- **NTC resolution.** The DR is two dice. The DRM is the building's TEM (B23.3). LOS Hindrance in the Location is admitted only as none. No leadership DRM applies, since the MMC moves alone. The NTC passes when the final DR is at or below the Morale Level, which comes from the reviewed catalog's printed morale.

## Cases

| Case | Facts | Disposition |
|---|---|---|
| `A1-ovr-ntc-election-reveals-another-defender` | Elected, another concealed non-Dummy unit present, revealed by Random Selection, no NTC | Forced back, 2 MF in the previous Location, MPh ended |
| `A1-ovr-ntc-lone-smc-mf-insufficient` | Election requested, no other unit, fewer than four MF | Election unavailable |
| `A1-ovr-ntc-failed` | Elected, no other unit, at least four MF, NTC failed | Forced back, 2 MF in the previous Location, MPh ended |
| `A1-ovr-ntc-passed-against-lone-smc` | Elected, no other unit, at least four MF, NTC passed | Indeterminate: the SMC's options and CC (A4.151, A4.152) are unreviewed |
| `A1-ovr-ntc-other-occupants-unknown` | Elected, other occupants unknown | Indeterminate |

## Relation to the existing packages

- **ConcealedSmcOverrun.** It records an elected OVR's NTC as unresolved or failed, and leaves both Indeterminate. This review decides the failed case. It does not change that package or its digest.
- **SecondDefender and SecondDefenderConsequence.** Their Definitive cases assume the NTC was passed before the second reveal. Under ruling 1 that order does not arise in live play: the second reveal comes first and denies the OVR without an NTC. Those packages stay as reviewed, and the case `A1-ovr-ntc-election-reveals-another-defender` is the path step 11 wires.

## The package

`scenario-a1-concealment-ovr-ntc` pins the matrix, both predecessor packages, the six source fragments, the rulings, and the NTC resolution by digest, and has no execution authority. Its resolver takes the unit, the attempted and previous locations, the observation version, and the case. It concludes Definitive for the two forced-back cases, with the return, the ordinary 2 MF in the previous Location, and the MPh ended. It concludes Abstained when the election is unavailable, and Indeterminate otherwise. Missing, contrary, extra, and stale facts refuse a conclusion, as in the earlier packages. Its observation provider over live games belongs to step 11.

## For step 11

- The only election live play can commit is one where another concealed non-Dummy unit is present: a Random Selection roll, the reveal, and the forced back.
- An election against a lone SMC stays refused. Its NTC could pass, and what follows a passed NTC is unreviewed, so the roll could not be committed.
- The NTC resolution and the failed case are recorded now for the step that reviews A4.151 and A4.152.

## Out of scope

- The leader's exemption from the NTC.
- Berserk movers and Dummies.
- LOS Hindrance in the Location.
- Fortified or other non-ordinary buildings.
- The SMC's options and CC after a passed NTC.
- Defensive First Fire and all follow-on fire.
