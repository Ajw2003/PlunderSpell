<!-- plain copy of: docs/4-systems/core.md @ 8fc6931dfa739a3b21cc0f1540054ff667beca5b -->

# Core

Full technical doc: [core.md](../../4-systems/core.md)

## What it is
The shared foundation every other part of the game sits on: one shared object per type, a way
for parts of the game to tell each other things happened, and one shape for a state machine.

## Why it matters
Every other system depends on this one, so a bug here spreads everywhere and its cost is paid
by the whole project.

## How it works
1. Each shared system keeps exactly one instance; a second one destroys itself.
2. Systems tell each other about events through one shared messenger, not directly.
3. That messenger tolerates a destroyed listener, and one unsubscribing mid-event.
4. States, like a player's action, are plain objects, testable without a game running.
5. Dev-only tools rebuild a placeholder scene on demand, always starting empty.

## Risks and safeguards
- **Code treating a destroyed object as still usable.** Lookups read it as genuinely absent.
- **A shared system's cleanup being skipped.** Unity can silently replace a base cleanup step, so
  every system must call it explicitly.
- **Rebuilt scenes leaving duplicates.** The dev tools always start from an empty scene first.

## Related
- [Raid](raid.md)

## Left out
File and class names, spell-target interfaces, and exact tooling commands.
