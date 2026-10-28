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

        [Tooltip("Keep this object's own renderer drawing what is left, in its own shader, and let the effect draw only falling debris. Off: the effect redraws every piece, as the original did.")]
        public bool keepMeshRenderer = false;

        [Tooltip("Metres of stone strip added along every edge a break leaves, front to back (keepMeshRenderer only). 0 = no caps. Visual only.")]
        public float capDepthMetres = 0f;

        [Tooltip("Double-sided mesh the fracture data comes from, when the MeshCollider holds the plain one-sided mesh. The collider then only ever gets the front triangles: a double-sided collider catches a guard's step sweep on the back of every stair riser.")]
        public Mesh fractureMesh;

        private Mesh renderMesh;
        private int[] originalPerSubmesh; // triangles per submesh before the reversed twins
        private readonly List<Vector3> baseVerts = new List<Vector3>(), baseNormals = new List<Vector3>(), capVerts = new List<Vector3>(), capNormals = new List<Vector3>();
        private readonly List<Vector2> baseUvs = new List<Vector2>(), baseUv2 = new List<Vector2>(), capUvs = new List<Vector2>(), capUv2 = new List<Vector2>();
        private readonly List<Vector4> baseTangents = new List<Vector4>(), capTangents = new List<Vector4>();
        private readonly List<Color> baseColors = new List<Color>(), capColors = new List<Color>();

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
        private int frontTriangleCount;

        private PieceMapping[] pieceMappings;
        private int[][] triangleNeighbors;
        private bool[] isSupportTriangle;
        private bool[] triangleIsBroken;

        private int[] initialTriangles;

        // Clusters (#491): null when the graph has none, and then every triangle breaks on its own as before.
        private int[] triangleCluster;
        private byte[] triangleKind;
        private int[] triangleEdgeOther;
        private int[][] clusterTriangles;
        private int[][] clusterNeighbors;
        private bool[] clusterIsSupport;
        private Vector3[] clusterCentroid;
        private float[] clusterRadius;
        private bool[] clusterIsBroken;
        private bool[] clusterVisited;

        // Cached collections for UpdateCollisionMesh
        private Queue<int> bfsQueue;
        private bool[] bfsVisited;
        private Dictionary<int, List<int>> droppedPiecesBySubmesh;
        private int[] newTrianglesBuffer;

        private void Start()
        {
            if (TryGetComponent<MeshCollider>(out meshCollider) && (fractureMesh != null || meshCollider.sharedMesh != null))
            {
                collisionMesh = Instantiate(fractureMesh != null ? fractureMesh : meshCollider.sharedMesh);
                meshCollider.sharedMesh = collisionMesh;
                initialTriangles = collisionMesh.triangles;
                collisionTriangles = initialTriangles;
                collisionVertices = collisionMesh.vertices;
                initialTriangleCount = initialTriangles.Length / 3;

                if (fractureMesh != null || (capDepthMetres > 0f && keepMeshRenderer))
                {
                    originalPerSubmesh = new int[collisionMesh.subMeshCount];
                    for (int s = 0; s < originalPerSubmesh.Length; s++) originalPerSubmesh[s] = collisionMesh.GetTriangles(s).Length / 6;
                }

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
                    LoadClusters(bakedGraphData);
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
                // After the graph: telling front triangles apart needs the piece mappings, or the cluster kinds.
                if (fractureMesh != null) SetColliderToFrontTriangles();
            }

            if (preInitialize && activeEffects == null)
            {
                activeEffects = FractureEngine.Fracture(this);
            }
        }

        private void LoadClusters(FractureGraphData data)
        {
            if (data.triangleCluster == null || data.triangleCluster.Length != initialTriangleCount
                || data.clusterTriangles == null || data.clusterTriangles.Length == 0) return;
            int count = data.clusterTriangles.Length;
            triangleCluster = data.triangleCluster;
            triangleKind = data.triangleKind != null && data.triangleKind.Length == initialTriangleCount ? data.triangleKind : null;
            triangleEdgeOther = data.triangleEdgeOther != null && data.triangleEdgeOther.Length == initialTriangleCount ? data.triangleEdgeOther : null;
            clusterTriangles = new int[count][];
            clusterNeighbors = new int[count][];
            for (int c = 0; c < count; c++)
            {
                clusterTriangles[c] = data.clusterTriangles[c].array;
                clusterNeighbors[c] = data.clusterNeighbors[c].array;
            }
            clusterIsSupport = data.clusterIsSupport;
            clusterCentroid = data.clusterCentroid;
            clusterRadius = data.clusterRadius;
            clusterIsBroken = new bool[count];
            clusterVisited = new bool[count];
        }

        /// <summary>The GPU piece layout for FractureEngine when the graph has clusters (#492): each triangle's piece within
        /// its submesh, and that piece's cluster, whose centroid (mesh space) and radius (mesh units) the piece takes.
        /// False when there are no clusters; every triangle is then its own piece.</summary>
        public bool TryGetClusterPieces(out PieceMapping[] mappings, out int[] clusterOfTriangle, out Vector3[] centroids, out float[] radii)
        {
            mappings = pieceMappings; clusterOfTriangle = triangleCluster; centroids = clusterCentroid; radii = clusterRadius;
            return triangleCluster != null && pieceMappings != null;
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

            if (triangleCluster != null)
            {
                // Whole clusters, by the same test CSImpact runs on the cluster pieces, so collider and debris agree.
                float scale = localToWorld.lossyScale.x;
                for (int c = 0; c < clusterIsBroken.Length; c++)
                {
                    if (clusterIsBroken[c]) continue;
                    float dist = Vector3.Distance(localToWorld.MultiplyPoint3x4(clusterCentroid[c]), point);
                    if (dist > radius + clusterRadius[c] * scale + 0.1f) continue;
                    BreakCluster(c);
                    changed = true;
                }
                return changed;
            }

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

        private void BreakCluster(int c)
        {
            clusterIsBroken[c] = true;
            foreach (int t in clusterTriangles[c]) triangleIsBroken[t] = true;
        }

        // Clusters joined to an unbroken ground cluster through unbroken neighbours; their triangles go in bfsVisited.
        private void FindSupportedClusters()
        {
            System.Array.Clear(clusterVisited, 0, clusterVisited.Length);
            bfsQueue.Clear();
            for (int c = 0; c < clusterIsBroken.Length; c++)
            {
                if (clusterIsBroken[c] || !clusterIsSupport[c]) continue;
                clusterVisited[c] = true;
                bfsQueue.Enqueue(c);
            }
            while (bfsQueue.Count > 0)
            {
                foreach (int n in clusterNeighbors[bfsQueue.Dequeue()])
                {
                    if (clusterIsBroken[n] || clusterVisited[n]) continue;
                    clusterVisited[n] = true;
                    bfsQueue.Enqueue(n);
                }
            }
            for (int i = 0; i < initialTriangleCount; i++) bfsVisited[i] = clusterVisited[triangleCluster[i]];
            for (int c = 0; c < clusterIsBroken.Length; c++) if (!clusterVisited[c]) clusterIsBroken[c] = true;
        }

        /// <summary>For tests: whether cluster <paramref name="c"/> is broken. False when the graph has no clusters.</summary>
        public bool IsClusterBroken(int c) => clusterIsBroken != null && clusterIsBroken[c];

        private void RebuildCollisionMesh()
        {
            if (meshCollider == null || collisionMesh == null || initialTriangles == null) return;

            if (triangleCluster != null) FindSupportedClusters();
            else
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
            }

            {
                // Collect dropped pieces grouped by submesh
                foreach (var list in droppedPiecesBySubmesh.Values)
                {
                    list.Clear();
                }
                int newTriangleCount = 0;
                int keptTriangles = 0;
                
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
                        else if (fractureMesh == null || IsOriginalTriangle(i))
                        {
                            // Keep it in the collision mesh
                            keptTriangles++;
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
                float remainingPercentage = (float)keptTriangles / (fractureMesh != null ? frontTriangleCount : initialTriangleCount);
                
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
                if (keepMeshRenderer) UpdateRenderMesh();
                
                // Force PhysX to rebuild the collision mesh by clearing it first
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = collisionMesh;
            }
        }

        // The renderer keeps its materials, so the surviving triangles go back into their own submeshes (#472).
        private void UpdateRenderMesh()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter == null || pieceMappings == null) return;
            if (renderMesh == null)
            {
                // Seen once on a co-op client (2026-10-28, cause not found yet): the throw stopped every later part's break.
                if (filter.sharedMesh == null)
                {
                    Debug.LogError($"[Fracture] {transform.parent?.name}/{name} has no mesh to redraw after a break; its collider still breaks.", this);
                    return;
                }
                renderMesh = Instantiate(filter.sharedMesh);
                filter.sharedMesh = renderMesh;
                if (capDepthMetres > 0f)
                {
                    renderMesh.GetVertices(baseVerts); renderMesh.GetNormals(baseNormals); renderMesh.GetUVs(0, baseUvs);
                    renderMesh.GetUVs(1, baseUv2); renderMesh.GetTangents(baseTangents); renderMesh.GetColors(baseColors);
                }
            }
            var lists = new List<int>[renderMesh.subMeshCount];
            for (int s = 0; s < lists.Length; s++) lists[s] = new List<int>();
            for (int i = 0; i < initialTriangleCount; i++)
            {
                if (triangleIsBroken[i] || !EdgeShows(i)) continue;
                int s = pieceMappings[i].submeshIndex;
                if (s < 0 || s >= lists.Length) continue;
                lists[s].Add(initialTriangles[i * 3]);
                lists[s].Add(initialTriangles[i * 3 + 1]);
                lists[s].Add(initialTriangles[i * 3 + 2]);
            }
            if (capDepthMetres > 0f && originalPerSubmesh != null) AddCaps(lists);
            // Triangles first, so the vertex count can shrink when a cap goes away.
            for (int s = 0; s < lists.Length; s++) renderMesh.SetTriangles(System.Array.Empty<int>(), s);
            if (capDepthMetres > 0f) ApplyCapVertices();
            for (int s = 0; s < lists.Length; s++) renderMesh.SetTriangles(lists[s], s);
        }

        // A stone edge shows only once the cluster across its border has broken (aj, 2026-10-28); every other triangle shows.
        private bool EdgeShows(int tri)
        {
            if (triangleKind == null || triangleKind[tri] != 2) return true;
            int other = triangleEdgeOther != null ? triangleEdgeOther[tri] : -1;
            return other >= 0 && clusterIsBroken != null && clusterIsBroken[other];
        }

        // Cap quads (#477): every edge of a surviving original triangle whose welded neighbour across it is broken
        // gets a strip capDepthMetres deep, along -face normal, so the hole is not an empty gap between the two surfaces.
        private void AddCaps(List<int>[] lists)
        {
            capVerts.Clear(); capNormals.Clear(); capUvs.Clear(); capUv2.Clear(); capTangents.Clear(); capColors.Clear();
            // Both surfaces of a wall cap the same hole: each goes half way, so the strips meet instead of overlapping.
            float depth = capDepthMetres * 0.5f / transform.lossyScale.x;
            float weld2 = mergeDistance * mergeDistance;
            int baseCount = baseVerts.Count;
            for (int i = 0; i < initialTriangleCount; i++)
            {
                if (triangleIsBroken[i] || !IsOriginalTriangle(i)) continue;
                int sub = pieceMappings[i].submeshIndex;
                if (sub < 0 || sub >= lists.Length) continue;
                int[] nb = triangleNeighbors[i];
                bool any = false;
                for (int k = 0; k < nb.Length && !any; k++) any = triangleIsBroken[nb[k]] && IsOriginalTriangle(nb[k]);
                if (!any) continue;

                Vector3 p0 = collisionVertices[initialTriangles[i * 3]];
                Vector3 p1 = collisionVertices[initialTriangles[i * 3 + 1]];
                Vector3 p2 = collisionVertices[initialTriangles[i * 3 + 2]];
                Vector3 inward = -Direction(Vector3.Cross(p1 - p0, p2 - p0)) * depth;
                Vector3 centroid = (p0 + p1 + p2) / 3f;

                for (int e = 0; e < 3; e++)
                {
                    int ia = initialTriangles[i * 3 + e], ib = initialTriangles[i * 3 + (e + 1) % 3];
                    Vector3 a = collisionVertices[ia], b = collisionVertices[ib];
                    for (int k = 0; k < nb.Length; k++)
                    {
                        int n = nb[k];
                        if (!triangleIsBroken[n] || !IsOriginalTriangle(n)) continue;
                        if (!TriangleHasPoint(n, a, weld2) || !TriangleHasPoint(n, b, weld2)) continue;

                        Vector3 w = Vector3.Cross(b - a, inward);
                        Vector3 nCentroid = (collisionVertices[initialTriangles[n * 3]] + collisionVertices[initialTriangles[n * 3 + 1]] + collisionVertices[initialTriangles[n * 3 + 2]]) / 3f;
                        bool flip = Vector3.Dot(w, nCentroid - centroid) < 0f;
                        Vector3 normal = Direction(flip ? -w : w);

                        int q = baseCount + capVerts.Count;
                        capVerts.Add(a); capVerts.Add(b); capVerts.Add(b + inward); capVerts.Add(a + inward);
                        for (int c = 0; c < 4; c++) capNormals.Add(normal);
                        int ua = ia, ub = ib;
                        if (baseUvs.Count == baseCount) { capUvs.Add(baseUvs[ua]); capUvs.Add(baseUvs[ub]); capUvs.Add(baseUvs[ub]); capUvs.Add(baseUvs[ua]); }
                        if (baseUv2.Count == baseCount) { capUv2.Add(baseUv2[ua]); capUv2.Add(baseUv2[ub]); capUv2.Add(baseUv2[ub]); capUv2.Add(baseUv2[ua]); }
                        if (baseTangents.Count == baseCount) { capTangents.Add(baseTangents[ua]); capTangents.Add(baseTangents[ub]); capTangents.Add(baseTangents[ub]); capTangents.Add(baseTangents[ua]); }
                        if (baseColors.Count == baseCount) { capColors.Add(baseColors[ua]); capColors.Add(baseColors[ub]); capColors.Add(baseColors[ub]); capColors.Add(baseColors[ua]); }

                        // Winding faces the hole side; with flip the quad's normal is -w.
                        int[] order = flip ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 };
                        foreach (int o in order) lists[sub].Add(q + o);
                        break;
                    }
                }
            }
        }

        // Castle parts live at 1/100 size, so their cross products are tiny and Vector3.normalized gives zero below 1e-5:
        // a zero normal lit in HDR makes NaN pixels that bloom spreads over the screen. Scale up first; up if truly flat.
        private static Vector3 Direction(Vector3 v)
        {
            Vector3 scaled = v * 1e6f;
            return scaled.sqrMagnitude > 1e-12f ? scaled.normalized : Vector3.up;
        }

        private void ApplyCapVertices()
        {
            var v = new List<Vector3>(baseVerts); v.AddRange(capVerts);
            var nrm = new List<Vector3>(baseNormals); nrm.AddRange(capNormals);
            renderMesh.SetVertices(v);
            renderMesh.SetNormals(nrm);
            if (baseUvs.Count == baseVerts.Count) { var l = new List<Vector2>(baseUvs); l.AddRange(capUvs); renderMesh.SetUVs(0, l); }
            if (baseUv2.Count == baseVerts.Count) { var l = new List<Vector2>(baseUv2); l.AddRange(capUv2); renderMesh.SetUVs(1, l); }
            if (baseTangents.Count == baseVerts.Count) { var l = new List<Vector4>(baseTangents); l.AddRange(capTangents); renderMesh.SetTangents(l); }
            if (baseColors.Count == baseVerts.Count) { var l = new List<Color>(baseColors); l.AddRange(capColors); renderMesh.SetColors(l); }
        }

        // The one-sided collider of a double-sided fracture mesh, with the same vertices so triangle indices still match.
        private void SetColliderToFrontTriangles()
        {
            var front = new List<int>(initialTriangles.Length / 2);
            for (int i = 0; i < initialTriangleCount; i++)
            {
                if (!IsOriginalTriangle(i)) continue;
                front.Add(initialTriangles[i * 3]); front.Add(initialTriangles[i * 3 + 1]); front.Add(initialTriangles[i * 3 + 2]);
            }
            frontTriangleCount = front.Count / 3;
            collisionMesh.triangles = front.ToArray();
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = collisionMesh;
        }

        // DoubleSidedMeshGenerator appends the reversed twins after the originals in each submesh.
        private bool IsOriginalTriangle(int tri)
        {
            // A clustered bake numbers pieces by cluster, not by triangle, so only its kinds tell front from twin and edge.
            if (triangleKind != null) return triangleKind[tri] == 0;
            PieceMapping m = pieceMappings[tri];
            return m.pieceIndex < originalPerSubmesh[m.submeshIndex];
        }

        private bool TriangleHasPoint(int tri, Vector3 p, float weld2)
        {
            for (int j = 0; j < 3; j++)
                if ((collisionVertices[initialTriangles[tri * 3 + j]] - p).sqrMagnitude <= weld2) return true;
            return false;
        }

        [ContextMenu("Test Fracture")]
        public void TriggerFracture()
        {
            TryGetComponent<Collider>(out Collider col);
            Hit(col, transform.position, explosionForce);
        }

