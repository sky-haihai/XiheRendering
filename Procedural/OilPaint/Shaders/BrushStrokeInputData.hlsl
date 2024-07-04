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