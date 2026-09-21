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

Options added for multi-column rulebooks (e.g. the ASL rulebook):
  * --columns 2         read each column top to bottom; full-width items split bands
  * --auto-figures      find maps, counters and diagrams on every page and rasterize them
  * --tables-as-text    write tables and columnar lists as laid-out ```text blocks
  * --page-image        embed the whole page as an image, then its charts as text blocks
  * --top-lim / --foot-frac / --footer-re   running head and footer bands

Usage:
  pip install pymupdf
  python pdf_to_markdown.py input.pdf -o out_dir
  python pdf_to_markdown.py input.pdf -o out_dir --first 96 --last 100   # sample
  python pdf_to_markdown.py rules.pdf -o out --columns 2 --auto-figures --tables-as-text \\
      --top-lim 82 --foot-frac 0.955 --footer-re '^[A-Z]\\d{1,3}$' --watermark '' --figure-pages ''

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
    if a.endswith('\u00ad'):                     # soft hyphen: a word broken at the line end
        return a[:-1] + b
    m = re.search('\u00ad' + r'(\*{1,3})$', a)  # ... inside emphasis: "*Loca-*" + "*tion*"
    if m:
        marks = m.group(1)
        if b.startswith(marks):
            return a[:m.start()] + b[len(marks):]
        return a[:m.start()] + marks + b
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
        sup = bool(s.get('flags', 0) & 1) and bool(t.strip()) and not code
        parts.append({'t': t, 'code': code, 'bold': bold, 'ital': ital, 'sup': sup})
    merged = []
    for p in parts:
        if merged and (merged[-1]['code'], merged[-1]['bold'], merged[-1]['ital'], merged[-1]['sup']) == \
                (p['code'], p['bold'], p['ital'], p['sup']):
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
            if p['sup']:
                core = f'<sup>{core}</sup>'
            if p['bold']:
                core = f'**{core}**'
            if p['ital']:
                core = f'*{core}*'
        out.append(lead + core + trail)
    return ''.join(out).rstrip()


def sup_text(s):
    """Span text for plain-text output, with superscripts written as ^x."""
    t = s['text']
    if s.get('flags', 0) & 1 and t.strip():
        return '^' + t.strip() + (' ' if t.endswith(' ') else '')
    return t


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


def grow_regions(rects, lines, pad_x=3, pad_y=6):
    """Grow each region over text lines that touch it, until nothing changes."""
    rects = cluster_rects(rects, pad=0) if rects else []
    grew = True
    while grew and rects:
        grew = False
        for r in lines:
            for i, c in enumerate(rects):
                if not c.contains(r) and \
                        pymupdf.Rect(c.x0 - pad_x, c.y0 - pad_y, c.x1 + pad_x, c.y1 + pad_y).intersects(r):
                    rects[i] = c | r
                    grew = True
                    break
        rects = cluster_rects(rects, pad=0)
    return rects


def merge_side_by_side(rects, gap=15, overlap=0.6):
    """Join regions that sit next to each other at the same height (one chart
    that straddles a column boundary)."""
    rects = [pymupdf.Rect(r) for r in rects]
    merged = True
    while merged:
        merged = False
        for i in range(len(rects)):
            for j in range(i + 1, len(rects)):
                a, b = rects[i], rects[j]
                ov = min(a.y1, b.y1) - max(a.y0, b.y0)
                hgap = max(a.x0, b.x0) - min(a.x1, b.x1)
                if ov > overlap * min(a.height, b.height) and hgap < gap:
                    rects[i] = a | b
                    del rects[j]
                    merged = True
                    break
            if merged:
                break
    return rects


def tabular_regions(lines, colw):
    """Regions where short lines sit side by side on shared baselines within one
    column (a list or chart set in columns), as opposed to flowing prose."""
    def col(r):
        return int(((r.x0 + r.x1) / 2) // colw)
    tab = []
    for r in lines:
        yc = (r.y0 + r.y1) / 2
        for q in lines:
            if q is r or col(q) != col(r) or abs((q.y0 + q.y1) / 2 - yc) > 2.5:
                continue
            if q.x0 - r.x1 > 6 or r.x0 - q.x1 > 6:
                tab.append(r)
                break
    out = []
    for c in cluster_rects(tab, pad=6):
        members = [r for r in tab if c.contains(r)]
        rows = {round((r.y0 + r.y1) / 4) for r in members}          # ~2pt bins
        if len(members) >= 4 and len(rows) >= 2:
            out.append(c)
    return out


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
        # image names carry the source name so several PDFs can share one images/ folder
        self.stem = re.sub(r'[^A-Za-z0-9_.-]+', '_', os.path.splitext(os.path.basename(src))[0])
        os.makedirs(self.imgdir, exist_ok=True)
        self.footer_re = re.compile(cfg['footer_re'])
        self.figure_pages = set(cfg['figure_pages'])
        self.toc = self.doc.get_toc()
        self.toc_by_page = {}
        for lvl, title, pno in self.toc:
            self.toc_by_page.setdefault(pno, []).append([lvl, norm(title), False])

    # ------------------------------------------------------ auto figures ---
    def auto_figures(self, page, top_lim, foot_lim, table_rects):
        """Find illustrations on any page: images and colored vector art, clustered.

        White or near-white fills are ignored (they are the background boxes that
        hold example text), as are clusters that sit mostly on a detected table.
        Small labels touching a cluster are pulled into it.
        """
        def whiteish(c):
            return c is None or min(c) > 0.95

        # an image shown through a clip path reports its full, unclipped bbox;
        # a clip lying (almost) wholly inside the image is its visible part
        clips = []
        for d in page.get_drawings(extended=True):
            s = d.get('scissor') if d['type'] == 'clip' else None
            if s is not None and not s.is_empty and s.get_area() >= 400 \
                    and s.get_area() < 0.8 * page.rect.get_area():
                clips.append(pymupdf.Rect(s))

        def visible(r):
            # a substantial clip inside the art is its visible window; small clips
            # (a counter's own outline on a big map) are ignored
            inner = [c for c in clips if (c & r).get_area() >= 0.9 * c.get_area()
                     and c.get_area() < 0.95 * r.get_area()
                     and (c.get_area() >= 0.2 * r.get_area() or not page.rect.contains(r))]
            if inner:
                r = max(inner, key=lambda c: c.get_area()) & r
            return r & page.rect

        seeds, thin = [], []
        for i in page.get_image_info():
            r = visible(pymupdf.Rect(i['bbox']))
            if r.y1 > top_lim and r.y0 < foot_lim and not r.is_empty:
                seeds.append(r)
        # vector art: clip each path to the clip region it is drawn under
        stack = {}                                              # nesting level -> scissor
        for dr in page.get_drawings(extended=True):
            if dr['type'] == 'clip':
                lvl = dr['level']
                stack = {k: v for k, v in stack.items() if k < lvl}
                if dr.get('scissor') is not None:
                    stack[lvl] = pymupdf.Rect(dr['scissor'])
                continue
            if dr['type'] == 'group':
                continue
            r = pymupdf.Rect(dr['rect'])
            under = [v for k, v in stack.items() if k < dr.get('level', 0)]
            if under:
                r &= under[-1] if len(under) == 1 else stack[max(k for k in stack if k < dr['level'])]
            r &= page.rect
            if r.is_empty or r.y1 <= top_lim or r.y0 >= foot_lim:
                continue
            if whiteish(dr.get('fill')) and whiteish(dr.get('color')):
                continue
            if r.get_area() > 0.4 * page.rect.get_area():       # page background tint
                continue
            if r.width < 1.5 or r.height < 1.5:
                thin.append(r)                                  # rules and arrows
            else:
                seeds.append(r)
        if not seeds:
            return []
        clusters = cluster_rects(seeds, pad=3)
        clusters = [c for c in clusters if c.width >= 18 and c.height >= 18 and c.width * c.height >= 500]
        # drop clusters that are really a table's shading or rules
        keep = []
        for c in clusters:
            area = c.width * c.height
            if any((c & t).get_area() > 0.5 * area for t in table_rects):
                continue
            keep.append(c)
        clusters = keep
        if not clusters:
            return []
        # arrows / leader lines that touch a figure belong to it
        for r in thin:
            for i, c in enumerate(clusters):
                if pymupdf.Rect(c.x0 - 3, c.y0 - 3, c.x1 + 3, c.y1 + 3).intersects(r):
                    clusters[i] = c | r
                    break
        # diagram callouts touching a figure belong to it: tiny text of any
        # width, or short lines (a label, not a line of wrapped prose)
        lsize = self.cfg.get('label_size', 7.5)
        lwidth = self.cfg.get('label_width', 110)
        allines, rule_heads = [], []
        for b in page.get_text('dict')['blocks']:
            if b['type'] != 0:
                continue
            for l in b['lines']:
                if not l['spans'] or not ''.join(s['text'] for s in l['spans']).strip():
                    continue
                allines.append((pymupdf.Rect(l['bbox']), max(s['size'] for s in l['spans'])))
                if norm(''.join(s['text'] for s in l['spans']))[:1].islower():
                    rule_heads.append(allines[-1][0])      # "ble (E4.2)." continues prose
                f0 = l['spans'][0]['font']
                if ('Bold' in f0 or 'Black' in f0) and \
                        re.match(r'^\(?[A-Z]?\.?\d+(\.\d+)*\b', norm(l['spans'][0]['text'])):
                    rule_heads.append(allines[-1][0])      # "25.79 ORDNANCE:" opens prose

        # prose wrapped beside a figure is narrow too, but it comes in runs of
        # stacked lines sharing a left edge; a label stands alone (or in a pair)
        def stacked(r):
            def nb(r, d):
                return [q for q, _ in allines if abs(q.x0 - r.x0) < 2.5 and 0 < d * (q.y0 - r.y0) < r.height + 4]
            up, down = nb(r, -1), nb(r, 1)
            run = 1 + len(up[:1]) + len(down[:1])
            if run < 3:
                if up and nb(up[0], -1):
                    run = 3
                if down and nb(down[0], 1):
                    run = 3
            return run >= 3

        # a piece of a justified prose line split at a wide gap has a neighbour on its row
        def in_row(r):
            yc = (r.y0 + r.y1) / 2
            return any(q is not r and abs((q.y0 + q.y1) / 2 - yc) < 2.5
                       and (0 <= q.x0 - r.x1 < 25 or 0 <= r.x0 - q.x1 < 25) for q, _ in allines)

        # the first or last line of a paragraph sits right against a wide prose line
        colw = page.rect.width / self.cfg.get('columns', 1)

        def next_to_prose(r):
            return any(q.width >= 0.45 * colw and min(q.x1, r.x1) - max(q.x0, r.x0) > 0
                       and (-r.height < r.y0 - q.y1 < 4 or -r.height < q.y0 - r.y1 < 4)
                       for q, _ in allines if q is not r)

        lines = [r for r, size in allines
                 if (size <= lsize or r.width <= lwidth) and not stacked(r) and not in_row(r)
                 and r not in rule_heads and not next_to_prose(r)]
        for _ in range(3):                                      # multi-line labels
            grew = False
            for r in lines:
                for i, c in enumerate(clusters):
                    if c.contains(r):
                        break
                    if pymupdf.Rect(c.x0 - 4, c.y0 - 4, c.x1 + 4, c.y1 + 4).intersects(r):
                        clusters[i] = c | r
                        grew = True
                        break
            if not grew:
                break
        clusters = cluster_rects(clusters, pad=0)
        return [pymupdf.Rect(c.x0, max(c.y0, top_lim), c.x1, min(c.y1, foot_lim)) for c in clusters]

    # ------------------------------------------------------- layout text ---
    def layout_text(self, page, rect):
        """Text inside rect as monospaced lines that keep the chart's rows and columns.

        Lines are grouped into rows by baseline and placed at a character column
        proportional to their x position. Rotated charts (text running up or down
        the page) are read in their own orientation.
        """
        found = []
        for b in page.get_text('dict', clip=rect)['blocks']:
            if b['type'] != 0:
                continue
            for l in b['lines']:
                t = ''.join(sup_text(s) for s in l['spans']).strip()
                lr = pymupdf.Rect(l['bbox'])
                if t and rect.contains((lr.tl + lr.br) / 2):
                    found.append((pymupdf.Rect(l['bbox']), tuple(round(d) for d in l['dir']), t))
        if not found:
            return []
        weight = {}
        for _, d, t in found:
            weight[d] = weight.get(d, 0) + len(t)
        dom = max(weight, key=weight.get)

        def uv(r):                          # (along-line start, along end, across centre, height)
            if dom == (0, -1):              # reads bottom-to-top
                return -r.y1, -r.y0, r.x0 + r.width / 2, r.width
            if dom == (0, 1):               # reads top-to-bottom
                return r.y0, r.y1, -(r.x0 + r.width / 2), r.width
            return r.x0, r.x1, r.y0 + r.height / 2, r.height

        items = [uv(r) + (t,) for r, d, t in found if d == dom]
        widths = sorted((u1 - u0) / len(t) for u0, u1, _, _, t in items if len(t) >= 3)
        cw = widths[len(widths) // 2] if widths else 4.0
        umin = min(i[0] for i in items)
        rows = []
        for it in sorted(items, key=lambda i: i[2]):
            if rows and abs(it[2] - rows[-1][0]) < 0.5 * it[3]:
                rows[-1][1].append(it)
            else:
                rows.append([it[2], [it]])
        out = []
        for _, row in rows:
            line = ''
            for u0, _, _, _, t in sorted(row, key=lambda i: i[0]):
                col = int(round((u0 - umin) / cw))
                if line and col < len(line) + 1:
                    col = len(line) + (2 if line[-1] != ' ' else 0)
                line = line.ljust(col) + t
            out.append(line.rstrip())
        return out

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

        # --- figures found automatically: raster images plus colored vector art ---
        if self.cfg.get('auto_figures') and not fig_rects and not self.cfg.get('page_image'):
            fig_rects = self.auto_figures(page, top_lim, foot_lim, table_rects)
            # grid lines inside a map or diagram are not a table
            def inside_fig(t):
                return any((t & f).get_area() > 0.5 * t.get_area() for f in fig_rects)
            tables = [t for t in tables if not inside_fig(t[0])]
            table_rects = [t[0] for t in tables]

        # --- chart pages: each chart box becomes a layout-preserving text block ---
        chart_rects = []
        tl = [pymupdf.Rect(l['bbox']) for b in page.get_text('dict')['blocks'] if b['type'] == 0
              for l in b['lines'] if ''.join(s['text'] for s in l['spans']).strip()]
        tl = [r for r in tl if top_lim < r.y1 and r.y0 < foot_lim
              and not any(f.contains((r.tl + r.br) / 2) for f in fig_rects)]
        if self.cfg.get('page_image'):
            # titles, headers and lightly tinted rows next to a chart belong to it
            chart_rects = grow_regions(self.auto_figures(page, top_lim, foot_lim, []), tl)
        elif self.cfg.get('tables_as_text'):
            # ruled/shaded tables and side-by-side short lines (lists, small DRM
            # charts) read far better as laid-out text than as prose or GFM tables
            colw = W / self.cfg.get('columns', 1)
            # short lines, but not the short last line of a prose paragraph
            def ends_para(r):
                return any(q.width >= colw * 0.6 and abs(q.x0 - r.x0) < 2.5 and q.y0 < r.y0
                           and -r.height < r.y0 - q.y1 < 4 for q in tl)
            short = [r for r in tl if r.width < colw * 0.6 and not ends_para(r)]
            seeds = [pymupdf.Rect(t[0]) for t in tables] + tabular_regions(tl, colw)
            chart_rects = grow_regions(merge_side_by_side(grow_regions(seeds, short)), short)
        if chart_rects:
            tables = [t for t in tables
                      if not any((t[0] & c).get_area() > 0.5 * t[0].get_area() for c in chart_rects)]
            table_rects = [t[0] for t in tables]

        # --- keep figure regions clear of captions, tables, and body text below ---
        if fig_rects and not self.cfg.get('auto_figures'):
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
                if abs(l['dir'][1]) > 0.5 and any(                # sideways label on a counter
                        pymupdf.Rect(f.x0 - 12, f.y0 - 12, f.x1 + 12, f.y1 + 12).intersects(l['bbox'])
                        for f in fig_rects):
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
                if any(r.contains(pt) for r in chart_rects):
                    continue
                items.append({'y0': y0, 'y1': y1, 'x0': x0, 'x1': x1, 'spans': spans, 'text': text})
        # a small raised number right after a line (footnote marker) joins that line
        keep = []
        for it in items:
            t = it['text'].strip()
            host = None
            if t.isdigit() and len(t) <= 3 and max(s['size'] for s in it['spans']) <= 7:
                host = next((q for q in items if q is not it and 0 <= it['x0'] - q['x1'] < 6
                             and q['y0'] - 4 <= it['y0'] <= q['y1']), None)
            if host is not None:
                host['spans'] = host['spans'] + [dict(it['spans'][0], text=t, flags=1)]
                host['text'] += t
                host['x1'] = it['x1']
            else:
                keep.append(it)
        items = keep
        items.sort(key=lambda i: (round(i['y0'], 1), i['x0']))

        # --- one reading-order flow of lines, tables, figures ---
        flow = [('line', it['y0'], it, it['x0'], it['x1']) for it in items]
        flow += [('table', r.y0, rows, r.x0, r.x1) for r, rows in tables]
        flow += [('figure', r.y0, (i, r), r.x0, r.x1) for i, r in enumerate(fig_rects)]
        flow += [('chart', r.y0, r, r.x0, r.x1) for r in chart_rects]
        ncols = self.cfg.get('columns', 1)
        if ncols > 1:
            # Full-width elements split the page into bands; within a band,
            # read each column top to bottom before moving to the next.
            colw = W / ncols
            spanning = lambda e: (e[4] - e[3]) > colw * 1.15
            cuts = sorted(e[1] for e in flow if spanning(e))

            def key(e):
                band = sum(1 for c in cuts if c <= e[1])
                col = -1 if spanning(e) else min(ncols - 1, int(((e[3] + e[4]) / 2) // colw))
                return (band, col, e[1], e[3])
            flow.sort(key=key)
        else:
            flow.sort(key=lambda e: e[1])
        flow = [e[:3] for e in flow]

        blocks = []
        if self.cfg.get('page_image'):
            # whole page as one picture (dense charts); text and tables still follow
            name = f'{self.stem}-p{pno}-page.png'
            page.get_pixmap(dpi=self.cfg['figure_dpi']).save(os.path.join(self.imgdir, name))
            blocks.append({'kind': 'figure', 'src': f'images/{name}', 'page': pno})
        para = None
        bullet_at = None                  # x of a bullet glyph set on its own line
        code = None
        pend = self.toc_by_page.get(pno, [])

        held = []                         # figures waiting for the paragraph they interrupt

        def end_para():
            nonlocal para
            if para is not None:
                blocks.append(para)
                para = None
            blocks.extend(held)
            held.clear()

        def flush():
            nonlocal para, code
            end_para()
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
            if kind == 'chart':
                flush()
                lines = self.layout_text(page, payload)
                if lines:
                    blocks.append({'kind': 'chart', 'lines': lines})
                continue
            if kind == 'figure':
                idx, r = payload
                name = f'{self.stem}-p{pno}-{idx + 1}.png'
                page.get_pixmap(clip=r, dpi=self.cfg['figure_dpi']).save(os.path.join(self.imgdir, name))
                fig = {'kind': 'figure', 'src': f'images/{name}', 'page': pno}
                if para is not None and self.cfg.get('auto_figures'):
                    held.append(fig)      # text wraps around it: keep the paragraph whole
                else:
                    flush()
                    blocks.append(fig)
                continue

            it = payload
            spans = it['spans']
            text = it['text']
            ntext = norm(text)
            maxsize = max(s['size'] for s in spans)
            fonts = [s['font'] for s in spans]
            allcode = all('Courier' in f for f, s in zip(fonts, spans) if s['text'].strip())
            first_bold = 'Bold' in fonts[0] or 'Black' in fonts[0]
            # heading text keeps superscript footnote markers
            htext = norm(''.join(f"<sup>{s['text'].strip()}</sup>" if s.get('flags', 0) & 1 and s['text'].strip()
                                 else s['text'] for s in spans))

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

            # section heading set in bold capitals at body size ("5. STACKING LIMITS")
            if self.cfg.get('columns', 1) > 1 and first_bold and maxsize >= 9.5 and len(ntext) < 60 \
                    and sum(ch.isalpha() for ch in ntext) >= 4 and ntext == ntext.upper() \
                    and all(('Bold' in f or 'Black' in f) for f, s in zip(fonts, spans) if s['text'].strip()):
                flush()
                blocks.append({'kind': 'heading', 'level': 2, 'text': htext})
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

            # the diamond set between a counter's front and back pictures
            if ntext == '◊':
                continue

            # a footnote marker set as its own superscript line belongs to the text before it
            small_num = ntext.isdigit() and maxsize <= 7 and para is None \
                and blocks and blocks[-1]['kind'] == 'heading'
            if len(ntext) <= 4 and (small_num or all(s.get('flags', 0) & 1 for s in spans if s['text'].strip())):
                target = para if para is not None else (blocks[-1] if blocks else None)
                if target is not None and 'text' in target:
                    target['text'] = target['text'].rstrip() + f'<sup>{esc(ntext)}</sup>'
                    continue

            # a bullet glyph set as its own line: the next line is the item
            if ntext in ('•', '▪', '◦'):
                flush()
                bullet_at = it['x0']
                continue

            # bullet
            m_b = re.match(r'^\s*([•▪◦o])\s+', ntext)
            if bullet_at is not None or \
                    (m_b and ('Black' in fonts[0] or 'Symbol' in fonts[0] or ntext[0] in '•▪◦')):
                flush()
                if ncols > 1:
                    lvl = 0
                else:
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
                gx0 = it['x0'] if bullet_at is None else bullet_at
                bullet_at = None
                blocks.append({'kind': 'bullet', 'level': lvl, 'text': body, 'last_y1': it['y1'],
                               'gx0': gx0, 'last_y0': it['y0'], 'last_x1': it['x1']})
                continue

            # ordinary text: join wrapped lines into paragraphs
            md = inline(spans)
            # a bold rule number (e.g. "A.2", "7.309", "EX:") opens a new paragraph
            rule_start = first_bold and re.match(r'^(\(?[A-Z]?\.?\d+(\.\d+)*|EX\b|NOTE\b)', ntext)
            if para is not None:
                gap = it['y0'] - para['last_y1']
                # the rest of a line that the PDF split at a wide justified gap
                same_row = abs(it['y0'] - para['last_y0']) < 2.5 and it['x0'] >= para['last_x1'] - 1
                # paragraph continuing at the top of the next column
                col_wrap = ncols > 1 and gap < 0 and it['x0'] - para['x0'] > W / ncols * 0.8
                # lines narrowed or shifted by a figure the text wraps around
                def beside_fig(y0, y1):
                    return any(f.y0 < y1 and f.y1 > y0 for f in fig_rects)
                around_fig = -(it['y1'] - it['y0']) < gap <= 7 and it['y0'] > para['last_y0'] + 2 and (bool(held) or beside_fig(para['last_y0'], para['last_y1'])
                                                or beside_fig(it['y0'], it['y1']))
                # centered lines (a note under a heading)
                centered = gap <= 4 and abs((it['x0'] + it['x1']) / 2 - para['last_cx']) < 6
                if not rule_start and (same_row or col_wrap or around_fig or centered
                                       or (gap <= 7 and abs(it['x0'] - para['x0']) < 8)):
                    if col_wrap or around_fig:
                        para['x0'] = it['x0']
                    para['text'] = join_wrapped(para['text'], md)
                    para['last_y1'] = it['y1']
                    para['last_y0'] = it['y0']
                    para['last_x1'] = it['x1']
                    para['last_cx'] = (it['x0'] + it['x1']) / 2
                    continue
                end_para()
            if blocks and blocks[-1]['kind'] == 'bullet':
                bl = blocks[-1]
                same_row = abs(it['y0'] - bl['last_y0']) < 2.5 and it['x0'] >= bl['last_x1'] - 1
                indented = it['x0'] > bl['gx0'] + 3 and (it['y0'] - bl['last_y1']) <= 7
                if same_row or indented:
                    bl['text'] = join_wrapped(bl['text'], md)
                    bl['last_y1'] = it['y1']
                    bl['last_y0'] = it['y0']
                    bl['last_x1'] = it['x1']
                    continue
            para = {'kind': 'para', 'text': md, 'x0': it['x0'], 'last_y1': it['y1'],
                    'last_y0': it['y0'], 'last_x1': it['x1'], 'last_cx': (it['x0'] + it['x1']) / 2}

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
            elif k == 'chart':
                out += ['', '```text'] + b['lines'] + ['```', '']
                state['last'] = 'chart'
                state['code_open'] = False
                continue
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
    ap.add_argument('--outfile', help='markdown file name (default: <pdf name>.md, trailing number padded to 4 digits)')
    ap.add_argument('--first', type=int, default=1, help='first page (1-based)')
    ap.add_argument('--last', type=int, default=None, help='last page (default: end)')
    ap.add_argument('--figure-pages', help='comma list of pages with figures, e.g. 45,46,48')
    ap.add_argument('--contents-pages', type=page_range, help='printed Contents pages, e.g. 3-14')
    ap.add_argument('--list-pages', type=page_range, help='List of Figures/Tables pages, e.g. 15-35')
    ap.add_argument('--watermark', help='exact watermark text to drop')
    ap.add_argument('--top-lim', type=float, help='running-head band: drop text ending above this y (pt)')
    ap.add_argument('--foot-frac', type=float, help='footer band starts at this fraction of page height')
    ap.add_argument('--footer-re', help='regex for footer lines in the footer band')
    ap.add_argument('--auto-figures', action='store_true', help='detect figures on every page')
    ap.add_argument('--page-image', action='store_true', help='embed each whole page as an image before its text')
    ap.add_argument('--tables-as-text', action='store_true', help='write tables and columnar lists as laid-out text blocks')
    ap.add_argument('--columns', type=int, default=1, help='text columns per page (default: 1)')
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

    if args.top_lim is not None:
        cfg['top_lim'] = args.top_lim
    if args.foot_frac is not None:
        cfg['foot_frac'] = args.foot_frac
    if args.footer_re is not None:
        cfg['footer_re'] = args.footer_re
    cfg['columns'] = args.columns
    cfg['auto_figures'] = args.auto_figures
    cfg['page_image'] = args.page_image
    cfg['tables_as_text'] = args.tables_as_text

    # a trailing number in the name is zero-padded so split-page files sort in order
    # ("eASLRB_v3_01 43.pdf" -> "eASLRB_v3_01 0043.md")
    stem = os.path.splitext(os.path.basename(args.pdf))[0]
    stem = re.sub(r'(\d+)$', lambda m: m.group(1).zfill(4), stem)
    outfile = args.outfile or stem + '.md'
    Converter(args.pdf, args.outdir, cfg).run(args.first, args.last, outfile)


if __name__ == '__main__':
    main()
