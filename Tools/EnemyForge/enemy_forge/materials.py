"""Procedural surfacing for Plunderspell constructs, plus the bake to game textures.

Each surface family is its own authoring material occupying one material slot, so
family boundaries land exactly where the geometry puts them. Those materials are
never exported: every channel is baked into one texture set shared by all five
slots, and a single plain Principled material is rebuilt from the bakes. What the
review renders show is therefore what Unity will load.

Slot index must match the family index in `parts.MATERIALS`, because that is the
number `build_bmesh` writes to each face.
"""

from __future__ import annotations

import os

import bpy

from .parts import MATERIALS

def _srgb(hex_colour: str) -> tuple[float, float, float]:
    """Convert a moodboard hex to linear RGB, which is what the shader wants.

    Colours are stored as the hex the moodboard states so the two can be diffed
    by eye; the sRGB-to-linear transfer happens here rather than in the table.
    """
    hex_colour = hex_colour.lstrip("#")
    out = []
    for i in (0, 2, 4):
        c = int(hex_colour[i:i + 2], 16) / 255.0
        out.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return tuple(out)


# Pigments, straight from docs/plunderspell-moodboard.html. The three reserved ones
# are named here so the discipline check below can refer to them by meaning.
BONE_BLACK, ASH = "#14120E", "#1E1A14"
VELLUM, VELLUM_DIM, VELLUM_FAINT = "#DCD2BA", "#9A9078", "#635C4C"
VERDIGRIS = "#5FA288"   # arcane and interactive only
ORPIMENT = "#C9A227"    # money only — never worn by an enemy
MADDER = "#C4542E"      # fire, alarm, blood
LAPIS = "#7A6AA0"       # the voice, and whatever answers it

# Surface families for the castle household. Deliberately drab: the moodboard
# reserves orpiment for money, so nothing an enemy wears may be gold. Colour on a
# guard therefore means state, not decoration.
PALETTE = {
    "wool":    dict(base="#3C3124", rough=0.94, metal=0.0, emit=None,  grain=0.34),
    "leather": dict(base="#2C2118", rough=0.70, metal=0.0, emit=None,  grain=0.26),
    "oak":     dict(base="#5A4630", rough=0.80, metal=0.0, emit=None,  grain=0.30),
    "mail":    dict(base="#2F3337", rough=0.74, metal=0.86, emit=None, grain=0.30),
    "steel":   dict(base="#8C939A", rough=0.46, metal=0.86, emit=None, grain=0.18),
    "linen":   dict(base=VELLUM_DIM, rough=0.90, metal=0.0, emit=None, grain=0.22),
    "flesh":   dict(base="#8A6A4E", rough=0.68, metal=0.0, emit=None,  grain=0.12),
    "bone":    dict(base="#B9B2A0", rough=0.74, metal=0.0, emit=None,  grain=0.20),
    "madder":  dict(base="#5E2617", rough=0.90, metal=0.0, emit=None,  grain=0.30),
    # The lantern flame: the one warm note on a calm household, and the colour that
    # changes when it wakes.
    "tallow":  dict(base="#2A2012", rough=0.50, metal=0.0, emit="#FFC46A", grain=0.08),
    # Reserved for the crypt. A word raised it, so it carries the voice's colour.
    "lapis":   dict(base="#181428", rough=0.55, metal=0.0, emit="#9C8AD6", grain=0.10),
}

# Families no living member of the household may use, and why. Checked per archetype
# in discipline_violations(); see "Colour on an enemy means state" in
# docs/4-systems/enemy-asset-pipeline.md.
ARCANE_ONLY = {"lapis"}

# The baked emission map is 8-bit and clamps at 1.0, so the glow is scaled back up
# in the shipping material. Unity needs the same multiplier as an HDR value.
EMISSION_STRENGTH = 9.0

CHANNELS = ("BaseMap", "Roughness", "Metallic", "Emission")
# Roughness and metallic hold raw numbers, not colour; sRGB encoding would skew them.
DATA_CHANNELS = {"Roughness", "Metallic"}

# Baking needs the raw channel sockets, which cannot be stored on an ID datablock.
_CHANNEL_SOCKETS: dict[str, dict] = {}


if set(PALETTE) != set(MATERIALS):
    raise RuntimeError(
        "materials.PALETTE and parts.MATERIALS disagree: "
        f"{sorted(set(PALETTE) ^ set(MATERIALS))}")


def _noise(nodes, links, coords, scale: float, detail: float, location):
    node = nodes.new("ShaderNodeTexNoise")
    node.inputs["Scale"].default_value = scale
    node.inputs["Detail"].default_value = detail
    node.location = location
    links.new(coords.outputs["Object"], node.inputs["Vector"])
    return node


