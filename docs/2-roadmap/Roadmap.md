# Roadmap

What "done" means, milestone by milestone. Where things stand against it lives in
[`docs/3-state/ProjectState.md`](../3-state/ProjectState.md); this document only defines the target.

**Changed 2026-09-26.** This roadmap used to be the pitch's four milestones (`docs/plunderspell.md`
§11, "Four milestones to knowing whether it is any fun"), which stop at the vertical slice and the
other Ages. It now runs to a game a stranger can play: M0-M3 are kept, M3's acceptance is
tightened, and M4-M7 are new. Why, and what was considered:
[`docs/6-decisions/Decisions.md`](../6-decisions/Decisions.md), "The roadmap runs to a game a
stranger can play". The draft it was adopted from is
[`docs/archive/roadmap-draft-2026-09-26.md`](../archive/roadmap-draft-2026-09-26.md).

## Rules

- **A milestone counts only when its acceptance criterion has actually been checked**, not when
  its code exists. "Written but never run" is not done.
- **Weights add to 100**, so that "X% done" is worked out from the milestones, not guessed.
- **Game systems come before expensive presentation** (the owner, 2026-09-26). Animation,
  recorded audio, final art and performance work wait for one final pass (M7), after the systems
  they dress have stopped changing. Cheap feel, such as screen shake, hit-stop and simple impact
  effects, can go in at any time and is listed under M2.
- **An acceptance criterion that changes says so in place**: what it used to say, what it says
  now, and the decision that moved it.

| # | Milestone | Weight | Pillar it proves |
|---|---|---:|---|
| M0 | Fork clean, cut gravity | 5 | (foundation) |
| M1 | Prove the voice | 10 | 1: say the word |
| M2 | The vertical slice | 20 | 2: nothing is a menu |
| M3 | Open the other Ages | 15 | 3: rob every century |
| M4 | The Lair and the Market | 15 | 4: the Mystical Market |
| M5 | The household is awake | 10 | 2: the house wakes |
| M6 | The castle fights back | 10 | 2 and 3 |
| M7 | Final art and performance pass | 15 | all, plus release |

The issue numbers are GitHub issues on `Ajw2003/PlunderSpell`. Every issue open on 2026-09-26 is
placed in a milestone except #24 (the loop epic, superseded by M2) and #159 (no description).

## M0 — Fork clean, cut gravity (5)

Branch from the real trunk (`claude/steam-multiplayer-framework-xia7ch`), delete the planetary
`Gravity` assembly, restore world gravity across the player states and items, confirm nothing
else moved.

**Contains:** removing `Assets/_Project/Scripts/Runtime/Gravity/` and its ~3 consumers; restoring
standard −Y gravity in the player FSM and item impact path.

**Acceptance:** crates and players fall along −Y and land flat; thrown items still deal
velocity-scaled damage; all runtime assemblies compile with zero remaining references to the
gravity module.

## M1 — Prove the voice (10)

A bare grey room and four spell words, nothing else. The riskiest assumption in the project,
whether shouting at your own computer feels like power or embarrassment, answered as early as
possible.

