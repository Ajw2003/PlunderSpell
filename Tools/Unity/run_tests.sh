#!/usr/bin/env bash
# Runs PlayMode tests in the connected Unity Editor and waits for the result.
# Usage: bash Tools/Unity/run_tests.sh Plunderspell.Tests.CastleArrivalTests [PlayMode|EditMode]
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"

filter="${1:?usage: run_tests.sh <test or class full name> [PlayMode|EditMode]}"
mode="${2:-PlayMode}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cli=(--caller plugin --skill unity-cli --no-banner --format json)

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
