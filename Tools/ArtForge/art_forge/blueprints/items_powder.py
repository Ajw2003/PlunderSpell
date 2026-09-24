"""Age of Powder plunder (docs/art/data/powder.json, items)."""

from __future__ import annotations

import math

from ..kit import Part, arc_path, rounded_rect, spline
from ..spec import Entry
from . import blueprint


# --------------------------------------------------------------------------------
# Small helpers (kept local so this module does not depend on another Age's file)
# --------------------------------------------------------------------------------

def upright(outline_xz, y_front: float, depth: float, mat: str, bevel: bool = True,
            **kwargs) -> Part:
    """A prism drawn in world XZ (x across, z up), `depth` thick along +Y from
    `y_front` (the face towards the viewer at -Y)."""
    return Part("prism", (0.0, y_front + depth / 2.0, 0.0), (1.0, 1.0, depth), mat=mat,
                rot=(90.0, 0.0, 0.0),
                extras={"outline": list(outline_xz), "bevel": bevel}, **kwargs)


def disc(x: float, z: float, diameter: float, y_front: float, depth: float, mat: str,
         segments: int = 12, bevel: bool = False, **kwargs) -> Part:
    """A coin-like cylinder facing -Y."""
    return Part("cyl", (x, y_front + depth / 2.0, z), (diameter, diameter, depth), mat=mat,
                rot=(90.0, 0.0, 0.0), segments=segments, extras={"bevel": bevel}, **kwargs)


def rect_loop(x0: float, x1: float, z0: float, z1: float, y: float) -> list[tuple]:
    """A closed rectangle in the XZ plane at depth y, for a moulding tube."""
    return [(x0, y, z0), (x1, y, z0), (x1, y, z1), (x0, y, z1)]


def box(lo, hi, mat: str, **kwargs) -> Part:
    """An axis-aligned box from its min and max corners."""
    centre = tuple((a + b) / 2.0 for a, b in zip(lo, hi))
    size = tuple(b - a for a, b in zip(lo, hi))
    return Part("box", centre, size, mat=mat, **kwargs)


# --------------------------------------------------------------------------------
# Cabinet of Curiosities
# --------------------------------------------------------------------------------

