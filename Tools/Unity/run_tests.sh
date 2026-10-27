#!/usr/bin/env bash
# Runs PlayMode tests in the connected Unity Editor and waits for the result.
# Usage: bash Tools/Unity/run_tests.sh Plunderspell.Tests.CastleArrivalTests [PlayMode|EditMode]
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"

filter="${1:?usage: run_tests.sh <test or class full name> [PlayMode|EditMode]}"
mode="${2:-PlayMode}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cli=(--caller plugin --skill unity-cli --no-banner --format json)

# A test run leaves the open scene, and a scene with unsaved edits makes Unity stop and ask
# "Scene(s) Have Been Modified", which holds the Editor until someone clicks (#371). Save them first:
# the scenes are in git, so the diff shows what was saved and it can be undone.
save_code='if (UnityEditor.EditorApplication.isPlaying) return "";
var saved = new System.Collections.Generic.List<string>();
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
{
    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
    if (!scene.isDirty) continue;
    if (string.IsNullOrEmpty(scene.path)) saved.Add("UNSAVABLE untitled scene");
    else if (UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) saved.Add(scene.path);
}
return string.Join(", ", saved);'
saved="$(unity command eval "${cli[@]}" --code "$save_code" 2>/dev/null \
    | python -c "import json,sys; d=json.load(sys.stdin); r=(d.get('data') or {}).get('result'); r=json.loads(r) if isinstance(r,str) and r.startswith('{') else r; print((r.get('result') if isinstance(r,dict) else r) or '')" 2>/dev/null || true)"
if [ -n "$saved" ]; then
    echo "Saved scene(s) with unsaved edits before the run, so Unity does not stop to ask: $saved (see git diff)" >&2
fi
exit_if_unity_dialog

# The report of the run before ours: until test_status changes, it is not ours yet.
before="$(unity command test_status "${cli[@]}" 2>/dev/null | python "$here/test_verdict.py" --raw || true)"

# The Editor refuses commands for a few seconds after a run or a recompile; ask again rather than
# waiting on a run that never started.
started=""
for _ in $(seq 1 12); do
    if reply="$(unity command run_tests --mode "$mode" --filter "$filter" --async_tests true "${cli[@]}" 2>&1)"         && printf '%s' "$reply" | python -c "import json,sys; sys.exit(0 if json.load(sys.stdin).get('success') else 1)" 2>/dev/null; then
        started=yes
        break
    fi
    case "$reply" in *"Main thread operation timed out"*) exit_if_unity_dialog ;; esac
    sleep 5
done
if [ -z "$started" ]; then
    echo "The Editor did not accept the test run. Last reply:" >&2
    printf '%s
' "${reply:0:600}" >&2
    exit 1
fi

for _ in $(seq 1 120); do
    # While the Editor reloads into Play mode for the run it does not answer; that is not a result.
    status_json="$(unity command test_status "${cli[@]}" 2>/dev/null || true)"
    # ...but a dialog waiting for a click never ends by itself (#371).
    case "$status_json" in *"Main thread operation timed out"*) exit_if_unity_dialog ;; esac
    verdict="$(printf '%s' "$status_json" | python "$here/test_verdict.py" "$filter" "$before")"
    if [ "$verdict" != "running" ]; then
        printf '%s\n' "$verdict"
        [ "$(printf '%s' "$verdict" | tail -n 1)" = "PASS" ]
        exit $?
    fi
    sleep 5
done

echo "Timed out after 10 minutes waiting for the test run." >&2
exit 1
