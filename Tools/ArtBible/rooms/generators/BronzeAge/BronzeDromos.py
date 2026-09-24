"""BronzeDromos: the passage into the tombs (stands in for CryptAntechamber).

Section A-A east-west at y = 0, looking north: the corbelled ashlar courses along
the east and west walls in profile, stepping inward as they rise; the two north
grave stelae on their bases; amphora offerings; a painted runner down the axis.
"""
from _bronze import *

ANCHORS = [(3.2, 4.65, 0.60), (-3.2, -4.65, 0.60), (-3.2, 3.6, 1.00)]
ASHLAR = "#9A8C72"
COURSES = ((0.0, 1.0, 0.8), (1.0, 2.0, 1.0), (2.0, 2.7, 1.2))     # (bottom, top, depth from the wall)


def stele(sh, x, base):
    """A grave stele on its base: a tall slab with a carved chariot and spiral panels."""
    kerect(sh, x - 0.45, base, x + 0.45, base + 0.30, f"url(#{sh.lin(ASHLAR, 'v', .25, .5)})", darken(ASHLAR, .6), .8)
    slab = poly_path([KE(x - 0.3, base + 0.30), KE(x + 0.3, base + 0.30), KE(x + 0.27, base + 1.60), KE(x - 0.27, base + 1.60)])
    sh.path(slab, f"url(#{sh.lin(STONE, 'h', .3, .5)})", darken(STONE, .6), 1)
    spiral_band(sh, KE(x - 0.24, 0)[0], KE(x + 0.24, 0)[0], KE(0, base + 1.52)[1], 0.16 * SK, darken(STONE, .45))
    cx, cy = KE(x, base + 0.75)
    sh.circle(cx - 6, cy + 8, 5, "none", darken(STONE, .45), 1.2)          # a chariot wheel
    sh.path(f"M{f(cx - 14)} {f(cy + 3)} l16 0 l6 -10 M{f(cx + 2)} {f(cy - 7)} l8 -10 l6 2", "none", darken(STONE, .45), 1.2)


def build():
    mats = [("limestone", STONE), ("ashlar", ASHLAR), ("clay", TERRA), ("haematite", RED),
            ("soot", SOOT), ("bronze", BRONZE), ("gold", GOLD)]
    sh = room_sheet("BronzeAge", "Crypt", "The Dromos", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W ACROSS THE DROMOS, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 1.0, "#6A5A3A", rx=320, ry=160, strength=.3)

    # ---- section ----
    kit_slab(sh, "#3A332A")
    kit_back_wall(sh, "Crypt", STONE, trim=ASHLAR, soot_depth=0.6)
    for x0, x1 in ((-IN, -1.3), (1.3, IN)):                   # coursed ashlar joints on the back wall
        for k in range(1, 5):
            sh.line(*KE(x0, FZ + k * 0.55), *KE(x1, FZ + k * 0.55), darken(STONE, .35), .8, op=.6)
    # Corbelled courses along the east and west walls, in profile: each steps further in as it rises.
    for sgn in (-1, 1):
        for bottom, top, depth in COURSES:
            x_in = sgn * (IN - depth)
            xa, xb = sorted((x_in, sgn * IN))
            kerect(sh, xa, FZ + bottom, xb, FZ + top, f"url(#{sh.lin(ASHLAR, 'h', .25, .5)})", darken(ASHLAR, .6), .9)
    # The north stelae and the amphora offerings at their feet.
    for x in (-3.2, 3.2):
        stele(sh, x, FZ)
    for x in (2.3, -2.4):
        jar(sh, x, FZ, 0.62, 0.34, TERRA, rim=0.12, bands=(0.35,))
    table(sh, -3.2, 0.6, 0.70, col=ASHLAR)
    kerect(sh, -0.6, FZ, 0.6, FZ + 0.03, RED, op=.9)                        # the runner, end-on
    kit_cut_walls(sh, "Crypt")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(3.2, FZ + 1.1), "GRAVE STELE", "carved chariot, spirals"),
        (*KE(3.2, FZ + 0.2), "STELE BASE", "offerings laid here · loot"),
        (*KE(4.9, FZ + 2.3), "CORBELLED COURSES", "step 0.2 m in, three high"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(-2.4, FZ + 0.4), "AMPHORA OFFERINGS", "wine and oil for the dead"),
        (*KE(0.0, FZ + 0.02), "PAINTED RUNNER", "1.20 m, haematite, flat"),
    ], 330, 230, 300, anchor="end")
    kit_clear_note(sh, "Crypt", x=0.0, text="3.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Crypt", floor="#1E1B16", wall="#3A332A", trim=ASHLAR)
    kprect(sh, -0.6, -IN, 0.6, IN, RED, op=.75)
    for sgn in (-1, 1):
        for sy in (-1, 1):
            for _, _, depth in COURSES:
                xa, xb = sorted((sgn * (IN - depth), sgn * IN))
                kprect(sh, xa, sy * 2.0, xb, sy * 5.4, ASHLAR, "#0E0C09", .4, op=.55)
    for x, y in ((3.2, 4.8), (-3.2, 4.8), (3.2, -4.8), (-3.2, -4.8)):
        plan_box(sh, x, y, 0.9, 0.5, ASHLAR)
        plan_box(sh, x, y + (0.1 if y > 0 else -0.1), 0.6, 0.15, STONE)
    for x, y in ((2.3, 4.4), (-2.4, 4.3), (2.4, -4.2)):
        plan_jar(sh, x, y, 0.34, TERRA)
    plan_box(sh, -3.2, 3.6, 0.6, 0.6, ASHLAR)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Crypt")
    socket_label(sh, *KP(4.6, 1.95), "CORBELLED")
    socket_label(sh, *KP(-4.6, 1.95), "CORBELLED")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.16 · all open"),
                    ("CLEAR CROSS", "dashed · the runner lies flat along it"),
                    ("DROMOS", "corbelled courses E and W walls, in the quadrants"),
                    ("STELAE", "4, one per quadrant, on bases"),
                    ("LOOT", "L1–L3: two stele bases, offering table")])
    return sh
