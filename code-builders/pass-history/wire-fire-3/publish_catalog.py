"""Prepare additive D3.7 source facts in this script's checkout, without altering legacy values."""
import argparse
import csv
import hashlib
import io
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SHEETS = ROOT / 'docs/ASL/SourceRegistry/CounterSheets'
TRANSCRIPTION = SHEETS / 'scenario-a1.counter-transcription.csv'
WORKSHEET = SHEETS / 'scenario-a1.counter-worksheet.csv'
SOURCE = SHEETS / 'asl-counter-sheets.scenario-a1.source-record.json'
VOCABULARY = ROOT / 'src/ASL/units/vocabulary/asl.vocab.json'
MANIFEST = ROOT / 'src/ASL/units/catalog/scenario-a1.catalog-manifest.json'
FIELDS = ['sheet', 'counter', 'face', 'attribute', 'value', 'transcriber', 'reviewer', 'note']
FACTS = {'printed-breakdown': 'not-printed', 'printed-elimination': 'not-printed',
         'asl:breakdown-circled': 'no', 'asl:breakdown-note-extends': 'no'}
LISTINGS = {
    'attacker-truck': ('VLG', '339; German 96 (349), N (350)'),
    'defender-truck': ('VLR', '356; Russian 47 (360)'),
    'attacker-halftrack': ('VLG', '339; German 63 (345), C (349), N (350)'),
    'attacker-tank': ('VLG', '337; German 13 (340), N (350)'),
    'defender-tank': ('VLR', '355; Russian 14 (357), C and M (361-362)'),
}


def write(path, text):
    path.write_text(text, encoding='utf-8', newline='\n')


def replace(path, old, new):
    text = path.read_text(encoding='utf-8')
    if old not in text:
        raise ValueError(f'{path}: expected value absent: {old}')
    write(path, text.replace(old, new))


def append_rows(path, rows):
    buffer = io.StringIO(newline='')
    csv.DictWriter(buffer, fieldnames=FIELDS, lineterminator='\n').writerows(rows)
    write(path, path.read_text(encoding='utf-8').rstrip('\n') + '\n' + buffer.getvalue())


def prepare():
    if 'asl:printed-breakdown' in VOCABULARY.read_text(encoding='utf-8'):
        raise ValueError('Preparation already applied; duplicate source rows are forbidden.')
    rows = list(csv.DictReader(io.StringIO(TRANSCRIPTION.read_text(encoding='utf-8'))))
    vehicles = [row['counter'] for row in rows if row['attribute'] == 'kind' and row['value'] == 'asl:vehicle']
    if len(vehicles) != 17 or not set(LISTINGS).issubset(vehicles):
        raise ValueError('Vehicle source inventory changed; review the new profiles first.')
    replace(VOCABULARY, '    { "name": "asl:rate-of-fire",',
            '    { "name": "asl:printed-breakdown", "label": "Printed vehicle B#", "type": "integer", "scope": "face", "rule": "D3.7, p. 201" },\n'
            '    { "name": "asl:printed-elimination", "label": "Printed vehicle X#", "type": "integer", "scope": "face", "rule": "D3.7, p. 201" },\n'
            '    { "name": "asl:rate-of-fire",')
    replace(VOCABULARY, '    { "name": "asl:breakdown-removes",',
            '    { "name": "asl:breakdown-circled", "label": "Circled vehicle B#", "rule": "D3.71, p. 202" },\n'
            '    { "name": "asl:breakdown-note-extends", "label": "Vehicle Note extends printed B# to other armament", "rule": "D3.7, p. 201" },\n'
            '    { "name": "asl:breakdown-removes",')
    replace(VOCABULARY, '"asl:sa-mount", "asl:sa-caliber", "asl:towing"',
            '"asl:sa-mount", "asl:sa-caliber", "asl:printed-breakdown", "asl:printed-elimination", "asl:towing"')
    replace(VOCABULARY, '"asl:no-ap", "asl:no-he", "asl:fixed-bmg"',
            '"asl:breakdown-circled", "asl:breakdown-note-extends", "asl:no-ap", "asl:no-he", "asl:fixed-bmg"')
    additions, questions = [], []
    for counter in vehicles:
        if counter in LISTINGS:
            sheet, pages = LISTINGS[counter]
            note = ('D3.7 source review: blank B/X entry, no circled B, and no applicable Vehicle Note '
                    f'extension; physical PDF {pages}. Legacy breakdown is unchanged.')
        else:
            sheet = 'MFG'
            note = ('Manufactured Goodwood setup profile under R0.3: explicitly no printed B/X, no circled B '
                    'and no note extension. This defines the manufactured profile, not a historical counter transcription. '
                    'Legacy breakdown is unchanged.')
        for attribute, value in FACTS.items():
            additions.append(dict(sheet=sheet, counter=counter, face='front', attribute=attribute, value=value,
                                  transcriber='Codex', reviewer='Codex independent source review', note=note))
            questions.append(dict(sheet='', counter=counter, face='front', attribute=attribute, value='',
                                  transcriber='', reviewer='', note='D3.7: printed evidence only; unknown stays not-in-source'))
    append_rows(TRANSCRIPTION, additions)
    append_rows(WORKSHEET, questions)
    record = json.loads(SOURCE.read_text(encoding='utf-8'))
    replace(SOURCE, record['transcription']['sha256'], hashlib.sha256(TRANSCRIPTION.read_bytes()).hexdigest())
    replace(SOURCE, '"transcribedOn": "2026-10-23"', '"transcribedOn": "2026-10-30"')
    replace(MANIFEST, '"version": "1.18.0"', '"version": "1.19.0"')
    print('Prepared 68 additive facts; legacy values and definition identities are unchanged.')


