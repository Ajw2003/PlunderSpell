"""Checks the art docs still agree with the art data and the code, so they cannot drift quietly.

Usage: python Tools/docs/check_art_docs.py
Exit code 1, with every disagreement listed, when:
  - an enemy in docs/art/data/*.json or EnemyPrefabForge.Specs is missing from docs/4-systems/scale.md;
  - scale.md's roster tables name an enemy neither source has;
  - scale.md gives an art-bible enemy a height other than its JSON height_m;
  - a repo path written in backticks in docs/art/WORKFLOW.md, or in a doc it links to, does not exist.
Run it whenever art data, a forge or one of those docs changes; CI runs it (.github/workflows/docs.yml).
"""
import glob
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SCALE = os.path.join(ROOT, 'docs', '4-systems', 'scale.md')
WORKFLOW = os.path.join(ROOT, 'docs', 'art', 'WORKFLOW.md')
FORGE = os.path.join(ROOT, 'Assets', '_Project', 'Scripts', 'Editor', 'EnemyPrefabForge.cs')

# A backticked token is checked as a path only when it clearly is one: it starts at a known top
# folder and has no wildcard or placeholder in it.
TOP_FOLDERS = ('docs/', 'Tools/', 'Assets/', 'Packages/', '.github/')
PLACEHOLDER = re.compile(r'[<>*{}$]|\.\.\.|…')


def read(path):
    with open(path, encoding='utf-8') as handle:
        return handle.read()


def art_bible_enemies():
    """{name: height_m} from every Age's JSON."""
    out = {}
    for path in sorted(glob.glob(os.path.join(ROOT, 'docs', 'art', 'data', '*.json'))):
        for enemy in json.loads(read(path)).get('enemies', []):
            out[enemy['name']] = float(enemy['height_m'])
    return out


def forge_enemies():
    """{id: height} from EnemyPrefabForge.Specs."""
    return {m.group(1): float(m.group(2))
            for m in re.finditer(r'new EnemySpec\("(\w+)",\s*([\d.]+)f', read(FORGE))}


def scale_rows():
    """{name: height} from every table row in scale.md's "## Enemies" section, plus the names
    written in backticks in its prose (the retired household four)."""
    text = read(SCALE)
    section = text[text.index('## Enemies'):text.index('## Invariants')]
    rows = {}
    for line in section.splitlines():
        cells = [c.strip() for c in line.strip().strip('|').split('|')]
        if len(cells) < 2 or cells[0] in ('Age', 'Enemy') or set(cells[0]) <= set('-: '):
            continue
        name_cell, height_cell = (cells[1], cells[2]) if len(cells) >= 4 else (cells[0], cells[1])
        height = re.match(r'([\d.]+)\s*m', height_cell)
        if height:
            rows[name_cell] = float(height.group(1))
    prose = set(re.findall(r'`(\w+)`\s+[\d.]+\s*m', section))
    return rows, prose


def linked_docs():
    """WORKFLOW.md and every .md it links to with a relative link."""
    docs = [WORKFLOW]
    for target in re.findall(r'\]\(([^)\s#]+\.md)', read(WORKFLOW)):
        path = os.path.normpath(os.path.join(os.path.dirname(WORKFLOW), target))
        if os.path.isfile(path):
            docs.append(path)
    return docs


def missing_paths(doc):
    problems = []
    for token in re.findall(r'`([^`\n]+)`', read(doc)):
        token = token.strip().split(' ')[0].rstrip(':,.')
        if not token.startswith(TOP_FOLDERS) or PLACEHOLDER.search(token):
            continue
        if not os.path.exists(os.path.join(ROOT, token)):
            problems.append(f'{os.path.relpath(doc, ROOT)}: names `{token}`, which does not exist')
    return problems


def main():
    problems = []
    bible = art_bible_enemies()
    forge = forge_enemies()
    rows, prose = scale_rows()
    documented = set(rows) | prose

    for name in sorted(set(bible) | set(forge)):
        if name not in documented:
            problems.append(f'scale.md: enemy `{name}` exists in the data or the forge but is not in the roster')
    for name in sorted(documented - set(bible) - set(forge)):
        problems.append(f'scale.md: enemy `{name}` is listed but neither the art data nor EnemyPrefabForge has it')
    for name, height in sorted(bible.items()):
        if name in rows and abs(rows[name] - height) > 0.005:
            problems.append(f'scale.md: `{name}` is {rows[name]} m, its JSON height_m is {height} m')
    for name, height in sorted(forge.items()):
        if name in rows and abs(rows[name] - height) > 0.005:
            problems.append(f'scale.md: `{name}` is {rows[name]} m, EnemyPrefabForge.Specs says {height} m')

    for doc in linked_docs():
        problems += missing_paths(doc)

    for problem in problems:
        print(problem)
    print(f'art doc disagreements: {len(problems)} '
          f'({len(bible)} art-bible enemies, {len(forge)} forge enemies, {len(linked_docs())} docs checked)')
    return 1 if problems else 0


if __name__ == '__main__':
    sys.exit(main())
