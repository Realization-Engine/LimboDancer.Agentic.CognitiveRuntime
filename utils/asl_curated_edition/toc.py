"""
toc.py - rebuild the ASL Rulebook Table of Contents (PDF pages 6-10) from the PDF.

The printed TOC sets each chapter's numbered entries in two side-by-side
columns under a centred heading. The registered conversion read those pages
as two page-high columns, so chapter halves were interleaved and headings
detached from their entries. This module reads span positions instead: each
entry row is assigned to the nearest heading above it, then the left column
is read before the right column.
"""
import collections
import re

HEAD_SIZE = 14.0       # chapter headings are set at 16 pt, entries at 8 pt
RUNNING_HEAD_Y = 40.0  # 'ADVANCED SQUAD LEADER RULEBOOK TABLE OF CONTENTS'
FOLIO_Y = 750.0        # roman page number
ROW_TOL = 2.5
GAP = 1.0             # pt; spans closer than this are one word
NUM_RE = re.compile(r'^(\d+)\.$')
BULLET = '•'

# Chapter banner icons in the registered image set, identified by visual
# inspection (the converter did not number them in page order).
ICONS = {
    'A': 'eASLRB_v3_01-p6-1.png', 'B': 'eASLRB_v3_01-p6-2.png', 'C': 'eASLRB_v3_01-p6-3.png',
    'D': 'eASLRB_v3_01-p7-1.png', 'E': 'eASLRB_v3_01-p7-2.png', 'F': 'eASLRB_v3_01-p7-4.png',
    'G': 'eASLRB_v3_01-p7-3.png', 'H': 'eASLRB_v3_01-p8-1.png', 'I': 'eASLRB_v3_01-p8-6.png',
    'J': 'eASLRB_v3_01-p8-2.png', 'K': 'eASLRB_v3_01-p8-3.png', 'L': 'eASLRB_v3_01-p8-4.png',
    'M': 'eASLRB_v3_01-p8-5.png', 'N': 'eASLRB_v3_01-p9-1.png', 'O': 'eASLRB_v3_01-p9-2.png',
    'P': 'eASLRB_v3_01-p9-3.png', 'Q': 'eASLRB_v3_01-p9-4.png', 'R': 'eASLRB_v3_01-p9-5.png',
    'S': 'eASLRB_v3_01-p10-1.png', 'T': 'eASLRB_v3_01-p10-2.png', 'W': 'eASLRB_v3_01-p10-3.png',
    'Z': 'eASLRB_v3_01-p10-4.png',
}


def _spans(page):
    out = []
    for b in page.get_text('dict')['blocks']:
        if b['type'] != 0:
            continue
        for line in b['lines']:
            for s in line['spans']:
                t = s['text'].replace('\n', ' ')
                if not t.strip():
                    continue
                y0, y1 = s['bbox'][1], s['bbox'][3]
                if y1 <= RUNNING_HEAD_Y or y0 >= FOLIO_Y:
                    continue
                out.append({'x0': s['bbox'][0], 'x1': s['bbox'][2], 'y0': y0, 'y1': y1,
                            'size': s['size'], 'text': t})
    return out


def _rows(spans):
    """Group spans whose tops align into rows, left to right."""
    rows = []
    for s in sorted(spans, key=lambda s: (s['y0'], s['x0'])):
        if rows and abs(rows[-1][0]['y0'] - s['y0']) <= ROW_TOL:
            rows[-1].append(s)
        else:
            rows.append([s])
    return [sorted(r, key=lambda s: s['x0']) for r in rows]


def _join(spans):
    """Concatenate spans, adding a space where the PDF leaves a visible gap."""
    out, prev = '', None
    for s in spans:
        if prev is not None and s['x0'] - prev['x1'] > GAP and out[-1:] != ' ' and s['text'][:1] != ' ':
            out += ' '
        out += s['text']
        prev = s
    return re.sub(r'\s+', ' ', out).strip()


def extract(doc, first=6, last=10):
    """Return chapters in printed order and anything that could not be placed."""
    chapters, stray = [], []
    for p in range(first, last + 1):
        page = doc[p - 1]
        mid = page.rect.width / 2
        page_chapters = []
        body = []
        for row in _rows(_spans(page)):
            heads = [s for s in row if s['size'] >= HEAD_SIZE]
            if heads:
                page_chapters.append({'heading': _join(heads), 'page': p, 'y': heads[0]['y0'],
                                      'cols': {'L': [], 'R': []}, 'para': []})
                row = [s for s in row if s['size'] < HEAD_SIZE]
            body.extend(row)
        for s in body:
            owner = None
            for ch in page_chapters:
                if ch['y'] < s['y0']:
                    owner = ch
            if owner is None:
                stray.append({'page': p, 'text': s['text']})
                continue
            owner['cols']['L' if s['x0'] < mid else 'R'].append(s)
        for ch in page_chapters:
            entries = []
            for col in ('L', 'R'):
                for row in _rows(ch['cols'][col]):
                    head = row[0]['text'].strip()
                    m = NUM_RE.match(head)
                    if m:
                        entries.append({'n': int(m.group(1)), 'text': _join(row[1:]), 'sub': []})
                    elif head == BULLET and entries:
                        entries[-1]['sub'].append(_join(row[1:]))
                    else:
                        ch['para'].append(_join(row))
            ch['entries'] = entries
            del ch['cols']
        chapters.extend(page_chapters)
    return chapters, stray


def render(chapters, image_prefix, title):
    out = [f'**{title}**', '']
    page = None
    for ch in chapters:
        if ch['page'] != page:
            page = ch['page']
            out += [f'<!-- pdf-page {page} -->', '']
        letter = ch['heading'].split('.')[0] if re.match(r'^[A-Z]\.', ch['heading']) else None
        if letter in ICONS:
            out += [f'![Figure from page {page}]({image_prefix}{ICONS[letter]})', '']
        out += [f'## {ch["heading"]}', '']
        if ch['para']:
            out += [' '.join(ch['para']), '']
        if ch['entries']:
            for e in ch['entries']:
                out.append(f'{e["n"]}. {e["text"]}')
                out += [f'   - {s}' for s in e['sub']]
            out.append('')
    return '\n'.join(out)


def words(text):
    return collections.Counter(re.findall(r'[A-Za-z0-9]+', text))


def check(chapters, stray):
    """Structural problems that must go to review rather than be guessed."""
    problems = [f'text on page {s["page"]} above the first heading: {s["text"]!r}' for s in stray]
    for ch in chapters:
        nums = [e['n'] for e in ch['entries']]
        if nums and nums != list(range(1, len(nums) + 1)):
            problems.append(f'{ch["heading"]}: entry numbers {nums} are not 1..{len(nums)}')
    return problems
