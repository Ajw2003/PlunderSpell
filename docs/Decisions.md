# Decisions

Append-only. An entry is never rewritten or deleted; the one allowed edit is flipping its
`Status` line to `Superseded` when a later entry replaces it. Newest entry at the top.

## 2026-09-22 — A spell burst is centred on where it lands, not where it starts

**Context.** The user reported casting still didn't visibly work in the raid scene after
`claude/playable-loop-fixes` (PR #96) landed its Input System fix. Reviewing that PR's logic found
nothing wrong with it — the keyboard→mic→spell-word pipeline checks out on inspection and under the
headless harness. A separate, unopened branch (`claude/verified-issue-fixes`, built on top of PR
#96) had already gotten further and left real Unity-rendered proof: `raid-cast-eye-Ignis.png` shows
an orange sphere filling almost the entire frame, and `raid-cast-eye-Tonitrus.png` is blank sky.
Both are the camera sitting inside the burst sphere — confirmed by `docs/systems/spells.md`'s own
"burst is invisible from inside itself" entry, which already diagnosed the back-face-culling half of
this but mitigated it with `SpellBurst.EnsureDoubleSided` rather than moving the burst.
`CastOrigin` (`SpellCastingSystem`) places a burst only 1m in front of the caster; `SpellLookbook`'s
burst radii run 1.8m–4m — bigger than that offset, so the camera is inside every burst regardless of
which side of the material renders.

**Decision.** `SpellVfxDirector.BurstPosition` pushes each burst's centre outward from `CastOrigin`,
along the cast direction, by the spell's own radius plus a small clearance — so the sphere's *near
edge* lands where the old centre was, instead of the centre itself. `CastReport.Origin` (the
gameplay-facing cast point, also used for e.g. area-effect targeting) is untouched; only the VFX
spawn position changes, in `SpellVfxDirector`, which already has the spell's `Radius` from
`SpellLookbook`.

**Why.** `EnsureDoubleSided` treats the symptom (nothing renders from inside a back-face-culled
sphere) without addressing why the camera is inside a 4m sphere in the first place — and fixing only
the culling trades invisibility for a screen-filling blob, neither of which reads as "a spell was
cast." Moving the spawn point is the actual fix; `EnsureDoubleSided` stays as a safety net for
whatever still grazes the near edge (a moving camera, a wide FOV), not as the primary mitigation.

**Status.** Current. Not yet confirmed in a real Editor session — verified by re-deriving the
distances by hand (for Tonitrus's 4m radius, the pushed-out sphere's near edge sits at ~1.15m from
the caster's pivot, comfortably outside where a first-person camera sits) and by the headless
harness (no new failures introduced), not by re-capturing the screenshots that exposed the bug.

## 2026-09-20 — The headless harness's own project files were never committed

**Context.** `Tools/Headless/verify.sh` has existed since `5f8e336` and every PR since has cited its
output ("Unity 6000.3.15f1 batchmode... EditMode X/X; PlayMode Y/Z" claims aside, several commit
messages also reference the headless path). Running it in this session for the first time with a
real .NET SDK present failed immediately: `Plunderspell.Headless/Plunderspell.Headless.csproj` does
not exist, and never has — `git log --all --diff-filter=A -- "*.csproj"` finds no commit that ever
added one anywhere in the repo. `.gitignore:49` has a blanket `*.csproj` rule, meant for Unity/Rider's
auto-generated per-assembly project files at the repo root, but unanchored it also matches the
hand-written `Tools/Headless/**/*.csproj` the harness depends on. Whoever wrote the harness had these
files locally; they were silently gitignored from the first commit and nobody has run `verify.sh`
against a real SDK since, in any session, ever — the shims that exist were written and reviewed by
reading, not by compiling.

**Decision.** Anchored the gitignore rule to the repo root (`/*.csproj`), reconstructed
`Plunderspell.Headless.csproj`, `Plunderspell.Headless.Editor.csproj` and
`Plunderspell.Headless.Tests.csproj` from `Tools/Headless/README.md`'s description of the intended
layout plus `verify.sh`'s own invocations, and filled the substantial shim gaps that reconstructing
and actually running it exposed (uGUI, several `UnityEditor`/`PrefabUtility`/`AssetDatabase`
members, `Physics.CheckCapsule`/`SyncTransforms`, `RenderTexture`, `Camera` fields, `Shader.PropertyToID`,
`Cursor`, and a `UnityAction` that was a plain class instead of a delegate — see the shim files'
own comments for the specifics). Three Runtime files gained a third and fourth headless-build
exclusion (`Net/PlayerNetworkOwnership.cs` alongside the existing two, `Net/SteamInviteGateway.cs`)
rather than being shimmed, for the same "native/generated SDK, little to verify" reasoning the
existing exclusions already used.

**Why.** The harness is the only "compiles and the tests pass" check available in a container with
no Unity install (this one, and apparently every prior session that touched this repo) — and it was
silently unusable the entire time. Fixing the gitignore rule and committing the reconstructed files
is the only way that stops recurring; filling the shim gaps rather than declaring the harness broken
is what makes `verify.sh` an honest check again instead of a script that fails before it reaches any
of the code someone actually changed.

**Status.** Current. `verify.sh` now runs 148/162 tests green; the twelve failures are catalogued in
`Tools/Headless/README.md`'s "What it does and does not prove" and are shim-fidelity gaps (real
`.unity` scene loading, off-screen rendering, real asset import, prefab-instance correlation), not
gameplay-logic regressions.

## 2026-09-20 — Enemy health bars are IMGUI, projected from world space, not a second uGUI system

**Context.** Issue #14 asks for enemy health to be readable in-world. `Assets/_Project/Scripts/Runtime/Core/UI/HealthBar.cs`
already exists — a generic `IHealth`-driven world-space slider — but it was never placed on any
prefab or in any scene; it's dead code. `RaidHudView` (the raid's real HUD) is deliberately all
IMGUI (see the crosshair entry above), built this way so the game is legible before any canvas art
is authored.

**Decision.** Enemy health bars are drawn by `RaidHudView.DrawEnemyHealthBars`, projecting each
living guard's world position (`CastleGuard.Active`, a self-registering static list mirroring the
existing `Intruders` pattern) through `Camera.main.WorldToScreenPoint` and drawing a small IMGUI bar
there, using the same `DrawBar` helper the alarm and player-health bars use. `HealthBar.cs` was left
alone rather than wired up.