def curiosity_cabinet(entry: Entry):
    """Ebony Kunstschrank on a walnut six-legged stand, built closed (the carry
    state the JSON dimensions and the front ortho describe). The 40-drawer bank is
    modelled behind the doors, so the open state and the burst-open break reuse it."""
    W, D, H = entry.dims                          # 0.95 × 0.55 × 1.30
    parts: list[Part] = []

    # --- Stand: walnut top board on six turned bulb legs, stretchers 0.05 m up.
    stand_top = 0.42
    board_t = 0.035
    parts.append(box((-W / 2, -D / 2, stand_top - board_t), (W / 2, D / 2, stand_top),
                     "black_walnut"))
    leg_profile = [
        (0.000, 0.000), (0.030, 0.000), (0.034, 0.012), (0.030, 0.030),   # bun foot
        (0.026, 0.035), (0.026, 0.072),                                    # through the stretcher
        (0.034, 0.080), (0.024, 0.092),                                    # collar ring
        (0.040, 0.130), (0.058, 0.180), (0.060, 0.205), (0.050, 0.245),   # bulb 0.12 dia
        (0.030, 0.290), (0.022, 0.320),                                    # neck
        (0.034, 0.335), (0.040, 0.352), (0.036, stand_top - board_t),      # cap
        (0.000, stand_top - board_t),
    ]
    leg_x, leg_y = 0.40, 0.205
    for x in (-leg_x, 0.0, leg_x):
        for y in (-leg_y, leg_y):
            parts.append(Part("lathe", (x, y, 0.0), (1, 1, 1), mat="black_walnut",
                              segments=10, extras={"profile": leg_profile, "bevel": False}))
    rail_z0, rail_z1 = 0.035, 0.068
    for y in (-leg_y, leg_y):
        parts.append(box((-leg_x - 0.03, y - 0.022, rail_z0), (leg_x + 0.03, y + 0.022, rail_z1),
                         "black_walnut"))
    for x in (-leg_x, 0.0, leg_x):
        parts.append(box((x - 0.022, -leg_y + 0.02, rail_z0), (x + 0.022, leg_y - 0.02, rail_z1),
                         "black_walnut"))

    # --- Carcass: ebony box 0.93 wide. The core stops 0.033 m short of the front;
    # ebony frame strips close that space round the drawer bank.
    cw, c0, c1 = 0.93, stand_top, 1.16
    hw = cw / 2.0
    back_y = D / 2.0 - 0.02
    core_front = -0.225
    door_y0, door_t = -0.279, 0.021               # doors' front face and thickness
    parts.append(box((-hw, core_front, c0), (hw, back_y, c1), "ebony_veneer"))
    frame_y0 = door_y0 + door_t
    for sx in (-1, 1):
        parts.append(box((sx * hw - 0.013 if sx > 0 else -hw, frame_y0, c0),
                         (hw if sx > 0 else -hw + 0.013, core_front + 0.002, c1), "ebony_veneer"))
    for z0, z1 in ((c0, c0 + 0.012), (c1 - 0.012, c1)):
        parts.append(box((-hw, frame_y0, z0), (hw, core_front + 0.002, z1), "ebony_veneer"))

    # Drawer bank (hidden until the doors open): a bone-inlay back so the gaps read
    # as 3 mm stringing round each face, 8 rows × 5 ebony faces, a gilt pull on each.
    bank_x, bank_z0, bank_z1 = 0.444, c0 + 0.013, c1 - 0.013
    parts.append(box((-bank_x, core_front - 0.003, bank_z0), (bank_x, core_front + 0.001, bank_z1),
                     "drawer_bone_inlay", extras={"bevel": False}))
    pitch_x, pitch_z = 2 * bank_x / 5.0, (bank_z1 - bank_z0) / 8.0
    for row in range(8):
        for col in range(5):
            x = -bank_x + pitch_x * (col + 0.5)
            z = bank_z0 + pitch_z * (row + 0.5)
            parts.append(Part("box", (x, core_front - 0.011, z), (0.170, 0.016, 0.080),
                              mat="ebony_veneer", extras={"bevel": False}))
            parts.append(disc(x, z, 0.015, core_front - 0.023, 0.004, "gilt_bronze", 5))

    # --- Doors: two ebony leaves 0.45 × 0.72 meeting on the centre line, each with
    # an ebony moulding round a gilt-edged painted copper landscape.
    door_w, gap = 0.4625, 0.003
    dz0, dz1 = c0 + 0.008, c1 - 0.008
    for side in (-1, 1):
        x_in, x_out = side * gap / 2.0, side * (gap / 2.0 + door_w)
        cx = (x_in + x_out) / 2.0
        parts.append(box((min(x_in, x_out), door_y0, dz0), (max(x_in, x_out), door_y0 + door_t, dz1),
                         "ebony_veneer"))
        # Raised field moulding (outer) and the panel frame (inner).
        cz = (dz0 + dz1) / 2.0
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="ebony_veneer", segments=4,
                          extras={"path": rect_loop(cx - 0.195, cx + 0.195, dz0 + 0.035, dz1 - 0.035,
                                                    door_y0 - 0.001),
                                  "closed": True, "section": (0.007, 0.007), "up": (0, 1, 0),
                                  "bevel": False}))
        pw, ph = 0.30, 0.45
        pz = cz + 0.01
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="ebony_veneer", segments=4,
                          extras={"path": rect_loop(cx - pw / 2 - 0.018, cx + pw / 2 + 0.018,
                                                    pz - ph / 2 - 0.018, pz + ph / 2 + 0.018,
                                                    door_y0 - 0.002),
                                  "closed": True, "section": (0.011, 0.011), "up": (0, 1, 0),
                                  "bevel": False}))
        parts.append(upright(rounded_rect(pw + 0.012, ph + 0.012, 0.002, 1, cx=cx, cy=pz),
                             door_y0 - 0.003, 0.003, "gilt_bronze", bevel=False))
        parts.append(upright(rounded_rect(pw, ph, 0.001, 1, cx=cx, cy=pz),
                             door_y0 - 0.0045, 0.0015, "painted_copper_panel", bevel=False))
        # The landscape: a pale sky whose lower edge is the mountain skyline, laid
        # over the green-earth ground (the concept's crags, left and right peaks).
        x0, x1 = cx - pw / 2, cx + pw / 2
        base = pz - ph / 2 + 0.03
        u = [0.00, 0.10, 0.22, 0.33, 0.42, 0.52, 0.63, 0.74, 0.84, 1.00]
        v = [0.20, 0.32, 0.30, 0.52, 0.40, 0.46, 0.62, 0.48, 0.34, 0.26]
        if side > 0:
            v = v[::-1]
        skyline = [(x0 + (x1 - x0) * a, base + ph * 0.8 * b) for a, b in zip(u, v)]
        sky = [(x1, skyline[-1][1])] + skyline[::-1][1:] + [(x0, pz + ph / 2), (x1, pz + ph / 2)]
        parts.append(upright(sky, door_y0 - 0.0055, 0.001, "drawer_bone_inlay", bevel=False))

    # Lock escutcheon on the meeting stile, with its keyhole.
    ez = 0.80
    esc = [(-0.017, ez - 0.035), (0.017, ez - 0.035), (0.021, ez - 0.012), (0.012, ez),
           (0.021, ez + 0.012), (0.017, ez + 0.035), (-0.017, ez + 0.035), (-0.021, ez + 0.012),
           (-0.012, ez), (-0.021, ez - 0.012)]
    parts.append(upright(esc, door_y0 - 0.004, 0.004, "gilt_bronze", bevel=False))
    parts.append(upright([(-0.0035, ez - 0.014), (0.0035, ez - 0.014), (0.0015, ez + 0.002),
                          (0.0045, ez + 0.006), (0.0, ez + 0.011), (-0.0045, ez + 0.006),
                          (-0.0015, ez + 0.002)],
                         door_y0 - 0.005, 0.001, "ebony_veneer", bevel=False))

    # --- Crown: two-step cornice, a gilt fillet, the low pediment and finial ball.
    parts.append(box((-hw - 0.004, -0.286, c1 - 0.004), (hw + 0.004, back_y + 0.006, c1 + 0.008),
                     "gilt_bronze"))
    parts.append(box((-hw - 0.006, -0.290, c1 + 0.008), (hw + 0.006, back_y + 0.008, c1 + 0.030),
                     "ebony_veneer"))
    top = c1 + 0.060
    parts.append(box((-W / 2 + 0.004, -D / 2, c1 + 0.030), (W / 2 - 0.004, D / 2 - 0.002, top),
                     "ebony_veneer"))
    ped_w, ped_h = 0.40, 0.065
    parts.append(upright([(-ped_w / 2, top), (ped_w / 2, top), (0.0, top + ped_h)],
                         -D / 2 + 0.01, 0.06, "ebony_veneer"))
    for sx in (-1, 1):
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="gilt_bronze", segments=4,
                          extras={"path": [(sx * (ped_w / 2 - 0.006), -D / 2 + 0.008, top + 0.004),
                                           (0.0, -D / 2 + 0.008, top + ped_h - 0.006)],
                                  "section": (0.004, 0.004), "smooth": True, "bevel": False}))
    parts.append(Part("sphere", (0.0, -D / 2 + 0.04, top + ped_h + 0.012), (0.05, 0.05, 0.05),
                      mat="gilt_bronze", segments=10, rings=6, extras={"bevel": False}))

    # --- Gilt-bronze corner mounts: 8 angle-pieces, on the front and back corners.
    m = 0.06
    for y_face, depth in ((door_y0 - 0.002, 0.004), (back_y, 0.004)):
        for sx in (-1, 1):
            for sz, zc in ((1, c1), (-1, c0)):
                corner = (sx * hw, zc)
                tri = [corner, (corner[0] - sx * m, corner[1]), (corner[0], corner[1] - sz * m)]
                parts.append(upright(tri, y_face, depth, "gilt_bronze", bevel=False))

    # --- Sides: an inset moulded panel and the drop handle (the grab points).
    for sx in (-1, 1):
        xs = sx * (hw + 0.002)
        loop = [(xs, -0.20, c0 + 0.05), (xs, 0.20, c0 + 0.05), (xs, 0.20, c1 - 0.05),
                (xs, -0.20, c1 - 0.05)]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="ebony_veneer", segments=4,
                          extras={"path": loop, "closed": True, "section": (0.006, 0.006),
                                  "up": (1, 0, 0), "bevel": False}))
        hz = 1.00
        for py in (-0.07, 0.07):
            parts.append(box((min(xs, xs + sx * 0.012), py - 0.012, hz - 0.004),
                             (max(xs, xs + sx * 0.012), py + 0.012, hz + 0.022),
                             "gilt_bronze", extras={"bevel": False}))
        bail = spline([(0.0, -0.07, hz), (0.004, -0.055, hz - 0.045), (0.008, 0.0, hz - 0.062),
                       (0.004, 0.055, hz - 0.045), (0.0, 0.07, hz)], 2)
        bail = [(xs + sx * (0.010 + dx), y, z) for dx, y, z in bail]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="gilt_bronze", segments=6,
                          extras={"path": bail, "section": (0.0055, 0.0055), "up": (1, 0, 0),
                                  "smooth": True, "bevel": False}))

    return blueprint(
        entry, parts,
        bevel=0.003,
        family_overrides={
            # Worn edges lighter, per the ebony note.
            "ebony_veneer": {"wear_to": "#3A3430", "wear_amount": 0.18, "grain": 0.12},
            "black_walnut": {"grain": 0.30},
            # Aged bone / the panels' hazy sky: blotched so it reads as the concept's
            # warm grey-khaki sky rather than clean white.
            "drawer_bone_inlay": {"grain": 0.45},
            # Gilt rubbed back to bronze on high points (JSON note).
            "gilt_bronze": {"wear_to": "#8A6A3E", "wear_amount": 0.25, "grain": 0.12},
            # "Copper" in the name makes spec.py call it metal, but the surface is
            # varnished oil paint on the copper sheet: dielectric.
            "painted_copper_panel": {"metal": 0.0, "rough": 0.4},
        },
        notes=["Built closed (the carry state); the 40-drawer bank is modelled under the "
               "doors so the open-door and burst-open states reuse it.",
               "Landscape sky on the door panels uses the vellum bone-inlay family "
               "(the pale ground of the concept's panels); the palette has no other light tone.",
               "Drawer faces are one box each here; the JSON's instancing (one drawer mesh, "
               "40 instances) is an engine-side import step."],
    )


