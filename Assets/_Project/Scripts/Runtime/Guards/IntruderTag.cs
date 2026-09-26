using UnityEngine;

namespace RogueAi.Guards
{
    /// <summary>
    /// Marks a player as something guards look for, for as long as it is enabled.
    ///
    /// Registration has to happen at runtime. <see cref="CastleGuard.Intruders"/> is a static list,
    /// so anything a scene-building tool registers at edit time is gone by the time the scene is
    /// played — and a guard would then never see anybody.
    /// </summary>
    [DisallowMultipleComponent]
    public class IntruderTag : MonoBehaviour
    {
        private void OnEnable() => CastleGuard.RegisterIntruder(transform);

        private void OnDisable() => CastleGuard.UnregisterIntruder(transform);
    }
}
