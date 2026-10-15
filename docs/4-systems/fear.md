# Fear: the Wick, the Ophan and the quota

**Status: planned, not built.** This doc describes the approved plan (Decisions, 2026-10-15, "Build
plan approved: the Wick and the Ophan in raids"). Each section is rewritten from the code as its
piece lands. Design: `docs/7-design/fear-pass.md`. Plan page:
`docs/plans/fear-entities/fear-entities-in-game.html`. Debt inventory:
`docs/plans/debt-to-quota/retiring-the-debt.html`.

## Pieces and order

| Piece | Issue | Builds on | Order |
|---|---|---|---|
| The Lantern Warden turns into the Wick | #417 | guard state set (`Guards/States/`), `EventManager` | first, at the same time as the quota |
| One more guard per ground-floor zone, every Age | filed with this plan | guard placement (`raid.md`, `alarm.md`) | with the Wick |
| The artifact quota replaces the debt and the Collector | #419 | `LairHubManager`, `LairEvents`, the ledger | first, at the same time as the Wick, once the debt list is approved |
| The altar beside the Lair's portal | filed with this plan | the art pipeline | before the quota can be paid in the Lair |
| The Ophan of Hours arrives, hunts and catches | #418 | the Wick's kill and event, the quota's hunted mark | last, one pull request, merged only when complete |

The Wick and the quota run in two builders at once because they touch different files: the Wick
lives under `Guards/`, the quota under `Lair/`, `Raid/RaidDirector.cs` and the ledger.

## Rules that must stay true

- **The turn is one-way.** A Lantern Warden turns when it confirms sight of a player, is hit, or is
  woken by a missed Somnus; hearing only sends it to investigate. Once turned it stays the Wick for
  the raid. In human form it is never killed: a hard blow to the head stuns it.
- **The Wick kills on a catch**, sees only inside its own fire's light, stalks at 1.4 m/s and hunts
  at 4.2 m/s. The dropped lantern is a loose physics object, in the same place on every machine.
- **A demand falls due every 3 to 5 raids** (a tuning asset). Each is drawn at random from the
  artifacts of the Age being raided, shown in the Lair's ledger, and paid by placing the artifacts on
  the altar beside the portal. While a demand is unpaid, every raid is hunted.
- **The Ophan's hunt.** It arrives 90 s before the portal closes at 2.5 m/s and reaches the players'
  speed (5 m/s, `RaidPlayer.prefab`) by 30 s left. It passes through walls and cannot be hidden from.
- **How a hunted raid ends.** Caught empty-handed: killed. Caught carrying the wanted artifact: it
  takes the artifact and the player lives. Reaching the portal first: the player lives and keeps the
  whole haul.
- **Every machine sees the same thing.** The turn, the lantern, the arrival and each catch are
  decided on the host and shown on every client; nothing here is polled each frame (`core.md`,
  "When Update is allowed").
