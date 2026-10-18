#!/usr/bin/env bash
# Checks that loot on the Lair floor keeps its place (#378), in local co-op: the Editor hosts, a Development build
# joins. The host lays two pieces on the pile and moves one 2 m away, turned 90 degrees; after the save poll the host
# quits to the main menu (the pause menu's Quit), hosts again, and the client rejoins. The moved piece must be back
# where it was left, on both sides, and the pile must still hold both pieces. Then the Editor stops and plays again
# (a fresh start of the game) and the host checks once more. Plays in the test slot, wiped at the start (test_slot.sh).
# Actions live in eval/coop_lair.cs. Start-up copied from coop_lair_check.sh.
# Usage: bash Tools/Unity/lair_pile_check.sh <label> [--build|--no-build]
# Needs the Editor open on this project, not in Play mode. Leaves it stopped. Prints PASS or FAIL lines.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/settings_restore.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: lair_pile_check.sh <label> [options]}"; shift
build=auto
while [ $# -gt 0 ]; do
    case "$1" in
        --build) build=yes ;; --no-build) build=no ;;
        *) echo "unknown option $1"; exit 1 ;;
    esac
    shift
done
out="docs/generated/lair-pile-$(date +%F)"; mkdir -p "$out"
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
cli=(--no-banner --format json)
client_pid=""
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
field() { python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get(sys.argv[1]))" "$1"; }
stop_client() {
    if [ -n "$client_pid" ] && kill -0 "$client_pid" 2>/dev/null; then
        "${E[@]}" client quit >/dev/null 2>&1 || true; sleep 2; kill "$client_pid" 2>/dev/null || true
    fi
    client_pid=""
}
cleanup() {
    log "cleaning up"
    stop_client
    unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":false}' --confirm true "${cli[@]}" >/dev/null 2>&1 || true
    settings_restore
    test_slot_restore; log "$test_slot_msg"
}
trap cleanup EXIT

playing="$(bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; exit 1; }
if [ "$playing" != "False False" ]; then log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; fi
settings_save || { log "FAIL cannot save ProjectSettings before building"; trap - EXIT; exit 1; }
test_slot_use || { log "FAIL cannot switch to the test slot: $test_slot_msg"; exit 1; }; log "$test_slot_msg"

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

L() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/coop_lair.cs" timeout 90 bash Tools/Unity/coop_eval.sh "$@" 2>&1; }
wait_for() { # wait_for <side> <text> <seconds>: until that side's "where" line contains the text
    for _ in $(seq 1 "$3"); do
        state="$(L "$1" where)"
        case "$state" in *"$2"*) log "$1: $state"; return 0 ;; esac
        sleep 1
    done
    log "FAIL $1 never reached '$2'; last: ${state:0:300}"; exit 1
}
fails=0
check() { if [ "$1" = ok ]; then log "PASS $2"; else log "FAIL $2"; fails=$((fails + 1)); fi; }
start_client() {
    ./Build/DevTest/Plunderspell.exe -coop-join 127.0.0.1 -screen-fullscreen 0 -screen-width 960 -screen-height 540 \
        -logFile "$(cygpath -w "$repo/$out/$label-client-$1.log")" >/dev/null 2>&1 &
    client_pid=$!
    log "client started (pid $client_pid)"
    wait_for client "state LairRoom local" 60
    sleep 3
}
host_lair() {
    "${E[@]}" host host_udp >/dev/null
    wait_for host "state LairRoom" 20
    sleep 2
}
# same_pose <pose line> <pose line>: positions within 0.15 m and yaw within 10 degrees
same_pose() {
    python -c "
import re, sys
def parse(s):
    m = re.match(r'pose (\S+),(\S+),(\S+) yaw (\S+)', s)
    return [float(x) for x in m.groups()] if m else None
a, b = parse(sys.argv[1]), parse(sys.argv[2])
if not a or not b: sys.exit(1)
d = ((a[0]-b[0])**2 + (a[1]-b[1])**2 + (a[2]-b[2])**2) ** 0.5
yaw = abs((a[3] - b[3] + 180) % 360 - 180)
sys.exit(0 if d <= 0.15 and yaw <= 10 else 1)" "$1" "$2"
}

