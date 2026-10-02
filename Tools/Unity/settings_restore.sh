#!/usr/bin/env bash
# Sourced by the co-op check scripts. A Pipeline build rewrites ProjectSettings.asset (an extra
# preloadedAssets entry, other line endings), so save a byte-exact copy first and put it back with cp
# on every exit path. Never git checkout/restore: the owner's hook holds those for approval (#199).
# settings_save: copy the files to a temp dir; returns 1 (after saying why) if a copy cannot be made.
# settings_restore: put the copies back, one line saying what it did per file.
# SETTINGS_ROOT (default ".") is the directory the paths below are relative to; the test overrides it.
SETTINGS_FILES=(ProjectSettings/ProjectSettings.asset ProjectSettings/Packages/com.unity.pipeline/RuntimePipelineConfig.json)
SETTINGS_SAVED=""
SETTINGS_DIR=""

settings_save() {
    local root="${SETTINGS_ROOT:-.}" f i=0
    SETTINGS_DIR="$(mktemp -d)" || { echo "settings: FAIL could not make a temp dir"; return 1; }
    for f in "${SETTINGS_FILES[@]}"; do
        i=$((i + 1))
        if [ ! -f "$root/$f" ]; then echo "settings: $f does not exist, nothing to save"; continue; fi
        cp -p "$root/$f" "$SETTINGS_DIR/$i" && cmp -s "$root/$f" "$SETTINGS_DIR/$i" \
            || { echo "settings: FAIL could not copy $f"; return 1; }
    done
    SETTINGS_SAVED=yes
    echo "settings: saved a byte-exact copy of the settings files"
}

settings_restore() {
    [ -n "$SETTINGS_SAVED" ] || { echo "settings: no saved copies, nothing restored"; return 0; }
    local root="${SETTINGS_ROOT:-.}" f i=0
    for f in "${SETTINGS_FILES[@]}"; do
        i=$((i + 1))
        [ -f "$SETTINGS_DIR/$i" ] || continue
        if cmp -s "$SETTINGS_DIR/$i" "$root/$f"; then echo "settings: $f unchanged"
        elif cp -p "$SETTINGS_DIR/$i" "$root/$f"; then echo "settings: $f restored from the saved copy"
        else echo "settings: FAIL could not restore $f (copy kept in $SETTINGS_DIR)"; return 1; fi
    done
    rm -rf "$SETTINGS_DIR"; SETTINGS_SAVED=""
}
