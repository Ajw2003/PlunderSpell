# Plan: Velox (dodge) and Saltus (high jump and slam) replace Tonitrus and Cadaver Surge

**Status: built 2026-09-26** (see `docs/4-systems/spells.md`, "Velox and Saltus", for what shipped
and where it differs from this plan: the dash holds its speed for 0.25 s rather than reusing the
old impulse, which never moved; the launch is 14 m/s, not the 9 m/s first tried, because the
player falls at 2.5 g; the slam's noise is as loud as the old thunderclap's). Roadmap: M2 (#106, more spells than Ignis
worth casting) and #152 (the slam as a real spell, currently under M4).

## What the owner decided

- Two new primary spells take the places of **Tonitrus** (thunderclap stun) and **Cadaver Surge**
  (raise a corpse) among the eight keybound casts.
- **Velox** ("swift") is the dodge. **Saltus** ("a leap") is the high jump, and pressing jump again
  in the air turns it into a **slam** that hits what is below.
- The **dodge key is removed**: dodging is only by spell, so it costs mana and is a choice.
- Tonitrus and Cadaver Surge are **deleted**, with their misfires, assets and lexicon entries. Git
  keeps them if either comes back.

## Choices made in this plan (not the owner's; change them if they read wrong in play)

- **Slots.** Velox takes Tonitrus's number key (5) and Saltus takes Cadaver Surge's (7), so the
  other six keys do not move.
- **Ids.** `SpellId.Velox` already exists (19, extended lexicon) and becomes a primary. `Saltus`
  is new at 47. Misfires: `MisfireVelox = 108`, `MisfireSaltus = 109`. The retired values 5, 7,
  103 and 105 are left as comments saying "retired, do not reuse", because ids are persisted in
  assets and sent over the network.
- **Slam trigger: the jump key, pressed again while airborne after Saltus.** Attack is bound to
  `G`, which is awkward in mid-air, and jump in the air currently does nothing, so nothing
  conflicts.
- **Mana.** Velox 10, Saltus 15; the slam costs nothing extra.
- **Misfires land as jokes, never as a free cast:** a misfired Velox dashes the caster in a random
  horizontal direction at full force; a misfired Saltus is a feeble hop that leaves the caster
  stunned for about a second.
- **Saltus only from the ground.** Cast in the air, it fizzles without spending mana, if the effect
  pipeline allows refusing a cast; otherwise it fizzles and spends mana, and the plan's report says
  which.

## Build steps

1. **Ids and catalogue.** `SpellId.cs`, `SpellCatalogue.cs`: remove Tonitrus, CadaverSurge and their
   misfires; add Saltus and the two new misfires; map Velox and Saltus to their misfires both ways.
2. **Effects.** `PrimarySpellEffects.cs`, `MisfireSpellEffects.cs`, `SpellEffectRegistry.cs`:
   delete `TonitrusEffect`, `CadaverSurgeEffect` and their misfire effects; add `VeloxEffect`,
   `SaltusEffect`, `MisfireVeloxEffect`, `MisfireSaltusEffect`. The pattern for an effect that moves
   the caster is `MisfireLevoEffect` (levitates the caster). Velox reuses the player's existing dash
   (`PlayerStateMachine.Dodge()` / `PlayerDodgeState`: an impulse along camera-relative input, or
   straight ahead with no input). Movement must be applied where the caster's body is simulated
   (the owning client), the same way the Levo misfire does it.
3. **The slam.** After a Saltus launch, the jump key in the air drives the body straight down
   fast. On landing: everything within about 3 m is knocked back and damaged through the one
   damage pathway (`IHealth.TakeDamage`), scaled by landing speed, and a loud noise event is
   emitted. Tuning values go in `SpellTuning` / `SpellTuningProfile` / `Resources/SpellTuning.asset`
   next to the existing ones, replacing the Tonitrus and Cadaver Surge values. Remove anything
   that only those two spells used (check `ISpellTargets`, `CastleGuard`, `StatusEffectReceiver`),
   but keep anything other spells still use, such as stun.
4. **Assets.** Delete `Spell_TONITRUS.asset` and `Spell_CADAVER_SURGE.asset` (with their `.meta`
   files); add `Spell_VELOX.asset` and `Spell_SALTUS.asset` (`SpellWord`: word, id, mana,
   alternative pronunciations, misfire id, `HeardAs` and `MisfireHeardAs`). Update
   `SpellLexicon.asset` so the eight primaries are, in key order: Ignis, Frango, Levo, Aurum Voco,
   Velox, Somnus, Saltus, Porta. Create assets through the live Editor, not by hand-writing YAML.
5. **Voice.** `HeardAs` phrases must be English words the Vosk model knows. Starting points:
   Velox "vee locks", "bee locks"; Saltus "salt us", "sal toss". Keep the misfire phrases distinct
   from the real ones. Check them against the model's vocabulary the same way the existing speech
   tests do. `MockVoiceInputService.cs` maps number keys to spells: update it.
6. **The dodge key.** Remove the `Dodge` action's use in `PlayerInputController.cs` (its binding is
   `<Keyboard>/ctrl` in `PlayerInputs.inputactions`, which also collides with Ctrl+number whisper
   casting). Keep `PlayerStateMachine.Dodge()`; Velox calls it.
7. **Presentation.** `SpellLook.cs` (both new spells use the cheap `Burst` look), `RaidHudView.cs`
   (spell names), `SpellVfxScreenshotForge.cs`.
8. **Tests.** Update every test that names the old spells (`CastingInputTests`,
   `CombatBenchCastingTests`, `FullRaidIntegrationTests`, `RaidSceneCastingTests`,
   `SpeechRecognitionTests`, `SpellEffectTests`, `SpellVfxTests`, `VoiceCastingTests`). Add, each
   failing first where it can: Velox moves the caster along their input; Saltus launches a grounded
   caster upward and fizzles in the air; the slam damages and pushes a guard standing below; each
   misfire does its misfire and never the real spell; the lexicon's eight primaries are in the order
   above; the dodge key no longer dodges.
9. **Docs.** `docs/4-systems/spells.md` (the spell table, the keys table, mana costs, the new
   effects) and `docs/4-systems/voice.md` if it lists the words; refresh their plain copies under
   `docs/plain/4-systems/` (the header hash is `git hash-object` of the source).

## Verification

- Recompile in the live Editor only when it is not in Play mode.
- Run the full EditMode and PlayMode suites. Known failures before this work:
  `ArtAssetImportTests` (EditMode) and `GuardAttackTests.Test_EveryAttackBumpsTheReplicatedSignal`
  (PlayMode). Anything else failing is this work's.
- Play a solo raid in the Editor and cast both spells and the slam; capture before and after
  screenshots into `docs/generated/dodge-and-leap-2026-09-26/`.
- Not checkable here: recognition of "Velox" and "Saltus" by a real voice. That needs the owner
  speaking into the microphone.
