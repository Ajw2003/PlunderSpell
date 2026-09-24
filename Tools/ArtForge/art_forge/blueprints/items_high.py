"""High Medieval plunder (docs/art/data/high.json, items)."""

from __future__ import annotations

import math

from ..kit import Part, arc_path, gable_outline, rounded_rect, spline
from ..spec import Entry
from . import blueprint


# --------------------------------------------------------------------------------
# Helpers for flat work standing in the XZ plane (panels, reliefs, frames)
# --------------------------------------------------------------------------------

def upright(outline_xz, y_front: float, depth: float, mat: str, bevel: bool = True,
            **kwargs) -> Part:
    """A prism whose outline is drawn in world XZ (x across, z up), `depth` thick
    along +Y starting at `y_front` (the face towards the viewer, who stands at -Y)."""
    return Part("prism", (0.0, y_front + depth / 2.0, 0.0), (1.0, 1.0, depth), mat=mat,
                rot=(90.0, 0.0, 0.0),
                extras={"outline": list(outline_xz), "bevel": bevel}, **kwargs)


def disc(x: float, z: float, diameter: float, y_front: float, depth: float, mat: str,
         segments: int = 16, bevel: bool = False, **kwargs) -> Part:
    """A coin-like cylinder facing -Y (roundels, halos, faces, bosses). Unbevelled
    by default: a bevelled rim costs ~4 triangles per segment."""
    return Part("cyl", (x, y_front + depth / 2.0, z), (diameter, diameter, depth), mat=mat,
                rot=(90.0, 0.0, 0.0), segments=segments, extras={"bevel": bevel}, **kwargs)


def figure_outline(cx: float, base_z: float, height: float, hem: float) -> list[tuple]:
    """A standing, robed figure below the neck: wide hem, sloping shoulders."""
    h, w = height, hem
    return [(cx - 0.50 * w, base_z), (cx + 0.50 * w, base_z),
            (cx + 0.43 * w, base_z + 0.45 * h), (cx + 0.37 * w, base_z + 0.82 * h),
            (cx + 0.26 * w, base_z + 0.97 * h), (cx + 0.08 * w, base_z + h),
            (cx - 0.08 * w, base_z + h), (cx - 0.26 * w, base_z + 0.97 * h),
            (cx - 0.37 * w, base_z + 0.82 * h), (cx - 0.43 * w, base_z + 0.45 * h)]


def round_arch_path(x0: float, x1: float, z0: float, spring: float, y: float,
                    steps: int = 8) -> list[tuple]:
    """Up one side, a semicircular head, down the other: a niche outline for a tube."""
    cx, r = (x0 + x1) / 2.0, (x1 - x0) / 2.0
    head = arc_path((cx, y, spring), r, 180.0, 0.0, steps,
                    axis_u=(1.0, 0.0, 0.0), axis_v=(0.0, 0.0, 1.0))
    return [(x0, y, z0)] + head + [(x1, y, z0)]


# --------------------------------------------------------------------------------
# Gilded altarpiece
# --------------------------------------------------------------------------------

