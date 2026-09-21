#!/usr/bin/env python3
"""
pdf_to_markdown.py - structure-aware PDF -> Markdown converter (PyMuPDF).

Built for the Nokia 1830 GX CLI Reference Guide, but the approach is general:
  * headings come from the PDF bookmark outline (falls back to font size)
  * ruled tables -> GFM tables, merged across page breaks ("(continued)")
  * monospaced (Courier) lines -> fenced code blocks, spacing preserved
  * inline bold / italic / `code` from span fonts
  * running heads, footers, and a watermark are stripped
  * figures on the listed pages are rasterized to images/*.png

Usage:
  pip install pymupdf
  python pdf_to_markdown.py input.pdf -o out_dir
  python pdf_to_markdown.py input.pdf -o out_dir --first 96 --last 100   # sample

Document-specific settings (header/footer bands, footer text, watermark,
figure pages, front-matter pages) are grouped under CONFIG below and can be
overridden from the command line.
"""
import argparse
import os
import re
import sys

import pymupdf

# ----------------------------------------------------------------- CONFIG ---
CONFIG = {
    # text whose bottom edge is at or above this y (pt) is a running head
    'top_lim': 78.0,
    # text starting below this fraction of page height may be a footer
    'foot_frac': 0.89,
    # footer lines (only dropped when they also sit in the footer band)
    'footer_re': r'^(©\s*20\d\d Nokia|Use subject to agreed|1900-003486|Release 9\.1|July 2026|Issue\b|\d{1,4}\s*$)',
    # exact watermark text to drop
    'watermark': '2026-07-24 Draft',
    # pages holding real figures (raster images and/or vector diagrams)
    'figure_pages': [45, 46, 48, 50, 64, 65, 1184],
    # printed Contents pages, replaced by a TOC generated from the outline
    'contents_pages': (3, 14),
    # List of Figures / List of Tables pages (dotted leaders -> bullet list)
    'list_pages': (15, 35),
    # render resolution for figures
    'figure_dpi': 170,
}
# ------------------------------------------------------------------------------

CAPTION_RE = re.compile(r'^(Table|Figure)\s+\d+[:.]')


# ---------------------------------------------------------------- helpers ---
def norm(s):
    return re.sub(r'\s+', ' ', s).strip()


def slugify(text, seen):
    """GitHub-style heading anchor, de-duplicated."""
    s = text.lower()
    s = re.sub(r'[^a-z0-9 \-]', '', s)
    s = re.sub(r'\s+', '-', s.strip())
    base = s or 'section'
    n = seen.get(base, 0)
    seen[base] = n + 1
    return base if n == 0 else f'{base}-{n}'


def esc(t):
    """Escape markdown-significant characters in plain prose."""
    return re.sub(r'([*_`<>|])', r'\\\1', t)


def join_wrapped(a, b):
    """Join a wrapped line onto the previous one, healing split hyphenated words."""
    a = a.rstrip()
    b = b.lstrip()
    if re.search(r'[A-Za-z0-9]-$', a) and re.match(r'[a-z0-9]', b):
        return a + b
    return a + ' ' + b


def inline(spans):
    """Build markdown with inline bold/italic/code from a list of spans."""
    parts = []
    for s in spans:
        t = s['text']
        if not t:
            continue
        f = s['font']
        code = 'Courier' in f
        bold = ('Bold' in f or 'Black' in f) and not code
        ital = 'Italic' in f and not code
        parts.append({'t': t, 'code': code, 'bold': bold, 'ital': ital})
    merged = []
    for p in parts:
        if merged and (merged[-1]['code'], merged[-1]['bold'], merged[-1]['ital']) == \
                (p['code'], p['bold'], p['ital']):
            merged[-1]['t'] += p['t']
        else:
            merged.append(dict(p))
    out = []
    for p in merged:
        t = p['t']
        if not t.strip():
            out.append(t)
            continue
        lead = t[:len(t) - len(t.lstrip())]
        trail = t[len(t.rstrip()):]
        core = t.strip()
        if p['code']:
            fence = '`'
            while fence in core:
                fence += '`'
            core = f'{fence}{core}{fence}'
        else:
            core = esc(core)
            if p['bold']:
                core = f'**{core}**'
            if p['ital']:
                core = f'*{core}*'
        out.append(lead + core + trail)
    return ''.join(out).rstrip()


