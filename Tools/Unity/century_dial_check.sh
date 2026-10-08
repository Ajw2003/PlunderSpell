#!/usr/bin/env bash
# Solo Play check of the century dial (#358): facing the dial, a queued E press (Input System, as lair_input_check.sh) turns
# it to the next Age, the rings ease (their angle is sampled every frame: no frame jumps more than a few degrees), the plaque
# text matches the Age, and setting out by the portal starts the raid in the chosen Age. Pictures of the dial and plaque go to
# docs/generated/century-dial-2026-10-07/. Runs in the test slot, leaves the Editor stopped.
# Usage: bash Tools/Unity/century_dial_check.sh   Needs the Editor open on this project, not in Play mode.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/century-dial-2026-10-07"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/run.log"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
cleanup() { log "cleaning up"; ev 'UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState()); return "keys up";' >/dev/null 2>&1; unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true; test_slot_restore; log "$test_slot_msg"; }
trap cleanup EXIT
fails=0
check() { if [ "$1" = ok ]; then log "PASS $2"; else log "FAIL $2"; fails=$((fails + 1)); fi; }
keys() { # keys <Key name or none> [update]
    local upd=""; [ "${2:-}" = update ] && upd="UnityEngine.InputSystem.InputSystem.Update();"
    local state="new UnityEngine.InputSystem.LowLevel.KeyboardState()"; [ "$1" = none ] || state="new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.$1)"
    ev "UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, $state); $upd return \"keys: $1\";"
}
dial() { ev 'var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.LairCenturyDial>(); return d.ShownEra + " | " + d.PlaqueText.Replace("\n", "/");'; }
era() { ev 'return Plunderspell.Lair.LairHubManager.Peek(Plunderspell.Lair.SaveSlots.Active).SelectedEra + " raid " + UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>().Era;'; }

playing="$(ev 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; trap - EXIT; exit 1; }
[ "$playing" = "False False" ] || { log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; }
test_slot_use || { log "FAIL cannot switch to the test slot: $test_slot_msg"; trap - EXIT; exit 1; }; log "$test_slot_msg"

unity command editor_play "${cli[@]}" >/dev/null
state=""
for _ in $(seq 1 60); do state="$(ev 'return Plunderspell.Core.GameServices.GameState.CurrentState.ToString();' 2>/dev/null)"; [ "$state" = MainMenu ] && break; sleep 1; done
log "state $state"
log "$(ev 'var s = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Net.CoopSession>(); s.PlaySolo(); Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.LairRoom); return "solo, slot " + Plunderspell.Lair.SaveSlots.Active;')"
for _ in $(seq 1 30); do p="$(ev 'return StateMachine.PlayerStateMachine.Local != null ? "player" : "none";' 2>/dev/null)"; [ "$p" = player ] && break; sleep 1; done
log "player: $p"
sleep 2

# Stand 1.8 m from the dial on the portal side, looking at it.
log "$(ev 'var player = StateMachine.PlayerStateMachine.Local; var rb = player.GetComponent<UnityEngine.Rigidbody>(); var stand = UnityEngine.GameObject.Find("/LairRoom/LairCenturyDialStand").transform; var target = stand.position + UnityEngine.Vector3.up * 0.9f; var spot = target + UnityEngine.Vector3.right * 1.8f; spot.y = player.transform.position.y; rb.position = spot; player.transform.position = spot; rb.linearVelocity = UnityEngine.Vector3.zero; player.FaceYaw(270f); var eye = UnityEngine.Camera.main.transform; float down = UnityEngine.Mathf.Atan2(eye.position.y - target.y, 1.8f) * UnityEngine.Mathf.Rad2Deg; typeof(StateMachine.PlayerStateMachine).GetField("_xRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(player, down); return "at " + spot;')"
sleep 1
log "looked at: $(ev 'return Plunderspell.Raid.LookTarget.IsLookedAt(UnityEngine.Camera.main, UnityEngine.GameObject.Find("/LairRoom/LairCenturyDialStand").transform, 3f).ToString();')"
bash Tools/Unity/capture.sh "$out/dial-bronze.png" >/dev/null 2>&1
log "start: $(dial) / $(era)"

