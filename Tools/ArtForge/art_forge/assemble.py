"""Blueprint -> mesh object -> bevel/unwrap -> bake -> FBX, glTF and .blend.

Two paths share everything up to the bake:

- static (items, structures): one mesh, no rig, no vertex groups;
- rigged (enemies, `Blueprint.bones` set): EnemyForge's armature, heat weights and
  exporter, called unchanged, then ArtForge's per-part bind rules (rig.py) so props
  stay rigid and nothing is weighted to a bone it has no business following.
"""

from __future__ import annotations

import json
import math
import os
from types import SimpleNamespace

import bmesh
import bpy

from enemy_forge import assemble as ef_assemble
from enemy_forge import materials as ef_materials
from enemy_forge.assemble import _dissolve_degenerate, finish_geometry, reset_scene  # noqa: F401

from . import kit, materials
from . import rig as rigmod
from .blueprint import Blueprint


def build_object(bp: Blueprint, authoring: list[bpy.types.Material],
                 family_index: dict[str, int]) -> bpy.types.Object:
    """The joined mesh with its authoring material slots (and vertex groups if rigged).

    Material slots are attached before face indices are written — see "Material
    indices are clamped to the number of slots" in docs/systems/enemy-asset-pipeline.md.
    """
    bm, bone_names = kit.build_bmesh(bp.parts, family_index)
    # EnemyForge's own torus comes out inside-out; this fixes it, per island.
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.verts.index_update()
    bm.faces.index_update()

    bone_layer = bm.verts.layers.int[kit.BONE_LAYER]
    groups: dict[str, list[int]] = {name: [] for name in bone_names}
    for vert in bm.verts:
        groups[bone_names[vert[bone_layer]]].append(vert.index)
    face_families = [face.material_index for face in bm.faces]

    mesh = bpy.data.meshes.new(bp.name)
    for mat in authoring:
        mesh.materials.append(mat)
    bm.to_mesh(mesh)
    bm.free()
    mesh.polygons.foreach_set("material_index", face_families)
    mesh.update()

    obj = bpy.data.objects.new(bp.name, mesh)
    bpy.context.collection.objects.link(obj)
    if bp.rigged:
        for bone, indices in groups.items():
            obj.vertex_groups.new(name=bone).add(indices, 1.0, "REPLACE")
    obj.location = (0.0, 0.0, 0.0)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    obj.scale = (1.0, 1.0, 1.0)
    return obj


def bake(obj: bpy.types.Object, bp: Blueprint, authoring: list[bpy.types.Material],
         texture_dir: str, resolution: int) -> bpy.types.Material:
    """Bake every family into one texture set with EnemyForge's bake and packer."""
    os.makedirs(texture_dir, exist_ok=True)
    expected = set(range(len(authoring)))
    present = {face.material_index for face in obj.data.polygons}
    if present != expected:
        names = [m.name for m in authoring]
        raise RuntimeError(
            f"{bp.name}: material slots on the mesh {sorted(present)} do not match the "
            f"families the parts declare {sorted(expected)} ({names}) — a family's "
            f"faces were lost (a part too thin for the bevel?) and the bake would be wrong")

    baked = ef_materials.bake_channels(obj, authoring, bp.name, texture_dir, resolution)
    packed = ef_materials.pack_channel_maps(bp.name, baked, texture_dir)

    obj.data.materials.clear()
    final = ef_materials.build_baked_material(bp.name, baked, packed)
    obj.data.materials.append(final)
    obj.data.polygons.foreach_set("material_index", [0] * len(obj.data.polygons))
    for mat in authoring:
        ef_materials._CHANNEL_SOCKETS.pop(mat.name, None)
        bpy.data.materials.remove(mat)
    return final


BEVEL_ANGLE = 32.0   # degrees; the same angle limit EnemyForge's bevel uses


