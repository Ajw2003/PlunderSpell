"""Render one outer-bailey vignette of the High Medieval castle under four candidate night looks.

Built to answer one art-direction question ("which reference look should the game anchor on?")
while the Unity Editor was unavailable. It lays out real castle modules from
Assets/_Project/Art/Models/Castle, adds stand-in braziers and wall torches, and renders the same
camera once per look with Eevee. These are mood comparisons: the lighting, fog and grade differ,
the surfaces are still the flat palette colours, because the game's surface shader does not exist
yet.

Run from the repo root:
    blender -b --python Tools/LookSamples/render_look_samples.py -- [look ...]
With no looks named, every look renders (the four references, then calm and alert). Output: docs/generated/look-samples-2026-09-24/<look>.png
"""

import math
import os
import sys

import bpy
from mathutils import Vector

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CASTLE = os.path.join(REPO, "Assets", "_Project", "Art", "Models", "Castle")
OUT = os.path.join(REPO, "docs", "generated", "look-samples-2026-09-24")

CELL = 12.0

# Module name, cell (x, y), yaw in degrees. The kit raises each wall on the cell's -Y side, so the
# curtain runs along y = -6 and the outer bailey is the open ground on +Y.
LAYOUT = [
    ("WallCorner", (-3, 0), 0),
    ("WallStraight", (-2, 0), 0),
    ("Bastion", (-1, 0), 0),
    ("GatehouseModule", (0, 0), 0),
    ("WallStraight", (1, 0), 0),
    ("Bastion", (2, 0), 0),
    ("WallStraight", (3, 0), 0),
    ("StableBlock", (-2, 2), 90),
    ("WellCourtyard", (2, 2), 0),
    ("StorehouseRoom", (-1, 3), 0),
    ("BlacksmithShop", (1, 3), 180),
]

# World-space spots for fire. Braziers stand in the bailey; torches hang on the wall's inner face.
BRAZIERS = [(-4.0, 4.0), (4.0, 4.0), (-14.0, 12.0)]
TORCHES = [(-8.0, -4.2, 3.2), (8.0, -4.2, 3.2), (-20.0, -4.2, 3.2), (20.0, -4.2, 3.2)]

# Lit only when the castle is alerted: beacons on the wall walk and a bonfire in the bailey.
ALARM_BEACONS = [(-8.0, -5.0, 6.2), (8.0, -5.0, 6.2), (-20.0, -5.0, 6.2), (20.0, -5.0, 6.2)]
ALARM_BONFIRES = [(-1.0, 10.0)]

CAMERA_POS = Vector((9.0, 17.0, 1.7))
CAMERA_TARGET = Vector((-3.0, -1.0, 4.2))