def cell_text(c):
    """Flatten a table cell into one markdown-safe line."""
    if not c:
        return ''
    t = re.sub(r'\s*\n\s*', ' ', c).strip()
    t = t.replace('|', '\\|').replace('<', '&lt;').replace('>', '&gt;')
    t = re.sub(r'\s+', ' ', t)
    t = re.sub(r'(?<=[A-Za-z0-9])-\s+(?=[a-z0-9])', '-', t)
    t = re.sub(r'\s*•\s*', '<br>• ', t)          # bullets inside cells
    if t.startswith('<br>'):
        t = t[4:]
    return t


def table_md(rows):
    ncol = max(len(r) for r in rows)
    rows = [list(r) + [''] * (ncol - len(r)) for r in rows]
    head = rows[0]
    if not any(h.strip() for h in head):
        head = [f'Column {i + 1}' for i in range(ncol)]
        body = rows
    else:
        body = rows[1:]
    lines = ['| ' + ' | '.join(head) + ' |',
             '|' + '|'.join([' --- '] * ncol) + '|']
    for r in body:
        lines.append('| ' + ' | '.join(r) + ' |')
    return '\n'.join(lines)


def row_md(cells, ncol):
    cells = (list(cells) + [''] * ncol)[:ncol]
    return '| ' + ' | '.join(cells) + ' |'


def cluster_rects(rects, pad=6):
    """Merge overlapping/nearby rectangles into clusters."""
    boxes = [pymupdf.Rect(r) for r in rects]
    changed = True
    while changed:
        changed = False
        out = []
        for b in boxes:
            placed = False
            for i, o in enumerate(out):
                if pymupdf.Rect(b.x0 - pad, b.y0 - pad, b.x1 + pad, b.y1 + pad).intersects(o):
                    out[i] = o | b
                    placed = True
                    changed = True
                    break
            if not placed:
                out.append(pymupdf.Rect(b))
        boxes = out
    return boxes


def pop_gap(out, extra=None):
    """Pop trailing blank lines / page markers (and one optional exact line)."""
    while out:
        t = out[-1].strip()
        if t == '' or t.startswith('<!-- page '):
            out.pop()
        elif extra is not None and out[-1] == extra:
            out.pop()
            extra = None
        else:
            break


