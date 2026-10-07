#!/usr/bin/env bash
# Checks the Lair room and the Market in local co-op (#314): the Editor hosts in Play mode, a Development build
# joins. Each player must arrive at their own Lair spawn; the client goes through the Market door and back, and
# the host must see its body move; after a raid both come home to their spawns and both see the same haul pile, grown by the two pieces extracted. It plays in the test slot (SaveSlots.TestSlot), wiped at the start, so every run begins from an empty pile and the owner's slots are untouched (test_slot.sh).
# Actions live in eval/coop_lair.cs. Start-up copied from coop_door_check.sh.
# Usage: bash Tools/Unity/coop_lair_check.sh <label> [--build|--no-build]
# Needs the Editor open on this project, not in Play mode. Leaves it stopped. Prints PASS or FAIL lines.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/settings_restore.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: coop_lair_check.sh <label> [options]}"; shift
build=auto
while [ $# -gt 0 ]; do
    case "$1" in
        --build) build=yes ;; --no-build) build=no ;;
        *) echo "unknown option $1"; exit 1 ;;
    esac
    shift
done
out="docs/generated/coop-lair-$(date +%F)"; mkdir -p "$out"
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
cli=(--no-banner --format json)
client_pid=""
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
field() { python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get(sys.argv[1]))" "$1"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
cleanup() {
    log "cleaning up"
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
near() { # near <side> <scene path> <metres> <what>: the side's local player stands within that distance of the object
    local r d; r="$(L "$1" at "$2")"; log "$1: $r"
    d="${r#distance }"; d="${d%% *}"
    python -c "import sys; sys.exit(0 if float(sys.argv[1]) <= float(sys.argv[2]) else 1)" "$d" "$3" 2>/dev/null && check ok "$4" || check no "$4"
}

unity command editor_play "${cli[@]}" >/dev/null
wait_for host "state MainMenu" 60
"${E[@]}" host host_udp >/dev/null
wait_for host "state LairRoom" 20
./Build/DevTest/Plunderspell.exe -coop-join 127.0.0.1 -screen-fullscreen 0 -screen-width 960 -screen-height 540 \
    -logFile "$(cygpath -w "$repo/$out/$label-client.log")" >/dev/null 2>&1 &
client_pid=$!
log "client started (pid $client_pid)"
wait_for client "state LairRoom local" 60
sleep 3

# Arriving: each at their own spawn (the client is owner 2).
near host /LairRoom/PlayerSpawns/Spawn1 0.6 "the host stands at Lair spawn 1"
near client /LairRoom/PlayerSpawns/Spawn2 0.6 "the client stands at Lair spawn 2"

# The Market door and back, client side; the host must see the body go and come back.
log "client: $(L client travel /LairRoom/MarketDoor)"; sleep 2
near client /MarketYard/PlayerSpawns/Spawn2 0.6 "the client's Market door leads to Market spawn 2"
h="$(L host bodies)"; log "host: $h"
case "$h" in *"1100."*|*"1101."*) check ok "the host sees the client in the Market" ;; *) check no "the host sees the client in the Market" ;; esac
log "client: $(L client travel /MarketYard/LairExit)"; sleep 2
near client /LairRoom/MarketDoorArrivals/Spawn2 0.6 "the client's way out leads back inside the Lair door"

# A raid: both set out, two pieces come home, both see the same pile, grown by exactly those two.
# The pile is saved across runs, so what the host sees before setting out is the starting count.
before="$(L host pile)"; log "host before: $before"
b="${before#pile }"; b="${b%% *}"
seed=777
log "castle: $(ev "var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>(); d.SetFixedSeed($seed); return \"seed $seed\";")"
timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
wait_for host "state Playing" 30
wait_for client "state Playing" 30
sleep 3
log "host: $(L host pad 2)"; sleep 2
log "host: $(ev 'UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>().CallExtraction(); return "extracting";')"
wait_for host "state LairRoom" 30
wait_for client "state LairRoom" 30
sleep 3
near client /LairRoom/PlayerSpawns/Spawn2 0.6 "the client comes home to Lair spawn 2"
h="$(L host pile)"; c="$(L client pile)"; log "host: $h"; log "client: $c"
n="${h#pile }"; n="${n%% *}"
[ "$h" = "$c" ] && [ "$n" = "$((b + 2))" ] && r=ok || r=no; check $r "both see the same pile, grown by the two extracted pieces ($b -> $n)"
L client shot "$(cygpath -m "$repo/$out/$label-client-pile.png")" >/dev/null 2>&1 || true

# Selling (#314): the host sets a piece on the Goldsmith's counter (server side); the CLIENT answers Plus then Satis
# through the counter's word path; both sides must show the same line, the host's gold and debt must move by the sold
# coins, and the piece must be gone on both sides.
log "client: $(L client travel /LairRoom/MarketDoor)"; sleep 2
log "client: $(L client look)"
log "host: $(L host put 4)"
for _ in $(seq 1 15); do hl="$(L host line)"; case "$hl" in *coin*) break ;; esac; sleep 1; done
sleep 1; hl="$(L host line)"; cl="$(L client line)"; log "host: $hl"; log "client: $cl"
r=no; [ "$hl" = "$cl" ] && case "$hl" in *coin*) r=ok ;; esac
check $r "both see the vendor's opening offer"
m0="$(L host money)"; log "host before: $m0"
log "client: $(L client speak Plus)"; sleep 2
hl="$(L host line)"; cl="$(L client line)"; log "host: $hl"; log "client: $cl"
r=no; [ "$hl" = "$cl" ] && case "$hl" in *"Very well"*|*"Too much"*|*"Enough"*) r=ok ;; esac
check $r "the client's Plus is answered, and both see the same line"
L client shot "$(cygpath -m "$repo/$out/$label-client-counter.png")" >/dev/null 2>&1 || true
log "client: $(L client speak Satis)"; sleep 2
hl="$(L host line)"; cl="$(L client line)"; log "host: $hl"; log "client: $cl"
r=no; [ "$hl" = "$cl" ] && case "$hl" in *Done*) r=ok ;; esac
check $r "the client's Satis sells it, and both see the same line"
m1="$(L host money)"; log "host after: $m1"
coins="${hl##*: }"; coins="${coins%% coin*}"
python -c "
import re, sys
a = [float(x) for x in re.findall(r'[0-9.]+', sys.argv[1])]; b = [float(x) for x in re.findall(r'[0-9.]+', sys.argv[2])]
sys.exit(0 if abs((a[1] - b[1]) + (b[0] - a[0]) - float(sys.argv[3])) < 0.01 else 1)" "$m0" "$m1" "$coins" 2>/dev/null && r=ok || r=no
check $r "the host's gold and debt moved by the $coins coins sold"
sleep 1
hp="$(L host piece)"; cp="$(L client piece)"; log "host: $hp"; log "client: $cp"
r=no; [ "$hp" = "pieces 0" ] && [ "$cp" = "pieces 0" ] && r=ok
check $r "the piece is gone on both sides"

# SocketError is a type name in LiteNetLib's stack frames, not an error.
log "client log error lines: $(grep -i 'error\|exception' "$out/$label-client.log" | grep -vc 'SocketError')"
[ "$fails" -eq 0 ] && log "PASS all Lair checks" || log "FAIL $fails Lair checks"
exit $((fails > 0))
