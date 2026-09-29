"""Which placeholder sounds does the indexed library already cover, and how sure is that?

    python Tools/AudioForge/audioforge.py coverage               # writes docs/generated/audio-coverage/
    python Tools/AudioForge/audioforge.py coverage --calibrate   # the check the threshold came from

Every sound still synthesised as a stand-in (not guard voices or music; the list the AI step used)
is searched with its ai_prompt, or its brief when it has none. Its best score decides "covered".
The threshold is derived on every run from the sounds that are already library sounds (choose_threshold);
the rule and the numbers are written to the README this command generates.
"""

import csv
import fnmatch
import sys
from collections import Counter

import numpy as np

from forge.build import FORGE, KENNEY, REPO
from forge.categories import category

from . import clap
from .search import Library, length_limits, manifest_rows, query_for

OUT = REPO / "docs" / "generated" / "audio-coverage"
TOP_HIT = 5
SEP = "\n"


def placeholder_rows(rows):
    from forge.ai_elevenlabs import placeholder_names
    names = placeholder_names()
    return [r for r in rows if r["name"] in names]


def expected_files(row):
    """The Kenney files this row's recipe already uses: relative posix paths under the kenney root."""
    found = set()
    for layer in row["recipe"].split(" + "):
        layer = layer.strip()
        if not layer.startswith("kenney:"):
            continue
        target = layer[len("kenney:"):].split()[0]
        pack, _, pattern = target.partition("/")
        for p in (KENNEY / pack / "Audio").glob(pattern + ("" if pattern.endswith(".ogg") else ".ogg")):
            found.add(p.relative_to(KENNEY).as_posix())
    return found


def calibrate(library, rows):
    """For each library row (final L, a Kenney layer), does its own file come back for its own brief?"""
    out = []
    vecs = clap.embed_text([query_for(r) for r in rows])
    for row, vec in zip(rows, vecs):
        want = expected_files(row)
        hits = [h for h in library.search(None, 10 ** 6, length_limits(row), text_vec=vec) if h["root"] == "kenney"]
        rank = next((i for i, h in enumerate(hits, 1) if h["file"] in want), None)
        own = [h["score"] for h in hits if h["file"] in want]
        out.append(dict(sound=row["name"], query=query_for(row), expected=len(want),
                        rank_of_own=rank or "", own_score=round(max(own), 3) if own else "",
                        top1_score=round(hits[0]["score"], 3) if hits else "", top1_file=hits[0]["file"] if hits else "",
                        hit=bool(rank and rank <= TOP_HIT)))
    return out


def library_rows(rows):
    return [r for r in rows if r["final"] == "L" and expected_files(r)]


def choose_threshold(cal):
    """The highest score (on a 0.025 grid) that still keeps at least half of the known hits."""
    hits = sorted(c["top1_score"] for c in cal if c["hit"])
    best = 0.0
    t = 0.0
    while t <= 1.0:
        if sum(1 for h in hits if h >= t) * 2 >= len(hits):
            best = t
        t = round(t + 0.025, 3)
    return best


def auc(cal):
    """Chance that a random hit has a higher top-1 score than a random miss."""
    pos = [c["top1_score"] for c in cal if c["hit"]]
    neg = [c["top1_score"] for c in cal if not c["hit"]]
    return float(np.mean([(p > n) + 0.5 * (p == n) for p in pos for n in neg])) if pos and neg else float("nan")


def sweep(cal, hits_total):
    lines = []
    for t in (0.2, 0.3, 0.35, 0.4, 0.45, 0.5, 0.55):
        above = [c for c in cal if c["top1_score"] >= t]
        good = [c for c in above if c["hit"]]
        lines.append(f"| {t:.2f} | {len(above)} | {len(good)} | {len(good) / max(len(above), 1):.0%} | "
                     f"{len(good) / max(hits_total, 1):.0%} |")
    return lines


