"""Turn an Archetype description into a rigged, textured, exported game asset."""

from __future__ import annotations

import math
import os

import bmesh
import bpy
from mathutils import Vector

from . import materials
from .archetypes import Archetype
from .parts import BONE_LAYER, MATERIALS, build_bmesh


def reset_scene() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def _expand_bones(bones: list[dict]) -> list[dict]:
    """Expand `mirror=True` bone records into a matching .R bone across the YZ plane."""
    expanded = []
    for bone in bones:
        expanded.append({k: v for k, v in bone.items() if k != "mirror"})
        if bone.get("mirror"):
            if not bone["name"].endswith(".L"):
                raise ValueError(f"mirrored bone {bone['name']!r} must end in '.L'")
            flipped = dict(bone)
            flipped.pop("mirror")
            flipped["name"] = bone["name"][:-2] + ".R"
            flipped["head"] = (-bone["head"][0], bone["head"][1], bone["head"][2])
            flipped["tail"] = (-bone["tail"][0], bone["tail"][1], bone["tail"][2])
            parent = bone.get("parent")
            if parent and parent.endswith(".L"):
                flipped["parent"] = parent[:-2] + ".R"
            expanded.append(flipped)
    return expanded


def build_mesh_object(arch: Archetype,
                      authoring: list[bpy.types.Material]) -> bpy.types.Object:
    """Create the joined mesh with its material slots and rigid vertex groups.

    The authoring materials must be attached before face indices are written, or
    Blender clamps every family onto slot 0 — see "Material indices are clamped to
    the number of slots" in docs/4-systems/enemy-asset-pipeline.md.
    """
    bm, bone_names = build_bmesh(arch.parts)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.verts.index_update()
    bm.faces.index_update()

    bone_layer = bm.verts.layers.int[BONE_LAYER]
    groups: dict[str, list[int]] = {name: [] for name in bone_names}
    for vert in bm.verts:
        groups[bone_names[vert[bone_layer]]].append(vert.index)
    face_families = [face.material_index for face in bm.faces]

    mesh = bpy.data.meshes.new(arch.name)
    for mat in authoring:
        mesh.materials.append(mat)
    bm.to_mesh(mesh)
    bm.free()
    mesh.polygons.foreach_set("material_index", face_families)
    mesh.update()

    obj = bpy.data.objects.new(arch.name, mesh)
    bpy.context.collection.objects.link(obj)

    for bone, indices in groups.items():
        obj.vertex_groups.new(name=bone).add(indices, 1.0, "REPLACE")

    # The mesh is authored directly in world space, so the object origin already sits
    # at the ground-plane centre that Unity expects as the pivot.
    obj.location = (0.0, 0.0, 0.0)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    obj.scale = (1.0, 1.0, 1.0)
    return obj


