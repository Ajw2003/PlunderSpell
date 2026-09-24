"""Read the art bible's per-Age JSON (docs/art/data/<age>.json) into build inputs.

The JSON is the single source of truth for what a model is: its size, its triangle
budget and its material list. This module turns one entry into an `Entry` holding
parsed numbers and a table of surface families, so a blueprint never re-parses
strings and never invents a colour the art bible does not state.
"""

from __future__ import annotations

import json
import os
import re
from collections import OrderedDict
from dataclasses import dataclass, field
from functools import lru_cache

from . import AGES, KINDS, REPO_ROOT

DATA_DIR = os.path.join(REPO_ROOT, "docs", "art", "data")
ART_DIR = os.path.join(REPO_ROOT, "docs", "art")


# --------------------------------------------------------------------------------
# Material families
# --------------------------------------------------------------------------------

# PBR defaults derived from words in a material's name. Checked in order; the first
# row whose keyword appears wins. A value stated in the JSON notes ("roughness 0.85",
# "metallic 1.0") beats these, and a blueprint override beats both.
_METALS = ("gold", "gilt", "gilded", "orpiment", "silver", "bronze", "brass",
           "copper", "iron", "steel", "pewter", "tin", "penny", "coin", "plate",
           "harness", "mail")
# Words that mean "made from a metal but no longer metal": corrosion, soot, paint.
_NOT_METAL = ("oxide", "rust", "tarnish", "patina", "soot", "grime", "shadow",
              "residue", "dust", "stain")

_ROUGHNESS_BY_KEYWORD = [
    (("glass", "crystal", "mirror", "glaze", "faience", "enamel", "nacre",
      "pearl", "garnet", "ruby", "sapphire", "gem"), 0.25),
    (("gold", "gilt", "gilded", "orpiment", "silver", "brass", "polish"), 0.30),
    (("bronze", "copper", "pewter", "coin", "penny"), 0.40),
    (("steel", "iron", "plate", "harness", "mail"), 0.50),
    (("wax", "ivory", "bone", "horn"), 0.55),
    (("leather", "hide", "rawhide", "oxhide", "calf"), 0.70),
    (("wood", "oak", "ash", "walnut", "ebony", "poplar", "pine", "cypress",
      "limewood", "olive", "timber", "panel", "board", "stock", "haft"), 0.70),
    (("cloth", "wool", "linen", "felt", "silk", "velvet", "cord", "rope",
      "hemp", "flax", "tapestry", "canvas", "baize", "bag", "shroud"), 0.90),
    (("stone", "plaster", "brick", "limestone", "sandstone", "ashlar", "rubble",
      "gesso", "clay", "terracotta", "mortar", "limewash", "tile", "grit"), 0.90),
]
DEFAULT_ROUGHNESS = 0.75

# Emissive families are rare and always state-bearing (a flame, a lit match); the
# name has to say so. Anything else that should glow is a blueprint override.
_EMISSIVE_WORDS = ("ember", "flame", "torch fire", "fuse")

# The mood board's reserved pigments, and how close (0-255 RGB distance) a family's
# hex must be to count as that pigment for the discipline rules.
ORPIMENT = "#C9A227"
VERDIGRIS = "#5FA288"
LAPIS = "#7A6AA0"
PIGMENT_DISTANCE = 24.0


def family_slug(name: str) -> str:
    """'Gilt (orpiment)' -> 'gilt'; 'Buff terracotta' -> 'buff_terracotta'."""
    stripped = re.sub(r"\(.*?\)", " ", name).lower()
    words = re.findall(r"[a-z0-9]+", stripped)
    return "_".join(words) or "unnamed"


def pascal_name(name: str) -> str:
    """'Banker's Ledger' -> 'BankersLedger'. Used for folders, files and objects."""
    words = re.findall(r"[A-Za-z0-9]+", name.replace("'", "").replace("’", ""))
    return "".join(w[:1].upper() + w[1:] for w in words)


def _hex_rgb(hex_colour: str) -> tuple[int, int, int]:
    h = hex_colour.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def hex_distance(a: str, b: str) -> float:
    return sum((x - y) ** 2 for x, y in zip(_hex_rgb(a), _hex_rgb(b))) ** 0.5


def _has_word(text: str, words) -> bool:
    return any(re.search(rf"\b{re.escape(w)}", text) for w in words)


