# Art bible enemy animations (plan, 2026-09-24): decisions recorded, three still open

Animating the 16 ArtForge enemies, and importing and driving those animations in Unity. It depends
on [`artbible-enemies-in-engine.md`](artbible-enemies-in-engine.md) (phases E0, E1 and E4) and
follows the [`docs/art/WORKFLOW.md`](../art/WORKFLOW.md) loop. The model review sheet becomes a
**clip review sheet**.

Status: **plan only. Nothing here is built.** The decisions marked **[DECIDE]** need the user
before work starts.

## The ask, counted

The specs (`docs/art/data/*.json`, `enemies[].rig.animations`) list **177 clips**, 10–12 per enemy.
Most are the same motion on a different body:
- every one has an idle, a walk, an alert turn, a hit reaction and a death;
- nine share "thrust with a polearm";
- four share "fire a long gun, then reload".

Grouping them cuts the work to about **80 authored clips**. By the matrix's own count, 104 of the 177 are base motions, 49 are weapon-family motions, 13 are true one-offs, and 11 belong to the hound:

| Layer | What | Authored once for | Clips |
|---|---|---|---|
| Base (shared) | idle, walk, run, alert turn, stagger, hit react, death fall, archway duck, get up, sleep (Somnus), levitate struggle (Levo), shout / call alarm | the shared human (Humanoid retarget) | ~12 |
| Carry poses | one upper-body pose per enemy for how it holds its gear: the lantern, the yoke of fire-pots, the pavise, the petard burden, a sloped halberd. Layered over the base clips, it turns `idle_lantern` and `walk_burdened` into a base clip plus a pose | each enemy (upper-body Avatar mask) | 16 |
| Weapon families | polearm (thrust, sweep, hook, chop), sword and shield (cut, overhead, bash, brace), crossbow (aim, shoot, span-reload), sling (wind-up, release, reload), long gun (brace, aim, fire, reload, club), pistol (draw, fire), throw (unhook, windup, throw) | one representative per family, retargeted | ~30 |
| Signature | the moves only one enemy has: the lantern raise, the visor raise, plant pavise, crouch behind it, plant petard, self-detonate, censer swing, helm knocked off, shoulder barge, call the pavisier | that enemy | 13 |
| Hound (Generic rig) | sleep curl, wake, sniff, point, bark, gallop, stairs run, lunge bite, worry shake, yelp, death | the hound | 11 |

The full enemy-by-clip matrix is on the review page (`docs/generated/enemy-animation-plan.html`),
with each clip tagged Base, Family or Signature.

## Decisions recorded (2026-09-24, from the user)

- **Old roster:** the art-bible set replaces the household four (Watchman, Man-at-Arms, Sergeant,
  War-hound). The five supernatural enemies stay for the Crypt.
- **Era gating:** enemies spawn only in their own Age. The era pass-through is built once, shared
  with the era-rooms work.
- **Animation source:** AnimForge keyframes, authored in Blender by code.
- **Ragdoll:** yes, for killed or thrown guards. Clips cover sleep, stun and knock-down.
- **Still open:** in-place clips versus root motion, 1024 versus 2048 textures, and LODs and
  spring bones now versus later. These were not selected, so they are not assumed.

## Decisions for the user **[DECIDE]**

