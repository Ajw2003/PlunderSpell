import importlib, sys
OUT = "/home/user/PlunderSpell/docs/art/concept/bronze/"
MODS = {"levy": "palace-levy", "slinger": "wall-slinger", "dendra": "dendra-champion", "keeper": "flame-keeper",
        "ingot": "oxhide-ingot", "amphora": "sealed-amphora", "hippo": "faience-hippo", "mask": "gold-death-mask",
        "tripod": "bronze-tripod", "gate": "lion-gate", "megaron": "megaron-hall", "pithos": "pithos-store"}
for m in sys.argv[1:] or MODS:
    mod = importlib.import_module(m)
    sh = mod.build()
    kw = getattr(mod, "GROUND", (40, 1160))
    svg = sh.render(ground=kw)
    open(OUT + MODS[m] + ".svg", "w").write(svg)
    print(MODS[m], svg.count("<"), "elements-ish")
