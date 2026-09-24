"""ArtForge: turns the art bible (docs/art/) into game-ready models.

Built on top of Tools/EnemyForge rather than beside it: the primitives, bake,
bevel/unwrap and geometry validator are EnemyForge's own code, imported, not copied.
Importing this package puts Tools/EnemyForge on sys.path so `enemy_forge` resolves.
"""

from __future__ import annotations

import os
import sys

TOOLS_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
REPO_ROOT = os.path.dirname(TOOLS_DIR)
ENEMY_FORGE_DIR = os.path.join(TOOLS_DIR, "EnemyForge")

if ENEMY_FORGE_DIR not in sys.path:
    sys.path.insert(0, ENEMY_FORGE_DIR)

KINDS = ("items", "structures", "enemies")
AGES = ("bronze", "high", "late", "powder")
