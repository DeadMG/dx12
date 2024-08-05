#pragma once

#include "Random.hlsl"

// Due to the different conventions in use in different materials,
// theta/phi will not be used, but rather, elevation/azimuth. This
// makes it more obvious what does what. Since we are LH Y-up then
// Y is affected by elevation only.
struct Spherical
{
    half r;
    half elevation;
    half azimuth;
};

Spherical randomSpherical(half r, inout uint seed)
{
    Spherical sphere;
    sphere.r = r;    
    sphere.elevation = acos((2 * uniformRand(seed)) - 1);
    sphere.azimuth = uniformRand(seed) * 2 * PI;
    return sphere;    
}

half3 sphericalToCartesian(Spherical sphere)
{
    half3 coords = half3(
        cos(sphere.elevation) * cos(sphere.azimuth),
        sin(sphere.elevation),
        cos(sphere.elevation) * sin(sphere.azimuth));
    
    return normalize(coords) * sphere.r;
}

Spherical cartesianToSpherical(half3 cartesian)
{
    Spherical ret;
    ret.r = length(cartesian);
    ret.elevation = asin(cartesian.y / ret.r);
    ret.azimuth = abs(cartesian.y / ret.r) == 1 ? 0 : atan2(cartesian.z, cartesian.x);
    return ret;
}

half3x3 AngleAxis3x3(half angle, half3 axis)
{
    half c, s;
    sincos(angle, s, c);

    half t = 1 - c;
    half x = axis.x;
    half y = axis.y;
    half z = axis.z;

    return half3x3(
        t * x * x + c, t * x * y - s * z, t * x * z + s * y,
        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
        t * x * z - s * y, t * y * z + s * x, t * z * z + c
    );
}

half3 rotateNormal(half3 normal, Spherical rotation)
{
    Spherical normalSphere = cartesianToSpherical(normal);
    normalSphere.elevation += (PI / 2);
    half3 axis = sphericalToCartesian(normalSphere);
    
    half3 elevated = mul(normal, AngleAxis3x3(rotation.elevation, axis));
    half3 rotated = mul(elevated, AngleAxis3x3(rotation.azimuth, normal));
    
    return rotated;
}
