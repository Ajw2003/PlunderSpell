"""
Write castle room sheet SVGs from their generators.

    python3 Tools/ArtBible/rooms/make_rooms.py                 # every generator
    python3 Tools/ArtBible/rooms/make_rooms.py BronzeAge       # one Age
    python3 Tools/ArtBible/rooms/make_rooms.py BronzeMegaron   # one sheet (or any key prefix)

Each generator is rooms/generators/<Age>/<Key>.py with a build() returning a
roomlib Sheet; its SVG goes to docs/art/rooms/concept/<Age>/<Key>.svg. Then
render the PNGs with:  NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs --rooms [<Age>|<prefix>]
"""
import importlib.util
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
sys.path.insert(0, HERE)


def main():
    wanted = sys.argv[1:]
    gen_root = os.path.join(HERE, "generators")
    done = 0
    for age in sorted(os.listdir(gen_root)):
        for name in sorted(os.listdir(os.path.join(gen_root, age))):
            if not name.endswith(".py"):
                continue
            key = name[:-3]
            if wanted and not any(age == w or key.startswith(w) for w in wanted):
                continue
            spec = importlib.util.spec_from_file_location(f"room_{key}", os.path.join(gen_root, age, name))
            mod = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(mod)
            sh = mod.build()
            out_dir = os.path.join(REPO, "docs", "art", "rooms", "concept", age)
            os.makedirs(out_dir, exist_ok=True)
            svg = sh.render(ground=getattr(mod, "GROUND", (40, 1160)))
            with open(os.path.join(out_dir, f"{key}.svg"), "w", encoding="utf8") as fh:
                fh.write(svg)
            print(f"wrote docs/art/rooms/concept/{age}/{key}.svg ({svg.count('<')} elements)")
            done += 1
    if not done:
        print(f"ERROR: no generator matches {wanted}")
        sys.exit(1)


if __name__ == "__main__":
    main()
