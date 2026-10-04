// The Replay page (pass 31b, design D5): the keys that move through the steps while the map or the timeline has the focus, and the step shown brought
// into view in the timeline. No styles are written here.

const stepKeys = new Set(["ArrowLeft", "ArrowRight", "Home", "End", "PageUp", "PageDown"]);
let keyWatcher = null;

// Left and Right step, Home and End go to the ends, Page Up and Page Down go a phase. Only inside an element marked data-replay-keys, and never in
// a field, so typing and the inspector's own tabs keep their keys.
export function watchKeys(dotnet) {
    unwatchKeys();
    keyWatcher = event => {
        if (event.ctrlKey || event.metaKey || event.altKey || event.shiftKey || !stepKeys.has(event.key)) {
            return;
        }

        const target = event.target;
        if (!(target instanceof Element) || !target.closest("[data-replay-keys]") || target.closest("input, select, textarea, [role=tab]")) {
            return;
        }

        event.preventDefault();
        dotnet.invokeMethodAsync("OnReplayKey", event.key);
    };
    document.addEventListener("keydown", keyWatcher);
}

export function unwatchKeys() {
    if (keyWatcher) {
        document.removeEventListener("keydown", keyWatcher);
        keyWatcher = null;
    }
}

// Brings the step shown into view inside the timeline's own pane. When the focus was on a step of the timeline, it follows to the step shown.
export function revealStep(id) {
    const element = document.getElementById(id);
    if (!element || element.closest("[hidden]")) {
        return;
    }

    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const pane = element.closest(".play-pane");
    if (pane && pane.scrollHeight > pane.clientHeight + 1) {
        const box = element.getBoundingClientRect();
        const frame = pane.getBoundingClientRect();
        if (box.top < frame.top || box.bottom > frame.bottom) {
            pane.scrollTo({ top: pane.scrollTop + box.top - frame.top - frame.height / 3, behavior: reduce ? "auto" : "smooth" });
        }
    } else {
        // Under 1024px the pane is as tall as its steps and the page scrolls.
        const box = element.getBoundingClientRect();
        if (box.top < 0 || box.bottom > window.innerHeight) {
            element.scrollIntoView({ block: "center", behavior: reduce ? "auto" : "smooth" });
        }
    }

    if (document.activeElement?.classList.contains("replay-step")) {
        element.focus({ preventScroll: true });
    }
}
