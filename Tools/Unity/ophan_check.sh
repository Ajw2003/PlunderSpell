#!/usr/bin/env bash
# Co-op check of the Ophan of Hours in the sky (#418): the Editor hosts a raid in Play mode, a Development build
# joins. Automatic hunts are off on the host, so the check brings the Ophan twice:
#   1. The take: the host carries an artifact into the open. The lock-on runs, the beam reaches them, the piece is
#      taken, the host lives and the Ophan leaves.
#   2. The beams: the host carries a plain piece into the open (lock-on, then the slow beam drains), steps behind
#      stone (the beam stops), waits out the charge, and the charged beam goes through the stone, kills, and
#      scorches the room open. The Ophan then turns to the client.
# Guards are removed first, so the only harm is the Ophan's. Every step logs what each machine reads (state shown, target, beaming, health, scorched rooms) and saves a
# screenshot from each machine's own view, slowed down so the tells hold still for it. The client stays behind
# stone as the witness.
# Usage: bash Tools/Unity/ophan_check.sh <label> [--build|--no-build]
#   label  names docs/generated/ophan-418/<label>-*
# Needs the Editor open on this project, not in Play mode. Leaves it stopped. Start-up from fear_wick_check.sh.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/settings_restore.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: ophan_check.sh <label> [--build|--no-build]}"; shift
build=auto
while [ $# -gt 0 ]; do
    case "$1" in --build) build=yes ;; --no-build) build=no ;; *) echo "unknown option $1"; exit 1 ;; esac
    shift