#if UNITY_EDITOR
        [ContextMenu("Bake Fracture Graph")]
        public void BakeFractureGraph() => BakeFractureGraph(null);

        /// <summary>Bakes the graph. <paramref name="triangleClusters"/> is a cluster id per triangle of the fracture mesh (submesh by submesh); null gives one cluster per triangle. <paramref name="triangleKinds"/> is 0 front surface, 1 back twin, 2 stone edge per triangle; null means all 0. <paramref name="edgeOther"/> is a stone edge's cluster across its border (-1 otherwise).</summary>
        public void BakeFractureGraph(int[] triangleClusters, byte[] triangleKinds = null, int[] edgeOther = null)
        {
            if (!TryGetComponent<MeshCollider>(out var mCol) || (fractureMesh == null && mCol.sharedMesh == null))
            {
                Debug.LogError("No MeshCollider or sharedMesh found to bake!");
                return;
            }
            
            // Re-simulate setup locally
            collisionMesh = fractureMesh != null ? fractureMesh : mCol.sharedMesh;
            initialTriangles = collisionMesh.triangles;
            collisionVertices = collisionMesh.vertices;
            initialTriangleCount = initialTriangles.Length / 3;

            bool clustered = triangleClusters != null && triangleClusters.Length == initialTriangleCount;
            int[] cluster = new int[initialTriangleCount];
            for (int i = 0; i < cluster.Length; i++) cluster[i] = clustered ? triangleClusters[i] : i;

            // A piece is a (submesh, cluster) pair: pieceIndex counts the submesh's clusters in first-appearance order.
            pieceMappings = new PieceMapping[initialTriangleCount];
            int globalTri = 0;
            for (int s = 0; s < collisionMesh.subMeshCount; s++)
            {
                int submeshTriCount = collisionMesh.GetTriangles(s).Length / 3;
                var pieceOf = new Dictionary<int, int>();
                for (int p = 0; p < submeshTriCount; p++)
                {
                    if (!pieceOf.TryGetValue(cluster[globalTri], out int piece)) pieceOf[cluster[globalTri]] = piece = pieceOf.Count;
                    pieceMappings[globalTri++] = new PieceMapping { submeshIndex = s, pieceIndex = piece };
                }
            }
            
            BuildAdjacencyGraph();

            int clusterCount = 0;
            foreach (int c in cluster) clusterCount = Mathf.Max(clusterCount, c + 1);
            var members = new List<int>[clusterCount];
            var near = new HashSet<int>[clusterCount];
            var support = new bool[clusterCount];
            var sum = new Vector3[clusterCount];
            for (int c = 0; c < clusterCount; c++) { members[c] = new List<int>(); near[c] = new HashSet<int>(); }
            for (int i = 0; i < initialTriangleCount; i++)
            {
                int c = cluster[i];
                members[c].Add(i);
                support[c] |= isSupportTriangle[i];
                for (int j = 0; j < 3; j++) sum[c] += collisionVertices[initialTriangles[i * 3 + j]];
                foreach (int n in triangleNeighbors[i]) if (cluster[n] != c) near[c].Add(cluster[n]);
            }
            var clusterData = new IntArray[clusterCount];
            var neighborData = new IntArray[clusterCount];
            var centroids = new Vector3[clusterCount];
            var radii = new float[clusterCount];
            for (int c = 0; c < clusterCount; c++)
            {
                clusterData[c] = new IntArray { array = members[c].ToArray() };
                neighborData[c] = new IntArray { array = new List<int>(near[c]).ToArray() };
                centroids[c] = sum[c] / (members[c].Count * 3);
                foreach (int i in members[c])
                    for (int j = 0; j < 3; j++)
                        radii[c] = Mathf.Max(radii[c], Vector3.Distance(centroids[c], collisionVertices[initialTriangles[i * 3 + j]]));
            }

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
            data.triangleCluster = cluster;
            data.triangleKind = triangleKinds != null && triangleKinds.Length == initialTriangleCount ? triangleKinds : new byte[initialTriangleCount];
            data.triangleEdgeOther = edgeOther != null && edgeOther.Length == initialTriangleCount ? edgeOther : null;
            data.clusterTriangles = clusterData;
            data.clusterNeighbors = neighborData;
            data.clusterIsSupport = support;
            data.clusterCentroid = centroids;
            data.clusterRadius = radii;
            
            data.triangleNeighbors = new IntArray[triangleNeighbors.Length];
            for (int i = 0; i < triangleNeighbors.Length; i++)
            {
                data.triangleNeighbors[i] = new IntArray { array = triangleNeighbors[i] };
            }

            // Deleted first, as the bake does its meshes: writing over the old asset left loaded prefabs pointing at the
            // destroyed old graph until each was reimported (2026-10-28).
            UnityEditor.AssetDatabase.DeleteAsset(path);
            UnityEditor.AssetDatabase.CreateAsset(data, path);
            UnityEditor.AssetDatabase.SaveAssets();

            this.bakedGraphData = data;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("Successfully baked and assigned FractureGraphData at " + path);
        }
#endif
    }
}
