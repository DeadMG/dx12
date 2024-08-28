#pragma once

#include "Random.hlsl"

// Due to the different conventions in use in different materials,
// theta/phi will not be used, but rather, elevation/azimuth. This
// makes it more obvious what does what. Since we are LH Y-up then
// Y is affected by elevation only.
struct Spherical
{
    float r;
    float elevation;
    float azimuth;
};

float3 sphericalToCartesian(Spherical sphere)
{
    float3 coords = float3(
        sin(sphere.elevation) * cos(sphere.azimuth),
        cos(sphere.elevation),
        sin(sphere.elevation) * sin(sphere.azimuth));
    
    return normalize(coords) * sphere.r;
}

Spherical cartesianToSpherical(float3 cartesian)
{
    Spherical ret;
    ret.r = length(cartesian);
    ret.elevation = acos(cartesian.y / ret.r);
    ret.azimuth = abs(cartesian.y / ret.r) == 1 ? 0 : atan2(cartesian.z, cartesian.x);
    return ret;
}

float3 rotateNormal(float3 normal, Spherical rotation) {
    normal = normalize(normal);
    float3 cartesianBased = sphericalToCartesian(rotation);
    
    float3 uPrime = cross(normal, float3(0, 1, 0));
    float3 vPrime = cross(normal, uPrime);
    
    return mul(cartesianBased, float3x3(uPrime, normal, vPrime));
}
