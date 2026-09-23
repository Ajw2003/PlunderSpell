# Steam co-op in the shipping raid

Requested 2026-09-23: "fix steam networking such that I can actually invite a friend to test."

## What is actually there (surveyed 2026-09-23)

Co-op is not broken in the shipping build. It was never wired into it.

- The standalone build ships one scene, `Assets/_Project/Scenes/RaidScene.unity`
  (`PlayerBuilder.cs`). That scene has **no `NetworkManager`, no transport and no lobby
  provider**. `CastleBench.unity` has none either.
- **Nothing starts Steam.** The only `SteamAPI.Init()` call is inside PurrLobby's
  `SteamLobbyProvider`, behind `handleSteamInit` (off by default), and no scene contains that
  provider except PurrLobby's own `LobbySample.unity`.
- `steam_appid.txt` (480, Spacewar) is at the repo root. Unity does not copy it into a build, so a
  built exe started outside Steam cannot initialise Steam even when the code asks it to.
- The player is placed in the scene (`RaidPlayer.prefab`), not spawned per connection, and it does
  not carry `PlayerNetworkOwnership`. There is no way for a second player to appear.
- Twelve gameplay scripts are PurrNet `NetworkBehaviour`s (`RaidDirector`, `CastleNetworkManager`,
  `LootPickup`, `CastleGuard`, and others). Because nothing spawns them on a network they have only
  ever run offline (`isSpawned == false`), so their server/client split has never been exercised.
- `SteamInviteGateway` (cold-launch `+connect_lobby` and rich presence) exists but is attached to
  nothing.

## Plan

Each stage lands as its own commit and is checked before the next starts.

1. **Steam starts in the build.** A Steam bootstrap initialises Steamworks once, at launch, and
   the build tool copies `steam_appid.txt` next to the exe. Checked by building and launching with
   Steam running: the overlay opens (Shift+Tab) and the log shows the signed-in persona.
2. **Host and join.** Add a `NetworkManager` to `RaidScene` with PurrNet's `SteamTransport` (and
   `UDPTransport` for local testing), a `SteamLobbyProvider` plus `SteamInviteGateway`, and
   main-menu buttons: *Host co-op* (creates a friends-only Steam lobby and starts the server) and
   *Invite friend* (opens the Steam invite overlay). Accepting an invite, from the overlay or from a
   cold launch, joins the lobby and connects the client to the host's Steam ID.
3. **Each player has a body.** The player becomes a network-spawned prefab (one per connection)
   carrying `NetworkIdentity`, `NetworkTransform` and `PlayerNetworkOwnership`, so each machine
   drives only its own body and camera.
4. **One raid, shared.** The host runs the raid: castle seed, loot, guards, alarm, extraction and
   the lair transition replicate to clients. This is where the twelve never-run `NetworkBehaviour`s
   are exercised for the first time; expect fixes here.

## How it is tested

- **On this machine:** two standalone instances over `UDPTransport` (host and client on
  localhost). This checks stages 2–4 end to end, with screenshots from both windows.
- **Over Steam:** one Steam account can create a lobby and open the invite overlay, which checks
  stage 1 and half of stage 2. A real invite needs a second account on a second machine, so the
  final check is the user and a friend. Until they have run it, it is reported as untested.
