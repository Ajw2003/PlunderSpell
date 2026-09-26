"""LateUndercroft: the wine undercroft (stands in for CryptAntechamber).

Four squat octagonal vault piers at (±2.50, ±2.50), each on a square base with a
moulded capital and abacus, and on top the springer stubs where the vault ribs start
east-west and north-south (the vault itself is open, rooms have no roof). Great wine
casks lie on cradles along the east and west walls, two in each quadrant, heads to
the room. Section A-A east-west at y = 0, looking north: the two north piers, the
north casks side-on.
"""
from _late import *

PIERS = [(sx * 2.5, sy * 2.5) for sx in (-1, 1) for sy in (-1, 1)]
PIER_R = 0.4
BASE_H, SHAFT_TOP, CAP_TOP, ABACUS_TOP, SPRING_TOP = 0.2, 2.1, 2.35, 2.5, 2.8
CASK_R, CASK_L, CRADLE = 0.5, 1.2, 0.2
CASK_X = IN - 0.05 - CASK_L / 2                      # 4.85
CASK_Y = (2.7, 4.2)
TOP = FZ + CRADLE + 2 * CASK_R - 0.05
ANCHORS = [(CASK_X, 2.7, TOP), (-CASK_X, 4.2, TOP), (CASK_X, -4.2, TOP), (-CASK_X, -2.7, TOP)]


def pier(sh, x, col=SAND):
    """A squat octagonal pier in elevation: base, shaft, capital, abacus, springer stubs."""
    kerect(sh, x - 0.5, FZ, x + 0.5, FZ + BASE_H, f"url(#{sh.lin(col, 'v', .25, .5)})", darken(col, .6), .9)
    kerect(sh, x - PIER_R, FZ + BASE_H, x + PIER_R, FZ + SHAFT_TOP, f"url(#{sh.lin(col, 'h', .3, .55)})",
           darken(col, .6), 1)
    for dx in (-PIER_R * .45, PIER_R * .45):
        sh.line(*KE(x + dx, FZ + BASE_H), *KE(x + dx, FZ + SHAFT_TOP), darken(col, .35), .8)
    cap = poly_path([KE(x - PIER_R, FZ + SHAFT_TOP), KE(x + PIER_R, FZ + SHAFT_TOP), KE(x + 0.45, FZ + CAP_TOP),
                     KE(x - 0.45, FZ + CAP_TOP)])
    sh.path(cap, lighten(col, .08), darken(col, .6), .9)
    kerect(sh, x - 0.55, FZ + CAP_TOP, x + 0.55, FZ + ABACUS_TOP, lighten(col, .12), darken(col, .6), .9)
    kerect(sh, x - 0.8, FZ + ABACUS_TOP, x + 0.8, FZ + SPRING_TOP, f"url(#{sh.lin(col, 'v', .25, .5)})",
           darken(col, .6), .9)
    kerect(sh, x - 0.15, FZ + ABACUS_TOP, x + 0.15, FZ + SPRING_TOP, darken(col, .1), darken(col, .6), .6)


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("iron", IRON), ("wine", ESTATE), ("soot", SOOT),
            ("flagstone", FLAG)]
    sh = room_sheet("LateMedieval", "Crypt", "The Undercroft", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE UNDERCROFT, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 1.0, "#5A4A30", rx=320, ry=160, strength=.3)

    # ---- section ----
    kit_slab(sh, "#2A241C")
    kit_back_wall(sh, "Crypt", SAND, soot=SOOT, trim="#3A342A", soot_depth=0.8)
    ashlar_courses(sh, "Crypt")
    for sgn in (-1, 1):
        x0, x1 = sorted((sgn * (IN - 0.05 - CASK_L), sgn * (IN - 0.05)))
        cask_side(sh, x0, x1, FZ, r=CASK_R)
    for sgn in (-1, 1):
        pier(sh, sgn * 2.5)
    kit_cut_walls(sh, "Crypt")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(2.5, FZ + 2.65), "SPRINGER STUBS", "where the ribs start, E–W and N–S"),
        (*KE(2.5, FZ + 1.2), "SQUAT PIER", "octagonal, r 0.40 m, at (±2.50, ±2.50)"),
        (*KE(4.85, FZ + 0.7), "WINE CASK", "r 0.50 × 1.20 m on a cradle · loot"),
    ], 610, 190, 330, slope=1.0)
    sh.callouts([
        (*KE(-2.5, FZ + 2.4), "CAPITAL + ABACUS", "moulded, 1.10 m square"),
    ], 330, 260, 260, anchor="end")
    kit_clear_note(sh, "Crypt", x=4.1, text="3.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Crypt", floor="#1E1B16", wall="#3A332A", trim="#3A342A")
    for x, y in PIERS:
        kprect(sh, x - 0.8, y - 0.15, x + 0.8, y + 0.15, darken(SAND, .1), "#0E0C09", .4)
        kprect(sh, x - 0.15, y - 0.8, x + 0.15, y + 0.8, darken(SAND, .1), "#0E0C09", .4)
        plan_box(sh, x, y, 1.0, 1.0, SAND)
        plan_disc(sh, x, y, PIER_R, lighten(SAND, .1))
    for sgn in (-1, 1):
        for sy in (-1, 1):
            for y in CASK_Y:
                plan_cask(sh, sgn * CASK_X, sy * y, CASK_L, CASK_R)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Crypt")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.16 · all open"),
                    ("CLEAR CROSS", "dashed · runs between the four piers"),
                    ("PIERS", "4 squat octagonal piers with springers, 2.80 m"),
                    ("CASKS", "8, on cradles along the E and W walls"),
                    ("LOOT", "L1–L4: a cask top in each quadrant")])
    return sh
