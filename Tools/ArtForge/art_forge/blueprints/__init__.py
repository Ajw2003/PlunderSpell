"""Blueprint registry: one module per (kind, age), e.g. `items_bronze.py`.

Each module exposes `BLUEPRINTS = {slug: builder}` where a builder takes the parsed
`spec.Entry` and returns a `Blueprint`. A missing module simply means nobody has
written that (kind, age) yet; `available()` reports which slugs have builders.
"""

from __future__ import annotations

import importlib
import pkgutil

from .. import AGES, KINDS, spec
from ..blueprint import Blueprint


def module_name(kind: str, age: str) -> str:
    return f"{kind}_{age}"


def _modules() -> set[str]:
    return {m.name for m in pkgutil.iter_modules(__path__)}


def builders(kind: str, age: str) -> dict:
    """{slug: builder} for one (kind, age); empty if no module exists yet."""
    name = module_name(kind, age)
    if name not in _modules():
        return {}
    module = importlib.import_module(f"{__name__}.{name}")
    table = getattr(module, "BLUEPRINTS", None)
    if not isinstance(table, dict):
        raise TypeError(f"{module.__name__} must define BLUEPRINTS = {{slug: builder}}")
    known = set(spec.slugs(age, kind))
    stray = set(table) - known
    if stray:
        raise KeyError(f"{module.__name__} has builders for slugs the art bible does not "
                       f"list under {age}/{kind}: {sorted(stray)}")
    return table


def available(kind: str, ages=AGES) -> list[tuple[str, str]]:
    """Every (age, slug) with a builder, in art-bible order."""
    out = []
    for age in ages:
        table = builders(kind, age)
        out += [(age, s) for s in spec.slugs(age, kind) if s in table]
    return out


def make(kind: str, age: str, slug: str) -> Blueprint:
    """Build the Blueprint for one entry and check it names itself correctly."""
    if kind not in KINDS:
        raise ValueError(f"unknown kind {kind!r}")
    table = builders(kind, age)
    if slug not in table:
        raise KeyError(f"no blueprint for {age}/{kind}/{slug}")
    entry = spec.entry(age, kind, slug)
    bp = table[slug](entry)
    if not isinstance(bp, Blueprint):
        raise TypeError(f"builder for {slug} returned {type(bp).__name__}, not Blueprint")
    if (bp.slug, bp.age, bp.kind) != (slug, age, kind):
        raise ValueError(f"builder for {age}/{kind}/{slug} returned a blueprint for "
                         f"{bp.age}/{bp.kind}/{bp.slug}")
    return bp


def blueprint(entry, parts, **kwargs) -> Blueprint:
    """Convenience for builders: fill slug/name/age/kind from the entry."""
    return Blueprint(slug=entry.slug, name=entry.pascal, age=entry.age, kind=entry.kind,
                     parts=parts, entry=entry, **kwargs)
