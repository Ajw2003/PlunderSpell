// Full-screen fog for the castle at night. Drawn by Plunderspell.Atmosphere.NightFogFeature after the
// opaques, blended over the camera colour as scene * transmittance + in-scattered light.
// Pass 1, "Ink" (#231), is drawn just before the fog by the same feature, so it ships in builds
// with it and fades into the fog like everything else.
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

                float4 fog = NightFogAlongRay(_WorldSpaceCameraPos, dir, dist);
                // Overhead, the sky shows through: stars and the moon above the fog. Only sky pixels,
                // and less the nearer the horizon, where the fires' glow stays.
                if (isSky)
                {
                    float clear = _NF_Sky.x * pow(saturate(dir.y), _NF_Sky.y);
                    fog.a = lerp(fog.a, 1.0, clear);
                    fog.rgb *= 1.0 - clear;
                }
                return half4(fog);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Ink"
            // Multiplies the scene: 1 leaves it alone, lower darkens it towards black.
            Blend DstColor Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

            // Set by CastleAtmosphere: x outline strength, y paper grain, z how sharp a depth step
            // must be to get a line. All 0 when no atmosphere is running.
            float4 _PlunderInk;

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

            // Blotchy paper: hashed values on a coarse grid, smoothly interpolated.
            half PaperBlotch(float2 pixel, float cell)
            {
                float2 p = pixel / cell;
                uint2 i = (uint2)floor(p);
                float2 f = smoothstep(0.0, 1.0, frac(p));
                half a = GenerateHashedRandomFloat(i);
                half b = GenerateHashedRandomFloat(i + uint2(1, 0));
                half c = GenerateHashedRandomFloat(i + uint2(0, 1));
                half d = GenerateHashedRandomFloat(i + uint2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Outlines: the Laplacian of raw depth. Raw depth runs as 1/distance, which is
                // linear across any flat surface, so walls and floors give zero and only
                // silhouettes and sharp corners give a line. Divided by the centre depth so a
                // near and a far edge of the same shape get the same line.
                float2 texel = _ScreenSize.zw * max(1.0, _ScreenSize.y / 1080.0);
                float c = SampleSceneDepth(input.uv);
                float l = SampleSceneDepth(input.uv - float2(texel.x, 0));
                float r = SampleSceneDepth(input.uv + float2(texel.x, 0));
                float d = SampleSceneDepth(input.uv - float2(0, texel.y));
                float u = SampleSceneDepth(input.uv + float2(0, texel.y));
                float edge = abs(l + r + u + d - 4.0 * c) / max(max(c, max(max(l, r), max(u, d))), 1e-6);
                half ink = smoothstep(_PlunderInk.z, _PlunderInk.z * 3.0, edge) * _PlunderInk.x;

                float2 pixel = input.uv * _ScreenSize.xy;
                half fibre = GenerateHashedRandomFloat((uint2)pixel);
                half paper = 1.0h - _PlunderInk.y * (0.35h * fibre + 0.65h * PaperBlotch(pixel, 48.0));

                return half4((1.0h - 0.85h * ink) * paper.xxx, 1.0h);
            }
            ENDHLSL
        }
    }
}