**Contains:** `IVoiceInputService`, the Vosk provider, the keyboard mock, `SpellLexicon`, the
misfire table, and a way to adjust microphone gain (#125), without which the Whisper/Shout
thresholds cannot be calibrated across players.

**Acceptance:** over 90% top-1 recognition across four accents on the 40-word lexicon, under
150 ms from word-end to effect, and misfires that land as jokes rather than frustration (#50).
This needs real microphones and real speakers of different accents; a unit test against the mock
provider does not check it.

## M2 — The vertical slice (20)

One castle, one century, four words, four players, the whole loop from Lair to Lair. "This is the
thing you put in front of people."

**Contains:** the castle generator; loot with value, weight and fragility; two-person carries;
an extraction portal that counts only what physically crosses it; a Lair that remembers what came
back. And, because each one breaks or undermines a real raid:

- A raid leaves no state behind for the next one (#143).
- Players never spawn somewhere with no entrance in sight (#140).
- Combat reads: melee damage scales sensibly (#110), and more spells than Ignis are worth casting
  (#106).
- Every item is held at a sensible grip point (#134), and carrying is not floppy (#155).
- Loot near the portal neither moves nor takes damage (#158).
- Enemy numbers and strength scale with the lobby size (#154), so a four-player raid is not tuned
  for one.
- The spell shader renders in the standalone build (#127). The menu flow works in the Editor
  (#131).
- Cheap feel: camera shake, hit-stop and simple impact effects (#51). Anything needing new art,
  animation or recorded audio waits for M7.
- Two different Steam accounts can join each other.

**Acceptance:** four real players complete a raid together, start to finish, through the actual
built game (not a scripted test), and loot they carry out changes what the Lair shows next time
(#55).

## M3 — Open the other Ages (15)

Once the era exists as a `ScriptableObject`-shaped concept, a century should be content rather
than engineering.

**Contains:** each era's own room set, loot table, enemy roster and tier of weapons. Weapons
deliberately carry across eras (a wheellock pistol in the Bronze Age is not corrected). And:

- High Medieval and Age of Powder get rooms of their own, in the same design language as the
  Bronze Age (#151, #17).
- No era spawns another era's pieces, and each era's lighting is laid out for its own geometry
  (#135, #150).
- Every model is an authored, adjustable, networked item, structure or enemy (#126).
- The weapon roster matches the pitch's twelve named weapons, three per era (#38).
- The bestiary question is settled: the household guards against the fantasy enemies (#41).

**Acceptance (changed 2026-09-26).** It used to say: selecting a different era in the Lair
produces a measurably different raid (a different room set, loot table, guard roster and
available weapons), not just a label. **It now says:** the same, and every era builds from its
own room set with none borrowed from another era, and a person has played all four eras side by
side. **Why:** the old wording was already met while High Medieval used the original generic
rooms and Age of Powder borrowed High Medieval's, which is what #151 and #135 are about.

## M4 — The Lair and the Market (15)

The pitch's fourth pillar, and the "damp, yours, and permanent" Lair. This is what makes a raid's
haul mean something between raids.

**Contains:**

- The Lair as a walkable 3D place instead of a menu screen (#30, #31). Blockout only; its
  lighting (#32) is M7.
- Debt and hoard shown in the world, not only as numbers (#33). Debt is clearly per player, so
  players either split the cost or turn on each other (#130).
- The Mystical Market: a place, a shop, and purchases that stay bought (#26, #27); wares and
  prices for all four stalls (#28); the mark-up and "spend what is left after the debt" rules
  (#29).
- Progression: start with fewer spells and unlock more over time (#108), including the
  high-jump-and-slam as a real spell instead of the accidental ride on a held item (#152).

**Acceptance:** across three consecutive sessions, with the game quit and relaunched between
them, a player pays their debt, spends what is left at a stall, and finds both the purchase and
the hoard still there, shown in the Lair itself.

## M5 — The household is awake (10)

The pitch's household notices, reacts and hunts, rather than standing about. Guards already
patrol, investigate noises, chase and search (`GuardAlertState`); this milestone makes that read
as alive and responsive in play. Behaviour comes through movement, facing, timing and physics
here; animation clips (#141) and barks (#42) are M7.

**Contains:**

- Enemies fight back: they close in, commit to attacks and pressure a player who is carrying
  (#160).
- Every hit gets a reaction: a guard struck by a weapon, spell or thrown object staggers and
  turns on whoever did it.
- The house hears the raid: a thrown object, a dropped piece of loot or a fallen guard draws
  nearby guards to look, and a guard who finds a body raises the alarm.
- Chases are hunts: guards call others in, lose a player who breaks line of sight, search the
  last place they saw them, then give up and go back to patrol.
- No guard stands frozen: patrols vary, and a guard at rest looks around.
- Stealth from the player's side, as keyboard controls: crouch slows you and makes your
  footsteps quieter (#153), and sprint is faster and louder (#103). Crouch is not a spell. The
  lowered crouch posture is animation and is M7.

**Acceptance:** in a recorded solo raid in each era:

- a guard reacts visibly to being hit, and turns toward whoever hit it;
- a guard walks to where a thrown object landed within its hearing;
- a player who breaks line of sight and crouches away is lost, and the guard searches the last
  seen spot before returning to patrol;
- no guard stands still facing nothing for more than 10 seconds, other than one posted on guard;
- a guard hears crouched footsteps at most half as far away as walking ones.

## M6 — The castle fights back (10)

The castle stops being a backdrop. This covers the castle revamp's unstarted phases 3-5
([`docs/plans/castle-revamp.md`](../plans/castle-revamp.md)) and the hazards the pitch promises.

**Contains:**

- Stairs that lead somewhere and doors that open (#111).
- Murder-holes and arrow-loops that are real hazards, not layout tags (#44). A drawbridge you can
  operate (#45).
- Era-specific hazards (#35).
- The Gilded Colossus as a real vault boss, with a telegraph and an arena (#43).

Mechanism first: a door may swing without an animation clip, and a hazard may use placeholder
effects until M7.

**Acceptance:** on five seeds per era, every door and stair in the layout can be used, and at
least one working hazard of that era appears in each raid.

## M7 — Final art and performance pass (15)

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

**Acceptance:** both of these.

- In a recorded play session, every swing, throw, hit, cast and death has both a visible and an
  audible response, and no enemy is seen sliding or T-posing in any state.
- Someone who has never seen the game installs a standalone build on a PC and on a Steam Deck,
  joins a friend through a Steam invite, and finishes a raid. Both machines hold the frame budget
  set in #54 for the whole raid.
