"""High Medieval plunder (docs/art/data/high.json, items)."""

from __future__ import annotations

import math

from ..kit import Part, arc_path, gable_outline, rounded_rect
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


BLUEPRINTS = {
    "gilded-altarpiece": gilded_altarpiece,
}
