Shader "XiheRendering/Toon/Toon3DLit" {
    Properties {
        [MainTexture] _MainTex ("Albedo Texture", 2D) = "white" {}
        _CutoffThreshold("_Cutoff Threshold",Range(0,1)) = 0.5
        _NormalTex ("Normal Texture", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 1)) = 1
        _RoughnessTex ("Roughness Texture", 2D) = "white" {}
        _ColorRampTex ("Color Ramp Texture", 2D) = "white" {}
        _AddLightRampTex ("Additional Light Ramp Texture", 2D) = "white" {}
        _MainLightInfluence ("Main Light Influence", Range(0, 1)) = 1
        _AdditionalLightsInfluence ("Additional Light Influence", Range(0, 5)) = 1
        [Toggle]_CastShadows ("Cast Shadows", Float) = 1
        _ShadowColor ("Shadow Color", Color) = (0.2,0.2,0.2,0)
        [Toggle]_Silhouette ("Enable Silhouette", Float) = 0
        _SilhouetteColor ("Silhouette Color", Color) = (0,0,0,1)
        _SilhouetteMax ("Silhouette Start Distance", Float) = 6
        _SilhouetteMin ("Silhouette Max Distance", Float) = 4
        [Toggle]_AntiOcclusion ("Enable Anti-Occlusion", Float) = 0
        _AntiOcclusionMax ("Solid Distance", Float) = 6
        _AntiOcclusionMin ("Transparent Distance", Float) = 4
    }
    SubShader {

        Tags {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalRenderPipeline"
        }

        Pass {
            Name "UniversalForward"
            Tags {
                "LightMode" = "UniversalForwardOnly"
            }

            // Render State
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex ComputeVertex
            #pragma fragment ComputeFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile_local_fragment _ _SILHOUETTE_ON
            #pragma multi_compile_local_fragment _ _ANTIOCCLUSION_ON

            #include "ToonFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 uv0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv0 : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float2 silhouette : TEXCOORD4; //silhouette factor and dithering factor
                float4 screenPos : TEXCOORD5;
            };

            //Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _NormalTex_TexelSize;
                float4 _RoughnessTex_TexelSize;
                float4 _ColorRampTex_TexelSize;
                float4 _AddLightRampTex_TexelSize;
                float _CutoffThreshold;
                float _NormalStrength;
                float _MainLightInfluence;
                float _AdditionalLightsInfluence;
                float _AdditionalLightsColorMultiplier;
                float4 _ShadowColor;
                float4 _SilhouetteColor;
                float _SilhouetteMin;
                float _SilhouetteMax;
                float _AntiOcclusionMax;
                float _AntiOcclusionMin;
            CBUFFER_END

            // Textures and Samplers
            SamplerState sampler_linear_clamp;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);

            TEXTURE2D(_RoughnessTex);
            SAMPLER(sampler_RoughnessTex);

            TEXTURE2D(_ColorRampTex);
            SAMPLER(sampler_ColorRampTex);

            TEXTURE2D(_AddLightRampTex);
            SAMPLER(sampler_AddLightRampTex);

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            // 4x4 Dithering pattern
            static const float4x4 ditherPattern = float4x4(
                0.0625, 0.5625, 0.1875, 0.6875,
                0.8125, 0.3125, 0.9375, 0.4375,
                0.25, 0.75, 0.125, 0.625,
                0.875, 0.375, 0.0, 0.5
            );

            float Dithering(float2 screenPos)
            {
                // Find position in dither pattern based on screen position
                int2 pos = int2(fmod(screenPos.x, 4), fmod(screenPos.y, 4));
                return ditherPattern[pos.x][pos.y];
            }

            Varyings ComputeVertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv0 = TRANSFORM_TEX(IN.uv0.xy, _MainTex);
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.tangentWS = float4(normalInputs.tangentWS, IN.tangentOS.w);
                float3 originWS = TransformObjectToWorld(float3(0, 0, 0));
                originWS.x = _WorldSpaceCameraPos.x;
                originWS.y = _WorldSpaceCameraPos.y;
                float camDistance = originWS.z - _WorldSpaceCameraPos.z;
                float silhouetteFactor = (clamp(camDistance, _SilhouetteMin, _SilhouetteMax) - _SilhouetteMin) / (_SilhouetteMax - _SilhouetteMin);
                float ditheringFactor = (clamp(camDistance, _AntiOcclusionMin, _AntiOcclusionMax) - _AntiOcclusionMin) / (_AntiOcclusionMax - _AntiOcclusionMin);
                OUT.silhouette = float2(silhouetteFactor, ditheringFactor);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            float4 ComputeFragment(Varyings IN) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv0).rgba;
                clip(color.a - _CutoffThreshold);
                float3 normalWS = GetNormalWS(_NormalTex, sampler_NormalTex, IN.normalWS, IN.tangentWS, IN.uv0, _NormalStrength);

                float4 result = ComputeToonSurface(color,
                                   _ShadowColor,
                                   normalWS,
                                   IN.positionWS,
                                   _MainLightInfluence,
                                   _AdditionalLightsInfluence,
                                   _ColorRampTex,
                                   sampler_ColorRampTex,
                                   _AddLightRampTex,
                                   sampler_AddLightRampTex);

                #ifdef _ANTIOCCLUSION_ON
                float dither = Dithering(IN.positionHCS.xy);
                clip(IN.silhouette.y - 1 + dither+0.001);
                #endif

                #ifdef _SILHOUETTE_ON
                result = IN.silhouette.x * result + (1 - IN.silhouette.x) * _SilhouetteColor;
                #endif

                return result;
            }
            ENDHLSL
        }

        Pass {
            Name "DepthOnly"

            Tags {
                "LightMode"="UniversalForwardOnly"
                "Queue"="Geometry+100"
            }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ComputeVertex
            #pragma fragment ComputeFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _NormalTex_TexelSize;
                float4 _RoughnessTex_TexelSize;
                float4 _ColorRampTex_TexelSize;
                float4 _AddLightRampTex_TexelSize;
                float _CutoffThreshold;
                float _NormalStrength;
                float _MainLightInfluence;
                float _AdditionalLightsInfluence;
                float _AdditionalLightsColorMultiplier;
                float4 _ShadowColor;
                float4 _SilhouetteColor;
                float _SilhouetteMin;
                float _SilhouetteMax;
                float _AntiOcclusionMax;
                float _AntiOcclusionMin;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 uv0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv0 : TEXCOORD1;
            };

            Varyings ComputeVertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // OUT.uv0 = TRANSFORM_TEX(IN.uv0.xy, _MainTex);
                OUT.uv0 = IN.uv0.xy;
                return OUT;
            }

            float4 ComputeFragment(Varyings IN) : SV_Target
            {
                float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv0).a;
                clip(a - _CutoffThreshold);
                return 0;
            }
            ENDHLSL
        }

        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _CASTSHADOWS_ON

            // -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
            // For Directional lights, _LightDirection is used when applying shadow Normal Bias.
            // For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
            float3 _LightDirection;
            float3 _LightPosition;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _NormalTex_TexelSize;
                float4 _RoughnessTex_TexelSize;
                float4 _ColorRampTex_TexelSize;
                float4 _AddLightRampTex_TexelSize;
                float _CutoffThreshold;
                float _NormalStrength;
                float _MainLightInfluence;
                float _AdditionalLightsInfluence;
                float _AdditionalLightsColorMultiplier;
                float4 _ShadowColor;
                float4 _SilhouetteColor;
                float _SilhouetteMin;
                float _SilhouetteMax;
                float _AntiOcclusionMax;
                float _AntiOcclusionMin;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                output.uv = input.texcoord;
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                #ifndef _CASTSHADOWS_ON
                discard;
                #endif
                float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(a - _CutoffThreshold);
                return 0;
            }
            ENDHLSL
        }
    }
}