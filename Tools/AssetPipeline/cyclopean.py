"""
The Bronze Age curtain wall's block layout, in plain Python so both the Blender
builders (castle_builders_bronze_curtain.py) and the room sheets
(Tools/ArtBible/rooms/generators/BronzeAge/) draw the same blocks.

A cyclopean run is laid in courses of huge irregular blocks. Widths come from fixed
lists, read from a different start per course and per run (`seed`), so vertical
joints never line up and the build is identical on every run. Each block also
gets an outer-face recess (`inset`, so the face reads as separate stones rather
than one slab) and a ragged inner face (`dj`, added to the run's depth).
"""

COURSES = (1.5, 1.4, 1.2)            # to the 4.10 m wall-walk
WIDTHS = (
    (2.2, 1.4, 1.9, 1.1, 2.4, 1.6, 1.4, 1.8),
    (1.3, 2.3, 1.2, 2.0, 1.5, 2.1, 1.6, 1.7),
    (1.8, 1.2, 2.2, 1.5, 1.3, 2.0, 1.4, 1.9),
    (1.6, 2.1, 1.3, 1.9, 1.2, 2.3, 1.5, 1.1),
)
INSETS = (0.0, 0.05, 0.02, 0.07, 0.03, 0.06, 0.01)
DEPTH_JITTER = (0.0, -0.15, 0.1, -0.05, 0.12, -0.1, 0.05)
MIN_BLOCK = 0.6                      # a remainder narrower than this joins the block before it


def course_heights(top):
    """Course heights up to `top`: the three standard courses scaled to fit up to
    4.10 m, and above that one more course (a tower's extra height)."""
    base = sum(COURSES)
    if top <= base + 1e-6:
        k = top / base
        return [c * k for c in COURSES]
    return list(COURSES) + [top - base]


def blocks(u0, u1, top=4.1, seed=0, bottom=0.0):
    """The blocks of a run from u0 to u1 along the wall, from `bottom` up to `top`:
    [dict(u0, u1, z0, z1, inset, dj), ...], course by course, west (or south) first."""
    out = []
    z = bottom
    for c, h in enumerate(course_heights(top - bottom)):
        widths = WIDTHS[c % len(WIDTHS)]
        pos, i = u0, seed + 2 * c
        row = []
        while pos < u1 - 1e-6:
            w = widths[i % len(widths)]
            if u1 - (pos + w) < MIN_BLOCK:
                w = u1 - pos
            row.append([pos, pos + w])
            pos += w
            i += 1
        for j, (a, b) in enumerate(row):
            k = seed + j + 3 * c
            out.append(dict(u0=a, u1=b, z0=z, z1=z + h, inset=INSETS[k % len(INSETS)],
                            dj=DEPTH_JITTER[k % len(DEPTH_JITTER)]))
        z += h
    return out
