// The Play workspace (plan sections 13.1 and 14): brings a pane into view and gives it the focus, even when it had the focus already, so the
// context's Review button always shows the review. With scroll false the pane takes the focus where it is (pass 29: after a commit in the wide
// layouts, where every pane is in view already). No styles are written here.

export async function reveal(element, scroll = true) {
    if (!element) {
        return;
    }

    // Pass 29: the render that shows a proposal also grows the context (its Review button and status). Wait two frames, so the context's new
    // height is measured and the pane stops below it, not under it.
    await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));

    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (scroll) {
        element.scrollIntoView({ block: "start", behavior: reduce ? "auto" : "smooth" });
    }

    element.focus({ preventScroll: true });
}

// Brings an element into view by its id, inside its own scrolling pane (pass 30: the stack or the counter of the setup list chosen on the map).
export function revealId(id) {
    const element = document.getElementById(id);
    if (!element) {
        return;
    }

    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    element.scrollIntoView({ block: "nearest", behavior: reduce ? "auto" : "smooth" });
}

// Opens a disclosure by id and brings it into view (pass 29: the scenario card starts closed, and the context's link opens it).
export function openDetails(id) {
    const details = document.getElementById(id);
    if (!details) {
        return;
    }

    details.open = true;
    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    details.scrollIntoView({ block: "start", behavior: reduce ? "auto" : "smooth" });
    details.querySelector("summary")?.focus({ preventScroll: true });
}

// Closes a disclosure and gives the focus back to the control that opened it (Claude Design's review: the card closes from its foot).
export function closeDetails(id, focusId) {
    const details = document.getElementById(id);
    if (details) {
        details.open = false;
    }

    document.getElementById(focusId)?.focus();
}

// The workspace fills the window below the sticky context, whose height changes with its content and the window's width (UI review, pass 28c):
// the context's height is kept in a custom property the stylesheet reads. Only a measurement is written here.
let contextWatcher = null;

export function watchContext() {
    unwatchContext();
    const context = document.getElementById("play-context");
    if (!context || typeof ResizeObserver === "undefined") {
        return;
    }

    // Pass 31c (design D15): where the context ends on the page is kept too, so the workspace fills what is left of the window below it,
    // whatever sits above the context (the Studio's bar), and the window does not scroll.
    contextWatcher = new ResizeObserver(() => {
        const box = context.getBoundingClientRect();
        document.documentElement.style.setProperty("--play-context-height", `${box.height}px`);
        document.documentElement.style.setProperty("--play-workspace-top", `${box.bottom + window.scrollY}px`);
    });
    contextWatcher.observe(context);
}

export function unwatchContext() {
    if (contextWatcher) {
        contextWatcher.disconnect();
        contextWatcher = null;
        document.documentElement.style.removeProperty("--play-context-height");
        document.documentElement.style.removeProperty("--play-workspace-top");
    }
}
