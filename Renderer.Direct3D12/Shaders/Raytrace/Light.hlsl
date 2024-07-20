#pragma once

#include "Spherical.hlsl"

static const int numLights = 5;

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

void initialLights(inout SampledLight lights[numLights], float3 bdrf)
{    
    SampledLight light;
    light.direction = bdrf;
    light.power = 0.1;
    lights[numLights - 1] = light;
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

float totalPower(inout SampledLight lights[numLights])
{    
    float power = 0;
    for (uint i = 0; i < numLights; i++)
    {
        power += lights[i].power;
    }
    
    return power;
}

SampledLight zeroLight()
{
    SampledLight light;
    light.power = 0;
    light.direction = float3(0, 0, 0);
    return light;
}

float3 partialSphereSample(inout uint seed, float3 spherePosition, float size, float3 normal, float3 origin)
{
    // Perform the plane intersection
    float d = dot(origin, normal);
    float rho = (normal.x * spherePosition.x) + (normal.y * spherePosition.y) + (normal.z * spherePosition.z) - d;
    float r = sqrt(size * size - rho * rho);
    
    float elevationAdjustment = asin(r / size) / PI;
    
    Spherical existing = cartesianToSpherical(normal);
    Spherical random = randomSpherical(size, seed);
    
    random.elevation *= elevationAdjustment;
    
    random.elevation = random.elevation + existing.elevation;
    random.azimuth = random.azimuth + existing.azimuth;
    
    return spherePosition + sphericalToCartesian(random);
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
    
    float top = cos(start + angle);
    float bottom = cos(start - angle);
    
    float topExtent = max(top, bottom);
    float bottomExtent = min(top, bottom);
    
    if (topExtent <= 0)
        return zeroLight();
    
    // Whole thing visible
    if (bottomExtent >= 0)
    {
        float3 targetPosition = position + sphericalToCartesian(randomSpherical(size, seed));
        float3 direction =  normalize(targetPosition - origin);
        
        SampledLight light;
        light.power = current;
        light.direction = direction;
        return light;
    }
    
    current = current * (topExtent / (topExtent - bottomExtent));    
    
    SampledLight light;
    light.power = current;
    light.direction = normalize(partialSphereSample(seed, position, size, normal, origin) - origin);
    return light;
}
