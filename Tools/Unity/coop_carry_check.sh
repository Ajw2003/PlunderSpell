#!/usr/bin/env bash
# Two-player carry check for #169, on one PC with nobody at the keyboard: the Editor hosts in Play
# mode, a Development build in Build/DevTest joins it on 127.0.0.1, and both are driven through
# Tools/Unity/coop_eval.sh. Prints one PASS or FAIL line per scenario, and saves the client's log
# and, for each failed scenario, both sides' screenshots under docs/generated/coop-carry-<date>/.
#
# Usage: bash Tools/Unity/coop_carry_check.sh [--build | --no-build] [--shots]
#   (default)    build Build/DevTest only if a file under Assets/ is newer than it (about a minute)
#   --build      always build;  --no-build  never build
#   --shots      screenshot every scenario, not just failed ones (each costs a few seconds)
#
# Needs the Editor open on this project, not in Play mode. Leaves it stopped, with the Pipeline
# runtime setting off and ProjectSettings.asset's preloadedAssets line as it was
# (docs/4-systems/net.md, "Testing it").
set -uo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/coop-carry-$(date +%F)"
mkdir -p "$out"
# Every command has a time limit, so a step that never answers fails instead of hanging the run.
E=(timeout 90 bash Tools/Unity/coop_eval.sh)
cli=(--no-banner --format json)
client_pid=""
failures=0

log() { printf '%s %s\n' "$(date +%T)" "$*"; }

field() { # field <json envelope on stdin> <key>: one key of a Pipeline command's result
    python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get(sys.argv[1]))" "$1"
}

cleanup() {
    log "cleaning up"
    if [ -n "$client_pid" ] && kill -0 "$client_pid" 2>/dev/null; then
        "${E[@]}" client quit >/dev/null 2>&1 || true
        sleep 2
        kill "$client_pid" 2>/dev/null || true
    fi
    unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":false}' --confirm true "${cli[@]}" >/dev/null 2>&1 || true
    # The Pipeline build adds the Input System actions to preloadedAssets; put the line back.
    # Only touched when that line is there: rewriting the file otherwise changes its line endings.
    if grep -q 'fileID: -944628639613478452, guid: e05f63c218fb0e54f8c41ecaf9e2ef10' ProjectSettings/ProjectSettings.asset; then
        python -c "
import re, sys
p = sys.argv[1]
text = open(p, newline='').read()
text = re.sub(r'  preloadedAssets:(\r?\n)  - \{fileID: -944628639613478452, guid: e05f63c218fb0e54f8c41ecaf9e2ef10, type: 3\}', r'  preloadedAssets: []', text)
open(p, 'w', newline='').write(text)
" ProjectSettings/ProjectSettings.asset
    fi
    # Toggling the setting makes Unity rewrite these files with other line endings and nothing else;
    # put back only a file whose content git sees as unchanged, so a real edit is never lost.
    for settings in ProjectSettings/ProjectSettings.asset ProjectSettings/Packages/com.unity.pipeline/RuntimePipelineConfig.json; do
        if ! git diff --quiet -- "$settings" 2>/dev/null; then
            log "left $settings: its content changed"
        elif [ -n "$(git status --porcelain -- "$settings")" ]; then
            git checkout -- "$settings"
        fi
    done
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

# ---------------------------------------------------------------------------------------------
# The client build.
# ---------------------------------------------------------------------------------------------
build=auto; shots=failed
for arg in "$@"; do
    case "$arg" in
        --build) build=yes ;;
        --no-build) build=no ;;
        --shots) shots=all ;;
        *) log "FAIL unknown option $arg"; trap - EXIT; exit 1 ;;
    esac
done
if [ "$build" = auto ]; then
    build=no
    [ -f Build/DevTest/Plunderspell.exe ] || build=yes
    # Anything the client build is made from, changed since it was built. Tests are not in it.
    if [ "$build" = no ] && [ -n "$(find Assets ProjectSettings -newer Build/DevTest/Plunderspell.exe -type f \
            ! -path '*/Tests/*' ! -name '*.meta' ! -name 'ProjectSettings.asset' -print -quit)" ]; then
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

timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
wait_for host "state Playing" 30
wait_for client "state Playing" 30
sleep 3

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
        if rise < float(min_rise):
            problems.append(f"piece rose {rise:.2f} m (wanted at least {min_rise} m)")
print("PASS" if not problems else "FAIL " + "; ".join(problems))
PY
)"
    log "$name: host [$host_read] client [$client_read]"
    printf '%s %s\n' "$verdict" "$name" | tee -a "$out/results.txt"
    case "$verdict" in PASS*) ;; *) failures=$((failures + 1)) ;; esac
    shoot "$name" "$verdict" both
}

shoot() { # shoot <name> <verdict> <both|host>: screenshots, for a failure or when --shots asked
    case "$2" in PASS*) [ "$shots" = all ] || return 0 ;; esac
    [ "$3" = both ] && "${E[@]}" client shot "$(cygpath -m "$repo/$out/$1-client.png")" >/dev/null 2>&1
    timeout 60 bash Tools/Unity/capture.sh "$out/$1-host.png" >/dev/null 2>&1
}

printf '\n# run %s\n' "$(date +%T)" >> "$out/results.txt"

stage light:0
"${E[@]}" host grab "$piece" >/dev/null; "${E[@]}" host level >/dev/null; sleep 2
check host_grabs holder=host near=host

stage light:1
"${E[@]}" client grab "$piece" >/dev/null; "${E[@]}" client level >/dev/null; sleep 2
check client_grabs holder=client near=client

stage light:5
"${E[@]}" client grab "$piece" >/dev/null; "${E[@]}" client level >/dev/null; sleep 2
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
shoot client_throws "$verdict" both
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
    shoot heavy_alone_tows "$verdict" both
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
shoot client_quits_holding "$verdict" host
case "$verdict" in PASS*) ;; *) failures=$((failures + 1)) ;; esac

log "$failures scenario(s) failed; results in $out/results.txt"
exit $((failures > 0))
