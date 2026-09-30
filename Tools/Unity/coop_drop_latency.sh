#!/usr/bin/env bash
# Measures physics-sound timing in co-op: the Editor hosts, the Development build joins, both set
# out, the host lifts the loot piece nearest its camera 2 m and drops it, and both sides log when the
# visible mesh lands, when their collision fires and when their phys_ sound starts
# (AudioLatencyProbe). Physics sounds are muted by SoundFocus in play, so both sides switch it off
# for the run. Prints the two [AudioLatency] lines.
# Usage: bash Tools/Unity/coop_drop_latency.sh [--build]
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/coop-drop-latency"
mkdir -p "$out"
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
log() { printf '%s %s\n' "$(date +%H:%M:%S)" "$*"; }
client_eval() { # client_eval <C# code>: runs code in the client build, prints its result or its error
    unity command --runtime Plunderspell eval --code "$1" --timeout 60 --caller plugin --skill unity-cli --no-banner --format json 2>&1         | python -c "
import json, sys
text = sys.stdin.read()
start = text.find('{')
if start < 0:
    print('ERROR', text.strip()[:400]); sys.exit(0)
envelope, _ = json.JSONDecoder().raw_decode(text[start:])
r = (envelope.get('data') or {}).get('result')
if isinstance(r, str):
    try: r = json.loads(r)
    except Exception: pass
if isinstance(r, dict) and 'result' in r:
    print(r.get('result') if r.get('success', True) else 'ERROR ' + str(r.get('error') or r.get('diagnostics'))[:600])
elif not envelope.get('success'):
    print('ERROR', json.dumps(envelope.get('errors'))[:600])
else:
    print(r)
"
}

playing="$(bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')"
[ "$playing" = "False False" ] || { log "FAIL the Editor is playing or compiling ($playing)"; exit 1; }

if [ "${1:-}" = "--build" ]; then
    log "building Build/DevTest"
    cli=(--no-banner --format json)
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":true}' --confirm true "${cli[@]}" >/dev/null
    unity command build --target StandaloneWindows64 --outputPath Build/DevTest/Plunderspell.exe --options '["Development"]' --confirm true "${cli[@]}" >/dev/null
    result=""
    for _ in $(seq 1 120); do
        sleep 5
        status_json="$(unity command build_status "${cli[@]}")"
        case "$status_json" in *'\"status\":\"completed\"'*) result="$(printf '%s' "$status_json" | grep -o '\\"result\\":\\"[A-Za-z]*' | sed 's/.*\\"//')"; break ;; esac
    done
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":false}' --confirm true "${cli[@]}" >/dev/null
    [ "$result" = "Succeeded" ] || { log "FAIL build result: ${result:-timed out}"; exit 1; }
    touch Build/DevTest/.built
    log "build Succeeded"
fi

wait_for() { # wait_for <side> <text> <seconds>
    for _ in $(seq 1 "$3"); do
        state="$("${E[@]}" "$1" state 2>&1 || true)"
        case "$state" in *"$2"*) log "$1: $state"; return 0 ;; esac
        sleep 1
    done
    log "FAIL $1 never reached '$2'; last: ${state:0:300}"; exit 1
}

unity command editor_play --no-banner --format json >/dev/null
trap 'kill "${client_pid:-0}" 2>/dev/null || true; bash Tools/Unity/eval.sh "UnityEditor.EditorApplication.isPlaying = false; return \"stopped\";" >/dev/null 2>&1 || true' EXIT
wait_for host "state MainMenu" 60
"${E[@]}" host host_udp >/dev/null
wait_for host "state Lair" 20
./Build/DevTest/Plunderspell.exe -coop-join 127.0.0.1 -screen-fullscreen 0 -screen-width 960 -screen-height 540 \
    -logFile "$(cygpath -w "$repo/$out/client.log")" >/dev/null 2>&1 &
client_pid=$!
log "client started (pid $client_pid)"
wait_for client "players 2" 60
timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
wait_for client "Playing" 60
sleep 4

probe() { # probe <method> <args as C# object[] items>: calls a static AudioLatencyProbe method by reflection
    printf 'var t = System.Type.GetType("Plunderspell.Audio.AudioLatencyProbe, Plunderspell.Audio"); return (string)t.GetMethod("%s").Invoke(null, new object[] { %s });' "$1" "$2"
}
log "host: $(bash Tools/Unity/eval.sh "$(probe SetSoundFocus false)" 2>&1 || true)"
log "client: $(client_eval "$(probe SetSoundFocus false)")"

# The host names the piece it will drop by position; the client watches the one nearest there.
host_log_start=$(wc -l < "$LOCALAPPDATA/Unity/Editor/Editor.log")
where="$(bash Tools/Unity/eval.sh "$(probe DropNearestToCamera 2f)" 2>&1 || true)"
log "host: dropping $where"
xyz="${where%% *}"
log "client: $(client_eval "$(probe WatchNearest "${xyz//,/f,}f, 8f")")"
sleep 11

tail -n +"$host_log_start" "$LOCALAPPDATA/Unity/Editor/Editor.log" | grep "\[AudioLatency\] drop" | sed 's/^/host   /' || log "host: no result line"
grep "\[AudioLatency\] watch" "$out/client.log" | sed 's/^/client /' || log "client: no result line"
