# The fix rounds of pass 71

Read `brief_build.md` beside this file first; it binds every round (the Rules boundary, absent-safety, a side's view, memory, the shell, tests by method, the strict test builds before reporting, the report's shape). Then the design's sections 2 to 7, its F15 (rulings R71.18 to R71.28, decided at the reviews), and the review files of your round. Read the diffs of the increments and earlier rounds you touch (`git -C E:/Archive/GitHub/dlandi/LimboDancer.MCP log --oneline 0b5b9a1d..HEAD`).

A finding marked fixed must have a test that fails without the fix (by method), unless it is a text: then the page test or record test that reads the text. A finding you do not fix gets one line in your report's first section, with its reason, and a proposed backlog row.

## Round A: hidden information

Files: `fixes_r2_hidden.md` (all), `fixes_r1_sniper.md` 3, `fixes_r5_ui_boundary.md` 11, `fixes_r4_table.md` 7, and the notes' known item (4a's public `conditions-changed` of a target that keeps its "?", and its scope in R2's 11).
- R71.18: the Sniper Check offered whenever the Location holds the attacked side's concealed units; a "take" with none eligible commits nothing.
- R71.19: the reposition choice the Sniper side's alone, a public line that it forfeits and repositions.
- R71.20: a concealed vehicle's PRC and crew in its concealed stack (one candidate; a "?" lost by one lost by all), membership decided in Rules, reading the container's "?" as the Fire rules' "what shares its '?'" does.
- R71.21: the "?" lost only to an unconcealed Good Order enemy seer (`SightPurpose.LossInView`), for the attack and for the Sniper Check's participants; with only concealed seers the "?" stays; the backlog row's text in your report.
- R71.22: the neutral public result `concealed-kept` and the owner-only twin; the effect events of a target that keeps its "?" owner-only (its conditions, a crew's Severity dr roll); the Sniper Check's dice with the record's visibility; one public sentence for every kept case and the owner's own line naming his unit and result.
Check from each side's view in your tests: the side that must not see reads nothing of the stack.

## Round B: the Sniper's and the wound's rules

Files: `fixes_r1_sniper.md` 1, 2, 4, 5, 7 (the pinned-Sniper hardening only); `fixes_r5_ui_boundary.md` 1 (the MAJOR, the choice keys), 12, 13; `fixes_r3_wounds.md` 1, 2, 3, 5, 7 (a test), 9 (a backlog row only).
- R5's 1: put the trigger roll id in both choice keys (`sniper:{id}:{trigger}:{step}`, `sniper-check:{id}:{trigger}`), with a Play test of two triggers of one Sniper that both reach the forfeit, and one where an answered Sniper choice is followed by a non-fire trigger.
- R71.25: a Sniper attack's own LLMC triggers resolve before its Sniper Check and before later triggers (put them at the head of what is still to resolve).
- R71.26: a SMC Temporary crew of an unarmored or Partially Armored vehicle eliminated by dr 1, wounded by dr 2, through the SMC paths.
- R1's 4: Random Selection inside a concealed stack honors ties, with the first-target choice and new drs.
- R1's 5: the lowest-TEM Location carried out of the equidistant tie-break; SMOKE counted per Location.
- R71.27: a broken SMC never pinned by a wound; a Rules and a Play test of a broken leader wounded after more than 3 MF.
- R3's 2: a recorded `woundPins: true` refused when the state's rule says false. R3's 3: the projector runs `CarriedMoveBar`. R3's 5: the Sniper's line says "wounded and pinned (A14.3, A17.2)" when the wound pins, and the Fire path's `pinned-wound` gets its words.
- R5's 12 and 13: the forfeit's placement read from the state before the commit (or the memo's comment corrected), and in-commit replays that do not fill the planner's kept projections (as `plotting` does), or the state in hand reused.

## Round C: the Rules boundary

Files: `fixes_r5_ui_boundary.md` 5 to 10; `fixes_r1_sniper.md` 6; `fixes_r2_hidden.md` 12; `fixes_r3_wounds.md` 4.
- The A14.2 DEFENDER (`SniperDefender`) into Rules.
- A14.4's "has fired or moved this Player Turn": the separate facts on `SniperCheckCandidate`, composed in Rules.
- A17.3's wounded leadership: a `Wounded` fact on `SniperCheckParticipant`, applied in `SniperCheckDrm`.
- A14.22's "impaired" and "manned": raw conditions to the Rules facts.
- The Search casualty's MF for A17.2: a Rules member taking the half-MF spent and the Search's cost.
- One vocabulary test for a SMC (`vocabulary.IsA(kind, "asl:smc")` or the existing Rules member) in `WithWoundPins`, `SnipeUnit`, and Searching; the AFV test and the "a DR is two dice" test owned by Rules alone.
No behavior changes in this round: the tests of 4a to 4g and of Rounds A and B are its guard (run the methods that reach each moved member).

## Round D: the page and the record lines

Files: `fixes_r4_table.md` (2 to 6, 8, 9, 11 to 15), `fixes_r5_ui_boundary.md` 2, 3, 4, 15, 16, and the notes' known items: the Sniper lines' raw ids ("(gs)", "the SAN roll fire-...-roll-1", "(rs)"), the doubled citation "(ruling R5.8)" after every choice's own (the panel's suffix), the hex said twice ("[L4]: 4-4-7 squad in [L4]"), sides in lower case ("the german Sniper"), the setup list's "a Dummy" for a Sniper counter, the hand placement's "choose a hex the setup offers", the wound line's raw id ("for de-l") without a hex, the carry's summary and refusals by raw id, `#move-ipc`'s "id (Location)" options (the hex UI rule).
- Texts name counters by the view's names, hex first, sides by `DisplayText.Side`; a citation once; a ruling cited as a ruling (R71.11 in the no-counter notice, R71.16 in the set-down refusal).
- The Sniper Check's choice names its checkers (hex first) and its DRM's total and causes; its record line gives the causes.
- The night SAN line says when the SAN stays at the printed floor (E1.76).
- The equidistant choice names where the Random Location DR landed.
- The Sniper's wound line shows its Severity dr (`sniper-wound-severity`); the roll purposes' words in `DisplayText.RollWords`.
- "Recalled" for a SMC crew says he is eliminated (D5.343); an ineffective dr reads "no attack (A14.3)"; each Random Selection die's target named.
- Option texts hex first ("[Q8]: the Inherent crew of ...", "[L4]: a "?" stack"), the vehicle-itself option told apart ("the vehicle itself (A14.33)").
- Disabled `#propose-sniper-{side}` and `#play-choice-propose` with a `title`; `.setup-sniper` and `#play-choice-select` styled for the narrow layout (check the CSS at 768 and 1023 pixels by reading it); `sniperHex` cleared after a placement commits; `CarryMen` and `Carriers` built once per (state, view).
- Regenerate the icon coverage if an id changes.
