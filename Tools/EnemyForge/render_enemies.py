#!/usr/bin/env python3
"""Render review sheets for every built enemy.

Loads each exported .blend, lights it on a studio set, and renders Isometric,
Front, Side, Three-quarter and Wireframe passes plus a contact sheet per enemy
and one roster line-up.

Runs either against the Blender Python module or a real Blender install:

    python3 Tools/EnemyForge/render_enemies.py --device CPU
    blender --background --python Tools/EnemyForge/render_enemies.py -- --device OPTIX

Pass --device to pick the Cycles backend. It never falls back silently: asking for
a GPU backend that is not present is an error, because a silent drop to CPU looks
exactly like a slow render.
"""

from __future__ import annotations

import argparse
import math
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

from enemy_forge.archetypes import BY_NAME, ROSTER  # noqa: E402

# This file lives at <repo>/Tools/EnemyForge/, so the repo root is three levels up.
REPO_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DEFAULT_MODELS = os.path.join(REPO_ROOT, "Assets", "Models", "Enemies")
DEFAULT_RENDERS = os.path.join(REPO_ROOT, "Enemy_Renders_Review")

# Orthographic views, as (label, azimuth degrees, elevation degrees). Azimuth 0 looks
# at the model's front, which faces -Y.
VIEWS = [
    ("Isometric", 45.0, 30.0),
    ("Front", 0.0, 0.0),
    ("Side", 90.0, 0.0),
    ("ThreeQuarter", -35.0, 12.0),
    ("Back", 180.0, 12.0),
]


def _clear_scene() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)


def _world(strength: float = 0.38) -> None:
    world = bpy.data.worlds.new("ReviewWorld")
    bpy.context.scene.world = world
    world.use_nodes = True
    nodes, links = world.node_tree.nodes, world.node_tree.links
    nodes.clear()

    gradient = nodes.new("ShaderNodeTexGradient")
    gradient.gradient_type = "LINEAR"
    mapping = nodes.new("ShaderNodeMapping")
    mapping.inputs["Rotation"].default_value = (0.0, math.radians(-90.0), 0.0)
    coords = nodes.new("ShaderNodeTexCoord")
    ramp = nodes.new("ShaderNodeValToRGB")
    # The Lair frame: "one candle, one fire, falling away into black."
    ramp.color_ramp.elements[0].color = (0.0045, 0.0035, 0.0022, 1.0)
    ramp.color_ramp.elements[1].color = (0.052, 0.033, 0.014, 1.0)

    background = nodes.new("ShaderNodeBackground")
    background.inputs["Strength"].default_value = strength
    out = nodes.new("ShaderNodeOutputWorld")

    links.new(coords.outputs["Generated"], mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], gradient.inputs["Vector"])
    links.new(gradient.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], background.inputs["Color"])
    links.new(background.outputs["Background"], out.inputs["Surface"])


def _area_light(name, location, rotation, energy, size, color=(1.0, 1.0, 1.0)):
    data = bpy.data.lights.new(name, type="AREA")
    data.energy = energy
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    obj.location = location
    obj.rotation_euler = rotation
    bpy.context.collection.objects.link(obj)
    return obj


def _studio(height: float) -> None:
    """Candlelight, not a photo studio.

    The moodboard commits to one lighting idea — a candle guttering in a dark stone
    room — so the key is warm, low and close, the fill is barely there, and the only
    cool light is a thin moon rim that keeps the silhouette off the background.
    """
    reach = max(1.6, height * 1.35)
    # Key: a candle at chest height, close and to the front-right. Warm enough to
    # read as flame rather than daylight.
    _area_light("Candle", (reach * 0.62, -reach * 0.78, height * 0.62),
                (math.radians(84), 0.0, math.radians(38)),
                energy=reach * reach * 95.0, size=reach * 0.34,
                color=(1.0, 0.63, 0.28))
    # Fill: the room's own firelight bouncing back, dim and browner.
    _area_light("Hearth", (-reach * 1.05, -reach * 0.45, height * 0.38),
                (math.radians(96), 0.0, math.radians(-62)),
                energy=reach * reach * 18.0, size=reach * 1.1,
                color=(1.0, 0.52, 0.24))
    # Rim: moon through an arrow-loop. Cold, weak, and the only non-fire light.
    _area_light("Moon", (-reach * 0.30, reach * 1.15, height * 1.35),
                (math.radians(118), 0.0, math.radians(-166)),
                energy=reach * reach * 40.0, size=reach * 0.55,
                color=(0.55, 0.62, 0.78))


