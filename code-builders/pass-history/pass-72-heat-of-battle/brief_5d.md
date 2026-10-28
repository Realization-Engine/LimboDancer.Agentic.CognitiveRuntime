# Increment 5d: Ammunition Shortage (72.2, part II)

Read `brief_build.md` beside this file first; it binds you. Then the design's sections 2 to 7 and its sections for the increments built, and `frame_substitution.md` section 2. 5c (the `ammo-shortage:` token and its reader) is committed before you start: read its diff.

## Pages
A19.131 (p. 86), A19.13 (p. 86, Replacement), A.9 (p. 43, Random Selection), A.11 (p. 43), A21.11 (p. 88), A9.22's Good Order note (p. 110), the Ammunition Depletion rules the armor package cites (C8.9 or its page; find it).

## Build (rulings R72.11, R72.12)
1. **The fact.** A fire attack (and an ordnance shot) carries an optional `ammunitionShortage` fact, true when the firing side's on-board OB has Ammunition Shortage by the card token (5c's reader); null otherwise. Recorded on the attack so the verifier checks it against the state, not taken as recorded like Extreme Winter's `BreakdownReduction`.
2. **Unit Replacement on an Original 12 (R72.11).** With the fact, an attack whose Original IFT DR is 12 and in which any MMC of the firing side used its Inherent FP: after the attack is resolved (after SW malfunction or elimination), one such MMC is chosen by Random Selection among them (A.9; a recorded roll of a new kind, with its key in `LiveFire.Rolls`; say whether it is a Sniper trigger by A14.1 and classify it for pass 71's trigger reader) and Replaced by the next lower quality unit (`ReplacementOf`), keeping its broken or unbroken status. A MMC incapable of further quality loss is broken instead, unless berserk. An underscored squad becomes its two HS. Use `FireResolution.FirerEffects` or a sibling list; the effects are public events (the firing side must see its own unit Replaced). No effect on CC, Sniper, or Residual FP attacks, nor on a To Hit DR of 12.
3. **The numbers (R72.12).** With the fact: every SW's Original B# and X# one lower (not a DC; not a captured SW, A21.11), in IFT fire and as ordnance, with A.11's X# for a lowered B# as the existing reduction does (check how it sums with Extreme Winter's and 5c's Inexperienced -1; the bound on `BreakdownReduction` at FireCalculator near 528 if they are summed); Ammunition Depletion Numbers one lower (`ScenarioA1ArmorCalculator.Depletion`/`ShotDepletion`, the Elite pattern); no Fire Lane may be declared (`FireLaneDeclarationBar`, a refusal citing A19.131).
4. **The page:** the record lines name the Random Selection and the Replacement with hexes; a refusal of a Fire Lane says why.

## Not in 5d
Vehicles' and Guns' circled B# and Low Ammo (pass 105); OBA (pass 150); A8.221 (pass 73, which will read the same fact).

## Tests by method
Rules: an Original 12 with Inherent FP Replaces one MMC chosen by the Random Selection dr; a squad with only a SW firing (no Inherent FP) is not Replaced; an elite underscored squad becomes two HS; a Conscript (incapable) is broken; a berserk incapable unit is not; a captured MG keeps its B#; a DC keeps its B#; a lowered B# becomes an X# by A.11; Depletion one lower; the Fire Lane refused. Play: a card with `ammo-shortage:russian`, a Russian FG's Original 12 Replaces one squad and the record is public; an older game's fire record without the fact verifies. Grep and run by method the Fire Lane, Extreme Winter `BreakdownReduction`, and Depletion tests you touch.

## Studio games
`games_5d.py`, board 3, a card with `ammo-shortage:russian`: a Russian FG of two 4-4-7 squads and a LMG firing (dice for an Original 12 and the Random Selection); a Russian MMG firing at its lowered B#; a Russian Fire Lane declaration refused.