def _bevel(obj: bpy.types.Object, width: float) -> None:
    """EnemyForge's bevel, but weight-limited so parts can opt out (kit.NOBEVEL_LAYER).

    Settings match enemy_forge.assemble.finish_geometry: one segment, arc mitres,
    clamped overlap, then dissolve the slivers the clamp leaves ("A clamped bevel
    still leaves slivers" in docs/systems/enemy-asset-pipeline.md). The only
    difference is which edges qualify: sharper than BEVEL_ANGLE AND not on a part
    that set extras["bevel"] = False.
    """
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    nobevel = bm.faces.layers.int.get(kit.NOBEVEL_LAYER)
    weight = bm.edges.layers.float.get("bevel_weight_edge") or \
        bm.edges.layers.float.new("bevel_weight_edge")
    limit = math.radians(BEVEL_ANGLE)
    for edge in bm.edges:
        faces = edge.link_faces
        ok = (len(faces) == 2 and edge.calc_face_angle(0.0) > limit
              and not (nobevel and any(f[nobevel] for f in faces)))
        edge[weight] = 1.0 if ok else 0.0
    bm.to_mesh(obj.data)
    bm.free()

    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bevel = obj.modifiers.new("Bevel", "BEVEL")
    bevel.width = width
    bevel.segments = 1
    bevel.limit_method = "WEIGHT"
    bevel.edge_weight = "bevel_weight_edge"
    bevel.miter_outer = "MITER_ARC"
    bevel.harden_normals = False
    bevel.use_clamp_overlap = True
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    _dissolve_degenerate(obj)


def _force_smooth_parts(obj: bpy.types.Object) -> None:
    """Clear sharp edges inside parts flagged extras["smooth"] (see kit.SMOOTH_LAYER).

    Runs after EnemyForge's finish_geometry has applied its 34° smooth-by-angle,
    which is right for boxes and wrong for a four-sided cord.
    """
    mesh = obj.data
    flags = mesh.attributes.get(kit.SMOOTH_LAYER)
    if flags is None:
        raise RuntimeError(f"{obj.name}: the {kit.SMOOTH_LAYER} face layer did not "
                           f"survive geometry finishing")
    smooth_faces = {i for i, a in enumerate(flags.data) if a.value}
    if not smooth_faces:
        return
    sharp = mesh.attributes.get("sharp_edge")
    if sharp is None:
        return   # nothing is sharp, so nothing to clear
    edge_faces: dict[int, list[int]] = {}
    for poly in mesh.polygons:
        for key in poly.edge_keys:
            edge_faces.setdefault(key, []).append(poly.index)
    cleared = 0
    for edge in mesh.edges:
        faces = edge_faces.get(edge.key, [])
        if faces and all(f in smooth_faces for f in faces) and sharp.data[edge.index].value:
            sharp.data[edge.index].value = False
            cleared += 1
    mesh.update()


def prepare(bp: Blueprint) -> tuple[bpy.types.Object, list[tuple[str, dict]]]:
    """Fresh scene, authoring materials, mesh, bevel/shade/unwrap. Nothing baked yet."""
    reset_scene()
    ordered = bp.ordered_families()
    family_index = {key: i for i, (key, _fam) in enumerate(ordered)}
    authoring = materials.build_authoring_set(bp.name, ordered, bp.wear)
    obj = build_object(bp, authoring, family_index)
    if bp.bevel > 0.0:
        _bevel(obj, bp.bevel)
    # EnemyForge's shade + unwrap, with its own bevel off: ours already ran.
    finish_geometry(obj, SimpleNamespace(bevel=0.0))
    _force_smooth_parts(obj)
    obj["artforge_authoring"] = [m.name for m in authoring]
    return obj, ordered


def _authoring(obj) -> list[bpy.types.Material]:
    return [bpy.data.materials[name] for name in obj["artforge_authoring"]]


