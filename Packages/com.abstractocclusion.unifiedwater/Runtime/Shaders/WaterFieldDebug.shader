// Greyscale debug view of one water field layer slice. Samples slice 0 of the Dynamic
// Texture2DArray and maps its red channel (height) through a scale into 0..1 greyscale. Drawn as a
// full-screen triangle by CoreUtils.DrawFullScreen. Hidden — it is a dev tool, not an authored material.
Shader "Hidden/AbstractOcclusion/UnifiedWater/WaterFieldDebug"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "WaterFieldDebugView"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_ARRAY(_DebugField);
            SAMPLER(sampler_DebugField);
            float _DebugValueScale;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Slice 0 = the finest cascade (the only slice for a bounded pond).
                float height = SAMPLE_TEXTURE2D_ARRAY(_DebugField, sampler_DebugField, input.uv, 0).r;
                float value = saturate(height * _DebugValueScale);
                return half4(value, value, value, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
