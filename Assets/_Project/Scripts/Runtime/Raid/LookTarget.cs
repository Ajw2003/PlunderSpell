using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>What the player is looking at, for the Lair's look-and-press-E objects (ledger, Market door).</summary>
    public static class LookTarget
    {
        /// <summary>Whether <paramref name="eye"/>'s centre ray hits <paramref name="target"/> or a child of it within reach.</summary>
        public static bool IsLookedAt(Camera eye, Transform target, float reach) =>
            eye != null
            && Physics.Raycast(eye.transform.position, eye.transform.forward, out RaycastHit hit, reach,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.transform.IsChildOf(target);
    }
}
