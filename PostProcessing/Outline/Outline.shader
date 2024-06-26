Shader "Hidden/XiheRendering/Outline" {
    Properties {
        [HideInInspector]_MainTex("Main Tex", 2D) = "white" {}
    }

    SubShader {
        Tags {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalRenderPipeline"
        }

        Pass {
            Name "UniversalForward"

            // Render State
            Cull Back
            Blend One Zero
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex ComputeVertex
            #pragma fragment ComputeFragment

            //keywords
            #pragma multi_compile_local _ _DEBUG_ON

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 uv0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv0 : TEXCOORD1;
                float2 screenPos : TEXCOORD2;
            };

            //Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                float4 _Color;
                float _Thickness;
                float _Threshold;
            CBUFFER_END

            // Textures and Samplers
            SamplerState sampler_linear_repeat;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            Varyings ComputeVertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv0 = IN.uv0.xy;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS).xy;

                return OUT;
            }

            float4 ComputeFragment(Varyings IN) : SV_Target
            {
                //horizontal
                // -1    0   1
                // -2    0   2
                // -1    0   1

                //vertical
                // 1    2   1
                // 0    0   0
                //-1   -2  -1

                //p6    p7    p8
                //p3    p4    p5
                //p0    p1    p2

                float2 p0_uv = (IN.screenPos.xy * _ScreenParams.xy + float2(-1 * _Thickness, -1 * _Thickness)) / _ScreenParams.xy;
                float2 p1_uv = (IN.screenPos.xy * _ScreenParams.xy + float2(0, -1 * _Thickness)) / _ScreenParams.xy;
                float2 p2_uv = (IN.screenPos.xy * _ScreenParams.xy + float2(1 * _Thickness, -1 * _Thickness)) / _ScreenParams.xy;
                float2 p3_uv = (IN.screenPos.xy * _ScreenParams.xy + float2(-1 * _Thickness, 0)) / _ScreenParams.xy;
                float2 p5_uv = (IN.screenPos.xy * _ScreenParams.xy + float2(1 * _Thickness, 0)) / _ScreenParams.xy;
                float2 p6_uv = (IN.screenPos.xy * _ScreenParams.xy + float2(-1 * _Thickness, 1 * _Thickness)) / _ScreenParams.xy;
                float2 p7_uv = (IN.screenPos.xy * _ScreenParams.xy + float2(0, 1 * _Thickness)) / _ScreenParams.xy;
                float2 p8_uv = (IN.screenPos.xy * _ScreenParams.xy + float2(1 * _Thickness, 1 * _Thickness)) / _ScreenParams.xy;

                float depth_p0 = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, p0_uv), _ZBufferParams);
                float depth_p1 = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, p1_uv), _ZBufferParams);
                float depth_p2 = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, p2_uv), _ZBufferParams);
                float depth_p3 = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, p3_uv), _ZBufferParams);
                float depth_p5 = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, p5_uv), _ZBufferParams);
                float depth_p6 = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, p6_uv), _ZBufferParams);
                float depth_p7 = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, p7_uv), _ZBufferParams);
                float depth_p8 = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, p8_uv), _ZBufferParams);

                float dx = depth_p0 * -1 + depth_p2 * 1 + depth_p3 * -2 + depth_p5 * 2 + depth_p6 * -1 + depth_p8 * 1;
                float dy = depth_p0 * -1 + depth_p1 * -2 + depth_p2 * -1 + depth_p6 * 1 + depth_p7 * 2 + depth_p8 * 1;
                float d = SafeSqrt(dx * dx + dy * dy);

                //edge mask
                float mask = step(_Threshold, d);


                //blend
                float4 screenColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv0);

                float4 result = float4(lerp(screenColor, _Color, mask * _Color.a));

                #ifdef  _DEBUG_ON
                return mask;
                #else
                return result;
                #endif
            }
            ENDHLSL
        }
    }

}