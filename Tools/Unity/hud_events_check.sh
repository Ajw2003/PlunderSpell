#!/usr/bin/env bash
# Checks that the raid HUD, now built from events only (#303), shows live values on both sides of a
# local co-op raid: the Editor hosts in Play mode, a Development build joins, both set out. On each side
# it reads RaidHudPresenter.Model twice, a few seconds apart (the raid clock must have counted down: on the
# client that clock arrives by replication and reaches the model only through ExtractionTimerChanged), then
# the host raises the alarm to 60 and both models must follow it (AlarmLevelChanged). It also saves the
# host's screen with the HUD on it.
# Usage: bash Tools/Unity/hud_events_check.sh <label> [--build|--no-build]
#   label  names docs/generated/hud-events-303/<label>-*
# Needs the Editor open on this project, not in Play mode. Leaves it stopped. Raid flow from perf_capture.sh.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/settings_restore.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: hud_events_check.sh <label> [--build|--no-build]}"; shift
build=auto
while [ $# -gt 0 ]; do
    case "$1" in --build) build=yes ;; --no-build) build=no ;; *) echo "unknown option $1"; exit 1 ;; esac
    shift
done
out="docs/generated/hud-events-303"; mkdir -p "$out"
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
cli=(--no-banner --format json)
client_pid=""
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
field() { python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get(sys.argv[1]))" "$1"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
# rt <host|client> <C#>: runs C# on one side and prints the result.
rt() {
    local target=(); [ "$1" = client ] && target=(--runtime Plunderspell)
    unity command "${target[@]}" eval --code "$2" --timeout 60 --caller plugin --skill unity-cli "${cli[@]}" 2>&1 \
        | python -c "import json,sys; t=sys.stdin.read(); e,_=json.JSONDecoder().raw_decode(t[t.find('{'):]); r=(e.get('data') or {}).get('result'); r=json.loads(r) if isinstance(r,str) and r.startswith('{') else r; print(r.get('result') if isinstance(r,dict) else r or e.get('errors'))"
}
model='var t = System.Type.GetType("Plunderspell.UI.RaidHudPresenter, Plunderspell.RaidHud"); var p = UnityEngine.Object.FindFirstObjectByType(t); if (p == null) return "no presenter"; var m = t.GetProperty("Model").GetValue(p); var mt = m.GetType(); System.Func<string, string> F = n => mt.GetField(n).GetValue(m).ToString(); return "phase=" + F("Phase") + " time=" + F("TimeRemaining") + " alarm=" + F("Alarm") + " level=" + F("AlarmLevel") + " debt=" + F("Debt") + " state=" + F("State");'
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
    for _ in $(seq 1 180); do sleep 5; status="$(unity command build_status "${cli[@]}" | field status)"; [ "$status" = "completed" ] && break; done
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
./Build/DevTest/Plunderspell.exe -coop-join 127.0.0.1 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 \
    -logFile "$(cygpath -w "$repo/$out/$label-client.log")" >/dev/null 2>&1 &
client_pid=$!
log "client started (pid $client_pid)"
wait_for client "players 2" 60
timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
wait_for host "state Playing" 30
wait_for client "state Playing" 30
sleep 5

log "host   model 1: $(rt host "$model")"
log "client model 1: $(rt client "$model")"
sleep 4
log "host   model 2: $(rt host "$model")"
log "client model 2: $(rt client "$model")"
log "alarm raised: $(ev 'var d = Plunderspell.Alarm.EnemyDirector.Current; if (d == null) return "no director"; d.SetAlarmLevel(60f); return "host alarm level 60";')"
sleep 2
log "host   after alarm: $(rt host "$model")"
log "client after alarm: $(rt client "$model")"
log "screenshot: $(timeout 90 bash Tools/Unity/capture.sh "$out/$label-host.png" screen 2>&1 | tail -1)"
echo "client log error lines: $(grep -ci 'error\|exception' "$out/$label-client.log")" | tee -a "$out/$label-run.log"
