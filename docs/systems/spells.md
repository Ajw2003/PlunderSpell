# Spells

Say the word correctly and the spell happens. Say it nearly correctly and something worse happens.
That is the game, and `RogueAi.Spells` is where it resolves.

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
  `IIgnitable`, `IOpenable` — declared in `RogueAi.Core` and implemented by whatever can be affected.
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
| `Burst` | Frango, Levo, AurumVoco, Tonitrus, Somnus, CadaverSurge, Porta | An expanding, fading shell of light at the caster's hands |

`SpellBurst` builds itself from a primitive — no prefab, no authored particle asset, because none
exist yet. It disables its collider *before* destroying it: `Destroy` is deferred to the end of the
frame, and a live collider expanding to 2.5 m punts every piece of loot in the room across it first.

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
| 1–8 | Ignis, Frango, Levo, Aurum Voco, Tonitrus, Somnus, Cadaver Surge, Porta | |
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

## Invariants

- **An intended spell never hits the caster; a misfire always aims at them.** Primary effects
  exclude the caster's transform from targeting. `MisfireEffectBase.SelfTarget` does the opposite.

- **Every cast is audible.** Each effect emits a `VoiceCast` noise through the normal acoustic path
  before doing anything else. There is no silent cast, at any volume — a spell that skipped it would
  be a free pass past the alarm.

- **Consequence is server-side.** `SpellCastingSystem.ServerCast` runs the effect; the
  `[ObserversRpc]` that follows is presentation only. Running effects in the observers RPC would
  have four clients each applying the same damage.

- **Misfire resolution never fails open.** A lexicon entry with no authored `misfireId` falls back
  to `SpellCatalogue.DefaultMisfireFor`, rather than casting the real spell. A half-authored lexicon
  degrades into misfires, not into free correct casts.

## Traps

- **A field cannot share its type's name.** `SpellWord.SpellWord` is CS0542 and broke the whole
  assembly; the field is `Word`, with `[FormerlySerializedAs("SpellWord")]` for older assets.

- **Frango shatters your own loot too.** That is intended — it is how a raid loses its payday — but
  it means the effect must never be used as a generic "break the thing I am aiming at".

- **A burst is invisible from inside itself.** `CastOrigin` puts a burst about 1m in front of the
  caster's own eyes, and every burst-style spell's `Radius` (`SpellLookbook`) is 1.8m–4m — the sphere
  passes the camera within roughly the first tenth of its 0.45s life (`SetProgress`'s ease-out curve
  reaches that fast). The default primitive material back-face culls, and a camera inside a
  back-face-culled sphere sees nothing: every surface normal points away from it. That reads exactly
  like "the cast fired (the console logs it) but nothing appeared" — which is the failure mode a
  first-person tester hits and a `-batchmode` capture from outside the sphere does not.
  `SpellBurst.EnsureDoubleSided` instances the material and sets `Cull` to `Off` so the near
  hemisphere still renders once the camera is inside it. If a future look needs single-sided
  rendering back (e.g. for a transparent material — see the opaque-material limitation above), keep
  this in mind: it exists specifically so the caster can see their own spell.
