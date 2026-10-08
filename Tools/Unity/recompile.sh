#!/usr/bin/env bash
# Recompiles scripts in the connected Unity Editor, waits for it, and prints any compile errors.
# Exit 0 when the compile finished clean, 1 on compile errors or a timeout.
# Usage: bash Tools/Unity/recompile.sh
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/pin.sh"

cli=(--caller plugin --skill unity-cli --no-banner --format json)

# Prints the status on the first line, then one compile error per line.
read_status() {
    python -c "
import json, sys
try:
    r = json.load(sys.stdin)['data']['result']
    r = json.loads(r) if isinstance(r, str) else r
except Exception:
    print('reloading')
    sys.exit(0)
print('failed' if r.get('failed') or r.get('compilationFailed') else r.get('status'))
for e in r.get('errors') or []:
    print(e if isinstance(e, str) else e.get('message', e))
"
}

exit_if_unity_dialog
# A failed request used to end the script silently under `set -e`; stop with the reason if a dialog
# holds the Editor, and otherwise let the bounded poll below find out what happened (#371).
if ! unity command recompile "${cli[@]}" > /dev/null; then
    exit_if_unity_dialog
fi

seen_busy=0
for attempt in $(seq 1 120); do
    sleep 2
    # The Editor drops the connection during the domain reload; keep polling through it.
    raw="$(unity command recompile_status "${cli[@]}" 2>/dev/null || true)"
    # ...but a dialog waiting for a click never ends by itself (#371).
    case "$raw" in *"Main thread operation timed out"*) exit_if_unity_dialog ;; esac
    report="$(printf '%s' "$raw" | read_status || echo reloading)"
    status="$(printf '%s\n' "$report" | head -n 1)"
    case "$status" in
        failed)
            echo "Compile FAILED:"
            printf '%s\n' "$report" | tail -n +2
            exit 1
            ;;
        completed|up_to_date|idle)
            # A "completed" before we ever saw it busy may be the previous compile; give ours a moment.
            if [ "$seen_busy" = 1 ] || [ "$attempt" -ge 4 ]; then
                echo "Compile clean ($status)."
                exit 0
            fi
            ;;
        *)
            seen_busy=1
            ;;
    esac
done

echo "Timed out after 4 minutes waiting for the recompile." >&2
exit 1
