"""BronzeOilPress: the olive press and oil store (stands in for StorehouseRoom).

Section A-A east-west at y = 0, looking north: the lever press in the NW, its beam
socketed into the west wall and running east over the stone press bed to the
stone weights on their rope; an amphora rack against the north wall in the NE.
The second rack, the small pithoi and the jug table are to the south (plan).
"""
from _bronze import *

ANCHORS = [(-2.95, 3.75, 0.70), (3.6, 5.1, 1.08), (3.6, -5.1, 1.08), (-2.6, -3.4, 1.00)]
OLIVE = "#6A6A3A"


def amphora_rack(sh, x0, x1, base=FZ, col=CYP):
    """An amphora rack in elevation: a board on posts over a row of amphorae."""
    for x in (x0, x1 - 0.08):
        kerect(sh, x, base, x + 0.08, base + 0.75, col, darken(col, .6), .6)
    kerect(sh, x0, base + 0.72, x1, base + 0.78, f"url(#{sh.lin(col, 'v', .25, .5)})", darken(col, .6), .7)
    kerect(sh, x0, base + 0.30, x1, base + 0.34, darken(col, .2))
    n = int((x1 - x0) / 0.55)
    for k in range(n):
        jar(sh, x0 + 0.3 + k * 0.55, base, 0.62, 0.34, TERRA, rim=0.12, bands=(0.35,), lugs=True)


def build():
    mats = [("plaster", OCHRE), ("limestone", STONE), ("cypress", CYP), ("clay", TERRA),
            ("olive oil", OLIVE), ("rope", "#9C8A60"), ("mud-brick", MUD)]
    sh = room_sheet("BronzeAge", "OuterBailey", "The Oil Press", mats, "≤ 3.2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE PRESS, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 1.6, "#5A5030", rx=380, ry=200, strength=.3)

    # ---- section ----
    kit_slab(sh, "#6A4E36")
    kit_back_wall(sh, "OuterBailey", OCHRE, trim=MUD, soot_depth=0.4)
    # Oil stains on the lower wall behind the press.
    kerect(sh, -5.2, FZ, -2.6, FZ + 0.8, OLIVE, op=.25)
    # NW: the press bed with olive frails on it, the collecting jar in front (dimmer, south of the bed).
    kerect(sh, -4.1, FZ, -2.7, FZ + 0.40, f"url(#{sh.lin(STONE, 'v', .25, .5)})", darken(STONE, .6), .9)
    for k in range(2):
        kerect(sh, -4.0, FZ + 0.40 + k * 0.12, -3.2, FZ + 0.52 + k * 0.12, "#8A7040", darken("#8A7040", .5), .6)
    # The beam: socketed in the west wall at 1.70 m, down to 1.25 m at its free end over the weights.
    beam = poly_path([KE(-IN, FZ + 1.60), KE(-2.0, FZ + 1.15), KE(-2.0, FZ + 1.35), KE(-IN, FZ + 1.80)])
    sh.path(beam, f"url(#{sh.lin(CYP, 'v', .25, .5)})", darken(CYP, .6), 1)
    kerect(sh, -3.68, FZ + 0.64, -3.52, FZ + 1.43, CYP, darken(CYP, .6), .6)        # the pressing board's post
    sh.line(*KE(-2.05, FZ + 1.2), *KE(-2.05, FZ + 0.5), "#9C8A60", 2)
    for k in range(2):
        kerect(sh, -2.35, FZ + k * 0.25, -1.75, FZ + 0.25 + k * 0.25, f"url(#{sh.lin(STONE, 'v', .3, .5)})",
               darken(STONE, .6), .8)
    # NE: an amphora rack against the north wall.
    amphora_rack(sh, 2.1, 5.1)
    kit_cut_walls(sh, "OuterBailey")
    khuman(sh, -0.6)
    sh.callouts([
        (*KE(3.6, FZ + 0.76), "AMPHORA RACK", "board over 5 jars · loot"),
        (*KE(2.4, FZ + 0.4), "OIL AMPHORAE", "sealed, 0.62 m"),
    ], 610, 230, 310, slope=1.0)
    sh.callouts([
        (*KE(-4.4, FZ + 1.7), "PRESS BEAM", "3.50 m, socketed in the wall"),
        (*KE(-3.5, FZ + 0.25), "PRESS BED", "stone, olive frails on it"),
        (*KE(-2.05, FZ + 0.35), "WEIGHTS", "two stones on a rope"),
    ], 330, 190, 330, anchor="end")
    kit_clear_note(sh, "OuterBailey", x=0.0, text="3.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "OuterBailey", floor="#2E2519", wall="#3A332A", trim=MUD)
    kprect(sh, -5.4, 3.0, -2.4, 5.4, OLIVE, op=.25)
    plan_box(sh, -3.4, 4.2, 1.4, 1.4, STONE)
    sh.circle(*KP(-3.6, 4.3), 0.4 * PK, "#8A7040", "#0E0C09", .6)
    plan_box(sh, -3.725, 4.2, 3.45, 0.2, CYP)
    sh.circle(*KP(-2.05, 4.2), 0.3 * PK, STONE, "#0E0C09", .8)
    plan_box(sh, -3.4, 3.35, 0.12, 0.3, STONE)
    plan_jar(sh, -3.4, 3.1, 0.5, TERRA)
    for y in (5.1, -5.1):
        plan_box(sh, 3.6, y, 3.0, 0.45, CYP)
        for k in range(5):
            sh.circle(*KP(2.4 + k * 0.55, y), 0.1 * PK, TERRA)
    for x, y in ((-4.6, -4.6), (-3.5, -4.8), (-4.7, -3.4)):
        plan_jar(sh, x, y, 0.7, TERRA)
    plan_box(sh, -2.6, -3.4, 0.6, 0.6, CYP)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "OuterBailey")
    socket_label(sh, *KP(-3.6, 2.35), "PRESS")
    socket_label(sh, *KP(3.6, 4.3), "AMPHORAE")
    socket_label(sh, *KP(3.6, -4.35), "AMPHORAE")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.59 · all open"),
                    ("CLEAR CROSS", "dashed · the beam's weights stop at x −1.75"),
                    ("PRESS", "NW, lever beam from the west wall"),
                    ("OIL", "amphora racks NE and SE · pithoi SW"),
                    ("LOOT", "L1–L4: press bed, two racks, jug table")])
    return sh
