// Sparks rising off a fire: soft round additive points, tinted by the particle colour.
Shader "Plunderspell/Ember"
{
    Properties
    {
        [HDR] _Color ("Colour", Color) = (4, 1.6, 0.5, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+11" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
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

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float r = length(input.uv - 0.5) * 2.0;
                float spot = saturate(1.0 - r);
                spot *= spot;
                return half4(_Color.rgb * input.color.rgb * input.color.a * spot, 0);
            }
            ENDHLSL
        }
    }
}
