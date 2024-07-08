Shader "URP/Toon/Stylized/S_StylizedLit" {
    Properties {
        // ------------------------------------- Base
        [Header(Base Settings)]
        [Space(6)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [Space(6)]
        [Toggle(_NORMALMAP)] _NormalMapToggle ("Use Normal Map", Int) = 0
        [NoScaleOffset][Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        [Space(6)]
        [Toggle(_OCCLUSIONMAP_ON)] _OcclusionMapToggle ("Use Occlusion Map (if on)", Int) = 0
        [NoScaleOffset] _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        // ------------------------------------- Work flow
        [Header(Work flow)]
        [Space(6)]
        [Toggle(_SPECULAR_SETUP_ON)] _WorkflowToggle ("Use Metallic (if off) / Specular Gloss (if on)", Int) = 0
        [Toggle(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)] _GlossSource ("Smoothness source, from Albedo Alpha (if on) / from Specular (if off)", Int) = 0
        _ReflProbeIntensity ("ReflProbeIntensity", Range(0.0, 1.0)) = 0.5
        _MetalReflProbeIntensity ("MetalReflProbeIntensity", Range(0.0, 1.0)) = 0.5
        [Toggle(_MAP_ON)] _MapToggle ("Use Metalic Map (if on)", Int) = 0
        [NoScaleOffset] _MetallicGlossMap ("Metallic Map", 2D) = "white" {}
        [NoScaleOffset] _SpecGlossMap ("Specular Map", 2D) = "white" {}
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.5
        // ------------------------------------- Ramp
        [Header(Stylized Diffuse)]
        [Space(6)]
        [Toggle(_RAMPMAP_ON)] _RampMapToggle ("Use Remap Map (if on) / Else (off)", Int) = 0
        _RampMap ("Remap Map", 2D) = "white" {}
        [Toggle] _ShadowStyle ("Shadow Style: Step Shadow (if on) / Custom Shadow (if off)", Int) = 1
        _ToneOffset ("Step Shadow Offest", Range(0.0, 1.0)) = 0.5
        _ShadowLayer ("Custom Shadow Layer", Range(3.0, 16.0)) = 3.0
        _ShadowColor ("Shadow Color", Color) = (0.0, 0.0, 0.0, 1.0)
        // ------------------------------------- Stylized PBR           
        [Header(Stylized Direct Lighting)]
        [Space(6)]
        [ToggleOff] _SpecularHighlights ("Use Specular Highlights (if on) / Else (off)", Int) = 1
        [Toggle] _GGXSpecular ("Use GGX Specular (if on) / Else (off)", Int) = 0
        _SpecColor ("Specular Color", Color) = (0.2, 0.2, 0.2, 1.0)
        _SpecularIntensity ("Specular Intensity", float) = 1.0
        _SpecularLightOffset ("Specular Light Offset", Vector) = (0.0, 0.0, 0.0, 0.0)
        _SpecularThreshold ("Specular Threshold", Range(0.1, 2.0)) = 0.5
        _SpecularSmooth ("Specular Smooth", Range(0, 0.5)) = 0.5

        [Header(Stylized Indirect Lighting)]
        [Space(6)]
        [ToggleOff] _EnvironmentReflections ("Environment Reflections", Int) = 1.0
        [Toggle] _DirectionalFresnel ("Directional Fresnel", Int) = 0
        _FresnelThreshold ("Fresnel Threshold", Range(0.0, 1.0)) = 0.5
        _FresnelSmooth ("Fresnel Smooth", Range(0.0, 0.5) ) = 0.5
        _FresnelIntensity ("Fresnel Intensity", float) = 1.0
        // ------------------------------------- Emission   
        [Header(Emission)]
        [Space(6)]
        [Toggle(_EMISSION)] _EmissionToggle ("Use Emission (if on) / Else off", Int) = 0
        [NoScaleOffset] _EmissionMap ("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0.0, 0.0, 0.0, 1.0)
        // ------------------------------------- Settings 
        [Header(Other Settings)]
        [Space(6)]
        [Toggle(_ALPHATEST_ON)] _AlphaTestToggle ("Alpha Clipping", Int) = 0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("CullMode", Float) = 2.0
        [Toggle(_ENABLESHADOW_ON)] _ReceiveShadows ("Receive Shadows (if on) / Else off", Int) = 1
    }
    // ------------------------------------- SubShader
    SubShader {
        // ------------------------------------- Tags           
        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        // -------------------------------------  Lit Pass
        Pass {
            Name "ForwardLit"
            Tags {
                "LightMode" = "UniversalForward"
            }
            // ------------------------------------- Render State Commands
            Cull [_CullMode]
            // ------------------------------------- HLSLPROGRAM
            HLSLPROGRAM
            #pragma target 2.0
            // ------------------------------------- Shader Stages
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            // ------------------------------------- Material Keywords
            #pragma shader_feature_local_fragment _WORKFLOWTOGGLE_ON
            #pragma shader_feature_local_fragment _MAP_ON
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _ENABLESHADOW_ON
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature _RAMPMAP_ON
            #pragma shader_feature _SHADOWSTYLE_ON
            #pragma shader_feature _SPECULAR_SETUP_ON
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _OCCLUSIONMAP_ON
            #pragma shader_feature _EMISSION
            #pragma shader_feature _ALPHATEST_ON
            // ------------------------------------- Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            // ------------------------------------- Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog
            //-------------------------------------- GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            // ------------------------------------- Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // ------------------------------------- Properties Stages            
            CBUFFER_START(UnityPerMaterial)
                // Base
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _BumpScale;
                float _OcclusionStrength;
                // Work flow
                float4 _SpecColor;
                float _Metallic;
                float _Smoothness;
                // Ramp
                float4 _RampMap_ST;
                float _ToneOffset, _ShadowLayer;
                float4 _ShadowColor;
                // StylizedPBR
                float _SpecularIntensity, _SpecularThreshold, _SpecularSmooth;
                float4 _SpecularLightOffset;
                float _ReflProbeIntensity, _ReflProbeRotation, _MetalReflProbeIntensity;
                float _ShadowThreshold, _ShadowSmooth;
                float _GGXSpecular;
                float _DirectionalFresnel, _FresnelIntensity, _FresnelThreshold, _FresnelSmooth;
                // Emission
                float4 _EmissionColor;
                float _EmissionInt;
                // Settings
                half _Cutoff;
            CBUFFER_END

            TEXTURE2D(_RampMap);
            SAMPLER(sampler_RampMap);
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_MetallicGlossMap);
            SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_SpecGlossMap);
            SAMPLER(sampler_SpecGlossMap);

            // ------------------------------------- Vertex Appdata
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                #ifdef _NORMALMAP
                    float4 tangentOS : TANGENT;
                #endif
                float2 texcoord : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // ------------------------------------- Vertex To Fragment
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;

                #ifdef _NORMALMAP
                    float4 tangentWS : TEXCOORD3;
                #endif

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    half4 fogFactorAndVertexLight : TEXCOORD5; 
                #else
                half fogFactor : TEXCOORD5;
                #endif

                #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
                    float4 shadowCoord : TEXCOORD6;
                #endif

                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);
                #ifdef DYNAMICLIGHTMAP_ON
                    float2  dynamicLightmapUV : TEXCOORD8;
                #endif

                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ------------------------------------- Vertex Shader
            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Vetex
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = positionInputs.positionWS;
                output.positionCS = positionInputs.positionCS;

                // Normal
                #ifdef _NORMALMAP
                    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                    real sign = input.tangentOS.w * GetOddNegativeScale();
                    output.tangentWS = half4(normalInputs.tangentWS.xyz, sign);
                #else
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                #endif
                output.normalWS = normalInputs.normalWS;

                // Fog
                half fogFactor = ComputeFogFactor(output.positionCS.z);
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    half3 vertexLight = VertexLighting(output.positionWS, output.normalWS.xyz);
                    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                #else
                output.fogFactor = fogFactor;
                #endif

                // Shadow
                #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
                    output.shadowCoord = GetShadowCoord(positionInputs);
                #endif

                // UV
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

                // Light
                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                #ifdef DYNAMICLIGHTMAP_ON
                    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

                return output;
            }

            // ------------------------------------- Includes
            #include "SIH_StylizedLit_Methods.hlsl"
            #include "SIH_StylizedLit_Surface.hlsl"
            #include "SIH_StylizedLit_Input.hlsl"
            // ------------------------------------- Fragment Shader
            half4 LitPassFragment(Varyings input) : SV_Target
            {
                // ------------------------------------- SurfaceData / InputData
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData = (SurfaceData)0;
                InitSurfaceData(input, surfaceData);

                InputData inputData = (InputData)0;
                InitInputData(input, surfaceData.normalTS, inputData);
                BRDFData brdfData;
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);
                // ------------------------------------- MainLightShadow
                half4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 mainlightColor = mainLight.color;
                half3 mainlightDir = normalize(mainLight.direction);
                #ifdef _ENABLESHADOW_ON
                half mainlightShadow = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                #else
                half mainlightShadow = 1;
                #endif
                // ------------------------------------- PreTextures
                half4 var_BaseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half var_EmissionMap = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv);
                // -------------------------------------  FinalRGB
                half3 rampDiffuse = CalculateStylizedDiffuse(mainLight, inputData.normalWS, input.uv, _ToneOffset, _ShadowLayer, _ShadowColor);
                rampDiffuse *= mainlightShadow * var_BaseMap;

                half3 emission = surfaceData.emission;

                half ndotl = LinearStep(_ShadowThreshold - _ShadowSmooth, _ShadowThreshold + _ShadowSmooth, dot(mainLight.direction, inputData.normalWS) * 0.5 + 0.5);

                half3 color = 0;
                color += MyStylizedGI(brdfData, rampDiffuse, inputData.bakedGI, surfaceData.occlusion, inputData.normalWS, inputData.viewDirectionWS, _Metallic,
         lerp(1, ndotl, _DirectionalFresnel));
                color += MyLightingStylizedPhysicallyBased(brdfData, rampDiffuse, mainLight, inputData.normalWS, inputData.viewDirectionWS);
                // -------------------------------------  Fog
                color = MixFog(color.rgb, inputData.fogCoord);

                return half4(color + emission, surfaceData.alpha);
            }
            ENDHLSL
        }
        // ------------------------------------- Shadow Pass
        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode" = "ShadowCaster"
            }
            // ------------------------------------- Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_CullMode]
            // ------------------------------------- HLSLPROGRAM
            HLSLPROGRAM
            #pragma target 2.0
            // ------------------------------------- Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            //-------------------------------------- GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            // ------------------------------------- Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            // ------------------------------------- Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            // ------------------------------------- Includes

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        // -------------------------------------  Depth Pass
        Pass {
            Name "DepthOnly"
            Tags {
                "LightMode" = "DepthOnly"
            }
            // ------------------------------------- Render State Commands
            ZWrite On
            ColorMask R
            Cull [_CullMode]
            // ------------------------------------- HLSLPROGRAM
            HLSLPROGRAM
            #pragma target 2.0
            // ------------------------------------- Shader Stages
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            // ------------------------------------- Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            // ------------------------------------- Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            //--------------------------------------  GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            // ------------------------------------- Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
    //FallBack "Hidden/Universal Render Pipeline/FallbackError"
}