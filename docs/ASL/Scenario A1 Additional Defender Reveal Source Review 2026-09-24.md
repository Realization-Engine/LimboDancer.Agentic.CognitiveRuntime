# Scenario A1 additional defender reveal: source review

**Date:** 2026-09-24 (UTC)

**Branch:** `decision-plane`

**Status:** Seven source-backed cases pass end-to-end conformance through separate immutable eligibility and consequence packages. Two exact second-reveal cases conclude a read-only return and attempted-entry MF expenditure in the supplied previous Location; no game-state mutation is admitted.

## Boundary and source evidence

This branch starts after an A12.15 attempted entry into a concealed Location: the defender first reveals an enemy SMC, and the attacker elects the optional Infantry OVR. The question is what can be concluded when the defender then reveals another non-Dummy unit. The [existing exact package](<./SourceRegistry/asl-scenario-a1.concealed-smc-overrun-package.json>) returns `Indeterminate` for `A1-concealed-smc-another-defender-revealed`; it remains immutable.

| Rule | Existing verified fragment | Relevant source proposition |
| --- | --- | --- |
| A12.15, physical PDF page 78 | `asl-fragment:sha256:e92332292b4bde444aff7f84127793084e1f0ecfd99af9750da6e4aa8af40ea3` | After only an SMC is revealed, the attacker may elect an immediate OVR if possible; that attempt requires the defender to reveal another non-Dummy unit immediately if one exists. The same paragraph supplies the detection/forced-back framework and exceptions. |
| A4.14, physical PDF page 49 | `asl-fragment:sha256:d22c4de11eefffac89c9585633ca3f4eeff313cc2d1ddd535975e1e8c23e0e69` | Ordinary MPh entry into an unconcealed enemy Location is prohibited, with Infantry OVR among the listed exceptions. |
| A4.15, physical PDF page 49 | `asl-fragment:sha256:189987c951bbe691bdab278fad56ca4435cd1c038f724d99888121c54c5eaa75` | Infantry OVR applies to a Location containing only one Known enemy SMC outside an AFV, with its NTC and doubled MF requirements. Other concealed units invoke A12.15. More than one SMC must be revealed to deny a Location to an MMC capable of OVR. The NTC may be taken before entry or in the Location when the SMC was concealed. |

These are the registered and verified source subjects already used by the [transition review](<./SourceRegistry/asl-scenario-a1.concealed-smc-overrun-transition.json>). This review adds no PDF attestation. A4.151/A4.152 address response and immediate CC **after an OVR proceeds**; they do not by themselves decide the second-reveal branch. B23.4 and the reviewed terrain chart still support the bounded 2 MF ordinary building cost if MF becomes controlling, but are not needed to identify the second defender.

## Semantic findings

1. A second **revealed SMC** is materially different from a second SMC merely known to the state source. A4.15 explicitly requires more than one SMC to be *revealed* to deny this Location to an OVR-capable MMC. A supplied count or concealed-stack marker alone does not satisfy that condition.
2. A second **revealed MMC** means the Location no longer fits A4.15's “only one Known enemy SMC” condition. A4.14's ordinary-entry restriction is then relevant. This supports a bounded finding that the single-SMC OVR permission cannot be inherited; the precise A12.15 forced-back, MF-spend, and follow-on fire consequences still require an exact timing and state contract.
3. Another non-Dummy defender whose type, reveal status, or Location is unknown cannot be classified as either branch. Dummy-only information also does not establish that no other non-Dummy unit exists.
4. The text calls the further revelation *immediate* upon the optional OVR attempt, while A4.15 permits the NTC in the Location when the SMC was concealed. It does not establish a universal order of “NTC passed → 4 MF available → second reveal.” The current ten-case package admits only its explicit sequence of supplied facts; the next package must model the event order without retroactively changing that immutable contract.

## Exact questions for the next admission

- Record the first A12.15 reveal, the OVR election, and each later reveal as distinct supplied events with order and snapshot version. Distinguish `revealedSecondSmc`, `revealedMmc`, `otherKnownNonDummy`, `noFurtherNonDummy`, and `unresolved`; separately preserve whether any other concealed/hidden unit remains.
- Establish whether the attacker is OVR-capable at the moment relevant to A4.15, including the NTC result, applicable TEM/LOS Hindrance, remaining MF, and excluded leader/Berserk exceptions. Do not infer these from the building metadata.
- Review the exact consequence when a second revealed SMC denies the Location and when a revealed MMC defeats the single-SMC condition. In particular, decide whether and when A12.15 forces the mover back and where attempted MF is spent. Do not turn a failed OVR precondition into an executed forced-back move by implication.
- Keep defender fire, residual FP, FFE/minefield, further movement, and CC out of any conclusion unless separately sourced and reviewed.

