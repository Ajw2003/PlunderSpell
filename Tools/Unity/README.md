# Tools/Unity

Scripts that drive the live Unity Editor (and the Development build in `Build/DevTest`) through the
`unity` CLI. Every script sources `pin.sh`, so its `unity` calls go to this checkout's Editor and no other.

## Before anything

- The Editor must be open on this project. `unity command editor_status --project-path <repo>` says
  `"status": "ready"` when it is. If it reports "No Pipeline instance found", the Editor is closed:
  `unity open "<repo>"` opens it.
- Never recompile or run tests while the Editor is in Play mode (a recompile mid-raid broke PurrNet
  and hung the Editor once). Check first:
  `bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;'`
  must print `False False`.
- After a compile, check that PurrNet kept the network prefab list:
  `git diff --ignore-cr-at-eol --stat Assets/_Project/Net/NetworkPrefabs.asset` must print nothing.

## Waiting on Unity

Use the wrappers below, which already wait correctly. If you must write your own wait:

1. **Parse the JSON, never text-match it.** `unity command <x> --format json` returns an envelope whose
   `data.result` is often itself a JSON *string*, so every inner quote is escaped:
   `"result": "{\"status\":\"completed\",...`. A grep or `case` match for `"completed"` never fires,
   and the loop runs to its time cap. On 2026-10-02 a 19 s build was waited on for about 10 minutes
   this way (Ajw2003/AjsClaudeCodeTools#138). Parse instead, decoding `result` a second time when it is a string:

   ```bash
   unity command build_status --no-banner --format json | python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get('status'))"
   ```

2. **Prove the success check can fire before you loop on it.** Run it once against the current state
   (or a state you know is finished) and see the value you are waiting for come out. A wait whose exit
   condition was never seen to work is a timer, not a wait.
3. **Bound every wait** and say what happens at the cap. No open-ended loops, no `sleep` without one.
4. **One long command looks like a stalled agent.** The stall watch only sees tool calls, so a single
   10-minute Bash call reads as STALLED. Prefer short bounded checks.
5. **"Main thread operation timed out" usually means a dialog is waiting for a click** (#371). A modal
   Unity prompt such as "Scene(s) Have Been Modified" holds the main thread, and it never ends by
   itself. `pin.sh`'s `unity` wrapper prints `BLOCKED: … waiting for a click on '<title>' [buttons]` when
   it sees one; call `exit_if_unity_dialog` in your wait loop to stop instead of running to the cap,
   or `unity_dialog` to just list them (`Tools/Unity/unity_dialog.ps1`). Nothing clicks a dialog by
   itself. `run_tests.sh` saves scenes with unsaved edits before a run, so that dialog does not come up.

## The scripts

| Script | What it does |
|---|---|
| `eval.sh` | Runs C# in the Editor and prints only its result or compile errors. `--file <path>` runs a file. |
| `recompile.sh` | Recompiles and waits; prints compile errors. Exit 0 when clean. |
| `run_tests.sh <class or Plunderspell.Tests> PlayMode\|EditMode` | Runs tests and waits; prints `total ...` and one `FAILED ...` line per failure. |
| `capture.sh` | Saves the Editor's game view to a PNG anywhere in the repo. |
| `view.sh` | Play mode: parks the local player's camera at a point and angle, optionally captures. |
| `coop_eval.sh host\|client <action>` | Runs one action of `eval/coop_carry.cs` on the host (Editor) or the client (built player). |
| `coop_local.sh [--build\|--no-build]` | **Play co-op on this PC by hand** (#372): rebuilds the client if the game changed, presses Play, hosts on this PC, starts the client joined at 127.0.0.1 and puts it beside the Editor. Leaves both running, in your own save slot. The main menu's **Host on this PC** / **Join on this PC** buttons (Editor and Development builds only) do the same by clicking. |
| `wizard_coop_check.sh <label>` | Co-op in the Lair; the client crouches, casts and walks, and the host's copy of its wizard must read each (#362). Screenshots in `docs/generated/wizard-362/`. |
| `coop_guard_check.sh <label>` | Co-op raid; sends every guard after the host and measures how long guards stand still. |
| `coop_swarm_check.sh <label>` | Co-op raid; raises the hue and cry and counts guards near the host. |
| `guard_awareness_check.sh` | Co-op raid; measures what a calm guard hears and sees of the host's player (#238). |
| `coop_carry_check.sh` | Co-op two-player carry check (#169). |
| `coop_lair_check.sh <label>` | Co-op Lair room and Market (#314): each player at their own spawn, the Market door and back, coming home from a raid, the same haul pile on both sides. About a minute without a build. |
| `coop_drop_latency.sh` | Co-op timing of physics sounds on host and client. |
| `night_captures.sh` | Fixed-camera night captures for comparing with the look samples. |
| `test_slot.sh` | Sourced by checks that play a campaign (#313): plays in the test save slot (`SaveSlots.TestSlot`, 99), which the Main Menu never offers, wipes it at the start and puts the owner's slot back at the end. `coop_lair_check.sh`, `coop_carry_check.sh` and `hud_events_check.sh` use it; a new campaign-playing check should too. |
| `pin.sh`, `settings_restore.sh` | Sourced helpers: pin calls to this Editor; save and restore ProjectSettings around a build. |

The co-op scripts build the client (`Build/DevTest`) when anything under `Assets` changed since the last
build. A stale client can fail to connect, stuck at "Connecting", so let them rebuild. Leave both windows
alone while a check runs: nobody is at the keyboard on purpose, and the host's player may be killed by guards.