def gilded_altarpiece(entry: Entry):
    """The chapel triptych in its carry state: wings folded shut over the gabled
    centre panel, standing on the roundel predella. Outer wing faces are grisaille;
    the gold-ground paintings inside are modelled too (as shallow relief between
    the panel and the closed wings) so the open state needs no new art."""
    W, D, H = entry.dims                       # 1.10 × 0.20 × 1.60 closed
    hw = W / 2.0
    pred_h, pred_d = 0.22, D - 0.005           # predella box 1.10 × 0.20 × 0.22
    panel_top, apex = pred_h + 1.20, H          # panel rect to 1.42, gable point at 1.60
    panel_t, wing_t = 0.05, 0.03
    panel_y0 = 0.0                              # centre panel front face
    wing_y0 = panel_y0 - 0.005 - wing_t         # closed wings' outer (front) face
    parts: list[Part] = []

    # --- Predella: oak box, gilt front field, five woad roundels with gesso busts.
    parts.append(Part("box", (0.0, 0.0, pred_h / 2.0), (W, pred_d, pred_h), mat="oak_panel"))
    front = -pred_d / 2.0
    parts.append(upright(rounded_rect(W - 0.08, pred_h - 0.06, 0.01, 1, cy=pred_h / 2.0),
                         front - 0.003, 0.006, "gilt"))
    for i in range(5):
        x = (i - 2) * 0.205
        parts.append(disc(x, pred_h / 2.0, 0.12, front - 0.006, 0.006, "woad_azure", 16))
        # Gesso bust: head and shoulders in low relief on the roundel.
        parts.append(upright([(x - 0.032, pred_h / 2.0 - 0.042), (x + 0.032, pred_h / 2.0 - 0.042),
                              (x + 0.026, pred_h / 2.0 - 0.012), (x + 0.010, pred_h / 2.0 - 0.004),
                              (x - 0.010, pred_h / 2.0 - 0.004), (x - 0.026, pred_h / 2.0 - 0.012)],
                             front - 0.0085, 0.004, "gesso", bevel=False))
        parts.append(disc(x, pred_h / 2.0 + 0.016, 0.028, front - 0.0085, 0.004, "gesso", 8))

    # --- Centre panel: oak board with the gable, 3 planks (joints at x = ±0.18).
    parts.append(upright(gable_outline(W, panel_top, apex, base=pred_h),
                         panel_y0, panel_t, "oak_panel"))
    # Gable field: gilt inside a 0.04 m oak border, a woad roundel with a gesso
    # emblem, and a gilt moulding along each raking edge and across the eaves.
    border = 0.045
    rise = (apex - panel_top) / hw
    inner = [(-hw + border, panel_top + 0.012), (hw - border, panel_top + 0.012),
             (0.0, apex - border * math.sqrt(1 + rise * rise))]
    parts.append(upright(inner, panel_y0 - 0.004, 0.006, "gilt"))
    roundel_z = panel_top + 0.07
    parts.append(disc(0.0, roundel_z, 0.12, panel_y0 - 0.010, 0.008, "woad_azure", 16))
    chevron = [(-0.030, roundel_z - 0.022), (0.0, roundel_z + 0.020), (0.030, roundel_z - 0.022),
               (0.018, roundel_z - 0.022), (0.0, roundel_z + 0.002), (-0.018, roundel_z - 0.022)]
    parts.append(upright(chevron, panel_y0 - 0.013, 0.004, "gesso", bevel=False))
    for side in (1, -1):
        rake = [(side * (hw - 0.012), panel_y0 - 0.008, panel_top + 0.004),
                (0.0, panel_y0 - 0.008, apex - 0.012)]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="gilt", segments=4,
                          extras={"path": rake, "section": (0.011, 0.011), "smooth": True,
                                  "bevel": False}))
    parts.append(Part("box", (0.0, (wing_y0 + panel_y0 + panel_t) / 2.0, panel_top + 0.012),
                      (W + 0.02, panel_y0 + panel_t - wing_y0 + 0.012, 0.024), mat="gilt"))

    # Crockets: four carved leaves per raking edge, pointing out of the slope,
    # and a pinnacle knop at the apex.
    edge = math.atan2(apex - panel_top, hw)                 # slope angle
    tilt = math.degrees(math.atan2(math.sin(edge), math.cos(edge)))
    for t in (0.18, 0.38, 0.58, 0.78):
        x, z = hw * (1.0 - t), panel_top + (apex - panel_top) * t
        nx, nz = math.sin(edge), math.cos(edge)
        parts.append(Part("cone", (x + nx * 0.030, panel_y0 + panel_t / 2.0, z + nz * 0.030),
                          (0.034, 0.030, 0.055), mat="gilt", segments=5,
                          rot=(0.0, tilt, 0.0), mirror=True, extras={"bevel": False}))
    parts.append(Part("lathe", (0.0, panel_y0 + panel_t / 2.0, apex - 0.02), (1, 1, 1),
                      mat="gilt", segments=8,
                      extras={"profile": [(0.0, 0.0), (0.022, 0.012), (0.016, 0.032),
                                          (0.024, 0.044), (0.0, 0.066)],
                              "bevel": False}))

    # Hidden until opened: the gold-ground centre painting — the Virgin enthroned
    # in a woad mantle over a kermes robe, gesso face, the Child on her knee.
    field = (panel_y0 - 0.002, 0.002)
    parts.append(upright(rounded_rect(W - 0.10, panel_top - pred_h - 0.06, 0.01, 1,
                                      cy=(pred_h + panel_top) / 2.0), *field, "gilt",
                         bevel=False))
    vz = pred_h + 0.14
    parts.append(upright(figure_outline(0.02, vz, 0.80, 0.50), panel_y0 - 0.0035, 0.0015, "kermes", bevel=False))
    parts.append(upright(figure_outline(-0.02, vz, 0.78, 0.44), panel_y0 - 0.0045, 0.0015, "woad_azure", bevel=False))
    parts.append(disc(0.0, vz + 0.86, 0.10, panel_y0 - 0.0045, 0.0015, "gesso", 10))
    parts.append(upright(figure_outline(0.03, vz + 0.36, 0.16, 0.10), panel_y0 - 0.0048, 0.0010, "gesso", bevel=False))

    # --- Wings, folded shut: oak boards on iron strap hinges at the outer edges.
    wing_w, wing_h = hw - 0.004, 1.20
    for side in (1, -1):
        cx = side * (0.004 + wing_w / 2.0)
        parts.append(Part("box", (cx, wing_y0 + wing_t / 2.0, pred_h + wing_h / 2.0),
                          (wing_w, wing_t, wing_h), mat="oak_panel"))
        # Outer face: grisaille field, a round-arched niche, a standing saint in
        # stone-grey relief with a gilt halo and gesso face.
        x0, x1 = cx - wing_w / 2.0 + 0.040, cx + wing_w / 2.0 - 0.040
        parts.append(upright(rounded_rect(x1 - x0, wing_h - 0.08, 0.006, 1, cx=cx,
                                          cy=pred_h + wing_h / 2.0),
                             wing_y0 - 0.003, 0.005, "grisaille"))
        niche = round_arch_path(x0 + 0.03, x1 - 0.03, pred_h + 0.06, pred_h + wing_h - 0.26,
                                wing_y0 - 0.006, steps=8)
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="oak_panel", segments=4,
                          extras={"path": niche, "section": (0.006, 0.006), "smooth": True,
                                  "up": (0.0, 1.0, 0.0), "bevel": False}))
        fig_z, fig_h, hem = pred_h + 0.08, 0.80, 0.30
        parts.append(upright(figure_outline(cx, fig_z, fig_h, hem),
                             wing_y0 - 0.011, 0.008, "grisaille"))
        # Grisaille modelling as the concept paints it: a pale lit fold down the
        # robe's right side and dark fold lines on its left.
        parts.append(upright([(cx + 0.02, fig_z), (cx + 0.5 * hem - 0.012, fig_z),
                              (cx + 0.37 * hem - 0.010, fig_z + 0.82 * fig_h),
                              (cx + 0.10 * hem, fig_z + 0.96 * fig_h),
                              (cx + 0.06, fig_z + 0.55 * fig_h)],
                             wing_y0 - 0.013, 0.003, "gesso", bevel=False))
        for fx, top in ((-0.085, 0.70), (-0.045, 0.78), (-0.005, 0.62)):
            parts.append(upright([(cx + fx - 0.004, fig_z + 0.01), (cx + fx + 0.004, fig_z + 0.01),
                                  (cx + fx * 0.55 + 0.002, fig_z + top * fig_h),
                                  (cx + fx * 0.55 - 0.002, fig_z + top * fig_h)],
                                 wing_y0 - 0.0125, 0.002, "oak_panel", bevel=False))
        # Head on the shoulders; the gilt halo only just larger than the face.
        head_z = fig_z + fig_h + 0.045
        parts.append(disc(cx, head_z + 0.012, 0.155, wing_y0 - 0.006, 0.003, "gilt", 16))
        parts.append(disc(cx, head_z, 0.105, wing_y0 - 0.013, 0.010, "gesso", 12))
        # Inner face (towards the centre panel): a saint on gold, hidden until opened.
        inner_y = wing_y0 + wing_t
        parts.append(upright(rounded_rect(x1 - x0, wing_h - 0.08, 0.006, 1, cx=cx,
                                          cy=pred_h + wing_h / 2.0), inner_y - 0.001, 0.0025, "gilt", bevel=False))
        parts.append(upright(figure_outline(cx, pred_h + 0.10, 0.76, 0.28),
                             inner_y + 0.0012, 0.0015, "kermes" if side < 0 else "woad_azure", bevel=False))
        # Two iron strap hinges on the outer edge, knuckles on the joint.
        for hz in (pred_h + 0.20, pred_h + wing_h - 0.20):
            parts.append(Part("box", (side * (hw - 0.07), wing_y0 - 0.003, hz),
                              (0.14, 0.006, 0.036), mat="strap_iron"))
            parts.append(Part("cyl", (side * (hw + 0.004), wing_y0 + 0.010, hz),
                              (0.022, 0.022, 0.07), mat="strap_iron", segments=8,
                              extras={"bevel": False}))

    # --- Back: two iron carrying staples, 0.20 m in from each end of the predella.
    back = pred_d / 2.0
    for side in (1, -1):
        x = side * (hw - 0.20)
        staple = [(x - 0.05, back - 0.012, pred_h * 0.55), (x - 0.05, back + 0.008, pred_h * 0.55),
                  (x + 0.05, back + 0.008, pred_h * 0.55), (x + 0.05, back - 0.012, pred_h * 0.55)]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="strap_iron", segments=6,
                          extras={"path": staple, "section": (0.007, 0.007), "smooth": True,
                                  "up": (0.0, 0.0, 1.0), "bevel": False}))

    return blueprint(
        entry, parts,
        bevel=0.003,
        extra_families={
            # The build bullets hang the wings on iron strap hinges and give the
            # back iron staples; the JSON palette lists no iron.
            "strap_iron": {"name": "Strap iron (hinges, staples)", "base": "#2E2A26",
                           "rough": 0.6, "metal": 1.0, "grain": 0.3},
        },
        family_overrides={
            # Gold leaf rubbed through to the red bole where hands touch.
            "gilt": {"wear_to": "#7E2A26", "wear_amount": 0.28, "grain": 0.14},
            "oak_panel": {"grain": 0.32},
            "grisaille": {"grain": 0.22},
        },
        notes=["Built closed (the carry state the JSON dimensions describe); the "
               "centre painting and wing interiors are modelled as relief under the "
               "wings, so the open state reuses this mesh once the wings are split off.",
               "Punch-work halos, plank joints, worm holes and candle soot are left to "
               "the painted-panel atlas / normal map, which EnemyForge's bake does not make."],
    )


