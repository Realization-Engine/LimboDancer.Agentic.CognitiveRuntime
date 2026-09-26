# Scenario A1 Rally: source and case review

**Status:** Reviewed; the package `scenario-a1-rally` is published with no execution authority. Unit step 19, parts 1 to 3.

**Date:** 2026-09-26

**Plan:** [ASL Unit Rally and Fire Extensions Plan](<ASL Unit Rally and Fire Extensions Plan.md>), section 4. The user accepted its rulings on 2026-09-26 and delegated this review, waiving the spot-checks; a second extraction pass stands in for them.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## Scope

A Rally attempt in the RPh by a broken MMC or leader of the reviewed Scenario A1 catalog (1.2.0), rallied by an unbroken friendly leader in its Location or by Self-Rally, in Open Ground, brush, woods, orchard, grain, or an ordinary building.

## Sources

Nine new fragments, compared with the text of their physical pages as the step 17 Fire comparison was (`asl-scenario-a1.rally-pdf-comparison.json`): A3.1 (p. 47), A10.6, A10.61, A10.62, A10.64 (p. 68), A10.71 (p. 69), A15.1 and its continuation (p. 83), and A18.11 (p. 85). A second pass with a different extractor (PyMuPDF) confirmed each. The package also cites fragments verified earlier: A7.302 (p. 55), A10.4 (p. 66), A10.63 and A10.7 (p. 68), A10.72 (p. 69), A12.14 (p. 77), A12.141 (p. 78), A17.11 and A17.3 (p. 85), and A19.12 (p. 86). No chart is needed.

## What the sources establish

- A broken unit rallies on a Final DR at most its broken Morale Level (A10.4, A10.6), once per Player Turn, and each unit takes one kind of RPh action (A3.1).
- A Good Order friendly leader in the Location rallies any broken unit there, applying his leadership (A10.6, A10.7); a non-zero modifier cannot be declined (A10.72). A wounded leader's modifier is one worse, and a wounded unit's Morale Level one lower (A17.3).
- Self-Rally is allowed to a unit with the capability, to a broken leader (A10.71), and to the first MMC Rally attempt of the side's own RPh regardless of capability (A18.11); never with a Good Order friendly leader present (A10.63) and never by a Disrupted unit (A19.12). It adds +1 (A10.63).
- DM adds +4 (A10.62), and is removed at the end of every RPh, not its start.
- Woods and buildings give -1 (A10.61).
- An Original 12 is Fate: Casualty Reduction and no rally (A10.64, A7.302); a leader's Reduction is a wound with a Wound Severity dr (A17.11).
- An Original 2 on a Rally other than Self-Rally calls for Heat of Battle (A15.1), and on the A18.11 attempt it rallies the unit and allows Leader Creation (A18.11).
- Rallying in the LOS of a Good Order enemy within 16 hexes costs a concealed unit its "?" (A12.141).

## Rulings

The user accepted these on 2026-09-26 (plan, section 4):

1. **DM sources (R19.1).** Breaking in this Player Turn, and being attacked while broken by FP that could cause a NMC. The ADJACENT and RtPh sources wait for routing.
2. **Heat of Battle (R19.2, R0.2).** An Original 2 on a leader rally rallies as its DR gives, and the record says Heat of Battle was not taken.
3. **Fate (R19.3).** Admitted.
4. **Field Promotion (R19.4).** The A18.11 attempt is an ordinary Self-Rally; its Original 2 rallies the unit, and the record says Leader Creation was not taken.
5. **Terrain (R19.5).** Woods and the ordinary buildings give -1; other admitted terrain 0; any other terrain abstains.
6. **Order (R19.6).** Free, except that a lone broken leader rallies himself first.
7. **Sides (R19.7).** Both, in every RPh.
8. **Concealment (R19.8).** A12.141 applies to the unit and to a concealed rallying leader.
9. **Wounded leaders (R19.9).** A17.3 applies.
10. **Exclusions (R19.10).** Commissars, Allied Troops, Encirclement, night, Extreme Winter, Recovery, and Deploy.

Two readings of the review itself:

- **A10.71 and A18.11.** A10.71 bars units without Self-Rally capability from rallying while their only leader is broken. The review reads the bar as covering the A18.11 attempt too, since that attempt is a Self-Rally.
- **Unrecorded capability.** The catalog records Self-Rally capability as not-in-source for every counter, since the national charts do not show broken sides. A Self-Rally that relies on capability is Indeterminate; leaders and the A18.11 attempt do not rely on it.

## Cases

| Case | Disposition |
|---|---|
| `A1-rally-resolved` | Resolved: the DRM, the Final DR against the broken Morale Level, and the effect |
| `A1-rally-phase-outside` | Abstained: not the RPh |
| `A1-rally-unit-outside` | Abstained: not a broken MMC or leader of the catalog in the Location |
| `A1-rally-already-attempted` | Abstained: a second attempt this Player Turn, or another RPh action |
| `A1-rally-leader-outside` | Abstained: the rallying leader is not an unbroken friendly leader in the Location |
| `A1-rally-self-rally-refused` | Abstained: Self-Rally with a leader present, by a Disrupted unit, or without the capability |
| `A1-rally-terrain-outside` | Abstained |
| `A1-rally-capability-unrecorded` | Indeterminate |
| `A1-rally-reduction-counter-missing` | Indeterminate |
| `A1-rally-roll-missing` | Indeterminate |

## The package

`scenario-a1-rally` pins the case matrix, the catalog, 19 verified fragments, the rulings, and the resolution by digest, and has no execution authority. `ScenarioA1RallyCalculator` resolves a declared attempt (the unit and its states, the leader or Self-Rally, the Location's terrain, the leaders present, whether it is the side's first MMC rally of its own RPh, and one recorded DR) and asks for a Wound Severity dr only after a leader's Fate. `Precheck` refuses an attempt unless every outcome the dice can reach is decided; the tests walk every roll of four accepted attempts.

## For later steps

The backlog records what this review leaves out: the other DM sources and DM retention, Rally terrain beyond woods and buildings, Commissars, Allied Troops, Encirclement, night, Extreme Winter, Recovery, Deploy, routing, Heat of Battle, Leader Creation, and the Self-Rally capability the catalog does not record.
