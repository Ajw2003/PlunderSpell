"""Reads `unity command test_status --format json` on stdin and prints a verdict for run_tests.sh.

Prints `running` while the run is not finished, or while the finished report is still the previous
run's (none of its results match the filter). Otherwise prints the summary, one line per failure,
then `PASS` or `FAIL` as the last line.

Usage: unity command test_status ... | python test_verdict.py <filter> [<report before the run>]
       unity command test_status ... | python test_verdict.py --raw
`--raw` prints a short fingerprint of the report, so the caller can pass it back as the "before" report
and a finished run that matches the filter but is the previous one is not mistaken for ours. A
fingerprint, not the text: after a full-suite run the report outgrows Windows' argument-length limit.
"""

import hashlib
import json
import sys


def fingerprint(raw: str) -> str:
    return hashlib.sha256(" ".join(raw.split()).encode("utf-8")).hexdigest()


def main() -> None:
    raw_mode = sys.argv[1] == "--raw"
    try:
        envelope = json.load(sys.stdin)
        raw = envelope["data"]["result"]
    except (ValueError, KeyError, TypeError):
        if raw_mode:
            print("")
            return
        # The Editor answers without a report while it enters Play mode for the run: not done yet.
        print("running")
        return
    if raw is None:
        print("running")
        return
    if sys.argv[1] == "--raw":
        print(fingerprint(raw))
        return

    test_filter = sys.argv[1]
    before = sys.argv[2] if len(sys.argv) > 2 else None
    if before is not None and fingerprint(raw) == before:
        print("running")
        return

    report = json.loads(raw)

    if report.get("status") != "completed":
        print("running")
        return

    results = report.get("results", [])
    # Substring, as Unity's own --filter matches: a bare class name like `CarryFeelTests` must count.
    if not any(test_filter in r.get("FullName", "") for r in results):
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
