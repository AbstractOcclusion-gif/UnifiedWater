#ifndef UNIFIED_WATER_FIELD_INCLUDED
#define UNIFIED_WATER_FIELD_INCLUDED

// Field globals published each frame by WaterFieldPublishPass for the primary domain. The names match
// WaterFieldShaderIds, and the world<->uv mapping below is a copy of BoundedDomainExtent.WorldToUv so
// the C# and HLSL sides of the affine map cannot drift. Requires the URP/core texture macros, so this
// is included after Core.hlsl.

TEXTURE2D_ARRAY(_UnifiedWater_Dynamic);
SAMPLER(sampler_UnifiedWater_Dynamic);
TEXTURE2D_ARRAY(_UnifiedWater_NormalFoam);
SAMPLER(sampler_UnifiedWater_NormalFoam);

float4 _UnifiedWater_Center;   // xy = world (X, Z) of the footprint centre; zw unused padding
float _UnifiedWater_Size;      // square footprint size, in metres
float _UnifiedWater_TexelSize; // world metres per field texel (reserved for later steps)

// A bounded pond samples cascade slice 0; larger-water cascades arrive with the clipmap surface.
#define UNIFIED_WATER_CASCADE_SLICE 0

static const float UNIFIED_WATER_UV_CENTRE = 0.5;

// Mirror of BoundedDomainExtent.WorldToUv: the same affine map, kept trivial so the two stay in step.
float2 UnifiedWater_WorldToUv(float3 positionWS)
{
    float u = (positionWS.x - _UnifiedWater_Center.x) / _UnifiedWater_Size + UNIFIED_WATER_UV_CENTRE;
    float v = (positionWS.z - _UnifiedWater_Center.y) / _UnifiedWater_Size + UNIFIED_WATER_UV_CENTRE;
    return float2(u, v);
}

// Dynamic layer stores height in R. Sampled with explicit LOD so it is valid in the vertex stage.
float UnifiedWater_SampleHeight(float2 uv)
{
    return SAMPLE_TEXTURE2D_ARRAY_LOD(
        _UnifiedWater_Dynamic, sampler_UnifiedWater_Dynamic, uv, UNIFIED_WATER_CASCADE_SLICE, 0).r;
}

// NormalFoam layer stores nx in R and nz in G; the compose pass drops +Y as reconstructable. Rebuild
// the unit world-space normal, guarding the square root against tiny numerical overshoot past 1.
float3 UnifiedWater_SampleNormal(float2 uv)
{
    float2 normalXz = SAMPLE_TEXTURE2D_ARRAY_LOD(
        _UnifiedWater_NormalFoam, sampler_UnifiedWater_NormalFoam, uv, UNIFIED_WATER_CASCADE_SLICE, 0).rg;
    float normalY = sqrt(saturate(1.0 - dot(normalXz, normalXz)));
    return normalize(float3(normalXz.x, normalY, normalXz.y));
}

#endif // UNIFIED_WATER_FIELD_INCLUDED
