"""BronzeFoundry: the bronze-smith's shop (stands in for BlacksmithShop).

Section A-A east-west at y = 0, looking north: the clay shaft furnace glowing in
the NW with its two bag bellows, the stone mould bench with crucibles against the
north wall in the NE, the tool shelf on the east wall end-on. The ingot stack,
charcoal heap, trough and anvil stone are in the south half (plan).
"""
from _bronze import *

ANCHORS = [(3.6, 5.15, 1.10), (4.0, -4.3, 0.66), (-2.6, -3.0, 0.80), (5.2, 2.9, 1.23)]
CHAR = "#1E1A16"


def build():
    mats = [("plaster", OCHRE), ("clay", TERRA), ("soot", SOOT), ("bronze", BRONZE),
            ("stone", STONE), ("hide", "#6E4A3A"), ("fire", MADDER_)]
    sh = room_sheet("BronzeAge", "OuterBailey", "The Foundry", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE FOUNDRY, LOOKING NORTH")
    kit_glow(sh, -4.2, FZ + 0.8, MADDER_, rx=340, ry=240, strength=.6)

    # ---- section ----
    kit_slab(sh, "#6A4E36")
    kit_back_wall(sh, "OuterBailey", OCHRE, trim=MUD, soot_depth=1.8)
    # NW: the shaft furnace, a clay cone with a glowing mouth.
    fur = [KE(-5.0, FZ), KE(-4.9, FZ + 0.6), KE(-4.55, FZ + 1.4), KE(-3.85, FZ + 1.4), KE(-3.5, FZ + 0.6), KE(-3.4, FZ)]
    sh.path(smooth_path(fur, tension=.3), f"url(#{sh.lin(TERRA, 'h', .3, .55)})", darken(TERRA, .6), 1.2)
    kerect(sh, -4.45, FZ + 1.36, -3.95, FZ + 1.44, SOOT)
    mouth = poly_path([KE(-4.45, FZ + 0.1), KE(-3.95, FZ + 0.1), KE(-3.95, FZ + 0.45), KE(-4.45, FZ + 0.45)])
    sh.path(mouth, MADDER_, darken(MADDER_, .5), .8)
    sh.path(mouth, "#F0B040", op=.35)
    for x, hh in ((-4.3, .3), (-4.2, .45), (-4.1, .35)):
        a, tip = KE(x, FZ + 1.44), KE(x, FZ + 1.44 + hh)
        sh.path(f"M{f(a[0] - 5)} {f(a[1])} Q{f(a[0] - 4)} {f((a[1] + tip[1]) / 2)} {f(tip[0])} {f(tip[1])} "
                f"Q{f(a[0] + 5)} {f((a[1] + tip[1]) / 2)} {f(a[0] + 5)} {f(a[1])} Z", MADDER_, op=.75)
    # The bag bellows east of the furnace (the second is behind the first), nozzle into it.
    bx, by = KE(-3.0, FZ + 0.18)
    sh.ellipse(bx, by, 0.35 * SK, 0.18 * SK, f"url(#{sh.lin('#6E4A3A', 'v', .3, .5)})", darken("#6E4A3A", .5), .9)
    sh.line(bx - 0.3 * SK, by, *KE(-3.5, FZ + 0.25), darken(TERRA, .3), 3)
    # NE: the stone mould bench, moulds and crucibles on it.
    kerect(sh, 2.3, FZ, 4.9, FZ + 0.80, f"url(#{sh.lin(STONE, 'v', .25, .5)})", darken(STONE, .6), .9)
    for x in (2.6, 3.1):
        kerect(sh, x - 0.2, FZ + 0.80, x + 0.2, FZ + 0.92, darken(STONE, .25), darken(STONE, .6), .6)
    for x in (4.2, 4.6):
        kerect(sh, x - 0.12, FZ + 0.80, x + 0.12, FZ + 0.95, SOOT, "#0E0C09", .6)
    # The tool shelf on the east wall, end-on: tongs and crucibles on two boards.
    kerect(sh, IN - 0.45, FZ, IN, FZ + 1.60, f"url(#{sh.lin(CYP, 'h', .25, .5)})", darken(CYP, .6), .7)
    for z in (0.35, 0.90):
        kerect(sh, IN - 0.45, FZ + z - 0.03, IN, FZ + z + 0.03, darken(CYP, .2))
    kit_cut_walls(sh, "OuterBailey")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(3.6, FZ + 0.6), "MOULD BENCH", "stone, 2.60 × 0.70 m · loot"),
        (*KE(4.4, FZ + 0.92), "CRUCIBLES", "clay, soot-black"),
        (*KE(5.25, FZ + 1.2), "TOOL SHELF", "tongs, ladles, east wall"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(-4.2, FZ + 1.0), "SHAFT FURNACE", "clay cone 1.60 × 1.40 m"),
        (*KE(-3.0, FZ + 0.2), "BAG BELLOWS", "goat hide, clay nozzles"),
    ], 330, 220, 300, anchor="end")
    kit_clear_note(sh, "OuterBailey", x=0.0, text="3.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "OuterBailey", floor="#2E2519", wall="#3A332A", trim=MUD)
    sh.circle(*KP(-4.2, 4.2), 0.8 * PK, TERRA, "#0E0C09", .8)
    sh.circle(*KP(-4.2, 4.2), 0.35 * PK, MADDER_)
    kprect(sh, -4.45, 3.35, -3.95, 3.5, MADDER_)
    for y in (4.6, 3.8):
        sh.ellipse(*KP(-3.0, y), 0.35 * PK, 0.25 * PK, "#6E4A3A", "#0E0C09", .6)
    plan_box(sh, 3.6, 5.15, 2.6, 0.7, STONE)
    for x in (2.6, 3.1):
        plan_box(sh, x, 5.15, 0.4, 0.3, darken(STONE, .25))
    for x in (4.2, 4.6):
        sh.circle(*KP(x, 5.15), 0.12 * PK, SOOT)
    plan_box(sh, 5.2, 2.9, 0.45, 1.6, darken(CYP, .15))
    plan_box(sh, 4.0, -4.3, 1.8, 1.2, darken(CYP, .1))
    for j in range(2):
        for k in range(2):
            plan_box(sh, 3.4 + j * 0.62 + 0.3, -4.6 + k * 0.42 + 0.1, 0.56, 0.36, BRONZE)
    sh.circle(*KP(-4.2, -4.3), 0.9 * PK, CHAR, "#0E0C09", .8)
    plan_box(sh, -2.6, -5.0, 1.4, 0.6, STONE)
    plan_box(sh, -2.6, -5.0, 1.2, 0.4, "#5E8A96")
    plan_box(sh, -2.6, -3.0, 0.6, 0.6, darken(STONE, .15))
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "OuterBailey")
    socket_label(sh, *KP(-4.2, 2.8), "FURNACE")
    socket_label(sh, *KP(4.0, -3.3), "INGOTS")
    socket_label(sh, *KP(-4.2, -3.1), "CHARCOAL")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.59 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("FURNACE", "NW, lit · bellows E of it (IGNIS source)"),
                    ("WORK", "moulds N · tools E · ingots SE · anvil SW"),
                    ("LOOT", "L1–L4: mould bench, ingots, anvil, tool shelf")])
    return sh
