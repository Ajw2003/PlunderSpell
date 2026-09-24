"""
Late Medieval OuterBailey pieces: LateArtilleryYard, LateGunFoundry, LateHandgunnerBarracks, LateBrewhouse, LateTreadwheelWell.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/late/<Key>.json,
drawing: docs/art/rooms/concept/late/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_late.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_late import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler
