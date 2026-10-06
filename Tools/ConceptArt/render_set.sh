#!/usr/bin/env bash
# Renders the painted-look concept set from existing repo screenshots and model sheets.
# Usage: bash Tools/ConceptArt/render_set.sh <out-dir>
set -euo pipefail
cd "$(dirname "$0")/../.."
O="${1:?out dir}"; mkdir -p "$O"
G=docs/generated; M=docs/art/models
P() { python3 Tools/ConceptArt/paintover.py "$@"; }
P $G/look-samples-2026-09-24/calm.png $O/01-courtyard-calm.png --warm 0.6 --scale 1.5 &
P $G/look-samples-2026-09-24/alert.png $O/02-courtyard-alarm.png --warm 0.2 --scale 1.5 &
P $G/era-integration-2026-09-24/latemedieval-enemy-a.png $O/03-late-medieval-guard.png --warm 0.4 &
P $G/era-integration-2026-09-24/bronze-enemy-a.png $O/04-bronze-age-champion.png --warm 0.7 &
wait
P $G/era-integration-2026-09-24/highmedieval-loot-a.png $O/05-altarpiece-plunder.png --warm 0.5 &
P $G/perf-2026-10-03/hud-host.png $O/06-portal-room.png --crop 280,90,1020,540 --warm 0.3 --scale 2.5 &
P $G/night-sky-2026-09-26/4-great-hall-looking-up-after.png $O/07-great-hall-portal.png --warm 0.4 --scale 1.5 &
P $G/night-look-preview-2026-09-25/bailey-view.png $O/08-bailey-at-night.png --warm 0.5 --scale 1.5 &
wait
P $M/high/household-knight.png $O/09-household-knight.png --crop 2834,740,3532,1410 --scale 2.5 --warm 0.4 &
P $M/high/gilded-altarpiece.png $O/10-gilded-altarpiece.png --crop 202,443,1063,1253 --scale 2 --warm 0.5 &
P $G/era-integration-2026-09-24/bronze-room.png $O/11-bronze-age-room.png --warm 0.7 &
wait
