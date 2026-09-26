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
| `Shims/UnityEngine.*.cs` | The UnityEngine surface the game uses: math, object/component model, physics queries, input, persistence. |
| `Shims/PurrNet.cs` | PurrNet 1.15's `NetworkBehaviour` / `SyncVar` / `SyncList` / RPC attributes. |
| `Shims/UnityEngine.TestTools.cs` | `[UnityTest]`, `LogAssert`. |
| `Plunderspell.Headless/` | Compiles the linked game sources against the shims. |
| `Plunderspell.Headless.Tests/` | Compiles and runs the linked test files under NUnit. |
| `Plunderspell.Headless.Tests/UnityTestBridge.cs` | Drives `[UnityTest]` coroutines, which plain NUnit cannot execute. |

Nothing here is copied. Both projects reference the real files under `Assets/_Project/` via linked
`<Compile Include=...>` items, so the harness cannot drift from the game: a compile error here is a
compile error in the editor.

## What it does and does not prove

**Proves:** every gameplay assembly compiles; all pure logic (castle generation, A* validation,
misfire resolution, alarm escalation and decay, fragility, carry thresholds, extraction tally, debt
maths, raid orchestration) behaves as specified; components construct, receive their Unity messages
and wire up correctly; acoustic propagation and occlusion, against a real (if simple) AABB physics
world.

**Does not prove:** rendering, animation, NavMesh baking, real PhysX behaviour, or a genuine
multi-peer PurrNet transport. The shim models a single listen-server host: an RPC call runs its body
in place, which is what a host observes. Genuine multi-peer replication still needs the editor's
PlayMode suite.

## Adding to the shims

Add only what the game actually calls, and keep the semantics honest — a shim that silently differs
from Unity is worse than a missing one. Two behaviours the shims deliberately reproduce because the
game depends on them: Unity's "fake null" for destroyed objects, and `[RequireComponent]`
auto-adding dependencies before a component's `Awake` runs.

## Excluded from the headless build

`Player/Input/PlayerInputs.cs` (the generated Input System action wrapper) and
`Player/PlayerInputController.cs`, which binds to it. Shimming the generated asset API would be a
large surface with little to verify. Everything else under `Assets/_Project/Scripts/Runtime` is
compiled, including the `HEADLESS`-guarded Vosk provider.
