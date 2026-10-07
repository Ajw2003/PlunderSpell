"""
Render a high view down into castle modules, to compare a room with its sheet:

    blender -b -P Tools/AssetPipeline/render_room_overhead.py -- <KeyOrPrefix> [...]

render_previews.py's three-quarter camera sits low outside the cell, so a room's
own walls hide most of what is in it. This camera stands high over the
south half looking steeply down and a little north, so the whole floor, the furniture and the
clear cross read at once, much as the plan on the room's sheet does. Output:
Tools/AssetPipeline/previews/overhead/<Key>.png. A separate script (not a flag on
render_previews.py) because that file hashes its own source to decide when
every preview is stale.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import asset_specs  # noqa: E402
import palette as pal  # noqa: E402
import render_previews as rp  # noqa: E402

OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "previews", "overhead")
RES = 720


def _aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def _cut_roof(obj, above=2.4):
    """The Lair's barrel vault would hide the whole room from above: delete every
    connected piece that starts above Z = `above` (the vault segments and ribs),
    leaving walls, pilasters, hearth and floor. Only this render is cut; the FBX is not."""
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    seen, doomed = set(), []
    for f in bm.faces:
        if f.index in seen:
            continue
        stack, island = [f], []
        seen.add(f.index)
        while stack:
            face = stack.pop()
            island.append(face)
            for e in face.edges:
                for g in e.link_faces:
                    if g.index not in seen:
                        seen.add(g.index)
                        stack.append(g)
        if min(v.co.z for g in island for v in g.verts) > above:
            doomed += island
    bmesh.ops.delete(bm, geom=doomed, context="FACES")
    bm.to_mesh(obj.data)
    bm.free()


def render_overhead(spec):
    rp.clear_scene()
    rp.setup_world_background()
    obj = rp.import_asset(spec)
    lair = spec["subdir"] == "Lair"
    if lair:
        _cut_roof(obj)
    # Unity is Y-up and the FBX import turns it back to Blender's Z-up, so the
    # module sits as it was built: +Y north, floor on Z = 0.
    cam_data = bpy.data.cameras.new("OverheadCam")
    cam_data.lens = 28
    cam = bpy.data.objects.new("OverheadCam", cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = (0.0, -6.5, 21.0)
    _aim(cam, (0.0, 0.2, 0.0))
    if lair:   # ~16 x 12 m outside: bigger than a castle cell
        cam.location = (0.0, -4.5, 15.5)
        _aim(cam, (0.0, 0.3, 0.0))
    bpy.context.scene.camera = cam

    sun = bpy.data.lights.new("Sun", type="SUN")
    sun.energy = 3.2
    sun.angle = math.radians(8)
    sun_obj = bpy.data.objects.new("Sun", sun)
    bpy.context.collection.objects.link(sun_obj)
    sun_obj.rotation_euler = (math.radians(38), math.radians(-18), math.radians(25))
    fill = bpy.data.lights.new("Fill", type="AREA")
    fill.energy = 2600
    fill.size = 14
    fill.color = pal.hex_to_rgb01(pal.PIGMENTS["vellum"])
    fill_obj = bpy.data.objects.new("Fill", fill)
    bpy.context.collection.objects.link(fill_obj)
    fill_obj.location = (0, 0, 14)
    _aim(fill_obj, (0, 0, 0))

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 32
    scene.cycles.use_denoising = False
    scene.render.resolution_x = RES
    scene.render.resolution_y = RES
    scene.render.image_settings.file_format = "PNG"
    os.makedirs(OUT_DIR, exist_ok=True)
    scene.render.filepath = os.path.join(OUT_DIR, f"{spec['key']}.png")
    bpy.ops.render.render(write_still=True)
    print(f"rendered overhead {spec['key']}")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if not argv:
        print("usage: blender -b -P render_room_overhead.py -- <KeyOrPrefix> [...]")
        sys.exit(2)
    specs = [s for s in asset_specs.ALL_SPECS
             if s["subdir"].startswith(("Castle", "Lair")) and asset_specs.key_matches(s["key"], ",".join(argv))]
    if not specs:
        print(f"ERROR: no castle module matches {argv}")
        sys.exit(1)
    for spec in specs:
        render_overhead(spec)


if __name__ == "__main__":
    main()
