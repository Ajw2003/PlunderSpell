"""
Stairs for the stacked castle (#256), one pair per Age: each reuses that Age's stairwell stair
(castle_builders._stair_flights / _stair_to_gallery) inside two storeys. Root at the bottom of the
lowest slab, exit faces +Z. Geometry contract: Editor/CastleStairPlaceholderForge.cs.

  <Age>StairUp:   ward lobby (archways all round) under a keep head storey (floor top 4.60,
                  opens north only). Lobby stair -> railed gallery at 2.90 -> third flight east
                  along the north wall to the head floor.
  <Age>StairDown: crypt storey (opens north only) under a ward lobby (floor top 3.60, archways
                  all round). Two 8-riser flights straight to the lobby floor.

A guard needs 2.40 m under any slab edge (guard 2.30 tall), so each slab has a well cut over
every walking surface that would be closer than that.
"""
import castle_builders as cb
import castle_builders_bronze as bronze
import castle_builders_late as late
import room_kit as rk
from castle_builders import IN, Q0, STONE, METAL, ZONE_HEIGHT

SLAB = 0.3
HALF = rk.HALF

# Per Age: wall pigment by zone, floor and trim tables, the stair's own stone and its rail.
MEDIEVAL = dict(stone=lambda zone: STONE, floor=cb.ZONE_FLOOR, trim=cb.ZONE_ACCENT, flight=STONE, rail=METAL)
LATE = dict(stone=lambda zone: late.WALL, floor=late.ZONE_FLOOR, trim=late.ZONE_TRIM, flight=late.WALL, rail=late.TIMBER)
BRONZE = dict(stone=lambda zone: bronze.ZONE_WALL.get(zone, bronze.WALL), floor=bronze.ZONE_FLOOR,
              trim=bronze.ZONE_TRIM, flight=bronze.LINEN, rail=bronze.TIMBER)


def _walls(bm, uv, kit, zone, base_z, door_sides):
    """Four walls of `zone`'s height on base_z, an archway on each side in door_sides."""
    h = ZONE_HEIGHT[zone]
    for side in rk.SIDES:
        length = rk.FOOTPRINT if side in ("north", "south") else rk.FOOTPRINT - 2 * rk.WALL_T
        rk.wall_run(bm, uv, side, base_z, h, kit["stone"](zone), opening=rk.door(h) if side in door_sides else None,
                    trim_pigment=kit["trim"][zone], length=length)


def _storey(bm, uv, kit, zone, door_sides):
    """Floor slab and walls on z = 0, exactly as the zone's rooms are built."""
    rk.room_shell(bm, uv, ZONE_HEIGHT[zone], kit["stone"](zone), trim=kit["trim"][zone],
                  floor_pigment=kit["floor"][zone], door_sides=door_sides)


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


def _stair_up(bm, uv, kit):
    _storey(bm, uv, kit, "InnerWard", rk.SIDES)
    top, gallery = 2.6, SLAB + 2.6
    cb._stair_to_gallery(bm, uv, SLAB, top, kit["flight"], kit["rail"])      # 8 risers of 0.325 to the gallery at 2.90
    # 8 risers of 0.2125: the nav tile samples every 0.5 m and joins cells at most 0.45 apart, so two risers must fit in that.
    steps, head, tread = 8, 4.6, 0.3
    _flight_east(bm, uv, gallery, (head - gallery) / steps, steps, -Q0 - steps * tread, tread, IN - 0.75, kit["flight"])
    # Head floor 4.30-4.60; the well takes the gallery and every flight past the landing (surface 1.60, 2.70 m under the slab).
    _slab_with_well(bm, uv, 4.3, head, kit["floor"]["Keep"], (-IN, -Q0, -4.2, IN))
    _walls(bm, uv, kit, "Keep", head, ("north",))
    cb._brazier(bm, uv, 3.8, -3.8, SLAB)
    cb._brazier(bm, uv, 3.8, 3.8, head)


def _stair_down(bm, uv, kit):
    _storey(bm, uv, kit, "Crypt", ("north",))
    top, lobby = 3.3, 3.6
    # 16 risers of 0.206, treads 0.3, to the lobby floor: the nav tile joins cells at most 0.45 apart, so two risers must fit in that.
    cb._stair_flights(bm, uv, SLAB, top, kit["flight"], steps=8, tread=0.3)
    # Lobby floor 3.30-3.60; the well takes both flights past the second tread (surface 0.71, 2.59 m under the slab edge).
    _slab_with_well(bm, uv, 3.3, lobby, kit["floor"]["InnerWard"], (-IN, -2.1, -IN, -IN + 1.5 + 8 * 0.3))
    _walls(bm, uv, kit, "InnerWard", lobby, rk.SIDES)
    cb._brazier(bm, uv, 3.8, -3.8, SLAB)
    cb._brazier(bm, uv, 3.8, 3.8, lobby)


def build_medieval_stair_up(bm, uv):
    _stair_up(bm, uv, MEDIEVAL)


def build_medieval_stair_down(bm, uv):
    _stair_down(bm, uv, MEDIEVAL)


def build_late_stair_up(bm, uv):
    _stair_up(bm, uv, LATE)


def build_late_stair_down(bm, uv):
    _stair_down(bm, uv, LATE)


def build_bronze_stair_up(bm, uv):
    _stair_up(bm, uv, BRONZE)


def build_bronze_stair_down(bm, uv):
    _stair_down(bm, uv, BRONZE)
