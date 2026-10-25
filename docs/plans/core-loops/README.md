# Core loops: research, analysis, proposal, delegation

**Status: stages 1 and 2 done 2026-10-25. The owner's direction is in [`owner-direction-2026-10-25.md`](owner-direction-2026-10-25.md); two of the page's choices (after the Ophan, campaign length) are still open before stage 3.**

The owner asked (2026-10-25) to research, plan, propose and then build PlunderSpell's primary,
secondary and tertiary loops: enemies with unique attacks and counters in the manner of Lethal
Company (starting from the Wick), intermittent reward, progression, a Market and economy that work,
perhaps some gambling, and what recent viral indie games did that PlunderSpell lacks. The work runs
in four stages, each approved before the next:

| Stage | Output | Status |
|---|---|---|
| 1. Raw research | `research/` (below) | done |
| 2. Analysis and an easy-to-read page | `analysis/analysis.md`, page `analysis/analysis.html` (published as https://claude.ai/artifact/4Mebn1titQFAEKqqpheaXT) | done; four choices wait on the owner |
| 3. Proposal page: loops, enemy roster, progression, economy, gambling, timeline, four workstreams | `proposal/` | waiting |
| 4. Delegation across the owner's four accounts, then building | issues and branches | waiting for approval of stage 3 |

## Stage 1 files

- [`research/repo-current-state.md`](research/repo-current-state.md): what the game has today, by branch.
- [`research/indie-hits-raw.md`](research/indie-hits-raw.md): sales, players, loops and reasons for spread of ten games.
- [`research/enemy-designs-raw.md`](research/enemy-designs-raw.md): every Lethal Company and R.E.P.O. monster's sense, attack and counter.
- [`research/reward-and-gambling-raw.md`](research/reward-and-gambling-raw.md): reward psychology, quotas, gambling shapes, rating risks.
- [`research/how-to-fish-raw.md`](research/how-to-fish-raw.md): How to Fish (10 M+ in under two months), Gamble With Your Friends, Webfishing and Dredge.
- [`research/progression-and-retention-raw.md`](research/progression-and-retention-raw.md): player counts over time, endings, Cookie Clicker, Scritchy Scratchy, roguelite progression.
- [`research/content-mods-raw.md`](research/content-mods-raw.md): the most-downloaded content mods for Lethal Company, R.E.P.O. and Minecraft.
- [`research/sources.md`](research/sources.md): every page read.

## Stage 2 files

- [`analysis/analysis.md`](analysis/analysis.md): the analysis in full.
- [`analysis/analysis.html`](analysis/analysis.html): the page, built by `make_page.py` from `analysis/analysis.page.json`.
- [`analysis/mods-analysis.md`](analysis/mods-analysis.md): what the content mods teach.
- `analysis/charts/`: the four charts (with mods.svg) as SVG and PNG (retention, sales, the three clocks of reward).
