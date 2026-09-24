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


BLUEPRINTS = {
    "curiosity-cabinet": curiosity_cabinet,
}
