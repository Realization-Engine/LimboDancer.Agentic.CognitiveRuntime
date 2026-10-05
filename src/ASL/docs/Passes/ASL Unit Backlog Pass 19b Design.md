# ASL Unit Backlog Pass 19b Design

**Status:** Built. Pass 19b of the [ASL Unit Scenario Card Games Plan](<../Plans/ASL Unit Scenario Card Games Plan.md>): the Fire table gap the pass 19 build found (backlog section 29, first row).

**Date:** 2026-09-29

**Requirements:** [ASL Unit Requirements](<../Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 19b Review](<Scenario A1 Backlog Pass 19b Review 2026-09-29.md>), the [ASL Unit Backlog Pass 19 Design](<ASL Unit Backlog Pass 19 Design.md>), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 29.

Ruling R19.7 is in the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **The Fire package's tables (R19.7).** `ScenarioA1FireReference` knows the German squads the cards added: the circled-E 5-4-8 and its 2-3-8 HS, and the plain-E 8-3-8 and its 3-3-8 HS (National Capabilities Chart, p. 695). `HalfSquads` gives each squad its HS, so Casualty Reduction, Deployment, and Recombination work for them in fire, Rally, Close Combat, and setup. `Replacements` gives the 5-4-8 the 4-4-7 and the 2-3-8 the 2-3-7. The underscored 8-3-8 and 3-3-8 take the existing underscored path (two broken HS; Disrupted). A new table, `CasualtyHalfSquads` (read through `CasualtyHalfSquadOf`), gives the broken 2-3-7 that a Casualty MC beyond an 8-3-8's ELR leaves, so no other rule Replaces a 3-3-8. All four are highest quality, so Battle Hardening makes them Fanatic.
- **The Close Combat package (R19.7; referee, pass 19b).** The case matrix's BPV table gains the four squads (13, 5, 16, 6), so a CC attack that mixes them with other MMC is decided and the 8-3-8 founds a created leader (A18.2, R14.12). The matrix and the package are re-pinned: matrix `0d2f60f9...7627`, package manifest `d5737fc6...29d0`.
- **The other card counters.** The British LMG, ATR, and 51mm mortar, the German and Russian HMG, and the 9-0 Commissar need no table of their own: every package reads them from the pinned catalog, and the ATR and the mortar are Guns of the Ordnance package.

## 2. Tests

`ScenarioA1Pass19bTests` (ScenarioA1): the tables; every definition the embedded cards field is known to the Fire, Rally, Close Combat, and Ordnance packages, with a HS for each squad and a CC BPV for each MMC; Casualty Reduction to each HS; the 5-4-8 and 2-3-8 Replaced beyond ELR; a K/3 and an Original 12 Casualty MC beyond ELR; the 8-3-8's own ELR 5 (a failure by five only breaks it; by six Replaces it by its two 3-3-8; a 3-3-8 is Disrupted); Battle Hardening to Fanatic; and a CC attack by an 8-3-8, its 3-3-8, and a 4-6-7. `BacklogPass19Tests` (Play): an 8-3-8 of The Tractor Works sets up Deployed through the planner on board 01, and one of Gambit's eight 5-4-8 may Deploy, not two (R19.6).
