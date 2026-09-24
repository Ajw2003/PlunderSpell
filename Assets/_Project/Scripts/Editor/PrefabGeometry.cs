using UnityEngine;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Measures where a model's geometry actually is, rather than where Unity's bounding boxes say.
    /// <see cref="Renderer.bounds"/> is padded on a rigged mesh and must not decide where a foot is.
    /// Issue <see href="https://github.com/Ajw2003/PlunderSpell/issues/94"/>.
    /// </summary>
    // doc-ref 892b docs/systems/scale.md
    public static class PrefabGeometry
    {
        /// <summary>
        /// Lowest and highest world-space Y of everything <paramref name="instance"/> draws, skinned
        /// meshes measured from their baked vertices. The instance must be in a scene: the vertices
        /// are read through live transforms. Returns false when nothing drawable was found.
        /// </summary>
        public static bool TryMeasureVerticalExtent(GameObject instance, out float lowest, out float highest)
        {
            lowest = float.MaxValue;
            highest = float.MinValue;

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                // Particles come and go; they are not part of where a body stands.
                if (renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                {
                    IncludeSkinned(skinned, ref lowest, ref highest);
                }
                else
                {
                    Bounds bounds = renderer.bounds;
                    lowest = Mathf.Min(lowest, bounds.min.y);
                    highest = Mathf.Max(highest, bounds.max.y);
                }
            }

            return lowest != float.MaxValue;
        }

        /// <summary>Lowest world-space Y the instance draws, or 0 if it draws nothing.</summary>
        public static float LowestY(GameObject instance)
        {
            return TryMeasureVerticalExtent(instance, out float lowest, out _) ? lowest : 0f;
        }

        // useScale must be true and the full localToWorldMatrix must place the result: the other
        // pairings land ~100x too large or too small (the FBX import scale), which reads as feet on
        // the origin for every model. Only this one reproduces a model's real height.
        private static void IncludeSkinned(SkinnedMeshRenderer skinned, ref float lowest, ref float highest)
        {
            var baked = new Mesh();
            skinned.BakeMesh(baked, true);
            Matrix4x4 toWorld = skinned.transform.localToWorldMatrix;

            foreach (Vector3 vertex in baked.vertices)
            {
                float y = toWorld.MultiplyPoint3x4(vertex).y;
                lowest = Mathf.Min(lowest, y);
                highest = Mathf.Max(highest, y);
            }

            Object.DestroyImmediate(baked);
        }
    }
}
