"""BronzeMegaron: the kit version of the art bible's Megaron (docs/art/concept/bronze/megaron-hall.svg).

Section A-A runs east-west through the hearth, looking north; the plan is the kit
cell with its clear cross and loot anchors. Every dimension here is the one
Tools/AssetPipeline/castle_builders_bronze_keep.py builds.
"""
from roomlib import *

PLAST, BLUE, RED, MADDER_, CYP, SOOT, GYP = "#C9A77A", "#3F6F86", "#8E3F2C", "#C4542E", "#5A3E28", "#2B231B", "#D8CDB2"
BRONZE = "#9B6A38"
CLEAR = 4.60
TOP = FZ + CLEAR

# (x, y, z) loot anchors, kit metres; the builder registers the same points.
ANCHORS = [(-3.1, 5.25, 0.70), (3.1, 5.25, 0.70), (-3.6, -5.25, 0.70), (3.6, -5.25, 0.70),
           (-3.5, 2.0, 1.44), (3.3, -2.0, 1.44), (5.05, 3.7, 0.76), (4.15, 3.7, 0.42),
           (-4.6, -3.6, 1.0), (-2.4, -4.3, 1.0), (4.6, -3.6, 1.0)]


def spiral_band(sh, x0, x1, y, h, col, bg=None):
    """Running spiral band between px x0..x1 at top y, height h px (from the art bible)."""
    out = []
    if bg:
        out.append(f'<rect x="{f(x0)}" y="{f(y)}" width="{f(x1 - x0)}" height="{f(h)}" fill="{bg}"/>')
    r = h * .32
    n = max(1, int((x1 - x0) / (h * 1.1)))
    for i in range(n):
        cx = x0 + (i + .5) * (x1 - x0) / n
        cy = y + h / 2
        out.append(f'<path d="M{f(cx - r * 1.6)} {f(cy + r * .9)} C{f(cx - r)} {f(cy + r * 1.2)} {f(cx - r * 1.1)} {f(cy - r)} '
                   f'{f(cx)} {f(cy - r)} A{f(r)} {f(r)} 0 1 1 {f(cx - r * .2)} {f(cy + r * .6)} A{f(r * .5)} {f(r * .5)} 0 1 1 {f(cx + r * .3)} {f(cy)}" '
                   f'fill="none" stroke="{col}" stroke-width="{f(max(1, h * .07))}"/>')
    sh.add("".join(out))


def griffin(sh, x, y, s, col, facing=1):
    """Couchant griffin silhouette, lower-left at (x, y) px (from the art bible)."""
    pts = [(0, 0), (10, -18), (30, -24), (58, -22), (78, -30), (84, -52), (96, -60), (106, -56), (104, -46), (114, -44),
           (104, -38), (94, -34), (92, -18), (100, 0)]
    sh.path(smooth_path([(x + facing * px * s, y + py * s) for px, py in pts], tension=.35), col, darken(col, .5), 1, op=.9)
    w = [(40, -24), (46, -52), (64, -64), (70, -46), (62, -28)]
    sh.path(smooth_path([(x + facing * px * s, y + py * s) for px, py in w], tension=.35), BLUE, darken(BLUE, .5), 1, op=.9)
    sh.circle(x + facing * 100 * s, y - 52 * s, 1.6 * s + .4, "#14120E")