# -------------------------------------------------------------- converter ---
class Converter:
    def __init__(self, src, outdir, cfg):
        self.doc = pymupdf.open(src)
        self.cfg = cfg
        self.outdir = outdir
        self.imgdir = os.path.join(outdir, 'images')
        os.makedirs(self.imgdir, exist_ok=True)
        self.footer_re = re.compile(cfg['footer_re'])
        self.figure_pages = set(cfg['figure_pages'])
        self.toc = self.doc.get_toc()
        self.toc_by_page = {}
        for lvl, title, pno in self.toc:
            self.toc_by_page.setdefault(pno, []).append([lvl, norm(title), False])

    # ---------------------------------------------------------- one page ---
    def process_page(self, pno):
        """Return a list of blocks (heading/para/bullet/code/table/caption/figure)."""
        page = self.doc[pno - 1]
        W, H = page.rect.width, page.rect.height
        top_lim = self.cfg['top_lim']
        foot_lim = H * self.cfg['foot_frac']

        # --- figures: images + vector drawings, only on known figure pages ---
        fig_rects = []
        if pno in self.figure_pages:
            rects = [i['bbox'] for i in page.get_image_info()]
            for dr in page.get_drawings():
                r = pymupdf.Rect(dr['rect'])
                if r.y1 <= top_lim or r.y0 >= foot_lim:
                    continue
                if r.width > W * 0.85 and r.height < 3:      # header/footer rules
                    continue
                if r.width < 2 and r.height < 2:
                    continue
                rects.append(tuple(r))
            if rects:
                fig_rects = cluster_rects(rects, pad=14)
                fig_rects = [r for r in fig_rects if r.width > 60 and r.height > 40]
                fig_rects = [pymupdf.Rect(r.x0, r.y0, r.x1, min(r.y1, foot_lim)) for r in fig_rects]

        # --- tables ---
        tables = []
        try:
            for t in page.find_tables().tables:
                b = t.bbox
                if b[1] < top_lim or b[1] > foot_lim:
                    continue
                if t.col_count < 2 or t.row_count < 2:
                    continue
                rows = [[cell_text(c) for c in r] for r in t.extract()]
                rows = [r for r in rows if any(c.strip() for c in r)]
                if len(rows) < 2:
                    continue
                tables.append((pymupdf.Rect(b), rows))
        except Exception:
            pass
        table_rects = [t[0] for t in tables]

        # --- keep figure regions clear of captions, tables, and body text below ---
        if fig_rects:
            stops = [r.y0 for r in table_rects]
            for b in page.get_text('dict')['blocks']:
                if b['type'] != 0:
                    continue
                for l in b['lines']:
                    t = norm(''.join(s['text'] for s in l['spans']))
                    if not t:
                        continue
                    x0, y0, x1, y1 = l['bbox']
                    if CAPTION_RE.match(t):
                        stops.append(y0)
                        continue
                    sp = l['spans'][0]
                    if x0 <= 66 and (x1 - x0) > 200 and sp['size'] >= 9.5 \
                            and 'Bold' not in sp['font'] and 'Black' not in sp['font']:
                        stops.append(y0)
            newf = []
            for r in fig_rects:
                below = [y for y in stops if y > r.y0 + 25]
                y1 = min(r.y1, min(below) - 4) if below else r.y1
                if y1 - r.y0 > 40:
                    newf.append(pymupdf.Rect(r.x0, r.y0, r.x1, y1))
            fig_rects = newf

        # --- text lines ---
        items = []
        for b in page.get_text('dict')['blocks']:
            if b['type'] != 0:
                continue
            for l in b['lines']:
                spans = list(l['spans'])
                if not spans:
                    continue
                text = ''.join(s['text'] for s in spans)
                if not text.strip():
                    continue
                x0, y0, x1, y1 = l['bbox']
                if x0 < 32 and abs(l['dir'][1]) > 0.5:          # vertical watermark
                    continue
                if norm(text) == self.cfg['watermark']:
                    continue
                if len(norm(text)) <= 1 and 'TimesNewRoman' in spans[0]['font']:
                    continue                                    # Note/Tip "i" icon
                if y1 <= top_lim:
                    continue
                if y0 > foot_lim and self.footer_re.match(norm(text)):
                    continue
                pt = pymupdf.Point((x0 + x1) / 2, (y0 + y1) / 2)
                if any(r.contains(pt) for r in table_rects):
                    continue
                if any(r.contains(pt) for r in fig_rects):
                    continue
                items.append({'y0': y0, 'y1': y1, 'x0': x0, 'spans': spans, 'text': text})
        items.sort(key=lambda i: (round(i['y0'], 1), i['x0']))

        # --- one reading-order flow of lines, tables, figures ---
        flow = [('line', it['y0'], it) for it in items]
        flow += [('table', r.y0, rows) for r, rows in tables]
        flow += [('figure', r.y0, (i, r)) for i, r in enumerate(fig_rects)]
        flow.sort(key=lambda e: e[1])

        blocks = []
        para = None
        code = None
        pend = self.toc_by_page.get(pno, [])

        def flush():
            nonlocal para, code
            if para is not None:
                blocks.append(para)
                para = None
            if code is not None:
                while code['lines'] and not code['lines'][-1].strip():
                    code['lines'].pop()
                if code['lines']:
                    blocks.append(code)
                code = None

        for kind, _y, payload in flow:
            if kind == 'table':
                flush()
                blocks.append({'kind': 'table', 'rows': payload})
                continue
            if kind == 'figure':
                flush()
                idx, r = payload
                name = f'figure-p{pno}-{idx + 1}.png'
                page.get_pixmap(clip=r, dpi=self.cfg['figure_dpi']).save(os.path.join(self.imgdir, name))
                blocks.append({'kind': 'figure', 'src': f'images/{name}', 'page': pno})
                continue

            it = payload
            spans = it['spans']
            text = it['text']
            ntext = norm(text)
            maxsize = max(s['size'] for s in spans)
            fonts = [s['font'] for s in spans]
            allcode = all('Courier' in f for f, s in zip(fonts, spans) if s['text'].strip())
            first_bold = 'Bold' in fonts[0] or 'Black' in fonts[0]

            # heading from the bookmark outline
            matched = None
            for entry in pend:
                if entry[2]:
                    continue
                if ntext == entry[1] or (len(ntext) > 8 and entry[1].startswith(ntext)) \
                        or ntext.startswith(entry[1]):
                    matched = entry
                    break
            if matched and (first_bold or maxsize >= 12):
                matched[2] = True
                flush()
                blocks.append({'kind': 'heading', 'level': matched[0], 'text': ntext})
                continue

            # heading from font size
            if first_bold and maxsize >= 12.5 and not allcode and len(ntext) >= 3:
                flush()
                lvl = 1 if maxsize >= 17 else (2 if maxsize >= 13.5 else 3)
                blocks.append({'kind': 'heading', 'level': lvl, 'text': ntext})
                continue

            # sub-heading (Command Description / Command Syntax / Examples ...)
            if first_bold and 10.5 <= maxsize < 12.5 and len(ntext) < 70 and not allcode \
                    and all(('Bold' in f or 'Black' in f) for f, s in zip(fonts, spans) if s['text'].strip()):
                flush()
                blocks.append({'kind': 'heading', 'level': 4, 'text': ntext, 'plain': True})
                continue

            # table / figure caption
            if CAPTION_RE.match(ntext) and 'Italic' in fonts[0]:
                flush()
                blocks.append({'kind': 'caption', 'text': ntext})
                continue

            # code
            if allcode:
                if code is None:
                    code = {'kind': 'code', 'lines': [], 'x0': it['x0']}
                elif it['y0'] - code.get('last_y1', it['y0']) > 26:
                    flush()
                    code = {'kind': 'code', 'lines': [], 'x0': it['x0']}
                extra = int(round(max(0.0, it['x0'] - code['x0']) / 5.0))
                code['lines'].append(' ' * extra + text.rstrip())
                code['last_y1'] = it['y1']
                continue
            elif code is not None:
                flush()

            # bullet
            m_b = re.match(r'^\s*([•▪◦o])\s+', ntext)
            if m_b and ('Black' in fonts[0] or 'Symbol' in fonts[0] or ntext[0] in '•▪◦'):
                flush()
                lvl = 2 if it['x0'] > 118 else (1 if it['x0'] > 95 else 0)
                keep, dropped = [], False
                for s in spans:
                    if not dropped and s['text'].strip() in ('•', '▪', '◦'):
                        dropped = True
                        continue
                    keep.append(s)
                body = inline(keep).lstrip()
                if not dropped:
                    body = re.sub(r'^[•▪◦]\s*', '', body)
                blocks.append({'kind': 'bullet', 'level': lvl, 'text': body, 'last_y1': it['y1']})
                continue

            # ordinary text: join wrapped lines into paragraphs
            md = inline(spans)
            if para is not None:
                gap = it['y0'] - para['last_y1']
                if gap <= 7 and abs(it['x0'] - para['x0']) < 8:
                    para['text'] = join_wrapped(para['text'], md)
                    para['last_y1'] = it['y1']
                    continue
                blocks.append(para)
                para = None
            if blocks and blocks[-1]['kind'] == 'bullet' and it['x0'] > 85 and \
                    (it['y0'] - blocks[-1].get('last_y1', -99)) <= 7:
                blocks[-1]['text'] = join_wrapped(blocks[-1]['text'], md)
                blocks[-1]['last_y1'] = it['y1']
                continue
            para = {'kind': 'para', 'text': md, 'x0': it['x0'], 'last_y1': it['y1']}

        flush()
        return blocks

    # ------------------------------------------------------------- render ---
    def render(self, blocks, out, state):
        """Append markdown for blocks, merging tables/code/paragraphs across pages."""
        for b in blocks:
            k = b['kind']
            if k == 'heading':
                lvl = min(6, 4 if b.get('plain') else b['level'])
                out += ['', '#' * lvl + ' ' + b['text'], '']
                state['tbl'] = None
            elif k == 'caption':
                out += ['', f"**{b['text']}**", '']
                state['caption'] = b['text']
            elif k == 'table':
                rows = b['rows']
                cap = state.pop('caption', None)
                base = re.sub(r'\s*\(continued\)\s*$', '', cap or '').strip()
                tbl = state.get('tbl')
                cont = False
                if tbl:
                    if cap and cap.rstrip().endswith('(continued)') and \
                            (base == tbl['cap'] or rows[0] == tbl['head']):
                        cont = True
                    elif not cap and state.get('last') == 'table' and rows[0] == tbl['head']:
                        cont = True
                if cont:
                    ncol = len(tbl['head'])
                    body = rows[1:] if rows[0] == tbl['head'] else rows
                    pop_gap(out, extra=f'**{cap}**' if cap else None)
                    # first row with an empty first cell continues the last row
                    if body and not body[0][0].strip() and tbl['rows']:
                        tail = body.pop(0)
                        last = tbl['rows'][-1]
                        for i, c in enumerate(tail):
                            if c.strip() and i < len(last):
                                last[i] = join_wrapped(last[i], c).strip()
                        if out and out[-1].startswith('|'):
                            out[-1] = row_md(last, ncol)
                    for r in body:
                        tbl['rows'].append(list(r))
                        out.append(row_md(r, ncol))
                    out.append('')
                else:
                    out.append('')
                    out.extend(table_md(rows).split('\n'))
                    out.append('')
                    has_head = any(c.strip() for c in rows[0])
                    state['tbl'] = {'rows': [list(r) for r in (rows[1:] if has_head else rows)],
                                    'head': rows[0], 'cap': base}
                state['last'] = 'table'
                state['code_open'] = False
                continue
            elif k == 'figure':
                out += ['', f"![Figure from page {b['page']}]({b['src']})", '']
            elif k == 'bullet':
                out.append('  ' * b['level'] + '- ' + b['text'])
            elif k == 'code':
                if state.get('last') == 'code' and state.get('code_open'):
                    pop_gap(out, extra='```')
                    if out and out[-1] == '```':
                        out.pop()
                    out.extend(b['lines'])
                    out += ['```', '']
                else:
                    out += ['', '```'] + b['lines'] + ['```', '']
                state['last'] = 'code'
                state['code_open'] = True
                state['merge_next'] = False
                continue
            elif k == 'para':
                txt = b['text'].strip()
                if state.get('merge_next') and state.get('last') == 'para':
                    pop_gap(out)
                    if out and out[-1].strip():
                        out[-1] = join_wrapped(out[-1], txt)
                    else:
                        out.append(txt)
                    out.append('')
                else:
                    out += ['', txt, '']
            state['last'] = k
            state['merge_next'] = False
            state['code_open'] = False
            if k in ('heading', 'figure'):
                state['tbl'] = None

    def build_toc_md(self):
        seen = {}
        lines = ['## Contents', '']
        for lvl, title, pno in self.toc:
            t = norm(title)
            anchor = slugify(t, seen)
            if t in ('CLI Reference Guide', 'Contents'):
                continue
            lines.append('  ' * max(0, lvl - 1) + f'- [{t}](#{anchor}) — p. {pno}')
        lines.append('')
        return '\n'.join(lines)

    # ---------------------------------------------------------------- run ---
    def run(self, first, last, outfile):
        last = last or len(self.doc)
        c_first, c_last = self.cfg['contents_pages']
        l_first, l_last = self.cfg['list_pages']
        out, state = [], {}
        for pno in range(first, last + 1):
            if first == 1 and pno == c_first:
                out += ['', self.build_toc_md()]
            if first == 1 and c_first <= pno <= c_last:
                continue                          # replaced by generated contents
            blocks = self.process_page(pno)

            if l_first <= pno <= l_last:          # dotted-leader lists -> bullets
                nb = []
                for b in blocks:
                    if b['kind'] in ('para', 'bullet') and '...' in b['text']:
                        items = re.findall(r'\s*(.+?)\s*\.{3,}\s*(\d{1,4})\b', b['text'])
                        if items:
                            nb += [{'kind': 'bullet', 'level': 0, 'text': f'{t.strip()} — p. {pg}'}
                                   for t, pg in items]
                            continue
                    nb.append(b)
                blocks = nb

            out.append(f'<!-- page {pno} -->')
            # a lowercase-starting paragraph continues one cut by the page break
            if blocks and blocks[0]['kind'] == 'para' and state.get('last') == 'para':
                t = blocks[0]['text'].lstrip()
                prev_end = next((ln.strip() for ln in reversed(out)
                                 if ln.strip() and not ln.startswith('<!--')), '')
                if t[:1].islower() and not prev_end.endswith(('.', ':', '!', '?', '|', '`')):
                    state['merge_next'] = True
            self.render(blocks, out, state)
            if pno % 50 == 0:
                print(f'  ...page {pno}', file=sys.stderr, flush=True)

        text = re.sub(r'\n{3,}', '\n\n', '\n'.join(out))
        path = os.path.join(self.outdir, outfile)
        with open(path, 'w', encoding='utf-8') as f:
            f.write(text)
        print('wrote', path, len(text), 'chars')


