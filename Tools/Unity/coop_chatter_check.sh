#!/usr/bin/env bash
# PR #177 check (guards overhear player chatter), co-op: the Editor hosts, the Development build
# joins, both set out. On each side it reports every PlayerChatterRelay (owner? subscribed to the
# voice service?), then feeds one line of talk into the joining player's own relay, as speech
# recognition would, and reads whether the host resolved it and the caption came back.
# Needs PR #177 (PlayerChatterRelay, CastleGuard.OverheardCount) in the checkout it builds from.
# Usage: bash Tools/Unity/coop_chatter_check.sh [--build]
#
# Session setup is copied from coop_drop_latency.sh.
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/coop-chatter-check"
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


relays='var sb = new System.Text.StringBuilder(); var t = System.Type.GetType("Plunderspell.Acoustics.PlayerChatterRelay, Plunderspell.Acoustics"); var sub = t.GetField("_subscribed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); foreach (var r in UnityEngine.Object.FindObjectsByType(t, UnityEngine.FindObjectsSortMode.None)) { sb.Append(((UnityEngine.Component)r).name + " owner=" + t.GetProperty("isOwner").GetValue(r) + " subscribed=" + sub.GetValue(r) + "; "); } var src = System.Type.GetType("Plunderspell.Voice.VoiceServiceLocator, Plunderspell.Voice").GetProperty("Current").GetValue(null); return sb.ToString() + "voice service " + (src == null ? "none" : src.GetType().Name);'
log "host relays: $(bash Tools/Unity/eval.sh "$relays" 2>&1 || true)"
log "client relays: $(client_eval "$relays")"

say='UnityEngine.PlayerPrefs.SetInt("Settings.GuardsHearChatter", 1); var t = System.Type.GetType("Plunderspell.Acoustics.PlayerChatterRelay, Plunderspell.Acoustics"); object mine = null; foreach (var r in UnityEngine.Object.FindObjectsByType(t, UnityEngine.FindObjectsSortMode.None)) if ((bool)t.GetProperty("isOwner").GetValue(r)) mine = r; if (mine == null) return "no owned relay"; var rt = System.Type.GetType("Plunderspell.Voice.ChatterReport, Plunderspell.Voice"); var vt = System.Type.GetType("Plunderspell.Voice.CastVolume, Plunderspell.Voice"); var report = System.Activator.CreateInstance(rt, "where is the gold", 0.2f, System.Enum.Parse(vt, "Shout")); t.GetMethod("HandleChatter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(mine, new object[] { report }); return "said it";'
# Host: bring the nearest guard to 3 m from the joining player's body, so the words have a listener.
log "host: $(bash Tools/Unity/eval.sh 'UnityEngine.Transform them = null; foreach (var r in UnityEngine.Object.FindObjectsByType<Plunderspell.Acoustics.PlayerChatterRelay>(UnityEngine.FindObjectsSortMode.None)) if (!r.isOwner) them = r.transform; if (them == null) return "no joining player"; Plunderspell.Guards.CastleGuard g = null; float bd = 1e9f; foreach (var c in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.CastleGuard>(UnityEngine.FindObjectsSortMode.None)) { float d = (c.transform.position - them.position).sqrMagnitude; if (d < bd) { bd = d; g = c; } } if (g == null) return "no guard"; var spot = them.position + them.forward * 3f; var agent = g.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (agent != null && agent.enabled) agent.Warp(spot); else g.transform.position = spot; return "moved " + g.name + " beside the joining player";' 2>&1 || true)"
sleep 1
log "client: $(client_eval "$say")"
sleep 2
caption='var t = System.Type.GetType("Plunderspell.UI.RaidHudView, Plunderspell.RaidHud"); var v = UnityEngine.Object.FindFirstObjectByType(t); if (v == null) return "no hud"; return "caption: " + t.GetField("_caption", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(v);'
log "client $(client_eval "$caption")"
log "host guards overheard: $(bash Tools/Unity/eval.sh 'int n = 0; string last = null; foreach (var g in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.CastleGuard>(UnityEngine.FindObjectsSortMode.None)) { n += g.OverheardCount; if (g.LastOverheard != null) last = g.LastOverheard; } return n + " line(s), last \"" + last + "\"";' 2>&1 || true)"
