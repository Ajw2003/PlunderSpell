"""
Validate the castle room sheets and write their handoff pages.

    python3 Tools/ArtBible/build_room_sheets.py                    # validate every Age, write docs/art/rooms/<age>.md
    python3 Tools/ArtBible/build_room_sheets.py --only BronzeAge   # one Age (or a key prefix, e.g. BronzeMeg)
    python3 Tools/ArtBible/build_room_sheets.py --check            # validate only, write nothing
    python3 Tools/ArtBible/build_room_sheets.py --models           # also hold each built model to its sheet

One sheet per room and curtain-wall piece in
Tools/AssetPipeline/asset_specs.ERA_CASTLE_SPECS (door plugs are plain slabs and
have none). Each is a JSON spec, docs/art/rooms/data/<Age>/<Key>.json, plus a
concept SVG, docs/art/rooms/concept/<Age>/<Key>.svg, drawn by a generator in
Tools/ArtBible/rooms/generators/<Age>/<Key>.py. Rules: docs/art/rooms/README.md.

--models compares every sheet's loot anchors with the ones the Blender builder
actually registered (Assets/_Project/Data/Castle/CastleLootAnchors.json): the
same count, each within ANCHOR_TOLERANCE metres. That is the check that the
model was built to its drawing and not drifted away from it.

Exits non-zero naming every problem found, and writes nothing unless the
selected set validates.
"""
import json
import math
import os
import sys

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, os.path.join(REPO, "Tools", "AssetPipeline"))
import asset_specs  # noqa: E402

ROOMS = os.path.join(REPO, "docs", "art", "rooms")
ANCHORS_JSON = os.path.join(REPO, "Assets", "_Project", "Data", "Castle", "CastleLootAnchors.json")
ANCHOR_TOLERANCE = 0.15

AGE_TITLES = {"BronzeAge": "The Bronze Age", "LateMedieval": "The Late Medieval", "AgeOfPowder": "The Age of Powder"}
ZONES = ("CurtainWall", "OuterBailey", "InnerWard", "Keep", "Crypt")
ARCH_H = {"OuterBailey": "2.59", "InnerWard": "2.88", "Keep": "3.31", "Crypt": "2.16"}
REQUIRED = {
    "key": str, "name": str, "age": str, "zone": str, "role": str, "summary": str, "description": str,
    "build": list, "sockets": list, "loot_anchors": list, "materials": list, "gameplay": list,
    "budget": str, "concept": str,
}
MIN_LEN = {"build": 8, "sockets": 2, "materials": 4, "gameplay": 3}


def sheet_specs(only=None):
    specs = [s for s in asset_specs.ERA_CASTLE_SPECS if s["kind"] != "plug"]
    if only:
        specs = [s for s in specs if s["era"] == only or asset_specs.key_matches(s["key"], only)]
    return specs


