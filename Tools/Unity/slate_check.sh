#!/usr/bin/env bash
# Solo Play check of the chalk slate on the Goldsmith's counter (#360): from the player's standing eye height in front of the
# counter it captures the slate idle, with a haggle open, after Plus (key 1) and after the sale (key 2), the keys pressed through
# the Input System, never a game call. It also samples the slate's opacity every frame across Plus, which must dip and return (a fade, not a snap). Runs in the test slot (test_slot.sh), leaves the Editor stopped.
# Usage: bash Tools/Unity/slate_check.sh   Captures and log: docs/generated/counter-slate-2026-10-07/
# Needs the Editor open on this project, not in Play mode. Prints PASS or FAIL lines.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/counter-slate-2026-10-07"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/run.log"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
L() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/coop_lair.cs" timeout 90 bash Tools/Unity/coop_eval.sh host "$@" 2>&1; }
keys() { # keys <Key name or none>: the whole keyboard state becomes just that key; the next frame sees it
    local state="new UnityEngine.InputSystem.LowLevel.KeyboardState()"; [ "$1" = none ] || state="new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.$1)"
    ev "UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, $state); return \"keys: $1\";"
}
press() { # the Input System drops keyboard events while the Game view is not focused, so focus it first
    ev 'UnityEditor.EditorWindow.FocusWindowIfItsOpen(System.Type.GetType("UnityEditor.GameView,UnityEditor")); return "game view focused";' >/dev/null
    keys "$1" >/dev/null; sleep 1.5; keys none >/dev/null
}
shot() { bash Tools/Unity/capture.sh "$out/$1.png" >/dev/null 2>&1; seen="$(L line)"; log "capture $out/$1.png; $seen"; }
cleanup() { log "cleaning up"; keys none >/dev/null 2>&1; unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true; test_slot_restore; log "$test_slot_msg"; }
trap cleanup EXIT
fails=0
check() { if [ "$1" = ok ]; then log "PASS $2"; else log "FAIL $2"; fails=$((fails + 1)); fi; }

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

log "$(L travel /LairRoom/MarketDoor)"; sleep 2
log "$(L look)"; sleep 2
shot idle
case "$(L line)" in *"buys metal dearly"*) check ok "idle slate names the Goldsmith and what he buys" ;; *) check no "idle slate names the Goldsmith and what he buys" ;; esac

log "$(L put 4)"; sleep 4
shot haggle-open
case "$(L line)" in *"1 Plus"*) check ok "open haggle shows the keys" ;; *) check no "open haggle shows the keys" ;; esac
log "$(ev 'var s = System.Linq.Enumerable.First(UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.CounterSlate>(UnityEngine.FindObjectsSortMode.None), x => x.Text.Contains("Goldsmith")); UnityEditor.SessionState.SetString("slate_alpha", ""); float until = UnityEngine.Time.realtimeSinceStartup + 6f; UnityEditor.EditorApplication.CallbackFunction watch = null; watch = () => { UnityEditor.SessionState.SetString("slate_alpha", UnityEditor.SessionState.GetString("slate_alpha", "") + s.Alpha.ToString("F2") + " "); if (UnityEngine.Time.realtimeSinceStartup > until) UnityEditor.EditorApplication.update -= watch; }; UnityEditor.EditorApplication.update += watch; return "sampling the slate opacity each frame for 6 s";')"
press Digit1
sleep 5
alphas="$(ev 'return UnityEditor.SessionState.GetString("slate_alpha", "");')"
verdict="$(python -c "import sys; a=[float(x) for x in sys.argv[1].split()]; fade=[x for x in a if 0.05 < x < 0.95]; print('ok' if min(a) < 0.2 and a[-1] > 0.95 and len(fade) >= 4 else 'no', len(a), 'samples, min', min(a), 'last', a[-1], len(fade), 'in between')" "$alphas" 2>&1)"
log "opacity: $verdict"; r=no; [ "${verdict%% *}" = ok ] && r=ok
check $r "Plus faded the slate out and back in over several frames, never a snap"
shot haggle-plus
press Digit2; sleep 1.5
shot sold
case "$seen" in *Done*) check ok "the sale shows his last word" ;; *) check no "the sale shows his last word" ;; esac
sleep 9
shot idle-again
case "$(L line)" in *"buys metal dearly"*) check ok "the slate returns to what he buys" ;; *) check no "the slate returns to what he buys" ;; esac
[ "$fails" -eq 0 ] && log "PASS all slate checks" || log "FAIL $fails slate checks"
exit $((fails > 0))
