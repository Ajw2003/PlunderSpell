#!/usr/bin/env bash
# Measures what a calm guard hears and sees of the host's player (#238), in a local co-op raid: the
# Editor hosts in Play mode and a Development build joins it. Eval actions: Tools/Unity/eval/guard_awareness.cs.
# Usage: bash Tools/Unity/guard_awareness_check.sh <label> [--build|--no-build]
#   label  "before" or "after": names the files under docs/generated/guard-awareness-2026-10-02/
# Needs the Editor open on this project, not in Play mode. Leaves it stopped and the client closed.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/settings_restore.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: guard_awareness_check.sh <label> [--build|--no-build]}"; shift
build=auto
while [ $# -gt 0 ]; do
    case "$1" in --build) build=yes ;; --no-build) build=no ;; *) echo "unknown option $1"; exit 1 ;; esac
    shift
done
out="docs/generated/guard-awareness-2026-10-02"; mkdir -p "$out"
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
cli=(--no-banner --format json)
client_pid=""
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
field() { python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get(sys.argv[1]))" "$1"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
aw() { # aw <action> [arg] [arg2]: one action of guard_awareness.cs (inline: eval_file does not see new files)
    timeout 90 bash Tools/Unity/eval.sh "$(sed -e "s|__ACTION__|$1|" -e "s|__ARG__|${2:-}|" -e "s|__ARG2__|${3:-}|" Tools/Unity/eval/guard_awareness.cs)"
}
cleanup() {
    log "cleaning up"
    if [ -n "$client_pid" ] && kill -0 "$client_pid" 2>/dev/null; then
        "${E[@]}" client quit >/dev/null 2>&1 || true; sleep 2; kill "$client_pid" 2>/dev/null || true
    fi
    unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":false}' --confirm true "${cli[@]}" >/dev/null 2>&1 || true
    settings_restore
}
trap cleanup EXIT

playing="$(bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; exit 1; }
if [ "$playing" != "False False" ]; then log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; fi
settings_save || { log "FAIL cannot save ProjectSettings before building"; trap - EXIT; exit 1; }

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
./Build/DevTest/Plunderspell.exe -coop-join 127.0.0.1 -screen-fullscreen 0 -screen-width 960 -screen-height 540 \
    -logFile "$(cygpath -w "$repo/$out/$label-client.log")" >/dev/null 2>&1 &
client_pid=$!
log "client started (pid $client_pid)"
wait_for client "players 2" 60
seed=3508293
log "castle: $(ev "var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>(); d.SetFixedSeed($seed); var lair = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Lair.LairHubManager>(); if (lair != null) lair.SelectEra(Plunderspell.Inventory.HistoricalEra.LateMedieval); return \"seed $seed, LateMedieval\";")"
timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
wait_for host "state Playing" 30
wait_for client "state Playing" 30
sleep 10   # past the calm arrival grace
log "pick: $(aw pick)"
log "info: $(aw info)"

# a. Footsteps: the positive control (the emitter called by hand) and the real thing (the player moving).
for d in 4 8 12; do
    log "--- footstep control, guard $d m behind"
    log "$(aw place BEHIND "$d")"; log "$(aw footstep Walk)"; sleep 0.4; log "read: $(aw read)"
done
for d in 3 6 10; do
    log "--- player walking, guard $d m behind"
    log "$(aw place WALK "$d")"
    log "$(aw walk 1.5)"; sleep 2.5
    log "read: $(aw read)"
done
# b. A jump landing and dropped loot, guard 3 m behind.
log "--- jump landing, guard 3 m behind"
log "$(aw place BEHIND 3)"; log "$(aw jump)"; sleep 2.5; log "read: $(aw read)"
log "--- loot dropped beside a guard 3 m behind"
log "$(aw place BEHIND 3)"; log "$(aw loot)"; sleep 2; log "read: $(aw read)"
# c. Sight: the guard faces the player, who is UP m higher than the guard, 3.5 m away.
for dist in 3.5 1.5; do
    for up in 0 1 2.5 4 8; do
        log "--- sight, player $up m above a guard facing it $dist m away"
        log "$(aw place "FRONT:$up" "$dist")"; aw look >/dev/null; sleep 0.5; log "read: $(aw read)"
    done
done
log "--- mid-jump, guard facing the player 1.5 m away"
log "$(aw place FRONT 1.5)"; log "$(aw jump)"; sleep 3; log "read: $(aw read)"
aw place BEHIND 30 >/dev/null   # puts the player back on its feet, not held up
log "done"
