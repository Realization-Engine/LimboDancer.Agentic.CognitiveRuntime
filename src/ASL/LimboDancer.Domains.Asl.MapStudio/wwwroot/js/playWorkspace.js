// The Play workspace (plan sections 13.1 and 14): brings a pane into view and gives it the focus, even when it had the focus already, so the
// context's Review button always shows the review. No styles are written here.

export async function reveal(element) {
    if (!element) {
        return;
    }

    // Pass 29: the render that shows a proposal also grows the context (its Review button and status). Wait two frames, so the context's new
    // height is measured and the pane stops below it, not under it.
    await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));

    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    element.scrollIntoView({ block: "start", behavior: reduce ? "auto" : "smooth" });
    element.focus({ preventScroll: true });
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

    contextWatcher = new ResizeObserver(() => {
        document.documentElement.style.setProperty("--play-context-height", `${context.getBoundingClientRect().height}px`);
    });
    contextWatcher.observe(context);
}

export function unwatchContext() {
    if (contextWatcher) {
        contextWatcher.disconnect();
        contextWatcher = null;
        document.documentElement.style.removeProperty("--play-context-height");
    }
}
