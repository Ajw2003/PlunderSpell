#!/usr/bin/env python3
"""Build every Plunderspell enemy: mesh, rig, baked textures, FBX/glTF export.

Run with the Blender Python runtime installed as a pip module:

    python3 Tools/EnemyForge/build_enemies.py

Outputs land in Assets/Models/Enemies/<Name>/ and the run fails loudly if any
model does not pass geometry validation.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bpy  # noqa: E402  (must follow the sys.path fix-up)

from enemy_forge import assemble, materials, validate  # noqa: E402
from enemy_forge.archetypes import BY_NAME, ROSTER  # noqa: E402

# This file lives at <repo>/Tools/EnemyForge/, so the repo root is three levels up.
REPO_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DEFAULT_OUT = os.path.join(REPO_ROOT, "Assets", "Models", "Enemies")


def build_one(arch, out_root: str, resolution: int) -> dict:
    assemble.reset_scene()
    authoring = materials.build_authoring_set(arch.name, wear=arch.wear)
    obj = assemble.build_mesh_object(arch, authoring)
    assemble.finish_geometry(obj, arch)
    rig = assemble.build_armature(arch, obj)

    model_dir = os.path.join(out_root, arch.name)
    texture_dir = os.path.join(model_dir, "Textures")
    assemble.texture_and_bake(obj, arch, authoring, texture_dir, resolution)

    report = validate.validate(obj, arch)
    print(validate.format_report(report))

    exported = assemble.export(obj, rig, arch, model_dir)
    return {
        "name": arch.name,
        "role": arch.role,
        "passed": report.passed,
        "stats": report.stats,
        "failures": report.failures,
        "warnings": report.warnings,
        "files": {k: os.path.relpath(v, REPO_ROOT) for k, v in exported.items()},
    }


def write_manifest(path: str, results: list[dict]) -> None:
    """Merge this run's results into the manifest, keeping enemies we did not build.

    A `--only` run must not drop the rest of the roster from the manifest, which is
    what downstream tooling reads to know what exists.
    """
    existing: dict[str, dict] = {}
    if os.path.exists(path):
        try:
            with open(path, encoding="utf-8") as handle:
                for entry in json.load(handle).get("enemies", []):
                    existing[entry["name"]] = entry
        except (json.JSONDecodeError, KeyError, OSError) as error:
            print(f"        ignoring unreadable manifest at {path}: {error}")

    for entry in results:
        existing[entry["name"]] = entry

    order = [a.name for a in ROSTER]
    ordered = sorted(existing.values(),
                     key=lambda e: order.index(e["name"]) if e["name"] in order else 999)
    with open(path, "w", encoding="utf-8") as handle:
        json.dump({"generated_by": "Tools/EnemyForge/build_enemies.py",
                   "enemies": ordered}, handle, indent=2)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", default=DEFAULT_OUT, help="output root directory")
    parser.add_argument("--resolution", type=int, default=1024, help="baked texture size")
    parser.add_argument("--only", nargs="*", default=None,
                        help="build only these archetypes by name")
    args = parser.parse_args()

    selected = ROSTER if not args.only else [BY_NAME[n] for n in args.only]
    os.makedirs(args.out, exist_ok=True)

    results = []
    started = time.time()
    for arch in selected:
        print(f"\n=== {arch.name} — {arch.role} ===")
        step = time.time()
        results.append(build_one(arch, args.out, args.resolution))
        print(f"        built in {time.time() - step:.1f}s")

    manifest_path = os.path.join(args.out, "enemy_manifest.json")
    write_manifest(manifest_path, results)

    failed = [r["name"] for r in results if not r["passed"]]
    print(f"\n{len(results)} models in {time.time() - started:.1f}s "
          f"-> {os.path.relpath(manifest_path, REPO_ROOT)}")
    if failed:
        print(f"VALIDATION FAILED: {', '.join(failed)}")
        return 1
    print("All models passed geometry validation.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