# --------------------------------------------------------------------------------
# Venetian Mirror
# --------------------------------------------------------------------------------

def _crest_outline(base_z: float, half_w: float, height: float) -> list[tuple]:
    """A symmetric acanthus cresting in XZ: flared foot, five flame-leaf lobes with
    the tallest in the middle (the concept's front view)."""
    # (x fraction of half width, z fraction of height) for the right half, centre out.
    right = [(0.00, 1.00), (0.10, 0.80), (0.20, 0.55), (0.30, 0.62), (0.40, 0.78),
             (0.50, 0.66), (0.60, 0.42), (0.70, 0.38), (0.80, 0.46), (0.88, 0.30),
             (0.95, 0.12), (1.00, 0.03)]
    top = spline([(-x, z) for x, z in reversed(right[1:])] + right, 2)
    pts = [(half_w * x, base_z + height * z) for x, z in top]
    return [(half_w, base_z)] + pts[::-1] + [(-half_w, base_z)]


def venetian_mirror(entry: Entry):
    """Murano plate in a walnut cushion frame with a gilt bead-and-reel slip, four
    gilt rosettes and a pierced gilt acanthus crest; pine back with turn-buttons and
    an iron hanging ring. Stands on its bottom rail, glass facing -Y."""
    W, D, H = entry.dims                           # 0.80 × 0.08 × 1.10
    crest_h = 0.14
    # The JSON's dimension line (H 1.10 overall) and its frame bullet (frame outer
    # 0.80 × 1.10, crest 0.14 on top) disagree by the crest. The concept sheet sides
    # with the dimension line: its frame is 0.80 × 0.96 with the crest bringing it
    # to 1.10, and its plate callout (0.58 × 0.74 visible) only fits that frame.
    fw, fh = W, H - crest_h                        # frame outer 0.80 × 0.96
    rail = 0.09
    parts: list[Part] = []

    # Flat walnut backing frame (butt-jointed rails, hidden under the cushion).
    back_t = 0.034
    for lo, hi in (((-fw / 2, 0.0, 0.0), (fw / 2, back_t, rail)),
                   ((-fw / 2, 0.0, fh - rail), (fw / 2, back_t, fh)),
                   ((-fw / 2, 0.0, rail - 0.001), (-fw / 2 + rail, back_t, fh - rail + 0.001)),
                   ((fw / 2 - rail, 0.0, rail - 0.001), (fw / 2, back_t, fh - rail + 0.001))):
        parts.append(box(lo, hi, "black_walnut", extras={"bevel": False}))
    # Cushion moulding: a flattened half-round swept round the mitred rectangle.
    ix, iz0, iz1 = fw / 2 - rail / 2, rail / 2, fh - rail / 2
    # (12 sides and a 0.02 half-depth: flatter or finer and Blender's whole-mesh
    # recalc_face_normals in assemble.build_object turns this ring inside out.)
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="black_walnut", segments=12,
                      extras={"path": rect_loop(-ix, ix, iz0, iz1, 0.0), "closed": True,
                              "section": (rail / 2 - 0.001, 0.020), "up": (0, 0, 1),
                              "smooth": True, "bevel": False}))
    # A thin outer fillet so the frame edge reads square, as the concept draws it.
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="black_walnut", segments=4,
                      extras={"path": rect_loop(-fw / 2 + 0.006, fw / 2 - 0.006, 0.006,
                                                fh - 0.006, -0.002),
                              "closed": True, "section": (0.006, 0.008), "up": (0, 0, 1),
                              "bevel": False}))

    # Plate: forest-green glass, the 2 cm bevel showing as a green margin round the
    # silvered field (foxing specks from the family's wear).
    ox, oz0, oz1 = fw / 2 - rail, rail, fh - rail          # frame opening
    cz = fh / 2
    parts.append(box((-ox - 0.015, 0.000, oz0 - 0.015), (ox + 0.015, 0.006, oz1 + 0.015),
                     "forest_window_glass", extras={"bevel": False}))
    parts.append(upright(rounded_rect(2 * ox - 0.06, oz1 - oz0 - 0.06, 0.004, 1, cy=cz),
                         -0.001, 0.0012, "mirror_silvering", bevel=False))

    # Gilt slip between the glass and the walnut, with a run of beads (bead-and-reel).
    sx, sz0, sz1 = ox - 0.004, oz0 + 0.004, oz1 - 0.004
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="gilt", segments=6,
                      extras={"path": rect_loop(-sx, sx, sz0, sz1, -0.004), "closed": True,
                              "section": (0.010, 0.007), "up": (0, 0, 1), "smooth": True,
                              "bevel": False}))
    bead_pitch = 0.052
    for (ax, az), (bx, bz) in (((-sx, sz0), (sx, sz0)), ((sx, sz0), (sx, sz1)),
                               ((sx, sz1), (-sx, sz1)), ((-sx, sz1), (-sx, sz0))):
        length = math.hypot(bx - ax, bz - az)
        n = int(length / bead_pitch)
        for i in range(n):
            t = (i + 0.5) / n
            parts.append(disc(ax + (bx - ax) * t, az + (bz - az) * t, 0.010, -0.0125, 0.003,
                              "gilt", 6))

    # Corner rosettes: little gilt domed flowers on the cushion's mitres.
    for x in (-ix, ix):
        for z in (iz0, iz1):
            parts.append(Part("lathe", (x, -0.012, z), (1, 1, 1), mat="gilt", segments=8,
                              rot=(90.0, 0.0, 0.0),
                              extras={"profile": [(0.0, 0.0), (0.020, 0.0), (0.020, 0.003),
                                                  (0.013, 0.007), (0.006, 0.009), (0.0, 0.010)],
                                      "bevel": False}))

    # Crest: pierced acanthus cresting on the top rail, C-scroll curls in front.
    crest_z = fh - 0.012
    parts.append(upright(_crest_outline(crest_z, 0.20, crest_h + 0.012), -0.012, 0.022, "gilt",
                         bevel=False))
    for x, z, d in ((0.0, crest_z + 0.075, 0.034), (-0.075, crest_z + 0.050, 0.028),
                    (0.075, crest_z + 0.050, 0.028), (-0.135, crest_z + 0.030, 0.022),
                    (0.135, crest_z + 0.030, 0.022)):
        parts.append(Part("torus", (x, -0.015, z), (d, d, 0.008), mat="gilt", segments=10,
                          rings=4, minor=0.22, rot=(90.0, 0.0, 0.0), extras={"bevel": False}))
    # The piercings: dark voids behind the scrolls (the walnut shows through).
    for x, z, d in ((0.0, crest_z + 0.075, 0.018), (-0.075, crest_z + 0.050, 0.014),
                    (0.075, crest_z + 0.050, 0.014)):
        parts.append(disc(x, z, d, -0.0135, 0.002, "black_walnut", 8))

    # Back: pine backboard, four turn-buttons, the iron hanging ring on a staple.
    bb_y0 = back_t
    parts.append(box((-fw / 2 + 0.03, bb_y0, 0.03), (fw / 2 - 0.03, bb_y0 + 0.012, fh - 0.03),
                     "pine_backboard"))
    for x, z, rot in ((0.0, 0.05, 0.0), (0.0, fh - 0.05, 0.0),
                      (-fw / 2 + 0.06, cz, 90.0), (fw / 2 - 0.06, cz, 90.0)):
        parts.append(Part("box", (x, bb_y0 + 0.016, z), (0.05, 0.008, 0.016), mat="black_walnut",
                          rot=(0.0, rot, 0.0), extras={"bevel": False}))
    ring_z = fh - 0.11
    parts.append(Part("torus", (0.0, bb_y0 + 0.017, ring_z), (0.05, 0.05, 0.010), mat="iron_ring",
                      segments=10, rings=4, minor=0.12, rot=(90.0, 0.0, 0.0),
                      extras={"bevel": False}))
    parts.append(box((-0.008, bb_y0 + 0.010, ring_z + 0.018), (0.008, bb_y0 + 0.022, ring_z + 0.030),
                     "iron_ring", extras={"bevel": False}))

    return blueprint(
        entry, parts,
        bevel=0.003,
        extra_families={
            # The back bullet asks for a hanging iron ring; the JSON gives its hex
            # (#34404E) only in the pine backboard's notes, not as a material.
            "iron_ring": {"name": "Iron ring (hanging ring, staple)", "base": "#34404E",
                          "rough": 0.55, "metal": 1.0, "grain": 0.3},
        },
        family_overrides={
            # Foxing: grey specks through the silvering (JSON note).
            "mirror_silvering": {"wear_to": "#5E5F58", "wear_amount": 0.22, "grain": 0.05},
            "forest_window_glass": {"rough": 0.12},
            "black_walnut": {"grain": 0.28},
            # Gilt rubbed back to the walnut on the crest tips and corners.
            "gilt": {"wear_to": "#3B2A1E", "wear_amount": 0.18, "grain": 0.12},
        },
        notes=["Frame outer 0.80 × 0.96 m plus the 0.14 m crest = 1.10 m overall: the "
               "dimension line and concept sheet win over the frame bullet's 0.80 × 1.10 "
               "outer size (which would make the mirror 1.24 m with its crest).",
               "Live reflection (mirror shader) is an engine material; the bake gives a "
               "metallic 0.08-rough silver field.",
               "Pre-fractured 12-shard glass is a separate break mesh, not built here."],
    )


