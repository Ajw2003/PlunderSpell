"""AnimForge: keyframed enemy animation for the art-bible humans, by code in Blender.

See Tools/ArtForge/README.md, "AnimForge", and docs/plans/artbible-enemy-animations.md.
Importing this package puts Tools/ArtForge on sys.path and imports art_forge (which
puts Tools/EnemyForge on sys.path and loads bpy).
"""

import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
_ARTFORGE = os.path.dirname(_HERE)
if _ARTFORGE not in sys.path:
    sys.path.insert(0, _ARTFORGE)

import bpy  # noqa: E402,F401  (must precede mathutils)
import art_forge  # noqa: E402,F401

REPO_ROOT = art_forge.REPO_ROOT
FPS = 30
SPEC_PATH = os.path.join(_ARTFORGE, "anim_spec.json")
OUT_DIR = os.path.join(REPO_ROOT, "Assets", "Models", "ArtBible", "Animations")
REVIEW_DIR = os.path.join(REPO_ROOT, "docs", "art", "anim")
WARDEN_BLEND = os.path.join(REPO_ROOT, "Assets", "Models", "ArtBible", "Enemies", "High",
                            "LanternWarden", "LanternWarden.blend")
WARDEN_CONCEPT = os.path.join(REPO_ROOT, "docs", "art", "concept", "high", "lantern-warden.png")
