Shader "XiheRendering/Toon/Toon3DLitDepthOnly" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _CutoffThreshold ("Cutoff Threshold", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
            "Queue"="Geometry+100"
        }

        Pass {
            Name "DepthOnly"

            Tags {
                "LightMode"="UniversalForwardOnly"
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
                float _CutoffThreshold;
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
    }
}