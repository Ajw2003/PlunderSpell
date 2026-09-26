// A stylised flame on a quad that turns about its own vertical axis to face the camera. Additive
// and HDR, so bloom and the fog halo round it carry the glow. Colour comes from FireSource through
// _FlameColor, which the atmosphere reddens as the alarm rises.
Shader "Plunderspell/Flame"
{
    Properties
    {
        [HDR] _FlameColor ("Flame colour", Color) = (1, 0.52, 0.2, 1)
        _CoreColor ("Core colour", Color) = (1, 0.93, 0.75, 1)
        _Brightness ("Brightness", Float) = 6
        _Speed ("Flicker speed", Float) = 2.4
        _Wobble ("Wobble", Range(0, 1)) = 0.35
        _FlameSeed ("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Flame"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "NightFogCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FlameColor;
                half4 _CoreColor;
                float _Brightness;
                float _Speed;
                float _Wobble;
                float _FlameSeed;
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

                // Face the camera round the vertical only, so the flame never lies down when seen from above.
                float3 toCamera = _WorldSpaceCameraPos - origin;
                toCamera.y = 0;
                float3 forward = normalize(toCamera + float3(1e-4, 0, 0));
                float3 right = normalize(cross(float3(0, 1, 0), forward));

                // The quad's bottom edge sits on the fire's origin; it rises from there.
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
                float t = _Time.y * _Speed + _FlameSeed;
                float2 uv = input.uv;

                // Two octaves of noise scrolling upward bend the flame and eat its edges.
                float n = ValueNoise(float2(uv.x * 3.0, uv.y * 2.2 - t * 1.3) + _FlameSeed)
                        * 0.65 + ValueNoise(float2(uv.x * 7.0, uv.y * 5.0 - t * 2.7) + _FlameSeed * 1.7) * 0.35;
                float sway = (ValueNoise(float2(t * 0.6, _FlameSeed)) - 0.5) * 0.25 * uv.y;
                float x = (uv.x - 0.5 - sway - (n - 0.5) * _Wobble * uv.y) * 2.0;

                // A teardrop: round at the base, drawn to a point at the top.
                float taper = max(1.0 - uv.y, 0.001);
                float2 q = float2(x / (0.45 + 0.55 * taper), (uv.y - 0.3) * 1.45);
                float r = length(q) + (n - 0.5) * 0.35 * uv.y;
                float body = smoothstep(1.0, 0.55, r);
                float core = smoothstep(0.55, 0.05, r) * smoothstep(0.85, 0.2, uv.y);

                half3 color = _FlameColor.rgb * body + _CoreColor.rgb * core * 1.6;
                color *= _Brightness * body;

                // Additive: fog only takes light away from behind it; the fog pass already added the glow.
                float3 toFlame = input.positionWS - _WorldSpaceCameraPos;
                float dist = length(toFlame);
                float transmittance = exp(-NightFogOpticalDepth(_WorldSpaceCameraPos, toFlame / max(dist, 1e-4), dist));
                return half4(color * transmittance, 0);
            }
            ENDHLSL
        }
    }
}
