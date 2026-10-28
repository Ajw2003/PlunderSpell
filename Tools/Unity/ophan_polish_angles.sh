#!/usr/bin/env bash
# Camera check before the recorded Ophan polish run (#468, aj 2026-10-28): sets up the kill-run positions in co-op and
# saves a still from each window, so the angles can be looked at before anything is recorded. Starts its own co-op
# session and stops it at the end. Output: docs/generated/ophan-polish-468/angles/.
# Usage: bash Tools/Unity/ophan_polish_angles.sh
set -uo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/ophan-polish-468/angles"; mkdir -p "$out"; abs="$(cygpath -m "$repo/$out")"
say() { printf '%s %s\n' "$(date +%T)" "$*"; }
gym() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/ophan_gym_check.cs" timeout 60 bash Tools/Unity/coop_eval.sh "$@" 2>&1 | tail -1; }
ophan() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/ophan_check.cs" timeout 90 bash Tools/Unity/coop_eval.sh "$@" 2>&1 | tail -1; }
cleanup() {
    say "cleaning up"
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$repo/Tools/Unity/place_windows.ps1")" release >/dev/null 2>&1
    powershell.exe -NoProfile -Command "Get-Process Plunderspell -ErrorAction SilentlyContinue | Where-Object { \$_.Path -like '*$(basename "$repo")*' } | Stop-Process -Force" >/dev/null 2>&1
    # PurrNet finishes network destroys asynchronously; stopping Play at once logs "Destroy may not be called from edit mode".
    sleep 2
    unity command editor_stop --no-banner >/dev/null 2>&1 || true
}
trap cleanup EXIT
bash Tools/Unity/coop_local.sh --gym | tail -1
say "windows: $(powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$repo/Tools/Unity/place_windows.ps1")" | tr -d '\r')"
say "$(timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs 2>&1 | tail -1)"
sleep 2
say "no damage: host $(gym host call ToggleLocalPlayerUntouchable), client $(gym client call ToggleLocalPlayerUntouchable)"
say "guard: $(ophan host keepguard)"
say "host: $(ophan host grabany)"
say "$(gym host call Summon)"; sleep 7
say "host: $(ophan host place roofed)"
say "client: $(ophan client watch)"
say "host: $(ophan host faceother)"
stills() {
    # Both game windows on top right before each set, or the Editor ends up in front of the client.
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$repo/Tools/Unity/place_windows.ps1")" >/dev/null 2>&1
    sleep 1
    bash Tools/Unity/capture.sh "$repo/$out/$1-host.png" screen >/dev/null 2>&1
    say "$(ophan client shot "$abs/$1-client.png")"
    # Desktop Duplication: the ordinary screen grab (gdigrab) shows the client's window black.
    ffmpeg -v error -y -init_hw_device d3d11va -filter_complex "ddagrab=output_idx=0:framerate=10,hwdownload,format=bgra" -frames:v 1 "$out/$1-desktop.png"
}
state() { ophan host read | grep -o 'hostState [A-Za-z]*' | cut -d' ' -f2; }
stills setup
end=$((SECONDS + 25)); while [ $SECONDS -lt $end ] && [ "$(state)" != Charging ]; do :; done
end=$((SECONDS + 15)); while [ $SECONDS -lt $end ] && [ "$(state)" = Charging ]; do :; done
sleep 3
say "after the break: Ophan $(state)"
stills broken
if [ "${1:-}" = probe ]; then
    for what in beams bursts debris ophan; do say "$(ophan host hide $what)"; sleep 1; bash Tools/Unity/capture.sh "$repo/$out/probe-no-$what.png" screen >/dev/null 2>&1; done
fi
say "stills in $out"
