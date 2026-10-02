// The Play workspace (plan sections 13.1 and 14): brings a pane into view and gives it the focus, even when it had the focus already, so the
// context's Review button always shows the review. No styles are written here.

export function reveal(element) {
    if (!element) {
        return;
    }

    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    element.scrollIntoView({ block: "start", behavior: reduce ? "auto" : "smooth" });
    element.focus({ preventScroll: true });
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
