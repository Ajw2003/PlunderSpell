// Full-screen fog for the castle at night. Drawn by RogueAi.Atmosphere.NightFogFeature after the
// opaques, blended over the camera colour as scene * transmittance + in-scattered light.
Shader "Hidden/Plunderspell/NightFog"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "NightFog"
            Blend One SrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "NightFogCommon.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.uv = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float rawDepth = SampleSceneDepth(input.uv);
                float3 positionWS = ComputeWorldSpacePosition(input.uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 toPoint = positionWS - _WorldSpaceCameraPos;
                float dist = length(toPoint);
                float3 dir = toPoint / max(dist, 1e-4);

                #if UNITY_REVERSED_Z
                    bool isSky = rawDepth <= 1e-6;
                #else
                    bool isSky = rawDepth >= 1.0 - 1e-6;
                #endif
                // The sky sits at a fixed distance, so looking up through thin fog stays dark and the
                // horizon, seen through a long run of it, glows with the castle's fires.
                if (isSky)
                    dist = _NF_Params.w;

                return half4(NightFogAlongRay(_WorldSpaceCameraPos, dir, dist));
            }
            ENDHLSL
        }
    }
}
