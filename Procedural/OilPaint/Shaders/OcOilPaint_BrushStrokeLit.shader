Shader "OcOilPaint/BrushStrokeLit" {
    Properties {
        _BaseMap ("Brush Stroke Mask", 2D) = "white" {}
        _YCount ("Row Count", Float) = 1.0
        _XCount ("Column Count", Float) = 1.0
        _ScaleMax ("Max Scale", Float) = 1.0
        _ScaleMin ("Min Scale", Float) = 0.0
        _HeightOffset ("Bumpiness", Float) = 0.0
        _AlphaCutoff ("Alpha Cutoff", Range(0.0,1.0)) = 0.5
        _RotationRandomness ("Rotation Randomness", Float) = 0.0

        [Toggle(_NORMALMAP)]_EnableBumpMap ("Enable Normal Map", Int) = 1
        [NoScaleOffset][Normal]_BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Intensity", Float) = 1.0

        [ToggleOff(_SPECULARHIGHLIGHTS_OFF)] _EnableSpecular ("Enable Specular", Int) = 1

        _Metallic ("Metallic", Range(0.0,1.0)) = 0.0
        _Smoothness ("Smoothness", Range(0.0,1.0)) = 0.5

        _RampTex ("Ramp Map", 2D) = "white" {}

        [Toggle]_UseEnvReflection ("Enable Environment Reflection", Int) = 0
        _FresnelIntensity ("Fresnel Intensity", float) = 1.0
        _FresnelThreshold ("Fresnel Threshold", Range(0.0,1.0)) = 0.5
        _FresnelSmoothness ("Fresnel Smoothness", Range(0.0,0.5) ) = 0.5
        _FresnelColor ("Fresnel Color", Color) = (1.0,1.0,1.0,1.0)

        [Toggle]_UseEmission ("Enable Emission", Int) = 0
        [HDR] _EmissionColor ("Emission Color", Color) = (0.0, 0.0, 0.0, 1.0)

        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("CullMode", Float) = 2.0
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("Blend mode Source", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDst ("Blend mode Destination", Int) = 0
        [Toggle] _ReceiveShadows ("Receive Shadows", Int) = 1
    }
    SubShader {

        Tags {
            "RenderPipeline"="UniversalRenderPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass {
            Name "BillboardForward"
            Blend [_BlendSrc] [_BlendDst]
            Cull[_CullMode]

            Tags {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex BrushStrokeVertex
            #pragma fragment BrushStrokeFragment
            #pragma target 4.5
            // ------------------------------------- Material Keywords
            #pragma shader_feature_local _USEENVREFLECTION_ON
            #pragma shader_feature_local_fragment _RECEIVESHADOWS_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _USEEMISSION_ON
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF

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
            //-------------------------------------- GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "OcOilPaint_Functions.hlsl"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4x4, _TRSMatrix)
            UNITY_INSTANCING_BUFFER_END(Props)

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _XCount;
                float _YCount;
                float _ScaleMax;
                float _ScaleMin;
                float _HeightOffset;
                float _AlphaCutoff;
                float _RotationRandomness;

                float _BumpScale;
            
                float _Metallic;
                float _Smoothness;

                float4 _RampTex_ST;

                float _FresnelIntensity;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float3 _FresnelColor;

                float4 _EmissionColor;
            CBUFFER_END

            TEXTURE2D(_RampTex);
            SAMPLER(sampler_RampTex);

            struct StrokeData
            {
                float3 position;
                float3 normal;
                float4 tangent;
                float4 color;
            };

            StructuredBuffer<StrokeData> _StrokeDataBuffer;

            struct BrushStrokeAttributes
            {
                float4 positionOS : POSITION;
                float4 uv : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct BrushStrokeVaryings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS: TEXCOORD1;
                #ifdef _NORMALMAP
                float3 tangentWS: TEXCOORD2;
                float3 binormalWS: TEXCOORD3;
                #endif
                float4 vertexColor : TEXCOORD4;
                float3 positionWS : TEXCOORD5;
                #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
                float4 shadowCoord : TEXCOORD6;
                #endif
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);
                #ifdef DYNAMICLIGHTMAP_ON
                float2  dynamicLightmapUV : TEXCOORD8;
                #endif

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            VertexPositionInputs GetVertexPositionInputs(float3 positionOS, StrokeData strokeData, uint instanceID, float4x4 _TRSMatrix)
            {
                VertexPositionInputs o;

                float4 vertexColor = strokeData.color;
                float3 strokePosOS = strokeData.position;
                float3 normalOS = strokeData.normal;
                float3 tangentOS = strokeData.tangent.xyz;
                tangentOS = RotateAroundAxis(tangentOS, normalOS, (Random01(instanceID * 123) * 2 - 1) * _RotationRandomness * 3.1415926);
                float3 binormal = -cross(normalOS, tangentOS);

                float3 vertexOffset = positionOS * float3(1, 1, 1);

                float3 x = tangentOS * vertexOffset.x;
                float3 y = normalOS * vertexOffset.y;
                float3 z = binormal * vertexOffset.z;
                vertexOffset = x + y + z + Random01(instanceID) * _HeightOffset * normalOS;
                float3 worldPos = strokePosOS + vertexOffset * lerp(_ScaleMin, _ScaleMax, vertexColor.a);
                worldPos = mul(_TRSMatrix, float4(worldPos, 1)).xyz;

                o.positionCS = TransformWorldToHClip(worldPos);
                o.positionWS = worldPos;
                o.positionVS = TransformWorldToView(worldPos);

                float4 ndc = o.positionCS * 0.5f;
                o.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
                o.positionNDC.zw = o.positionCS.zw;

                return o;
            }

            BrushStrokeVaryings BrushStrokeVertex(BrushStrokeAttributes IN, uint instanceID : SV_InstanceID)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                StrokeData strokeData = _StrokeDataBuffer[instanceID];
                VertexPositionInputs inputData = GetVertexPositionInputs(IN.positionOS.xyz, strokeData, instanceID, _TRSMatrix);
                BrushStrokeVaryings o;
                o.positionHCS = inputData.positionCS;
                o.positionWS = inputData.positionWS;
                o.normalWS = normalize(mul(_TRSMatrix, float4(strokeData.normal, 0)).xyz);
                #ifdef _NORMALMAP
                o.tangentWS = normalize(mul(_TRSMatrix, strokeData.tangent).xyz);
                o.binormalWS = normalize(cross(o.normalWS, o.tangentWS));
                #endif
                o.vertexColor = strokeData.color;

                //defined by vertex color alpha channel
                float uvY = IN.uv.y / _YCount + floor(clamp(strokeData.color.a, 0, 0.999999) * _YCount) / _YCount;
                //random pick one along x axis
                float uvX = IN.uv.x / _XCount + floor(clamp(Random01(instanceID), 0, 0.999999) * _XCount) / _XCount;
                o.uv = float2(uvX, uvY);

                #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
                    o.shadowCoord = GetShadowCoord(inputData);
                #endif

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                #ifdef DYNAMICLIGHTMAP_ON
                    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                OUTPUT_SH(o.normalWS.xyz, o.vertexSH);
                return o;
            }

            half3 MyLightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat,
                  half3 lightColor, half3 lightDirectionWS, half lightAttenuation,
                  half3 normalWS, half3 viewDirectionWS,
                  half clearCoatMask, bool specularHighlightsOff)
            {
                half NdotL = saturate(dot(normalWS, lightDirectionWS));
                half3 radiance = lightColor * (lightAttenuation * NdotL);

                half3 brdf = brdfData.diffuse;
                #ifndef _SPECULARHIGHLIGHTS_OFF
                [branch] if (!specularHighlightsOff)
                {
                    brdf += brdfData.specular * DirectBRDFSpecular(brdfData, normalWS, lightDirectionWS, viewDirectionWS);
                    // return brdfData.specular * DirectBRDFSpecular(brdfData, normalWS, lightDirectionWS, viewDirectionWS);
                }
                #else
                return float3(1,0,1);
                #endif // _SPECULARHIGHLIGHTS_OFF

                return brdf * radiance;
            }

            half3 MyLightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat, Light light, half3 normalWS, half3 viewDirectionWS, half clearCoatMask,
                bool specularHighlightsOff)
            {
                return MyLightingPhysicallyBased(brdfData, brdfDataClearCoat, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS,
                    viewDirectionWS, clearCoatMask, specularHighlightsOff);
            }

            half3 MyLightingPhysicallyBased(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS)
            {
                #ifdef _SPECULARHIGHLIGHTS_OFF
                bool specularHighlightsOff = true;
                #else
                bool specularHighlightsOff = false;
                #endif
                const BRDFData noClearCoat = (BRDFData)0;
                return MyLightingPhysicallyBased(brdfData, noClearCoat, light, normalWS, viewDirectionWS, 0.0, specularHighlightsOff);
            }


            float4 BrushStrokeFragment(BrushStrokeVaryings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 mask = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                clip(mask.a - _AlphaCutoff);

                float3 normalWS = IN.normalWS;
                #ifdef _NORMALMAP
                normalWS = SampleNormal(IN.uv, _BumpMap, sampler_BumpMap,-_BumpScale);
                normalWS = normalWS.x * IN.tangentWS + normalWS.y * IN.binormalWS + normalWS.z * IN.normalWS;
                #endif

                //shadow
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                //view direction
                float3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                BRDFData brdfData;
                InitializeBRDFData(IN.vertexColor, _Metallic,0, _Smoothness, mask.a, brdfData);

                // return float4(brdfData.specular, 1);
                
                //direct lighting
                float3 directColor = LightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirectionWS);
                return float4(directColor, 1);


                half3 bakedGI = 0;
                #if defined(DYNAMICLIGHTMAP_ON)
                bakedGI = SAMPLE_GI(IN.staticLightmapUV, IN.dynamicLightmapUV, IN.vertexSH, normalWS);
                #else
                bakedGI = SAMPLE_GI(input.staticLightmapUV, IN.vertexSH, IN.normalWS);
                #endif

                float3 indirectColor = GlobalIllumination(brdfData, bakedGI, 1, normalWS, viewDirectionWS);

                return float4(directColor + indirectColor, mask.a);
            }
            ENDHLSL
        }
    }
}