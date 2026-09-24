#!/usr/bin/env python3
"""
build_curated_edition.py - deterministic ASL curated Markdown edition builder.

Reads the registered converter output under docs/ASL/Rulebook_Markdown and the
pinned eASLRB 3.01 PDF, and writes a curated edition plus a change ledger and
review queue under docs/ASL/CuratedEdition. The registered Markdown is never
modified. Every text change is recorded with its rule, class, base line and
PDF evidence; anything the evidence cannot decide goes to the review queue
unchanged.

Rules and their change classes:
  * page-marker        (conversion-fix) replace conversion `<!-- page N -->`
                       markers with `<!-- pdf-page N -->` markers placed where
                       physical PDF page N begins, inline when a page starts
                       mid-paragraph; restores pages the converter left unmarked
  * linebreak-hyphen   (conversion-fix) remove a hyphen the PDF sets at a line
                       or page end when the word-form evidence says it is a
                       soft break; real hyphens that fall at a line end stay
  * stale-hyphen       (typographic-correction) remove a hyphen the PDF itself
                       prints mid-line inside an ordinary word, left over from
                       an earlier line layout
  * split-emphasis     (markup-normalization) merge `*a* *b*` / `**a** **b**`
                       runs split by the converter (rendered text unchanged)
  * image-link-rebase  (markup-normalization) point image links at the
                       registered image directory
  * symbol-font-glyph  (conversion-fix) map a private-use code point to its
                       Unicode symbol when the PDF sets it in the expected
                       symbol font (SYMBOL_FONT_MAP)
  * blank-page         (observation) PDF page with no body text
  * toc-rebuild        (conversion-fix) rebuild the Table of Contents from PDF
                       span positions; fails closed unless it carries exactly
                       the printed words and every image the conversion used

Undecidable cases go to review/ unchanged. Reviewer decisions in
decisions/decisions.json (join, retain or replace, with reviewer and rationale) are
applied as class `reviewed-decision`; a decision that matches no queued item
fails the build. The build also fails unless the edition equals the base text
once hyphens, emphasis markers, whitespace and comments are removed, and
unless every removed hyphen is ledgered.

Usage (from the repository root):
  python utils/asl_curated_edition/build_curated_edition.py --pdf <eASLRB_v3_01.pdf>
"""
import argparse
import bisect
import collections
import hashlib
import json
import os
import re
import sys
import unicodedata

import pymupdf

TOOL_VERSION = '0.1.0'
EDITION_ID = 'asl-easlrb-3.01-a-e-curated'
PDF_SHA256 = '957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247'
REGISTRY = 'docs/ASL/SourceRegistry/asl-3.10-a-e.source-registry.json'
BASE_DIR = 'docs/ASL/Rulebook_Markdown'
OUT_DIR = 'docs/ASL/CuratedEdition'
DECISIONS = OUT_DIR + '/decisions/decisions.json'
IMAGE_PREFIX = '../../Rulebook_Markdown/images/'
# running heads sit above this y (pt); converter used the same band
TOP_LIM = 82.0
FOOT_FRAC = 0.955
PROBE_LEN = 40

HYPHENS = '\u00ad\u2010\u2011'
TOKEN_RE = re.compile(r'[A-Za-z0-9]+(?:-[A-Za-z0-9]+)+')
WORD_RE = re.compile(r'[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*')
ITALIC_SPLIT_RE = re.compile(r'(?<=[^\s*\\])\* \*(?=[^\s*])')
BOLD_SPLIT_RE = re.compile(r'(?<=[^\s*\\])\*\* \*\*(?=[^\s*])')
BASE_MARKER_RE = re.compile(r'<!-- page (\d+) -->\n(?:\n)?')
PROTECT_RES = [
    re.compile(r'(?ms)^```.*?^```[ \t]*$'),        # fenced layout blocks
    re.compile(r'!\[[^\]]*\]\([^)]*\)'),           # images
    re.compile(r'<!--.*?-->', re.S),               # comments
    re.compile(r'</?[A-Za-z][^>]*>'),              # html tags
]
# symbol-font code points the text layer exposes as private-use characters:
# code point -> (required PDF font, Unicode replacement, description)
SYMBOL_FONT_MAP = {
    0xF0AB: ('Wingdings', 0x2605, 'Wingdings 0xAB black five-pointed star'),
}
# prefixes that also form legitimate hyphenated words (re-fused vs refused)
PREFIXES = {'re', 'pre', 'co', 'de', 'non', 'un', 'sub', 'semi', 'anti', 'multi', 'self', 'ex', 'over', 'under', 'inter', 'counter'}
PUA_RE = re.compile(r'[\ue000-\uf8ff]')


