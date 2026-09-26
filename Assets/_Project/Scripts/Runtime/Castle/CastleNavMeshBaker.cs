using Unity.AI.Navigation;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Rebuilds the scene's walkable surface once the castle exists.
    ///
    /// The castle is instantiated from the seed at runtime, so a NavMesh baked in the Editor would
    /// only ever cover the empty ground plane. See docs/4-systems/raid-scene-assembly.md ("Navigation")
    /// for why the bake is driven from the raid sequence rather than from Start().
    /// </summary>
    [RequireComponent(typeof(NavMeshSurface))]
    public class CastleNavMeshBaker : MonoBehaviour
    {
        private NavMeshSurface _surface;

        /// <summary>True once a surface has been built for the current castle.</summary>
        public bool HasBaked { get; private set; }

        private void Awake()
        {
            _surface = GetComponent<NavMeshSurface>();
        }

        /// <summary>
        /// Bakes the walkable surface over whatever is currently in the scene. Must run after the
        /// castle is instantiated and before the garrison spawns, or guards land off-mesh and never
        /// move.
        /// </summary>
        public void Rebuild()
        {
            if (_surface == null)
                _surface = GetComponent<NavMeshSurface>();

            if (_surface == null)
            {
                Debug.LogWarning("[Castle] No NavMeshSurface to bake; the garrison will not move.");
                return;
            }

            _surface.BuildNavMesh();
            HasBaked = true;
        }
    }
}
