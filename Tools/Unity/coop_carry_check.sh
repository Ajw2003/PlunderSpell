#!/usr/bin/env bash
# Two-player carry check for #169, on one PC with nobody at the keyboard: the Editor hosts in Play
# mode, a Development build in Build/DevTest joins it on 127.0.0.1, and both are driven through
# Tools/Unity/coop_eval.sh. Prints one PASS or FAIL line per scenario, and saves under
# docs/generated/coop-carry-<date>/: the client's log; trace/<scenario>-<side>.csv, the piece's
# state on every physics step on each side; and, for each failed scenario, both sides'
# screenshots, a recording of the primary monitor (video/<scenario>.mkv) and 8 frames of it in
# one image (<scenario>-frames.png).
#
# Usage: bash Tools/Unity/coop_carry_check.sh [--build | --no-build] [--shots] [--no-video]
#   (default)    build Build/DevTest only if a file under Assets/ is newer than it (about a minute)
#   --build      always build;  --no-build  never build
#   --shots      screenshot every scenario, not just failed ones (each costs a few seconds)
#   --no-video   do not record the screen (it needs ffmpeg on PATH)
#   --raid-end   instead of the carry scenarios: end the raid from the host and check the client
#                took it cleanly (no SyncVar permission errors), about a minute
#
# Needs the Editor open on this project, not in Play mode. Leaves it stopped, with the Pipeline
# runtime setting off and ProjectSettings.asset's preloadedAssets line as it was
# (docs/4-systems/net.md, "Testing it").
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/settings_restore.sh"

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/coop-carry-$(date +%F)"
mkdir -p "$out"
# Every command has a time limit, so a step that never answers fails instead of hanging the run.
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
cli=(--no-banner --format json)
client_pid=""
failures=0
rec_pid=""
rec_area=""

log() { printf '%s %s\n' "$(date +%T)" "$*"; }

field() { # field <json envelope on stdin> <key>: one key of a Pipeline command's result
    python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get(sys.argv[1]))" "$1"
}

cleanup() {
    log "cleaning up"
    rec_stop discard PASS
    [ -n "$rec_area" ] && powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/place_windows.ps1 release >/dev/null
    if [ -n "$client_pid" ] && kill -0 "$client_pid" 2>/dev/null; then
        "${E[@]}" client quit >/dev/null 2>&1 || true
        sleep 2
        kill "$client_pid" 2>/dev/null || true
    fi
    unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":false}' --confirm true "${cli[@]}" >/dev/null 2>&1 || true
    # The Pipeline build rewrites ProjectSettings; put back the byte-exact copy saved at the start.
    settings_restore
}
trap cleanup EXIT

# ---------------------------------------------------------------------------------------------
# Preconditions: the Editor answers and nobody is playtesting in it.
# ---------------------------------------------------------------------------------------------
playing="$(bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" \
    || { log "FAIL the Editor did not answer: $playing"; exit 1; }
if [ "$playing" != "False False" ]; then
    log "FAIL the Editor is playing or compiling ($playing); stop Play mode and run again"
    trap - EXIT
    exit 1
fi

settings_save || { log "FAIL cannot save ProjectSettings before building"; trap - EXIT; exit 1; }

# ---------------------------------------------------------------------------------------------
# The client build.
# ---------------------------------------------------------------------------------------------
build=auto; shots=failed; video=on; mode=carry
for arg in "$@"; do
    case "$arg" in
        --build) build=yes ;;
        --no-build) build=no ;;
        --shots) shots=all ;;
        --no-video) video=off ;;
        --raid-end) mode=raid_end ;;
        *) log "FAIL unknown option $arg"; trap - EXIT; exit 1 ;;
    esac
done
if [ "$build" = auto ]; then
    build=no
    [ -f Build/DevTest/.built ] || build=yes
    # Anything the client build is made from, changed since it was built. Tests are not in it. Not
    # compared with Plunderspell.exe: the build copies that file with its old date.
    if [ "$build" = no ] && [ -n "$(find Assets ProjectSettings -newer Build/DevTest/.built -type f \
            ! -path '*/Tests/*' ! -path 'ProjectSettings/Packages/*' ! -name '*.meta' ! -name 'ProjectSettings.asset' -print -quit)" ]; then
        build=yes
    fi
    log "build: $build (auto)"
