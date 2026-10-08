#!/usr/bin/env bash
# Solo Play check that the Lair screen is gone (#359): E at the ledger book opens nothing and the book stays readable; Esc in
# the Lair room opens the pause menu (with Invite Friend when a session can invite); Resume returns to the Lair room and W
# still moves the player (keys queued through the Input System, as lair_input_check.sh does); a client walking into the
# portal sees "The host sets out" (the session is made a client's by overriding IsSessionAuthority; the real two-player
# case is coop_lair_check.sh). Runs in the test slot (test_slot.sh), leaves the Editor stopped.
# Usage: bash Tools/Unity/no_lair_screen_check.sh     Captures and log: docs/generated/no-lair-screen-2026-10-07/
# Needs the Editor open on this project, not in Play mode. Prints PASS or FAIL lines.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/no-lair-screen-2026-10-07"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/run.log"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
cleanup() { log "cleaning up"; ev 'UnityEngine.Time.timeScale = 1f; UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState()); return "keys up";' >/dev/null 2>&1; unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true; test_slot_restore; log "$test_slot_msg"; }
trap cleanup EXIT
fails=0
check() { if [ "$1" = ok ]; then log "PASS $2"; else log "FAIL $2"; fails=$((fails + 1)); fi; }
keys() { # keys <Key name or none> [update]: the whole keyboard state becomes just that key; the next frame reads it ("update" also runs InputSystem.Update() now, which a held key wants and a press (WasPressedThisFrame) must not have)
    local upd=""; [ "${2:-}" = update ] && upd="UnityEngine.InputSystem.InputSystem.Update();"
    local state="new UnityEngine.InputSystem.LowLevel.KeyboardState()"; [ "$1" = none ] || state="new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.$1)"
    ev "UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, $state); $upd return \"keys: $1\";"
}
tap() { keys "$1" >/dev/null; sleep 0.5; keys none >/dev/null; sleep 1; }
state() { ev 'return Plunderspell.Core.GameServices.GameState.CurrentState.ToString();'; }
pos() { ev 'var p = StateMachine.PlayerStateMachine.Local.transform.position; return p.x.ToString("F2") + "," + p.y.ToString("F2") + "," + p.z.ToString("F2");'; }

playing="$(ev 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; trap - EXIT; exit 1; }
[ "$playing" = "False False" ] || { log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; }
test_slot_use || { log "FAIL cannot switch to the test slot: $test_slot_msg"; trap - EXIT; exit 1; }; log "$test_slot_msg"

unity command editor_play "${cli[@]}" >/dev/null
s=""
for _ in $(seq 1 60); do s="$(state 2>/dev/null)"; [ "$s" = MainMenu ] && break; sleep 1; done
log "state $s"
log "$(ev 'var s = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Net.CoopSession>(); s.PlaySolo(); Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.LairRoom); return "solo, slot " + Plunderspell.Lair.SaveSlots.Active;')"
for _ in $(seq 1 30); do p="$(ev 'return StateMachine.PlayerStateMachine.Local != null ? "player" : "none";' 2>/dev/null)"; [ "$p" = player ] && break; sleep 1; done
log "player: $p"; sleep 2

# E at the book: nothing opens, and the book still reads.
log "$(ev 'var player = StateMachine.PlayerStateMachine.Local; var rb = player.GetComponent<UnityEngine.Rigidbody>(); var book = UnityEngine.GameObject.Find("/LairRoom/LairLedger").transform; var c = book.GetComponentInChildren<UnityEngine.Collider>(); UnityEngine.Vector3 target = c != null ? c.bounds.center : book.position; var d0 = target - player.transform.position; d0.y = 0f; var spot = target - d0.normalized * 1.5f; spot.y = player.transform.position.y; rb.position = spot; player.transform.position = spot; rb.linearVelocity = UnityEngine.Vector3.zero; player.FaceYaw(UnityEngine.Quaternion.LookRotation(d0).eulerAngles.y); var eye = UnityEngine.Camera.main.transform; float down = UnityEngine.Mathf.Atan2(eye.position.y - target.y, 1.5f) * UnityEngine.Mathf.Rad2Deg; typeof(StateMachine.PlayerStateMachine).GetField("_xRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(player, down); return "facing the book from " + spot;')"
sleep 1
tap E
s="$(state)"; log "after E at the book: $s"
bash Tools/Unity/capture.sh "$out/01-book-after-E.png" >/dev/null 2>&1; log "capture: $out/01-book-after-E.png"
book="$(ev 'var b = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.LairLedgerBook>(); return b.LeftText.Replace("\n", "/") + " || " + b.RightText.Replace("\n", "/");')"; log "book: $book"
r=no; [ "$s" = LairRoom ] && case "$book" in *Owed*) r=ok ;; esac
check $r "E at the book left the player in the Lair room and the book still reads"