**Why.** Standing up `HealthBar.cs` would mean authoring a uGUI canvas + slider prefab per enemy
and placing it in every enemy prefab, which is real art/prefab work this pass isn't scoped for, and
it would leave two parallel health-bar systems (one IMGUI, one uGUI) rather than one. Projecting
from the existing IMGUI view costs one method and no new assets, matches `docs/Decisions.md`'s
existing "swap for a canvas when the art pass arrives" plan for the rest of the HUD, and gives every
enemy a bar today rather than only the ones someone remembers to wire a prefab for. When the uGUI
art pass happens, `HealthBar.cs` is the natural component to revive — or delete, if the projected
IMGUI bars are kept.

**Status.** Current.

## 2026-09-18 — The raid scene is authored; the builder gets a scaffold path

**Context.** `RaidSceneBuilder.BuildPlayer` assembled a player carrying
`FreeLookPlaytestController`, but the committed `RaidScene.unity` carried `PlayerStateMachine` +
`PlayerInputController` — the scene had been hand-edited away from what its own builder produced.
Running *Build Playable Raid Scene* would have silently swapped the real player for the ItemGym
harness and, after the same day's issue-9 work, removed the input gate with it. The requested
direction was to stop regenerating the scene and author it instead.

**Decision.** `RaidScene.unity` is authored and nothing regenerates it. `RaidSceneBuilder` writes
`RaidScene.Scaffold.unity`, and asserts at entry that its output path is not the authored one. The
raid player is extracted to `Prefabs/RaidPlayer.prefab`, which both the authored scene and the
scaffold instance, and `BuildPlayer` instantiates that prefab rather than assembling a rig. The
fallback path, used only when the prefab is missing, builds the shipping controller — not the
harness.

**Why.** A generator and a hand-edited artefact cannot both own one file; whichever ran last won,
which is not a rule anyone can reason about. Giving the generator a different path costs nothing —
the scaffold is still useful for checking the catalogues assemble — and removes the whole class of
"a tool quietly ate a day of authoring". Making the player a prefab is what stops the two scenes
disagreeing about what a player is a second time.

**What replaces the builder's guarantee.** A generated scene re-wired its references every run, so a
dropped reference healed itself. An authored one does not. `AuthoredRaidSceneTests` asserts the
player is an instance of the authored prefab, carries the shipping controller and not
`FreeLookPlaytestController`, and that every serialised reference on `RaidDirector` and
`RaidHudPresenter` is still assigned.

**Not done.** `Prefabs/Player.prefab` is left alone: it is `TestSceneBuilder`'s third-person
2.0 m × 0.5 m rig for `TestScene.unity`, a different thing that happens to share a name shape.
`EnemyPrefabForge` is also untouched — issue 94 stands.

**Status.** Standing.

## 2026-09-18 — Issue 9's gate belongs on the raid's player, not only on the playtest harness

**Context.** Issue 9 (the player moves and looks while the main menu is open) was implemented by
gating `FreeLookPlaytestController` and `PushToCastController` on `GameServices.IsPlaying`, and a
PlayMode test was written against `FreeLookPlaytestController`. `RaidScene.unity` does not contain
that component — it carries `PlayerStateMachine` plus `PlayerInputController`. The harness gained a
gate, the shipping player did not, and the test passed anyway.

**Decision.** `PlayerInputController` gates every input path it owns — the per-frame look, and the
Move/Attack/Jump/Dodge/item-click callbacks — on `GameServices.IsPlaying`, and clears held movement
on the frame the gate closes. The rule is exposed as the pure predicate
`PlayerInputController.AcceptsInputIn(GameState)`, mirroring `CursorLockPolicy.ShouldCapture`, and a
test asserts the two agree state for state.

**Why.** Clearing movement is not optional: `MovementDirection` persists between Input System
callbacks, so gating the callbacks alone leaves the body travelling on the last value delivered
before the menu opened. Making the rule a pure predicate rather than an inline
`GameServices.IsPlaying` check is what lets the cursor rule and the input rule be asserted against
each other — a menu the cursor is free on but the player still walks behind is precisely the bug,
and that class of disagreement is now a test failure rather than a playtest report.

**Why it was missed.** `FreeLookPlaytestController`'s own doc comment described
`PlayerStateMachine` as bound to "a player prefab that does not exist yet," which had stopped being
true by the time issue 9 was worked. A component's comment claiming it is the only playable body is
not evidence that it is in the scene; the scene file is. That comment now says where it is actually
used (`ItemGym.unity`) and warns that changes made there do not reach the raid.

**Status.** Standing. Supersedes the scope, not the mechanism, of "One owner for the cursor; input
gates on the state variable" — that entry's rule was right and its coverage was incomplete.

## 2026-09-17 — Melee hit detection has no enemy layer to filter on

**Context.** Issue 37 asked for reach-based melee hit detection. The obvious approach —
`Physics.OverlapSphere` filtered to an "Enemy" layer mask — doesn't work here: `ProjectSettings/TagManager.asset`
defines no layers beyond `Default`, and every serialized `LayerMask` field found across the codebase
(including the pre-existing, unused `PlayerStateMachine.EnemyLayers`) is `m_Bits: 0`, meaning it was
never actually configured to select anything.