def build_authoring_material(family: str, name: str, wear: float) -> bpy.types.Material:
    """One surface family as a standalone material exposing four bakeable channels."""
    spec = PALETTE[family]
    mat = bpy.data.materials.new(f"{name}_{family}_Authoring")
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()

    coords = nodes.new("ShaderNodeTexCoord")
    coords.location = (-1100, -200)
    blotch = _noise(nodes, links, coords, 5.5, 6.0, (-900, -120))
    grain = _noise(nodes, links, coords, 46.0, 3.0, (-900, -360))

    base_rgb = nodes.new("ShaderNodeRGB")
    base_rgb.outputs[0].default_value = (*_srgb(spec["base"]), 1.0)
    base_rgb.location = (-900, 250)

    # Weathering darkens the surface unevenly rather than tinting it.
    shade = nodes.new("ShaderNodeMix")
    shade.data_type = "RGBA"
    shade.blend_type = "MULTIPLY"
    shade.inputs["Factor"].default_value = min(1.0, spec["grain"] * wear)
    shade.location = (-650, 250)
    links.new(base_rgb.outputs[0], shade.inputs[6])
    links.new(blotch.outputs["Fac"], shade.inputs[7])

    rough_value = nodes.new("ShaderNodeValue")
    rough_value.outputs[0].default_value = spec["rough"]
    rough_value.location = (-900, 40)

    rough_offset = nodes.new("ShaderNodeMath")
    rough_offset.operation = "MULTIPLY_ADD"
    rough_offset.inputs[1].default_value = 0.16 * spec["grain"] * wear
    rough_offset.location = (-650, 40)
    links.new(grain.outputs["Fac"], rough_offset.inputs[0])
    links.new(rough_value.outputs[0], rough_offset.inputs[2])

    rough_clamped = nodes.new("ShaderNodeClamp")
    rough_clamped.inputs["Min"].default_value = 0.04
    rough_clamped.inputs["Max"].default_value = 1.0
    rough_clamped.location = (-430, 40)
    links.new(rough_offset.outputs["Value"], rough_clamped.inputs["Value"])

    metal_value = nodes.new("ShaderNodeValue")
    metal_value.outputs[0].default_value = spec["metal"]
    metal_value.location = (-900, -60)

    glow = _srgb(spec["emit"]) if spec["emit"] else (0.0, 0.0, 0.0)
    emit_rgb = nodes.new("ShaderNodeRGB")
    emit_rgb.outputs[0].default_value = (*glow, 1.0)
    emit_rgb.location = (-900, -560)

    if spec["emit"]:
        # Let the glow vary spatially so a large emissive panel is not a flat slab.
        pulse = nodes.new("ShaderNodeMix")
        pulse.data_type = "RGBA"
        pulse.blend_type = "MULTIPLY"
        pulse.inputs["Factor"].default_value = 0.35
        pulse.location = (-650, -560)
        links.new(emit_rgb.outputs[0], pulse.inputs[6])
        links.new(blotch.outputs["Fac"], pulse.inputs[7])
        emit_socket = pulse.outputs[2]
    else:
        emit_socket = emit_rgb.outputs[0]

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


def discipline_violations(name: str, families: set[str], arcane: bool) -> list[str]:
    """Check an archetype against the moodboard's colour rules.

    Orpiment is structurally impossible — there is no gold family to select — so the
    live rule is that only an arcane archetype may carry the voice's colour.
    """
    problems = []
    if not arcane:
        for family in sorted(families & ARCANE_ONLY):
            problems.append(
                f"{name} is not arcane but uses the {family!r} family; the moodboard "
                f"reserves lapis for the voice and whatever answers it")
    unknown = sorted(families - set(PALETTE))
    if unknown:
        problems.append(f"{name} uses families that are not in the palette: {unknown}")
    return problems


def build_authoring_set(name: str, wear: float) -> list[bpy.types.Material]:
    """All five families, ordered so slot index matches the family index in MATERIALS."""
    ordered = sorted(MATERIALS.items(), key=lambda kv: kv[1])
    return [build_authoring_material(family, name, wear) for family, _index in ordered]