# --------------------------------------------------------------------------------
# Arm reliquary
# --------------------------------------------------------------------------------

def _oval_point(r: float, k: float, theta_deg: float, z: float, lift: float = 0.0):
    """A point on an oval section (x radius r, y radius k*r) at angle theta, pushed
    `lift` metres out along the section's outward normal; returns (point, normal)."""
    t = math.radians(theta_deg)
    nx, ny = math.cos(t), math.sin(t) / k
    n = math.hypot(nx, ny)
    nx, ny = nx / n, ny / n
    return (r * math.cos(t) + nx * lift, k * r * math.sin(t) + ny * lift, z), (nx, ny, 0.0)


def arm_reliquary(entry: Entry):
    """A silver forearm raised in blessing on a stepped plinth: oval tapered sleeve
    with chased folds and gilt edge bands, a rock-crystal window on the relic, a
    jewelled cuff, and a hand with index and middle fingers raised."""
    W, D, H = entry.dims                      # 0.16 × 0.14 × 0.52
    parts: list[Part] = []

    # --- Plinth: lower gilt step with seven garnets on the front, upper silver step,
    # a niello moulding line between them.
    parts.append(Part("prism", (0, 0, 0.015), (1, 1, 0.03), mat="gilt",
                      extras={"outline": rounded_rect(W, D, 0.018, 1)}))
    parts.append(Part("prism", (0, 0, 0.0315), (1, 1, 0.003), mat="niello_soot",
                      extras={"outline": rounded_rect(0.136, 0.116, 0.012, 1), "bevel": False}))
    parts.append(Part("prism", (0, 0, 0.0465), (1, 1, 0.027), mat="silver",
                      extras={"outline": rounded_rect(0.13, 0.11, 0.012, 1), "bevel": False}))
    for i in range(7):
        parts.append(Part("sphere", ((i - 3) * 0.02, -D / 2.0 - 0.001, 0.015),
                          (0.010, 0.006, 0.010), mat="garnet", segments=6, rings=2,
                          extras={"smooth": True, "bevel": False}))

    # --- Sleeve: oval section (depth/width 0.10/0.12), tapering 0.12 -> 0.086 wide
    # over 0.32 m, flared a little into the plinth.
    k = 0.10 / 0.12
    z0, z1 = 0.058, 0.382
    r0, r1 = 0.060, 0.043

    def r_at(z: float) -> float:
        return r0 + (r1 - r0) * (z - 0.06) / 0.32

    parts.append(Part("lathe", (0, 0, 0), (1.0, k, 1.0), mat="silver", segments=16,
                      extras={"profile": [(0.063, z0), (0.061, 0.068), (r1, z1)], "smooth": True, "bevel": False}))
    # Seven chased drapery folds (4 mm proud ribs) with a niello groove beside each,
    # avoiding the window on the front (-90°) and the gilt edge bands (0°, 180°).
    for theta in (-152, -127, -53, -28, 52, 90, 128):
        rib, groove = [], []
        for z in (0.068, 0.372):
            p, n = _oval_point(r_at(z), k, theta, z, lift=0.0005)
            rib.append(p)
            g, _ = _oval_point(r_at(z), k, theta + 9, z, lift=0.0003)
            groove.append(g)
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="silver", segments=5,
                          extras={"path": rib, "section": (0.0042, 0.0065), "up": n,
                                  "smooth": True, "bevel": False}))
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="niello_soot", segments=3,
                          extras={"path": groove, "section": (0.0012, 0.0016), "up": n,
                                  "bevel": False}))
    # Gilt edge bands down both sides.
    for theta in (0, 180):
        band = [_oval_point(r_at(z), k, theta, z, lift=0.0005)[0] for z in (0.068, 0.378)]
        n = _oval_point(r1, k, theta, 0.3)[1]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="gilt", segments=4,
                          extras={"path": band, "section": (0.0025, 0.0045), "up": n,
                                  "smooth": True, "bevel": False}))

    # --- Window: oval rock crystal 0.05 × 0.09 m, 15 mm proud, in a gilt collet with
    # 12 beads; the wrapped relic bone shows at the crystal's face.
    wz = 0.21
    front = -k * r_at(wz)
    parts.append(Part("torus", (0, front - 0.002, wz), (0.064, 0.104, 0.014), mat="gilt",
                      rot=(90, 0, 0), segments=12, rings=4, minor=0.16,
                      extras={"smooth": True, "bevel": False}))
    cr = (0.025, 0.015, 0.045)                  # crystal semi-axes
    parts.append(Part("sphere", (0, front, wz), (2 * cr[0], 2 * cr[1], 2 * cr[2]),
                      mat="rock_crystal", segments=10, rings=5,
                      extras={"smooth": True, "bevel": False}))
    for i in range(12):
        a = 2 * math.pi * i / 12
        parts.append(Part("sphere", (0.032 * math.cos(a), front - 0.006, wz + 0.052 * math.sin(a)),
                          (0.0075, 0.0075, 0.0075), mat="gilt", segments=4, rings=2,
                          extras={"smooth": True, "bevel": False}))

    def on_crystal(x: float, z: float, lift: float):
        u = 1.0 - (x / cr[0]) ** 2 - ((z - wz) / cr[2]) ** 2
        return (x, front - cr[1] * math.sqrt(max(u, 0.0)) - lift, z)

    bone = [on_crystal(x, wz + dz, 0.0006) for x, dz in
            ((-0.004, 0.034), (0.006, 0.016), (-0.003, -0.004), (0.004, -0.022), (-0.002, -0.034))]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="relic_bone", segments=4,
                      extras={"path": bone, "section": (0.0012, 0.0055), "up": (0, -1, 0),
                              "smooth": True, "bevel": False}))
    silk = [on_crystal(x, wz + dz, 0.0012) for x, dz in ((-0.015, -0.020), (0.0, -0.012), (0.016, -0.004))]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="red_silk", segments=4,
                      extras={"path": silk, "section": (0.0010, 0.0045), "up": (0, -1, 0),
                              "smooth": True, "bevel": False}))

    # --- Cuff: gilt band 0.04 m × 0.11 m, five oval garnets on the front half, a
    # rolled top edge (the beading is below mesh resolution at this budget).
    kc = 0.09 / 0.11
    parts.append(Part("lathe", (0, 0, 0), (1.0, kc, 1.0), mat="gilt", segments=14,
                      extras={"profile": [(0.050, 0.378), (0.055, 0.383), (0.055, 0.412),
                                          (0.0585, 0.4155), (0.054, 0.4195), (0.046, 0.4195)],
                              "smooth": True, "bevel": False}))
    for theta in (-150, -120, -90, -60, -30):
        (x, y, z), (nx, ny, _) = _oval_point(0.055, kc, theta, 0.3975, lift=0.0)
        yaw = math.degrees(math.atan2(ny, nx)) + 90.0
        parts.append(Part("sphere", (x, y, z), (0.011, 0.009, 0.016),
                          mat="garnet", segments=6, rings=3, rot=(0, 0, yaw),
                          extras={"smooth": True, "bevel": False}))

    # --- Hand: palm facing the viewer; ring and little fingers folded on the
    # viewer's left, index and middle raised, thumb out to the viewer's right.
    parts.append(Part("lathe", (0.004, 0, 0), (1.0, 0.07 / 0.084, 1.0), mat="silver", segments=8,
                      extras={"profile": [(0.041, 0.416), (0.044, 0.438), (0.046, 0.461),
                                          (0.040, 0.469), (0.0, 0.471)], "smooth": True,
                              "bevel": False}))
    for x in (-0.028, -0.010):                 # folded ring + little fingers (curled knuckles)
        curl = [(x, 0.006, 0.466), (x, -0.024, 0.473), (x, -0.036, 0.452)]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="silver", segments=6,
                          extras={"path": curl, "section": (0.0085, 0.0085), "up": (1, 0, 0),
                                  "smooth": True, "bevel": False}))
    for x, ring in ((0.004, False), (0.024, True)):  # middle, index (gilt ring on index)
        prof = [(0.0092, 0.455), (0.0098, 0.477), (0.0098, 0.482), (0.0090, 0.496),
                (0.0090, 0.498), (0.0084, 0.509), (0.0055, 0.516), (0.0, H)]
        # Knuckle lines (niello) at the two bulges; on the index the lower one is the
        # gilt ring instead.
        paint = [{"mat": "gilt" if ring else "niello_soot",
                  "min": (-1, -1, 0.4765), "max": (1, 1, 0.4825)},
                 {"mat": "niello_soot", "min": (-1, -1, 0.4955), "max": (1, 1, 0.4985)}]
        parts.append(Part("lathe", (x, -0.004, 0), (1.0, 0.9, 1.0), mat="silver", segments=6,
                          extras={"profile": prof, "paint": paint, "smooth": True,
                                  "bevel": False}))
    thumb = [(0.034, -0.006, 0.428), (0.050, -0.010, 0.444), (0.058, -0.012, 0.462),
             (0.057, -0.012, 0.476)]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="silver", segments=6,
                      extras={"path": thumb, "section": (0.0095, 0.0080), "up": (0, 1, 0),
                              "smooth": True, "bevel": False}))

    return blueprint(
        entry, parts,
        bevel=0.002,
        extra_families={
            # The build bullet wraps the relic bone in red silk; the JSON palette has
            # no silk. Kermes red from the High palette.
            "red_silk": {"name": "Red silk (relic wrap)", "base": "#7E2A26", "rough": 0.7,
                         "metal": 0.0, "grain": 0.2},
        },
        family_overrides={
            # Tarnish in the chasing; gilt rubbed back to the silver under it.
            "silver": {"wear_to": "#5A5850", "wear_amount": 0.25, "grain": 0.16},
            "gilt": {"wear_to": "#B8B4A8", "wear_amount": 0.25, "grain": 0.14},
            "rock_crystal": {"rough": 0.08, "grain": 0.12},
            "garnet": {"grain": 0.1},
        },
        notes=["Rock crystal is opaque in this bake (no refraction mask yet); the relic "
               "bone and its red silk wrap are laid on the crystal's face so they read "
               "through the window.",
               "Felt underside and the vellum ground behind the relic are not modelled "
               "(never seen at game distance / hidden by the opaque crystal)."],
    )


