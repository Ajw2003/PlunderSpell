"""Build the art bible: per-Age handoff sheets and the structures/enemies/plunder mood board.

Reads docs/art/data/<age>.json (shape documented in Tools/ArtBible/README.md), validates every
Age against docs/art/BRIEF.md and docs/systems/scale.md, then writes docs/art/<age>.md and
docs/generated/plunderspell-art-bible-moodboard.html. Nothing is written unless all Ages pass.
"""

import argparse
import html
import json
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
ART = REPO / "docs" / "art"
DATA = ART / "data"
MOODBOARD = REPO / "docs" / "generated" / "plunderspell-art-bible-moodboard.html"
ORIGINAL_MOODBOARD = REPO / "docs" / "generated" / "plunderspell-moodboard.html"

AGE_ORDER = ["bronze", "high", "late", "powder"]
AGE_BAND = {"bronze": "l-bronze", "high": "l-high", "late": "l-late", "powder": "l-powder"}

# docs/systems/scale.md — clear height above the floor, per zone.
ZONE_CLEAR_HEIGHT = {"Crypt": 3.00, "OuterBailey": 3.60, "InnerWard": 4.00, "Keep": 4.60, "CurtainWall": 5.20}
ROLES = ["patrol", "ranged", "heavy", "special"]
COUNTS = {"structures": 3, "enemies": 4, "items": 5}
HEX = re.compile(r"^#[0-9A-Fa-f]{6}$")

AGE_KEYS = ["slug", "name", "year", "stratum", "headline", "intro", "palette"]
STRUCTURE_KEYS = ["slug", "name", "zone", "footprint", "height_m", "summary", "description", "build",
                  "sockets", "materials", "gameplay", "budget", "concept"]
ENEMY_KEYS = ["slug", "name", "role", "zones", "height_m", "summary", "description", "silhouette", "build",
              "materials", "rig", "breakables", "budget", "dont", "concept"]
ITEM_KEYS = ["slug", "name", "worth", "bulk", "fragility", "artifact", "dimensions", "summary", "description",
             "build", "materials", "grab", "breaks", "budget", "concept"]


# ─────────────────────────────── validation ───────────────────────────────

def validate_age(age, problems):
    where = age.get("slug", "?")

    def need(obj, keys, label):
        for key in keys:
            if key not in obj or obj[key] in ("", None, []):
                problems.append(f"{label}: missing '{key}'")

    def check_materials(materials, label):
        for material in materials or []:
            if not HEX.match(material.get("hex", "")):
                problems.append(f"{label}: material '{material.get('name')}' has bad hex '{material.get('hex')}'")

    def check_concept(entry, label):
        concept = entry.get("concept", "")
        expected = f"concept/{where}/{entry.get('slug')}.svg"
        if concept != expected:
            problems.append(f"{label}: concept should be '{expected}', is '{concept}'")
        elif not (ART / concept).is_file():
            problems.append(f"{label}: concept file docs/art/{concept} does not exist")

    need(age, AGE_KEYS, where)
    if where not in AGE_ORDER:
        problems.append(f"{where}: slug must be one of {AGE_ORDER}")
    for swatch in age.get("palette", []):
        if not HEX.match(swatch.get("hex", "")):
            problems.append(f"{where}: palette '{swatch.get('name')}' has bad hex '{swatch.get('hex')}'")

    for kind, count in COUNTS.items():
        if len(age.get(kind, [])) != count:
            problems.append(f"{where}: needs exactly {count} {kind}, has {len(age.get(kind, []))}")

    for structure in age.get("structures", []):
        label = f"{where}/structure/{structure.get('slug')}"
        need(structure, STRUCTURE_KEYS, label)
        if structure.get("zone") not in ZONE_CLEAR_HEIGHT:
            problems.append(f"{label}: unknown zone '{structure.get('zone')}'")
        check_materials(structure.get("materials"), label)
        check_concept(structure, label)

    roles = sorted(enemy.get("role", "") for enemy in age.get("enemies", []))
    if roles != sorted(ROLES):
        problems.append(f"{where}: enemy roles must be {ROLES} once each, are {roles}")
    for enemy in age.get("enemies", []):
        label = f"{where}/enemy/{enemy.get('slug')}"
        need(enemy, ENEMY_KEYS, label)
        for zone in enemy.get("zones", []):
            if zone not in ZONE_CLEAR_HEIGHT:
                problems.append(f"{label}: unknown zone '{zone}'")
            elif float(enemy.get("height_m", 0)) >= ZONE_CLEAR_HEIGHT[zone]:
                problems.append(f"{label}: {enemy.get('height_m')} m is not under {zone}'s "
                                f"{ZONE_CLEAR_HEIGHT[zone]} m clear height")
        rig = enemy.get("rig", {})
        if not rig.get("skeleton") or not rig.get("animations"):
            problems.append(f"{label}: rig needs 'skeleton' and 'animations'")
        check_materials(enemy.get("materials"), label)
        check_concept(enemy, label)

    for item in age.get("items", []):
        label = f"{where}/item/{item.get('slug')}"
        need(item, [key for key in ITEM_KEYS if key != "artifact"], label)
        if not isinstance(item.get("artifact"), bool):
            problems.append(f"{label}: 'artifact' must be true or false")
        for key in ("worth", "bulk", "fragility"):
            if not isinstance(item.get(key), (int, float)) or item.get(key) <= 0:
                problems.append(f"{label}: '{key}' must be a positive number")
        check_materials(item.get("materials"), label)
        check_concept(item, label)


