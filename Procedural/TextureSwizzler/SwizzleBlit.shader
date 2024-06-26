Shader "Hidden/XiheRendering/SwizzleBlit" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _RChannelTex ("R Channel Texture", 2D) = "black" {}
        _GChannelTex ("G Channel Texture", 2D) = "black" {}
        _BChannelTex ("B Channel Texture", 2D) = "black" {}
        _AChannelTex ("A Channel Texture", 2D) = "black" {}
        _RTexChannelMask ("R Texture Channel Mask", Vector) = (0,0,0,0)
        _GTexChannelMask ("G Texture Channel Mask", Vector) = (0,0,0,0)
        _BTexChannelMask ("B Texture Channel Mask", Vector) = (0,0,0,0)
        _ATexChannelMask ("A Texture Channel Mask", Vector) = (0,0,0,0)
        _GammaCorrectionAlpha ("Gamma Correction Alpha", Float) = 2.2
    }
    SubShader {
        Pass {
            Name "SwizzleBlit"
            ColorMask RGBA

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_RChannelTex);
            SAMPLER(sampler_RChannelTex);

            TEXTURE2D(_GChannelTex);
            SAMPLER(sampler_GChannelTex);

            TEXTURE2D(_BChannelTex);
            SAMPLER(sampler_BChannelTex);

            TEXTURE2D(_AChannelTex);
            SAMPLER(sampler_AChannelTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _RTexChannelMask;
                float4 _GTexChannelMask;
                float4 _BTexChannelMask;
                float4 _ATexChannelMask;
                float _GammaCorrectionAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.vertex);
                OUT.uv = IN.uv.xy;
                return OUT;
            }

            float4 Fragment(Varyings IN) : SV_Target
            {
                float4 r = SAMPLE_TEXTURE2D(_RChannelTex, sampler_RChannelTex, IN.uv) * _RTexChannelMask;
                float4 g = SAMPLE_TEXTURE2D(_GChannelTex, sampler_GChannelTex, IN.uv) * _GTexChannelMask;
                float4 b = SAMPLE_TEXTURE2D(_BChannelTex, sampler_BChannelTex, IN.uv) * _BTexChannelMask;
                float4 a = SAMPLE_TEXTURE2D(_AChannelTex, sampler_AChannelTex, IN.uv) * _ATexChannelMask;

                float rd = distance(r, float4(0, 0, 0, 0));
                float gd = distance(g, float4(0, 0, 0, 0));
                float bd = distance(b, float4(0, 0, 0, 0));
                float ad = distance(a, float4(0, 0, 0, 0));
                ad = pow(ad, 1.0 / _GammaCorrectionAlpha);

                // return r;
                return float4(rd, gd, bd, ad);
            }
            ENDHLSL
        }
    }
}