def bake_channels(obj, mats: list[bpy.types.Material], name: str,
                  out_dir: str, resolution: int) -> dict[str, str]:
    """Bake each channel as pure emission, so no lighting leaks into the textures.

    Every slot bakes into the same image, so all five materials are rewired and
    given the same active image node before each pass.
    """
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 1
    scene.cycles.use_denoising = False
    scene.render.bake.use_clear = True
    scene.render.bake.margin = 12
    scene.render.bake.use_selected_to_active = False

    rigs = []
    for mat in mats:
        nodes, links = mat.node_tree.nodes, mat.node_tree.links
        emit = nodes.new("ShaderNodeEmission")
        emit.inputs["Strength"].default_value = 1.0
        out_node = next(n for n in nodes if n.type == "OUTPUT_MATERIAL")
        original = out_node.inputs["Surface"].links[0].from_socket
        target = nodes.new("ShaderNodeTexImage")
        nodes.active = target
        rigs.append((mat, nodes, links, emit, out_node, original, target))

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    written: dict[str, str] = {}
    for channel in CHANNELS:
        image = bpy.data.images.new(
            f"{name}_{channel}", width=resolution, height=resolution,
            alpha=False, float_buffer=False,
        )
        image.colorspace_settings.name = (
            "Non-Color" if channel in DATA_CHANNELS else "sRGB"
        )
        for mat, nodes, links, emit, out_node, _original, target in rigs:
            target.image = image
            nodes.active = target
            links.new(_CHANNEL_SOCKETS[mat.name][channel], emit.inputs["Color"])
            links.new(emit.outputs["Emission"], out_node.inputs["Surface"])

        bpy.ops.object.bake(type="EMIT")

        path = os.path.join(out_dir, f"{name}_{channel}.png")
        image.filepath_raw = path
        image.file_format = "PNG"
        image.save()
        written[channel] = path

    for _mat, nodes, links, emit, out_node, original, target in rigs:
        links.new(original, out_node.inputs["Surface"])
        nodes.remove(emit)
        nodes.remove(target)
    return written


def pack_channel_maps(name: str, baked: dict[str, str], out_dir: str) -> dict[str, str]:
    """Combine the raw bakes into the packed maps Unity URP and glTF each expect."""
    import numpy as np

    def pixels(path):
        image = bpy.data.images.load(path, check_existing=False)
        image.colorspace_settings.name = "Non-Color"
        buffer = np.empty(len(image.pixels), dtype=np.float32)
        image.pixels.foreach_get(buffer)
        return buffer.reshape(image.size[1], image.size[0], 4), tuple(image.size)

    rough, size = pixels(baked["Roughness"])
    metal, _ = pixels(baked["Metallic"])

    def write(path, rgba):
        image = bpy.data.images.new(
            os.path.basename(path), width=size[0], height=size[1],
            alpha=True, float_buffer=False,
        )
        image.colorspace_settings.name = "Non-Color"
        image.pixels.foreach_set(rgba.ravel())
        image.filepath_raw = path
        image.file_format = "PNG"
        image.save()
        return path

    # Unity URP Lit: _MetallicGlossMap is metallic in RGB, smoothness in alpha.
    unity = np.ones_like(rough)
    unity[..., 0:3] = metal[..., 0:1]
    unity[..., 3] = 1.0 - rough[..., 0]

    # glTF metallic-roughness: occlusion R, roughness G, metallic B.
    orm = np.ones_like(rough)
    orm[..., 0] = 1.0
    orm[..., 1] = rough[..., 0]
    orm[..., 2] = metal[..., 0]

    return {
        "MetallicGloss": write(os.path.join(out_dir, f"{name}_MetallicGloss.png"), unity),
        "ORM": write(os.path.join(out_dir, f"{name}_ORM.png"), orm),
    }


def build_baked_material(name: str, baked: dict[str, str], packed: dict[str, str]):
    """The material that actually ships: plain Principled driven by the baked maps."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()

    def texture(path, colorspace, location):
        node = nodes.new("ShaderNodeTexImage")
        node.image = bpy.data.images.load(path, check_existing=True)
        node.image.colorspace_settings.name = colorspace
        node.location = location
        return node

    albedo = texture(baked["BaseMap"], "sRGB", (-700, 300))
    orm = texture(packed["ORM"], "Non-Color", (-700, 0))
    emissive = texture(baked["Emission"], "sRGB", (-700, -300))

    split = nodes.new("ShaderNodeSeparateColor")
    split.location = (-420, 0)
    links.new(orm.outputs["Color"], split.inputs["Color"])

    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (-120, 0)
    links.new(albedo.outputs["Color"], principled.inputs["Base Color"])
    links.new(split.outputs["Green"], principled.inputs["Roughness"])
    links.new(split.outputs["Blue"], principled.inputs["Metallic"])
    links.new(emissive.outputs["Color"], principled.inputs["Emission Color"])
    # The baked map is 8-bit and clamps at 1.0, so the glow is scaled back up here.
    # Unity: give the URP Lit emission colour the same HDR multiplier.
    principled.inputs["Emission Strength"].default_value = EMISSION_STRENGTH

    out = nodes.new("ShaderNodeOutputMaterial")
    out.location = (200, 0)
    links.new(principled.outputs["BSDF"], out.inputs["Surface"])
    return mat
