"""
Render the Lair assembled from its built FBXs: the cellar, every prop placed as in
docs/art/concept/lair/lair.png, a hearth light and a candle light, and a 1.80 m
figure for scale. Cycles CPU, modest samples (a few minutes on 4 cores).

    blender -b -P Tools/AssetPipeline/render_lair_scene.py [-- --out path.png]

Output: docs/art/models/lair/lair-assembled.png. Imports the FBXs exactly as Unity
will, so the placements here (offsets, rotations) are also the reference for the
Unity prefab: see the Lair section of the README.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import palette as pal  # noqa: E402
import render_previews as rp  # noqa: E402

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(REPO_ROOT, "docs", "art", "models", "lair", "lair-assembled.png")
FLOOR = 0.30
TABLE = (-0.8, -1.0)
DIAL = (2.2, -0.2)
HEARTH_LIGHT = (3.5, 4.35, FLOOR + 0.8)
CAMERA = (2.4, -4.4, 1.65)
CAMERA_TARGET = (-0.6, 1.8, 1.3)
HUMAN = (-2.8, 3.0)

# (key, location (x, y, z), rotation degrees (x, y, z))
PLACEMENTS = [
    ("LairCellar", (0, 0, 0), (0, 0, 0)),
    ("LairPortalArch", (-7.2, 0, FLOOR), (0, 0, 90)),           # in its recess, stones toward the room
    ("LairLedgerTable", (TABLE[0], TABLE[1], FLOOR), (0, 0, 0)),
    ("LairLedger", (TABLE[0], TABLE[1], FLOOR + 0.80), (0, 0, 0)),
    ("LairCandle", (TABLE[0] + 1.0, TABLE[1] + 0.25, FLOOR + 0.80), (0, 0, 0)),
    ("LairCenturyDialStand", (DIAL[0], DIAL[1], FLOOR), (0, 0, 0)),
    ("LairWeaponRack", (-4.2, 4.7, FLOOR), (0, 0, 0)),
] + [("LairStrongbox", (TABLE[0] + dx, TABLE[1] - 0.62, FLOOR), (0, 0, 0)) for dx in (-1.05, -0.35, 0.35, 1.05)] + [
    ("LairCenturyDialRing%d" % n, (DIAL[0], DIAL[1], FLOOR + 1.18), rot)
    for n, rot in ((1, (15, 0, 0)), (2, (-30, 0, 20)), (3, (0, 35, 60)), (4, (25, -20, 110)))
]
GLOW = {"madder": (1.0, 0.35, 0.1, 5.0), "orpiment": (1.0, 0.7, 0.2, 5.0)}


def import_fbx(key):
    path = os.path.join(REPO_ROOT, "Assets", "_Project", "Art", "Models", "Lair", key + ".fbx")
    before = set(bpy.data.objects.keys())
    bpy.ops.import_scene.fbx(filepath=path)
    return [o for o in bpy.data.objects if o.name not in before and o.type == "MESH"][0]


def glow(obj):
    """The fire's and the flame's pigments emit, so they light their own surroundings."""
    for m in obj.data.materials:
        for p, (r, g, b, s) in GLOW.items():
            if m.name.endswith("_" + p):
                bsdf = m.node_tree.nodes["Principled BSDF"]
                bsdf.inputs["Emission Color"].default_value = (r, g, b, 1)
                bsdf.inputs["Emission Strength"].default_value = s


def point(name, loc, watts, color, radius=0.1):
    d = bpy.data.lights.new(name, "POINT")
    d.energy, d.color, d.shadow_soft_size = watts, color, radius
    o = bpy.data.objects.new(name, d)
    bpy.context.collection.objects.link(o)
    o.location = loc
    return o


def look(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def emissive(name, rgb, strength):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    b = mat.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Emission Color"].default_value = (*rgb, 1)
    b.inputs["Emission Strength"].default_value = strength
    return mat


def add_human(x, y):
    """A 1.80 m figure: capsule of radius 0.22 standing on the floor."""
    made = []
    bpy.ops.mesh.primitive_cylinder_add(radius=0.22, depth=1.36, location=(x, y, FLOOR + 0.9))
    made.append(bpy.context.active_object)
    for z in (FLOOR + 0.22, FLOOR + 1.58):
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.22, location=(x, y, z))
        made.append(bpy.context.active_object)
    mat = bpy.data.materials.new("Human")
    mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.86, 0.82, 0.73, 1)
    for o in made:
        o.data.materials.append(mat)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = argv[argv.index("--out") + 1] if "--out" in argv else OUT
    rp.clear_scene()
    rp.setup_world_background()
    for key, loc, rot in PLACEMENTS:
        o = import_fbx(key)
        o.location = loc
        o.rotation_euler = tuple(math.radians(a) for a in rot)
        if key in ("LairCellar", "LairCandle"):
            glow(o)
    # the portal's glow: an arch-shaped sheet inside the arch's thickness
    pts = [(-0.97, 0.1), (0.97, 0.1)] + [(0.97 * math.cos(math.radians(a)), 1.9 + 0.97 * math.sin(math.radians(a)))
                                         for a in range(0, 181, 15)]
    me = bpy.data.meshes.new("PortalGlow")
    me.from_pydata([(-7.2, u, FLOOR + z) for u, z in pts], [], [list(range(len(pts)))])
    pl = bpy.data.objects.new("PortalGlow", me)
    bpy.context.collection.objects.link(pl)
    pl.data.materials.append(emissive("PortalGlow", pal.hex_to_rgb01(pal.PIGMENTS["lapis"]), 0.4))
    add_human(*HUMAN)
    # one fire, one candle, a breath of portal light
    point("Hearth", HEARTH_LIGHT, 4500, (1.0, 0.45, 0.15), 0.3)
    point("Candle", (TABLE[0] + 1.0, TABLE[1] + 0.25, FLOOR + 1.35), 80, (1.0, 0.75, 0.4), 0.03)
    point("PortalLight", (-6.6, 0, FLOOR + 1.6), 120, (0.48, 0.42, 0.63), 0.4)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.lens = 17
    bpy.context.collection.objects.link(cam)
    eye, target = CAMERA, CAMERA_TARGET
    if os.environ.get("LAIR_CAM"):   # "x,y,z,tx,ty,tz": an alternative view for audits
        v = [float(t) for t in os.environ["LAIR_CAM"].split(",")]
        eye, target = v[:3], v[3:]
    cam.location = eye
    look(cam, target)
    bpy.context.scene.camera = cam
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 128
    sc.cycles.use_denoising = False
    sc.cycles.max_bounces = 6
    sc.cycles.sample_clamp_indirect = 3.0
    sc.view_settings.view_transform = "Filmic"
    sc.view_settings.look = "None"
    res = os.environ.get("LAIR_PREVIEW_RES")   # e.g. 640: a quick low-res test render
    sc.render.resolution_x, sc.render.resolution_y = (1280, 720) if not res else (int(res), int(res) * 9 // 16)
    if res:
        sc.cycles.samples = 24
    sc.render.image_settings.file_format = "PNG"
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sc.render.filepath = out
    bpy.ops.render.render(write_still=True)
    print("rendered", out)


if __name__ == "__main__":
    main()
