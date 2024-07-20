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

Spherical randomSpherical(float r, inout uint seed)
{
    Spherical sphere;
    sphere.r = r;    
    sphere.elevation = acos((2 * uniformRand(seed)) - 1);
    sphere.azimuth = uniformRand(seed) * 2 * PI;
    return sphere;    
}

float3 sphericalToCartesian(Spherical sphere)
{
    float3 coords = float3(
        cos(sphere.elevation) * cos(sphere.azimuth),
        sin(sphere.elevation),
        cos(sphere.elevation) * sin(sphere.azimuth));
    
    return coords * sphere.r;
}

Spherical cartesianToSpherical(float3 cartesian)
{
    Spherical ret;
    ret.r = length(cartesian);
    ret.elevation = asin(cartesian.y / ret.r);
    ret.azimuth = abs(cartesian.y / ret.r) == 1 ? 0 : atan2(cartesian.z, cartesian.x);
    return ret;
}
