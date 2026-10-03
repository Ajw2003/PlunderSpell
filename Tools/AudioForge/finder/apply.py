"""Turns a review-decisions.json (from the review page's Export decisions button) into picks.csv.

    python Tools/AudioForge/audioforge.py apply review-decisions.json [--dry-run]

Every candidate marked "keep" for a sound is one recorded take. The sound's variants take the kept
candidates in order, wrapping round when there are fewer kept than variants (a wrapped variant is
nudged in pitch, as the build does when it reuses a Kenney file). All of a sound's variants are
rewritten, replacing any earlier pick for it; other sounds' picks are left alone. Then run
`audioforge.py build` and `check` as usual: the build reads picks.csv and reports the source and
licence of each picked file in build_report.csv.
"""

import argparse
import csv
import json
import shlex
import sys

from forge import tailor
from forge.build import PICKS

from .search import manifest_rows

NUDGE = 0.04


def recipe_for(candidate, cycle):
    """The candidate's recipe, with a pitch nudge on the second and later passes over the kept list."""
    recipe = candidate["recipe"]
    if cycle:
        recipe += f" shift={1 + NUDGE * ((cycle + 1) // 2) * (1 if cycle % 2 else -1):.2f}"
    return recipe


def check_recipe(recipe):
    """A picked recipe must be a single lib: layer whose file exists."""
    layer = recipe.split(" + ")[0].strip()
    if not layer.startswith("lib:"):
        raise ValueError(f"not a lib: recipe: {recipe!r}")
    tailor.resolve(shlex.split(layer[4:])[0])


def picks_from(decisions, rows):
    kept = {}
    for d in decisions["decisions"]:
        if d.get("decision") == "keep":
            kept.setdefault(d["sound"], []).append(d)
    by_name = {r["name"]: r for r in rows}
    out = []
    for sound, cands in kept.items():
        if sound not in by_name:
            raise KeyError(f"decisions name a sound that is not in manifest.csv: {sound}")
        for c in cands:
            check_recipe(c["recipe"])
        for v in range(int(by_name[sound]["variants"])):
            out.append(dict(name=sound, variant=v + 1, recipe=recipe_for(cands[v % len(cands)], v // len(cands))))
    return out


def read_picks():
    if not PICKS.exists():
        return []
    with PICKS.open(newline="", encoding="utf-8") as f:
        return list(csv.DictReader(f))


def main(argv=None):
    parser = argparse.ArgumentParser(prog="audioforge.py apply")
    parser.add_argument("decisions", help="review-decisions.json")
    parser.add_argument("--dry-run", action="store_true", help="print the picks, write nothing")
    args = parser.parse_args(argv)
    with open(args.decisions, encoding="utf-8") as f:
        decisions = json.load(f)
    new = picks_from(decisions, manifest_rows())
    replaced = {p["name"] for p in new}
    merged = [p for p in read_picks() if p["name"] not in replaced] + new
    merged.sort(key=lambda p: (p["name"], int(p["variant"])))
    for p in new:
        print(f"  {p['name']}_{int(p['variant']):02d}: {p['recipe']}")
    if args.dry_run:
        print(f"dry run: {len(new)} picks for {len(replaced)} sounds, nothing written")
        return 0
    with PICKS.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=["name", "variant", "recipe"])
        writer.writeheader()
        writer.writerows(merged)
    print(f"wrote {PICKS}: {len(merged)} picks ({len(new)} new or replaced, {len(replaced)} sounds)")
    if replaced:
        print("next: python Tools/AudioForge/audioforge.py build " + " ".join(f"'{n}'" for n in sorted(replaced))
              + " && python Tools/AudioForge/audioforge.py check")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