# Each look: moon, sky, fire, fog and the compositor grade.
LOOKS = {
    "a-dishonored": dict(
        title="A - Dishonored: painterly, warm against cool, desaturated",
        moon_color=(0.42, 0.55, 0.9), moon_strength=7.0, moon_elevation=28,
        sky_color=(0.035, 0.05, 0.09), sky_strength=1.0,
        fire_color=(1.0, 0.55, 0.22), fire_power=900, ember_strength=25,
        fog_color=(0.85, 0.88, 0.92), fog_density=0.009, fog_anisotropy=0.4,
        saturation=0.72, contrast=1.18,
        lift=(0.96, 0.98, 1.04), gamma=(1.0, 0.98, 1.0), gain=(1.08, 1.0, 0.92),
        bloom=0.45, bloom_threshold=1.0, vignette=0.5, exposure=-0.35,
    ),
    "b-sea-of-thieves": dict(
        title="B - Sea of Thieves: saturated night blue, glowing lanterns",
        moon_color=(0.45, 0.62, 1.0), moon_strength=10.0, moon_elevation=38,
        sky_color=(0.03, 0.07, 0.2), sky_strength=1.0,
        fire_color=(1.0, 0.62, 0.25), fire_power=1300, ember_strength=45,
        fog_color=(0.8, 0.88, 0.95), fog_density=0.005, fog_anisotropy=0.3,
        saturation=1.35, contrast=1.05,
        lift=(0.98, 1.0, 1.06), gamma=(0.98, 1.0, 1.04), gain=(1.04, 1.02, 1.0),
        bloom=0.8, bloom_threshold=0.9, vignette=0.25, exposure=-0.6,
    ),
    "c-thief-hunt": dict(
        title="C - Thief / Hunt: dark, gritty, oppressive fog",
        moon_color=(0.6, 0.66, 0.7), moon_strength=5.0, moon_elevation=22,
        sky_color=(0.03, 0.035, 0.038), sky_strength=1.0,
        fire_color=(1.0, 0.5, 0.2), fire_power=550, ember_strength=18,
        fog_color=(0.8, 0.82, 0.8), fog_density=0.022, fog_anisotropy=0.2,
        saturation=0.45, contrast=1.3,
        lift=(0.98, 1.0, 0.98), gamma=(0.97, 1.0, 0.96), gain=(1.02, 1.0, 0.94),
        bloom=0.2, bloom_threshold=1.5, vignette=0.6, exposure=0.1,
    ),
    "d-valheim": dict(
        title="D - Valheim: lo-fi surfaces lifted by thick fog, shafts and bloom",
        moon_color=(0.5, 0.62, 0.9), moon_strength=8.0, moon_elevation=24,
        sky_color=(0.05, 0.07, 0.12), sky_strength=1.0,
        fire_color=(1.0, 0.5, 0.18), fire_power=1100, ember_strength=40,
        fog_color=(0.85, 0.9, 0.95), fog_density=0.016, fog_anisotropy=0.75,
        saturation=0.9, contrast=1.12,
        lift=(0.97, 0.99, 1.05), gamma=(1.0, 1.0, 1.02), gain=(1.06, 1.0, 0.94),
        bloom=1.0, bloom_threshold=0.8, vignette=0.35, exposure=-0.2,
        pixelate=3,
    ),
    # The chosen direction (2026-09-24): the castle holds two states. Warmth comes from fire lighting
    # the fog; the moon is a faint cool fill that the fog mostly swallows.
    "calm": dict(
        title="Calm - the castle asleep: low fires glowing through warm fog",
        moon_color=(0.45, 0.58, 0.9), moon_strength=0.8, moon_elevation=26,
        sky_color=(0.02, 0.022, 0.03), sky_strength=1.0,
        fire_color=(1.0, 0.52, 0.2), fire_power=800, ember_strength=30,
        fog_color=(0.35, 0.42, 0.55), fog_density=0.035, fog_anisotropy=0.6,
        saturation=0.95, contrast=1.1,
        lift=(0.98, 0.98, 1.02), gamma=(1.0, 0.99, 0.98), gain=(1.06, 1.0, 0.9),
        bloom=0.8, bloom_threshold=0.8, vignette=0.45, exposure=0.35,
    ),
    "alert": dict(
        title="Alert - alarm raised: beacons lit, braziers roaring, redder flame",
        moon_color=(0.45, 0.58, 0.9), moon_strength=0.8, moon_elevation=26,
        sky_color=(0.03, 0.018, 0.015), sky_strength=1.0,
        fire_color=(1.0, 0.36, 0.1), fire_power=1100, ember_strength=70,
        fog_color=(0.35, 0.42, 0.55), fog_density=0.028, fog_anisotropy=0.6,
        saturation=1.05, contrast=1.3,
        lift=(1.0, 0.97, 0.97), gamma=(1.02, 0.98, 0.96), gain=(1.1, 0.97, 0.86),
        bloom=1.0, bloom_threshold=0.7, vignette=0.55, exposure=0.0,
        brazier_scale=1.6, alarm_fires=True,
    ),
}


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_module(name, cell, yaw):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(CASTLE, name + ".fbx"))
    for obj in (o for o in bpy.data.objects if o not in before and o.parent is None):
        obj.location = (cell[0] * CELL, cell[1] * CELL, 0.0)
        obj.rotation_euler = (obj.rotation_euler.x, obj.rotation_euler.y, math.radians(yaw))


def flat_material(name, color, emission=None, strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.9
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = strength
    return mat


def add_ground():
    bpy.ops.mesh.primitive_plane_add(size=400, location=(0, 0, 0.0))
    bpy.context.object.data.materials.append(flat_material("Earth", (0.09, 0.075, 0.06)))
    # The packed-earth path from the gate into the bailey.
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 14, 0.01))
    path = bpy.context.object
    path.scale = (4.5, 36, 1)
    path.data.materials.append(flat_material("Path", (0.16, 0.14, 0.11)))


