#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
// ------------------------------------- Tex Functions
//Metallic vs Specular
half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half4 specGloss;

#ifdef _SPECULAR_SETUP_ON //Specular Workflow 因为现在基本不用specular工作流了 所以没加map的条件判断 :)
    specGloss = SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv);
    specGloss.rgb = specGloss;
    
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        specGloss.a = albedoAlpha * _Smoothness;
    #else
        specGloss.a *= _Smoothness;
    #endif
#else //Metallic Workflow
    specGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
    #ifdef _MAP_ON
    specGloss.rgb = specGloss;
    #else
    specGloss.rgb = _Metallic.rrr;
    #endif
    
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        specGloss.a = albedoAlpha * _Smoothness;
    #else
        specGloss.a = _Smoothness;
    #endif
#endif

    return specGloss;
}
//Occlusion
half SampleOcclusion(float2 uv)
{
#ifdef _OCCLUSIONMAP_ON
    half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
    return lerp(1.0, occ, _OcclusionStrength);
#else
    return 1.0;
#endif
}
// ------------------------------------- InitializeStandardLitSurfaceData
void InitSurfaceData(Varyings input, out SurfaceData surfaceData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(input.uv,_BaseMap, sampler_BaseMap);
    
    surfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    half4 specGloss = SampleMetallicSpecGloss(input.uv, albedoAlpha.a);
    surfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;

#if _SPECULAR_SETUP_ON
    surfaceData.metallic = 1.0;
    surfaceData.specular = specGloss.rgb;
#else
    surfaceData.metallic = specGloss.r;
    surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
#endif

    surfaceData.smoothness = specGloss.a;
    surfaceData.normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    surfaceData.occlusion = SampleOcclusion(input.uv);
    surfaceData.emission = SampleEmission(input.uv, _EmissionColor.rgb,_EmissionMap, sampler_EmissionMap);
    
    surfaceData.clearCoatMask = 0.0;
    surfaceData.clearCoatSmoothness = 0.0;
}

