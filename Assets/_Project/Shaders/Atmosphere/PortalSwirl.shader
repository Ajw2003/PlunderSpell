// The portal the team arrives and leaves by: a standing oval of swirling lapis light. Additive and
// HDR; it turns about its vertical axis to face the camera, like the flames.
Shader "Plunderspell/PortalSwirl"
{
    Properties
    {
        [HDR] _ColorA ("Lapis", Color) = (1.4, 1.1, 3.2, 1)
        [HDR] _ColorB ("Verdigris edge", Color) = (0.6, 2.2, 1.6, 1)
        _Brightness ("Brightness", Float) = 2.5
        _Speed ("Swirl speed", Float) = 0.6
        _Strength ("Strength", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+5" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "NightFogCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA;
                half4 _ColorB;
                float _Brightness;
                float _Speed;
                float _Strength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 origin = TransformObjectToWorld(float3(0, 0, 0));
                float width = length(GetObjectToWorldMatrix()._m00_m10_m20);
                float height = length(GetObjectToWorldMatrix()._m01_m11_m21);
                float3 toCamera = _WorldSpaceCameraPos - origin;
                toCamera.y = 0;
                float3 forward = normalize(toCamera + float3(1e-4, 0, 0));
                float3 right = normalize(cross(float3(0, 1, 0), forward));
                float3 positionWS = origin + right * input.positionOS.x * width
                                    + float3(0, 1, 0) * (input.positionOS.y + 0.5) * height;
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = (input.uv - 0.5) * float2(2.0, 2.0);
                float r = length(p / float2(0.8, 1.0));
                float angle = atan2(p.y, p.x);
                float t = _Time.y * _Speed;

                // Arms spiralling inward, broken up by noise.
                float spiral = sin(angle * 3.0 + r * 9.0 - t * 6.0) * 0.5 + 0.5;
                float n = ValueNoise(float2(angle * 2.0 + t, r * 5.0 - t * 2.0));
                float oval = smoothstep(1.0, 0.82, r);
                float rim = smoothstep(0.7, 0.95, r) * oval;
                float body = oval * (0.25 + 0.75 * spiral * n) * smoothstep(0.0, 0.5, r + 0.2);

                half3 color = (_ColorA.rgb * body + _ColorB.rgb * rim * 1.2) * _Brightness * _Strength;

                float3 toPortal = input.positionWS - _WorldSpaceCameraPos;
                float dist = length(toPortal);
                float transmittance = exp(-NightFogOpticalDepth(_WorldSpaceCameraPos, toPortal / max(dist, 1e-4), dist));
                return half4(color * transmittance, 0);
            }
            ENDHLSL
        }
    }
}
