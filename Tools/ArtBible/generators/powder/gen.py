import importlib, sys, re
OUT = "/home/user/PlunderSpell/docs/art/concept/powder/"
MODS = {"partisan-guard": "guard", "musketeer": "musketeer", "cuirassier": "cuirassier", "petardier": "petardier",
        "curiosity-cabinet": "cabinet", "venetian-mirror": "mirror", "brass-astrolabe": "astrolabe",
        "silver-tureen": "tureen", "nautilus-cup": "nautilus", "mirror-gallery": "gallery",
        "powder-magazine": "magazine", "kunstkammer": "kunstkammer"}
want = sys.argv[1:] or list(MODS)
for slug in want:
    m = importlib.import_module(MODS[slug])
    svg = m.build()
    ids = re.findall(r'id="([^"]+)"', svg)
    assert len(ids) == len(set(ids)), "dup ids"
    n = len(re.findall(r"<(path|circle|ellipse|rect|line|text|polygon)\b", svg))
    open(OUT + slug + ".svg", "w").write(svg)
    print(slug, "elements", n, "bytes", len(svg))
