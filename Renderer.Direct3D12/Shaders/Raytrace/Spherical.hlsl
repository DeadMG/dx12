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
    
    return normalize(coords) * sphere.r;
}

Spherical cartesianToSpherical(float3 cartesian)
{
    Spherical ret;
    ret.r = length(cartesian);
    ret.elevation = asin(cartesian.y / ret.r);
    ret.azimuth = abs(cartesian.y / ret.r) == 1 ? 0 : atan2(cartesian.z, cartesian.x);
    return ret;
}

float3x3 AngleAxis3x3(float angle, float3 axis)
{
    float c, s;
    sincos(angle, s, c);

    float t = 1 - c;
    float x = axis.x;
    float y = axis.y;
    float z = axis.z;

    return float3x3(
        t * x * x + c, t * x * y - s * z, t * x * z + s * y,
        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
        t * x * z - s * y, t * y * z + s * x, t * z * z + c
    );
}

float3 rotateNormal(float3 normal, Spherical rotation)
{
    Spherical normalSphere = cartesianToSpherical(normal);
    normalSphere.elevation += (PI / 2);
    float3 axis = sphericalToCartesian(normalSphere);
    
    float3 elevated = mul(normal, AngleAxis3x3(rotation.elevation, axis));
    float3 rotated = mul(elevated, AngleAxis3x3(rotation.azimuth, normal));
    
    return rotated;
}
