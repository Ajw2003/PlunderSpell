#!/usr/bin/env bash
# Solo Play check of the coin pouch and the strongboxes (#313), in the test slot (test_slot.sh): sell a golden goblet at
# the Goldsmith's counter, find the pouch it leaves (coins, mass, no LootValue), capture it on the counter, move it by its
# rigidbody into strongbox 1 and read purse 1 and the debt before and after, and that the pouch is gone.
# Usage: bash Tools/Unity/pouch_solo_check.sh <label>     Logs and the capture: docs/generated/pouch-solo-2026-10-07/
# Needs the Editor open on this project, not in Play mode. Leaves it stopped. Prints PASS or FAIL lines.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: pouch_solo_check.sh <label>}"
out="docs/generated/pouch-solo-2026-10-07"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
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
for _ in $(seq 1 30); do p="$(ev 'return StateMachine.PlayerStateMachine.Local != null ? "player" : "none";' 2>/dev/null)"; [ "$p" = player ] && break; sleep 1; done
log "player: $p"
log "to the Market: $(ev 'var d = UnityEngine.GameObject.Find("/LairRoom/MarketDoor").GetComponent<Plunderspell.Raid.RoomTravel>(); d.Travel(StateMachine.PlayerStateMachine.Local); return "travelled";')"

# A golden goblet (loot table entry 4) on the Goldsmith's counter; the vendor opens, Plus then Satis sells it.
log "$(ev --file Tools/Unity/eval/market_spawn_piece.cs)"
for _ in $(seq 1 15); do open="$(ev 'foreach (var c in UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None)) if (c.Vendor.ToString() == "Goldsmith") return c.Open != null ? "open" : "closed"; return "none";')"; [ "$open" = open ] && break; sleep 1; done
log "haggle: $open"
before="$(ev 'var l = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Lair.LairHubManager>(); return l.TotalDebt + " " + l.Purse(0) + " " + l.Purse(1);')"
log "debt purse1 purse2 before the sale: $before"
log "$(ev 'foreach (var c in UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None)) if (c.Vendor.ToString() == "Goldsmith") { var o = c.Answer(Plunderspell.Market.HaggleWord.Satis); return "Satis: " + o; } return "none";')"
sleep 1
pouch="$(ev 'var ps = UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.CoinPouch>(UnityEngine.FindObjectsSortMode.None); if (ps.Length != 1) return "pouches " + ps.Length; var p = ps[0]; var l = UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None); return "pouch coins " + p.Coins + " mass " + p.GetComponent<UnityEngine.Rigidbody>().mass + " lootvalue " + (p.GetComponent<Plunderspell.Loot.LootValue>() != null) + " carryable " + (p.GetComponent<Plunderspell.Loot.LootPickup>() != null) + " at " + p.transform.position;')"
log "$pouch"
case "$pouch" in "pouch coins "*"lootvalue False carryable True"*) check ok "the sale left one carryable pouch with coins and no LootValue" ;; *) check no "the sale left one carryable pouch with coins and no LootValue" ;; esac
log "sold piece gone: $(ev 'return "loot pieces " + UnityEngine.Object.FindObjectsByType<Plunderspell.Loot.LootValue>(UnityEngine.FindObjectsSortMode.None).Length;')"

# Look at it on the counter: a spare camera 0.7 m from it, above and on the player's side (the player's own view cannot tilt this far down).
log "$(ev 'var p = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.CoinPouch>().transform.position; var side = (StateMachine.PlayerStateMachine.Local.transform.position - p); side.y = 0f; var cam = new UnityEngine.GameObject("PouchCam").AddComponent<UnityEngine.Camera>(); cam.transform.position = p + side.normalized * 0.5f + UnityEngine.Vector3.up * 0.45f; cam.transform.LookAt(p); cam.nearClipPlane = 0.05f; return "PouchCam at " + cam.transform.position;')"
bash Tools/Unity/capture.sh "$out/$label-pouch-on-counter.png" camera PouchCam >/dev/null 2>&1; log "capture: $out/$label-pouch-on-counter.png"
ev 'UnityEngine.Object.Destroy(UnityEngine.GameObject.Find("PouchCam")); return "camera removed";' >/dev/null

# Carry it into strongbox 1 through its rigidbody: kinematic hand (carried), then let go in the lid.
log "$(ev 'var pickup = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.CoinPouch>().GetComponent<Plunderspell.Loot.LootPickup>(); var travel = UnityEngine.GameObject.Find("/MarketYard/LairExit").GetComponent<Plunderspell.Raid.RoomTravel>(); travel.Travel(StateMachine.PlayerStateMachine.Local); return "back in the Lair, pouch carried " + pickup.IsBeingCarried;')"
sleep 1
log "$(ev 'var lid = UnityEngine.GameObject.Find("/LairRoom/LairStrongbox/StrongboxLid1"); var pouch = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.CoinPouch>(); var body = pouch.GetComponent<UnityEngine.Rigidbody>(); body.linearVelocity = UnityEngine.Vector3.zero; body.position = lid.GetComponent<UnityEngine.BoxCollider>().bounds.center; pouch.transform.position = body.position; return "pouch (" + pouch.Coins + " coins) set into " + lid.name + " at " + body.position;')"
for _ in $(seq 1 10); do left="$(ev 'return UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.CoinPouch>(UnityEngine.FindObjectsSortMode.None).Length.ToString();')"; [ "$left" = 0 ] && break; sleep 1; done
after="$(ev 'var l = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Lair.LairHubManager>(); return l.TotalDebt + " " + l.Purse(0) + " " + l.Purse(1);')"
log "debt purse1 purse2 after the strongbox: $after (pouches left: $left)"
python -c "
import sys
b = [float(x) for x in sys.argv[1].split()]; a = [float(x) for x in sys.argv[2].split()]
sys.exit(0 if a[1] > b[1] and a[2] == b[2] and a[0] < b[0] and sys.argv[3] == '0' else 1)" "$before" "$after" "$left" && r=ok || r=no
check $r "strongbox 1 banked the pouch: purse 1 grew, purse 2 did not, the debt fell, the pouch is gone"
log "saved purse 1 in slot 99: $(ev 'return Plunderspell.Lair.LairHubManager.PeekPurse(Plunderspell.Lair.SaveSlots.TestSlot, 0).ToString();')"
[ "$fails" -eq 0 ] && log "PASS all pouch checks" || log "FAIL $fails pouch checks"
exit $((fails > 0))