# --------------------------------------------------------------------------------
# Astrolabe
# --------------------------------------------------------------------------------

def _circle_xz(cx: float, cz: float, r: float, y: float, n: int) -> list[tuple]:
    return [(cx + r * math.cos(2 * math.pi * i / n), y, cz + r * math.sin(2 * math.pi * i / n))
            for i in range(n)]


def brass_astrolabe(entry: Entry):
    """Planispheric astrolabe hanging face-on (-Y): mater with a raised limb, the
    climate plate sunk inside it, the pierced rete and the front rule lying in the
    recess, pin and horse, the pierced throne, shackle and suspension ring on top,
    and the alidade with its two sighting vanes on the back."""
    W, D, H = entry.dims                        # 0.24 × 0.03 × 0.30
    R = 0.11                                    # mater 0.22 dia
    zc = R                                      # hangs with the mater's foot at z = 0
    limb_in = R - 0.015                         # limb 0.015 wide
    plate_z, limb_z = 0.006, 0.012              # recess depth / mater thickness (local z = -Y)
    parts: list[Part] = []

    # Mater: one lathe, local +Z turned to face -Y. The plate steps down twice
    # towards the centre on narrow slopes (engraved circles, one span each); those
    # slopes, the limb's inner wall and the degree-scale step are painted with grime
    # by their z level, since each sits at its own depth.
    g = 0.0006
    prof = [(0.0, 0.0), (R, 0.0), (R, 0.0115),                       # back, outer wall
            (R - 0.0055, 0.0115), (R - 0.0055, limb_z),              # degree scale step
            (limb_in, limb_z), (limb_in, plate_z),                   # limb, inner wall
            (0.0795, plate_z), (0.078, plate_z - g),                 # almucantar circles
            (0.0515, plate_z - g), (0.050, plate_z - 2 * g),
            (0.0, plate_z - 2 * g)]
    parts.append(Part("lathe", (0.0, 0.0, zc), (1, 1, 1), mat="gilt_brass", segments=24,
                      rot=(90.0, 0.0, 0.0), extras={"profile": prof, "paint": [
                          # outer edge rubbed back to brass
                          {"mat": "worn_brass", "min": (-1, -1, 0.001), "max": (1, 1, 0.0114)},
                          # The climate plate reads a shade darker than the rete over it
                          # (the concept's grimy engraved plate under the bright rete):
                          # it takes the worn-brass tone rather than the full gilt.
                          {"mat": "worn_brass", "min": (-limb_in, -limb_in, plate_z - 2 * g - 1e-5),
                           "max": (limb_in, limb_in, plate_z + 1e-5)},
                          # the two engraved slopes (centroids half a step down)
                          {"mat": "engraving_grime", "min": (-1, -1, plate_z - 0.6 * g),
                           "max": (1, 1, plate_z - 0.4 * g)},
                          {"mat": "engraving_grime", "min": (-1, -1, plate_z - 1.6 * g),
                           "max": (1, 1, plate_z - 1.4 * g)},
                          # limb inner wall
                          {"mat": "engraving_grime", "min": (-limb_in - 1e-4, -1, plate_z + 1e-4),
                           "max": (limb_in + 1e-4, 1, limb_z - 1e-4)},
                          # degree/hour dividing step
                          {"mat": "engraving_grime", "min": (-1, -1, 0.01151), "max": (1, 1, 0.01199)},
                      ]}))
    # Azimuth / hour lines: fine grime bars across the plate.
    for a in (90.0, 30.0, -30.0):
        t = math.radians(a)
        dx, dz = math.cos(t) * limb_in, math.sin(t) * limb_in
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="engraving_grime", segments=3,
                          extras={"path": [(-dx, -plate_z + 0.0002, zc - dz),
                                           (dx, -plate_z + 0.0002, zc + dz)],
                                  "section": (0.0004, 0.0007), "up": (0, 1, 0), "bevel": False}))

    # Rete: rim ring, the eccentric zodiac ring, four spokes and 20 flame pointers.
    ry = -(plate_z + 0.0012)
    rete = [
        Part("tube", (0, 0, 0), (1, 1, 1), mat="gilt_brass", segments=4,
             extras={"path": _circle_xz(0.0, zc, limb_in - 0.004, ry, 20), "closed": True,
                     "section": (0.0012, 0.003), "up": (0, 1, 0), "bevel": False}),
        Part("tube", (0, 0, 0), (1, 1, 1), mat="gilt_brass", segments=4,
             extras={"path": _circle_xz(0.0, zc + 0.024, 0.058, ry, 16), "closed": True,
                     "section": (0.0012, 0.0055), "up": (0, 1, 0), "bevel": False}),
    ]
    for a in (80.0, 160.0, 250.0, 335.0):
        t = math.radians(a)
        rete.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="gilt_brass", segments=4,
                         extras={"path": [(0.0, ry, zc), (math.cos(t) * (limb_in - 0.004), ry,
                                                           zc + math.sin(t) * (limb_in - 0.004))],
                                 "section": (0.001, 0.0022), "up": (0, 1, 0), "bevel": False}))
    stars = [(0.040, 20), (0.052, 55), (0.084, 38), (0.070, 100), (0.060, 128), (0.086, 150),
             (0.045, 175), (0.078, 198), (0.064, 215), (0.088, 232), (0.036, 245),
             (0.074, 262), (0.058, 283), (0.086, 300), (0.046, 318), (0.070, 345),
             (0.083, 8), (0.030, 120), (0.080, 80), (0.052, 290)]
    for i, (r, a) in enumerate(stars):
        t = math.radians(a)
        tip = (math.cos(t) * r, zc + math.sin(t) * r)
        back = math.radians(a + (150 if i % 2 else 210))       # flames lean alternately
        bx, bz = tip[0] + math.cos(back) * 0.013, tip[1] + math.sin(back) * 0.013
        nx, nz = -math.sin(back) * 0.0032, math.cos(back) * 0.0032
        rete.append(upright([tip, (bx + nx, bz + nz), (bx - nx, bz - nz)],
                            ry - 0.0012, 0.0024, "gilt_brass", bevel=False))
    parts += rete

    # Front rule, lying in the recess over the rete: fiducial-edged bar 0.19 m.
    rule_a = math.radians(24.0)
    ru = (math.cos(rule_a), math.sin(rule_a))
    rn = (-ru[1], ru[0])
    L, w = 0.094, 0.0065
    rule = [(ru[0] * L, zc + ru[1] * L), (ru[0] * (L - 0.012) + rn[0] * w, zc + ru[1] * (L - 0.012) + rn[1] * w),
            (-ru[0] * (L - 0.012) + rn[0] * w, zc - ru[1] * (L - 0.012) + rn[1] * w),
            (-ru[0] * L, zc - ru[1] * L),
            (-ru[0] * (L - 0.012) - rn[0] * w, zc - ru[1] * (L - 0.012) - rn[1] * w),
            (ru[0] * (L - 0.012) - rn[0] * w, zc + ru[1] * (L - 0.012) - rn[1] * w)]
    rule_y = ry - 0.0012 - 0.0022
    parts.append(upright(rule, rule_y, 0.0022, "worn_brass", bevel=False))
    # Graduation ticks on the rule (grime).
    parts.append(upright([(-ru[0] * 0.08 + rn[0] * 0.002, zc - ru[1] * 0.08 + rn[1] * 0.002),
                          (ru[0] * 0.08 + rn[0] * 0.002, zc + ru[1] * 0.08 + rn[1] * 0.002),
                          (ru[0] * 0.08 + rn[0] * 0.0032, zc + ru[1] * 0.08 + rn[1] * 0.0032),
                          (-ru[0] * 0.08 + rn[0] * 0.0032, zc - ru[1] * 0.08 + rn[1] * 0.0032)],
                         rule_y - 0.0003, 0.0004, "engraving_grime", bevel=False))

    # Pin through everything, and the horse-head wedge through its slot.
    pin_front = rule_y - 0.0045
    parts.append(Part("cyl", (0.0, (pin_front + 0.004) / 2.0, zc), (0.005, 0.004 - pin_front, 0.005),
                      mat="steel_pin", segments=8, rot=(90.0, 0.0, 0.0), extras={"bevel": False}))
    parts.append(Part("sphere", (0.0, pin_front + 0.0005, zc), (0.0075, 0.004, 0.0075),
                      mat="steel_pin", segments=6, rings=4, extras={"bevel": False}))
    horse = spline([(0.004, zc + 0.003), (0.012, zc + 0.006), (0.024, zc + 0.004),
                    (0.030, zc - 0.001), (0.022, zc - 0.004), (0.010, zc - 0.004),
                    (0.004, zc - 0.003)], 2)
    parts.append(upright(horse, rule_y - 0.0032, 0.0032, "steel_pin", bevel=False))

    # Throne: pierced scrollwork shoulder standing on the top of the mater.
    # Rises from the mater's shoulders (z ~ 0.19 at x = ±0.07) to a neck at 0.247.
    right = [(0.0095, 0.247), (0.013, 0.241), (0.022, 0.233), (0.036, 0.223),
             (0.050, 0.213), (0.062, 0.201), (0.071, 0.187)]
    throne = spline(right, 2)
    # Its foot follows the limb (r = 0.099) so it never covers the sunken plate.
    foot = [(0.099 * math.cos(math.radians(a)), zc + 0.099 * math.sin(math.radians(a)))
            for a in (44, 58, 74, 90, 106, 122, 136)]
    outline = [(-x, z) for x, z in reversed(throne)] + throne + foot
    parts.append(upright(outline, -0.0115, 0.010, "gilt_brass", bevel=False))
    for x, z, d in ((0.0, 0.237, 0.008), (-0.029, 0.226, 0.011), (0.029, 0.226, 0.011)):
        # The piercing, shown on the face only (the back of the throne sits
        # against the hand; the triangles went to the rete's 20 pointers instead).
        parts.append(disc(x, z, d, -0.0119, 0.0005, "engraving_grime", 6))
    # Shackle and suspension ring (worn brass, 5 mm stock, 0.05 dia).
    parts.append(Part("box", (0.0, -0.006, 0.2515), (0.011, 0.006, 0.013), mat="worn_brass",
                      extras={"bevel": False}))
    ring_d = 0.046     # 0.05 in the bullet, 0.04 on the concept callout; 0.046 fits H 0.30
    parts.append(Part("torus", (0.0, -0.006, H - ring_d / 2.0), (ring_d - 0.005,) * 3,
                      mat="worn_brass", segments=12, rings=4, minor=0.005 / (ring_d - 0.005),
                      rot=(90.0, 0.0, 0.0), extras={"bevel": False, "smooth": True}))

    # Alidade on the back, with its two sighting vanes standing proud.
    al_a = math.radians(78.0)
    au = (math.cos(al_a), math.sin(al_a))
    an = (-au[1], au[0])
    Lb, wb = 0.100, 0.006
    alidade = [(au[0] * Lb + an[0] * wb * 0.4, zc + au[1] * Lb + an[1] * wb * 0.4),
               (-au[0] * Lb + an[0] * wb * 0.4, zc - au[1] * Lb + an[1] * wb * 0.4),
               (-au[0] * Lb - an[0] * wb, zc - au[1] * Lb - an[1] * wb),
               (au[0] * Lb - an[0] * wb, zc + au[1] * Lb - an[1] * wb)]
    parts.append(upright(alidade, 0.0, 0.0022, "gilt_brass", bevel=False))
    for s in (-1, 1):
        cx, cz = s * au[0] * 0.070, zc + s * au[1] * 0.070
        parts.append(Part("box", (cx, 0.0022 + 0.0065, cz), (0.011, 0.013, 0.0022),
                          mat="gilt_brass", rot=(0.0, 90.0 - math.degrees(al_a), 0.0),
                          extras={"bevel": False}))

    return blueprint(
        entry, parts,
        bevel=0.0,          # coin-thin brass: the bevel would triple the rims for nothing
        family_overrides={
            # Gilt rubbed back to brass on high points (JSON: rim, throne, ring).
            "gilt_brass": {"wear_to": "#A07E3E", "wear_amount": 0.22, "grain": 0.10},
            "worn_brass": {"grain": 0.16},
        },
        notes=["Hangs face-on, mater foot at z = 0; the throne and ring give the height.",
               "Mater is 0.22 m (the build bullet); the dimension line's 0.24 m W is not "
               "reached by anything the bullets list, and 0.225 m is inside tolerance.",
               "Degree ticks, hour letters and star names are below mesh resolution: the "
               "limb step, two almucantar grooves and three azimuth lines carry the "
               "engraving at game distance; the rest belongs in the albedo/normal.",
               "Rete and rule are modelled in place; the break state splits them off."],
    )


