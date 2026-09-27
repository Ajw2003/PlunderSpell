#!/usr/bin/env bash
# Runs one action of Tools/Unity/eval/coop_carry.cs on one side of a local co-op session and prints
# its result. The host is the Editor in Play mode; the client is the Development build in
# Build/DevTest, reached by its process name.
# Usage: bash Tools/Unity/coop_eval.sh host|client <action> [arg]
set -euo pipefail

side="${1:?usage: coop_eval.sh host|client <action> [arg]}"
action="${2:?usage: coop_eval.sh host|client <action> [arg]}"
arg="${3:-}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

code="$(sed -e "s|__ACTION__|$action|" -e "s|__ARG__|$arg|" "$here/eval/coop_carry.cs")"

target=()
[ "$side" = "client" ] && target=(--runtime Plunderspell)

out="$(unity command "${target[@]}" eval --code "$code" --timeout 60 --caller plugin --skill unity-cli --no-banner --format json 2>&1 || true)"

printf '%s' "$out" | python -c "
import json, sys
text = sys.stdin.read()
start = text.find('{')
if start < 0:
    print('ERROR', text.strip()[:600]); sys.exit(1)
envelope, _ = json.JSONDecoder().raw_decode(text[start:])
data = envelope.get('data') or {}
r = data.get('result')
if isinstance(r, str):
    try: r = json.loads(r)
    except Exception: pass
if isinstance(r, dict) and 'result' in r:
    for d in r.get('diagnostics') or []:
        print('diag:', d)
    if r.get('success', True):
        print(r.get('result')); sys.exit(0)
    print('ERROR', r.get('error') or r.get('result')); sys.exit(1)
if not envelope.get('success'):
    print('ERROR', json.dumps(envelope.get('errors'))[:1500]); sys.exit(1)
print(r)
"
