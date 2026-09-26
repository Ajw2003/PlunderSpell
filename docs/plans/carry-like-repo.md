# Carrying like R.E.P.O. — research and plan (#144)

Status: **research done, 2026-09-25; the user chose phase 1 (see "Decisions"), being built on `claude/issue-backlog`.**
Issue: #144, "Fix the way weight works to be more like repo". Added by the user in the same
session: it needs a visual that links the held object to the cursor, the way R.E.P.O.'s grab beam
does.

## What R.E.P.O. does

Sources are listed at the end. Each point is marked with where it comes from:
- **[src]**: identifiers read in a mod's source code.
- **[guide]**: a published guide or wiki.
- **[play]**: how the game is commonly described playing. Not checked in the game itself. Needs
  the user to confirm.

1. **Every grabbable thing is one kind of physics object.** Items, valuables, carts and even
   monsters are a `PhysGrabObject`. The player's tool is a `PhysGrabber`, which holds
   `grabbedPhysGrabObject` plus a `physGrabBeam` GameObject drawn by a `Renderer`. The beam's
   material colour comes from a colour state (`ColorStateSetColor`). The beam is switched on and
   off by RPCs (`PhysGrabBeamActivateRPC` / `PhysGrabBeamDeactivateRPC`), so every player sees
   everyone else's beam. [src]
2. **Strength is a stat, and objects have mass.** `PhysGrabber` has `grabStrength`,
   `throwStrength` and `grabRange`. Upgrades raise them. Low strength lifts small and medium
   things; the heaviest need a lot of strength. [src, guide]
3. **Co-op lifting adds strengths together.** Two players lifting in the same direction count
   their strengths together. Pulling opposite ways drops the object. [guide]
4. **You grab the point you clicked, not the object's centre.** The object hangs from that point.
   An off-centre grab swings and turns under gravity, and a heavy object grabbed low can be
   dragged along the floor ("grab it close to the floor… and go backwards"). [guide, play]
5. **Too heavy means it drags, not that it fails.** Anything past your strength stays on or near
   the ground and slides after the beam instead of lifting. [play]
6. **Controls.** Hold left mouse to grab and carry. The scroll wheel pushes the object away or
   pulls it closer. Right mouse rotates it. [guide]
7. **Valuables lose value on every impact, not only when they break.** Bumping a doorframe,
   dropping it, or a monster hitting it all cost money. The advice is to walk rather than run with
   fragile loot, and to pull it close through doorways. [guide]
8. **The beam tells you the weight.** A beam colour mod turns this into light, medium and heavy
   colours (green, yellow, red) and brightens the beam with impact force. That the mod exists
   suggests the base game only hints at weight. [src]
9. **The beam's shape.** It leaves the player's hand and ends on the grab point. It curves when
   the object lags behind where you are aiming, so you can see the strain. [play]

## What we have (`Item` + `ItemManager`, see `docs/4-systems/damage.md`, "Weight")

