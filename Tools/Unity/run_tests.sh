#!/usr/bin/env bash
# Runs PlayMode tests in the connected Unity Editor and waits for the result.
# Usage: bash Tools/Unity/run_tests.sh RogueAi.Tests.CastleArrivalTests
set -euo pipefail

filter="${1:?usage: run_tests.sh <test or class full name>}"
cli=(--caller plugin --skill unity-cli --no-banner --format json)

unity command run_tests --mode PlayMode --filter "$filter" --async_tests true "${cli[@]}" > /dev/null

for _ in $(seq 1 120); do
    status_json="$(unity command test_status "${cli[@]}")"
    verdict="$(printf '%s' "$status_json" | python -c '
import json, sys
envelope = json.load(sys.stdin)
report = json.loads(envelope["data"]["result"])
if report.get("status") != "completed":
    print("running")
    sys.exit(0)
s = report["summary"]
print(f"total {s[\"total\"]}  passed {s[\"passed\"]}  failed {s[\"failed\"]}  skipped {s[\"skipped\"]}")
for r in report.get("results", []):
    if r.get("Status") not in ("Passed", "Skipped"):
        print(f"FAILED {r[\"FullName\"]}: {r.get(\"Message\")}")
print("FAIL" if s["failed"] or s["total"] == 0 else "PASS")
')"
    if [ "$verdict" != "running" ]; then
        printf '%s\n' "$verdict"
        [ "$(printf '%s' "$verdict" | tail -n 1)" = "PASS" ]
        exit $?
    fi
    sleep 5
done

echo "Timed out after 10 minutes waiting for the test run." >&2
exit 1
