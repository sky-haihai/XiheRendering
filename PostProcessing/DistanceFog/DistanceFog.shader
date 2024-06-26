Shader "Hidden/XiheRendering/DistanceFog" {
    Properties {
        [HideInInspector]_MainTex("Main Tex", 2D) = "white" {}
        [HideInInspector]_NoiseTex("Noise Texture", 2D) = "white" {}
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
                float4 _NoiseTex_TexelSize;
                float4 _Color;
                float _Intensity;
                float _Exponent;
                float _NoiseScale;
                float _NoiseSpeed;
                float _DistanceOffset;
            CBUFFER_END

            // Textures and Samplers
            SamplerState sampler_linear_repeat;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            TEXTURE2D(_CameraDepthAttachment);
            SAMPLER(sampler_CameraDepthAttachment);

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
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthAttachment, sampler_CameraDepthAttachment, IN.uv0);
                depth = Linear01Depth(depth, _ZBufferParams);
                depth += _DistanceOffset;
                depth = pow(depth, _Exponent) * _Intensity;

                //noise mask
                float2 noiseUV = IN.uv0 * _NoiseScale;
                noiseUV.x += _Time.x * _NoiseSpeed;
                float noiseMask = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                float mask = depth * noiseMask;
                mask = clamp(mask, 0, 1);

                //blend
                float4 screenColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv0);

                float4 result = float4(lerp(screenColor.rgb, _Color, mask), 1);

                #ifdef _DEBUG_ON
                return mask;
                #else
                return result;
                #endif
            }
            ENDHLSL
        }
    }

}