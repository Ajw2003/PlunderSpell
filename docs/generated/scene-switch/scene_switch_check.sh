#!/usr/bin/env bash
# Records setting out and coming home in co-op, for reading frame by frame (aj, 2026-10-29): the loading card must cover
# every frame between the scene left and the scene arrived in, on both windows. Also photographs the haul pile from
# above the Lair. Kept here, beside its runs, because Tools/ was locked by the design loop when it was written.
# Output: docs/generated/scene-switch/<label>/ (run.mkv, pile-from-above.png).
# Usage: bash docs/generated/scene-switch/scene_switch_check.sh <label>
set -uo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
cd "$repo"
label="${1:?usage: scene_switch_check.sh <label>}"
out="docs/generated/scene-switch/$label"
mkdir -p "$out"
say() { printf '%s %s\n' "$(date +%T)" "$*"; }
ev() { timeout 60 bash Tools/Unity/eval.sh "$@" 2>&1 | tail -1; }
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
say "windows: $(powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$repo/Tools/Unity/place_windows.ps1")" | tr -d '\r')"
sleep 3
say "$(ev 'var c = new UnityEngine.GameObject("PileShotCamera").AddComponent<UnityEngine.Camera>(); c.transform.position = new UnityEngine.Vector3(1000f, 4.6f, 0f); c.transform.rotation = UnityEngine.Quaternion.Euler(90f, 0f, 0f); c.fieldOfView = 100f; c.enabled = false; return "camera placed";')"
bash Tools/Unity/capture.sh "$repo/$out/pile-from-above.png" camera PileShotCamera >/dev/null 2>&1 && say "pile photographed"
ev 'UnityEngine.Object.Destroy(UnityEngine.GameObject.Find("PileShotCamera")); return "removed";' >/dev/null

ffmpeg -v error -y -f gdigrab -framerate 30 -i desktop -t 60 -c:v libx264 -preset ultrafast "$out/run.mkv" </dev/null &
rec=$!
sleep 2
say "set out: $(ev --file Tools/Unity/eval/set_out.cs)"
sleep 12
say "home: $(ev 'UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>().AbandonRaid(); Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.LairRoom); return "returned";')"
sleep 10
say "loading screen warnings: $(unity command console --no-banner --format json 2>/dev/null | grep -o 'SceneFlow[^"]*' | head -3)"
say "done; recording in $out/run.mkv"
