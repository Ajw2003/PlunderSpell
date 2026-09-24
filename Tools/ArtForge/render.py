#!/usr/bin/env python3
"""Render art-bible models and compose concept-vs-model review sheets.

    python3 Tools/ArtForge/render.py items
    python3 Tools/ArtForge/render.py items --age high --only gilded-altarpiece
    python3 Tools/ArtForge/render.py items --samples 64 --resolution 900

For each built model: opens its exported .blend, renders three-quarter, front and
side views plus a wireframe (Cycles, CPU) on a dark studio, then writes ONE sheet
docs/art/models/<age>/<slug>.png — the concept sheet on the left, the four renders
on the right, a caption with name, triangles/budget and bbox vs spec underneath.
Build first (build.py); this reads Assets/Models/ArtBible/.
Needs Pillow (`pip install pillow`) for the sheet.
"""

from __future__ import annotations

import argparse
import json
import math
import os
import shutil
import sys
import tempfile
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import art_forge  # noqa: E402  (puts Tools/EnemyForge on sys.path)
import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

from art_forge import AGES, KINDS, REPO_ROOT, blueprints, spec  # noqa: E402

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError as error:  # loud: the sheet is the whole point of this script
    raise SystemExit("render.py needs Pillow for the review sheet: pip install pillow") from error

MODELS_ROOT = os.path.join(REPO_ROOT, "Assets", "Models", "ArtBible")
SHEETS_ROOT = os.path.join(REPO_ROOT, "docs", "art", "models")
BONE_BLACK = "#14120E"

# (label, azimuth°, elevation°). Azimuth 0 looks at the model's front, which faces -Y;
# 90 looks from +X. Three-quarter matches the concept sheets' hero angle.
VIEWS = [
    ("THREE-QUARTER", -35.0, 16.0),
    ("FRONT", 0.0, 0.0),
    ("SIDE", 90.0, 0.0),
    ("WIREFRAME", -35.0, 16.0),
]

FONT_PATHS = ["/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",
              "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"]


def _linear(hex_colour: str) -> tuple[float, float, float]:
    from enemy_forge.materials import _srgb
    return _srgb(hex_colour)


# --------------------------------------------------------------------------------
# Scene
# --------------------------------------------------------------------------------

def _open(blend_path: str) -> bpy.types.Object:
    """Open the built .blend itself, so its relative texture paths resolve."""
    bpy.ops.wm.open_mainfile(filepath=blend_path)
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if len(meshes) != 1:
        raise RuntimeError(f"{blend_path}: expected one mesh, found {[o.name for o in meshes]}")
    missing = [img.filepath for img in bpy.data.images
               if img.source == "FILE" and not os.path.exists(bpy.path.abspath(img.filepath))]
    if missing:
        raise RuntimeError(f"{blend_path}: textures not found: {missing}")
    return meshes[0]


def _bounds(obj) -> tuple[Vector, Vector]:
    points = [obj.matrix_world @ v.co for v in obj.data.vertices]
    lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return lo, hi


def _configure(resolution: int, samples: int) -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_adaptive_sampling = True
    scene.cycles.adaptive_threshold = 0.02
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 4
    scene.cycles.transparent_max_bounces = 2
    scene.render.resolution_x = resolution
    scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.render.image_settings.file_format = "PNG"


def _world() -> None:
    world = bpy.data.worlds.new("ReviewWorld")
    bpy.context.scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes["Background"]
    background.inputs["Color"].default_value = (0.010, 0.008, 0.006, 1.0)
    background.inputs["Strength"].default_value = 1.0


def _area_light(name, location, target, energy, size, color):
    data = bpy.data.lights.new(name, type="AREA")
    data.energy = energy
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    obj.location = location
    direction = (Vector(target) - Vector(location)).normalized()
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.collection.objects.link(obj)
    return obj


def _studio(centre: Vector, span: float) -> None:
    """Warm key from front-right-above, dim warm fill, cool thin rim from behind."""
    d = max(span, 0.25) * 2.2
    energy = d * d
    _area_light("Key", centre + Vector((d * 0.75, -d * 0.95, d * 0.85)), centre,
                energy * 48.0, d * 0.5, (1.0, 0.82, 0.62))
    _area_light("Fill", centre + Vector((-d * 1.1, -d * 0.55, d * 0.25)), centre,
                energy * 10.0, d * 1.0, (1.0, 0.74, 0.52))
    _area_light("Rim", centre + Vector((-d * 0.3, d * 1.2, d * 1.0)), centre,
                energy * 30.0, d * 0.6, (0.70, 0.76, 0.90))


