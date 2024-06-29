Shader "Hidden/XiheRendering/VertexDensityBaker/DisplayVertexColor" {
    Properties {
        [Enum(R,0,G,1,B,2,A,3,RGB,4)]_DisplayChannel ("Display Channel", Int ) = 0
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }

        Pass {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color : COLOR0;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 vertexColor : COLOR0;
                float3 normalWS : TEXCOORD0;
            };

            float _DisplayChannel;
            float4 _ShadowColor;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.vertex);
                OUT.vertexColor = IN.color;
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normal));
                return OUT;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                float4 result;

                if (_DisplayChannel == 0) // Display Red channel
                    result = IN.vertexColor.r;
                else if (_DisplayChannel == 1) // Display Green channel
                    result = IN.vertexColor.g;
                else if (_DisplayChannel == 2) // Display Blue channel
                    result = IN.vertexColor.b;
                else if (_DisplayChannel == 3) // Default to Alpha channel
                    result = IN.vertexColor.a;
                else // Display RGB
                    result = float4(IN.vertexColor.rgb, 1.0);

                Light mainLight = GetMainLight();
                // float ndotl = dot(normal, mainLight.direction);
                float ndotl = dot(IN.normalWS, mainLight.direction);
                result.rgb = lerp(result, saturate(result * _ShadowColor), saturate(1 - ndotl));

                return result;
            }
            ENDHLSL
        }
    }
}