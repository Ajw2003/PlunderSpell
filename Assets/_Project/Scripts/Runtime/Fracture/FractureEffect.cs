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
        
        private int kernelID;
        private int kernelImpactID;

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
            
            computeShader.SetBuffer(kernelID, "_Pieces", pieceBuffer);
            computeShader.SetBuffer(kernelImpactID, "_Pieces", pieceBuffer);
        }

        public void DropPieces(int[] pieceIndices)
        {
            if (pieceBuffer == null || pieceIndices == null || pieceIndices.Length == 0) return;
            
            int kernelDropID = computeShader.FindKernel("CSDrop");
            if (kernelDropID < 0) return;
            
            ComputeBuffer dropBuffer = new ComputeBuffer(pieceIndices.Length, 4);
            dropBuffer.SetData(pieceIndices);
            
            computeShader.SetBuffer(kernelDropID, "_Pieces", pieceBuffer);
            computeShader.SetBuffer(kernelDropID, "_DropIndices", dropBuffer);
            computeShader.SetInt("_DropCount", pieceIndices.Length);
            computeShader.SetFloat("_DespawnTime", despawnTime);
            computeShader.SetFloat("_GroundY", groundY);
            
            int groups = Mathf.CeilToInt(pieceIndices.Length / 64f);
            computeShader.Dispatch(kernelDropID, groups, 1, 1);
            
            dropBuffer.Release();
        }

        public void ApplyImpact(float impactRadius, Vector3 point, float force)
        {
            if (pieceBuffer == null) return;
            
            computeShader.SetBuffer(kernelImpactID, "_Pieces", pieceBuffer);
            computeShader.SetVector("_ImpactPoint", point);
            computeShader.SetFloat("_ImpactRadius", impactRadius);
            computeShader.SetFloat("_ImpactForce", force);
            computeShader.SetFloat("_DespawnTime", despawnTime);
            computeShader.SetFloat("_GroundY", groundY);
            
            int groups = Mathf.CeilToInt(pieceCount / 64f);
            computeShader.Dispatch(kernelImpactID, groups, 1, 1);
        }

        private void Update()
        {
            if (pieceBuffer == null || vertexBuffer == null) return;
            
            computeShader.SetBuffer(kernelID, "_Pieces", pieceBuffer);
            computeShader.SetFloat("_DeltaTime", Time.deltaTime);
            computeShader.SetVector("_Gravity", gravity);
            computeShader.SetFloat("_GroundY", groundY);
            
            int groups = Mathf.CeilToInt(pieceCount / 64f);
            computeShader.Dispatch(kernelID, groups, 1, 1);
            
            Bounds bounds = new Bounds(transform.position, Vector3.one * 1000f);
            Graphics.DrawProcedural(renderMaterial, bounds, MeshTopology.Triangles, vertexCount, 1, null, null, UnityEngine.Rendering.ShadowCastingMode.On, true, gameObject.layer);
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
