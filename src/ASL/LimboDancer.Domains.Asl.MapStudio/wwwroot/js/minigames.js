// The Minigames page: when the page opens on a minigame further down its list, the list (which scrolls on its own) shows that minigame's entry.
// The page itself does not move. No styles are written here.

export function showChosen(listId) {
    const list = document.getElementById(listId);
    const chosen = list?.querySelector("a.active");
    if (!list || !chosen) {
        return;
    }

    const pane = list.closest(".minigame-index");
    if (!pane) {
        return;
    }

    const top = chosen.getBoundingClientRect().top - pane.getBoundingClientRect().top + pane.scrollTop;
    if (top < pane.scrollTop || top + chosen.offsetHeight > pane.scrollTop + pane.clientHeight) {
        pane.scrollTop = Math.max(0, top - pane.clientHeight / 3);
    }
}