# ─────────────────────────────── markdown sheets ───────────────────────────────

def md_escape_cell(text):
    return str(text).replace("|", "\\|").replace("\n", " ")


def md_materials(materials):
    lines = ["| Material | Hex | Notes |", "|---|---|---|"]
    for material in materials:
        lines.append(f"| {md_escape_cell(material['name'])} | `{material['hex']}` | {md_escape_cell(material.get('notes', ''))} |")
    return "\n".join(lines)


def md_bullets(entries):
    return "\n".join(f"- {entry}" for entry in entries)


def fragility_words(fragility):
    if fragility >= 999:
        return "unbreakable"
    return f"shatters above {fragility:g} m/s impact"


def render_age_markdown(age):
    slug = age["slug"]
    out = [
        f"# {age['name']} — handoff sheet",
        "",
        f"*Stratum {age['stratum']} · {age['year']} · {age['headline']}*",
        "",
        "> Generated by `Tools/ArtBible/build_art_bible.py` from "
        f"`docs/art/data/{slug}.json`. Edit the JSON, not this file. Rules every entry obeys: "
        "[`BRIEF.md`](BRIEF.md). Scale: [`docs/systems/scale.md`](../systems/scale.md).",
        "",
        age["intro"],
        "",
        "## Age palette",
        "",
        "| Swatch | Hex | Used for |",
        "|---|---|---|",
    ]
    for swatch in age["palette"]:
        out.append(f"| {md_escape_cell(swatch['name'])} | `{swatch['hex']}` | {md_escape_cell(swatch['use'])} |")

    out += ["", "## Contents", ""]
    for kind, title in (("structures", "Structures"), ("enemies", "Enemies"), ("items", "Plunder")):
        out.append(f"- **{title}:** " + " · ".join(f"[{entry['name']}](#{entry['slug']})" for entry in age[kind]))

    out += ["", "---", "", "## Structures", ""]
    for structure in age["structures"]:
        png = structure["concept"].replace(".svg", ".png")
        out += [
            f'<a id="{structure["slug"]}"></a>',
            f"### {structure['name']}",
            "",
            f"![{structure['name']} concept sheet]({png})",
            "",
            f"*{structure['summary']}*",
            "",
            "| Zone | Footprint | Height | Budget | Concept |",
            "|---|---|---|---|---|",
            f"| {structure['zone']} (clear {ZONE_CLEAR_HEIGHT[structure['zone']]:.2f} m) | {md_escape_cell(structure['footprint'])} "
            f"| {structure['height_m']:.2f} m | {md_escape_cell(structure['budget'])} | [SVG]({structure['concept']}) · [PNG]({png}) |",
            "",
            structure["description"],
            "",
            "**Build**",
            "",
            md_bullets(structure["build"]),
            "",
            "**Sockets**",
            "",
            md_bullets(structure["sockets"]),
            "",
            "**Materials**",
            "",
            md_materials(structure["materials"]),
            "",
            "**In play**",
            "",
            md_bullets(structure["gameplay"]),
            "",
            "---",
            "",
        ]

    out += ["## Enemies", ""]
    for enemy in age["enemies"]:
        png = enemy["concept"].replace(".svg", ".png")
        tightest = min(ZONE_CLEAR_HEIGHT[zone] for zone in enemy["zones"])
        out += [
            f'<a id="{enemy["slug"]}"></a>',
            f"### {enemy['name']} — {enemy['role']}",
            "",
            f"![{enemy['name']} concept sheet]({png})",
            "",
            f"*{enemy['summary']}*",
            "",
            "| Height | Posted to | Tightest clear height | Budget | Concept |",
            "|---|---|---|---|---|",
            f"| {enemy['height_m']:.2f} m ({enemy['height_m'] / 1.8:.2f} × standard human) | {', '.join(enemy['zones'])} "
            f"| {tightest:.2f} m | {md_escape_cell(enemy['budget'])} | [SVG]({enemy['concept']}) · [PNG]({png}) |",
            "",
            enemy["description"],
            "",
            f"**Silhouette.** {enemy['silhouette']}",
            "",
            "**Build**",
            "",
            md_bullets(enemy["build"]),
            "",
            "**Materials**",
            "",
            md_materials(enemy["materials"]),
            "",
            "**Rig and animation**",
            "",
            f"Skeleton: {enemy['rig']['skeleton']}",
            "",
            md_bullets(enemy["rig"]["animations"]),
            "",
            "**Breakables and damage states**",
            "",
            md_bullets(enemy["breakables"]),
            "",
            "**Do not**",
            "",
            md_bullets(enemy["dont"]),
            "",
            "---",
            "",
        ]

    out += ["## Plunder", ""]
    for item in age["items"]:
        png = item["concept"].replace(".svg", ".png")
        out += [
            f'<a id="{item["slug"]}"></a>',
            f"### {item['name']}" + (" — artifact" if item["artifact"] else ""),
            "",
            f"![{item['name']} concept sheet]({png})",
            "",
            f"*{item['summary']}*",
            "",
            "| Worth | Bulk | Fragility | Carry | Size | Budget | Concept |",
            "|---|---|---|---|---|---|---|",
            f"| {item['worth']:g} coin | {item['bulk']:g} st | {fragility_words(item['fragility'])} "
            f"| {'two players' if item['bulk'] > 10 else 'one player'} | {md_escape_cell(item['dimensions'])} "
            f"| {md_escape_cell(item['budget'])} | [SVG]({item['concept']}) · [PNG]({png}) |",
            "",
            item["description"],
            "",
            "**Build**",
            "",
            md_bullets(item["build"]),
            "",
            "**Materials**",
            "",
            md_materials(item["materials"]),
            "",
            f"**Grab points.** {item['grab']}",
            "",
            f"**When it breaks.** {item['breaks']}",
            "",
            "**`LootItem` asset values**",
            "",
            f"`Worth = {item['worth']:g}`, `Bulk = {item['bulk']:g}`, `Fragility = {item['fragility']:g}`, "
            f"`IsArtifact = {'true' if item['artifact'] else 'false'}`, `DisplayName = \"{item['name']}\"`",
            "",
            "---",
            "",
        ]
    return "\n".join(out).rstrip() + "\n"


