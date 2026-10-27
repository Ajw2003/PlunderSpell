#!/usr/bin/env bash
# Films the Ophan's beams in the Ophan Gym from both machines (#460, #461): the slow beam from its target (the host)
# and from a second player (the client), then the charged beam bursting through the wall the host hides behind, frame
# by frame at a quarter of normal speed, and encodes the frames to .mp4 and .gif. Starts its own co-op session with
# coop_local.sh --gym and stops it at the end. Output: docs/generated/ophan-gym-451/<label>/.
# Usage: bash Tools/Unity/ophan_gym_film.sh <label>
set -uo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: ophan_gym_film.sh <label>}"
out="docs/generated/ophan-gym-451/$label"
mkdir -p "$out"
say() { printf '%s %s\n' "$(date +%T)" "$*"; }
gym() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/ophan_gym_check.cs" timeout 60 bash Tools/Unity/coop_eval.sh "$@" 2>&1 | tail -1; }
ophan() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/ophan_check.cs" timeout 90 bash Tools/Unity/coop_eval.sh "$@" 2>&1 | tail -1; }
state() { ophan host read | grep -o 'hostState [A-Za-z]*' | cut -d' ' -f2; }
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
say "host: $(ophan host grabany); $(ophan host place open)"; ophan host face >/dev/null
say "client: $(ophan client place open)"; ophan client face >/dev/null
for _ in $(seq 1 15); do [ "$(state)" = Draining ] && break; sleep 1; done
say "slow beam: Ophan $(state)"
bash Tools/Unity/capture.sh "$repo/$out/slow-target-host.png" screen >/dev/null 2>&1
say "$(ophan client shot "$(cygpath -m "$repo/$out")/slow-witness-client.png")"

# Into cover; a spot the slow beam still reaches gets another step sideways.
say "host: $(ophan host place cover)"; ophan host face >/dev/null
for _ in 1 2 3 4; do
    sleep 1.5
    [ "$(state)" != Draining ] && break
    say "still in the open; host: $(ophan host place step)"; ophan host face >/dev/null
done
for _ in $(seq 1 30); do [ "$(state)" = Charging ] && break; sleep 0.5; done
say "charged beam: Ophan $(state); host takes damage again: $(gym host call ToggleLocalPlayerUntouchable)"
gym host call CycleTimeScale >/dev/null; gym host call CycleTimeScale >/dev/null
for f in $(seq -w 1 24); do
    ophan host shot "$(cygpath -m "$repo/$out")/charged-host-$f.png" >/dev/null
    ophan client shot "$(cygpath -m "$repo/$out")/charged-client-$f.png" >/dev/null
done
gym host call CycleTimeScale >/dev/null
say "after: $(ophan host read | grep -o 'scorched [0-9]*\|player local=True[^|]*' | tr '\n' ' ')"
sleep 1
for side in host client; do
    ffmpeg -y -loglevel error -framerate 6 -i "$out/charged-$side-%02d.png" -vf "scale=960:-2" -pix_fmt yuv420p "$out/charged-$side.mp4" \
        && ffmpeg -y -loglevel error -framerate 6 -i "$out/charged-$side-%02d.png" -vf "scale=640:-1:flags=lanczos,split[a][b];[a]palettegen[p];[b][p]paletteuse" "$out/charged-$side.gif" \
        && say "encoded $out/charged-$side.mp4 and .gif" || say "FAIL encoding the $side frames"
done