# Esc: the pause menu, from the Lair room.
tap Escape
s="$(state)"; log "after Esc: $s, resumes to $(ev 'return Plunderspell.Core.GameServices.GameState.PausedFrom.ToString();')"
log "cursor: $(ev 'return UnityEngine.Cursor.lockState + " " + UnityEngine.Cursor.visible;')"
r=no; [ "$s" = Paused ] && r=ok; check $r "Esc in the Lair room opened the pause menu"
bash Tools/Unity/capture.sh "$out/02-pause-menu-solo.png" >/dev/null 2>&1; log "capture: $out/02-pause-menu-solo.png"

# Resume goes back to the Lair room; W moves the player.
tap Escape
s="$(state)"; log "after Esc again: $s"
r=no; [ "$s" = LairRoom ] && r=ok; check $r "Esc in the pause menu returned to the Lair room, not Playing"
a="$(pos)"; log "before W: $a"
keys W update >/dev/null; sleep 1; keys W update >/dev/null; sleep 1
b="$(pos)"; log "while W held: $b"
keys none update >/dev/null
python -c "
import sys
a=[float(x) for x in sys.argv[1].split(',')]; b=[float(x) for x in sys.argv[2].split(',')]
sys.exit(0 if ((a[0]-b[0])**2+(a[2]-b[2])**2)**0.5 > 0.5 else 1)" "$a" "$b" && check ok "after Resume, W through the Input System moved the player ($a -> $b)" || check no "after Resume, W through the Input System moved the player ($a -> $b)"

# With a session that can invite (forced: no Steam lobby in the Editor): the Invite Friend button, then its friend list; the Resume button
# (clicked, as the mouse would) goes back to the Lair room.
log "$(ev 'var s = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Net.CoopSession>(); typeof(Plunderspell.Net.CoopSession).GetField("_hostingLobby", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(s, true); Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.Paused); return "lobby host forced on; CanInvite " + s.CanInvite + "; state " + Plunderspell.Core.GameServices.GameState.CurrentState;')"
sleep 1
bash Tools/Unity/capture.sh "$out/03-pause-menu-invite.png" >/dev/null 2>&1; log "capture: $out/03-pause-menu-invite.png"
click() { ev "foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsSortMode.None)) if (b.name == \"$1\" && b.gameObject.activeInHierarchy) { b.onClick.Invoke(); return \"clicked $1\"; } return \"no visible $1\";"; }
log "$(click InviteButton)"; sleep 1
bash Tools/Unity/capture.sh "$out/04-pause-menu-friends.png" >/dev/null 2>&1; log "capture: $out/04-pause-menu-friends.png"
tap Escape
s="$(state)"; log "after Esc with the friend list open: $s"
r=no; [ "$s" = LairRoom ] && r=ok; check $r "Esc with the friend list open returned to the Lair room"
if [ "$s" = Paused ]; then log "$(click ResumeButton)"; sleep 1; log "after clicking Resume: $(state)"; fi

# A client at the portal: the line, slowed to a quarter speed so a capture can catch it.
log "$(ev 'Plunderspell.Core.GameServices.IsSessionAuthority = () => false; UnityEngine.Time.timeScale = 0.25f; var t = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.LairPortalTrigger>(); var c = t.GetComponent<UnityEngine.BoxCollider>().bounds.center; var player = StateMachine.PlayerStateMachine.Local; var rb = player.GetComponent<UnityEngine.Rigidbody>(); var spot = new UnityEngine.Vector3(c.x - 2.2f, player.transform.position.y, c.z); rb.position = spot; player.transform.position = spot; rb.linearVelocity = UnityEngine.Vector3.zero; player.FaceYaw(90f); UnityEngine.Camera.main.transform.localRotation = UnityEngine.Quaternion.identity; return "short of the portal at " + spot;')"
sleep 1
log "$(ev 'var t = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.LairPortalTrigger>(); var c = t.GetComponent<UnityEngine.BoxCollider>().bounds.center; var player = StateMachine.PlayerStateMachine.Local; var rb = player.GetComponent<UnityEngine.Rigidbody>(); var into = new UnityEngine.Vector3(c.x, player.transform.position.y, c.z); rb.position = into; player.transform.position = into; return "stepped in at " + into;')"
sleep 1.2
log "line: $(ev 'return UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.LairPortalTrigger>().HostLine.Alpha.ToString("F2");')"
bash Tools/Unity/capture.sh "$out/05-client-portal-line.png" >/dev/null 2>&1; log "capture: $out/05-client-portal-line.png"
sleep 6
log "$(ev 'var l = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.LairPortalTrigger>().HostLine; return "alpha " + l.Alpha.ToString("F2") + " peak " + l.PeakAlpha.ToString("F2");')"
s="$(state)"; log "state after the client stepped in: $s"
r=no; [ "$s" = LairRoom ] && r=ok; check $r "a client walking into the portal did not set out"
[ "$fails" -eq 0 ] && log "PASS all no-lair-screen checks" || log "FAIL $fails no-lair-screen checks"
exit $((fails > 0))
