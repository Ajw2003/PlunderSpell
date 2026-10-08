#!/usr/bin/env bash
# Co-op on this PC, to play by hand (#372). The Editor hosts over the local network (not Steam: a second copy of
# the game on this PC is the same Steam account, and Steam will not let it join itself), and the Development build
# in Build/DevTest joins it at 127.0.0.1 and is put beside the Editor. Both stay running for you to play.
#   - Editor not in Play mode: rebuilds the client if the game changed since the last build, presses Play,
#     hosts on this PC.
#   - Editor already in Play mode: hosts on this PC if it is not in a session yet (the menu's "Host on this PC"
#     does the same), then starts the client. It cannot rebuild while playing and says so if the build is old.
# Plays in your own save slot, not the checks' test slot. To end: close the client window, stop Play.
# Usage: bash Tools/Unity/coop_local.sh [--build|--no-build]
set -uo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/settings_restore.sh"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo"
build=auto
while [ $# -gt 0 ]; do
    case "$1" in --build) build=yes ;; --no-build) build=no ;; *) echo "unknown option $1"; exit 1 ;; esac
    shift
done
cli=(--no-banner --format json)
say() { printf '%s %s\n' "$(date +%T)" "$*"; }
ev() { timeout 90 bash Tools/Unity/eval.sh "$@"; }
field() { python -c "import json,sys; r=json.load(sys.stdin)['data']['result']; r=json.loads(r) if isinstance(r,str) else r; print(r.get(sys.argv[1]))" "$1"; }

exit_if_unity_dialog
editor="$(ev 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;')" \
    || { say "The Editor did not answer. Is it open on $(basename "$repo")?"; exit 1; }
case "$editor" in
    "False False") playing=no ;;
    "True False") playing=yes ;;
    *) say "The Editor is compiling; run this again when it has finished."; exit 1 ;;
esac
if tasklist //FI "IMAGENAME eq Plunderspell.exe" 2>/dev/null | grep -q Plunderspell; then
    say "A Plunderspell window is already open. Close it first: it is either already in your game, or it would lock the build."
    exit 1
fi

stale=no
[ -f Build/DevTest/.built ] || stale=yes
if [ "$stale" = no ] && [ -n "$(find Assets ProjectSettings -newer Build/DevTest/.built -type f ! -path '*/Tests/*' ! -path 'ProjectSettings/Packages/*' ! -name '*.meta' ! -name 'ProjectSettings.asset' -print -quit)" ]; then stale=yes; fi
[ "$build" = auto ] && build="$stale"
if [ "$build" = yes ] && [ "$playing" = yes ]; then
    say "The client build is older than your changes, but the Editor is in Play mode and cannot build. Using the old build;"
    say "stop Play and run this again to rebuild it."
    build=no
fi
if [ "$build" = yes ]; then
    settings_save || { say "Cannot save ProjectSettings before building."; exit 1; }
    say "Building the client (Build/DevTest), about 30 s..."
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":true}' --confirm true "${cli[@]}" >/dev/null
    unity command build --target StandaloneWindows64 --outputPath Build/DevTest/Plunderspell.exe --options '["Development"]' --confirm true "${cli[@]}" >/dev/null
    status=""
    for _ in $(seq 1 180); do sleep 5; status="$(unity command build_status "${cli[@]}" | field status)"; [ "$status" = "completed" ] && break; done
    result="$(unity command build_status "${cli[@]}" | field result)"
    unity command set_runtime_pipeline_settings --settings '{"enableInBuilds":false}' --confirm true "${cli[@]}" >/dev/null
    settings_restore
    [ "$result" = "Succeeded" ] || { say "The build did not succeed: $status $result"; exit 1; }
    touch Build/DevTest/.built; say "Build succeeded."
fi
[ -f Build/DevTest/Plunderspell.exe ] || { say "No client build yet: run this with the Editor out of Play mode so it can build one."; exit 1; }

if [ "$playing" = no ]; then
    say "Pressing Play in the Editor..."
    unity command editor_play "${cli[@]}" >/dev/null
    for _ in $(seq 1 60); do
        menu="$(ev 'return Plunderspell.Core.GameServices.Coop != null ? Plunderspell.Core.GameServices.GameState.CurrentState.ToString() : "starting";' 2>/dev/null)"
        [ "$menu" = MainMenu ] && break; sleep 1
    done
    [ "$menu" = MainMenu ] || { say "The Editor did not reach the main menu (last: $menu)."; exit 1; }
fi

host="$(ev 'var coop = Plunderspell.Core.GameServices.Coop; if (coop == null) return "no session object"; if (!coop.IsInSession) coop.HostLocal(); return coop.Status;')"
case "$host" in
    *"local network"*) say "Editor: $host" ;;
    *) say "The Editor is already in a session that is not on the local network ($host). Leave it (pause menu) and run this again."; exit 1 ;;
esac

./Build/DevTest/Plunderspell.exe -coop-join 127.0.0.1 -screen-fullscreen 0 -screen-width 1280 -screen-height 720 >/dev/null 2>&1 &
say "Client window starting (Build/DevTest, pid $!), joining 127.0.0.1..."
players=""
for _ in $(seq 1 60); do
    players="$(ev 'return StateMachine.PlayerStateMachine.Local == null ? "0" : UnityEngine.Object.FindObjectsByType<StateMachine.PlayerStateMachine>(UnityEngine.FindObjectsSortMode.None).Length.ToString();' 2>/dev/null)"
    [ "$players" = 2 ] && break; sleep 1
done
if [ "$players" != 2 ]; then
    say "The client did not appear in the Editor's game within 60 s (players: ${players:-none}). Its window shows why, or see its log."
    exit 1
fi
say "Placed: $(powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$repo/Tools/Unity/place_windows.ps1")" beside | tr -d '\r')"
say "Co-op on this PC is up: the Editor hosts, the client has joined (2 players). Close the client window and stop Play to end."
