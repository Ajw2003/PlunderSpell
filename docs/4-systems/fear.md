# Fear: the Wick, the Ophan and the quota

**Status: the Wick is built (#417); the rest is planned, not built.** This doc describes the approved plan (Decisions, 2026-10-15, "Build
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
| The Ophan of Hours in the sky (redesigned 2026-10-27) | #418, steps #439-#444 | the Wick's kill path, `OphanDemandTuning` (#433), the raid clock | after #433; a test switch stands in for the quota until #419 |

The Wick and the quota run in two builders at once because they touch different files: the Wick
lives under `Guards/`, the quota under `Lair/`, `Raid/RaidDirector.cs` and the ledger.

## Rules that must stay true

- **The turn is one-way (built, #417).** A Lantern Warden turns when it confirms sight of a player
  (`ChaseState.cs:79`), is hit (`DamageDealt`, `GuardTrueForm.cs:50`), or is woken from Somnus by a cast's noise
  (`GuardHearing.cs:64`); hearing only sends it to investigate. Once turned (`Guard.IsTurned`, a replicated flag,
  `Guard.cs:54`) it stays the Wick for the raid. In either form it is never killed (`GuardTrueForm.LimitDamage`
  leaves one health point); in human form a hard blow to the head (20 damage or more, 1.5 m or higher) stuns it
  for 6 s and it turns when it gets up.
- **The Wick kills on a catch** (the Combat strike deals 100000), sees only inside its own fire's light (6 m all round,
  `GuardSight.SeeOnlyInLight`), stalks at 1.4 m/s and hunts at 4.2 m/s (`TrueFormTuning.BecomeWick`). The dropped
  lantern is a loose networked rigidbody the host spawns (`WardenLanternDrop`), in the same place on every machine,
  left where it fell, dark. Mechanism: `docs/4-systems/alarm.md`, "The Lantern Warden's true form (#417)".
- **The "guard turned" event** (`GuardTurned`, `Alarm/EnemyDirectorEvents.cs`) is raised on every machine at the
  start of the turn (`GuardTurnView`); `AudioDirector` plays the player-down sting on it as a placeholder, and the
  camera and music hook in there.
- **A demand falls due every 3 to 5 raids** (a tuning asset). Each is drawn at random from the
  artifacts of the Age being raided, shown in the Lair's ledger, and paid by placing the artifacts on
  the altar beside the portal. While a demand is unpaid, every raid is hunted.
- **The Ophan in the sky (planned, #418; redesigned 2026-10-27, Decisions).** It never walks, chases or catches. It
  hangs far off in the sky, 150 times its model (about 1.4 km tall) and about 900 m from the castle, eye always on the
  players, and appears `OphanDemandTuning.HuntWindowSeconds` (60 s) before the portal closes, announced by a code-made
  drone and the existing camera shake (#434). The player camera's far clip plane rises to 3000 m so it is drawn whole.
- **Its target and tells (planned).** Its `Eye` bone follows whoever carries the most worth (the carried piece's sell
  value), then down the list. Its rings (`GreatWheel`, `InnerWheel`, `TiltWheel`) spin fastest while charging, half that
  while locking on, slowly while watching, and spin down as it leaves. The lock-on's tell is a quickening pulse of the eye;
  the charge's is the eye swelling to 1.5 times and brightening to full blast.
- **Its beams (planned).** A slow beam, after a 3 s lock-on, drains 25 health a second while no stone lies between its
  eye and the target. A charged beam, after 3 s of the target in cover, charges 3 s, kills in one hit, scorches the room
  it hits (the slow beam then passes through that room's walls) and waits 10 s. A beam on a player carrying a piece
  flagged `IsArtifact` lifts that piece into the sky instead: they live and the Ophan leaves (`HuntSecondsAfterPaid` 0).
  Until #419 exists, a test switch forces a hunted raid. Plan page: `docs/plans/ophan-sky-418/ophan-sky-418.html`.
- **Every machine sees the same thing.** The turn, the lantern, the arrival and each catch are
  decided on the host and shown on every client (the Ophan's target, beams, scorched rooms and takes too); nothing here is polled each frame (`core.md`,
  "When Update is allowed").
