// One shader for the whole castle (docs/plans/night-atmosphere.md, section 3). Castle models get
// it on import (RogueAi.EditorTools.CastleSurfaceMaterials), with the detail channel and the
// stone flag chosen from each material's pigment. Fog is drawn afterwards by the night fog pass.
Shader "Plunderspell/Surface"
{
    Properties
    {
        [MainTexture] _BaseMap ("Palette", 2D) = "white" {}
        [MainColor] _BaseColor ("Colour", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5

        [NoScaleOffset] _DetailMap ("Detail (R stone, G wood, B iron, A cloth)", 2D) = "grey" {}
        _DetailMask ("Detail channel", Vector) = (1, 0, 0, 0)
        _DetailScale ("Detail tiles per metre", Float) = 0.5
        _DetailStrength ("Detail strength", Range(0, 1)) = 0.6

        _IsStone ("Takes the night stone tint", Range(0, 1)) = 0
        _VertexSoot ("Vertex-colour soot", Range(0, 1)) = 0
        _GroundGrime ("Grime at the foot of walls", Range(0, 1)) = 0.6

        _Bands ("Light bands", Range(1, 6)) = 3
        _BandSoftness ("Band softness", Range(0.01, 0.5)) = 0.12
        _BandAmount ("Banding", Range(0, 1)) = 0.75
        _ShadowTint ("Shadow-side tint", Color) = (1.15, 0.95, 0.8, 1)

        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        [NoScaleOffset] _EmissionMap ("Emission map", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SurfaceVertex
            #pragma fragment SurfaceFragment

            #pragma multi_compile_fragment _ _PLUNDER_TRIPLANAR

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile_instancing

            #include "PlunderspellSurfaceInput.hlsl"
            #include "PlunderspellSurfaceForward.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "PlunderspellSurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "PlunderspellSurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "PlunderspellSurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
}
