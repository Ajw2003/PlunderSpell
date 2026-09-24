"""Drawing helpers for the Bronze Age curtain-wall sheets: cyclopean blocks, the
mud-brick parapet with rounded merlons, the plastered wall-walk, in south
elevation (outside face, looking north) and in plan.

The blocks come from Tools/AssetPipeline/cyclopean.py, the same layout the Blender
builders lay, so each drawing shows the model's actual stones. Heights here are
above the ground (wall pieces have no floor slab); the kit's cell edge is 6.05 m
from the centre and the sheets clip at the drawn 6.00 m frame."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", "AssetPipeline"))
import cyclopean as cy  # noqa: E402

from _bronze import *  # noqa: E402,F401,F403

KH = 6.05                 # the kit's cell half-width: runs are laid to it
WALK_Z = 4.1              # top of the masonry, under the walk's plaster
PARAPET_T = 0.4
PARAPET_IN = 0.04         # the parapet's outer face sits this far inside the cell line
LOW = 0.5                 # the breastwork under the merlons
PITCH = 1.2               # merlon + crenel
CONGLOM = "#9A8666"
PLASTER = OCHRE


def clip(u):
    return max(-HALF, min(HALF, u))


def merlon_centres(u0, u1):
    n = int((u1 - u0 + 1e-6) // PITCH)
    mid = (u0 + u1) / 2
    return [mid - (n - 1) * PITCH / 2 + k * PITCH for k in range(n)]


def elev_blocks(sh, bl, lit=STONE):
    """The blocks of a run in elevation: each a rounded stone, deeper-set ones darker,
    a few weathering pocks on each face."""
    for n, b in enumerate(bl):
        x0, x1 = clip(b["u0"]), clip(b["u1"])
        if x1 - x0 < 0.05:
            continue
        col = darken(lit, b["inset"] * 2.2)
        a, c = KE(x0, b["z1"]), KE(x1, b["z0"])
        sh.add(f'<rect x="{f(a[0] + 1.5)}" y="{f(a[1] + 1.5)}" width="{f(c[0] - a[0] - 3)}" height="{f(c[1] - a[1] - 3)}" '
               f'rx="7" fill="url(#{sh.lin(col, "v", .22, .5)})" stroke="#2A251D" stroke-width="1.6"/>')
        for k in range(3):
            px = x0 + (x1 - x0) * ((n * 37 + k * 53) % 89 + 5) / 100
            pz = b["z0"] + (b["z1"] - b["z0"]) * ((n * 29 + k * 41) % 71 + 15) / 100
            sh.ellipse(*KE(px, pz), 3 + k % 2, 2, darken(col, .3), op=.7)


def elev_parapet(sh, u0, u1, base=WALK_Z, slots=True):
    """The mud-brick breastwork and its rounded merlons in elevation; sling slots
    in every other merlon, from the first."""
    x0, x1 = clip(u0), clip(u1)
    kerect(sh, x0, base, x1, base + LOW, f"url(#{sh.lin(MUD, 'v', .25, .5)})", darken(MUD, .6), 1)
    for k in range(1, 3):
        sh.line(*KE(x0, base + k * LOW / 3), *KE(x1, base + k * LOW / 3), darken(MUD, .35), .7, op=.7)
    for k, u in enumerate(merlon_centres(u0, u1)):
        if u - 0.3 < -HALF - 1e-6 or u + 0.3 > HALF + 1e-6:
            continue
        cx, cy0 = KE(u, base + LOW + 0.30)
        r = 0.30 * SK
        sh.path(f"M{f(cx - r)} {f(KE(0, base + LOW)[1])} L{f(cx - r)} {f(cy0)} A{f(r)} {f(r)} 0 0 1 {f(cx + r)} {f(cy0)} "
                f"L{f(cx + r)} {f(KE(0, base + LOW)[1])} Z", f"url(#{sh.lin(MUD, 'h', .3, .5)})", darken(MUD, .6), 1)
        if slots and k % 2 == 0:
            kerect(sh, u - 0.06, base + 0.35, u + 0.06, base + 0.85, "#14100C")


def elev_walk(sh, u0, u1, z=WALK_Z):
    """The walk's plaster edge, seen as a thin line over the masonry (behind the parapet)."""
    kerect(sh, clip(u0), z, clip(u1), z + 0.1, lighten(PLASTER, .1), darken(PLASTER, .5), .6)


