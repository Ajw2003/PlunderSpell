"""
Headless build + validate + export for every prop in asset_specs.ALL_SPECS.

    blender -b -P Tools/AssetPipeline/build_assets.py

For each asset: build geometry with a bmesh, run validate_in_blender's gate
BEFORE export (transforms / manifold / quads / budget / UV), export FBX for
Unity (Assets/_Project/Art/Models/<subdir>/<Key>.fbx, textures embedded so
Unity never sees a missing material reference), export a companion GLB into
the scratch dir for the standalone trimesh validator, then re-import that
GLB and diff vertex/triangle counts against the pre-export mesh to catch
anything the exporter silently dropped.

An asset whose mesh fingerprint still matches asset_manifest.json is
validated but NOT re-exported, so a no-op rebuild leaves the committed
FBX files untouched (see manifest.py for why that matters). Pass --force
after `--` to re-export everything regardless.

Exits 1 if any asset fails any check, printing every issue found so the
next edit knows exactly what to fix.
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import asset_specs  # noqa: E402
import builders  # noqa: E402
import castle_builders  # noqa: E402
import manifest  # noqa: E402
import mesh_kit as mk  # noqa: E402
import validate_in_blender as val  # noqa: E402

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MODELS_ROOT = os.path.join(REPO_ROOT, "Assets", "_Project", "Art", "Models")
PALETTE_PNG = os.path.join(REPO_ROOT, "Assets", "_Project", "Art", "Textures", "PlunderspellPalette.png")
SCRATCH_GLB_DIR = os.environ.get("PLUNDERSPELL_SCRATCH_GLB", "/tmp/plunderspell_glb")


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def build_one(spec) -> tuple[object, list[str]]:
    bm = mk.new_bmesh()
    uv = bm.loops.layers.uv.new("UVMap")
    mk.reset_material_order()

    builder_fn = getattr(builders, spec["builder"], None) or getattr(castle_builders, spec["builder"])
    builder_fn(bm, uv)

    obj = mk.finalize_to_object(bm, spec["key"], mk.used_pigments(), PALETTE_PNG)
    issues = val.validate_object(obj, spec["tri_budget"])
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    return obj, issues, tris, manifest.fingerprint_mesh(obj)


def fbx_path_for(spec) -> str:
    return os.path.join(MODELS_ROOT, spec["subdir"], f"{spec['key']}.fbx")


def export_fbx(obj, spec) -> str:
    out_path = fbx_path_for(spec)
    os.makedirs(os.path.dirname(out_path), exist_ok=True)

    for o in bpy.context.selected_objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    bpy.ops.export_scene.fbx(
        filepath=out_path,
        use_selection=True,
        embed_textures=True,
        path_mode="COPY",
        object_types={"MESH"},
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        axis_forward="-Z",
        axis_up="Y",
    )
    return out_path


def export_glb_for_validation(obj, spec) -> str:
    os.makedirs(SCRATCH_GLB_DIR, exist_ok=True)
    out_path = os.path.join(SCRATCH_GLB_DIR, f"{spec['key']}.glb")

    for o in bpy.context.selected_objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    bpy.ops.export_scene.gltf(
        filepath=out_path,
        use_selection=True,
        export_format="GLB",
    )
    return out_path


def reimport_and_diff(fbx_path: str, obj) -> list[str]:
    """Re-import the exported FBX into a scratch collection and compare
    triangle/vertex counts to the pre-export mesh, so a silent exporter
    drop (a face, the UV map, the material) shows up as a real failure."""
    issues = []
    existing = set(bpy.data.objects.keys())
    bpy.ops.import_scene.fbx(filepath=fbx_path)
    imported = [o for o in bpy.data.objects if o.name not in existing and o.type == "MESH"]

    if not imported:
        issues.append("re-import produced no mesh object")
        return issues

    imp_obj = imported[0]
    imp_mesh = imp_obj.data
    orig_mesh = obj.data

    orig_tris = sum(len(p.vertices) - 2 for p in orig_mesh.polygons)
    imp_tris = sum(len(p.vertices) - 2 for p in imp_mesh.polygons)
    if orig_tris != imp_tris:
        issues.append(f"export/import tri mismatch: built {orig_tris}, re-imported {imp_tris}")

    if len(imp_mesh.vertices) == 0:
        issues.append("re-imported mesh has zero vertices")

    if not imp_mesh.materials or any(m is None for m in imp_mesh.materials):
        issues.append("re-imported mesh missing material reference")
    else:
        for m in imp_mesh.materials:
            has_image = any(
                n.type == "TEX_IMAGE" and n.image is not None
                for n in (m.node_tree.nodes if m.use_nodes else [])
            )
            if not has_image:
                issues.append(f"re-imported material '{m.name}' has no resolved texture image")

    if not imp_mesh.uv_layers:
        issues.append("re-imported mesh has no UV layer")

    for o in imported:
        bpy.data.objects.remove(o, do_unlink=True)
    return issues


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    force = "--force" in argv

    if os.environ.get("PYTHONHASHSEED") != "0":
        print("WARNING: PYTHONHASHSEED is not 0 — Blender's FBX exporter derives "
              "object UIDs from salted string hashes, so exports will differ "
              "byte-for-byte between runs. Use run_pipeline.sh.")

    clear_scene()
    recorded = manifest.load()
    results = []
    any_failed = False

    for spec in asset_specs.ALL_SPECS:
        key = spec["key"]
        obj, issues, tris, fingerprint = build_one(spec)
        fbx_path = fbx_path_for(spec)

        unchanged = (
            not force
            and os.path.isfile(fbx_path)
            and recorded.get(key, {}).get("content") == fingerprint
        )
        if not unchanged:
            export_fbx(obj, spec)
        # the GLB is scratch-only (gate 2 reads it), so always refresh it
        export_glb_for_validation(obj, spec)
        issues += reimport_and_diff(fbx_path, obj)

        ok = len(issues) == 0
        any_failed |= not ok
        if ok:
            entry = dict(recorded.get(key, {}))
            entry.update(content=fingerprint, tris=tris, budget=spec["tri_budget"])
            recorded[key] = entry
        results.append((key, ok, issues, tris, spec["tri_budget"], unchanged))

    manifest.save(recorded)

    print("\n" + "=" * 70)
    print("PLUNDERSPELL ASSET PIPELINE — build report")
    print("=" * 70)
    for key, ok, issues, tris, budget, unchanged in results:
        status = "PASS" if ok else "FAIL"
        note = "unchanged" if unchanged else "EXPORTED"
        print(f"[{status}] {key:20s} {tris:5d} tris / {budget:4d} budget   {note}")
        for issue in issues:
            print(f"         - {issue}")
    n_pass = sum(1 for r in results if r[1])
    n_written = sum(1 for r in results if not r[5])
    print("-" * 70)
    print(f"{n_pass}/{len(results)} assets passed all checks; {n_written} re-exported.")
    print("=" * 70)

    sys.exit(1 if any_failed else 0)


if __name__ == "__main__":
    main()
