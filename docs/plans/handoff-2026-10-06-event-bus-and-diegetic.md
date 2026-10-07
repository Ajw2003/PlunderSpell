# Handoff 2026-10-06 (night): event bus move, then diegetic raid UI, Lair, Market

**For:** a fresh agent on aj's PC (Unity Editor open on this repo). **Branch:** `claude/staging-2026-10-07`, clean and
pushed at `8434f4ee`. Read first: `CLAUDE.md`, `docs/1-landing/README.md`, this file.

## Where things stand

- Push to GitHub was fixed: a 470 MB model in `Assets/StreamingAssets/LLM/` was removed from unpushed history and
  added to `.gitignore` (still on disk, needed locally). Local branch `backup/pre-lfs-fix-2026-10-06` still holds the
  old commits **with the model** — never push it; aj has not yet said whether to delete it.
- Leftover branches merged and the Unity results: top entry of `docs/5-today/Today.md`. Unity baseline on staging:
  EditMode 225 pass / 3 known failures (`ArtAssetImportTests`, `LootAmountTests...AboutDoubleTheOldHaul`,
  `LootBalanceTests...TooHeavyToLift`) / 3 skip; PlayMode 413/413 before the lifetime test was added; that test passed alone, the full suite was not re-run with it.
- aj's decisions on the event bus: `docs/6-decisions/Decisions.md` (2026-10-06) and "Decisions (settled)" at the end
  of `docs/plans/ui-events-mvc.md`. In short: every event between systems moves to `EventManager`; a component
  talking to its own object stays a direct call; continuous values (mic level, chant progress, input
  performed/cancelled) publish on every change; the mic sensitivity meter moves to Settings; always unsubscribe on
  close / state change / disable / quit.
- Issues: parent Ajw2003/PlunderSpell#297, steps #298-#305 (the issue bodies hold each step's file list).
  **#298 done** (`aff60541`, merged): bus lives from start-up to quit, unsubscribe without reflection,
  `SubscriptionCount`/`TotalSubscriptionCount`, tests `EventManagerTests` (EditMode) and
  `EventManagerLifetimeTests` (PlayMode). Described in `docs/4-systems/core.md`. Not closed: closing asks aj.
- `docs/plans/handoff-2026-10-07-staging-and-ui-events.md` §2 (compile + tests) is done; its §3 manual Unity checks
  (mic switch, Profiler checks, prefab field values) are not.

## Next

1. **#299**: `Alarm/EnemyDirectorBus.cs` + `EnemyDirector.cs:73,133-148` onto `EventManager`; guards report to the
   director by event (#252). Then #300, #301, #302, #303, #304, #305 in order.
2. Then diegetic raid UI (phase 1 of `docs/plans/diegetic-ui-lair-market.md`; grimoire and watch stay flat UI), then
   the Lair room, then the Market (`docs/plans/lair-market-in-engine.md` steps 2-8). Each needs a parent issue plus
   one child per step before code (house rule). aj chose this order.

## How aj wants the work done (learned this session)

- **Edit in place. Never overwrite a whole existing file**, above all core infrastructure. aj stopped a builder that
  was rewriting `EventManager.cs` into a plain C# class. Keep the class's shape and API; Write only for new files.
  Every builder prompt must say so (memory `edit-in-place-never-rewrite`).
- One branch per issue off staging (`claude/<topic>-<issue>`), small commits ending `Committed by AJ's agent`,
  `Refs #N`, push, then `--no-ff` merge into staging and push. Report commits as GitHub links.
- Verify in the real Editor: check `False False` for Play/compiling first, `bash Tools/Unity/recompile.sh`,
  `git diff --ignore-cr-at-eol --stat Assets/_Project/Net/NetworkPrefabs.asset` must be empty,
  `bash Tools/Unity/run_tests.sh <Class> EditMode|PlayMode`; full suites once per step, not repeatedly.
- Builders must work in the main checkout, not a worktree: the Editor is pinned to this folder.

## Suggested skills

- `unity:unity-cli` — driving the open Editor (recompile, tests, eval).
- `superpowers:test-driven-development` — each event move: test the subscribe/unsubscribe and delivery first.
- `mattpocock-skills:codebase-design` — where event structs live and how listeners filter by sender.
- `superpowers:verification-before-completion` — before reporting any step done.
- `ponytail:ponytail` — smallest in-place diff per step.
