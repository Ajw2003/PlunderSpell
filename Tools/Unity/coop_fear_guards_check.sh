#!/usr/bin/env bash
# Co-op look at #420: for each Age, the Editor hosts a raid in Play mode, a Development build in
# Build/DevTest joins it, and the host's view is put in a ground-floor room facing its guards (see
# Tools/Unity/eval/fear_guard_view.cs). Saves one screenshot per Age under
# docs/generated/fear-guards-<date>/ and the guard counts beside it.
# Usage: bash Tools/Unity/coop_fear_guards_check.sh [--build | --no-build]
# Needs the Editor open on this project, not in Play mode. Leaves it stopped. Build/DevTest must
# already exist (build it with coop_carry_check.sh --build, or pass --build here).
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/settings_restore.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/fear-guards-2026-10-15"; mkdir -p "$out"
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
cli=(--no-banner --format json)
client_pid=""
seed=3508293
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/run.log"; }
field() { python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get(sys.argv[1]))" "$1"; }
view() { COOP_EVAL_FILE=Tools/Unity/eval/fear_guard_view.cs "${E[@]}" host "$1"; }

stop_session() {
    if [ -n "$client_pid" ] && kill -0 "$client_pid" 2>/dev/null; then
        "${E[@]}" client quit >/dev/null 2>&1 || true; sleep 2; kill "$client_pid" 2>/dev/null || true
    fi
    client_pid=""
    unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true
}
cleanup() {
    log "cleaning up"; stop_session
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":false}' --confirm true "${cli[@]}" >/dev/null 2>&1 || true
    settings_restore
}
trap cleanup EXIT

wait_for() { # wait_for <side> <text> <seconds>
    for _ in $(seq 1 "$3"); do
        state="$("${E[@]}" "$1" state 2>&1)"
        case "$state" in *"$2"*) log "$1: $state"; return 0 ;; esac
        sleep 1
    done
    log "FAIL $1 never reached '$2'; last: ${state:0:300}"; exit 1
}

playing="$(bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; exit 1; }
if [ "$playing" != "False False" ]; then log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; fi
settings_save || { log "FAIL cannot save ProjectSettings"; trap - EXIT; exit 1; }

if [ "${1:-}" = "--build" ]; then
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

for era in BronzeAge HighMedieval LateMedieval AgeOfPowder; do
    log "=== $era ==="
    unity command editor_play "${cli[@]}" >/dev/null
    wait_for host "state MainMenu" 60
    "${E[@]}" host host_udp >/dev/null
    wait_for host "state Lair" 20
    ./Build/DevTest/Plunderspell.exe -coop-join 127.0.0.1 -screen-fullscreen 0 -screen-width 960 -screen-height 540 \
        -logFile "$(cygpath -w "$repo/$out/$era-client.log")" >/dev/null 2>&1 &
    client_pid=$!
    wait_for client "players 2" 60
    timeout 90 bash Tools/Unity/eval.sh "var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>(); d.SetFixedSeed($seed); var lair = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Lair.LairHubManager>(); if (lair != null) lair.SelectEra(Plunderspell.Inventory.HistoricalEra.$era); return \"seed $seed, $era\";" | tee -a "$out/run.log"
    timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
    wait_for host "state Playing" 30
    wait_for client "state Playing" 30
    sleep 6
    log "$(view frame)"
    sleep 1
    timeout 60 bash Tools/Unity/capture.sh "$out/$era-ground-floor.png" 2>&1 | tee -a "$out/run.log"
    log "$era $(view count)"
    "${E[@]}" client shot "$(cygpath -m "$repo/$out/$era-client.png")" >/dev/null 2>&1
    stop_session
    sleep 5
done
log "done"
