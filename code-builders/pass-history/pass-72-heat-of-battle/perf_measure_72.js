// Pass 72's Studio measurement (design, section 7), run in the browser pane on /games/play?game=p70-perf-tw at 1920x1080.
// Pass 70's method (perf_measure.js), brought to today's page: the hand-over is the #play-hand-over button, then its confirm.
// Each time is from the click to the page's last DOM change (a quiet window). Same method on the base Studio (main-bin on 6671) and the
// pass's (studio-6670 on 6670), one Studio at a time, each in a fresh process.
window.quiet = (ms) => new Promise(res => {
  const t0 = performance.now(); let last = t0;
  const mo = new MutationObserver(() => { last = performance.now(); });
  mo.observe(document.body, { subtree: true, childList: true, characterData: true, attributes: true });
  const iv = setInterval(() => { if (performance.now() - last > ms) { clearInterval(iv); mo.disconnect(); res(last - t0); } }, 25);
});
const wait = async (sel) => { let e = null; for (let k = 0; k < 60 && !(e = document.querySelector(sel)); k++) await new Promise(r => setTimeout(r, 100)); return e; };
// 1. The hand-over, ten times from the Russian view (so it ends in the Russian): the confirm's click to the last change.
const handover = [];
for (let i = 0; i < 10; i++) {
  // The waiting side's view has a hand-over button; the phasing side's has only the perspective select.
  const h = document.querySelector('#play-hand-over');
  if (h) { h.click(); } else {
    const p = document.querySelector('#play-perspective'); p.value = p.value === 'german' ? 'russian' : 'german';
    p.dispatchEvent(new Event('change', { bubbles: true }));
  }
  const b = await wait('#play-handover-confirm'); if (!b) { handover.push('no confirm'); break; }
  await quiet(500); b.click(); handover.push(Math.round(await quiet(800)));
}
// 2. In the Russian view (the phasing side at revision 1316, the MPh with r-squad-10 berserk): "end the phase", five times.
const propose = [];
if (document.querySelector('#play-perspective').value === 'russian') {
  for (let i = 0; i < 5; i++) { const a = document.querySelector('#propose-advance'); if (!a) { propose.push('none'); break; } a.click(); propose.push(Math.round(await quiet(800))); }
}
JSON.stringify({ handover, propose, view: document.querySelector('#play-perspective').value });
