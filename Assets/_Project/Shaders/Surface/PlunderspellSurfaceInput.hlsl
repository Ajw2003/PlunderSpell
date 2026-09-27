#ifndef PLUNDERSPELL_SURFACE_INPUT_INCLUDED
#define PLUNDERSPELL_SURFACE_INPUT_INCLUDED

// Properties shared by every pass of Plunderspell/Surface, so the SRP Batcher sees one layout.
// _BaseMap, _BaseColor and _Cutoff keep URP's names: the castle's imported materials already
// carry them, and URP's shadow and depth passes read them.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Cutoff;
    half4 _DetailMask;
    float _DetailScale;
    half _DetailStrength;
    half _IsStone;
    half _VertexSoot;
    half _GroundGrime;
    half _Bands;
    half _BandSoftness;
    half _BandAmount;
    half4 _ShadowTint;
    half4 _EmissionColor;
CBUFFER_END

TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);
// _EmissionMap is declared by URP's SurfaceInput.hlsl.

// Set per era by Plunderspell.Atmosphere.CastleAtmosphere: rgb darkens and warms stone at night,
// a is how much of it applies (0 when no atmosphere is running, so nothing turns black).
half4 _PlunderStoneTint;

#endif
