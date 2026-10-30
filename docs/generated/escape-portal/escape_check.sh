#!/usr/bin/env bash
# Co-op check of the escape portal (#556): the arrival portal closes at 20 s, the escape portal opens in the same room
# on both machines as the hunt window opens, its hum plays there, and leaving through it saves the host and leaves the
# client behind. Moves the raid clock instead of waiting 5 minutes, so the Ophan (timed at raid start) does not come.
# Records both windows (desktop duplication) to <label>/run.mkv. Kept beside its runs.
# Usage: bash docs/generated/escape-portal/escape_check.sh <label>
set -uo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
cd "$repo"
label="${1:?usage: escape_check.sh <label>}"
out="docs/generated/escape-portal/$label"
mkdir -p "$out"
abs="$(cygpath -m "$repo/$out")"
say() { printf '%s %s\n' "$(date +%T)" "$*"; }
ev() { timeout 60 bash Tools/Unity/eval.sh "$@" 2>&1 | tail -1; }
esc() { COOP_EVAL_FILE="$repo/docs/generated/escape-portal/escape_check.cs" timeout 60 bash Tools/Unity/coop_eval.sh "$@" 2>&1 | tail -1; }
cleanup() {
    say "cleaning up"
    [ -n "${rec:-}" ] && kill "$rec" 2>/dev/null
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$repo/Tools/Unity/place_windows.ps1")" release >/dev/null 2>&1
    powershell.exe -NoProfile -Command "Get-Process Plunderspell -ErrorAction SilentlyContinue | Where-Object { \$_.Path -like '*$(basename "$repo")*' } | Stop-Process -Force" >/dev/null 2>&1
    sleep 2
    unity command editor_stop --no-banner >/dev/null 2>&1 || true
}
trap cleanup EXIT

bash Tools/Unity/coop_local.sh | tail -1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$repo/Tools/Unity/place_windows.ps1")" >/dev/null
powershell.exe -NoProfile -Command "Add-Type -Name W -Namespace U -MemberDefinition '[DllImport(\"user32.dll\")] public static extern bool SetWindowPos(System.IntPtr w, System.IntPtr a, int x, int y, int cx, int cy, uint f);'; \$c = Get-Process Plunderspell | Select-Object -First 1; [U.W]::SetWindowPos(\$c.MainWindowHandle, [System.IntPtr]-1, 0, 0, 0, 0, 0x3)" >/dev/null
ffmpeg -v error -y -f lavfi -i "ddagrab=output_idx=0:framerate=30" -t 120 -vf "hwdownload,format=bgra" -c:v libx264 -preset ultrafast "$out/run.mkv" </dev/null &
rec=$!
say "set out: $(ev --file Tools/Unity/eval/set_out.cs)"
sleep 8
say "$(esc host noguards)"
say "arrival, host: $(esc host read)"
say "arrival, client: $(esc client read)"
esc host shot "$abs/arrival-host.png" >/dev/null
say "clock past the grace: $(esc host clock 275)"
sleep 3
say "closed, host: $(esc host read)"
say "closed, client: $(esc client read)"
say "clock before the hunt window: $(esc host clock 62)"
sleep 4
say "escape, host: $(esc host read)"
say "escape, client: $(esc client read)"
esc host shot "$abs/escape-host.png" >/dev/null
esc client shot "$abs/escape-client.png" >/dev/null
say "host leaves: $(esc host pinself)"
sleep 6
say "after leaving, host: $(esc host phase)"
say "after leaving, client: $(esc client phase)"
say "zone, host: $(esc host read)"
say "warnings: $(unity command console --no-banner --format json 2>/dev/null | grep -o '\[Raid\] Escape[^"\]*\|ExtractionZone:[^"\]*\|No room qualified[^"\]*' | sort -u | head -5)"
say "done; recording in $out/run.mkv"
