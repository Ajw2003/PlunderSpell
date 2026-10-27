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
cleanup() { log "cleaning up"; ev 'UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState()); return "keys up";' >/dev/null 2>&1; unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true; test_slot_restore; log "$test_slot_msg"; }
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
[ "$right" = "The Ophan demands" ] && check ok "the right page says only \"The Ophan demands\"" || check no "the right page says only \"The Ophan demands\""

# Camera spots, from the book's own position: as a reader stands at the table (+Z side), straight over it, and across the room.
book="$(ev 'var b = UnityEngine.GameObject.Find("/LairRoom/LairLedger").transform.position; return b.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + b.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + b.z.ToString(System.Globalization.CultureInfo.InvariantCulture);')"
read -r bx by bz <<< "$book"
log "book at $bx $by $bz"
at() { python -c "import sys; print(' '.join(str(round(float(a) + float(b), 3)) for a, b in zip(sys.argv[1:4], sys.argv[4:7])))" "$bx" "$by" "$bz" "$@"; }
shot() { # shot <name> <dx dy dz from the book> <yaw> <pitch>
    read -r x y z <<< "$(at "$2" "$3" "$4")"
    local said; said="$(bash Tools/Unity/view.sh "$x" "$y" "$z" "$5" "$6" "$out/$1.png" 2>&1 | tr '\n' ' ')"
    log "$1: $said"
    case "$said" in "camera at"*) ;; *) check no "$1 was taken from its spot (view.sh did not place the camera)" ;; esac
}
shot reading 0 0.55 0.5 180 48
shot over-the-book 0 0.5 0.08 180 82
shot across-the-room 2.6 0.9 1.6 238 20
# The glow's three colours (#447), each caught near its swell's peak (view.sh captures about 1.6 s after it is called):
# 3 raids left, 2, and due this raid.
for raids in 3 2 1; do
    peak="$(ev 'var b = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.LairLedgerBook>(); b.SetRaidsUntilDue('"$raids"'); float period = Plunderspell.Raid.LairLedgerBook.BreathSeconds('"$raids"'); float wait = period * 0.5f - 1.6f - (UnityEngine.Time.time % period); while (wait < 0f) wait += period; return wait.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);')"
    sleep "$peak"
    shot "glow-$raids-raids" 0 0.5 0.08 180 82
done
for f in drawing reading over-the-book across-the-room glow-3-raids glow-2-raids glow-1-raids; do [ -s "$out/$f.png" ] && check ok "$f.png saved" || check no "$f.png saved"; done

# Tearing the page out (#448): from over the book, look at it and press E (a queued key press through the Input System, as
# century_dial_check.sh does). The page must be torn and held by the local player (seat 0 solo); a second E changes nothing.
keys() { # keys <Key name or none>
    local state="new UnityEngine.InputSystem.LowLevel.KeyboardState()"; [ "$1" = none ] || state="new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.$1)"
    ev "UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, $state); return \"keys: $1\";"
}
page() { ev 'var k = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.DemandPageKeeper>(); return "torn " + k.IsTorn + " holder " + k.HolderSeat + " local " + k.LocalPlayerHolds + " drawing " + (k.Drawing != null);'; }
read -r x y z <<< "$(at 0 0.5 0.08)"
bash Tools/Unity/view.sh "$x" "$y" "$z" 180 82 >/dev/null 2>&1
sleep 1
looked="$(ev 'return Plunderspell.Raid.LookTarget.IsLookedAt(UnityEngine.Camera.main, UnityEngine.GameObject.Find("/LairRoom/LairLedger").transform, 3f).ToString();')"
log "looking at the book: $looked"
[ "$looked" = True ] && check ok "the player looks at the book from over the table" || check no "the player looks at the book from over the table"
before="$(page)"; log "before E: $before"
[ "$before" = "torn False holder -1 local False drawing True" ] && check ok "the page starts in the book" || check no "the page starts in the book"
log "$(keys E)"; sleep 0.3; log "$(keys none)"; sleep 1
after="$(page)"; log "after E: $after"
[ "$after" = "torn True holder 0 local True drawing True" ] && check ok "E tore the page out and the local player holds it" || check no "E tore the page out and the local player holds it"
log "$(keys E)"; sleep 0.3; log "$(keys none)"; sleep 1
again="$(page)"; log "after a second E: $again"
[ "$again" = "$after" ] && check ok "a second E on the stub changes nothing" || check no "a second E on the stub changes nothing"
shown="$(ev 'var p = UnityEngine.GameObject.Find("/LairRoom/LedgerPages"); return "drawing quad " + (p.transform.Find("DemandSketchPage") == null ? "gone" : p.transform.Find("DemandSketchPage").gameObject.activeSelf.ToString()) + " stub " + p.transform.Find("TornStub").gameObject.activeSelf;')"
log "$shown"
[ "$shown" = "drawing quad False stub True" ] && check ok "the drawing is hidden and the stub shown" || check no "the drawing is hidden and the stub shown"
shot torn-book 0 0.5 0.08 180 82
[ -s "$out/torn-book.png" ] && check ok "torn-book.png saved" || check no "torn-book.png saved"

