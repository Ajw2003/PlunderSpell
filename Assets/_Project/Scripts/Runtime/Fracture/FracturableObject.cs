// Ported from ufo-3-rag-dahl FractureSystem (aj); see docs/plans/ophan-redo-plan.html.
using UnityEngine;
using System.Collections.Generic;

namespace Plunderspell.Fracture
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FracturableObject : MonoBehaviour
    {
        [Header("Fracture Settings")]
        [Tooltip("Random seed for fracture pattern.")]
        public int seed = 12345;
        
        [Tooltip("Force applied to pieces when they fracture.")]
        public float explosionForce = 10f;
        
        [Tooltip("Maximum distance to weld vertices together. Lower prevents false bridges, higher catches loose leaves.")]
        public float mergeDistance = 0.05f;
        
        [Tooltip("Gravity applied to pieces.")]
        public Vector3 gravity = new Vector3(0, -9.81f, 0);
        
        [Tooltip("How long before the pieces disappear.")]
        public float despawnTime = 5f;

        [Header("Resources")]
        [Tooltip("Falls back to Resources/Fracture/FractureCompute if null.")]
        public ComputeShader computeShader;

        [Tooltip("Material using the FractureRender shader. Falls back to Resources/Fracture/Fracture if null.")]
        public Material renderMaterial;

        [Tooltip("World Y of the ground plane the falling pieces bounce on.")]
        public float groundY = 0.05f;

        [Tooltip("Destroy this GameObject when almost nothing is left. Turn off for castle parts.")]
        public bool destroyWhenEmpty = true;

        [Header("Optimization")]
        [Tooltip("Initialize fracture data on Start instead of first hit. Prevents lag spikes when first hit.")]
        public bool preInitialize = false;
        
        [Tooltip("If the remaining mesh falls below this percentage (0.0 to 1.0), the entire object is despawned.")]
        [Range(0f, 1f)]
        public float destroyThreshold = 0.005f;

        [Tooltip("Percentage of the mesh height from the bottom that counts as anchored to the ground.")]
        [Range(0f, 1f)]
        public float supportHeightRatio = 0.10f;

        [Tooltip("Optional pre-baked graph data to avoid processing at runtime.")]
        public FractureGraphData bakedGraphData;

        private FractureEffect[] activeEffects;
        private MeshCollider meshCollider;
        private Mesh collisionMesh;
        private int[] collisionTriangles;
        private Vector3[] collisionVertices;
        private int initialTriangleCount;

        private PieceMapping[] pieceMappings;
        private int[][] triangleNeighbors;
        private bool[] isSupportTriangle;
        private bool[] triangleIsBroken;

        private int[] initialTriangles;

        // Cached collections for UpdateCollisionMesh
        private Queue<int> bfsQueue;
        private bool[] bfsVisited;
        private Dictionary<int, List<int>> droppedPiecesBySubmesh;
        private int[] newTrianglesBuffer;

        private void Start()
        {
            if (TryGetComponent<MeshCollider>(out meshCollider) && meshCollider.sharedMesh != null)
            {
                collisionMesh = Instantiate(meshCollider.sharedMesh);
                meshCollider.sharedMesh = collisionMesh;
                initialTriangles = collisionMesh.triangles;
                collisionTriangles = initialTriangles;
                collisionVertices = collisionMesh.vertices;
                initialTriangleCount = initialTriangles.Length / 3;

                bfsQueue = new Queue<int>(initialTriangleCount);
                bfsVisited = new bool[initialTriangleCount];
                droppedPiecesBySubmesh = new Dictionary<int, List<int>>();
                newTrianglesBuffer = new int[initialTriangles.Length];

                if (bakedGraphData != null && bakedGraphData.pieceMappings.Length == initialTriangleCount)
                {
                    pieceMappings = bakedGraphData.pieceMappings;
                    isSupportTriangle = bakedGraphData.isSupportTriangle;
                    triangleIsBroken = new bool[initialTriangleCount];
                    
                    triangleNeighbors = new int[initialTriangleCount][];
                    for (int i = 0; i < initialTriangleCount; i++)
                    {
                        triangleNeighbors[i] = bakedGraphData.triangleNeighbors[i].array;
                    }
                }
                else
                {
                    pieceMappings = new PieceMapping[initialTriangleCount];
                    int globalTri = 0;
                    for (int s = 0; s < collisionMesh.subMeshCount; s++)
                    {
                        int submeshTriCount = collisionMesh.GetTriangles(s).Length / 3;
                        for (int p = 0; p < submeshTriCount; p++)
                        {
                            pieceMappings[globalTri++] = new PieceMapping { submeshIndex = s, pieceIndex = p };
                        }
                    }
                    
                    BuildAdjacencyGraph();
                }
            }

            if (preInitialize && activeEffects == null)
            {
                activeEffects = FractureEngine.Fracture(this);
            }
        }

        private void BuildAdjacencyGraph()
        {
            int triCount = initialTriangleCount;
            triangleNeighbors = new int[triCount][];
            isSupportTriangle = new bool[triCount];
            triangleIsBroken = new bool[triCount];

            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < triCount; i++)
            {
                int i0 = initialTriangles[i * 3];
                int i1 = initialTriangles[i * 3 + 1];
                int i2 = initialTriangles[i * 3 + 2];
                Vector3 v0 = collisionVertices[i0];
                Vector3 v1 = collisionVertices[i1];
                Vector3 v2 = collisionVertices[i2];
                
                Vector3 centroid = (v0 + v1 + v2) / 3f;
                minY = Mathf.Min(minY, centroid.y);
                maxY = Mathf.Max(maxY, centroid.y);
            }

            // 2. Identify support triangles based on local height
            float supportThreshold = minY + (maxY - minY) * supportHeightRatio;
            for (int i = 0; i < triCount; i++)
            {
                int i0 = initialTriangles[i * 3];
                int i1 = initialTriangles[i * 3 + 1];
                int i2 = initialTriangles[i * 3 + 2];
                Vector3 centroid = (collisionVertices[i0] + collisionVertices[i1] + collisionVertices[i2]) / 3f;
                if (centroid.y <= supportThreshold)
                {
                    isSupportTriangle[i] = true;
                }
            }

            // 3. Build adjacency lists using vertex proximity
            Dictionary<Vector3Int, List<int>> spatialHash = new Dictionary<Vector3Int, List<int>>();
            int[] mappedVertices = new int[collisionVertices.Length];
            
            for (int i = 0; i < collisionVertices.Length; i++)
            {
                Vector3 v = collisionVertices[i];
                Vector3Int cell = new Vector3Int(
                    Mathf.RoundToInt(v.x / mergeDistance),
                    Mathf.RoundToInt(v.y / mergeDistance),
                    Mathf.RoundToInt(v.z / mergeDistance)
                );
                
                int mappedIndex = i;
                bool found = false;
                
                for (int x = -1; x <= 1 && !found; x++)
                for (int y = -1; y <= 1 && !found; y++)
                for (int z = -1; z <= 1 && !found; z++)
                {
                    Vector3Int neighborCell = cell + new Vector3Int(x, y, z);
                    if (spatialHash.TryGetValue(neighborCell, out List<int> list))
                    {
                        foreach(int idx in list)
                        {
                            if (Vector3.Distance(v, collisionVertices[idx]) <= mergeDistance)
                            {
                                mappedIndex = idx;
                                found = true;
                                break;
                            }
                        }
                    }
                }

                if (!found)
                {
                    if (!spatialHash.TryGetValue(cell, out List<int> list))
                    {
                        list = new List<int>();
                        spatialHash[cell] = list;
                    }
                    list.Add(i);
                }
                mappedVertices[i] = mappedIndex;
            }

            Dictionary<int, List<int>> vertexToTriangles = new Dictionary<int, List<int>>();
            for (int i = 0; i < triCount; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int vIdx = mappedVertices[initialTriangles[i * 3 + j]];
                    if (!vertexToTriangles.TryGetValue(vIdx, out List<int> list))
                    {
                        list = new List<int>();
                        vertexToTriangles[vIdx] = list;
                    }
                    list.Add(i);
                }
            }

            for (int i = 0; i < triCount; i++)
            {
                Dictionary<int, int> sharedVertexCount = new Dictionary<int, int>();
                
                for (int j = 0; j < 3; j++)
                {
                    int vIdx = mappedVertices[initialTriangles[i * 3 + j]];
                    foreach (int tIdx in vertexToTriangles[vIdx])
                    {
                        if (tIdx != i)
                        {
                            if (sharedVertexCount.TryGetValue(tIdx, out int count))
                            {
                                sharedVertexCount[tIdx] = count + 1;
                            }
                            else
                            {
                                sharedVertexCount[tIdx] = 1;
                            }
                        }
                    }
                }
                
                List<int> validNeighbors = new List<int>();
                foreach (var kvp in sharedVertexCount)
                {
                    if (kvp.Value >= 2)
                    {
                        validNeighbors.Add(kvp.Key);
                    }
                }
                
                triangleNeighbors[i] = validNeighbors.ToArray();
            }
        }

        public ComputeShader ResolveComputeShader()
        {
            if (computeShader == null) computeShader = Resources.Load<ComputeShader>("Fracture/FractureCompute");
            if (computeShader == null) Debug.LogError("ComputeShader is missing on " + gameObject.name + " and Resources/Fracture/FractureCompute was not found.");
            return computeShader;
        }

        public Material ResolveRenderMaterial()
        {
            if (renderMaterial == null) renderMaterial = Resources.Load<Material>("Fracture/Fracture");
            if (renderMaterial == null) Debug.LogError("renderMaterial is missing on " + gameObject.name + " and Resources/Fracture/Fracture was not found.");
            return renderMaterial;
        }

        public void Hit(Collider impactCollider, Vector3 point, float force)
        {
            if (activeEffects == null)
            {
                activeEffects = FractureEngine.Fracture(this);
            }
            
            float radius = 1f;
            if (impactCollider != null)
            {
                radius = impactCollider.bounds.extents.magnitude;
            }
            
            if (activeEffects != null)
            {
                foreach (var effect in activeEffects)
                {
                    if (effect != null)
                        effect.ApplyImpact(radius, point, force);
                }
            }
            
            if (MarkBroken(point, radius)) RebuildCollisionMesh();
        }

        // Applies the impact at points along from->to that lie within this renderer's bounds expanded by radius,
        // spaced radius*0.5 apart. Triangles are marked for every point, then one BFS + one collider rebuild.
        public void HitAlongLine(Vector3 from, Vector3 to, float radius, float force)
        {
            if (!TryGetComponent<MeshRenderer>(out var mr)) return;
            Bounds b = mr.bounds; // stays valid after the renderer is disabled by fracturing
            b.Expand(radius * 2f);
            float len = Vector3.Distance(from, to);
            float step = Mathf.Max(radius * 0.5f, 0.01f);

            bool changed = false;
            for (float d = 0f; d <= len + 1e-4f; d += step)
            {
                Vector3 p = len > 0f ? Vector3.Lerp(from, to, d / len) : from;
                if (!b.Contains(p)) continue;
                if (activeEffects == null) activeEffects = FractureEngine.Fracture(this);
                if (activeEffects != null)
                {
                    foreach (var effect in activeEffects)
                    {
                        if (effect != null) effect.ApplyImpact(radius, p, force);
                    }
                }
                changed |= MarkBroken(p, radius);
                if (len <= 0f) break;
            }
            if (changed) RebuildCollisionMesh();
        }

        private bool MarkBroken(Vector3 point, float radius)
        {
            if (meshCollider == null || collisionMesh == null || initialTriangles == null) return false;

            bool changed = false;
            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            
            for (int i = 0; i < initialTriangleCount; i++)
            {
                if (triangleIsBroken[i]) continue;
                
                int i0 = initialTriangles[i * 3];
                int i1 = initialTriangles[i * 3 + 1];
                int i2 = initialTriangles[i * 3 + 2];
                
                Vector3 v0 = collisionVertices[i0];
                Vector3 v1 = collisionVertices[i1];
                Vector3 v2 = collisionVertices[i2];
                
                Vector3 centroid = (v0 + v1 + v2) / 3f;
                Vector3 worldCentroid = localToWorld.MultiplyPoint3x4(centroid);
                
                float r0 = Vector3.Distance(centroid, v0);
                float r1 = Vector3.Distance(centroid, v1);
                float r2 = Vector3.Distance(centroid, v2);
                float maxR = Mathf.Max(r0, Mathf.Max(r1, r2)) * localToWorld.lossyScale.x;
                
                float dist = Vector3.Distance(worldCentroid, point);
                
                if (dist <= radius + maxR + 0.1f)
                {
                    triangleIsBroken[i] = true;
                    changed = true;
                }
            }
            
            return changed;
        }

        private void RebuildCollisionMesh()
        {
            if (meshCollider == null || collisionMesh == null || initialTriangles == null) return;

            {
                // Run BFS to find disconnected islands
                System.Array.Clear(bfsVisited, 0, initialTriangleCount);
                bfsQueue.Clear();

                for (int i = 0; i < initialTriangleCount; i++)
                {
                    if (!triangleIsBroken[i] && isSupportTriangle[i])
                    {
                        bfsVisited[i] = true;
                        bfsQueue.Enqueue(i);
                    }
                }

                while (bfsQueue.Count > 0)
                {
                    int curr = bfsQueue.Dequeue();
                    foreach (int neighbor in triangleNeighbors[curr])
                    {
                        if (!triangleIsBroken[neighbor] && !bfsVisited[neighbor])
                        {
                            bfsVisited[neighbor] = true;
                            bfsQueue.Enqueue(neighbor);
                        }
                    }
                }

                // Collect dropped pieces grouped by submesh
                foreach (var list in droppedPiecesBySubmesh.Values)
                {
                    list.Clear();
                }
                int newTriangleCount = 0;
                
                for (int i = 0; i < initialTriangleCount; i++)
                {
                    if (!triangleIsBroken[i])
                    {
                        if (!bfsVisited[i])
                        {
                            // Disconnected! Drop it
                            triangleIsBroken[i] = true;
                            PieceMapping map = pieceMappings[i];
                            if (!droppedPiecesBySubmesh.TryGetValue(map.submeshIndex, out List<int> list))
                            {
                                list = new List<int>();
                                droppedPiecesBySubmesh[map.submeshIndex] = list;
                            }
                            list.Add(map.pieceIndex);
                        }
                        else
                        {
                            // Keep it in the collision mesh
                            newTrianglesBuffer[newTriangleCount++] = initialTriangles[i * 3];
                            newTrianglesBuffer[newTriangleCount++] = initialTriangles[i * 3 + 1];
                            newTrianglesBuffer[newTriangleCount++] = initialTriangles[i * 3 + 2];
                        }
                    }
                }
                
                // Notify FractureEffects of the disconnected pieces
                if (droppedPiecesBySubmesh.Count > 0 && activeEffects != null)
                {
                    foreach (var kvp in droppedPiecesBySubmesh)
                    {
                        if (kvp.Value.Count > 0 && kvp.Key >= 0 && kvp.Key < activeEffects.Length && activeEffects[kvp.Key] != null)
                        {
                            activeEffects[kvp.Key].DropPieces(kvp.Value.ToArray());
                        }
                    }
                }

                // Calculate remaining percentage
                int remainingTriangles = newTriangleCount / 3;
                float remainingPercentage = (float)remainingTriangles / initialTriangleCount;
                
                if (remainingPercentage <= destroyThreshold || newTriangleCount == 0)
                {
                    // Clean up all fracture effects
                    if (activeEffects != null)
                    {
                        foreach (var effect in activeEffects)
                        {
                            if (effect != null && effect.gameObject != null)
                            {
                                // Let the GPU finish rendering the falling pieces before destroying
                                effect.SetSelfDestruct();
                            }
                        }
                    }
                    // Clean up dynamically created mesh to prevent memory leaks
                    if (collisionMesh != null)
                    {
                        Destroy(collisionMesh);
                    }
                    // Clean up the main object
                    if (destroyWhenEmpty) Destroy(gameObject);
                    else meshCollider.sharedMesh = null; // collider must not keep the destroyed mesh
                    return; // Prevent PhysX errors by exiting before setting an empty or broken mesh
                }

                int[] finalTriangles = new int[newTriangleCount];
                System.Array.Copy(newTrianglesBuffer, finalTriangles, newTriangleCount);
                collisionTriangles = finalTriangles;
                
                collisionMesh.triangles = collisionTriangles;
                
                // Force PhysX to rebuild the collision mesh by clearing it first
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = collisionMesh;
            }
        }

        [ContextMenu("Test Fracture")]
        public void TriggerFracture()
        {
            TryGetComponent<Collider>(out Collider col);
            Hit(col, transform.position, explosionForce);
        }