def _ground(radius: float) -> bpy.types.Object:
    bpy.ops.mesh.primitive_plane_add(size=radius * 8.0, location=(0.0, 0.0, 0.0))
    plane = bpy.context.object
    plane.name = "ReviewGround"

    mat = bpy.data.materials.new("ReviewGround")
    mat.use_nodes = True
    principled = mat.node_tree.nodes["Principled BSDF"]
    principled.inputs["Base Color"].default_value = (0.012, 0.010, 0.008, 1.0)
    principled.inputs["Roughness"].default_value = 0.62
    principled.inputs["Metallic"].default_value = 0.0
    plane.data.materials.append(mat)
    return plane


def _camera(subject_height: float, subject_width: float,
            azimuth: float, elevation: float) -> bpy.types.Object:
    """Orthographic camera framing the subject, orbited to the requested angle."""
    data = bpy.data.cameras.new("ReviewCamera")
    data.type = "ORTHO"
    # Fit the larger of height and width, with a margin so nothing touches the edge.
    data.ortho_scale = max(subject_height, subject_width) * 1.15
    camera = bpy.data.objects.new("ReviewCamera", data)
    bpy.context.collection.objects.link(camera)

    target = Vector((0.0, 0.0, subject_height * 0.52))
    distance = max(subject_height, subject_width) * 4.0
    yaw, pitch = math.radians(azimuth), math.radians(elevation)
    offset = Vector((
        math.sin(yaw) * math.cos(pitch),
        -math.cos(yaw) * math.cos(pitch),
        math.sin(pitch),
    )) * distance
    camera.location = target + offset

    direction = (target - camera.location).normalized()
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = camera
    return camera


# Set from --device in main(); every _clear_scene() resets the scene, so the backend
# has to be re-applied for each render rather than configured once at startup.
RENDER_DEVICE = "CPU"


def _configure_cycles(resolution: int, samples: int) -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    configure_device(RENDER_DEVICE)
    scene.cycles.samples = samples
    scene.cycles.use_adaptive_sampling = True
    scene.cycles.adaptive_threshold = 0.01
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 6
    scene.cycles.transparent_max_bounces = 4
    scene.render.resolution_x = resolution
    scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"


def _enable_wireframe() -> bpy.types.Material:
    """Clay override with its own edge overlay: the technical topology pass.

    Deliberately not Freestyle, and rendered without a ground plane because the
    override hits every object — see "Freestyle is not usable headlessly here" in
    docs/4-systems/enemy-asset-pipeline.md.
    """
    clay = bpy.data.materials.new("ReviewClay")
    clay.use_nodes = True
    nodes, links = clay.node_tree.nodes, clay.node_tree.links
    nodes.clear()

    surface = nodes.new("ShaderNodeBsdfPrincipled")
    surface.inputs["Base Color"].default_value = (0.17, 0.20, 0.26, 1.0)
    surface.inputs["Roughness"].default_value = 0.72
    surface.inputs["Metallic"].default_value = 0.0
    surface.location = (-300, 120)

    edges = nodes.new("ShaderNodeEmission")
    edges.inputs["Color"].default_value = (0.35, 0.92, 1.0, 1.0)
    edges.inputs["Strength"].default_value = 1.6
    edges.location = (-300, -140)

    wire = nodes.new("ShaderNodeWireframe")
    wire.use_pixel_size = True
    wire.inputs["Size"].default_value = 1.15
    wire.location = (-520, -30)

    mix = nodes.new("ShaderNodeMixShader")
    mix.location = (-60, 0)
    links.new(wire.outputs["Fac"], mix.inputs["Fac"])
    links.new(surface.outputs["BSDF"], mix.inputs[1])
    links.new(edges.outputs["Emission"], mix.inputs[2])

    out = nodes.new("ShaderNodeOutputMaterial")
    out.location = (180, 0)
    links.new(mix.outputs["Shader"], out.inputs["Surface"])

    bpy.context.view_layer.material_override = clay
    return clay


def _load_model(blend_path: str) -> list[bpy.types.Object]:
    """Append the mesh and rig from a built .blend into the current scene."""
    with bpy.data.libraries.load(blend_path, link=False) as (source, target):
        target.objects = list(source.objects)

    linked = []
    for obj in target.objects:
        if obj is None or obj.type not in {"MESH", "ARMATURE"}:
            continue
        bpy.context.collection.objects.link(obj)
        linked.append(obj)
    if not any(o.type == "MESH" for o in linked):
        raise RuntimeError(f"no mesh found in {blend_path}")
    return linked


