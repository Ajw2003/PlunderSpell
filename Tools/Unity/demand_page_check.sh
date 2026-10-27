#!/usr/bin/env bash
# Solo Play check of the demand page (#419): in the Lair, the left page of the ledger carries the demanded piece drawn in
# ink across the whole page, and the right page names it. Takes pictures from where a player reads the book, from over the
# table, and from across the room, and saves the drawing itself, into docs/generated/demand-page-2026-10-27/.
# Runs in the test slot, leaves the Editor stopped.
# Usage: bash Tools/Unity/demand_page_check.sh   Needs the Editor open on this project, not in Play mode.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/demand-page-2026-10-27"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/run.log"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
cleanup() { log "cleaning up"; unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true; test_slot_restore; log "$test_slot_msg"; }
trap cleanup EXIT
fails=0
check() { if [ "$1" = ok ]; then log "PASS $2"; else log "FAIL $2"; fails=$((fails + 1)); fi; }

playing="$(ev 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; trap - EXIT; exit 1; }
[ "$playing" = "False False" ] || { log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; }
test_slot_use || { log "FAIL cannot switch to the test slot: $test_slot_msg"; trap - EXIT; exit 1; }; log "$test_slot_msg"

unity command editor_play "${cli[@]}" >/dev/null
state=""
for _ in $(seq 1 60); do state="$(ev 'return Plunderspell.Core.GameServices.GameState.CurrentState.ToString();' 2>/dev/null)"; [ "$state" = MainMenu ] && break; sleep 1; done
log "state $state"
log "$(ev 'var s = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Net.CoopSession>(); s.PlaySolo(); Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.LairRoom); return "solo, slot " + Plunderspell.Lair.SaveSlots.Active;')"
p=none
for _ in $(seq 1 30); do p="$(ev 'return StateMachine.PlayerStateMachine.Local != null ? "player" : "none";' 2>/dev/null)"; [ "$p" = player ] && break; sleep 1; done
log "player: $p"
sleep 3

sketch="$(ev 'var s = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.LedgerDemandSketch>(); if (s == null || s.Drawing == null) return "no drawing"; var w = s.Drawing.width; var h = s.Drawing.height; var rt = UnityEngine.RenderTexture.GetTemporary(w, h, 0); UnityEngine.Graphics.Blit(s.Drawing, rt); var was = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt; var copy = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGBA32, false); copy.ReadPixels(new UnityEngine.Rect(0, 0, w, h), 0, 0); UnityEngine.RenderTexture.active = was; UnityEngine.RenderTexture.ReleaseTemporary(rt); int inked = 0; foreach (var c in copy.GetPixels32()) if (c.r < 160) inked++; System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath("'"$out"'/drawing.png"), UnityEngine.ImageConversion.EncodeToPNG(copy)); return "drawing " + s.Drawing.width + "x" + s.Drawing.height + " inked " + inked;')"
log "$sketch"
inked="$(printf '%s' "$sketch" | sed -n 's/.*inked \([0-9]*\).*/\1/p')"
[ "${inked:-0}" -gt 5000 ] && check ok "the left page carries an inked drawing" || check no "the left page carries an inked drawing"

right="$(ev 'return UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.LairLedgerBook>().RightText.Replace("\n", "/");')"
log "right page: $right"
case "$right" in *"The Ophan demands"*"Arm Reliquary"*"deep in the vault"*) check ok "the right page names the demanded piece and where it lies" ;; *) check no "the right page names the demanded piece and where it lies" ;; esac

# Camera spots, from the book's own position: as a reader stands at the table (+Z side), straight over it, and across the room.
book="$(ev 'var b = UnityEngine.GameObject.Find("/LairRoom/LairLedger").transform.position; return b.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + b.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + b.z.ToString(System.Globalization.CultureInfo.InvariantCulture);')"
read -r bx by bz <<< "$book"
log "book at $bx $by $bz"
at() { python -c "import sys; print(' '.join(str(round(float(a) + float(b), 3)) for a, b in zip(sys.argv[1:4], sys.argv[4:7])))" "$bx" "$by" "$bz" "$@"; }
shot() { # shot <name> <dx dy dz from the book> <yaw> <pitch>
    read -r x y z <<< "$(at "$2" "$3" "$4")"
    log "$1: $(bash Tools/Unity/view.sh "$x" "$y" "$z" "$5" "$6" "$out/$1.png" 2>&1 | tr '\n' ' ')"
}
shot reading 0 0.55 0.5 180 48
shot over-the-book 0 0.5 0.08 180 82
shot across-the-room 2.6 0.9 1.6 238 20
for f in drawing reading over-the-book across-the-room; do [ -s "$out/$f.png" ] && check ok "$f.png saved" || check no "$f.png saved"; done

log "fails $fails"
exit $((fails > 0))
