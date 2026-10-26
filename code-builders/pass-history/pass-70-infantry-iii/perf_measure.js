// Pass 70's Studio measurement (design, section 7), run in the browser pane on /games/play?game=p70-perf-tw at 1920x1080.
// Paste the body into the pane's script runner; it times what the page does after each action to its last DOM change (quiet window).
// Same method on the base Studio (main-bin on 6671) and the pass's (studio-6670 on 6670), one Studio at a time.
window.quiet = (ms) => new Promise(res => {
  const t0 = performance.now(); let last = t0;
  const mo = new MutationObserver(() => { last = performance.now(); });
  mo.observe(document.body, { subtree: true, childList: true, characterData: true, attributes: true });
  const iv = setInterval(() => { if (performance.now() - last > ms) { clearInterval(iv); mo.disconnect(); res(last - t0); } }, 25);
});
// 1. The hand-over, four times alternating sides: the time from the confirm's click to the last change.
const handover = [];
let cur = document.querySelector('#play-perspective').value;
for (let i = 0; i < 4; i++) {
  const side = cur === 'german' ? 'russian' : 'german';
  const p = document.querySelector('#play-perspective'); p.value = side; p.dispatchEvent(new Event('change', { bubbles: true }));
  let b = null; for (let k = 0; k < 40 && !(b = document.querySelector('#play-handover-confirm')); k++) await new Promise(r => setTimeout(r, 100));
  if (!b) { handover.push(side + ': no confirm'); continue; }
  await quiet(500); b.click(); handover.push(side + ':' + Math.round(await quiet(800))); cur = side;
}
// 2. "End the phase" from the Russian view (refused at revision 1316: a window is open), five times, to the last change.
// (Run with the Russian view handed over.)
const propose = [];
for (let i = 0; i < 5; i++) { const a = document.querySelector('#propose-advance'); if (!a) break; a.click(); propose.push(Math.round(await quiet(800))); }
// 3. The open of the whole dl-tractor-works-02, the first in the Studio process: the time to its hand-over confirm.
JSON.stringify({ handover, propose });
