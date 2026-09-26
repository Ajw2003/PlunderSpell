#ifndef PLUNDERSPELL_SURFACE_FORWARD_INCLUDED
#define PLUNDERSPELL_SURFACE_FORWARD_INCLUDED

// The castle's painted look (docs/plans/night-atmosphere.md, section 3): flat palette colour,
// greyscale detail projected by world position, grime at the foot of walls, and light that falls
// off in soft bands with a warm-dark tint on the shadowed side instead of grey.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    half4 color : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings SurfaceVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS);
    output.positionCS = position.positionCS;
    output.positionWS = position.positionWS;
    output.normalWS = normal.normalWS;
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.color = input.color;
    return output;
}

// Greyscale detail for this surface: its pigment's channel of the detail texture, projected along
// the three world axes and blended by the surface's facing, or along the dominant one only on Low.
half SampleDetail(float3 positionWS, half3 normalWS)
{
    float3 p = positionWS * _DetailScale;
    half3 w = abs(normalWS);
    #if defined(_PLUNDER_TRIPLANAR)
        w = w * w * w * w;
        w /= max(w.x + w.y + w.z, 1e-4);
        half4 x = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, p.zy);
        half4 y = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, p.xz);
        half4 z = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, p.xy);
        half4 d = x * w.x + y * w.y + z * w.z;
    #else
        float2 uv = w.y >= max(w.x, w.z) ? p.xz : (w.x >= w.z ? p.zy : p.xy);
        half4 d = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, uv);
    #endif
    return dot(d, _DetailMask);
}

// Soft steps: the light's strength is squeezed into 0-1, snapped to _Bands levels with soft
// edges, and expanded back, so a fire lights the wall in rings of flat colour rather than a
// smooth gradient. _BandAmount mixes the banded result with the smooth one.
half BandLight(half strength)
{
    half t = 1.0h - exp(-strength);
    half v = t * _Bands;
    half i = floor(v);
    half f = v - i;
    half stepped = (i + smoothstep(0.5h - _BandSoftness, 0.5h + _BandSoftness, f)) / _Bands;
    half banded = -log(max(1.0h - stepped, 0.02h));
    return lerp(strength, banded, _BandAmount);
}

// Fires light in bands; the moon, a faint fill, stays smooth, or banding would round it to nothing.
half3 LightContribution(Light light, half3 normalWS, bool banded)
{
    half facing = saturate(dot(normalWS, light.direction));
    half strength = facing * light.distanceAttenuation * light.shadowAttenuation;
    return light.color * (banded ? BandLight(strength) : strength);
}

half4 SurfaceFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half3 normalWS = normalize(input.normalWS);
    half4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
    half3 albedo = base.rgb;

    half detail = SampleDetail(input.positionWS, normalWS);
    albedo *= lerp(1.0h, detail * 2.0h, _DetailStrength);

    // Stone darkens and warms at night, so fire reads against it (the palette's limestone is
    // near-white under any light).
    albedo *= lerp(half3(1, 1, 1), _PlunderStoneTint.rgb, _PlunderStoneTint.a * _IsStone);

    // Soot: crevices baked into vertex colour by the pipeline, and grime rising from the ground on walls.
    albedo *= lerp(1.0h, input.color.r, _VertexSoot);
    half wall = 1.0h - abs(normalWS.y);
    half aboveGround = saturate((input.positionWS.y - 0.3) / 1.6);
    albedo *= lerp(1.0h, lerp(_GroundGrime, 1.0h, aboveGround), wall);

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = normalWS;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    half4 shadowMask = half4(1, 1, 1, 1);

    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);

    // The shadowed side: ambient only, tinted warm-dark rather than grey.
    half3 lighting = SampleSH(normalWS) * _ShadowTint.rgb * ao.indirectAmbientOcclusion;

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);
    lighting += LightContribution(mainLight, normalWS, false) * ao.directAmbientOcclusion;

    #if defined(_ADDITIONAL_LIGHTS)
        uint pixelLightCount = GetAdditionalLightsCount();
        #if USE_CLUSTER_LIGHT_LOOP
            [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
            {
                Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
                lighting += LightContribution(light, normalWS, false);
            }
        #endif
        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowMask);
            lighting += LightContribution(light, normalWS, true);
        LIGHT_LOOP_END
    #endif

    half3 color = albedo * lighting;
    #if defined(_EMISSION)
        color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, input.uv).rgb * _EmissionColor.rgb;
    #endif
    return half4(color, 1.0h);
}

#endif
