Shader "AbstractOcclusion/UnifiedWater/WaterSurface"
{
    Properties
    {
        _WaterTint ("Water Tint", Color) = (0.05, 0.25, 0.35, 0.75)
        _HeightScale ("Height Scale (m per sim unit)", Float) = 1.0
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
            #include "UnifiedWaterField.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _WaterTint;
                float _HeightScale;
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

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = UnifiedWater_SampleNormal(input.fieldUv);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);

                Light mainLight = GetMainLight();
                float3 lightColor = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
                float diffuseTerm = saturate(dot(normalWS, mainLight.direction));
                float3 ambient = SampleSH(normalWS);
                float3 diffuse = _WaterTint.rgb * (ambient + lightColor * diffuseTerm);

                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float specularTerm = pow(saturate(dot(normalWS, halfDir)), _Smoothness);
                float3 specular = _SpecularColor.rgb * lightColor * specularTerm;

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                float3 color = diffuse + specular + _FresnelColor.rgb * fresnel;

                float alpha = saturate(_WaterTint.a + fresnel);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