unity command editor_play "${cli[@]}" >/dev/null
wait_for host "state MainMenu" 60
host_lair
start_client first

log "host: $(L host spawnpile 0)"; log "host: $(L host spawnpile 1)"; sleep 3
p0="$(L host pile)"; log "host: $p0"
n0="${p0#pile }"; n0="${n0%% *}"
[ "$n0" -ge 2 ] 2>/dev/null || { log "FAIL the two pieces were not laid on the pile ($p0)"; exit 1; }

# Move one piece off the pile, as if a player had carried it over and put it down, then wait past the 2 s save poll.
landing="$(L host at /LairRoom/HaulLanding)"; log "host: $landing"
lx="$(python -c "import re,sys; m=re.search(r'at (\S+),(\S+),(\S+)', sys.argv[1]); print(float(m.group(1)) + 2.0)" "$landing")"
lz="$(python -c "import re,sys; m=re.search(r'at (\S+),(\S+),(\S+)', sys.argv[1]); print(float(m.group(3)) + 1.0)" "$landing")"
moved="$(L host movepiece "$lx,$lz,90")"; log "host: $moved"
name="${moved#moved }"; name="${name%% to *}"
sleep 5
before_h="$(L host piecepose "$lx,$lz")"; before_c="$(L client piecepose "$lx,$lz")"; log "host before: $before_h"; log "client before: $before_c"
same_pose "$before_h" "$before_c" && r=ok || r=no
check $r "both sides see the moved piece in the same place before quitting"
L host shot "$(cygpath -m "$repo/$out/$label-host-before.png")" >/dev/null 2>&1 || true

# Back to the menu: the host quits; the client is dropped to its own menu and is closed.
log "host: $(L host quitmenu)"
wait_for host "state MainMenu" 20
stop_client
sleep 2

# Host again; the client rejoins.
host_lair
start_client second
after_h="$(L host piecepose "$lx,$lz")"; after_c="$(L client piecepose "$lx,$lz")"; log "host after: $after_h"; log "client after: $after_c"
same_pose "$before_h" "$after_h" && r=ok || r=no
check $r "after quitting to the menu and hosting again, the host sees the piece where it was left"
same_pose "$before_h" "$after_c" && r=ok || r=no
check $r "the rejoined client sees the piece where it was left"
p1="$(L host pile)"; log "host: $p1"
n1="${p1#pile }"; n1="${n1%% *}"
# The moved piece stands 2 m from the landing, outside the 5 m pile count only if it rolled; count both ways.
near="$(L host findnear "$lx,$lz")"; log "host: $near"
case "$near" in *"$name"*) r=ok ;; *) r=no ;; esac
check $r "the moved piece is still at the spot it was moved to, not back in the grid"
[ "$n1" = "$n0" ] && r=ok || r=no
check $r "the pile still holds every piece ($n0 -> $n1)"
L host shot "$(cygpath -m "$repo/$out/$label-host-after.png")" >/dev/null 2>&1 || true
L client shot "$(cygpath -m "$repo/$out/$label-client-after.png")" >/dev/null 2>&1 || true

# A fresh start of the game: stop Play mode, play again, host, and look once more.
stop_client
unity command editor_stop "${cli[@]}" >/dev/null
for _ in $(seq 1 30); do [ "$(bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying.ToString();')" = "False" ] && break; sleep 1; done
unity command editor_play "${cli[@]}" >/dev/null
wait_for host "state MainMenu" 60
host_lair
again="$(L host piecepose "$lx,$lz")"; log "host after a fresh start: $again"
same_pose "$before_h" "$again" && r=ok || r=no
check $r "after the game is started again, the piece is still where it was left"

log "client log error lines: $(cat "$out/$label"-client-*.log 2>/dev/null | grep -i 'error\|exception' | grep -vc 'SocketError')"
[ "$fails" -eq 0 ] && log "PASS all Lair pile checks" || log "FAIL $fails Lair pile checks"
exit $((fails > 0))
