# Increment 4g: carrying a wounded man (71.2, part II)

Read `brief_build.md` beside this file first; it binds you. Then the design's sections 2 to 7 and its sections for 4a to 4f, and `frame_wounds_codemap.md` section 1 (b). Read the diffs of the increments already committed.

## Pages
A17.2 (p. 85: "A Good Order wounded man who is not portaging a SW may be carried by any Good Order MMC at a cost of five PP"), A4.4 to A4.43 on portage (find the pages), and A4.11 (p. 48).

## Build (ruling R71.16)
1. **A `carried` argument of `asl.game.move`** (the man, the carrier), recorded on the step event (absent, as before).
2. **The bar, in Rules:** both Good Order; the man wounded and possessing no SW; the carrier a MMC moving in the same stack; one man per carrier. The carrier's portage gains 5 PP (`Portage()`, `MfAllotment`, `Laden`), so its MF fall by the portage rules.
3. **The projector** moves the man with his carrier, as `MovePrisoners` does for prisoners, and he spends no MF of his own; `StepMoverFacts` accepts the carried man. He is set down when the move ends, or when the carrier breaks, is eliminated, or stops being Good Order.
4. **The page:** in the movement panel, a choice to carry a wounded man of the stack, listing "[hex]: name" (the hex UI rule).

## Not in 4g
A carried man in the RtPh or the APh: read A17.2 and A4 for whether the rule reaches them; if it does and the build is long, backlog row.

## Tests by method
Rules: the bar (each refusal), the portage and MF. Play: a squad carries a wounded leader two hexes; the leader's MF unspent; the carrier's MF reduced; a refusal for a man holding a SW. MapStudio: the panel's choice.

## Studio game
`games_4g.py`: board 3, a German 4-6-7 and a wounded 8-1 leader in [D9], German MPh, no enemy in LOS.
