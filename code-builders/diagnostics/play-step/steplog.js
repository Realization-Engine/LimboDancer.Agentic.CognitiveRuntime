// The step log of a Play follower run (the analysis's section 2.1): evaluate this in the Play page once it follows a record, then press
// "Run, a step a second" (#follow-record-run). Each change of "revision N of M" (#follow-record-progress) is noted as
// [performance.now(), Date.now(), N]; the run is stopped at the revision given. Read window.__playStep.log afterwards and save it as the
// JSON list split.py and trace_split.py read: a step's work is its interval less the follow loop's one-second pause.
(() => {
  const stopAt = Number(window.__playStepStopAt ?? 1e9);
  const panel = document.querySelector('#follow-record');
  if (!panel || !panel.querySelector('#follow-record-progress')) { return 'no #follow-record-progress: start following first'; }
  const revisionOf = () => Number(((panel.querySelector('#follow-record-progress')?.textContent ?? '').match(/revision (\d+) of/) || [])[1]);
  if (window.__playStep?.observer) { window.__playStep.observer.disconnect(); }
  const state = { log: [[performance.now(), Date.now(), revisionOf()]], stopped: false };
  state.observer = new MutationObserver(() => {
    const revision = revisionOf();
    const last = state.log[state.log.length - 1];
    if (revision !== last[2]) { state.log.push([performance.now(), Date.now(), revision]); }
    if (revision >= stopAt && !state.stopped) {
      state.stopped = true;
      const run = document.querySelector('#follow-record-run');
      if (run && run.textContent.trim() === 'Stop') { run.click(); }
    }
  });
  state.observer.observe(panel, { childList: true, characterData: true, subtree: true });
  window.__playStep = state;
  return `logging from revision ${revisionOf()}, stop at ${stopAt}`;
})();
