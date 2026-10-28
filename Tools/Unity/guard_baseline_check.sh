#!/usr/bin/env bash
# Records how guards behave in the five scenarios from aj's 2026-10-28 co-op playtest (#480, #489), in the Ophan Gym
# in co-op: a guard beside the player, a guard at a checked spot, a chase broken off behind cover, three melee guards
# on one player, and a guard and player either side of a beam hole. Stages each through eval/guard_baseline.cs on the
# host, samples the guards twice a second into run.log, and screen-records the desktop for the frame-by-frame read.
# Usage: bash Tools/Unity/guard_baseline_check.sh <label>
#   label  "before" or "after": the folder docs/generated/guard-awareness-<label>/
# Needs the gym already running in co-op (bash Tools/Unity/coop_local.sh --gym). Leaves it running.
set -uo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$here/pin.sh"
repo="$(cd "$here/../.." && pwd)"
cd "$repo"
label="${1:?usage: guard_baseline_check.sh <label>}"
out="docs/generated/guard-awareness-$label"; mkdir -p "$out"
: > "$out/run.log"
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/run.log"; }
gb() { # gb <action> [arg]: one action of guard_baseline.cs on the host
    timeout 60 bash "$here/eval.sh" "$(sed -e "s|__ACTION__|$1|" -e "s|__ARG__|${2:-}|" "$here/eval/guard_baseline.cs")"
}
gym() { COOP_EVAL_FILE="$here/eval/ophan_gym_check.cs" timeout 60 bash "$here/coop_eval.sh" "$1" call "$2"; }
watch() { # watch <seconds>: samples the subject guards twice a second
    local end=$((SECONDS + $1))
    while [ $SECONDS -lt $end ]; do log "  $(gb sample)"; sleep 0.5; done
}

playing="$(bash "$here/eval.sh" 'return UnityEditor.EditorApplication.isPlaying + "";')"
[ "$playing" = "True" ] || { log "FAIL the Editor is not in Play mode; start the gym with coop_local.sh --gym"; exit 1; }

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$here/place_windows.ps1")" >/dev/null 2>&1 || log "note: place_windows.ps1 failed"
# Fragmented MP4, so stopping ffmpeg by force still leaves a playable file (a Ctrl-C from Git Bash does not reach it).
ffmpeg -y -loglevel error -f gdigrab -framerate 30 -i desktop -t 420 -c:v libx264 -preset ultrafast     -movflags +frag_keyframe+empty_moov "$out/run.mp4" &
stop_recording() {
    powershell.exe -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='ffmpeg.exe'\" | Where-Object { \$_.CommandLine -like '*guard-awareness-$label*' } | ForEach-Object { Stop-Process -Id \$_.ProcessId }" >/dev/null 2>&1
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$here/place_windows.ps1")" release >/dev/null 2>&1
}
trap stop_recording EXIT
sleep 2
log "recording started"

log "set out: $(timeout 60 bash "$here/eval.sh" --file "$here/eval/set_out.cs")"
prep=""
for _ in $(seq 1 30); do prep="$(gb prep)"; case "$prep" in *"guards 0;"*|*"map False"*) sleep 2 ;; *) break ;; esac; done
log "prep: $prep"
case "$prep" in *"guards 0;"*|*"map False"*) log "FAIL no castle or guards 60 s after setting out"; exit 1 ;; esac
# The Ophan arrives 30 s into a gym raid and may break stone: snap the map before it can.
log "map snap: $(gb snap)"
me() { COOP_EVAL_FILE="$here/eval/local_player.cs" timeout 60 bash "$here/coop_eval.sh" "$@"; }
log "host: $(me host untouchable)"
log "client: $(me client untouchable)"
log "client: $(me client park "$(gb far)")"

log "SCENARIO beside: $(gb stage beside)"; watch 5
log "SCENARIO spot: $(gb stage spot)"; watch 14
log "SCENARIO pillar: $(gb stage pillar)"; watch 2
log "  hide: $(gb hide)"; watch 18
log "SCENARIO trio: $(gb stage trio)"; watch 12

log "SCENARIO hole: summoning the Ophan to break the castle: $(gym host Summon)"
holes="holes 0"
for _ in $(seq 1 24); do sleep 5; holes="$(gb holes)"; log "  $holes"; [ "$holes" != "holes 0" ] && break; done
if [ "$holes" = "holes 0" ]; then
    log "SKIP hole: no floor hole after 120 s"
else
    sleep 3
    log "  stage: $(gb stage hole)"; watch 12
fi
log "done"
