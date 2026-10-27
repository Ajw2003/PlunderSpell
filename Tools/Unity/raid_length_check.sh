#!/usr/bin/env bash
# Solo check of the raid's length (#433): the Editor plays a raid, raises the pocket watch at the start and saves a
# shot of it, then runs the raid to its end at a raised time scale and measures, in game seconds, how long the clock
# ran. The garrison is cleared first so no guard ends the raid early. Passes when the raid is RaidSeconds (300) long
# and ends on the clock.
# Usage: bash Tools/Unity/raid_length_check.sh <label> [time scale, default 10]
#   label  names docs/generated/ophan-demand-tuning-433/<label>-*
# Needs the Editor open on this project, not in Play mode. Leaves it stopped.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: raid_length_check.sh <label> [time scale]}"
scale="${2:-10}"
out="docs/generated/ophan-demand-tuning-433"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
host() { timeout 90 bash Tools/Unity/coop_eval.sh host "$@" 2>&1; }
probe() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/raid_length.cs" timeout 90 bash Tools/Unity/coop_eval.sh host "$1" "${2:-}" 2>&1; }
num() { sed -n "s/.* $1 \([0-9.-]*\).*/\1/p" <<<"$2"; }
cleanup() {
    log "cleaning up"
    probe slow 1 >/dev/null 2>&1 || true
    unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true
}
trap cleanup EXIT

playing="$(bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; exit 1; }
if [ "$playing" != "False False" ]; then log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; fi

wait_for() {
    for _ in $(seq 1 "$2"); do
        state="$(host state)"
        case "$state" in *"$1"*) log "host: $state"; return 0 ;; esac
        sleep 1
    done
    log "FAIL never reached '$1'; last: ${state:0:300}"; exit 1
}

unity command editor_play "${cli[@]}" >/dev/null
wait_for "state MainMenu" 60
log "solo: $(timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/start_solo_raid.cs)"
wait_for "state LairRoom" 20
log "set out: $(timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs)"
wait_for "state Playing" 30
log "garrison: $(timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/quiet_castle.cs)"

log "watch: $(probe watch)"
sleep 1
start="$(probe read)"; log "start: $start"
bash Tools/Unity/capture.sh "$out/$label-watch-start.png" screen >/dev/null 2>&1 || log "start shot failed"
length="$(num length "$start")"; left0="$(num left "$start")"; t0="$(num gametime "$start")"

log "time scale: $(probe slow "$scale")"
# Back to real time for the last 20 s: a read takes about a second, so at speed the end would be measured coarsely.
end=""; fast=yes
for _ in $(seq 1 240); do
    now="$(probe read)"
    case "$now" in *"done True"*|*"state "[!P]*) end="$now"; break ;; esac
    if [ "$fast" = yes ] && python -c "import sys; sys.exit(0 if float(sys.argv[1]) < 20 else 1)" "$(num left "$now")"; then
        log "near the end, real time: $(probe slow 1) | $now"; fast=no
    fi
    sleep 0.5
done
probe slow 1 >/dev/null
[ -n "$end" ] || { log "FAIL the raid did not end within 240 s of real time; last: $now"; exit 1; }
log "end: $end"
bash Tools/Unity/capture.sh "$out/$label-end.png" screen >/dev/null 2>&1 || log "end shot failed"
t1="$(num gametime "$end")"

verdict="$(python -c "
import sys
length, left0, t0, t1 = map(float, sys.argv[1:5])
ran = (length - left0) + (t1 - t0)   # clock used before the first read, plus game time from then to the end
ok = abs(length - 300.0) < 0.01 and abs(ran - length) <= 2.0
print(('PASS' if ok else 'FAIL') + f' raid length {length:.1f} s; the clock ran {ran:.1f} game s before the raid ended')
" "$length" "$left0" "$t0" "$t1")"
log "$verdict (time scale $scale while it ran)"
case "$verdict" in PASS*) exit 0 ;; *) exit 1 ;; esac
