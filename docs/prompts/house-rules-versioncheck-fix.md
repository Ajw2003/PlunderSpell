# Handoff: fix house-rules `versioncheck` so "couldn't check" is never silent

**For:** an agent working in the `Ajw2003/AjsClaudeCodeTools` repo (not this one). This file lives
in PlunderSpell only because that's where the failure was noticed; the work is entirely in the
tools repo. Written 2026-09-24.

## What happened

At session start on this machine, house-rules was **2.31.0** installed while GitHub's default branch
published **2.33.0**. The `versioncheck` SessionStart hook said nothing the model could see, so the
session went ahead on an out-of-date copy. The user had to spot it themselves.

Reproduced by running the hook by hand (`echo '{"session_id":"probe-test"}' | sh scripts/run.sh
versioncheck`):

```
{"systemMessage":"versioncheck: could not fully verify the plugin is current - could not reach GitHub to check the published version (URLError)"}
```

Two independent causes, both in `claude-house-rules/plugins/house-rules/scripts/hook.py` (line
numbers from 2.33.0):

1. **Single network path.** `_github_version()` (~line 756) fetches only
   `raw.githubusercontent.com/.../plugin.json` (`_GITHUB_PLUGIN_JSON_URL`, ~line 704). From this
   machine's Claude Code sandbox that host resets the connection (`curl: (35) Recv failure:
   Connection was reset`), while `api.github.com` works — `gh api
   repos/Ajw2003/AjsClaudeCodeTools/contents/claude-house-rules/plugins/house-rules/.claude-plugin/plugin.json`
   returned `"version": "2.33.0"`. There is no fallback, so one blocked host blinds the check.
2. **"Couldn't verify" goes to the user only, and quietly.** When the installed copy and the
   marketplace clone agree (both 2.31.0 — both stale) and GitHub is unreachable, `reasons` is empty
   and the handler calls `trace(...)` (~line 874), which emits a `systemMessage`. A `systemMessage`
   is shown in the UI but never reaches the model, and in the desktop app it is easy to miss. So
   an unverified result looks identical to a verified one from the model's side — against the
   plugin's own "Nothing fails silently" rule.

## What to change

1. **Add an API fallback in `_github_version()`.** If the raw URL fails, try
   `https://api.github.com/repos/<owner>/<repo>/contents/<path>?ref=main` with header
   `Accept: application/vnd.github.raw` (same 4 s timeout budget overall, standard library
   `urllib` only — the hook must stay dependency-free). Derive the API URL from the same owner/repo/path
   so `HOUSE_RULES_VC_GITHUB_URL` forks still work; add a `HOUSE_RULES_VC_GITHUB_API_URL` override
   only if deriving it is awkward. Record in `problems` which route(s) failed and why.
2. **Surface "couldn't verify" to the model, not just the UI.** On the `problems and not reasons`
   path, emit `hookSpecificOutput.additionalContext` (SessionStart) telling the model to say, in
   its first reply, that the plugin's freshness could not be checked, naming each failure, plus the
   installed version — alongside the existing `systemMessage` for the user. Keep it short and
   plainly worded. Do **not** treat it as "out of date": no banner, no guard marker. The documented
   stance ("Network failures are best-effort and never treated as 'out of date'") stays; what
   changes is that best-effort failures are now announced to the model.
3. **Keep the all-agree path quiet to the model** (a trace `systemMessage` only, as now).

## Tests (in `scripts/verify.py`, the versioncheck block ~line 4059)

Existing seams: `HOUSE_RULES_VC_MARKETPLACE` / `HOUSE_RULES_VC_GITHUB`. Add a seam that simulates
fetch failure (e.g. `HOUSE_RULES_VC_GITHUB_URL` + an API-URL override pointing at an unroutable
address, or a dedicated `HOUSE_RULES_VC_GITHUB=__fail__` sentinel — whichever fits the file's style),
then add cases for:

- raw fails, API succeeds with a newer version → out-of-date banner fires (the exact 2026-09-24 case).
- raw and API both fail, installed == marketplace → `additionalContext` present and names the
  failure; no marker written; no out-of-date banner.
- all three agree → no `additionalContext` (regression guard for item 3).

Run `python claude-house-rules/plugins/house-rules/scripts/verify.py` and quote its summary. Then
run the real hook once against the real network, per the house rule that a green suite isn't proof
it works.

## Docs to update in the same change

- `docs/architecture.md` — the `versioncheck` row (~line 38) and the section "`versioncheck` checks
  three copies of the version, not two" (~line 583): the fallback route and the new
  model-visible "couldn't verify" notice.
- `docs/6-decisions/Decisions.md` — a dated entry: previously an unverifiable check told the model nothing;
  now it tells the model, because an out-of-date install went unnoticed on 2026-09-24.
- `claude-house-rules/README.md` if it describes versioncheck's output.
- Bump the version in `.claude-plugin/plugin.json` (and the marketplace manifest if it carries one),
  following the repo's usual release steps.

## Done when

- `verify.py` passes, including the new cases, with its output quoted.
- A real `versioncheck` run on a machine that blocks `raw.githubusercontent.com` either reports the
  true published version via the API or produces a model-visible "couldn't verify" notice — never
  silence.
- Work is committed on a `claude/` branch in `Ajw2003/AjsClaudeCodeTools` and pushed, with a PR opened.