# Record ring 1's angle against its first pose, and the plaque alpha, every frame from now on.
log "$(ev 'var rings = new[] { "LairCenturyDialRing1", "LairCenturyDialRing2", "LairCenturyDialRing3", "LairCenturyDialRing4" }; var list = new System.Collections.Generic.List<string>(); System.AppDomain.CurrentDomain.SetData("dialSamples", list); var ts = new[] { UnityEngine.GameObject.Find("/LairRoom/" + rings[0]).transform, UnityEngine.GameObject.Find("/LairRoom/" + rings[3]).transform }; var first = new[] { ts[0].localRotation, ts[1].localRotation }; var plaque = UnityEngine.GameObject.Find("/LairRoom/PlaqueText").GetComponent<TMPro.TextMeshPro>(); UnityEditor.EditorApplication.CallbackFunction rec = null; rec = () => { list.Add(UnityEngine.Time.frameCount + " " + UnityEngine.Quaternion.Angle(first[0], ts[0].localRotation).ToString("F2") + " " + UnityEngine.Quaternion.Angle(first[1], ts[1].localRotation).ToString("F2") + " " + plaque.alpha.ToString("F2")); }; UnityEditor.EditorApplication.update += rec; return "recording";')"
log "$(keys E)"; sleep 0.3; log "$(keys none)"
sleep 2.5
dial_now="$(dial)"; log "after one E: $dial_now / $(era)"
case "$dial_now" in "HighMedieval | "*"High Medieval"*"Stratum II"*"c. 1250"*"Curtain walls"*) check ok "E turned the dial to High Medieval and the plaque says so" ;; *) check no "E turned the dial to High Medieval and the plaque says so" ;; esac
bash Tools/Unity/capture.sh "$out/dial-high-medieval.png" >/dev/null 2>&1
samples="$(ev 'var list = (System.Collections.Generic.List<string>)System.AppDomain.CurrentDomain.GetData("dialSamples"); return string.Join(";", list);')"
printf '%s\n' "$samples" | tr ';' '\n' > "$out/ring-samples.txt"
python -c "
import sys
rows=[l.split() for l in open(sys.argv[1]) if l.strip()]
a=[float(r[1]) for r in rows]; b=[float(r[2]) for r in rows]
step=max(max(abs(a[i+1]-a[i]) for i in range(len(a)-1)), max(abs(b[i+1]-b[i]) for i in range(len(b)-1)))
moving=sum(1 for i in range(len(a)-1) if a[i+1]!=a[i])
print('frames', len(rows), 'ring1 final', a[-1], 'ring4 final', b[-1], 'largest one-frame step', round(step,2), 'frames moving', moving)
sys.exit(0 if a[-1]>5 and moving>=15 and step<=8 else 1)" "$out/ring-samples.txt" 2>&1 | while read -r line; do log "$line"; done
[ "${PIPESTATUS[0]}" = 0 ] && check ok "the rings eased: many frames in motion, no frame jumped more than 8 degrees" || check no "the rings eased: many frames in motion, no frame jumped more than 8 degrees"

# Round the whole circle.
for _ in 1 2 3; do keys E >/dev/null; sleep 0.3; keys none >/dev/null; sleep 1.5; done
dial_now="$(dial)"; log "after four E: $dial_now"
case "$dial_now" in "BronzeAge | "*"Bronze Age"*"c. 1200 BC"*) check ok "four presses went round to the Bronze Age again" ;; *) check no "four presses went round to the Bronze Age again" ;; esac
keys E >/dev/null; sleep 0.3; keys none >/dev/null; sleep 1.5
keys E >/dev/null; sleep 0.3; keys none >/dev/null; sleep 2
bash Tools/Unity/capture.sh "$out/dial-late-medieval.png" >/dev/null 2>&1
log "chosen: $(dial) / $(era)"

# Set out through the portal: the raid must start in the chosen Age.
timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
for _ in $(seq 1 30); do state="$(ev 'return Plunderspell.Core.GameServices.GameState.CurrentState.ToString();' 2>/dev/null)"; [ "$state" = Playing ] && break; sleep 1; done
log "state $state"
r="$(era)"; log "after setting out: $r"
case "$r" in "LateMedieval raid LateMedieval") check ok "setting out started the raid in the chosen Age" ;; *) check no "setting out started the raid in the chosen Age" ;; esac
[ "$fails" -eq 0 ] && log "PASS all century dial checks" || log "FAIL $fails century dial checks"
exit $((fails > 0))
