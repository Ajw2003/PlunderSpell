"""BronzePalaceKitchen: the palace kitchen (stands in for KitchenRoom).

Section A-A east-west at y = 0, looking north: the round hearth with a tripod
cauldron over the fire in the NW, the domed bread oven and its table in the NE,
soot heavy on the walls. The quern bench and the pot bench to the south show in
the plan.
"""
from _bronze import *

ANCHORS = [(-3.8, 3.8, 1.79), (2.4, 4.9, 1.10), (3.6, -5.1, 0.80), (-5.1, -3.6, 0.80)]


def flame(sh, x, base, hh, w=10):
    a, tip = KE(x, base), KE(x, base + hh)
    sh.path(f"M{f(a[0] - w)} {f(a[1])} Q{f(a[0] - w * .8)} {f((a[1] + tip[1]) / 2)} {f(tip[0])} {f(tip[1])} "
            f"Q{f(a[0] + w)} {f((a[1] + tip[1]) / 2)} {f(a[0] + w)} {f(a[1])} Z", MADDER_, op=.9)


def build():
    mats = [("plaster", PLAST), ("clay", TERRA), ("soot", SOOT), ("bronze", BRONZE),
            ("cypress", CYP), ("stone", STONE), ("fire", MADDER_)]
    sh = room_sheet("BronzeAge", "InnerWard", "The Palace Kitchen", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE KITCHEN, LOOKING NORTH")
    kit_glow(sh, -3.8, FZ + 1.0, MADDER_, rx=320, ry=220, strength=.5)
    kit_glow(sh, 4.2, FZ + 0.6, MADDER_, rx=200, ry=150, strength=.3)

    # ---- section ----
    kit_slab(sh, FLOOR)
    kit_back_wall(sh, "InnerWard", PLAST, trim=BLUE, soot_depth=2.2)
    # NW: the round hearth, the fire, and the tripod cauldron standing in it.
    kerect(sh, -4.6, FZ, -3.0, FZ + 0.35, f"url(#{sh.lin(TERRA, 'v', .25, .5)})", darken(TERRA, .6), 1)
    for x, hh in ((-4.1, .35), (-3.85, .5), (-3.55, .4)):
        flame(sh, x, FZ + 0.35, hh)
    tripod(sh, -3.8, base=FZ + 0.35)
    # NE: the domed bread oven, its mouth glowing, and the table where the loaves wait.
    dome = [KE(3.3, FZ), KE(3.35, FZ + 0.5), KE(3.7, FZ + 1.05), KE(4.2, FZ + 1.2), KE(4.7, FZ + 1.05),
            KE(5.05, FZ + 0.5), KE(5.1, FZ)]
    sh.path(smooth_path(dome, tension=.35), f"url(#{sh.lin(TERRA, 'h', .3, .55)})", darken(TERRA, .6), 1.2)
    mouth = poly_path([KE(3.95, FZ + 0.1), KE(4.45, FZ + 0.1), KE(4.45, FZ + 0.45), KE(3.95, FZ + 0.45)])
    sh.path(mouth, "#2A1208", darken(TERRA, .7), .8)
    sh.path(mouth, MADDER_, op=.35)
    table(sh, 2.4, 1.0, 0.80, col=CYP)
    for dx in (-0.25, 0.1):
        cx, cy = KE(2.4 + dx, FZ + 0.86)
        sh.ellipse(cx, cy, 0.16 * SK, 0.06 * SK, "#C8A060", darken("#C8A060", .5), .7)
    kit_cut_walls(sh, "InnerWard")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(4.2, FZ + 0.9), "BREAD OVEN", "clay dome 1.80 m across, 1.20 m"),
        (*KE(2.4, FZ + 0.85), "LOAF TABLE", "1.00 × 0.60 m · loot"),
    ], 610, 230, 310, slope=1.0)
    sh.callouts([
        (*KE(-3.8, FZ + 1.3), "TRIPOD CAULDRON", "over the fire · loot"),
        (*KE(-3.3, FZ + 0.2), "ROUND HEARTH", "clay drum 1.60 × 0.35 m"),
        (*KE(-4.6, FZ + 3.0), "SOOT", "the walls black to 2.2 m down"),
    ], 330, 190, 330, anchor="end")
    kit_clear_note(sh, "InnerWard", x=0.0, text="4.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "InnerWard", floor="#2A2419", wall="#3A332A", trim=BLUE)
    sh.circle(*KP(-3.8, 3.8), 0.8 * PK, TERRA, "#0E0C09", .8)
    sh.circle(*KP(-3.8, 3.8), 0.45 * PK, MADDER_, op=.8)
    sh.circle(*KP(-3.8, 3.8), 0.30 * PK, BRONZE, "#0E0C09", .8)
    sh.circle(*KP(4.2, 4.2), 0.9 * PK, TERRA, "#0E0C09", .8)
    kprect(sh, 3.95, 3.25, 4.45, 3.4, "#2A1208")
    plan_box(sh, 2.4, 4.9, 1.0, 0.6, CYP)
    plan_box(sh, 3.6, -5.1, 3.0, 0.7, STONE)
    for x in (2.5, 4.7):
        plan_box(sh, x, -5.1, 0.5, 0.3, lighten(STONE, .2))
    plan_box(sh, -5.1, -3.6, 0.7, 3.0, TERRA)
    for y in (-4.7, -4.1, -3.1, -2.5):
        sh.circle(*KP(-5.1, y), 0.2 * PK, darken(TERRA, .3), "#0E0C09", .5)
    for x, y in ((-2.6, -4.8), (-3.3, -4.9)):
        plan_jar(sh, x, y, 0.34, TERRA)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "InnerWard")
    socket_label(sh, *KP(-3.8, 2.7), "HEARTH")
    socket_label(sh, *KP(4.2, 2.95), "OVEN")
    socket_label(sh, *KP(3.6, -4.45), "QUERNS")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.88 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("FIRE", "hearth NW, oven NE: both IGNIS sources"),
                    ("BENCHES", "querns S wall · cooking pots W wall"),
                    ("LOOT", "L1–L4: cauldron, loaf table, quern and pot benches")])
    return sh