1. **Where the motion comes from.**
   - **A (recommended): keyframed in Blender by code** ("AnimForge", an ArtForge module). Poses are
     authored on the shared `figures.Human` rig, interpolated, and exported as FBX animation. It
     fits the art's stylised, readable look, runs here end to end, is reproducible, and every clip
     gets a review sheet like the models did. Cost: it reads as hand-keyed, not captured. Weight
     and follow-through have to be designed in.
   - **B: motion capture from a library** (Mixamo and similar), retargeted through Unity Humanoid.
     It looks more natural for locomotion. But it must be downloaded by hand with an account (it
     can't be automated from here), the licence terms need checking, and the weapon-specific
     motions (spanning a crossbow, planting a pavise) mostly don't exist.
   - **C: hybrid.** Library locomotion (idle, walk, run) for the Base layer; AnimForge for weapon
     families and signatures.
2. **Root motion.**
   - **Recommended: in place.** The `NavMeshAgent` moves the guard; the clip only animates. Walk
     and run strides are authored to match each role's agent speed, so feet don't slide.
   - **Alternative: root motion.** More grounded, but it fights the NavMesh and network sync.
3. **Death:** a death clip, a ragdoll, or a clip that blends into a ragdoll.
   - **Recommended:** the clip for sleep, stun and "knocked down".
   - **Recommended:** a ragdoll for thrown or killed guards. The pitch says guards can be picked up
     and thrown, and `DownedPlayerCarryAdapter` already has the ragdoll switch to copy.
4. **Spring bones** (skirts, coats, the hound's jowls, which the specs ask for).
   - **Recommended: later.** First try Unity's Animation Rigging "damped transform", or a cheap
     script, on the prop and skirt bones. Adding real spring chains to ArtForge rigs is a separate
     change.

## Phases

### A0: clip taxonomy and timing (paper first)
One table, generated from the JSON clip lists: every clip's layer, loop flag, length, key frames,
and its events. It becomes `Tools/ArtForge/anim_spec.json`. The review page's matrix is its first
draft.

Events:
- `Footstep`, which drives the existing `FootstepNoiseEmitter` so guards make noise in the
  acoustics system;
- `AttackHit`, the frame damage applies;
- `ProjectileRelease`;
- `PropDetach`: the lantern falls, the helm flies off.

**Audit:** the table covers every clip the 16 specs list; each one is mapped or explicitly dropped
with a reason.

### A1: AnimForge (Blender, this container)
`Tools/ArtForge/anim_forge/`:
- **A pose library** on the shared human: named poses (`guard_high`, `thrust_extend`,
  `recoil`, …) stored as bone rotations.
- **Clips** as timed pose keys with easing, anticipation and overshoot. Locomotion comes from a
  parametric gait (stride, cadence, bob) matched to the agent speed.
- **Export:**
  - one animation-only FBX per clip family (`Humanoid_Base.fbx`, `Humanoid_Polearm.fbx`, …) on
    the reference skeleton, for Humanoid retargeting;
  - per-enemy FBX for signature clips that move that enemy's prop bones;
  - the hound's clips on its Generic rig.
- **Weapon grips.** Families are authored with an IK target on the weapon, so a two-handed polearm
  stays in both hands after retargeting. Unity's Humanoid IK matches the hands to it.

**Audit:** a clip review sheet per clip, `docs/art/anim/<clip>.png`. It shows:
- 8 frames of the clip on a representative enemy, beside that enemy's concept;
- a foot-contact trace for locomotion (sliding shows as a smeared contact);
- a short MP4 alongside, made with Chromium's bundled ffmpeg.

Then fix and audit again, per the workflow.

### A2: import (Unity)
`ArtBibleModelImporter` (engine plan E0) also owns the animation import:
- Humanoid for the family FBX files and Generic for the hound;
- per-clip loop flags, root transform settings (bake into pose, in place) and events, all taken
  from `anim_spec.json`;
- nothing set by hand in the Inspector.

**Audit:** an EditMode test that every clip in `anim_spec.json` exists, retargets (no missing-bone
warnings), has the right loop flag, and carries its events.

### A3: animator
- **One base controller**, `Household_Humanoid.controller`:
  - Locomotion layer: a blend tree on speed (idle, walk, run), plus turn-in-place.
  - Action layer (upper-body Avatar mask): attacks, aim, reload, shout.
  - Additive layer: hit reactions.
  - Full-body override states: stagger, sleep, levitate, death, archway duck.
- **One override controller per enemy**, generated by the forge. It swaps the Action layer's
  family clips for that enemy's weapon and adds its signature clips. Sixteen small assets, no
  hand-wired graphs.
- **The hound** gets its own `Hound_Generic.controller`.

**Audit:** an EditMode test that every override controller fills every slot of the base controller
(no enemy falls back to T-pose).

### A4: driving it from the game
`Runtime/Guards/GuardAnimationDriver.cs` (no gameplay logic) reads:
- the synced `GuardAlertState` from `CastleGuard`, and whether the guard is incapacitated or held by
  Levo;
- the guard's speed. Clients don't run the agent, so speed comes from position change per frame,
  smoothed;
- the replicated attack signal from engine plan E4, so every player sees every swing.

It sets Animator parameters only. Animation events come back into the existing systems: footstep
noise, and projectile release timing on the server. Death and thrown states switch to the ragdoll
(decision 3).

**Audit:**
- CombatBench with each enemy: patrol, notice, chase, attack, get hit, sleep, levitate, die.
- A two-player co-op run: the other player's view shows the same attacks.
- In-engine review sheets per enemy (`docs/art/engine/`), extended with a short strip of the
  attack.

## Size and order

Rough effort, in this repo's terms. One Age can ship end to end before the rest: the High Medieval
set is the natural first slice.

1. A0 table: small.
2. A1 Base (12) and one family (polearm): medium. This proves the whole pipeline on the Lantern
   Warden before anything is multiplied.
3. A2, A3 and A4 for that one enemy: medium. The first animated guard in a raid.
4. The remaining families and signatures, one Age at a time, each through the audit loop: large,
   and parallel by Age, as with the models.
5. The hound: small to medium.

## Who can run what
- **This container:** AnimForge, clip review sheets, the importer, controller forge and driver C#,
  and the headless compile and tests.
- **A Unity editor machine:** import, controller generation (menu item), CombatBench, co-op, and
  in-engine sheets. Each is handed over with exact steps and marked `UNTESTED` until it has been
  run there.
