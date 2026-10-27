#!/usr/bin/env bash
# Checks the castle doors in a local co-op raid (#248): the Editor hosts in Play mode, a Development build
# joins. Both sides must see the same doors; the client opens one by hand and the host must see it open;
# the host locks down and the client must see every door locked; a locked door refuses the client's hand
# but opens to the client's Porta. Actions live in eval/coop_doors.cs. Start-up copied from coop_swarm_check.sh.
# Usage: bash Tools/Unity/coop_door_check.sh <label> [--build|--no-build]
# Needs the Editor open on this project, not in Play mode. Leaves it stopped. Prints PASS or FAIL lines.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/settings_restore.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: coop_door_check.sh <label> [options]}"; shift
build=auto; solo=no; norepeat=no
while [ $# -gt 0 ]; do
    case "$1" in
        --build) build=yes ;; --no-build) build=no ;; --solo) solo=yes ;; --no-repeat) norepeat=yes ;;
        *) echo "unknown option $1"; exit 1 ;;
    esac
    shift
done
out="docs/generated/castle-floors-2026-10-03"; mkdir -p "$out"
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
cli=(--no-banner --format json)
client_pid=""
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/doors-$label-run.log"; }
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
    settings_restore
}
trap cleanup EXIT

playing="$(bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; exit 1; }
if [ "$playing" != "False False" ]; then log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; fi
settings_save || { log "FAIL cannot save ProjectSettings before building"; trap - EXIT; exit 1; }

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
        -logFile "$(cygpath -w "$repo/$out/doors-$label-client.log")" >/dev/null 2>&1 &
    client_pid=$!
    log "client started (pid $client_pid)"
    wait_for client "players 2" 60
fi
seed=777
log "castle: $(ev "var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>(); d.SetFixedSeed($seed); var lair = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Lair.LairHubManager>(); if (lair != null) lair.SelectEra(Plunderspell.Inventory.HistoricalEra.HighMedieval); return \"seed $seed, HighMedieval\";")"
timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
wait_for host "state Playing" 30
wait_for client "state Playing" 30
sleep 5
D() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/coop_doors.cs" timeout 90 bash Tools/Unity/coop_eval.sh "$@" 2>&1; }
fails=0
check() { if [ "$1" = ok ]; then log "PASS $2"; else log "FAIL $2"; fails=$((fails + 1)); fi; }
h="$(D host doors)"; c="$(D client doors)"; log "host: $h"; log "client: $c"
[ "${h%% open*}" = "${c%% open*}" ] && [ "${h%% open*}" != "doors 0" ] && r=ok || r=no; check $r "both sides see the same doors"
log "$(D client hand 0)"; sleep 2
h="$(D host door 0)"; c="$(D client door 0)"; log "host: $h"; log "client: $c"
case "$h$c" in "door 0 open True"*"door 0 open True"*) r=ok ;; *) r=no ;; esac; check $r "the client's hand opens door 0 for both"
log "$(D host lockdown)"; sleep 2
h="$(D host doors)"; c="$(D client doors)"; log "host: $h"; log "client: $c"
[ "$h" = "$c" ] && case "$c" in *"locked 0 "*) false ;; esac && r=ok || r=no; check $r "the client sees the host's lockdown"
log "$(D client hand 1)"; sleep 2
h="$(D host door 1)"; log "host: $h"
case "$h" in "door 1 open False locked True"*) r=ok ;; *) r=no ;; esac; check $r "a locked door refuses the client's hand"
log "$(D client porta 1)"; sleep 2
h="$(D host door 1)"; c="$(D client door 1)"; log "host: $h"; log "client: $c"
case "$h$c" in "door 1 open True"*"door 1 open True"*) r=ok ;; *) r=no ;; esac; check $r "the client's Porta opens a locked door for both"
log "client log error lines: $(grep -ci 'error\|exception' "$out/doors-$label-client.log")"
[ "$fails" -eq 0 ] && log "PASS all door checks" || log "FAIL $fails door checks"
