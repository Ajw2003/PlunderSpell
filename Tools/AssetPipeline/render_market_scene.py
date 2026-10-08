"""
Render the Market assembled from its built FBXs: the yard, four stalls in a loose ring round
the well as in docs/art/concept/lair/market.png, a counter and a slate at each, the scales on
the goldsmith's counter with coins, lanterns lit with warm point lights, a dark night sky and a
1.80 m figure for scale. Cycles CPU, 128 samples, 1280 x 720 (a few minutes on 4 cores).

    blender -b -P Tools/AssetPipeline/render_market_scene.py [-- --out path.png]
    blender -b -P Tools/AssetPipeline/render_market_scene.py -- --placements Tools/AssetPipeline/placements/market.json

Output: docs/art/models/market/market-assembled.png. Imports the FBXs exactly as Unity will, so
the placements here are also the reference for the Unity prefab: see the Market section of the
README. MARKET_PREVIEW_RES=640 gives a quick test; MARKET_CAM="x,y,z,tx,ty,tz" another view.
"""
import math
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import market_builders as mb  # noqa: E402
import render_lair_scene as rls  # noqa: E402  (point, look, glow, add_human: shared with the Lair render)
import render_previews as rp  # noqa: E402

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(REPO_ROOT, "docs", "art", "models", "market", "market-assembled.png")
FL = mb.FL
CAMERA = (0.0, -9.6, 3.2)
CAMERA_TARGET = (0.0, 1.5, 1.2)
HUMAN = (-1.3, -2.4)
WARM = (1.0, 0.62, 0.28)

# stall frames: (key, yard position); the front (local -Y) faces the well
STALLS = {"gold": (-4.0, 4.6), "pardoner": (4.0, 4.6), "antiq": (5.9, -1.0), "fence": (-4.3, -1.4)}
CART = (-6.9, -1.4)
LANTERNS = []     # (x, y, z of the lantern base, watts)
PLACED = []       # (key, (x, y, z), z rotation in degrees): every model placed, for --placements


def facing(p):
    """Z rotation (radians) that turns local -Y toward the yard's centre."""
    n = math.hypot(*p)
    return math.atan2(-p[0] / n, p[1] / n)


def to_world(p, th, lx, ly):
    return (p[0] + lx * math.cos(th) - ly * math.sin(th), p[1] + lx * math.sin(th) + ly * math.cos(th))


def import_fbx(key):
    path = os.path.join(REPO_ROOT, "Assets", "_Project", "Art", "Models", "Market", key + ".fbx")
    before = set(bpy.data.objects.keys())
    bpy.ops.import_scene.fbx(filepath=path)
    return [o for o in bpy.data.objects if o.name not in before and o.type == "MESH"][0]


def place(key, x, y, z, rot_deg=0.0, glow=False):
    o = import_fbx(key)
    o.location = (x, y, z)
    o.rotation_euler = (0, 0, math.radians(rot_deg))
    PLACED.append((key, (x, y, z), rot_deg))
    if glow:
        rls.glow(o)
    return o


def at(key, p, th, lx, ly, z, extra=0.0, glow=False):
    """Place `key` at stall-local (lx, ly), height z above the yard floor."""
    x, y = to_world(p, th, lx, ly)
    return place(key, x, y, FL + z, math.degrees(th + extra), glow)


def lantern_at(p, th, lx, ly, hook_z, watts=90):
    x, y = to_world(p, th, lx, ly)
    LANTERNS.append((x, y, FL + hook_z - 0.41, watts))


def counter_stuff(p, th, ly, with_scales=False):
    at("MarketCounter", p, th, 0, ly, 0)
    top = mb.COUNTER_TOP if hasattr(mb, "COUNTER_TOP") else 1.0
    if with_scales:
        sx = -0.4
        at("MarketScalesBase", p, th, sx, ly, top)
        at("MarketScalesBeam", p, th, sx, ly, top + 0.5)
        for s in (-1, 1):
            at("MarketScalesPan", p, th, sx + s * 0.28, ly, top + 0.5 - 0.26)
        at("MarketCoin", p, th, sx - 0.28 + 0.01, ly, top + 0.5 - 0.26 + 0.011)
        at("MarketCoin", p, th, sx - 0.28 - 0.025, ly + 0.02, top + 0.5 - 0.26 + 0.011, extra=0.6)
        at("MarketCoinStack", p, th, sx + 0.28, ly, top + 0.5 - 0.26 + 0.011)
        at("MarketCoinStack", p, th, 0.35, ly - 0.1, top)
        at("MarketCoin", p, th, 0.5, ly + 0.15, top, extra=0.3)
        at("MarketCoin", p, th, 0.62, ly + 0.08, top, extra=1.0)
        at("MarketPouch", p, th, 0.72, ly + 0.1, top)
    else:
        at("MarketCoinStack", p, th, 0.3, ly - 0.05, top)
        at("MarketCoinStack", p, th, 0.42, ly - 0.12, top, extra=1.0)
        at("MarketCoin", p, th, 0.1, ly - 0.15, top, extra=0.5)
        at("MarketPouch", p, th, -0.5, ly + 0.05, top)


