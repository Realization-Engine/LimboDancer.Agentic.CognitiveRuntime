# Increment 2j: wrecks and Scrounging (task 65.9)

Read `brief_build.md` beside this file first; it binds you. The ruling is R65.23 of the design (section 6, F22). Increments 2a to 2i are committed before you start.

## Pages
- p. 210: D10.1.
- p. 211: D10.5, D10.51, D10.52.
- p. 209: D9.3, a wreck's cover, built.
- p. 64 and 65: A9.x, a dismantled MG.
- p. 87: A20.552, rearming, which is pass 75's; read it only to leave it out.

## Build
1. **A wreck attacked as its vehicle (D10.1).**
   - A non-burning wreck may be the target of ordnance on the Vehicle Target Type (named like a vehicle), of the IFT (on the Vehicle line), and of a FT or a MOL (at a named vehicle, as pass 60 built them).
   - Only a Burn matters: it places a Blaze and the wreck burns (`vehicle-wrecked` with `burning: true` on a wreck; the projector must accept it on a vehicle already wrecked).
   - Every other result does nothing.
   - The wreck's TCA becomes its VCA (D10.1), as the wreck face already shows.
   - CC at a wreck is pass 70's: leave it refused with its reason.
2. **Scrounging (D10.5 to D10.52).**
   - A new RPh action, `asl.game.scrounge`, for an unbroken, non-berserk MMC not in Melee, in the Location of a non-burning wreck or an Abandoned vehicle that is not Scrounged and not Disabled (2g's condition).
   - A dr of 1 to 3 succeeds, and the dr is the number of SW taken, by Random Selection among those that qualify: a BMG that is not fixed (no `asl:fixed-bmg`), an AAMG, or a mortar MA.
   - A MG taken becomes the nationality's LMG, dismantled (D10.52: the British LMG for the Americans). Find how `LiveFire.LmgDefinition` picks one.
   - The unit and its SW are TI. The vehicle is marked Scrounged (a new condition), whether or not anything was found.
   - A new event, `vehicle-scrounged`, with the dr and the SW taken, through the reader, writer, projector, PlayRecords, ReplaySteps, and `GameActions.All`.
   - Rearming unarmed units with scrounged small arms is pass 75's: leave it.

## Gating
- Fire at a wreck and Scrounging were impossible before the pass: no record changes. Say how a wreck now appearing in a target list affects older records' fire facts (an older attack whose Location held a wreck must resolve as it did: the wreck is a target only where named, or where the attack carries the pass's fact).

## Tests (by method)
- **Rules:** a wreck's Burn and nothing else; Scrounging's qualifying SW, success, and count.
- **Play:** a T-34 burns a PzKpfw IIIH wreck; a Russian squad scrounges a wreck's BMG as a dismantled LMG, TI, and the wreck is marked; a second scrounge refused.

## Studio
`games_2j.py`, on board 3: a PzKpfw IIIH wreck at [F8] (not burning), a Russian 4-4-7 squad in [F8] in the Russian RPh, and a T-34 M41 in range.