def _render_to(path: str) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.context.scene.render.filepath = path
    bpy.context.scene.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True)


def render_enemy(arch, models_root: str, out_root: str,
                 resolution: int, samples: int) -> list[str]:
    blend_path = os.path.join(models_root, arch.name, f"{arch.name}.blend")
    if not os.path.exists(blend_path):
        raise FileNotFoundError(f"{blend_path} — run build_enemies.py first")

    out_dir = os.path.join(out_root, arch.name)
    written = []

    for label, azimuth, elevation in VIEWS + [("Wireframe", 45.0, 30.0)]:
        _clear_scene()
        _configure_cycles(resolution, samples)
        _world()
        objects = _load_model(blend_path)
        mesh = next(o for o in objects if o.type == "MESH")

        bounds = [mesh.matrix_world @ Vector(c) for c in mesh.bound_box]
        height = max(b.z for b in bounds)
        width = max(max(b.x for b in bounds) - min(b.x for b in bounds),
                    max(b.y for b in bounds) - min(b.y for b in bounds))

        _studio(height)
        _camera(height, width, azimuth, elevation)
        if label == "Wireframe":
            _enable_wireframe()
        else:
            _ground(max(height, width))

        path = os.path.join(out_dir, f"{arch.name}_{label}.png")
        _render_to(path)
        written.append(path)
        print(f"        {label:<13} {os.path.relpath(path, REPO_ROOT)}")
    return written


