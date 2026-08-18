Shader "Hospital/Site/S5B Instanced Grass Blade"
{
    Properties
    {
        _CoverageMask("S5B Coverage Mask", 2D) = "white" {}
        _RootColor("Root Colour", Color) = (0.025,0.15,0.018,1)
        _TipColor("Tip Colour", Color) = (0.09,0.32,0.055,1)
        _SiteBounds("Site Bounds XZ", Vector) = (-90,-100,180,175)
        _FadeStart("Fade Start", Float) = 10.5
        _FadeEnd("Fade End", Float) = 12
        _WindAmplitude("Wind Tip Amplitude", Float) = 0.01
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+1" }
        LOD 150
        Cull Off
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_CoverageMask); SAMPLER(sampler_CoverageMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _CoverageMask_ST;
                half4 _RootColor;
                half4 _TipColor;
                float4 _SiteBounds;
                float _FadeStart;
                float _FadeEnd;
                float _WindAmplitude;
            CBUFFER_END

            // Deliberately outside UnityPerMaterial: this draw uses explicit GPU instancing.
            float4 _ViewerPosition;
            float _LodLevel;

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
                half heightFactor : TEXCOORD2;
                half4 color : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = input.positionOS.xyz;
                // The immutable S5B patch mesh is Z-up. The runtime matrix performs
                // the FBX root conversion for direct instanced rendering.
                half heightFactor = saturate(positionOS.z / 0.09);
                float3 originalWS = TransformObjectToWorld(positionOS);
                float distanceToViewer = distance(originalWS.xz, _ViewerPosition.xz);
                float fade = _LodLevel < 0.5 ? 1.0 : 1.0 - smoothstep(_FadeStart, _FadeEnd, distanceToViewer);
                positionOS.z *= fade;
                float wind = sin(originalWS.x * 1.73 + originalWS.z * 1.19 + _Time.y * 1.25) * _WindAmplitude;
                positionOS.xy += float2(wind, wind * 0.47) * heightFactor * fade;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.heightFactor = heightFactor;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 maskUv = float2(
                    (input.positionWS.x - _SiteBounds.x) / _SiteBounds.z,
                    (input.positionWS.z - _SiteBounds.y) / _SiteBounds.w);
                half coverage = SAMPLE_TEXTURE2D(_CoverageMask, sampler_CoverageMask, saturate(maskUv)).r;
                clip(coverage - 0.5h);

                // The S5B mesh colours encode blade-to-blade variation, not an albedo.
                // Applying their hue directly can turn tips cyan/white in Unity. Preserve
                // the authored signal as a restrained value modulation of the approved
                // root-to-tip grass palette.
                half vertexSignal = dot(input.color.rgb, half3(0.25h, 0.50h, 0.25h));
                half hasVertexColour = step(0.001h, dot(input.color.rgb, input.color.rgb));
                half vertexVariation = lerp(1.0h, lerp(0.82h, 1.12h, saturate(vertexSignal)), hasVertexColour);
                half3 baseColor = lerp(_RootColor.rgb, _TipColor.rgb, input.heightFactor) * vertexVariation;
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                // Two-sided opaque blades must light consistently from either face.
                half wrappedLight = saturate(abs(dot(normalWS, mainLight.direction)) * 0.5h + 0.5h);
                half3 lighting = saturate(max(SampleSH(normalWS) + mainLight.color * wrappedLight * mainLight.distanceAttenuation,
                    half3(0.58h, 0.58h, 0.58h)));
                return half4(baseColor * lighting, 1.0h);
            }
            ENDHLSL
        }
    }
}
