# Archive

Documents that were correct when written and are now inert. Nothing here describes current
behaviour — check `docs/4-systems/` for that.

## `2026-09-15-integration/`

The paper trail from merging 7 unmerged feature branches into `integration/staging-2026-09-15`
(later merged into `main` as PR #3), formerly committed at the repo root as `.agent_reports/`.
One-time event, now finished — the branches it describes no longer exist as separate lineages,
and its "current status" (0 compile errors, 105/105 PlayMode tests passing at that point in time)
is a historical snapshot, not a live claim. See [`docs/3-state/ProjectState.md`](../3-state/ProjectState.md) for
where things stand now.

- `FINAL_MERGE_SUMMARY.md` — the merge order, every conflict and how it was actually resolved, and
  the real defects found along the way.
- `progress.md` — the same, as an append-only log with one section per branch.
- `KNOWN_ISSUES.md` — one still-relevant technical finding pulled out of the above: why
  `WaitForEndOfFrame` doesn't work under Unity's `-batchmode` and what to do instead. Worth
  reading on its own if you ever need a PlayMode test to capture a screenshot headlessly.
- `branch_manifest.txt`, `results_*.xml` — raw inputs the two narrative docs above summarize.
