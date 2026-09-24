"""Clip review sheets and MP4s on the Lantern Warden, beside its concept.

docs/art/anim/<clip>.png:
    left   the warden's concept sheet
    right  8 frames of the clip (Cycles, the review studio of render.py), a fixed
           three-quarter camera framing the whole clip, each frame labelled with its
           time and any event on it
    below  (locomotion) the foot-contact trace: every sole point's path in the
           WORLD, the agent's travel added back, side view. A planted foot is a
           single dot; a sliding one smears along the travel axis.
           (weapon clips) the left fist's distance to the haft per frame.
    caption  length, loop, events, measured slide / haft error / floor penetration
docs/art/anim/<clip>.mp4: every frame at 30 fps (two cycles for a loop), H.264,
    written by Blender's own FFmpeg (the Playwright ffmpeg here encodes only VP8/WebM).
"""

from __future__ import annotations

import math
import os
import shutil
import tempfile

import bpy
from mathutils import Vector

from . import FPS, REVIEW_DIR, WARDEN_CONCEPT, bake

try:
    from PIL import Image, ImageDraw
except ImportError as error:
    raise SystemExit("anim review needs Pillow: pip install pillow") from error

import render as studio   # Tools/ArtForge/render.py: _configure, _world, _studio, _ground, _camera, _font

BG = "#14120E"
INK = "#DCD2BA"
DIM = "#9A9078"
FOOT_COLOURS = {"L": (95, 162, 136), "R": (196, 84, 46)}   # verdigris, madder


def _bounds(skel, frames, rig_obj_offsets) -> tuple[Vector, Vector]:
    lo = Vector((1e9, 1e9, 0.0))
    hi = Vector((-1e9, -1e9, -1e9))
    for s, (yaw, lift) in zip(frames, rig_obj_offsets):
        posed = skel.fk(s.Qx, s.hips, s.free)
        rot = Vector((0, 0, 1)).to_track_quat("Z", "Y")  # identity
        for name in skel.order:
            for p in (posed.heads[name], posed.tail(name)):
                q = p.copy()
                if yaw:
                    c, sn = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
                    q = Vector((q.x * c - q.y * sn, q.x * sn + q.y * c, q.z))
                q.z += lift
                lo = Vector((min(lo.x, q.x), min(lo.y, q.y), min(lo.z, q.z)))
                hi = Vector((max(hi.x, q.x), max(hi.y, q.y), max(hi.z, q.z)))
    pad = Vector((0.22, 0.22, 0.10))
    lo, hi = lo - pad, hi + pad
    lo.z = min(lo.z, 0.0)
    return lo, hi


class Stage:
    """The opened warden .blend with the review studio around it."""

    def __init__(self, rig_obj, mesh_obj, skel):
        self.rig, self.mesh, self.skel = rig_obj, mesh_obj, skel
        self.framed = None

    def frame(self, lo, hi, resolution, samples):
        # render.py's studio, lit and framed once per clip
        for o in [o for o in bpy.context.scene.objects if o.type in {"LIGHT", "CAMERA"}
                  or o.name.startswith("ReviewGround")]:
            bpy.data.objects.remove(o, do_unlink=True)
        studio._configure(resolution, samples)
        studio._world()
        studio._studio((lo + hi) / 2.0, (hi - lo).length)
        studio._ground((hi - lo).length)
        studio._camera(lo, hi, -35.0, 12.0)

    def pose(self, solved, yaw=0.0, lift=0.0):
        bake.apply_frame(self.rig, self.skel, solved)
        self.rig.rotation_euler = (0.0, 0.0, math.radians(yaw))
        self.rig.location = (0.0, 0.0, lift)
        bpy.context.view_layer.update()

    def shoot(self, path, samples=None):
        if samples:
            bpy.context.scene.cycles.samples = samples
        bpy.context.scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        if not os.path.exists(path):
            raise RuntimeError(f"render did not write {path}")
        return path


def event_at(events, t_norm, length) -> list[str]:
    half = 0.5 / FPS / max(length, 1e-6)
    return [e["type"] + (f" {e['param'].get('foot') or e['param'].get('prop')}" if e.get("param") else "")
            for e in events if abs(e["t"] - t_norm) <= half + 1e-9]


def still_indices(n_frames: int, loop: bool) -> list[int]:
    last = n_frames - 1
    if loop:
        return [round(last * i / 8) for i in range(8)]
    return [round(last * i / 7) for i in range(8)]


