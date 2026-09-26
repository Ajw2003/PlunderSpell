#!/usr/bin/env python3
"""Build a browsable gallery of every ArtForge review sheet.

Writes docs/generated/art-bible-models/: web-sized JPEG copies of each sheet in
docs/art/models/<age>/<slug>.png (2400 px wide) and an index.html that shows them
grouped by Age, plunder first, then enemies, with a full-size viewer.

    python3 Tools/ArtForge/gallery.py
"""

import html
import json
import os

from PIL import Image

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(REPO, "docs", "generated", "art-bible-models")
AGES = ["bronze", "high", "late", "powder"]
KIND_TITLE = {"items": "Plunder", "enemies": "The household"}


def sheets():
    for age in AGES:
        data = json.load(open(os.path.join(REPO, "docs", "art", "data", f"{age}.json")))
        for kind in ("items", "enemies"):
            for entry in data[kind]:
                src = os.path.join(REPO, "docs", "art", "models", age, f"{entry['slug']}.png")
                if not os.path.exists(src):
                    print(f"MISSING review sheet: {os.path.relpath(src, REPO)}")
                    continue
                yield data, kind, entry, src


def build():
    os.makedirs(os.path.join(OUT, "img"), exist_ok=True)
    sections, count = {}, 0
    for data, kind, entry, src in sheets():
        image = Image.open(src).convert("RGB")
        width, height = image.size
        image = image.resize((2400, round(height * 2400 / width)), Image.LANCZOS)
        rel = f"img/{data['slug']}-{entry['slug']}.jpg"
        image.save(os.path.join(OUT, rel), quality=84, optimize=True)
        if kind == "items":
            meta = f"{entry['worth']:g} coin · {entry['bulk']:g} st" + (" · artifact" if entry.get("artifact") else "")
        else:
            meta = f"{entry['role']} · {entry['height_m']:.2f} m"
        sections.setdefault(data["slug"], {"data": data, "items": [], "enemies": []})[kind].append(
            (entry, rel, meta))
        count += 1

    nav, body = [], []
    for age in AGES:
        section = sections.get(age)
        if not section:
            continue
        data = section["data"]
        nav.append(f'<a href="#{age}">{html.escape(data["name"])}</a>')
        body.append(f'<section id="{age}"><header><span class="yr">{html.escape(data["year"])} · Stratum '
                    f'{html.escape(data["stratum"])}</span><h2>{html.escape(data["name"])}</h2>'
                    f'<p>{html.escape(data["headline"])}</p></header>')
        for kind in ("items", "enemies"):
            if not section[kind]:
                continue
            body.append(f'<h3>{KIND_TITLE[kind]} <span>{len(section[kind])}</span></h3><div class="grid">')
            for entry, rel, meta in section[kind]:
                body.append(
                    f'<figure><button type="button" class="open" data-src="{rel}" '
                    f'data-title="{html.escape(entry["name"])}"><img src="{rel}" loading="lazy" '
                    f'alt="{html.escape(entry["name"])}: concept sheet beside renders of the model"></button>'
                    f'<figcaption><b>{html.escape(entry["name"])}</b><span class="meta">{html.escape(meta)}</span>'
                    f'<span class="sum">{html.escape(entry["summary"])}</span></figcaption></figure>')
            body.append("</div>")
        body.append("</section>")

    page = TEMPLATE.replace("{{NAV}}", "".join(nav)).replace("{{BODY}}", "\n".join(body)).replace(
        "{{COUNT}}", str(count))
    with open(os.path.join(OUT, "index.html"), "w", encoding="utf-8") as handle:
        handle.write(page)
    print(f"wrote {os.path.relpath(OUT, REPO)}/index.html with {count} sheets")


