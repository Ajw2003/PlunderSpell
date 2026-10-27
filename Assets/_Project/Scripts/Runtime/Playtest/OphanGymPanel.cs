using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Playtest
{
    /// <summary>
    /// The Ophan Gym's corner panel (#453, #454): the countdown to the Ophan's arrival, what it is doing once it is here,
    /// and the test keys. IMGUI, like the combat bench's panel: a test harness, never shipped in the real game.
    /// </summary>
    [RequireComponent(typeof(OphanGym))]
    public sealed class OphanGymPanel : MonoBehaviour
    {
        private const float PanelWidth = 280f;
        private const float PanelMargin = 12f;

        private OphanGym _gym;
        private GUIStyle _rich;

        private void Awake() => _gym = GetComponent<OphanGym>();

        // per-frame: IMGUI redraws the countdown, which changes every frame
        private void OnGUI()
        {
            if (_gym.RaidStartedAt < 0f)
                return;
            _rich ??= new GUIStyle(GUI.skin.label) { richText = true };

            GUILayout.BeginArea(new Rect(Screen.width - PanelWidth - PanelMargin, PanelMargin, PanelWidth, 200f), GUI.skin.box);
            GUILayout.Label("<b>Ophan Gym</b>", _rich);
            GUILayout.Label(Headline(), _rich);
            foreach ((string key, string does) in OphanGymKeys.Legend)
                GUILayout.Label($"<b>{key}</b>  {does}{Status(key)}", _rich);
            GUILayout.EndArea();
        }

        private string Headline()
        {
            float left = OphanGym.ArriveAfterSeconds - (Time.time - _gym.RaidStartedAt);
            if (_gym.OphanState == OphanState.Absent && left > 0f)
                return $"The Ophan arrives in <b>{Mathf.CeilToInt(left)}</b> s. Reach the gold pillar first.";
            return $"The Ophan: <b>{_gym.OphanState}</b>";
        }

        private string Status(string key) => key switch
        {
            "F8" => _gym.IsLocalPlayerUntouchable ? "  (on)" : "",
            "F10" => $"  ({_gym.TimeScale:0.##}x)",
            _ => "",
        };
    }
}
