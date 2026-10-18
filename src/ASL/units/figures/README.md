# Figure parts

The parts the personnel counters' figures are composed from (ASL Unit Figure Art Plan, `src/ASL/docs/Plans`):

- `poses/<pose>.svg`: body poses traced from the user's Midjourney picks, bareheaded and unarmed, 1000 units high, with the head and grip anchors as `data-` attributes.
- `kit/<side>-helmet.svg` and `kit/<side>-<weapon>.svg`: helmets and weapons drawn from each side's documented standard issue by `code-builders/unit-figures/draw_kit.py`.
- `figure-plan.json`: the briefs, prompts, and batches of the Unit Lab's Art tab.
- `figures.json`: what the renderer embeds, written by `code-builders/unit-figures/build_figures.py`; never edited by hand.

A part is a stand-in until its source carries `data-accepted="true"`: the Unit Lab's Art tab draws stand-ins, the counters in play do not. Nothing here is traced from counter artwork or from a book's plates (ASL-UNIT-072; decision F7).
