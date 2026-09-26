#!/usr/bin/env bash
# Runs PlayMode tests in the connected Unity Editor and waits for the result.
# Usage: bash Tools/Unity/run_tests.sh RogueAi.Tests.CastleArrivalTests
set -euo pipefail

filter="${1:?usage: run_tests.sh <test or class full name>}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cli=(--caller plugin --skill unity-cli --no-banner --format json)

# The report of the run before ours: until test_status changes, it is not ours yet.
before="$(unity command test_status "${cli[@]}" | python "$here/test_verdict.py" --raw)"

unity command run_tests --mode PlayMode --filter "$filter" --async_tests true "${cli[@]}" > /dev/null

for _ in $(seq 1 120); do
    status_json="$(unity command test_status "${cli[@]}")"
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