fi
if [ "$build" = yes ]; then
    log "building Build/DevTest (Development, Pipeline runtime on for this build only)"
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":true}' --confirm true "${cli[@]}" >/dev/null
    unity command build --target StandaloneWindows64 --outputPath Build/DevTest/Plunderspell.exe \
        --options '["Development"]' --confirm true "${cli[@]}" >/dev/null
    status=""
    for _ in $(seq 1 120); do
        sleep 5
        status="$(unity command build_status "${cli[@]}" | field status)"
        [ "$status" = "completed" ] && break
    done
    result="$(unity command build_status "${cli[@]}" | field result)"
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":false}' --confirm true "${cli[@]}" >/dev/null
    [ "$result" = "Succeeded" ] || { log "FAIL build: status $status, result $result"; exit 1; }
    touch Build/DevTest/.built
    log "build $result"
fi
[ -f Build/DevTest/Plunderspell.exe ] || { log "FAIL no Build/DevTest/Plunderspell.exe; run without --no-build"; exit 1; }

# ---------------------------------------------------------------------------------------------
# The session: host in the Editor, client in a window, both set out on a raid.
# ---------------------------------------------------------------------------------------------
wait_for() { # wait_for <side> <text> <seconds>: until that side's state line contains the text
    for _ in $(seq 1 "$3"); do
        state="$("${E[@]}" "$1" state 2>&1)"
        case "$state" in *"$2"*) log "$1: $state"; return 0 ;; esac
        sleep 1
    done
    log "FAIL $1 never reached '$2'; last: ${state:0:300}"
    exit 1
}

unity command editor_play "${cli[@]}" >/dev/null
wait_for host "state MainMenu" 60
"${E[@]}" host host_udp >/dev/null
wait_for host "state Lair" 20

./Build/DevTest/Plunderspell.exe -coop-join 127.0.0.1 -screen-fullscreen 0 -screen-width 960 -screen-height 540 \
    -logFile "$(cygpath -w "$repo/$out/client.log")" >/dev/null 2>&1 &
client_pid=$!
log "client started (pid $client_pid)"
wait_for client "players 2" 60

# One castle every run: a random seed moves the spawn (onto the extraction pad, for some seeds,
# which ends the raid within seconds) and changes which pieces there are to stage.
seed=3508293
pinned="$(timeout 60 bash Tools/Unity/eval.sh "var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>(); d.SetFixedSeed($seed); var lair = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Lair.LairHubManager>(); if (lair != null) lair.SelectEra(Plunderspell.Inventory.HistoricalEra.LateMedieval); return \"seed $seed, LateMedieval\";")"
log "castle: $pinned"
timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
wait_for host "state Playing" 30
wait_for client "state Playing" 30
sleep 3
# No guards: with the fixed castle they spawn near the players, and a few minutes in they end the
# raid, which reads as every later scenario failing.
guards="$(timeout 60 bash Tools/Unity/eval.sh 'var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>(); var f = typeof(Plunderspell.Raid.RaidDirector).GetField("_guardSpawner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); var spawner = f.GetValue(d); spawner.GetType().GetMethod("Clear").Invoke(spawner, null); return "guards cleared";')"
log "$guards"
if [ "$video" = on ]; then
    if ! command -v ffmpeg >/dev/null; then
        log "no ffmpeg on PATH: recording off"; video=off
    else
        # Client beside the Editor, so the recording shows both games.
        rec_area="$(powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Unity/place_windows.ps1 | tr -d '')"
        [ "$rec_area" = none ] && { log "client or Editor window not found: recording off"; video=off; }
    fi
