#!/usr/bin/env bash
# Runs C# in the connected Unity Editor and prints only its result (or its compile errors).
# Usage: bash Tools/Unity/eval.sh '<C# statements ending in return ...;>'
#        bash Tools/Unity/eval.sh --file Tools/Unity/eval/<name>.cs
set -euo pipefail

if [ "${1:-}" = "--file" ]; then
    out="$(unity command eval_file --file "$2" --timeout 600 --caller plugin --skill unity-cli --no-banner --format json 2>&1 || true)"
else
    out="$(unity command eval --code "$1" --timeout 600 --caller plugin --skill unity-cli --no-banner --format json 2>&1 || true)"
fi

printf '%s' "$out" | python -c "
import json, sys
text = sys.stdin.read()
start = text.find('{')
if start < 0:
    print(text.strip()); sys.exit(1)
envelope, _ = json.JSONDecoder().raw_decode(text[start:])
data = envelope.get('data') or {}
r = data.get('result')
if isinstance(r, str):
    try: r = json.loads(r)
    except Exception: pass
if isinstance(r, dict) and 'result' in r:
    if r.get('diagnostics'):
        for d in r['diagnostics']: print('diag:', d)
    print(r.get('result'))
    sys.exit(0 if r.get('success', True) else 1)
if not envelope.get('success'):
    print(json.dumps(envelope.get('errors'), indent=1)[:3000]); sys.exit(1)
print(r)
"
