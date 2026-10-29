// Ported from ufo-3-rag-dahl FractureSystem (aj); see docs/plans/ophan-redo-plan.html.
using UnityEngine;
using Unity.Collections;

namespace Plunderspell.Fracture
{
    public class FractureEffect : MonoBehaviour
    {
        public Vector3 gravity;
        public float groundY = 0.05f;
        public float despawnTime = 5f;
        
        private ComputeBuffer pieceBuffer;
        private ComputeBuffer vertexBuffer;
        private ComputeShader computeShader;
        private Material renderMaterial;
        public bool hideStatic;
        private int pieceCount;
        private int vertexCount;
        private float awakeUntil; // hideStatic only: no dispatch/draw after this time
        
        private int kernelID;
        private int kernelImpactID;
        private int kernelDropID;

        // Property IDs instead of per-call string lookups on the per-frame path (audit P19).
        private static readonly int PiecesId = Shader.PropertyToID("_Pieces");
        private static readonly int DeltaTimeId = Shader.PropertyToID("_DeltaTime");
        private static readonly int GravityId = Shader.PropertyToID("_Gravity");
        private static readonly int GroundYId = Shader.PropertyToID("_GroundY");
        private static readonly int ImpactPointId = Shader.PropertyToID("_ImpactPoint");
        private static readonly int ImpactRadiusId = Shader.PropertyToID("_ImpactRadius");
        private static readonly int ImpactForceId = Shader.PropertyToID("_ImpactForce");
        private static readonly int DespawnTimeId = Shader.PropertyToID("_DespawnTime");
        private static readonly int DropIndicesId = Shader.PropertyToID("_DropIndices");
        private static readonly int DropCountId = Shader.PropertyToID("_DropCount");

        public void Initialize(NativeArray<PieceData> pieceData, NativeArray<VertexData> vertexData, ComputeShader shader, Material material, Shader fractureShader)
        {
            if (!pieceData.IsCreated || pieceData.Length == 0 || !vertexData.IsCreated || vertexData.Length == 0) return;

            pieceCount = pieceData.Length;
            vertexCount = vertexData.Length;
            computeShader = shader;
            renderMaterial = material;
            
            pieceBuffer = new ComputeBuffer(pieceCount, 64);
            pieceBuffer.SetData(pieceData);
            
            vertexBuffer = new ComputeBuffer(vertexCount, 48);
            vertexBuffer.SetData(vertexData);
            
            kernelID = computeShader.FindKernel("CSMain");
            kernelImpactID = computeShader.FindKernel("CSImpact");
            kernelDropID = computeShader.FindKernel("CSDrop");

            // A copy of the piece's own material with the fracture shader swapped in, as the original did; the shader comes from
            // the material FracturableObject resolved (serialized or Resources/Fracture/Fracture) instead of Shader.Find.
            renderMaterial = new Material(renderMaterial);
            // URP Lit keeps its colour in _BaseMap/_BaseColor; the fracture shader reads _MainTex/_Color.
            Texture baseMap = renderMaterial.HasProperty("_BaseMap") ? renderMaterial.GetTexture("_BaseMap") : null;
            Color? baseColor = renderMaterial.HasProperty("_BaseColor") ? renderMaterial.GetColor("_BaseColor") : (Color?)null;
            if (fractureShader != null) renderMaterial.shader = fractureShader;
            if (baseMap != null) renderMaterial.SetTexture("_MainTex", baseMap);
            if (baseColor.HasValue) renderMaterial.SetColor("_Color", baseColor.Value);
            renderMaterial.SetInt("_HideStatic", hideStatic ? 1 : 0);
            renderMaterial.SetBuffer("_Pieces", pieceBuffer);
            renderMaterial.SetBuffer("_Vertices", vertexBuffer);
            
            computeShader.SetBuffer(kernelID, PiecesId, pieceBuffer);
            computeShader.SetBuffer(kernelImpactID, PiecesId, pieceBuffer);

            // A first dispatch at load, far from every piece, so the kernel is ready before the first real hit (aj's
            // hitches, 2026-10-29). It moves nothing.
            computeShader.SetVector(ImpactPointId, new Vector3(1e7f, 1e7f, 1e7f));
            computeShader.SetFloat(ImpactRadiusId, 0f);
            computeShader.Dispatch(kernelImpactID, Mathf.CeilToInt(pieceCount / 64f), 1, 1);
        }

        public void DropPieces(int[] pieceIndices)
        {
            if (pieceBuffer == null || pieceIndices == null || pieceIndices.Length == 0) return;
            
            Wake();
            if (kernelDropID < 0) return;

            ComputeBuffer dropBuffer = new ComputeBuffer(pieceIndices.Length, 4);
            dropBuffer.SetData(pieceIndices);

            computeShader.SetBuffer(kernelDropID, PiecesId, pieceBuffer);
            computeShader.SetBuffer(kernelDropID, DropIndicesId, dropBuffer);
            computeShader.SetInt(DropCountId, pieceIndices.Length);
            computeShader.SetFloat(DespawnTimeId, despawnTime);
            computeShader.SetFloat(GroundYId, groundY);
            
            int groups = Mathf.CeilToInt(pieceIndices.Length / 64f);
            computeShader.Dispatch(kernelDropID, groups, 1, 1);
            
            dropBuffer.Release();
        }

        public void ApplyImpact(float impactRadius, Vector3 point, float force)
        {
            if (pieceBuffer == null) return;
            
            Wake();
            computeShader.SetBuffer(kernelImpactID, PiecesId, pieceBuffer);
            computeShader.SetVector(ImpactPointId, point);
            computeShader.SetFloat(ImpactRadiusId, impactRadius);
            computeShader.SetFloat(ImpactForceId, force);
            computeShader.SetFloat(DespawnTimeId, despawnTime);
            computeShader.SetFloat(GroundYId, groundY);
            
            int groups = Mathf.CeilToInt(pieceCount / 64f);
            computeShader.Dispatch(kernelImpactID, groups, 1, 1);
        }

        // Longest a woken piece lives is despawnTime * 1.5 (CSImpact/CSDrop); plus a margin.
        private void Wake() { awakeUntil = Time.time + despawnTime * 1.5f + 0.5f; }

        private void Update() // per-frame: simulates and draws the falling pieces while any are in flight
        {
            if (pieceBuffer == null || vertexBuffer == null) return;
            if (hideStatic && Time.time > awakeUntil) return;
            
            computeShader.SetBuffer(kernelID, PiecesId, pieceBuffer);
            computeShader.SetFloat(DeltaTimeId, Time.deltaTime);
            computeShader.SetVector(GravityId, gravity);
            computeShader.SetFloat(GroundYId, groundY);
            
            int groups = Mathf.CeilToInt(pieceCount / 64f);
            computeShader.Dispatch(kernelID, groups, 1, 1);
            
            Bounds bounds = new Bounds(transform.position, Vector3.one * 1000f);
            Graphics.DrawProcedural(renderMaterial, bounds, MeshTopology.Triangles, vertexCount, 1, null, null, hideStatic ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
        }

        public void SetSelfDestruct()
        {
            // Destroy this effect object after enough time has passed for all particles to naturally despawn
            Destroy(gameObject, despawnTime + 1f);
        }

        private void OnDestroy()
        {
            if (pieceBuffer != null)
            {
                pieceBuffer.Release();
                pieceBuffer = null;
            }
            if (vertexBuffer != null)
            {
                vertexBuffer.Release();
                vertexBuffer = null;
            }
            if (renderMaterial != null)
            {
                Destroy(renderMaterial);
            }
        }
    }
}
