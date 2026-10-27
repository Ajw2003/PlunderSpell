"""Builds takes-tts/listen.html: every stand-in guard line on one page with a play button, the words,
the tone it was asked for and how it was made, grouped by Age and situation. Open it by double-clicking;
the audio is linked by relative path, so nothing is uploaded anywhere.

    python Tools/GuardVoice/make_listen_page.py
"""

import csv
import html
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
TAKES = HERE / "takes-tts"

STYLE = """
:root{--bg:#14110e;--panel:#1d1915;--line:#3a332b;--text:#efe6d8;--dim:#b3a793;--gold:#e0b25a}
@media (prefers-color-scheme: light){:root{--bg:#f6f0e4;--panel:#fffaf0;--line:#d8cdb8;--text:#2a241b;--dim:#6b5f4c;--gold:#8a5a00}}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:16px/1.5 system-ui,sans-serif}
main{max-width:980px;margin:0 auto;padding:24px 16px 64px}h1{font-size:1.6rem;margin:0 0 4px}
p.lead{color:var(--dim);margin:0 0 20px}nav{display:flex;gap:8px;flex-wrap:wrap;margin-bottom:20px}
nav a{color:var(--gold);border:1px solid var(--line);border-radius:6px;padding:6px 12px;text-decoration:none}
h2{font-size:1.25rem;margin:32px 0 4px;color:var(--gold)}h3{font-size:1rem;margin:20px 0 6px;text-transform:uppercase;letter-spacing:.08em;color:var(--dim)}
.row{display:grid;grid-template-columns:minmax(0,1fr) 300px;gap:8px 16px;align-items:center;background:var(--panel);border:1px solid var(--line);border-radius:8px;padding:10px 12px;margin:6px 0}
.say{font-weight:600}.meta{color:var(--dim);font-size:.85rem}.tag{border:1px solid var(--line);border-radius:4px;padding:0 6px;margin-right:6px;font-size:.78rem}
audio{width:100%}@media (max-width:640px){.row{grid-template-columns:1fr}}
"""


def main():
    data = json.loads((HERE / "record-lines.json").read_text(encoding="utf-8"))
    with (TAKES / "tts-manifest.csv").open(encoding="utf-8-sig", newline="") as handle:
        made = {row["file"]: row for row in csv.DictReader(handle)}

    parts = [
        "<!doctype html><html lang='en'><head><meta charset='utf-8'>",
        "<meta name='viewport' content='width=device-width,initial-scale=1'>",
        "<title>Guard stand-in clips</title>",
        f"<style>{STYLE}</style></head><body><main>",
        "<h1>Guard stand-in clips</h1>",
        "<p class='lead'>Computer-voice placeholders (Windows David and Zira, plus synthesised snores) for "
        "every guard line. Real recordings replace them file by file. Listen for: does each line fit its "
        "situation, and is the loud/quiet split clear?</p><nav>",
    ]
    parts += [f"<a href='#{age['id']}'>{html.escape(age['title'])}</a>" for age in data["ages"]]
    parts.append("</nav>")

    count = 0
    for age in data["ages"]:
        parts.append(f"<h2 id='{age['id']}'>{html.escape(age['title'])}</h2>")
        parts.append(f"<p class='meta'>{html.escape(age['language'])}</p>")
        current = None
        for line in age["lines"]:
            if line["situation"] != current:
                current = line["situation"]
                parts.append(f"<h3>{html.escape(current)}</h3>")
            path = TAKES / age["id"] / line["file"]
            if not path.exists():
                raise SystemExit(f"{line['file']} is missing: run make_tts_base.ps1 first.")
            info = made.get(line["file"], {})
            spoken = info.get("spoken") or "(snore, synthesised)"
            treatment = info.get("treatment", "")
            voice = info.get("voice", "").replace("Microsoft ", "").replace(" Desktop", "")
            parts.append(
                "<div class='row'><div>"
                f"<div class='say'>{html.escape(spoken)}</div>"
                f"<div class='meta'><span class='tag'>{html.escape(line['loudness'])}</span>"
                f"<span class='tag'>{html.escape(voice or 'synth')}</span>"
                f"{html.escape(treatment)}. Wanted: {html.escape(line['tone'])}</div></div>"
                f"<audio controls preload='none' src='{age['id']}/{html.escape(line['file'])}'></audio></div>"
            )
            count += 1

    parts.append("</main></body></html>")
    (TAKES / "listen.html").write_text("".join(parts), encoding="utf-8")
    print(f"Wrote {TAKES / 'listen.html'} with {count} clips")


if __name__ == "__main__":
    main()
