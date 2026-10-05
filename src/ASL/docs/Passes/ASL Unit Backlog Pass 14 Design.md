# ASL Unit Backlog Pass 14 Design

**Status:** Built. Backlog pass 14 (Close Combat and capture, part 2), as the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 3, sets out.

**Date:** 2026-09-28

**Requirements:** [ASL Unit Requirements](<../Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 14 Review](<Scenario A1 Backlog Pass 14 Review 2026-09-28.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Backlog Pass 13 Design](<ASL Unit Backlog Pass 13 Design.md>), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 24.

Rulings R14.1 to R14.14 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **Hand-to-Hand (R14.1).** Where the game's SSRs include `hand-to-hand`, the ATTACKER declares a Location's CC Hand-to-Hand with its first round other than the prisoners' (`handToHand` on `asl.game.close-combat`), unless it was ambushed there; every attack there that CCPh uses the red Kill Number (black plus two, p. 692). J2.31's own text is not in the registered PDF.
- **Concealment in CC (R14.2).** A concealed unit may advance, keeping its "?" unless it enters Open Ground in the LOS of a Good Order enemy within 16 hexes. An advance may enter a Location of concealed or hidden units or Dummies. As the CCPh begins, the projector removes Dummies and places hidden units beneath a "?" in every Location holding both sides' units (`GameState.HiddenPlaced`). An Ambush can occur with or against a concealed unit in any terrain, or after a hidden unit was placed, with -2 to a concealed force's dr; the ambushed side loses all concealment. An attack on a concealed defender is halved; a concealed attacker or director loses its "?" unless its ambushing attack clears its target; so does a unit Casualty Reduced or wounded, and a concealed unit declared stacked with a Known one (A11.14). A concealed unit is neither held in Melee nor holds.
- **TI (R14.3).** +1 to a TI unit's CC attack, -1 to one against it; the old refusal is gone.
- **Capture attempts (R14.4).** An attack may be `capture: true`: +1, or -1 against an Inexperienced (or Unarmed) defender. Below the Kill Number the defenders are captured; at it one unit of the defender's declared `yield` order, a squad giving up one HS (a Deployment lineage, the other HS keeping the SW). The Guard is the attack's named `guard`, its first attacker, or another unit of that side with capacity; with none the unit is freed as Unarmed. A side wiped out in a simultaneous round captures no one (A20.221).
- **Unarmed units and Guards (R14.5).** A new condition, `asl:unarmed`, set on capture and kept when freed: CC FP one, Inexperienced, no fire, no SW use, Recovery, or transfer to it, no entry into a Known enemy Location. A Guard may advance into CC with its prisoners; its CC FP against non-prisoners is halved. When a Guard leaves its prisoners' Location, is eliminated, or is captured, the projector hands them to another armed unit of its side there with capacity, or frees them; a Reduced or Replaced Guard keeps them in the unit that takes its place. A surrendering unit with no captor able to guard it is freed as Unarmed (`free` on `asl.game.take-prisoner`).
- **Escape and rearming (R14.6).** `round: "prisoners"` on `asl.game.close-combat` is the prisoners' round, before any other in the Location: prisoners of a broken Guard pass a NTC first (none when the Guard is held in Melee), and each attack takes in the Guard. A prisoner that attacked is freed (`prisoner-freed`); a SMC among them is Armed once its escape succeeds. An attacking Unarmed MMC is rearmed as a Conscript MMC of its size for each armed enemy unit of at least its size its attack took (a Replacement lineage).
- **Prisoner transfer (R14.7).** `asl.game.guard-prisoners`: in its side's RPh or APh a Guard not in Melee hands its prisoners to another armed unit of its side in its Location with capacity, or abandons them.
- **Infiltration (R14.8).** `infiltrations` on `asl.game.close-combat` names each unit's destination. The ATTACKER's attacks resolve before the DEFENDER's; after an Original 2 the attackers with a destination leave (with a leader they created), and after an Original 12 the surviving unpinned defenders with one leave, forfeiting their own attacks not yet resolved; no later attack affects a unit that left.
- **Ambush Withdrawal (R14.9).** `asl.game.ambush-withdraw`: the ambushing side's units leave before the Location's first round or once its CC is over; when every ambusher has left, the ambushed side's round closes the Location.
- **Overstacking (R14.10).** +1 per excess squad-equivalent to a side's CC attacks and -1 to attacks on its units; an advance into an overstacked Location pays one MF more per excess and is no longer refused.
- **A Disrupted unit's surrender (R14.11).** Good Order armed Known Personnel advancing into a Disrupted enemy's Location take its surrender (a pending surrender, the captor's choice), unless No Quarter.
- **Field Promotion (R14.12).** The MMC of the highest BPV (from the National Capabilities Chart, p. 109, recorded in the matrix's `bpv`) founds a leader created in CC; when an enemy attack takes some of the attack's MMC, the one he defends with is chosen by Random Selection (`leaderStack`).
- **Odds and Ambush drm (R14.13).** Odds from 10 to 1 up to 11 to 1 are 10-1; a berserk force takes both +1 berserk and +1 Lax.
- **Mandatory CC (R14.14).** A berserk or reinforcing unit's required round lapses when the package refuses every attack it could make there.

## 2. The package

The Close Combat package is revised with its prior manifest digest kept (`299e72a8...`, prior `2dde0d2b...`; matrix `1a49b85e...`): 28 new source fragments (80 in all), the rulings `handToHand`, `concealment`, `ti`, `capture`, `prisoners`, `escape`, `infiltration`, `overstacking`, and `bpv`, revised `odds`, `fieldPromotion`, and `scope`, the `bpv` values and their source, ten new cases, and three cases removed (`A1-cc-concealment-unreviewed`, `A1-cc-prisoners-unreviewed`, `A1-cc-overstacked-unreviewed`). The pass 14 PDF comparison (`asl-scenario-a1.pass14-pdf-comparison.json`, `01407862...`) registers nine new fragments.

The round is now figured in two passes: the attacks in their order (the ATTACKER's first), each rolled as its turn comes, with the departures the Original 2s and 12s allow; then every attack again with each created leader as if he had been there all along, the departures kept. A forfeited attack rolls nothing.

## 3. The game model and records

- The new record `prisoner-freed` (an escape, an abandonment, or a surrendering unit no one can guard); the new condition `asl:unarmed`; `CloseCombatLocation.HandToHand`; the round `prisoners`; `GameState.HiddenPlaced`, cleared at every phase change.
- The projector starts each CCPh by removing Dummies and placing hidden units, keeps Guards (succession or freeing) after every event, moves a Guard's prisoners with it on any move, and allows the prisoners' round before any other.
- `LiveCloseCombat` reads Unarmed units, prisoners' Guards, TI, the declared Infiltrations, and Hand-to-Hand into the facts, and the new roll keys `escapeNtc` and `leaderStack`.
- The gate reads back a freed prisoner.

## 4. The Play page

- The CC panel: a Hand-to-Hand box where the SSR allows it, a prisoners' escape round box, a capture attempt box with the Guard and the defender's order of choice, each unit's Infiltration destination, and Ambush Withdrawal buttons after an Ambush.
- A surrender offers "free it as Unarmed"; in the RPh and APh a Prisoners panel hands a Guard's prisoners to another unit or abandons them.
- The CC record names captures and their Guards, escape NTCs, escapes, rearming, Infiltrations, lost concealment, and Hand-to-Hand; the Units table shows Unarmed, captured, concealed, and TI units.
