// The night sky behind the castle: near-black overhead, a little lighter at the horizon, and a
// soft moon glow. The fog pass paints over most of it; this is what shows where the fog is thin.
Shader "Plunderspell/NightSky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.01, 0.012, 0.018, 1)
        _Horizon ("Horizon", Color) = (0.03, 0.028, 0.03, 1)
        _MoonColor ("Moon", Color) = (0.45, 0.58, 0.9, 1)
        _MoonDir ("Moon direction", Vector) = (0.3, 0.44, -0.85, 0)
        _MoonSize ("Moon disc size", Range(0.9990, 0.99999)) = 0.99997
        _MoonGlow ("Moon glow", Range(0, 2)) = 0.25
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith;
                half4 _Horizon;
                half4 _MoonColor;
                float4 _MoonDir;
                float _MoonSize;
                float _MoonGlow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float up = saturate(dir.y);
                half3 color = lerp(_Horizon.rgb, _Zenith.rgb, pow(up, 0.45));

                float3 moonDir = normalize(_MoonDir.xyz);
                float facing = dot(dir, moonDir);
                float disc = smoothstep(_MoonSize - 0.00001, _MoonSize, facing);
                float glow = pow(saturate(facing), 180.0) * 0.6 + pow(saturate(facing), 24.0) * 0.12;
                color += _MoonColor.rgb * (disc * 0.5 + glow * _MoonGlow);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