# ─────────────────────────────── mood board ───────────────────────────────

def e(text):
    return html.escape(str(text), quote=True)


def inline_svg(age_slug, entry):
    """Inline a concept SVG with every id prefixed, so 48 sheets on one page cannot collide."""
    source = (ART / entry["concept"]).read_text(encoding="utf-8")
    source = re.sub(r"<\?xml[^>]*\?>", "", source)
    source = re.sub(r"<!DOCTYPE[^>]*>", "", source)
    prefix = f"{age_slug}-{entry['slug']}-"
    ids = set(re.findall(r'\bid="([^"]+)"', source))
    for old in sorted(ids, key=len, reverse=True):
        source = re.sub(rf'\bid="{re.escape(old)}"', f'id="{prefix}{old}"', source)
        source = re.sub(rf"url\(#{re.escape(old)}\)", f"url(#{prefix}{old})", source)
        source = re.sub(rf'href="#{re.escape(old)}"', f'href="#{prefix}{old}"', source)
    source = re.sub(r"<svg\b", f'<svg role="img" aria-label="{e(entry["name"])} concept sheet"', source, count=1)
    return source.strip()


def original_css():
    text = ORIGINAL_MOODBOARD.read_text(encoding="utf-8")
    start = text.find("<style>\n/*")
    return text[start:text.find("</style>", start) + len("</style>")]


