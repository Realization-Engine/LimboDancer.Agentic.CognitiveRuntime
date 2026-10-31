# Minigames apart from real games: design

Approved by the user on 2026-10-31 ("Yes, build it"). The user's word that started it: the minigames were listed with the actual games, which was
confusing; a minigame page of its own, or the minigames moved into the Rule Workbench.

Built as: `Components/Pages/MinigameList.razor` (the page; not named `Minigames`, which would hide the registry `Minigames` inside every page),
`Components/Shared/MinigameNote.razor` (the "Minigame: ..." line), and `LivePlay.GameNames()`, `MinigameGameNames(bundle)`, `GamesLike(game)`
in place of `GameNames(bool)`. Scenarios shows a minigame's card only by a link that names it, and lists it alone. The Workbench closes its game when
the address names none, so its list goes back to the minigames' games. Page tests: `MinigameListsTests`.

## Today

- A game is a minigame's when its record carries `minigame` (ruling RPS.6): taken from the card at the first commit, from `Minigames.BundleOfCard`. A card is a minigame's when a bundle manifest names it (`Minigames.IsMinigameCard`).
- Play, Replay, Games (inspector) and Scenarios each have a "Show minigames" box, off by default (RPS.10). Ticked, the minigames' games and cards are mixed into the same list as the real ones. The game open is always listed, so opening a minigame mixes it in too.
- The Rule Workbench has the same box, on by default: minigames first, then every real game, in one list.
- A minigame is started only on Play: tick the box, pick the minigame's card, start.
- Live games without the field look like real games: p70-perf-tw, p71-*, p72-* (pass check games, older than minigames). Bundle files `a23-placed-dc/games/wire-dc-*.game.json` lack the field too.

## Recommendation: a Minigames page as the hub; no list ever mixes the two

1. New page `games/minigames`, in the Play group of the navigation (Play, Replay, Minigames, Rule Workbench). One section per bundle, from its manifest: title, rules, contracts, the example and its page, the number of checks; then its live games (those whose `minigame` is the bundle). Buttons: "Start a new game" (Play, on the bundle's card), and for each game Play, Replay, Rule Workbench.
2. Play, Replay, Games, Scenarios lose the "Show minigames" box. Their lists hold real games and real cards only. When a minigame's game or card is opened by a link (from the Minigames page), the list holds only that bundle's games or cards, with a line "Minigame: C9 Mortars" and a link back. A list is always one kind.
3. The Rule Workbench loses its box and lists minigames' games only. A real game still reaches it by the Play and Replay links (RPS.11); then its list holds real games only.
4. Play takes `?card=` so the Minigames page can open a new game on the bundle's card.

How a game is told to be a minigame does not change: the record's `minigame` field, and for a card, the manifests.

## Why not the Workbench alone

The Workbench reads one position and asks one contract; it neither starts nor plays a game. Starting needs Play's card and setup flow, and playing needs Play's whole move UI, so a minigame would still leave the Workbench for Play. A page of its own holds the overview (rules, checks, games) and sends each action to the page that does it. The Workbench stays the place where a minigame is inspected.

## Left out

- p70/p71/p72 live games carry no `minigame` field, so they stay in the real lists; the user is clearing old games in a parallel session.
- `a23-placed-dc/games/wire-dc-*.game.json` lack the field; a follow-up can add it.

## Second step: the minigames' cards (approved by the user, 2026-10-31)

After PR 89 a minigame's card still opened on the Scenario cards page (from the Minigames page's "Its card" and from Play's "The whole card"), and
its "Copy this card to edit it" made a copy that the card editor saved as one of the user's scenario cards, without the minigame's link.

1. The Minigames page shows each minigame's card in its own section: "Show its card" draws it, only while open. `games/minigames?open=<bundle>`
   opens that card with the page and brings its section to the top (`wwwroot/js/minigames.js`; at a narrow width it follows the menu's collapse).
2. Play's "The whole card" links to that address for a minigame's game, and to the Scenario cards page for any other.
3. The Scenario cards page never shows a minigame's card: an address that names one keeps the scenario cards and adds a line pointing to the
   Minigames page.
4. The card editor takes no minigame's card as its source: an address that names one starts from a minimal card and says why. A deliberate
   "make a scenario from this minigame" can be added later as its own action.
