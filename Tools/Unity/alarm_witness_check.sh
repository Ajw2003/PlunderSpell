#!/usr/bin/env bash
# Live check of the alarm's witness rule (#259), solo: one guard spots the player, then the alarm is sampled
# every 2 s for 30 s. Eval actions: Tools/Unity/eval/alarm_witness_check.cs.
# Usage: bash Tools/Unity/alarm_witness_check.sh [output file]
# Needs the Editor open on this project, not in Play mode. Leaves it stopped.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="${1:-docs/generated/castle-floors-2026-10-03/alarm-witness-run1.txt}"
: > "$out"
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
aw() { ev "$(sed "s|__ACTION__|$1|" Tools/Unity/eval/alarm_witness_check.cs)"; }
trap 'unity command editor_stop --no-banner >/dev/null 2>&1 || true; log "Play stopped"' EXIT

playing="$(ev 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; exit 1; }
if [ "$playing" != "False False" ]; then log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; fi

unity command editor_play --caller plugin --skill unity-cli --no-banner >/dev/null
for _ in $(seq 1 30); do sleep 2; [ "$(ev 'return UnityEditor.EditorApplication.isPlaying.ToString();' 2>/dev/null)" = True ] && break; done
sleep 3
log "start: $(ev --file Tools/Unity/eval/start_solo_raid.cs)"
sleep 4
log "set out: $(ev --file Tools/Unity/eval/set_out_seed.cs)"
sleep 20   # guards settle
log "before: $(aw sample)" ; aw sample >/dev/null || { log "FAIL sample does not run"; exit 1; }
log "spot: $(aw spot)"
for i in $(seq 1 15); do sleep 2; log "t+$((i * 2))s: $(aw sample)"; done
