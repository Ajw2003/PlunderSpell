#!/usr/bin/env bash
# Solo Play check that what the player carries on the beam travels with them through the Market door and gate (#333),
# in the test slot (test_slot.sh). Through the real carry path (ItemManager.StartDragging, what a left-click calls):
# grab a pile piece, go through the Market door, carry it onto the Goldsmith's counter and let go, haggle to a sale
# (SellCounter.Speak), grab the pouch, go back through the gate, let go over strongbox 1, read purse 1.
# Actions live in eval/coop_lair.cs (run here on the Editor side). Captures: the player holding the piece in the Market.
# Usage: bash Tools/Unity/carry_solo_check.sh <label>     Logs and captures: docs/generated/carry-travel-2026-10-07/
# Needs the Editor open on this project, not in Play mode. Leaves it stopped. Prints PASS or FAIL lines.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: carry_solo_check.sh <label>}"
out="docs/generated/carry-travel-2026-10-07"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
L() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/coop_lair.cs" timeout 90 bash Tools/Unity/coop_eval.sh host "$@" 2>&1; }
cleanup() { log "cleaning up"; unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true; test_slot_restore; log "$test_slot_msg"; }
trap cleanup EXIT
fails=0
check() { if [ "$1" = ok ]; then log "PASS $2"; else log "FAIL $2"; fails=$((fails + 1)); fi; }
rel() { python -c "import re,sys; m=re.search(r' rel (\S+) ', sys.argv[1]); print(m.group(1) if m else 'none')" "$1"; }
walkto() { # walkto <C# Vector3 expression>: face it, tilted 20 degrees down, and walk the player there at 4 m/s, stopping 1.4 m short (editor update loop)
    ev 'var player = StateMachine.PlayerStateMachine.Local; var rb = player.GetComponent<UnityEngine.Rigidbody>(); UnityEngine.Vector3 target = '"$1"'; var d0 = target - player.transform.position; d0.y = 0f; float yaw = UnityEngine.Quaternion.LookRotation(d0).eulerAngles.y; player.FaceYaw(yaw); typeof(StateMachine.PlayerStateMachine).GetField("_xRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(player, 20f); UnityEngine.Camera.main.transform.localRotation = UnityEngine.Quaternion.Euler(20f, yaw, 0f); UnityEditor.EditorApplication.CallbackFunction step = null; step = () => { var d = target - player.transform.position; d.y = 0f; if (d.magnitude <= 1.4f) { UnityEditor.EditorApplication.update -= step; return; } var next = player.transform.position + d.normalized * 4f * UnityEngine.Time.deltaTime; rb.position = next; player.transform.position = next; rb.linearVelocity = UnityEngine.Vector3.zero; }; UnityEditor.EditorApplication.update += step; return "walking from " + player.transform.position + " to " + target;'
}
same() { # same <rel a> <rel b> <what>: offsets within 0.15 m of each other
    python -c "
import sys
a=[float(x) for x in sys.argv[1].split(',')]; b=[float(x) for x in sys.argv[2].split(',')]
sys.exit(0 if sum((x-y)**2 for x,y in zip(a,b))**0.5 < 0.15 else 1)" "$1" "$2" 2>/dev/null && check ok "$3" || check no "$3"
}

playing="$(ev 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; trap - EXIT; exit 1; }
[ "$playing" = "False False" ] || { log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; }
test_slot_use || { log "FAIL cannot switch to the test slot: $test_slot_msg"; trap - EXIT; exit 1; }; log "$test_slot_msg"

unity command editor_play "${cli[@]}" >/dev/null
state=""
for _ in $(seq 1 60); do state="$(ev 'return Plunderspell.Core.GameServices.GameState.CurrentState.ToString();' 2>/dev/null)"; [ "$state" = MainMenu ] && break; sleep 1; done
log "state $state"
log "$(ev 'var s = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Net.CoopSession>(); s.PlaySolo(); Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.LairRoom); return "solo, slot " + Plunderspell.Lair.SaveSlots.Active;')"
for _ in $(seq 1 30); do p="$(ev 'return StateMachine.PlayerStateMachine.Local != null ? "player" : "none";' 2>/dev/null)"; [ "$p" = player ] && break; sleep 1; done
log "player: $p"
sleep 2

# A pile piece (a golden goblet), grabbed in the Lair, taken through the Market door.
log "$(L spawnpile 4)"; sleep 3
log "$(L grab pile)"; sleep 2
a="$(L held)"; log "in the Lair: $a"
case "$a" in "held "*"dragging True"*) check ok "the piece is held on the beam in the Lair" ;; *) check no "the piece is held on the beam in the Lair" ;; esac
log "$(L travel /LairRoom/MarketDoor)"
b="$(L held)"; log "just through the door: $b"
sleep 2
c="$(L held)"; log "two seconds later: $c"
same "$(rel "$a")" "$(rel "$b")" "just through the Market door the piece sits at the same offset from the player, in their view frame"
case "$b" in "held "*"dragging True"*) check ok "still held after the door" ;; *) check no "still held after the door" ;; esac
python -c "
import re,sys
m=re.search(r'speed (\S+) at (\S+) body (\S+)', sys.argv[1]); p=[float(x) for x in m.group(2).split(',')]; q=[float(x) for x in m.group(3).split(',')]
sys.exit(0 if float(m.group(1)) < 1.0 and sum((x-y)**2 for x,y in zip(p,q))**0.5 < 4 and p[0] > 1050 else 1)" "$b" && check ok "after the door it is slow, in the Market (x over 1050) and within 4 m of the player" || check no "after the door it is slow, in the Market and within 4 m of the player"
same "$(rel "$a")" "$(rel "$c")" "two seconds later it still hangs at that offset"
sleep 1
bash Tools/Unity/capture.sh "$out/$label-holding-in-the-market.png" >/dev/null 2>&1; log "capture: $out/$label-holding-in-the-market.png"