def sha256_file(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for chunk in iter(lambda: f.read(1 << 20), b''):
            h.update(chunk)
    return h.hexdigest()


def sha256_text(text):
    return hashlib.sha256(text.encode('utf-8')).hexdigest()


def fold(s):
    s = unicodedata.normalize('NFKC', s)
    for h in HYPHENS:
        s = s.replace(h, '-')
    return s


# ------------------------------------------------------------------ PDF ---
class PdfEvidence:
    """Word stream, line-break hyphens and word-form attestation per page."""

    def __init__(self, path, first, last):
        self.doc = pymupdf.open(path)
        self.first, self.last = first, last
        self.body = {}            # page -> list of (text, line_end)
        self.blocks = {}          # page -> list of block texts in content order
        self.glyphs = {}          # page -> Counter of (private-use code point, font)
        for p in range(first, last + 1):
            page = self.doc[p - 1]
            foot = page.rect.height * FOOT_FRAC
            words = [w for w in page.get_text('words', sort=False)
                     if w[3] > TOP_LIM and w[1] < foot]
            seq = []
            for i, w in enumerate(words):
                nxt = words[i + 1] if i + 1 < len(words) else None
                line_end = nxt is None or (nxt[5], nxt[6]) != (w[5], w[6])
                seq.append((fold(w[4]), line_end))
            self.body[p] = seq
            self.blocks[p] = [fold(b[4]) for b in page.get_text('blocks', sort=False)
                              if b[6] == 0 and b[3] > TOP_LIM and b[1] < foot]
            glyphs = collections.Counter()
            for blk in page.get_text('rawdict')['blocks']:
                for line in blk.get('lines', []):
                    for span in line['spans']:
                        for ch in span['chars']:
                            if 0xE000 <= ord(ch['c']) <= 0xF8FF:
                                glyphs[(ord(ch['c']), span['font'])] += 1
            self.glyphs[p] = glyphs
        self.occ = collections.defaultdict(list)   # token -> [(page, break_idx)]
        self.joined = collections.Counter()        # unbroken lowercase word forms
        self.hyph_midline = collections.Counter()  # hyphenated forms seen mid-line
        pages = list(range(first, last + 1))
        for p in pages:
            seq = self.body[p]
            i = 0
            while i < len(seq):
                text, line_end = seq[i]
                brk = None
                # breaks across a page end are recovered from page-marker placement
                if line_end and text.endswith('-') and len(text) > 1 and i + 1 < len(seq) \
                        and seq[i + 1][0][:1].isalnum():
                    brk = len(text) - 1
                    text = text + seq[i + 1][0]
                    i += 1
                self._index(p, text, brk)
                i += 1

    def _index(self, page, text, brk):
        for m in TOKEN_RE.finditer(text):
            b = None
            if brk is not None and m.start() <= brk < m.end():
                b = sum(1 for c in text[m.start():brk] if c == '-')
            self.occ[m.group()].append((page, b))
            if b is None:
                self.hyph_midline[m.group().lower()] += 1
        for m in WORD_RE.finditer(text):
            if brk is not None and m.start() <= brk < m.end():
                continue
            if '-' not in m.group():
                self.joined[m.group().lower()] += 1


def norm_stream(text, protected):
    """Lowercase alnum stream of unprotected text with offsets into text."""
    chars, offs = [], []
    for i, c in enumerate(text):
        if protected[i]:
            continue
        c = unicodedata.normalize('NFKC', c)
        for cc in c:
            if cc.isalnum():
                chars.append(cc.lower())
                offs.append(i)
    return ''.join(chars), offs


def invariant_form(text):
    """Text with every change class the builder may make stripped out."""
    text = re.sub(r'<!--.*?-->', '', text, flags=re.S)
    text = text.replace('](' + IMAGE_PREFIX, '](images/')
    for cp, (_, uni, _) in SYMBOL_FONT_MAP.items():
        text = text.replace(chr(cp), chr(uni))
    return re.sub(r'[-*\s]', '', text)


def norm_plain(text):
    return ''.join(c.lower() for c in unicodedata.normalize('NFKC', text) if c.isalnum())


# -------------------------------------------------------------- builder ---
class FileBuild:
    def __init__(self, art, base_text, pdf, ledger_prefix, decisions):
        self.art = art
        self.decisions = decisions   # (rule, baseLine, text) -> decision
        self.used = set()
        self.src = base_text
        self.pdf = pdf
        self.prefix = ledger_prefix
        self.edits = []       # (start, end, replacement, ledger entry or None)
        self.ledger = []
        self.review = []
        self.line_starts = [0] + [m.end() for m in re.finditer('\n', base_text)]
        self.protected = bytearray(len(base_text))
        self.fenced = bytearray(len(base_text))
        for k, rx in enumerate(PROTECT_RES):
            for m in rx.finditer(base_text):
                for i in range(m.start(), m.end()):
                    self.protected[i] = 1
                    if k == 0:
                        self.fenced[i] = 1
        self.page_at = []     # sorted (offset, page)
        self.page_splits = {} # offset inside a word where a PDF page begins -> page

    def line(self, off):
        return bisect.bisect_right(self.line_starts, off)

    def context(self, off, end=None, width=36):
        end = off if end is None else end
        s = self.src[max(0, off - width):end + width]
        return s.replace('\n', ' ')

    def page_of(self, off):
        i = bisect.bisect_right([o for o, _ in self.page_at], off) - 1
        return self.page_at[i][1] if i >= 0 else self.art['startPage']

    def add(self, rule, cls, start, end, repl, **extra):
        eid = f'{self.prefix}-{len(self.ledger) + 1:05d}'
        entry = {'id': eid, 'rule': rule, 'class': cls, 'baseLine': self.line(start),
                 'before': self.src[start:end], 'after': repl}
        entry.update(extra)
        self.ledger.append(entry)
        self.edits.append((start, end, repl))

    def flag(self, rule, off, end, reason, **extra):
        text = self.src[off:end]
        dec = self.decisions.get((rule, self.line(off), text))
        if dec is not None:
            self.used.add(id(dec))
            self.apply_decision(dec, rule, off, end, reason)
            return
        entry = {'id': f'{self.prefix}-R{len(self.review) + 1:04d}', 'rule': rule,
                 'baseLine': self.line(off), 'text': self.src[off:end],
                 'context': self.context(off, end), 'reason': reason}
        entry.update(extra)
        self.review.append(entry)

    def apply_decision(self, dec, rule, off, end, reason):
        """Apply a reviewer decision recorded in decisions/decisions.json."""
        text = self.src[off:end]
        base = {'rule': rule, 'baseLine': self.line(off), 'decision': dec['decision'],
                'decidedBy': dec['decidedBy'], 'rationale': dec['rationale'],
                'queuedReason': reason}
        if dec['decision'] == 'retain':
            self.ledger.append({'id': f'{self.prefix}-{len(self.ledger) + 1:05d}',
                                'class': 'reviewed-decision', 'text': text, **base})
            return
        if dec['decision'] == 'replace':
            # the fail-closed invariant still limits the difference to the
            # permitted classes (hyphens, emphasis, whitespace)
            self.add(rule, 'reviewed-decision', off, end, dec['replacement'], token=text,
                     result=dec['replacement'],
                     **{key: v for key, v in base.items() if key not in ('rule', 'baseLine')})
            return
        # join: remove the hyphen at hyphenIndex (0-based among the token's hyphens)
        idx = [i for i, c in enumerate(text) if c == '-']
        k = dec.get('hyphenIndex', 0 if len(idx) == 1 else None)
        if k is None or k >= len(idx):
            sys.exit(f'decision for {text!r} at line {base["baseLine"]} needs a valid hyphenIndex')
        cut = off + idx[k]
        self.add(rule, 'reviewed-decision', cut, cut + 1, '', token=text,
                 result=text[:idx[k]] + text[idx[k] + 1:],
                 **{key: v for key, v in base.items() if key not in ('rule', 'baseLine')})

    # ---- page markers -------------------------------------------------
    def place_pages(self):
        base_markers = {int(m.group(1)): m for m in BASE_MARKER_RE.finditer(self.src)}
        for p, m in base_markers.items():
            self.edits.append((m.start(), m.end(), ''))
        stream, offs = norm_stream(self.src, self.protected)
        first, last = self.art['startPage'], self.art['endPage']
        cursor = 0
        for p in range(first, last + 1):
            limit = len(stream)
            for q in range(p + 1, last + 2):
                if q in base_markers:
                    lim_off = base_markers[q].start()
                    limit = bisect.bisect_left(offs, lim_off)
                    break
            # content order is not always reading order, so take the earliest
            # position at which any text block of the page occurs
            found = None
            for block in self.pdf.blocks.get(p, []):
                probe = norm_plain(block)[:PROBE_LEN]
                if len(probe) < 16:
                    continue
                k = stream.find(probe, cursor, limit + len(probe))
                if k >= 0 and stream.find(probe, k + 1, limit + len(probe)) >= 0:
                    continue   # repeated text (common in the index) cannot anchor a page
                if k >= 0 and (found is None or k < found):
                    found = k
            if found is None:
                img = re.search(rf'!\[[^\]]*\]\(images/eASLRB_v3_01-p{p}-\d+\.png\)', self.src)
                if p in base_markers:
                    off, how = base_markers[p].end(), 'base-marker'
                elif img:
                    off, how = img.start(), 'figure'
                else:
                    self.flag('page-marker', 0, 0, f'no PDF text of page {p} located', pdfPage=p)
                    continue
            else:
                cursor = found
                off, how = offs[found], 'pdf-text'
            self.insert_marker(p, off, how, base_markers.get(p))
            self.page_at.append((off, p))
        self.page_at.sort()
        # a page whose span holds no text and no figure was dropped by the converter
        bounds = self.page_at + [(len(self.src), None)]
        for (a, p), (b, _) in zip(bounds, bounds[1:]):
            span = self.src[a:b]
            if not norm_plain(BASE_MARKER_RE.sub('', span)) and '![' not in span:
                page = self.pdf.doc[p - 1]
                if not self.pdf.blocks.get(p):
                    self.ledger.append({'id': f'{self.prefix}-{len(self.ledger) + 1:05d}',
                                        'rule': 'blank-page', 'class': 'observation', 'pdfPage': p,
                                        'note': 'no body text; running head, banner and folio only'})
                    continue
                self.flag('page-content-missing', a, a,
                          f'PDF page {p} has no converted text or figure '
                          f'({len(page.get_images())} raster image(s) on the page)', pdfPage=p)

    def insert_marker(self, p, off, how, base_marker):
        s = self.src
        ls = s.rfind('\n', 0, off) + 1
        lead = s[ls:off]
        # skip back over leading markup on the line, including a converter
        # heading (`###### 4.152 CC`) that directly precedes this paragraph
        if re.fullmatch(r'[#*\s>\[(]*', lead):
            pos = ls
            prev = s.rfind('\n', 0, max(0, ls - 2)) + 1
            if ls >= 2 and s[ls - 2:ls] == '\n\n' and s.startswith('#', prev):
                pos = prev
            marker, kind = f'<!-- pdf-page {p} -->\n\n', 'block'
        else:
            pos = off
            if s[off - 1].isalnum() or s[off - 1] == '-':
                while pos < len(s) and not s[pos].isspace():
                    pos += 1
                while pos < len(s) and s[pos] == ' ':
                    pos += 1
                kind = 'inline-after-split-word'
                self.page_splits[off] = p
            else:
                while pos > ls and not s[pos - 1].isspace():
                    pos -= 1
                kind = 'inline'
            marker = f'<!-- pdf-page {p} --> '
        base = int(base_marker.group(1)) if base_marker else None
        self.ledger.append({'id': f'{self.prefix}-{len(self.ledger) + 1:05d}', 'rule': 'page-marker',
                            'class': 'conversion-fix', 'baseLine': self.line(pos), 'pdfPage': p,
                            'placement': kind, 'locatedBy': how,
                            'baseMarker': 'present' if base else 'missing',
                            'context': self.context(pos)})
        self.edits.append((pos, pos, marker))

    # ---- line-break hyphens -------------------------------------------
    def resolve_hyphens(self):
        by_key = collections.defaultdict(list)
        for m in TOKEN_RE.finditer(self.src):
            if self.protected[m.start()] or self.protected[m.end() - 1]:
                continue
            if m.start() > 0 and (self.src[m.start() - 1].isalnum() or self.src[m.start() - 1] == '\\'):
                continue
            split = [o for o in self.page_splits if m.start() < o < m.end() and self.src[o - 1] == '-']
            if split:
                b = self.src[m.start():split[0] - 1].count('-')
                self.decide(m, b, self.page_splits[split[0]] - 1, 'page-end')
                continue
            by_key[m.group()].append(m)
        for key, mds in by_key.items():
            pdf_occ = self.pdf.occ.get(key, [])
            per_page = collections.defaultdict(list)
            for pg, b in pdf_occ:
                per_page[pg].append(b)
            md_by_page = collections.defaultdict(list)
            for m in mds:
                md_by_page[self.page_of(m.start())].append(m)
            for pg, ms in md_by_page.items():
                cands = per_page.get(pg, [])
                if not cands:
                    # a marker placed slightly early or late shifts the page by one
                    near = per_page.get(pg - 1, []) + per_page.get(pg + 1, [])
                    if near and len(set(near)) == 1:
                        cands = near
                if not cands:
                    for m in ms:
                        if not any(b is not None for _, b in pdf_occ):
                            continue      # never broken anywhere: ordinary hyphen
                        self.flag('linebreak-hyphen', m.start(), m.end(),
                                  'token not found on its PDF page', pdfPage=pg)
                    continue
                if len(set(cands)) == 1:
                    statuses = [cands[0]] * len(ms)
                elif len(cands) == len(ms):
                    statuses = cands
                elif all(self.verdict(key, b)[0] == 'retain' for b in set(cands) if b is not None):
                    continue   # every line-end instance is a real hyphen: nothing changes
                else:
                    for m in ms:
                        self.flag('linebreak-hyphen', m.start(), m.end(),
                                  'mixed line-break and mid-line occurrences on page; counts differ',
                                  pdfPage=pg)
                    continue
                for m, b in zip(ms, statuses):
                    if b is not None:
                        self.decide(m, b, pg, 'line-end')
                    else:
                        self.stale(m, pg)

    def word(self, w):
        return self.pdf.joined[w.lower()] > 0

    def verdict(self, key, b):
        """Classify a hyphen the PDF sets at a line or page end.

        Returns (outcome, basis, joined, jc, hc) where outcome is
        'join', 'retain' or 'review'.
        """
        parts = key.split('-')
        left, right = '-'.join(parts[:b + 1]), '-'.join(parts[b + 1:])
        lpart, rpart = parts[b], parts[b + 1]
        joined = left + right
        jl = joined.lower()
        jc = self.pdf.hyph_midline[jl] if '-' in joined else self.pdf.joined[jl]
        hc = self.pdf.hyph_midline[key.lower()]
        if lpart[-1:].isdigit() and rpart[:1].isdigit():
            return 'retain', 'numeric', joined, jc, hc
        if jc > 0 and hc == 0:
            return 'join', 'joined-form-attested', joined, jc, hc
        if hc > 0 and jc == 0:
            return 'retain', 'hyphenated-form-attested', joined, jc, hc
        if rpart[:1].isupper() or rpart[:1].isdigit() or lpart[-1:].isdigit() \
                or (len(lpart) > 1 and lpart.isupper()):
            if jc == 0:
                return 'retain', 'acronym-or-numeric-compound', joined, jc, hc
            return 'review', None, joined, jc, hc
        if 1 <= hc <= 2 and jc >= 20 * hc:
            return 'join', 'mid-line-hyphenated-form-rare', joined, jc, hc
        if 1 <= hc <= 2 and jc >= 3 * hc and not self.word(rpart):
            return 'join', 'mid-line-hyphenated-form-rare', joined, jc, hc
        if jc == 0 and hc == 0 and len(parts) == 2 and lpart.isalpha() and rpart.isalpha() \
                and not self.word(lpart) and not self.word(rpart):
            return 'join', 'parts-are-not-words', joined, jc, hc
        return 'review', None, joined, jc, hc

    def decide(self, m, b, pg, where):
        key = m.group()
        outcome, basis, joined, jc, hc = self.verdict(key, b)
        if outcome == 'retain':
            return
        left = '-'.join(key.split('-')[:b + 1])
        ev = {'pdfPage': pg, 'pdfBreak': where, 'pdfBreakAfter': left + '-',
              'joinedFormAttested': jc, 'hyphenatedFormAttestedMidLine': hc}
        if outcome == 'review':
            self.flag('linebreak-hyphen', m.start(), m.end(),
                      'both forms attested' if jc and hc else 'neither form attested',
                      candidate=joined, evidence=ev)
            return
        ev['basis'] = basis
        cut = m.start() + len(left)
        self.add('linebreak-hyphen', 'conversion-fix', cut, cut + 1, '',
                 token=key, result=joined, evidence=ev)

    def stale(self, m, pg):
        """A hyphen printed mid-line inside an ordinary word (left over from reflow)."""
        key = m.group()
        parts = key.split('-')
        if len(parts) != 2 or not parts[0].isalpha() or not parts[1].isalpha() \
                or not parts[1].islower() or not parts[0][1:].islower():
            return
        joined = (parts[0] + parts[1]).lower()
        jc = self.pdf.joined[joined]
        hc = self.pdf.hyph_midline[key.lower()]
        if parts[0].lower() in PREFIXES:
            if jc >= 3 and hc <= 2:
                self.flag('stale-hyphen', m.start(), m.end(),
                          'mid-line hyphen after a real prefix; joined form is attested',
                          candidate=parts[0] + parts[1], pdfPage=pg,
                          evidence={'joinedFormAttested': jc, 'hyphenatedFormAttestedMidLine': hc})
            return
        if jc >= 3 and hc <= 2 and jc >= 3 * hc and not self.word(parts[1]):
            cut = m.start() + len(parts[0])
            self.add('stale-hyphen', 'typographic-correction', cut, cut + 1, '',
                     token=key, result=parts[0] + parts[1],
                     evidence={'pdfPage': pg, 'pdfBreak': 'none (printed mid-line)',
                               'joinedFormAttested': jc, 'hyphenatedFormAttestedMidLine': hc,
                               'basis': 'suffix-is-not-a-word'})

    # ---- emphasis, glyphs, links --------------------------------------
    def merge_emphasis(self):
        for rx, kind in ((BOLD_SPLIT_RE, 'bold'), (ITALIC_SPLIT_RE, 'italic')):
            for m in rx.finditer(self.src):
                if any(self.protected[i] for i in range(m.start(), m.end())):
                    continue
                if any(s < m.end() and m.start() < e for s, e, _ in self.edits):
                    continue
                self.add('split-emphasis', 'markup-normalization', m.start(), m.end(), ' ',
                         emphasis=kind, context=self.context(m.start(), m.end()))

    def flag_glyphs(self):
        for m in PUA_RE.finditer(self.src):
            if self.protected[m.start()]:
                continue
            cp, pg = ord(m.group()), self.page_of(m.start())
            known = SYMBOL_FONT_MAP.get(cp)
            fonts = sorted({f for (c, f) in self.pdf.glyphs.get(pg, {}) if c == cp})
            if known and fonts and all(known[0] in f for f in fonts):
                self.add('symbol-font-glyph', 'conversion-fix', m.start(), m.end(), chr(known[1]),
                         codePoint=f'U+{cp:04X}', result=f'U+{known[1]:04X}',
                         evidence={'pdfPage': pg, 'pdfFonts': fonts, 'glyph': known[2]},
                         context=self.context(m.start(), m.end()))
                continue
            self.flag('private-use-glyph', m.start(), m.end(),
                      f'private-use code point U+{ord(m.group()):04X} has no Unicode meaning; '
                      'compare the symbol on the PDF page', pdfPage=self.page_of(m.start()))

    def rebase_images(self):
        n = 0
        for m in re.finditer(r'\]\(images/', self.src):
            self.edits.append((m.start() + 2, m.start() + 9, IMAGE_PREFIX))
            n += 1
        if n:
            self.ledger.append({'id': f'{self.prefix}-{len(self.ledger) + 1:05d}',
                                'rule': 'image-link-rebase', 'class': 'markup-normalization',
                                'count': n, 'before': '](images/', 'after': '](' + IMAGE_PREFIX})

    def render(self):
        out, pos = [], 0
        for s, e, r in sorted(self.edits, key=lambda x: (x[0], x[1])):
            if s < pos:
                raise RuntimeError(f'overlapping edits at {s} in {self.art["path"]}')
            out.append(self.src[pos:s])
            out.append(r)
            pos = e
        out.append(self.src[pos:])
        return ''.join(out)


# ------------------------------------------------------------------ TOC ---
# The printed TOC (PDF pages 6-10) sets each chapter's numbered entries in two
# side-by-side columns under a centred heading. The registered conversion read
# those pages as two page-high columns, interleaving chapter halves and
# detaching headings from their entries. These helpers read span positions
# instead: each entry row is assigned to the nearest heading above it, and the
# left column is read before the right column.

TOC_HEAD_SIZE = 14.0   # chapter headings are set at 16 pt, entries at 8 pt
TOC_RUNNING_HEAD_Y = 40.0  # 'ADVANCED SQUAD LEADER RULEBOOK TABLE OF CONTENTS'
TOC_FOLIO_Y = 750.0        # roman page number
TOC_ROW_TOL = 2.5
TOC_GAP = 1.0             # pt; spans closer than this are one word
TOC_NUM_RE = re.compile(r'^(\d+)\.$')
TOC_BULLET = '•'

# Chapter banner icons in the registered image set, identified by visual
# inspection (the converter did not number them in page order).
TOC_ICONS = {
    'A': 'eASLRB_v3_01-p6-1.png', 'B': 'eASLRB_v3_01-p6-2.png', 'C': 'eASLRB_v3_01-p6-3.png',
    'D': 'eASLRB_v3_01-p7-1.png', 'E': 'eASLRB_v3_01-p7-2.png', 'F': 'eASLRB_v3_01-p7-4.png',
    'G': 'eASLRB_v3_01-p7-3.png', 'H': 'eASLRB_v3_01-p8-1.png', 'I': 'eASLRB_v3_01-p8-6.png',
    'J': 'eASLRB_v3_01-p8-2.png', 'K': 'eASLRB_v3_01-p8-3.png', 'L': 'eASLRB_v3_01-p8-4.png',
    'M': 'eASLRB_v3_01-p8-5.png', 'N': 'eASLRB_v3_01-p9-1.png', 'O': 'eASLRB_v3_01-p9-2.png',
    'P': 'eASLRB_v3_01-p9-3.png', 'Q': 'eASLRB_v3_01-p9-4.png', 'R': 'eASLRB_v3_01-p9-5.png',
    'S': 'eASLRB_v3_01-p10-1.png', 'T': 'eASLRB_v3_01-p10-2.png', 'W': 'eASLRB_v3_01-p10-3.png',
    'Z': 'eASLRB_v3_01-p10-4.png',
}


def toc_spans(page):
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
                if y1 <= TOC_RUNNING_HEAD_Y or y0 >= TOC_FOLIO_Y:
                    continue
                out.append({'x0': s['bbox'][0], 'x1': s['bbox'][2], 'y0': y0, 'y1': y1,
                            'size': s['size'], 'text': t})
    return out


def toc_rows(spans):
    """Group spans whose tops align into rows, left to right."""
    rows = []
    for s in sorted(spans, key=lambda s: (s['y0'], s['x0'])):
        if rows and abs(rows[-1][0]['y0'] - s['y0']) <= TOC_ROW_TOL:
            rows[-1].append(s)
        else:
            rows.append([s])
    return [sorted(r, key=lambda s: s['x0']) for r in rows]


def toc_join(spans):
    """Concatenate spans, adding a space where the PDF leaves a visible gap."""
    out, prev = '', None
    for s in spans:
        if prev is not None and s['x0'] - prev['x1'] > TOC_GAP and out[-1:] != ' ' and s['text'][:1] != ' ':
            out += ' '
        out += s['text']
        prev = s
    return re.sub(r'\s+', ' ', out).strip()


def toc_extract(doc, first=6, last=10):
    """Return chapters in printed order and anything that could not be placed."""
    chapters, stray = [], []
    for p in range(first, last + 1):
        page = doc[p - 1]
        mid = page.rect.width / 2
        page_chapters = []
        body = []
        for row in toc_rows(toc_spans(page)):
            heads = [s for s in row if s['size'] >= TOC_HEAD_SIZE]
            if heads:
                page_chapters.append({'heading': toc_join(heads), 'page': p, 'y': heads[0]['y0'],
                                      'cols': {'L': [], 'R': []}, 'para': []})
                row = [s for s in row if s['size'] < TOC_HEAD_SIZE]
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
                for row in toc_rows(ch['cols'][col]):
                    head = row[0]['text'].strip()
                    m = TOC_NUM_RE.match(head)
                    if m:
                        entries.append({'n': int(m.group(1)), 'text': toc_join(row[1:]), 'sub': []})
                    elif head == TOC_BULLET and entries:
                        entries[-1]['sub'].append(toc_join(row[1:]))
                    else:
                        ch['para'].append(toc_join(row))
            ch['entries'] = entries
            del ch['cols']
        chapters.extend(page_chapters)
    return chapters, stray


def toc_render(chapters, image_prefix, title):
    out = [f'**{title}**', '']
    page = None
    for ch in chapters:
        if ch['page'] != page:
            page = ch['page']
            out += [f'<!-- pdf-page {page} -->', '']
        letter = ch['heading'].split('.')[0] if re.match(r'^[A-Z]\.', ch['heading']) else None
        if letter in TOC_ICONS:
            out += [f'![Figure from page {page}]({image_prefix}{TOC_ICONS[letter]})', '']
        out += [f'## {ch["heading"]}', '']
        if ch['para']:
            out += [' '.join(ch['para']), '']
        if ch['entries']:
            for e in ch['entries']:
                out.append(f'{e["n"]}. {e["text"]}')
                out += [f'   - {s}' for s in e['sub']]
            out.append('')
    return '\n'.join(out)


def toc_words(text):
    return collections.Counter(re.findall(r'[A-Za-z0-9]+', text))


def toc_check(chapters, stray):
    """Structural problems that must go to review rather than be guessed."""
    problems = [f'text on page {s["page"]} above the first heading: {s["text"]!r}' for s in stray]
    for ch in chapters:
        nums = [e['n'] for e in ch['entries']]
        if nums and nums != list(range(1, len(nums) + 1)):
            problems.append(f'{ch["heading"]}: entry numbers {nums} are not 1..{len(nums)}')
    return problems


TOC_TITLE = 'ADVANCED SQUAD LEADER RULEBOOK TABLE OF CONTENTS'
CAMEL_RE = re.compile(r'\b[A-Za-z][a-z]{2,}[A-Z][a-z]{2,}\b')
# registered chapter-banner crops with visible defects (observations only)
ICON_NOTES = {
    'eASLRB_v3_01-p6-3.png': 'crop includes the top of the C heading',
    'eASLRB_v3_01-p7-4.png': 'crop includes the top of the F heading',
    'eASLRB_v3_01-p9-1.png': 'crop includes the top of the N heading',
    'eASLRB_v3_01-p10-3.png': 'crop has the emblem only, without the banner frame',
}


def build_toc(art, base, pdf, decisions, prefix):
    """Rebuild the TOC from PDF span positions (rule toc-rebuild).

    The generic invariant cannot hold here, because entries move. Instead the
    build fails closed unless the edition carries exactly the words printed on
    the TOC pages and every image the conversion referenced.
    """
    ledger, review, used = [], [], set()

    def lid():
        return f'{prefix}-{len(ledger) + 1:05d}'

    chapters, stray = toc_extract(pdf.doc, art['startPage'], art['endPage'])
    text = toc_render(chapters, IMAGE_PREFIX, TOC_TITLE) + '\n'

    pdf_words = collections.Counter()
    for p in range(art['startPage'], art['endPage'] + 1):
        for sp in toc_spans(pdf.doc[p - 1]):
            pdf_words.update(toc_words(sp['text']))
    body = re.sub(r'<!--.*?-->|!\[[^\]]*\]\([^)]*\)', '', text, flags=re.S).replace(f'**{TOC_TITLE}**', '')
    if toc_words(body) != pdf_words:
        diff = (toc_words(body) - pdf_words) + (pdf_words - toc_words(body))
        sys.exit(f'{art["path"]}: rebuilt TOC words differ from the PDF: {dict(diff)}')
    base_images = set(re.findall(r'eASLRB_v3_01-p\d+-\d+\.png', base))
    new_images = set(re.findall(r'eASLRB_v3_01-p\d+-\d+\.png', text))
    if base_images != new_images:
        sys.exit(f'{art["path"]}: image set changed: {sorted(base_images ^ new_images)}')

    base_body = re.sub(r'<!--.*?-->|!\[[^\]]*\]\([^)]*\)', '', base, flags=re.S).replace(f'**{TOC_TITLE}**', '')
    missing = pdf_words - toc_words(base_body)
    extra = toc_words(base_body) - pdf_words
    for ch in chapters:
        letter = ch['heading'].split('.')[0] if re.match(r'^[A-Z]\.', ch['heading']) else None
        ledger.append({'id': lid(), 'rule': 'toc-rebuild', 'class': 'conversion-fix',
                       'pdfPage': ch['page'], 'heading': ch['heading'],
                       'entries': len(ch['entries']),
                       'subEntries': sum(len(e['sub']) for e in ch['entries']),
                       'paragraph': bool(ch['para']),
                       'icon': TOC_ICONS.get(letter),
                       'basis': 'entries assigned to the nearest heading above them; '
                                'left column read before right column'})
    ledger.append({'id': lid(), 'rule': 'toc-rebuild', 'class': 'observation',
                   'note': 'word comparison of the registered conversion with the PDF TOC pages',
                   'wordsMissingFromConversion': dict(sorted(missing.items())),
                   'wordsExtraInConversion': dict(sorted(extra.items()))})
    for img, note in ICON_NOTES.items():
        ledger.append({'id': lid(), 'rule': 'image-crop', 'class': 'observation',
                       'image': img, 'note': note})

    problems = toc_check(chapters, stray)
    for msg in problems:
        review.append({'id': f'{prefix}-R{len(review) + 1:04d}', 'rule': 'toc-structure',
                       'baseLine': 0, 'text': '', 'reason': msg})
    for ch in chapters:
        for e in ch['entries']:
            for m in CAMEL_RE.finditer(e['text']):
                key = ('suspected-source-typo', 0, m.group())
                dec = decisions.get(key)
                entry = {'rule': 'suspected-source-typo', 'baseLine': 0, 'text': m.group(),
                         'pdfPage': ch['page'], 'context': f'{ch["heading"]} {e["n"]}. {e["text"]}'}
                if dec is not None:
                    used.add(id(dec))
                    ledger.append({'id': lid(), 'class': 'reviewed-decision', **entry,
                                   'decision': dec['decision'], 'decidedBy': dec['decidedBy'],
                                   'rationale': dec['rationale']})
                    continue
                review.append({'id': f'{prefix}-R{len(review) + 1:04d}', **entry,
                               'reason': 'words run together as printed in the PDF (no glyph gap); '
                                         'kept as printed, a change needs a cited authority'})
    stale = [d for d in decisions.values() if id(d) not in used]
    if stale:
        sys.exit(f'{art["path"]}: decisions match no review item: '
                 + ', '.join(f'{d["text"]!r}' for d in stale))
    return text, ledger, review


def load_decisions(path):
    """Reviewer decisions; each must name its reviewer and rationale.

    `join`, `retain` and `replace` are accepted; a replacement may differ from
    the base only in hyphens, emphasis markers and whitespace.
    Any decision whose class is `source-correction` must cite an authority
    (for example an official errata document); none is supported yet.
    """
    if not os.path.exists(path):
        return []
    doc = json.load(open(path, encoding='utf-8'))
    out = []
    for d in doc.get('decisions', []):
        for field in ('sourceId', 'rule', 'baseLine', 'text', 'decision', 'decidedBy', 'rationale'):
            if not d.get(field) and d.get(field) != 0:
                sys.exit(f'decision {d} lacks {field}')
        if d['decision'] not in ('join', 'retain', 'replace'):
            sys.exit(f'decision {d["decision"]!r} is not supported: {d}')
        if d['decision'] == 'replace' and not d.get('replacement'):
            sys.exit(f'replace decision lacks a replacement: {d}')
        if d.get('class') == 'source-correction' and not d.get('authority'):
            sys.exit(f'source-correction decision lacks a cited authority: {d}')
        out.append(d)
    return out


def write_outputs(out, art, text, ledger, review, manifest):
    name = os.path.basename(art['path'])
    stem = os.path.splitext(name)[0]
    with open(os.path.join(out, 'edition', name), 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)
    ledger_doc = {'sourceId': art['sourceId'], 'basePath': art['path'], 'baseSha256': art['sha256'],
                  'entries': ledger}
    review_doc = {'sourceId': art['sourceId'], 'basePath': art['path'], 'items': review}
    for sub, doc in (('ledger', ledger_doc), ('review', review_doc)):
        with open(os.path.join(out, sub, stem + '.json'), 'w', encoding='utf-8', newline='\n') as f:
            json.dump(doc, f, ensure_ascii=False, indent=1)
            f.write('\n')
    counts = collections.Counter(e['rule'] for e in ledger)
    rcounts = collections.Counter(e['rule'] for e in review)
    manifest['files'].append({
        'sourceId': art['sourceId'],
        'basePath': art['path'], 'baseSha256': art['sha256'],
        'editionPath': f'{OUT_DIR}/edition/{name}', 'editionSha256': sha256_text(text),
        'pdfPages': [art['startPage'], art['endPage']],
        'ledgerCounts': dict(sorted(counts.items())),
        'reviewCounts': dict(sorted(rcounts.items())),
        'certifiedSections': [],
    })
    print(f'{name}: {dict(counts)} review {dict(rcounts)}')


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--pdf', required=True, help='path to eASLRB_v3_01.pdf')
    ap.add_argument('--root', default='.', help='repository root')
    args = ap.parse_args()
    root = os.path.abspath(args.root)

    pdf_sha = sha256_file(args.pdf)
    if pdf_sha != PDF_SHA256:
        sys.exit(f'PDF digest {pdf_sha} does not match pinned {PDF_SHA256}')
    registry = json.load(open(os.path.join(root, REGISTRY), encoding='utf-8'))
    arts = [a for a in registry['artifacts'] if a['kind'] == 'markdown']
    first = min(a['startPage'] for a in arts)
    last = max(a['endPage'] for a in arts)
    pdf = PdfEvidence(args.pdf, first, last)
    decisions = load_decisions(os.path.join(root, DECISIONS))

    out = os.path.join(root, OUT_DIR)
    for sub in ('edition', 'ledger', 'review'):
        os.makedirs(os.path.join(out, sub), exist_ok=True)

    tool_path = os.path.relpath(os.path.abspath(__file__), root).replace('\\', '/')
    manifest = {
        'schemaVersion': '1.0.0',
        'editionId': EDITION_ID,
        'editionStatus': 'draft-uncertified',
        'edition': registry['edition'],
        'baseRegistry': {'path': REGISTRY, 'registryId': registry['registryId'],
                         'sha256': sha256_file(os.path.join(root, REGISTRY))},
        'pdf': {'fileName': os.path.basename(args.pdf), 'sha256': pdf_sha,
                'pageCount': pdf.doc.page_count, 'evidencePages': [first, last]},
        'tool': {'path': tool_path, 'version': TOOL_VERSION,
                 # LF-normalized so a CRLF checkout reports the committed digest
                 'sha256': hashlib.sha256(open(os.path.abspath(__file__), 'rb').read()
                                          .replace(b'\r\n', b'\n')).hexdigest(),
                 'pymupdf': pymupdf.VersionBind},
        'files': [],
    }
    for n, art in enumerate(arts):
        base_path = os.path.join(root, art['path'])
        # registered digests cover the committed LF bytes; a Windows checkout may use CRLF
        raw = open(base_path, 'rb').read().replace(b'\r\n', b'\n')
        if hashlib.sha256(raw).hexdigest() != art['sha256']:
            sys.exit(f'{art["path"]} does not match its registered digest')
        base = raw.decode('utf-8')
        prefix = art['sourceId'].split(':')[1].upper().replace('CHAPTER-', 'CH')
        mine = {(d['rule'], d['baseLine'], d['text']): d
                for d in decisions if d['sourceId'] == art['sourceId']}
        if art['sourceId'].endswith(':contents'):
            text, ledger, review = build_toc(art, base, pdf, mine, prefix)
            write_outputs(out, art, text, ledger, review, manifest)
            continue
        fb = FileBuild(art, base, pdf, prefix, mine)
        fb.place_pages()
        fb.resolve_hyphens()
        fb.merge_emphasis()
        fb.flag_glyphs()
        fb.rebase_images()
        text = fb.render()
        stale = [d for d in mine.values() if id(d) not in fb.used]
        if stale:
            sys.exit(f'{art["path"]}: decisions match no review item: '
                     + ', '.join(f'{d["text"]!r}@{d["baseLine"]}' for d in stale))
        # fail closed: only hyphens, emphasis markers, whitespace and comments may differ
        if invariant_form(text) != invariant_form(base):
            sys.exit(f'{art["path"]}: edition differs from base beyond the permitted change classes')
        uncommented = [re.sub(r'<!--.*?-->', '', x, flags=re.S) for x in (base, text)]
        removed = uncommented[0].count('-') - uncommented[1].count('-')
        ledgered = sum(1 for e in fb.ledger
                       if e['rule'] in ('linebreak-hyphen', 'stale-hyphen') and 'result' in e
                       and e.get('decision') != 'replace')
        if removed != ledgered:
            sys.exit(f'{art["path"]}: {removed} hyphens removed but {ledgered} ledgered')

        write_outputs(out, art, text, fb.ledger, fb.review, manifest)
    with open(os.path.join(out, 'manifest.json'), 'w', encoding='utf-8', newline='\n') as f:
        json.dump(manifest, f, ensure_ascii=False, indent=1)
        f.write('\n')


if __name__ == '__main__':
    main()
