Shader "OilPaint/BrushStrokeInstancing" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalTex ("Normal (RGB)", 2D) = "white" {}
        _NormalStrength ("Normal Strength", Float) = 1
        _SpecularStrength ("Specular Strength", Float) = 1
        _SpecularTintColor ("Specular Tint Color", Color) = (1,1,1,1)
        _YCount ("Row Count", Float) = 1
        _XCount ("Column Count", Float) = 1
        _ShadowColor ("Shadow Color", Color) = (0,0,0,1)
    }
    SubShader {
        Cull Off
        Zwrite On

        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalRenderPipeline"
        }

        Pass {
            Name "BillboardForward"
            Blend SrcAlpha OneMinusSrcAlpha

            Tags {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _RotationRandomness)
                UNITY_DEFINE_INSTANCED_PROP(float4x4, _TRSMatrix)
                UNITY_DEFINE_INSTANCED_PROP(float, _HeightOffset)
                UNITY_DEFINE_INSTANCED_PROP(float, _AlphaCutoff)
                UNITY_DEFINE_INSTANCED_PROP(float, _ScaleMax)
                UNITY_DEFINE_INSTANCED_PROP(float, _ScaleMin)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS: TEXCOORD1;
                float3 tangentWS: TEXCOORD2;
                float3 binormalWS: TEXCOORD3;
                float4 vertexColor : TEXCOORD4;
                float3 positionWS : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);

            float _NormalStrength;
            float _SpecularStrength;
            float _XCount;
            float _YCount;
            float3 _SpecularTintColor;
            float3 _ShadowColor;

            StructuredBuffer<float3> _PositionBuffer;
            StructuredBuffer<float3> _NormalBuffer;
            StructuredBuffer<float4> _TangentBuffer;
            StructuredBuffer<float4> _ColorBuffer;

            float Random01(float seed)
            {
                seed = frac(sin(seed * 89.456) * 67890.5432 + 2.1234);
                return seed;
            }


            Varyings Vertex(Attributes IN, uint instanceID : SV_InstanceID)
            {
                float4 vertexColor = _ColorBuffer[instanceID];
                float3 strokePosOS = _PositionBuffer[instanceID].xyz;
                float3 strokeNormal = _NormalBuffer[instanceID].xyz;
                float3 strokeTangent = _TangentBuffer[instanceID].xyz;
                float3 vertexOffset = IN.vertex.xyz * float3(1.5, 1, 1);
                float3 binormal = -cross(strokeNormal, strokeTangent);
                binormal += (Random01(instanceID) * 2 - 1) * _RotationRandomness * strokeTangent;
                binormal = normalize(binormal);
                strokeTangent = cross(strokeNormal, binormal);

                float3 x = strokeTangent * vertexOffset.x;
                float3 y = strokeNormal * vertexOffset.y;
                float3 z = binormal * vertexOffset.z;
                vertexOffset = x + y + z + Random01(instanceID) * _HeightOffset * strokeNormal;

                Varyings o;
                float3 worldPos = strokePosOS + vertexOffset * lerp(_ScaleMin, _ScaleMax, vertexColor.a);
                worldPos = mul(_TRSMatrix, float4(worldPos, 1)).xyz;
                o.positionHCS = TransformWorldToHClip(worldPos);
                o.positionWS = worldPos;
                o.normalWS = normalize(mul(_TRSMatrix, float4(strokeNormal, 0)).xyz);
                o.tangentWS = normalize(mul(_TRSMatrix, float4(strokeTangent, 0)).xyz);
                o.binormalWS = normalize(cross(o.normalWS, o.tangentWS));
                o.vertexColor = vertexColor;
                o.vertexColor = (Random01(instanceID) * 2 - 1);


                //defined by vertex color alpha channel
                float uvY = IN.uv.y / _YCount + floor(clamp(o.vertexColor.a, 0, 0.999999) * _YCount) / _YCount;
                //random pick one along x axis
                float uvX = IN.uv.x / _XCount + floor(clamp(Random01(instanceID), 0, 0.999999) * _XCount) / _XCount;
                o.uv = float2(uvX, uvY);

                return o;
            }

            float4 Fragment(Varyings IN) : SV_Target
            {
                float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                clip(albedo.a - _AlphaCutoff);
                // return albedo;
                Light mainLight = GetMainLight();

                float ndoth = dot(normalize(mainLight.direction + _WorldSpaceCameraPos.xyz - IN.positionWS), IN.normalWS);
                // ndoth = 1 - ndoth;
                ndoth = smoothstep(.85, .9, ndoth);
                // return float4(ndoth, ndoth, ndoth, 1);
                float3 normal = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, IN.uv), -_NormalStrength * (ndoth));
                normal = normal.x * IN.tangentWS + normal.y * IN.binormalWS + normal.z * IN.normalWS;
                normal = normalize(normal);
                float ndotl = dot(normal, mainLight.direction);
                // float ndotl = dot(IN.normalWS, mainLight.direction);

                ndoth = dot(normalize(mainLight.direction + _WorldSpaceCameraPos.xyz - IN.positionWS), normal);
                // ndoth = smoothstep(.95, 1., ndoth);

                // return float4(ndoth, ndoth, ndoth, 1);

                float4 output = 1;

                output.rgb = lerp(IN.vertexColor.rgb + pow(saturate(ndoth), _SpecularStrength) * IN.vertexColor.rgb * _SpecularTintColor,
                                      saturate(IN.vertexColor.rgb * _ShadowColor),
                                      saturate(1 - ndotl));
                // output.rgb = IN.normalWS;
                // output.rgb=IN.vertexColor.a;

                output.a = albedo.a;
                return output;
            }
            ENDHLSL
        }
    }
}