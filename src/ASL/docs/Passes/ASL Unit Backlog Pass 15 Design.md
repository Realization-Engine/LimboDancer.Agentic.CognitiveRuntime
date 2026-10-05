# ASL Unit Backlog Pass 15 Design

**Status:** Built. Backlog pass 15 (special units and nationalities), as the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 3, sets out.

**Date:** 2026-09-29

**Requirements:** [ASL Unit Requirements](<../Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 15 Review](<Scenario A1 Backlog Pass 15 Review 2026-09-29.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Backlog Pass 14 Design](<ASL Unit Backlog Pass 14 Design.md>), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 25.

Rulings R15.1 to R15.14 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **FT (R15.1).** A FT is fired through `asl.game.fire`, named as its firer's only weapon (the firer fires it apart from its inherent FP, with or without `withoutInherent`): 24 FP at range 0 or 1 (never raised for PBF or TPBF), 12 at range 2 through a LOS with no Hindrance, halved as Area Fire and two levels away, not halved in the AFPh; no TEM, leadership, or heroic DRM; removed on an Original DR of 10, two lower for a non-elite user other than a Finnish 1st Line one, two lower again when captured, and one lower for an Inexperienced or Conscript user. A unit uses one FT or DC in a Player Turn, and one that fired only its FT in the PFPh has Prep Fired. A unit possessing a FT takes each attack's DR one lower per FT, alone of its Location.
- **DC (R15.2, R15.3).** A DC is Placed by a move step (`placeDc`, `placeDcAt` on `asl.game.move`) for the MF entering the target Location; it is operably Placed once its placer leaves or ends its move neither broken nor pinned, and `asl.game.detonate-dc` detonates it in the AFPh, which may not end before. `asl.game.throw-dc` Throws a DC into an ADJACENT Location: +2 there, then +3 at the thrower's Location, each on its own DR. 30 FP, the target's TEM, no Hindrance, leadership, or Cowering; removed on 12 as the FT, by the first DR only. A DC is not Placed where a vehicle is or where the target's TEM is not reviewed. A squad whose only fire this phase is its Thrown DC still fires its inherent FP.
- **MOL (R15.4).** Where the SSRs include `mol:<side>`, `mol` on `asl.game.fire` names the firer making the MOL Check: 1 to 3 after its drm passes and adds four FP after every modification; a colored 6 breaks its user and voids its FP and the MOL's.
- **Snipers (R15.5).** After a fire record, each IFT, MC, or TC Original DR equal to the enemy SAN calls that side's Sniper attack, resolved in the same commit: the Sniper dr, the Random Location DR from the `asl:sniper` counter, the closest hex with an eligible target (Personnel and Dummies), Random Selection, and the effect; the counter moves to the Location attacked. The new record `sniper-attacked` names the result.
- **Commissars (R15.6).** The Russian 9-0, 10-0, and 8+1 Commissars: first to check, no LLMC or LLTC, +1 to the Morale Level of his Location's other units below 10, his leadership alone, never Replaced or Battle Hardened, Surrender as Berserk, a berserk Commissar takes his Location berserk; his rally is immune to DM and Replaces a unit that fails; the RPh does not end while he has not tried to rally a broken unit of his Location.
- **NKVD Field Promotion (R15.7).** The A25.25 table creates a Commissar, in a Rally and in CC.
- **Allied Troops (R15.8).** A side may field several nationalities; a leader influencing Allied Troops of another nationality is one worse, and they take LLMC on that modifier.
- **Underscored Morale Factors (R15.9).** A MMC with an underscored Morale Factor has an ELR of 5 (A1.23). A squad failing a MC by more than 5 becomes its two broken HS; such a HS is Disrupted instead unless Fanatic.
- **Green MMC (R15.10).** The live game supplies a Green MMC's Inexperience from its stack.
- **Heroes (R15.11, R15.12).** A hero fires a MG at full FP, +1 for a SW needing two men and -1 heroic; a hero created by a concealed unit keeping its "?" is concealed.
- **Nationalities (R15.13).** American, British, Italian, Finnish, and French MMC, leaders (the Finns with their own ranks, 8+1 to 10-1), and heroes, with their Heat of Battle and Leader Creation rules; the Italians and Finns Replace and Battle Harden by their own progressions; British elite and 1st Line units and Finns but Conscripts never Cower; Finns but Conscripts Self-Rally; the Finns create no leader.
- **Berserk SW (R15.14).** `keep` on the charge names the 1PP SW kept within the IPC.

## 2. The catalog and the packages

Catalog 1.10.0 adds 92 counters (1,136 worksheet rows): 38 MMC of five nationalities from the A./G. National Capabilities Chart (p. 109), a leader of every grade and a hero of each, the three Commissars (A25.22, A25.224), and a FT and a DC of each side (sheet MFG: FP, range, and removal from A22.1, A22.5, A23.1, A23.4; portage manufactured under R0.3). Every game must use 1.10.0 for the reviewed packages, as before.

The Fire package is revised with its prior manifest digest kept (`f4ce61fa...`, prior `42fa78bb...`; matrix `3d83aded...`): 46 new PDF-compared fragments (A14.1 to A14.3, A22.1 to A22.6111, A23.1 to A23.63, A25.22 to A25.224, A19.3, A25.41, A25.51, and after the referee A1.23, A19.32, A25.45, A25.61, A25.62, A25.7 to A25.72, A25.74), ten rulings, eight cases, and revised exclusions. The Rally package (`3eb6e214...`, prior `ba0278e0...`) adds the Commissar rulings and two cases; the Close Combat package (`7ca1ea7c...`, prior `299e72a8...`) the NKVD table and the BPV of the new MMC; the Ordnance package (`b10deb34...`, prior `120b1df9...`) the new catalog only. The pass 15 PDF comparison (`asl-scenario-a1.pass15-pdf-comparison.json`, `3cccc3cd...`) registers the 46 fragments.

## 3. The game model and records

- `MovementStepped.DcPlacement`, `GameState.PlacedCharges` (pending, then operable; the DC left in its target Location), cleared with the Player Turn; the AFPh may not end with an operable Placed DC the Fire package can detonate.
- `GameState.AssaultWeaponUsers`: the units that fired a FT or Threw or Placed a DC this Player Turn (A22.3). A squad whose only fire of the phase is a Thrown DC is recorded as one SW use (A23.2), as a mortar or PF firer is.
- The record `sniper-attacked`.
- The fire record's facts gain `firingNationalities`, `demolitionCharge`, a firer's `mol`, and a target's `flamethrowers` and Green `inexperienced`; its resolution `molCheck`, `demolitionChargeMalfunctioned`, `flamethrowerRemoved`, and a unit's `splitIntoHalfSquads`.
- A unit Replaced or Reduced by its own Rally attempt has attempted to rally this Player Turn.

## 4. The Play page

- The fire panel offers a FT among the firers' weapons (fired without inherent FP) and a MOL user where the SSR gives MOL; a unit whose SW Prep Fired alone is refused in the AFPh.
- A Demolition Charges panel Throws a DC; in the AFPh each operable Placed DC has a detonate button.
- The move panel Places a DC, and names the SW a berserk mover keeps.
- A Snipers list shows each Sniper attack.