**Decision.** `MeleeWeapon.DealDamage` overlaps all colliders at the swing's reach point and filters
by `TryGetComponent(out IHealth)` rather than by layer — the same pattern `Item.OnCollisionEnter`
already uses for physics-impact damage.

**Why.** Adding a new project layer is a manual Unity Editor step (Project Settings → Tags and
Layers) that can't be scripted from outside the Editor and that nobody would remember to do on
every new enemy prefab. `IHealth` is already the trait that distinguishes a damageable thing from
scenery in this codebase, so reusing it needs no scene configuration and can't silently miss an
enemy that was never assigned to the right layer. The player's own capsule is never at risk of
self-hits because the hit point is projected `Reach` metres in front of the swing origin, clear of
the player's own collider radius.

**Status.** Standing. Revisit if a real "Enemy" layer gets introduced for another reason (e.g.
occlusion queries) — at that point the overlap could add the layer mask as a first-pass filter
ahead of the `IHealth` check, purely as a performance optimization.

## 2026-09-17 — Melee weight is read from InventoryItem, not duplicated onto MeleeWeaponStats

**Context.** Issue 37's melee system needs a weapon's weight to drive both swing speed and damage.
`InventoryItem.Weight` (in stone, matching the pitch's Heft column in `docs/plunderspell.md`, "I
present the field") already exists and is already authored on every weapon asset (`BronzeSword.asset`,
`Longsword.asset`).

**Decision.** `MeleeWeaponStats` holds a reference to the weapon's `InventoryItem` and reads
`Weight` from it (`MeleeWeaponStats.Weight => m_item.Weight`) rather than declaring its own weight
field.

**Why.** A weapon's carry weight and its swing weight are the same physical fact; a second field
would need to be kept in sync by hand on every weapon asset and would eventually drift (DRY). The
new `ArmingSword.asset`/`ArmingSword_MeleeStats.asset` pair follows this: `Weight: 2` (stone, per the
pitch's "2 st" Heft for the arming sword) lives once, on the `InventoryItem`.

**Status.** Standing.

## 2026-09-22 — A ranged shot spawns ahead of the wielder instead of tracking their colliders

**Context.** Issue 39's Crossbow fires a real `Rigidbody` projectile (`NetworkedProjectile`, reusing
the existing `Bolt.prefab`). `CastleGuard.FireAt` and `SpellBook.CastSpell` both solve the "don't hit
your own collider" problem by walking the shooter's own colliders and calling
`Physics.IgnoreCollision` against the shot — but both live on the shooter itself, where
`GetComponentsInChildren<Collider>()` finds those colliders. `RangedWeapon` lives on the held
*weapon* item, not on the player, so it has no direct handle on "the wielder's colliders."

**Decision.** `RangedWeapon.SpawnProjectile` instantiates the shot at `origin + direction * 0.8`
(a fixed muzzle offset) rather than at the camera/eye position, and does no collision-ignore
bookkeeping at all.

**Why.** 0.8m clears the player capsule's 0.5m radius by construction, so there is nothing to
ignore. This is the same idea already used for spell bursts — see "A spell burst is centred on where
it lands, not where it starts" above — moving the spawn point instead of suppressing the collision
it would otherwise cause.

**Status.** Standing. Revisit only if a future weapon's muzzle needs to differ from this fixed
offset (e.g. a much bigger holdable prop).

## 2026-09-17 — Four doorways on every room plus plugs, rather than socket-matched placement

**Context.** Issue 5 (rooms do not connect, doorways do not align) and issue 19 (modules do not
fill the 12 m grid cell). The generator places a module on a grid cell and rotates it to face the
already-placed neighbour it attached to; it never inspects the module's geometry. Rooms were
authored with archways on an arbitrary one to four sides, so a placed room routinely met its
neighbour archway-to-blank-wall.

**Decision.** Author every enclosed room with an archway on all four sides, and seal the archways
that end up facing an empty cell after placement, using one plain stone door-plug prefab per
enclosed zone. Set `room_kit.FOOTPRINT` to 12.0 so a module fills its cell exactly.

**Why.** The alternative considered was the one the original issue text suggested: have the
generator read each candidate's sockets and choose or rotate a module whose doorways line up. That
makes placement a constraint-satisfaction problem, can fail to place anything on a cell, and
couples the layout algorithm to the art. Four openings everywhere makes door alignment
structurally true instead of something the algorithm has to achieve, at the cost of one extra
prefab per zone and a post-pass. Rejected as over-engineered for a layout that is already
guaranteed 4-connected by construction.

**Status.** Standing.

## 2026-09-17 — One standard human at 1.80 m, with enemies scaled by the forge rather than re-modelled

**Context.** Issue 6. The player was 2.0 m, rooms were 2.6–5.2 m, and the ten enemy models were
authored between 0.77 m and 3.41 m tall with no common reference. The `GildedColossus` at 3.41 m
did not fit in the Crypt it spawns in.

**Decision.** A standard human is 1.80 m (eyes 1.65 m). Per-zone room heights are raised against
it, and `EnemyPrefabForge` carries a standing height per enemy and scales each model uniformly
from its authored height to that figure. The models themselves are untouched. Written up in
`docs/systems/scale.md`.

**Why.** Re-authoring ten rigged, skinned models in Blender to agree on a metre is a large change
with real risk to the rigs, and it puts the game's scale standard somewhere nothing can check it.
A number in the forge's spec table is one line per enemy, is visible next to the rest of that
enemy's tuning, and is applied by the same tool that already authors the prefabs.

**Status.** Standing.

## 2026-09-16 — Complete the docs structure by salvaging an abandoned scaffold branch, not rewriting it

