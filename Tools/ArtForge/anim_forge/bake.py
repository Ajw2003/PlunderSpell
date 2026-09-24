"""Write solved frames into Blender Actions, and export animation-only FBX.

Keys every bone on every frame (rotation_quaternion + location), as Blender pose
basis values derived from the armature-space pose matrix each bone should have:

    basis = rest^-1 @ parent_rest @ parent_pose^-1 @ pose

so rotations, the Hips bob and a detached prop's free movement all go through the
same formula. Quaternion signs are kept continuous between frames (no flips for
the FBX baker to interpolate the long way round).
"""

from __future__ import annotations

import json
import os

import bpy
from mathutils import Matrix, Quaternion, Vector

from .skeleton import Skeleton
from .solve import Solved


def pose_matrices(skel: Skeleton, solved: Solved) -> dict[str, Matrix]:
    posed = skel.fk(solved.Qx, solved.hips, solved.free)
    out = {}
    for name in skel.order:
        head = posed.heads[name]
        rot = (solved.Qx.get(name, posed.world[name]) @ skel.rest_q[name]).to_matrix().to_4x4()
        out[name] = Matrix.Translation(head) @ rot
    return out


def rest_matrix(skel: Skeleton, name: str) -> Matrix:
    return Matrix.Translation(skel.head[name]) @ skel.rest_q[name].to_matrix().to_4x4()


def basis_matrices(skel: Skeleton, solved: Solved) -> dict[str, Matrix]:
    pose = pose_matrices(skel, solved)
    out = {}
    for name in skel.order:
        par = skel.parent[name]
        rest = rest_matrix(skel, name)
        if par:
            out[name] = rest.inverted() @ rest_matrix(skel, par) @ pose[par].inverted() @ pose[name]
        else:
            out[name] = rest.inverted() @ pose[name]
    return out


def apply_frame(rig: bpy.types.Object, skel: Skeleton, solved: Solved) -> None:
    """Set the rig's pose (no keys) — for renders and checks."""
    for name, m in basis_matrices(skel, solved).items():
        pb = rig.pose.bones[name]
        pb.rotation_mode = "QUATERNION"
        loc, rot, _scale = m.decompose()
        pb.location = loc
        pb.rotation_quaternion = rot
        pb.scale = (1.0, 1.0, 1.0)


def bake_action(rig: bpy.types.Object, skel: Skeleton, name: str,
                frames: list[Solved], fps: int) -> bpy.types.Action:
    """One Action with a key per frame per bone. Frame i is at scene frame i."""
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    if rig.animation_data is None:
        rig.animation_data_create()
    rig.animation_data.action = action
    previous: dict[str, Quaternion] = {}
    for i, solved in enumerate(frames):
        for bone, m in basis_matrices(skel, solved).items():
            pb = rig.pose.bones[bone]
            pb.rotation_mode = "QUATERNION"
            loc, rot, _scale = m.decompose()
            prev = previous.get(bone)
            if prev is not None and prev.dot(rot) < 0.0:
                rot = -rot
            previous[bone] = rot.copy()
            pb.location = loc
            pb.rotation_quaternion = rot
            pb.keyframe_insert("location", frame=i, group=bone)
            pb.keyframe_insert("rotation_quaternion", frame=i, group=bone)
    for fc in _fcurves(action):
        for kp in fc.keyframe_points:
            kp.interpolation = "LINEAR"
    action.frame_range = (0, len(frames) - 1)
    return action


def _fcurves(action):
    """Action F-curves across Blender versions (5.0 uses layered actions)."""
    if hasattr(action, "fcurves") and action.fcurves is not None:
        try:
            return list(action.fcurves)
        except TypeError:
            pass
    out = []
    for layer in getattr(action, "layers", []):
        for strip in layer.strips:
            for bag in strip.channelbags:
                out.extend(bag.fcurves)
    return out


def export_fbx(rig: bpy.types.Object, actions: list[bpy.types.Action], path: str, fps: int) -> None:
    """Animation-only FBX: the armature (no mesh) and one take per action, named
    after the action. Same axes as the models (-Z forward, Y up), no leaf bones."""
    scene = bpy.context.scene
    scene.render.fps = fps
    scene.frame_start = 0
    scene.frame_end = max(int(a.frame_range[1]) for a in actions)
    # One NLA track per action; the exporter writes each strip as its own take.
    rig.animation_data.action = None
    for track in list(rig.animation_data.nla_tracks):
        rig.animation_data.nla_tracks.remove(track)
    for action in actions:
        track = rig.animation_data.nla_tracks.new()
        track.name = action.name
        strip = track.strips.new(action.name, 0, action)
        strip.name = action.name
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"ARMATURE"},
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=True,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        apply_scale_options="FBX_SCALE_NONE",
        axis_forward="-Z",
        axis_up="Y",
    )


def read_back(path: str) -> dict[str, tuple[int, int]]:
    """Import an FBX into a fresh scene and list its takes: {name: (first, last) frame}."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    out = {}
    for action in bpy.data.actions:
        a, b = action.frame_range
        out[action.name] = (int(round(a)), int(round(b)))
    return out