def column(sh, x):
    """Tapered-down column on a black base, cushion capital and abacus up to the wall top."""
    kerect(sh, x - 0.30, FZ, x + 0.30, FZ + 0.10, SOOT, darken(SOOT, .5))
    shaft_top = TOP - 0.46
    d = poly_path([KE(x - 0.17, FZ + 0.10), KE(x + 0.17, FZ + 0.10), KE(x + 0.23, shaft_top), KE(x - 0.23, shaft_top)])
    sh.path(d, f"url(#{sh.lin(RED, 'h', .3, .55)})", darken(RED, .6), 1.2)
    side = 1 if x < 0 else -1          # soot on the hearth side
    a = KE(x + side * 0.05, FZ)
    sh.clipped(d, f'<rect x="{f(min(a[0], a[0] + side * 30))}" y="{f(KE(0, shaft_top)[1])}" width="30" '
                  f'height="{f((shaft_top - FZ) * SK)}" fill="{SOOT}" opacity=".45"/>')
    sh.flecks(d, (KE(x - .25, 0)[0], KE(0, shaft_top)[1], KE(x + .25, 0)[0], KE(0, FZ + .1)[1]), 30, SOOT, .5, 1.3, .5)
    cap = [KE(x - 0.23, shaft_top), KE(x - 0.35, shaft_top + 0.16), KE(x - 0.30, TOP - 0.10), KE(x + 0.30, TOP - 0.10),
           KE(x + 0.35, shaft_top + 0.16), KE(x + 0.23, shaft_top)]
    sh.path(smooth_path(cap, tension=.35), f"url(#{sh.lin(SOOT, 'h', .35, .5)})", "#0E0C09", 1)
    kerect(sh, x - 0.35, TOP - 0.10, x + 0.35, TOP, lighten(SOOT, .15), "#0E0C09")


def tripod(sh, x, near=True):
    bx, by = KE(x, FZ + 1.0)
    sh.path(f"M{f(bx - 16)} {f(by)} a16 9 0 0 0 32 0 z", f"url(#{sh.lin(BRONZE, 'h', .35, .5)})", darken(BRONZE, .6), 1)
    for dx in (-12, 0, 12):
        sh.line(bx + dx * .5, by + 4, bx + dx * 1.1, KE(0, FZ)[1], BRONZE, 1.8 if near else 1.2)