def validate(spec, pigments):
    """Problems with one sheet, as strings; [] means it passes."""
    key, era = spec["key"], spec["era"]
    path = os.path.join(ROOMS, "data", era, f"{key}.json")
    if not os.path.isfile(path):
        return [f"{key}: no spec at {os.path.relpath(path, REPO)}"], None
    try:
        with open(path, encoding="utf8") as fh:
            d = json.load(fh)
    except json.JSONDecodeError as e:
        return [f"{key}: {os.path.relpath(path, REPO)} is not valid JSON: {e}"], None

    issues = []
    for field, typ in REQUIRED.items():
        if not isinstance(d.get(field), typ):
            issues.append(f"{key}: `{field}` missing or not a {typ.__name__}")
    if issues:
        return issues, d
    for field, n in MIN_LEN.items():
        if len(d[field]) < n:
            issues.append(f"{key}: `{field}` has {len(d[field])} entries, needs at least {n}")
    if d["key"] != key:
        issues.append(f"{key}: `key` says {d['key']!r}")
    if d["age"] != era:
        issues.append(f"{key}: `age` says {d['age']!r}, the spec list says {era!r}")
    if d["zone"] != spec["zone"]:
        issues.append(f"{key}: `zone` says {d['zone']!r}, the spec list says {spec['zone']!r}")

    if spec["kind"] == "room":
        if len(d["loot_anchors"]) < 2:
            issues.append(f"{key}: a room needs at least 2 loot anchors, has {len(d['loot_anchors'])}")
        arch = f"2.60 × {ARCH_H[spec['zone']]}"
        if not any(arch in s for s in d["sockets"]):
            issues.append(f"{key}: no socket names the {spec['zone']} archway ({arch} m)")
    for i, a in enumerate(d["loot_anchors"]):
        xyz = a.get("xyz") if isinstance(a, dict) else None
        if not (isinstance(a, dict) and isinstance(a.get("at"), str) and isinstance(xyz, list) and len(xyz) == 3):
            issues.append(f"{key}: loot_anchors[{i}] must be {{\"at\": str, \"xyz\": [x, y, z]}}")
        elif max(abs(xyz[0]), abs(xyz[1])) > 5.5:
            issues.append(f"{key}: loot_anchors[{i}] {xyz} is outside the room")
    for i, m in enumerate(d["materials"]):
        if not all(isinstance(m.get(k), str) for k in ("name", "hex", "pigment", "notes")):
            issues.append(f"{key}: materials[{i}] needs name, hex, pigment and notes")
        elif m["pigment"] not in pigments:
            issues.append(f"{key}: materials[{i}] pigment {m['pigment']!r} is not in palette.py")

    concept = os.path.join(ROOMS, d["concept"])
    if d["concept"] != f"concept/{era}/{key}.svg":
        issues.append(f"{key}: `concept` should be concept/{era}/{key}.svg")
    elif not os.path.isfile(concept):
        issues.append(f"{key}: concept sheet {os.path.relpath(concept, REPO)} does not exist")
    else:
        svg = open(concept, encoding="utf8").read()
        for bad in ("<image", "<script", "foreignObject"):
            if bad in svg:
                issues.append(f"{key}: concept sheet contains {bad}")
        import re
        m = re.search(r'id="loot-anchors" data-xy="([^"]*)"', svg)
        drawn = [tuple(float(v) for v in p.split(",")) for p in m.group(1).split(";") if p] if m else []
        listed = [tuple(a["xyz"][:2]) for a in d["loot_anchors"] if isinstance(a, dict) and "xyz" in a]
        if len(drawn) != len(listed) or any(math.dist(p, q) > 0.02 for p, q in zip(drawn, listed)):
            issues.append(f"{key}: the sheet's L1…L{len(drawn)} and the spec's loot_anchors are not the same points "
                          f"in the same order")
        if 'viewBox="0 0 1200 800"' not in svg:
            issues.append(f"{key}: concept sheet is not viewBox 0 0 1200 800")
        if not os.path.isfile(concept[:-4] + ".png"):
            issues.append(f"{key}: no PNG render beside the SVG (run render_png.cjs --rooms)")
    return issues, d


def check_model(d, built):
    """The sheet's loot anchors against the builder's (Blender space: x east,
    y north, z up, module-local; the sheet uses the same frame)."""
    key = d["key"]
    got = built.get(key)
    if got is None:
        return [f"{key}: no model built yet (no entry in CastleLootAnchors.json)"]
    want = [a["xyz"] for a in d["loot_anchors"]]
    if len(got) != len(want):
        return [f"{key}: sheet has {len(want)} loot anchors, model registers {len(got)}"]
    issues = []
    free = list(got)
    for w in want:
        best = min(free, key=lambda g: math.dist(g, w))
        if math.dist(best, w) > ANCHOR_TOLERANCE:
            issues.append(f"{key}: sheet anchor {w} has no model anchor within {ANCHOR_TOLERANCE} m "
                          f"(nearest {best})")
        free.remove(best)
    return issues


