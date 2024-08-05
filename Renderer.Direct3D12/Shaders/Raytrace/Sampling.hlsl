#pragma once

#include "Spherical.hlsl"
#include "Constants.hlsl"

float3 cosineHemisphere(inout uint seed, float3 normal)
{
    Spherical weighted;
    weighted.r = 1;
    weighted.azimuth = 2 * PI * uniformRand(seed);
    weighted.elevation = acos(sqrt(uniformRand(seed)));
    return rotateNormal(normal, weighted);
}
