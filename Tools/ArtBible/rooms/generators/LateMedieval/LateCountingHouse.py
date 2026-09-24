"""LateCountingHouse: the counting house (stands in for GuardRoomInner, the steward's
office), the art bible's Counting House (docs/art/late.md) built to the kit.

NW: a brick strong room built into the corner, walls 0.30 m, a low segmental
barrel-vault cap, its iron-latticed door standing open against the front; inside,
two iron-bound chests. NE: the ledger case against the north wall, four tiers of
ledgers and rolls. SE: the counting table under a chequered wool cloth with a
balance, coin stacks, an open ledger and a candle, a bench before it; a barred
window on the east wall. SW: the floor strongbox, a flagstone lid with an iron
ring, and coin sacks. Kit overrides: the art bible's sunk floor strongbox becomes a
flat lid (rooms keep one floor level) and its strong-room door stands open so the
chests can be reached. Section A-A east-west at y = 0, looking north.
"""
from _late import *

SR = (-IN, -2.2, 2.2, IN)                              # strong room: x0, x1, y0, y1 (outer)
SR_T, SR_H, VAULT = 0.30, 2.6, 0.6
DOOR = (-4.3, -3.4, 2.0)                               # doorway x0, x1, height
SR_CHESTS = [(-4.55, 5.1), (-3.3, 5.1)]
LEDGER = (3.6, 2.4, 0.45, 2.4)                         # x, width, depth, height
TABLE = (3.6, -3.4, 2.0, 1.0, 0.8)
HATCH = (-3.6, -3.6)
SACKS = [(-4.8, -4.6), (-4.4, -4.9), (-5.0, -4.1)]
ANCHORS = [(TABLE[0], TABLE[1], FZ + TABLE[4] + 0.02), (SR_CHESTS[0][0], SR_CHESTS[0][1], FZ + 0.65),
           (SR_CHESTS[1][0], SR_CHESTS[1][1], FZ + 0.65), (LEDGER[0], IN - 0.25, FZ + 1.23)]


