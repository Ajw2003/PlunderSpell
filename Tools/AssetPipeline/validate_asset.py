"""
Standalone, non-Blender validation pass — plain python3 + trimesh, no bpy.

Usage:
    python3 Tools/AssetPipeline/validate_asset.py <asset.glb> [tri_budget]
    python3 Tools/AssetPipeline/validate_asset.py --all

This is the second, independent check the directive's iteration loop calls
for ("python validate_asset.py exported_asset.fbx"). It runs against the
GLB companion export rather than the FBX itself: trimesh has no FBX loader
at all (confirmed empirically — only glb/gltf are registered), so an FBX
path here would silently validate nothing. build_assets.py exports a GLB
next to every FBX for exactly this purpose; the FBX in Assets/ is what
Unity actually imports and is already checked in-process by
validate_in_blender.py plus the export/re-import diff in build_assets.py.
"""
import os
import sys

import numpy as np
import trimesh

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import asset_specs  # noqa: E402

UV_EPS = 1e-4


def validate_glb(path: str, tri_budget: int | None) -> list[str]:
    issues = []
    if not os.path.isfile(path) or os.path.getsize(path) == 0:
        return [f"missing or empty file: {path}"]

    scene = trimesh.load(path, force="scene")
    meshes = list(scene.geometry.values())
    if not meshes:
        return ["no geometry found in export"]

    total_tris = 0
    for mesh in meshes:
        total_tris += len(mesh.faces)

        if mesh.visual is None or not hasattr(mesh.visual, "uv") or mesh.visual.uv is None:
            issues.append(f"{mesh.metadata.get('name', '?')}: no UV coordinates in export")
        else:
            uv = np.asarray(mesh.visual.uv)
            if len(uv) and (uv.min() < -UV_EPS or uv.max() > 1 + UV_EPS):
                issues.append(
                    f"{mesh.metadata.get('name', '?')}: UV outside 0-1 "
                    f"(min={uv.min():.3f}, max={uv.max():.3f})"
                )

        # duplicate/isolated vertices: trimesh merges on load by default,
        # so compare referenced vs. present vertex counts as a sanity check
        used = np.unique(mesh.faces.reshape(-1))
        if len(used) != len(mesh.vertices):
            issues.append(
                f"{mesh.metadata.get('name', '?')}: {len(mesh.vertices) - len(used)} "
                "unreferenced (isolated) vertices"
            )

        # Manifold-ness is a property of the geometric shell, not of vertex
        # *indices* — and every face here has its own independent UV
        # island (mesh_kit.paint gives each face its own inset square in
        # its pigment's cell), so glTF export duplicates a vertex at every
        # seam. Checking edges by raw vertex index would flag nearly every
        # edge in the mesh as "boundary". Re-key vertices by rounded
        # position first so seam duplicates collapse back together, then
        # check that.
        rounded = np.round(mesh.vertices, 5)
        _, pos_index = np.unique(rounded, axis=0, return_inverse=True)
        faces_by_pos = pos_index[mesh.faces]
        edges_by_pos = np.sort(trimesh.geometry.faces_to_edges(faces_by_pos), axis=1)
        boundary_idx = (
            trimesh.grouping.group_rows(edges_by_pos, require_count=1)
            if len(edges_by_pos) else np.array([])
        )
        if len(boundary_idx):
            issues.append(f"{mesh.metadata.get('name', '?')}: {len(boundary_idx)} boundary/non-manifold edges")

    if tri_budget is not None and total_tris > tri_budget:
        issues.append(f"over budget: {total_tris} tris > {tri_budget}")

    return issues


def _budget_for(key: str) -> int | None:
    for spec in asset_specs.ALL_SPECS:
        if spec["key"] == key:
            return spec["tri_budget"]
    return None


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(2)

    if sys.argv[1] == "--all":
        glb_dir = os.environ.get("PLUNDERSPELL_SCRATCH_GLB", "/tmp/plunderspell_glb")
        any_failed = False
        # --all --only <prefix>: just the keys build_assets.py --only rebuilt.
        only = sys.argv[sys.argv.index("--only") + 1] if "--only" in sys.argv else None
        for spec in asset_specs.ALL_SPECS:
            if only is not None and not asset_specs.key_matches(spec["key"], only):
                continue
            path = os.path.join(glb_dir, f"{spec['key']}.glb")
            issues = validate_glb(path, spec["tri_budget"])
            status = "PASS" if not issues else "FAIL"
            print(f"[{status}] {spec['key']}")
            for issue in issues:
                print(f"         - {issue}")
            any_failed |= bool(issues)
        sys.exit(1 if any_failed else 0)

    path = sys.argv[1]
    key = os.path.splitext(os.path.basename(path))[0]
    budget = int(sys.argv[2]) if len(sys.argv) > 2 else _budget_for(key)
    issues = validate_glb(path, budget)
    if issues:
        print(f"[FAIL] {path}")
        for issue in issues:
            print(f"       - {issue}")
        sys.exit(1)
    print(f"[PASS] {path}")


if __name__ == "__main__":
    main()
