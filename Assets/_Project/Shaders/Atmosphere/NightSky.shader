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
        _StarScale ("Star field scale", Range(50, 800)) = 320
        _StarDensity ("Share of cells with a star", Range(0, 1)) = 0.12
        _StarBrightness ("Star brightness", Range(0, 4)) = 0.9
        _StarTwinkle ("Twinkle", Range(0, 1)) = 0.35
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
                float _StarScale;
                float _StarDensity;
                float _StarBrightness;
                float _StarTwinkle;
            CBUFFER_END

            float3 StarHash(float3 cell)
            {
                cell = frac(cell * float3(0.1031, 0.1030, 0.0973));
                cell += dot(cell, cell.yxz + 33.33);
                return frac((cell.xxy + cell.yxx) * cell.zyx);
            }

            // Points of light on a grid of cells over the sky sphere: one candidate star per cell,
            // at a random spot in it, kept by a random draw against the density.
            half Stars(float3 dir)
            {
                float3 p = dir * _StarScale;
                float3 cell = floor(p);
                float3 h = StarHash(cell);
                if (h.x > _StarDensity)
                    return 0.0;
                float3 centre = cell + 0.2 + 0.6 * StarHash(cell + 17.0);
                float d = length(p - centre);
                float star = saturate(1.0 - d * 2.2);
                star *= star;
                float size = lerp(0.35, 1.0, h.y * h.y);
                float twinkle = 1.0 - _StarTwinkle * (0.5 + 0.5 * sin(_Time.y * (1.5 + 3.0 * h.z) + h.x * 40.0));
                return star * size * twinkle;
            }

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
                // Stars fade out low in the sky and near the moon's glare.
                half starFade = saturate(dir.y * 4.0) * (1.0 - saturate(glow * 4.0));
                color += half3(0.85, 0.9, 1.0) * Stars(dir) * _StarBrightness * starFade;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