# --------------------------------------------------------------------------------
# Silver Service Tureen
# --------------------------------------------------------------------------------

def _profile_r(profile, z: float) -> float:
    """Radius of a lathe profile at height z (first rising span that contains z)."""
    for (r0, z0), (r1, z1) in zip(profile, profile[1:]):
        if z0 <= z <= z1 and z1 > z0:
            return r0 + (r1 - r0) * (z - z0) / (z1 - z0)
    raise ValueError(f"z={z} is outside the profile")


def silver_tureen(entry: Entry):
    """Oval hammered-silver tureen on a shaped skirt and four lion's-paw feet, scroll
    loop handles with leaf thumb-pieces on the short (X) ends, domed lid with a
    pomegranate finial and a ladle standing out of the lid's notch. The long sides
    face ±Y; the model faces -Y."""
    W, D, H = entry.dims                          # 0.46 × 0.30 × 0.36 with lid, finial, ladle
    # Rim 0.40 × 0.26 in the bullet; 0.70 (0.40 × 0.28) so the lid flange reaches
    # the dimension line's 0.30 m D within tolerance.
    OVAL = 0.70
    oval = (1.0, OVAL, 1.0)
    parts: list[Part] = []

    # --- Bowl: one lathe, squashed oval. Outer wall, everted 0.01 m rim, then the
    # inside back down to a floor of dried soup (visible with the lid off).
    rim_z = 0.210
    outer = [(0.120, 0.056), (0.142, 0.062), (0.158, 0.071), (0.170, 0.085), (0.178, 0.115),
             (0.176, 0.145), (0.184, 0.180), (0.192, 0.198), (0.200, 0.204),
             (0.200, rim_z)]
    bowl_profile = ([(0.0, 0.056)] + outer +
                    [(0.188, rim_z), (0.180, 0.196), (0.166, 0.140), (0.150, 0.095),
                     (0.110, 0.074), (0.0, 0.070)])
    parts.append(Part("lathe", (0, 0, 0), oval, mat="sterling_silver", segments=24,
                      extras={"profile": bowl_profile, "smooth": True, "paint": [
                          # bright polish on the rolled rim
                          {"mat": "bright_polish", "min": (-1, -1, 0.201), "max": (1, 1, 0.215)},
                          # dried soup on the floor inside
                          {"mat": "soup_residue", "min": (-0.13, -0.13, 0.069), "max": (0.13, 0.13, 0.080)},
                      ]}))

    # Skirt: a shaped oval foot-ring under the bowl, the paws beneath it.
    # The top is rolled (no flat step), or it mirrors the studio's area lights as
    # bright rectangles.
    skirt = [(0.0, 0.036), (0.140, 0.036), (0.158, 0.042), (0.161, 0.050), (0.154, 0.057),
             (0.140, 0.061), (0.122, 0.064), (0.0, 0.064)]
    parts.append(Part("lathe", (0, 0, 0), (1.0, 0.70, 1.0), mat="sterling_silver", segments=24,
                      extras={"profile": skirt, "smooth": True}))

    # Lion's-paw feet: a tarnished ankle flaring to a pad, three silver toes in front.
    paw = [(0.0, 0.0), (0.024, 0.0), (0.024, 0.006), (0.018, 0.016), (0.013, 0.030),
           (0.016, 0.040), (0.0, 0.040)]
    for fx in (-0.105, 0.105):
        for fy in (-0.068, 0.068):
            parts.append(Part("lathe", (fx, fy, 0.0), (1, 1, 1), mat="silver_tarnish", segments=8,
                              extras={"profile": paw, "smooth": True, "bevel": False}))
            ax, ay = fx / math.hypot(fx, fy), fy / math.hypot(fx, fy)   # outward
            px, py = -ay, ax
            for k in (-1, 0, 1):
                parts.append(Part("sphere", (fx + ax * 0.018 + px * 0.011 * k,
                                             fy + ay * 0.018 + py * 0.011 * k, 0.008),
                                  (0.013, 0.013, 0.016), mat="sterling_silver",
                                  segments=6, rings=3, extras={"bevel": False, "smooth": True}))

    # Scroll loop handles on the short ends, each with a leaf thumb-piece on top.
    loop = spline([(0.186, 0.0, 0.207), (0.206, 0.0, 0.218), (0.222, 0.0, 0.205),
                   (0.228, 0.0, 0.180), (0.223, 0.0, 0.155), (0.206, 0.0, 0.141),
                   (0.168, 0.0, 0.134)], 2)
    for side in (-1, 1):
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="sterling_silver", segments=6,
                          extras={"path": [(side * x, y, z) for x, y, z in loop],
                                  "section": (0.0065, 0.009), "up": (0, 1, 0),
                                  "smooth": True, "bevel": False}))
        parts.append(Part("cone", (side * 0.208, 0.0, 0.232), (0.016, 0.012, 0.030),
                          mat="sterling_silver", segments=5, rot=(0.0, side * 12.0, 0.0),
                          extras={"bevel": False}))
        # tarnish where the handle joins the belly
        parts.append(Part("sphere", (side * 0.172, 0.0, 0.134), (0.018, 0.022, 0.016),
                          mat="silver_tarnish", segments=6, rings=4,
                          extras={"bevel": False, "smooth": True}))

    # Engraved arms: a double lozenge on each long side, riding the belly.
    def on_belly(x: float, z: float, sign: float) -> tuple:
        r = _profile_r(outer, z)
        return (x, sign * (OVAL * math.sqrt(max(r * r - x * x, 0.0)) + 0.0008), z)

    for sign in (-1, 1):
        for half_w, half_h in ((0.030, 0.042), (0.017, 0.024)):
            corners = [(0.0, 0.120 + half_h), (half_w, 0.120), (0.0, 0.120 - half_h),
                       (-half_w, 0.120)]
            path = []
            for (x0, z0), (x1, z1) in zip(corners, corners[1:] + corners[:1]):
                for t in (0.0, 0.5):
                    path.append(on_belly(x0 + (x1 - x0) * t, z0 + (z1 - z0) * t, sign))
            parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="silver_tarnish", segments=3,
                              extras={"path": path, "closed": True, "section": (0.0012, 0.0016),
                                      "up": (0, sign, 0), "bevel": False}))

    # --- Lid: domed oval with a stepped flange, polished crown, the ladle notch
    # (a dark cut at the +X end), and the pomegranate finial.
    lid_profile = [(0.0, 0.206), (0.203, 0.206), (0.205, 0.214), (0.196, 0.218),
                   (0.190, 0.224), (0.170, 0.242), (0.140, 0.262), (0.100, 0.276),
                   (0.050, 0.284), (0.0, 0.286)]
    parts.append(Part("lathe", (0, 0, 0), oval, mat="sterling_silver", segments=24,
                      extras={"profile": lid_profile, "smooth": True, "paint": [
                          # polished crown: every face above the 0.262 m row (an oval cap)
                          {"mat": "bright_polish", "min": (-1, -1, 0.260), "max": (1, 1, 0.29)},
                          {"mat": "bright_polish", "min": (-1, -1, 0.2065), "max": (1, 1, 0.2155)},
                          {"mat": "silver_tarnish", "min": (0.140, -0.030, 0.2065), "max": (1, 0.030, 0.245)},
                      ]}))
    fin = [(0.0, 0.284), (0.009, 0.284), (0.007, 0.293), (0.010, 0.298), (0.018, 0.308),
           (0.020, 0.318), (0.015, 0.328), (0.007, 0.332), (0.0, 0.333)]
    parts.append(Part("lathe", (0, 0, 0), (1, 1, 1), mat="sterling_silver", segments=10,
                      extras={"profile": fin, "smooth": True, "bevel": False,
                              "paint": [{"mat": "silver_tarnish", "min": (-1, -1, 0.284),
                                         "max": (1, 1, 0.296)}]}))
    for k in range(5):                                         # the pomegranate's crown
        a = 2 * math.pi * k / 5
        parts.append(Part("cone", (math.cos(a) * 0.006, math.sin(a) * 0.006, 0.337),
                          (0.006, 0.006, 0.012), mat="sterling_silver", segments=4,
                          rot=(math.degrees(-math.sin(a)) * 0.3, math.degrees(math.cos(a)) * 0.3, 0.0),
                          extras={"bevel": False}))

    # --- Ladle: stem out of the notch at 28° from vertical, a flat oval terminal,
    # and its 0.07 m bowl down inside the tureen.
    notch = (0.160, 0.0, 0.228)
    lean = 28.0
    dx, dz = math.sin(math.radians(lean)), math.cos(math.radians(lean))
    tip = (notch[0] + dx * 0.130, 0.0, notch[2] + dz * 0.130)
    heel = (notch[0] - dx * 0.125, 0.0, notch[2] - dz * 0.125)
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="sterling_silver", segments=6,
                      extras={"path": [heel, notch, tip], "section": (0.0045, 0.0035),
                                "up": (0, 1, 0), "smooth": True, "bevel": False}))
    parts.append(Part("sphere", (tip[0] + dx * 0.010, 0.0, tip[2] + dz * 0.010),
                      (0.022, 0.010, 0.032), mat="bright_polish", segments=8, rings=5,
                      rot=(0.0, lean, 0.0), extras={"bevel": False, "smooth": True}))
    cup = [(0.0, 0.0), (0.020, 0.004), (0.032, 0.014), (0.035, 0.024), (0.031, 0.026),
           (0.028, 0.017), (0.018, 0.009), (0.0, 0.006)]
    parts.append(Part("lathe", (heel[0] - 0.012, 0.0, heel[2] - 0.020), (1, 1, 1),
                      mat="sterling_silver", segments=10,
                      extras={"profile": cup, "smooth": True, "bevel": False}))

    return blueprint(
        entry, parts,
        bevel=0.0,          # all turned and cast work; smooth shading carries the curves
        family_overrides={
            # Hammer marks: patches rubbed toward tarnish across the silver (the
            # facets themselves belong in the normal map EnemyForge does not bake).
            "sterling_silver": {"wear_to": "#8E8A7E", "wear_amount": 0.30, "grain": 0.10},
            # Polish and tarnish are states of the same metal, not paints.
            "bright_polish": {"metal": 1.0, "rough": 0.12},
            "silver_tarnish": {"metal": 0.8, "rough": 0.5},
            "soup_residue": {"rough": 0.85},
        },
        notes=["Lid and ladle are modelled in place on the same mesh and texture set; "
               "splitting them into separate physics objects is an import step.",
               "The ladle notch is a tarnished patch on the lid flange where the stem "
               "rests, not a cut: a real cut would split the lid's single shell.",
               "The engraved coat of arms is a raised double lozenge line in tarnish; "
               "the charges inside belong in the albedo/normal."],
    )


BLUEPRINTS = {
    "curiosity-cabinet": curiosity_cabinet,
    "venetian-mirror": venetian_mirror,
    "brass-astrolabe": brass_astrolabe,
    "silver-tureen": silver_tureen,
}