def write_md(era, sheets):
    title = AGE_TITLES[era]
    out = [f"# {title}: castle rooms\n",
           "Generated by `python3 Tools/ArtBible/build_room_sheets.py` from `docs/art/rooms/data/"
           f"{era}/*.json`. Do not edit by hand. One sheet per room and curtain-wall piece of the "
           f"{title} castle set; the models are `Assets/_Project/Art/Models/Castle/{era}/<Key>.fbx`, "
           "built to these sheets. Rules: [`README.md`](README.md). Plan: "
           "[`docs/plans/era-castle-rooms.md`](../../plans/era-castle-rooms.md).\n"]
    for zone in ZONES:
        zs = [d for d in sheets if d["zone"] == zone]
        if not zs:
            continue
        out.append(f"\n## {zone}\n")
        out.append("| Key | Room | Stands in for | |\n|---|---|---|---|")
        for d in zs:
            out.append(f"| `{d['key']}` | [{d['name']}](#{d['key'].lower()}) | {d['role']} | {d['summary']} |")
        for d in zs:
            out.append(f"\n### {d['key']}\n")
            out.append(f"**{d['name']}** · {zone} · stands in for {d['role']} · {d['budget']}\n")
            out.append(f"![{d['name']}]({d['concept'][:-4]}.png)\n")
            out.append(d["description"] + "\n")
            if d.get("source"):
                out.append(f"Source: the art bible's {d['source']}.\n")
            if d.get("kit_changes"):
                out.append("**Where the kit differs from the art bible**\n")
                out += [f"- {c}" for c in d["kit_changes"]]
                out.append("")
            out.append("**Build** (kit metres: x east, y north, from the cell centre; heights above ground, "
                       "floor top at 0.30)\n")
            out += [f"- {b}" for b in d["build"]]
            out.append("\n**Sockets**\n")
            out += [f"- {s}" for s in d["sockets"]]
            out.append("\n**Loot anchors**\n")
            out += [f"- L{i}: {a['at']} at ({a['xyz'][0]:.2f}, {a['xyz'][1]:.2f}, {a['xyz'][2]:.2f})"
                    for i, a in enumerate(d["loot_anchors"], 1)]
            out.append("\n**Materials** (art-pass colour, then the kit's atlas pigment)\n")
            out.append("| Material | Hex | Kit pigment | Notes |\n|---|---|---|---|")
            out += [f"| {m['name']} | `{m['hex']}` | `{m['pigment']}` | {m['notes']} |" for m in d["materials"]]
            out.append("\n**In play**\n")
            out += [f"- {g}" for g in d["gameplay"]]
    path = os.path.join(ROOMS, f"{era}.md")
    with open(path, "w", encoding="utf8") as fh:
        fh.write("\n".join(out).rstrip() + "\n")
    return path


def main():
    argv = sys.argv[1:]
    only = argv[argv.index("--only") + 1] if "--only" in argv else None
    check_only = "--check" in argv
    models = "--models" in argv

    import palette  # noqa: E402  (Tools/AssetPipeline, on sys.path above)
    pigments = set(palette.PIGMENTS)
    built = {}
    if models and os.path.isfile(ANCHORS_JSON):
        with open(ANCHORS_JSON, encoding="utf8") as fh:
            built = json.load(fh)["rooms"]

    specs = sheet_specs(only)
    if not specs:
        print(f"ERROR: --only {only!r} matches nothing")
        sys.exit(1)
    problems, by_era = [], {}
    for spec in specs:
        issues, d = validate(spec, pigments)
        if not issues and models:
            issues = check_model(d, built)
        problems += issues
        if d is not None:
            by_era.setdefault(spec["era"], []).append(d)

    if problems:
        print(f"{len(problems)} problem(s):")
        for p in problems:
            print(f"  - {p}")
        sys.exit(1)
    print(f"OK: {len(specs)} room sheet(s) validate" + (" and match their models" if models else ""))
    if check_only:
        return
    full_eras = {e for e in by_era if len(by_era[e]) == len(sheet_specs(e))}
    for era in sorted(full_eras):
        print("wrote", os.path.relpath(write_md(era, by_era[era]), REPO))
    for era in sorted(set(by_era) - full_eras):
        print(f"skipped {era}.md: only part of the Age was selected")


if __name__ == "__main__":
    main()
