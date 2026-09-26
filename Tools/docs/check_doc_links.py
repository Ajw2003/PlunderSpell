"""Checks every relative Markdown link in docs/ (and the root CLAUDE.md) points at a file that exists.

Usage: python Tools/docs/check_doc_links.py [--list]
Prints the number of broken links, and each one with --list. docs/generated/ is skipped: it holds
tool output kept as a record, not hand-maintained docs. Exit code 1 when anything is broken.
"""
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
LINK = re.compile(r'\]\(([^)\s#]+)(#[^)]*)?\)')


def markdown_files():
    yield os.path.join(ROOT, 'CLAUDE.md')
    for folder, _, names in os.walk(os.path.join(ROOT, 'docs')):
        if os.sep + 'generated' in folder:
            continue
        for name in names:
            if name.endswith('.md'):
                yield os.path.join(folder, name)


def broken_links():
    for path in markdown_files():
        if not os.path.isfile(path):
            continue
        text = open(path, encoding='utf-8').read()
        for match in LINK.finditer(text):
            target = match.group(1)
            if re.match(r'^[a-z]+:', target) or target.startswith('/'):
                continue
            resolved = os.path.normpath(os.path.join(os.path.dirname(path), target))
            if not os.path.exists(resolved):
                yield os.path.relpath(path, ROOT), target


if __name__ == '__main__':
    found = list(broken_links())
    if '--list' in sys.argv:
        for source, target in found:
            print(f'{source}: {target}')
    print(f'broken relative links: {len(found)}')
    sys.exit(1 if found else 0)
