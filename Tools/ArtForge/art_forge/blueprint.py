"""The Blueprint: everything a builder function says about one model.

A builder is `def my_item(entry: spec.Entry) -> Blueprint`, registered in the
`BLUEPRINTS` dict of `art_forge/blueprints/<kind>_<age>.py`. It reads sizes from
`entry.dims` / `entry.height_m` and colours from `entry.families`; the Blueprint it
returns is the only thing the assembler, validator and exporter look at.
"""

from __future__ import annotations

from dataclasses import dataclass, field

from .kit import Part, families_used
from .spec import Entry


@dataclass
class Blueprint:
    slug: str
    name: str                     # PascalName: folder, file and object name
    age: str
    kind: str                     # items | structures | enemies
    parts: list[Part]
    entry: Entry
    # Rig (enemies only). EnemyForge bone dicts: name, head, tail, parent, mirror.
    bones: list[dict] | None = None
    # Per-family overrides merged over the JSON-derived family spec, e.g.
    # {"gilt": {"rough": 0.22, "wear_to": "#7E2A26"}}. See materials.py for keys.
    family_overrides: dict[str, dict] = field(default_factory=dict)
    # Families the JSON does not list but the build bullets require (e.g. iron
    # hinges on a painted panel). Full specs: {"iron": {"name": "Strap iron",
    # "base": "#2E2A26", "rough": 0.6, "metal": 1.0}}. Reported in the manifest.
    extra_families: dict[str, dict] = field(default_factory=dict)
    bevel: float = 0.004
    tri_budget: int | None = None   # None -> the JSON budget
    grounded: bool = True
    wear: float = 1.0
    # Per-axis bounding-box expectations that override the JSON dimensions, with a
    # reason the report prints: {"X": (0.50, "handles arch 0.08 m out")}.
    # Axes: X = W, Y = D, Z = H. Use only when the JSON's own words disagree with
    # its dimension line; never to paper over a model that is simply the wrong size.
    bbox_overrides: dict[str, tuple[float, str]] = field(default_factory=dict)
    # A structure/enemy that legitimately carries orpiment gold says why here.
    gold_reason: str | None = None
    notes: list[str] = field(default_factory=list)

    @property
    def budget(self) -> int:
        return self.tri_budget if self.tri_budget is not None else self.entry.tri_budget

    @property
    def rigged(self) -> bool:
        return self.bones is not None

    def resolved_families(self) -> dict[str, dict]:
        """Every family the asset may use: JSON families + extras, overrides applied."""
        merged: dict[str, dict] = {}
        for key, fam in self.entry.families.items():
            merged[key] = dict(fam)
        for key, fam in self.extra_families.items():
            if key in merged:
                raise ValueError(f"{self.slug}: extra family {key!r} shadows a JSON family")
            if "base" not in fam:
                raise ValueError(f"{self.slug}: extra family {key!r} needs a 'base' hex")
            merged[key] = {"name": key, "rough": 0.7, "metal": 0.0, "emit": None,
                           "grain": 0.25, "notes": "blueprint extra", **fam}
        for key, override in self.family_overrides.items():
            if key not in merged:
                raise ValueError(f"{self.slug}: override for unknown family {key!r}; "
                                 f"families are {sorted(merged)}")
            merged[key].update(override)
        return merged

    def ordered_families(self) -> list[tuple[str, dict]]:
        """The families the parts actually use, in art-bible order: slot i = entry i."""
        resolved = self.resolved_families()
        used = families_used(self.parts)
        unknown = used - set(resolved)
        if unknown:
            raise ValueError(f"{self.slug}: parts use families not in the spec or "
                             f"extra_families: {sorted(unknown)}; known: {sorted(resolved)}")
        return [(key, resolved[key]) for key in resolved if key in used]

    def unused_families(self) -> list[str]:
        used = families_used(self.parts)
        return [key for key in self.entry.families if key not in used]
