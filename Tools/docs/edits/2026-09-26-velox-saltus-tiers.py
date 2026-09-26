"""One-off tier 3/5/6 and plan updates for the Velox/Saltus change (2026-09-26)."""
import os
import re
import subprocess

os.chdir(os.path.join(os.path.dirname(__file__), '..', '..', '..'))


def sub(p, old, new):
    s = open(p, encoding='utf-8').read()
    assert s.count(old) == 1, (p, old[:70], s.count(old))
    open(p, 'w', encoding='utf-8', newline='').write(s.replace(old, new))


p = 'docs/6-decisions/Decisions.md'
s = open(p, encoding='utf-8').read().rstrip('\n')
s += '''

## 2026-09-26 — Velox and Saltus replace Tonitrus and Cadaver Surge; no dodge key

**Context.** The owner asked for a dodge spell and a high jump spell in place of Tonitrus (a
thunderclap that stunned everything near the aim point) and Cadaver Surge (which only reported the
corpse it would raise; there was no necromancy behind it). #152 had asked for the jump-and-slam to
be a real spell instead of an accident of riding a held item.

**Decision.** Velox ("swift") dashes about 3.5 m and is the only dodge: the Ctrl dodge key is gone.
Saltus ("a leap") launches about 4 m straight up from the ground, and jump in the air then slams
down, hurting and shoving everything within 3 m of the landing. They take keys 5 and 7, so the
other six keys keep their places. Tonitrus and Cadaver Surge are deleted with their misfires,
assets and tuning; their ids (5, 7, 103, 105) are retired, not reused, because ids are stored in
assets and sent over the network. The words, the dodge key's removal, the slam and the deletion
are the owner's choices; the misfires, mana costs, dash length, launch height and slam numbers are
this change's defaults, all in `SpellTuning.asset`.

**Why.** Moving the caster runs on the caster's own machine, not the server like every other
effect, because that is where a player's body is simulated: moved on the server it would miss a
remote player, and doing both would move a host twice (`ICasterMovementSpell`). The slam's damage
still resolves on the server. The slam is on the jump key rather than attack because attack is
`G`, awkward in mid-air, and jump in the air did nothing before. Making the dodge a spell gives it a
cost, so getting out of the way is a choice; it also freed Ctrl, which is the whisper modifier.
Building it found that the old dodge never moved at all (see `docs/4-systems/spells.md`, Traps).
How it works: `docs/4-systems/spells.md`, "Velox and Saltus".

**Status.** Standing.
'''
open(p, 'w', encoding='utf-8', newline='').write(s + '\n')

p = 'docs/3-state/ProjectState.md'
sub(p, '''The code's namespaces and assemblies are `Plunderspell.*`; the old `RogueAi` names are
gone (Decisions, 2026-09-26).''', '''The code's namespaces and assemblies are `Plunderspell.*`; the old `RogueAi` names are
gone (Decisions, 2026-09-26). Velox (a dash, the only dodge) and Saltus (a high jump that the jump
key turns into a slam) replace Tonitrus and Cadaver Surge on keys 5 and 7, checked in a raid in the
Editor; neither word has been tried with a real voice (`docs/4-systems/spells.md`, "Velox and
Saltus").''')

p = 'docs/5-today/Today.md'
sub(p, '''Tests before those two:''', '''Then, at the owner's request: GitHub milestones M0-M7 created and every open issue assigned
(`Tools/github/sync_milestones.py`, run twice, the second time updating in place), #24 and #159
closed, the Tripod Cauldron back to 12 kg. And Velox (dash) and Saltus (high jump and slam)
replace Tonitrus and Cadaver Surge, with the dodge key removed
(`docs/plans/dodge-and-leap-spells.md`). A delegated agent stopped after two files and left the
project not compiling; the rest was finished here. Building it found that the dodge never moved
(it ended on its first physics step); fixed. Tests: PlayMode 228 of 229 (the known guard test);
EditMode 69 pass, 3 skip, 2 fail: the known `ArtAssetImportTests`, and `LootBalanceTests`, which
fails on the owner's uncommitted Rolled Tapestry edit (12 kg in the Late Medieval outer bailey).
In a solo raid in the Editor: Velox dashed 3.9 m, Saltus rose 4.0 m, the slam took a Lantern
Warden 1.8 m away from 80 to 56.9 health, both misfires did their misfire and spent mana, and Ctrl
no longer moves the player. Captures: `docs/generated/dodge-and-leap-2026-09-26/`. Not tried: the
two words spoken by a real person; two machines.

Tests before those two:''')

p = 'docs/plans/dodge-and-leap-spells.md'
sub(p, '''**Status: approved by the owner 2026-09-26, not built.**''', '''**Status: built 2026-09-26** (see `docs/4-systems/spells.md`, "Velox and Saltus", for what shipped
and where it differs from this plan: the dash holds its speed for 0.25 s rather than reusing the
old impulse, which never moved; the launch is 14 m/s, not the 9 m/s first tried, because the
player falls at 2.5 g; the slam's noise is as loud as the old thunderclap's).''')

h = subprocess.run(['git', 'hash-object', 'docs/3-state/ProjectState.md'], capture_output=True, text=True).stdout.strip()
q = 'docs/plain/3-state/ProjectState.md'
t = open(q, encoding='utf-8').read()
open(q, 'w', encoding='utf-8', newline='').write(re.sub(r'@ [0-9a-f]{40} -->', '@ ' + h + ' -->', t, count=1))
print('ok')