def record_review():
    text = TRANSCRIPTION.read_text(encoding='utf-8')
    lines = text.splitlines()
    for index, line in enumerate(lines):
        if line.startswith('MFG,') and 'Codex independent source review' in line:
            lines[index] = line.replace('Codex independent source review', 'Codex independent manufactured-profile review')
    write(TRANSCRIPTION, '\n'.join(lines) + '\n')
    record = json.loads(SOURCE.read_text(encoding='utf-8'))
    record['transcription']['sha256'] = hashlib.sha256(TRANSCRIPTION.read_bytes()).hexdigest()
    reviewers = record['review']['additionalReviewers']
    for reviewer in ('Codex independent source review', 'Codex independent manufactured-profile review'):
        if reviewer not in reviewers:
            reviewers.append(reviewer)
    evidence = (' On 2026-10-30, wire-fire-3 added four separate D3.7 facts for each vehicle, leaving legacy '
                'breakdown values unchanged. Codex and an independent read-only Codex referee visually checked the five '
                'listed vehicles on physical PDF pp. 337, 339, 355 and 356, and read their complete numbered/letter '
                'Vehicle Notes on pp. 340, 345, 349-350, 357 and 360-362. All five have blank B/X entries, no circled B '
                'and no applicable extension to other armament. The twelve MFG profiles explicitly declare no printed '
                'B/X, circle or note extension under the existing R0.3 delegation; the independent review checks those '
                'manufactured definitions, not historical accuracy. Dennis Landi did not review these new rows value by value.')
    if evidence not in record['review']['method']:
        record['review']['method'] += evidence
    write(SOURCE, json.dumps(record, indent=2, ensure_ascii=False) + '\n')
    print('Registered independent source and manufactured-profile reviews.')


def publish(generated, rules_root):
    registry = ROOT / 'docs/ASL/SourceRegistry'
    catalog = ROOT / 'src/ASL/units/catalog/scenario-a1.catalog.json'
    packages = {'fire': 'Fire', 'rally': 'Rally', 'close-combat': 'CloseCombat', 'ordnance': 'Ordnance'}
    digest = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
    old_catalog = digest(catalog)
    matrices = {name: registry / f'asl-scenario-a1.{name}-case-matrix.json' for name in packages}
    manifests = {name: registry / f'asl-scenario-a1.{name}-package.json' for name in packages}
    old_matrices = {name: digest(path) for name, path in matrices.items()}
    old_manifests = {name: digest(path) for name, path in manifests.items()}
    candidate = generated.read_text(encoding='utf-8')
    parsed = json.loads(candidate)
    if parsed.get('version') != '1.19.0' or parsed['sources'][0]['recordSha256'] != digest(SOURCE):
        raise ValueError('Expected a canonical 1.19.0 candidate generated from the current source record.')
    write(catalog, candidate)
    for path in [*matrices.values(), *manifests.values()]:
        replace(path, old_catalog, digest(catalog))
    for name, suffix in packages.items():
        replace(manifests[name], old_matrices[name], digest(matrices[name]))
        declaration = rules_root / f'src/ASL/LimboDancer.Domains.Asl.Rules/ScenarioA1{suffix}Package.cs'
        replace(declaration, old_matrices[name], digest(matrices[name]))
        replace(declaration, old_manifests[name], digest(manifests[name]))
    replace(ROOT / 'src/ASL/LimboDancer.Domains.Asl.Play/LiveFire.cs',
            'public const string CatalogVersion = "1.18.0";', 'public const string CatalogVersion = "1.19.0";')
    replace(ROOT / 'code-builders/test-games/game_file.py', 'asl-scenario-a1@1.18.0', 'asl-scenario-a1@1.19.0')
    print('Published catalog and package pins. Integrate the Rules declaration commit before verification.')


def repin_tests(baseline):
    replacements = {}
    for package in ('fire', 'rally', 'close-combat', 'ordnance'):
        for kind in ('case-matrix', 'package'):
            path = f'docs/ASL/SourceRegistry/asl-scenario-a1.{package}-{kind}.json'
            before = subprocess.run(['git', 'show', f'{baseline}:{path}'], cwd=ROOT,
                                    check=True, capture_output=True).stdout
            replacements[hashlib.sha256(before).hexdigest()] = hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
    for path in (ROOT / 'src/ASL/tests').rglob('*.cs'):
        if 'obj' in path.parts or 'bin' in path.parts:
            continue
        text = path.read_text(encoding='utf-8')
        updated = text
        for old, new in replacements.items():
            updated = updated.replace(old, new)
        if updated != text:
            write(path, updated)
            print(path.relative_to(ROOT).as_posix())


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('stage', choices=('prepare', 'review', 'publish', 'tests'))
    parser.add_argument('--catalog', type=Path)
    parser.add_argument('--rules-root', type=Path)
    parser.add_argument('--baseline', default='origin/main')
    args = parser.parse_args()
    if args.stage == 'prepare':
        prepare()
    elif args.stage == 'review':
        record_review()
    elif args.stage == 'tests':
        repin_tests(args.baseline)
    elif args.catalog is None or args.rules_root is None:
        parser.error('publish requires --catalog and --rules-root')
    else:
        publish(args.catalog.resolve(), args.rules_root.resolve())