EXTRA_CSS = """<style>
/* ── ART BIBLE additions: folios for full concept sheets ── */
.folios{ display:grid; grid-template-columns: repeat(auto-fill, minmax(min(100%,520px),1fr)); gap:1px; background:var(--line-soft); border:1px solid var(--line-soft); }
.folios.wide{ grid-template-columns: 1fr; }
.folio{ background:var(--ash); display:flex; flex-direction:column; }
.folio .sheet{ background:var(--bone-black); border-bottom:1px solid var(--line-soft); }
.folio .sheet svg{ display:block; width:100%; height:auto; }
.folio .cap{ padding:20px 22px 22px; display:flex; flex-direction:column; gap:10px; flex:1; }
.folio .era{ font-family:var(--ff-mono); font-size:10px; letter-spacing:.16em; text-transform:uppercase; color:var(--vellum-faint); }
.folio .nm{ font-family:var(--ff-display); font-size:1.28rem; font-weight:600; line-height:1.15; }
.folio .sum{ font-family:var(--ff-display); font-weight:500; color:var(--vellum); font-size:1rem; }
.folio p{ font-size:.88rem; line-height:1.6; color:var(--vellum-dim); }
.folio .facts{ font-family:var(--ff-mono); font-size:11px; color:var(--vellum-dim); line-height:1.9; }
.folio .facts b{ color:var(--vellum); font-weight:400; }
.folio .rail .stat{ grid-template-columns: 74px 1fr 64px; }
.spec{ border-top:1px solid var(--line-soft); padding-top:12px; }
.spec summary{ cursor:pointer; font-family:var(--ff-mono); font-size:10.5px; letter-spacing:.12em; text-transform:uppercase; color:var(--verdigris); list-style:none; }
.spec summary::-webkit-details-marker{ display:none; }
.spec summary::after{ content:" ▸"; }
.spec[open] summary::after{ content:" ▾"; }
.spec summary:focus-visible{ outline:1px solid var(--verdigris); outline-offset:3px; }
.spec .k{ font-family:var(--ff-mono); font-size:10px; letter-spacing:.16em; text-transform:uppercase; color:var(--vellum-faint); margin:16px 0 6px; }
.spec ul{ margin:0; padding-left:1.1em; font-size:.84rem; line-height:1.55; color:var(--vellum-dim); }
.spec li{ margin-bottom:.3em; }
.spec p{ font-size:.84rem; }
.spec .mats{ display:grid; gap:6px; font-size:.82rem; color:var(--vellum-dim); }
.spec .mat{ display:grid; grid-template-columns:18px 1fr; gap:10px; align-items:start; }
.spec .mat i{ width:18px; height:18px; border:1px solid var(--line); margin-top:2px; }
.spec .mat b{ color:var(--vellum); font-weight:400; }
.spec .mat code, .spec code{ font-family:var(--ff-mono); font-size:.9em; color:var(--vellum-faint); }
.role{ font-family:var(--ff-mono); font-size:10px; letter-spacing:.16em; text-transform:uppercase; padding:2px 8px; border:1px solid var(--line); margin-left:8px; vertical-align:middle; }
.role.patrol{ color:var(--vellum-dim) } .role.ranged{ color:var(--verdigris) } .role.heavy{ color:var(--madder) } .role.special{ color:var(--lapis) }
.artifact{ color:var(--orpiment); }
.subhead{ font-family:var(--ff-display); font-size:1.5rem; font-weight:600; margin:54px 0 8px; }
.subhead + p{ margin-bottom:22px; }
.agehead{ display:grid; grid-template-columns: 132px 1fr; border:1px solid var(--line-soft); margin-bottom:30px; }
.agehead .depth{ padding:24px 18px; font-family:var(--ff-mono); font-size:11px; line-height:1.5; color:var(--vellum-faint); border-right:1px solid var(--line-soft); }
.agehead .depth .yr{ display:block; color:var(--vellum); font-size:13px; margin-bottom:4px; }
.agehead .body{ padding:24px 22px; background:var(--ash); }
.agehead .tag{ font-family:var(--ff-mono); font-size:10px; letter-spacing:.16em; text-transform:uppercase; margin-bottom:12px; }
.agehead.l-bronze .depth{ background: linear-gradient(180deg, #3A2E18, #241C10); } .agehead.l-bronze .tag{ color:#B98A34 }
.agehead.l-high .depth{ background: linear-gradient(180deg, #1F3A33, #14241F); } .agehead.l-high .tag{ color:var(--verdigris) }
.agehead.l-late .depth{ background: linear-gradient(180deg, #3A2320, #231413); } .agehead.l-late .tag{ color:var(--madder) }
.agehead.l-powder .depth{ background: linear-gradient(180deg, #2B2740, #191728); } .agehead.l-powder .tag{ color:var(--lapis) }
.pigments.small .pig .chip{ height:54px; }
.toc{ display:grid; grid-template-columns: repeat(auto-fit,minmax(220px,1fr)); gap:1px; background:var(--line-soft); border:1px solid var(--line-soft); }
.toc a{ background:var(--ash); padding:18px 20px; text-decoration:none; color:var(--vellum); display:block; }
.toc a:hover{ background:var(--ash-hi); }
.toc .yr{ font-family:var(--ff-mono); font-size:11px; color:var(--vellum-faint); display:block; }
.toc .t{ font-family:var(--ff-display); font-size:1.08rem; font-weight:600; }
@media (max-width: 640px){ .agehead{ grid-template-columns:1fr; } .agehead .depth{ border-right:0; border-bottom:1px solid var(--line-soft); } }
</style>"""