done
out="docs/generated/ophan-418"; mkdir -p "$out"
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
cli=(--no-banner --format json)
client_pid=""
failed=0
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
field() { python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get(sys.argv[1]))" "$1"; }
side() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/ophan_check.cs" timeout 90 bash Tools/Unity/coop_eval.sh "$1" "$2" "${3:-}" 2>&1; }
both() { log "$1 | host:   $(side host read)"; log "$1 | client: $(side client read)"; }
slow() { log "slow $1: host $(side host slow "$1") | client $(side client slow "$1")"; }
shots() {
    bash Tools/Unity/capture.sh "$out/$label-$1-host.png" screen >/dev/null 2>&1 || log "host shot $1 failed"
    log "client shot: $(side client shot "$(cygpath -m "$repo/$out")/$label-$1-client.png")"
}
# until_host <regex> <tries>: poll the host's read (twice a second) until it matches.
until_host() {
    local r=""
    for _ in $(seq 1 "$2"); do
        r="$(side host read)"
        if grep -qE "$1" <<<"$r"; then return 0; fi
        sleep 0.5
    done
    log "FAIL the host never read '$1'; last: ${r:0:400}"; failed=1; return 1
}
check() { if grep -qE "$2" <<<"$3"; then log "PASS $1"; else log "FAIL $1: ${3:0:400}"; failed=1; fi; }
cleanup() {
    log "cleaning up"
    side host huntauto >/dev/null 2>&1 || true
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
log "hunts: $(side host huntoff)"
timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
wait_for host "state Playing" 30
wait_for client "state Playing" 30
sleep 5

# need <what> <regex> <text>: as check, but a failure stops the run (every later step would only cascade).
need() { check "$@"; [ "$failed" = 0 ] || exit 1; }
# take_up <action>: the host grabs (grabartifact or grabany) until its read shows it holding something, up to 3 tries.
take_up() {
    for _ in 1 2 3; do
        log "host: $(side host "$1")"
        sleep 1
        if grep -q 'holding [^n]' <<<"$(side host read)"; then return 0; fi
    done
    log "FAIL the host could not take up a piece ($1)"; exit 1
}
# hunted_safely: the client hides until the host carries the most worth and the hunt has settled on it, then
# steps into the open as the witness who sees it all from outside.
hunted_safely() {
    until_host 'shown Watching' 30 || exit 1
    sleep 2
    log "client: $(side client place open)"
    sleep 2
    need "the client stands in the open unharmed (it is not the target)" 'local=True health=100 dead=False' "$(tr '|' '\n' <<<"$(side client read)")"
}

# ---- 1. Arrival and the take -------------------------------------------------------------------------------
# The look only exists once it has arrived, and placing aims at its eye. While it unfolds (4 s) both hide and the
# host takes up the artifact, so it is the one hunted (the most worth carried).
log "guards: $(side host noguards)"
log "arrive: $(side host arrive)"
until_host 'shown Arriving' 10 || exit 1
log "host: $(side host place cover)"
log "client: $(side client place cover)"
take_up grabartifact
need "the host holds an artifact" 'artifact=True' "$(side host read)"
hunted_safely
log "client: $(side client face)"
sleep 1
both "watching"
shots watching

slow 0.25   # first: the lock-on is 3 s, and the calls below take seconds each
log "host: $(side host place open)"
log "the carrier steps out | host: $(side host read)"
until_host 'shown Locking' 20
sleep 2
shots lock-artifact
both "locking on to the artifact carrier"
until_host 'shown Leaving' 60 || exit 1
# Still at a quarter speed: about 1.5 s into its 4 s leave the piece is on its way up the beam.
sleep 6
slow 0.05
log "host: $(side host face) | client: $(side client face)"
shots take
log "host lift shot: $(side host liftshot "$(cygpath -m "$repo/$out")/$label-lift-host.png")"
log "client lift shot: $(side client liftshot "$(cygpath -m "$repo/$out")/$label-lift-client.png")"
taken="$(side host read)"
# Section 2's set-up, while it is still leaving at a twentieth speed (placing aims at its eye): both hide and the
# host takes up a plain piece, so on the next arrival the host is hunted from its first look.
log "host: $(side host place cover)"
log "client: $(side client place cover)"
take_up grabany
log "after the take | host: $taken"
log "after the take | client: $(side client read)"
check "the take: the piece is gone" 'holding nothing' "$taken"
check "the take: the host shows the piece rising" 'lifted ' "$taken"
check "the take: the client shows the piece rising" 'lifted ' "$(side client read)"
check "the take: the carrier is not hurt" 'local=True health=100 dead=False' "$(tr '|' '\n' <<<"$taken")"

# ---- 2. The slow beam, cover, the charge and the scorch ----------------------------------------------------
slow 1
until_host 'shown none' 30 || exit 1
log "guards: $(side host noguards)"
log "arrive again: $(side host arrive)"
hunted_safely
# A quarter speed from before the lock-on until the host is back in cover: at full speed the drain (25 a second)
# kills before the screenshots and reads are done.
slow 0.25
log "host: $(side host place open)"
until_host 'shown Locking' 20
sleep 2
shots lock
both "locking on"
until_host 'beaming True' 60
# A twentieth speed while the shots and reads are taken, so the drain leaves the host alive to reach cover.
slow 0.05
shots beam
log "slow beam | host: $(side host read)"
check "the slow beam: the client shows it beaming" 'beaming True' "$(side client read)"
log "host: $(side host place cover)"
slow 0.25
sleep 1
first="$(side host read)"
sleep 2
second="$(side host read)"
log "in cover | host: $first"
log "in cover two seconds later | host: $second"
check "cover: the beam stops" 'beaming False' "$second"
health() { tr '|' '\n' <<<"$1" | grep 'local=True' | sed -E 's/.*health=(-?[0-9]+).*/\1/'; }
if [ "$(health "$first")" = "$(health "$second")" ] && [ "$(health "$second")" -gt 0 ]; then log "PASS cover: health holds at $(health "$second")"
else log "FAIL cover: health went from $(health "$first") to $(health "$second")"; failed=1; fi
shots cover
# Still at a quarter speed: 3 game seconds in cover start the charge, 3 more fire it (24 s here).
until_host 'shown Charging' 60 || exit 1
sleep 4
shots charge
both "charging"
until_host 'scorched [1-9]' 60
slow 0.05
shots charged
fired="$(side host read)"
log "after the charged beam | host: $fired"
client_seen="$(side client read)"
log "after the charged beam | client: $client_seen"
check "the charged beam kills through stone" 'local=True health=[^ ]+ dead=True' "$(tr '|' '\n' <<<"$fired")"
check "the client sees the room scorched" 'scorched [1-9]' "$client_seen"
log "host room shot: $(side host roomshot "$(cygpath -m "$repo/$out")/$label-scorched-host.png")"
log "client room shot: $(side client roomshot "$(cygpath -m "$repo/$out")/$label-scorched-client.png")"
# After the kill it turns to the living: the client, in the open, gets the full lock-on first, not the beam at once.
slow 0.25
until_host 'shown (Locking|Draining)' 60
after="$(side host read)"
log "after the kill | host: $after"
check "after the kill it locks on to the next player first" 'shown Locking' "$after"
check "the next player is unhurt by the lock-on" 'local=False health=100 dead=False' "$(tr '|' '\n' <<<"$after")"
log "client: $(side client face)"
shots hunts-on
both "after the kill, it hunts on"
slow 1

log "client log error lines: $(grep -ciE 'error|exception' "$out/$label-client.log")"
[ "$failed" = 0 ] && log "DONE all checks passed" || log "DONE with failures"
exit "$failed"
