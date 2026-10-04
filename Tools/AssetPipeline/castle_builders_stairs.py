"""
High Medieval stairs for the stacked castle (#256): each reuses the kit's L stair
(castle_builders._stair_flights) inside two storeys. Root at the bottom of the lowest
slab, exit faces +Z. Geometry contract: Editor/CastleStairPlaceholderForge.cs.

  MedievalStairUp:   ward lobby (archways all round) under a keep head storey (floor top
                     4.60, opens north only). Lobby stair -> railed gallery at 2.90 ->
                     third flight east along the north wall to the head floor.
  MedievalStairDown: crypt storey (opens north only) under a ward lobby (floor top 3.60,
                     archways all round). Two 6-riser flights straight to the lobby floor.

A guard needs 2.40 m under any slab edge (guard 2.30 tall), so each slab has a well cut over
every walking surface that would be closer than that.
"""
import castle_builders as cb
import room_kit as rk
from castle_builders import IN, Q0, STONE, METAL, ZONE_ACCENT, ZONE_FLOOR, ZONE_HEIGHT

SLAB = 0.3
HALF = rk.HALF


def _walls(bm, uv, zone, base_z, door_sides):
    """Four walls of `zone`'s height on base_z, an archway on each side in door_sides."""
    h = ZONE_HEIGHT[zone]
    for side in rk.SIDES:
        length = rk.FOOTPRINT if side in ("north", "south") else rk.FOOTPRINT - 2 * rk.WALL_T
        rk.wall_run(bm, uv, side, base_z, h, STONE, opening=rk.door(h) if side in door_sides else None,
                    trim_pigment=ZONE_ACCENT[zone], length=length)


def _storey(bm, uv, zone, door_sides):
    """Floor slab and walls on z = 0, exactly as the zone's rooms are built."""
    rk.room_shell(bm, uv, ZONE_HEIGHT[zone], STONE, trim=ZONE_ACCENT[zone],
                  floor_pigment=ZONE_FLOOR[zone], door_sides=door_sides)


def _slab_with_well(bm, uv, z0, z1, floor, well):
    """A full-footprint slab from z0 to z1 with a rectangular well (x0, x1, y0, y1) cut out of it."""
    x0, x1, y0, y1 = well
    pieces = ((-HALF, x0, -HALF, HALF), (x1, HALF, -HALF, HALF), (x0, x1, -HALF, y0), (x0, x1, y1, HALF))
    for ax0, ax1, ay0, ay1 in pieces:
        cb._box(bm, uv, floor, ((ax0 + ax1) / 2, (ay0 + ay1) / 2, (z0 + z1) / 2), (ax1 - ax0, ay1 - ay0, z1 - z0))


def _flight_east(bm, uv, z0, rise, steps, x_foot, tread, y, pigment):
    """A 1.5 m wide flight climbing east from x_foot along y, `steps` risers of `rise` above z0."""
    for i in range(steps):
        z = z0 + rise * (i + 1)
        cb._box(bm, uv, pigment, (x_foot + (i + 0.5) * tread, y, (z0 + z) / 2), (tread, 1.5, z - z0))


def build_medieval_stair_up(bm, uv):
    _storey(bm, uv, "InnerWard", rk.SIDES)
    top, gallery = 2.6, SLAB + 2.6
    cb._stair_to_gallery(bm, uv, SLAB, top, STONE, METAL)      # 8 risers of 0.325 to the gallery at 2.90
    steps, head = 6, 4.6
    _flight_east(bm, uv, gallery, (head - gallery) / steps, steps, -Q0 - 6 * 0.3, 0.3, IN - 0.75, STONE)   # 6 risers of 0.283
    # Head floor 4.30-4.60; the well takes the gallery and every flight past the landing (surface 1.60, 2.70 m under the slab).
    _slab_with_well(bm, uv, 4.3, head, ZONE_FLOOR["Keep"], (-IN, -Q0, -4.2, IN))
    _walls(bm, uv, "Keep", head, ("north",))
    cb._brazier(bm, uv, 3.8, -3.8, SLAB)
    cb._brazier(bm, uv, 3.8, 3.8, head)


def build_medieval_stair_down(bm, uv):
    _storey(bm, uv, "Crypt", ("north",))
    top, lobby = 3.3, 3.6
    cb._stair_flights(bm, uv, SLAB, top, STONE, steps=6)       # 12 risers of 0.275 to the lobby floor
    # Lobby floor 3.30-3.60; the well takes both flights past the second tread (surface 0.85, 2.45 m under the slab).
    _slab_with_well(bm, uv, 3.3, lobby, ZONE_FLOOR["InnerWard"], (-IN, -2.5, -IN, -Q0 - 0.2))
    _walls(bm, uv, "InnerWard", lobby, rk.SIDES)
    cb._brazier(bm, uv, 3.8, -3.8, SLAB)
    cb._brazier(bm, uv, 3.8, 3.8, lobby)
