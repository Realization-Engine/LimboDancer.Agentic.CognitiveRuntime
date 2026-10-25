# Increment 3e: the owner's choices on a "?", and concealment for crews and vehicles (tasks 70.4, 70.5)

Read `brief_build.md` beside this file first; it binds you. The rulings are R70.20 to R70.22 (design section 6, F9 and F10). Frame B's report, sections 2 and 3, names the code. 3a is committed: every "who sees" read goes through its nearest-seer memo; use it.

## Pages
- p. 76 to 78: A12.11, A12.12, A12.121 and the Concealment Loss/Gain Table, A12.122, A12.14 (voluntary removal at any time; the Sniper EXC; the viewer's option), A12.141.
- p. 79: A12.2 (5/8" counters: vehicles, PRC, non-Emplaced Guns; Case H; woods-road and like hexes at setup).
- p. 80: A12.3, A12.31, A12.33, A12.34 (HIP; Hidden Guns).
- **Render the Concealment Table** (its columns are scrambled in the text) at 200 dpi or more and read Cases J and K for a crew and a vehicle, and Case H. Keep the render in the scratch folder.

## Build
1. **Voluntary removal (R70.20).** An action of the owner at any time in his or the enemy's Player Turn: it writes `concealment-lost` for the chosen units (or a new event if `concealment-lost` cannot carry a voluntary cause; say which). A Dummy so removed is eliminated; a hidden unit is placed and revealed in full (A12.31). The Sniper exception is noted in a comment and not built. The page offers it hex first, own units only.
2. **The viewer's option (R70.21).** Where every Loss seer of the viewing side within 16 of a concealed enemy unit that would lose "?" is itself concealed, the viewing side may decline to force the loss: a pending choice to that side at the one read of 3a (`ChoicePending` with Take/Decline is the model, as A12.41's reveal-or-PAATC uses it), its brief reveal shown to the moving side only. Build it for the move first; then the advance and fire if their resumes fit. If they do not fit this increment, stop there, keep R31d.2's deviation for the rest, and say so: that part goes to the backlog.
3. **Gain for an Emplaced Gun's crew and a vehicle (R70.22).** Under the table's Cases J and K; A12.122's +1 Lax, -1 Stealthy, and a possessed Gun's US# in the concealment dr. The gain search today excludes vehicles and Gun-holders (`ScenarioA1FireFollowUps.cs` about :52, :55).
4. **A vehicle's Concealment Terrain at setup (R70.22).** Woods-, orchard-, brush-, and grain-road hexes count as Concealment Terrain for placing OB "?" and SSR HIP at setup only; afterwards the vehicle is on the road (B13.31). The rule for vehicles today is grain in season (`ScenarioA1VehicleSightRules.cs` about :11).
5. **Entering concealed (R70.22).** Reinforcements whose card OB gives "?" may enter concealed (A12.12), lifting R20.5's refusal; a Dummy among them if the card gives Dummies (read how the card's OB names them; if no card does, say so and build the concealed entry only).
6. **HIP (R70.22).** HIP for vehicles by SSR (A12.3; `hip:side:n` exists; `HiddenSetupKinds` is Infantry only, the setup refuses hidden vehicles). An Emplaced Gun may set up hidden out of Concealment Terrain when out of every enemy LOS (A12.34), revealed when that changes: after each step and at each phase, read through 3a's memo.
7. **Records and the page.** What the side that must not see is shown: a voluntary removal shows the unit; a declined loss shows nothing to the viewing side beyond what it saw; a hidden vehicle shows nothing.

## Tests (by method)
- **Rules:** removal of a Dummy and of a hidden unit; the viewer's option offered only when every seer is concealed; Cases J and K for a crew and a vehicle; the dr with Lax, Stealthy, and a Gun's US#; a woods-road vehicle's setup "?"; HIP for a vehicle by SSR; an Emplaced Gun's HIP out of Concealment Terrain and its reveal.
- **Play:** a voluntary removal; a declined loss in a move; a vehicle gaining "?"; a concealed reinforcement entering; a hidden Gun revealed when an enemy comes into LOS.
- **Blast radius:** frame B's section 7, its HIP and gain methods (`HipNeedsItsSsrAndStaysUnseen`, `OnlyInfantrySetsUpHidden`, the gain methods); run those your change reaches, by method.

## Studio
`games_3e.py`, on board 3, one game per item that stages simply (a concealed German squad in the woods of [F5] for the removal and the viewer's option, a German vehicle in grain for the gain, a hidden Gun out of Concealment Terrain). Say which hex pairs you rely on for LOS and how you checked them.
