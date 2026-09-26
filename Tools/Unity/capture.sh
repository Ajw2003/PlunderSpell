#!/usr/bin/env bash
# Captures the live Editor's game view to a PNG anywhere in the repo (capture_game_view itself only
# writes under Assets/, which would import every capture as a texture).
# Usage: bash Tools/Unity/capture.sh <out.png> [screen|camera] [camera name] [width] [height]
set -euo pipefail

out="${1:?usage: capture.sh <out.png> [screen|camera] [camera name] [width] [height]}"
source="${2:-screen}"
camera="${3:-}"
width="${4:-1280}"
height="${5:-720}"

args=(--source "$source" --width "$width" --height "$height" --max_resolution 4096)
[ -n "$camera" ] && args+=(--camera "$camera")

mkdir -p "$(dirname "$out")"
unity command capture_game_view "${args[@]}" --caller plugin --skill unity-cli --no-banner --format json \
    | python -c "
import base64, json, sys
envelope = json.load(sys.stdin)
data = envelope.get('data') or {}
r = data.get('result')
r = json.loads(r) if isinstance(r, str) else (r or {})
b64 = r.get('base64') or r.get('image') or r.get('data')
if not envelope.get('success') or not b64:
    sys.exit('capture failed: ' + json.dumps(envelope)[:400])
open(sys.argv[1], 'wb').write(base64.b64decode(b64))
print('saved', sys.argv[1], r.get('width'), 'x', r.get('height'))
" "$out"
