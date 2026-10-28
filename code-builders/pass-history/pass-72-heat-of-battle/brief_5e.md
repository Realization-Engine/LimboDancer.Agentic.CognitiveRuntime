# Increment 5e: the hero's weapons (72.1, part III)

Read `brief_build.md` beside this file first; it binds you. Then the design's sections 2 to 7 and its sections for the increments built, and `frame_heroes.md` (your code map). Read `git -C E:/Archive/GitHub/dlandi/LimboDancer.MCP log --oneline -5` first.

## Pages
A15.2 to A15.24 (p. 83), C13.21, C13.25, C13.45, C13.48, C13.62 (pp. 183 to 185), C9.2 on a lone SMC's mortar (find its page).

## Build (ruling R72.8)
1. **The -1 with a non-MG SW.** A hero firing an ordnance SW that is not a MG (light mortar, PF, PIAT, ATR at a vehicle) takes -1 on its To Hit DR only, never again on the HE's IFT DR after a hit, never on a To Kill (C13.25). A firer DRM named `hero:<id>` in the ordnance calculator's To Hit DRM (near the `Leadership` DRM), labeled in `DisplayText` (near 392) and the record lines.
2. **The ATR as Small Arms.** Lift the hero's refusal (ScenarioA1FireCalculator near 753, and its text in `RefusalReasons`). He forfeits his inherent FP as with a MG; A15.24's existing heroic -1 applies within the ATR's Normal Range; no `hero-mg` +1 (it is no MG) and no second -1.
3. **A lone hero's PSK.** Lift the refusal for a hero (OrdnanceCalculator near 442; a leader stays refused, R9.11): +1 on the To Hit for one man firing a two-man SW, negated by his heroic -1 (C13.45, C13.48), both named in the arithmetic. Two SMC together stay refused (pass 110).
4. Check whether a hero firing a light mortar forfeits his inherent FP in the record (A15.23); if not, make it so.

## Not in 5e
Guns, the AAMG as a Rider, the bazooka, the RCL, the MOL-Projector (passes 105 and 110), a heroic leader's heroic qualities (pass 73).

## Tests by method
Rules: a hero's light mortar To Hit with -1 and no -1 on the HE IFT; a hero's PF and ATR at a vehicle with -1 on the To Hit; a lone hero's PSK with +1 and -1; a leader's PSK still refused; a hero's ATR as Small Arms at Personnel with the heroic -1 and no `hero-mg`. Play: a hero fires a PSK at a tank; a hero fires an ATR at a squad. Grep and run by method the hero tests (`ScenarioA1Pass15Tests.AHeroFiresAMgWithItsTwoManDrmAndHisHeroicDrm`, `BacklogPass15Tests.AHeroFiresAMgAtFullFpWithTheTwoManDrmNegated`, `BacklogPass15TablePlayerTests.AHalfSquadAndAHeroFireFlamethrowers`), the PSK and ATR refusal tests, and the ordnance To Hit DRM tests you touch.

## Studio games
`games_5e.py`, board 3: a German hero with a PSK against a Russian T-34; a Russian hero with an ATR against a German squad; a German hero with a light mortar against a Russian squad.
