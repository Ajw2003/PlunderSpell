"""Text search over the CLAP index.

    python Tools/AudioForge/audioforge.py find "a book page turning" [--top 10] [--for <sound>]
                                               [--category ui] [--loop] [--min-s S] [--max-s S] [--root NAME]

Cosine similarity of the text embedding against every window, best window per file, then the
duration filters. --for <sound> takes the category and loop flag from that manifest row, so its
length rule applies (forge/categories.py); --category and --loop set them by hand. Each hit prints
the audit flags (forge/audit.py) of its window; the level rule is left out, because the build sets
every file's level itself.
"""

import argparse
import csv
import re
import sys

import numpy as np

from forge.audit import measure, rules
from forge.build import MANIFEST
from forge.categories import CATEGORY, category

from . import clap
from .index import INDEX, load_index, load_roots, read_window

LOOP_MIN_S = 4.0      # a loop cannot come out of a click
ONE_SHOT_SLACK = 1.5  # a one-shot window may run this much over its category's max length (silence tails)
DEFAULT_ROW = dict(name="find", folder="Physics", bus="SFX/World", loop="0", noise="mid")


def clean_query(text):
    """Drop what CLAP has no use for: a spoken duration and loop instructions in an ai_prompt."""
    text = re.sub(r",?\s*\d+(\.\d+)? seconds?\b", "", text)
    text = re.sub(r",?\s*seamless loop\b", "", text)
    return text.strip(" ,")


def query_for(row):
    return clean_query(row["ai_prompt"] or row["brief"])


def length_limits(row):
    """(min, max) seconds a candidate window may run for this manifest row (max None = unlimited)."""
    if row["loop"] == "1":
        return LOOP_MIN_S, None
    limit = CATEGORY[category(row)]["max_s"]
    return 0.0, (limit * ONE_SHOT_SLACK if limit else None)


class Library:
    """Every indexed root, stacked: vectors, per-window metadata and the root each belongs to."""

    def __init__(self, names=None, roots_path=None):
        self.roots = {r["name"]: r for r in (load_roots(roots_path) if roots_path else load_roots())}
        self.rows, chunks = [], []
        for name in self.roots:
            if names and name not in names:
                continue
            rows, emb = load_index(name)
            for r in rows:
                r["root"] = name
                r["start"], r["dur"], r["file_dur"] = float(r["start"]), float(r["dur"]), float(r["file_dur"])
            self.rows += rows
            chunks.append(emb)
        if not self.rows:
            raise SystemExit(f"no index found in {INDEX}; run: audioforge.py index")
        self.emb = np.concatenate(chunks)
        self.file_ids = {}
        for i, r in enumerate(self.rows):
            self.file_ids.setdefault((r["root"], r["file"]), []).append(i)

    def path(self, hit):
        return self.roots[hit["root"]]["abs"] / hit["file"]

    def search(self, query, top=10, limits=(0.0, None), text_vec=None):
        """Best window per file for one query; returns hits sorted by score."""
        vec = clap.embed_text([query])[0] if text_vec is None else text_vec
        scores = self.emb @ vec
        lo, hi = limits
        best = {}
        for i, r in enumerate(self.rows):
            if r["dur"] < lo or (hi is not None and r["dur"] > hi):
                continue
            key = (r["root"], r["file"])
            if key not in best or scores[i] > scores[best[key]]:
                best[key] = i
        order = sorted(best.values(), key=lambda i: -scores[i])[:top]
        return [dict(root=self.rows[i]["root"], file=self.rows[i]["file"], start=self.rows[i]["start"],
                     dur=self.rows[i]["dur"], file_dur=self.rows[i]["file_dur"], score=float(scores[i]))
                for i in order]

    def flags(self, hit, row=None):
        """Audit flags of the hit's window, minus the level rule (the build sets the level)."""
        x = read_window(self.path(hit), hit["start"], hit["dur"])
        _, found = rules(row or DEFAULT_ROW, measure(x))
        return [f for f in found if not f.startswith("level:")]


def manifest_rows():
    with MANIFEST.open(newline="", encoding="utf-8") as f:
        return list(csv.DictReader(f))


def main(argv=None):
    parser = argparse.ArgumentParser(prog="audioforge.py find")
    parser.add_argument("query", nargs="?", help="text to search for (default: the --for row's prompt or brief)")
    parser.add_argument("--top", type=int, default=10)
    parser.add_argument("--for", dest="sound", help="manifest sound name: use its category, loop flag and text")
    parser.add_argument("--category", choices=sorted(CATEGORY))
    parser.add_argument("--loop", action="store_true")
    parser.add_argument("--min-s", type=float)
    parser.add_argument("--max-s", type=float)
    parser.add_argument("--root", action="append", help="limit to this root (repeatable)")
    args = parser.parse_args(argv)

    row = None
    if args.sound:
        matches = [r for r in manifest_rows() if r["name"] == args.sound]
        if not matches:
            print(f"no sound named {args.sound!r} in manifest.csv")
            return 2
        row = matches[0]
    query = args.query or (query_for(row) if row else None)
    if not query:
        parser.error("give a query or --for <sound>")
    ctx = dict(row or DEFAULT_ROW)
    if args.category:  # pick a row shape that lands in the category
        ctx.update(dict(ui=dict(folder="UI", bus="UI"), cue=dict(folder="SFX/Lair", bus="UI"),
                        foley=dict(folder="Foley"), physics=dict(folder="Physics"), weapons=dict(folder="SFX/Weapons"),
                        spells=dict(folder="SFX/Spells"), creatures=dict(folder="SFX/Enemies"),
                        world=dict(folder="SFX/Castle"), ambience=dict(folder="Ambience"),
                        music=dict(folder="Music")).get(args.category, {}))
    if args.loop:
        ctx["loop"] = "1"
    lo, hi = length_limits(ctx)
    lo = args.min_s if args.min_s is not None else lo
    hi = args.max_s if args.max_s is not None else hi
    library = Library(args.root)
    print(f"query: {query!r}   category {category(ctx)}, windows {lo:g}..{'any' if hi is None else format(hi, 'g')} s, "
          f"{len(library.file_ids)} files on {clap.device()}")
    for n, hit in enumerate(library.search(query, args.top, (lo, hi)), 1):
        flags = library.flags(hit, ctx)
        window = f"{hit['start']:g}-{hit['start'] + hit['dur']:g}s" if hit["file_dur"] > hit["dur"] + 0.01 else "whole"
        print(f"{n:2d}  {hit['score']:.3f}  {hit['root']}/{hit['file']}  [{window}]  {hit['dur']:.2f}s"
              + (f"  FLAGS: {'; '.join(flags)}" if flags else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
