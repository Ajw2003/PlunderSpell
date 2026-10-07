#!/usr/bin/env bash
# Sourced by checks that play a campaign (#313). They play in SaveSlots.TestSlot (99), which the Main Menu never
# offers (it steps through slots 1 to 3), so a check never touches the owner's saves. Needs the Editor out of Play mode.
#   test_slot_use      remembers the active slot, wipes the test slot (campaign and haul pile) and makes it active
#   test_slot_restore  makes the remembered slot active again (call from the check's exit trap)
# Both leave a one-line account in $test_slot_msg. Call them directly, not inside $(...): the remembered slot is a variable.
test_slot_prev=""
test_slot_msg=""
test_slot_eval() { timeout 60 bash "$(dirname "${BASH_SOURCE[0]}")/eval.sh" "$1"; }
test_slot_use() {
    test_slot_prev="$(test_slot_eval 'return Plunderspell.Lair.SaveSlots.Active;')" || { test_slot_msg="cannot read the active slot"; return 1; }
    case "$test_slot_prev" in 99) test_slot_prev=1 ;; esac # a crashed check left the test slot active: the owner's slot is then unknown, so slot 1
    test_slot_msg="$(test_slot_eval 'Plunderspell.Lair.LairHubManager.ResetSlot(Plunderspell.Lair.SaveSlots.TestSlot); Plunderspell.Lair.SaveSlots.Active = Plunderspell.Lair.SaveSlots.TestSlot; return "test slot " + Plunderspell.Lair.SaveSlots.Active + " wiped";')" || return 1
    test_slot_msg="$test_slot_msg, owner's slot to restore: $test_slot_prev"
}
test_slot_restore() {
    [ -n "$test_slot_prev" ] || { test_slot_msg="no slot to restore"; return 0; }
    test_slot_msg="$(test_slot_eval "Plunderspell.Lair.SaveSlots.Active = $test_slot_prev; return \"active slot \" + Plunderspell.Lair.SaveSlots.Active;")"
}
