#!/usr/bin/env bash
# Co-op check of the Lantern Warden turning into the Wick (#417): the Editor hosts a raid in Play mode, a
# Development build joins. A warden stands in front of the host's player with its back turned; the check saves a
# "before" shot on both machines, turns the warden to face the player (a confirmed sighting), then saves shots
# during the turn and after it. Each step logs what the host and the client each see of the warden: replicated
# state, turned flag, the Wick showing, the loose lantern's position (the two machines must agree), and the
# players' health (a catch must kill).
# Usage: bash Tools/Unity/fear_wick_check.sh <label> [--build|--no-build]
#   label  names docs/generated/fear-wick-2026-10-15/<label>-*
# Needs the Editor open on this project, not in Play mode. Leaves it stopped. Start-up from coop_guard_check.sh.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/settings_restore.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: fear_wick_check.sh <label> [--build|--no-build]}"; shift
build=auto
while [ $# -gt 0 ]; do
    case "$1" in --build) build=yes ;; --no-build) build=no ;; *) echo "unknown option $1"; exit 1 ;; esac
    shift
done
out="docs/generated/fear-wick-2026-10-15"; mkdir -p "$out"
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
cli=(--no-banner --format json)
client_pid=""
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
field() { python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get(sys.argv[1]))" "$1"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
host() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/fear_wick_host.cs" timeout 90 bash Tools/Unity/coop_eval.sh host "$1" 2>&1; }
read_side() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/fear_wick_read.cs" timeout 90 bash Tools/Unity/coop_eval.sh "$1" "$2" "${3:-}" 2>&1; }
# shots <name>: one screenshot from each machine's own view.
shots() {
    bash Tools/Unity/capture.sh "$out/$label-$1-host.png" screen >/dev/null 2>&1 || log "host shot $1 failed"
    log "client shot: $(read_side client shot "$(cygpath -m "$repo/$out")/$label-$1-client.png")"
}
# both <step>: what each machine reads of the warden.
both() { log "$1 | host:   $(read_side host read)"; log "$1 | client: $(read_side client read)"; }
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
timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
wait_for host "state Playing" 30
wait_for client "state Playing" 30
sleep 5

log "spawn: $(host spawn)"
sleep 2
log "look: $(host look)"
log "client: $(read_side client face)"
sleep 1
both "before the turn"
shots before
slow() { log "slow $1: host $(read_side host slow "$1") | client $(read_side client slow "$1")"; }
log "provoke: $(host provoke)"
slow 0.2   # screenshots take seconds; the Wick hunts at 4.2 m/s
shots turning
both "start of the turn"
sleep 8
shots standing
both "the Wick standing (model swapped)"
sleep 2
shots stalking
both "stalking or hunting"
# The caught player as the other machine sees it: wait (bounded) for either machine's own player to be dead,
# turn the other machine's player to look at that body, then shoot both. The other machine must read the caught
# player as dead, down and drawn down; it once kept them standing at health 100.
# has <read> <regex>: whether one player entry of a read (entries split on '|') matches.
has() { tr '|' '
' <<<"$1" | grep -qE "$2"; }
caught=""
for _ in $(seq 1 40); do
    if has "$(read_side host read)" 'local=True health=[^ ]+ dead=True'; then caught=host; watcher=client
    elif has "$(read_side client read)" 'local=True health=[^ ]+ dead=True'; then caught=client; watcher=host; fi
    [ -n "$caught" ] && break
    sleep 0.5
done
if [ -z "$caught" ]; then
    log "FAIL nobody was caught within 20 s"; exit 1
fi
# Near-freeze the watcher first: on the host that also stops the Wick, so it cannot catch the watcher mid-shot.
log "caught: the $caught's player; the $watcher slows: $(read_side "$watcher" slow 0.02); looks: $(read_side "$watcher" faceteammate)"
sleep 1
shots caught-player
both "the caught player"
seen="$(read_side "$watcher" read)"
if has "$seen" 'local=False health=[^ ]+ dead=True netDown=True drawnDown=True'; then
    log "PASS the $watcher sees the $caught's player down"
else
    log "FAIL the $watcher still sees the $caught's player alive or standing: $seen"; exit 1
fi
# The catch: wait (bounded) for the host's warden to reach Combat, slow right down, then shoot.
for _ in $(seq 1 60); do
    case "$(read_side host read)" in *"state=Combat"*) break ;; esac
    sleep 0.5
done
slow 0.05
shots catch
both "at the catch"
slow 1
sleep 3
both "after the catch"
shots after
log "client log error lines: $(grep -ci 'error\|exception' "$out/$label-client.log")"