# -------------------------------------------------------------------- CLI ---
def page_range(s):
    a, b = s.split('-')
    return int(a), int(b)


def main():
    ap = argparse.ArgumentParser(description='Structure-aware PDF to Markdown converter.')
    ap.add_argument('pdf', help='input PDF')
    ap.add_argument('-o', '--outdir', default='out', help='output folder (default: out)')
    ap.add_argument('--outfile', help='markdown file name (default: <pdf name>.md)')
    ap.add_argument('--first', type=int, default=1, help='first page (1-based)')
    ap.add_argument('--last', type=int, default=None, help='last page (default: end)')
    ap.add_argument('--figure-pages', help='comma list of pages with figures, e.g. 45,46,48')
    ap.add_argument('--contents-pages', type=page_range, help='printed Contents pages, e.g. 3-14')
    ap.add_argument('--list-pages', type=page_range, help='List of Figures/Tables pages, e.g. 15-35')
    ap.add_argument('--watermark', help='exact watermark text to drop')
    args = ap.parse_args()

    cfg = dict(CONFIG)
    if args.figure_pages is not None:
        cfg['figure_pages'] = [int(p) for p in args.figure_pages.split(',') if p.strip()]
    if args.contents_pages:
        cfg['contents_pages'] = args.contents_pages
    if args.list_pages:
        cfg['list_pages'] = args.list_pages
    if args.watermark is not None:
        cfg['watermark'] = args.watermark

    outfile = args.outfile or os.path.splitext(os.path.basename(args.pdf))[0] + '.md'
    Converter(args.pdf, args.outdir, cfg).run(args.first, args.last, outfile)


if __name__ == '__main__':
    main()
