Shader "OilPaint/BrushStrokeInstancing" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _YCount ("Row Count", Float) = 1
        _XCount ("Column Count", Float) = 1

        _NormalTex ("Normal (RGB)", 2D) = "white" {}
        _NormalStrength ("Normal Strength", Float) = 1

        _SpecularStrength ("Specular Strength", Float) = 1
        _SpecularTintColor ("Specular Tint Color", Color) = (1,1,1,1)

        _ShadowColor ("Shadow Color", Color) = (0,0,0,1)
        _EdgeAlpha ("Edge Alpha", Range(0,1.)) = 0.5
    }
    SubShader {
        Cull Back
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
            #pragma vertex BrushStrokeVertex
            #pragma fragment BrushStrokeFragment
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _RotationRandomness)
                UNITY_DEFINE_INSTANCED_PROP(float4x4, _TRSMatrix)
                UNITY_DEFINE_INSTANCED_PROP(float, _HeightOffset)
                UNITY_DEFINE_INSTANCED_PROP(float, _AlphaCutoff)
                UNITY_DEFINE_INSTANCED_PROP(float, _ScaleMax)
                UNITY_DEFINE_INSTANCED_PROP(float, _ScaleMin)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct StrokeData
            {
                float3 position;
                float3 normal;
                float4 tangent;
                float4 color;
            };

            struct BrushStrokeAttributes
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
            };

            struct BrushStrokeVaryings
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

            //anything not per object specific goes here
            float _NormalStrength;
            float _SpecularStrength;
            float _XCount;
            float _YCount;
            float3 _SpecularTintColor;
            float3 _ShadowColor;
            float _EdgeAlpha;

            StructuredBuffer<StrokeData> _StrokeDataBuffer;

            float Random01(float seed)
            {
                const float a = 2;
                const float b = 3;
                const float c = 2;
                return frac(sin(seed * a) * b + c);
            }

            float3 RotateAroundAxis(float3 v, float3 axis, float angle)
            {
                axis = normalize(axis);
                float cosAngle = cos(angle);
                float sinAngle = sin(angle);

                return v * cosAngle + cross(axis, v) * sinAngle + axis * dot(axis, v) * (1 - cosAngle);
            }

            BrushStrokeVaryings BrushStrokeVertex(BrushStrokeAttributes IN, uint instanceID : SV_InstanceID)
            {
                StrokeData strokeData = _StrokeDataBuffer[instanceID];
                float4 vertexColor = strokeData.color;
                float3 strokePosOS = strokeData.position;
                float3 strokeNormal = strokeData.normal;
                float3 strokeTangent = strokeData.tangent.xyz;
                strokeTangent = RotateAroundAxis(strokeTangent, strokeNormal, (Random01(instanceID * 123) * 2 - 1) * _RotationRandomness * 3.1415926);

                float3 vertexOffset = IN.vertex.xyz * float3(1, 1, 1);
                float3 binormal = -cross(strokeNormal, strokeTangent);

                float3 x = strokeTangent * vertexOffset.x;
                float3 y = strokeNormal * vertexOffset.y;
                float3 z = binormal * vertexOffset.z;
                vertexOffset = x + y + z + Random01(instanceID) * _HeightOffset * strokeNormal;

                BrushStrokeVaryings o;
                float3 worldPos = strokePosOS + vertexOffset * lerp(_ScaleMin, _ScaleMax, vertexColor.a);
                worldPos = mul(_TRSMatrix, float4(worldPos, 1)).xyz;
                o.positionHCS = TransformWorldToHClip(worldPos);
                o.positionWS = worldPos;
                o.normalWS = normalize(mul(_TRSMatrix, float4(strokeNormal, 0)).xyz);
                o.tangentWS = normalize(mul(_TRSMatrix, float4(strokeTangent, 0)).xyz);
                o.binormalWS = normalize(cross(o.normalWS, o.tangentWS));
                o.vertexColor = vertexColor;
                // o.vertexColor.rgb = Random01(instanceID);

                //defined by vertex color alpha channel
                float uvY = IN.uv.y / _YCount + floor(clamp(vertexColor.a, 0, 0.999999) * _YCount) / _YCount;
                //random pick one along x axis
                float uvX = IN.uv.x / _XCount + floor(clamp(Random01(instanceID), 0, 0.999999) * _XCount) / _XCount;
                o.uv = float2(uvX, uvY);

                return o;
            }

            float4 BrushStrokeFragment(BrushStrokeVaryings IN) : SV_Target
            {
                float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                clip(albedo.a - _AlphaCutoff);
                // return albedo;
                Light mainLight = GetMainLight();

                float ndoth = dot(normalize(mainLight.direction + _WorldSpaceCameraPos.xyz - IN.positionWS), IN.normalWS);
                ndoth = smoothstep(.85, .9, ndoth);
                // return float4(ndoth, ndoth, ndoth, 1);
                float3 normal = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, IN.uv), -_NormalStrength);
                normal = normal.x * IN.tangentWS + normal.y * IN.binormalWS + normal.z * IN.normalWS;
                normal = normalize(normal);
                ndoth = dot(normalize(mainLight.direction + _WorldSpaceCameraPos.xyz - IN.positionWS), normal);
                float ndotl = dot(normal, mainLight.direction);

                float4 output = 1;

                float specular = pow(saturate(ndoth), _SpecularStrength);
                output.rgb = lerp(IN.vertexColor.rgb + specular * IN.vertexColor.rgb * _SpecularTintColor,
                                  saturate(IN.vertexColor.rgb * _ShadowColor),
                                  saturate(1 - ndotl));

                // output.rgb=ndoth;
                output.a = lerp(albedo.a, _EdgeAlpha, ndoth);
                return output;
            }
            ENDHLSL
        }
    }
}