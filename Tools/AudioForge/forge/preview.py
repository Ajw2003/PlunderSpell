"""Writes docs/generated/audio-preview/index.html: every sound with a player, its brief and status.

Open the page from the repo checkout in a browser; its audio links point at the built files in
Assets/_Project/Audio, so it always plays what the build last made. Regenerate after a build.
"""

import csv
import html
import os
import sys
from collections import OrderedDict

from .build import MANIFEST, OUT, REPO, REPORT

PAGE = REPO / "docs" / "generated" / "audio-preview" / "index.html"
SOURCE_NAMES = {"G": "generated", "L": "library", "A": "AI sound", "R": "recorded", "C": "composed",
                "M": "AI music"}
STATUS_NOTE = {"placeholder": "placeholder", "generated": "generated", "library-cc0": "CC0 library", "library": "library",
               "ai": "AI take", "ai-music": "AI music", "recorded": "recorded", "composed": "composed"}

STYLE = """
:root{--bg:#14120E;--panel:#1d1a15;--ink:#DCD2BA;--dim:#9c937f;--line:#3a342a;--verd:#5FA288;--gold:#C9A227;--madder:#C4542E;--lapis:#7A6AA0}
@media (prefers-color-scheme: light){:root:not([data-theme="dark"]){--bg:#f3eee2;--panel:#fbf8f0;--ink:#231f18;--dim:#6b6353;--line:#d8cfbb}}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--ink);font:15px/1.45 Georgia,serif}
main{max-width:1100px;margin:0 auto;padding:24px 16px 80px}h1{font-size:28px;margin:0 0 4px}
.sub{color:var(--dim);margin:0 0 20px}nav{position:sticky;top:0;background:var(--bg);padding:8px 0;border-bottom:1px solid var(--line);z-index:2}
nav input{width:100%;padding:8px 10px;background:var(--panel);color:var(--ink);border:1px solid var(--line);border-radius:6px;font:inherit}
h2{margin:28px 0 8px;font-size:19px;border-bottom:1px solid var(--line);padding-bottom:4px}
.row{display:grid;grid-template-columns:minmax(0,1.3fr) minmax(0,1.6fr) auto;gap:10px;align-items:center;padding:8px 0;border-bottom:1px dotted var(--line)}
.name{font-family:ui-monospace,Menlo,monospace;font-size:13px;word-break:break-all}.brief{color:var(--dim);font-size:14px}
.tags{display:flex;gap:4px;flex-wrap:wrap;justify-content:flex-end}.tag{font-size:11px;padding:1px 6px;border-radius:9px;border:1px solid var(--line);white-space:nowrap}
.placeholder{color:var(--madder);border-color:var(--madder)}.library-cc0,.library,.generated{color:var(--verd);border-color:var(--verd)}
.ai,.ai-music,.recorded,.composed{color:var(--gold);border-color:var(--gold)}
.players{grid-column:1/-1;display:flex;flex-wrap:wrap;gap:6px}audio{height:30px;max-width:100%}
.summary{display:flex;gap:16px;flex-wrap:wrap;margin:8px 0 0;color:var(--dim)}.summary b{color:var(--ink)}
@media (max-width:640px){.row{grid-template-columns:1fr}.tags{justify-content:flex-start}}
"""

SCRIPT = """
const q=document.getElementById('q');q.addEventListener('input',()=>{const t=q.value.toLowerCase();
document.querySelectorAll('.row').forEach(r=>{r.style.display=r.dataset.k.includes(t)?'':'none'})});
document.addEventListener('play',e=>{document.querySelectorAll('audio').forEach(a=>{if(a!==e.target)a.pause()})},true);
"""


def main(argv=None):
    rows = list(csv.DictReader(MANIFEST.open()))
    report = {(r["name"], int(r["variant"])): r for r in csv.DictReader(REPORT.open())} if REPORT.exists() else {}
    groups = OrderedDict()
    for row in rows:
        groups.setdefault(row["folder"], []).append(row)
    counts = {}
    for r in report.values():
        counts[r["status"]] = counts.get(r["status"], 0) + 1

    parts = [f"<!doctype html><html lang=en><head><meta charset=utf-8>"
             f"<meta name=viewport content='width=device-width,initial-scale=1'><title>Plunderspell Sound Review</title>"
             f"<style>{STYLE}</style></head><body><main>",
             "<h1>Plunderspell — every sound</h1>",
             f"<p class=sub>{len(rows)} sounds, {sum(int(r['variants']) for r in rows)} files, built by "
             "Tools/AudioForge. Red tags are placeholders waiting for their real source.</p>",
             "<div class=summary>" + "".join(f"<span><b>{v}</b> {STATUS_NOTE.get(k, k)}</span>"
                                             for k, v in sorted(counts.items())) + "</div>",
             "<nav><input id=q placeholder='Filter by name or description (e.g. ignis, bell, knight, loop)'></nav>"]
    for folder, members in groups.items():
        parts.append(f"<h2>{html.escape(folder)}</h2>")
        for row in members:
            variants = int(row["variants"])
            status = report.get((row["name"], 1), {}).get("status", "missing")
            tags = [f"<span class='tag {status}'>{STATUS_NOTE.get(status, status)}</span>",
                    f"<span class=tag>ships as: {SOURCE_NAMES.get(row['final'], row['final'])}</span>"]
            if row["loop"] == "1":
                tags.append("<span class=tag>loop</span>")
            if row["noise"] not in ("none", ""):
                tags.append(f"<span class=tag>noise: {row['noise']}</span>")
            players = []
            for v in range(1, variants + 1):
                path = OUT / row["folder"] / f"{row['name']}_{v:02d}.ogg"
                src = os.path.relpath(path, PAGE.parent).replace(os.sep, "/")
                players.append(f"<audio controls preload=none {'loop ' if row['loop'] == '1' else ''}"
                               f"src='{html.escape(src)}' title='variant {v}'></audio>")
            key = html.escape(f"{row['name']} {row['brief']} {folder}".lower())
            parts.append(f"<div class=row data-k='{key}'><div class=name>{html.escape(row['name'])}"
                         f"{f' ×{variants}' if variants > 1 else ''}</div>"
                         f"<div class=brief>{html.escape(row['brief'])}</div><div class=tags>{''.join(tags)}</div>"
                         f"<div class=players>{''.join(players)}</div></div>")
    parts.append(f"</main><script>{SCRIPT}</script></body></html>")
    PAGE.parent.mkdir(parents=True, exist_ok=True)
    PAGE.write_text("\n".join(parts))
    print(f"wrote {PAGE.relative_to(REPO)} ({len(rows)} sounds)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
