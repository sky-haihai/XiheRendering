#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float NDotL(float3 normalWS, half3 lightDirectionWS)
{
    return dot(normalWS, lightDirectionWS);
}

float3 GetNormalWS(Texture2D normalTexTS, sampler normalTexSampler, float3 normalWS, float4 tangentWS, float2 uv, float normalStrength)
{
    float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(normalTexTS, normalTexSampler, uv));
    normalTS = normalize(float3(normalTS.x * normalStrength, normalTS.y * normalStrength, normalTS.z));
    float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    float3 result = normalize(TransformTangentToWorld(normalTS, tangentToWorld));
    return result;
}

void GetAdditionalLightsInfo(float3 positionWS, float lightColorInfluence, out float shadowMask, out float3 lightColor)
{
    float shadow = 0;
    float3 color = 0;
    for (int lightIndex = 0; lightIndex < GetAdditionalLightsCount(); lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, positionWS, 1.0);
        float3 lightC = light.color * clamp(light.distanceAttenuation * lightColorInfluence, 0, 1);
        color += lightC;

        float lightIntensity = 0.299 * light.color.r + 0.587 * light.color.g + 0.114 * light.color.b;
        float shadowAtten = light.shadowAttenuation * light.distanceAttenuation * lightIntensity;
        shadow += shadowAtten;
    }

    shadowMask = clamp(shadow, 0, 1);
    lightColor = color;
}

float GetLuminance(float r, float g, float b)
{
    return 0.333 * r + 0.333 * g + 0.333 * b;
}

float GetLuminance(float3 color)
{
    return 0.333 * color.r + 0.333 * color.g + 0.333 * color.b;
}

float4 ComputeToonSurface(float4 color, float4 shadowColor, float3 normalWS, float3 vertexPosWS, float mainLightInfluence, float additionalLightInfluence, Texture2D colorRampTex,
                          sampler colorRampTexSampler, Texture2D addColorRampTex, sampler addColorRampTexSampler)
{
    //main light color
    float4 shadowCoord = TransformWorldToShadowCoord(vertexPosWS);
    Light mainLight = GetMainLight(shadowCoord);
    float mainLightNdotL = NDotL(normalWS, mainLight.direction);
    mainLightNdotL = mainLightNdotL * 0.5 + 0.5;
    float mainShadowAttenuation = mainLight.shadowAttenuation * mainLightNdotL;
    mainShadowAttenuation = SAMPLE_TEXTURE2D(colorRampTex, colorRampTexSampler, float2(mainShadowAttenuation, 0)).r;
    float3 mainLightColor = mainLight.color * mainShadowAttenuation;
    mainLightColor = lerp(mainLightColor, mainLight.color * shadowColor.rgb, 1 - mainShadowAttenuation);

    //additional light color
    float3 additionalLightColor = 0;
    for (int lightIndex = 0; lightIndex < GetAdditionalLightsCount(); lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, vertexPosWS, 1.0);
        float addShadowAttenuation = light.shadowAttenuation * light.distanceAttenuation * GetLuminance(light.color);
        addShadowAttenuation = SAMPLE_TEXTURE2D(addColorRampTex, addColorRampTexSampler, float2(saturate(addShadowAttenuation), 0)).r;
        additionalLightColor += light.color * addShadowAttenuation;
    }

    //surface color
    float3 lightColor = mainLightColor * mainLightInfluence + additionalLightColor * additionalLightInfluence + 0.03;
    float4 result = color * float4(lightColor, 1);
    return result;
}
