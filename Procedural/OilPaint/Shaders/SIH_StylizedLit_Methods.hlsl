// ------------------------------------- DirLight
half LinearStep(half minValue, half maxValue, half In)
{
    return saturate((In-minValue) / (maxValue - minValue));
}
// StylizedDiffuse
half3 CalculateStylizedDiffuse(Light light, half3 normalWS, half2 uv, half _ToneOffset, half _ToneLayer, half3 _ShadowColor)
{
    half NdotL = dot(normalWS, light.direction) * 0.5 + 0.5;
    half3 rampDiffuse;
                
    #ifdef  _RAMPMAP_ON
    rampDiffuse = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(NdotL, 1.0)).rgb;
    #else
        #ifdef _SHADOWSTYLE_ON 
        rampDiffuse = step(_ToneOffset, NdotL).rrr;
        #else
        rampDiffuse = (floor(NdotL * _ToneLayer) / _ToneLayer).rrr;
        #endif
    #endif
    rampDiffuse = lerp(_ShadowColor.rgb, half3(1.0, 1.0, 1.0), rampDiffuse);
    rampDiffuse = light.color * rampDiffuse;
    return rampDiffuse;
}
//SpecularBRDF
half3 CalculateStylizedGGX(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
    #ifndef _SPECULARHIGHLIGHTS_OFF
    float3 halfDir = SafeNormalize(float3(lightDirectionWS) + float3(viewDirectionWS));
    
    float NoH = saturate(dot(normalWS, halfDir));
    half LoH = saturate(dot(lightDirectionWS, halfDir));
    
    float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;
    
    half LoH2 = LoH * LoH;
    half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm) * brdfData.specular;
                
    half3 color = lerp(LinearStep( _SpecularThreshold - _SpecularSmooth, _SpecularThreshold + _SpecularSmooth, specularTerm ), specularTerm, _GGXSpecular)
                  * brdfData.specular * max(0,_SpecularIntensity) * _SpecColor + brdfData.diffuse;
    return color;
    #else
    return brdfData.diffuse;
    #endif
}
//BRDF
half3 MyLightingStylizedPhysicallyBased(BRDFData brdfData, half3 radiance, half3 lightColor, half3 lightDirectionWS, half lightAttenuation, half3 normalWS, half3 viewDirectionWS)
{
    return  CalculateStylizedGGX(brdfData, normalWS, normalize(lightDirectionWS + _SpecularLightOffset.xyz), viewDirectionWS) * radiance;
}

half3 MyLightingStylizedPhysicallyBased(BRDFData brdfData, half3 radiance, Light light, half3 normalWS, half3 viewDirectionWS)
{
   return MyLightingStylizedPhysicallyBased(brdfData, radiance, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS);

}
// ------------------------------------- InDirLight
//EnvSpec
half3 MyEnvBRDFCustom(BRDFData brdfData, half3 radiance, half3 indirectDiffuse, half3 indirectSpecular, half fresnelTerm)  
{
    half3 c = indirectDiffuse * brdfData.diffuse;
    float surfaceReduction = 1.0 / (brdfData.roughness2 + 1.0);
    c += surfaceReduction * indirectSpecular * lerp(brdfData.specular * radiance, brdfData.grazingTerm, fresnelTerm);
    return c;
}
//EnvDiffuse
half3 MyStylizedGI(BRDFData brdfData, half3 radiance, half3 bakedGI, half occlusion, half3 normalWS, half3 viewDirectionWS, half metallic, half ndotl)
{
    half3 reflectVector = reflect(-viewDirectionWS, normalWS);
    half fresnelTerm = LinearStep( _FresnelThreshold - _FresnelSmooth, _FresnelThreshold += _FresnelSmooth, 1.0 - saturate(dot(normalWS, viewDirectionWS))) * max(0,_FresnelIntensity) * ndotl;

    half3 indirectDiffuse = bakedGI * occlusion;

    half3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, brdfData.perceptualRoughness, occlusion) * lerp(max(0,_ReflProbeIntensity), max(0,_MetalReflProbeIntensity), metallic) ;
    return MyEnvBRDFCustom(brdfData, radiance, indirectDiffuse, indirectSpecular, fresnelTerm);
}
