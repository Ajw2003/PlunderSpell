"""Drawing helpers for the Late Medieval curtain-wall sheets: the machicolated wall
in south elevation (outside face, looking north), in plan, and in a small N-S
section. The numbers are the builder's (castle_builders_late_curtain.py): every
Late wall piece keeps its outer face FACE m inside the cell edge so the corbelled
parapet can project out to the cell line and stay inside the cell.

Distances `d` are measured inward from the cell edge of the wall's side; `u` runs
along the wall (x for the south wall, y for the west)."""
from _late import *  # noqa: F401,F403

KH = 6.05                 # the kit's cell half-width
FACE = 0.45               # the wall's outer face, in from the cell edge
MASS = 1.6                # the sandstone mass behind it
WALK_Z = 4.0              # top of the mass: the stone walk
FRIEZE = (3.0, 3.4)       # brick frieze band on the outer face
CORBEL = (3.4, 3.7, 4.0)  # corbel courses: lower block, upper block, top
CORBEL_PITCH, CORBEL_W = 0.9, 0.3
PARAPET = (0.05, 0.33)    # parapet from d .. to d; the gap to FACE is the machicolation slot
LOW_TOP, MERLON_TOP = 4.6, 5.2
MERLON_PITCH, MERLON_W = 1.3, 0.8
TIMBER_WALK = (2.05, 3.05, 3.9)   # d from, d to, underside
LOOP_Z = 1.0              # keyhole gun-loop: round port bottom


def clip(u):
    return max(-HALF, min(HALF, u))