def trace_panel(trace: dict, speed: float, width: int, height: int, slide: dict) -> Image.Image:
    img = Image.new("RGB", (width, height), BG)
    d = ImageDraw.Draw(img)
    pts = [p for seq in trace.values() for _t, p in seq]
    ys = [-p.y for p in pts]          # travel direction to the right
    y0, y1 = min(ys) - 0.1, max(ys) + 0.1
    zmax = max(p.z for p in pts) + 0.02
    margin = 50
    sx = (width - 2 * margin) / (y1 - y0)
    zscale = min(4.0, (height - 70) / max(zmax, 0.05) / sx)   # vertical exaggeration
    base = height - 34

    def xy(p):
        return margin + (-p.y - y0) * sx, base - p.z * sx * zscale
    d.line([(margin, base), (width - margin, base)], fill="#4A4436", width=1)
    for m in range(int(math.floor(y0)), int(math.ceil(y1)) + 1):
        x = margin + (m - y0) * sx
        if margin <= x <= width - margin:
            d.line([(x, base), (x, base + 6)], fill="#635C4C")
            d.text((x + 3, base + 6), f"{m} m", fill="#635C4C", font=studio._font(13))
    for (side, k), seq in trace.items():
        col = FOOT_COLOURS[side]
        faint = tuple(int(c * 0.45) for c in col)
        d.line([xy(p) for _t, p in seq], fill=faint, width=1)
        for _t, p in seq:
            if p.z < 0.004:
                x, y = xy(p)
                r = 3 if k == "heel" else 2
                d.ellipse([x - r, y - r, x + r, y + r], fill=col)
    font = studio._font(15)
    d.text((margin, 8), f"FOOT CONTACT TRACE  ·  world space, agent at {speed:.2f} m/s added back  ·  "
                        f"side view, height x{zscale:.1f}  ·  dots = sole point on the floor "
                        f"(heel big, ball/toe small; green L, red R): a planted foot is one dot, "
                        f"a slide is a smear", fill=DIM, font=font)
    d.text((margin, 28), "max slide per contact: " + "  ".join(
        f"{k} {v['max_slide_m'] * 100:.1f} cm" for k, v in slide.items()), fill=INK, font=font)
    return img


