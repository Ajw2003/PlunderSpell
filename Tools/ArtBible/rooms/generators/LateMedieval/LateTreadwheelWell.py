"""LateTreadwheelWell: the treadwheel well (stands in for WellCourtyard).

NE: a treadwheel against the north wall, two oak rims r 1.40 m joined by treads, on
an axle between two A-frames, winding the rope that runs south over a pulley on a
beam and down to a bucket over the well-head, a stone kerb before the wheel. NW: a
handcart with a water barrel on it. SW: a stone trough along the west wall, buckets
beside it. SE: a water butt and a bench. Section A-A east-west at y = 0, looking north:
the wheel face-on, the beam, rope and bucket, the kerb in front.
Kit change: the plan put the wheel along the east wall in the SE; here it stands
against the north wall so the section shows it face-on.
"""
import math

from _late import *

WHEEL = (3.6, 5.0, 1.4, 1.55)                     # centre x, centre y, radius, axle height
RIM_Y = (4.7, 5.3)
WELL = (3.6, 3.0, 0.6, 0.7)                       # x, y, r, kerb height
POSTS_X, BEAM_Z = (2.9, 4.3), 2.3
CART = (-3.4, 4.0, 1.4, 0.8)
TROUGH = (-5.0, -3.6, 0.8, 2.0, 0.6)
BUCKETS = [(-4.1, -2.7), (-4.2, -4.6), (-3.8, -4.4)]
BUTT = (4.8, -4.7, 0.5, 1.0)
BENCH = (3.0, -5.1, 1.8)
ANCHORS = [(CART[0] - 0.35, CART[1], FZ + 0.75), (TROUGH[0] + 0.3, TROUGH[1], FZ + TROUGH[4])]


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("iron", IRON), ("rope", "#9C8A60"), ("water", "#3A4A50"),
            ("stone", "#8A8474"), ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "OuterBailey", "The Treadwheel Well", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE COURT, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 1.8, "#6A6A60", rx=380, ry=200, strength=.2)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "OuterBailey", SAND, soot=SOOT, trim=BRICK, soot_depth=0.5)
    ashlar_courses(sh, "OuterBailey")
    # NE: A-frame legs, the wheel face-on (rims, spokes, treads), the axle.
    wx, wy, wr, wz = WHEEL
    for s in (-1, 1):
        sh.line(*KE(wx + s * 0.9, FZ), *KE(wx, FZ + wz), OAK, 4)
    cx, cz = KE(wx, FZ + wz)
    sh.circle(cx, cz, wr * SK, "none", OAK, 7)
    sh.circle(cx, cz, (wr - 0.1) * SK, "none", darken(OAK, .3), 2)
    for k in range(4):
        a = k * math.pi / 4
        sh.line(cx - math.cos(a) * wr * SK, cz - math.sin(a) * wr * SK, cx + math.cos(a) * wr * SK,
                cz + math.sin(a) * wr * SK, OAK, 3)
    for k in range(16):
        a = k * math.pi / 8
        sh.circle(cx + math.cos(a) * (wr - 0.05) * SK, cz + math.sin(a) * (wr - 0.05) * SK, 3, lighten(OAK, .15))
    sh.circle(cx, cz, 0.12 * SK, IRON)
    # In front: posts, beam, pulley, rope, bucket, the kerb.
    for x in POSTS_X:
        kerect(sh, x - 0.07, FZ, x + 0.07, FZ + BEAM_Z, f"url(#{sh.lin(OAK, 'h', .3, .5)})", darken(OAK, .6), .8)
    kerect(sh, POSTS_X[0] - 0.12, FZ + BEAM_Z, POSTS_X[1] + 0.12, FZ + BEAM_Z + 0.16, f"url(#{sh.lin(OAK, 'v', .3, .5)})",
           darken(OAK, .6), .8)
    sh.circle(*KE(wx, FZ + BEAM_Z - 0.1), 0.12 * SK, IRON)
    sh.line(*KE(wx, FZ + BEAM_Z - 0.2), *KE(wx, FZ + 1.3), "#9C8A60", 1.6)
    bucket = poly_path([KE(wx - 0.15, FZ + 1.3), KE(wx + 0.15, FZ + 1.3), KE(wx + 0.12, FZ + 0.98), KE(wx - 0.12, FZ + 0.98)])
    sh.path(bucket, OAK, darken(OAK, .6), .8)
    ux, uy, ur, uh = WELL
    kerect(sh, ux - ur, FZ, ux + ur, FZ + uh, f"url(#{sh.lin('#8A8474', 'h', .3, .55)})", darken("#8A8474", .6), 1)
    for k in range(1, 3):
        sh.line(*KE(ux - ur, FZ + k * uh / 3), *KE(ux + ur, FZ + k * uh / 3), darken("#8A8474", .35), .7)
    # NW: the handcart side-on, its barrel.
    kx, ky, kw, kd = CART
    kerect(sh, kx - kw / 2, FZ + 0.62, kx + kw / 2, FZ + 0.75, OAK, darken(OAK, .6), .8)
    sh.circle(*KE(kx - 0.3, FZ + 0.35), 0.35 * SK, "none", OAK, 3)
    sh.line(*KE(kx + kw / 2, FZ + 0.7), *KE(kx + kw / 2 + 0.6, FZ + 0.45), OAK, 3)
    cask_end(sh, kx + 0.35, FZ + 0.75, r=0.3, cradle=False)
    kit_cut_walls(sh, "OuterBailey")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(wx + 1.2, FZ + wz + 0.7), "TREADWHEEL", "2 oak rims r 1.40 m, treads between"),
        (*KE(wx, FZ + BEAM_Z + 0.1), "BEAM AND PULLEY", "on two posts over the well"),
        (*KE(ux + 0.4, FZ + 0.4), "WELL-HEAD", "stone kerb r 0.60 m, 0.70 m high"),
    ], 610, 190, 330, slope=1.0)
    sh.callouts([
        (*KE(kx, FZ + 0.9), "HANDCART", "a water barrel on it · loot"),
    ], 330, 260, 260, anchor="end")
    kit_clear_note(sh, "OuterBailey", x=0.0, text="3.60 clear · open to the sky")

    # ---- plan ----
    kit_plan(sh, "OuterBailey", floor="#34302A", wall="#3A332A", trim=BRICK)
    for y in RIM_Y:
        kprect(sh, wx - wr, y - 0.05, wx + wr, y + 0.05, OAK, "#0E0C09", .5)
    for k in range(9):
        x = wx - wr + 0.15 + k * (2 * wr - 0.3) / 8
        kprect(sh, x - 0.04, RIM_Y[0], x + 0.04, RIM_Y[1], lighten(OAK, .1))
    kprect(sh, wx - 0.06, 4.5, wx + 0.06, IN, IRON)
    for y in (4.55, 5.42):
        kprect(sh, wx - 0.9, y - 0.05, wx + 0.9, y + 0.05, darken(OAK, .2))
    plan_disc(sh, ux, uy, ur, "#8A8474")
    plan_disc(sh, ux, uy, ur - 0.15, "#3A4A50", None)
    for x in POSTS_X:
        plan_box(sh, x, uy, 0.14, 0.14, OAK)
    kprect(sh, POSTS_X[0], uy - 0.06, POSTS_X[1], uy + 0.06, OAK)
    kprect(sh, wx - 0.02, uy, wx + 0.02, RIM_Y[0], "#9C8A60")
    plan_box(sh, kx, ky, kw, kd, OAK)
    plan_box(sh, kx + 0.35, ky, 0.6, 0.7, darken(OAK, .15))
    for dy in (-0.25, 0.25):
        kprect(sh, kx + kw / 2, ky + dy - 0.03, kx + kw / 2 + 0.6, ky + dy + 0.03, OAK)
    tx, ty, tw, tl, th = TROUGH
    plan_box(sh, tx, ty, tw, tl, "#8A8474")
    plan_box(sh, tx, ty, tw - 0.2, tl - 0.2, "#3A4A50")
    for x, y in BUCKETS:
        plan_disc(sh, x, y, 0.16, OAK)
    plan_disc(sh, BUTT[0], BUTT[1], BUTT[2], OAK)
    plan_box(sh, BENCH[0], BENCH[1], BENCH[2], 0.35, OAK)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "OuterBailey")
    socket_label(sh, *KP(3.6, 2.05), "WELL + TREADWHEEL")
    socket_label(sh, *KP(-3.8, -2.0), "TROUGH + BUCKETS")
    socket_label(sh, *KP(4.0, -3.8), "WATER BUTT")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.59 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("NE", "treadwheel on the N wall, beam, pulley, well"),
                    ("ROOM", "handcart NW · trough, buckets SW · butt SE"),
                    ("LOOT", "L1–L2: handcart, trough edge")])
    return sh