TEMPLATE = """<title>Plunderspell Model Review</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Eczar:wght@600;700&family=Spectral:wght@400&family=Overpass+Mono:wght@400;600&display=swap">
<style>
:root{color-scheme:dark;--bg:#14120E;--ash:#1E1A14;--ash-hi:#282318;--line:#332D22;--vellum:#DCD2BA;--dim:#9A9078;--faint:#635C4C;--verdigris:#5FA288;--orpiment:#C9A227;
--display:"Eczar",Georgia,serif;--body:"Spectral",Georgia,serif;--mono:"Overpass Mono",ui-monospace,Menlo,monospace}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--vellum);font-family:var(--body);line-height:1.55}
.wrap{max-width:1500px;margin:0 auto;padding-inline:20px;padding-block:28px 60px}
.top{display:flex;flex-wrap:wrap;align-items:baseline;gap:10px 28px;border-bottom:1px solid var(--line);padding-bottom:18px;margin-bottom:8px}
.top h1{font-family:var(--display);font-size:clamp(1.8rem,4vw,2.6rem);margin:0;letter-spacing:-.01em}
.top p{margin:0;color:var(--dim);font-size:.95rem;flex:1 1 320px}
nav{position:sticky;top:env(safe-area-inset-top,0px);z-index:5;background:var(--bg);display:flex;flex-wrap:wrap;gap:6px;padding-block:12px;border-bottom:1px solid var(--line)}
nav a{font-family:var(--mono);font-size:11px;letter-spacing:.14em;text-transform:uppercase;color:var(--dim);text-decoration:none;border:1px solid var(--line);padding:6px 11px}
nav a:hover,nav a:focus-visible{color:var(--vellum);border-color:var(--faint);outline:none}
section{padding-top:34px}
section header{display:grid;gap:2px;margin-bottom:6px}
.yr{font-family:var(--mono);font-size:11px;letter-spacing:.2em;text-transform:uppercase;color:var(--faint)}
h2{font-family:var(--display);font-size:1.9rem;margin:0}
section header p{margin:0;color:var(--dim)}
h3{font-family:var(--mono);font-size:12px;letter-spacing:.2em;text-transform:uppercase;color:var(--verdigris);margin:22px 0 10px;font-weight:600}
h3 span{color:var(--faint);margin-left:6px}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(min(100%,560px),1fr));gap:14px}
figure{margin:0;background:var(--ash);border:1px solid var(--line);display:flex;flex-direction:column}
.open{all:unset;cursor:zoom-in;display:block}
.open:focus-visible{outline:2px solid var(--verdigris);outline-offset:-2px}
.open img{display:block;width:100%;height:auto;max-width:100%;background:#0E0C09}
figcaption{padding:12px 14px 14px;display:grid;gap:3px}
figcaption b{font-family:var(--display);font-size:1.15rem;font-weight:600}
.meta{font-family:var(--mono);font-size:11px;letter-spacing:.08em;color:var(--orpiment)}
.sum{font-size:.9rem;color:var(--dim)}
dialog{border:0;padding:0;background:#0B0A08;max-width:100vw;max-height:100vh;width:100vw;height:100vh}
dialog::backdrop{background:rgba(0,0,0,.85)}
.viewer{height:100%;display:flex;flex-direction:column}
.bar{display:flex;align-items:center;gap:12px;padding:10px 16px;border-bottom:1px solid var(--line);font-family:var(--mono);font-size:12px;color:var(--dim)}
.bar strong{color:var(--vellum);font-family:var(--display);font-size:1.05rem;flex:1}
.bar button{font:inherit;color:var(--vellum);background:var(--ash);border:1px solid var(--line);padding:6px 12px;cursor:pointer}
.bar button:hover,.bar button:focus-visible{border-color:var(--verdigris);outline:none}
.pane{flex:1;overflow:auto}
.pane img{display:block;width:100%;min-width:1200px;height:auto}
@media (max-width:640px){.pane img{min-width:1000px}}
</style>
<div class="wrap">
  <div class="top"><h1>Model review</h1>
  <p>{{COUNT}} review sheets from ArtForge: each concept sheet sits on the left, with renders of the finished model on the right. Enemies also show two posed views, to prove the skin bends. Select a sheet to see it full size.</p></div>
  <nav aria-label="Ages">{{NAV}}</nav>
  {{BODY}}
</div>
<dialog id="viewer" aria-label="Full-size review sheet"><div class="viewer">
  <div class="bar"><strong id="vt"></strong><button type="button" id="prev">‹ Prev</button><button type="button" id="next">Next ›</button><button type="button" id="close">Close</button></div>
  <div class="pane"><img id="vi" alt=""></div></div></dialog>
<script>
const buttons=[...document.querySelectorAll('.open')];const dlg=document.getElementById('viewer');
const vi=document.getElementById('vi'),vt=document.getElementById('vt');let at=0;
function show(i){at=(i+buttons.length)%buttons.length;const b=buttons[at];vi.src=b.dataset.src;vi.alt=b.dataset.title+' review sheet';vt.textContent=b.dataset.title+'  ·  '+(at+1)+' / '+buttons.length;}
buttons.forEach((b,i)=>b.addEventListener('click',()=>{show(i);dlg.showModal();}));
document.getElementById('prev').onclick=()=>show(at-1);document.getElementById('next').onclick=()=>show(at+1);
document.getElementById('close').onclick=()=>dlg.close();
dlg.addEventListener('keydown',e=>{if(e.key==='ArrowRight')show(at+1);if(e.key==='ArrowLeft')show(at-1);});
</script>
"""

if __name__ == "__main__":
    build()
