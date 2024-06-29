Shader "Hidden/XiheRendering/VertexDensityBaker/DisplayVertexColor" {
    Properties {
        [Enum(R,0,G,1,B,2,A,3)]_DisplayChannel ("Display Channel", Int ) = 0
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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 vertexColor : COLOR0;
            };

            float _DisplayChannel;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.vertex);
                OUT.vertexColor = IN.color;
                return OUT;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                float displayValue;

                if (_DisplayChannel == 0) // Display Red channel
                    displayValue = IN.vertexColor.r;
                else if (_DisplayChannel == 1) // Display Green channel
                    displayValue = IN.vertexColor.g;
                else if (_DisplayChannel == 2) // Display Blue channel
                    displayValue = IN.vertexColor.b;
                else // Default to Alpha channel
                    displayValue = IN.vertexColor.a;
                return float4(displayValue, displayValue, displayValue, 1.0);
            }
            ENDHLSL
        }
    }
}