**Context.** Running a `docs/` structure/audit pass, `origin/claude/repo-status-check-hjp6z7` was
found: a fully-written five-tier scaffold (tiers 1, 2, 3, 5, plus `docs/systems/README.md` and
three previously-missing system docs — `voice.md`, `castle.md`, `alarm.md` — and the
`docs/archive/2026-09-15-integration/` move) committed once (`d721d7b`) and never merged. It was
cut from a point 11 commits behind current `main`, so its tier 1/2/3/5 drafts were stale relative
to the raid-scene-assembly work, the menu/lair navigation feature, and the 2026-09-16 issue
backlog that landed afterward — but its tier-4 system docs (voice, castle, alarm) covered code
that hadn't changed at all in the interim.

**Decision.** Adopt the branch's `voice.md`, `castle.md` and `alarm.md` verbatim; rewrite
`README.md`, `Roadmap.md`, `ProjectState.md` and `Today.md` using its structure and reasoning as a
base, updated for what shipped since; add the tier 6 (`Decisions.md`) and `docs/generated/` it
didn't have; and additionally move the still-loose HTML previews and the live GitHub issue
manifest into `docs/generated/`, which the branch had left at the `docs/` root.

**Why.** The branch's tier-4 work was accurate, well-cited, and covered systems (voice, castle,
alarm) that no other doc addressed — discarding it and re-deriving the same analysis from scratch
would have cost real effort for no better result. Its tier 1/2/3/5 drafts needed updating either
way, since a project's current state is exactly the part of this structure that's expected to move
between passes.

**Status.** Standing.

## 2026-09-16 — `RaidSceneBuilder` aborts when a catalogue is missing, rather than falling back to primitives

**Context.** The scene builder used to generate its own placeholder box room, gold cube and
capsule guard whenever the room/loot/enemy catalogues were empty. Meanwhile the project already
contained 25 modelled castle rooms, 5 modelled loot prefabs and 10 rigged enemy models, wired into
complete `ScriptableObject` catalogues — none of it referenced by anything. The placeholder
fallback was silent, so this went unnoticed for the length of an entire asset-pipeline effort. See
`docs/systems/raid-scene-assembly.md` ("The placeholder era").

**Decision.** `RaidSceneBuilder` now aborts the build and names every missing catalogue by name,
instead of silently substituting primitives.

**Why.** A silent fallback is precisely what let real, already-finished art sit unused — a loud
failure at build time makes a missing catalogue impossible to miss again.

**Status.** Standing.

## 2026-09-16 — Compose spawn rotations with the prefab's own rotation; never replace it

**Context.** Castle room, loot and enemy prefabs each carry a different Blender-to-Unity axis
correction baked into their root rotation (see `docs/systems/raid-scene-assembly.md`,
"Orientation"). `RaidSceneBuilder` and `ProceduralCastleGenerator.PlaceModule` called
`Instantiate(prefab, pos, rot, parent)`, which overwrites a prefab's root rotation outright —
laying every castle room on its edge.

**Decision.** Every spawn site composes instead: `Instantiate(prefab, pos, rot *
prefab.transform.rotation, parent)`.

**Why.** The three prefab families don't agree on where their axis correction lives, and fixing
that inconsistency would mean re-authoring art. Composing rather than replacing respects whatever
convention each family already uses, and is the minimal code-only fix.

**Status.** Standing.

## 2026-09-15 — Loot placement stays a pure function; the physics-fling defect is not patched by raycasting

**Context.** `LootPlacementPlanner` places loot 0.5 m above a room's centre as a pure function of
(layout, table, seed) — no scene, no components, no time. Against real room geometry, this can
land inside a wall or prop, and PhysX ejects the overlapping rigidbody hard: measured at roughly
2–7 of ~19 pieces flung per raid. An attempt to fix this by raycasting downward for the floor made
it measurably worse (15/22 flung), because loot ended up landing on room roofs instead.

**Decision.** Revert the raycast attempt. Leave the fling defect open and documented
(`docs/systems/raid-scene-assembly.md`, "Traps"; tracked as GitHub issue #20) rather than
compromise the planner's purity for a fix that didn't work.

**Why.** The planner being pure — knowing the layout and the seed but nothing about mesh geometry
— is what makes placement *rules* assertable in a test rather than eyeballed in the editor. A real
fix needs the *spawner* (which does touch the scene) to find a clear resting spot, not the planner
to stop being pure.

**Status.** Standing — open defect.

## 2026-09-14 — Authority checks read `isSpawned && !isServer`, never `isServer` alone

**Context.** PurrNet's `isServer` is false both on a real client and on an object that was never
spawned onto the network at all. `if (!isServer) return;` therefore silently disables a system in
single-player, because an unspawned object looks identical to a client to that check. This caused
three separate, independently-discovered silent failures: voice casting (the entire game did
nothing offline because `OnSpawned` never fired, so nothing ever subscribed), the extraction
clock (never counted down), and trigger tracking.

**Decision.** Every authority check in the project reads `if (isSpawned && !isServer) return;` —
an unspawned object is treated as its own authority.

**Why.** This is the one check that's correct in both single-player (unspawned) and real
multiplayer (spawned client), without a separate offline code path. See `docs/systems/raid.md`,
`docs/systems/voice.md` and `docs/systems/alarm.md` for where this bit.

**Status.** Standing — the underlying trap is a property of PurrNet's authority model, not
something the codebase can rule out for a future `NetworkBehaviour` that skips the `isSpawned`
half.

## 2026-09-11 — `feature/Owen/PCG` is a clean-room boundary

**Context.** A branch survey for the Plunderspell pivot found `feature/Owen/PCG`, 9 commits of
procedural room generation by a contributor who has since left the project. Before the
restriction below was decided, `ProceduralRoom.cs` (~78 lines) was read in full, along with that
branch's file list, as part of the same survey.

**Decision.** The castle generator is written from the pitch/plan specification and general
castle architecture only. `feature/Owen/PCG` is never opened, diffed, merged, cherry-picked or
rebased into this lineage again. The one file already read is recorded rather than treated as
unread: it implements a recursive branching room-web (random rotation, retry-on-overlap), which is
a different algorithm from the deterministic outward-in ward nesting this project's generator
uses (`docs/systems/castle.md`).

**Why.** Avoids any dependency — even a convergent, coincidental one — on code from a departed
contributor whose branch was never a sanctioned reference for this codebase.

**Status.** Standing.

## 2026-09-11 — Fork the wizard pivot from `claude/steam-multiplayer-framework-xia7ch`, not `main`

**Context.** A branch survey ahead of the Plunderspell pivot found the branch that was checked
out at the time was a stale copy of `main`, 30 commits behind. `claude/steam-multiplayer-framework-xia7ch`
already contained a working first-person Rigidbody FSM, physics grab/carry/throw, a working
`SpellBook`, Steam multiplayer via PurrNet, and a clean `Core` module — roughly 2,900 lines beyond
`main`.

**Decision.** Reset the working branch onto that trunk (`git checkout -B <branch>
origin/claude/steam-multiplayer-framework-xia7ch`) rather than building the pivot from `main` or
continuing on the stale branch.

**Why.** Building from `main` would have meant re-deriving nearly 3,000 lines of already-working
FSM, physics and networking code from scratch for no benefit.

**Status.** Standing.

## 2026-09-11 — Voice casting is on-device keyword spotting; no audio ever leaves the machine

**Context.** Decided with the user while scoping the voice-casting pillar of the Plunderspell
pivot (`docs/plans/plunderspell.md`, "Decisions taken with the user").

**Decision.** Recognition runs entirely on the player's own machine (Vosk, with a keyboard mock
where no microphone is available). Only the recognised spell id crosses the network, as a PurrNet
RPC — never raw audio.