def haft_panel(errors: list[float], raw: list[float], weights: list[float], width: int,
               height: int) -> Image.Image:
    img = Image.new("RGB", (width, height), BG)
    d = ImageDraw.Draw(img)
    margin = 50
    top = 50
    n = len(errors)
    peak = max(max(raw, default=0.0), 0.05)
    font = studio._font(15)
    d.text((margin, 8), "LEFT FIST TO HAFT  ·  distance from the left fist centre to the glaive's "
                        "axis per frame, on the warden. Red: retargeted only (what Humanoid gives "
                        "before IK). Green: with the hand IK to the grip target. Grey band: left "
                        "hand meant to be on the haft.", fill=DIM, font=font)
    h = height - top - 30

    def xy(i, v):
        return margin + (width - 2 * margin) * i / max(1, n - 1), top + h - h * v / peak
    for i, w in enumerate(weights):
        if w > 0.99:
            x = xy(i, 0)[0]
            d.line([(x, top), (x, top + h)], fill="#262119", width=max(1, (width - 2 * margin) // max(1, n)))
    d.line([xy(i, v) for i, v in enumerate(raw)], fill=FOOT_COLOURS["R"], width=2)
    d.line([xy(i, v) for i, v in enumerate(errors)], fill=FOOT_COLOURS["L"], width=2)
    d.line([(margin, top + h), (width - margin, top + h)], fill="#4A4436")
    d.text((margin, top + h + 6), f"0 cm  ·  top {peak * 100:.0f} cm  ·  worst on-haft "
                                  f"with IK {max([e for e, w in zip(errors, weights) if w > 0.99], default=0) * 100:.1f} cm, "
                                  f"retarget only {max([e for e, w in zip(raw, weights) if w > 0.99], default=0) * 100:.1f} cm",
           fill=INK, font=font)
    return img


def compose(clip_id: str, tiles: list[tuple[str, str]], caption: list[str], panel,
            out_path: str, tile: int) -> str:
    gap = 8
    grid_w, grid_h = tile * 4 + gap * 3, tile * 2 + gap
    concept = Image.open(WARDEN_CONCEPT).convert("RGB")
    concept = concept.resize((int(concept.width * grid_h / concept.height), grid_h), Image.LANCZOS)
    panel_h = panel.height if panel is not None else 0
    cap_h = 30 + 24 * len(caption)
    width = gap + concept.width + gap + grid_w + gap
    height = gap + grid_h + (gap + panel_h if panel is not None else 0) + gap + cap_h
    sheet = Image.new("RGB", (width, height), BG)
    sheet.paste(concept, (gap, gap))
    d = ImageDraw.Draw(sheet)
    left = gap + concept.width + gap
    lf = studio._font(max(13, tile // 26))
    for i, (path, label) in enumerate(tiles):
        r, c = divmod(i, 4)
        x, y = left + c * (tile + gap), gap + r * (tile + gap)
        sheet.paste(Image.open(path).convert("RGB"), (x, y))
        d.text((x + 10, y + 8), label, fill=DIM, font=lf)
    y = gap + grid_h + gap
    if panel is not None:
        sheet.paste(panel.resize((width - 2 * gap, panel_h)), (gap, y))
        y += panel_h + gap
    d.text((gap + 6, y + 4), caption[0], fill=INK, font=studio._font(22))
    for k, line in enumerate(caption[1:]):
        d.text((gap + 6, y + 34 + 24 * k), line, fill=DIM, font=studio._font(16))
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    sheet.save(out_path, optimize=True)
    return out_path


def movie(stage: Stage, frames, offsets, events, length, loop, out_path, resolution=360,
          samples=10, label="") -> str:
    """Render every frame (twice round for a loop), stamp time and events, encode
    H.264 MP4 with Blender's FFmpeg through the sequencer."""
    scratch = tempfile.mkdtemp(prefix="animforge_mp4_")
    try:
        paths = []
        bpy.context.scene.render.resolution_x = resolution
        bpy.context.scene.render.resolution_y = resolution
        bpy.context.scene.cycles.samples = samples
        n = len(frames)
        for i, (s, (yaw, lift)) in enumerate(zip(frames, offsets)):
            if loop and i == n - 1:
                break       # the last frame of a loop repeats the first
            stage.pose(s, yaw, lift)
            p = stage.shoot(os.path.join(scratch, f"raw{i:04d}.png"))
            img = Image.open(p).convert("RGB")
            dd = ImageDraw.Draw(img)
            t = i / FPS
            tn = t / max(length, 1e-6)
            dd.text((8, 6), f"{label}  {t:4.2f}s  ({tn:4.2f})", fill=INK, font=studio._font(14))
            ev = event_at(events, tn, length)
            if ev:
                dd.text((8, 24), " + ".join(ev), fill=(230, 190, 90), font=studio._font(15))
            img.save(os.path.join(scratch, f"f{i:04d}.png"))
            paths.append(f"f{i:04d}.png")
        seq = paths * (2 if loop else 1)
        out = _encode(scratch, seq, out_path, resolution)
    finally:
        shutil.rmtree(scratch, ignore_errors=True)
    return out


def _encode(folder, names, out_path, resolution) -> str:
    """Encode a PNG list into an MP4 in a separate scene (keeps the review scene)."""
    main = bpy.context.window.scene if bpy.context.window else bpy.context.scene
    scene = bpy.data.scenes.new("AnimForgeEncode")
    try:
        scene.render.resolution_x = resolution
        scene.render.resolution_y = resolution
        scene.render.resolution_percentage = 100
        scene.render.fps = FPS
        scene.sequence_editor_create()
        se = scene.sequence_editor
        strips = se.strips if hasattr(se, "strips") else se.sequences
        # the image strip takes one directory; repeat frames by copying names
        strip = strips.new_image("frames", os.path.join(folder, names[0]), 1, 1)
        for name in names[1:]:
            strip.elements.append(name)
        scene.frame_start, scene.frame_end = 1, len(names)
        ims = scene.render.image_settings
        ims.media_type = "VIDEO"
        ims.file_format = "FFMPEG"
        ff = scene.render.ffmpeg
        ff.format, ff.codec = "MPEG4", "H264"
        ff.constant_rate_factor = "HIGH"
        scene.render.use_sequencer = True
        os.makedirs(os.path.dirname(out_path), exist_ok=True)
        if os.path.exists(out_path):
            os.remove(out_path)
        base = out_path[:-4]
        scene.render.filepath = base
        scene.render.use_file_extension = True
        bpy.ops.render.render(animation=True, scene=scene.name)
        made = [f for f in os.listdir(os.path.dirname(out_path))
                if f.startswith(os.path.basename(base)) and f.endswith(".mp4")]
        if not made:
            raise RuntimeError(f"no MP4 written for {out_path}")
        produced = os.path.join(os.path.dirname(out_path), sorted(made, key=len)[0])
        if produced != out_path:
            os.replace(produced, out_path)
    finally:
        bpy.data.scenes.remove(scene)
    return out_path
