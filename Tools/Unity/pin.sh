# Sourced by the Tools/Unity scripts: every `unity` call is pinned to THIS checkout's Editor, so it
# can never drift to another worktree's Editor (it did, 2026-09-30). Calls aimed at a built player
# (--runtime) are left alone.
_pin_repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd -W 2>/dev/null || pwd)"
unity() {
    case " $* " in
        *" --runtime "*) command unity "$@" ;;
        *) command unity "$@" --project-path "$_pin_repo" ;;
    esac
}
export _pin_repo
export -f unity
