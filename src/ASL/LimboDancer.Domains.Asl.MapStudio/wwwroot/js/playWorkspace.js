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