The affirmative xUnit review pins the event vocabulary, source dependencies, and candidate outcomes before any new immutable package is published. The board 01 adapter may validate the terrain; all reveal, unit-type, NTC, MF, and timing facts must come from the supplied state source.

The [case matrix](<./SourceRegistry/asl-scenario-a1.second-defender-reveal-case-matrix.json>) and its affirmative xUnit review pin two narrowly bounded eligibility findings: a second **revealed SMC** denies the single-SMC OVR to a supplied OVR-capable MMC, while a **revealed MMC** defeats that single-SMC exception. Five other cases remain indeterminate or abstain. The matrix separates the required first-reveal → election → second-reveal ordering from NTC timing and grants no movement, MF-spend, or execution conclusion. The [new immutable package](<./SourceRegistry/asl-scenario-a1.second-defender-reveal-package.json>) and resolver serve only these exact read-only eligibility findings. The observation adapter projects supplied ordered events and MF at the second reveal; this bounded projection requires a passed NTC recorded before that reveal and makes no claim that this is the universal rule sequence.

## End-to-end conformance and remaining consequence

`ScenarioA1SecondDefenderConformanceTests` feeds all seven reviewed event states through the board 01 observation provider, exact package descriptor, and resolver. It checks the case disposition, pinned matrix and versioned observation evidence, and the absence of any forced-back, MF-spend, or response/CC resolution. It also confirms this package cannot resolve or observe an earlier milestone's identity; the earlier packages remain available under their own identities.

The later consequence review below decides the bounded A12.15 return and attempted-entry MF location for two exact disqualified OVR branches. A definitive **eligibility** finding alone does not authorize moving a unit back, charging MF, or resolving defensive fire or CC.

## Consequence review candidate

The [consequence matrix](<./SourceRegistry/asl-scenario-a1.second-defender-consequence-case-matrix.json>) and its affirmative xUnit review pin a narrower continuation: the first concealed SMC is revealed on attempted ordinary entry, the attacker elects an OVR, and a second SMC or MMC is revealed before OVR entry resolves. The state source must identify the last occupied Location and the ordinary building entry attempt, whose reviewed cost is 2 MF. A second revealed SMC denies the Location under A4.15; a revealed MMC defeats the single-SMC condition. With no other A4.14 exception, A12.15 returns the mover to the prior Location and treats the attempted entry MF as expended there. These are read-only findings; the matrix neither records an additional doubled OVR charge nor executes the return. Other unit identities, unrevealed units, unresolved capability and insufficient MF remain nondefinitive or outside this contract. NTC timing relative to the second reveal remains a supplied fact rather than a universal sequence imposed by this review.

Defensive attacks, residual FP, FFE/minefield, other terrain, concealment changes, movement termination and CC require separate handling.

The [immutable consequence package](<./SourceRegistry/asl-scenario-a1.second-defender-consequence-package.json>) resolves the two exact cases as read-only conclusions. Its resolver requires the previous Location both in the observation and as a resolved question entity, returns that Location as the destination and MF expenditure Location, and states the attempted entry cost of 2 MF. It admits no extra OVR MF charge or state execution.

The consequence adapter reuses the earlier pinned eligibility projection for the seven event cases, while separately requiring the last occupied Location, attempt event before first reveal, recorded 2 MF ordinary entry, and an OVR entry that has not resolved. It rejects stale, conflicting, late or foreign events. `ScenarioA1SecondDefenderConsequenceConformanceTests` feeds every reviewed case through this adapter, the exact package and the resolver; it checks versioned matrix and observation evidence, the two exact return and MF findings, the five nondefinitive or abstained branches, and separation from the eligibility package.

**Remaining boundary:** These conclusions state rule consequences without executing movement or debiting MF. An execution contract would need separately reviewed authority and handling for the A12.15 follow-on conditions, including attack timing, Residual FP, minefields and FFE. The doubled OVR surcharge is not inferred from an OVR that did not proceed.
