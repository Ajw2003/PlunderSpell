#!/usr/bin/env bash
# Co-op look at the wizard body (#362): the Editor hosts in Play mode, a Development build joins, both stand in the
# Lair room. The host stands about 3 m from the client's wizard, looking at it, and the client turns to face the host.
# Then the client holds keys through the Input System (none, C, V, D: idle, crouching, casting, walking) and for each
# pose the host saves a screenshot and logs what its copy of the client's animator holds: Speed, Crouch, Casting.
# A pose passes when the host's animator reads it (walking: Speed above 0.5). Keys go to the client because the
# Editor's Input System ignores the keyboard while its Game view is not focused, and the client window has focus;
# the client's flags reach the host over the network, the harder direction.
# Usage: bash Tools/Unity/wizard_coop_check.sh <label> [--build|--no-build]
#   label  names docs/generated/wizard-362/<label>-*
# Needs the Editor open on this project, not in Play mode. Leaves it stopped. Start-up from hud_events_check.sh.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/settings_restore.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: wizard_coop_check.sh <label> [--build|--no-build]}"; shift
build=auto
while [ $# -gt 0 ]; do
    case "$1" in --build) build=yes ;; --no-build) build=no ;; *) echo "unknown option $1"; exit 1 ;; esac
    shift
done
out="docs/generated/wizard-362"; mkdir -p "$out"
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
cli=(--no-banner --format json)
client_pid=""
fails=0
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
check() { if [ "$1" = ok ]; then log "PASS $2"; else log "FAIL $2"; fails=$((fails + 1)); fi; }
field() { python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get(sys.argv[1]))" "$1"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
L() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/coop_lair.cs" timeout 90 bash Tools/Unity/coop_eval.sh "$@" 2>&1; }
# rt <host|client> <C#>: runs C# on one side and prints the result.
rt() {
    local target=(); [ "$1" = client ] && target=(--runtime Plunderspell)
    unity command "${target[@]}" eval --code "$2" --timeout 60 --caller plugin --skill unity-cli "${cli[@]}" 2>&1 \
        | python -c "import json,sys; t=sys.stdin.read(); e,_=json.JSONDecoder().raw_decode(t[t.find('{'):]); r=(e.get('data') or {}).get('result'); r=json.loads(r) if isinstance(r,str) and r.startswith('{') else r; print(r.get('result') if isinstance(r,dict) else r or e.get('errors'))"
}
# keys <Key name or none>: on the client, the whole keyboard state becomes just that key (as lair_input_check.sh does).
# The build's eval has no reference to the Input System assembly, so its types are reached by name.
keys() {
    rt client 'var a = ", Unity.InputSystem"; var keyT = System.Type.GetType("UnityEngine.InputSystem.Key" + a); var stateT = System.Type.GetType("UnityEngine.InputSystem.LowLevel.KeyboardState" + a);
var pressed = System.Array.CreateInstance(keyT, "'"$1"'" == "none" ? 0 : 1); if (pressed.Length == 1) pressed.SetValue(System.Enum.Parse(keyT, "'"$1"'"), 0);
var state = System.Activator.CreateInstance(stateT, new object[] { pressed });
var keyboard = System.Type.GetType("UnityEngine.InputSystem.Keyboard" + a).GetProperty("current").GetValue(null);
System.Reflection.MethodInfo queue = null; foreach (var m in System.Type.GetType("UnityEngine.InputSystem.InputSystem" + a).GetMethods()) if (m.Name == "QueueStateEvent" && m.IsGenericMethodDefinition) queue = m;
queue.MakeGenericMethod(stateT).Invoke(null, new object[] { keyboard, state, -1.0 });
return "client keys: '"$1"'";'
}

