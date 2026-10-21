// The Hex UI Context rule (the user, 2026-10-21; Pass Standing Rules, section 2): no choice the Play tab offers names a counter without its hex.
// Run in the Studio's page (the browser tool's javascript_exec) on a game's Play page: paste this function, then
//   JSON.stringify(await hexAudit([...document.querySelector('#play-perspective').options].map(o => o.value)))
// For each view it hands the screen over, opens every section, and lists each select option, unit checkbox, and unit button whose text has no
// "[hex]". A checkbox under a hex heading (.unit-group-hex) counts as having its hex. An option whose value is a Location, a level, an edge, or an
// ammunition is not a counter. The game pickers are not checked. An empty "withoutHex" for every view is the pass.
async function hexAudit(views) {
    const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
    const counter = /\b(squad|leader|half-squad|crew|Dummy|hero|MMG|LMG|HMG|mortar|ATR|DC|tank|PSK|PIAT|bazooka|FT|Panzerfaust|vehicle|gun|charge|SW|commissar)\b/i;
    const notCounter = value => value === ''
        || (value.includes(':') && /^bd|^[a-z]+\d*:[A-Z]/i.test(value) && /:-?\d+$/.test(value))
        || /^-?\d+$/.test(value)
        || ['top', 'bottom', 'left', 'right', 'yes', 'no', 'he', 'ap', 'apcr', 'apds', 'smoke', 'wp', 'heat', 'canister', 'front', 'rear'].includes(value.toLowerCase());
    const out = {};
    for (const view of views) {
        const select = document.querySelector('#play-perspective');
        if (select.value !== view) {
            select.value = view;
            select.dispatchEvent(new Event('change', { bubbles: true }));
            await wait(1500);
        }
        for (let tries = 0; tries < 8; tries++) {
            const handOver = [...document.querySelectorAll('button')].find(button => /^I am the /.test(button.innerText));
            if (!handOver) break;
            handOver.click();
            await wait(1500);
        }
        await wait(1200);
        const panel = document.querySelector('#play-panel-actions');
        if (!panel) {
            out[view] = 'no panel';
            continue;
        }
        panel.querySelectorAll('details').forEach(details => details.open = true);
        await wait(500);
        const bad = [];
        let total = 0;
        panel.querySelectorAll('select:not(#play-game):not(#follow-record-source):not(#play-perspective) option').forEach(option => {
            const text = option.text.trim();
            if (option.value === '' || (notCounter(option.value) && !counter.test(text))) return;
            total++;
            if (!text.includes('[')) bad.push(`option #${option.closest('select').id} [${option.value}]: ${text}`);
        });
        panel.querySelectorAll('label').forEach(label => {
            const box = label.querySelector('input[type=checkbox][data-unit]');
            if (!box) return;
            total++;
            const heading = label.closest('.unit-dropdown-list, [role=group]')?.querySelector('.unit-group-hex');
            if (!label.innerText.includes('[') && !heading) bad.push(`checkbox ${box.className}: ${label.innerText.trim()}`);
        });
        panel.querySelectorAll('button[data-unit]').forEach(button => {
            total++;
            if (!button.innerText.includes('[')) bad.push(`button ${button.className}: ${button.innerText.trim()}`);
        });
        out[view] = { phase: document.querySelector('.play-inspector-turn')?.innerText.replace(/\n/g, ' '), counterChoices: total, withoutHex: bad.slice(0, 25) };
    }
    return out;
}
