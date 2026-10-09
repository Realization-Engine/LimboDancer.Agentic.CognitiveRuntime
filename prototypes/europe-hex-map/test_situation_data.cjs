const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const read = name => fs.readFileSync(path.join(__dirname, name), 'utf8');
const scripts = JSON.parse(read('situation-data/scripts.json'));
const context = vm.createContext({window: {}});
for (const script of scripts) vm.runInContext(read(script), context, {filename: script});
const library = context.window.PANZER_SITUATION_LIBRARY;
const sources = fs.readdirSync(path.join(__dirname, 'sources/situations')).filter(f => /^panzer-leader-\d+\.json$/.test(f)).sort();
assert.equal(library.length, sources.length);
for (const [index, source] of sources.entries()) {
  assert.deepEqual(JSON.parse(JSON.stringify(library[index])), JSON.parse(read('sources/situations/' + source)), source);
}
assert.equal(context.window.PANZER_SITUATION_DATA, library.find(d => d.situation.number === 4));
assert.deepEqual(JSON.parse(JSON.stringify(context.window.CAMPAIGN_REGISTRY)), JSON.parse(read('sources/campaign-registry.json')));
for (const page of ['index.html', 'viewer.html', 'counter-color-test.html', 'terrain-colors.html']) {
  const html = read(page);
  let offset = -1;
  for (const script of scripts) {
    const next = html.indexOf('<script src="' + script + '"></script>');
    assert.ok(next > offset, page + ': missing or unordered ' + script);
    offset = next;
  }
}
console.log('All split packages match canonical sources exactly; all four pages load them in order.');