def _dissolve_degenerate(obj: bpy.types.Object, distance: float = 1e-4) -> None:
    """Collapse the slivers a clamped bevel leaves on very small features."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.dissolve_degenerate(bm, dist=distance, edges=bm.edges)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def finish_geometry(obj: bpy.types.Object, arch: Archetype) -> None:
    """Bevel, shade and unwrap. Vertex groups are already set, and bevel preserves them."""
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)

    if arch.bevel > 0.0:
        bevel = obj.modifiers.new("Bevel", "BEVEL")
        bevel.width = arch.bevel
        bevel.segments = 1
        bevel.limit_method = "ANGLE"
        bevel.angle_limit = math.radians(32.0)
        bevel.miter_outer = "MITER_ARC"
        bevel.harden_normals = False
        # Small trim pieces are thinner than the bevel width; without clamping the
        # bevel would eat straight through them and leave zero-area faces behind.
        bevel.use_clamp_overlap = True
        bpy.ops.object.modifier_apply(modifier=bevel.name)
        _dissolve_degenerate(obj)

    bpy.ops.object.shade_smooth()
    try:
        bpy.ops.object.shade_auto_smooth(angle=math.radians(34.0))
        for modifier in list(obj.modifiers):
            if modifier.type == "NODES":
                bpy.ops.object.modifier_apply(modifier=modifier.name)
    except (AttributeError, RuntimeError):
        # Older Blender exposes auto-smooth as a mesh flag rather than a modifier.
        bpy.ops.object.shade_flat()

    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.006)
    bpy.ops.object.mode_set(mode="OBJECT")


def build_armature(arch: Archetype, mesh_obj: bpy.types.Object) -> bpy.types.Object:
    """Create the rig and bind it. Weights are rigid — every construct is rigid."""
    armature_data = bpy.data.armatures.new(f"{arch.name}_Rig")
    rig = bpy.data.objects.new(f"{arch.name}_Rig", armature_data)
    bpy.context.collection.objects.link(rig)

    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    created = {}
    for spec in _expand_bones(arch.bones):
        bone = armature_data.edit_bones.new(spec["name"])
        bone.head = Vector(spec["head"])
        bone.tail = Vector(spec["tail"])
        if bone.length < 1e-4:
            raise ValueError(f"bone {spec['name']!r} has zero length")
        created[spec["name"]] = bone
    for spec in _expand_bones(arch.bones):
        if spec.get("parent"):
            created[spec["name"]].parent = created[spec["parent"]]
    bpy.ops.object.mode_set(mode="OBJECT")

    known = set(created)
    unbound = {g.name for g in mesh_obj.vertex_groups} - known
    if unbound:
        raise ValueError(f"{arch.name}: vertex groups with no matching bone: {sorted(unbound)}")

    mesh_obj.parent = rig
    modifier = mesh_obj.modifiers.new("Armature", "ARMATURE")
    modifier.object = rig
    return rig


def apply_smooth_weights(mesh_obj: bpy.types.Object, rig: bpy.types.Object,
                         iterations: int = 3, max_influences: int = 4) -> dict:
    """Replace the rigid per-part weights with blended ones, keeping rigid as a net.

    Rigid weights are right for a construct and wrong for a man in wool: at every
    joint the parts would swing as separate blocks. Blender's heat weighting blends
    them, but it can fail on a mesh of disjoint islands and leave vertices with no
    influence at all, which would drop them on the floor when the rig moves. So the
    per-part assignment is kept and restored for exactly those vertices.
    """
    rigid = [[g.group for g in v.groups] for v in mesh_obj.data.vertices]
    group_names = [g.name for g in mesh_obj.vertex_groups]

    bpy.ops.object.select_all(action="DESELECT")
    mesh_obj.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    try:
        bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    except RuntimeError as error:
        # Heat weighting refused outright; the rigid weights already on the mesh
        # are a correct, if stiff, binding, so the build continues.
        return {"auto_weights": False, "reason": str(error), "restored": 0}

    lookup = {name: mesh_obj.vertex_groups.get(name) for name in group_names}
    restored = 0
    for vert in mesh_obj.data.vertices:
        if vert.groups and sum(g.weight for g in vert.groups) > 1e-4:
            continue
        for group_index in rigid[vert.index]:
            group = lookup.get(group_names[group_index])
            if group is not None:
                group.add([vert.index], 1.0, "REPLACE")
                restored += 1

    bpy.context.view_layer.objects.active = mesh_obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    if iterations:
        bpy.ops.object.vertex_group_smooth(group_select_mode="ALL",
                                           factor=0.5, repeat=iterations)
    # Unity blends at most four bones per vertex; trimming here rather than letting
    # the importer silently drop the smallest influences keeps the two in agreement.
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=max_influences)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)
    bpy.ops.object.mode_set(mode="OBJECT")

    influences = [len([g for g in v.groups if g.weight > 1e-4])
                  for v in mesh_obj.data.vertices]
    return {"auto_weights": True, "restored": restored,
            "max_influences": max(influences) if influences else 0,
            "mean_influences": round(sum(influences) / max(1, len(influences)), 2)}


def texture_and_bake(obj: bpy.types.Object, arch: Archetype,
                     authoring: list[bpy.types.Material],
                     texture_dir: str, resolution: int) -> bpy.types.Material:
    """Bake the five authoring families down to one texture set and one material."""
    os.makedirs(texture_dir, exist_ok=True)
    expected = {MATERIALS[part.mat] for part in arch.parts}
    present = {face.material_index for face in obj.data.polygons}
    if present != expected:
        raise RuntimeError(
            f"{arch.name}: surface families on the mesh {sorted(present)} do not match "
            f"the families its parts declare {sorted(expected)} — the bake would be wrong")

    baked = materials.bake_channels(obj, authoring, arch.name, texture_dir, resolution)
    packed = materials.pack_channel_maps(arch.name, baked, texture_dir)

    obj.data.materials.clear()
    final = materials.build_baked_material(arch.name, baked, packed)
    obj.data.materials.append(final)
    # Every face now points at slot 0, the only slot that survives.
    obj.data.polygons.foreach_set("material_index", [0] * len(obj.data.polygons))
    for mat in authoring:
        bpy.data.materials.remove(mat)
    return final


def export(obj: bpy.types.Object, rig: bpy.types.Object, arch: Archetype, model_dir: str) -> dict:
    os.makedirs(model_dir, exist_ok=True)
    gltf_dir = os.path.join(model_dir, "glTF")
    os.makedirs(gltf_dir, exist_ok=True)

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig

    fbx_path = os.path.join(model_dir, f"{arch.name}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        bake_anim=False,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=False,
        # RELATIVE, not COPY: the textures already sit in Textures/ next to the FBX,
        # and COPY duplicates every map into a parallel .fbm folder.
        path_mode="RELATIVE",
        embed_textures=False,
        apply_scale_options="FBX_SCALE_NONE",
        axis_forward="-Z",
        axis_up="Y",
    )

    gltf_path = os.path.join(gltf_dir, arch.name)
    bpy.ops.export_scene.gltf(
        filepath=gltf_path,
        export_format="GLTF_SEPARATE",
        use_selection=True,
        export_apply=False,
        export_yup=True,
    )

    blend_path = os.path.join(model_dir, f"{arch.name}.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path, copy=True)
    return {"fbx": fbx_path, "gltf": gltf_path + ".gltf", "blend": blend_path}
