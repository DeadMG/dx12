#pragma once

#include "Spherical.hlsl"
#include "Constants.hlsl"
#include "Direction.hlsl"
#include "Power.hlsl"
#include "Light.hlsl"

struct MonteCarloSample
{
    float3 direction;
    bool isNextEventEstimation;
    bool isValid;
};

MonteCarloSample newSample()
{
    MonteCarloSample sample;
    sample.isValid = true;
    return sample;
}

MonteCarloSample cosineHemisphere(inout uint seed, float3 normal)
{
    Spherical weighted;
    weighted.r = 1;
    weighted.azimuth = 2 * PI * uniformRand(seed);
    weighted.elevation = acos(sqrt(uniformRand(seed)));
    
    MonteCarloSample sample = newSample();
    sample.direction = rotateNormal(normal, weighted);
    sample.isNextEventEstimation = false;
    return sample;
}

MonteCarloSample cone(inout uint seed, float3 direction, float angle)
{
    float eta = uniformRand(seed);
    Spherical weighted;
    weighted.r = 1;
    weighted.elevation = acos((1 - eta) + (eta * cos(angle)));
    weighted.azimuth = 2 * PI * uniformRand(seed);
    
    MonteCarloSample sample = newSample();
    sample.direction = rotateNormal(direction, weighted);
    sample.isNextEventEstimation = false;
    return sample;
}

MonteCarloSample zeroSample()
{
    MonteCarloSample sample;
    sample.isValid = false;
    return sample;
}

bool isValidSample(MonteCarloSample sample, float3 normal)
{
    if (!sample.isValid)
        return false;
    if (dot(sample.direction, normal) < 0)
        return false;
    return true;
}

static const int numSamples = 4;

struct PreweightedMonteCarloSample
{
    Direction direction;
    float weight;
};

int samplesOfDistribution(inout MonteCarloSample samples[numSamples], bool nextEventEstimation)
{
    int result = 0;
    
    [unroll]
    for (int i = 0; i < numSamples; i++)
    {
        if (samples[i].isNextEventEstimation == nextEventEstimation)
            result += 1;
    }
    
    return result;
}

MonteCarloSample sampleSphereLight(LightSource light, inout uint seed, float3 origin, float3 normal)
{
    float3 direction = light.Position - origin;
    float l = length(direction);
    
    float ratio = light.Size / l;
    if (ratio > 1 || ratio < -1)
        return zeroSample(); // We are this object
    
    float angle = asin(ratio);
    float start = acos(dot(normal, normalize(direction)));
    
    if ((start - angle) >= (PI / 2))
        return zeroSample();
    
    return cone(seed, normalize(direction), angle);
}

MonteCarloSample sampleLight(LightSource light, inout uint seed, float3 origin, float3 normal)
{
    if (!isValidLight(light))
    {
        return zeroSample();
    }
    
    MonteCarloSample sample = sampleSphereLight(light, seed, origin, normal);
    sample.isNextEventEstimation = true;
    return sample;
}

MonteCarloSample sampleLights(inout LightSource lights[numLights], inout uint seed, float3 origin, float3 normal)
{
    float targetPower = uniformRand(seed);
    float powerSoFar = 0;
    
    [unroll]
    for (uint i = 0; i < numLights; i++)
    {
        LightSource light = lights[i];
        
        powerSoFar += light.Power;
        
        if (powerSoFar > targetPower)
        {
            return sampleLight(light, seed, origin, normal);
        }
    }
    
    return sampleLight(lights[numLights - 1], seed, origin, normal);
}

float brdfPdf(float3 normal, float3 direction)
{    
    return dot(normal, direction) / PI;
}

float neePdf(inout LightSource lights[numLights], float3 origin, float3 sampleDirection)
{
    float total = 0;
    
    for (uint i = 0; i < numLights; i++)
    {
        LightSource light = lights[i];
        
        if (!isValidLight(light))
            continue;
        
        float3 direction = light.Position - origin;
        float l = length(direction);
    
        float ratio = light.Size / l;
        if (ratio > 1 || ratio < -1)
            continue;
    
        float angle = asin(ratio);
        float targetAngle = acos(dot(sampleDirection, direction));
        
        if (targetAngle < angle)
            continue;
        
        if (light.DistanceIndependent)
            angle = (PI / 2);
        
        total += light.Power * (1.0f / (2.0f * PI * (1.0f - cos(angle))));
    }
    
    return total;
}

void preweightSamples(inout PreweightedMonteCarloSample result[numSamples], inout MonteCarloSample samples[numSamples], inout LightSource lights[numLights], float3 origin, float3 normal)
{    
    int neeSamples = samplesOfDistribution(samples, true);
    int brdfSamples = numSamples - neeSamples;
    
    for (int i = 0; i < numSamples; i++)
    {
        MonteCarloSample sample = samples[i];
        
        // If we have no samples, we have no valid light. This is probably uniform, so this hot-path will
        // probably actually be hot.
        float neeWeight = neeSamples == 0 ? 0 : neePdf(lights, origin, sample.direction);
        float brdfWeight = brdfPdf(normal, sample.direction);
        
        int thisDistributionSamples = sample.isNextEventEstimation ? neeSamples : brdfSamples;
        float thisDistributionPdf = sample.isNextEventEstimation ? neeWeight : brdfWeight;
        
        PreweightedMonteCarloSample preweighted;
        preweighted.direction = cartesianToDirection(sample.direction);
        preweighted.weight = thisDistributionSamples * thisDistributionPdf * dot(normal, sample.direction) / (PI * (pow2(neeSamples * neeWeight) + pow2(brdfSamples * brdfWeight)));
        result[i] = preweighted;
    }
}
