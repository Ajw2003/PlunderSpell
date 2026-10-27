using UnityEngine;

namespace Plunderspell.Playtest
{
    /// <summary>
    /// A pillar of gold light standing over the Ophan Gym's race artifact, so it can be found from anywhere in the castle
    /// (#453). Added to the piece on every machine by <see cref="OphanGym"/>; goes when the piece does.
    /// </summary>
    public sealed class OphanGymBeacon : MonoBehaviour
    {
        private const float PillarMetres = 80f;
        private const float PillarWidth = 0.6f;
        private static readonly Color Gold = new Color(1f, 0.8f, 0.3f, 0.8f);

        private LineRenderer _pillar;

        private void Awake()
        {
            var go = new GameObject("OphanGymPillar");
            go.transform.SetParent(transform, false);
            _pillar = go.AddComponent<LineRenderer>();
            _pillar.positionCount = 2;
            _pillar.useWorldSpace = true;
            _pillar.widthMultiplier = PillarWidth;
            _pillar.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _pillar.receiveShadows = false;
            _pillar.sharedMaterial = Resources.Load<Material>("GrabBeam");
            _pillar.startColor = Gold;
            _pillar.endColor = new Color(Gold.r, Gold.g, Gold.b, 0f);
        }

        // per-frame: the piece is carried, thrown and tumbles, and the pillar stands upright over wherever it is
        private void LateUpdate()
        {
            Vector3 foot = transform.position;
            _pillar.SetPosition(0, foot);
            _pillar.SetPosition(1, foot + Vector3.up * PillarMetres);
        }
    }
}
