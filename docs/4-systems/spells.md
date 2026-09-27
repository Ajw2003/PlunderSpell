# Spells

Say the word correctly and the spell happens. Say it nearly correctly and something worse happens.
That is the game, and `Plunderspell.Spells` is where it resolves.

## How it works

- **Resolution** (`MisfireEngine`): exact match on the normalised phrase → the intended spell;
  Levenshtein distance within the lexicon's tolerance → that word's misfire; nothing close → a
  silent fizzle.

- **Execution** (`SpellEffectRegistry` → `ISpellEffect`): effects are stateless singletons resolved
  by `SpellId`. Adding a spell is one class and one registry line, and a test can swap in a spy.
  `Execute` returns how many things an effect affected, so "cast at nothing" (0) and "no effect
  registered" (-1) are distinguishable outcomes rather than both being silence.

- **Volume is the dial** (`SpellTuning`): `CastVolume` scales power *and* noise in the same
  direction. A whisper is weak and near-silent; a shout is strong and wakes the castle. Nothing else
  in the game gives the player that trade-off, so it is deliberately steep.

- **Targeting** (`SpellTargeting`): an overlap query returning *distinct* components implementing an
  interface, nearest first. Distinct matters — a guard with three colliders would otherwise take
  triple damage.

- **Reaching the world without cycling**: Loot and Player sit downstream of Spells, so a spell never
  sees a `LootPickup` or a player. It sees `IBreakable`, `ILevitatable`, `ISleepable`, `IStunnable`,
  `IIgnitable`, `IOpenable` — declared in `Plunderspell.Foundation` and implemented by whatever can be affected.
  `StatusEffectReceiver` implements four of them in one component, so anything can be made a valid
  target by adding it.

## Seeing a cast

Until 2026-09-18 a cast was invisible. The effects did real work — ignite, shatter, levitate, put to
sleep — and nothing appeared on screen, so the only evidence a spell had happened was a line in the
console.

Visuals are a **presentation layer**, deliberately not part of the effects.
`SpellEffectRegistry`'s effects stay pure (no scene lookups, no spawning) so they remain testable by
handing them a context. `SpellVfxDirector` subscribes to `SpellCastingSystem.CastResolved` instead,
which fires on every peer — so a teammate's spell is visible to everyone, not just to whoever spoke.

`CastReport` carries `Origin` and `Direction`, derived from the same `CastOrigin`/`CastDirection`
the effect context uses, so the visual and the effect cannot disagree about where the spell came
from.

### What each spell looks like

`SpellLookbook` maps a `SpellId` to a colour and a style. Every misfire returns the same angry
orange regardless of what it was aimed at, so "that went wrong" is readable before the caption is.

| Style | Spells | What it is |
|---|---|---|
| `Bolt` | Ignis | A tinted `Bolt.prefab` fired along the aim, plus a flash at the hands |
| `Burst` | Frango, Levo, AurumVoco, Velox, Somnus, Saltus, Porta | An expanding, fading shell of light at the caster's hands |

`SpellBurst` builds itself from a primitive — no prefab, no authored particle asset, because none
exist yet. It disables its collider *before* destroying it: `Destroy` is deferred to the end of the
frame, and a live collider expanding to 2.5 m punts every piece of loot in the room across it first.

### A cast follows the camera, not the body

Until 2026-09-20, `CastOrigin`/`CastDirection` used the caster's body transform — the thing that
only yaws, per `PlayerStateMachine.Look`. Pitch lives on `CameraTransform` (the child the mouse-look
`_xRotation` is applied to) instead, so a cast always left the caster's chest level and always went
exactly horizontal, whatever they were actually looking at. `ItemManager`'s melee swing already
aimed from `CameraTransform.position`/`.forward` — casting was the one path still aiming from the
body.