def write_placements(path):
    """Writes every placed model and lantern light (Blender coordinates) for the Unity Market builder,
    MarketYardForge, so the Unity prefab and this render are placed by the same code."""
    import json
    data = {
        "models": [{"key": k, "position": list(p), "zDegrees": r} for k, p, r in PLACED],
        "lights": [{"position": [x, y, z + 0.17], "watts": w} for x, y, z, w in LANTERNS],
    }
    with open(path, "w") as f:
        json.dump(data, f, indent=1)
    print("wrote", path, len(PLACED), "models", len(LANTERNS), "lights")


def world_setup():
    rp.clear_scene()
    world = bpy.data.worlds.new("Night")
    world.use_nodes = True
    bpy.context.scene.world = world
    bg = world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.003, 0.004, 0.011, 1)
    bg.inputs[1].default_value = 1.0
    moon = bpy.data.lights.new("Moon", "SUN")
    moon.energy, moon.color, moon.angle = 0.12, (0.55, 0.65, 1.0), math.radians(3)
    mo = bpy.data.objects.new("Moon", moon)
    bpy.context.collection.objects.link(mo)
    mo.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = argv[argv.index("--out") + 1] if "--out" in argv else OUT
    world_setup()
    place("MarketYard", 0, 0, 0, glow=True)
    # well lantern, post lanterns
    LANTERNS.append((mb.WELL_LANTERN[0], mb.WELL_LANTERN[1], FL + mb.WELL_LANTERN[2] - 0.41, 140))
    for p in mb.LANTERN_POSTS:
        hx, hy, hz = mb.post_hook(p)
        LANTERNS.append((hx, hy, FL + hz - 0.37, 160))

    g, th = STALLS["gold"], facing(STALLS["gold"])
    at("MarketGoldsmithStall", g, th, 0, 0, 0)
    counter_stuff(g, th, -0.6, with_scales=True)
    at("MarketSlateBoard", g, th, -1.9, -1.5, 0)
    at("MarketAnvil", g, th, 1.9, -0.6, 0)
    lantern_at(g, th, 0.9, -0.4, 1.85)

    d, th = STALLS["pardoner"], facing(STALLS["pardoner"])
    at("MarketPardonerBooth", d, th, 0, 0, 0)
    counter_stuff(d, th, -0.95)
    at("MarketSlateBoard", d, th, 1.9, -1.8, 0)
    lantern_at(d, th, 0.9, -0.85, 2.0)

    a, th = STALLS["antiq"], facing(STALLS["antiq"])
    at("MarketAntiquarianCabinet", a, th, 0, 0, 0)
    counter_stuff(a, th, -1.25)
    at("MarketSlateBoard", a, th, -1.9, -1.9, 0)

    f, th = STALLS["fence"], facing(STALLS["fence"])
    place("MarketFenceCart", CART[0], CART[1], FL, 0)
    counter_stuff(f, th, 0.0)
    at("MarketSlateBoard", f, th, 1.9, -0.9, 0)
    LANTERNS.append((CART[0] + 0.2, CART[1] + 0.85, FL + 1.0, 70))      # a lantern hooked on the cart's tail

    for i, (x, y, z, w) in enumerate(LANTERNS):
        place("MarketLantern", x, y, z, glow=True)
        rls.point("L%d" % i, (x, y, z + 0.17), w, WARM, 0.08)
    if "--placements" in argv:
        write_placements(argv[argv.index("--placements") + 1])
        return
    rls.add_human(*HUMAN)

    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.lens = 17
    bpy.context.collection.objects.link(cam)
    eye, target = CAMERA, CAMERA_TARGET
    if os.environ.get("MARKET_CAM"):
        v = [float(t) for t in os.environ["MARKET_CAM"].split(",")]
        eye, target = v[:3], v[3:]
    cam.location = eye
    rls.look(cam, target)
    bpy.context.scene.camera = cam
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 128
    sc.cycles.use_denoising = False   # this Blender build has no OpenImageDenoise
    sc.cycles.max_bounces = 6
    sc.cycles.sample_clamp_direct = 10.0
    sc.cycles.sample_clamp_indirect = 3.0
    sc.view_settings.view_transform = "Filmic"
    sc.view_settings.look = "None"
    res = os.environ.get("MARKET_PREVIEW_RES")
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
