# Damage

Everything that loses health, how it loses it, and how a player can tell. Added 2026-09-23 for #14
("one damage pathway", readable health) and #12 (visual feedback for taking and dealing damage).

## What it owns

`Interfaces.Damage` (`Assets/_Project/Scripts/Runtime/Core/Interfaces/Damage.cs`) — the only way
anything in the game is hurt — and `RogueAi.UI.DamageFeedbackView`
(`Assets/_Project/Scripts/Runtime/UI/RaidHud/DamageFeedbackView.cs`), which draws every hit. The
three health owners (`PlayerStateMachine`, `CastleGuard`, `MonsterStateMachine`) still own their
numbers through `IHealth`; this system only decides how damage reaches them and what it looks like.

## How it works
<!-- ref:7f7c -->

- **One call.** `Damage.Apply(target, amount, source, instigator, point, kind, impactVelocity)`
  hurts an `IHealth`, measures how much health it actually lost, and — only if it lost some —
  raises `Damage.Dealt` with a `DamageReport`. Every weapon, spell, guard, fire and flying object
  calls it: `MeleeWeapon`, `NetworkedProjectile`, `Item` (impacts), `CastleGuard` and
  `MonsterAttackState` (enemy attacks), `MonsterPickedUpState` (choking), `MisfireSpellEffects`
  (corpse blast), `StatusEffectReceiver` (burning). `grep "\.TakeDamage(" Runtime/` finds nothing
  outside `Damage.cs`.
- **Source vs instigator.** `Source` is the thing that physically hit (the goblet, the bolt);
  `Instigator` is who is to blame (the player who threw it, the guard who fired it). An `Item`
  remembers its `Holder` while held and blames them for 4 s after a throw; a projectile carries
  its shooter; a fire remembers who lit it. `DamageReport.SelfInflicted` is true when the
  instigator and the target share a root — your own misfire, your own junk on the rebound.
- **Feedback** (`DamageFeedbackView`, IMGUI, creates itself before the first scene loads — no
  scene needs wiring):
  - a number rises off the hit point: **yellow** when you dealt it, **orange** for friendly fire,
    **red** when it was you, grey otherwise; "KO" and a larger font on a kill;
  - whatever was hit **flashes red** for 0.12 s (a `MaterialPropertyBlock` on its renderers,
    restored afterwards);
  - a **health bar** appears over anything recently hurt for 4 s — red over an enemy, green over
    another player;
  - a **hit marker** on the crosshair when you landed the hit (red on a kill);
  - when *you* are hurt: a **red screen edge** sized by the hit, a line naming what did it
    ("−18 Watchman", "−17 yourself (fire)") — repeated hits from the same thing merge into one
    growing number — and a pulsing edge below 30 % health.
- **The HUD health bar is the body's health.** `PlayerStateMachine` publishes its health into
  `GameServices.PlayerStats` (`PublishHealth`) whenever it changes, so the canvas HUD's bar and
  its "82 / 100" label move. Before this they read a second, never-written number.
- **Death is a lost raid, not a reload.** The local player's `Die()` switches to
  `GameState.GameOver`: the "YOU DIED" screen, `RaidBootstrapper` abandons the raid
  (`RaidDirector.AbandonRaid` — nothing banked, castle cleared), and its button goes to the Lair.
  Setting out again revives the player at full health.

Screenshots of every case: `docs/generated/playtest-2026-09-23/05`–`12`.

## Weight

Added 2026-09-23. A held `Item` is a **dynamic body pulled toward the hand by a force of limited
strength** (`Item.FixedUpdate`), not a kinematic object teleported to the crosshair:

- The hand wants a velocity proportional to how far the item is from the aim point
  (`_followSpeed`, capped at `_maxHoldSpeed`), and pays for it plus gravity out of a fixed
  `_gripStrength` (180 N). A 1 kg pot spends 10 N staying up and has the rest to move with; a
  15 kg chest spends 147 N and has almost nothing left, so it lags, swings wide and sags to the
  floor when you turn. Measured (a 90° turn): pot 0.34 s to catch up, goblet 0.32 s, 5 kg relic
  0.43 s with 0.25 m of sag, 15 kg chest 1.78 s with 1.7 m.
- Because it stays a physics body, walls stop it and it carries momentum: **swinging a held thing
  into someone is an impact hit** through `Item.OnCollisionEnter`, at `speed × 2 × heft` damage
  (`heft` = ×1 at 1 kg, ×1.5 at 4 kg, ×2 at 9 kg), blamed on the holder. A held item never collides
  with its holder (`Physics.IgnoreCollision`, restored 0.4 s after letting go).
- Throws are impulses capped at 18 m/s, so the same arm throws a pot far and a chest barely at all.
- Carrying slows you: `Item.CarrySpeedMultiplier` is 1 up to 2 kg, falling to 0.5 at 15 kg,
  applied in `PlayerWalkState`.
- Grabbing and holding aim through the screen centre (the crosshair), within `_maxDragDepth`
  (10 m) — the hover ray used to be 100 m.
- Weapon prefabs carry real masses (sword 1–1.5 kg, round shield 3.5, pavise 8, matchlock 4…);
  they were all 1 kg.
- **Fragile loot breaks in your hands.** A valuable hit harder than its `Fragility` (m/s — goblet 3,
  plate 2) shatters even while held, shows "SHATTERED −150" where it broke, and drops out of your
  hands (`ItemManager` lets go of anything whose collisions switched off). Swinging the goblet at a
  guard hurts the guard and costs you the goblet — the REPO trade.

## Invariants

- **Nothing calls `IHealth.TakeDamage` except `Damage.Apply`.** A direct call still hurts, but no
  number, flash, bar or blame appears, and the player cannot tell it happened — exactly the bug
  #14 reopened on.
- **Only a hit that cost health is reported.** `Apply` measures before and after, so a target that
  ignores a soft impact (`TakeDamage(amount, impactVelocity)` below its threshold) produces no
  feedback.

## Traps

- **Unheld items only hurt above 4 m/s** (`Item.k_minUnheldImpactSpeed`). Loot settling at spawn
  was ticking 1-point hits off guards it landed on. Anything a player is holding or threw in the
  last 4 s still hurts at any speed.
- **"Local player" is whoever owns `Camera.main`.** The feedback view and
  `PlayerStateMachine.Local` both assume one rendering camera per machine. A spectator or split
  screen camera would need that rule revisited.
- **Screenshots miss the flashes.** The hit flash lasts 0.12 s and the Editor's frame capture lands
  later than that; pausing first returns a stale frame. The flash was verified by reading the
  renderer's property block during and after the hit instead.
