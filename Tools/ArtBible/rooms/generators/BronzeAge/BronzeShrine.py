"""BronzeShrine: the palace shrine (stands in for ChapelRoom).

Section A-A east-west at y = 0, looking north: the double axe on its stepped stand
in the NW, the stepped bench altar in the NE with the horns of consecration and
clay idols with raised arms, a painted procession behind. The worshippers'
benches fill the two south quadrants (plan).
"""
from _bronze import *

ANCHORS = [(4.6, 4.75, 0.75), (3.6, 3.3, 1.00), (-3.6, -2.4, 0.70), (3.6, -2.4, 0.70)]


def idol(sh, x, base, h=0.45, col="#C9A77A"):
    """A clay idol with raised arms: a bell skirt, a small head, arms up in a U."""
    a, b = KE(x - 0.1, base), KE(x + 0.1, base)
    top = KE(x, base + h * 0.72)
    sh.path(f"M{f(a[0])} {f(a[1])} L{f(b[0])} {f(b[1])} L{f(top[0] + 3)} {f(top[1])} L{f(top[0] - 3)} {f(top[1])} Z",
            f"url(#{sh.lin(col, 'h', .3, .5)})", darken(col, .55), .8)
    hx, hy = KE(x, base + h * 0.84)
    sh.circle(hx, hy, 0.06 * SK, col, darken(col, .55), .8)
    sh.path(f"M{f(top[0] - 3)} {f(top[1] + 3)} q-9 -2 -9 -14 M{f(top[0] + 3)} {f(top[1] + 3)} q9 -2 9 -14",
            "none", darken(col, .2), 2.4)


def labrys(sh, x, base, col=BRONZE):
    """The double axe on its pole, seen face-on: two curved blades."""
    cx, cy = KE(x, base)
    w, hgt = 0.5 * SK, 0.25 * SK
    for s in (-1, 1):
        sh.path(f"M{f(cx + s * 3)} {f(cy - 4)} Q{f(cx + s * w * .5)} {f(cy - hgt * .2)} {f(cx + s * w)} {f(cy - hgt)} "
                f"Q{f(cx + s * w * 1.12)} {f(cy)} {f(cx + s * w)} {f(cy + hgt)} Q{f(cx + s * w * .5)} {f(cy + hgt * .2)} "
                f"{f(cx + s * 3)} {f(cy + 4)} Z", f"url(#{sh.lin(col, 'h', .4, .45)})", darken(col, .55), 1)


def build():
    mats = [("plaster", PLAST), ("fresco", BLUE), ("haematite", RED), ("gypsum", GYP),
            ("bronze", BRONZE), ("clay", "#C9A77A"), ("soot", SOOT)]
    sh = room_sheet("BronzeAge", "InnerWard", "The Shrine", mats, "≤ 1.6k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE SHRINE, LOOKING NORTH")
    kit_glow(sh, 3.8, FZ + 1.4, "#A07030", rx=280, ry=200, strength=.4)

    # ---- section ----
    kit_slab(sh, FLOOR)
    kit_back_wall(sh, "InnerWard", PLAST, trim=BLUE, soot_depth=0.8)
    fresco_band(sh, "InnerWard", bottom=1.6, top=3.0, figures=True)
    # NE: the stepped bench altar against the north wall, horns on the top step, idols on the lower.
    kerect(sh, 2.5, FZ, 5.1, FZ + 0.45, f"url(#{sh.lin(GYP, 'v', .25, .45)})", darken(GYP, .6), .9)
    kerect(sh, 2.9, FZ + 0.45, 4.7, FZ + 0.80, f"url(#{sh.lin(GYP, 'v', .25, .45)})", darken(GYP, .6), .9)
    kerect(sh, 2.5, FZ + 0.40, 5.1, FZ + 0.45, RED, op=.8)
    horns(sh, 3.8, FZ + 0.80, 0.8, GYP)
    for x in (3.0, 3.35, 4.25):
        idol(sh, x, FZ + 0.45)
    # NW: the double axe on its stepped stand.
    kerect(sh, -4.2, FZ, -3.4, FZ + 0.30, f"url(#{sh.lin(GYP, 'v', .25, .45)})", darken(GYP, .6), .8)
    kerect(sh, -4.05, FZ + 0.30, -3.55, FZ + 0.60, f"url(#{sh.lin(GYP, 'v', .25, .45)})", darken(GYP, .6), .8)
    kerect(sh, -3.83, FZ + 0.60, -3.77, FZ + 2.00, BRONZE, darken(BRONZE, .5), .6)
    labrys(sh, -3.8, FZ + 2.00)
    # The offering table before the altar (in front, nearer the cut).
    table(sh, 3.6, 0.6, 0.70)
    kit_cut_walls(sh, "InnerWard")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(3.8, FZ + 1.3), "HORNS OF CONSECRATION", "gypsum, 0.80 m across"),
        (*KE(3.0, FZ + 0.75), "CLAY IDOLS", "arms raised · loot"),
        (*KE(4.9, FZ + 0.25), "STEPPED ALTAR", "2.60 × 0.90 m, two steps"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(-3.8, FZ + 2.1), "DOUBLE AXE", "bronze labrys on a 1.40 m pole"),
        (*KE(-4.4, FZ + 2.3), "PROCESSION FRESCO", "women bearing gifts"),
    ], 330, 200, 280, anchor="end")
    kit_clear_note(sh, "InnerWard", x=0.0, text="4.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "InnerWard", floor="#2E2719", wall="#3A332A", trim=BLUE)
    plan_box(sh, 3.8, 5.05, 2.6, 0.9, GYP)
    plan_box(sh, 3.8, 5.27, 1.8, 0.45, lighten(GYP, .15))
    kprect(sh, 3.4, 5.2, 4.2, 5.34, "#B8AD92")
    for x in (3.0, 3.35, 4.25):
        sh.circle(*KP(x, 4.75), 0.09 * PK, "#C9A77A", "#0E0C09", .5)
    plan_box(sh, 3.6, 3.3, 0.6, 0.6, FLOOR)
    plan_box(sh, -3.8, 4.2, 0.8, 0.8, GYP)
    plan_box(sh, -3.8, 4.2, 0.5, 0.5, lighten(GYP, .15))
    kprect(sh, -4.3, 4.18, -3.3, 4.22, BRONZE)
    for sx in (-1, 1):
        for y in (-2.4, -3.6, -4.8):
            plan_box(sh, sx * 3.6, y, 3.0, 0.4, darken(GYP, .2))
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "InnerWard")
    socket_label(sh, *KP(3.8, 4.1), "ALTAR")
    socket_label(sh, *KP(-3.8, 3.3), "DOUBLE AXE")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.88 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("ALTAR", "NE, two steps, horns and idols"),
                    ("BENCHES", "3 rows each side, S, facing north"),
                    ("LOOT", "L1–L4: altar step, offering table, front benches")])
    return sh