ARTIFACT_MARK = ' <span class="artifact">✦</span>'

SIGIL = """<svg class="sigil" viewBox="0 0 100 100" role="img" aria-label="Plunderspell sigil: a key crossed with a wand inside a summoning ring">
    <circle cx="50" cy="50" r="42" fill="none" stroke="#332D22" stroke-width="1.5"/>
    <circle cx="50" cy="50" r="34" fill="none" stroke="#5FA288" stroke-width="1" stroke-dasharray="3 5"/>
    <path d="M32 68 L66 34" stroke="#C9A227" stroke-width="3" fill="none" stroke-linecap="round"/>
    <circle cx="30" cy="70" r="6.5" fill="none" stroke="#C9A227" stroke-width="3"/>
    <path d="M58 42 l7 7 M63 37 l6 6" stroke="#C9A227" stroke-width="3" fill="none" stroke-linecap="round"/>
    <path d="M68 68 L34 34" stroke="#7A6AA0" stroke-width="2.5" fill="none" stroke-linecap="round"/>
    <circle cx="70" cy="70" r="4" fill="#7A6AA0"/>
  </svg>"""


def stat(label, fraction, value, kind=""):
    width = max(2, min(100, round(fraction * 100)))
    return (f'<div class="stat {kind}"><span>{e(label)}</span><div class="bar"><i style="width:{width}%"></i></div>'
            f"<b>{e(value)}</b></div>")


def spec_details(entry, kind):
    """The whole handoff sheet for one entry, folded under the card, so it travels with the page wherever it is opened."""
    def bullets(title, lines):
        return f'<div class="k">{e(title)}</div><ul>' + "".join(f"<li>{e(line)}</li>" for line in lines) + "</ul>"

    def para(title, text):
        return f'<div class="k">{e(title)}</div><p>{e(text)}</p>'

    materials = '<div class="k">Materials</div><div class="mats">' + "".join(
        f'<div class="mat"><i style="background:{e(m["hex"])}"></i><div><b>{e(m["name"])}</b> <code>{e(m["hex"])}</code><br>{e(m.get("notes", ""))}</div></div>'
        for m in entry["materials"]) + "</div>"
    parts = [para("Budget", entry["budget"]), bullets("Build", entry["build"]), materials]
    if kind == "structure":
        parts += [bullets("Sockets", entry["sockets"]), bullets("In play", entry["gameplay"])]
    elif kind == "enemy":
        parts += [para("Skeleton", entry["rig"]["skeleton"]), bullets("Animations", entry["rig"]["animations"]),
                  bullets("Breakables and damage states", entry["breakables"]), bullets("Do not", entry["dont"])]
    else:
        parts += [para("Grab points", entry["grab"]), para("When it breaks", entry["breaks"]),
                  '<div class="k">LootItem asset values</div><p><code>'
                  + e(f"Worth = {entry['worth']:g} · Bulk = {entry['bulk']:g} · Fragility = {entry['fragility']:g} · "
                      f"IsArtifact = {'true' if entry['artifact'] else 'false'}") + "</code></p>"]
    return f'<details class="spec"><summary>Full handoff sheet</summary>{"".join(parts)}</details>'


