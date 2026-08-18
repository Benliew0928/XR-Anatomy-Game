Shader "Hospital/Site/World Mapped Opaque"
{
    Properties
    {
        _ColorA("Dark Colour", Color) = (0.1,0.1,0.1,1)
        _ColorB("Light Colour", Color) = (0.2,0.2,0.2,1)
        _DetailTex("Shared Detail", 2D) = "gray" {}
        _DetailNormal("Shared Detail Normal", 2D) = "bump" {}
        _WorldScale("World Scale", Float) = 1
        _Roughness("Roughness", Range(0,1)) = 0.8
        _Metallic("Metallic", Range(0,1)) = 0
        _NormalStrength("Normal Strength", Range(0,1)) = 0.1
        _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Cull [_Cull]
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"

            TEXTURE2D(_DetailTex); SAMPLER(sampler_DetailTex);
            TEXTURE2D(_DetailNormal); SAMPLER(sampler_DetailNormal);

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA;
                half4 _ColorB;
                float4 _DetailTex_ST;
                float4 _DetailNormal_ST;
                float _WorldScale;
                half _Roughness;
                half _Metallic;
                half _NormalStrength;
                float _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                return output;
            }

            float2 ProjectionUv(float3 positionWS, half3 normalWS)
            {
                half3 axis = abs(normalWS);
                if (axis.y >= axis.x && axis.y >= axis.z) return positionWS.xz * _WorldScale;
                if (axis.x >= axis.z) return positionWS.zy * _WorldScale;
                return positionWS.xy * _WorldScale;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                #ifdef LOD_FADE_CROSSFADE
                    LODFadeCrossFade(input.positionCS);
                #endif

                half3 normalWS = normalize(input.normalWS);
                float2 detailUv = ProjectionUv(input.positionWS, normalWS);
                half detail = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, detailUv).r;
                half3 color = lerp(_ColorA.rgb, _ColorB.rgb, detail);
                color *= lerp(half3(1,1,1), input.color.rgb, step(0.001h, dot(input.color.rgb, input.color.rgb)));

                if (abs(normalWS.y) > 0.70h && _NormalStrength > 0.001h)
                {
                    half3 detailNormal = UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormal, sampler_DetailNormal, detailUv));
                    normalWS = normalize(normalWS + half3(detailNormal.x, 0.0h, detailNormal.y) * _NormalStrength);
                }

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS);
                half3 diffuse = ambient + mainLight.color * ndotl * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                half3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDirection = SafeNormalize(mainLight.direction + viewDirection);
                half smoothness = 1.0h - _Roughness;
                half specular = pow(saturate(dot(normalWS, halfDirection)), lerp(4.0h, 96.0h, smoothness));
                half3 specularColor = lerp(0.04h.xxx, color, _Metallic);
                return half4(color * diffuse + specularColor * specular * mainLight.shadowAttenuation, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
