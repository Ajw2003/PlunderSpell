#!/usr/bin/env bash
# One castle room, end to end: its sheet (SVG, then PNG), its model (build + validate),
# its preview render, then the check that the model matches the sheet.
#
#   Tools/ArtBible/rooms/room_cycle.sh BronzeTreasury [--no-preview]
#
# Run from anywhere. Needs blender, node + Playwright (see docs/art/rooms/README.md).
set -euo pipefail
cd "$(dirname "$0")/../../.."
export PYTHONHASHSEED=0
KEY="$1"
echo "== sheet: $KEY"
python3 Tools/ArtBible/rooms/make_rooms.py "$KEY"
NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs --rooms "$KEY" | tail -1
echo "== model: $KEY"
blender -b -P Tools/AssetPipeline/build_assets.py -- --only "$KEY" 2>&1 | grep -E "^\[|^ +- |Traceback|Error" || true
if [ "${2:-}" != "--no-preview" ]; then
  blender -b -P Tools/AssetPipeline/render_previews_only.py -- "$KEY" 2>&1 | grep -E "^rendered|^unchanged|Error" || true
fi
echo "== sheet vs model: $KEY"
python3 Tools/ArtBible/build_room_sheets.py --only "$KEY" --check --models