def _ground(span: float) -> None:
    mesh = bpy.data.meshes.new("ReviewGround")
    size = max(span, 0.25) * 40.0
    verts = [(-size, -size, 0.0), (size, -size, 0.0), (size, size, 0.0), (-size, size, 0.0)]
    mesh.from_pydata(verts, [], [(0, 1, 2, 3)])
    obj = bpy.data.objects.new("ReviewGround", mesh)
    bpy.context.scene.collection.objects.link(obj)
    mat = bpy.data.materials.new("ReviewGround")
    mat.use_nodes = True
    principled = mat.node_tree.nodes["Principled BSDF"]
    principled.inputs["Base Color"].default_value = (*_linear(BONE_BLACK), 1.0)
    principled.inputs["Roughness"].default_value = 0.7
    mesh.materials.append(mat)


def _camera(lo: Vector, hi: Vector, azimuth: float, elevation: float) -> None:
    """Orthographic camera orbiting the bbox centre, scaled to fit the projected box."""
    centre = (lo + hi) / 2.0
    span = (hi - lo).length
    yaw, pitch = math.radians(azimuth), math.radians(elevation)
    forward = -Vector((math.sin(yaw) * math.cos(pitch),
                       -math.cos(yaw) * math.cos(pitch),
                       math.sin(pitch)))
    right = forward.cross(Vector((0, 0, 1))).normalized()
    up = right.cross(forward).normalized()

    corners = [Vector((x, y, z)) for x in (lo.x, hi.x) for y in (lo.y, hi.y) for z in (lo.z, hi.z)]
    us = [(c - centre).dot(right) for c in corners]
    vs = [(c - centre).dot(up) for c in corners]
    # Re-centre on the projected box so wide or tall models are framed tightly.
    shift = right * ((max(us) + min(us)) / 2.0) + up * ((max(vs) + min(vs)) / 2.0)
    target = centre + shift

    data = bpy.data.cameras.new("ReviewCamera")
    data.type = "ORTHO"
    data.ortho_scale = max(max(us) - min(us), max(vs) - min(vs)) * 1.14
    data.clip_start = 0.01
    data.clip_end = span * 20.0 + 10.0
    camera = bpy.data.objects.new("ReviewCamera", data)
    bpy.context.scene.collection.objects.link(camera)
    camera.location = target - forward * (span * 4.0 + 1.0)
    camera.rotation_euler = forward.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = camera


def _wire_material() -> bpy.types.Material:
    """Clay with a Wireframe-node edge overlay (Freestyle aborts headless here)."""
    mat = bpy.data.materials.new("ReviewWire")
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    clay = nodes.new("ShaderNodeBsdfPrincipled")
    clay.inputs["Base Color"].default_value = (0.10, 0.095, 0.085, 1.0)
    clay.inputs["Roughness"].default_value = 0.75
    edge = nodes.new("ShaderNodeEmission")
    edge.inputs["Color"].default_value = (*_linear("#DCD2BA"), 1.0)
    edge.inputs["Strength"].default_value = 1.4
    wire = nodes.new("ShaderNodeWireframe")
    wire.use_pixel_size = True
    wire.inputs["Size"].default_value = 0.8
    mix = nodes.new("ShaderNodeMixShader")
    links.new(wire.outputs["Fac"], mix.inputs["Fac"])
    links.new(clay.outputs["BSDF"], mix.inputs[1])
    links.new(edge.outputs["Emission"], mix.inputs[2])
    out = nodes.new("ShaderNodeOutputMaterial")
    links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def render_views(blend_path: str, out_dir: str, resolution: int, samples: int) -> dict:
    tiles = {}
    stats = {}
    for label, azimuth, elevation in VIEWS:
        mesh = _open(blend_path)
        lo, hi = _bounds(mesh)
        if not stats:
            tris = sum(len(p.vertices) - 2 for p in mesh.data.polygons)
            stats = {"triangles": tris, "bbox": tuple(round(hi[i] - lo[i], 3) for i in range(3))}
        _configure(resolution, samples)
        _world()
        _studio((lo + hi) / 2.0, (hi - lo).length)
        _ground((hi - lo).length)
        _camera(lo, hi, azimuth, elevation)
        if label == "WIREFRAME":
            wire = _wire_material()
            for slot in mesh.material_slots:
                slot.material = wire
        path = os.path.join(out_dir, f"{label.lower()}.png")
        bpy.context.scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        if not os.path.exists(path):
            raise RuntimeError(f"render did not write {path}")
        tiles[label] = path
    return {"tiles": tiles, **stats}


# --------------------------------------------------------------------------------
# Sheet
# --------------------------------------------------------------------------------

def _font(size: int):
    for path in FONT_PATHS:
        if os.path.exists(path):
            return ImageFont.truetype(path, size)
    print("        note: no DejaVu font found; captions use Pillow's default bitmap font")
    return ImageFont.load_default()


