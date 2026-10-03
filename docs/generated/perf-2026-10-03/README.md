# Performance pass, 2026-10-03 (#242)

Co-op raid, seed 3508293, Late Medieval: the Editor hosts in Play mode, a Development build joins at
1920x1080. Each label is one run of `bash Tools/Unity/perf_capture.sh <label>` (10 s calm, then 10 s
after the hue and cry is raised, Profiler recording on both sides), summarised by
`bash Tools/Unity/perf_report.sh <label>` into `<label>-report.txt`. The `.raw` captures (0.5-1 GB
each) are not committed. The Profiler keeps the last 2,000 frames of each.

## Results

Host = the Editor, the owner's usual way of playing. Client = the Development build.

| Run | What changed | Host hue and cry: avg fps / p99 ms / worst ms | Client hue and cry: avg fps / p99 / worst |
|---|---|---|---|
| `before` | nothing | 109 / 19.9 / 204 | 241 / 12.1 / 28.5 |
| `audio` | short sounds in memory (#244) | 116 / 15.2 / 202 | 232 / 9.2 / 243 * |
| `smooth` | route smoothing look-ahead capped at 8 m (#243) | 121 / 13.9 / 65 | 235 / 8.8 / 238 * |
| `reach` | nearest-cell scan drops a ring that can never match (#243) | 125 / 10.5 / 33 | 251 / 8.4 / 19 |
| `hud` | raid HUD draws on Repaint only (#245) | 119 / 11.0 / 33 | 252 / 8.0 / 256 * |
| `atmos2` | fire glow sort computes distances once (#217) | 127 / 12.0 / 48 | 268 / 8.0 / 23 |

\* One-off frames in `PlayerNetworkOwnership.OnGUI > GUIStyle.GetMeshInfo`: IMGUI laying out text the
first time (a font being built). Not fixed yet.

Run-to-run noise is a few fps. Each fix's own marker is the better measure:

- Sound stream teardown (`SoundHandle.Instance.Destructor`): 485-557 ms per 10 s -> none.
- `GuardPathPlanner.Smooth`: 513 -> 53 ms per 10 s on the host; hue-and-cry spike frames 41 -> 4.
- `RaidHudView.OnGUI` garbage: 46 -> 26 KB per frame (client total 48 -> 29 KB/frame).
- `CastleAtmosphere.GatherScatterLights`: 0.44 -> 0.16 ms per frame (host), 0.135 -> 0.05 (client).

## What the numbers say

- **The Editor is half the host's frame.** The same raid runs at ~290 fps calm in the build and
  ~135 fps in the Editor. Editor windows (`OnGUI`, `TextGenerator`, `IMGUIContainer`) and Editor
  bookkeeping take the difference. Play-mode fps in the Editor is not the game's fps.
- **The GPU is not the limit at 1080p.** The client's main thread spends ~0.1 ms per frame waiting on
  the swap chain, and shadow work on the CPU (`Shadows.*`) is ~0.1 ms. Frames are CPU-bound.
- **Left on the table** (largest first): IMGUI text in the HUD (26 KB/frame garbage, ~0.3 ms), the
  voice pump's spikes (`MainThreadPump.Update`, #215), fire flicker (`BurnFires`, 0.2-0.4 ms, #217),
  PurrNet's transform sync (~0.1 ms).
