// Ported from ufo-3-rag-dahl FractureSystem (aj); see docs/plans/ophan-redo-plan.html.
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Plunderspell.Fracture
{
    public static class FractureEngine
    {
        [BurstCompile]
        private struct FractureMathJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> triangles;
            [ReadOnly] public NativeArray<Vector3> vertices;
            [ReadOnly] public NativeArray<Vector3> normals;
            [ReadOnly] public NativeArray<Vector2> uvs;
            
            public Matrix4x4 localToWorld;
            public float lossyScaleX;
            public bool hasNormals;
            public bool hasUVs;

            [WriteOnly] public NativeArray<PieceData> pieceData;
            [NativeDisableParallelForRestriction]
            [WriteOnly] public NativeArray<VertexData> vertexData;

            public void Execute(int i)
            {
                int i0 = triangles[i * 3 + 0];
                int i1 = triangles[i * 3 + 1];
                int i2 = triangles[i * 3 + 2];
                
                Vector3 v0 = vertices[i0];
                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                
                Vector3 centroid = (v0 + v1 + v2) / 3f;
                Vector3 worldCentroid = localToWorld.MultiplyPoint3x4(centroid);
                
                float r0 = math.distance(centroid, v0);
                float r1 = math.distance(centroid, v1);
                float r2 = math.distance(centroid, v2);
                float radius = math.max(r0, math.max(r1, r2));

                pieceData[i] = new PieceData
                {
                    position = worldCentroid,
                    isStatic = 1,
                    rotation = Quaternion.identity,
                    velocity = Vector3.zero,
                    radius = radius * lossyScaleX,
                    angularVelocity = Vector3.zero,
                    lifeTime = 0f
                };

                for (int j = 0; j < 3; j++)
                {
                    int vIdx = triangles[i * 3 + j];
                    Vector3 worldV = localToWorld.MultiplyPoint3x4(vertices[vIdx]);
                    Vector3 pieceLocalV = worldV - worldCentroid;
                    
                    Vector3 worldNorm = hasNormals ? localToWorld.MultiplyVector(normals[vIdx]).normalized : Vector3.up;
                    Vector2 uv = hasUVs ? uvs[vIdx] : Vector2.zero;

                    vertexData[i * 3 + j] = new VertexData
                    {
                        localPosition = pieceLocalV,
                        pieceIndex = (uint)i,
                        localNormal = worldNorm,
                        pad0 = 0f,
                        uv = uv,
                        pad1 = Vector2.zero
                    };
                }
            }
        }

        public static FractureEffect[] Fracture(FracturableObject target)
        {
            if (target == null) return null;
            ComputeShader compute = target.ResolveComputeShader();
            Material renderMat = target.ResolveRenderMaterial();
            if (compute == null || renderMat == null) return null;
            
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return null;
            
            MeshRenderer mr = target.GetComponent<MeshRenderer>();
            if (mr == null) return null;
            
            Mesh mesh = filter.sharedMesh;
            
            int subMeshCount = mesh.subMeshCount;
            FractureEffect[] effects = new FractureEffect[subMeshCount];
            Matrix4x4 localToWorld = target.transform.localToWorldMatrix;
            float lossyScaleX = target.transform.lossyScale.x;

            Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            Mesh.MeshData meshData = meshDataArray[0];

            int vertexCount = mesh.vertexCount;
            var verticesNative = new NativeArray<Vector3>(vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            meshData.GetVertices(verticesNative);

            bool hasNormals = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal);
            var normalsNative = new NativeArray<Vector3>(hasNormals ? vertexCount : 1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            if (hasNormals) meshData.GetNormals(normalsNative);

            bool hasUVs = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0);
            var uvsNative = new NativeArray<Vector2>(hasUVs ? vertexCount : 1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            if (hasUVs) meshData.GetUVs(0, uvsNative);

            bool isUInt16 = mesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16;
            var rawIndices16 = isUInt16 ? meshData.GetIndexData<ushort>() : default;
            var rawIndices32 = !isUInt16 ? meshData.GetIndexData<int>() : default;

            for (int s = 0; s < subMeshCount; s++)
            {
                var subMeshDesc = meshData.GetSubMesh(s);
                int indexCount = subMeshDesc.indexCount;
                if (indexCount == 0) continue;
                
                NativeArray<int> trianglesNative = new NativeArray<int>(indexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                
                int indexStart = subMeshDesc.indexStart;
                if (isUInt16)
                {
                    for (int i = 0; i < indexCount; i++)
                    {
                        trianglesNative[i] = rawIndices16[indexStart + i];
                    }
                }
                else
                {
                    NativeArray<int>.Copy(rawIndices32, indexStart, trianglesNative, 0, indexCount);
                }
                
                int triangleCount = indexCount / 3;
                int pieceCount = triangleCount;
                
                NativeArray<PieceData> pieceData = new NativeArray<PieceData>(pieceCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                NativeArray<VertexData> vertexData = new NativeArray<VertexData>(indexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                FractureMathJob job = new FractureMathJob
                {
                    triangles = trianglesNative,
                    vertices = verticesNative,
                    normals = normalsNative,
                    uvs = uvsNative,
                    localToWorld = localToWorld,
                    lossyScaleX = lossyScaleX,
                    hasNormals = hasNormals,
                    hasUVs = hasUVs,
                    pieceData = pieceData,
                    vertexData = vertexData
                };

                JobHandle handle = job.Schedule(triangleCount, 64);
                handle.Complete();

                GameObject effectObj = new GameObject("FractureEffect_" + target.gameObject.name + "_SubMesh_" + s);
                effectObj.transform.position = target.transform.position;
                
                FractureEffect effect = effectObj.AddComponent<FractureEffect>();
                effect.gravity = target.gravity;
                effect.despawnTime = target.despawnTime;
                effect.groundY = target.groundY;
                
                // As the original: each submesh keeps its own material (the castle's palette colours), drawn with the fracture shader.
                Material[] own = mr.sharedMaterials;
                Material source = own != null && own.Length > s && own[s] != null ? own[s] : renderMat;
                effect.Initialize(pieceData, vertexData, compute, source, renderMat.shader);
                effects[s] = effect;

                trianglesNative.Dispose();
                pieceData.Dispose();
                vertexData.Dispose();
            }

            verticesNative.Dispose();
            if (normalsNative.IsCreated) normalsNative.Dispose();
            if (uvsNative.IsCreated) uvsNative.Dispose();
            meshDataArray.Dispose();

            mr.enabled = false;
            
            return effects;
        }
    }
}