def render_lineup(roster, models_root: str, out_root: str,
                  resolution: int, samples: int) -> str:
    """One shot of the whole roster side by side, to check relative scale."""
    _clear_scene()
    _configure_cycles(resolution, max(samples // 2, 24))
    bpy.context.scene.render.resolution_x = int(resolution * 2.2)
    _world()

    spacing, cursor, tallest = 0.0, 0.0, 0.0
    placements = []
    for arch in roster:
        blend_path = os.path.join(models_root, arch.name, f"{arch.name}.blend")
        objects = _load_model(blend_path)
        mesh = next(o for o in objects if o.type == "MESH")
        bounds = [Vector(c) for c in mesh.bound_box]
        span = max(b.x for b in bounds) - min(b.x for b in bounds)
        tallest = max(tallest, max(b.z for b in bounds))

        cursor += span * 0.5 + spacing
        root = next((o for o in objects if o.type == "ARMATURE"), mesh)
        root.location.x = cursor
        if mesh is not root:
            mesh.location.x = 0.0
        placements.append(cursor)
        cursor += span * 0.5
        spacing = 0.45

    centre = (placements[0] + placements[-1]) / 2.0 if placements else 0.0
    for obj in bpy.context.scene.objects:
        if obj.type in {"MESH", "ARMATURE"} and obj.parent is None:
            obj.location.x -= centre

    _studio(tallest)
    _ground(cursor)

    total_width = cursor + 1.0
    camera = _camera(tallest, total_width, 8.0, 6.0)
    # ortho_scale sizes the wide axis, so the vertical extent it covers is only
    # scale / aspect. Fit whichever of width or height is the binding constraint.
    aspect = bpy.context.scene.render.resolution_x / bpy.context.scene.render.resolution_y
    camera.data.ortho_scale = max(total_width * 1.08, tallest * 1.18 * aspect)

    path = os.path.join(out_root, "00_Roster_Lineup.png")
    _render_to(path)
    print(f"        Lineup        {os.path.relpath(path, REPO_ROOT)}")
    return path


def contact_sheet(arch, tiles: list[str], out_root: str) -> str:
    """Stitch one enemy's passes into a single review sheet."""
    import numpy as np

    loaded = []
    for path in tiles:
        image = bpy.data.images.load(path, check_existing=False)
        buffer = np.empty(len(image.pixels), dtype=np.float32)
        image.pixels.foreach_get(buffer)
        loaded.append(buffer.reshape(image.size[1], image.size[0], 4))
        bpy.data.images.remove(image)

    columns = 3
    rows = (len(loaded) + columns - 1) // columns
    tile_h, tile_w = loaded[0].shape[:2]
    gap = max(4, tile_w // 120)

    sheet = np.zeros((rows * tile_h + (rows + 1) * gap,
                      columns * tile_w + (columns + 1) * gap, 4), dtype=np.float32)
    sheet[..., 3] = 1.0

    for index, tile in enumerate(loaded):
        row, column = divmod(index, columns)
        # Blender image rows run bottom-up, so fill from the bottom of the sheet.
        top = (rows - 1 - row) * (tile_h + gap) + gap
        left = column * (tile_w + gap) + gap
        sheet[top:top + tile_h, left:left + tile_w, :] = tile

    out = bpy.data.images.new(f"{arch.name}_Sheet",
                              width=sheet.shape[1], height=sheet.shape[0],
                              alpha=True, float_buffer=False)
    out.pixels.foreach_set(sheet.ravel())
    path = os.path.join(out_root, f"{arch.name}_ContactSheet.png")
    out.filepath_raw = path
    out.file_format = "PNG"
    out.save()
    bpy.data.images.remove(out)
    print(f"        ContactSheet  {os.path.relpath(path, REPO_ROOT)}")
    return path


def script_argv() -> list[str]:
    """Arguments meant for this script under either launch mode.

    `blender --background --python x.py -- --samples 64` puts Blender's own flags in
    sys.argv, so everything after a lone `--` belongs to us; run as plain Python
    there is no separator and the usual argv tail applies.
    """
    if "--" in sys.argv:
        return sys.argv[sys.argv.index("--") + 1:]
    # Launched as `python3 render_enemies.py ...`: argv[0] is this script.
    if os.path.basename(sys.argv[0]) == os.path.basename(__file__):
        return sys.argv[1:]
    # Launched by the Blender CLI with no `--`: argv holds Blender's flags, not ours.
    return []


def configure_device(requested: str) -> str:
    """Point Cycles at a compute backend and report exactly what it will use."""
    scene = bpy.context.scene
    if requested == "CPU":
        scene.cycles.device = "CPU"
        return "CPU"

    prefs = bpy.context.preferences.addons["cycles"].preferences
    candidates = [requested] if requested != "AUTO" else ["OPTIX", "CUDA", "HIP",
                                                          "METAL", "ONEAPI"]
    for backend in candidates:
        try:
            prefs.compute_device_type = backend
        except TypeError:
            continue  # this build has no kernels for that backend at all

        refresh = getattr(prefs, "refresh_devices", None) or prefs.get_devices
        refresh()
        usable = [d for d in prefs.devices if d.type == backend]
        if not usable:
            continue

        for device in prefs.devices:
            device.use = device.type == backend
        scene.cycles.device = "GPU"
        names = ", ".join(sorted({d.name for d in usable}))
        return f"{backend} ({names})"

    available = sorted({d.type for d in getattr(prefs, "devices", [])} - {"CPU"})
    if requested == "AUTO":
        scene.cycles.device = "CPU"
        return "CPU (no GPU backend available)"
    raise SystemExit(
        f"--device {requested} requested but no {requested} device is available. "
        f"Backends this Blender can see: {available or 'none'}. "
        f"Use --device CPU to render without a GPU.")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--models", default=DEFAULT_MODELS)
    parser.add_argument("--out", default=DEFAULT_RENDERS)
    parser.add_argument("--resolution", type=int, default=1100)
    parser.add_argument("--samples", type=int, default=64)
    parser.add_argument("--only", nargs="*", default=None)
    parser.add_argument("--skip-lineup", action="store_true")
    parser.add_argument("--lineup-only", action="store_true",
                        help="render just the roster line-up, skipping per-enemy passes")
    parser.add_argument("--device", default="AUTO",
                        choices=["AUTO", "OPTIX", "CUDA", "HIP", "METAL", "ONEAPI", "CPU"],
                        help="Cycles compute backend (default: AUTO, GPU if one exists)")
    args = parser.parse_args(script_argv())

    global RENDER_DEVICE
    RENDER_DEVICE = args.device

    selected = ROSTER if not args.only else [BY_NAME[n] for n in args.only]
    os.makedirs(args.out, exist_ok=True)

    bpy.context.scene.render.engine = "CYCLES"
    print(f"Cycles device: {configure_device(args.device)}")

    started = time.time()
    for arch in [] if args.lineup_only else selected:
        print(f"\n=== {arch.name} ===")
        step = time.time()
        tiles = render_enemy(arch, args.models, args.out,
                             args.resolution, args.samples)
        contact_sheet(arch, tiles, args.out)
        print(f"        rendered in {time.time() - step:.1f}s")

    if not args.skip_lineup:
        print("\n=== Roster line-up ===")
        render_lineup(selected, args.models, args.out, args.resolution, args.samples)

    print(f"\nRenders written to {os.path.relpath(args.out, REPO_ROOT)} "
          f"in {time.time() - started:.1f}s")
    return 0


if __name__ == "__main__":
    sys.exit(main())