def build():
    mats = [("sandstone", SAND), ("brick", BRICK), ("oak", OAK), ("iron", IRON), ("cloth", WOOL),
            ("coin", GOLD), ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "InnerWard", "The Counting House", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE HOUSE, LOOKING NORTH")
    kit_glow(sh, 3.0, FZ + 1.4, "#8A6A30", rx=260, ry=180, strength=.3)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "InnerWard", SAND, soot=SOOT, trim=WOOL, soot_depth=0.5)
    ashlar_courses(sh, "InnerWard")
    # NW: the strong room's brick front, the dark doorway, the vault cap in profile, the open lattice door.
    x0, x1 = SR[0], SR[1]
    kerect(sh, x0, FZ, x1, FZ + SR_H, f"url(#{sh.lin(BRICK, 'v', .2, .5)})", darken(BRICK, .6), 1)
    for k in range(1, int(SR_H / 0.2)):
        sh.line(*KE(x0, FZ + k * 0.2), *KE(x1, FZ + k * 0.2), darken(BRICK, .35), .5, op=.6)
    kerect(sh, DOOR[0], FZ, DOOR[1], FZ + DOOR[2], "#0E0C09")
    cap = []
    for k in range(9):
        u = -1 + k / 4
        cap.append(KE((x0 + x1) / 2 + u * (x1 - x0) / 2, FZ + SR_H + VAULT * (1 - u * u)))
    sh.path(poly_path([KE(x0, FZ + SR_H)] + cap + [KE(x1, FZ + SR_H)]),
            f"url(#{sh.lin(BRICK, 'v', .25, .5)})", darken(BRICK, .6), 1)
    lx0 = DOOR[1]
    kerect(sh, lx0, FZ, lx0 + 0.9, FZ + 2.0, "none", IRON, 1.4)
    for k in range(1, 4):
        sh.line(*KE(lx0 + k * 0.225, FZ), *KE(lx0 + k * 0.225, FZ + 2.0), IRON, 1.6)
    for k in range(1, 5):
        sh.line(*KE(lx0, FZ + k * 0.4), *KE(lx0 + 0.9, FZ + k * 0.4), IRON, 1.6)
    # NE: the ledger case.
    lx, lw, ld, lh = LEDGER
    press(sh, lx - lw / 2, lx + lw / 2, h=lh, tiers=4)
    kit_cut_walls(sh, "InnerWard")
    khuman(sh, 0.9)
    sh.callouts([
        (*KE(lx + 0.6, FZ + 1.9), "LEDGER CASE", "oak, 4 tiers, pigeonholes · loot"),
    ], 610, 260, 260, slope=1.0)
    sh.callouts([
        (*KE(-3.0, FZ + SR_H + 0.4), "BARREL-VAULT CAP", "brick, 0.60 m rise"),
        (*KE(-2.6, FZ + 1.3), "IRON LATTICE DOOR", "0.90 × 2.00 m, standing open"),
        (*KE(-4.9, FZ + 1.0), "BRICK STRONG ROOM", "3.30 m square, 2 chests · loot"),
    ], 330, 190, 330, anchor="end")
    kit_clear_note(sh, "InnerWard", x=0.0, text="4.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "InnerWard", floor="#3A3226", wall="#3A332A", trim=WOOL)
    kprect(sh, x0, SR[2], x1, SR[3], BRICK, "#0E0C09", .8)
    kprect(sh, x0 + 0.02, SR[2] + SR_T, x1 - SR_T, SR[3] - 0.02, "#2A1E18", "#0E0C09", .5)
    kprect(sh, DOOR[0], SR[2], DOOR[1], SR[2] + SR_T, "#2A1E18")
    kprect(sh, DOOR[1], SR[2] - 0.05, DOOR[1] + 0.9, SR[2], IRON)
    for x, y in SR_CHESTS:
        plan_box(sh, x, y, 1.0, 0.6, OAK)
        kprect(sh, x - 0.03, y - 0.3, x + 0.03, y + 0.3, IRON)
    kprect(sh, lx - lw / 2, IN - ld, lx + lw / 2, IN, OAK, "#0E0C09", .7)
    plan_box(sh, *TABLE[:4], WOOL)
    for i in range(8):
        for j in range(4):
            if (i + j) % 2 == 0:
                x = TABLE[0] - 1.0 + i * 0.25
                y = TABLE[1] - 0.5 + j * 0.25
                kprect(sh, x, y, x + 0.25, y + 0.25, darken(WOOL, .35))
    for dx in (-0.6, -0.4):
        plan_disc(sh, TABLE[0] + dx, TABLE[1] + 0.2, 0.05, GOLD)
    plan_box(sh, TABLE[0], -4.25, 2.0, 0.35, OAK)
    kprect(sh, IN - 0.1, -4.0, IN, -2.8, "#2A3238", "#0E0C09", .6)
    plan_box(sh, *HATCH, 0.8, 0.6, lighten(SAND, .05))
    plan_disc(sh, HATCH[0], HATCH[1], 0.08, None, IRON)
    for x, y in SACKS:
        plan_disc(sh, x, y, 0.2, "#5A4630")
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "InnerWard")
    socket_label(sh, *KP(-3.85, 1.9), "STRONG ROOM")
    socket_label(sh, *KP(3.6, -4.75), "COUNTING TABLE")
    socket_label(sh, *KP(-3.6, -2.9), "FLOOR STRONGBOX")
    socket_label(sh, *KP(4.4, -1.95), "BARRED WINDOW")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.88 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("NW / NE", "brick strong room, 2 chests · ledger case"),
                    ("SE / SW", "counting table, balance · strongbox lid, sacks"),
                    ("LOOT", "L1–L4: table, strong-room chests, ledgers")])
    return sh
