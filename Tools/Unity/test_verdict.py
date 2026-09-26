"""Reads `unity command test_status --format json` on stdin and prints a verdict for run_tests.sh.

Prints `running` while the run is not finished, or while the finished report is still the previous
run's (none of its results match the filter). Otherwise prints the summary, one line per failure,
then `PASS` or `FAIL` as the last line.

Usage: unity command test_status ... | python test_verdict.py <filter> [<report before the run>]
       unity command test_status ... | python test_verdict.py --raw
`--raw` prints the report's text on one line, so the caller can pass it back as the "before" report
and a finished run that matches the filter but is the previous one is not mistaken for ours.
"""

import json
import sys


def main() -> None:
    envelope = json.load(sys.stdin)
    raw = envelope["data"]["result"]
    if sys.argv[1] == "--raw":
        print(" ".join(raw.split()))
        return

    test_filter = sys.argv[1]
    before = sys.argv[2] if len(sys.argv) > 2 else None
    if before is not None and " ".join(raw.split()) == before:
        print("running")
        return

    report = json.loads(raw)

    if report.get("status") != "completed":
        print("running")
        return

    results = report.get("results", [])
    if not any(r.get("FullName", "").startswith(test_filter) for r in results):
        # test_status still holds the run before ours.
        print("running")
        return

    summary = report["summary"]
    print(
        f"total {summary['total']}  passed {summary['passed']}  "
        f"failed {summary['failed']}  skipped {summary['skipped']}"
    )
    for r in results:
        if r.get("Status") not in ("Passed", "Skipped"):
            print(f"FAILED {r['FullName']}: {r.get('Message')}")
    print("FAIL" if summary["failed"] or summary["total"] == 0 else "PASS")


if __name__ == "__main__":
    main()
