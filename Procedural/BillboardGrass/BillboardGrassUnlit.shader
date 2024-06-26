Shader "Hidden/XiheRendering/BillboardGrassUnlit" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _ControlTex ("Control (RGBA)", 2D) = "white" {}//R: Position Offset, G: Density, B: Scale, A: Height
        _NoiseTex ("Noise (RGBA)", 2D) = "white" {}//R: PosX, G: PosY, B: PosZ, A: Scale
    }
    SubShader {
        Cull Off
        Zwrite On

        Pass {
            Name "BillboardGrassForward"

            Tags {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Dimension) //x: dimensionX, y: dimensionZ, z: densityX, w: densityZ
                UNITY_DEFINE_INSTANCED_PROP(float, _DimensionY)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ControlNoiseScale)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SwingScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _Speed)
                UNITY_DEFINE_INSTANCED_PROP(float, _Scale)
            UNITY_INSTANCING_BUFFER_END(Props)

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            //R: PosX, G: PosZ, B: Scale, A: Height
            TEXTURE2D(_ControlTex);
            SAMPLER(sampler_ControlTex);

            //R: PosX, G: PosY, B: PosZ, A: Scale
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vertex(Attributes IN, uint instanceID : SV_InstanceID)
            {
                float dimensionZ = floor(_Dimension.y * _Dimension.w); //actual number of instances allowed on z-axis
                float offsetX = floor(instanceID / dimensionZ) / _Dimension.z;
                float offsetZ = instanceID % dimensionZ / _Dimension.w;

                float3 localPosition = IN.vertex.xyz;
                // float2 instanceID2D01 = float2(offsetX / _Dimension.x, offsetZ / _Dimension.y);
                float2 instanceID2D01 = float2(offsetX / _Dimension.x, offsetZ / _Dimension.y);

                //R: Pos Noise, G: Scale Noise, B: Density, A: Height
                float4 control = SAMPLE_TEXTURE2D_LOD(_ControlTex, sampler_ControlTex, instanceID2D01, 0);
                float4 noise = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, instanceID2D01+_Time.xy*_Speed, 0);

                localPosition.x += lerp(0, (noise.r * 2 - 1) * _SwingScale.x, IN.uv.x);
                localPosition.z += lerp(0, (noise.g * 2 - 1) * _SwingScale.z, IN.uv.y);
                localPosition *= (noise.a * 2 - 1) * _SwingScale.w + 1;
                localPosition *= step(.5, control.g) * (control.b * 2) * _ControlNoiseScale.z + 1;
                localPosition *= _Scale;

                localPosition.x += (control.r * 2 - 1) * _ControlNoiseScale.x;
                localPosition.z += (control.r * 2 - 1) * _ControlNoiseScale.y;
                localPosition.y += -_DimensionY / 2 + control.a * _DimensionY;

                //calculate world position                
                float3 worldPosition = localPosition - float3(_Dimension.x, 0, _Dimension.y) / 2.0 + float3(offsetX, 0, offsetZ);

                Varyings o;
                o.positionHCS = TransformObjectToHClip(worldPosition);
                o.uv = IN.uv.xy;
                return o;
            }

            float4 Fragment(Varyings IN) : SV_Target
            {
                float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                clip(albedo.a - 0.5);

                float4 output = albedo;

                return output;
            }
            ENDHLSL
        }
    }
}