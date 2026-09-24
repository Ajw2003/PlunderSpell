"""
Render previews for just the assets whose key starts with a prefix:

    blender -b -P Tools/AssetPipeline/render_previews_only.py -- Bronze [--force]

A token that is a whole key matches only that key; any other token is a key
prefix, as for build_assets.py --only.

A wrapper rather than a flag on render_previews.py because that file hashes
its own source to decide when every preview is stale (_rig_version): any
edit to it, even a new CLI flag, re-renders all of them for no visual change.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import asset_specs  # noqa: E402
import render_previews  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
prefixes = [a for a in argv if not a.startswith("--")]
if not prefixes:
    print("usage: blender -b -P render_previews_only.py -- <KeyPrefix> [--force]")
    sys.exit(2)
asset_specs.ALL_SPECS = [s for s in asset_specs.ALL_SPECS if asset_specs.key_matches(s["key"], ",".join(prefixes))]
if not asset_specs.ALL_SPECS:
    print(f"ERROR: no asset key starts with {prefixes}")
    sys.exit(1)
render_previews.main()
