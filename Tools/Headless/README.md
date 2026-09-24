# Headless verification harness

Plunderspell is a Unity project, and Unity cannot run in CI here (no editor, no licence). This
harness compiles **the exact same gameplay sources** Unity compiles and runs **the exact same test
files** the Unity Test Runner runs — against a shim of the Unity and PurrNet APIs.

```bash
./Tools/Headless/verify.sh          # build + test
./Tools/Headless/verify.sh --build  # compile only
```

It needs a .NET 8 SDK; `verify.sh` looks on `PATH`, then `/opt/dotnet`, then `~/.dotnet`, and prints
the one-line install command if it finds none.

## Layout

| Path | What it is |
| --- | --- |
| `Shims/UnityEngine.*.cs` | The UnityEngine surface the game uses: math, object/component model, physics queries, input, persistence, a slice of uGUI. |
| `Shims/UnityEditor.cs` | The UnityEditor surface the Editor tooling uses: assets, prefabs, scenes, serialized objects. |
| `Shims/PurrNet.cs` | PurrNet 1.15's `NetworkBehaviour` / `SyncVar` / `SyncList` / RPC attributes. |
| `Shims/UnityEngine.TestTools.cs` | `[UnityTest]`, `LogAssert`. |
| `Shims/Unity.AI.Navigation.cs` | `NavMeshSurface`, so `CastleNavMeshBaker` compiles. |
| `Shims/Player.PlayerInputController.cs` | Stub for the excluded `PlayerInputController` (see below), carrying only its real `AcceptsInputIn` predicate. |
| `Plunderspell.Headless/` | Compiles the linked Runtime gameplay sources against the shims. |
| `Plunderspell.Headless.Editor/` | Compiles the linked Editor tooling (scene builders, forges) against the shimmed UnityEditor surface, referencing `Plunderspell.Headless`. |
| `Plunderspell.Headless.Tests/` | Compiles and runs every linked test file (`Tests/Editor`, `Tests/Runtime`, and the two legacy `Tests/EditMode`/`Tests/PlayMode` assemblies) under NUnit, referencing both projects above. |
| `Plunderspell.Headless.Tests/UnityTestBridge.cs` | Drives `[UnityTest]` coroutines, which plain NUnit cannot execute. |

Nothing here is copied. All three projects reference the real files under `Assets/_Project/` via
linked `<Compile Include=...>` items, so the harness cannot drift from the game: a compile error
here is a compile error in the editor.

**The three `.csproj` files above are hand-written and must stay committed.** They were lost once
already to a `.gitignore` rule that swallowed every `*.csproj` in the repo, including these —
see `docs/Decisions.md`, "The headless harness's own project files were never committed". The
gitignore rule is now anchored to the repo root (`/*.csproj`, where Unity/Rider actually dump their
auto-generated ones); `Tools/Headless/**/*.csproj` is intentionally outside that anchor.

## What it does and does not prove

**Proves:** every gameplay assembly compiles; all pure logic (castle generation, A* validation,
misfire resolution, alarm escalation and decay, fragility, carry thresholds, extraction tally, debt
maths, raid orchestration) behaves as specified; components construct, receive their Unity messages
and wire up correctly; acoustic propagation and occlusion, against a real (if simple) AABB physics
world.

**Does not prove:** rendering, animation, NavMesh baking, real PhysX behaviour, or a genuine
multi-peer PurrNet transport. The shim models a single listen-server host: an RPC call runs its body
in place, which is what a host observes. Genuine multi-peer replication still needs the editor's
PlayMode suite. Nor does it prove: loading a real `.unity` scene file (`EditorSceneManager.OpenScene`
returns an empty stand-in scene, not a parsed one — `AuthoredRaidSceneTests` fails here for exactly
this reason and needs the real editor), off-screen rendering (`Camera.Render`/`RenderTexture` are
no-ops, so the screenshot forges run without error but write nothing meaningful), real asset import
(`ArtAssetImportTests`/`ArtAssetImportValidator` need the real importer pipeline), or prefab-instance
correlation (`PrefabUtility.GetCorrespondingObjectFromSource` always returns null). As of this
writing the full suite runs 148/162 green headlessly; the twelve failures are all in these four
categories, not in gameplay logic.

**2026-09-24:** the harness had stopped compiling (the co-op work used PurrNet transports,
`NetworkTransform`, `localPlayerForced` and a dozen Unity APIs the shims lacked), so `verify.sh`
had been failing at the build step. The shims were extended for each missing member, keeping the
semantics honest and noting each approximation inline, and `PlayerNetworkOwnership.cs` is now
compiled (it builds against the `PlayerInputController` stub). Result: **217 tests, 187 pass,
4 skipped, 26 fail.** The failures are fidelity gaps, not gameplay regressions: scene loading
(`AuthoredRaidSceneTests`, `RaidSceneCastingTests`, `CombatBenchCastingTests`), the real importer
(`ArtAssetImportTests`), prefab/asset loading (`ScaleInvariantTests`), no `FixedUpdate` or rigidbody
simulation (`PlayableLoopTests`, `LootSettleAndInputGatingTests`, `Test_SpawnedLootIsFrozen…`),
`GameObject.CreatePrimitive` adding no collider (`SpellVfxTests`), and `Resources.Load`
(`Test_SpellNumbersComeFromTheAuthoredAsset`). Which of those were already failing before the
harness broke is not known: there was no green run to compare against.

**Two deliberate compromises on faithfulness**, both flagged inline where they matter:
- `Renderer.bounds` returns a zero-size box at the renderer's position rather than the mesh's real
  extents, since there is no real mesh data to measure headlessly. Anything that reasons about an
  object's on-screen or world footprint from `Renderer.bounds` (e.g. `EnemyPrefabForge`'s scale
  checks) will not get a meaningful answer from this shim.
- `Camera.WorldToScreenPoint` always returns the screen centre rather than a real projection. See
  `docs/Decisions.md`, "Enemy health bars are IMGUI, projected from world space" — nothing in the
  test suite currently asserts on its actual value, only on whether callers handle "behind the
  camera" and "off-screen" correctly, which the stub cannot exercise either. Fixing it properly
  needs at least an FOV/aspect/viewport model, not a one-line change.

## Adding to the shims

Add only what the game actually calls, and keep the semantics honest — a shim that silently differs
from Unity is worse than a missing one. Two behaviours the shims deliberately reproduce because the
game depends on them: Unity's "fake null" for destroyed objects, and `[RequireComponent]`
auto-adding dependencies before a component's `Awake` runs.

## Excluded from the headless build

- `Player/Input/PlayerInputs.cs` — the generated Input System action wrapper. Shimming a generated
  asset API would be a large surface with little to verify.
- `Player/PlayerInputController.cs`, which binds to it. Its one piece of pure, testable logic
  (`AcceptsInputIn`) is mirrored — not re-derived — in `Shims/Player.PlayerInputController.cs`,
  which is how `LootSettleAndInputGatingTests` still exercises the actual predicate headlessly. Keep
  the two in sync if that predicate ever changes.
- `Net/SteamInviteGateway.cs` — Steamworks.NET and PurrLobby are native/platform SDKs, same
  reasoning as the generated Input asset.
- `Editor/PlayerBuilder.cs` (issue #53) — calls `BuildPipeline.BuildPlayer`/`BuildReport`, which the
  `UnityEditor` shim does not model. A shimmed "successful build" would prove nothing; this is
  verified against the real Unity Editor directly instead.

Everything else under `Assets/_Project/Scripts/Runtime` is compiled, including the
`HEADLESS`-guarded Vosk provider.
