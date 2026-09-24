# Net

How a session starts, how a friend joins it over Steam, and how each machine gets its own body.
Code: `Assets/_Project/Scripts/Runtime/Net/` (`RogueAi.Net`), plus the networked paths in
`RaidDirector`, `PlayerStateMachine` and the menus. Plan and survey:
[`docs/plans/steam-coop-raid.md`](../plans/steam-coop-raid.md).

## How it works

<!-- ref:4e69 -->
### Every session runs through PurrNet, solo included

`CoopSession` (on the `Network` object in `RaidScene`, beside PurrNet's `NetworkManager`, its three
transports and a `PlayerSpawner`) is the one place a session starts and ends. The raid therefore
has a single code path:

| Menu button | Transport | Who can join |
|---|---|---|
| Play Solo | `LocalTransport` | nobody |
| Host Co-op, Steam running | `SteamTransport` (peer-to-peer relay) | Steam friends you invite |
| Host Co-op, no Steam | `UDPTransport`, port 5000 | the local network |

The `NetworkManager` never starts itself (both start flags are `None`); `CoopSession` picks the
transport and calls `StartHost` / `StartClient`. `ICoopSession` in `Plunderspell.Core` is what the
menus see, so the UI assembly references neither PurrNet nor Steamworks; `GameServices.Coop` is
null in the bench scenes, which stay offline.

Ending one session and starting another (hosting from inside a solo session, accepting an invite
mid-game) waits for PurrNet to report both sides stopped before starting again
(`CoopSession.LeaveThenRetry`); starting in the same frame as stopping left the server down.

### Steam

`SteamBootstrap` starts Steamworks before the first scene loads, pumps `SteamAPI.RunCallbacks`
every frame and shuts down on quit. Outside Steam it reads the App ID from `steam_appid.txt` (480,
Spacewar): at the project root in the Editor, and next to the exe in a build, where
`PlayerBuilder` copies it. It logs one line: signed-in persona, app, and whether the overlay is on.

Hosting creates a friends-only lobby (4 players), sets rich presence `connect` to
`+connect_lobby <id>`, and starts the host on `SteamTransport`. A friend joins through any of:

- an invite accepted while the game runs (`GameLobbyJoinRequested_t`);
- "Join Game" in the friends list while it runs (`GameRichPresenceJoinRequested_t`);
- either of those while the game is closed, which launches it with `+connect_lobby <id>`.

Each enters the lobby; its owner is the host, and the client connects to the owner's Steam ID.
No addresses or ports are involved.

**Inviting.** The Lair's Invite Friend button opens Steam's invite dialog when the overlay is
hooked in. When it is not (see Traps), it shows the online friends list instead, and a click sends
`InviteUserToLobby`. The invite arrives in the friend's Steam chat.

### One body per connection

The player is not placed in the scene. `PlayerSpawner` instantiates `RaidPlayer.prefab` for every
connection (four spawn points in the Lair) and gives the connection ownership.
`PlayerNetworkOwnership` then decides, on each machine, whether that body is "mine":

- **Mine:** input, camera, microphone (`PushToCastController`) and `PlayerStateMachine` on, a
  dynamic rigidbody, and `PlayerStateMachine.ClaimLocal()`.
- **Everyone else's:** all of those off, a kinematic rigidbody, and it follows `NetworkTransform`
  (owner-authoritative).

`PlayerStateMachine.Local` is decided by ownership alone on a networked body
(`LocalDecidedByNetwork`). The HUD, `ItemManager`'s camera and `RaidDirector`'s spawn placement
all read it lazily, because the body appears after they wake.

### One raid, built on every machine from one seed

The host runs the raid. `RaidDirector` replicates the phase and the seed. A client builds the
castle geometry locally from the seed (only the seed crosses the network) as soon as it has both
(`OnPhaseReplicated`, `OnSeedReplicated` and a per-frame fallback, since the two can arrive in
either order), then moves its own body to the gate. The host's loot and guards arrive as network
objects. A client follows the host from the Lair into the raid (`RaidBootstrapper.OnPhaseChanged`)
and cannot set out itself; the Lair shows it "Waiting for the host to set out."

Each machine places only its own player, offset around the gate by owner number, so two bodies
are never placed inside each other. The placement moves the rigidbody as well as the transform.

### Guards, loot and carrying

Every enemy and loot prefab carries a `NetworkTransform`, so a client sees guards walk and loot
move as the host simulates them. A client's guards switch their own `NavMeshAgent` off
(`CastleGuard.OnSpawned`); the server's copy runs the AI.

To carry loot a machine must control its transform. `Item.CanDriveHere` and `Item.RequestDrive` are
installed by `NetworkCarry`: grabbing a piece this machine does not control asks the server
(`LootPickup.RequestCarry`), which hands over ownership, and `ItemManager` starts the drag once it
is granted. The carrier then simulates the piece and its movement replicates, so the host's
extraction pad sees it land. Only the machine simulating a piece judges its impacts; a client
carrier asks the server to break it (`LootPickup.RequestBreak`). Weapons have no
`NetworkTransform`: each machine has its own copies, and a friend does not see yours move.