def main(argv=None):
    argv = argv or []
    rows = manifest_rows()
    library = Library()
    OUT.mkdir(parents=True, exist_ok=True)
    cal = calibrate(library, library_rows(rows))
    with (OUT / "calibration.csv").open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=list(cal[0]))
        writer.writeheader()
        writer.writerows(cal)
    hits = [c for c in cal if c["hit"]]
    at = lambda n: sum(1 for c in cal if c["rank_of_own"] and c["rank_of_own"] <= n)
    threshold = choose_threshold(cal)
    if "--calibrate" in argv:
        print(f"{len(cal)} library sounds queried with their own brief; own file in top {TOP_HIT}: {len(hits)}, "
              f"top 10: {at(10)}, top 20: {at(20)}")
        for c in cal:
            print(f"  {'HIT ' if c['hit'] else 'MISS'} {c['sound']:<38} rank {c['rank_of_own'] or '-':>4} "
                  f"own {c['own_score'] or '-':>6} top1 {c['top1_score']}  {c['query'][:50]}")
        print("threshold sweep: top-1 score >= T | rows | hits | precision | recall")
        print(SEP.join(sweep(cal, len(hits))))
        print(f"chosen threshold: {threshold}")
        return 0

    targets = placeholder_rows(rows)
    vecs = clap.embed_text([query_for(r) for r in targets])
    results = []
    for row, vec in zip(targets, vecs):
        found = library.search(None, 3, length_limits(row), text_vec=vec)
        top = found[0] if found else None
        results.append(dict(sound=row["name"], category=category(row), loop=row["loop"], query=query_for(row),
                            from_ai_prompt=bool(row["ai_prompt"]),
                            best_score=round(top["score"], 3) if top else "",
                            best_file=f"{top['root']}/{top['file']}" if top else "",
                            window=f"{top['start']:g}-{top['start'] + top['dur']:g}" if top else "",
                            covered=bool(top and top["score"] >= threshold)))
    results.sort(key=lambda r: -(r["best_score"] or -1))
    with (OUT / "coverage.csv").open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=list(results[0]))
        writer.writeheader()
        writer.writerows(results)
    covered = [r for r in results if r["covered"]]
    prompted = [r for r in covered if r["from_ai_prompt"]]
    by_cat = Counter((r["category"], r["covered"]) for r in results)
    brief = {r["name"]: r["brief"] for r in rows}
    md = [
        "# Audio coverage: which placeholders the indexed libraries already cover", "",
        "Generated by `python Tools/AudioForge/audioforge.py coverage`; do not edit by hand. Files:",
        "`coverage.csv` (one row per placeholder sound), `calibration.csv` (the check the threshold came from).", "",
        f"Libraries indexed: {', '.join(sorted({r['root'] for r in library.rows}))}, {len(library.file_ids)} files.",
        f"**{len(covered)} of {len(results)} placeholder sounds are covered** (best candidate scores at least "
        f"**{threshold}**). Guard voices and music are not counted.", "",
        "Each placeholder is searched with its AI prompt, or its brief when it has none (prefixed \"the sound of\"), "
        "under its category's length rule. \"Covered\" only means a candidate worth putting on the review page: "
        "listen before keeping one.", "",
        "## How the threshold was chosen", "",
        f"The {len(cal)} sounds that are already library sounds (final `L`, at least one Kenney layer) were searched "
        f"with their own briefs. Their own file came back in the top {TOP_HIT} for {len(hits)} of them "
        f"(top 10: {at(10)}, top 20: {at(20)}); the other {len(cal) - len(hits)} did not, so a bare score is a weak "
        f"judge here. The briefs are loose (\"a heavy curtain drawn\") and the earlier picks were stand-ins chosen by "
        f"hand, so a miss is not always a wrong answer. The rule used: the highest score on a 0.025 grid that still "
        f"keeps at least half of the {len(hits)} hits, which is **{threshold}**. It is recomputed on every run.", "",
        "| top-1 score at least | rows | own file in top 5 | precision | share of hits kept |", "|---|---:|---:|---:|---:|",
        *sweep(cal, len(hits)), "",
        f"How well the top-1 score tells a hit from a miss (AUC, 0.5 is a coin flip): **{auc(cal):.2f}**. "
        f"That is close to a coin flip, so **the covered count is an upper bound, not a measurement**. "
        f"{sum(1 for r in covered if r['from_ai_prompt'])} of the {len(covered)} covered sounds were searched with a long "
        f"AI prompt, and long prompts score higher against almost any file"
        + (f" (`{prompted[0]['sound']}` reaches {prompted[0]['best_score']} with `{prompted[0]['best_file'].split('/')[-1]}`)"
           if prompted else "")
        + ". The review page is where a candidate is really judged.", "",
        "## Covered and not, by category", "", "| category | covered | not covered |", "|---|---:|---:|",
        *[f"| {cat} | {by_cat[(cat, True)]} | {by_cat[(cat, False)]} |"
          for cat in sorted({r['category'] for r in results})], "",
        f"## The {len(results) - len(covered)} sounds no indexed library covers", "",
        "| sound | best score | best file | brief |", "|---|---:|---|---|",
        *[f"| `{r['sound']}` | {r['best_score'] if r['best_score'] != '' else 'no candidate'} | "
          f"`{r['best_file'].split('/')[-1]}` | {brief[r['sound']]} |" for r in results if not r["covered"]], ""]
    (OUT / "README.md").write_text(SEP.join(md), encoding="utf-8")
    print(f"{len(covered)} of {len(results)} placeholder sounds covered at score >= {threshold}; "
          f"library check: own file in top {TOP_HIT} for {len(hits)} of {len(cal)}")
    print(f"wrote {OUT.relative_to(REPO)}/coverage.csv, README.md, calibration.csv")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