**Why.** Stated as a design pillar, not just an implementation detail: no server cost per player,
no latency floor from a network round trip, and no voice data ever leaves the house
(`docs/plunderspell.md` §4).

**Status.** Standing.

## 2025-09-17 — `SingletonBase.Instance` never auto-creates a scene owner

**Context.** The originally ported `SingletonBase.Instance` auto-created a `GameObject` when
`_instance` was `null`, which meant `EventManager.Instance?.Publish(...)` could never actually
observe a missing instance, and spawned a stray object after scene teardown. It also always called
`DontDestroyOnLoad`, ignoring the `PersistBetweenScenes` flag's own stated purpose.

**Decision.** The getter returns `null` when no instance has claimed ownership, instead of
self-creating one; `DontDestroyOnLoad` is only called when `PersistBetweenScenes` is true.

**Why.** An absent instance is information a caller needs (no scene owner has been set up yet),
not a problem to paper over by spawning a `GameObject` nobody asked for. See
`docs/systems/core.md`'s invariant: "`SingletonBase.Instance` never creates anything."

**Status.** Standing.

## 2026-09-17 — Castle rooms stay open-topped until the loop is proven fun

**Context.** Closing out Issues 5/6/19/25, the new multi-angle captures
(`docs/generated/castle-screenshots/`, written by `CastleScreenshotForge`) showed that
`room_kit.room_shell` builds a floor and four walls and no roof, so every enclosed room is open to
the sky. An overhead plan cannot show this — you are looking down into the rooms either way — which
is why it survived this long unnoticed.

**Decision.** Rooms stay roofless for now. Roofing is deferred to a later phase, and explicitly
**not** pinned to Phase 5 / Issue 23: it is a correctness gap, not an art-polish one, so it should
be picked up as soon as the core loop is proven fun rather than waiting for the art pass. The flat
silhouette found in the same pass (nothing masses above the curtain wall but the corner drums)
*is* ordinary blandness and does belong with Issue 23 in Phase 5.

**Why.** The user's standing directive is to get each feature working and find out whether the game
is mechanically fun before doing more art. A roofless castle is fully walkable, lootable and
testable, so roofs block nothing in Phase 1. They are a precondition for the lighting the pitch
calls for ("one candle, one fire, falling into black", `docs/plunderspell.md`), which cannot read in
a room open to the sky — so this must not be lost.

**Status.** Standing. Not currently covered by any of the 55 filed backlog issues; this entry is
the only record.

## 2026-09-17 — Loot reads through glow and prompt; size is tuned on the prefab, not in Blender

**Context.** Issue 15 asked for loot to be discoverable, and offered a choice: fix the authored size
in `Tools/AssetPipeline` (the root cause) or scale the prefab.

**Decision.** Scale on the prefab root (`AncientRelic`/`CopperPot`/`SilverPlate` ×2.0,
`GoldenGoblet` ×2.2, `HeavyChest` ×1.4). The builders in `Tools/AssetPipeline/builders.py` were left
alone.

**Why.** The meshes are not authored wrong — a copper pot really is 26 cm across, a goblet 19 cm
tall. What loot needs is a *gameplay readability* multiplier, which is a tuning value and not a
property of the model. Changing the builders would also invalidate each prefab's baked `BoxCollider`
extents, which are mesh-derived, forcing a full Blender rebuild plus manifest and preview churn plus
an extra Editor re-bake pass. Scaling the prefab root moves mesh and collider together in one edit.

**Status.** Standing. Revisit if the art pass re-authors these meshes anyway.

## 2026-09-17 — Focus glow is a property block, added at runtime

**Context.** Issue 15 wanted the focused item highlighted.

**Decision.** `LootHighlight` pushes an `_EmissionColor` through a `MaterialPropertyBlock`, and
`LootInteractor` attaches it to an item the first time that item is looked at rather than it being
authored onto each prefab.

