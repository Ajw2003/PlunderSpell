using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Gives the raid scene's Ground a hole over the castle's centre cell, where the down-stair's well opens
    /// into the crypt (#254). The flat plane at height 0 ran straight through the well, so players stood on it
    /// instead of going down. Ground stays one object with one renderer, so tools that find it by name still work.
    /// </summary>
    public static class GroundStairWellCutter
    {
        private const string MeshPath = "Assets/_Project/Art/Models/GroundWithStairWell.asset";
        private const float OuterHalf = 58f;   // the old plane: 10 units at scale 11.6
        private const float WellHalf = 6f;     // one 12 m cell at the origin, the down-stair's cell

        [MenuItem("Tools/Plunderspell/Cut Stair Well In Ground")]
        public static void Cut()
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                Debug.LogError("[GroundWell] No Ground in the open scene.");
                return;
            }

            Mesh mesh = SaveMesh(BuildMesh());
            ground.transform.localScale = Vector3.one;
            ground.GetComponent<MeshFilter>().sharedMesh = mesh;
            ground.GetComponent<MeshCollider>().sharedMesh = mesh;
            EditorSceneManager.MarkSceneDirty(ground.scene);
            Debug.Log($"[GroundWell] Ground now has a {WellHalf * 2f} m well at the centre.");
        }

        /// <summary>A flat square of half-size OuterHalf with a square hole of half-size WellHalf, facing up.</summary>
        public static Mesh BuildMesh()
        {
            float o = OuterHalf, i = WellHalf;
            // Outer ring 0-3, inner ring 4-7, both counter-clockwise from (-x, -z) seen from above.
            var vertices = new[]
            {
                new Vector3(-o, 0f, -o), new Vector3(o, 0f, -o), new Vector3(o, 0f, o), new Vector3(-o, 0f, o),
                new Vector3(-i, 0f, -i), new Vector3(i, 0f, -i), new Vector3(i, 0f, i), new Vector3(-i, 0f, i),
            };
            var triangles = new int[24];
            for (int side = 0; side < 4; side++)
            {
                int next = (side + 1) % 4, t = side * 6;
                // Clockwise seen from above, so the face points up.
                triangles[t] = side; triangles[t + 1] = side + 4; triangles[t + 2] = next;
                triangles[t + 3] = next; triangles[t + 4] = side + 4; triangles[t + 5] = next + 4;
            }
            var uvs = new Vector2[vertices.Length];
            for (int v = 0; v < vertices.Length; v++)
                uvs[v] = new Vector2(vertices[v].x / (2f * o) + 0.5f, vertices[v].z / (2f * o) + 0.5f);

            var mesh = new Mesh { name = "GroundWithStairWell", vertices = vertices, triangles = triangles, uv = uvs };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh SaveMesh(Mesh mesh)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, MeshPath);
                return mesh;
            }
            EditorUtility.CopySerialized(mesh, existing);
            AssetDatabase.SaveAssets();
            return existing;
        }
    }
}
