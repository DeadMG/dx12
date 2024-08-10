#pragma once

#include "Spherical.hlsl"
#include "Structured.hlsl"
#include "Sampling.hlsl"

static const int numLights = 1;

struct LightSource
{
    float Power;
    uint VerticesIndex;
    uint TrianglesIndex;
    float4x4 WorldMatrix;
    float3 Position;
    float Size;
    bool DistanceIndependent;
};

struct SampledLight
{
    float3 direction;
    float power;
};

void addLight(SampledLight l, inout SampledLight lights[numLights])
{
    if (l.power < lights[0].power)
        return;
    
    [unroll]
    for (int i = 1; i < numLights; i++)
    {
        if (l.power > lights[i].power)
        {
            lights[i - 1] = lights[i];
        }
        else
        {
            lights[i - 1] = l;
            return;
        }
    }
    
    // We got to the end but didn't find a stronger light.
    lights[numLights - 1] = l;
}

bool normalizePower(inout SampledLight lights[numLights])
{
    float totalPower = 0;
    
    [unroll]
    for (uint i = 0; i < numLights; i++)
    {
        totalPower += lights[i].power;
    }
    
    if (totalPower == 0)
        return false;
    
    [unroll]
    for (uint i = 0; i < numLights; i++)
    {
        lights[i].power /= totalPower;
    }
    
    return true;
}

float distanceFactor(float3 origin, float3 position, bool distanceIndependent)
{
    float3 direction = position - origin;
    float l = length(direction);
    
    if (!distanceIndependent)
    {
        return l * l;
    }
    
    return 1;
}

SampledLight zeroLight()
{
    SampledLight light;
    light.power = 0;
    light.direction = float3(0, 0, 0);
    return light;
}

SampledLight sampleSphereLight(inout uint seed, float3 origin, float3 normal, float power, float3 position, float size, bool distanceIndependent)
{
    float current = power / distanceFactor(origin, position, distanceIndependent);
    
    float3 direction = position - origin;
    float l = length(direction);
    
    float ratio = size / l;
    if (ratio > 1 || ratio < -1)
        return zeroLight(); // We are this object
    
    float angle = asin(ratio);
    float start = acos(dot(normal, normalize(direction)));
    
    if ((start - angle) >= (PI / 2))
        return zeroLight();

    SampledLight light;
    light.power = current;
    light.direction = cone(seed, normalize(direction), angle);
    return light;
}

// True if there's at least one valid light
bool prepareLights(inout SampledLight lights[numLights], LightSource light, inout uint seed, float3 origin, float3 normal)
{
    [unroll]
    for (int i = 0; i < numLights; i++)
    {
        lights[i] = zeroLight();
    }
    
    addLight(sampleSphereLight(seed, origin, normal, light.Power, light.Position, light.Size, light.DistanceIndependent), lights);
    
    return normalizePower(lights);
}

SampledLight sampleLights(inout SampledLight lights[numLights], inout uint seed)
{
    float targetPower = uniformRand(seed);
    float powerSoFar = 0;
    
    [unroll]
    for (uint i = 0; i < numLights; i++)
    {
        SampledLight light = lights[i];
        
        powerSoFar += light.power;
        
        if (powerSoFar > targetPower)
        {
            return light;
        }
    }
    
    return lights[numLights - 1];
}