def plan_run(sh, bl, depth, side="south", fill=STONE, ragged=True):
    """A run's footprint in plan, course by course from the bottom (a lower course that
    reaches further in shows beyond the one above it)."""
    top_course = max(b["z0"] for b in bl)
    for level in sorted({round(b["z0"], 3) for b in bl}):
        shade = fill if level < top_course - 1e-6 else lighten(fill, .08)
        for b in bl:
            if round(b["z0"], 3) != level:
                continue
            d = depth + (b["dj"] if ragged else 0.0)
            plan_strip(sh, b["u0"], b["u1"], 0.0, d, side, shade, "#2A251D", .6)


def plan_strip(sh, u0, u1, d0, d1, side, col, stroke=None, sw=1, op=None):
    """A rectangle measured from the outer (cell) edge of `side`: along the wall from u0
    to u1, inward from d0 to d1."""
    if side == "south":
        kprect(sh, clip(u0), -KH + d0, clip(u1), -KH + d1, col, stroke, sw, op)
    else:                                                          # west: u runs along y
        kprect(sh, -KH + d0, clip(u0), -KH + d1, clip(u1), col, stroke, sw, op)


def plan_parapet(sh, u0, u1, side="south"):
    """The parapet strip on the outer edge, merlons as darker ticks."""
    plan_strip(sh, u0, u1, PARAPET_IN, PARAPET_IN + PARAPET_T, side, MUD, "#2A251D", .6)
    for u in merlon_centres(u0, u1):
        plan_strip(sh, u - 0.3, u + 0.3, PARAPET_IN, PARAPET_IN + PARAPET_T, side, darken(MUD, .25))


def plan_walk(sh, u0, u1, depth, side="south"):
    plan_strip(sh, u0, u1, PARAPET_IN + PARAPET_T, depth - 0.2, side, lighten(PLASTER, .05), "#2A251D", .5)


def wall_labels(sh):
    """The inside is labelled; the outside is beyond the plan's south edge (the cell line)."""
    socket_label(sh, *KP(0, 5.0), "BAILEY (NORTH) · OUTSIDE IS BEYOND THE SOUTH EDGE")


def ground(sh, x0=-HALF - 0.5, x1=HALF + 0.5):
    kerect(sh, x0, -0.08, x1, 0.0, "#3A332A")


MS = 30.0                 # the small cross-section's scale, px per metre


def mini_section(sh, ox, oy, rects, label, width=3.2, scale=MS, label_above=False):
    """A small N–S cross-section (1 m = `scale` px, 30 by default): (ox, oy) is the outer
    face at ground, north to the right. `rects` are (d0, z0, d1, z1, fill) in metres from
    the outer face. The label goes under it, or over it when that space is taken."""
    sh.line(ox - 12, oy, ox + width * scale + 24, oy, "#635C4C", 1)
    top = oy
    for d0, z0, d1, z1, col in rects:
        sh.add(f'<rect x="{f(ox + d0 * scale)}" y="{f(oy - z1 * scale)}" width="{f((d1 - d0) * scale)}" '
               f'height="{f((z1 - z0) * scale)}" fill="{col}" stroke="#14120E" stroke-width=".8"/>')
        top = min(top, oy - z1 * scale)
    sh.text(ox + width * scale / 2, top - 8 if label_above else oy + 16, label, 9, "#9A9078", "middle", ls=2)
    sh.text(ox - 8, oy - 4, "S", 8.5, "#9A9078", "end")
    sh.text(ox + width * scale + 20, oy - 4, "N", 8.5, "#9A9078", "start")
