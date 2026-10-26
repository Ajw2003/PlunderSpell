using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The warden lets go of its lantern at the turn's <c>PropDetach</c> event (#417). On every machine the
    /// lantern on the body disappears (the model is one skinned mesh, so its bone is shrunk to nothing). On
    /// the server one loose lantern is spawned in its place: a networked physics prefab, so it comes to rest
    /// in the same spot on every machine, and it is left where it fell, dark.
    /// </summary>
    public sealed class WardenLanternDrop
    {
        private const float TossSpeed = 1.2f;

        private readonly Transform _ring;
        private readonly GameObject _looseLantern;
        private readonly Guard _guard;
        private bool _dropped;

        public WardenLanternDrop(Guard guard, Transform ring, GameObject looseLantern)
        {
            _guard = guard;
            _ring = ring;
            _looseLantern = looseLantern;
        }

        public void Drop()
        {
            if (_dropped || _ring == null)
                return;

            _dropped = true;
            if (_guard.IsAuthority && _looseLantern != null)
                SpawnLoose();
            _ring.localScale = Vector3.zero;
        }

        private void SpawnLoose()
        {
            GameObject lantern = Object.Instantiate(_looseLantern, _ring.position, _ring.rotation);
            if (lantern.TryGetComponent(out Rigidbody body))
                body.linearVelocity = _guard.transform.forward * TossSpeed;
        }
    }
}
