// The Minigames page: a link that opens a minigame's card (Play's "The whole card") brings its section to the top. No styles are written here.
// At a narrow width the Studio's menu collapses just after the page becomes interactive and moves the content up, so for a moment after the
// page opens the section is brought back to the top whenever the page's height changes.

export function showSection(id) {
    const show = () => document.getElementById(id)?.scrollIntoView({ block: "start", behavior: "auto" });
    show();
    const observer = new ResizeObserver(show);
    observer.observe(document.body);
    setTimeout(() => observer.disconnect(), 1500);
}
