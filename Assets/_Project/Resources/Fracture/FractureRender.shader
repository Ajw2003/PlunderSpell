Shader "FractureSystem/FractureRender"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Back

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma shader_feature_local _ALPHATEST_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct PieceData
            {
                float3 position;
                int isStatic;
                float4 rotation;
                float3 velocity;
                float radius;
                float3 angularVelocity;
                float lifeTime;
            };

            struct VertexData
            {
                float3 localPosition;
                uint pieceIndex;
                float3 localNormal;
                float pad0;
                float2 uv;
                float2 pad1;
            };

            StructuredBuffer<PieceData> _Pieces;
            StructuredBuffer<VertexData> _Vertices;
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _Color;
            int _HideStatic; // 1: the object's own renderer draws what is still in place (#472)
            float _Cutoff;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : NORMAL;
                float3 positionWS : TEXCOORD1;
            };

            float3 RotateVector(float3 v, float4 q)
            {
                float3 t = 2.0 * cross(q.xyz, v);
                return v + q.w * t + cross(q.xyz, t);
            }

            v2f vert (uint vertexID : SV_VertexID)
            {
                v2f o;
                
                VertexData vd = _Vertices[vertexID];
                PieceData pd = _Pieces[vd.pieceIndex];
                
                if (pd.isStatic == -1 || (_HideStatic == 1 && pd.isStatic == 1))
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.normalWS = float3(0, 1, 0);
                    o.uv = float2(0, 0);
                    o.positionWS = float3(0,0,0);
                    return o;
                }
                
                float3 worldPos = RotateVector(vd.localPosition, pd.rotation) + pd.position;
                float3 worldNorm = normalize(RotateVector(vd.localNormal, pd.rotation));
                
                o.pos = TransformWorldToHClip(worldPos);
                o.normalWS = worldNorm;
                o.uv = vd.uv;
                o.positionWS = worldPos;
                
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;
                
                #if defined(_ALPHATEST_ON)
                clip(tex.a - _Cutoff);
                #endif
                
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                half3 ambient = SampleSH(i.normalWS);
                half ndotl = saturate(dot(i.normalWS, mainLight.direction));
                
                half3 lighting = ambient + mainLight.color * (ndotl * mainLight.shadowAttenuation * mainLight.distanceAttenuation);
                
                return half4(tex.rgb * lighting, tex.a);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma shader_feature_local _ALPHATEST_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct PieceData
            {
                float3 position;
                int isStatic;
                float4 rotation;
                float3 velocity;
                float radius;
                float3 angularVelocity;
                float lifeTime;
            };

            struct VertexData
            {
                float3 localPosition;
                uint pieceIndex;
                float3 localNormal;
                float pad0;
                float2 uv;
                float2 pad1;
            };

            StructuredBuffer<PieceData> _Pieces;
            StructuredBuffer<VertexData> _Vertices;

            int _HideStatic; // the object's own renderer casts the shadows of what is in place
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _Cutoff;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float3 RotateVector(float3 v, float4 q)
            {
                float3 t = 2.0 * cross(q.xyz, v);
                return v + q.w * t + cross(q.xyz, t);
            }

            float3 _LightDirection;
            float3 _LightPosition;

            v2f vert(uint vertexID : SV_VertexID)
            {
                v2f o;
                VertexData vd = _Vertices[vertexID];
                o.uv = vd.uv;
                PieceData pd = _Pieces[vd.pieceIndex];
                
                // PERFORMANCE TWEAK: Only cast shadows if the piece is completely static (attached to the tree)
                // Flying pieces (isStatic == 0) and despawned pieces (isStatic == -1) will not cast shadows.
                if (pd.isStatic != 1 || _HideStatic == 1)
                {
                    o.pos = float4(0,0,0,0);
                    return o;
                }
                
                float3 worldPos = RotateVector(vd.localPosition, pd.rotation) + pd.position;
                float3 worldNorm = normalize(RotateVector(vd.localNormal, pd.rotation));
                
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - worldPos);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                
                float3 positionWS = ApplyShadowBias(worldPos, worldNorm, lightDir);
                o.pos = TransformWorldToHClip(positionWS);
                
                #if UNITY_REVERSED_Z
                o.pos.z = min(o.pos.z, o.pos.w * UNITY_NEAR_CLIP_VALUE);
                #else
                o.pos.z = max(o.pos.z, o.pos.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                clip(alpha - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }
}
