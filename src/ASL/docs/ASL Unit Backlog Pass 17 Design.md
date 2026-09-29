# ASL Unit Backlog Pass 17 Design

**Status:** Built. Backlog pass 17 (scenario cards), as the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 3, sets out after the user's scope ruling of 2026-09-29: the card presentation only. Card-driven play is backlog.

**Date:** 2026-09-29

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 17 Review](<Scenario A1 Backlog Pass 17 Review 2026-09-29.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Backlog Pass 16 Design](<ASL Unit Backlog Pass 16 Design.md>), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 27.

Rulings R17.1 to R17.11 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **A card format (R17.1).** `asl-scenario-card/1`: a JSON document with the title, the place and date, an introduction, the board configuration and North, a playable area, the Turn Record Chart, the Scenario Defender, two sides (SAN, Battlefield Integrity total, Friendly Board Edge with its basis, Balance, and OB groups with their ELR, setup or entry areas, and counter lines naming catalog definitions), the SSRs with their status, the Victory Conditions as text, the aftermath, and the source and adaptation notes. Unknown fields are refused.
- **A reader and validation (R17.2 to R17.11).** `ScenarioCards` (Play) reads the cards embedded from `src/ASL/units/scenarios` and checks them against the published catalog: the catalog version, the date, the boards and their slots, North, the Turn Record Chart's sides, SAN 0 to 7, ELR 0 to 5, the Friendly Board Edges and their basis, setup areas on the card's boards and entries within its turns, every counter in the catalog and of its side, the Battlefield Integrity totals recomputed from the catalog's BPV (refused when a starting MMC has no BPV, and none printed when a side starts with fewer than ten squad-equivalents), a building area that includes its anchor hex, the Scenario Defender only when it sets up on board against a side that enters wholly from offboard, the SSRs numbered and each with a status, the SSR tokens checked by the same check a new game uses, and every rule cited either a fragment of the pass 17 PDF comparison or a ruling.
- **Two adapted cards (R17.2, R17.9).** The Guards Counterattack (board 1, Stalingrad, 6 October 1942) and Gambit (boards 4 and 2, Crete, 21 May 1941), adapted from The General's ASL Scenario A (Vol. 22 No. 6, p. 51) and ASL Scenario T14 (Vol. 28 No. 6, p. 64). Their text is paraphrased; every entry rests on the registered rulebook; what it cannot support is shown as not enforced.
- **Catalog 1.11.0 (R17.9).** Six counters: the German circled-E 5-4-8 squad and its 2-3-8 HS (sheet NCC), the British OML 2-in. mortar (sheet OLG), and the German HMG, British LMG, and British ATR, manufactured under R0.3 on sheet MFG. The legacy Russian 9-0 is the 9-0 Commissar (A25.22).
- **The Studio's Scenario cards page (R17.1).** `/units/scenarios` shows a card: its source and adaptation, the boards, the Turn Record Chart, the Scenario Defender, each side's SAN, Integrity total (marked optional and not built), Friendly Board Edge with its compass direction and basis, Balance, and OB tables (count, counter, setup or entry, and the source sheet, with manufactured counters marked), the SSRs with what the game does with each and the project's rulings marked as rulings, the Victory Conditions marked not evaluated, and the adaptation notes. The boards read by row from the map's top, and a building area lists its hexes.

## 2. Format

| Field | Content | Rule |
|---|---|---|
| `format`, `id`, `title`, `catalog` | `asl-scenario-card/1`; the catalog as `asl-scenario-a1@1.11.0` | R17.1 |
| `source` | `basis`, `legacy`, `adaptation` notes | R17.2 |
| `place`, `date`, `introduction`, `aftermath` | Text and the day, month, and year | R17.1 |
| `boards`, `north`, `playableArea` | Each board with `column`, `row`, `reversed`; North as a map edge; the playable area with `enforced` | A2.1; R17.3 |
| `turns` | `count`, `halfTurn`, `setsUpFirst`, `movesFirst` | A3.9; R17.4 |
| `scenarioDefender` | A side or null | Index; R17.6 |
| `sides[]` | `side`, `san`, `integrityBpv`, `friendlyEdge` (`edge`, `basis`, `note`), `balance`, `groups[]` | A14.1, A16.1, A20.53, A26.4; R17.5, R17.7 |
| `groups[]` | `name`, `elr`, `areas[]` (`building` hexes, `hex-numbers` from and to on a board, or `entry` on a turn along an edge, each with an optional `limit`), `units[]` (`definition`, `count`, `area`) | A19.1; R17.8 |
| `specialRules[]` | `number`, `text`, `status` (`token`, `game-default`, `not-enforced`), `tokens`, `rules`, `note` | R17.10 |
| `victoryConditions` | `kind` (`control`, `exit`, `casualty`, `other`), `text`, `rules` | A26; R17.11 |

## 3. What is not built

Setting up a game from a card, reinforcements entering during play, evaluating the Victory Conditions, Battlefield Integrity, the playable-area limit, Balance selection, the full Chapter H DYO purchase, and the SSRs the game has no token for. They are in the backlog, section 27.

## 4. Tests

`BacklogPass17Tests` (Play): the cards read and validate; each card presents its numbers; the Integrity totals are the catalog's BPV; Gambit prints none; the exit hexes and setup buildings are on their boards (Hex Fact oracles for boards 01, 02, and 04), the Victory Conditions count only stone buildings, every setup building is its own building, and the Friendly Board Edges follow from the setup column by column; the counters come from the catalog with the manufactured ones labeled, the 9-0 Commissar, and Gambit's two 8-0; and the refusals (a Scenario Defender against an on-board side or one that enters wholly from offboard, a building area without its anchor hex, SSR tokens as a new game checks them, a token status without tokens, a not-enforced SSR without a note, an uncited rule, a counter not in the catalog or of the other side, SAN, ELR, and edge limits, an unknown field, the old catalog, and a setup area off the boards or after the last turn). `ScenarioCardsPageTests` (MapStudio): the page shows both cards. `ThePass17SubjectsAreVerified` (Authoring): the 22 compared fragments.
