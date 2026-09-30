#!/usr/bin/env bash
# Measures how long guards stand still with nowhere to go (#193-#195), in a local co-op raid: the
# Editor hosts in Play mode, a Development build joins it, and Tools/Unity/eval/guard_watch.cs
# samples every guard about once a second for a fixed time. Guards are NOT cleared, unlike the carry
# check; at the start every guard is sent chasing a player's spot so they all lose sight and search.
# Usage: bash Tools/Unity/coop_guard_check.sh <label> [--build|--no-build] [--seconds N] [--solo]
#   label    "before" or "after": names the files under docs/generated/playability-2026-09-30/
#   --solo   host only, no client (used when the co-op session cannot be made to work)
# Needs the Editor open on this project, not in Play mode. Leaves it stopped.
set -uo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: coop_guard_check.sh <label> [options]}"; shift
build=auto; seconds=90; solo=no
while [ $# -gt 0 ]; do
    case "$1" in
        --build) build=yes ;; --no-build) build=no ;; --solo) solo=yes ;;
        --seconds) shift; seconds="$1" ;;
        *) echo "unknown option $1"; exit 1 ;;
    esac
    shift
done
out="docs/generated/playability-2026-09-30"; mkdir -p "$out"
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
cli=(--no-banner --format json)
client_pid=""
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
field() { python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get(sys.argv[1]))" "$1"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
evf() { # evf <action>: run guard_watch.cs with the action filled in (inline: eval_file does not see new files)
    timeout 90 bash Tools/Unity/eval.sh "$(sed "s|__ACTION__|$1|" Tools/Unity/eval/guard_watch.cs)"
}
cleanup() {
    log "cleaning up"
    if [ -n "$client_pid" ] && kill -0 "$client_pid" 2>/dev/null; then
        "${E[@]}" client quit >/dev/null 2>&1 || true; sleep 2; kill "$client_pid" 2>/dev/null || true
    fi
    unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":false}' --confirm true "${cli[@]}" >/dev/null 2>&1 || true
    if grep -q 'fileID: -944628639613478452, guid: e05f63c218fb0e54f8c41ecaf9e2ef10' ProjectSettings/ProjectSettings.asset; then
        python -c "
import re, sys
p = sys.argv[1]
text = open(p, newline='').read()
text = re.sub(r'  preloadedAssets:(\r?\n)  - \{fileID: -944628639613478452, guid: e05f63c218fb0e54f8c41ecaf9e2ef10, type: 3\}', r'  preloadedAssets: []', text)
open(p, 'w', newline='').write(text)
" ProjectSettings/ProjectSettings.asset
    fi
    for s in ProjectSettings/ProjectSettings.asset ProjectSettings/Packages/com.unity.pipeline/RuntimePipelineConfig.json; do
        if ! git diff --quiet -- "$s" 2>/dev/null; then log "left $s: its content changed"
        elif [ -n "$(git status --porcelain -- "$s")" ]; then git checkout -- "$s"; fi
    done
}
trap cleanup EXIT

playing="$(bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; exit 1; }
if [ "$playing" != "False False" ]; then log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; fi

if [ "$solo" = no ]; then
    if [ "$build" = auto ]; then
        build=no; [ -f Build/DevTest/.built ] || build=yes
        if [ "$build" = no ] && [ -n "$(find Assets ProjectSettings -newer Build/DevTest/.built -type f ! -path '*/Tests/*' ! -path 'ProjectSettings/Packages/*' ! -name '*.meta' ! -name 'ProjectSettings.asset' -print -quit)" ]; then build=yes; fi
        log "build: $build (auto)"
    fi
    if [ "$build" = yes ]; then
        log "building Build/DevTest"
        unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":true}' --confirm true "${cli[@]}" >/dev/null
        unity command build --target StandaloneWindows64 --outputPath Build/DevTest/Plunderspell.exe --options '["Development"]' --confirm true "${cli[@]}" >/dev/null
        status=""
        for _ in $(seq 1 120); do sleep 5; status="$(unity command build_status "${cli[@]}" | field status)"; [ "$status" = "completed" ] && break; done
        result="$(unity command build_status "${cli[@]}" | field result)"
        unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":false}' --confirm true "${cli[@]}" >/dev/null
        [ "$result" = "Succeeded" ] || { log "FAIL build: $status $result"; exit 1; }
        touch Build/DevTest/.built; log "build $result"
    fi
fi

wait_for() {
    for _ in $(seq 1 "$3"); do
        state="$("${E[@]}" "$1" state 2>&1)"
        case "$state" in *"$2"*) log "$1: $state"; return 0 ;; esac
        sleep 1
    done
    log "FAIL $1 never reached '$2'; last: ${state:0:300}"; exit 1
}

unity command editor_play "${cli[@]}" >/dev/null
wait_for host "state MainMenu" 60
"${E[@]}" host host_udp >/dev/null
wait_for host "state Lair" 20
if [ "$solo" = no ]; then
    ./Build/DevTest/Plunderspell.exe -coop-join 127.0.0.1 -screen-fullscreen 0 -screen-width 960 -screen-height 540 \
        -logFile "$(cygpath -w "$repo/$out/$label-client.log")" >/dev/null 2>&1 &
    client_pid=$!
    log "client started (pid $client_pid)"
    wait_for client "players 2" 60
fi
seed=3508293
log "castle: $(ev "var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>(); d.SetFixedSeed($seed); var lair = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Lair.LairHubManager>(); if (lair != null) lair.SelectEra(Plunderspell.Inventory.HistoricalEra.LateMedieval); return \"seed $seed, LateMedieval\";")"
timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
wait_for host "state Playing" 30
[ "$solo" = no ] && wait_for client "state Playing" 30
sleep 5
log "$(evf provoke)"
evf report >/dev/null   # registers every guard's starting point
start=$(date +%s)
: > "$out/$label-samples.txt"
while [ $(( $(date +%s) - start )) -lt "$seconds" ]; do
    printf '%s %s\n' "$(( $(date +%s) - start ))s" "$(evf sample)" >> "$out/$label-samples.txt"
    sleep 1
done
evf report | tee "$out/$label-report.txt"
log "state after: $("${E[@]}" host state 2>&1)"
if [ "$solo" = no ]; then
    echo "client log error lines: $(grep -ci 'error\|exception' "$out/$label-client.log")" | tee -a "$out/$label-run.log"
fi
