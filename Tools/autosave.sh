#!/usr/bin/env bash
# Periodically commit and push work in progress, so a session that is cut off (usage
# limit, container reclaimed) loses at most one interval of work.
#
# Usage (from anywhere):
#   Tools/autosave.sh [--interval SECONDS] [--count N] [--dry-run] PATH [PATH ...]
#
#   --interval  seconds between checks (default 180)
#   --count     number of checks before exiting (default 160, i.e. 8 hours at 180 s)
#   --dry-run   report what would be committed; commit and push nothing
#   PATH ...    repo-relative paths to watch; only these are ever staged
#
# Example — the loop used while the ArtForge enemy workers run:
#   Tools/autosave.sh Tools/ArtForge Assets/Models/ArtBible/Enemies \
#       Assets/Models/ArtBible/artforge_manifest.json docs/art/models
#
# Rules it keeps:
#   * Only commits on a branch whose name starts with "claude/" — never on main or on
#     someone else's branch. It refuses to start otherwise.
#   * Only stages the paths given, so it cannot sweep up another agent's files that
#     happen to be in the same checkout. Keep the list as narrow as the work.
#   * Pushes to the current branch's upstream, retrying 4 times (2, 4, 8, 16 s).
#   * Prints one line per check ("checkpoint <sha> pushed" / "nothing new" / an error),
#     so it is never silent. Run it where you can see it, not detached.

set -u

interval=180
count=160
dry_run=0
paths=()
while [ $# -gt 0 ]; do
    case "$1" in
        --interval) interval="$2"; shift 2 ;;
        --count) count="$2"; shift 2 ;;
        --dry-run) dry_run=1; shift ;;
        -h|--help) sed -n '2,24p' "$0"; exit 0 ;;
        *) paths+=("$1"); shift ;;
    esac
done

if [ ${#paths[@]} -eq 0 ]; then
    echo "autosave: give at least one path to watch (see --help)" >&2
    exit 2
fi

repo="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)" || exit 2
cd "$repo" || exit 2
branch="$(git rev-parse --abbrev-ref HEAD)"
case "$branch" in
    claude/*) ;;
    *) echo "autosave: refusing to run on '$branch' — only claude/* branches" >&2; exit 2 ;;
esac

echo "autosave: watching ${paths[*]} on $branch every ${interval}s, $count checks$([ $dry_run -eq 1 ] && echo ' (dry run)')"
for ((i = 1; i <= count; i++)); do
    [ "$i" -gt 1 ] && sleep "$interval"
    stamp="$(date -u +%H:%M:%S)"
    changes="$(git status --porcelain -- "${paths[@]}")"
    if [ -z "$changes" ]; then
        echo "$stamp nothing new"
        continue
    fi
    if [ $dry_run -eq 1 ]; then
        echo "$stamp would commit $(echo "$changes" | wc -l) path(s):"
        echo "$changes" | sed 's/^/    /'
        continue
    fi
    # A watched path that does not exist yet (an output folder a worker has not created)
    # makes `git add` reject the whole command, so stage only paths that exist on disk or
    # that git already tracks (a tracked path may have been deleted, which is a change too).
    present=()
    for path in "${paths[@]}"; do
        if [ -e "$path" ] || [ -n "$(git ls-files -- "$path")" ]; then present+=("$path"); fi
    done
    if ! git add -- "${present[@]}"; then
        echo "$stamp ERROR: git add failed (see above)"
        continue
    fi
    if ! git commit -q -m "chore: auto-checkpoint work in progress

Paths watched: ${paths[*]}

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_014LiHGbRYGYiFrULqqX1U1w"; then
        echo "$stamp ERROR: commit failed (see above)"
        continue
    fi
    pushed=0
    for wait in 2 4 8 16; do
        if git push -q origin "$branch"; then pushed=1; break; fi
        sleep "$wait"
    done
    if [ $pushed -eq 1 ]; then
        echo "$stamp checkpoint $(git rev-parse --short HEAD) pushed"
    else
        echo "$stamp ERROR: checkpoint $(git rev-parse --short HEAD) committed but NOT pushed"
    fi
done
echo "autosave: finished $count checks"
