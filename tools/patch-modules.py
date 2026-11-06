#!/usr/bin/env python3
"""Keeps every JSON patch entry conditioned on the module switch that owns it.

  patch-modules.py --check   fail if any entry's condition differs from the map below
  patch-modules.py --fix     insert missing conditions as text (never reformats a file)

The map is the source of truth for which switch gates which patch. A file that is not in
the map fails --check, so new patches have to be classified.
"""
import json, re, sys, glob, os

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Rudiments', 'assets', 'rudiments', 'patches')
ALWAYS = None  # entry registers types a save may contain, or is already conditioned

# file -> key for every entry, or {index: key, '*': default}
MAP = {
    'barrel-retting.json': 'Fibre',
    'bow-durabilitybehavior.json': 'Fibre',
    'clayworks-retting.json': 'Fibre',
    'linseedoil-fruitpress.json': 'Fibre',
    'stringsense-compat.json': 'Fibre',
    'quality-behaviors.json': {0: 'Fibre', 1: 'Fibre', 2: 'ReedSpread', 3: ALWAYS},
    'crop-flax.json': {0: 'Fibre', 1: 'flax:bloom', 2: 'flax:legacy', 3: 'Fibre'},
    'spinning-compat.json': {0: 'Nettle', 1: 'Fibre'},
    'cooking-nettle.json': 'Nettle',
    'nettle-worldgen.json': 'Nettle!',
    'wool-carding.json': {0: ALWAYS, 1: ALWAYS, '*': 'WoolCarding'},
    'player-carding-anim.json': 'WoolCarding',
    'crop-seeds.json': {17: 'SeedsOnlyWhenMature', '*': 'SeedsOnlyWhenMature'},
    'ware-fragility.json': 'Ware',
    'ware-tierkeep.json': 'Ware',
    'ware-seepage.json': {0: 'ClaySeepage', 1: 'Ware', 2: 'ClaySeepage', 3: 'Ware'},
    'lead-poisoning.json': {0: 'Glazes', '*': 'Ware'},
    'wateringcan-class.json': 'Ware',
    'compat/clayworks-behaviors.json': 'Ware',
    'compat/clayworks-wateringcan.json': 'Ware',
    'compat/clayworks-seepage.json': 'ClaySeepage',
    'ware-beehive.json': 'WareTiers',
    'compat/clayworks-ware.json': 'WareTiers',
    'ware-dropbreak.json': 'ClayBreakage',
    'ware-dropexempt.json': 'ClayBreakage',
    'ware-groundstorage-wear.json': 'ClayBreakage',
    'ware-firepit-wear.json': 'ClayBreakage',
    'crucible-smelting-failure.json': 'ClayBreakage',
    'ingotmold-tighten.json': 'ClayBreakage',
    'compat/structural-dropexempt.json': 'ClayBreakage',
    'compat/fitnshard-clay-recipes.json': {5: 'ClayBreakage', '*': ALWAYS},
    'handbook-fireclay.json': 'Porcelain',
    'glaze-applicator.json': 'Glazes',
    'glaze-rawware-class.json': 'Glazes',
    'compat/clayworks-glaze.json': 'Glazes',
    'glaze-salt.json': 'SaltGlaze',
    # Variants and types that exist whether or not their group is on:
    'porcelain-clay.json': ALWAYS,
    'porcelain-ware-raw.json': ALWAYS,
    'porcelain-ware-fired.json': ALWAYS,
    'compat/clayworks-porcelain-wet.json': ALWAYS,
    'crucible-crackopen.json': ALWAYS,
}


def want(rel, i):
    m = MAP[rel]
    if not isinstance(m, dict):
        return m
    return m[i] if i in m else m['*']


def expected(key):
    if key is None:
        return None
    if key.startswith('flax:'):
        return ('Rudiments.FlaxHarvest', key[5:])
    if key.endswith('!'):
        return ('Rudiments.' + key[:-1], 'false')
    return ('Rudiments.' + key, 'true')


def entry_spans(text):
    """(start, end) offsets of each top-level object in a JSON array, string-aware."""
    spans, depth, instr, esc, start = [], 0, False, False, None
    for i, c in enumerate(text):
        if instr:
            esc = (c == '\\') and not esc
            if c == '"' and not esc:
                instr = False
            continue
        if c == '"':
            instr = True
        elif c in '{[':
            depth += 1
            if c == '{' and depth == 2:
                start = i
        elif c in '}]':
            if c == '}' and depth == 2:
                spans.append((start, i))
            depth -= 1
    return spans


def main(fix):
    files = sorted(glob.glob(os.path.join(ROOT, '**', '*.json'), recursive=True))
    problems, fixed = [], 0
    for path in files:
        rel = os.path.relpath(path, ROOT).replace(os.sep, '/')
        text = open(path, encoding='utf-8').read()
        entries = json.loads(text)
        if rel not in MAP:
            problems.append(f'{rel}: not in tools/patch-modules.py MAP')
            continue
        spans = entry_spans(text)
        assert len(spans) == len(entries), rel
        inserts = []
        for i, entry in enumerate(entries):
            exp = expected(want(rel, i))
            cond = entry.get('condition')
            have = (cond['when'], cond['isValue']) if cond else None
            if exp is None:
                # Always-on entries may carry a legacy condition only if it is not ours.
                if have and have[0].startswith('Rudiments.') and rel != 'crop-seeds.json':
                    problems.append(f'{rel} #{i}: always-on entry has condition {have}')
                continue
            if have == exp:
                continue
            if have is not None:
                problems.append(f'{rel} #{i}: condition {have}, expected {exp}')
                continue
            if not fix:
                problems.append(f'{rel} #{i}: missing condition {exp}')
                continue
            inserts.append((spans[i][0], exp))
        if inserts:
            for pos, (when, val) in sorted(inserts, reverse=True):
                after = text[pos + 1:]
                cond = f'"condition": {{ "when": "{when}", "isValue": "{val}" }},'
                m = re.match(r'[ \t]*\n([ \t]*)\S', after)
                if m:
                    text = text[:pos + 1] + '\n' + m.group(1) + cond + text[pos + 1:]
                else:
                    text = text[:pos + 1] + ' ' + cond + text[pos + 1:]
            before = entries
            after_entries = json.loads(text)
            for b, a in zip(before, after_entries):
                a2 = dict(a); a2.pop('condition', None)
                b2 = dict(b); b2.pop('condition', None)
                assert a2 == b2, f'{rel}: fix changed more than conditions'
            open(path, 'w', encoding='utf-8', newline='').write(text)
            fixed += len(inserts)
    for rel in MAP:
        if not os.path.exists(os.path.join(ROOT, rel)):
            problems.append(f'{rel}: in MAP but file is missing')
    if fix:
        print(f'inserted {fixed} conditions')
        return main(False) if not problems else 1
    for p in problems:
        print('  ' + p)
    print('patch-modules: ' + ('FAILED' if problems else 'ok'))
    return 1 if problems else 0


if __name__ == '__main__':
    if len(sys.argv) != 2 or sys.argv[1] not in ('--check', '--fix'):
        sys.exit(__doc__)
    sys.exit(main(sys.argv[1] == '--fix'))