def add_fire(location, look, pole_height, scale=1.0):
    x, y, z = location
    ember = flat_material("Ember", (0.1, 0.05, 0.02), look["fire_color"], look["ember_strength"])
    iron = flat_material("Iron", (0.06, 0.06, 0.065))
    if pole_height > 0:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=pole_height,
                                            location=(x, y, pole_height / 2))
        bpy.context.object.data.materials.append(iron)
        bpy.ops.mesh.primitive_cone_add(radius1=0.25 * scale, radius2=0.45 * scale, depth=0.35,
                                        location=(x, y, pole_height + 0.1))
        bpy.context.object.data.materials.append(iron)
        z = pole_height + 0.35
    bpy.ops.mesh.primitive_ico_sphere_add(radius=(0.22 if pole_height else 0.12) * scale,
                                          subdivisions=2,
                                          location=(x, y, z))
    bpy.context.object.data.materials.append(ember)
    bpy.context.object.visible_shadow = False  # the flame must not shadow its own light
    # The flame stands above the bowl; a light level with the rim would be shadowed by it.
    bpy.ops.object.light_add(type="POINT", location=(x, y, z + 0.45 * scale))
    light = bpy.context.object.data
    light.color = look["fire_color"]
    light.energy = look["fire_power"] * (1.0 if pole_height else 0.55) * scale * scale
    light.shadow_soft_size = 0.3


def add_props():
    wood = flat_material("Wood", (0.22, 0.15, 0.09))
    hay = flat_material("Hay", (0.45, 0.36, 0.16))
    # A cart and stacked crates break up the open bailey.
    for (loc, scale, mat) in [
        ((-6.5, 9.0, 0.6), (1.1, 2.0, 0.35), wood),
        ((-6.5, 9.0, 1.05), (1.0, 1.8, 0.12), hay),
        ((8.5, 5.5, 0.45), (0.45, 0.45, 0.45), wood),
        ((9.4, 5.8, 0.45), (0.45, 0.45, 0.45), wood),
        ((8.9, 5.6, 1.3), (0.4, 0.4, 0.4), wood),
        ((-2.0, 16.0, 0.5), (1.4, 0.7, 0.5), hay),
    ]:
        bpy.ops.mesh.primitive_cube_add(size=2, location=loc)
        bpy.context.object.scale = scale
        bpy.context.object.data.materials.append(mat)
    for wheel_y in (7.6, 10.4):
        bpy.ops.mesh.primitive_cylinder_add(radius=0.55, depth=0.12, location=(-7.7, wheel_y, 0.55),
                                            rotation=(0, math.radians(90), 0))
        bpy.context.object.data.materials.append(wood)


def add_fog_box(look):
    """Fog as a finite box around the castle. Eevee treats a world volume as infinitely deep, so
    it swallows sunlight completely and the moon lights nothing."""
    if look["fog_density"] <= 0:
        return
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 10, 12))
    box = bpy.context.object
    box.scale = (180, 180, 24)
    mat = bpy.data.materials.new("Fog")
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.remove(nodes["Principled BSDF"])
    scatter = nodes.new("ShaderNodeVolumeScatter")
    scatter.inputs["Color"].default_value = (*look["fog_color"], 1.0)
    scatter.inputs["Density"].default_value = look["fog_density"]
    scatter.inputs["Anisotropy"].default_value = look["fog_anisotropy"]
    links.new(scatter.outputs["Volume"], nodes["Material Output"].inputs["Volume"])
    box.data.materials.append(mat)


def setup_world(look):
    scene = bpy.context.scene
    world = bpy.data.worlds.new("Night")
    scene.world = world
    world.use_nodes = True
    nodes, links = world.node_tree.nodes, world.node_tree.links
    nodes["Background"].inputs["Color"].default_value = (*look["sky_color"], 1.0)
    nodes["Background"].inputs["Strength"].default_value = look["sky_strength"]
    add_fog_box(look)

    bpy.ops.object.light_add(type="SUN", location=(0, 0, 50))
    moon = bpy.context.object
    # Moonlight comes over the camera's right shoulder so it lands on the wall's inner face, the
    # side the bailey sees; lit from outside, every visible surface would be fire-lit only.
    elevation = math.radians(look["moon_elevation"])
    travel = Vector((-0.55, -0.8, 0.0)).normalized() * math.cos(elevation)
    travel.z = -math.sin(elevation)
    moon.rotation_euler = travel.to_track_quat("-Z", "Y").to_euler()
    moon.data.color = look["moon_color"]
    moon.data.energy = look["moon_strength"]
    moon.data.angle = math.radians(1.0)


