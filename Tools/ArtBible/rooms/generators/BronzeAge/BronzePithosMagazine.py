"""BronzePithosMagazine: the kit version of the art bible's Pithos Magazine
(docs/art/concept/bronze/pithos-store.svg), standing in for a storehouse in the palace.

One clay bench per quadrant, each carrying two rows of three man-high pithoi: 24
jars, grain on the west benches, oil (lidded, darker) on the east. Section A-A
east-west at y = 0, looking north, shows the north benches' front jars; the plan
shows all four benches, the wall aisles, the scribe's bench and the oil pools.
"""
from _bronze import *

JAR_H, JAR_B, BENCH_H = 1.70, 0.90, 0.30
ROWS_X = (2.5, 3.8)
ROWS_Y = (2.4, 3.5, 4.6)
ANCHORS = [(-5.2, -3.5, 1.10), (5.1, 2.3, 1.00), (-5.0, 2.3, 0.70)]


def build():
    mats = [("plaster", OCHRE), ("terracotta", TERRA), ("oil clay", OILCLAY), ("clay floor", "#7A5A3E"),
            ("grain", "#C8A868"), ("fresco", BLUE), ("soot", SOOT)]
    sh = room_sheet("BronzeAge", "InnerWard", "The Pithos Magazine", mats, "≤ 4.8k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE CROSS-AISLE, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 2.0, "#6A5030", rx=420, ry=220, strength=.35)

    # ---- section ----
    kit_slab(sh, "#7A5A3E")
    kit_back_wall(sh, "InnerWard", OCHRE, trim=BLUE, soot_depth=0.8)
    # Scribes' row numbers painted on the wall at 1.80 m, over each bench (Linear-B-ish marks).
    for x0 in (-4.2, 2.6):
        for k, mark in enumerate(("⊢", "ᛉ", "⊥")):
            sh.text(*KE(x0 + k * 0.55, FZ + 1.9), mark, 14, RED, "middle", family="serif")
    # The north benches (y 1.9–5.1) and their front row of jars; the back rows peek over.
    for sx, col, lid in ((-1, TERRA, None), (1, OILCLAY, GYP)):
        xs = [sx * x for x in ROWS_X]
        kerect(sh, min(xs) - 0.6, FZ, max(xs) + 0.6, FZ + BENCH_H, f"url(#{sh.lin('#8A6A4A', 'v', .25, .5)})",
               darken("#8A6A4A", .6), .8)
        for x in xs:
            jar(sh, x + 0.12 * sx, FZ + BENCH_H + 0.04, JAR_H, JAR_B * .92, darken(col, .25), bands=(0.62 * JAR_H,),
                lid=lid, lugs=False)
        for x in xs:
            jar(sh, x, FZ + BENCH_H, JAR_H, JAR_B, col, bands=(0.62 * JAR_H,), lid=lid)
            if sx > 0:                                    # oil drips from the rim
                for dx in (-0.08, 0.1):
                    sh.line(*KE(x + dx, FZ + BENCH_H + JAR_H * .93), *KE(x + dx * 1.4, FZ + BENCH_H + JAR_H * .6),
                            "#1E1712", 2, op=.7)
    # A step-ladder leaning on the NW front jar from the west aisle.
    lx0, lx1 = -5.2, -4.35
    sh.line(*KE(lx0, FZ), *KE(lx1, FZ + 1.95), CYP, 3)
    sh.line(*KE(lx0 + 0.35, FZ), *KE(lx1 + 0.3, FZ + 1.95), CYP, 3)
    for k in range(1, 7):
        t = k / 7
        a = KE(lx0 + (lx1 - lx0) * t, FZ + 1.95 * t)
        b = KE(lx0 + 0.35 + (lx1 + 0.3 - lx0 - 0.35) * t, FZ + 1.95 * t)
        sh.line(a[0], a[1], b[0], b[1], CYP, 2)
    # Oil pools on the floor in front of the east benches (flat, flammable).
    sh.ellipse(*KE(3.2, FZ + 0.01), 0.7 * SK, 3, "#1E1712", op=.8)
    kit_cut_walls(sh, "InnerWard")
    khuman(sh, 0.6)
    sh.callouts([
        (*KE(3.8, FZ + 1.4), "OIL PITHOI", "1.70 m, lidded · 12"),
        (*KE(3.2, FZ + 0.02), "OIL POOL", "flat · IGNIS lights the aisle"),
        (*KE(2.6, FZ + 1.9), "ROW MARKS", "painted at 1.80 m"),
    ], 610, 200, 320, slope=1.0)
    sh.callouts([
        (*KE(-3.8, FZ + 1.0), "GRAIN PITHOI", "belly 0.90 m, rope band"),
        (*KE(-4.9, FZ + 1.0), "STEP-LADDER", "leant on a jar for ladling"),
        (*KE(-3.2, FZ + 0.15), "CLAY BENCH", "2.40 × 3.20 × 0.30 m"),
    ], 330, 190, 330, anchor="end")
    kit_clear_note(sh, "InnerWard", x=0.0, text="4.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "InnerWard", floor="#2A2419", wall="#3A332A", trim=BLUE)
    for sx in (-1, 1):
        for sy in (-1, 1):
            xs = [sx * x for x in ROWS_X]
            ys = [sy * y for y in ROWS_Y]
            kprect(sh, min(xs) - 0.6, min(ys) - 0.5, max(xs) + 0.6, max(ys) + 0.5, "#6A5038", "#0E0C09", .6)
            for x in xs:
                for y in ys:
                    plan_jar(sh, x, y, JAR_B, OILCLAY if sx > 0 else TERRA, lid=GYP if sx > 0 else None)
    for x, y, r in ((3.2, 1.9, 0.55), (4.8, -2.9, 0.4)):
        sh.ellipse(*KP(x, y), r * PK, r * .6 * PK, "#1E1712", op=.85)
    plan_box(sh, -5.2, -3.5, 0.6, 1.8, "#8A6A4A")
    for k in range(6):
        plan_box(sh, -5.2, -4.2 + k * 0.28, 0.12, 0.07, "#B89A70")
    plan_box(sh, 5.1, 2.3, 0.5, 0.5, FLOOR)
    sh.circle(*KP(-5.0, 2.3), 0.28 * PK, "#C8A868", "#0E0C09", .6)
    kprect(sh, -5.25, 3.0, -4.35, 3.35, CYP, "#0E0C09", .5)                    # ladder
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "InnerWard")
    socket_label(sh, *KP(-3.15, 1.35), "GRAIN")
    socket_label(sh, *KP(3.15, 1.35), "OIL")
    socket_label(sh, *KP(-3.4, -1.35), "GRAIN")
    socket_label(sh, *KP(3.4, -1.35), "OIL")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.88 · all open"),
                    ("CLEAR CROSS", "dashed · the cross-aisles, 3.2 m wide"),
                    ("PITHOI", "24 on 4 benches · grain W, oil E"),
                    ("WALL AISLE", "1.15 m, pinched by the scribe's bench"),
                    ("LOOT", "L1–L3: scribe's bench, ladle table, basket")])
    return sh