def render_structure(age, structure):
    return f"""
    <article class="folio" id="{e(age['slug'])}-{e(structure['slug'])}">
      <div class="sheet">{inline_svg(age['slug'], structure)}</div>
      <div class="cap">
        <div class="era">{e(age['name'])} · {e(structure['zone'])}</div>
        <div class="nm">{e(structure['name'])}</div>
        <div class="sum">{e(structure['summary'])}</div>
        <p>{e(structure['description'])}</p>
        <div class="facts"><b>Footprint:</b> {e(structure['footprint'])} · <b>Height:</b> {structure['height_m']:.2f} m<br>
        <b>Sockets:</b> {e(' · '.join(structure['sockets']))}<br><b>In play:</b> {e(' · '.join(structure['gameplay']))}</div>
        {spec_details(structure, 'structure')}
      </div>
    </article>"""


def render_enemy(age, enemy):
    tightest = min(ZONE_CLEAR_HEIGHT[zone] for zone in enemy["zones"])
    return f"""
    <article class="folio" id="{e(age['slug'])}-{e(enemy['slug'])}">
      <div class="sheet">{inline_svg(age['slug'], enemy)}</div>
      <div class="cap">
        <div class="era">{e(age['name'])} · {e(', '.join(enemy['zones']))}</div>
        <div class="nm">{e(enemy['name'])}<span class="role {e(enemy['role'])}">{e(enemy['role'])}</span></div>
        <div class="sum">{e(enemy['summary'])}</div>
        <p>{e(enemy['description'])}</p>
        <p><b style="color:var(--vellum)">Silhouette.</b> {e(enemy['silhouette'])}</p>
        <div class="rail">
          {stat('Height', enemy['height_m'] / 3.0, f"{enemy['height_m']:.2f} m")}
          {stat('Headroom', (tightest - enemy['height_m']) / tightest, f"{tightest - enemy['height_m']:.2f} m", 'heft')}
        </div>
        {spec_details(enemy, 'enemy')}
      </div>
    </article>"""


def render_item(age, item, max_worth):
    fragile = 0.0 if item["fragility"] >= 999 else max(0.0, 1 - item["fragility"] / 12)
    fragility_text = "never" if item["fragility"] >= 999 else f"{item['fragility']:g} m/s"
    return f"""
    <article class="folio" id="{e(age['slug'])}-{e(item['slug'])}">
      <div class="sheet">{inline_svg(age['slug'], item)}</div>
      <div class="cap">
        <div class="era">{e(age['name'])} · {'two carriers' if item['bulk'] > 10 else 'one carrier'}{' · <span class="artifact">artifact</span>' if item['artifact'] else ''}</div>
        <div class="nm">{e(item['name'])}</div>
        <div class="sum">{e(item['summary'])}</div>
        <p>{e(item['description'])}</p>
        <div class="facts"><b>Size:</b> {e(item['dimensions'])}</div>
        <div class="rail">
          {stat('Worth', item['worth'] / max_worth, f"{item['worth']:g}")}
          {stat('Bulk', item['bulk'] / 25, f"{item['bulk']:g} st", 'heft')}
          {stat('Breaks', fragile, fragility_text, 'risk')}
        </div>
        {spec_details(item, 'item')}
      </div>
    </article>"""


