#!/usr/bin/env bash
# Solo Play check that a piece too heavy to lift alone sells from the floor at a counter's foot (#356): the heaviest
# loot-table piece is set on the Lair's pile, checked too heavy for one player, then put by its rigidbody on the floor in
# the Goldsmith's SellFoot box (towing it across the yard is not scripted here); the haggle opens on the slate, Satis sells
# it, the piece goes and a pouch appears. Runs in the test slot (test_slot.sh), leaves the Editor stopped.
# Usage: bash Tools/Unity/floor_sale_check.sh     Log and capture: docs/generated/floor-sale-2026-10-07/. Prints PASS or FAIL lines.
# Needs the Editor open on this project, not in Play mode.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
out="docs/generated/floor-sale-2026-10-07"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/run.log"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
L() { COOP_EVAL_FILE="$repo/Tools/Unity/eval/coop_lair.cs" timeout 90 bash Tools/Unity/coop_eval.sh host "$@" 2>&1; }
cleanup() { log "cleaning up"; unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true; test_slot_restore; log "$test_slot_msg"; }
trap cleanup EXIT
fails=0
check() { if [ "$1" = ok ]; then log "PASS $2"; else log "FAIL $2"; fails=$((fails + 1)); fi; }
goldsmith='System.Linq.Enumerable.First(UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.SellCounter>(UnityEngine.FindObjectsSortMode.None), c => c.Vendor.ToString() == "Goldsmith")'

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

# The heaviest piece the raid's loot table holds, by its prefab's rigidbody mass.
heaviest="$(ev 'object table = null; foreach (var sp in UnityEngine.Object.FindObjectsByType<Plunderspell.Raid.LootSpawner>(UnityEngine.FindObjectsSortMode.None)) { var f = sp.GetType().GetField("Table") ?? sp.GetType().GetField("_table", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); var p = sp.GetType().GetProperty("Table"); var t = p != null ? p.GetValue(sp) : f != null ? f.GetValue(sp) : null; if (t != null) table = t; } var entries = (System.Collections.IList)table.GetType().GetField("Entries").GetValue(table); int best = -1; float mass = 0f; for (int i = 0; i < entries.Count; i++) { var prefab = (UnityEngine.GameObject)entries[i].GetType().GetField("Prefab").GetValue(entries[i]); var rb = prefab != null ? prefab.GetComponent<UnityEngine.Rigidbody>() : null; if (rb != null && rb.mass > mass) { mass = rb.mass; best = i; } } return best + " " + mass;')"
log "heaviest entry: $heaviest"
log "$(L spawnpile "${heaviest%% *}")"; sleep 3

heavy="$(ev 'var p = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Loot.LootValue>(); var item = p.GetComponent<Item>(); return p.name + " mass " + p.GetComponent<UnityEngine.Rigidbody>().mass + " too heavy alone " + (item != null && item.IsTooHeavyToLift);')"
log "piece: $heavy"
case "$heavy" in *"too heavy alone True") check ok "the piece is too heavy for one player to lift" ;; *) check no "the piece is too heavy for one player to lift ($heavy)" ;; esac

log "$(L travel /LairRoom/MarketDoor)"; sleep 2
log "$(ev 'UnityEngine.BoxCollider zone = null; foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.BoxCollider>(UnityEngine.FindObjectsSortMode.None)) if (b.name == "SellFoot" && (zone == null || (b.bounds.center - '"$goldsmith"'.transform.position).sqrMagnitude < (zone.bounds.center - '"$goldsmith"'.transform.position).sqrMagnitude)) zone = b; var p = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Loot.LootValue>(); var rb = p.GetComponent<UnityEngine.Rigidbody>(); var at = new UnityEngine.Vector3(zone.bounds.center.x, zone.bounds.min.y + 0.4f, zone.bounds.center.z); rb.linearVelocity = UnityEngine.Vector3.zero; rb.position = at; p.transform.position = at; return "set on the Goldsmith foot at " + at;')"
log "$(L look)"; sleep 5
line=""; for _ in $(seq 1 15); do line="$(L line)"; case "$line" in *coin*) break ;; esac; sleep 1; done
log "slate: $line"
case "$line" in *coin*) check ok "a heavy piece on the floor at the counter's foot opens a haggle" ;; *) check no "a heavy piece on the floor at the counter's foot opens a haggle" ;; esac
bash Tools/Unity/capture.sh "$out/heavy-piece-at-the-foot.png" >/dev/null 2>&1; log "capture: $out/heavy-piece-at-the-foot.png"

log "$(L speak Satis)"; sleep 2
sold="$(L line)"; log "slate: $sold"
po="$(L pouch)"; log "$po"
left="$(ev 'return "heavy pieces left " + UnityEngine.Object.FindObjectsByType<Plunderspell.Loot.LootValue>(UnityEngine.FindObjectsSortMode.None).Length;')"; log "$left"
case "$sold" in *Done*) check ok "Satis sells it" ;; *) check no "Satis sells it" ;; esac
case "$po" in "pouches 1 coins "*) check ok "the sale left one coin pouch" ;; *) check no "the sale left one coin pouch" ;; esac
[ "$left" = "heavy pieces left 0" ] && check ok "the piece is gone" || check no "the piece is gone ($left)"

[ "$fails" -eq 0 ] && log "PASS all floor-sale checks" || log "FAIL $fails floor-sale checks"
exit $((fails > 0))
