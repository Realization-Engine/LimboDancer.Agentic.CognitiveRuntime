# Scenario A1 Backlog Pass 14: source and case review

**Status:** Reviewed. The package `scenario-a1-close-combat` is revised and republished with its prior manifest digest kept. It has no execution authority. Backlog pass 14 (Close Combat and capture, part 2).

**Date:** 2026-09-28

**Plan:** [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), sections 1, 3 (pass 14), and 5 (rulings R14.1 to R14.14). Two independent reviewers take the place of the user's review:

- a referee, a separate agent briefed as a skeptical ASL rules referee, after the review stage;
- a table player, after the live stage.

**Design:** [ASL Unit Backlog Pass 14 Design](<ASL Unit Backlog Pass 14 Design.md>).

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). The whole rulebook is in scope.

## Scope

Hand-to-Hand CC (the CCT's red Kill Numbers; J2.31 as G1.64, A25.43, and W.6B describe it), concealed and hidden units and Dummies in CC (A11.19, A11.4, A11.14, A11.15, A12.14), TI units in CC (A4.8), capture attempts (A20.22, A19.35, A20.2, A20.221, A20.24), Unarmed units and Guards (A20.5 to A20.54), prisoners' escape and rearming (A20.55, A11.33, A11.34, A20.551), prisoner transfer and abandonment (A20.5), Infiltration (A11.22, A18.12), Ambush Withdrawal (A11.41), overstacking in CC and on an advance (A5.1, A5.5, A5.11, A5.12, A5.131), a Disrupted unit's surrender (A19.12), Field Promotion's base MMC and the MMC its leader defends with (A18.2), the odds between 10 and 11 to 1 and a berserk force's Ambush drm (A11.11, A11.4), and mandatory CC (A15.43, A11.15).

## Sources

The rule text of each new subject was compared with the PDF page text (`pdftotext -raw`), normalized to letters and digits: `asl-scenario-a1.pass14-pdf-comparison.json`, SHA-256 `01407862...8dbc`, nine Chapter A fragments: A4.8, A5.11 (p. 52), A11.33, A11.34 (p. 73), A12.15 (p. 78), A19.35 (p. 86), A20.22, A20.221 (p. 87), A20.551 (p. 88). The other rules the package now rests on (A5.12, A5.131, A12.14, A19.12, A20.2, A20.21, A20.24, A20.3, A20.5 to A20.54) were registered by earlier passes; the package cites 80 fragments. `ThePass14SubjectsAreVerified` checks the nine.

J2.31 is not in the registered PDF (it has no Chapter J). R14.1 rests on the CCT's red Kill Numbers and its note (pp. 72, 692), and on G1.64, A25.43, and W.6B, which say who may declare Hand-to-Hand and that it is never used by or against vehicles, PRC, or pillbox occupants.

The BPV of each catalog MMC was read from a 150 dpi render of the A./G. National Capabilities Chart (p. 109) and recorded in the case matrix (`bpv`, with `bpvSource`); the referee checked every value against the page.

An Unarmed MMC's NTC to escape uses its own unit's Morale Level: no registered source prints an Unarmed counter's (R0.3; the referee found it reasonable, if generous).

## Rulings

R14.1 to R14.14 are in the plan, section 5. The referee's findings changed R14.1, R14.2, R14.5, R14.6, R14.8, R14.10, R14.11, R14.12, and R14.14.

## Referee findings

| Finding | Rules | Disposition |
|---|---|---|
| 1. Ambush Withdrawal of every ambusher left the Location open: the ambusher's round was forced with no ambusher, and the CCPh could not end | A11.41 | Fixed: with no ambusher left, the ambushed side's round closes the Location (package, facts, and projector); `WhenEveryAmbusherWithdrawsTheAmbushedSideCloses`. |
| 2. A leader created by an infiltrating unit was re-figured into attacks already resolved | A18.12 | Fixed: no attack but his own is re-figured for him; `RefereeFindingsOnTheRoundAreFixed`. |
| 3. The mandatory CC check dropped a prisoner's Guard, so the requirement lapsed wrongly | A15.43, A20.5 | Fixed: the prisoners kept keep their Guards; R14.14 reworded to the unit's attack alone. |
| 4. A11.14's forfeiture of concealment by a stacked concealed unit was not built | A11.14 | Fixed: the concealed member of a mixed stacking loses its "?" before the attacks, and attacks on the stack are not halved; tested. |
| 5. Unarmed units were not Inexperienced | A19.3, A19.35 | Fixed: an Unarmed MMC is Inexperienced (Lax, -1 to a capture attempt on it); tested. |
| 6. An escaped SMC was Armed while still in Melee | A20.55, A20.551 | Fixed: Armed only when the escape succeeds (no enemy but prisoners left, or Infiltration); `AnEscapedSmcIsArmedOnlyWhenTheEscapeSucceeds`. |
| 7. The prisoners' round is declared at once, so a later attack cannot leave out a Guard an earlier one eliminated | A20.55 | Recorded: a reading in R14.6; backlog section 24. |
| 8. A Disrupted unit in Melee did not surrender to fresh advancers | A19.12 | Fixed: the "not in Melee" test applies to the captors, not the Disrupted unit. |
| 9. Hand-to-Hand could be declared in the prisoners' round, after a first round, or by an ambushed ATTACKER | A25.43, G1.64 | Fixed: refused in those cases; R14.1 reworded. |
| 10. "> 10-1" literally covers 10.5 to 1 | A11.11 | Kept as a reading (R14.13); the user confirmed it on 2026-09-28. |
| 11. Six to nine SMC counted as more than one HS | A5.5 | Fixed: each five SMC is a HS; tested. |
| 12. A squad giving up one HS dropped all its SW, and rearming counted it as a squad | A20.22, A20.24, A20.551 | Fixed: the free HS keeps the SW, and rearming counts a HS. |
| 13. An Original 12 let a pinned unit infiltrate | A11.22 | Fixed; tested. |
| A Guard could be a berserk or withdrawing unit; a Reduced Guard's prisoners went to the first unit by id | A20.2, A20.5 | Fixed: neither may guard, and a Reduced or Replaced Guard keeps its prisoners in the unit that takes its place. |

The referee found R14.3, R14.5 (apart from item 5), R14.7, R14.10's MF penalty and DRM, R14.12's BPV values, the red Kill Numbers, R14.13's berserk and Lax drm, R14.4's per-defender DRM and the defender's choice, A20.221, the Dummy removal and hidden placement, concealed units and Melee, and the Ambush drm sound.

## Table player findings

The table player played 25 situations (39 cases) in a local clone in the scratchpad, since `git worktree add` failed on the `.git/worktrees` folder still held from pass 13: captures below and at the Kill Number, the yield order, Inexperienced targets, a named Guard over capacity, escapes against a broken Guard (NTC failed and passed, and the Melee after), a Good Order Guard, a broken Guard's own RtPh surrender, abandonment and transfer, a freed Unarmed unit's actions, a Guard advancing into CC and dying there or to fire, advances into Dummies and hidden units, Ambushes against concealed units in Open Ground, Infiltration on an Original 2 and 12, Hand-to-Hand in a continuing Melee, an overstacked Location, the "free" option, and mandatory CC by reinforcing and berserk units.

| Finding | Rules | Disposition |
|---|---|---|
| 1. An escape that eliminated the Guard could not be recorded: the Guard's elimination freed the prisoner before its `prisoner-freed` record (a freeze, with the dice thrown away) | A20.55 | Fixed: the record finds the prisoner already free; the rearmed Conscript HS is no longer marked captured; `AnEscapeThatEliminatesTheGuardIsRecordedAndRearmsThePrisoner`. |
| 2. A freed Unarmed unit Recovered a SW | A20.5 | Fixed: an Unarmed unit neither Recovers nor receives a SW. |
| 3. A concealed unit could not advance at all | A12.14 | Fixed: it advances, keeping its "?" unless it enters Open Ground in the LOS of a Good Order enemy within 16 hexes; the refusal now names the one condition that bars an advance. |
| 4. The ambushed side's round was required with no word that it may declare no attacks | A11.32 | Fixed: the refusal and the CC due list say so. |
| 5. The new package refusals gave a code only | | Fixed: sentences for capture, escape, prisoner, Guard, Infiltration, BPV, and rearming refusals, and the Ambush one reworded. |
| 6. Fire at the firer's own Location holding an Unarmed enemy is refused as "a unit named twice" | A7.212 | Recorded: backlog section 24. |
| 7. A concealed Guard keeps its "?" after abandoning prisoners in its Location | A12.14 | Recorded: backlog section 24. |
| 8. A created leader's leadership takes the place of the director's, rather than adding to it | A18.12, A10.7 | Kept: R29.12 reads A10.7 ("not cumulative"); the user confirmed it on 2026-09-28. |

The A19.12 advance surrender and the no-capacity "free" option did not arise in play, since a RtPh surrender comes first; `AnAdvanceMayOverstackTheLocation` and the package tests cover the pieces, and `ASurrenderToAGuardWithNoCapacityLeftStillNamesItsCaptors` the captors.

## Visual check

In the Studio (`map-studio-scripted`, game `pass14-demo`, driven by DOM events): a German squad advanced into Open Ground holding a Russian HS and a concealed squad; the Ambush dr was offered and rolled (none); the capture attempt was declared on the CC panel and its record read "r1: 5 + 1 (capture, A20.22) = Final DR 6: captured; r1 is captured and guarded by g1"; the Units table showed "captured, unarmed, guarded by g1"; in the next APh the Prisoners panel offered only abandonment until a second squad advanced in, then the transfer, after which r1 was "guarded by g2". The Ambush hint on the page was reworded.
