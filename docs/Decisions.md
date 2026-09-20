# Decisions

Append-only. An entry is never rewritten or deleted; the one allowed edit is flipping its
`Status` line to `Superseded` when a later entry replaces it. Newest entry at the top.

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
