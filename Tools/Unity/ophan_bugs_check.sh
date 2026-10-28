#!/usr/bin/env bash
# Checks the Ophan bugs and solid beams build (#463) in the Ophan Gym, in co-op: whether walls and roofs count as cover
# (#465), the solid slow beam and the growing charged beam from both machines (#466), the hole the charged beam cuts
# (#467), and the host's spectator view once the charge kills it (#464). Starts its own co-op session with
# coop_local.sh --gym and stops it at the end. Output: docs/generated/ophan-bugs-463/<label>/.
# Usage: bash Tools/Unity/ophan_bugs_check.sh <label>
set -uo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: ophan_bugs_check.sh <label>}"
out="docs/generated/ophan-bugs-463/$label"
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
    powershell.exe -NoProfile -Command "Get-Process Plunderspell -ErrorAction SilentlyContinue | Where-Object { \$_.Path -like '*$(basename "$repo")*' } | Stop-Process -Force" >/dev/null 2>&1
    unity command editor_stop --no-banner >/dev/null 2>&1 || true
}
trap cleanup EXIT

bash Tools/Unity/coop_local.sh --gym | tail -1
say "$(timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs 2>&1 | tail -1)"
sleep 2
say "guards: $(gym host call RemoveGuards); client no damage: $(gym client call ToggleLocalPlayerUntouchable)"
say "host no damage while it is placed: $(gym host call ToggleLocalPlayerUntouchable); $(gym host call Summon)"
sleep 6
say "cover (#465): $(ophan host survey)"

say "host: $(ophan host grabany); $(ophan host place open)"; ophan host face >/dev/null
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
wait_state Charging 10 || fail "no charge within 10 s of cover (Ophan $(state))"
say "charging; host takes damage again: $(gym host call ToggleLocalPlayerUntouchable)"
sleep 1.5
bash Tools/Unity/capture.sh "$repo/$out/charging-target-host.png" screen >/dev/null 2>&1
say "$(ophan client shot "$abs/charging-witness-client.png")"
wait_state Draining 6 || fail "the charged beam did not fire within 6 s (Ophan $(state))"
sleep 0.2
say "$(ophan client shot "$abs/charged-witness-client.png")"
say "hole (#467): $(ophan host hole)"
sleep 3
say "after the hit: $(ophan host read | grep -o 'player local=True[^|]*' | tr '\n' ' ')"
bash Tools/Unity/capture.sh "$repo/$out/spectating-host.png" screen >/dev/null 2>&1
say "spectator camera far clip (#464): $(bash Tools/Unity/eval.sh 'var c = UnityEngine.Camera.main; return c == null ? "no main camera" : c.name + " far " + c.farClipPlane;' 2>&1 | tail -1)"
say "$(gym host call ToggleLocalPlayerUntouchable)"
say "done; pictures in $out"