def export_static(obj: bpy.types.Object, bp: Blueprint, model_dir: str) -> dict:
    """FBX (Unity axes, EnemyForge's settings, mesh only), glTF (separate), .blend."""
    os.makedirs(model_dir, exist_ok=True)
    gltf_dir = os.path.join(model_dir, "glTF")
    os.makedirs(gltf_dir, exist_ok=True)

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    fbx_path = os.path.join(model_dir, f"{bp.name}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        object_types={"MESH"},
        bake_anim=False,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=False,
        path_mode="RELATIVE",
        embed_textures=False,
        apply_scale_options="FBX_SCALE_NONE",
        axis_forward="-Z",
        axis_up="Y",
    )

    gltf_path = os.path.join(gltf_dir, bp.name)
    bpy.ops.export_scene.gltf(
        filepath=gltf_path,
        export_format="GLTF_SEPARATE",
        use_selection=True,
        export_apply=False,
        export_yup=True,
    )

    blend_path = save_blend(bp, model_dir)
    return {"fbx": fbx_path, "gltf": gltf_path + ".gltf", "blend": blend_path}


def save_blend(bp: Blueprint, model_dir: str) -> str:
    """Save the .blend with texture paths relative to it, so it opens from any checkout.

    Done last: the FBX/glTF exporters resolve image paths themselves, and a "//"
    path in an unsaved session would resolve against the wrong directory.
    """
    for image in bpy.data.images:
        raw = image.filepath_raw
        if raw and not raw.startswith("//"):
            image.filepath_raw = bpy.path.relpath(bpy.path.abspath(raw), start=model_dir)
    blend_path = os.path.join(model_dir, f"{bp.name}.blend")
    # No .blend1 backups: a rebuild regenerates the file, the backup is only noise.
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=blend_path, copy=True)
    return blend_path


def build_static(bp: Blueprint, model_dir: str, resolution: int):
    """Items and structures. Returns (obj, exported file paths)."""
    obj, _ordered = prepare(bp)
    bake(obj, bp, _authoring(obj), os.path.join(model_dir, "Textures"), resolution)
    del obj["artforge_authoring"]
    return obj, None


def build_rigged(bp: Blueprint, model_dir: str, resolution: int):
    """Enemies: EnemyForge's armature + heat weights, then ArtForge's bind rules.

    Order matters: bevel and unwrap first (prepare), because the bevel adds vertices
    and interpolates vertex groups; then the armature and the weights on the final
    topology; then the bake (the armature modifier sits at rest, so it bakes the
    bind pose). Blueprint quacks like an EnemyForge Archetype for what
    build_armature reads (name, bones). Returns (obj, rig).
    """
    obj, _ordered = prepare(bp)
    rig = ef_assemble.build_armature(bp, obj)
    heat = rigmod.smooth_weights_with_retry(ef_assemble.apply_smooth_weights, obj, rig,
                                            max_influences=rigmod.MAX_INFLUENCES)
    rigmod.dedupe_armature_modifiers(obj, rig)
    rules = rigmod.apply_bind_rules(obj, bp)
    bake(obj, bp, _authoring(obj), os.path.join(model_dir, "Textures"), resolution)
    del obj["artforge_authoring"]
    rig["artforge_skin"] = json.dumps({"heat": heat, "rules": rules})
    rigmod.store_pose(rig, bp.pose)
    obj["artforge_skin"] = rig["artforge_skin"]
    return obj, rig


def export(obj, rig, bp: Blueprint, model_dir: str) -> dict:
    if rig is None:
        return export_static(obj, bp, model_dir)
    # EnemyForge's exporter: FBX with armature + mesh (Unity axes, no leaf bones),
    # glTF with the skin, and a .blend that save_blend then re-saves with relative
    # texture paths.
    bpy.context.view_layer.objects.active = rig
    for pose_bone in rig.pose.bones:   # export the bind pose, whatever happened before
        pose_bone.matrix_basis.identity()
    # EnemyForge's exporter saves the .blend itself; with backups on, a rebuild
    # leaves a stale <Name>.blend1 next to it.
    bpy.context.preferences.filepaths.save_version = 0
    files = ef_assemble.export(obj, rig, bp, model_dir)
    files["blend"] = save_blend(bp, model_dir)
    return files
