#!/usr/bin/env bash
# Solo Play check that the Lair room and Market are safe (#355): in the Lair a hit through Damage.Apply leaves the player's
# health alone and a pile piece neither would break nor breaks on a hard impact; the moment the state is a raid, the same
# impact would break it (the rule must not leak into raids). Runs in the test slot (test_slot.sh), leaves the Editor stopped.
# Usage: bash Tools/Unity/safe_lair_check.sh      Log: docs/generated/safe-lair-2026-10-07/run.log. Prints PASS or FAIL lines.
# Needs the Editor open on this project, not in Play mode.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/safe-lair-2026-10-07"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/run.log"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
L() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/coop_lair.cs" timeout 90 bash Tools/Unity/coop_eval.sh host "$@" 2>&1; }
cleanup() { log "cleaning up"; unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true; test_slot_restore; log "$test_slot_msg"; }
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
log "$(L spawnpile 4)"; sleep 3

hit="$(ev 'var h = StateMachine.PlayerStateMachine.Local.GetComponentInChildren<Interfaces.IHealth>(); float before = h.CurrentHealth; float lost = Interfaces.Damage.Apply(h, 25f, null, null, UnityEngine.Vector3.zero, Interfaces.DamageKind.Impact); return "lost " + lost + " health " + before + " -> " + h.CurrentHealth;')"
log "player hit in the Lair: $hit"
case "$hit" in "lost 0 "*) python -c "import sys; a,b=sys.argv[1].split(' -> '); sys.exit(0 if float(a.split()[-1])==float(b) else 1)" "$hit" && check ok "a 25-point hit in the Lair costs the player nothing" || check no "a hit in the Lair costs the player nothing" ;; *) check no "a hit in the Lair costs the player nothing" ;; esac

lair="$(ev 'var p = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Loot.LootPickup>(); bool would = p.WouldBreak(1000f); p.BreakItem(); return "piece " + p.name + " would break " + would + " broken " + p.IsBroken;')"
log "hard impact in the Lair: $lair"
case "$lair" in *"would break False broken False") check ok "a piece in the Lair neither would break nor breaks on a 1000 m/s impact" ;; *) check no "a piece in the Lair stays whole on a hard impact" ;; esac

raid="$(ev 'var states = Plunderspell.Core.GameServices.GameState; states.ChangeState(Plunderspell.Core.GameState.Playing); var p = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Loot.LootPickup>(); bool would = p.WouldBreak(1000f); bool safe = Interfaces.Damage.InSafePlace; states.ChangeState(Plunderspell.Core.GameState.LairRoom); return "raid: safe " + safe + " would break " + would;')"
log "the same impact in a raid: $raid"
[ "$raid" = "raid: safe False would break True" ] && check ok "in a raid the same impact would break it (the rule stays in the Lair)" || check no "in a raid the same impact would break it"

[ "$fails" -eq 0 ] && log "PASS all safe-Lair checks" || log "FAIL $fails safe-Lair checks"
exit $((fails > 0))
