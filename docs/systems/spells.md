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