def default_family(name: str, hex_colour: str, notes: str = "") -> dict:
    """A family spec (the dict materials.build_authoring_material reads) for one
    JSON material, with PBR values derived from its name and notes."""
    lowered = name.lower()
    metal = 0.0
    if _has_word(lowered, _METALS) and not _has_word(lowered, _NOT_METAL):
        metal = 1.0

    rough = DEFAULT_ROUGHNESS
    for words, value in _ROUGHNESS_BY_KEYWORD:
        if _has_word(lowered, words):
            rough = value
            break

    # Numbers the art bible states outright win over keyword guesses.
    note = notes.lower()
    stated_rough = re.search(r"roughness\s*(?:of\s*)?([01](?:\.\d+)?)", note)
    if stated_rough:
        rough = float(stated_rough.group(1))
    stated_metal = re.search(r"metallic\s*(?:of\s*)?([01](?:\.\d+)?)", note)
    if stated_metal:
        metal = float(stated_metal.group(1))

    emit = hex_colour if _has_word(lowered, _EMISSIVE_WORDS) else None
    return {
        "name": name,
        "base": hex_colour.upper(),
        "rough": rough,
        "metal": metal,
        "emit": emit,
        # Weathering strength: how much the blotch noise darkens the base colour.
        "grain": 0.18 if metal else 0.26,
        "notes": notes,
    }


def is_orpiment_gold(family: dict) -> bool:
    lowered = family["name"].lower()
    return (_has_word(lowered, ("gold", "gilt", "gilded", "orpiment"))
            or hex_distance(family["base"], ORPIMENT) < PIGMENT_DISTANCE)


def is_reserved_arcane(family: dict) -> bool:
    """Verdigris or lapis — the arcane pigments no enemy may wear."""
    lowered = family["name"].lower()
    return (_has_word(lowered, ("verdigris", "lapis"))
            or hex_distance(family["base"], VERDIGRIS) < PIGMENT_DISTANCE
            or hex_distance(family["base"], LAPIS) < PIGMENT_DISTANCE)


# --------------------------------------------------------------------------------
# Numbers from strings
# --------------------------------------------------------------------------------

def parse_budget(budget: str) -> int:
    """'≤ 1.5k tris (two-handed but light), 1024² set.' -> 1500."""
    match = re.search(r"≤\s*([\d.]+)\s*(k?)\s*tris", budget)
    if not match:
        raise ValueError(f"cannot find a triangle budget in {budget!r}")
    value = float(match.group(1)) * (1000.0 if match.group(2) else 1.0)
    return int(round(value))


def parse_dimensions(dimensions: str) -> tuple[float, float, float]:
    """'0.34 × 0.34 × 0.62 m (W × D × H)' -> (0.34, 0.34, 0.62).

    W runs along X, D along Y (the model faces -Y), H along Z.
    """
    match = re.search(r"([\d.]+)\s*[×x]\s*([\d.]+)\s*[×x]\s*([\d.]+)\s*m", dimensions)
    if not match:
        raise ValueError(f"cannot find W × D × H in {dimensions!r}")
    return tuple(float(g) for g in match.groups())


# --------------------------------------------------------------------------------
# Entries
# --------------------------------------------------------------------------------

@dataclass
class Entry:
    """One art-bible entry, parsed. `raw` keeps the whole JSON object."""

    age: str
    kind: str
    slug: str
    name: str
    raw: dict
    families: "OrderedDict[str, dict]"
    tri_budget: int
    dims: tuple[float, float, float] | None = None   # items: (W=X, D=Y, H=Z) metres
    height_m: float | None = None                    # structures and enemies
    build: list[str] = field(default_factory=list)

    @property
    def pascal(self) -> str:
        return pascal_name(self.name)

    @property
    def concept_png(self) -> str:
        return os.path.join(ART_DIR, "concept", self.age, f"{self.slug}.png")

    def family(self, slug: str) -> dict:
        if slug not in self.families:
            raise KeyError(f"{self.slug}: no material family {slug!r}; "
                           f"the JSON lists {list(self.families)}")
        return self.families[slug]


@lru_cache(maxsize=None)
def load_age(age: str) -> dict:
    if age not in AGES:
        raise ValueError(f"unknown age {age!r}; expected one of {AGES}")
    with open(os.path.join(DATA_DIR, f"{age}.json"), encoding="utf-8") as handle:
        return json.load(handle)


def entry(age: str, kind: str, slug: str) -> Entry:
    """Look up one entry by (age, kind, slug) and parse it."""
    if kind not in KINDS:
        raise ValueError(f"unknown kind {kind!r}; expected one of {KINDS}")
    data = load_age(age)
    matches = [e for e in data[kind] if e["slug"] == slug]
    if not matches:
        raise KeyError(f"{age}/{kind} has no entry {slug!r}; it has "
                       f"{[e['slug'] for e in data[kind]]}")
    raw = matches[0]

    families: OrderedDict[str, dict] = OrderedDict()
    for material in raw["materials"]:
        key = family_slug(material["name"])
        if key in families:
            raise ValueError(f"{slug}: two materials slug to {key!r}")
        families[key] = default_family(material["name"], material["hex"],
                                       material.get("notes", ""))

    return Entry(
        age=age, kind=kind, slug=slug, name=raw["name"], raw=raw,
        families=families,
        tri_budget=parse_budget(raw["budget"]),
        dims=parse_dimensions(raw["dimensions"]) if kind == "items" else None,
        height_m=raw.get("height_m"),
        build=list(raw.get("build", [])),
    )


def slugs(age: str, kind: str) -> list[str]:
    return [e["slug"] for e in load_age(age)[kind]]
