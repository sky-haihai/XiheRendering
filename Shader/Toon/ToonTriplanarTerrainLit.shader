Shader "XiheRendering/Toon/ToonTriplanarTerrainLit" {
    Properties {
        _SideTex("XY/ZY Tex", 2D) = "white" {}
        _SideNormalTex ("XY/ZY Normal Texture", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 1)) = 1
        _ColorRampTex ("Color Ramp Texture", 2D) = "white" {}
        _AddLightRampTex ("Additional Light Ramp Texture", 2D) = "white" {}
        _MainLightInfluence ("Overall Light Influence", Range(0, 1)) = 1
        _AdditionalLightsInfluence ("Additional Light Influence", Range(0, 1)) = 1
        _ShadowColor ("Shadow Color", Color) = (0.2,0.2,0.2,0)
        //unity terrain outputs
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3("Layer 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Splat2("Layer 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Splat1("Layer 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Splat0("Layer 0 (R)", 2D) = "grey" {}
        [HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
        [HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
        [HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
        [HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}
        [HideInInspector] _Mask3("Mask 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Mask2("Mask 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask1("Mask 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask0("Mask 0 (R)", 2D) = "grey" {}
        [HideInInspector][Gamma] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 0.5

        _Layer0TileScale("Layer 0 Tile Scale", Range(0,30)) = 15
        _Layer1TileScale("Layer 1 Tile Scale", Range(0,30)) = 15
        _Layer2TileScale("Layer 2 Tile Scale", Range(0,30)) = 15
        _Layer3TileScale("Layer 3 Tile Scale", Range(0,30)) = 15
        _Tiling("XY/ZY Tile Scale", Range(0,1)) = 1
        //        _Blending("Edge Blending", Range( 0.2 , 0.5)) = 0.5
    }
    SubShader {
        Tags {
            "Queue" = "Geometry-100"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "False"
            "TerrainCompatible" = "True"
        }

        Pass {
            Name "UniversalForward"
            Tags {
                "LightMode" = "UniversalForwardOnly"
            }

            // Render State
            Cull Back
            Blend One Zero
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex ComputeVertex
            #pragma fragment ComputeFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

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
            };

            //Properties
            CBUFFER_START(_Terrain)
                float _Layer0TileScale;
                float _Layer1TileScale;
                float _Layer2TileScale;
                float _Layer3TileScale;
                float4 _ColorRampTex_TexelSize;
                float4 _AddLightRampTex_TexelSize;
                float4 _Splat0_TexelSize;
                float4 _Splat1_TexelSize;
                float4 _Splat2_TexelSize;
                float4 _Splat3_TexelSize;
                float4 _Normal0_TexelSize;
                float4 _Normal1_TexelSize;
                float4 _Normal2_TexelSize;
                float4 _Normal3_TexelSize;
                float4 _Mask0_TexelSize;
                float4 _Mask1_TexelSize;
                float4 _Mask2_TexelSize;
                float4 _Mask3_TexelSize;
                float4 _Metallic0_TexelSize;
                float4 _Metallic1_TexelSize;
                float4 _Metallic2_TexelSize;
                float4 _Metallic3_TexelSize;
                float4 _Smoothness0_TexelSize;
                float4 _Smoothness1_TexelSize;
                float4 _Smoothness2_TexelSize;
                float4 _Smoothness3_TexelSize;
                float4 _SideTex_TexelSize;
                float4 _SideNormalTex_TexelSize;
                float4 _Control_TexelSize;
                float _NormalStrength;
                float _MainLightInfluence;
                float _AdditionalLightsInfluence;
                float4 _ShadowColor;
                float _Tiling;
                // float _Blending;

                //unity terrain outputs
                float4 _Metallic0;
                float4 _Metallic1;
                float4 _Metallic2;
                float4 _Metallic3;
                float4 _Smoothness0;
                float4 _Smoothness1;
                float4 _Smoothness2;
                float4 _Smoothness3;
            CBUFFER_END

            // Textures and Samplers
            SamplerState sampler_linear_clamp;

            TEXTURE2D(_SideTex);
            SAMPLER(sampler_SideTex);

            TEXTURE2D(_SideNormalTex);
            SAMPLER(sampler_SideNormalTex);

            TEXTURE2D(_ColorRampTex);
            SAMPLER(sampler_ColorRampTex);

            TEXTURE2D(_AddLightRampTex);
            SAMPLER(sampler_AddLightRampTex);

            TEXTURE2D(_Control);
            SAMPLER(sampler_Control);

            TEXTURE2D(_Splat0);
            SAMPLER(sampler_Splat0);
            TEXTURE2D(_Splat1);
            SAMPLER(sampler_Splat1);
            TEXTURE2D(_Splat2);
            SAMPLER(sampler_Splat2);
            TEXTURE2D(_Splat3);
            SAMPLER(sampler_Splat3);

            TEXTURE2D(_Normal0);
            SAMPLER(sampler_Normal0);
            TEXTURE2D(_Normal1);
            SAMPLER(sampler_Normal1);
            TEXTURE2D(_Normal2);
            SAMPLER(sampler_Normal2);
            TEXTURE2D(_Normal3);
            SAMPLER(sampler_Normal3);

            TEXTURE2D(_Mask0);
            SAMPLER(sampler_Mask0);
            TEXTURE2D(_Mask1);
            SAMPLER(sampler_Mask1);
            TEXTURE2D(_Mask2);
            SAMPLER(sampler_Mask2);
            TEXTURE2D(_Mask3);
            SAMPLER(sampler_Mask3);


            Varyings ComputeVertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv0 = IN.uv0.xy;
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.tangentWS = float4(normalInputs.tangentWS, IN.tangentOS.w);
                return OUT;
            }

            float4 ComputeFragment(Varyings IN) : SV_Target
            {
                float4 result = 0;
                float3 normal0WS = GetNormalWS(_Normal0, sampler_Normal0, IN.normalWS, IN.tangentWS, IN.uv0, _NormalStrength);
                float3 normal1WS = GetNormalWS(_Normal1, sampler_Normal0, IN.normalWS, IN.tangentWS, IN.uv0, _NormalStrength);
                float3 normal2WS = GetNormalWS(_Normal2, sampler_Normal0, IN.normalWS, IN.tangentWS, IN.uv0, _NormalStrength);
                float3 normal3WS = GetNormalWS(_Normal3, sampler_Normal0, IN.normalWS, IN.tangentWS, IN.uv0, _NormalStrength);
                //triplanar
                float4 control = SAMPLE_TEXTURE2D(_Control, sampler_Control, IN.uv0).rgba;
                float3 normal = control.r * normal0WS + control.g * normal1WS + control.b * normal2WS + control.a * normal3WS;
                float3 splat0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, IN.uv0*_Layer0TileScale).rgb;
                float3 splat1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, IN.uv0*_Layer1TileScale).rgb;
                float3 splat2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, IN.uv0*_Layer2TileScale).rgb;
                float3 splat3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, IN.uv0*_Layer3TileScale).rgb;

                float3 top = control.r * splat0.rgb + control.g * splat1.rgb + control.b * splat2.rgb + control.a * splat3.rgb;

                float3 xy = SAMPLE_TEXTURE2D(_SideTex, sampler_SideTex, IN.positionWS.xy*_Tiling).rgb;
                float3 xyNormal = GetNormalWS(_SideNormalTex, sampler_SideNormalTex, IN.normalWS, IN.tangentWS, IN.uv0, _NormalStrength);
                float3 zy = SAMPLE_TEXTURE2D(_SideTex, sampler_SideTex, IN.positionWS.zy*_Tiling).rgb;
                float3 zyNormal = GetNormalWS(_SideNormalTex, sampler_SideNormalTex, IN.normalWS, IN.tangentWS, IN.uv0, _NormalStrength);

                float NdotY = dot(normal, float3(0, 1, 0));
                float NdotX = dot(normal, float3(1, 0, 0));

                //TODO: change to smooth step
                float3 xy_blend_yz_normal = lerp(xyNormal, zyNormal, step(0.5, abs(NdotX)));
                normal = lerp(xy_blend_yz_normal, normal, step(0.5, abs(NdotY)));
                float3 xy_blend_yz = lerp(xy, zy, step(0.5, abs(NdotX)));
                float3 top_blend_side = lerp(xy_blend_yz, top, step(0.5, abs(NdotY)));

                float4 color = float4(top_blend_side, 1);

                return ComputeToonSurface(color, _ShadowColor, normal, IN.positionWS, _MainLightInfluence, _AdditionalLightsInfluence, _ColorRampTex,
                                          sampler_ColorRampTex, _AddLightRampTex, sampler_AddLightRampTex);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}