# Reading the torn page (#449): hold R and the holder raises it on their own view; W held with R keeps walking; letting go lowers it.
chord() { # chord <Key names...>: all held together
    local list=""; for k in "$@"; do list="$list${list:+, }UnityEngine.InputSystem.Key.$k"; done
    ev "UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState($list)); UnityEngine.InputSystem.InputSystem.Update(); return \"keys: $*\";"
}
raise() { ev 'return UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.DemandPageReader>().Raise.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);'; }
where() { ev 'var p = StateMachine.PlayerStateMachine.Local.transform.position; return p.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " " + p.z.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);'; }
raise_check() { # raise_check <picture name> <label>
    log "$(chord R)"; sleep 0.8
    local up; up="$(raise)"; log "$2: raise $up"
    [ "$up" = "1.00" ] && check ok "$2: holding R raised the page" || check no "$2: holding R raised the page"
    bash Tools/Unity/capture.sh "$out/$1.png" >/dev/null 2>&1
    [ -s "$out/$1.png" ] && check ok "$1.png saved" || check no "$1.png saved"
}
lowered_check() { # lowered_check <label>
    log "$(keys none)"; sleep 0.8
    local down; down="$(raise)"; log "$1: raise $down after letting go"
    [ "$down" = "0.00" ] && check ok "$1: letting go lowered the page" || check no "$1: letting go lowered the page"
}
# view.sh left the player parked (kinematic) and looking down at the book: free them and look ahead, as a walking player does.
log "$(ev 'var p = StateMachine.PlayerStateMachine.Local; p.GetComponent<UnityEngine.Rigidbody>().isKinematic = false; p.FaceYaw(180f); typeof(StateMachine.PlayerStateMachine).GetField("_xRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(p, 8f); return "freed, looking ahead";')"
sleep 1
raise_check raised-page-lair "lair"
walk_from="$(where)"; log "$(chord R W)"; sleep 1; log "$(chord R W)"; sleep 1; walk_to="$(where)"; log "$(chord R)"
log "walked with the page up: $walk_from -> $walk_to, raise $(raise)"
[ "$walk_from" != "$walk_to" ] && [ "$(raise)" = "1.00" ] && check ok "W held with R kept walking and the page stayed up" || check no "W held with R kept walking and the page stayed up"
lowered_check "lair"

# Out on a raid: the page is still held and R raises it there too.
timeout 60 bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out.cs >/dev/null
for _ in $(seq 1 30); do state="$(ev 'return Plunderspell.Core.GameServices.GameState.CurrentState.ToString();' 2>/dev/null)"; [ "$state" = Playing ] && break; sleep 1; done
log "state $state"
[ "$state" = Playing ] && check ok "set out on a raid" || check no "set out on a raid"
sleep 3
raise_check raised-page-raid "raid"
lowered_check "raid"

log "fails $fails"
exit $((fails > 0))
