"""Pass 45, task 45.2: the ROF button gets a block of its own, shown to the DEFENDER and to the adjudicator (the DFF window is not the adjudicator's)."""
import sys

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor"
OLD_BUTTON = """                            @if (GamePlanner.RateOfFireForfeit(state, history!.Events) is { } forfeit)
                            {
                                <button id="propose-forfeit-rof" title="A8.23: a weapon that kept its Multiple ROF leaves no Residual FP of its own. Give the ROF up now, before anything else, and its FP counts toward the Residual FP; the weapon is marked First Fire."
                                    @onclick='@(() => ProposeSimple(GameActions.ForfeitRof, "forfeit-rof"))'>
                                    Propose: give up the ROF of @string.Join(", ", forfeit.Weapons.Select(id => Words(id))) to leave @forfeit.Fp Residual FP in @PlaceOf(forfeit.At)
                                </button>
                            }
"""
ANCHOR = """                    @* Pass 28c (A8.1, A8.11): after each step the DEFENDER, from its own view, sees where the stack is and fires at it or passes.
"""
BLOCK = """                    @* Pass 45 (A8.23): at once after a Defensive First Fire attack in which a weapon kept its Multiple ROF, the DEFENDER may give it up. *@
                    @if (state.Phase == "mph" && (Viewer.IsAdjudicator || viewAs != state.PhasingSide) && GamePlanner.RateOfFireForfeit(state, history!.Events) is { } forfeit)
                    {
                        <div class="toolbar" id="forfeit-rof">
                            <span class="toolbar-note">@string.Join(", ", forfeit.Weapons.Select(id => Words(id))) kept its Multiple ROF, so its FP left no Residual FP. Give the ROF up now, before anything else, and @forfeit.Fp Residual FP stands in @PlaceOf(forfeit.At); or go on, and it may fire again (A8.23).</span>
                            <button id="propose-forfeit-rof" @onclick='@(() => ProposeSimple(GameActions.ForfeitRof, "forfeit-rof"))'>Propose: give up the ROF for @forfeit.Fp Residual FP</button>
                        </div>
                    }
"""
with open(PATH, encoding="utf-8", newline="") as f:
    text = f.read()
if text.count(OLD_BUTTON) != 1 or text.count(ANCHOR) != 1:
    sys.exit(f"anchors: {text.count(OLD_BUTTON)} {text.count(ANCHOR)}")
text = text.replace(OLD_BUTTON, "").replace(ANCHOR, BLOCK + ANCHOR)
with open(PATH, "w", encoding="utf-8", newline="") as f:
    f.write(text)
print("ok")
