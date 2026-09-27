#!/usr/bin/env bash
# Two-player carry check for #169, on one PC with nobody at the keyboard: the Editor hosts in Play
# mode, a Development build in Build/DevTest joins it on 127.0.0.1, and both are driven through
# Tools/Unity/coop_eval.sh. Prints one PASS or FAIL line per scenario, and saves both sides'
# screenshots and the client's log under docs/generated/coop-carry-<date>/.
#
# Usage: bash Tools/Unity/coop_carry_check.sh [--no-build]
#   --no-build   reuse the existing Build/DevTest instead of building it first (about a minute).
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
if [ "${1:-}" != "--no-build" ]; then
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
    "${E[@]}" client shot "$(cygpath -m "$repo/$out/$name-client.png")" >/dev/null 2>&1
    timeout 60 bash Tools/Unity/capture.sh "$out/$name-host.png" >/dev/null 2>&1
    verdict="$(python - "$host_read" "$client_read" "$@" <<'PY'
import math, re, sys
host, client, rules = sys.argv[1], sys.argv[2], sys.argv[3:]

def parse(line):
    m = re.match(r"pos ([^ ]+) held (\w+) aim ([^ ]+) grip ([^ ]+)", line)
    if not m:
        return None
    vec = lambda s: None if s == "none" else [float(v) for v in s.split(",")]
    return {"pos": vec(m.group(1)), "held": m.group(2) == "True", "aim": vec(m.group(3)), "grip": vec(m.group(4))}

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
print("PASS" if not problems else "FAIL " + "; ".join(problems))
PY
)"
    log "$name: host [$host_read] client [$client_read]"
    printf '%s %s\n' "$verdict" "$name" | tee -a "$out/results.txt"
    case "$verdict" in PASS*) ;; *) failures=$((failures + 1)) ;; esac
}

printf '\n# run %s\n' "$(date +%T)" >> "$out/results.txt"

stage light:0
"${E[@]}" host grab "$piece" >/dev/null; "${E[@]}" host level >/dev/null; sleep 2
check host_grabs holder=host near=host

stage light:1
"${E[@]}" client grab "$piece" >/dev/null; "${E[@]}" client level >/dev/null; sleep 2
check client_grabs holder=client near=client

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
timeout 60 bash Tools/Unity/capture.sh "$out/client_quits_holding-host.png" >/dev/null 2>&1
verdict="$(python -c "
import math, re, sys
m = re.match(r'pos ([^ ]+) held (\w+) aim ([^ ]+) grip ([^ ]+)', sys.argv[1])
if not m or m.group(2) != 'True' or m.group(3) == 'none': print('FAIL host lost the piece: ' + sys.argv[1][:200]); sys.exit()
d = math.dist([float(v) for v in m.group(4).split(',')], [float(v) for v in m.group(3).split(',')])
print('PASS' if d <= 0.35 else f'FAIL {d:.2f} m from the host aim after the client left')
" "$host_read")"
log "client_quits_holding: host [$host_read]"
printf '%s %s\n' "$verdict" client_quits_holding | tee -a "$out/results.txt"
case "$verdict" in PASS*) ;; *) failures=$((failures + 1)) ;; esac

log "$failures scenario(s) failed; results in $out/results.txt"
exit $((failures > 0))
