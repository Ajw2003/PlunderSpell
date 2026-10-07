#!/usr/bin/env bash
# Solo Play check of the Collector (#313), in the test slot (test_slot.sh): bank a 200-coin pouch into purse I, read purse /
# debt / paid before, capture the Lair screen's ledger, set out (the Collector calls), read them after and capture the ledger
# again. Alone, seat I owes the whole debt (500), so the Collector takes the 200 and the debt falls to 300 (then +50 for the raid).
# Usage: bash Tools/Unity/collector_solo_check.sh <label>     Logs and captures: docs/generated/collector-solo-2026-10-07/
# Needs the Editor open on this project, not in Play mode. Leaves it stopped. Prints PASS or FAIL lines.
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test_slot.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
label="${1:?usage: collector_solo_check.sh <label>}"
out="docs/generated/collector-solo-2026-10-07"; mkdir -p "$out"
cli=(--no-banner --format json)
log() { printf '%s %s\n' "$(date +%T)" "$*" | tee -a "$out/$label-run.log"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
cleanup() { log "cleaning up"; unity command editor_stop "${cli[@]}" >/dev/null 2>&1 || true; test_slot_restore; log "$test_slot_msg"; }
trap cleanup EXIT
fails=0
check() { if [ "$1" = ok ]; then log "PASS $2"; else log "FAIL $2"; fails=$((fails + 1)); fi; }
read_lair='var l = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Lair.LairHubManager>(); return l.TotalDebt + " " + l.Purse(0) + " " + l.PaidLast(0) + " | " + l.CollectorLine;'

playing="$(ev 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" || { log "FAIL Editor did not answer"; trap - EXIT; exit 1; }
[ "$playing" = "False False" ] || { log "FAIL Editor is playing or compiling ($playing)"; trap - EXIT; exit 1; }
test_slot_use || { log "FAIL cannot switch to the test slot: $test_slot_msg"; trap - EXIT; exit 1; }; log "$test_slot_msg"
s1_before="$(ev 'var h = Plunderspell.Lair.LairHubManager.Peek(1); return "slot1 debt " + h.TotalDebt + " gold " + h.AccumulatedGold + " purses " + Plunderspell.Lair.LairHubManager.PeekPurse(1, 0) + " " + Plunderspell.Lair.LairHubManager.PeekPurse(1, 1) + " paid " + Plunderspell.Lair.LairHubManager.PeekPaidLast(1, 0);')"; log "before: $s1_before"

unity command editor_play "${cli[@]}" >/dev/null
state=""
for _ in $(seq 1 60); do state="$(ev 'return Plunderspell.Core.GameServices.GameState.CurrentState.ToString();' 2>/dev/null)"; [ "$state" = MainMenu ] && break; sleep 1; done
log "state $state"
log "$(ev 'var s = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Net.CoopSession>(); s.PlaySolo(); Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.LairRoom); return "solo, slot " + Plunderspell.Lair.SaveSlots.Active;')"
for _ in $(seq 1 30); do p="$(ev 'return StateMachine.PlayerStateMachine.Local != null ? "player" : "none";' 2>/dev/null)"; [ "$p" = player ] && break; sleep 1; done
log "player: $p"

log "$(ev 'UnityEngine.Object.FindFirstObjectByType<Plunderspell.Lair.LairHubManager>().BankPouch(0, 200); return "banked 200 into purse I";')"
sleep 1
before="$(ev "$read_lair")"; log "debt purse-I paid-I | line, before setting out: $before"
log "$(ev 'Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.Lair); return "ledger open";')"; sleep 2
bash Tools/Unity/capture.sh "$out/$label-ledger-before.png" >/dev/null 2>&1; log "capture: $out/$label-ledger-before.png"

log "$(ev 'Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.LairRoom); return "back in the room";')"; sleep 1
log "$(ev --file Tools/Unity/eval/set_out.cs)"; sleep 2
after="$(ev "$read_lair")"; log "debt purse-I paid-I | line, after setting out: $after"
python -c "
import sys
b = sys.argv[1].split(' | ')[0].split(); a = sys.argv[2].split(' | ')[0].split()
sys.exit(0 if float(b[0]) == 500 and float(b[1]) == 200 and float(b[2]) == 0 and float(a[1]) == 0 and float(a[2]) == 200 and float(a[0]) == 350 and 'takes 200 from I' in sys.argv[2] else 1)" "$before" "$after" && r=ok || r=no
check $r "the Collector took the 200 from purse I (debt 500 -> 300, then +50 for the raid = 350), paid-last 200, and said so"
log "saved in slot 99: $(ev 'var s = Plunderspell.Lair.SaveSlots.TestSlot; return "purse I " + Plunderspell.Lair.LairHubManager.PeekPurse(s, 0) + " paid " + Plunderspell.Lair.LairHubManager.PeekPaidLast(s, 0) + " debt " + Plunderspell.Lair.LairHubManager.Peek(s).TotalDebt;')"

log "$(ev 'Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.Lair); return "ledger open";')"; sleep 2
bash Tools/Unity/capture.sh "$out/$label-ledger-after.png" >/dev/null 2>&1; log "capture: $out/$label-ledger-after.png"
s1_after="$(ev 'var h = Plunderspell.Lair.LairHubManager.Peek(1); return "slot1 debt " + h.TotalDebt + " gold " + h.AccumulatedGold + " purses " + Plunderspell.Lair.LairHubManager.PeekPurse(1, 0) + " " + Plunderspell.Lair.LairHubManager.PeekPurse(1, 1) + " paid " + Plunderspell.Lair.LairHubManager.PeekPaidLast(1, 0);')"; log "after: $s1_after"
[ "$s1_before" = "$s1_after" ] && r=ok || r=no; check $r "the owner's slot 1 is unchanged ($s1_before)"
[ "$fails" -eq 0 ] && log "PASS all Collector solo checks" || log "FAIL $fails Collector solo checks"
exit $((fails > 0))
