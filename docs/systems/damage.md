# Damage

Everything that loses health, how it loses it, and how a player can tell. Added 2026-09-23 for #14
("one damage pathway", readable health) and #12 (visual feedback for taking and dealing damage).

## What it owns

`Interfaces.Damage` (`Assets/_Project/Scripts/Runtime/Core/Interfaces/Damage.cs`) — the only way
anything in the game is hurt — and `Plunderspell.UI.DamageFeedbackView`
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

Added 2026-09-23; rebuilt 2026-09-25 to carry like R.E.P.O. (#144, research and choices in
[`docs/plans/carry-like-repo.md`](../plans/carry-like-repo.md)). A held `Item` is a **dynamic body
hung from the point you grabbed, pulled by a spring of limited strength** (`Item.FixedUpdate`):

- **You hold the point you aimed at.** `ItemManager` keeps the hover ray's hit point and passes it
  to `Item.StartDragging(holder, grabPoint)`. The item stores it in its own frame. Picking up
  without a point (tests, older callers) uses the authored `GripPoint`, or the mesh centre.
- **A beam spring on that point.** The spring pulls the held point to a target on the crosshair
  ray at the held depth (scroll changes the depth). Its stiffness is `_springRate` (120/s²),
  damped at 0.9 of critical toward the target's own velocity. `Item` estimates that velocity from
  successive `UpdateTargetPosition` calls, and `UpdateTarget` can supply it. The force is applied
  with `AddForceAtPosition` at the held point.
- **It keeps its orientation** (2026-09-26, replacing the free hang). A held item keeps the
  rotation it had when picked up, relative to where the holder faces, and turns with them
  (`Item.SetViewYaw`, fed by `ItemManager` every frame). Hanging freely from an off-centre grab was
  floppy. A rotation spring does it, limited by mass, so heavy things turn slowly. While the
  orientation is held, the beam's pull acts at the centre of mass (moved so the held point lands on
  the target), not at the held point: pulled off-centre, a light item was twisted by the spring and
  twisted back by the hold every step, and shook, 17 degrees a step at 0.5 kg. It now settles to
  nothing (`CarryFeelTests.Test_AHeldItemSettlesInsteadOfShaking`, cubes and the real hippo, goblet,
  nautilus cup and hat badge).
- **Lift or drag.** Upward force is capped at `_gripStrength` (100 N) and sideways force at
  `_haulStrength` (250 N). Holding up an item takes `mass × 9.81` of the upward budget: under about
  10 kg it lifts, and heavier items (tapestry, cabinet, cauldron, parade armour, altarpiece, chest)
  stay on the floor and are dragged. `Item.Load` is that fraction and `Item.IsTooHeavyToLift` is
  load over 1.
- **Walking.** There is no special case for the holder's body. The target moves with the camera
  and the spring follows it, so heavy things trail and swing when you turn. Measured
  (`CarryFeelTests`), turning round at full walking speed: a 2 kg item trails about 0.35 m and an
  8 kg one about 1 m, with no single-frame jump over 5 cm.
- **Carrying what you can lift never slows you.** Its weight shows as lag and swing. Towing a
  piece too heavy to lift does slow you: see below.
- **Turning it on purpose.** Hold middle mouse: `Item.SetRotating(true)` locks its current
  rotation as a target, the mouse turns it, and letting go keeps the new orientation.
- **Weapons sit in the hand, not on the beam** (2026-09-26). Anything with a `RangedWeapon` or
  `MeleeWeapon` is held rigidly low right of the view, pointing where you look
  (`Item.HoldInHand`, `Item.SetHandPose`, posed in `RenderPipelineManager.beginCameraRendering`
  so it never lags the camera). It is held by its authored grip, or else by its origin (a
  crossbow's butt, a sword's pommel). Its pointing axis is measured from its meshes
  (`Item.AimFrameLocal`), since the forged weapons point along different local axes. In the hand
  the body is kinematic and its colliders are triggers on the Ignore Raycast layer. On the beam a
  crossbow hung on the crosshair line more than 0.8 m out, and its own bolt hit it. No beam is
  drawn for a weapon. Capture: `docs/generated/held-weapons-2026-09-26/crossbow-in-hand.png`.