**Why.** Emissive brightening needs no render-feature wiring, so it works whichever pipeline the
project lands on, and a property block never writes to the shared material asset or allocates a
per-item material instance. Attaching at runtime means conjured loot, tooling-built scenes and
test-built loot all highlight, and no piece of loot can ship without it.

**Status.** Standing. An outline shader is the obvious upgrade once a render pipeline is settled.

## 2026-09-17 — One owner for the cursor; input gates on the state variable

**Context.** Issues 8 and 9. `PlayerInputController` locked the cursor in `Awake` and freed it when
the inventory key was pressed, while nothing else had an opinion, so the cursor ended up stuck
between a menu and the world.

**Decision.** `CursorLockPolicy` (on the bootstrapped `UIRoot`) is the only thing in the game that
touches `Cursor`. It follows `GameStateManager.StateChanged` and implements `OnApplicationFocus`.
The writes in `PlayerInputController` were removed. `FreeLookPlaytestController` and
`PushToCastController` gate on `GameServices.IsPlaying` at the top of their input reads.

**Why.** With two writers the one that ran last wins, which is not a rule anyone can reason about.
Gating on the state variable rather than enabling/disabling the components is deliberate: a
component switched off and on again re-runs `Awake` wiring and loses the camera and rigidbody it
resolved — Issue 9's plan calls this out explicitly.

**Status.** Standing.

## 2026-09-17 — The crosshair is IMGUI, and therefore invisible to the screenshot test

**Context.** Issue 7 placed the crosshair in `RaidHudView`, which draws with IMGUI.

**Decision.** The crosshair is generated in code (no asset, no third-party dependency) and drawn in
`RaidHudView.DrawCrosshair`, visible only in `GameState.Playing`.

**Why.** `RaidHudView` is deliberately IMGUI so the raid is playable from a code-built scene with no
authored prefabs. The cost is that `UIScreenshotPlayModeTests` captures through a camera into a
`RenderTexture`, and `Camera.Render()` never invokes `OnGUI` — so the committed `02_HUD.png` cannot
show the crosshair or the interact prompt no matter how well they work. Visual proof of the HUD has
to wait for the uGUI/art pass that replaces `RaidHudView` against the same `RaidHudModel`.

**Status.** Standing, and a known gap in the screenshot evidence.

## 2026-09-21 — Enemy feet are measured on vertices, and the model is grounded, not the prefab regenerated

**Context.** Issue 94 reported three enemies off the floor: `ArcRevenant` up 0.15 m, `GildedColossus`
and `VaultWarden` down 0.16 m and 0.12 m. The measurement behind it read `Renderer.bounds`.

**Decision.** Feet are measured on baked vertices (`PrefabGeometry`), not bounds. `EnemyPrefabForge`
now grounds each model after scaling (`GroundModel`), and the one prefab that was really off the floor
was repaired in place with `Ground Enemy Prefabs In Place` rather than by re-running the forge.
`SigilWisp` (0.072 m up, inside the 0.10 m tolerance) was left as a hovering orb.

**Why.** Photographed on a ground slab, only `ArcRevenant` floats; `GildedColossus` and `VaultWarden`
stand exactly on the floor and the bounds were padded. A re-forge regenerates all ten prefabs and
would discard the hand edits since commit 7a3ec27, so the repair touched one prefab and left nine
byte-identical. Evidence: `docs/generated/enemy-stance-screenshots/`.

**Not decided.** The same padding makes the forge under-scale most enemies (`WarHound` draws 0.685 m
against a 0.85 m table entry). Correcting it resizes the whole cast against the rooms and archways, so
it was recorded in `docs/systems/scale.md` "Traps" and left for a design call.

**Status.** Standing.

## 2026-09-22 — Voice casting listens for English spellings, and the Vosk binaries are committed

**Context.** The user played the standalone build and could not cast by voice. Investigation found
voice recognition had never worked in any build: the repo carried only a throwing compile stub for
Vosk and an empty model folder; the capture loop called main-thread-only `Microphone` APIs from a
worker thread and hid the exception; the mic was `devices[0]` (a virtual device on the dev machine);
and every push-to-talk cast was classed as a Whisper. Once the real engine was installed, a test
with synthesized speech showed the deeper problem: the English model has no Latin, so it heard
FRANGO as "franco" and IGNIS as "agnes" — the misfire spellings.

