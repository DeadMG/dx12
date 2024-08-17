#pragma once

#include "Spherical.hlsl"
#include "Structured.hlsl"
#include "Direction.hlsl"

static const int numLights = 5;

void addLight(LightSource l, inout LightSource lights[numLights])
{
    if (l.Power < lights[0].Power)
        return;
    
    [unroll]
    for (int i = 1; i < numLights; i++)
    {
        if (l.Power > lights[i].Power)
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

void normalizePower(inout LightSource lights[numLights])
{
    float totalPower = 0;
    
    [unroll]
    for (uint i = 0; i < numLights; i++)
    {
        totalPower += lights[i].Power;
    }
    
    if (totalPower == 0)
        return;
    
    [unroll]
    for (uint i = 0; i < numLights; i++)
    {
        lights[i].Power /= totalPower;
    }    
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

LightSource zeroLight()
{
    LightSource light;
    light.Power = 0;
    light.Position = float3(0, 0, 0);
    return light;
}

bool isValidLight(LightSource light)
{
    return light.Power > 0;
}

// True if there's at least one valid light
void prepareLights(inout LightSource lights[numLights], StructuredBuffer<LightSource> allLights, inout uint seed, float3 origin, float3 normal)
{
    [unroll]
    for (int i = 0; i < numLights; i++)
    {
        lights[i] = zeroLight();
    }
    
    uint lightCount;
    uint structStride;
    allLights.GetDimensions(lightCount, structStride);
    
    for (int i = 0; i < lightCount; i++)
    {
        LightSource light = allLights[i];
        light.Power = distanceFactor(origin, light.Position, light.DistanceIndependent);
        light.Power = max(light.Power * dot(normal, light.Position - origin), 0);
        addLight(light, lights);
    }
    
    normalizePower(lights);
}
