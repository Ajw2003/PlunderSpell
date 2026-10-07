#!/usr/bin/env bash
# Sourced by checks that play a campaign (#313). They play in SaveSlots.TestSlot (99), which the Main Menu never
# offers (it steps through slots 1 to 3), so a check never touches the owner's saves. Needs the Editor out of Play mode.
#   test_slot_use      remembers the active slot, wipes the test slot (campaign and haul pile) and makes it active
#   test_slot_restore  makes the remembered slot active again (call from the check's exit trap)
test_slot_prev=""
test_slot_use() {
    test_slot_prev="$(timeout 60 bash "$(dirname "${BASH_SOURCE[0]}")/eval.sh" 'return Plunderspell.Lair.SaveSlots.Active;')" || return 1
    timeout 60 bash "$(dirname "${BASH_SOURCE[0]}")/eval.sh" 'Plunderspell.Lair.LairHubManager.ResetSlot(Plunderspell.Lair.SaveSlots.TestSlot); Plunderspell.Lair.SaveSlots.Active = Plunderspell.Lair.SaveSlots.TestSlot; return "test slot " + Plunderspell.Lair.SaveSlots.Active + " wiped, was on slot '"$test_slot_prev"'";'
}
test_slot_restore() {
    [ -n "$test_slot_prev" ] || return 0
    timeout 60 bash "$(dirname "${BASH_SOURCE[0]}")/eval.sh" "Plunderspell.Lair.SaveSlots.Active = $test_slot_prev; return \"active slot \" + Plunderspell.Lair.SaveSlots.Active;"
}
