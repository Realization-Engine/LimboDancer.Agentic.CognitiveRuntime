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

// Pass 31c (design D18; play test P-24): a gate request that runs past half a second says "Working" beside the button that started it. The page
// marks its status busy while the gate works; the button last clicked in the workspace is marked while that lasts, and the stylesheet says the
// word. Only an attribute is written here.
let workingWatcher = null;

export function watchWorking() {
    unwatchWorking();
    const status = document.getElementById("play-status");
    if (!status || typeof MutationObserver === "undefined") {
        return;
    }

    const state = { button: null, timer: null, marked: null };
    const clear = () => {
        clearTimeout(state.timer);
        state.timer = null;
        state.marked?.removeAttribute("data-working");
        state.marked = null;
    };
    const onClick = event => {
        const button = event.target instanceof Element ? event.target.closest("button") : null;
        if (button && button.closest("#play-workspace, #play-context")) {
            state.button = button;
        }
    };
    const observer = new MutationObserver(() => {
        if (!status.classList.contains("busy")) {
            clear();
        } else if (!state.timer && !state.marked && state.button?.isConnected) {
            state.timer = setTimeout(() => {
                state.timer = null;
                if (status.classList.contains("busy") && state.button?.isConnected) {
                    state.marked = state.button;
                    state.marked.setAttribute("data-working", "true");
                }
            }, 500);
        }
    });
    observer.observe(status, { attributes: true, attributeFilter: ["class"] });
    document.addEventListener("click", onClick, true);
    workingWatcher = { observer, onClick, clear };
}

export function unwatchWorking() {
    if (workingWatcher) {
        workingWatcher.observer.disconnect();
        document.removeEventListener("click", workingWatcher.onClick, true);
        workingWatcher.clear();
        workingWatcher = null;
    }
}
