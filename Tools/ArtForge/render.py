#!/usr/bin/env python3
"""Render art-bible models and compose concept-vs-model review sheets.

    python3 Tools/ArtForge/render.py items
    python3 Tools/ArtForge/render.py items --age high --only gilded-altarpiece
    python3 Tools/ArtForge/render.py items --samples 64 --resolution 900

For each built model: opens its exported .blend, renders three-quarter, front and
side views plus a wireframe (Cycles, CPU) on a dark studio, then writes ONE sheet
docs/art/models/<age>/<slug>.png — the concept sheet on the left, the renders on
the right, a caption with name, triangles/budget and bbox vs spec underneath.
Enemies get six views: three-quarter, front (beside a faint 1.80 m reference
human), side, two POSED views (the rig's review pose, to show the skin deforming)
and the wireframe.
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
# Enemies: three columns. POSED views apply the blueprint's review pose (stored on
# the rig as `artforge_pose`) to prove the skinning deforms without tearing. FRONT
# carries a faint 1.80 m reference human beside the model.
ENEMY_VIEWS = [
    ("THREE-QUARTER", -35.0, 16.0),
    ("FRONT", 0.0, 0.0),
    ("SIDE", 90.0, 0.0),
    ("POSED", -35.0, 16.0),
    ("POSED FRONT", 20.0, 6.0),
    ("WIREFRAME", -35.0, 16.0),
]
REFERENCE_HEIGHT = 1.80


def views_for(kind: str) -> list[tuple[str, float, float]]:
    return ENEMY_VIEWS if kind == "enemies" else VIEWS

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
    # Opening a file runs its scene; a rig left posed would skew every view.
    for rig in [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]:
        for pose_bone in rig.pose.bones:
            pose_bone.matrix_basis.identity()
    bpy.context.view_layer.update()
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if len(meshes) != 1:
        raise RuntimeError(f"{blend_path}: expected one mesh, found {[o.name for o in meshes]}")
    missing = [img.filepath for img in bpy.data.images
               if img.source == "FILE" and not os.path.exists(bpy.path.abspath(img.filepath))]
    if missing:
        raise RuntimeError(f"{blend_path}: textures not found: {missing}")
    return meshes[0]


def _bounds(obj) -> tuple[Vector, Vector]:
    """World bounds of the mesh as it is drawn (armature deformation included)."""
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = obj.evaluated_get(depsgraph)
    points = [evaluated.matrix_world @ v.co for v in evaluated.data.vertices]
    lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return lo, hi


def _configure(resolution: int, samples: int) -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_adaptive_sampling = True
    # Review renders, not beauty shots: a dark studio with a denoiser needs few
    # bounces and a loose noise threshold. Each of these roughly halves a view.
    scene.cycles.adaptive_threshold = 0.04
    scene.cycles.use_denoising = True
    scene.cycles.denoiser = "OPENIMAGEDENOISE"
    scene.cycles.max_bounces = 3
    scene.cycles.diffuse_bounces = 1
    scene.cycles.glossy_bounces = 2
    scene.cycles.transmission_bounces = 1
    scene.cycles.transparent_max_bounces = 2
    scene.cycles.use_auto_tile = False
    # Three area lights and a world: the light tree only adds traversal cost here
    # (measured: 15.7 s -> 10.2 s per 700 px view at 32 samples on 4 cores).
    scene.cycles.use_light_tree = False
    scene.render.resolution_x = resolution
    scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Base Contrast"
    scene.render.image_settings.file_format = "PNG"


def _world() -> None:
    """Near-black to the camera, a warm dim room to everything else.

    Metals are mostly reflection; in a pure-black world orpiment gilt renders as
    brown lacquer. So camera rays see bone-black, but glossy/diffuse rays see a warm
    environment — the same trick as a studio HDRI behind a black backdrop.
    """
    world = bpy.data.worlds.new("ReviewWorld")
    bpy.context.scene.world = world
    world.use_nodes = True
    nodes, links = world.node_tree.nodes, world.node_tree.links
    nodes.clear()
    path = nodes.new("ShaderNodeLightPath")
    seen = nodes.new("ShaderNodeBackground")
    seen.inputs["Color"].default_value = (0.006, 0.005, 0.004, 1.0)
    room = nodes.new("ShaderNodeBackground")
    room.inputs["Color"].default_value = (0.30, 0.22, 0.14, 1.0)
    room.inputs["Strength"].default_value = 0.9
    mix = nodes.new("ShaderNodeMixShader")
    out = nodes.new("ShaderNodeOutputWorld")
    links.new(path.outputs["Is Camera Ray"], mix.inputs["Fac"])
    links.new(room.outputs["Background"], mix.inputs[1])
    links.new(seen.outputs["Background"], mix.inputs[2])
    links.new(mix.outputs["Shader"], out.inputs["Surface"])


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
                energy * 26.0, d * 0.6, (1.0, 0.84, 0.66))
    _area_light("Fill", centre + Vector((-d * 1.1, -d * 0.55, d * 0.25)), centre,
                energy * 5.0, d * 1.0, (1.0, 0.74, 0.52))
    _area_light("Rim", centre + Vector((-d * 0.3, d * 1.2, d * 1.0)), centre,
                energy * 22.0, d * 0.6, (0.70, 0.76, 0.90))


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


def _rig_of(mesh):
    return next((m.object for m in mesh.modifiers if m.type == "ARMATURE" and m.object), None)


def _pose(mesh) -> list[str]:
    """Apply the review pose stored on the mesh's rig. Loud if there is none."""
    from art_forge import rig as rigmod
    rig = _rig_of(mesh)
    if rig is None:
        raise RuntimeError(f"{mesh.name}: POSED view needs a rig, and the mesh has none")
    posed = rigmod.apply_pose(rig)
    if not posed:
        raise RuntimeError(f"{mesh.name}: the rig carries no artforge_pose; the POSED "
                           f"view would just repeat the rest pose")
    return posed