def compose_sheet(entry: spec.Entry, rendered: dict, manifest_entry: dict | None,
                  out_path: str, tile: int) -> str:
    gap = max(6, tile // 80)
    grid = tile * 2 + gap
    caption_h = max(56, tile // 9)

    concept = Image.open(entry.concept_png).convert("RGB")
    scale = grid / concept.height
    concept = concept.resize((int(concept.width * scale), grid), Image.LANCZOS)

    width = gap + concept.width + gap + grid + gap
    height = gap + grid + gap + caption_h
    sheet = Image.new("RGB", (width, height), BONE_BLACK)
    sheet.paste(concept, (gap, gap))

    draw = ImageDraw.Draw(sheet)
    label_font = _font(max(12, tile // 34))
    left = gap + concept.width + gap
    for index, (label, _az, _el) in enumerate(VIEWS):
        row, column = divmod(index, 2)
        x, y = left + column * (tile + gap), gap + row * (tile + gap)
        sheet.paste(Image.open(rendered["tiles"][label]).convert("RGB"), (x, y))
        draw.text((x + 12, y + 10), label, fill="#9A9078", font=label_font)

    tris = rendered["triangles"]
    budget = entry.tri_budget
    bx, by, bz = rendered["bbox"]
    if entry.dims:
        w, d, h = entry.dims
        size_text = (f"bbox W×D×H {bx:.3f} × {by:.3f} × {bz:.3f} m  vs spec "
                     f"{w:.2f} × {d:.2f} × {h:.2f} m")
    else:
        size_text = (f"bbox {bx:.2f} × {by:.2f} × {bz:.2f} m  vs spec height "
                     f"{entry.height_m} m")
    status = ""
    if manifest_entry is not None:
        status = "  ·  validation PASS" if manifest_entry.get("passed") else "  ·  validation FAIL"
        overrides = [w for w in manifest_entry.get("warnings", []) if "instead of the JSON" in w]
        if overrides:
            size_text += "  (" + "; ".join(o.split(":")[0] for o in overrides) + ")"
    caption = (f"{entry.name}  ·  {entry.age}/{entry.kind}/{entry.slug}  ·  "
               f"{tris} / {budget} tris{status}")
    cap_font = _font(max(14, caption_h // 3))
    draw.text((gap + 6, gap + grid + gap + 4), caption, fill="#DCD2BA", font=cap_font)
    draw.text((gap + 6, gap + grid + gap + 4 + caption_h // 2 - 2), size_text,
              fill="#9A9078", font=_font(max(12, caption_h // 4)))
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    sheet.save(out_path, optimize=True)
    return out_path


def _manifest() -> dict[str, dict]:
    path = os.path.join(MODELS_ROOT, "artforge_manifest.json")
    if not os.path.exists(path):
        print(f"        note: no manifest at {os.path.relpath(path, REPO_ROOT)}; "
              f"captions omit validation status")
        return {}
    with open(path, encoding="utf-8") as handle:
        return {e["key"]: e for e in json.load(handle).get("assets", [])}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("kind", choices=KINDS)
    parser.add_argument("--age", choices=AGES, action="append")
    parser.add_argument("--only", nargs="*", default=None)
    parser.add_argument("--samples", type=int, default=32)
    parser.add_argument("--resolution", type=int, default=700, help="pixels per view")
    args = parser.parse_args()

    ages = args.age or list(AGES)
    chosen = blueprints.available(args.kind, ages)
    if args.only:
        missing = sorted(set(args.only) - {s for _a, s in chosen})
        if missing:
            raise SystemExit(f"no {args.kind} blueprint for: {', '.join(missing)}")
        chosen = [(a, s) for a, s in chosen if s in args.only]
    if not chosen:
        print(f"No {args.kind} blueprints for {', '.join(ages)}; nothing to render.")
        return 1

    manifest = _manifest()
    failures = []
    started = time.time()
    for age, slug in chosen:
        entry = spec.entry(age, args.kind, slug)
        blend = os.path.join(MODELS_ROOT, args.kind.capitalize(), age.capitalize(),
                             entry.pascal, f"{entry.pascal}.blend")
        print(f"\n=== {args.kind}/{age}/{slug} ===")
        if not os.path.exists(blend):
            print(f"        MISSING {os.path.relpath(blend, REPO_ROOT)} — run build.py first")
            failures.append(slug)
            continue
        step = time.time()
        scratch = tempfile.mkdtemp(prefix=f"artforge_{slug}_")
        try:
            rendered = render_views(blend, scratch, args.resolution, args.samples)
            out = compose_sheet(entry, rendered, manifest.get(f"{args.kind}/{age}/{slug}"),
                                os.path.join(SHEETS_ROOT, age, f"{slug}.png"), args.resolution)
        finally:
            shutil.rmtree(scratch, ignore_errors=True)
        print(f"        {rendered['triangles']} tris, bbox {rendered['bbox']} -> "
              f"{os.path.relpath(out, REPO_ROOT)} in {time.time() - step:.1f}s")

    print(f"\nRendered {len(chosen) - len(failures)} sheet(s) in {time.time() - started:.1f}s")
    if failures:
        print(f"NOT RENDERED: {', '.join(failures)}")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
