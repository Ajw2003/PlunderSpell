"""Builds the CLAP index of every audio file under the library roots in finder/roots.json.

    python Tools/AudioForge/audioforge.py index [--roots roots.json] [--rebuild] [name ...]

Each file is cut into windows of at most 10 s (hop 5 s) and embedded. Output, per root:
finder/index/<root>.npz (unit vectors, one per window) and finder/index/<root>.csv (file relative to
the root, window start, window length, file length, file size and mtime). A file whose path, size
and mtime are unchanged is not embedded again. Nothing is ever written into a library root.
The index of a root with "commit": false is listed in finder/index/.gitignore.
"""

import argparse
import csv
import json
import sys
import time
from pathlib import Path

import numpy as np
import soundfile as sf

from . import clap

FINDER = Path(__file__).resolve().parent
FORGE = FINDER.parent
INDEX = FINDER / "index"
ROOTS = FINDER / "roots.json"
EXTENSIONS = {".ogg", ".wav", ".flac", ".mp3"}
WINDOW_S, HOP_S = 10.0, 5.0
FIELDS = ["file", "start", "dur", "file_dur", "size", "mtime"]


def load_roots(path=ROOTS):
    roots = json.loads(Path(path).read_text(encoding="utf-8"))
    for r in roots:
        p = Path(r["path"])
        r["abs"] = p if p.is_absolute() else (FORGE / p)
    return roots


def windows(duration):
    """(start, length) pairs: at most 10 s each, hop 5 s, the last one reaching the end of the file."""
    out, start = [], 0.0
    while True:
        out.append((start, min(WINDOW_S, duration - start)))
        if start + WINDOW_S >= duration:
            return out
        start += HOP_S


def read_window(path, start, dur):
    with sf.SoundFile(str(path)) as f:
        f.seek(int(start * f.samplerate))
        x = f.read(int(dur * f.samplerate), dtype="float32", always_2d=True)
        return clap.resample(x.mean(axis=1), f.samplerate)


def list_files(root):
    return sorted(p for p in root.rglob("*") if p.suffix.lower() in EXTENSIONS and p.is_file())


def load_index(name):
    csv_path, npz_path = INDEX / f"{name}.csv", INDEX / f"{name}.npz"
    if not (csv_path.exists() and npz_path.exists()):
        return [], np.zeros((0, 512), dtype=np.float32)
    with csv_path.open(newline="", encoding="utf-8") as f:
        rows = list(csv.DictReader(f))
    emb = np.load(npz_path)["emb"]
    if len(rows) != len(emb):
        raise SystemExit(f"index for {name} is inconsistent ({len(rows)} rows, {len(emb)} vectors); rerun with --rebuild")
    return rows, emb


def index_root(root, rebuild=False, log=print):
    name, base = root["name"], root["abs"]
    if not base.is_dir():
        log(f"SKIP {name}: {base} is not a folder")
        return None
    old_rows, old_emb = ([], np.zeros((0, 512), dtype=np.float32)) if rebuild else load_index(name)
    by_file = {}
    for i, r in enumerate(old_rows):
        by_file.setdefault(r["file"], []).append(i)
    rows, chunks = [], []
    reused = embedded = failed = 0
    started = time.time()
    for path in list_files(base):
        rel = path.relative_to(base).as_posix()
        st = path.stat()
        size, mtime = str(st.st_size), str(int(st.st_mtime))
        prev = by_file.get(rel)
        if prev and old_rows[prev[0]]["size"] == size and old_rows[prev[0]]["mtime"] == mtime:
            rows += [old_rows[i] for i in prev]
            chunks.append(old_emb[prev])
            reused += 1
            continue
        try:
            duration = sf.info(str(path)).duration
            wins = windows(duration)
            clips = [read_window(path, s, d) for s, d in wins]
            emb = clap.embed_audio(clips)
        except Exception as exc:
            log(f"FAILED {name}/{rel}: {type(exc).__name__}: {exc}")
            failed += 1
            continue
        rows += [dict(file=rel, start=round(s, 3), dur=round(d, 3), file_dur=round(duration, 3), size=size, mtime=mtime)
                 for s, d in wins]
        chunks.append(emb)
        embedded += 1
    INDEX.mkdir(parents=True, exist_ok=True)
    emb_all = np.concatenate(chunks) if chunks else np.zeros((0, 512), dtype=np.float32)
    np.savez_compressed(INDEX / f"{name}.npz", emb=emb_all.astype(np.float32))
    with (INDEX / f"{name}.csv").open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=FIELDS)
        writer.writeheader()
        writer.writerows(rows)
    log(f"{name}: {reused + embedded} files ({embedded} embedded, {reused} unchanged, {failed} failed), "
        f"{len(rows)} windows, {time.time() - started:.1f} s on {clap.device() if embedded else 'no model needed'}")
    return len(rows)


def write_ignore(roots):
    """List every non-committed root's index in finder/index/.gitignore."""
    lines = ["# Written by finder/index.py: the index of a root marked commit:false is not committed."]
    for r in roots:
        if not r.get("commit", False):
            lines += [f"{r['name']}.npz", f"{r['name']}.csv"]
    INDEX.mkdir(parents=True, exist_ok=True)
    (INDEX / ".gitignore").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main(argv=None):
    parser = argparse.ArgumentParser(prog="audioforge.py index")
    parser.add_argument("names", nargs="*", help="root names to index (default: all)")
    parser.add_argument("--roots", default=str(ROOTS))
    parser.add_argument("--rebuild", action="store_true", help="ignore the existing index")
    args = parser.parse_args(argv)
    roots = load_roots(args.roots)
    write_ignore(roots)
    chosen = [r for r in roots if not args.names or r["name"] in args.names]
    if not chosen:
        print("no matching roots")
        return 2
    for r in chosen:
        index_root(r, args.rebuild)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
