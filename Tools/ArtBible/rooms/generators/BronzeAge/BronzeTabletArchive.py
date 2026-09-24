"""BronzeTabletArchive: the palace archive (stands in for GuardRoomInner).

Section A-A east-west at y = 0, looking north: clay benches along the north wall
stacked with baskets of tablets, the tablet shelf on the east wall end-on. The
scribe's table and the archive guard's post (spears, stool, sealed jars) are in
the south half (plan).
"""
from _bronze import *

ANCHORS = [(-3.6, 5.2, 0.90), (3.6, 5.2, 0.90), (3.6, -3.4, 1.05), (5.2, 3.2, 1.78), (-4.2, -3.2, 0.75)]
TABLET = "#B89A70"


def basket(sh, x, base, w=0.44, h=0.30):
    """A wicker basket of clay tablets in elevation."""
    kerect(sh, x - w / 2, base, x + w / 2, base + h, f"url(#{sh.lin('#8A6A40', 'v', .25, .5)})", darken("#8A6A40", .6), .7)
    for k in range(3):
        z = base + 0.07 + k * 0.08
        sh.line(*KE(x - w / 2 + .02, z), *KE(x + w / 2 - .02, z), darken("#8A6A40", .4), .8, op=.8)
    for k in range(4):
        kerect(sh, x - w / 2 + 0.05 + k * 0.1, base + h - 0.02, x - w / 2 + 0.12 + k * 0.1, base + h + 0.06, TABLET,
               darken(TABLET, .5), .5)


def build():
    mats = [("plaster", OCHRE), ("haematite", RED), ("clay", TERRA), ("tablets", TABLET),
            ("wicker", "#8A6A40"), ("cypress", CYP), ("bronze", BRONZE)]
    sh = room_sheet("BronzeAge", "InnerWard", "The Tablet Archive", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE ARCHIVE, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 2.0, "#6A5030", rx=380, ry=220, strength=.3)

    # ---- section ----
    kit_slab(sh, FLOOR)
    kit_back_wall(sh, "InnerWard", OCHRE, trim=BLUE, soot_depth=0.6)
    for x0, x1 in ((-IN, -1.3), (1.3, IN)):
        kerect(sh, x0, FZ, x1, FZ + 0.40, f"url(#{sh.lin(RED, 'v', .2, .45)})", darken(RED, .6), .6)
    # Scribes' tallies scratched on the plaster above the benches.
    for x0 in (-5.0, 2.2):
        for k in range(9):
            sh.line(*KE(x0 + k * 0.12, FZ + 1.5), *KE(x0 + k * 0.12 + 0.02, FZ + 1.75), RED, 1.2, op=.7)
        sh.line(*KE(x0 - 0.05, FZ + 1.55), *KE(x0 + 1.05, FZ + 1.7), RED, 1.2, op=.7)
    # Clay benches along the north wall, baskets of tablets on them.
    for sx in (-1, 1):
        x = sx * 3.6
        kerect(sh, x - 1.6, FZ, x + 1.6, FZ + 0.60, f"url(#{sh.lin(TERRA, 'v', .25, .5)})", darken(TERRA, .6), .8)
        for dx in (-1.0, 1.0):
            basket(sh, x + dx, FZ + 0.60)
    # The tablet shelf on the east wall, seen end-on.
    kerect(sh, IN - 0.45, FZ, IN, FZ + 2.15, f"url(#{sh.lin(CYP, 'h', .25, .5)})", darken(CYP, .6), .7)
    for z in (0.35, 0.90, 1.45):
        kerect(sh, IN - 0.45, FZ + z - 0.03, IN, FZ + z + 0.03, darken(CYP, .2))
        kerect(sh, IN - 0.4, FZ + z + 0.03, IN - 0.05, FZ + z + 0.12, TABLET, darken(TABLET, .5), .5)
    kit_cut_walls(sh, "InnerWard")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(4.6, FZ + 0.85), "TABLET BASKETS", "wicker, on clay benches · loot"),
        (*KE(5.25, FZ + 1.5), "TABLET SHELF", "east wall, three boards"),
    ], 610, 240, 320, slope=1.0)
    sh.callouts([
        (*KE(-4.5, FZ + 1.62), "TALLY MARKS", "scratched and painted, 1.50 m"),
        (*KE(-3.0, FZ + 0.3), "CLAY BENCH", "3.20 × 0.60 × 0.60 m"),
        (*KE(-4.8, FZ + 0.2), "HAEMATITE DADO", "0.40 m"),
    ], 330, 190, 330, anchor="end")
    kit_clear_note(sh, "InnerWard", x=0.0, text="4.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "InnerWard", floor="#2E2719", wall="#3A332A", trim=BLUE)
    for sx in (-1, 1):
        plan_box(sh, sx * 3.6, 5.2, 3.2, 0.6, TERRA)
        for dx in (-1.0, 1.0):
            sh.circle(*KP(sx * 3.6 + dx, 5.2), 0.22 * PK, "#8A6A40", "#0E0C09", .6)
    plan_box(sh, 5.2, 3.2, 0.45, 2.2, darken(CYP, .15))
    plan_box(sh, 3.6, -3.4, 1.4, 0.8, CYP)
    for k in range(4):
        plan_box(sh, 3.2 + k * 0.25, -3.3, 0.12, 0.07, TABLET)
    sh.circle(*KP(3.6, -4.2), 0.2 * PK, CYP, "#0E0C09", .6)
    plan_box(sh, -5.3, -3.6, 0.15, 1.8, CYP)
    for y in (-4.2, -3.8, -3.4, -3.0):
        sh.circle(*KP(-5.3, y), 0.05 * PK, BRONZE)
    sh.circle(*KP(-4.2, -3.2), 0.22 * PK, CYP, "#0E0C09", .6)
    for x in (-4.8, -4.1, -3.4, -2.7):
        plan_jar(sh, x, -5.1, 0.36, TERRA, lid=GYP)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "InnerWard")
    socket_label(sh, *KP(3.6, -2.55), "SCRIBE'S TABLE")
    socket_label(sh, *KP(-3.9, -2.2), "GUARD POST")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.88 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("ARCHIVE", "tablet baskets N · shelf E · scribe SE"),
                    ("GUARD", "spear rack, stool and sealed jars, SW"),
                    ("LOOT", "L1–L5: benches, table, shelf, guard's stool")])
    return sh
