#!/usr/bin/env bash
# Summarises the Profiler captures from perf_capture.sh (#242) into one text report per label.
# Usage: bash Tools/Unity/perf_report.sh <label>
#   writes docs/generated/perf-2026-10-03/<label>-report.txt
# Needs the Editor open on this project, not in Play mode.
set -uo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: perf_report.sh <label>}"
out="docs/generated/perf-2026-10-03"
report="$out/$label-report.txt"
: > "$report"
for raw in "$out/$label"-*.raw; do
    code="$(sed "s|__PATH__|$(cygpath -m "$repo/$raw")|" Tools/Unity/eval/perf_report.cs)"
    timeout 600 bash Tools/Unity/eval.sh "$code" | tee -a "$report"
    echo | tee -a "$report"
done