def build():
    mats = [("plaster", PLAST), ("fresco", BLUE), ("haematite", RED), ("madder", MADDER_),
            ("cypress", CYP), ("soot", SOOT), ("gypsum", GYP)]
    sh = room_sheet("BronzeAge", "Keep", "The Megaron", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE HEARTH, LOOKING NORTH")

    # Hearth glow behind everything.
    g1 = sh.rad([(0, MADDER_, .5), (0.35, "#6A3418", .4), (1, "#14120E", 0)])
    hx, hy = KE(0, FZ + 0.6)
    sh.back.append(f'<ellipse cx="{f(hx)}" cy="{f(hy - 40)}" rx="330" ry="220" fill="url(#{g1})"/>')

    # ---- section ----
    kerect(sh, -6, 0, 6, FZ, f"url(#{sh.lin('#7A6A58', 'v', .1, .4)})", darken(PLAST, .7))
    # North wall inner face, from the slab to the wall top.
    wall = poly_path([KE(-IN, FZ), KE(IN, FZ), KE(IN, TOP), KE(-IN, TOP)])
    sh.path(wall, f"url(#{sh.lin(PLAST, 'v', .1, .5)})", darken(PLAST, .6), 1)
    gs = sh.lin(SOOT, "v", 0, 0, stops=[(0, SOOT), (0.45, SOOT), (1, PLAST)])
    sh.clipped(wall, f'<rect x="{f(KE(-IN, 0)[0])}" y="{f(KE(0, TOP)[1])}" width="{f(2 * IN * SK)}" '
                     f'height="{f(1.2 * SK)}" fill="url(#{gs})" opacity=".6"/>')
    # Fresco band 1.40-3.00 m above the floor on each wall section, haematite border over it.
    for x0, x1 in ((-IN, -1.3), (1.3, IN)):
        kerect(sh, x0, FZ + 1.40, x1, FZ + 3.00, mix(PLAST, "#E0C8A0", .3), darken(PLAST, .5), .8)
        spiral_band(sh, KE(x0, 0)[0], KE(x1, 0)[0], KE(0, FZ + 3.18)[1], 0.18 * SK, BLUE, bg=RED)
        spiral_band(sh, KE(x0, 0)[0], KE(x1, 0)[0], KE(0, FZ + 1.62)[1], 0.22 * SK, BLUE, bg=mix(BLUE, PLAST, .5))
    for i, x in enumerate((-4.9, -4.1, -3.3, -2.5)):
        bx, by = KE(x, FZ + 1.62)
        col = RED if i % 2 else BLUE
        sh.path(f"M{f(bx)} {f(by)} l6 -34 l-3 -8 a6 6 0 1 1 8 0 l-3 8 l6 34 z", col, darken(col, .5), .8, op=.9)
    griffin(sh, *KE(2.1, FZ + 1.66), 0.6, lighten(PLAST, .25), 1)
    # The north archway, the Keep's 2.60 × 3.31.
    kit_arch_section(sh, "Keep")
    # Trim band: a haematite capstone course along the wall top (the kit's TRIM_BAND, 0.45 m).
    kerect(sh, -6, TOP - 0.45, 6, TOP, f"url(#{sh.lin(RED, 'v', .2, .5)})", darken(RED, .6), .8)
    # Clay benches along the north wall, either side of the archway.
    for x in (-3.1, 3.1):
        kerect(sh, x - 1.2, FZ, x + 1.2, FZ + 0.40, f"url(#{sh.lin(GYP, 'v', .25, .45)})", darken(PLAST, .6))
        kerect(sh, x - 0.7, FZ + 0.40, x + 0.3, FZ + 0.48, "#CFC3A2", darken(PLAST, .5), .6)   # fleece
    # North pair of columns on the 5 m square.
    for x in (-2.5, 2.5):
        column(sh, x)
    # Cut walls at y = 0: this line passes through the east and west archways,
    # so only the lintel over each opening is cut.
    for x0, x1 in ((-6, -IN), (IN, 6)):
        d = poly_path([KE(x0, FZ + 3.31), KE(x1, FZ + 3.31), KE(x1, TOP), KE(x0, TOP)])
        sh.path(d, "#3A332A", "#0E0C09", 1.2)
        a, b = KE(x0, TOP), KE(x1, FZ + 3.31)
        hatch(sh, d, (a[0], a[1], b[0], b[1]), "#635C4C", 5, .7)
    # The throne in profile against the east wall (NE quadrant, facing west).
    th = [KE(4.70, FZ), KE(4.70, FZ + 0.46), KE(5.20, FZ + 0.46), KE(5.20, FZ + 1.62), KE(5.26, FZ + 1.78),
          KE(5.34, FZ + 1.70), KE(5.42, FZ + 1.84), KE(IN, FZ + 1.80), KE(IN, FZ)]
    sh.path(smooth_path(th, tension=.15), f"url(#{sh.lin(GYP, 'h', .2, .45)})", darken(GYP, .6), 1.2)
    kerect(sh, 3.55, FZ, 4.75, FZ + 0.12, "#B8AD92", darken(GYP, .6), .8)   # footstool step
    # The hearth ring: flat, inlaid, walkable (0.04 / 0.07 / 0.10 m).
    kerect(sh, -1.8, FZ, 1.8, FZ + 0.04, BLUE, darken(BLUE, .5), .6)
    kerect(sh, -1.5, FZ, 1.5, FZ + 0.07, "#B7803E", darken("#B7803E", .6), .6)
    kerect(sh, -1.05, FZ, 1.05, FZ + 0.10, SOOT, "#0E0C09", .6)
    for x, h in ((-0.5, .7), (-0.15, 1.0), (0.2, .85), (0.5, .6)):
        a, tip = KE(x, FZ + 0.1), KE(x + .05, FZ + 0.1 + h)
        sh.path(f"M{f(a[0] - 10)} {f(a[1])} Q{f(a[0] - 8)} {f((a[1] + tip[1]) / 2)} {f(tip[0])} {f(tip[1])} "
                f"Q{f(a[0] + 10)} {f((a[1] + tip[1]) / 2)} {f(a[0] + 10)} {f(a[1])} Z", MADDER_, op=.85)
    # Tripods (west one behind the cut, north of it).
    tripod(sh, -3.5)
    khuman(sh, -1.9)
    # Callouts.
    sh.callouts([
        (*KE(2.5, FZ + 2.4), "TAPERED COLUMN", "0.34 → 0.46 m, haematite"),
        (*KE(5.3, FZ + 1.2), "GYPSUM THRONE", "NE, faces west, wavy crest"),
        (*KE(3.1, FZ + 0.3), "CLAY BENCH", "0.40 m, fleeces · loot"),
        (*KE(0.9, FZ + 0.06), "PAINTED HEARTH", "3.60 m ring, flat inlay"),
    ], 610, 190, 330, slope=1.0)
    sh.callouts([
        (*KE(-4.0, FZ + 2.3), "FRESCO BAND", "1.40–3.00 m, spirals"),
        (*KE(-3.5, FZ + 1.0), "BRONZE TRIPOD", "bowl 0.60 m on 0.90 m legs"),
    ], 330, 190, 250, anchor="end")
    sh.text(KE(4.4, 0)[0], KE(0, TOP - 0.62)[1], "4.60 clear · open roof", 9, "#DCD2BA", "middle")

    # ---- plan ----
    kit_plan(sh, "Keep", floor="#2A2419", wall="#3A332A", trim=RED)
    # Painted floor grid (albedo only).
    for i in range(-5, 6):
        sh.line(*KP(i, -IN), *KP(i, IN), "#3A3124", .5, op=.6)
        sh.line(*KP(-IN, i), *KP(IN, i), "#3A3124", .5, op=.6)
    hc = KP(0, 0)
    g2 = sh.rad([(0, MADDER_, .55), (1, MADDER_, 0)])
    sh.circle(*hc, 2.6 * PK, f"url(#{g2})")
    sh.circle(*hc, 1.8 * PK, BLUE, "#0E0C09", 1)
    sh.circle(*hc, 1.5 * PK, "#B7803E", "#0E0C09", 1)
    sh.circle(*hc, 1.05 * PK, SOOT)
    sh.circle(*hc, 0.45 * PK, MADDER_, op=.8)
    for x, y in ((-2.5, 2.5), (2.5, 2.5), (-2.5, -2.5), (2.5, -2.5)):
        sh.circle(*KP(x, y), 0.35 * PK, SOOT, "#0E0C09", .8)
        sh.circle(*KP(x, y), 0.23 * PK, RED, "#0E0C09", .8)
    for x, y, w in ((-3.1, 5.25, 2.4), (3.1, 5.25, 2.4), (-3.6, -5.25, 3.2), (3.6, -5.25, 3.2)):
        kprect(sh, x - w / 2, y - 0.25, x + w / 2, y + 0.25, darken(GYP, .25), "#0E0C09", .6)
    kprect(sh, 4.70, 3.2, IN, 4.2, GYP, "#0E0C09", .8)              # throne
    kprect(sh, 3.55, 2.9, 4.75, 4.5, "#8C826C", "#0E0C09", .6)      # footstool step
    for dy in (-1.5, 1.4):                                             # griffin panels
        kprect(sh, IN - 0.06, 3.7 + dy - 0.7, IN, 3.7 + dy + 0.7, BLUE)
    for x, y in ((-3.5, 2.0), (3.3, -2.0)):
        sh.circle(*KP(x, y), 0.30 * PK, BRONZE, "#0E0C09", .8)
    for x, y in ((-4.6, -3.6), (-2.4, -4.3), (4.6, -3.6)):
        kprect(sh, x - .3, y - .3, x + .3, y + .3, "#8C826C", "#0E0C09", .6)
    sh.line(*KP(1.8, 0.05), *KP(4.4, 3.2), "#9A9078", .8, dash="2 2")   # libation channel
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    sh.line(*KP(-6.6, 0), *KP(6.6, 0), "#9A9078", .8, dash="8 3 2 3")
    for x in (-6.6, 6.6):
        p = KP(x, 0)
        sh.path(f"M{f(p[0])} {f(p[1])} l0 -10 l-4 5 m4 -5 l4 5", "none", "#9A9078", 1)
        sh.text(p[0], p[1] + 12, "A", 9, "#9A9078", "middle")
    socket_label(sh, *KP(0, 4.55), "ARCHWAY N 2.60 × 3.31")
    socket_label(sh, *KP(0, -4.85), "ARCHWAY S 2.60 × 3.31")
    socket_label(sh, *KP(4.2, 4.85), "THRONE")
    socket_label(sh, *KP(0, 2.05), "HEARTH")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 3.31 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("COLUMNS", "4 on the 5 m square · 4.60 m to the wall top"),
                    ("HEARTH", "3.60 m painted ring, inlaid flat"),
                    ("LOOT", "L1–L11: benches, tripods, throne, tables")])
    return sh
