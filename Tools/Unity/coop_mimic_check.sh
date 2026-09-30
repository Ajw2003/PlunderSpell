#!/usr/bin/env bash
# Guard mimic prototype check, co-op: the Editor hosts, the Development build joins, both set out.
# The host brings a guard beside the joining player; the joining player is then fed a four-word
# "recording" (a tone per word, as the speech recogniser would report it). Pass: its GuardMimic
# banks the words, starts the local language model, and the guard answers with banked words
# ("[Mimic] ... says" in the client log). The first run starts llama-server, so allow ~10 s.
# Usage: bash Tools/Unity/coop_mimic_check.sh [--build]
# Session setup is copied from coop_drop_latency.sh.
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/coop-mimic-check"
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



log "host: $(bash Tools/Unity/eval.sh 'UnityEngine.Transform them = null; foreach (var r in UnityEngine.Object.FindObjectsByType<Plunderspell.Acoustics.PlayerChatterRelay>(UnityEngine.FindObjectsSortMode.None)) if (!r.isOwner) them = r.transform; if (them == null) return "no joining player"; Plunderspell.Guards.CastleGuard g = null; float bd = 1e9f; foreach (var c in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.CastleGuard>(UnityEngine.FindObjectsSortMode.None)) { float d = (c.transform.position - them.position).sqrMagnitude; if (d < bd) { bd = d; g = c; } } if (g == null) return "no guard"; var spot = them.position + them.forward * 3f; var agent = g.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (agent != null && agent.enabled) agent.Warp(spot); else g.transform.position = spot; return "moved " + g.name + " beside the joining player";' 2>&1 || true)"
sleep 1

speak='var gm = System.Type.GetType("Plunderspell.Audio.Mimic.GuardMimic, Plunderspell.Audio"); var ad = System.Type.GetType("Plunderspell.Audio.AudioDirector, Plunderspell.Audio"); var comp = ((UnityEngine.Component)ad.GetProperty("Instance").GetValue(null)).GetComponent(gm); var wt = System.Type.GetType("Plunderspell.Voice.WordTiming, Plunderspell.Voice"); var rt = System.Type.GetType("Plunderspell.Voice.ChatterReport, Plunderspell.Voice"); var vt = System.Type.GetType("Plunderspell.Voice.CastVolume, Plunderspell.Voice"); string[] w = { "where", "is", "the", "gold" }; var words = System.Array.CreateInstance(wt, w.Length); var samples = new float[16000 * 3]; for (int k = 0; k < w.Length; k++) { float s0 = 0.2f + k * 0.6f, s1 = s0 + 0.4f; words.SetValue(System.Activator.CreateInstance(wt, w[k], s0, s1, 1f), k); for (int i = (int)(s0 * 16000); i < (int)(s1 * 16000); i++) samples[i] = 0.3f * UnityEngine.Mathf.Sin(2f * UnityEngine.Mathf.PI * (220f + 60f * k) * i / 16000f); } var report = System.Activator.CreateInstance(rt, "where is the gold", 0.2f, System.Enum.Parse(vt, "Normal"), words, samples); gm.GetMethod("Hear").Invoke(comp, new object[] { report }); var bank = gm.GetProperty("Bank").GetValue(comp); return "fed four words; bank holds " + bank.GetType().GetProperty("WordCount").GetValue(bank);'
client_eval 'UnityEngine.PlayerPrefs.SetInt("Settings.GuardsHearChatter", 1); return "chatter on";' >/dev/null
sleep 6  # the model starts once a raid has guards and chatter is on
log "client: $(client_eval "$speak")"
reply='var gm = System.Type.GetType("Plunderspell.Audio.Mimic.GuardMimic, Plunderspell.Audio"); var ad = System.Type.GetType("Plunderspell.Audio.AudioDirector, Plunderspell.Audio"); var comp = ((UnityEngine.Component)ad.GetProperty("Instance").GetValue(null)).GetComponent(gm); return "last reply: " + (gm.GetProperty("LastReply").GetValue(comp) ?? "(none yet)");'
for _ in $(seq 1 25); do
    r="$(client_eval "$reply")"
    case "$r" in *"(none yet)"*) sleep 1 ;; *) break ;; esac
done
log "client $r"
sleep 3
log "client: $(client_eval "$speak")"
sleep 3
grep "\[Mimic\]" "$out/client.log" | sed 's/^/client log: /' || log "client log: no [Mimic] lines"
# The client is killed rather than quit, so its language model server is not stopped by the game.
taskkill //IM llama-server.exe //F >/dev/null 2>&1 && log "stopped llama-server" || true