# --------------------------------------------------------------------------------
# Silver ewer
# --------------------------------------------------------------------------------

def silver_ewer(entry: Entry):
    """Hammered silver ewer: pear-shaped lathe body on a gilt-ringed foot, a niello
    band at the belly, the household enamel on the front, a long straight spout on
    +X, a C-scroll handle on -X and a domed lid with a gilt finial."""
    W, D, H = entry.dims                      # 0.22 × 0.16 × 0.34
    parts: list[Part] = []

    band_lo, band_hi = 0.111, 0.129           # niello band, 0.018 m at the belly
    body = [
        (0.050, 0.000), (0.050, 0.006),       # foot 0.10 dia × 0.015
        (0.044, 0.015), (0.034, 0.020),       # foot ring (gilt) 0.02–0.03
        (0.030, 0.031),                       # stem 0.06 dia
        (0.042, 0.050), (0.062, 0.071),
        (0.075, 0.092), (0.080, band_lo),     # belly 0.16 dia at 0.12
        (0.080, band_hi),
        (0.074, 0.150), (0.060, 0.170),       # shoulder 0.12 dia at 0.17
        (0.046, 0.192), (0.036, 0.222),
        (0.035, 0.250),                       # neck 0.07 dia at 0.245
        (0.041, 0.282), (0.046, 0.300),       # flaring to the lip, 0.092 dia at 0.30
    ]
    parts.append(Part("lathe", (0, 0, 0), (1, 1, 1), mat="silver", segments=24, extras={
        "profile": body, "smooth": True, "bevel": False, "paint": [
            {"mat": "tarnish", "min": (-1, -1, -1), "max": (1, 1, 0.003)},
            {"mat": "parcel_gilt", "min": (-1, -1, 0.0195), "max": (1, 1, 0.0315)},
            {"mat": "niello", "min": (-1, -1, band_lo - 1e-4), "max": (1, 1, band_hi + 1e-4)},
            {"mat": "parcel_gilt", "min": (-1, -1, 0.282), "max": (1, 1, 0.301)},
        ]}))

    # Lid: silver dome seated on the gilt lip, gilt finial ball (1.6 cm) on top;
    # hinge knuckle and gilt thumbpiece on the handle side (-X).
    # The 12-sided lid rim is kept wider than the 24-sided lip so the lip's
    # vertices never poke through its flats.
    lid = [(0.0485, 0.2985), (0.0485, 0.304), (0.043, 0.312), (0.034, 0.320),
           (0.020, 0.325), (0.007, 0.3265), (0.0, 0.327)]
    parts.append(Part("lathe", (0, 0, 0), (1, 1, 1), mat="silver", segments=12,
                      extras={"profile": lid, "smooth": True, "bevel": False}))
    parts.append(Part("cyl", (0, 0, 0.3285), (0.008, 0.008, 0.006), mat="parcel_gilt",
                      segments=8, extras={"bevel": False}))
    parts.append(Part("sphere", (0, 0, H - 0.008), (0.016, 0.016, 0.016), mat="parcel_gilt",
                      segments=8, rings=4, extras={"smooth": True, "bevel": False}))
    parts.append(Part("cyl", (-0.048, 0, 0.303), (0.008, 0.008, 0.022), mat="silver",
                      segments=6, rot=(90, 0, 0), extras={"smooth": True, "bevel": False}))
    parts.append(Part("cone", (-0.056, 0, 0.318), (0.014, 0.006, 0.024), mat="parcel_gilt",
                      segments=4, rot=(0, -38, 0), extras={"bevel": False}))

    # Spout: a straight tapered lathe along the spout axis, from a flared root on
    # the shoulder (1.8 cm) to a 0.9 cm tip 0.12 m out and 0.275 m up; gilt sleeve at
    # the root, a dark mouth.
    root, tip = (0.052, 0.0, 0.138), (0.117, 0.0, 0.275)
    dx, dz = tip[0] - root[0], tip[2] - root[2]
    L = math.hypot(dx, dz)
    spout = [(0.023, 0.0), (0.0145, 0.014), (0.0105, 0.026), (0.0098, 0.040),
             (0.0068, 0.10), (0.0045, L - 0.003), (0.0052, L)]
    parts.append(Part("lathe", root, (1, 1, 1), mat="silver", segments=8,
                      rot=(0, math.degrees(math.atan2(dx, dz)), 0), extras={
                          "profile": spout, "smooth": True, "bevel": False, "paint": [
                              {"mat": "parcel_gilt", "min": (-1, -1, 0.0135), "max": (1, 1, 0.041)},
                              {"mat": "tarnish", "min": (-1, -1, L - 1e-4), "max": (1, 1, 1)},
                          ]}))

    # Handle: C-scroll strap 1.1 × 0.8 cm from the neck (0.27 m) out to 0.11 m and
    # down into the belly (0.11 m), with a small outward curl at the foot of the C;
    # gilt boss at the top of the bow. Both ends are buried in the body.
    ctrl = [(-0.030, 0.272), (-0.072, 0.281), (-0.101, 0.250), (-0.104, 0.190),
            (-0.086, 0.136), (-0.071, 0.106)]
    path = [(x, 0.0, z) for x, z in spline(ctrl, 2)]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="silver", segments=6, extras={
        "path": path, "section": (0.0055, 0.0045), "up": (0, 1, 0), "smooth": True,
        "bevel": False}))
    parts.append(Part("sphere", (-0.106, 0.0, 0.244), (0.018, 0.018, 0.018), mat="parcel_gilt",
                      segments=8, rings=4, extras={"smooth": True, "bevel": False}))

    # Medallion: champlevé roundel 5.8 cm on the belly front, tilted to the body's
    # slope — gilt rim, woad field, silver chevron.
    mz, tilt = 0.157, 22.0
    ny, nz = -math.cos(math.radians(tilt)), math.sin(math.radians(tilt))
    cy = -0.0665

    def along(d: float):
        return (0.0, cy + ny * d, mz + nz * d)

    parts.append(Part("cyl", along(0.0), (0.058, 0.058, 0.010), mat="parcel_gilt",
                      segments=16, rot=(90 - tilt, 0, 0), extras={"bevel": False}))
    parts.append(Part("cyl", along(0.0055), (0.047, 0.047, 0.002), mat="woad_enamel",
                      segments=16, rot=(90 - tilt, 0, 0), extras={"bevel": False}))
    chevron = [(-0.017, -0.012), (0.0, 0.012), (0.017, -0.012),
               (0.009, -0.012), (0.0, 0.0), (-0.009, -0.012)]
    parts.append(Part("prism", along(0.007), (1, 1, 0.002), mat="silver",
                      rot=(90 - tilt, 0, 0), extras={"outline": chevron, "bevel": False}))

    return blueprint(
        entry, parts,
        bevel=0.0,
        family_overrides={
            # Tarnish rubbed into the hammered sheet; parcel gilt rubbed to silver.
            # Roughness 0.3 (JSON: 0.2 belly, 0.45 foot; one value per family here),
            # so the hammered sheet reads pale rather than mirror-dark.
            "silver": {"rough": 0.3, "wear_to": "#5A5850", "wear_amount": 0.22, "grain": 0.14},
            "parcel_gilt": {"wear_to": "#B8B4A8", "wear_amount": 0.25, "grain": 0.12},
        },
        notes=["Medallion faces front (-Y) as the concept's hero and front views draw it; "
               "the build bullet's 'under the spout' is the side view's reading.",
               "Chased chevrons in the niello band, the 6 mm hammer marks and the polished "
               "patch on the handle are normal/roughness detail EnemyForge's bake does not "
               "make; the band is solid niello.",
               "Dent blend shapes (states 1 and 2) are not authored by ArtForge yet."],
    )


BLUEPRINTS = {
    "gilded-altarpiece": gilded_altarpiece,
    "arm-reliquary": arm_reliquary,
    "silver-ewer": silver_ewer,
}