# The other player's body as this side sees it: the PlayerStateMachine that is not local.
other='var pt = System.Type.GetType("StateMachine.PlayerStateMachine, Plunderspell.Player"); UnityEngine.Component other = null; foreach (UnityEngine.Component p in UnityEngine.Object.FindObjectsByType(pt, UnityEngine.FindObjectsSortMode.None)) if (!(bool)pt.GetProperty("IsLocal").GetValue(p)) other = p;'
# Viewer (host): stand 3 m from the other body (keeping the side we are on), face it, eyes a little down.
stand="$other"' if (other == null) return "no other body"; var me = (UnityEngine.Component)pt.GetProperty("Local").GetValue(null); var away = me.transform.position - other.transform.position; away.y = 0f; if (away.sqrMagnitude < 0.01f) away = UnityEngine.Vector3.forward; var from = other.transform.position + away.normalized * 3f; from.y = me.transform.position.y; var body = me.GetComponent<UnityEngine.Rigidbody>(); if (body != null) { body.linearVelocity = UnityEngine.Vector3.zero; body.position = from; } me.transform.position = from; var d = other.transform.position - from; d.y = 0f; float yaw = UnityEngine.Quaternion.LookRotation(d).eulerAngles.y; pt.GetMethod("FaceYaw").Invoke(me, new object[] { yaw }); pt.GetField("_xRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(me, 8f); return "host 3 m from the client at " + from;'
# Actor (client): turn to face the host's body.
face="$other"' if (other == null) return "no other body"; var me = (UnityEngine.Component)pt.GetProperty("Local").GetValue(null); var d = other.transform.position - me.transform.position; d.y = 0f; pt.GetMethod("FaceYaw").Invoke(me, new object[] { UnityEngine.Quaternion.LookRotation(d).eulerAngles.y }); return "client faces the host";'
# Viewer (host): what its copy of the client's wizard animator holds now.
anim="$other"' if (other == null) return "no other body"; var a = other.GetComponentInChildren<UnityEngine.Animator>(); if (a == null) return "no animator on the other body"; var at = other.transform.position; return "at " + at.x.ToString("F2") + "," + at.z.ToString("F2") + " Speed=" + a.GetFloat("Speed").ToString("F2") + " Crouch=" + a.GetBool("Crouch") + " Casting=" + a.GetBool("Casting") + " Dead=" + a.GetBool("Dead") + " avatarHuman=" + (a.avatar != null && a.avatar.isHuman) + " controller=" + (a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "none");'

cleanup() {
    log "cleaning up"
    rt client 'UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState()); return "keys up";' >/dev/null 2>&1
    if [ -n "$client_pid" ] && kill -0 "$client_pid" 2>/dev/null; then
        "${E[@]}" client quit >/dev/null 2>&1 || true; sleep 2; kill "$client_pid" 2>/dev/null || true
    fi
    unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":false}' --confirm true "${cli[@]}" >/dev/null 2>&1 || true
    settings_restore
    test_slot_restore; log "$test_slot_msg"
}
trap cleanup EXIT

playing="$(bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; exit 1; }
if [ "$playing" != "False False" ]; then log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; fi
settings_save || { log "FAIL cannot save ProjectSettings before building"; trap - EXIT; exit 1; }
test_slot_use || { log "FAIL cannot switch to the test slot: $test_slot_msg"; trap - EXIT; exit 1; }; log "$test_slot_msg"

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
sleep 3

log "$(rt host "$stand")"
sleep 1
log "$(rt client "$face")"
sleep 2

# shot <name>: the host's screen, as capture.sh saves it
shot() { log "$1 screenshot: $(timeout 90 bash Tools/Unity/capture.sh "$out/$label-$1.png" screen 2>&1 | tail -1)"; }
# pose <name> <client key> <what the host's animator must read>
pose() {
    log "$(keys "$2")"
    sleep 1.2
    local seen; seen="$(rt host "$anim")"
    log "host sees $1: $seen"
    shot "$1"
    sleep 1
    case "$seen" in *"$3"*) check ok "the host's copy of the client's wizard reads $1 ($3)" ;; *) check no "the host's copy of the client's wizard reads $1 ($3); saw: $seen" ;; esac
    log "$(keys none)"
    sleep 1.5
}
pose idle none "Crouch=False Casting=False Dead=False avatarHuman=True controller=Wizard"
pose crouch C "Crouch=True"
pose cast V "Casting=True"
# Backwards (S), away from the host, into the open floor; read soon, before a wall can stop the walk.
log "host sees before walking: $(rt host "$anim")"
log "$(keys S)"; sleep 0.2
seen="$(rt host "$anim")"; log "host sees walking: $seen"
shot walk
log "$(keys none)"
python -c "import re,sys; m=re.search(r'Speed=([0-9.]+)', sys.argv[1]); sys.exit(0 if m and float(m.group(1)) > 0.5 else 1)" "$seen" \
    && check ok "the host's copy of the client's wizard reads walking (Speed above 0.5)" \
    || check no "the host's copy of the client's wizard reads walking (Speed above 0.5); saw: $seen"

log "client log error lines: $(grep -ci 'exception\|\berror\b' "$out/$label-client.log")"
if [ "$fails" -eq 0 ]; then log "PASS all wizard poses"; else log "FAIL $fails wizard pose check(s)"; fi
[ "$fails" -eq 0 ]
