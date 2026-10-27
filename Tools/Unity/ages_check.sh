#!/usr/bin/env bash
# The stacked castle in every Age (#257): for each Age, a solo raid on seed 777, the garrison cleared, the light
# audit (eval/light_audit.cs) and fixed views from outside, the ground floor, the keep, the crypt and both
# stairs, saved under docs/generated/ages-check-<date>/<Age>/. Leaves Play mode stopped.
# Usage: bash Tools/Unity/ages_check.sh [Age ...]   (default: BronzeAge HighMedieval LateMedieval AgeOfPowder)
# Needs the Editor open on this project, not in Play mode.
set -uo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd "$here/../.." && pwd)"
cd "$repo"
ages=("$@")
[ ${#ages[@]} -eq 0 ] && ages=(BronzeAge HighMedieval LateMedieval AgeOfPowder)
out="docs/generated/ages-check-$(date +%Y-%m-%d)"
cli=(--caller plugin --skill unity-cli --no-banner)
ev() { timeout 90 bash "$here/eval.sh" "$@" | tail -1; }

playing="$(ev 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')"
[ "$playing" = "False False" ] || { echo "FAIL Editor is playing or compiling ($playing)"; exit 1; }
trap 'unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true' EXIT

# name x y z yaw pitch. The castle is a radius-3 grid of 12 m cells; keep floor 4.60, crypt floor -3.00.
views=(
    "outside-south 0 28 -70 0 22"
    "outside-east 70 22 30 240 18"
    "outside-low -60 2 -45 50 -2"
    "ground-bailey -24 2.0 -30 20 2"
    "ground-ward 0 2.0 -10 0 5"
    "keep-centre -4 6.3 -4 45 5"
    "keep-corner 14 6.3 14 225 10"
    "crypt-centre -4 -1.3 -4 45 5"
    "crypt-north 0 -1.3 14 180 5"
    "stair-up-lobby -7.5 2.0 0.5 270 5"
    "stair-down-lobby 3 2.0 3 225 30"
)

for age in "${ages[@]}"; do
    dir="$out/$age"; mkdir -p "$dir"
    unity command editor_play "${cli[@]}" > /dev/null
    sleep 10
    ev --file "$here/eval/start_solo_raid.cs" >/dev/null
    ev "var lair = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Lair.LairHubManager>(); lair.SelectEra(Plunderspell.Inventory.HistoricalEra.$age); return \"era $age\";"
    sleep 3
    ev --file "$here/eval/set_out_seed.cs"
    sleep 4
    ev --file "$here/eval/quiet_castle.cs" >/dev/null
    timeout 90 bash "$here/eval.sh" --file "$here/eval/light_audit.cs" > "$dir/light-audit.txt"
    head -1 "$dir/light-audit.txt"
    timeout 90 bash "$here/eval.sh" --file "$here/eval/stair_modules.cs" > "$dir/stairs.txt"
    for v in "${views[@]}"; do
        read -r name x y z yaw pitch <<< "$v"
        timeout 90 bash "$here/view.sh" "$x" "$y" "$z" "$yaw" "$pitch" "$dir/$name.png" | tail -1
    done
    unity command editor_stop "${cli[@]}" > /dev/null
    sleep 5
done
echo "done: $out"
