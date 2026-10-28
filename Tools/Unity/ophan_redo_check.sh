#!/usr/bin/env bash
# Records the Ophan redo (#468) in the Ophan Gym, in co-op, for reading frame by frame before aj's QA (Decisions
# 2026-10-27): the chest aim (#469), the charged beam's two shots (#470) and the walls it breaks with the fracture
# system (#472). The whole desktop is recorded to run.mkv for the full run; frame rate is read before and after the
# break. Starts its own co-op session with coop_local.sh --gym and stops it at the end.
# Output: docs/generated/ophan-redo-468/<label>/.
# Usage: bash Tools/Unity/ophan_redo_check.sh <label>
set -uo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: ophan_redo_check.sh <label>}"
out="docs/generated/ophan-redo-468/$label"
mkdir -p "$out"
abs="$(cygpath -m "$repo/$out")"
say() { printf '%s %s\n' "$(date +%T)" "$*"; }
gym() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/ophan_gym_check.cs" timeout 60 bash Tools/Unity/coop_eval.sh "$@" 2>&1 | tail -1; }
ophan() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/ophan_check.cs" timeout 90 bash Tools/Unity/coop_eval.sh "$@" 2>&1 | tail -1; }
state() { ophan host read | grep -o 'hostState [A-Za-z]*' | cut -d' ' -f2; }
# Waits up to $2 seconds of wall time (each read is a co-op eval of about 1.5 s, so a poll count would overrun).
wait_state() { local end=$((SECONDS + $2)); while [ $SECONDS -lt $end ]; do [ "$(state)" = "$1" ] && return 0; done; return 1; }
fail() { say "FAIL: $*"; exit 1; }
cleanup() {
    say "cleaning up"
    [ -n "${rec:-}" ] && kill "$rec" 2>/dev/null
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$repo/Tools/Unity/place_windows.ps1")" release >/dev/null 2>&1
    powershell.exe -NoProfile -Command "Get-Process Plunderspell -ErrorAction SilentlyContinue | Where-Object { \$_.Path -like '*$(basename "$repo")*' } | Stop-Process -Force" >/dev/null 2>&1
    unity command editor_stop --no-banner >/dev/null 2>&1 || true
}
trap cleanup EXIT

bash Tools/Unity/coop_local.sh --gym | tail -1
# Both game windows above everything else, or the recording shows whatever covers them (#468 first run).
say "windows: $(powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$repo/Tools/Unity/place_windows.ps1")" | tr -d '')"
# Matroska survives being stopped mid-write, so the cleanup can end the recording at any point.
ffmpeg -v error -y -f gdigrab -framerate 30 -i desktop -t 240 -c:v libx264 -preset ultrafast "$out/run.mkv" </dev/null &
rec=$!
fps() { bash Tools/Unity/eval.sh 'return (1f / UnityEngine.Time.smoothDeltaTime).ToString("F0") + " fps";' 2>&1 | tail -1; }
say "$(timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs 2>&1 | tail -1)"
sleep 2
say "guards: $(gym host call RemoveGuards); client no damage: $(gym client call ToggleLocalPlayerUntouchable)"
# The host carries the most worth before the Ophan comes, or it locks on to the client and keeps it (#468 first run).
say "host no damage while it is placed: $(gym host call ToggleLocalPlayerUntouchable); $(ophan host grabany)"
say "$(gym host call Summon)"
sleep 6
say "frame rate before any break: $(fps)"

say "host: $(ophan host place open)"; ophan host face >/dev/null
say "client: $(ophan client place open)"; ophan client face >/dev/null
wait_state Draining 15 || fail "no slow beam within 15 s (Ophan $(state))"
say "target: $(ophan host read | grep -o "target [^ ]*")"
sleep 1
bash Tools/Unity/capture.sh "$repo/$out/slow-target-host.png" screen >/dev/null 2>&1
say "$(ophan client shot "$abs/slow-witness-client.png")"

say "host: $(ophan host place cover)"; ophan host face >/dev/null
for _ in 1 2 3 4; do
    sleep 1.5
    [ "$(state)" != Draining ] && break
    say "still in the open; host: $(ophan host place step)"; ophan host face >/dev/null
done
wait_state Charging 20 || fail "no charge within 20 s of cover (Ophan $(state))"
say "charging; host takes damage again: $(gym host call ToggleLocalPlayerUntouchable)"
sleep 1.5
bash Tools/Unity/capture.sh "$repo/$out/charging-target-host.png" screen >/dev/null 2>&1
say "$(ophan client shot "$abs/charging-witness-client.png")"
wait_state Draining 6 || fail "the first shot did not fire within 6 s (Ophan $(state))"
say "first shot fired (breaks stone, no damage): $(ophan host read | grep -o 'player local=True[^|]*')"
sleep 2
say "after the second shot: $(ophan host read | grep -o 'player local=True[^|]*')"
say "frame rate after the break: $(fps)"
sleep 4
say "done; recording in $out/run.mkv"