**Decision.** Commit the real `Vosk` 0.3.38 native/managed DLLs and the small English model (with
the user's approval — reversing the old "too large to commit" note in the model folder's README, in
line with the repo's commit-everything rule). Constrain recognition to a grammar of English
spellings authored per `SpellWord` (`HeardAs`, `MisfireHeardAs`) and map each back to the Latin
word. Where the model can't separate a right/wrong pair (FRANGO/FRANCO), the ambiguity resolves to
the **correct** spell. Always offer number-key casting alongside speech.

**Why.** A grammar was measured, not guessed: 16/16 correct pronunciations cast the right spell
across two synthetic voices, versus most of them misfiring free-form. Failing toward the correct
spell keeps "say it right and it works" true; a false misfire on a correct word would make the
pillar feel broken. Rejected: a bigger Vosk model (still no Latin, 3–30× the size); fuzzy phonetic
matching over free-form text (can't separate IGNIS from AGNIS when both come back "agnes").

**Not decided.** The heard-as spellings are tuned against text-to-speech only. A real person, and
the multi-accent measurement issue #50 asks for, may need different ones — tune them in the
`SpellWord` assets and re-run `SpeechRecognitionTests`.

**Status.** Standing. Mechanism: `docs/systems/voice.md`, "Latin through an English model".

## 2026-09-23 — Dying ends the raid on a "You died" screen; the scene reload is gone

**Context.** `PlayerDeadState.Enter` reloaded the active scene. In play that wiped the raid with no
explanation and left `GameState` on `MainMenu` with no menu showing; the player had no way to know
they had died, let alone why (a guard posted next to the spawn, see `raid.md` "Invariants").

**Decision.** The local player's death switches to `GameState.GameOver` ("YOU DIED — the castle
keeps everything you didn't carry out"), `RaidBootstrapper` abandons the raid through
`RaidDirector.AbandonRaid` (nothing banked, castle and guards cleared), and the screen's button
goes to the Lair. Setting out again revives the player at full health. `ReviveTo` now enters Idle,
not `PlayerRespawnState`, whose exit ran on a thread-pool task and never took effect.

**Why.** A lost raid is the consequence the pitch describes — the loot stays behind, the debt still
grows — and it keeps the Lair → raid loop intact. A reload threw away the Lair's state and the
session's flow for no gain.

**Not decided.** Co-op downing (`DownedPlayerCarryAdapter` exists) should replace instant death
once there is more than one player; with one player, down is out.

**Status.** Standing. Mechanism: `docs/systems/damage.md`, "How it works".

## 2026-09-23 — #100's root cause was the voice pipeline, not a missing component

**Context.** #100 said `Player.prefab` lacked `SpellCastingSystem`, `AcousticEmitter` and
`FootstepNoiseEmitter`, and that this was why casting failed in the shipped raid.

**Finding.** `RaidScene.unity` instantiates `RaidPlayer.prefab`, not `Player.prefab`, and
`RaidPlayer.prefab` has all three plus `PushToCastController`, `StatusEffectReceiver`,
`IntruderTag` and `LootInteractor`. `RaidSceneCastingTests` passes 2/2 in the real Editor. The
headless harness's failure of that test is a shim-fidelity gap (it cannot load a real `.unity`
scene), not this bug. Casting failed in the build because the speech engine was a throwing stub
and a machine with a microphone lost the number-key fallback — both fixed in `1dae40b` (see
"Voice casting listens for English spellings", above).

**Decision.** Leave `Player.prefab` as is; it is not what the raid spawns.

**Status.** Standing.

## 2026-09-23 — The player body interpolates, and mouse yaw turns the camera, not the body (#104)

**Context.** #104: "items and enemies lag, a slight motion blur, ~100 ms". Measured in Play mode at
~500 fps: the camera (a child of the player's rigidbody) was frozen on 90% of rendered frames. It
only moved on 50 Hz physics steps, because the body had no interpolation, while interpolated held
items moved every frame. Guards were frozen on ~35% of frames: a NavMeshAgent moved them each frame
while a dynamic rigidbody on the same object wrote its own position back each physics step. No
motion blur (intensity 0) or temporal anti-aliasing is involved.

**Decision.** The player's rigidbody interpolates. Because interpolation overwrites any rotation set
on the body's transform between steps (mouse look turned 2.5° instead of ~24° when tried), yaw now
goes on the camera (`PlayerStateMachine.Look` sets the Eye's local yaw and pitch) and the capsule
never turns. Movement, dodge, aiming, spells and melee all read the camera, so nothing needed the
body's facing (dodge was the one exception, now camera-relative). Guards with a NavMeshAgent get a
kinematic rigidbody: still solid, still hit by thrown things, no longer fought by physics. After the
fix, camera, guards and held items all move on 100% of frames.

**Also.** `PlayerIdleState` now zeroes horizontal speed. The body hovers on its ground snap and
never touches the floor, so nothing else bled off speed; arriving in Idle while moving coasted off
the map.

**Status.** Standing. Regression tests: `PlayableLoopTests.Test_TheViewMovesEveryFrameAndLookTurnsTheCamera`,
`Test_IdleStandsStill`.

## 2026-09-23 — Pausing freezes the world, but only for the host (#107)

**Context.** Esc swapped screens and nothing else: the raid clock, guards and physics kept running
behind the pause menu.

**Decision.** `PausePolicy` (on the persistent UI root, beside `CursorLockPolicy`) sets
`Time.timeScale = 0` and pauses audio while the pause menu, or Settings opened from it, is up, on the
machine that owns the simulation only (`GameServices.IsSessionAuthority`: the host or an offline
player; the raid supplies the network check). A client's pause menu stays an overlay, because it
cannot stop three other people's game. The inventory is not a pause.

**Trap.** Anything waiting on scaled time (`WaitForSeconds`, `WaitForFixedUpdate`) never finishes
while paused. One test did exactly that and hung the suite; it now uses the inventory to test the
"menus block input" rule.

**Status.** Standing. Verified in Play mode: the clock held at 295.00 s and a guard held still for
2 s while paused, stayed frozen in Settings, and resumed on Esc.

## 2026-09-23 — Frango is a force blast, not a loot-breaker; Levo lifts guards (#106)

**Context.** #106: "make other spells actually useful, Ignis is by far the most usable." Checked
what each spell could affect. Frango shattered every `IBreakable` within 4 m, and the only
`IBreakable` in the game is `LootPickup`, so the spell could only destroy the players' own haul.
Its old doc comment said so on purpose ("casting it near the haul is how a raid loses its payday").
Levo couldn't lift a guard once guard bodies became kinematic (#104). Every spell also targeted
whatever was nearest a point in front of the caster's face, not what they aimed at.

**Decision.** Spells aim (see `spells.md`, "Spells go where you aim"). Frango now blasts what you
aim at: 30 damage (× volume power) through `Damage.Apply`, a 1.2 s stagger and a 2.5 m shove (via
the NavMeshAgent, so never through a wall), or it smashes an aimed door open, locked or not
(`IHandOpenable.ForceOpen`, which is loud). It no longer touches loot. Levo on a guard suspends its
agent, raises it 1.8 m (helpless: levitating now counts as incapacitated), then drops it under real
gravity for 9 damage per metre, blamed on the caster, and puts it back on the navmesh. All numbers
are in the `SpellTuning` asset.

**Reverses.** Frango's "breaks your own loot" risk. That risk still exists as Frango's *misfire*
("breaks a random inventory item"), which is where a punishment for speaking badly belongs.

**Not decided (needs the user).**
- **Cadaver Surge** raises an event nothing listens to, so it does nothing. Raising a corpse as a
  temporary ally, or as a noisy lure, are both real designs; neither is guessed at here.
- **Porta has nothing to open.** `CastleDoor` is never placed: no prefab or scene contains one, so
  the alarm's lockdown locks nothing either. Doors belong in the castle revamp.

**Status.** Standing. Verified in Play mode: `playtest-2026-09-23/27` (aimed Ignis), `/28` (Frango
−30 and a 2.5 m shove), `/30` (a Levo'd guard floating 1.8 m up, then −17 on landing). A locked test
door opened to Frango.

## 2026-09-23 — Every session is networked; solo is a host nobody can join

**Context.** The user asked to invite a friend over Steam from a standalone build. The build ships
`RaidScene` alone, and that scene had no `NetworkManager`, no transport and nothing that started
Steam; its player was placed in the scene. Wiring co-op on beside a scene-placed player does not
work: that player's network components have the same scene ID on every machine, so a friend's
casts and pickups would land on the host's body.

**Decision.** `RaidScene` gets a `Network` object (`NetworkManager`, Local/UDP/Steam transports,
`PlayerSpawner`, `CoopSession`). Every session starts through `CoopSession`: Play Solo hosts on
`LocalTransport`, Host Co-op hosts on Steam (UDP without Steam), and a friend joins as a client. The
player is spawned per connection from `RaidPlayer.prefab`, and ownership decides which body each
machine drives. Mechanism: `docs/systems/net.md`.

**Considered and rejected.** Keeping solo offline and adding network "avatars" for remote players.
Two code paths for the raid, and the scene player's collision with itself across machines would
still need removing. Solo as a local host costs one path through the server/client split the raid
was already written for, and that path now runs in every test that loads `RaidScene`.

**Reverses.** "The player is a prefab instance placed in `RaidScene`"
(`AuthoredRaidSceneTests`, `raid-scene-assembly.md`). The tests now check the spawner instead.

**Status.** Standing. Checked on one machine: Editor host + built client over UDP (both build the
same castle, each sees the other, both directions replicate), solo raid (spawn, cast, carry,
extract 800), and Steam in the build (signed in, lobby created, hosting on `SteamTransport`, the
invite list shows online friends). **Not yet checked:** a second Steam account joining, which needs
the user and a friend.

## 2026-09-23 — The raid player copies the CastleBench player

**Context.** Playing the co-op build, the user found spells did nothing and the view sat inside
door lintels. The build spawns `RaidPlayer.prefab`, which had differed from the player built into
`CastleBench.unity` since `5052c97`: push-to-cast on F19 instead of V, the eye 1.65 m above the
capsule's centre instead of 0.75 m, mass 70 instead of 1, and other controller values. The user
had tested voice on the CastleBench player, so the fault never showed until the raid scene shipped.

**Decision.** The user named the CastleBench player as correct. `RaidPlayer.prefab` now matches it
field for field, keeping only `NetworkTransform` and `PlayerNetworkOwnership` on top. That removed
`LootInteractor` (E to pick up, Q to drop), which the bench player does not have; pickup is
`ItemManager`'s mouse drag. The prefab asset was edited in place rather than replaced, so the
network spawner's reference to it holds.

**Status.** Standing. Verified in a solo raid: V + 1 cast Ignis, V + 5 cast Tonitrus, and the
spawned player stands exactly as the bench player does (eye 1.94 m above the floor in both).

## 2026-09-23 — Hits go to whoever owns the target's health; the dead watch a teammate

**Context.** Stage 4 of `docs/plans/steam-coop-raid.md`. Health lives on one machine per target,
and `Damage.Apply` is the one path every hit takes, so the question was where a hit is applied.
The user also asked that a player who dies in co-op spectates rather than ending the raid.

**Decision.** `Damage.Apply` offers each hit to `Damage.Forward`, installed by `DamageRelay`: hits on
server-spawned things are applied on the server, hits on a player's body on that player's machine.
The hitter gets the result back for its feedback. In co-op a dead player watches a standing
teammate through their eye, and the raid is lost only when the server sees every body down; solo
death is unchanged. A client shows the host's campaign without saving it.

**Considered and rejected.** Server-authoritative player health: the HUD, the damage flash and the
death screen all live on the owner's machine, and the raid's player code was written for local
health, so each would have needed a replicated mirror. Loot carried with no ownership change (the
server simulating what a client holds): the carrier would feel a round trip of lag on every swing.

**Status.** Standing. Verified over UDP with two game windows (see the plan's stage 4 list);
regression tests in `CoopRulesTests`. Untested over Steam between two accounts.

## 2026-09-23 — Weapons are loot you find; a raid starts with a short grace

**Context.** The user asked for weapons to be networked, and described them: "weapons are just
regular items but they can be used via right click instead of thrown like normal items, this way it
is always a risk to check if its actually a weapon." No weapon existed in `RaidScene`; they were
only in the bench scenes. Separately, guards killed players standing at the spawn.

**Decision.** Weapons spawn in the raid as loot table entries, with loot's components, so they are
networked, carried and sold exactly like loot. Ranged shots are shown on other machines as harmless
copies. Guards keep two rooms clear of the gate, patrols included, and a calm garrison cannot see
anyone for the first 20 seconds of a raid.

**Considered and rejected.** A separate weapon-spawning system: it would duplicate the loot
placement, and the user's point is that a weapon is found like any other item. A larger safe ring:
the castle is about nine rooms across, so three rooms would empty most of it.

**Status.** Standing. Verified with two game windows over UDP and in a solo raid (see the plan).