fi
mkdir -p "$out/trace" "$out/video" "$out/frames"
# This run's evidence only: an earlier run's failure video would read as this run's (git keeps it).
rm -f "${out:?}"/video/*.mkv "${out:?}"/frames/*.png

printf '\n# run %s (%s)\n' "$(date +%T)" "$mode" >> "$out/results.txt"

# Every run: the client builds the castle once, in the host's era. The seed and era used to arrive
# separately, so the client built the last raid's era first and then built again.
builds="$(grep -c 'Building the castle from seed' "$out/client.log")"
if [ "$builds" = 1 ]; then
    verdict=PASS
else
    verdict="FAIL client built the castle $builds times: $(grep 'Building the castle' "$out/client.log" | tr '\n' ';')"
fi
printf '%s %s\n' "$verdict" castle_built_once | tee -a "$out/results.txt"
case "$verdict" in PASS*) ;; *) failures=$((failures + 1)) ;; esac

if [ "$mode" = raid_end ]; then
    # The host ends the raid as the clock running out would; the client must follow it to the Lair
    # without writing the server's SyncVars ("Invalid permissions when setting ...").
    timeout 60 bash Tools/Unity/eval.sh 'UnityEngine.Object.FindFirstObjectByType<Plunderspell.Extraction.ExtractionZone>().ResolveExtraction(); return "resolved";' >/dev/null
    for _ in $(seq 1 20); do grep -q "Host moved the raid to Resolved" "$out/client.log" && break; sleep 1; done
    sleep 2
    if ! grep -q "Host moved the raid to Resolved" "$out/client.log"; then
        verdict="FAIL the client never heard the raid end"
    elif grep -q "Invalid permissions" "$out/client.log"; then
        verdict="FAIL $(grep -m1 'Invalid permissions' "$out/client.log")"
    else
        verdict=PASS
    fi
    printf '%s %s\n' "$verdict" raid_ends_cleanly | tee -a "$out/results.txt"
    case "$verdict" in PASS*) ;; *) failures=$((failures + 1)) ;; esac
    log "$failures scenario(s) failed; results in $out/results.txt"
    exit $((failures > 0))
fi

# ---------------------------------------------------------------------------------------------
# Scenarios.
# ---------------------------------------------------------------------------------------------
piece=""

stage() { # stage <light|heavy>:<n>: fresh piece in front of the host, client beside the host, both aim
    "${E[@]}" host release >/dev/null
    "${E[@]}" client release >/dev/null
    sleep 1
    [ -n "$piece" ] && "${E[@]}" host park "$piece" >/dev/null
    local staged
    staged="$("${E[@]}" host stage "$1")"
    stage_failed=""
    case "$staged" in
        "no spawned"*) stage_failed="$staged"; piece=""; log "FAIL $staged"; return ;;
    esac
    piece="${staged%% *}"
    local host_at right
    host_at="$(printf '%s' "$staged" | sed -n 's/.* host \([^ ]*\) right \([^ ]*\)$/\1/p')"
    right="$(printf '%s' "$staged" | sed -n 's/.* right \([^ ]*\)$/\1/p')"
    local beside
    beside="$(python -c "import sys; h=[float(v) for v in sys.argv[1].split(',')]; r=[float(v) for v in sys.argv[2].split(',')]; print(','.join(f'{h[i]+1.2*r[i]:.3f}' for i in range(3)))" "$host_at" "$right")"
    "${E[@]}" client beside "$beside" >/dev/null
    sleep 1.5
    "${E[@]}" host aim "$piece" >/dev/null
    "${E[@]}" client aim "$piece" >/dev/null
    log "staged: $staged"
    begin
}

# ---------------------------------------------------------------------------------------------
# Evidence for each scenario: from begin to finish, both sides trace the piece every physics step
# and ffmpeg records the primary monitor. finish keeps the traces always, and the recording only
# for a failure; then it begins again, for scenarios that carry on with the same piece.
# ---------------------------------------------------------------------------------------------
rec_start() {
    rec_stop discard PASS
    [ "$video" = on ] || return 0
    local x y w h
    read -r x y w h <<< "$rec_area"
    # ffmpeg stops cleanly on a "q" down its stdin (fd 7); a killed ffmpeg leaves an unreadable file.
    exec 7> >(exec ffmpeg -loglevel error -y -f gdigrab -framerate 10 -offset_x "$x" -offset_y "$y"         -video_size "${w}x${h}" -i desktop -c:v libx264 -preset ultrafast -crf 30 "$out/video/.current.mkv"         2>> "$out/video/ffmpeg.log")
    rec_pid=$!
}

rec_stop() { # rec_stop <name> <verdict>: stop recording; frames for every scenario, the video for a failure
    [ -n "$rec_pid" ] || return 0
    printf q >&7 2>/dev/null; exec 7>&-
    wait "$rec_pid" 2>/dev/null
    rec_pid=""
    local video="$out/video/.current.mkv"
    [ "$1" = discard ] && { rm -f "$video"; return 0; }
    # 8 frames across the scenario, cropped to the two game windows along the top of the monitor.
    # Numbers alone miss things a picture shows at once, such as a piece jammed against a wall.
    local seconds
    seconds="$(ffprobe -v error -show_entries format=duration -of csv=p=0 "$video")"
    ffmpeg -loglevel error -y -i "$video" -vf "fps=8/$seconds,crop=iw:min(ih\,640):0:0,scale=960:-2,tile=2x4" -frames:v 1 "$out/frames/$1.png" || log "$1: could not take frames"
    case "$2" in
        PASS*) rm -f "$video" ;;
        *) mv -f "$video" "$out/video/$1.mkv"; log "$1: recording in $out/video/$1.mkv" ;;
    esac
}

begin() {
    [ -n "$piece" ] || return 0
    "${E[@]}" host trace_start "$piece" >/dev/null 2>&1 &
    local host_tracer=$!
    "${E[@]}" client trace_start "$piece" >/dev/null 2>&1 &
    wait "$host_tracer" $!
    rec_start
}

finish() { # finish <name> <verdict> <both|host>: save the traces, the recording on a failure, screenshots
    "${E[@]}" host trace_stop "$(cygpath -m "$repo/$out/trace/$1-host.csv")" >/dev/null 2>&1 &
    local host_saver=$!
    if [ "$3" = both ]; then
        "${E[@]}" client trace_stop "$(cygpath -m "$repo/$out/trace/$1-client.csv")" >/dev/null 2>&1 &
        wait "$host_saver" $!
    else
        wait "$host_saver"
    fi
    rec_stop "$1" "$2"
    shoot "$1" "$2" "$3"
    begin
}

# check <name> <rule>: reads the piece on both sides and applies one rule. Every rule also needs
# both sides to agree on where the piece is, within 0.1 m.
#   holder=<host|client|both|none>  who must report holding it
#   near=<host|client>              that side's held point must be within 0.35 m of its aim point
#   between                         with two aim points, the piece must not sit on either one: both
#                                   pulls count (within 75% of the separation from each)
check() {
    local name="$1"; shift
    local host_read client_read verdict
    # Both sides at once: a piece still swinging would otherwise look like a disagreement.
    "${E[@]}" host read "$piece" > "$out/.host_read" 2>&1 &
    local host_reader=$!
    "${E[@]}" client read "$piece" > "$out/.client_read" 2>&1 &
    local client_reader=$!
    # Only these two: a bare wait would also wait for the client game itself.
    wait "$host_reader" "$client_reader"
    host_read="$(cat "$out/.host_read")"
    client_read="$(cat "$out/.client_read")"
    rm -f "$out/.host_read" "$out/.client_read"
    verdict="$(python - "$host_read" "$client_read" "$@" <<'PY'
import math, re, sys
host, client, rules = sys.argv[1], sys.argv[2], sys.argv[3:]

def parse(line):
    m = re.match(r"pos ([^ ]+) held (\w+) aim ([^ ]+) grip ([^ ]+) load ([^ ]+) heavy (\w+)", line)
    if not m:
        return None
    vec = lambda s: None if s == "none" else [float(v) for v in s.split(",")]
    return {"pos": vec(m.group(1)), "held": m.group(2) == "True", "aim": vec(m.group(3)), "grip": vec(m.group(4)),
            "load": float(m.group(5)), "heavy": m.group(6) == "True"}

h, c = parse(host), parse(client)
if h is None or c is None:
    print(f"FAIL unreadable: host '{host[:200]}' client '{client[:200]}'"); sys.exit()
problems = []
gap = math.dist(h["pos"], c["pos"])
if gap > 0.1:
    problems.append(f"sides disagree by {gap:.3f} m")
for rule in rules:
    key, _, value = rule.partition("=")
    if key == "holder":
        want = {"host": (True, False), "client": (False, True), "both": (True, True), "none": (False, False)}[value]
        if (h["held"], c["held"]) != want:
            problems.append(f"held host={h['held']} client={c['held']}, wanted {value}")
    elif key == "near":
        side = h if value == "host" else c
        if side["aim"] is None:
            problems.append(f"{value} has no aim point")
        elif math.dist(side["aim"], side["grip"]) > 0.35:
            problems.append(f"held point {math.dist(side['aim'], side['grip']):.2f} m from {value}'s aim")
    elif key == "between":
        if h["aim"] is None or c["aim"] is None:
            problems.append("needs both aim points")
        else:
            apart = math.dist(h["aim"], c["aim"])
            # Each holder's own held point against their own aim: if one pull is ignored, that
            # holder is off by the whole separation and the other by nothing.
            dh, dc = math.dist(h["grip"], h["aim"]), math.dist(c["grip"], c["aim"])
            if max(dh, dc) > 0.75 * apart:
                problems.append(f"aims {apart:.2f} m apart, host's held point {dh:.2f} m off, client's {dc:.2f} m off: one pull is ignored")
    elif key == "light":
        # Whether each side reports the piece too heavy to lift: proves TotalGrip replicated to a
        # client holder, not just the host that controls the body.
        want = {"host": (False, True), "client": (True, False), "both": (False, False)}[value]
        if (h["heavy"], c["heavy"]) != want:
            problems.append(f"heavy host={h['heavy']} client={c['heavy']}, wanted light={value}")
    elif key == "rose":
        before_y, min_rise = value.split(":")
        rise = h["pos"][1] - float(before_y)
        # A piece that started high (resting on furniture, or held up in the portal) cannot rise the
        # full amount: reaching the host's aim height counts as lifted too.
        reached = h["aim"] is not None and abs(h["pos"][1] - h["aim"][1]) < 0.3
        if rise < float(min_rise) and not reached:
            problems.append(f"piece rose {rise:.2f} m (wanted at least {min_rise} m, or to within 0.3 m of the aim height)")
print("PASS" if not problems else "FAIL " + "; ".join(problems))
PY
)"
    log "$name: host [$host_read] client [$client_read]"
    printf '%s %s\n' "$verdict" "$name" | tee -a "$out/results.txt"
    case "$verdict" in PASS*) ;; *) failures=$((failures + 1)) ;; esac
    finish "$name" "$verdict" both
}

shoot() { # shoot <name> <verdict> <both|host>: screenshots, for a failure or when --shots asked
    case "$2" in PASS*) [ "$shots" = all ] || return 0 ;; esac
    [ "$3" = both ] && "${E[@]}" client shot "$(cygpath -m "$repo/$out/$1-client.png")" >/dev/null 2>&1
    timeout 60 bash Tools/Unity/capture.sh "$out/$1-host.png" >/dev/null 2>&1
}


stage light:0
"${E[@]}" host grab "$piece" >/dev/null; "${E[@]}" host level >/dev/null; sleep 2
check host_grabs holder=host near=host

stage light:1
"${E[@]}" client grab "$piece" >/dev/null; "${E[@]}" client level >/dev/null; sleep 2
check client_grabs holder=client near=client

stage light:5
"${E[@]}" client grab "$piece" >/dev/null; "${E[@]}" client level >/dev/null; sleep 2
# Towards open space, so the throw is not stopped by a wall the piece was held against.
"${E[@]}" client face_open >/dev/null; sleep 1.5
before="$("${E[@]}" host read "$piece" 2>&1)"
"${E[@]}" client throw >/dev/null
sleep 0.3
after_host="$("${E[@]}" host read "$piece" 2>&1)"
after_client="$("${E[@]}" client read "$piece" 2>&1)"
verdict="$(python -c "
import math, re, sys
def parse(line):
    m = re.match(r'pos ([^ ]+) held (\w+)', line)
    if not m: return None
    return {'pos': [float(v) for v in m.group(1).split(',')], 'held': m.group(2) == 'True'}
b, ah, ac = parse(sys.argv[1]), parse(sys.argv[2]), parse(sys.argv[3])
if b is None or ah is None or ac is None:
    print(f'FAIL unreadable: before {sys.argv[1][:200]!r} after_host {sys.argv[2][:200]!r} after_client {sys.argv[3][:200]!r}'); sys.exit()
dist = math.hypot(ah['pos'][0] - b['pos'][0], ah['pos'][2] - b['pos'][2])
if ac['held']:
    print('FAIL client still holds it after throw')
elif dist < 1.0:
    print(f'FAIL only moved {dist:.2f} m horizontally')
else:
    print('PASS')
" "$before" "$after_host" "$after_client")"
log "client_throws: before [$before] after_host [$after_host] after_client [$after_client]"
printf '%s %s\n' "$verdict" client_throws | tee -a "$out/results.txt"
finish client_throws "$verdict" both
case "$verdict" in PASS*) ;; *) failures=$((failures + 1)) ;; esac

# Regression coverage for a client holding a piece low and close to their own body (#169 Fix 1):
# it did not fail before the collision fix (a target this close, at pitch 45 / depth 0.7, did not
# turn out to meet the client's capsule in this scene), but it stays as a guard against that
# regressing.
stage light:6
"${E[@]}" client grab "$piece" >/dev/null; "${E[@]}" client level >/dev/null
"${E[@]}" client pitch 45 >/dev/null
"${E[@]}" client depth 0.7 >/dev/null
sleep 2
check client_holds_close holder=client near=client

# Two-holder strength adds together (#169 step 4): one holder cannot lift a 10-16 kg piece (it
# tows instead), but two combine their grip and lift it clear of the floor.
stage heavy:0
if [ -n "$stage_failed" ]; then
    printf 'FAIL %s heavy_alone_tows\n' "$stage_failed" | tee -a "$out/results.txt"
    failures=$((failures + 1))
else
    "${E[@]}" host grab "$piece" >/dev/null; "${E[@]}" host level >/dev/null; sleep 2
    host_read="$("${E[@]}" host read "$piece" 2>&1)"
    verdict="$(python -c "
import re, sys
m = re.match(r'pos ([^ ]+) held (\w+) aim ([^ ]+) grip ([^ ]+) load ([^ ]+) heavy (\w+)', sys.argv[1])
if not m:
    print('FAIL unreadable: ' + sys.argv[1][:200]); sys.exit()
print('PASS' if m.group(6) == 'True' else 'FAIL heavy is False for a 10-16 kg piece towed by one holder')
" "$host_read")"
    log "heavy_alone_tows: host [$host_read]"
    printf '%s %s\n' "$verdict" heavy_alone_tows | tee -a "$out/results.txt"
    finish heavy_alone_tows "$verdict" both
    case "$verdict" in PASS*) ;; *) failures=$((failures + 1)) ;; esac

    before_y="$(python -c "
import re, sys
m = re.match(r'pos [^,]+,([^,]+),', sys.argv[1])
print(m.group(1) if m else '0')
" "$host_read")"

    "${E[@]}" client grab "$piece" >/dev/null; "${E[@]}" client level >/dev/null; sleep 3
    check heavy_lifted_together holder=both "light=both" "rose=${before_y}:0.6"
fi

# Opposite pulls (#169 step 4): pulling the same piece apart drops it for both holders instead of
# stretching the beam forever.
stage light:7
"${E[@]}" host grab "$piece" >/dev/null; "${E[@]}" host level >/dev/null
"${E[@]}" client grab "$piece" >/dev/null; "${E[@]}" client level >/dev/null
sleep 1
"${E[@]}" host turn 80 >/dev/null
"${E[@]}" client turn -80 >/dev/null
sleep 2
check opposite_pulls_snap holder=none

stage light:2
"${E[@]}" host grab "$piece" >/dev/null; "${E[@]}" host level >/dev/null; sleep 1
"${E[@]}" client grab "$piece" >/dev/null; "${E[@]}" client level >/dev/null; sleep 1
"${E[@]}" host turn 25 >/dev/null; sleep 2
check both_grab_same_piece holder=both between

"${E[@]}" client release >/dev/null; sleep 2
check client_lets_go holder=host near=host

"${E[@]}" client grab "$piece" >/dev/null; sleep 1
"${E[@]}" client quit >/dev/null 2>&1
for _ in $(seq 1 20); do kill -0 "$client_pid" 2>/dev/null || break; sleep 0.5; done
sleep 2
host_read="$("${E[@]}" host read "$piece" 2>&1)"
verdict="$(python -c "
import math, re, sys
m = re.match(r'pos ([^ ]+) held (\w+) aim ([^ ]+) grip ([^ ]+)', sys.argv[1])
if not m or m.group(2) != 'True' or m.group(3) == 'none': print('FAIL host lost the piece: ' + sys.argv[1][:200]); sys.exit()
d = math.dist([float(v) for v in m.group(4).split(',')], [float(v) for v in m.group(3).split(',')])
print('PASS' if d <= 0.35 else f'FAIL {d:.2f} m from the host aim after the client left')
" "$host_read")"
log "client_quits_holding: host [$host_read]"
printf '%s %s\n' "$verdict" client_quits_holding | tee -a "$out/results.txt"
finish client_quits_holding "$verdict" host
case "$verdict" in PASS*) ;; *) failures=$((failures + 1)) ;; esac

log "$failures scenario(s) failed; results in $out/results.txt"
exit $((failures > 0))