def _reference_human(beside_x: float) -> tuple[Vector, Vector]:
    """A faint 1.80 m figure (figures.Human, the same body enemies are built on)
    standing to the model's right in a front view (-X). Returns its bounds."""
    import bmesh
    from art_forge import figures, kit

    fig = figures.Human(height=REFERENCE_HEIGHT / 1.0)
    parts = [p for p in fig.body("ref", "ref") if p.kind != "sphere"]
    bm, _bones = kit.build_bmesh(parts, {"ref": 0})
    mesh = bpy.data.meshes.new("ReferenceHuman")
    bm.to_mesh(mesh)
    bm.free()
    for poly in mesh.polygons:
        poly.use_smooth = True
    obj = bpy.data.objects.new("ReferenceHuman", mesh)
    bpy.context.scene.collection.objects.link(obj)
    xs = [v.co.x for v in mesh.vertices]
    obj.location.x = beside_x - max(xs) - 0.12
    mat = bpy.data.materials.new("ReferenceHuman")
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    glow = nodes.new("ShaderNodeEmission")
    glow.inputs["Color"].default_value = (*_linear("#DCD2BA"), 1.0)
    glow.inputs["Strength"].default_value = 0.06
    clear = nodes.new("ShaderNodeBsdfTransparent")
    mix = nodes.new("ShaderNodeMixShader")
    mix.inputs["Fac"].default_value = 0.35
    links.new(clear.outputs["BSDF"], mix.inputs[1])
    links.new(glow.outputs["Emission"], mix.inputs[2])
    out = nodes.new("ShaderNodeOutputMaterial")
    links.new(mix.outputs["Shader"], out.inputs["Surface"])
    mesh.materials.append(mat)
    obj.visible_shadow = False
    lo = Vector((obj.location.x + min(xs), min(v.co.y for v in mesh.vertices), 0.0))
    hi = Vector((obj.location.x + max(xs), max(v.co.y for v in mesh.vertices),
                 max(v.co.z for v in mesh.vertices)))
    return lo, hi


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


def render_views(blend_path: str, out_dir: str, resolution: int, samples: int,
                 kind: str = "items") -> dict:
    tiles = {}
    stats = {}
    for label, azimuth, elevation in views_for(kind):
        mesh = _open(blend_path)
        lo, hi = _bounds(mesh)
        if not stats:
            tris = sum(len(p.vertices) - 2 for p in mesh.data.polygons)
            stats = {"triangles": tris, "bbox": tuple(round(hi[i] - lo[i], 3) for i in range(3))}
        if label.startswith("POSED"):
            posed = _pose(mesh)
            stats["posed_bones"] = posed
            plo, phi = _bounds(mesh)
            lo = Vector([min(a, b) for a, b in zip(lo, plo)])
            hi = Vector([max(a, b) for a, b in zip(hi, phi)])
        _configure(resolution, samples)
        _world()
        _studio((lo + hi) / 2.0, (hi - lo).length)
        _ground((hi - lo).length)
        if kind == "enemies" and label == "FRONT":
            rlo, rhi = _reference_human(lo.x)
            lo = Vector([min(a, b) for a, b in zip(lo, rlo)])
            hi = Vector([max(a, b) for a, b in zip(hi, rhi)])
        _camera(lo, hi, azimuth, elevation)
        if label == "WIREFRAME":
            # Emissive edges over flat clay converge in far fewer samples.
            bpy.context.scene.cycles.samples = max(8, samples // 2)
            wire = _wire_material()
            for slot in mesh.material_slots:
                slot.material = wire
        path = os.path.join(out_dir, f"{label.lower().replace(' ', '_')}.png")
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
    views = views_for(entry.kind)
    columns = 3 if len(views) > 4 else 2
    gap = max(6, tile // 80)
    grid = tile * 2 + gap
    grid_w = tile * columns + gap * (columns - 1)
    caption_h = max(56, tile // 9)

    concept = Image.open(entry.concept_png).convert("RGB")
    scale = grid / concept.height
    concept = concept.resize((int(concept.width * scale), grid), Image.LANCZOS)

    width = gap + concept.width + gap + grid_w + gap
    height = gap + grid + gap + caption_h
    sheet = Image.new("RGB", (width, height), BONE_BLACK)
    sheet.paste(concept, (gap, gap))

    draw = ImageDraw.Draw(sheet)
    label_font = _font(max(12, tile // 34))
    left = gap + concept.width + gap
    for index, (label, _az, _el) in enumerate(views):
        row, column = divmod(index, columns)
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
        stats = (manifest_entry or {}).get("stats", {})
        if "height_body_m" in stats:
            size_text = (f"body height {stats['height_body_m']:.3f} m vs spec "
                         f"{entry.height_m} m (±5 %)  ·  with props {bz:.2f} m  ·  "
                         f"bbox {bx:.2f} × {by:.2f} m  ·  {stats.get('rig_bones')} bones "
                         f"({stats.get('skinned_bones')} skinned)  ·  posed: "
                         f"{', '.join(rendered.get('posed_bones', []))}")
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
            rendered = render_views(blend, scratch, args.resolution, args.samples,
                                    args.kind)
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
