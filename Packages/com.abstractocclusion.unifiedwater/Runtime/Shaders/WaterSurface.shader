Shader "AbstractOcclusion/UnifiedWater/WaterSurface"
{
    Properties
    {
        _HeightScale ("Height Scale (m per sim unit)", Float) = 1.0
        _DeepColor ("Deep Color", Color) = (0.02, 0.1, 0.15, 1.0)
        _AbsorptionDensity ("Absorption Density (per m)", Range(0.0, 2.0)) = 0.35
        _RefractionStrength ("Refraction Strength", Range(0.0, 0.2)) = 0.05
        _EdgeFade ("Edge Fade (m)", Range(0.01, 3.0)) = 0.5
        _SpecularColor ("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Smoothness ("Specular Smoothness", Range(1.0, 256.0)) = 64.0
        _FresnelColor ("Fresnel Color", Color) = (0.6, 0.8, 0.9, 1.0)
        _FresnelPower ("Fresnel Power", Range(0.5, 10.0)) = 5.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "UnifiedWaterForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "UnifiedWaterField.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _HeightScale;
                float4 _DeepColor;
                float _AbsorptionDensity;
                float _RefractionStrength;
                float _EdgeFade;
                float4 _SpecularColor;
                float _Smoothness;
                float4 _FresnelColor;
                float _FresnelPower;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 fieldUv : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float2 fieldUv = UnifiedWater_WorldToUv(positionWS);
                positionWS.y += UnifiedWater_SampleHeight(fieldUv) * _HeightScale;

                output.positionWS = positionWS;
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.fieldUv = fieldUv;
                return output;
            }

            // Eye-space thickness of water between this surface fragment and the scene behind it.
            float WaterDepth(float2 screenUv, float surfaceEyeDepth)
            {
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUv), _ZBufferParams);
                return max(0.0, sceneEyeDepth - surfaceEyeDepth);
            }

            // Warp the background by the surface normal. The offset scales with water depth so shallow
            // water and the shoreline barely refract (killing the ghost/double-image there), and the warp
            // is skipped for anything nearer than the surface so foreground objects can't bleed across.
            float3 RefractedSceneColor(float2 screenUv, float3 normalWS, float surfaceEyeDepth, float waterDepth)
            {
                float depthScale = saturate(waterDepth / _EdgeFade);
                float2 refractedUv = screenUv + normalWS.xz * (_RefractionStrength * depthScale);
                float refractedEyeDepth = LinearEyeDepth(SampleSceneDepth(refractedUv), _ZBufferParams);
                float2 sampleUv = refractedEyeDepth < surfaceEyeDepth ? screenUv : refractedUv;
                return SampleSceneColor(sampleUv);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = UnifiedWater_SampleNormal(input.fieldUv);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                float2 screenUv = GetNormalizedScreenSpaceUV(input.positionHCS);

                float surfaceEyeDepth = LinearEyeDepth(input.positionHCS.z, _ZBufferParams);
                float waterDepth = WaterDepth(screenUv, surfaceEyeDepth);

                float3 refracted = RefractedSceneColor(screenUv, normalWS, surfaceEyeDepth, waterDepth);

                // Beer-Lambert: the deeper the water, the more the background is absorbed into deep colour.
                float transmission = exp(-waterDepth * _AbsorptionDensity);
                float3 body = lerp(_DeepColor.rgb, refracted, transmission);

                Light mainLight = GetMainLight();
                float3 lightColor = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float specularTerm = pow(saturate(dot(normalWS, halfDir)), _Smoothness);
                float3 specular = _SpecularColor.rgb * lightColor * specularTerm;

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                float3 rim = _FresnelColor.rgb * fresnel;

                // Fade the surface highlights to nothing at the shoreline so the waterline has no seam.
                float shoreFade = saturate(waterDepth / _EdgeFade);
                float3 color = body + (specular + rim) * shoreFade;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
