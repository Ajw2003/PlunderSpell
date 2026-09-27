using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Four invisible walls on the curtain wall's outer face. The castle is the whole world for
    /// now (docs/plans/night-atmosphere.md, section 6): the gatehouse is sealed and nothing exists
    /// past the battlements, so this stops a player walking out of the gate arch or being thrown
    /// over the wall by a spell.
    /// </summary>
    public class CastleBoundary : MonoBehaviour
    {
        /// <summary>How high the walls reach. Well above anything a spell can launch a body to.</summary>
        public const float Height = 40f;

        private const float k_Thickness = 2f;
        private const float k_BelowGround = 1f;
        private const string k_WallName = "BoundaryWall";

        /// <summary>Distance from the castle's centre to the curtain wall's outer face.</summary>
        public static float OuterEdge(int curtainWallRadius, float cellSize) =>
            (curtainWallRadius + 0.5f) * cellSize;

        /// <summary>Gets the boundary on <paramref name="host"/>, adding one if it has none.</summary>
        public static CastleBoundary EnsureOn(GameObject host) =>
            host.TryGetComponent(out CastleBoundary boundary) ? boundary : host.AddComponent<CastleBoundary>();

        /// <summary>Replaces the walls to fit a castle of this size.</summary>
        public void Rebuild(int curtainWallRadius, float cellSize)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == k_WallName)
                    DestroyImmediate(child.gameObject);
            }

            float edge = OuterEdge(curtainWallRadius, cellSize);
            float span = (edge + k_Thickness) * 2f;
            float centreY = (Height - k_BelowGround) * 0.5f;
            float offset = edge + k_Thickness * 0.5f;

            AddWall(new Vector3(offset, centreY, 0f), new Vector3(k_Thickness, Height + k_BelowGround, span));
            AddWall(new Vector3(-offset, centreY, 0f), new Vector3(k_Thickness, Height + k_BelowGround, span));
            AddWall(new Vector3(0f, centreY, offset), new Vector3(span, Height + k_BelowGround, k_Thickness));
            AddWall(new Vector3(0f, centreY, -offset), new Vector3(span, Height + k_BelowGround, k_Thickness));
        }

        private void AddWall(Vector3 localCentre, Vector3 size)
        {
            var wall = new GameObject(k_WallName);
            wall.transform.SetParent(transform, false);
            wall.transform.localPosition = localCentre;
            wall.AddComponent<BoxCollider>().size = size;
        }
    }
}
