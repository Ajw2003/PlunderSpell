"""Authoring materials per asset family, feeding EnemyForge's bake unchanged.

EnemyForge surfaces every enemy from one global palette. The art bible instead
gives each asset its own material list (name + hex), so here a family is built
from a spec dict (see spec.default_family) and registered in
`enemy_forge.materials._CHANNEL_SOCKETS`, which is all `bake_channels` needs to
bake it. The packed maps and the shipping material are EnemyForge's too.

Family spec keys (all but `base` optional):
  base   "#RRGGBB" albedo, as the art bible states it
  rough  0..1 roughness            metal  0..1 metallic
  emit   "#RRGGBB" or None         grain  0..1 how much blotch noise darkens base
  wear_to  "#RRGGBB": colour the surface is rubbed towards in patches
           (gilt rubbed to bole, silver to tarnish); wear_amount 0..1 (default 0.35)
  ridges   (spacing_m, strength): horizontal bands along Z, e.g. wheel ridges on
           a pot, planks on a door. strength 0..1 darkening.
"""

from __future__ import annotations

import bpy

from enemy_forge.materials import _CHANNEL_SOCKETS, _srgb

from . import spec as spec_mod


def _noise(nodes, links, coords, scale: float, detail: float, location):
    node = nodes.new("ShaderNodeTexNoise")
    node.inputs["Scale"].default_value = scale
    node.inputs["Detail"].default_value = detail
    node.location = location
    links.new(coords.outputs["Object"], node.inputs["Vector"])
    return node


def _mix(nodes, links, blend: str, factor, a, b, location):
    """RGBA mix; `factor` is a float or a socket."""
    node = nodes.new("ShaderNodeMix")
    node.data_type = "RGBA"
    node.blend_type = blend
    node.location = location
    if isinstance(factor, (int, float)):
        node.inputs["Factor"].default_value = float(factor)
    else:
        links.new(factor, node.inputs["Factor"])
    links.new(a, node.inputs[6])
    links.new(b, node.inputs[7])
    return node


