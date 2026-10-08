#!/usr/bin/env bash
# Measures frame times in a live solo raid with "Guards hear my voice" on, then off, so the cost of the
# always-listening recogniser shows up as a number. Drives the Editor that is open on this worktree.
# Usage: bash Tools/Unity/frame_cost_check.sh [frames per pass, default 600]
# Restores the saved setting and leaves Play mode when done. Solo on purpose: it measures one machine's
# main-thread cost, which co-op does not change.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/../.."
export UNITY_PROJECT_PATH="$(pwd -W 2>/dev/null || pwd)"
FRAMES="${1:-600}"
ev() { bash Tools/Unity/eval.sh "$1"; }
evf() { bash Tools/Unity/eval.sh --file "$1"; }

if [ "$(ev 'return UnityEditor.EditorApplication.isPlaying.ToString();')" != "False" ]; then
    echo "The Editor is already in Play mode; stop it first." >&2
    exit 1
fi

original="$(ev 'return Plunderspell.Core.AudioInputSettings.GuardsHearChatter.ToString();')"
echo "Saved 'Guards hear my voice' is $original; it is restored at the end."

ev 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/RaidScene.unity"); return "scene open";' >/dev/null
unity command editor_play --no-banner --format json >/dev/null
echo "Entering Play mode..."
for _ in $(seq 1 60); do
    if [ "$(ev 'return UnityEditor.EditorApplication.isPlaying.ToString();' 2>/dev/null || true)" = "True" ]; then break; fi
done
for _ in $(seq 1 60); do
    if ev 'return UnityEngine.Object.FindFirstObjectByType<Plunderspell.Net.CoopSession>() != null ? "yes" : "no";' 2>/dev/null | grep -q yes; then break; fi
done

evf Tools/Unity/eval/start_solo_raid.cs
evf Tools/Unity/eval/set_out.cs >/dev/null
echo "Waiting for the raid to be running..."
raiding=no
for _ in $(seq 1 120); do
    phase="$(ev 'var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>(); return d == null ? "none" : d.Phase.ToString();' 2>/dev/null || true)"
    if [ "$phase" = "Raiding" ]; then raiding=yes; break; fi
done
[ "$raiding" = yes ] || { echo "The raid never reached Raiding (last phase: $phase)." >&2; ev 'UnityEditor.EditorApplication.ExitPlaymode(); return "left play";' >/dev/null; exit 1; }

sample() { # label, chatter true/false, optional C# to run once sampling has started
    ev "Plunderspell.Core.AudioInputSettings.GuardsHearChatter = $2; return \"set\";" >/dev/null
    # Give the recogniser time to start or stop before measuring.
    ev 'var l = new System.Collections.Generic.List<float>(); System.AppDomain.CurrentDomain.SetData("ftList", l); float[] last = { UnityEngine.Time.realtimeSinceStartup }; UnityEditor.EditorApplication.CallbackFunction cb = () => { float n = UnityEngine.Time.realtimeSinceStartup; l.Add((n - last[0]) * 1000f); last[0] = n; }; System.AppDomain.CurrentDomain.SetData("ftCb", cb); UnityEditor.EditorApplication.update += cb; return "sampling";' >/dev/null
    if [ -n "${3:-}" ]; then echo "  ($1 trigger: $(ev "$3"))"; fi
    for _ in $(seq 1 600); do
        n="$(ev 'return ((System.Collections.Generic.List<float>)System.AppDomain.CurrentDomain.GetData("ftList")).Count.ToString();' 2>/dev/null || echo 0)"
        [ "${n:-0}" -ge "$FRAMES" ] && break
    done
    echo "$1: $(ev 'UnityEditor.EditorApplication.update -= (UnityEditor.EditorApplication.CallbackFunction)System.AppDomain.CurrentDomain.GetData("ftCb"); var l = (System.Collections.Generic.List<float>)System.AppDomain.CurrentDomain.GetData("ftList"); var s = new System.Collections.Generic.List<float>(l); s.Sort(); float sum = 0; foreach (var v in s) sum += v; return s.Count + " frames, mean " + (sum / s.Count).ToString("0.0") + " ms, median " + s[s.Count / 2].ToString("0.0") + " ms, 95th " + s[(int)(s.Count * 0.95f)].ToString("0.0") + " ms, worst " + s[s.Count - 1].ToString("0.0") + " ms, frames over 33 ms: " + s.FindAll(x => x > 33f).Count;')"
}

# Worst case for guard speech: every living guard stood around the player and made to speak at once, so
# every line needs a fresh render at the same moment. Listening stays on, as the owner plays.
guards_do() { # C# that acts on each guard, given as the body of a lambda taking g and the spot
    echo 'var caster = Plunderspell.Spells.SpellCastingSystem.Local; var body = caster.transform.root; int n = 0; float a = 0f; foreach (var g in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.CastleGuard>(UnityEngine.FindObjectsSortMode.None)) { if (g == null || g.CurrentHealth <= 0f) continue; var agent = g.GetComponent<UnityEngine.AI.NavMeshAgent>(); var spot = body.position + new UnityEngine.Vector3(UnityEngine.Mathf.Cos(a) * 4f, -1f, UnityEngine.Mathf.Sin(a) * 4f); if (agent != null && agent.enabled) agent.Warp(spot); else g.transform.position = spot; '"$1"' a += 0.7f; n++; } return n + " guards";'
}

sample "Listening ON " true
sample "Listening OFF" false
sample "Guards all alerted" true "$(guards_do 'g.SetAlertState(Plunderspell.Guards.GuardAlertState.Investigating);')"
sample "Guards all chase  " true "$(guards_do 'g.SetAlertState(Plunderspell.Guards.GuardAlertState.Chasing);')"
sample "Guards all hurt   " true "$(guards_do 'g.TakeDamage(1f);')"

ev "Plunderspell.Core.AudioInputSettings.GuardsHearChatter = ${original,,}; return \"restored\";" >/dev/null
ev 'UnityEditor.EditorApplication.ExitPlaymode(); return "left play";' >/dev/null
echo "Restored the setting to $original and left Play mode."
