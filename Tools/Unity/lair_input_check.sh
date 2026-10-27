#!/usr/bin/env bash
# Solo Play check that the Input System actions drive the Lair (#350): W held through a queued keyboard state moves the
# player, and a queued E press while facing the Market door takes them to the Market (x over 1050). Keys are queued with
# InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(...)), never by calling game methods. Runs in the test
# slot (test_slot.sh), leaves the Editor stopped. Usage: bash Tools/Unity/lair_input_check.sh
# Needs the Editor open on this project, not in Play mode. Logs: docs/generated/lair-input-2026-10-07/. Prints PASS or FAIL lines.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/lair-input-2026-10-07"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/run.log"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
cleanup() { log "cleaning up"; ev 'UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState()); return "keys up";' >/dev/null 2>&1; unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true; test_slot_restore; log "$test_slot_msg"; }
trap cleanup EXIT
fails=0
check() { if [ "$1" = ok ]; then log "PASS $2"; else log "FAIL $2"; fails=$((fails + 1)); fi; }
keys() { # keys <Key name or none> [update]: the whole keyboard state becomes just that key; "update" also runs InputSystem.Update() now, else the next frame does (needed for WasPressedThisFrame)
    local upd=""; [ "${2:-}" = update ] && upd="UnityEngine.InputSystem.InputSystem.Update();"
    local state="new UnityEngine.InputSystem.LowLevel.KeyboardState()"; [ "$1" = none ] || state="new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.$1)"
    ev "UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, $state); $upd return \"keys: $1\";"
}
pos() { ev 'var p = StateMachine.PlayerStateMachine.Local.transform.position; return p.x.ToString("F2") + "," + p.y.ToString("F2") + "," + p.z.ToString("F2");'; }

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

# W held for about a second.
a="$(pos)"; log "before W: $a"
log "$(keys W update)"; sleep 1; log "$(keys W update)"; sleep 1
b="$(pos)"; log "while W held: $b"
log "$(keys none update)"
python -c "
import sys
a=[float(x) for x in sys.argv[1].split(',')]; b=[float(x) for x in sys.argv[2].split(',')]
sys.exit(0 if ((a[0]-b[0])**2+(a[2]-b[2])**2)**0.5 > 0.5 else 1)" "$a" "$b" && check ok "holding W through the Input System moved the player ($a -> $b)" || check no "holding W through the Input System moved the player ($a -> $b)"
sleep 1

# Face the Market door within reach, then press E.
log "$(ev 'var player = StateMachine.PlayerStateMachine.Local; var rb = player.GetComponent<UnityEngine.Rigidbody>(); var door = UnityEngine.GameObject.Find("/LairRoom/MarketDoor").transform; var c = door.GetComponentInChildren<UnityEngine.Collider>(); UnityEngine.Vector3 target = c != null ? c.bounds.center : door.position; var d0 = target - player.transform.position; d0.y = 0f; var spot = target - d0.normalized * 1.5f; spot.y = player.transform.position.y; rb.position = spot; player.transform.position = spot; rb.linearVelocity = UnityEngine.Vector3.zero; player.FaceYaw(UnityEngine.Quaternion.LookRotation(d0).eulerAngles.y); var eye = UnityEngine.Camera.main.transform; float down = UnityEngine.Mathf.Atan2(eye.position.y - target.y, 1.5f) * UnityEngine.Mathf.Rad2Deg; typeof(StateMachine.PlayerStateMachine).GetField("_xRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(player, down); return "facing the door from " + spot;')"
sleep 1
log "looked at: $(ev 'return Plunderspell.Raid.LookTarget.IsLookedAt(UnityEngine.Camera.main, UnityEngine.GameObject.Find("/LairRoom/MarketDoor").transform, 3f).ToString();')"
log "$(keys E)"; sleep 1; log "$(keys none)"; sleep 2
c="$(pos)"; log "after E: $c"
python -c "import sys; sys.exit(0 if float(sys.argv[1].split(',')[0]) > 1050 else 1)" "$c" && check ok "pressing E at the Market door took the player to the Market ($c)" || check no "pressing E at the Market door took the player to the Market ($c)"
[ "$fails" -eq 0 ] && log "PASS all lair input checks" || log "FAIL $fails lair input checks"
exit $((fails > 0))
