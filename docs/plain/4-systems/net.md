<!-- plain copy of: docs/4-systems/net.md @ 81b9a637e5bb0abd990be8d705e9220a03f3a44f -->

# Net

Full technical doc: [net.md](../../4-systems/net.md)

## What it is
How a game session starts, how a friend joins it through Steam, and how each connected machine
gets its own player body that everyone else can see move.

## Why it matters
A co-op heist only works if every player's machine agrees on the same castle, the same guards,
and who is holding what. Getting this wrong means players seeing different things, invites that
do not work, or one player's actions not showing up for the others.

## How it works
1. Every session, even solo play, starts through the same one place. Solo, a Steam invite, and
   joining over a local network are three settings of the same switch, not three code paths.
2. Hosting through Steam opens a friends-only lobby, and joining works through Steam's own invite
   and friends-list tools rather than typing in an address.
3. Every connected player gets their own body. Only the machine that owns a body drives its
   input, camera and microphone; every other machine just watches it move.
4. Only the castle's starting seed crosses the network. Every machine builds the same castle
   from it locally, rather than sending the whole building over the connection.
5. The host runs guards and enemies; other machines just see them move. Picking up loot briefly
   asks the host for permission to control it, so only one machine drives a piece at a time.
6. Every hit is judged by whoever owns the thing being hit: the host for guards and loot, the
   player themselves for their own body, so results are trustworthy either way.
7. If every player is downed at once, the raid ends for everyone together; a downed player can
   otherwise spectate a teammate until the next raid begins.

## Risks and safeguards
- **Two players ending up standing inside each other, or in the wrong place.** Each machine
  places only its own body, offset from the others around the same entry point.
- **A networked value arriving out of order and confusing what it means.** Related pieces of
  information that must agree, like an attack's count and its kind, are packed into a single
  value instead of two that could arrive in either order.
- **A late-joining player replaying past events.** Anything that already happened before they
  joined is not replayed to them, only the current state.
- **A friend's own saved progress being affected by visiting someone else's game.** Visiting
  shows the host's progress without touching or saving over the visitor's own.
- **Steam's invite dialog silently doing nothing.** This happens when the game was not started
  through Steam; the friends list in the game's own menu covers that case instead.

## Related
- [Raid](../../4-systems/raid.md) *(no plain copy yet)*

## Left out
File and class names, the exact replicated fields and RPC plumbing, Steam App ID and firewall
specifics, and the manual multi-machine testing steps.
