"""BronzeLarnaxVault: the larnax vault (stands in for BurialVault).

Two painted clay chest-coffins per quadrant, one along the north or south wall and
one along the east or west wall, each on four short legs under a gabled lid, and a
storage jar in every corner. Section A-A east-west at y = 0, looking north: the
north larnakes side-on, the east and west ones end-on, the corner jars.
"""
from _bronze import *

LID = 0.12 + 0.55 + 0.20
ALONG_WALL = [(sx * 3.3, sy * 4.9, True) for sx in (-1, 1) for sy in (-1, 1)]
ALONG_SIDE = [(sx * 4.95, sy * 2.9, False) for sx in (-1, 1) for sy in (-1, 1)]
ANCHORS = [(x, y, FZ + LID) for x, y, _ in ALONG_WALL + ALONG_SIDE]


def build():
    mats = [("limestone", STONE), ("larnax clay", TERRA), ("haematite", RED), ("fresco", BLUE),
            ("ashlar", "#9A8C72"), ("soot", SOOT), ("linen", LINEN)]
    sh = room_sheet("BronzeAge", "Crypt", "The Larnax Vault", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE VAULT, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 0.8, "#6A5A3A", rx=320, ry=150, strength=.3)

    # ---- section ----
    kit_slab(sh, "#3A332A")
    kit_back_wall(sh, "Crypt", STONE, trim="#9A8C72", soot_depth=0.6)
    for x0, x1 in ((-IN, -1.3), (1.3, IN)):
        for k in range(1, 5):
            sh.line(*KE(x0, FZ + k * 0.55), *KE(x1, FZ + k * 0.55), darken(STONE, .35), .8, op=.6)
    # Corner jars, behind.
    for x in (-4.9, 4.9):
        jar(sh, x, FZ, 1.0, 0.6, darken(TERRA, .2), bands=(0.6,))
    # North larnakes side-on.
    for x in (-3.3, 3.3):
        larnax(sh, x, 1.6)
    # East and west larnakes end-on, nearer the cut.
    for x in (-4.95, 4.95):
        larnax(sh, x, 0.6)
    kit_cut_walls(sh, "Crypt")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(3.3, FZ + 0.45), "PAINTED LARNAX", "1.60 × 0.60 m, bands, octopus"),
        (*KE(3.3, FZ + LID - 0.05), "GABLED LID", "offerings on it · loot"),
        (*KE(4.95, FZ + 0.4), "END-ON LARNAX", "E and W walls"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(-4.9, FZ + 0.8), "STORAGE JAR", "one in each corner, 1.00 m"),
    ], 330, 250, 250, anchor="end")
    kit_clear_note(sh, "Crypt", x=2.6, text="3.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Crypt", floor="#1E1B16", wall="#3A332A", trim="#9A8C72")
    for x, y, along_x in ALONG_WALL + ALONG_SIDE:
        w, d = (1.6, 0.6) if along_x else (0.6, 1.6)
        plan_box(sh, x, y, w + 0.06, d + 0.06, TERRA)
        if along_x:
            kprect(sh, x - 0.8, y - 0.02, x + 0.8, y + 0.02, darken(TERRA, .45))
        else:
            kprect(sh, x - 0.02, y - 0.8, x + 0.02, y + 0.8, darken(TERRA, .45))
    for sx in (-1, 1):
        for sy in (-1, 1):
            plan_jar(sh, sx * 4.9, sy * 4.9, 0.6, darken(TERRA, .2))
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Crypt")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.16 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("LARNAKES", "8, two per quadrant, on 0.12 m legs"),
                    ("JARS", "a storage jar in each corner"),
                    ("LOOT", "L1–L8: the larnax lids")])
    return sh