def setup_camera():
    bpy.ops.object.camera_add(location=CAMERA_POS)
    camera = bpy.context.object
    direction = CAMERA_TARGET - CAMERA_POS
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 22
    camera.data.clip_end = 600
    bpy.context.scene.camera = camera


def setup_render(look, width=1600, height=900):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    scene.eevee.taa_render_samples = 64
    scene.eevee.use_shadows = True
    scene.eevee.volumetric_tile_size = "4"
    scene.eevee.volumetric_end = 200
    scene.eevee.use_volumetric_shadows = True
    scene.eevee.shadow_pool_size = "1024"  # the alert state's extra fires overflow the default
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Punchy" if look["contrast"] > 1.15 else "None"
    scene.view_settings.exposure = look["exposure"]
    setup_grade(look)


def setup_grade(look):
    scene = bpy.context.scene
    scene.use_nodes = True
    tree = scene.node_tree
    tree.nodes.clear()
    rl = tree.nodes.new("CompositorNodeRLayers")
    glare = tree.nodes.new("CompositorNodeGlare")
    glare.glare_type = "BLOOM"
    glare.threshold = look["bloom_threshold"]
    glare.mix = look["bloom"] - 1.0  # -1 = no glare, 0 = even mix
    balance = tree.nodes.new("CompositorNodeColorBalance")
    balance.correction_method = "LIFT_GAMMA_GAIN"
    balance.lift = look["lift"]
    balance.gamma = look["gamma"]
    balance.gain = look["gain"]
    hsv = tree.nodes.new("CompositorNodeHueSat")
    hsv.inputs["Saturation"].default_value = look["saturation"]
    contrast = tree.nodes.new("CompositorNodeBrightContrast")
    contrast.inputs["Contrast"].default_value = (look["contrast"] - 1.0) * 60

    mask = tree.nodes.new("CompositorNodeEllipseMask")
    mask.width, mask.height = 1.15, 1.25
    blur = tree.nodes.new("CompositorNodeBlur")
    blur.size_x = blur.size_y = 220
    vignette = tree.nodes.new("CompositorNodeMixRGB")
    vignette.blend_type = "MULTIPLY"
    vignette.inputs["Fac"].default_value = look["vignette"]
    out = tree.nodes.new("CompositorNodeComposite")

    links = tree.links
    source = rl.outputs["Image"]
    if look.get("pixelate"):
        # Valheim's lo-fi read: chunky pixels under full-resolution light and fog.
        pixelate = tree.nodes.new("CompositorNodePixelate")
        pixelate.pixel_size = look["pixelate"]
        links.new(source, pixelate.inputs["Color"])
        source = pixelate.outputs["Color"]
    links.new(source, glare.inputs["Image"])
    links.new(glare.outputs["Image"], balance.inputs["Image"])
    links.new(balance.outputs["Image"], hsv.inputs["Image"])
    links.new(hsv.outputs["Image"], contrast.inputs["Image"])
    links.new(contrast.outputs["Image"], vignette.inputs[1])
    links.new(mask.outputs["Mask"], blur.inputs["Image"])
    links.new(blur.outputs["Image"], vignette.inputs[2])
    links.new(vignette.outputs["Image"], out.inputs["Image"])


def render(key, look):
    reset()
    for name, cell, yaw in LAYOUT:
        import_module(name, cell, yaw)
    add_ground()
    add_props()
    for x, y in BRAZIERS:
        add_fire((x, y, 0.0), look, pole_height=1.2, scale=look.get("brazier_scale", 1.0))
    for torch in TORCHES:
        add_fire(torch, look, pole_height=0)
    if look.get("alarm_fires"):
        for beacon in ALARM_BEACONS:
            add_fire(beacon, look, pole_height=0, scale=2.0)
        for x, y in ALARM_BONFIRES:
            add_fire((x, y, 0.0), look, pole_height=0.4, scale=2.5)
    setup_world(look)
    setup_camera()
    setup_render(look)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, key + ".png")
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print(f"[LookSamples] wrote {path}")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    keys = argv or list(LOOKS)
    for key in keys:
        render(key, LOOKS[key])


if __name__ == "__main__":
    main()