def render_age_section(age, max_worth):
    palette = "".join(
        f'<div class="pig"><div class="chip" style="background:{e(s["hex"])}"></div><div class="meta">'
        f'<div class="nm">{e(s["name"])}</div><div class="hx">{e(s["hex"])}</div><div class="use">{e(s["use"])}</div></div></div>'
        for s in age["palette"])
    structures = "".join(render_structure(age, s) for s in age["structures"])
    enemies = "".join(render_enemy(age, x) for x in sorted(age["enemies"], key=lambda x: ROLES.index(x["role"])))
    items = "".join(render_item(age, i, max_worth) for i in age["items"])
    return f"""
<!-- ════════════ {e(age['name']).upper()} ════════════ -->
<section id="{e(age['slug'])}">
  <div class="eyebrow">Stratum {e(age['stratum'])} · {e(age['year'])}</div>
  <div class="agehead {AGE_BAND[age['slug']]}">
    <div class="depth"><span class="yr">{e(age['year'])}</span>Stratum {e(age['stratum'])}</div>
    <div class="body">
      <div class="tag">{e(age['name'])}</div>
      <h2 style="margin-bottom:.35em">{e(age['headline'])}</h2>
      <p class="dim" style="font-size:.98rem">{e(age['intro'])}</p>
    </div>
  </div>
  <div class="pigments small">{palette}</div>

  <h3 class="subhead">The stonework</h3>
  <p class="spine dim">Three pieces of building. Each sits on the twelve-metre cell and knows its own joins.</p>
  <div class="folios wide">{structures}
  </div>

  <h3 class="subhead">The household</h3>
  <p class="spine dim">Four defenders: the one who walks the walls, the one who shoots, the one you avoid, and the one this century is remembered for.</p>
  <div class="folios">{enemies}
  </div>

  <h3 class="subhead">The plunder</h3>
  <p class="spine dim">Five things worth the trip. Worth is coin at the lair; bulk is stone in your arms; the red bar is how little it takes to ruin it.</p>
  <div class="folios">{items}
  </div>
</section>"""


def render_moodboard(ages):
    max_worth = max(item["worth"] for age in ages for item in age["items"])
    toc = "".join(
        f'<a href="#{e(a["slug"])}"><span class="yr">{e(a["year"])} · Stratum {e(a["stratum"])}</span>'
        f'<span class="t">{e(a["name"])}</span></a>' for a in ages)
    roster_rows = "".join(
        f"<tr><td>{e(a['name'])}</td><td><a href=\"#{e(a['slug'])}-{e(x['slug'])}\">{e(x['name'])}</a></td>"
        f"<td class=\"ms\">{e(x['role'])}</td><td class=\"ms\">{x['height_m']:.2f} m</td><td class=\"ms\">{e(', '.join(x['zones']))}</td></tr>"
        for a in ages for x in sorted(a["enemies"], key=lambda x: ROLES.index(x["role"])))
    plunder_rows = "".join(
        f"<tr><td>{e(a['name'])}</td><td><a href=\"#{e(a['slug'])}-{e(i['slug'])}\">{e(i['name'])}</a>"
        f"{ARTIFACT_MARK if i['artifact'] else ''}</td>"
        f"<td class=\"ms\">{i['worth']:g}</td><td class=\"ms\">{i['bulk']:g} st</td>"
        f"<td class=\"ms\">{'never' if i['fragility'] >= 999 else str(i['fragility']) + ' m/s'}</td></tr>"
        for a in ages for i in a["items"])
    sections = "".join(render_age_section(age, max_worth) for age in ages)
    counts = {kind: sum(len(a[kind]) for a in ages) for kind in COUNTS}
    return f"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Plunderspell Art Bible</title>
