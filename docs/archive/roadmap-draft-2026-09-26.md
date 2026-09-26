# Roadmap draft: from "is it fun" to "can a stranger play it"

**Status: adopted 2026-09-26, with changes; inert.** The live roadmap is
[`docs/2-roadmap/Roadmap.md`](../2-roadmap/Roadmap.md). On adoption the owner made crouch a keyboard
control rather than a spell, and asked for a milestone on enemies being responsive and alive, which
became M5 and renumbered the castle and final-pass milestones to M6 and M7. Reasons:
[`docs/6-decisions/Decisions.md`](../6-decisions/Decisions.md), "The roadmap runs to a game a
stranger can play".

## Why the current roadmap needs replacing

The current roadmap is the pitch's four milestones (`docs/plunderspell.md` §11), and the pitch
titles them "Four milestones to knowing whether it is any fun". They stop at the vertical slice
plus the other Ages. Everything past that point has no home in it:

- **57 open issues, none tied to a milestone.** No GitHub milestones exist. The priority queue
  (`docs/plans/GitIssues/Priority_Queue.md`) orders issues up to #59 into phases, but it is an
  order, not a definition of done, and the 25 open issues filed since (#103 onwards) are not in
  it. Every one of the 57 is placed below.
- **A whole pillar is missing.** The pitch names four pillars. The Mystical Market (#26) and the
  Lair as a place (#30) are neither in any milestone nor built.
- **"Done" for M3 is already met on paper.** A different era already gives a measurably different
  raid, but two eras borrow another era's rooms (#135, #150, #151). The current criterion cannot
  catch that.

The draft keeps M0-M3 and their acceptance criteria, changes one criterion (M3, marked below),
and adds three milestones after them. Each milestone gets a weight, so that "X% done" can be
worked out rather than guessed.

**Ordering rule (the owner, 2026-09-26):** game systems come before expensive presentation. The
Lair and Market (M4) and the castle's hazards (M5) come first. Animation, audio, art and
performance work wait for one final art and performance pass (M6). The exception is cheap feel,
such as screen shake, hit-stop and simple impact effects, which can go in at any time and is
listed under M2.

## The milestones

Weights add to 100. A milestone counts only when its acceptance criterion has actually been
checked, not when its code exists. That rule is unchanged.

| # | Milestone | Weight | Pillar it proves |
|---|---|---:|---|
| M0 | Fork clean, cut gravity | 5 | (foundation) |
| M1 | Prove the voice | 10 | 1: say the word |
| M2 | The vertical slice | 25 | 2: nothing is a menu |
| M3 | Open the other Ages | 15 | 3: rob every century |
| M4 | The Lair and the Market | 15 | 4: the Mystical Market |
| M5 | The castle fights back | 10 | 2 and 3 |
| M6 | Final art and performance pass | 20 | all, plus release |

### M0 — Fork clean, cut gravity (5)

Unchanged. Branch from the real trunk, delete the planetary `Gravity` assembly, restore world
gravity.

**Acceptance:** crates and players fall along −Y and land flat; thrown items still deal
velocity-scaled damage; every runtime assembly compiles with zero references to the gravity
module.

### M1 — Prove the voice (10)

Unchanged definition. Whether shouting at your own computer feels like power or embarrassment.

**Contains:** `IVoiceInputService`, the Vosk provider, the keyboard mock, `SpellLexicon`, the
misfire table. Added: a way to adjust microphone gain (#125), because the Whisper/Shout
thresholds cannot be calibrated across players without it.

**Acceptance (unchanged):** over 90% top-1 recognition across four accents on the 40-word
lexicon, under 150 ms from word-end to effect, and misfires that land as jokes rather than
frustration. Measured with real microphones and real speakers (#50). A test against the mock
provider does not count.

### M2 — The vertical slice (25)

Unchanged definition: one castle, one century, four words, four players, the whole loop from Lair
to Lair. "This is the thing you put in front of people."

**Contains:** the castle generator; loot with value, weight and fragility; two-person carries;
an extraction portal that counts only what physically crosses it; a Lair that remembers what came
back. Added, because each one breaks or undermines a real raid today:

- A raid leaves no state behind for the next one (#143).
- Players never spawn somewhere with no entrance in sight (#140).
- Combat reads: enemies fight back (#160), melee damage scales sensibly (#110), and more spells
  than Ignis are worth casting (#106).
- Every item is held at a sensible grip point (#134). Carrying is no longer floppy; #155 needs a
  check against the 2026-09-26 carry fix, then closing.
- Loot near the portal neither moves nor takes damage (#158).
- Enemy numbers and strength scale with the lobby size (#154). Without this, a four-player raid
  is tuned for one.
- The spell shader renders in the standalone build (#127). The menu flow works in the Editor
  (#131).
- Cheap feel only: camera shake, hit-stop and simple impact effects (#51). Anything that needs
  new art, animation or recorded audio waits for M6.
- Two different Steam accounts can join each other. So far this is checked only between two game
  windows on one machine.

**Acceptance (unchanged):** four real players complete a raid together, start to finish, through
the actual built game (not a scripted test), and loot they carry out changes what the Lair shows
next time (#55).

### M3 — Open the other Ages (15)

Unchanged definition: once an era is a `ScriptableObject`, a century is content rather than
engineering. Weapons still travel across eras on purpose.

**Contains:** each era's own room set, loot table, enemy roster and tier of weapons. Added:

- High Medieval and Age of Powder get rooms of their own, in the same design language as the
  Bronze Age (#151, #17).
- No era spawns another era's pieces, and each era's lighting is laid out for its own geometry
  (#135, #150).
- Every model is an authored, adjustable, networked item, structure or enemy (#126).
- The weapon roster matches the pitch's twelve named weapons, three per era (#38).
- A decision on the bestiary: the household guards against the fantasy enemies (#41). The art
  bible has since given each era four human enemies, so this may only need closing with a note.

**Acceptance (changed).** It used to say: selecting a different era produces a measurably
different raid (a different room set, loot table, guard roster and available weapons), not just
a label. **It would now say:** the same, plus every era builds from its own room set with none
borrowed from another era, and a person has played all four eras side by side. **Why:** the old
wording is already met while High Medieval uses the original generic rooms and Age of Powder
borrows High Medieval's. That is exactly what #151 and #135 complain about.

### M4 — The Lair and the Market (15) — new

The pitch's fourth pillar, and the "damp, yours, and permanent" Lair. This is what makes a raid's
haul mean something between raids.

**Contains:**

- The Lair as a walkable 3D place instead of a menu screen (#30, #31). Its look is blockout
  here; the lighting pass (#32) waits for M6.
- Debt and hoard shown in the world, not only as numbers (#33). Debt is clearly per player, so
  players can either split the cost or turn on each other (#130).
- The Mystical Market: a place, a shop, and purchases that stay bought (#26, #27). Wares and
  prices for all four stalls (#28). The mark-up and "spend what is left after the debt" rules
  (#29).
- Progression: start with fewer spells and unlock more over time (#108). The spell set it
  unlocks from gains the high-jump-and-slam as a real spell, replacing the accidental ride on a
  held item (#152), and sprint and crouch as moves (#103, #153). The crouch's lowered posture
  is animation and waits for M6.

**Acceptance:** across three consecutive sessions, with the game quit and relaunched between
them, a player pays their debt, spends what is left at a stall, and finds both the purchase and
the hoard still there, shown in the Lair itself.

### M5 — The castle fights back (10) — new

The castle stops being a backdrop. This covers the castle revamp's unstarted phases 3-5
(`docs/plans/castle-revamp.md`) and the hazards the pitch promises.

**Contains:**

- Stairs that lead somewhere and doors that open (#111).
- Murder-holes and arrow-loops that are real hazards, not layout tags (#44). A drawbridge you can
  operate (#45).
- Era-specific hazards (#35).
- The Gilded Colossus as a real vault boss, with a telegraph and an arena (#43).

Mechanism first: a door may swing without an animation clip, and a hazard may use placeholder
effects until M6.

**Acceptance:** on five seeds per era, every door and stair in the layout can be used, and at
least one working hazard of that era appears in each raid.

### M6 — Final art and performance pass (20) — new

Everything expensive, done once, after the systems it dresses have stopped changing. Then the
build goes to someone who was not there.

**Contains:**

- Animation: the player and doors (#11), every enemy in every state it has (#141), wind-up and
  follow-through on throws and swings (#52), and the crouch posture (#153).
- Audio that carries meaning: guards murmur on patrol, bark on alert and shout on a chase (#42),
  and teammates hear a word half a heartbeat before it resolves (#48).
- Art: main menu art (#16), an icon set (#34, #40), Steam lobby and invite branding (#58), the
  pitch's lighting frames (#59), post-processing (#121), castles that no longer look bland (#23),
  and the Lair's lighting (#32).
- Performance: a budget and a profiling pass for full-art raids (#54), with a Steam Deck as the
  low end (the night atmosphere's quality levels already target it).
- Settings: complete audio mix, keybinds and microphone device (#56), and every word bindable to
  a key (#57).

**Acceptance:** two checks, both needed.

- In a recorded play session, every swing, throw, hit, cast and death has both a visible and an
  audible response, and no enemy is seen sliding or T-posing in any state.
- Someone who has never seen the game installs a standalone build on a PC and on a Steam Deck,
  joins a friend through a Steam invite, and finishes a raid. Both machines hold the frame budget
  set in #54 for the whole raid.

## Issues with no milestone

- **#24** (epic: build the game loop end to end) is superseded by M2 and #55. Proposed: close it
  with a pointer to M2.
- **#159** ("bruh") has no body. Proposed: ask what it meant, or close it.

## Not issues, but blocking a milestone

- The 44 commits on `claude/issue-backlog` are not in `main` yet. M2's acceptance runs "through
  the actual built game", so they need merging first.
- Two known test failures: `GuardAttackTests.Test_EveryAttackBumpsTheReplicatedSignal` (a
  separate task is on it) and `ArtAssetImportTests` (art-bible animations, enemy emissive HDR).

## What adopting this would do to the headline

A rough estimate only, to show the scale of the change. The real figure would be measured in
`docs/3-state/ProjectState.md`, milestone by milestone. Against the current four milestones the
headline says about 65%. Against these seven it comes out near 43%:

| Milestone | Weight | Rough share done | Why |
|---|---:|---:|---|
| M0 | 5 | 100% | Acceptance checked |
| M1 | 10 | ~70% | Works for one person on one machine; #50 never measured |
| M2 | 25 | ~70% | Loop plays solo and over UDP; #55 never run; the M2 issues above are open |
| M3 | 15 | ~60% | Two of four eras have their own rooms; the other two borrow |
| M4 | 15 | ~5% | Debt exists as a Lair number; no market, no 3D Lair |
| M5 | 10 | ~5% | Doors and hazards are tags; revamp phases 3-5 not started |
| M6 | 20 | ~15% | Hit and spell feedback, the build tool, quality levels and a partial settings menu exist; no animation |
| | | **≈ 43%** | |

The drop is not lost progress. It is the same work, measured against a larger definition of done.

## Not decided here, for the owner

- Whether to mirror these milestones as GitHub milestones and assign the issues to them. Doing so
  would replace the priority queue's phases as the working order.
