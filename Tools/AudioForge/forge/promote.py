"""Files a finished sound into final/ so the next build ships it instead of the placeholder.

    python3 -m forge.promote <source file> <name>_<NN> --licence "<who made it, on what terms>"
    python3 -m forge.promote --scan --licence "<terms>"

The first form copies one file (an AI take, a recording, a Suno download) to
final/<name>_<NN>.<ext> and records it in final/LICENCES.csv. The second records every file already
dropped into final/ by hand that has no licence row yet, which suits a batch of recordings a friend
sent over already named correctly. Either way, run `python3 -m forge.build` afterwards.
"""

import argparse
import csv
import datetime
import re
import shutil
import sys

from .build import FINAL, FINAL_EXTENSIONS, MANIFEST

LICENCES = FINAL / "LICENCES.csv"
FIELDS = ("file", "name", "variant", "licence", "origin", "date")
TARGET = re.compile(r"^(?P<name>[a-z0-9_]+)_(?P<variant>\d{2})$")


def manifest_rows():
    return {r["name"]: r for r in csv.DictReader(MANIFEST.open())}


def load_licences():
    if not LICENCES.exists():
        return []
    return list(csv.DictReader(LICENCES.open()))


def save_licences(entries):
    FINAL.mkdir(exist_ok=True)
    with LICENCES.open("w", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=FIELDS)
        writer.writeheader()
        writer.writerows(sorted(entries, key=lambda e: e["file"]))


def validate_target(target, rows):
    m = TARGET.match(target)
    if not m:
        raise SystemExit(f"target must look like <name>_<NN>, e.g. vo_high_knight_chase_01 (got {target!r})")
    name, variant = m["name"], int(m["variant"])
    if name not in rows:
        raise SystemExit(f"{name!r} is not in manifest.csv")
    if not 1 <= variant <= int(rows[name]["variants"]):
        raise SystemExit(f"{name} has {rows[name]['variants']} variant(s); {variant:02d} is out of range")
    return name, variant


def record(entries, filename, name, variant, licence, origin):
    entries = [e for e in entries if e["file"] != filename]
    entries.append(dict(file=filename, name=name, variant=f"{variant:02d}", licence=licence, origin=origin,
                        date=datetime.date.today().isoformat()))
    return entries


def promote(source, target, licence, origin):
    rows = manifest_rows()
    name, variant = validate_target(target, rows)
    ext = source.suffix.lower()
    if ext not in FINAL_EXTENSIONS:
        raise SystemExit(f"unsupported file type {ext}; use one of {', '.join(FINAL_EXTENSIONS)}")
    FINAL.mkdir(exist_ok=True)
    for old_ext in FINAL_EXTENSIONS:  # one finished file per slot
        old = FINAL / f"{name}_{variant:02d}{old_ext}"
        if old.exists() and old_ext != ext:
            old.unlink()
    dest = FINAL / f"{name}_{variant:02d}{ext}"
    shutil.copyfile(source, dest)
    save_licences(record(load_licences(), dest.name, name, variant, licence, origin or str(source.name)))
    print(f"promoted {source} -> final/{dest.name}")
    return dest


def scan(licence, origin):
    rows = manifest_rows()
    entries = load_licences()
    known = {e["file"] for e in entries}
    added, refused = 0, []
    for path in sorted(FINAL.glob("*.*")):
        if path.name in ("LICENCES.csv", "README.md") or path.name in known:
            continue
        try:
            name, variant = validate_target(path.stem, rows)
        except SystemExit as why:
            refused.append(f"{path.name}: {why}")
            continue
        entries = record(entries, path.name, name, variant, licence, origin or "dropped into final/")
        added += 1
        print(f"recorded final/{path.name}")
    save_licences(entries)
    print(f"{added} file(s) recorded")
    for line in refused:
        print(f"REFUSED {line}")
    return 1 if refused else 0


def main(argv):
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("source", nargs="?", help="file to promote")
    parser.add_argument("target", nargs="?", help="<name>_<NN>, e.g. vo_high_knight_chase_01")
    parser.add_argument("--licence", required=True, help='e.g. "ElevenLabs Creator plan, commercial use"')
    parser.add_argument("--origin", default="", help="where it came from (prompt id, performer, track url)")
    parser.add_argument("--scan", action="store_true", help="record every unlicensed file already in final/")
    args = parser.parse_args(argv)
    if args.scan:
        return scan(args.licence, args.origin)
    elif args.source and args.target:
        from pathlib import Path
        promote(Path(args.source), args.target, args.licence, args.origin)
    else:
        parser.error("give <source> <target>, or --scan")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