- **Two-person pieces are towed behind you, slowly** (2026-09-26, the owner's call). Anything too
  heavy to lift (`LootItem.RequiresDualCarry` pieces are 12-15 kg) is not pulled toward the
  crosshair. It is towed on a rope as long as the reach it was grabbed at (1.5-3 m), from wherever
  the holder's body is, so you walk forward and it scrapes along behind (`Item.SetTow`, fed by
  `ItemManager`):
  - **Tuning, per item.** On the item's prefab, `Item` > *Towing*: **Custom Tow Pace** on, then
    **Tow Pace** (the holder's walk while towing it: 1 = no slowdown, 0.5 = half speed) and **Tow
    Strength** (the most the rope pulls it with, N; lower is slower to get going). The five
    two-person pieces have their own values (cabinet and cauldron 0.5, parade armour 0.46,
    altarpiece 0.43, chest 0.4, all at 160 N); with Custom Tow Pace off, an item uses 6 / mass.
  - **You walk slower.** `Item.TowSpeedMultiplier` is the item's `TowPace`, and a rope does not
    stretch: while the piece lags past the rope's length
    you are held back further, down to a fifth of that at a metre of strain. `PlayerWalkState`
    applies it. Past 2 m of strain the piece is stuck on something and you let go.
  - **It plods.** Once the rope is taut the piece is driven toward your own pace along the rope,
    plus a gentle catch-up for any strain (`Item.TowVelocity`), with at most 160 N
    (`_towStrength`), at its centre of mass. A slack rope pulls nothing and friction stops it. The
    grip still bears what weight it can (up to the 100 N lift limit) at the height it was grabbed,
    so a tall piece held by its top stays upright and slides instead of falling on its face.
  - Earlier versions, both measured: a spring toward a point behind you let pieces lurch to
    4-6 m/s behind a player walking 2-2.5 m/s, and towing with no lift toppled the altarpiece,
    which then stuck. Measured now with the real prefabs
    (`CarryFeelTests.Test_OnePlayerCanDragEveryTwoPersonPieceBehindThem`): each of the five trails
    3.1-4.2 m behind on a 3 m rope, at a top ground speed of 2.1-2.7 m/s behind a holder slowed
    to 2.0-2.5 m/s.
- **The beam.** `GrabBeam` is a `LineRenderer` drawn as a quadratic curve. It starts at the hand
  (low right of the view, `ItemManager.BeamHand`) and ends at the held point, bent through the
  aim target. When the item keeps up the line is straight; when it lags or sags the line bends.
  The colour follows the load: violet when easy, gold, orange near the limit, red and flickering
  when dragging. It trembles more with strain. The material is `Resources/GrabBeam.mat` (URP
  Particles/Unlit, additive). Other players see your beam through `Plunderspell.Net.CarryBeamRelay` (a
  RaidScene object, like `ShotRelay`). The holder sends the hand point, aim point, held point and
  load about 15 times a second; others ease toward it and end the line on the replicated item.
  Only networked loot gets a remote beam: weapons are a local copy on each machine.
- **Prefabs carry their own strength.** `Item`'s serialized `_gripStrength` overrides the code
  default. All 35 item prefabs were set to 100 N on 2026-09-25. A newly forged prefab
  (`EraContentForge`) takes the code defaults.
- Because it stays a physics body, walls stop it and it carries momentum: **swinging a held thing
  into someone is an impact hit** through `Item.OnCollisionEnter`, at `speed × 2 × heft` damage
  (`heft` = ×1 at 1 kg, ×1.5 at 4 kg, ×2 at 9 kg), blamed on the holder. A held item never collides
  with its holder (`Physics.IgnoreCollision`, restored 0.4 s after letting go).
- Throws are impulses capped at 18 m/s, so the same arm throws a pot far and a chest barely at all.
- Grabbing and holding aim through the screen centre (the crosshair), within `_maxDragDepth`
  (10 m) — the hover ray used to be 100 m.
- Weapon prefabs carry real masses (sword 1–1.5 kg, round shield 3.5, pavise 8, matchlock 4…);
  they were all 1 kg.
- **Fragile loot breaks in your hands.** A valuable hit harder than its `Fragility` (m/s; 4.5 at the
  least since #142, about a 1 m drop, see [`docs/plans/loot-balance.md`](../plans/loot-balance.md)) shatters even while held, shows "SHATTERED −150" where it broke, and drops out of your
  hands (`ItemManager` lets go of anything whose collisions switched off). Swinging the goblet at a
  guard hurts the guard and costs you the goblet — the REPO trade.
- **Bodies bumping into loot never break it** (#142, `LootPickup.OnCollisionEnter`). Players set
  their velocity every physics step, so walking into loot shoved it and then struck it again while
  it moved: the faience hippopotamus (Fragility 2) shattered on the second bump. A kicked item still
  breaks if it hits a wall hard enough. Breaks are judged on the closing speed **along the contact
  normal**, so an item tumbling along the floor after a knock is not struck afresh at every
  corner. `ImpactDamageTests` covers both, and a 3 m drop still shattering it.

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
- **Only the item's own speed counts** (#146). A contact's relative velocity includes the speed of
  whatever ran into the item, so a player walking (5 m/s) into a 12 kg cauldron on the floor took
  24 damage from it. `Item.OnCollisionEnter` now uses the smaller of the relative speed and the
  item's own speed going into that physics step (`_velocityIntoStep`, recorded in `FixedUpdate`;
  after the step the contact has already spent it). `ImpactDamageTests` covers walking into loot
  and a thrown cauldron.
- **Cooldown clocks start at minus infinity.** `_lastDamageTime` started at 0, so no item could hurt
  anything in the first half second of a session. That matters in PlayMode tests, which start
  fresh.
- **"Local player" is whoever owns `Camera.main`.** The feedback view and
  `PlayerStateMachine.Local` both assume one rendering camera per machine. A spectator or split
  screen camera would need that rule revisited.
- **Screenshots miss the flashes.** The hit flash lasts 0.12 s and the Editor's frame capture lands
  later than that; pausing first returns a stale frame. The flash was verified by reading the
  renderer's property block during and after the hit instead.