def build_authoring_material(asset: str, family: str, fam: dict,
                             wear: float = 1.0) -> bpy.types.Material:
    """One family as a standalone material exposing EnemyForge's four bake channels."""
    mat = bpy.data.materials.new(f"{asset}_{family}_Authoring")
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()

    coords = nodes.new("ShaderNodeTexCoord")
    coords.location = (-1300, -200)
    blotch = _noise(nodes, links, coords, 5.5, 6.0, (-1100, -120))
    grain = _noise(nodes, links, coords, 46.0, 3.0, (-1100, -360))

    base_rgb = nodes.new("ShaderNodeRGB")
    base_rgb.outputs[0].default_value = (*_srgb(fam["base"]), 1.0)
    base_rgb.location = (-1100, 250)
    colour = base_rgb.outputs[0]

    if fam.get("wear_to"):
        # Rubbed patches: a thresholded blotch reveals the under-colour.
        under = nodes.new("ShaderNodeRGB")
        under.outputs[0].default_value = (*_srgb(fam["wear_to"]), 1.0)
        under.location = (-1100, 420)
        ramp = nodes.new("ShaderNodeValToRGB")
        ramp.location = (-900, 420)
        amount = max(0.0, min(1.0, fam.get("wear_amount", 0.35) * wear))
        ramp.color_ramp.elements[0].position = 1.0 - amount * 0.55
        ramp.color_ramp.elements[1].position = min(1.0, 1.0 - amount * 0.55 + 0.08)
        detail = _noise(nodes, links, coords, 14.0, 8.0, (-1100, 560))
        links.new(detail.outputs["Fac"], ramp.inputs["Fac"])
        colour = _mix(nodes, links, "MIX", ramp.outputs["Color"], colour,
                      under.outputs[0], (-700, 330)).outputs[2]

    if fam.get("ridges"):
        spacing, strength = fam["ridges"]
        wave = nodes.new("ShaderNodeTexWave")
        wave.wave_type = "BANDS"
        wave.bands_direction = "Z"
        # Cycles' wave is sin(20 * scale * z): one band every 2*pi / (20 * scale).
        wave.inputs["Scale"].default_value = 3.14159265 / (10.0 * max(1e-4, spacing))
        wave.inputs["Distortion"].default_value = 0.4
        wave.location = (-1100, 700)
        links.new(coords.outputs["Object"], wave.inputs["Vector"])
        ridge = nodes.new("ShaderNodeMapRange")
        ridge.inputs["To Min"].default_value = 1.0 - strength
        ridge.inputs["To Max"].default_value = 1.0
        ridge.location = (-900, 700)
        links.new(wave.outputs["Fac"], ridge.inputs["Value"])
        colour = _mix(nodes, links, "MULTIPLY", 1.0, colour,
                      ridge.outputs["Result"], (-700, 600)).outputs[2]

    # Weathering darkens the surface unevenly rather than tinting it.
    shade = _mix(nodes, links, "MULTIPLY", min(1.0, fam.get("grain", 0.25) * wear),
                 colour, blotch.outputs["Fac"], (-500, 250))

    rough_value = nodes.new("ShaderNodeValue")
    rough_value.outputs[0].default_value = fam["rough"]
    rough_value.location = (-1100, 40)
    rough_offset = nodes.new("ShaderNodeMath")
    rough_offset.operation = "MULTIPLY_ADD"
    rough_offset.inputs[1].default_value = 0.16 * fam.get("grain", 0.25) * wear
    rough_offset.location = (-700, 40)
    links.new(grain.outputs["Fac"], rough_offset.inputs[0])
    links.new(rough_value.outputs[0], rough_offset.inputs[2])
    rough_clamped = nodes.new("ShaderNodeClamp")
    rough_clamped.inputs["Min"].default_value = 0.04
    rough_clamped.inputs["Max"].default_value = 1.0
    rough_clamped.location = (-500, 40)
    links.new(rough_offset.outputs["Value"], rough_clamped.inputs["Value"])

    metal_value = nodes.new("ShaderNodeValue")
    metal_value.outputs[0].default_value = fam["metal"]
    metal_value.location = (-1100, -60)

    glow = _srgb(fam["emit"]) if fam.get("emit") else (0.0, 0.0, 0.0)
    emit_rgb = nodes.new("ShaderNodeRGB")
    emit_rgb.outputs[0].default_value = (*glow, 1.0)
    emit_rgb.location = (-1100, -560)
    emit_socket = emit_rgb.outputs[0]
    if fam.get("emit"):
        emit_socket = _mix(nodes, links, "MULTIPLY", 0.35, emit_socket,
                           blotch.outputs["Fac"], (-700, -560)).outputs[2]

    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (-150, 0)
    links.new(shade.outputs[2], principled.inputs["Base Color"])
    links.new(rough_clamped.outputs["Result"], principled.inputs["Roughness"])
    links.new(metal_value.outputs[0], principled.inputs["Metallic"])
    links.new(emit_socket, principled.inputs["Emission Color"])
    principled.inputs["Emission Strength"].default_value = 1.0

    out = nodes.new("ShaderNodeOutputMaterial")
    out.location = (200, 0)
    links.new(principled.outputs["BSDF"], out.inputs["Surface"])

    _CHANNEL_SOCKETS[mat.name] = {
        "BaseMap": shade.outputs[2],
        "Roughness": rough_clamped.outputs["Result"],
        "Metallic": metal_value.outputs[0],
        "Emission": emit_socket,
    }
    return mat


_SINGULAR = {"items": "item", "structures": "structure", "enemies": "enemy"}


def discipline_violations(kind: str, name: str, families: dict[str, dict],
                          gold_reason: str | None = None) -> list[str]:
    """The mood board's pigment rules, applied to the families an asset uses.

    - Orpiment gold is for value only: items may use it freely. A structure or
      enemy may only carry it when the art bible marks that material as stealable
      (its name or notes say plunder / loot / stealable) or the blueprint gives a
      reason in `gold_reason`.
    - Enemies never wear verdigris or lapis (arcane and voice pigments).
    """
    problems = []
    for key, fam in families.items():
        if kind != "items" and spec_mod.is_orpiment_gold(fam):
            text = (fam["name"] + " " + fam.get("notes", "")).lower()
            stealable = any(w in text for w in ("plunder", "loot", "stealable"))
            if not stealable and not gold_reason:
                problems.append(
                    f"{name}: {_SINGULAR.get(kind, kind)} uses orpiment-gold family {key!r} "
                    f"({fam['base']}); gold is for plunder only — mark it stealable "
                    f"in the art bible or set Blueprint.gold_reason")
        if kind == "enemies" and spec_mod.is_reserved_arcane(fam):
            problems.append(
                f"{name}: enemy uses {key!r} ({fam['base']}), which reads as "
                f"verdigris/lapis; the brief forbids both on enemy costume")
    return problems


def build_authoring_set(asset: str, ordered: list[tuple[str, dict]],
                        wear: float) -> list[bpy.types.Material]:
    """Materials in slot order: `ordered[i]` becomes material slot i."""
    return [build_authoring_material(asset, key, fam, wear) for key, fam in ordered]
