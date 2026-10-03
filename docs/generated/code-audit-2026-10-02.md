# Code size and over-engineering audit, 2026-10-02

Read-only recon. Nothing was edited or deleted. Line counts are `wc -l` per tracked `.cs` file
(blank lines and comments included), taken on branch `claude/playability-fixes`.

## 1. How big is the C# code base

Total: **~136k lines in 846 tracked `.cs` files.**

| Lines | Files | Who owns it |
|---:|---:|---|
| 70,627 | 367 | PurrNet 1.15.0 (third party, vendored) |
| 3,062 | 30 | PurrLobby (third party, vendored) |
| 258 | 2 | Unity `TutorialInfo` boilerplate |
| **36,360** | ~290 | **Our runtime game code** |
| 12,959 | 78 | Our tests |
| 8,228 | 43 | Our editor tools |
| 4,289 | 22 | `Tools/` (headless shims, Unity eval scripts) |
| 742 | 18 | Stray scripts in `docs/generated/` |

Written by us: **~57.5k** (runtime + tests + editor), **~62.6k** with `Tools/` and the stray scripts.
Of the 36.4k runtime lines, ~2.5k are dead (section 3), leaving **~34k live game code**.

> Correction: an earlier chat message said runtime game code was ~45.7k. The folder sums below
> add up to 36,360. The 45.7k figure was wrong.

### Our runtime code by folder (`Assets/_Project/Scripts/Runtime`)

| Lines | Folder | Lines | Folder |
|---:|---|---:|---|
| 4,643 | UI | 1,115 | Core |
| 4,391 | Guards | 651 | Enemies |
| 4,054 | Castle | 544 | Acoustics |
| 3,158 | Audio | 490 | Extraction |
| 2,419 | Spells | 432 | Playtest |
| 2,216 | Items | 231 | Status |
| 2,078 | Atmosphere | 209 | Lair |
| 2,011 | Raid | 130 | Inventory |
| 1,943 | Player | | |
| 1,706 | Alarm | | |
| 1,426 | Voice | | |
| 1,288 | Net | | |
| 1,225 | Loot | | |

## 2. How much of PurrNet is ours

Almost none. `git log` on `Assets/PurrNet/**/*.cs` shows two commits, both 2025-10-14
(`41047503` import, `479d685c` "actually make it work lol"). Diffing the import against HEAD shows
one changed file, `Runtime/Transports/PurrTransport.cs`, **4 lines** (the default `_masterServer`
URL and `_region`). Everything else is upstream 1.15.0.

Our own network code is `Scripts/Runtime/Net` (1,288 lines) and uses PurrNet without editing it.

Which PurrNet transports the game uses (from scenes, prefabs and code):

| Transport | Used | Pulls in |
|---|---|---|
| `UDPTransport` | yes | LiteNetLib, 10.2k lines (keep) |
| `SteamTransport` | yes | Steam addon, ~1k lines (keep) |
| `LocalTransport` | yes | nothing extra |
| `WebTransport`, `PurrTransport` | no | `SimpleWebTransport`, 2.8k lines (best removal candidate, but it would diverge from upstream) |

Editor-only profiling tools (`ProfileBandwidth`, `ProfileDeltas`) add ~1.4k lines.

## 3. Already dead

**Marked `[Obsolete]` (2,051 lines in 11 files)**, listed in `docs/reference/deprecated-code.md`:
`MonsterStateMachine` and its seven `Monster*State` classes, `MonsterState`, `HealthBar`,
`PlayerInteractState`, and `CastleGuard` (1,317 lines, replaced by `Guard` in #214).

**Never referenced by anything else (615 lines in 11 files)**, matched by type name and by Unity
script ID against all code, prefabs, scenes and assets:
`AudioLatencyProbe` (284), `SteamInviteGateway` (106), `AudioGapProbe` (88), `HealthBar` (75),
`NetworkTypes` (16), `PlayerInteractState` (11), and five player-input events (walk, sprint,
jump, interact, dodge; 7 lines each).

Caveat: the reference scan cannot see reflection, `Resources.Load` or string-name lookups.

## 4. Over-engineering findings (ponytail audit)

Ranked, biggest cut first. Tags: `delete` dead, `yagni` unused abstraction, `shrink` same logic
in fewer lines.

1. `delete:` Old monster state machine, `HealthBar`, `PlayerInteractState`. ~700 lines. Already obsolete-tagged.
2. `delete:` `CastleGuard` (1,317) and `GuardPrefabSwapTool` (121). Delete once old prefabs are migrated.
3. `delete:` Unused packages: `com.unity.timeline`, `com.unity.visualscripting`, `com.unity.multiplayer.center`. No references in code, scenes or prefabs; no `.playable` assets.
4. `yagni:` `Tools/Headless` (3.5k lines of Unity/PurrNet stand-ins) plus `Tools/Unity/eval` (679). Only needed if `verify.sh` is still run. If not, delete.
5. `delete:` Scratch scripts in `docs/generated/` (18 files, 742 lines). Keep as text if wanted, not `.cs`.
6. `delete:` Never-referenced types from section 3. ~520 lines.
7. `shrink:` The Windows audio-device COM declarations (`IMMDevice`, `IPropertyStore`, enumerator) are duplicated in `AudioOutputDevices.cs` and `MicrophonePicker.cs`. One shared class saves ~60 to 80 lines.
8. `yagni:` One-implementer interfaces: `ICoopSession`, `ISpellEffect`, `IEavesdropper`, `ICarryableCreature`, `IChokeDamageSource`, `IPortalResting`. Check whether tests fake them before cutting. ~120 lines.
9. `yagni:` `ISpellTargets.cs`: six interfaces with one implementer each, `IOpenable` with none.
10. `yagni:` `VoiceServiceLocator` (99) and `DownedPlayerCarryAdapter` (180) wrap a single backend or case. Review before cutting.
11. `yagni:` Packages probably not needed: `com.unity.collab-proxy` (project uses git) and `com.unity.pipeline` (experimental 0.7). Not referenced in code.
12. `shrink:` Editor forges (8.2k lines; `EraContentForge` 831, `NightAtmosphereForge` 720, `RaidSceneBuilder` 454, plus five screenshot forges, ~1k). Possible shared helper. **Lead only: these files were not read.**

Not flagged: `UIFactory.cs` (707 lines) is 30 static methods called from 9 files, so a large helper
and not a one-product factory. `PlayerInputs.cs` (850) is Unity's generated input file.

**Net: about -5,900 to -7,000 lines and -5 packages possible**, out of ~62.6k written by us.

## 5. For review: decisions needed

- Is `Tools/Headless` still used? (item 4, ~4.2k lines with `Tools/Unity/eval`)
- Are the old guard prefabs migrated, so `CastleGuard` can go? (item 2, ~1.4k lines)
- Do tests fake `ICoopSession` or `ISpellEffect`? (item 8)
- Is it worth diverging from upstream PurrNet to drop `SimpleWebTransport`? (section 2; recommendation: no)
- Remove the 3 unused packages, plus `collab-proxy` and `pipeline`? (items 3 and 11)
- Related open issue: #230 (mark dead code as deprecated).
