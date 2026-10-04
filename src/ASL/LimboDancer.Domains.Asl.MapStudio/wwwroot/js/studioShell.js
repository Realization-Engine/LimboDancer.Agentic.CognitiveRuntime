// The Studio shell (plan section 10): tells the layout whether the window is narrow, so the navigation becomes a drawer.
// No styles are written here; the layout sets classes and site.css does the rest (section 11.4).

// Pass 31c (design D15): a window under 600px high is narrow too, whatever its width, so a short window gets the tabs and not a cramped grid.
const narrowQuery = "(max-width: 1023.98px), (max-height: 599.98px)";
let watcher = null;

export function watchNarrow(dotnet) {
    unwatchNarrow();
    const query = window.matchMedia(narrowQuery);
    const listener = event => dotnet.invokeMethodAsync("SetNarrow", event.matches);
    query.addEventListener("change", listener);
    watcher = { query, listener };
    return query.matches;
}

// Plan section 11.1: from 1024px to 1439px the navigation starts collapsed, so the workspaces keep their width; it stays the user's choice after.
export function isMedium() {
    return window.matchMedia("(max-width: 1439.98px)").matches;
}

export function unwatchNarrow() {
    if (watcher) {
        watcher.query.removeEventListener("change", watcher.listener);
        watcher = null;
    }
}
