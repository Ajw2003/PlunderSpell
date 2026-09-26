#!/usr/bin/env bash
# Fixed-camera captures of the castle at night, for comparing with the Blender look samples
# (docs/generated/look-samples-2026-09-24/). Plays a solo raid on seed 777 in the live Editor,
# clears the garrison so nothing interrupts, then captures each view in the given alarm states.
#
# Usage: bash Tools/Unity/night_captures.sh <out dir> [state ...]
#   states: Calm Stirred Roused HueAndCry (default: Calm HueAndCry)
# Extra C# to run before capturing (live tuning) can be passed in the TUNE environment variable
# as a path to an eval file.
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
out="${1:?usage: night_captures.sh <out dir> [state ...]}"
shift
states=("$@")
[ ${#states[@]} -eq 0 ] && states=(Calm HueAndCry)
cli=(--caller plugin --skill unity-cli --no-banner)

mode="$(unity command editor_status "${cli[@]}" --format json | python -c "import json,sys; print(json.load(sys.stdin)['data']['result']['playMode'])")"
if [ "$mode" != "playing" ]; then
    unity command editor_play "${cli[@]}" > /dev/null
    sleep 8
    bash "$here/eval.sh" --file "$here/eval/start_solo_raid.cs"
    sleep 3
    bash "$here/eval.sh" --file "$here/eval/set_out_seed.cs"
    sleep 2
fi
bash "$here/eval.sh" --file "$here/eval/quiet_castle.cs"
[ -n "${TUNE:-}" ] && bash "$here/eval.sh" --file "$TUNE"

# name x y z yaw pitch — views into the seed-777 castle (gate at (48, 0), arrival at (-24, -12)).
views=(
    "strip 45 1.7 -16 10 -4"
    "vignette 43 1.7 11 153 -9"
    "bailey 44 2.2 22 200 -2"
    "gate 44 1.7 -8 40 -8"
    "arrival -24 1.7 -8.5 180 -6"
    "overview 0 22 -40 20 28"
)

# Always leave Play mode afterwards: the parked player is held kinematic, and the controller
# complains every frame about setting velocity on it.
trap 'unity command editor_stop "${cli[@]}" > /dev/null' EXIT

for state in "${states[@]}"; do
    level=0
    case "$state" in
        Calm) level=0 ;; Stirred) level=30 ;; Roused) level=60 ;; HueAndCry) level=95 ;;
    esac
    bash "$here/eval.sh" "var a = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Alarm.AlarmFSMManager>(); a.SetAlarmLevel(${level}f); return a.State.ToString();"
    sleep 3
    for view in "${views[@]}"; do
        read -r name x y z yaw pitch <<< "$view"
        bash "$here/view.sh" "$x" "$y" "$z" "$yaw" "$pitch" "$out/${name}-${state}.png" > /dev/null
        echo "captured $out/${name}-${state}.png"
    done
done
