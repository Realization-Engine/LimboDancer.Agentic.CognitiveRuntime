// Pass 70's measurement of a game's open (design, section 7): run on /games/play with no game chosen, in a Studio process just started.
// It opens the game by the page's own select and times the wait for its hand-over confirm, then opens it a second time (warm).
const game = 'dl-tractor-works-02';
async function open(id) {
  const blank = document.querySelector('#play-game'); blank.value = ''; blank.dispatchEvent(new Event('change', { bubbles: true }));
  await new Promise(r => setTimeout(r, 2500));
  const s = document.querySelector('#play-game'); const t0 = performance.now();
  s.value = id; s.dispatchEvent(new Event('change', { bubbles: true }));
  for (let k = 0; k < 2400; k++) {
    if (document.location.search.includes(id) && document.querySelector('#play-handover-confirm')) break;
    await new Promise(r => setTimeout(r, 10));
  }
  return Math.round(performance.now() - t0);
}
const first = await open(game);
const second = await open(game);
JSON.stringify({ first, second });