# Onto the Goldsmith's counter, let go, haggle to a sale.
log "$(walkto 'System.Linq.Enumerable.First(UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None), c => c.Vendor.ToString() == "Goldsmith").GetComponentInChildren<UnityEngine.BoxCollider>().bounds.center')"; sleep 6
log "held at the counter: $(L held)"
bash Tools/Unity/capture.sh "$out/$label-at-the-counter.png" >/dev/null 2>&1
log "$(L drop)"; sleep 3
log "$(L findnear 1103.6,-4.15)"
log "colliders under the box centre: $(ev 'var s = ""; var c = System.Linq.Enumerable.First(UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None), k => k.Vendor.ToString() == "Goldsmith"); var b = c.GetComponentInChildren<UnityEngine.BoxCollider>(); s += "box trigger " + b.isTrigger + " on " + b.name + "; "; foreach (var h in UnityEngine.Physics.RaycastAll(b.bounds.center + UnityEngine.Vector3.up * 3f, UnityEngine.Vector3.down, 6f)) s += h.collider.name + " y" + h.point.y.ToString("F2") + "; "; foreach (var h in UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(1103.5f, 3f, -3.6f), UnityEngine.Vector3.down, 6f)) s += "at piece: " + h.collider.name + " y" + h.point.y.ToString("F2") + "; "; return s;')"
log "counter box:$(ev 'return System.Linq.Enumerable.First(UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None), c => c.Vendor.ToString() == "Goldsmith").GetComponentInChildren<UnityEngine.BoxCollider>().bounds.ToString();')"
pc="$(L piece)"; log "$pc"
[ "$pc" = "pieces 1" ] && check ok "the dropped piece lies on the Goldsmith's counter" || check no "the dropped piece lies on the Goldsmith's counter ($pc)"
for _ in $(seq 1 15); do hl="$(L line)"; case "$hl" in *coin*) break ;; esac; sleep 1; done
log "$hl"
log "ahead at y1.45: $(ev 'var s = ""; foreach (var h in UnityEngine.Physics.RaycastAll(new UnityEngine.Vector3(1103.5f, 1.45f, -2.5f), UnityEngine.Vector3.back, 3f)) s += h.collider.name + " z" + h.point.z.ToString("F2") + " trig " + h.collider.isTrigger + "; "; var c = System.Linq.Enumerable.First(UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None), k => k.Vendor.ToString() == "Goldsmith"); var b = c.GetComponentInChildren<UnityEngine.BoxCollider>(); return s + " zone size " + b.size + " centre " + b.center + " scale " + b.transform.lossyScale + " rot " + b.transform.eulerAngles;')"
log "haggle open:$(ev 'var c = System.Linq.Enumerable.First(UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None), k => k.Vendor.ToString() == "Goldsmith"); var v = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Loot.LootValue>(); var rb = v.GetComponent<UnityEngine.Rigidbody>(); return "open " + (c.Open != null) + " piece at " + v.transform.position + " speed " + rb.linearVelocity.magnitude + " kinematic " + rb.isKinematic + " carried " + v.GetComponent<Plunderspell.Loot.LootPickup>().IsBeingCarried + " worth " + v.Worth;')"
m0="$(L money)"; log "before: $m0"
log "$(L speak Plus)"; sleep 2; log "$(L line)"
log "$(L speak Satis)"; sleep 2
hl="$(L line)"; log "$hl"
case "$hl" in *Done*) check ok "the haggle ends in a sale (Done)" ;; *) check no "the haggle ends in a sale" ;; esac
sleep 1
po="$(L pouch)"; log "$po"
case "$po" in "pouches 1 coins "*) check ok "the sale left one coin pouch" ;; *) check no "the sale left one coin pouch" ;; esac

# The pouch back through the gate and into strongbox 1.
p0="$(L purse 1)"; log "before: $p0"
log "$(L grab pouch)"; sleep 2
a="$(L held)"; log "pouch in the Market: $a"
log "$(L travel /MarketYard/LairExit)"
b="$(L held)"; log "pouch just through the gate: $b"
same "$(rel "$a")" "$(rel "$b")" "just through the gate the pouch sits at the same offset from the player"
case "$b" in "held "*"dragging True"*) check ok "pouch still held after the gate" ;; *) check no "pouch still held after the gate" ;; esac
log "$(walkto 'UnityEngine.GameObject.Find("/LairRoom/LairStrongbox/StrongboxLid1").GetComponent<UnityEngine.BoxCollider>().bounds.center')"; sleep 4
log "held at the strongbox: $(L held)"
log "$(L drop)"; sleep 3
po="$(L pouch)"; log "$po"
p1="$(L purse 1)"; log "after: $p1"
[ "$po" = "pouches 0" ] && check ok "the pouch is banked and gone" || check no "the pouch is banked and gone ($po)"
python -c "import sys; sys.exit(0 if int(sys.argv[2].split()[-1]) > int(sys.argv[1].split()[-1]) else 1)" "$p0" "$p1" && check ok "purse 1 grew ($p0 -> $p1)" || check no "purse 1 grew ($p0 -> $p1)"
[ "$fails" -eq 0 ] && log "PASS all carry-travel checks" || log "FAIL $fails carry-travel checks"
exit $((fails > 0))
