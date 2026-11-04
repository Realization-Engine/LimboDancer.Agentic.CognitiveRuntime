"""Read-only whole-rulebook search for the C2.21 ammunition-default design."""
from pathlib import Path
import re
import sys
import pymupdf

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / 'code-builders'))
from paths import RULEBOOK_PDF, WORK

out = Path(WORK) / 'batch3-reading'
out.mkdir(parents=True, exist_ok=True)
patterns = {
    'C2.21 cross-reference': r'\bC2\.21\b',
    'ammunition default': r'assumed to be firing|declaration to the contrary|default ammunition',
    'limited stowage': r'limited stowage|\bAP#|\bHE#',
    'HE equivalency': r'HE Equivalency',
    'caliber limitation': r'overscor|underscor',
}
doc = pymupdf.open(RULEBOOK_PDF)
report = []
for title, pattern in patterns.items():
    hits = []
    for index, page in enumerate(doc):
        current = 'unidentified'
        for line in page.get_text().splitlines():
            if re.match(r'^\d{1,2}\.\d{1,4}\b', line):
                current = line.split()[0]
            if re.search(pattern, line, re.I):
                hits.append(f'p{index + 1}, nearest head {current}: {line}')
    report += [f'## {title} ({len(hits)} matches)', *hits, '']
    print(f'{title}: {len(hits)} matches across {len(doc)} pages')
(out / 'whole-book-scan.txt').write_text('\n'.join(report), encoding='utf-8', newline='\n')
for number in (167, 169, 170, 178, 179, 182):
    (out / f'p{number}.txt').write_text(doc[number - 1].get_text(), encoding='utf-8', newline='\n')
