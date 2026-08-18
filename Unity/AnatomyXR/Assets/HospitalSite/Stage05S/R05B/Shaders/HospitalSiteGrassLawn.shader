Shader "Hospital/Site/S5B Grass Lawn"
{
    Properties
    {
        _BaseMap("Grass005 Base Colour", 2D) = "white" {}
        _NormalMap("Grass005 NormalGL", 2D) = "bump" {}
        _RoughnessMap("Grass005 Roughness", 2D) = "white" {}
        _OcclusionMap("Grass005 AO", 2D) = "white" {}
        _MacroTex("Shared Macro Variation", 2D) = "gray" {}
        _BaseTileMetres("Base Tile Metres", Float) = 2
        _MacroTileMetres("Macro Tile Metres", Float) = 14
        _NormalStrength("Normal Strength", Range(0,1)) = 0.32
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 250
        Cull Back
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_MacroTex); SAMPLER(sampler_MacroTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NormalMap_ST;
                float4 _RoughnessMap_ST;
                float4 _OcclusionMap_ST;
                float4 _MacroTex_ST;
                float _BaseTileMetres;
                float _MacroTileMetres;
                half _NormalStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.positionWS.xz / max(_BaseTileMetres, 0.01);
                float2 uvRotated = float2(-uv.y, uv.x) + float2(0.371, 0.619);
                half3 baseA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
                half3 baseB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvRotated).rgb;
                half macro = SAMPLE_TEXTURE2D(_MacroTex, sampler_MacroTex,
                    input.positionWS.xz / max(_MacroTileMetres, 0.01)).r;
                half3 color = lerp(baseA, baseB, saturate(0.25h + macro * 0.45h));

                half3 tangentNormal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv));
                half3 normalWS = normalize(input.normalWS +
                    half3(tangentNormal.x, 0.0h, tangentNormal.y) * _NormalStrength);
                half roughnessSample = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv).r;
                half roughness = lerp(0.72h, 0.90h, roughnessSample);
                half occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).r;

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS) * lerp(0.65h, 1.0h, occlusion);
                half3 diffuse = ambient + mainLight.color * ndotl * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDirection = SafeNormalize(mainLight.direction + viewDirection);
                half specular = pow(saturate(dot(normalWS, halfDirection)), lerp(4.0h, 32.0h, 1.0h - roughness));
                return half4(color * diffuse + 0.025h.xxx * specular, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
