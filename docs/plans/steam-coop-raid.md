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

## Status (2026-09-23)

The user chose stages 1–3 first; stage 4 is the next pass.

- **Stage 1, done.** Steam starts in the build and in the Editor (logged: persona, app 480, overlay
  state). The build tool copies `steam_appid.txt`.
- **Stage 2, done on one machine.** Host Co-op creates a friends-only Steam lobby and hosts over
  `SteamTransport`. Invite Friend opens Steam's dialog when the overlay is available, otherwise an
  in-game list of online friends. Joining over UDP was checked end to end with two game windows;
  joining over Steam needs a second account and is **untested**.
- **Stage 3, done over UDP.** One body per connection; the client builds the same castle from the
  replicated seed, follows the host into the raid, and each machine sees the other's body move.
- **Stage 4, not started,** apart from gating a few client-side writes (loot breaking, the
  extraction clock, guard agents). Known gaps: loot has no `NetworkTransform`, so a client does not
  see loot move; guard and loot physics on a client are not reconciled; damage to a remote player
  and extraction counting of a client's body are unverified.

### Stage 4 checklist (started and done 2026-09-23)

Surveyed: loot and guard prefabs carry no `NetworkTransform`, so a client sees guards frozen where
they spawned and loot that never moves. Weapons are plain local `Item`s on each machine.

1. **Guards move on clients:** `NetworkTransform` on every enemy prefab (server-driven).
2. **Loot moves on clients, and a client can carry it:** `NetworkTransform` on loot; grabbing a
   piece asks the server for ownership, and the drag starts once it is granted.
3. **Damage goes to whoever owns the target:** a client hitting a guard sends the hit to the
   server; the server hitting a friend's body sends it to that friend's machine, where their health
   lives. The hitter still sees the damage numbers.
4. **A client's spells aim where the client looks:** the cast carries its origin and direction,
   because the host never sees a friend's camera pitch.
5. **The Lair shows the host's debt and bank on every machine** after a raid.
6. **A dead player spectates** (asked 2026-09-23): in co-op, dying hands the view to a living
   teammate instead of the "You died" screen, and the raid is only lost when every player is down.
   Solo keeps the current screen.
7. **Checked end to end over UDP with two windows,** the Editor hosting and a Development build
   as the client, driven through the Pipeline runtime:
   - guards match on both machines by network ID;
   - the client picks up loot (ownership 001 to 002), sets it on the pad, and the host's extraction
     banks it (150; debt 700 to 550), with the client's Lair showing 550 and "last raid 150";
   - the client hits a Sergeant (130 to 105 on the host) and sees the damage number;
   - a Watchman kills the client (health on the client's machine), which spectates the host;
     killing the host too ends the raid for both;
   - the host goes down while the client stands, and watches through the client's eyes;
   - a client's aimed Frango hits the aimed Watchman on the host (70 to 40);
   - the next raid rebuilds on the client and places it at the new gate.

   **Still untested:** any of this over Steam between two accounts.

### After stage 4 (2026-09-23)

Asked for: network the weapons and close the gaps found in stage 4.

- **Weapons** spawn in the castle as loot and are networked like it. Checked: a client picked up
  the Arming Sword and the host saw it move with the client (client (9.10, 2.25, -7.50), host
  (9.05, 2.26, -7.41)); a client's crossbow shot appeared on the host.
- **A downed body lies down** on every machine. Checked with both players down.
- **Guards at the gate:** the safe ring is two rooms and patrols stay out of it; a 20 second arrival
  grace keeps a calm garrison from seeing players. Checked: an idle solo player was first hit at
  36.9 s, against about 10 s before. `PlayableLoopTests` now fails on the old planner.

Found along the way, not fixed: guards are posted within sight of the gate, and a player standing
still at the spawn is killed in about 20 seconds.

## How it is tested

- **On this machine:** two standalone instances over `UDPTransport` (host and client on
  localhost). This checks stages 2–4 end to end, with screenshots from both windows.
- **Over Steam:** one Steam account can create a lobby and open the invite overlay, which checks
  stage 1 and half of stage 2. A real invite needs a second account on a second machine, so the
  final check is the user and a friend. Until they have run it, it is reported as untested.
