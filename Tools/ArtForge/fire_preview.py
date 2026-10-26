"""Preview where a model's shader fire will burn, for models that bake a FireMask.

The fear pass draws fire with a shader in engine, not with geometry (owner, 2026-10-11), so
the ordinary review sheet shows the model unlit. This renders the built model with its
<Name>_FireMask.png lit as glowing madder, front and three-quarter, so the placement of the
fire can be looked at before any engine work. The glow stands in for the shader; it is not
what the shader will look like.

    python3.11 Tools/ArtForge/fire_preview.py enemies fear wick
        -> docs/art/models/fear/wick-fire.png
"""

from __future__ import annotations

import os
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bpy  # noqa: E402

import render  # noqa: E402  (the review sheet's scene, studio and camera)
from art_forge import REPO_ROOT, spec  # noqa: E402

MADDER = "#C4542E"
GLOW = 7.0   # emission strength where the mask is white


def _light_the_mask(mesh, mask_path: str) -> None:
    mat = mesh.material_slots[0].material
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    principled = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(mask_path)
    tex.image.colorspace_settings.name = "Non-Color"
    uv = next((n for n in nodes if n.type == "UVMAP"), None)
    if uv is not None:
        links.new(uv.outputs["UV"], tex.inputs["Vector"])
    tint = nodes.new("ShaderNodeMix")
    tint.data_type = "RGBA"
    tint.blend_type = "MULTIPLY"
    tint.inputs["Factor"].default_value = 1.0
    links.new(tex.outputs["Color"], tint.inputs["A"])
    tint.inputs["B"].default_value = (*render._linear(MADDER), 1.0)
    links.new(tint.outputs["Result"], principled.inputs["Emission Color"])
    principled.inputs["Emission Strength"].default_value = GLOW


def main() -> int:
    if len(sys.argv) != 4:
        print(__doc__)
        return 2
    kind, age, slug = sys.argv[1:]
    entry = spec.entry(age, kind, slug)
    kind_dir = kind[:1].upper() + kind[1:]
    folder = os.path.join(REPO_ROOT, "Assets", "Models", "ArtBible", kind_dir, age.capitalize(), entry.pascal)
    blend = os.path.join(folder, f"{entry.pascal}.blend")
    mask = os.path.join(folder, "Textures", f"{entry.pascal}_FireMask.png")
    for path in (blend, mask):
        if not os.path.exists(path):
            print(f"missing {path}: build the model first (Tools/ArtForge/build.py)")
            return 1
    from PIL import Image, ImageDraw
    tiles = []
    with tempfile.TemporaryDirectory() as tmp:
        for label, azimuth, elevation in (("FRONT", 0.0, 0.0), ("THREE-QUARTER", -35.0, 16.0)):
            mesh = render._open(blend)
            lo, hi = render._bounds(mesh)
            render._configure(700, 32)
            render._world()
            render._studio((lo + hi) / 2.0, (hi - lo).length * 0.6)   # dimmer: let the fire carry
            render._ground((hi - lo).length)
            render._camera(lo, hi, azimuth, elevation)
            _light_the_mask(mesh, mask)
            path = os.path.join(tmp, f"{label}.png")
            bpy.context.scene.render.filepath = path
            bpy.ops.render.render(write_still=True)
            tiles.append((label, Image.open(path).convert("RGB")))
        w, h = tiles[0][1].size
        sheet = Image.new("RGB", (w * 2 + 30, h + 70), (20, 18, 14))
        draw = ImageDraw.Draw(sheet)
        font = render._font(18)
        for i, (label, img) in enumerate(tiles):
            sheet.paste(img, (10 + i * (w + 10), 10))
            draw.text((20 + i * (w + 10), 18), label, fill=(220, 210, 186), font=font)
        draw.text((10, h + 24), f"{entry.name}: FireMask lit as madder glow, standing in for the engine's fire "
                  f"shader (white = flames, grey = smoulder)", fill=(154, 144, 120), font=render._font(16))
        out = os.path.join(REPO_ROOT, "docs", "art", "models", age, f"{slug}-fire.png")
        os.makedirs(os.path.dirname(out), exist_ok=True)
        sheet.save(out)
    print(f"wrote {os.path.relpath(out, REPO_ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
