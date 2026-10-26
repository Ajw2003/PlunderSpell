"""Shared frame for the fear-pass proposal plates (#386): the true form options and the creature options.

Reuses the High Medieval sheet helpers so the plates sit beside the Lantern Warden's sheet in the
same frame, grid, fonts and pigments. Plates land in docs/art/concept/fear/.
"""
import importlib.util
import os

# The High Medieval helpers are also a module called "lib", so load them under another name.
_spec = importlib.util.spec_from_file_location(
    "high_lib", os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "high", "lib.py"))
_high = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_high)
globals().update({k: v for k, v in vars(_high).items() if not k.startswith("_")})

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "..", "docs", "art", "concept", "fear") + os.sep

BLACK = "#14120E"; VELLUM = "#DCD2BA"; MADDER = "#C4542E"; LAPIS = "#7A6AA0"


def ladder_and_human(px_per_m, top_m):
    return metre_ladder(px_per_m=px_per_m, top_m=top_m) + human(px_per_m=px_per_m)


def notes(x, y, rows, width=330):
    """A short block of labelled lines: rows = [(label, line), ...]. Used for the tell and the hint."""
    out = []
    for label, line in rows:
        out.append(text(x, y, label.upper(), 10, "#635C4C", extra=' letter-spacing="2"'))
        y += 16
        for chunk in wrap(line, width // 7):
            out.append(text(x, y, chunk, 12, "#DCD2BA"))
            y += 16
        y += 10
    return ''.join(out)


def wrap(line, n):
    words, rows, cur = line.split(), [], ""
    for w in words:
        if len(cur) + len(w) + 1 > n and cur:
            rows.append(cur); cur = w
        else:
            cur = (cur + " " + w).strip()
    rows.append(cur)
    return rows


def write_plate(sheet, head, parts, tail_palette):
    os.makedirs(OUT, exist_ok=True)
    svg = head + '\n'.join(parts) + '\n' + tail_palette + '\n</svg>\n'
    path = OUT + sheet.slug + '.svg'
    with open(path, 'w', encoding='utf-8') as f:
        f.write(svg)
    print(f"wrote {os.path.relpath(path)}")
