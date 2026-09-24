"""BronzeCistern: the cistern court (stands in for WellCourtyard).

Section A-A east-west at y = 0, looking north: the hydria stand against the north
wall in the NW, the clay basin on its pedestal in the NE; the cistern head in the
SE (behind the cut plane, so drawn in the plan) with its winch beam rising over
the wall line. Paved floor, a trough in the SW.
"""
from _bronze import *

ANCHORS = [(3.6, -2.9, 1.34), (-3.8, 4.9, 0.80), (3.8, 4.2, 1.30), (2.2, -4.8, 0.65)]
WATER = "#5E8A96"


def build():
    mats = [("plaster", OCHRE), ("limestone", STONE), ("clay", TERRA), ("water", WATER),
            ("cypress", CYP), ("gypsum", GYP), ("mud-brick", MUD)]
    sh = room_sheet("BronzeAge", "OuterBailey", "The Cistern Court", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE COURT, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 2.0, "#7A7A6A", rx=420, ry=220, strength=.2)

    # ---- section ----
    kit_slab(sh, STONE)
    kit_back_wall(sh, "OuterBailey", OCHRE, trim=MUD, soot_depth=0.3)
    # NW: the hydria stand against the north wall, three water jars on it.
    table(sh, -3.8, 1.6, 0.50, col=CYP)
    for dx in (-0.55, 0.55):
        jar(sh, -3.8 + dx, FZ + 0.50, 0.62, 0.40, TERRA, bands=(0.25,))
    # NE: the clay basin on its pedestal.
    kerect(sh, 3.55, FZ, 4.05, FZ + 0.10, GYP, darken(GYP, .5), .7)
    kerect(sh, 3.72, FZ + 0.10, 3.88, FZ + 0.80, f"url(#{sh.lin(TERRA, 'h', .25, .5)})", darken(TERRA, .6), .7)
    bowl = [KE(3.2, FZ + 1.0), KE(3.3, FZ + 0.80), KE(4.3, FZ + 0.80), KE(4.4, FZ + 1.0)]
    sh.path(poly_path(bowl), f"url(#{sh.lin(TERRA, 'h', .3, .5)})", darken(TERRA, .6), 1)
    sh.ellipse(*KE(3.8, FZ + 1.0), 0.55 * SK, 4, WATER, darken(WATER, .5), .6)
    # The winch beam over the cistern (SE, beyond the cut) shows above the NE basin line: dashed.
    for x in (2.6, 4.6):
        sh.line(*KE(x, FZ + 0.8), *KE(x, FZ + 2.35), CYP, 2, dash="4 3", op=.7)
    sh.line(*KE(2.5, FZ + 2.35), *KE(4.7, FZ + 2.35), CYP, 3, dash="4 3", op=.7)
    sh.text(*KE(3.6, FZ + 2.5), "WINCH BEAM (BEYOND, SE)", 8.5, "#9A9078", "middle")
    kit_cut_walls(sh, "OuterBailey")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(3.8, FZ + 0.95), "CLAY BASIN", "on a pedestal · loot"),
        (*KE(3.6, FZ + 2.3), "CISTERN WINCH", "posts on the rim, beam 2.35 m"),
    ], 610, 230, 310, slope=1.0)
    sh.callouts([
        (*KE(-4.35, FZ + 0.8), "HYDRIAE", "water jars on a cypress stand"),
        (*KE(-3.0, FZ + 0.1), "PAVED COURT", "limestone flags"),
    ], 330, 230, 300, anchor="end")
    kit_clear_note(sh, "OuterBailey", x=0.0, text="3.60 clear · open to the sky")

    # ---- plan ----
    kit_plan(sh, "OuterBailey", floor="#34302A", wall="#3A332A", trim=MUD)
    for i in range(-5, 6):
        for j in range(-5, 6):
            if (i + j) % 2 == 0:
                kprect(sh, i - 0.5, j - 0.5, i + 0.5, j + 0.5, "#3A352D", op=.6)
    sh.circle(*KP(3.6, -3.6), 1.1 * PK, STONE, "#0E0C09", 1)
    sh.circle(*KP(3.6, -3.6), 0.85 * PK, WATER, "#0E0C09", .8)
    plan_box(sh, 3.6, -3.0, 1.6, 0.8, lighten(STONE, .1))
    for x in (2.6, 4.6):
        plan_box(sh, x, -3.6, 0.14, 0.14, CYP)
    plan_box(sh, 3.6, -3.6, 2.1, 0.12, CYP)
    sh.circle(*KP(2.2, -4.8), 0.2 * PK, CYP, "#0E0C09", .6)
    sh.circle(*KP(3.6, -3.6), 1.0 * PK, "none", darken(STONE, .3), 3)            # the stone lip
    plan_box(sh, -3.8, 4.9, 1.6, 0.5, CYP)
    for dx in (-0.55, 0.55):
        plan_jar(sh, -3.8 + dx, 4.9, 0.40, TERRA)
    sh.circle(*KP(3.8, 4.2), 0.55 * PK, TERRA, "#0E0C09", .8)
    sh.circle(*KP(3.8, 4.2), 0.42 * PK, WATER)
    plan_box(sh, -3.8, -5.0, 2.2, 0.7, STONE)
    plan_box(sh, -3.8, -5.0, 2.0, 0.5, WATER)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    socket_label(sh, *KP(0, 4.55), "ARCHWAY N 2.60 × 2.59")
    socket_label(sh, *KP(-0.6, -4.85), "ARCHWAY S 2.60 × 2.59")
    socket_label(sh, *KP(3.6, -1.9), "CISTERN")
    socket_label(sh, *KP(-3.8, -4.2), "TROUGH")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.59 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("CISTERN", "SE, 2.20 m drum, lid half drawn, winch"),
                    ("WATER", "hydriae NW · basin NE · trough SW"),
                    ("LOOT", "L1–L4: cistern lid, jar stand, basin, bucket")])
    return sh
