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

1. **Order.** The NTC comes before the second reveal. A4.15 lets the NTC be taken in the enemy Location in exactly this case, a Location that held only a concealed SMC, which places it inside the OVR attempt. A4.15 also denies the Location, by more than one revealed SMC, to an enemy MMC capable of OVR, which the MMC is only once its NTC is passed. A12.15's immediate reveal then follows the attempt, NTC included. The reading also keeps the second reveal behind a check that can fail, so an election is not a free way to uncover the other defenders.

   The user first ruled the other way (the second reveal on election, before any NTC) and, on review of these points, reversed it the same day, before the package was merged.
2. **The second defender.** When several concealed units remain, Random Selection (A.9) chooses which is revealed, as A12.15 does for the first reveal.
3. **A failed NTC.** The OVR does not enter. The mover is forced back to its last Location under A12.15, with the ordinary attempted-entry MF (2) expended there and its MPh ended. No doubled OVR cost is inferred, as the additional-defender review already recorded.

Two further points follow from the sources:

- **Election availability.** A12.15 offers the OVR only if possible, and A4.15 charges double the MF of entry. With fewer than four MF left, the OVR is not possible, and only a decline remains.
- **NTC resolution.** The DR is two dice. The DRM is the building's TEM (B23.3). LOS Hindrance in the Location is admitted only as none. No leadership DRM applies, since the MMC moves alone. The NTC passes when the final DR is at or below the Morale Level, which comes from the reviewed catalog's printed morale.

## Cases

| Case | Facts | Disposition |
|---|---|---|
| `A1-ovr-ntc-passed-second-defender-revealed` | Elected, at least four MF, NTC passed, another concealed non-Dummy unit present and revealed by Random Selection | Forced back, 2 MF in the previous Location, MPh ended |
| `A1-ovr-ntc-mf-insufficient` | Election requested, fewer than four MF | Election unavailable |
| `A1-ovr-ntc-failed` | Elected, at least four MF, NTC failed | Forced back, 2 MF in the previous Location, MPh ended; no further unit is revealed |
| `A1-ovr-ntc-passed-against-lone-smc` | Elected, at least four MF, NTC passed, no other unit | Indeterminate: the SMC's options and CC (A4.151, A4.152) are unreviewed |
| `A1-ovr-ntc-passed-other-occupants-unknown` | Elected, at least four MF, NTC passed, other occupants unknown | Indeterminate |

## Relation to the existing packages

- **ConcealedSmcOverrun.** It records an elected OVR's NTC as unresolved or failed, and leaves both Indeterminate. This review decides the failed case. It does not change that package or its digest.
- **SecondDefender and SecondDefenderConsequence.** Their Definitive cases assume the NTC was passed before the second reveal, which is ruling 1. The case `A1-ovr-ntc-passed-second-defender-revealed` gives the same return, and the package pins the SecondDefenderConsequence manifest so the two cannot drift apart. Step 11 can wire either; the second defender's choice by Random Selection is decided here.

## The package

`scenario-a1-concealment-ovr-ntc` pins the matrix, its three predecessor packages (ConcealedSmcOverrun, PostReveal, SecondDefenderConsequence), the six source fragments, the rulings, and the NTC resolution by digest, and has no execution authority. Its resolver takes the unit, the attempted and previous locations, the observation version, and the case. It concludes Definitive for the two forced-back cases, with the return, the ordinary 2 MF in the previous Location, and the MPh ended. It concludes Abstained when the election is unavailable, and Indeterminate otherwise. Missing, contrary, extra, and stale facts refuse a conclusion, as in the earlier packages. Its observation provider over live games belongs to step 11.

## For step 11

- An election can be committed when another concealed non-Dummy unit is present. Its NTC is a system roll. A failure forces the mover back with 2 MF, and the other units stay concealed. A pass is followed by a Random Selection roll for the second reveal, and the forced back.
- An election against a lone SMC stays refused. Its NTC could pass, and what follows a passed NTC is unreviewed, so the roll could not be committed.
- An election needs at least four MF left.

## Out of scope

- The leader's exemption from the NTC.
- Berserk movers and Dummies.
- LOS Hindrance in the Location.
- Fortified or other non-ordinary buildings.
- The SMC's options and CC after a passed NTC.
- Defensive First Fire and all follow-on fire.