def centres(u0, u1, pitch):
    n = int((u1 - u0 + 1e-6) // pitch)
    mid = (u0 + u1) / 2
    return [mid - (n - 1) * pitch / 2 + k * pitch for k in range(n)]


def ground(sh, x0=-HALF - 0.5, x1=HALF + 0.5):
    kerect(sh, x0, -0.08, x1, 0.0, "#3A332A")


def elev_ashlar(sh, u0, u1, top, col=SAND, course=0.45, z0=0.0):
    """Coursed ashlar face from u0 to u1, z0..top: joints every `course`, perpends staggered."""
    x0, x1 = clip(u0), clip(u1)
    kerect(sh, x0, z0, x1, top, f"url(#{sh.lin(col, 'v', .15, .45)})", darken(col, .6), 1)
    k, z = 0, z0 + course
    while z < top - 1e-6:
        sh.line(*KE(x0, z), *KE(x1, z), darken(col, .3), .7, op=.6)
        x = x0 + (0.4 if k % 2 else 0.0) + 0.3
        while x < x1 - 0.05:
            sh.line(*KE(x, z - course), *KE(x, z), darken(col, .3), .6, op=.45)
            x += 0.8
        z += course
        k += 1


def elev_crown(sh, u0, u1, base=0.0, loops=()):
    """The run's frieze, corbels, parapet and merlons in elevation, `base` added to every
    height (a tower's crown sits higher), and keyhole gun-loops at the `loops` u."""
    x0, x1 = clip(u0), clip(u1)
    kerect(sh, x0, base + FRIEZE[0], x1, base + FRIEZE[1], f"url(#{sh.lin(BRICK, 'v', .25, .5)})", darken(BRICK, .6), .8)
    sh.line(*KE(x0, base + FRIEZE[0] + 0.2), *KE(x1, base + FRIEZE[0] + 0.2), darken(BRICK, .35), .6, op=.7)
    kerect(sh, x0, base + CORBEL[0], x1, base + CORBEL[2], "#14120E", op=.55)     # shadow under the parapet
    for u in centres(u0, u1, CORBEL_PITCH):
        if u - CORBEL_W / 2 < -HALF - 1e-6 or u + CORBEL_W / 2 > HALF + 1e-6:
            continue
        kerect(sh, u - CORBEL_W / 2, base + CORBEL[0], u + CORBEL_W / 2, base + CORBEL[1], darken(SAND, .08),
               darken(SAND, .6), .6)
        kerect(sh, u - CORBEL_W / 2 - 0.03, base + CORBEL[1], u + CORBEL_W / 2 + 0.03, base + CORBEL[2], SAND,
               darken(SAND, .6), .6)
    kerect(sh, x0, base + CORBEL[2], x1, base + LOW_TOP, f"url(#{sh.lin(SAND, 'v', .2, .5)})", darken(SAND, .6), 1)
    for u in centres(u0, u1, MERLON_PITCH):
        if u - MERLON_W / 2 < -HALF - 1e-6 or u + MERLON_W / 2 > HALF + 1e-6:
            continue
        kerect(sh, u - MERLON_W / 2, base + LOW_TOP, u + MERLON_W / 2, base + MERLON_TOP,
               f"url(#{sh.lin(SAND, 'h', .25, .5)})", darken(SAND, .6), 1)
    for u in loops:
        keyhole(sh, u, LOOP_Z)


def keyhole(sh, u, z0):
    """A keyhole gun-loop in elevation: a round port under a sighting slit, a soot plume."""
    kerect(sh, u - 0.05, z0 + 0.3, u + 0.05, z0 + 1.2, "#0E0C09")
    sh.circle(*KE(u, z0 + 0.15), 0.1 * SK, "#0E0C09")
    sh.path(poly_path([KE(u - 0.08, z0 + 1.2), KE(u + 0.08, z0 + 1.2), KE(u + 0.25, z0 + 1.9), KE(u - 0.25, z0 + 1.9)]),
            SOOT, op=.3)


def plan_strip(sh, u0, u1, d0, d1, side, col, stroke=None, sw=1, op=None):
    """A rectangle measured from the outer (cell) edge of `side`: along the wall from u0
    to u1, inward from d0 to d1."""
    if side == "south":
        kprect(sh, clip(u0), -KH + d0, clip(u1), -KH + d1, col, stroke, sw, op)
    else:
        kprect(sh, -KH + d0, clip(u0), -KH + d1, clip(u1), col, stroke, sw, op)


def plan_run(sh, u0, u1, side="south", walk=True):
    """A run in plan: the corbel row, the parapet, the mass and its stone walk, the timber
    walk on the inner face."""
    for u in centres(u0, u1, CORBEL_PITCH):
        plan_strip(sh, u - CORBEL_W / 2, u + CORBEL_W / 2, PARAPET[0], FACE, side, darken(SAND, .25))
    plan_strip(sh, u0, u1, FACE, FACE + MASS, side, lighten(SAND, .05), "#2A251D", .6)
    plan_strip(sh, u0, u1, PARAPET[0], PARAPET[1], side, SAND, "#2A251D", .6)
    for u in centres(u0, u1, MERLON_PITCH):
        plan_strip(sh, u - MERLON_W / 2, u + MERLON_W / 2, PARAPET[0], PARAPET[1], side, darken(SAND, .2))
    if walk:
        plan_strip(sh, u0, u1, TIMBER_WALK[0], TIMBER_WALK[1], side, OAK, "#2A251D", .5)
        n = int((u1 - u0) / 0.3)
        for k in range(1, n):
            u = u0 + k * (u1 - u0) / n
            plan_strip(sh, u - 0.01, u + 0.01, TIMBER_WALK[0], TIMBER_WALK[1], side, darken(OAK, .4))


MS = 30.0


def mini_section(sh, ox, oy, rects, label, width=3.2, scale=MS, label_above=False):
    """A small N-S section (1 m = `scale` px): (ox, oy) is the cell edge at ground, north to
    the right. `rects` are (d0, z0, d1, z1, fill) in metres from the cell edge."""
    sh.line(ox - 12, oy, ox + width * scale + 24, oy, "#635C4C", 1)
    top = oy
    for d0, z0, d1, z1, col in rects:
        sh.add(f'<rect x="{f(ox + d0 * scale)}" y="{f(oy - z1 * scale)}" width="{f((d1 - d0) * scale)}" '
               f'height="{f((z1 - z0) * scale)}" fill="{col}" stroke="#14120E" stroke-width=".8"/>')
        top = min(top, oy - z1 * scale)
    sh.text(ox + width * scale / 2, top - 8 if label_above else oy + 16, label, 9, "#9A9078", "middle", ls=2)
    sh.text(ox - 8, oy - 4, "S", 8.5, "#9A9078", "end")
    sh.text(ox + width * scale + 20, oy - 4, "N", 8.5, "#9A9078", "start")


def wall_section_rects():
    """The standard run's N-S section as mini_section rectangles."""
    c0, c1, c2 = CORBEL
    return [
        (FACE, 0, FACE + MASS, WALK_Z, SAND),
        (FACE - 0.02, FRIEZE[0], FACE, FRIEZE[1], BRICK),
        (0.25, c0, FACE, c1, darken(SAND, .1)),
        (PARAPET[0], c1, FACE, c2, SAND),
        (PARAPET[0], c2, PARAPET[1], MERLON_TOP, SAND),
        (TIMBER_WALK[0], TIMBER_WALK[2] - 0.4, TIMBER_WALK[0] + 0.4, TIMBER_WALK[2], darken(SAND, .1)),
        (TIMBER_WALK[0], TIMBER_WALK[2], TIMBER_WALK[1], WALK_Z, OAK),
    ]
