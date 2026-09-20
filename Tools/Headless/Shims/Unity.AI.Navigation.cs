// Minimal shim of the NavMeshSurface baking component (the "AI Navigation" package), so
// CastleNavMeshBaker compiles headlessly. BuildNavMesh() is a no-op: this harness proves the
// component wires up, not that a mesh gets baked. See Tools/Headless/README.md, "What it does and
// does not prove".
using UnityEngine;
using UnityEngine.AI;

namespace Unity.AI.Navigation
{
    public enum CollectObjects { All, Volume, Children }
    public enum NavMeshCollectGeometry { RenderMeshes, PhysicsColliders }

    public class NavMeshSurface : MonoBehaviour
    {
        public CollectObjects collectObjects;
        public NavMeshCollectGeometry useGeometry;

        public void BuildNavMesh()
        {
        }
    }
}
