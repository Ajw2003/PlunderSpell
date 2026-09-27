# Moodboard gap closure

What it would take for Plunderspell as built to match `docs/plunderspell.md` (pitch bible v0.2)
and the project's mood board (`docs/generated/plunderspell-moodboard.html`, synced from the
project doc `plunderspell-moodboard (4).html`). Live until the backlog below is triaged and
either executed or explicitly descoped; then archive it.

**Method.** Read the pitch bible and mood board in full. Read every tier-3/4/5/6 doc
(`ProjectState.md`, `systems/*.md`, `Today.md`, `Decisions.md`) and the engineering plan. Walked
`Assets/`, `Assets/_Project/{Art,Data,Prefabs,Scripts}` and `Tools/` on disk (folder-listing
level, not a line-by-line code read — see "What this pass did not check" at the end) and cross-
referenced what actually exists against what the pitch names. Did not re-derive the 2026-09-16
playtesting backlog (GitHub issues #5–#25, `docs/generated/github-issues.json`) — those are
counted as already covering the ground they cover, and this document is additive to them, not a
replacement.

**Headline.** The four pillars the pitch is built on — voice-casting, physics loot, the historical
Ages, and the debt/market economy — are unevenly realised. Voice-casting and physics loot are real
systems with real tests. The Ages exist as a data type and one era's worth of art. **The fourth
pillar, the Mystical Market, does not exist anywhere in the codebase** — no stall, no shop UI, no
wares data. And under every pillar, the thing the mood board spends the most words on —
*texture*: named weapons with a personality, an era-specific treasury, six lighting frames, a
bestiary of household guards rather than monsters — is almost entirely unauthored. The raid loop
runs; almost nothing in it looks, sounds, or names itself the way the pitch promised.

---

## 1. Corrections to the brief

Two things worth saying before the gap list, because getting them wrong would send effort the
wrong way.

**The eight spells are already fully defined.** `Assets/_Project/Data/Spells/` holds one asset per
spell (`Spell_IGNIS`, `Spell_FRANGO`, `Spell_LEVO`, `Spell_AURUM_VOCO`, `Spell_TONITRUS`,
`Spell_SOMNUS`, `Spell_CADAVER_SURGE`, `Spell_PORTA`) plus `SpellLexicon.asset`, and
`docs/4-systems/spells.md` documents resolution, misfires, targeting and the invariants around all
of them. The gap is not definition — it's that casting one produces no visible or audible effect
(already filed as issue #13, and see §5 below for what's still missing beyond that filing).

**The weapon *system* mostly doesn't exist yet, separately from the weapon *roster* not matching.**
The pitch describes twelve named weapons with reach/heft/noise stats and a melee feel ("a sword is
merely a heavy object with a sharp end that you swung hard"). What's built is physics-based
throwing/impact damage (`docs/plunderspell.md` §10: "Physics grab / carry / rotate / throw with
velocity-scaled impact damage") plus eight generically-named weapon models
(`Assets/_Project/Prefabs/Weapons/`) that aren't wired into a raid at all (issue #17). There is no
evidence anywhere in `docs/4-systems/` of a swing/reach/heft-driven melee attack — see §4.

---

## 2. Pillar-by-pillar

### 2.1 Voice — mostly built, feedback is missing

Built: `Plunderspell.Voice` (Vosk + keyboard mock), `SpellLexicon`, `MisfireEngine`, volume-scaled
casting. This is the most complete pillar in the game.

Not built:
- **A loudness meter.** The pitch calls volume "the whole trick" (§4) and `SpellTuning.CastVolume`
  scales power and noise (`docs/4-systems/spells.md`), but nothing on screen shows a player where
  they are between whisper and shout while holding the cast key.
- **Teammates don't hear the word.** The pitch's most quoted line — "your fellows hear the word
  half a heartbeat before the world obeys it" — isn't represented over the network. Only the
  resolved `SpellId` crosses the wire (`docs/6-decisions/Decisions.md`, 2026-09-11 entry); there is no
  networked "someone near you is mid-cast" tell, visual or audio, before the effect lands.
- **No caption of what was recognised.** Nothing on screen echoes the phrase the recogniser heard,
  which is both an accessibility gap and the only way to debug a mis-recognition versus a genuine
  misfire.
- **M1's acceptance criterion has never been measured.** `docs/3-state/ProjectState.md` already says this
  plainly (">90% top-1 recognition across four accents... under 150ms" — "no real-microphone,
  multi-accent, latency measurement exists anywhere in the repo"). It has no GitHub issue yet.

### 2.2 Physics loot — built, treasury is unauthored

Built: grab/carry/throw, velocity-scaled damage, bulk in stone, two-person heavy carries (listed
built in `docs/plunderspell.md` §10).

Not built:
- **The treasury the pitch describes doesn't exist.** Five generic loot prefabs
  (`CopperPot`, `SilverPlate`, `GoldenGoblet`, `HeavyChest`, `AncientRelic`, plus one
  `Loot_Conjured_Coin` for Aurum Voco) stand in for what the pitch names by era: ingots, faience,
  ceremonial bronze, sealed amphorae (Bronze); reliquaries, altar plate, illuminated psalters, coin
  (High Medieval); Burgundian plate, tapestry, banking ledgers, jewels (Late Medieval); cabinets of
  curiosity, mirrors, astrolabes, silver services (Powder). None of that era-specific dressing is
  modelled, and the mood board's own signature prop — the two-person **Altarpiece** — has no
  prefab at all.
- **Two-person carries have no feedback.** Nothing communicates strain, an uneven carry, or a
  stumble when one carrier lets go.

### 2.3 The Ages — one era of four

`HistoricalEra` is a real enum with lair-side selection, but only High Medieval has rooms, loot or
enemies (issue #17 already covers this precisely — not re-filed here). Two things from the pitch
are specifically unaddressed by that filing and worth calling out on their own:

- **Era-specific hazards are entirely unbuilt.** The pitch names one per era — fire runs faster in
  the Bronze Age, the High Medieval stairs "turn clockwise, and not in your favour", Late Medieval
  guards "hunt in pairs", and one stray `IGNIS` near a Powder-age magazine "ends the evening." None
  of these appear in `docs/4-systems/castle.md`, `alarm.md` or `raid.md` — the only hazard-adjacent
  system that exists is generic noise/alarm.
- **Weapons crossing eras — the pitch's "that IS the progression" rule — can't be exercised yet,**
  because weapon prefabs aren't wired into any raid (issue #17) and there's no loadout/inventory UI
  to carry one into a different era's raid to begin with.

### 2.4 The Mystical Market — does not exist

This is the single largest pillar-level gap. `docs/plunderspell.md`'s mood board devotes a full
section to it (the Spellmonger's Stall, the Smith's Stall, the Alchemist's Stall, the Curiosity
Dealer — unlearned incantations, weapon upgrades, one-use consumables, category-less curios), with
specific rules (buying here always costs more than finding it in a dungeon; it remembers what it's
sold a player; spend what's left after the debt as you like). **None of it exists**: no market
scene or UI, no stall data, no shop-transaction code, nothing in `docs/4-systems/` describes it.
This did not surface in the 2026-09-16 playtesting backlog because that backlog was filed against
the *raid* loop; the market sits at the *lair*, which the backlog didn't touch.

### 2.5 The Lair — a menu, not a place

The pitch's Lair is physical: "damp, yours, and permanent," lit by "one candle, one fire, falling
away into black — the only safe frame in the whole game." What's built (`LairScreen`, per
`docs/4-systems/raid-scene-assembly.md`'s "Getting into a raid" section) is a UI screen showing debt,
banked gold and the four eras — there is no 3D lair space anywhere in `Assets/Models` or
`Assets/_Project/Art` (which holds only `Models/Castle`, `Models/Loot`, `Models/Weapons` and
`Models/Enemies` — no `Models/Lair`). The debt and hoard the pitch treats as the emotional core of
the game currently exist only as HUD numbers.

### 2.6 The bestiary — half matches the pitch, half doesn't

`Assets/Models/Enemies/enemy_manifest.json` names ten enemies. Five read exactly as the pitch's
"household": **Watchman** ("walks the patrol routes, carries the lantern"), **ManAtArms** ("the one
guard who comes to check"), **Sergeant** ("leads the sweep, carries the horn"), **WarHound** ("the
chase — reacts to noise, runs you down") — all grounded, human, matching "the household does not
forget what it heard" (§3 of the pitch) precisely.

The other five do not: **SigilWisp** ("scout flier, swarms"), **VaultWarden** ("melee grunt,
armoured"), **HexTurret** ("static sentry — yaws to track, pitches to fire"), **ArcRevenant**
("elite caster — hovers, shields allies, casts at range"), **GildedColossus** ("vault boss —
asymmetric siege construct"). Nothing in the pitch or mood board describes magical constructs,
casters, hovering elites or a swarming flier-scout as antagonists — the pitch's threat is
explicitly and repeatedly human ("the household," "guards," "a war-hound at your heels," never a
monster). **This is a real creative fork, not a build gap**, and it's flagged here as a decision
the project needs to make deliberately rather than by accretion: either these five get reworked
into the household-guard register the pitch promises (a battle-mage retainer instead of
`ArcRevenant`, a war-machine instead of `HexTurret`, and so on), or the pitch/mood board is updated
to admit the game now has a supernatural register the original pitch didn't. Either is legitimate;
leaving it unresolved means art, animation and audio keep getting built against two different
premises.

### 2.7 Castles, portal, and "the six frames" — bland and empty

Issue #19 already covers material/lighting blandness and issue #21 covers the missing portal
asset. Two things the mood board specifies that aren't covered by either filing:

- **The six reference lighting frames** the pitch names as a production target (Lair, Treasury,
  Portal, the Hue & Cry, the Approach, Bronze Age Interior — `docs/plunderspell.md` §9) have no
  concept art, lighting rig, or even a defined camera setup to check "did we hit this" against.
  Three of the six frames (Lair, Portal, Approach) aren't castle-interior lighting at all, so
  issue #19's castle-materials fix won't touch them even once it's done.
- **The portal has no closing urgency.** The pitch's central tension beat — "the way home begins
  to narrow... it will not wait up for you" — has no visual or audio countdown; extraction is a
  trigger volume with a timer value nobody can see change.

### 2.8 Networking, market persistence, juice

- The **acoustics simulation is real but silent** — `Plunderspell.Acoustics` computes attenuated noise
  events for guards and the alarm to react to, but produces no sound a player hears
  (`docs/4-systems/alarm.md`). Guards are correspondingly silent: no patrol murmur, alert bark, or
  chase shout — a second, narrower gap than the general "no SFX" already filed as issue #18,
  because it's specifically about the enemies' own voice, not the player's.
- **No camera shake, hit-stop, or impact weight anywhere** — casting, throwing and being hit all
  resolve with no juice beyond whatever the (absent) VFX/SFX pass will add.
- **No standalone Unity player build has ever been produced.** `docs/3-state/ProjectState.md` and
  `docs/5-today/Today.md` both already say this in plain terms — confirmed again by folder-listing this
  pass — but it has no GitHub issue tracking it.

---

## 3. The new backlog

34 new issues, grouped into 3 new epics plus standalone items, filed via
`Tools/mkissues_moodboard_gap.py` (same mechanism as the existing 21 — `gh issue create` run
locally, since this cloud session has no GitHub write path). They deliberately do not re-file
anything already covered by #5–#25; where a new item sits next to an existing one, the item says
so. Exact issue numbers land in `docs/generated/github-issues-moodboard-gap.json` once the script
is run — this document refers to items by title, not by a number it can't yet know.

| Group | Items |
|---|---|
| EPIC — The Mystical Market pillar does not exist | 1 epic + 3 |
| EPIC — The Lair is a menu, not the physical place the pitch describes | 1 epic + 3 |
| EPIC — No icon set exists anywhere | 1 epic |
| Ages / eras content (hazards + loot catalogue) | 2 |
| Weapons & melee combat | 4 |
| Bestiary | 3 |
| Structures & hazards | 2 |
| Portal & extraction feel | 1 |
| Voice feedback | 4 |
| Juice | 2 |
| Testing, build, performance | 3 |
| Accessibility & settings | 2 |
| Networking / co-op polish | 1 |
| Lighting reference | 1 |

**Total: 34.** See `Tools/mkissues_moodboard_gap.py` for the exact title, labels and body of every
item — it's the source of truth for the backlog, same convention as `Tools/mkissues.py` for
#5–#25.

---

## 4. Suggested triage order

Not a milestone renumbering — `docs/2-roadmap/Roadmap.md` owns that. This is what to look at first if
picking off the new backlog:

1. **The bestiary fork (§2.6).** Everything downstream of it — animation, VFX, audio, boss design
   — is more expensive to redo than to decide correctly once. Resolve before authoring more art
   for the five divergent enemies.
2. **Market epic.** It's not a polish pass on an existing system; it's a whole unbuilt pillar, and
   the debt/lair loop the rest of the game motivates around is only half-present without it.
3. **Lair-as-a-place epic.** Same reasoning — the pitch's emotional anchor currently has no scene.
4. Everything else can interleave with the existing #5–#25 backlog by ordinary priority.

---

## What this pass did not check

Consistent with the tier-4 convention of citing what a document's claims can and can't show: this
audit read `docs/`, the mood board, and folder/asset listings (file names, sizes, counts) under
`Assets/`, `Assets/_Project/` and `Tools/`. It did **not** open individual `.cs` scripts to verify
implementation details beyond what the existing `docs/4-systems/*.md` already document and cite —
so where a claim above says "X doesn't exist," it means no asset, folder, or documented system
covers it, not that every source file was read line by line. It also did not open any `.asset`
YAML to check field values (e.g., whether the `Inventory/*.asset` weapon data already carries
reach/heft/noise fields with no consumer, versus not carrying them at all) — that distinction is
worth checking before starting on the melee-combat items in §4.2 of the new backlog.