### Damage

`Damage.Apply` offers every hit to `Damage.Forward`, which `DamageRelay` (on the `Network` object)
installs while spawned. A hit goes where the target's health lives:

- a guard, loot or anything else the server spawned: the server;
- a player's body: that player's own machine, where their health, HUD and death are.

A client hitting a guard sends the hit to the server (`HitOnServer`), which applies it and sends
the result back (`TellHitter`, then `Damage.ReportRemote`), so the hitter still sees the number.
The host's guards hitting a friend's body send it to the friend (`HitOwnedBody`). `DamageKind`
travels as an `int` for the same reason `CastVolume` does.

A client's spell carries its origin and direction in the cast RPC (`ServerCast`), because the host
never sees a friend's camera pitch.

### Spectating and a party wipe

Each body replicates an owner-set "down" flag (`PlayerNetworkOwnership._isDown`). When this
machine's player dies and a teammate is still standing (`PlayerStateMachine.SpectateOnDeath`), the
"You died" screen is skipped; a `SpectatorCamera` follows the teammate's eye, whose rotation
replicates through a `NetworkTransform` on the player's `Eye` (remote cameras are disabled
components on an active object for exactly this). When the server sees every body down it tells
every machine (`PartyDown`), and all of them go to "You died". A dead player revives on entering the
next raid, from the Lair or from that screen, and a client follows the host from either.

### The host's campaign on a friend's Lair

`RaidDirector` replicates the host's debt, bank and last haul; a client shows them with
`LairHubManager.ShowHostCampaign`, which does not save, and reloads its own campaign when it leaves
the session. A friend's own save is never touched by joining.

## Traps

- **The Steam overlay is off when the game is not started by Steam.** Steam hooks its overlay into
  a process it launches; a build started by double-clicking the exe, or the Editor, reports
  `overlay off`, Shift+Tab does nothing, and `ActivateGameOverlayInviteDialog` shows nothing. The
  Lair's friends list exists for exactly this case.
- **With App ID 480, "Join Game" and accepting an invite while the game is closed launch
  Spacewar,** the app that ID belongs to, not Plunderspell. A friend must have Plunderspell open
  when they accept. A real App ID removes this.
- **Ownership callbacks fire twice on a host,** once as the server and once as its own client.
  Ownership is only applied from the client-side call; the server-side one would switch the host's
  own body off.
- **A client sees ownership after the spawn,** so its body first looks remote and is switched back
  on by `OnOwnerChanged`.
- **An interpolated rigidbody overwrites a transform-only move** on the next physics step. Moving a
  player means setting `Rigidbody.position` too.
- **RPC arguments must be types PurrNet's code generation registered.** `CastVolume` (Voice
  assembly) failed to pack, silently dropping every networked cast, so the cast RPCs carry it as a
  `byte`.
- **Setting a SyncVar on a client is refused** with an "Invalid permissions" error. `Awake` runs
  before spawn, so `isSpawned` cannot tell a client there; check
  `NetworkManager.main.isClientOnly` (see `ExtractionZone.ResetClock`).
- **Windows Firewall asks about `Plunderspell.exe`** the first time it hosts or joins over UDP.
  Localhost works either way, and Steam traffic goes through Steam.
- **`SteamInviteGateway` is unused.** It drove PurrLobby's `SteamLobbyProvider`, which no scene
  contains; `CoopSession` replaced both.

- **The host can finish one raid and start the next in a single frame,** so a client may never see
  the Lair phase in between. The client rebuilds whenever the replicated seed differs from the one
  its castle came from (`RaidDirector.NeedsClientBuild`), not when its castle is null.
- **Standing on the extraction pad starts the leave countdown.** A test that parks a player on the
  pad ends the raid within seconds, with whatever is on it.

## Testing it

On one machine without Steam: host from the Editor (Play → Host Co-op), then run the build with
`-coop-join 127.0.0.1` (and `-screen-fullscreen 0 -screen-width 960 -screen-height 540` for a
window). The reverse also works: the build with `-coop-host` hosts. Each side logs `[Coop]` and
`[Raid]` lines: who it plays as, the seed arriving, the castle built, where its player was placed.

To drive the client too, build a Development player with the Pipeline runtime on
(`set_runtime_pipeline_settings enableInBuilds true`, build to `Build/DevTest` with the
`Development` option, then set it back to false) and send it commands with
`unity command --runtime Plunderspell eval`. Its code sees the game's types only through
reflection. Never ship a build with the runtime on. Building through the Pipeline's `build` command leaves the Input System's
actions asset in `ProjectSettings.asset`'s `preloadedAssets`; revert that line before committing.

Screenshots of each checked step: `docs/generated/coop-2026-09-23/`.
