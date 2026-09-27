# Project preferences

## Documentation

Start at [`docs/1-landing/README.md`](docs/1-landing/README.md) — it indexes the roadmap, current project state,
per-system docs, and everything else worth reaching. Don't duplicate its content here.

## Commit everything — paper trail and asset backup take priority

The user wants a durable record of all work in this repo: every asset,
tool, and generated artifact produced during a session should be
committed and pushed, not left as local-only or treated as disposable.

- **Default to committing, not gitignoring.** Don't add something to
  `.gitignore` just because it's "regenerable," a build output, or a
  rendering/preview of something else already committed (e.g. screenshots
  of a 3D asset, a baked texture, a contact sheet). If it was produced as
  part of the work, it belongs in git.
- **Only ignore genuine build/tool noise**: things like `__pycache__/`,
  editor caches, OS cruft, or intermediate files that get regenerated on
  every single run and carry no information not already in a tracked
  source file. When in doubt, commit it rather than ignore it.
- **Push, don't just commit locally.** A commit sitting unpushed in a
  container that can be reclaimed is not a backup.
- If you're about to add a path to `.gitignore`, pause and ask: "does
  this need to survive if the session/container disappears?" If yes (any
  asset, screenshot, export, doc, or tool the user or a reviewer might
  want later), it should be tracked, not ignored.