#if UNITY_EDITOR
        [ContextMenu("Bake Fracture Graph")]
        public void BakeFractureGraph()
        {
            if (!TryGetComponent<MeshCollider>(out var mCol) || mCol.sharedMesh == null)
            {
                Debug.LogError("No MeshCollider or sharedMesh found to bake!");
                return;
            }
            
            // Re-simulate setup locally
            collisionMesh = mCol.sharedMesh;
            initialTriangles = collisionMesh.triangles;
            collisionVertices = collisionMesh.vertices;
            initialTriangleCount = initialTriangles.Length / 3;

            pieceMappings = new PieceMapping[initialTriangleCount];
            int globalTri = 0;
            for (int s = 0; s < collisionMesh.subMeshCount; s++)
            {
                int submeshTriCount = collisionMesh.GetTriangles(s).Length / 3;
                for (int p = 0; p < submeshTriCount; p++)
                {
                    pieceMappings[globalTri++] = new PieceMapping { submeshIndex = s, pieceIndex = p };
                }
            }
            
            BuildAdjacencyGraph();

            // Create asset
            string safeName = string.Join("_", gameObject.name.Split(System.IO.Path.GetInvalidFileNameChars()));
            string safeMesh = string.Join("_", collisionMesh.name.Split(System.IO.Path.GetInvalidFileNameChars()));
            const string bakeFolder = "Assets/_Project/Art/Models/Castle/Parts/Fracture";
            string path = bakeFolder + "/BakedData_" + safeName + "_" + safeMesh + ".asset";
            
            if (!UnityEditor.AssetDatabase.IsValidFolder(bakeFolder))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/_Project/Art/Models/Castle/Parts", "Fracture");
            }
            
            FractureGraphData data = ScriptableObject.CreateInstance<FractureGraphData>();
            data.pieceMappings = pieceMappings;
            data.isSupportTriangle = isSupportTriangle;
            
            data.triangleNeighbors = new IntArray[triangleNeighbors.Length];
            for (int i = 0; i < triangleNeighbors.Length; i++)
            {
                data.triangleNeighbors[i] = new IntArray { array = triangleNeighbors[i] };
            }

            UnityEditor.AssetDatabase.CreateAsset(data, path);
            UnityEditor.AssetDatabase.SaveAssets();

            this.bakedGraphData = data;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("Successfully baked and assigned FractureGraphData at " + path);
        }
#endif
    }
}
