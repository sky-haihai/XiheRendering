Shader "XiheRendering/PlaneDepthFog" {
    Properties {
        _Color("Color", Color) = (1,1,1,1)
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _Intensity("Intensity", Range(0, 1)) = 0.5
        _NoiseScale("Noise Scale", Range(0, 1)) = 0.5
        _NoiseSpeed("Noise Speed", Range(-1, 1)) = 0
    }

    SubShader {
        Tags {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalRenderPipeline"
        }

        Pass {
            Name "UniversalForward"
            Tags {
                "LightMode" = "UniversalForward"
            }
            // Render State
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ColorMask RGBA
            Cull Off

            HLSLPROGRAM
            #pragma vertex ComputeVertex
            #pragma fragment ComputeFragment

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
                float4 screenPos : TEXCOORD2;
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

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            Varyings ComputeVertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv0 = IN.uv0.xy;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);

                return OUT;
            }

            float4 ComputeFragment(Varyings IN) : SV_Target
            {
                half depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, IN.screenPos.xy / IN.screenPos.w).r, _ZBufferParams);
                float fog = depth - IN.screenPos.w;

                //noise mask
                float2 noiseUV = IN.uv0 * _NoiseScale;
                noiseUV.x += _Time.x * _NoiseSpeed;
                float noiseMask = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                float4 result = 0;
                result.rgb = _Color.rgb;
                result.a = saturate(fog * _Intensity * noiseMask) * _Color.a;

                //blend
                return result;
            }
            ENDHLSL
        }
    }

}