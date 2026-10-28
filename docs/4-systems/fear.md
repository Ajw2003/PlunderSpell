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
- **The Ophan in the sky (#418, built on `AjsAgent/ophan-arrival-418`; redesigned 2026-10-27, Decisions).** It never walks, chases or catches. It
  hangs far off in the sky, 150 times its model (about 1.4 km tall) and about 900 m from the castle, eye always on the
  players, and appears `OphanDemandTuning.HuntWindowSeconds` (60 s) before the portal closes, announced by a code-made
  drone and the existing camera shake (#434). The player camera's far clip plane rises to 3000 m so it is drawn whole.
  It holds `ophan_spread` after `ophan_arrive` (#443): the hover's wheels and eye with all six wings spread, 13.2 m
  wide by 7.8 m tall on the model (`OphanPrefabForge`, `Tools/ArtForge/anim_ophan.py`).
- **Its target and tells.** Its `Eye` bone follows whoever carries the most worth (the carried piece's sell
  value), then down the list. Its rings (`GreatWheel`, `InnerWheel`, `TiltWheel`) spin fastest while charging, half that
  while locking on, slowly while watching, and spin down as it leaves. The lock-on's tell is a quickening pulse of the eye;
  the charge's is the eye swelling to 1.5 times and brightening to full blast.
- **Its beams.** A slow beam, after a 3 s lock-on, drains 25 health a second while no stone lies between its
  eye and the target. A charged beam, after 3 s of the target in cover, charges 3 s, kills in one hit, scorches the room
  it hits and cuts a hole along its path (the slow beam passes only through the hole; #467) and waits 10 s. A beam on a player carrying a piece
  flagged `IsArtifact` lifts that piece into the sky instead: they live and the Ophan leaves (`HuntSecondsAfterPaid` 0).
  The host despawns the piece a second later; first every machine hides it, flashes a beam to it and raises a copy of
  its meshes up the beam to the eye over `LeaveSeconds`, growing so it still reads far up (`OphanLook.Lift`). A new
  target (the last one died or was outworth) always gets the full lock-on: `OphanHunt.Retarget` drops a lock-on, drain
  or charge on the last one back to watching. The slow beam is 40 m wide at the eye and 0.8 m at the target and fades out over
  its last 2 m, or its last 25 m on the target's own machine, where a camera-facing line that close would fill the view
  (`OphanLook.DrawBeam`). The co-op check is `Tools/Unity/ophan_check.sh` (guards removed, hunts switched off so it brings the Ophan itself).
  Until #419 exists, a test switch forces a hunted raid. Plan page: `docs/plans/ophan-sky-418/ophan-sky-418.html`.
- **The Ophan Gym (#451, planned 2026-10-27, being built on `AjsAgent/ophan-gym-418`).** A hand-test copy of the main level,
  `Assets/_Project/Scenes/OphanGym.unity`, rebuilt from `RaidScene.unity` by *Tools > Plunderspell > Build Ophan Gym* so it
  never goes stale; the only addition is one `OphanGym` object, and the real game never loads it. In the gym the raid lasts
  10 minutes, the Ophan hunts every raid and arrives 30 s in, and one artifact is always placed in the castle under a pillar
  of light with a countdown on screen: reach it in time and the Ophan takes it and spares you, miss it and it hunts you.
  F7 summons it again, F8 stops damage to the local player, F9 removes the guards, F10 cycles time (1, 0.5, 0.25); a client's
  key asks the host. `bash Tools/Unity/coop_local.sh --gym` plays it in co-op with its own client in `Build/OphanGym`.
  It reuses the Ophan's own `ArriveNow` and the `OphanOfHours.HuntOverride` switch; no Ophan code changes for it.
  Plan page: `docs/plans/ophan-gym-plan.html`. Built and proved in co-op 2026-10-27; F8 is a `Damage.Untouchable` hook.
- **Beam fixes (#459, approved 2026-10-27, being built on `AjsAgent/ophan-gym-418`).** Found playing: a flat
  camera-facing ribbon from the eye is end-on to anyone looking up at the Ophan, so the beams showed only as a sliver by
  the eye. Both beams become a glowing tapered shaft (a tube mesh), which reads end-on as light closing in around the eye
  (#460). The charged beam bursts a glowing hole in each wall in its path on every machine and the kill lands 0.5 s
  later (#461); the hole is visual only, walls stay solid. A Lantern Warden before its turn could stand half in the
  floor on the main floor (#462). Options page: `docs/plans/ophan-beam-fixes.html`.
- **Ophan bugs and solid beams (approved 2026-10-27, being built on `AjsAgent/gym-with-demand-page`).** Its target is
  always the living player carrying the most worth, then down the list, at any range and never chosen by sight; sight
  only picks the attack. Seen, it locks on and drains with the slow beam; behind stone (walls and roofs alike) for
  `CoverSecondsBeforeCharge`, it charges, from watching as well as from draining. The spectator camera copies the player
  camera (far clip 3000 m) so the whole Ophan draws while down, on host and client. The slow beam is a bright opaque core
  inside the soft shaft. Through the 3 s charge a thin beam runs from the eye to the target, following them and growing
  wider and brighter until the blast. The charged beam cuts a hole for real: the host sends the rooms and the beam's line,
  and every machine removes the triangles of those room meshes within 1.5 m of the line from both the drawn mesh and its
  collider. Scorched rooms no longer let the slow beam through whole; only the cut hole does. This reverses pick 9A (Decisions, 2026-10-27). Plan page: `docs/plans/ophan-bugs-and-beams.html`.
- **Ophan redo after QA (approved 2026-10-27, `docs/plans/ophan-redo-plan.html`).** aj's QA (recording in
  `docs/generated/ophan-bugs-463/qa-recording/`) found every hunt ending in death within 11 s and the beam coming from
  overhead. The beam aims at the chest (pivot plus 0.3 m), not 2.1 m up, and the host's cover test, the cut and the drawn
  beam all start from the eye bone. The beams keep their look. The charged beam fires twice: the first shot breaks the
  stone on its line and deals no damage; after `OphanTuning.SecondShotDelaySeconds` (default 1 s) the second re-aims at
  the target, breaks any new stone on its line, and kills. Destruction is aj's fracture system from
  `Projects/ufo-3-rag-dahl/Assets/FractureSystem`, networked: the host sends the room, the part, the beam's line and the
  radius, and every machine runs the same hit on the same baked graph, so colliders match; debris is a per-machine look.
  Each room is split into wall, roof, floor and props in Unity when its fracture data is baked (pick 2A), checked on a
  review sheet. `OphanWallCut` is removed once the port works.
- **Every machine sees the same thing.** The turn, the lantern, the arrival and each catch are
  decided on the host and shown on every client (the Ophan's target, beams, scorched rooms and takes too); nothing here is polled each frame (`core.md`,
  "When Update is allowed").