| | Plunderspell today | R.E.P.O. |
|---|---|---|
| Where you hold it | a fixed `GripPoint` per item, or its mesh centre | the surface point you clicked |
| Its rotation while held | locked to a target rotation; turns slowly if heavy | hangs freely from the grab point; you rotate it on purpose |
| Strength | one fixed 180 N grip for everyone | a stat, upgradable, added together between players |
| Too heavy | sags and lags, but still lifts | drags along the floor |
| Walking | the item rides rigidly with your body (#119 fix); weight only shows on mouse moves | the object is pulled by the beam, so it trails when you move and turn [play] |
| Weight slows you | yes, `CarrySpeedMultiplier`, down to 0.5× at 15 kg | no slowdown reported in the sources; the object's lag is the cost [play] |
| Two players on one object | no: one holder, who owns the body over the network | yes, forces add up |
| Visual link to the cursor | none | the grab beam |
| Value loss | fragile loot shatters over its `Fragility` threshold | value drops a little on every impact |
| Controls | left hold grab, scroll depth, middle mouse rotate, right click throw | left hold grab, scroll push and pull, right mouse rotate |

## The gap that matters for feel

Two things make R.E.P.O.'s carrying feel the way it does:

1. **The object hangs from where you grabbed it, pulled by a spring you can see.** Its weight
   shows as swing, lag and dragging, and the beam shows that strain directly. Ours holds the item
   stiffly at a fixed point with a locked rotation, with no line between you and it.
2. **Strength against mass decides lift or drag.** Ours always lifts.

The #119 fix (the item rides with your body) deliberately went away from (1) for walking, because
the old spring jerked on every direction change. Going back to a beam spring has to keep that jerk
out. The fix is in the design below: damp the spring toward the target's own velocity, so there is
no step to jerk on.

## Proposed design

Phase 1 is the core feel. Phases 2 and 3 are separable.

**Phase 1: grab point, beam spring, the beam.**
- **Grab where you clicked.** Store the hit point in the item's local space at pickup.
  `GripPoint` stays as a fallback for a pickup that has no surface hit.
- **A beam spring on that point.** Each physics step, apply force at the grab point
  (`AddForceAtPosition`) toward the target on the crosshair ray at the current depth. The force
  is a spring plus damping toward the target's velocity, with the target's velocity taken from
  the camera. That is what stops walking from jerking it. The force is capped by strength. With
  force applied at the grab point, off-centre grabs swing and turn by themselves. Angular damping
  keeps it from spinning.
- **Rotation.** Free by default: it hangs. Holding the rotate key (middle mouse, as now) turns it
  to follow the mouse, like R.E.P.O.'s right mouse.
- **Lift or drag.** If `mass × g` is more than the strength can lift, the vertical part of the
  pull is capped, so the object slides along the floor after you instead of rising.
- **The beam.** A `LineRenderer` drawn as a quadratic Bézier curve with three points:
  - from the player's hand;
  - to the grab point on the object;
  - bent through a control point on the crosshair ray at the held depth.

  When the object keeps up, the line is straight. When it lags or sags, the line bends, showing
  the strain. Its colour and width come from load, `mass × g / strength`: pale and thin when easy,
  hot and thick near the limit, flickering when dragging. A small glow sits at the grab point. It
  is drawn on every peer from replicated state (holder, grab point in the item's local space, and
  the target point), like `physGrabBeam`'s RPC toggles.
- **Tests.** An off-centre grab rotates the item under gravity. A too-heavy item stays within a
  few centimetres of the floor while being pulled. Strafing at walking speed does not jerk: the
  #119 test, `CarryFeelTests`, still passes. The beam's middle point bends away from the straight
  line when the item lags.

**Phase 2: strength as a stat, and two people on one object.**
- `grabStrength` becomes a per-player value in place of the fixed 180 N. It could later be raised
  from the Lair.
- Two players on one object add their strength. That needs the server to own the body and apply
  every grabber's spring force. Today the one holder's machine drives it (`Item.CanDriveHere`).
  This is the networking-heavy part.

**Phase 3: value lost to bumps.** Every impact above a small speed takes some value off, scaled by
the item's fragility, instead of only shattering past a threshold. It changes the economy, so it is
the user's call.

## Decisions

The user's answers, 2026-09-25:

1. **Walking slowdown: dropped.** `CarrySpeedMultiplier` goes. Weight shows only as lag, swing
   and dragging.
2. **Walking: the item trails on the beam spring**, as in R.E.P.O. This replaces the #119
   "rides with your body" behaviour. The spring damps toward the target's own velocity, so a
   direction change does not jerk it.
3. **Scope: phase 1 only.** No strength stat and no co-op lifting yet.
4. **Loot damage: shatter-only stays.**

## Sources

- [DynamicRepoGrabBeam on Thunderstore](https://thunderstore.io/c/repo/p/REPOWorkshop/DynamicRepoGrabBeam/)
  and its source, [M1llerF/REPO-GrabBeam-PhysicsColorMod](https://github.com/M1llerF/REPO-GrabBeam-PhysicsColorMod):
  the identifiers `PhysGrabber.grabbedPhysGrabObject`, `PhysGrabber.physGrabBeam`,
  `ColorStateSetColor`, `ResetBeam`, `PhysGrabBeamActivateRPC`, `PhysGrabObject.isPlayer`,
  `PhysGrabObjectImpactDetector.breakForce` and `impactFragilityMultiplier`.
- [SyncUpgrades on Thunderstore](https://thunderstore.io/c/repo/p/TGO/SyncUpgrades/v/1.5.6/):
  `PhysGrabber.grabStrength`, `throwStrength` and `grabRange`, updated by upgrades.
- [NetworkingReworked](https://github.com/Ian0526/NetworkingReworked): every interactable is a
  `PhysGrabObject` synced by a `PhotonTransformView`.
- [GamesRadar: Repo strength explained](https://www.gamesradar.com/games/horror/repo-strength/)
  and [Destructoid: strength chart](https://www.destructoid.com/r-e-p-o-strength-chart-how-many-strength-upgrades-do-you-need-to-grab-everything/):
  what strength lifts, and co-op strength adding up.
- [R.E.P.O. controls](https://www.repo-game.org/en/repogame-controls): grab, scroll push and pull,
  right-click rotate.
- [R.E.P.O. tips](https://www.repo-game.org/repogame-save-guide/repogame-tips) and
  [the Steam valuables thread](https://steamcommunity.com/app/3241660/discussions/0/689743495770948023/):
  value lost on every impact, pulling close through doors, dragging heavy things low.
- [PC Gamer on the tumble-grab update](https://www.pcgamer.com/games/horror/repo-devs-are-adding-a-new-mechanic-thatll-let-you-grab-things-while-tumbling-and-more-importantly-kill-things-with-butt-physics/):
  the overcharge on grabbed monsters.
