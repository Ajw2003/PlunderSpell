"""Generates takes for the AI-sourced sounds with the ElevenLabs sound-effects API.

    ELEVENLABS_API_KEY=... python3 -m forge.ai_elevenlabs                 # every A/M row with a prompt
    ELEVENLABS_API_KEY=... python3 -m forge.ai_elevenlabs 'sfx_spell_*'   # only matching rows
    python3 -m forge.ai_elevenlabs --dry-run                              # list what it would ask for

For each manifest row whose `final` is A (AI sound) or M (AI music) and that has an `ai_prompt`,
it asks for --takes takes per variant and saves them to ai/takes/<name>/<name>_<NN>_take<K>.mp3,
logging every request to ai/log.csv. It never overwrites a take, so re-running only fills gaps.

Picking takes is a listening job: open ai/takes/, choose, then promote the keeper, e.g.
    python3 -m forge.promote ai/takes/sfx_spell_ignis_cast/sfx_spell_ignis_cast_01_take2.mp3 \
        sfx_spell_ignis_cast_01 --licence "ElevenLabs <plan>, commercial use"
or pass --auto-promote to promote take 1 of each (fast, unheard: fine for a first pass).

Every file this makes must be declared in Steam's AI content survey; ai/log.csv is that record.
"""

import argparse
import csv
import datetime
import fnmatch
import json
import os
import re
import sys
import time
import urllib.error
import urllib.request

from .build import FORGE, MANIFEST
from .promote import promote

ENDPOINT = "https://api.elevenlabs.io/v1/sound-generation"
TAKES = FORGE / "ai" / "takes"
LOG = FORGE / "ai" / "log.csv"
STYLE = ("Medieval castle heist game, stylised and slightly comic, recorded dry and close, "
         "no music, no reverb unless asked: ")
MODEL = "eleven_text_to_sound_v2"   # the only model the API accepts; the one that supports loop
MIN_SECONDS, MAX_SECONDS = 0.5, 30.0
REPORT = FORGE / "build_report.csv"


def duration_for(row):
    m = re.search(r"(\d+(?:\.\d+)?) seconds?", prompt_for(row))
    if m:
        seconds = float(m.group(1))
    else:
        durs = [float(d) for d in re.findall(r"\bdur=([\d.]+)", row["recipe"])]
        seconds = max(durs) if durs else 2.0
    return max(MIN_SECONDS, min(MAX_SECONDS, seconds))


def prompt_for(row):
    """The AI prompt, or the manifest's one-line brief for a row that was never given a prompt."""
    return row["ai_prompt"] or row["brief"]


def placeholder_names():
    """Sounds whose files are still synthesised stand-ins, minus guard voices (friends record those)
    and music (its own plan)."""
    with REPORT.open(newline="") as f:
        names = {r["name"] for r in csv.DictReader(f) if r["status"] == "placeholder"}
    return {n for n in names if not n.startswith(("vo_", "mus_"))}


def request_take(api_key, prompt, seconds, influence, loop=False):
    body = json.dumps({"text": prompt, "duration_seconds": seconds, "prompt_influence": influence,
                       "loop": loop, "model_id": MODEL}).encode()
    req = urllib.request.Request(ENDPOINT, data=body, method="POST",
                                 headers={"xi-api-key": api_key, "Content-Type": "application/json",
                                          "Accept": "audio/mpeg"})
    with urllib.request.urlopen(req, timeout=120) as resp:
        return resp.read()


def log(entry):
    LOG.parent.mkdir(parents=True, exist_ok=True)
    new = not LOG.exists()
    with LOG.open("a", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=list(entry.keys()))
        if new:
            writer.writeheader()
        writer.writerow(entry)


def main(argv):
    parser = argparse.ArgumentParser(description="Generate AI takes with ElevenLabs")
    parser.add_argument("patterns", nargs="*", default=["*"])
    parser.add_argument("--takes", type=int, default=3, help="takes per variant (default 3)")
    parser.add_argument("--influence", type=float, default=0.4, help="prompt influence 0-1 (default 0.4)")
    parser.add_argument("--auto-promote", action="store_true", help="promote take 1 of every variant")
    parser.add_argument("--licence", default="ElevenLabs paid plan, commercial use (AI-generated)")
    parser.add_argument("--placeholders", action="store_true",
                        help="also do every sound still synthesised as a stand-in (not guard voices or music), "
                             "using its brief when it has no AI prompt")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args(argv)

    stand_ins = placeholder_names() if args.placeholders else set()
    rows = [r for r in csv.DictReader(MANIFEST.open())
            if ((r["final"] in ("A", "M") and r["ai_prompt"]) or r["name"] in stand_ins)
            and any(fnmatch.fnmatch(r["name"], p) for p in args.patterns)]
    jobs = [(r, v, k) for r in rows for v in range(1, int(r["variants"]) + 1) for k in range(1, args.takes + 1)]
    print(f"{len(rows)} sounds, {len(jobs)} takes requested "
          f"(~{sum(duration_for(r) for r, _, _ in jobs):.0f} s of audio)")
    if args.dry_run:
        for r in rows:
            loop = " loop" if r["loop"] == "1" else ""
            print(f"  {r['name']} x{r['variants']} ({duration_for(r):.1f}s{loop}): {prompt_for(r)}")
        return 0
    api_key = os.environ.get("ELEVENLABS_API_KEY")
    if not api_key:
        print("ELEVENLABS_API_KEY is not set; nothing was generated. See Tools/AudioForge/README.md.")
        return 2

    failures = 0
    for row, variant, take in jobs:
        out = TAKES / row["name"] / f"{row['name']}_{variant:02d}_take{take}.mp3"
        if out.exists():
            continue
        out.parent.mkdir(parents=True, exist_ok=True)
        prompt, seconds = STYLE + prompt_for(row), duration_for(row)
        loop = row["loop"] == "1"
        try:
            audio = request_take(api_key, prompt, seconds, args.influence, loop)
        except urllib.error.HTTPError as exc:
            detail = exc.read().decode(errors="replace")[:300]
            print(f"FAILED {out.name}: HTTP {exc.code} {detail}")
            failures += 1
            if exc.code in (401, 403):
                print("The key was refused; stopping.")
                return 1
            time.sleep(2)
            continue
        out.write_bytes(audio)
        log(dict(file=str(out.relative_to(FORGE)), name=row["name"], variant=variant, take=take,
                 service="elevenlabs sound-generation " + MODEL, prompt=prompt, seconds=seconds, loop=loop,
                 influence=args.influence, date=datetime.datetime.now().isoformat(timespec="seconds")))
        print(f"saved {out.relative_to(FORGE)}")
        if args.auto_promote and take == 1:
            promote(out, f"{row['name']}_{variant:02d}", args.licence, str(out.relative_to(FORGE)))
    print(f"done, {failures} failure(s). Next: python3 -m forge.build && python3 -m forge.check")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
