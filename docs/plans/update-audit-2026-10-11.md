# Update audit (2026-10-11)

**Why.** On 2026-10-11 an agent saved the Lair pile from a polling check in `Update`; the poll read a torn-down pile
as empty and wiped the save (#378). The owner asked for every per-frame method in the codebase to be audited against
the rule: `Update`, `LateUpdate` and `FixedUpdate` are only for work that genuinely runs every frame. Changes go
through `EventManager` on a need-to-know basis, timing through coroutines or scheduled calls, and the architecture is
MVC (views fed by events), singletons, factories and SOLID. Read-only; nothing was changed.

**Scope.** All 66 runtime scripts that have a per-frame method, every editor script (none has one), and the tests that
wait by polling. Details, with `file:line` for every row:
- [Part 1](update-audit-2026-10-11-part1.md): the first 33 runtime files.
- [Part 2](update-audit-2026-10-11-part2.md): the other 33 runtime files, and the tests.

## What it found

| Bucket | Part 1 | Part 2 | Total | Meaning |
|---|---:|---:|---:|---|
| KEEP | 14 | 22 | **36** | Truly per-frame: movement, physics, camera, fades, animation |
| EVENT | 10 | 35 | **45** | Watches for a change every frame; should listen for an event |
| TIMER | 6 | 11 | **17** | Counts time; should be a coroutine or scheduled call |
| REMOVE | 4 | 1 | **5** | Dead, redundant or debug-only |
| **Methods or parts** | 34 | 69 | **103** | |

Tests that wait by polling or by a fixed delay that hopes something happened: **26** (part 2).

So about two thirds of the per-frame code (67 of 103) does not need to run every frame.

## A bug found on the way

`Player/PlayerInputController.cs:23` subscribes to `PlayerIdleEvent` with `EnableAllInputs`, and
`EnableAllInputs` (`:227`) adds the jump, attack, look and other handlers with `+=` every time, without removing the
earlier ones. Each time the player goes idle the handlers stack, so one key press can fire several times; the
subscription is also never removed. Found by reading; not yet run.

## Where it goes next

- The rule is written into the project (`docs/4-systems/core.md`, "When `Update` is allowed") and into the house-rules
  plugin (Ajw2003/AjsClaudeCodeTools#218).
- The fixes go one system at a time, each its own issue under one parent, starting with the Lair pile (#378) done with
  events.