<meta name="description" content="Structures, defenders and plunder of the four Ages of Plunderspell — concept sheets and build notes for 3D artists.">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Eczar:wght@500;600;700;800&family=Spectral:ital,wght@0,300;0,400;0,600;1,400&family=Overpass+Mono:wght@400;600&display=swap">
<!-- Generated by Tools/ArtBible/build_art_bible.py from docs/art/data/*.json. Do not hand-edit. -->
</head>
<body>
{original_css()}
{EXTRA_CSS}
<div class="wrap">

<section class="hero">
  {SIGIL}
  <div class="sub">An inventory · of things to build</div>
  <h1>Plunder<em>spell</em></h1>
  <p class="hook">The <b>stonework</b>, the <b>household</b> and the <b>plunder</b> of four centuries — drawn plainly enough that someone can go and build them.</p>
  <div class="stamp"><span>{counts['structures']} structures</span><span>{counts['enemies']} defenders</span><span>{counts['items']} treasures</span><span>4 Ages</span></div>
</section>

<section>
  <div class="eyebrow">The herald, briefly</div>
  <h2 class="spine">I have been asked to describe the furniture</h2>
  <p class="lede spine">Last time I sold you the idea. This time I am handing you the inventory — every wall you will break, every man who will object, and every heavy golden thing you will drop on a staircase.</p>
  <p class="spine dim">Each sheet below is drawn against one rule: a standard man is <b style="color:var(--vellum)">1.80 metres</b>, and nothing that guards a room may be taller than that room. Rooms sit on a twelve-metre cell. Gold is orpiment and only orpiment; verdigris is only ever something you may touch; the voice alone is lapis. The full build notes for each thing — sizes, materials, rigs, how it breaks — are in the handoff sheet it links to.</p>
  <div class="toc" style="margin-top:30px">{toc}</div>
  <div class="grid g3" style="margin-top:30px">
    <div><div class="kicker">The measure</div><p style="font-size:.9rem;color:var(--vellum-dim)">1.80 m standard human. Clear heights: Crypt 3.00 · Outer Bailey 3.60 · Inner Ward 4.00 · Keep 4.60 · Curtain Wall open. Archways 2.60 m wide.</p></div>
    <div><div class="kicker">The household</div><p style="font-size:.9rem;color:var(--vellum-dim)">Men and animals only — no spirits, no constructs. Four to an Age: the patrol, the ranged, the heavy, and the one the century is remembered for.</p></div>
    <div><div class="kicker">The plunder</div><p style="font-size:.9rem;color:var(--vellum-dim)">Worth in coin, bulk in stone, fragility in the speed that ruins it. Over ten stone takes two of you. These are the numbers the game's loot assets carry.</p></div>
  </div>
</section>
{sections}

<!-- ════════════ LEDGER ════════════ -->
<section>
  <div class="eyebrow">Here endeth the poetry</div>
  <h2 class="spine">The roster, in a plainer voice</h2>
  <p class="spine dim" style="margin-bottom:24px">Every defender and every treasure on one page, for whoever has to schedule the work.</p>
  <div class="tablewrap"><table><thead><tr><th>Age</th><th>Defender</th><th>Role</th><th>Height</th><th>Posted to</th></tr></thead><tbody>{roster_rows}</tbody></table></div>
  <div class="tablewrap" style="margin-top:30px"><table><thead><tr><th>Age</th><th>Treasure</th><th>Worth</th><th>Bulk</th><th>Breaks at</th></tr></thead><tbody>{plunder_rows}</tbody></table></div>
</section>

<footer>
  PLUNDERSPELL · ART BIBLE · STRUCTURES, HOUSEHOLD &amp; PLUNDER · COMPANION TO PITCH BIBLE v0.2 · GENERATED FROM docs/art/data
</footer>

</div>
</body>
</html>
"""


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="validate only; write nothing")
    parser.add_argument("--only", choices=AGE_ORDER, help="validate a single Age (implies --check)")
    args = parser.parse_args()

    slugs = [args.only] if args.only else AGE_ORDER
    ages, problems = [], []
    for slug in slugs:
        path = DATA / f"{slug}.json"
        if not path.is_file():
            problems.append(f"{slug}: {path.relative_to(REPO)} does not exist")
            continue
        try:
            age = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as error:
            problems.append(f"{slug}: {path.relative_to(REPO)} is not valid JSON: {error}")
            continue
        validate_age(age, problems)
        ages.append(age)

    if problems:
        print(f"FAILED — {len(problems)} problem(s):")
        for problem in problems:
            print(f"  - {problem}")
        return 1
    print(f"OK — {len(ages)} Age(s) validated: "
          + ", ".join(f"{a['slug']} ({len(a['structures'])}s/{len(a['enemies'])}e/{len(a['items'])}i)" for a in ages))
    if args.check or args.only:
        return 0

    for age in ages:
        target = ART / f"{age['slug']}.md"
        target.write_text(render_age_markdown(age), encoding="utf-8")
        print(f"wrote {target.relative_to(REPO)}")
    MOODBOARD.write_text(render_moodboard(ages), encoding="utf-8")
    print(f"wrote {MOODBOARD.relative_to(REPO)} ({MOODBOARD.stat().st_size // 1024} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
