#!/usr/bin/env python3
"""Build art-bible models: mesh, baked textures, FBX/glTF/.blend, validation.

    python3 Tools/ArtForge/build.py items
    python3 Tools/ArtForge/build.py items --age bronze --only sealed-amphora
    python3 Tools/ArtForge/build.py structures --resolution 2048

Outputs go to Assets/Models/ArtBible/<Kind>/<Age>/<PascalName>/ and the manifest
Assets/Models/ArtBible/artforge_manifest.json. Exits non-zero if any model fails
validation, and if --only names a slug that has no blueprint.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
import traceback

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import art_forge  # noqa: E402  (puts Tools/EnemyForge on sys.path)
import bpy  # noqa: E402,F401

from art_forge import AGES, KINDS, REPO_ROOT, assemble, blueprints, materials, validate  # noqa: E402

OUT_ROOT = os.path.join(REPO_ROOT, "Assets", "Models", "ArtBible")
MANIFEST = os.path.join(OUT_ROOT, "artforge_manifest.json")


def model_dir(kind: str, age: str, pascal: str, out_root: str = OUT_ROOT) -> str:
    return os.path.join(out_root, kind.capitalize(), age.capitalize(), pascal)


def build_one(kind: str, age: str, slug: str, resolution: int, out_root: str) -> dict:
    bp = blueprints.make(kind, age, slug)
    used = dict(bp.ordered_families())
    violations = materials.discipline_violations(kind, bp.name, used, bp.gold_reason)
    if violations:
        raise RuntimeError("; ".join(violations))

    directory = model_dir(kind, age, bp.name, out_root)
    if bp.rigged:
        obj, rig = assemble.build_rigged(bp, directory, resolution)
    else:
        obj, rig = assemble.build_static(bp, directory, resolution)

    report = validate.validate(obj, bp)
    unused = bp.unused_families()
    if unused:
        report.warnings.append(f"JSON materials not used by any part: {unused}")
    if bp.extra_families:
        report.warnings.append(f"families not in the JSON (blueprint extras): "
                               f"{sorted(bp.extra_families)}")
    print(validate.format_report(report))

    files = assemble.export(obj, rig, bp, directory)
    return {
        "key": f"{kind}/{age}/{slug}",
        "kind": kind, "age": age, "slug": slug, "name": bp.name,
        "title": bp.entry.name,
        "passed": report.passed,
        "stats": report.stats,
        "spec": {"dims_WDH_m": bp.entry.dims, "height_m": bp.entry.height_m,
                 "tri_budget": bp.budget},
        "families": {k: {"name": f["name"], "base": f["base"], "rough": f["rough"],
                         "metal": f["metal"], "emit": f.get("emit")}
                     for k, f in used.items()},
        "failures": report.failures,
        "warnings": report.warnings,
        "notes": bp.notes,
        "resolution": resolution,
        "files": {k: os.path.relpath(v, REPO_ROOT) for k, v in files.items()},
    }


def write_manifest(path: str, results: list[dict]) -> None:
    """Merge into the manifest, keeping entries this run did not rebuild."""
    existing: dict[str, dict] = {}
    if os.path.exists(path):
        try:
            with open(path, encoding="utf-8") as handle:
                for item in json.load(handle).get("assets", []):
                    existing[item["key"]] = item
        except (json.JSONDecodeError, KeyError, OSError) as error:
            # Loud, not silent: a bad manifest means earlier entries are dropped.
            print(f"WARNING: manifest at {path} is unreadable ({error}); "
                  f"rewriting it with this run's results only")
    for item in results:
        existing[item["key"]] = item

    order = {k: i for i, k in enumerate(KINDS)}
    ages = {a: i for i, a in enumerate(AGES)}
    ordered = sorted(existing.values(),
                     key=lambda e: (order.get(e["kind"], 9), ages.get(e["age"], 9), e["slug"]))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as handle:
        json.dump({"generated_by": "Tools/ArtForge/build.py", "assets": ordered},
                  handle, indent=2, ensure_ascii=False)


def select(kind: str, ages: list[str], only: list[str] | None) -> list[tuple[str, str]]:
    available = blueprints.available(kind, ages)
    if not only:
        return available
    chosen = [(a, s) for a, s in available if s in only]
    missing = sorted(set(only) - {s for _a, s in chosen})
    if missing:
        raise SystemExit(f"no {kind} blueprint for: {', '.join(missing)} "
                         f"(ages searched: {', '.join(ages)}; available: "
                         f"{', '.join(s for _a, s in available) or 'none'})")
    return chosen


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("kind", choices=KINDS)
    parser.add_argument("--age", choices=AGES, action="append",
                        help="limit to one age (repeatable); default all")
    parser.add_argument("--only", nargs="*", default=None, help="slugs to build")
    parser.add_argument("--resolution", type=int, default=1024, help="baked texture size")
    parser.add_argument("--out", default=OUT_ROOT, help="output root (default: %(default)s)")
    args = parser.parse_args()

    ages = args.age or list(AGES)
    chosen = select(args.kind, ages, args.only)
    if not chosen:
        print(f"No {args.kind} blueprints exist yet for {', '.join(ages)}; nothing to build.")
        return 1

    results, crashed = [], []
    started = time.time()
    for age, slug in chosen:
        print(f"\n=== {args.kind}/{age}/{slug} ===")
        step = time.time()
        try:
            result = build_one(args.kind, age, slug, args.resolution, args.out)
        except Exception:  # noqa: BLE001 — reported and turned into a failed exit
            traceback.print_exc()
            crashed.append(f"{age}/{slug}")
            continue
        result["build_seconds"] = round(time.time() - step, 1)
        results.append(result)
        print(f"        built in {result['build_seconds']}s -> "
              f"{os.path.relpath(model_dir(args.kind, age, result['name'], args.out), REPO_ROOT)}")

    manifest = os.path.join(args.out, "artforge_manifest.json")
    if results:
        write_manifest(manifest, results)
    failed = [f"{r['age']}/{r['slug']}" for r in results if not r["passed"]]
    print(f"\n{len(results)} built, {len(crashed)} crashed in {time.time() - started:.1f}s "
          f"-> {os.path.relpath(manifest, REPO_ROOT)}")
    if crashed:
        print(f"BUILD CRASHED: {', '.join(crashed)}")
    if failed:
        print(f"VALIDATION FAILED: {', '.join(failed)}")
    if crashed or failed:
        return 1
    print("All models passed validation.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
