# Per-frame cost and standards audit (2026-10-01)

Code audit for #201, which finds what causes frame-rate drops. It is read-only. Two scouts split
`Assets/_Project/Scripts/Runtime`, and the top items were spot-checked against the code by the
planning session. The checked lines are marked ✔. This is a code reading, not a profile; #201's
Profiler captures decide what actually dominates.

## Ranked findings

| # | Where | What happens each frame | Cost | Fix |
|---|---|---|---|---|
| 1 | `Voice/VoskVoiceInputService.cs:382` ✔ | `if (_floatBuffer.Length != available) _floatBuffer = new float[available];` allocates a new float array almost every frame, because the sample count varies. `_shortBuffer` grows the same way (`:384`). It runs whenever the mic listens, and with chatter on that is all the time | **High**: steady garbage, so GC spikes | Allocate a fixed buffer at warm-up for the largest read (or a ring buffer) and read `available` samples into it |
| 2 | `UI/RaidHud/DamageFeedbackView.cs:286-287` ✔ | `_numbers.RemoveAll(n => now - n.Born > …)` and `_hurtLines.RemoveAll(...)`: each lambda captures the local `now`, so two closures are allocated every frame | **Medium**: small, but every frame in a raid | A reverse `for` loop with `RemoveAt` |
| 3 | `UI/RaidHud/DamageFeedbackView.cs:341, 447` | In `OnGUI`, `GetComponentInChildren<IHealth>()` runs every frame, and `GetComponentInChildren<Renderer>()` runs per damage number. `OnGUI` can run several times a frame | **Medium** | Cache the player's `IHealth` when the player spawns; cache the renderer per target when the number starts |
| 4 | `Atmosphere/CastleAtmosphere.cs:339-340` ✔ (interval `:57` = 0.1 s) | Two `Physics.Linecast` per visible fire every 0.1 s, so ~20 per second per visible fire | **Medium**, growing with fire count | Stagger fires across probes (check N per probe), or lengthen the interval for fires far from the camera |
| 5 | `Atmosphere/CastleAtmosphere.cs:317-321`, `FireSource.cs:148-150` | Loops over every `FireSource.All` per frame; two `Mathf.PerlinNoise` per fire per frame for flicker | **Low–medium** | Keep a list of lit fires updated on state change, not rebuilt; flicker is fine unless the profile says otherwise |
| 6 | `Guards/CastleGuard.cs:736` ✔ (loop `:729`) | One `Physics.Linecast` per guard per intruder per frame: 20 guards × 2–4 players = 40–80 a frame | **Medium**: linecasts are cheap but add up | Run sight 10–15 times a second, staggered per guard. This belongs with `GuardSight` in #206 |
| 7 | `Acoustics/NoiseBroadcaster.cs:91` ✔ | `Physics.RaycastAll` allocates per listener per noise broadcast | **Medium** in fights (many noises) | `RaycastNonAlloc` with a reused buffer |
| 8 | `Items/GrabBeam.cs:72` | `SetPositions` and gradient set every frame while a beam shows | **Low** | Set the gradient only when the colour changes |
| 9 | `Guards/CastleGuard.cs:426` | `TryGetComponent(out Rigidbody)` every frame while levitating | **Low** | Use the cached body (#206 / #211) |
| 10 | `Camera.main` in `ItemManager.cs:69`, `DamageFeedbackView.cs:318`, `CastleAtmosphere.cs:305,367,391` | `Camera.main` per frame | **Low**: Unity has cached it since 2020 | Optional tidy |

## Checked clean (scouts' reading)

- No LINQ, no per-frame string building and no `Debug.Log` in hot paths.
- No per-frame `Instantiate`/`Destroy` churn, and no SyncVar written every frame.
- `EnemyDirector.Publish` passes readonly structs and allocates nothing.
- Guard NavMesh calls are throttled by the 0.75 m resend and the stuck watchdog.

## Standards (non-performance)

- `Player/PlayerStateMachine.cs:49,51,59`: the public fields `_rb`, `walkSpeed` and `respawnSpeed` break the naming rule.
- `RangedWeapon.cs:14-18`: the `m_` prefix.
- `Items/GrabBeam.cs`: no namespace.

## Corrections to the scouts

- #2 is closure allocation only. `RemoveAll` compacts the list in place; it does not rebuild it.
- The `Camera.main` items were rated medium; Unity caches `Camera.main` internally, so they are low.

## Issues filed

#215 (voice buffers), #216 (HUD damage numbers), #217 (fire checks), #218 (noise raycasts), #219 (naming). Guard sight throttling and the levitation lookup are folded into #206.
