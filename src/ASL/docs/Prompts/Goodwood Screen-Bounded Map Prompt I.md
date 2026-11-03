# Goodwood Screen-Bounded Map, Prompt I

Written 2026-11-03 at the end of the Map Lab session, for the next session.

## The user's word

"In our next session, let's try to apply these improvements to the Goodwood ASL Map."

Until now the Map Lab's findings stayed in the Lab (the user's decision of 2026-11-02: the Lab has no target outside itself, and its findings are applied elsewhere only when the user gives a reason). This is that reason, for the Goodwood ASL map. The user has also said not to re-argue why the old presentation is slow: the analysis is done and committed. The task is to bring what the Lab shows to Goodwood.

## State

- `main` is at a98cbfd3, CI green. Merged on 2026-11-02 and 2026-11-03: PR 108 (the Map Lab), PR 109 (the screen-bounded map for Goodwood, behind `?present=bounded`), PR 110 (hex labels as one canvas), PR 111 (the Lab drags without pauses).
- No branch is open for this work. My Studio is stopped and the outside build outputs are cleaned. The untracked `.codex/` folder is not mine.

## What the Lab shows

The Map Lab Plan, sections 8 to 8.2, holds the measures. In short:

- With the terrain, the grid, and the Formation boundaries painted into one set of tiles (`tileslines`), a drag runs at the display's rate at every size measured, up to 1,000,000 hexes (F6). Nothing sent to the browser grows with the map but the labels' data (F7).
- On a 10,000-hex map with labels readable, a pane-long drag ran at 59.6 fps on a 60 Hz display, longest frame 33 ms, and the user's own drags showed no pauses. Two things gave that: labels painted into one canvas off the page, and every tile painted in the background before it is needed.
- The Lab's map is two layers, the tiles and the labels. Its grid stays in the tiles at every zoom: no vector grid, no switch of tile variant as the view zooms in.

## Where Goodwood stands

The Screen-Bounded Map Analysis, sections 5.6 and 5.7, holds the measures. With `?present=bounded`, Goodwood already has lines in the tiles, tiles painted ahead to disk at every level, counters and setup marks hidden outside the view, the labels as one canvas, and old tiles kept through a switch. Against the Lab, it differs in these ways:

- **At near zoom it switches to a vector grid** (decision N2): the tile variant changes from `te+g` to `te`, and the near grid is drawn as one path for the view. A commit at near zoom still makes a frame of about 60 ms with labels hidden.
- **It has more layers:** counters, setup marks (still sent as markup, the deviation from N3), and the boundaries tied to a game's formations.
- **It is not the default** (N6 is open): the old presentation is what a user sees without `?present=bounded`.
- **The figures:**
  - A first pan over new ground went from 41.5 to 82.4 fps, with no long frame.
  - Zooms did not improve. A deep wheel zoom from Fit takes about 700 ms to become sharp, with two frames of 143 ms.
  - Labels cost 5 to 7 fps and one or two frames of 60 to 70 ms at readable zooms.

I checked the trigger for painting ahead. On Goodwood the page asks for `ahead-*.json`, which is served `no-store`, so the browser's cache cannot block it. The Lab needed a fix there; Goodwood does not.

## What is open

- The backlog's section 96 (Large Campaign Map) lists the following:
  - the deep-zoom stalls;
  - making the screen-bounded map the default (N6);
  - setup marks as data;
  - centre dots in the near grid;
  - a warm start from the tiles on disk, not yet measured.
- Section 97 holds the Lab's deferred items.
- In the Lab, one frame of 183 ms five steps in is still unexplained.

## Measuring

Speed is judged in the user's Chrome, never in the browser pane. Different runs were made on different screens:

- The Lab's runs in section 8.2: 1,290 by 534, on a 60 Hz display.
- Goodwood's runs: 912 by 558 or 734, on a display of about 85 Hz.

So compare each map against its own earlier runs, not one map against the other.

## Where to start

Read the Screen-Bounded Map Analysis (sections 5 to 7), the Map Lab Plan (section 8), and the backlog's sections 96 and 97. Then propose how to bring the Lab's approach to Goodwood, taking each difference above in turn.

The plan states its baseline, its expected gains, its method, and its stop threshold before its tasks, and answers the checklist of the ASL Memory and Performance Guidelines. Nothing is built until the user approves it.

The standing rules are in `Pass Standing Rules and Harness Lessons.md` in this folder, and they apply as written.
