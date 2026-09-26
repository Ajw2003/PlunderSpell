# Roadmap

Milestones as defined in the pitch (`docs/plunderspell.md` §11) and the engineering plan
(`docs/plans/plunderspell.md`). Percentages and current status live in `docs/3-state/ProjectState.md`;
this document only defines what "done" means for each one.

## M0 — Fork clean, cut gravity

Branch from the real trunk (`claude/steam-multiplayer-framework-xia7ch`), delete the planetary
`Gravity` assembly, restore world gravity across the player states and items, confirm nothing
else moved.

**Contains:** removing `Assets/_Project/Scripts/Runtime/Gravity/` and its ~3 consumers; restoring
standard −Y gravity in the player FSM and item impact path.

**Acceptance:** crates and players fall along −Y and land flat; thrown items still deal
velocity-scaled damage; all seven runtime assemblies compile with zero remaining references to
the gravity module.

## M1 — Prove the voice

A bare grey room and four spell words, nothing else. The riskiest assumption in the project —
whether shouting at your own computer feels like power or embarrassment — answered as early as
possible.

**Contains:** `IVoiceInputService`, the Vosk provider, the keyboard mock, `SpellLexicon`, the
misfire table.

**Acceptance:** >90% top-1 recognition across four accents on the 40-word lexicon, under 150 ms
from word-end to effect, and misfires that land as jokes rather than frustration. This requires
a real microphone and real speakers of different accents — a unit test against the mock provider
does not check this criterion; see `docs/3-state/ProjectState.md`.

## M2 — The vertical slice

One castle, one century, four words, four players, the whole loop from lair to lair. "This is the
thing you put in front of people."

**Contains:** the castle generator built fresh; loot with value, bulk (stone) and fragility;
two-person carries; an extraction portal that counts only what physically crosses it; a lair that
remembers what came back.

**Acceptance:** four real players complete a raid together, start to finish, through the actual
built game (not a scripted test) — loot they carry out changes what the lair shows next time.

## M3 — Open the other Ages

Once the era exists as a `ScriptableObject`-shaped concept, a century should be content rather
than engineering.

**Contains:** each era brings its own room set, loot table, enemy roster, and tier of weapons.
Weapons deliberately carry across eras (a wheellock pistol in the Bronze Age is not corrected).

**Acceptance:** selecting a different era in the Lair produces a measurably different raid — a
different room set, different loot table, different guard roster, different available weapons —
not just a label that says which century you're nominally in.
