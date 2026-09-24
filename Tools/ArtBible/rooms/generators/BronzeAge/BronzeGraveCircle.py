"""BronzeGraveCircle: a royal grave circle (stands in for TombCorridor), after Grave
Circle A at Mycenae.

A ring of upright limestone slabs at 5.00 m from the centre, capped by flat
slabs, built as four arcs (one per quadrant) so the clear cross passes through the
gaps; a shaft grave's cover slab inside each arc. Section A-A east-west at y = 0,
looking north: the north arcs in elevation, the covers low in front of them.
"""
import math

from _bronze import *

R = 5.0
ANGLES = [25 + k * 8 for k in range(6)]            # degrees within each quadrant
ANCHORS = [(2.7, 2.7, 0.50), (-2.7, 2.7, 0.50), (2.7, -2.7, 0.50), (-2.7, -2.7, 0.50)]


def slab_positions():
    """(x, y, angle) of every upright, all four quadrants."""
    out = []
    for qx, qy in ((1, 1), (-1, 1), (-1, -1), (1, -1)):
        for a in ANGLES:
            t = math.radians(a)
            out.append((qx * R * math.cos(t), qy * R * math.sin(t), t))
    return out


def build():
    mats = [("limestone", STONE), ("ashlar", "#9A8C72"), ("soot", SOOT), ("earth", "#4A3C2C"),
            ("gold", GOLD), ("bronze", BRONZE), ("haematite", RED)]
    sh = room_sheet("BronzeAge", "Crypt", "The Grave Circle", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE CIRCLE, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 0.8, "#6A5A3A", rx=320, ry=150, strength=.3)

    # ---- section ----
    kit_slab(sh, "#3A332A")
    kit_back_wall(sh, "Crypt", STONE, trim="#9A8C72", soot_depth=0.6)
    # The north arcs in elevation: uprights at their projected x, the farther ones darker.
    for x, y, t in sorted(slab_positions(), key=lambda p: -p[1]):
        if y < 0:
            continue
        w = 0.6 * abs(math.sin(t)) + 0.2 * abs(math.cos(t))
        shade = STONE if y < 3.5 else darken(STONE, .25)
        kerect(sh, x - w / 2, FZ, x + w / 2, FZ + 1.00, f"url(#{sh.lin(shade, 'h', .3, .5)})", darken(STONE, .6), .8)
    for sx in (-1, 1):
        xs = [sx * R * math.cos(math.radians(a)) for a in ANGLES]
        kerect(sh, min(xs) - 0.3, FZ + 1.00, max(xs) + 0.3, FZ + 1.12, "#9A8C72", darken(STONE, .6), .8)
    # The shaft graves' cover slabs, low, in front.
    for sx in (-1, 1):
        kerect(sh, sx * 2.7 - 0.6, FZ, sx * 2.7 + 0.6, FZ + 0.20, f"url(#{sh.lin('#9A8C72', 'v', .25, .5)})",
               darken(STONE, .6), .8)
        cx, cy = KE(sx * 2.7, FZ + 0.27)
        sh.ellipse(cx, cy, 8, 3, GOLD, darken(GOLD, .5), .7)                   # a gold cup left on the cover
    kit_cut_walls(sh, "Crypt")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(4.4, FZ + 0.6), "UPRIGHT SLABS", "limestone, 1.00 m, r 5.00 m"),
        (*KE(3.0, FZ + 1.06), "CAP SLABS", "laid flat across the ring"),
        (*KE(2.7, FZ + 0.12), "SHAFT GRAVE COVER", "1.20 × 0.80 m · loot"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(-4.0, FZ + 0.6), "FOUR ARCS", "gapped at the axes"),
    ], 330, 250, 250, anchor="end")
    kit_clear_note(sh, "Crypt", x=2.6, text="3.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Crypt", floor="#1E1B16", wall="#3A332A", trim="#9A8C72")
    sh.circle(*KP(0, 0), R * PK, "none", darken(STONE, .4), 1, op=.4)
    for x, y, t in slab_positions():
        cx, cy = KP(x, y)
        deg = -math.degrees(t) if x * y > 0 else math.degrees(t)
        sh.add(f'<rect x="{f(cx - 0.1 * PK)}" y="{f(cy - 0.3 * PK)}" width="{f(0.2 * PK)}" height="{f(0.6 * PK)}" '
               f'fill="{STONE}" stroke="#0E0C09" stroke-width=".6" transform="rotate({f(deg)} {f(cx)} {f(cy)})"/>')
    for x, y, _ in ANCHORS:
        plan_box(sh, x, y, 1.2, 0.8, "#9A8C72")
        sh.circle(*KP(x - 0.3, y), 0.08 * PK, GOLD)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Crypt")
    socket_label(sh, *KP(0, 2.2), "THE CIRCLE")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.16 · all open"),
                    ("CLEAR CROSS", "dashed · passes through the ring's four gaps"),
                    ("RING", "24 uprights at r 5.00 m, capped, 1.12 m"),
                    ("GRAVES", "4 shaft graves, cover slabs inside the arcs"),
                    ("LOOT", "L1–L4: the four grave covers")])
    return sh