`SpellCastingSystem._aimSource` fixes this: it self-wires to the first child `Camera` (same
convention as `SpellBook`'s self-wiring), and `CastOrigin`/`CastDirection` read its position and
forward directly, with a fixed distance in front (`_castOriginForwardOffset`) and no separate height
offset — the camera is already at eye height. This changes where the *effect* resolves from too, not
just the visual: `SpellEffectContext.Origin` is the same value, so e.g. Ignis now looks for a target
near where you are actually looking rather than always at chest height in front of you.

For a remote caster's cast (`BroadcastCast` on another peer), `AimTransform` resolves through that
caster's own `SpellCastingSystem._aimSource` — every player wires their own camera on `Start`, so
this is correct for whoever cast, not just the local player.

### Two ways to cast

The project contains two casting systems, and only one of them is the game's.

| | `SpellCastingSystem` | `SpellBook` |
|---|---|---|
| Origin | Plunderspell | ported from the predecessor project |
| Input | hold `V`, speak (or press 1-8) | the attack button |
| Resolves | phrase → `SpellId` → `ISpellEffect` | fires a projectile |
| On the raid player | **yes** | no |
| Needs authored assets | `SpellLexicon.asset` (assigned) | a `SpellStats` asset — **none exists in the project** |

`PlayerStateMachine.Attack` casts through `SpellBook` **only when one is assigned**, and swings the
held item otherwise. The raid player has no `SpellBook`, so attacking swings and casting is the
voice path. That is the intended design: the pitch is a game about speaking words.

`SpellBook`'s field was an auto-property, which Unity does not serialize — which is why no slot for
it ever appeared in the Inspector however public it looked. It is now a serialized field with a
property over it, so it can be assigned. Two supporting fixes make assigning one safe: `SpellBook`
self-wires its camera and shoot point, and reports a missing `SpellStats` rather than throwing in
`Start` and taking the whole player down with it.

**Assigning a `SpellBook` still will not fire anything**, because no `SpellStats` asset exists.
Voice casting is unaffected either way.

### Casting runs on the new Input System

`PushToCastController` and `MockVoiceInputService` originally read the legacy `Input` class while
the rest of the project (`GameFlowInput`, `ItemManager`) uses `Keyboard.current`. Both now use the
new Input System. The split was a silent failure waiting to happen: the project's Active Input
Handling is currently "Both", and the moment it is set to "Input System Package (New)" every
casting key stops working with no error.

`CastingInputTests` presses the real keys through `InputTestFixture` and asserts the whole chain,
so the input layer is covered rather than assumed.

### Casting it in the Editor

There is no microphone involved in the Editor. `VoiceServiceLocator.ShouldUseMock()` returns true
unconditionally under `UNITY_EDITOR`, so `MockVoiceInputService` is always the provider, and it is
driven by the keyboard:

**Hold `V`, press `1`–`8` while still holding it, then release `V`.**

Holding `V` is what opens the mic (`PushToCastController`), and the mock only reads number keys
`while IsListening` — press a number without holding `V` and nothing happens at all, which is the
easiest way to conclude the spells are broken when they are not.

| Key (while holding V) | Spell | Hold also… |
|---|---|---|
| 1–8 | Ignis, Frango, Levo, Aurum Voco, Velox, Somnus, Saltus, Porta | |
| 1–8 | the near-match misfire of each | `Shift` |
| 1–8 | whisper (quiet, weak) | `Ctrl` |

`Shift` doubles as the shout volume, so a shifted key is both a misfire and a shout.

The raid's player carries `SpellCastingSystem` and `PushToCastController` and has **no**
`SpellBook`, so `PlayerStateMachine.Attack` swings the held item rather than casting — casting is
the voice path only.

### What the visuals actually look like

`Tools ▸ Plunderspell ▸ Capture Spell VFX Screenshots` photographs every look into
`docs/generated/spell-vfx-screenshots/`. The committed set is the evidence that these render at all.

**Known limitation, visible in that capture:** a burst is an *opaque* sphere. `SpellBurst` fades by
writing alpha into `_BaseColor`, and the URP/Lit material it builds is opaque, so the alpha does
nothing — the fade is currently dead code and a burst reads as a solid coloured ball rather than
light. Fixing it means a transparent or additive material on the burst.

### A spell's bolt carries no damage

This is the one thing to know before changing it. The effect has **already resolved on the server**
by the time `CastResolved` fires, so the bolt is cosmetic — `Damage` is set to 0 on spawn. A bolt
that damaged what it hit would apply Ignis twice.

Making the bolt the thing that deals the damage would mean moving Ignis's resolution out of
`IgnisEffect` and into a projectile hit, which is a change to how the spell system works rather
than a visual — not attempted here.


### Spells go where you aim

Added 2026-09-23 (#106). Single-target spells (Ignis, Levo, Porta) take the target
closest to the crosshair within `AimConeDegrees` (22°) and `AimRange` (14 m × the volume power),
preferring centred over near; anything within 1.5 m counts even off-centre. Area spells (Somnus)
burst on the sleeper under the crosshair when the cone finds one, else at the crosshair's aim point,
not on the caster's feet. (Changed 2026-09-26, #106: Somnus used only the aim point, a single ray,
and a guard a little off it let the ray run on to the wall behind, so the sleep went off 9 m past
the guard. In a live raid it slept 0 guards where Ignis, Frango and Levo, aimed the same way, all
hit.) Before, every spell took
whatever was nearest a point 1 m in front of the caster's face, often something beside or behind
them. The overlap buffer grows instead of capping at 128 colliders. Misfires still centre on the
caster, on purpose.

### Tuning is an asset

Added 2026-09-23 (#105). Every spell number (the volume dial's power/noise, each spell's damage,
durations, radii, the misfire penalties) lives in `Assets/_Project/Resources/SpellTuning.asset`, a
`SpellTuningProfile`, grouped by spell with tooltips. Edit it in the Inspector; no code change or
recompile needed. `SpellTuning` reads it (from Resources, so it ships in builds); if the asset is
missing it falls back to the values the game had when they were constants. `SpellTuning.Use` swaps
in another profile for tests or balance experiments. Spell *behaviour* (what Ignis does) is still
code in `PrimarySpellEffects` / `MisfireSpellEffects`.

### Mana and the keyboard chant

Added 2026-09-25 (#116, and the user's request to make mana useful rather than remove it).

- **Every word costs mana.** The cost is `SpellWord.ManaCost` on each spell's asset in
  `Assets/_Project/Data/Spells/`: Porta 10, Velox 10, Levo 12, Ignis 15, Saltus 15, Frango 20,
  Somnus 20, Aurum Voco 30. A misfire costs the same as the word it garbled
  (`SpellLexicon.ManaCostOf`). A fizzle costs nothing.
- **The pool is the local player's `GameServices.PlayerStats`**, 100 points, refilled when the
  caster subscribes (a new body in a raid) and regained at `SpellTuning.ManaRegenPerSecond`
  (2.5/s, about 40 s from empty). The check and the spend happen on the caster's machine before
  the cast is sent to the server, the same side the phrase is resolved on. A word the pool can't
  cover is refused: nothing is cast and nothing is spent, and the caption says "not enough mana".
- **A number-key cast is chanted before it fires.** `VoiceRecognitionResult.FromKeyboard` marks
  a keyed phrase; `SpellCastingSystem` holds it for `SpellTuning.KeyboardCastSeconds` (1.5 s),
  then spends the mana and casts, aimed wherever you look when it finishes. One chant at a time;
  keys pressed during a chant are ignored. Speech casts the moment the phrase is recognised, so
  the keys are a slight disadvantage, not the fastest way to cast.
- **The HUD** shows mana under health (`HUDScreen`), each word's cost in the spellbook (dimmed when
  you can't afford it), and a "Chanting IGNIS" bar under the crosshair (`RaidHudView.DrawChant`).
- Tests: `CastingInputTests` presses the real keys and checks the chant delay, the spend, and the
  refusal on an empty pool. The scene casting suites set `KeyboardCastSeconds` to 0 and refill
  between casts, because they test that words resolve, not the chant.

### Velox and Saltus

Added 2026-09-26, at the owner's call; they replace Tonitrus (a thunderclap stun) and Cadaver Surge
(raise a corpse), which are deleted. Plan: `docs/plans/dodge-and-leap-spells.md`.

- **Velox** (key 5, 10 mana) is the dodge: a dash at `VeloxDashSpeed` (20 m/s) for
  `VeloxDashSeconds` (0.5 s), about 10 m, along where you are steering, or where you look when
  you are not. There is no dodge key any more (it was Ctrl, which also whispers).
- **Saltus** (key 7, 15 mana) is a high jump, only from the ground: `SaltusLaunchSpeed` (25 m/s,
  times the volume power) straight up, about 13 m, since the player falls at 2.5 g. The owner set
  these (and Levo's lift, 6 to 15) in the Inspector on 2026-09-26; the first defaults were 14 m/s
  and 0.25 s for the dash and 14 m/s for the launch. Cast in the air
  it fizzles and costs nothing.
- **The slam.** After a Saltus launch, jump in the air drives the body straight down at
  `SaltusSlamSpeed` (22 m/s). On landing, every living thing within `SlamRadius` (3 m) except the
  caster takes up to `SlamDamage` (30), half at the edge, scaled by landing speed, and is shoved
  `SlamKnockback` (2.5 m); the landing makes an `Explosion` noise of `SlamNoiseStrength` 1 over
  `SlamNoiseRadius` (14 m). Jump in the air without a Saltus launch does nothing, as before.
- **Misfires.** A misfired Velox (VELOS, VELO) is a full dash in a random direction. A misfired
  Saltus (SALTAS, SULTUS) is a hop at `MisfireSaltusHop` (a quarter) of a launch, and the legs lock
  for `MisfireSaltusStaggerSeconds` (1 s); it never arms the slam, and is paid for even in mid-air.
- **Where it runs.** These are `ICasterMovementSpell`s: `SpellCastingSystem.Cast` checks
  `CanMove` before spending mana and calls `MoveCaster` on the caster's machine, where the body is
  simulated, through `ISpellMovable` (the player's `PlayerStateMachine`). The server's `Execute`
  only makes the cast noise. The slam's landing is reported by `PlayerStateMachine.SlamLanded`, and
  `SpellCastingSystem` resolves its damage on the server (`ServerSlam` → `SaltusEffect.ResolveSlam`).
- **Speech.** The small English model hears VELOX as "the locks" and SALTUS as "salt is"; the
  `HeardAs` lists on `Spell_VELOX.asset` and `Spell_SALTUS.asset` start from what it heard on the
  synthetic clips. Zira's mispronounced SULTUS is also heard as "salt is", so it casts Saltus;
  David's is heard as "salt soldiers" and misfires. A real voice has not been tried.
- Tests: `MovementSpellTests` (a real player body on a floor), `SpellEffectTests` (the slam's
  damage, falloff, noise, and that a caster with no body fizzles).

## Invariants

- **An intended spell never hits the caster; a misfire always aims at them.** Primary effects
  exclude the caster's transform from targeting. `MisfireEffectBase.SelfTarget` does the opposite.

- **Every cast is audible.** Each effect emits a `VoiceCast` noise through the normal acoustic path
  before doing anything else. There is no silent cast, at any volume — a spell that skipped it would
  be a free pass past the alarm.

- **Consequence is server-side.** `SpellCastingSystem.ServerCast` runs the effect; the
  `[ObserversRpc]` that follows is presentation only. Running effects in the observers RPC would
  have four clients each applying the same damage. The one exception is moving the caster's own
  body (Velox, Saltus), which happens on the caster's machine because that is where the body is
  simulated; the damage a slam does still resolves on the server.

- **Misfire resolution never fails open.** A lexicon entry with no authored `misfireId` falls back
  to `SpellCatalogue.DefaultMisfireFor`, rather than casting the real spell. A half-authored lexicon
  degrades into misfires, not into free correct casts.

## Traps

- **Porta has nothing to open in a raid** (found 2026-09-26, #106). `CastleDoor` exists, but the
  castle generator places none: a live raid had 0 doors, and Porta cast at nothing. It becomes
  useful with #111 (doors that open), phase 5 of `docs/plans/castle-revamp.md`. Frango's
  "forces doors" is idle for the same reason.

- **Raid guards have no Rigidbody, so Levo's drop is simulated by hand** (#106).
  `CastleGuard.UpdateLevitation` used to land a bodiless guard the frame the spell ended, a 1.8 m
  drop that cost nothing. It now falls under gravity back to the height it was lifted from and takes
  9 damage a metre (about 16). `GuardTests.Test_ALevitatedGuardWithNoBodyFallsAndIsHurt`.

- **The dodge used to go nowhere.** `PlayerDodgeState` gave one impulse and ended once the body
  was slower than 1 m/s, which the very first physics step always was, before the impulse had been
  applied. It now holds the dash speed for the dash's length. Found by `MovementSpellTests` when
  Velox reused it; the old dodge key had the same fault.

- **A field cannot share its type's name.** `SpellWord.SpellWord` is CS0542 and broke the whole
  assembly; the field is `Word`, with `[FormerlySerializedAs("SpellWord")]` for older assets.

- **Frango shatters your own loot too.** That is intended — it is how a raid loses its payday — but
  it means the effect must never be used as a generic "break the thing I am aiming at".

- **A burst used to spawn centred on the caster's own eyes.** `CastOrigin` puts a burst about 1m in
  front of the caster, and every burst-style spell's `Radius` (`SpellLookbook`) is 1.8m–4m — bigger
  than that 1m offset, so the camera ended up *inside* the sphere for every burst spell. Two failure
  modes came from this, both visible in `docs/generated/spell-vfx-screenshots/raid-cast-eye-*.png`:
  a back-face-culled material reads as nothing at all (every surface normal points away from a camera
  inside it — "the cast fired, the console logs it, nothing appeared"), and `EnsureDoubleSided`
  (below) turns that into the opposite problem, an orange wall filling the whole screen.
  `SpellVfxDirector.BurstPosition` is the real fix: it pushes the burst's centre out from
  `CastOrigin` by the spell's own radius (plus a small clearance), so the sphere's *near edge* lands
  at the cast origin instead of its centre — the caster's camera sits just outside the burst rather
  than swallowed by it.
  `SpellBurst.EnsureDoubleSided` (instances the material, sets `Cull` to `Off`) stays in place as a
  safety net for whatever grazes the near edge, not as the primary fix. If a future look needs
  single-sided rendering back (e.g. for a transparent material — see the opaque-material limitation
  above), the double-sided behaviour is the thing to reconsider, not `BurstPosition`.
