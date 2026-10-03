#!/usr/bin/env bash
# Checks settings_restore.sh on a temp copy of ProjectSettings.asset; the real file is never written.
set -u
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
real="$here/../../ProjectSettings/ProjectSettings.asset"
source "$here/settings_restore.sh"
tmp="$(mktemp -d)"; trap 'rm -rf "$tmp"' EXIT
mkdir -p "$tmp/ProjectSettings"; cp "$real" "$tmp/ProjectSettings/ProjectSettings.asset"
cp "$real" "$tmp/original"
SETTINGS_ROOT="$tmp"; SETTINGS_FILES=(ProjectSettings/ProjectSettings.asset)
settings_save || { echo "FAIL save"; exit 1; }
printf '  - {fileID: -944628639613478452, guid: e05f63c218fb0e54f8c41ecaf9e2ef10, type: 3}\r\n' >> "$tmp/ProjectSettings/ProjectSettings.asset"
cmp -s "$tmp/original" "$tmp/ProjectSettings/ProjectSettings.asset" && { echo "FAIL the simulated build changed nothing"; exit 1; }
settings_restore || { echo "FAIL restore"; exit 1; }
if cmp "$tmp/original" "$tmp/ProjectSettings/ProjectSettings.asset"; then echo "PASS restored byte-identical"; else echo "FAIL differs after restore"; exit 1; fi
settings_save >/dev/null; settings_restore   # unchanged path
