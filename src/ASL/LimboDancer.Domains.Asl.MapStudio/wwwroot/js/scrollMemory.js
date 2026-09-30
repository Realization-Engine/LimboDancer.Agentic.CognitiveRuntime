// A page's scroll position kept for the browser tab (plan section 12.1), so a list is where the user left it on return from a viewer.
// Only on back or forward is it restored. Only the page's own path is saved, so the scroll to the top of the next page is not recorded as this page's position.

const listeners = new Map();

// The time of the last back or forward navigation; a list is put back where it was only then, not when opened from a link.
let poppedAt = 0;
window.addEventListener("popstate", () => {
    poppedAt = Date.now();
});

function read(key) {
    try {
        return sessionStorage.getItem("scroll:" + key);
    } catch {
        return null;
    }
}

function write(key, value) {
    try {
        sessionStorage.setItem("scroll:" + key, value);
    } catch {
        // Storage may be blocked; the page still works without it.
    }
}

export function restore(key) {
    forget(key);
    const saved = Number(read(key));
    if (saved > 0 && Date.now() - poppedAt < 5000) {
        window.scrollTo(0, saved);
    }

    const path = location.pathname;
    let pending = false;
    const listener = () => {
        if (pending || location.pathname !== path) {
            return;
        }

        pending = true;
        requestAnimationFrame(() => {
            pending = false;
            if (location.pathname === path) {
                write(key, String(Math.round(window.scrollY)));
            }
        });
    };
    window.addEventListener("scroll", listener, { passive: true });
    listeners.set(key, listener);
}

export function forget(key) {
    const listener = listeners.get(key);
    if (listener) {
        window.removeEventListener("scroll", listener);
        listeners.delete(key);
    }
}
