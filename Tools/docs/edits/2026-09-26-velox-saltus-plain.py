"""One-off plain-copy refresh for the Velox/Saltus change (2026-09-26). Stamps each plain copy with
its source's current blob hash, so run it after the sources' final edit."""
import os
import re
import subprocess

os.chdir(os.path.join(os.path.dirname(__file__), '..', '..', '..'))


def sub(p, old, new):
    s = open(p, encoding='utf-8').read()
    assert s.count(old) == 1, (p, old[:70], s.count(old))
    open(p, 'w', encoding='utf-8', newline='').write(s.replace(old, new))


p = 'docs/plain/4-systems/spells.md'
old = '''7. Casting by keyboard chants for a short moment before firing, so it is a slight disadvantage
   compared to speaking, never the fastest way to cast.
'''
if old in open(p, encoding='utf-8').read():
    sub(p, old, old + '''8. Two spells move the caster: one is a quick dash, the game's only dodge, and the other is a
   high jump that, with a second press of jump in mid-air, turns into a slam that hurts and
   knocks back everything around the landing. Moving the caster happens on their own machine;
   the damage a slam does is still worked out centrally.
''')
old = '''- **A visible spell effect swallowing the caster's own camera.**'''
if '**A dodge that went nowhere.**' not in open(p, encoding='utf-8').read():
    sub(p, old, '''- **A dodge that went nowhere.** The dash now keeps its speed for its whole length; it used to
  stop on its very first moment, before it had moved at all.
''' + old)

for plain, source in [('docs/plain/4-systems/spells.md', 'docs/4-systems/spells.md'),
                      ('docs/plain/4-systems/raid.md', 'docs/4-systems/raid.md')]:
    h = subprocess.run(['git', 'hash-object', source], capture_output=True, text=True).stdout.strip()
    s = open(plain, encoding='utf-8').read()
    open(plain, 'w', encoding='utf-8', newline='').write(re.sub(r'@ [0-9a-f]{40} -->', '@ ' + h + ' -->', s, count=1))
print('ok')
