# Post-mortem: a builder sat stuck for about 20 minutes under a stall watch (2026-10-01)

The owner caught this; I did not. The 5-minute stall check from the house rules
(`Ajw2003/AjsClaudeCodeTools#120`) exists to catch exactly this, and it was running.

## What happened

| Time (local) | Event |
|---|---|
| ~07:22 | Builder for #181 started. A `stallcheck.py --watch` Monitor started alongside it. |
| 07:23:09 | The builder's last file edit (`AtmosphereQuality.cs`, `SavedSettingsStartupTests.cs`). |
| ~07:24–07:34 | It started a test run in the background. The run filtered on `Plunderspell.Tests.Audio`, which matches no test, and `run_tests.sh` gave up after 10 minutes: "Timed out after 10 minutes waiting for the test run." |
| 07:33:35 | The builder started a wait loop with no time limit: `until [ $(grep -c "^total" $F) -ge 2 ]; do sleep 5; done`. Only one `total` line would ever appear, so it could never end. |
| — | **STALLED alert 1** (315 s). I checked `git status`, saw uncommitted edits, and told the owner it was "most likely mid-thought or in a long step". No evidence supported that. The edits' timestamps would have shown nothing had changed since 07:23. |
| — | **STALLED alert 2** (301 s). I saw the Editor idle and a test run with 0 tests, and sent the builder a message. SendMessage only delivers at the agent's next tool round, so it could not reach an agent blocked inside a tool call. I said as much and still waited. |
| — | About 7 more "finished" lines, each for another session's agent. I correctly called each one "not ours". While doing so I also said "the stall watch hasn't flagged it, so it's still active", which treats silence as health. |
| ~07:43 | The owner asked me to check in. A process listing found the wait loop in 4 minutes. |
| ~07:45 | With the owner's approval I stopped the loop. The builder finished and reported within minutes. |

Stuck from 07:33 to 07:45 in the wait loop, and making no useful progress from about 07:24. The
watch fired twice; the rest of that time was silent.

## Why the stall watch didn't save it

Source: `scripts/stallcheck.py` in the house-rules plugin.

1. **It alerts once, then goes quiet.** `--watch` prints a row only when its state changes
   (`seen.get(row[0]) != row[1]`). An agent that stays STALLED is reported once and never again.
   "Check every 5 minutes" turns into "say it once". Its second alert only happened because the
   transcript was written in between (my message), which briefly flipped the state to `ok`.
2. **It watches every session's agents, not this one's.** `_subagent_files` globs
   `~/.claude/projects/*/*/subagents/agent-*.jsonl`: every project and every session touched in
   the last 3 hours. Most lines it printed were about other sessions' agents. That noise trained
   me to read its lines as "not ours, ignore".
3. **Its guidance leans the wrong way.** The rule and the hook say "do not assume the subagent
   died". Nothing says what looking means. I read it as "assume it's alive" and checked the wrong
   things: whether files exist, and the agent list's `running` flag. The right checks are the
   transcript's age, the agent's last tool call, and any child process still running.

## My mistakes, separate from the tool

- On a STALLED line I looked for reasons it might be fine instead of finding what it was blocked
  on. The blocking process took one process listing to find.
- I told the owner things I had not checked ("most likely mid-thought", "hasn't been flagged, so
  still active").
- I relied on SendMessage to unstick an agent that could not receive it, knew that, and waited
  anyway.
- The builder's prompt didn't forbid waits with no time limit, so the builder wrote one. The
  house rules already ban hidden or blind wait loops for the main session; the builder needed the
  same rule spelled out.

## Fixes filed

The fixes are issues in `Ajw2003/AjsClaudeCodeTools`, linked to #120: #122 (re-alert while still stuck), #123 (watch only this session's agents), #124 (what to do on a STALLED line), #125 (no waits without a time limit).
