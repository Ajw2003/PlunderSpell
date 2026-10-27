#!/usr/bin/env bash
# Solo Play check that the Market has a crosshair and a proper grab (#353): a loot piece is set on the Goldsmith's counter,
# the player stands in the Market aiming at it, the game view is captured (the crosshair must show), then the left mouse
# button is pressed through the Input System (a Mouse state event, never a game call): ItemManager must start dragging.
# The piece is carried onto the counter and let go. Runs in the test slot (test_slot.sh), leaves the Editor stopped.
# Usage: bash Tools/Unity/market_grab_check.sh <label>   (before|after)   Logs and captures: docs/generated/market-grab-2026-10-07/
# Needs the Editor open on this project, not in Play mode. Prints PASS or FAIL lines.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: market_grab_check.sh <label>}"
out="docs/generated/market-grab-2026-10-07"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
L() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/coop_lair.cs" timeout 90 bash Tools/Unity/coop_eval.sh host "$@" 2>&1; }
mouse() { # mouse left|none: the whole mouse state becomes just that button, applied now
    local state="new UnityEngine.InputSystem.LowLevel.MouseState()"; [ "$1" = none ] || state="new UnityEngine.InputSystem.LowLevel.MouseState().WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left)"
    ev "UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current, $state); UnityEngine.InputSystem.InputSystem.Update(); return \"mouse: $1\";"
}
cleanup() { log "cleaning up"; mouse none >/dev/null 2>&1; unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true; test_slot_restore; log "$test_slot_msg"; }
trap cleanup EXIT
fails=0
check() { if [ "$1" = ok ]; then log "PASS $2"; else log "FAIL $2"; fails=$((fails + 1)); fi; }
counter='System.Linq.Enumerable.First(UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None), c => c.Vendor.ToString() == "Goldsmith").GetComponentInChildren<UnityEngine.BoxCollider>().bounds.center'
walkto() { # walkto <C# Vector3 expression>: face it, tilted 20 degrees down, and walk the player there at 4 m/s, stopping 1.4 m short
    ev 'var player = StateMachine.PlayerStateMachine.Local; var rb = player.GetComponent<UnityEngine.Rigidbody>(); UnityEngine.Vector3 target = '"$1"'; var d0 = target - player.transform.position; d0.y = 0f; float yaw = UnityEngine.Quaternion.LookRotation(d0).eulerAngles.y; player.FaceYaw(yaw); typeof(StateMachine.PlayerStateMachine).GetField("_xRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(player, 20f); UnityEngine.Camera.main.transform.localRotation = UnityEngine.Quaternion.Euler(20f, yaw, 0f); UnityEditor.EditorApplication.CallbackFunction step = null; step = () => { var d = target - player.transform.position; d.y = 0f; if (d.magnitude <= 1.4f) { UnityEditor.EditorApplication.update -= step; return; } var next = player.transform.position + d.normalized * 4f * UnityEngine.Time.deltaTime; rb.position = next; player.transform.position = next; rb.linearVelocity = UnityEngine.Vector3.zero; }; UnityEditor.EditorApplication.update += step; return "walking to " + target;'
}

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

# Through the Market door, a piece on the Goldsmith's counter, aimed at from 1.8 m.
log "$(L travel /LairRoom/MarketDoor)"; sleep 2
log "state: $(ev 'return Plunderspell.Core.GameServices.GameState.CurrentState.ToString();')"
log "$(L put 4)"; sleep 3
log "$(ev 'var piece = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Loot.LootValue>(); var player = StateMachine.PlayerStateMachine.Local; var rb = player.GetComponent<UnityEngine.Rigidbody>(); var p = piece.GetComponentInChildren<UnityEngine.Collider>().bounds.center; var away = player.transform.position - p; away.y = 0f; var from = p + away.normalized * 1.8f; from.y = player.transform.position.y; float yaw = UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(p.x - from.x, 0f, p.z - from.z)).eulerAngles.y; rb.linearVelocity = UnityEngine.Vector3.zero; rb.position = from; player.transform.position = from; player.FaceYaw(yaw); float pitch = UnityEngine.Mathf.Atan2(UnityEngine.Camera.main.transform.position.y - p.y, 1.8f) * UnityEngine.Mathf.Rad2Deg; typeof(StateMachine.PlayerStateMachine).GetField("_xRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(player, pitch); UnityEngine.Camera.main.transform.localRotation = UnityEngine.Quaternion.Euler(pitch, yaw, 0f); return "aiming at " + piece.name + " from " + from;')"
sleep 2
hov="$(ev 'var im = ItemManager.Instance; var f = typeof(ItemManager).GetField("_hoveredItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); var h = f.GetValue(im) as UnityEngine.Component; var cam = UnityEngine.Camera.main; UnityEngine.RaycastHit rh; bool hit = UnityEngine.Physics.Raycast(cam.ViewportPointToRay(new UnityEngine.Vector3(0.5f, 0.5f, 0f)), out rh, 4f); return "ray hits " + (hit ? rh.collider.name : "nothing") + "; hovered " + (h != null ? h.name : "none") + " crosshair over-something " + UnityEngine.Object.FindFirstObjectByType<Plunderspell.UI.CrosshairView>().HasTarget + "; interactor focus " + (UnityEngine.Object.FindFirstObjectByType<Plunderspell.Loot.LootInteractor>() is var li && li != null && li.Focus != null ? li.Focus.name : "none");' 2>&1)"; log "$hov"
bash Tools/Unity/capture.sh "$out/$label.png" >/dev/null 2>&1; log "capture: $out/$label.png"

# The left mouse button, through the Input System.
ev 'var piece = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Loot.LootValue>(); var player = StateMachine.PlayerStateMachine.Local; var rb = player.GetComponent<UnityEngine.Rigidbody>(); var p = piece.GetComponentInChildren<UnityEngine.Collider>().bounds.center; var away = player.transform.position - p; away.y = 0f; var from = p + away.normalized * 1.8f; from.y = player.transform.position.y; float yaw = UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(p.x - from.x, 0f, p.z - from.z)).eulerAngles.y; rb.linearVelocity = UnityEngine.Vector3.zero; rb.position = from; player.transform.position = from; player.FaceYaw(yaw); float pitch = UnityEngine.Mathf.Atan2(UnityEngine.Camera.main.transform.position.y - p.y, 1.8f) * UnityEngine.Mathf.Rad2Deg; typeof(StateMachine.PlayerStateMachine).GetField("_xRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(player, pitch); UnityEngine.Camera.main.transform.localRotation = UnityEngine.Quaternion.Euler(pitch, yaw, 0f); return "aiming at " + piece.name + " from " + from;' >/dev/null
log "$(mouse left)"; sleep 1
h="$(L held)"; log "after the click: $h"
case "$h" in "held "*"dragging True"*) check ok "a left click through the Input System picks the piece up in the Market" ;; *) check no "a left click through the Input System picks the piece up in the Market" ;; esac
log "$(walkto "$counter")"; sleep 5
log "held at the counter: $(L held)"
log "$(mouse none)"; sleep 3
log "$(L piece)"
h="$(L held)"; log "after letting go: $h"
[ "$h" = "held none" ] && check ok "letting go the button lets go of the piece at the Goldsmith's counter (the counter may take it for a sale)" || check no "letting go the button lets go of the piece ($h)"
[ "$fails" -eq 0 ] && log "PASS all Market grab checks" || log "FAIL $fails Market grab checks"
exit $((fails > 0))
