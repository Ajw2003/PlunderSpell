# Sourced by the Tools/Unity scripts: every `unity` call is pinned to THIS checkout's Editor, so it
# can never drift to another worktree's Editor (it did, 2026-09-30). Calls aimed at a built player
# (--runtime) are left alone.
_pin_repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd -W 2>/dev/null || pwd)"
_pin_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Prints each dialog this checkout's Editor has open ('Title' [Buttons]), or nothing (#371).
unity_dialog() {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$_pin_dir/unity_dialog.ps1")" \
        -Project "$(basename "$_pin_repo")" 2>/dev/null | tr -d '\r'
}

# Ends the calling script when a dialog is holding the Editor: nothing runs until someone clicks it,
# so waiting longer only hides why (#371).
exit_if_unity_dialog() {
    local dialog
    dialog="$(unity_dialog)"
    if [ -n "$dialog" ]; then
        echo "BLOCKED: the Unity Editor is waiting for a click on $dialog; nothing else runs until it is answered." >&2
        exit 1
    fi
}

unity() {
    case " $* " in
        *" --runtime "*) command unity "$@"; return ;;
    esac
    local out rc=0
    out="$(command unity "$@" --project-path "$_pin_repo")" || rc=$?
    if [ -n "$out" ]; then printf '%s\n' "$out"; fi
    # A call that fails or finds the main thread held is often stuck behind a dialog waiting for a
    # click; the CLI only says "timed out", so say which dialog (#371).
    case "$rc:$out" in
        0:*"Main thread operation timed out"*|[1-9]*)
            local dialog
            dialog="$(unity_dialog)"
            if [ -n "$dialog" ]; then echo "BLOCKED: the Unity Editor is waiting for a click on $dialog." >&2; fi
            ;;
    esac
    return $rc
}
export _pin_repo _pin_dir
export -f unity unity_dialog exit_if_unity_dialog
