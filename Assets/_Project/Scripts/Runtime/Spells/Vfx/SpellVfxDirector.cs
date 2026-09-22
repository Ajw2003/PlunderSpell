using UnityEngine;

namespace RogueAi.Spells.Vfx
{
    /// <summary>
    /// Turns a resolved cast into something you can see. Listens to
    /// <see cref="SpellCastingSystem.CastResolved"/>, which fires on every peer, so a teammate's
    /// spell is visible to everyone rather than only to whoever spoke.
    ///
    /// See docs/systems/spells.md, "Seeing a cast", including why a spell's bolt carries no damage.
    /// </summary>
    public class SpellVfxDirector : MonoBehaviour
    {
        [Tooltip("The bolt fired for projectile spells. Falls back to a burst when empty.")]
        [SerializeField] private GameObject m_boltPrefab;

        [Tooltip("Speed a spell bolt travels at, in metres per second.")]
        [SerializeField] private float m_boltSpeed = 24f;

        [Tooltip("How long a burst takes to expand and fade.")]
        [SerializeField] private float m_burstDuration = 0.45f;

        /// <summary>Clearance past the burst's own radius, so its near edge doesn't graze the camera.</summary>
        private const float k_burstClearance = 0.15f;

        private void OnEnable() => SpellCastingSystem.CastResolved += OnCastResolved;

        private void OnDisable() => SpellCastingSystem.CastResolved -= OnCastResolved;

        private void OnCastResolved(SpellCastingSystem.CastReport report)
        {
            SpellLook look = SpellLookbook.For(report.Spell);

            if (look.Style == SpellVisualStyle.Bolt && m_boltPrefab != null)
            {
                FireBolt(report, look);
                return;
            }

            SpellBurst.Spawn(BurstPosition(report, look.Radius), look.Colour, look.Radius, m_burstDuration);
        }

        /// <summary>
        /// Pushes the burst's centre out from the cast origin by its own radius, so the near edge
        /// lands at the origin rather than the caster's own camera ending up inside the sphere. See
        /// docs/Decisions.md, "A spell burst is centred on where it lands, not where it starts".
        /// </summary>
        private static Vector3 BurstPosition(SpellCastingSystem.CastReport report, float radius) =>
            report.Origin + report.Direction.normalized * (radius + k_burstClearance);

        /// <summary>
        /// Fires a cosmetic bolt. It carries no damage on purpose: the effect already resolved on the
        /// server before this ran, and a bolt that damaged what it hit would apply the spell twice.
        /// </summary>
        private void FireBolt(SpellCastingSystem.CastReport report, SpellLook look)
        {
            GameObject bolt = Instantiate(m_boltPrefab, report.Origin,
                Quaternion.LookRotation(report.Direction));

            if (bolt.TryGetComponent(out NetworkedProjectile projectile))
            {
                projectile.Damage = 0;
            }

            if (bolt.TryGetComponent(out ProjectileTint tint))
            {
                tint.Apply(look.Colour);
            }

            if (bolt.TryGetComponent(out Rigidbody body))
            {
                body.linearVelocity = report.Direction * m_boltSpeed;
            }

            // A muzzle flash at the hands, so a bolt that flies off down a corridor still reads as
            // having come from the caster.
            SpellBurst.Spawn(BurstPosition(report, look.Radius), look.Colour, look.Radius, m_burstDuration * 0.5f);
        }
    }
}
