// The Studio shell (plan section 10): tells the layout whether the window is narrow, so the navigation becomes a drawer.
// No styles are written here; the layout sets classes and site.css does the rest (section 11.4).

const narrowQuery = "(max-width: 1023.98px)";
let watcher = null;

export function watchNarrow(dotnet) {
    unwatchNarrow();
    const query = window.matchMedia(narrowQuery);
    const listener = event => dotnet.invokeMethodAsync("SetNarrow", event.matches);
    query.addEventListener("change", listener);
    watcher = { query, listener };
    return query.matches;
}

export function unwatchNarrow() {
    if (watcher) {
        watcher.query.removeEventListener("change", watcher.listener);
        watcher = null;
    }
}
