#!/usr/bin/env bash
# Solo Play check for #352: after dying in a raid and pressing "Back to the Lair", nothing of the raid HUD or the damage tint
# stays on screen. Sets out (GameState.Playing, the portal's own call), kills the local player through IHealth.TakeDamage, waits for
# GameOver, does what the return button does (ChangeState(LairRoom)), then captures the Lair and reads the state that draws the tint.
# Runs in the test slot (test_slot.sh), leaves the Editor stopped.
# Usage: bash Tools/Unity/death_return_check.sh <label: before|after>
# Needs the Editor open on this project, not in Play mode. Output: docs/generated/death-return-2026-10-07/<label>.png and run.log.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: death_return_check.sh <before|after>}"
out="docs/generated/death-return-2026-10-07"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/run.log"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
cleanup() { log "cleaning up"; unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true; test_slot_restore; log "$test_slot_msg"; }
trap cleanup EXIT
gs='Plunderspell.Core.GameServices.GameState'
wait_state() { # wait_state <state> : up to 40 s
    local s=""; for _ in $(seq 1 40); do s="$(ev "return $gs.CurrentState.ToString();" 2>/dev/null)"; [ "$s" = "$1" ] && break; sleep 1; done; log "state $s"; [ "$s" = "$1" ]
}

playing="$(ev 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; trap - EXIT; exit 1; }
[ "$playing" = "False False" ] || { log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; }
test_slot_use || { log "FAIL cannot switch to the test slot: $test_slot_msg"; trap - EXIT; exit 1; }; log "$test_slot_msg"

unity command editor_play "${cli[@]}" >/dev/null
wait_state MainMenu
log "$(ev 'var s = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Net.CoopSession>(); s.PlaySolo(); Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.LairRoom); return "solo";')"
for _ in $(seq 1 30); do p="$(ev 'return StateMachine.PlayerStateMachine.Local != null ? "player" : "none";' 2>/dev/null)"; [ "$p" = player ] && break; sleep 1; done
log "player: $p"; sleep 2

log "$(ev "$gs.ChangeState(Plunderspell.Core.GameState.Playing); return \"set out\";")"
wait_state Playing; sleep 3
bash Tools/Unity/capture.sh "$out/$label-raid.png" >/dev/null && log "captured $out/$label-raid.png"
log "$(ev 'var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Alarm.EnemyDirector>(); var a = d.GetType().GetProperty("Alarm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(d); a.GetType().GetMethod("SetLevel").Invoke(a, new object[] { 100f, 3 }); return "alarm " + Plunderspell.Atmosphere.CastleAtmosphere.CurrentState;')"
sleep 3
log "$(ev 'var h = (Interfaces.IHealth)StateMachine.PlayerStateMachine.Local; var me = StateMachine.PlayerStateMachine.Local.gameObject; Interfaces.Damage.Apply(h, 40f, me, null, me.transform.position, Interfaces.DamageKind.EnemyAttack); Interfaces.Damage.Apply(h, 500f, me, null, me.transform.position, Interfaces.DamageKind.EnemyAttack); return "health " + h.CurrentHealth;')"
wait_state GameOver; sleep 1
bash Tools/Unity/capture.sh "$out/$label-gameover.png" >/dev/null && log "captured $out/$label-gameover.png"
log "$(ev "$gs.ChangeState(Plunderspell.Core.GameState.LairRoom); return \"returned\";")"
wait_state LairRoom; sleep 3
bash Tools/Unity/capture.sh "$out/$label.png" >/dev/null && log "captured $out/$label.png"
log "$(ev 'var f = Plunderspell.UI.DamageFeedbackView.Instance; var p = StateMachine.PlayerStateMachine.Local; var h = (Interfaces.IHealth)p; return "alarm " + Plunderspell.Atmosphere.CastleAtmosphere.CurrentState + ", vignette " + f.Vignette + ", health " + h.CurrentHealth + "/" + h.MaxHealth + ", hurtLines " + System.Linq.Enumerable.Count(f.HurtLines);')"
