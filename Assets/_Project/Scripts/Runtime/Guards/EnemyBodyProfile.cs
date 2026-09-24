using UnityEngine;

namespace RogueAi.Guards
{
    /// <summary>
    /// The art bible's numbers for one enemy, written onto its prefab by <c>ArtBibleEnemyForge</c>
    /// so the runtime (and the animation work in docs/plans/artbible-enemy-animations.md) can read
    /// them without the JSON. Data only: nothing here acts.
    /// </summary>
    public class EnemyBodyProfile : MonoBehaviour
    {
        [Tooltip("Stable identifier: the model's name in the ArtForge manifest.")]
        [SerializeField] private string _enemyId;

        [Tooltip("patrol, ranged, heavy or special, from docs/art/data/<age>.json.")]
        [SerializeField] private string _role;

        [Tooltip("Standing height without props, in metres, from the art bible.")]
        [SerializeField] private float _bodyHeight = 1.8f;

        [Tooltip("Height including props (spear, lantern pole…), in metres, as ArtForge measured it.")]
        [SerializeField] private float _heightWithProps = 1.8f;

        [Tooltip("The lowest archway of any zone this enemy is posted to, in metres.")]
        [SerializeField] private float _archwayClearance = 2.16f;

        /// <summary>Stable identifier: the model's name in the ArtForge manifest.</summary>
        public string EnemyId => _enemyId;

        /// <summary>patrol, ranged, heavy or special.</summary>
        public string Role => _role;

        public float BodyHeight => _bodyHeight;
        public float HeightWithProps => _heightWithProps;
        public float ArchwayClearance => _archwayClearance;

        /// <summary>
        /// True when something this enemy carries is taller than an archway it must pass: the
        /// animation plan's "archway duck" clip plays for it. The NavMesh agent's height is already
        /// capped at <see cref="ArchwayClearance"/>, so the duck is visual, not pathing.
        /// </summary>
        public bool NeedsArchwayDuck => _heightWithProps > _archwayClearance;

        /// <summary>Writes the profile from code: for the forge and for tests.</summary>
        public void Configure(string enemyId, string role, float bodyHeight, float heightWithProps,
            float archwayClearance)
        {
            _enemyId = enemyId;
            _role = role;
            _bodyHeight = bodyHeight;
            _heightWithProps = heightWithProps;
            _archwayClearance = archwayClearance;
        }
    }
}
