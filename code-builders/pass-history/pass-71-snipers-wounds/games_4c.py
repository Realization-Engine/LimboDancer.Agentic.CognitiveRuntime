"""The Studio game of increment 4c (the Sniper counter at setup, A14.01, A14.2; ruling R71.11), on board 3.

(a) p71-sniper-by-hand: a game made without a card, still in setup (no phase has begun): the German 4-6-7 g1 in [P10], the Russian 4-4-7 r1 in [P8], the
Russian SAN 3, and no Sniper counter. No LOS pair is relied on (A14.2 counts hexes only). In the Russian view, in the setup form: choose "the Sniper
counter (russian)" and a Location, add it, and propose the list.
  - bd03:P8:0 is refused: "play.sniper-placement: [P8] is no hex for the russian Sniper counter: it goes in an unoccupied hex within six hexes of 1 or
    more enemy-occupied hexes, the most any unoccupied hex has; choose a hex the setup offers (A14.2; ruling R71.11)" (r1 holds it).
  - bd03:B5:0 is refused the same way (fourteen hexrows from [P10]).
  - bd03:P9:0 is accepted: read back an "instance-created" with "kind": "asl:sniper", "side": "russian", "position": {"at": "bd03:P9:0"}, visibility "all".
  Then "end the phase" starts play; the German SAN is none, so #play-sniper-missing is not shown. Written with the German SAN 2 instead (g.san("german", 2)),
  the page says once, in #play-snipers' panel: "The German side has a SAN but no Sniper counter in this game, so it makes no Sniper attacks (A14.01)."

(b) The card case is started in the Studio itself, not written here: a card's game must be set up through the gate. New game from "The Guards
Counterattack" (board 01; both SANs 6; the Germans set up first, the Russians move first, so the Germans are the DEFENDER). Set up the German OB, then the
Russian OB (setup plans or "Set up" under Counters). Under Counters, the fieldset #setup-sniper appears:
  - German view: "Sniper counter for German (A14.2; ruling R71.11)", the select #sniper-hex-german ("Choose a hex (N)", then each hex as "[F7]"), the
    button #propose-sniper-german "Propose: place German's Sniper counter", and #sniper-next "The Russian side places its own next (A14.2: the DEFENDER
    first)." Russian view: only #sniper-waiting "The German side places its Sniper counter now (A14.2). ...", no hex.
  - "end the phase" before then is refused: "play.setup-incomplete: the german side has a SAN of 2 or more and places its Sniper counter before play
    starts: propose its hex in the setup's Sniper step (A14.01, A14.2; ruling R71.11)".
  - Propose and confirm a hex; then the Russian view offers #sniper-hex-russian; once it is placed, "end the phase" starts play. Read back two
    "instance-created" events of "sniper-german" and "sniper-russian", kind "asl:sniper", visibility "all"; the proposal's arguments carry "sniper":
    {"side": ..., "at": ...}.
  - An old card game (any game started before the pass, such as one whose setup is closed) shows #play-sniper-missing: "The German and the Russian sides
    have a SAN but no Sniper counter in this game, so they make no Sniper attacks (A14.01)."

Not run by the building agent: the session runs it (it writes under src/ASL/boards).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

g = GameFile("p71-sniper-by-hand", "pass 71, 4c: a Sniper counter placed by hand under A14.2's check (ruling R71.11)", phasing="german")
g.san("russian", 3)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:P10:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
g.write()
